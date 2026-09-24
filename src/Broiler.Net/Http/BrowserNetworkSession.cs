using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Broiler.Net.Cookies;
using Broiler.Net.Sites;

namespace Broiler.Net.Http;

/// <summary>
/// One profile's network session: a pooled HTTP handler with automatic cookies and redirects disabled,
/// the profile cookie store, and the Fetch-style request policy. Thread-safe; share one per profile.
/// <see cref="BrowserNetworkSessionOptions.Timeout"/> covers every hop up to the final response headers and
/// surfaces like HttpClient's: a <see cref="TaskCanceledException"/> whose inner exception is a <see cref="TimeoutException"/>.
/// </summary>
public sealed class BrowserNetworkSession : IBrowserRequestTransport, IDocumentCookieAccess, IDisposable
{
    // Cookie, Host and Origin, plus the message framing and connection headers the session and handler control.
    private static readonly HashSet<string> OwnedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cookie", "Cookie2", "Host", "Origin", "Content-Length", "Transfer-Encoding", "Connection", "Keep-Alive", "TE",
        "Trailer", "Upgrade", "Expect",
    };
    private static readonly HashSet<string> RequestBodyHeaders = new(StringComparer.OrdinalIgnoreCase)
        { "Content-Encoding", "Content-Language", "Content-Location", "Content-Type" };
    // Fetch "bad port".
    private static readonly HashSet<int> BadPorts =
    [
        0, 1, 7, 9, 11, 13, 15, 17, 19, 20, 21, 22, 23, 25, 37, 42, 43, 53, 69, 77, 79, 87, 95, 101, 102, 103, 104, 109, 110, 111,
        113, 115, 117, 119, 123, 135, 137, 139, 143, 161, 179, 389, 427, 465, 512, 513, 514, 515, 526, 530, 531, 532, 540, 548,
        554, 556, 563, 587, 601, 636, 989, 990, 993, 995, 1719, 1720, 1723, 2049, 3659, 4045, 4190, 5060, 5061, 6000, 6566,
        6665, 6666, 6667, 6668, 6669, 6679, 6697, 10080,
    ];

    private readonly BrowserNetworkSessionOptions _options;
    private readonly HttpMessageInvoker _invoker;
    private int _disposed;

    public BrowserNetworkSession(BrowserNetworkSessionOptions? options = null)
    {
        _options = options ?? new();
        ArgumentNullException.ThrowIfNull(_options.UserAgent, nameof(options));
        if (!FetchHeaders.IsHeaderValue(_options.UserAgent)) throw new ArgumentException("UserAgent must be a header value.", nameof(options));
        if (_options.AcceptLanguage is { } language && !FetchHeaders.IsHeaderValue(language))
            throw new ArgumentException("AcceptLanguage must be a header value.", nameof(options));
        if (_options.Timeout != Timeout.InfiniteTimeSpan && (_options.Timeout <= TimeSpan.Zero || _options.Timeout.TotalMilliseconds > int.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(options), "Timeout must be positive or infinite.");
        if (_options.MaximumRedirects < 0) throw new ArgumentOutOfRangeException(nameof(options), "MaximumRedirects must not be negative.");
        if (_options.Cookies is { } cookies)
        {
            // Same-site, partition and cookie acceptance decisions must share one Public Suffix List.
            if (_options.Sites is { } sites && !ReferenceEquals(sites, cookies.Sites))
                throw new ArgumentException("Sites must be the cookie store's resolver.", nameof(options));
            (Sites, Cookies) = (cookies.Sites, cookies);
        }
        else
        {
            Sites = _options.Sites ?? SiteResolver.Default;
            Cookies = new CookieStore(new CookiePolicy(Sites));
        }
        DocumentCookies = new DocumentCookieAccess(Cookies, _options.CookiesEnabled, _options.CookieObserverError);
        _invoker = new HttpMessageInvoker(_options.Handler ?? CreateHandler(_options), disposeHandler: _options.Handler is null);
    }

    public CookieStore Cookies { get; }
    public ISiteResolver Sites { get; }
    public bool CookiesEnabled => _options.CookiesEnabled;

    public Task<TransportResponse> SendAsync(HttpRequestMessage request, RequestContext context, CancellationToken cancellationToken = default)
    {
        CheckSend(request, context);
        return SendCore(request, context, async: true, cancellationToken).AsTask();
    }

    public TransportResponse Send(HttpRequestMessage request, RequestContext context, CancellationToken cancellationToken = default)
    {
        CheckSend(request, context);
        var pending = SendCore(request, context, async: false, cancellationToken);
        // With async false nothing awaits an incomplete operation, so the ValueTask has already completed.
        return pending.GetAwaiter().GetResult();
    }

    /// <summary>The document.cookie view of the profile store, for script bindings.</summary>
    public DocumentCookieAccess DocumentCookies { get; }

    public bool TryGetCookie(DocumentRequestContext document, out string cookie) => DocumentCookies.TryGetCookie(document, out cookie);
    public bool TrySetCookie(DocumentRequestContext document, string value) => DocumentCookies.TrySetCookie(document, value);

    /// <inheritdoc cref="DocumentCookieAccess.GetSiteForCookies"/>
    public Origin GetSiteForCookies(DocumentRequestContext document) => DocumentCookies.GetSiteForCookies(document);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) _invoker.Dispose();
    }

    private void CheckSend(HttpRequestMessage request, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    private static void CheckContext(RequestContext context)
    {
        var frame = RequestContext.IsFrameDestination(context.Destination);
        if (context.IsNavigation && !context.IsTopLevelNavigation && !frame)
            throw Error(TransportError.InvalidRequest, $"Navigate mode needs a document, iframe, frame, object or embed destination, not {context.Destination}.");
        if ((context.Container is not null) != (context.IsNavigation && frame))
            throw Error(TransportError.InvalidRequest, "Every nested navigation, and nothing else, has a container.");
        if (context.Client is null && !context.IsNavigation)
            throw Error(TransportError.InvalidRequest, "Only a navigation may omit the client.");
        if (context.IsUserReload && !context.IsNavigation) throw Error(TransportError.InvalidRequest, "Only a navigation can be a user reload.");
        if (context.ReloadWasSameSite && !context.IsUserReload) throw Error(TransportError.InvalidRequest, "ReloadWasSameSite needs IsUserReload.");
    }

    private async ValueTask<TransportResponse> SendCore(HttpRequestMessage request, RequestContext context, bool async, CancellationToken cancellationToken)
    {
        var url = request.RequestUri;
        if (url is null || !IsFetchable(url)) throw Error(TransportError.InvalidRequest, "The request URL must be an absolute HTTP(S) URL.");
        url = WithCanonicalHost(url);
        if (BadPorts.Contains(url.Port)) throw Error(TransportError.InvalidRequest, $"Port {url.Port} is blocked.");
        var method = FetchHeaders.NormalizeMethod(request.Method.Method);
        if (FetchHeaders.IsForbiddenMethod(method)) throw Error(TransportError.InvalidRequest, $"The {method} method is forbidden.");
        CheckContext(context);
        if (context.Mode == RequestMode.NoCors && !FetchHeaders.IsCorsSafelistedMethod(method))
            throw Error(TransportError.InvalidRequest, $"A no-cors request cannot use the {method} method.");
        var requestOrigin = context.Client?.Origin;
        // Fetch main fetch, "no-cors": a cross-origin request that does not follow redirects is a network error.
        if (context.Mode == RequestMode.NoCors && context.Redirect != RedirectMode.Follow && IsCrossOrigin(requestOrigin, url))
            throw Error(TransportError.InvalidRequest, "A cross-origin no-cors request must follow redirects.");
        var urlList = GetUrlList(context.RedirectChain, url);
        if (urlList.Count - 1 > _options.MaximumRedirects)
            throw Error(TransportError.RedirectLimit, $"More than {_options.MaximumRedirects} redirects.");
        var credentials = context.IsNavigation ? CredentialsMode.Include : context.Credentials;
        var clientSite = context.Client is null ? null : GetSiteForCookies(context.Client);
        var containerSite = context.Container is null ? null : GetSiteForCookies(context.Container);
        // The document that will use the response: the navigated frame's container, or the client.
        var (holder, holderSite) = context.Container is null ? (context.Client, clientSite) : (context.Container, containerSite);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_options.Timeout != Timeout.InfiniteTimeSpan) timeout.CancelAfter(_options.Timeout);
        var token = timeout.Token;
        try
        {
            var headers = GetAuthorHeaders(request, context);
            var body = request.Content is null ? null : await ReadBodyAsync(request.Content, async, token).ConfigureAwait(false);
            var tainting = ResponseTainting.Basic;
            var taintedOrigin = false;
            // Replay what a continued manual redirect chain already did.
            for (var i = 0; i + 1 < urlList.Count; i++)
            {
                tainting = Taint(context, requestOrigin, urlList[i], tainting);
                taintedOrigin |= TaintsOrigin(requestOrigin, urlList[i], urlList[i + 1]);
                LeaveOrigin(headers, urlList[0], urlList[i], urlList[i + 1]);
            }
            for (var hop = urlList.Count - 1; ; hop++)
            {
                token.ThrowIfCancellationRequested();
                var current = urlList[^1];
                if (context.HopPolicy is { } policy && !policy(current, hop))
                    throw Error(TransportError.Blocked, $"The host's policy blocks {current}.");
                tainting = Taint(context, requestOrigin, current, tainting);
                var includeCredentials = credentials == CredentialsMode.Include ||
                    (credentials == CredentialsMode.SameOrigin && tainting == ResponseTainting.Basic);
                var serializedOrigin = taintedOrigin ? "null" : requestOrigin?.ToString();

                if (tainting == ResponseTainting.Cors &&
                    (!FetchHeaders.IsCorsSafelistedMethod(method) || FetchHeaders.GetCorsUnsafeRequestHeaderNames(headers).Count > 0))
                    await PreflightAsync(current, method, headers, serializedOrigin!, includeCredentials, async, token).ConfigureAwait(false);

                var sameSite = GetSameSite(context, containerSite, clientSite, urlList);
                var cookieContext = new CookieRequestContext(current, sameSite, method, context.IsTopLevelNavigation,
                    GetPartitionKey(context, holder, holderSite, current));
                var message = CreateMessage(method, current, headers, body);
                if (serializedOrigin is not null && (tainting == ResponseTainting.Cors || method is not ("GET" or "HEAD")))
                    message.Headers.TryAddWithoutValidation("Origin", tainting != ResponseTainting.Cors && IsDowngrade(requestOrigin!, current)
                        ? "null" : serializedOrigin);
                if (includeCredentials && CookiesEnabled && Cookies.BuildRequestHeader(cookieContext) is { Length: > 0 } cookie)
                    message.Headers.TryAddWithoutValidation("Cookie", cookie);

                var response = await InvokeAsync(message, async, token).ConfigureAwait(false);
                try
                {
                    if (includeCredentials && CookiesEnabled && response.Headers.NonValidated.TryGetValues("Set-Cookie", out var setCookies))
                    {
                        try { Cookies.ReceiveResponseCookies(setCookies, cookieContext); }
                        catch (CookieObserverException error) { _options.CookieObserverError?.Invoke(error); }
                    }
                    if (tainting == ResponseTainting.Cors && !PassesCorsCheck(response, serializedOrigin!, includeCredentials))
                        throw Error(TransportError.Cors, $"The response from {current} failed the CORS check.");
                    TransportResponse Final(ResponseTainting type) => new(response, Array.AsReadOnly(urlList.ToArray()), type, credentials, sameSite);
                    if (!IsRedirect(response.StatusCode)) return Final(tainting);
                    // Fetch switches on the redirect mode before it reads Location.
                    if (context.Redirect == RedirectMode.Error) throw Error(TransportError.RedirectDisallowed, $"{current} redirected.");
                    if (context.Redirect == RedirectMode.Manual) return Final(context.IsNavigation ? tainting : ResponseTainting.OpaqueRedirect);
                    if (!response.Headers.NonValidated.TryGetValues("Location", out var locations)) return Final(tainting);

                    var location = ResolveLocation(current, locations);
                    if (hop >= _options.MaximumRedirects)
                        throw Error(TransportError.RedirectLimit, $"More than {_options.MaximumRedirects} redirects.");
                    var hasCredentials = location.UserInfo.Length > 0;
                    if (hasCredentials && (tainting == ResponseTainting.Cors || (context.Mode == RequestMode.Cors && IsCrossOrigin(requestOrigin, location))))
                        throw Error(TransportError.Cors, "A CORS redirect target must not include credentials.");
                    taintedOrigin |= TaintsOrigin(requestOrigin, current, location);
                    var status = (int)response.StatusCode;
                    if ((status is 301 or 302 && method == "POST") || (status == 303 && method is not ("GET" or "HEAD")))
                    {
                        method = "GET";
                        body = null;
                        // Content headers other than the request-body ones can only travel with a body, so they go too
                        // and a later preflight never announces a header that is not sent.
                        headers.RemoveAll(h => RequestBodyHeaders.Contains(h.Key) || NeedsContent(h.Key));
                    }
                    LeaveOrigin(headers, urlList[0], current, location);
                    urlList.Add(location);
                }
                catch
                {
                    response.Dispose();
                    throw;
                }
                response.Dispose();
            }
        }
        catch (OperationCanceledException error) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException($"The request was canceled due to the configured Timeout of {_options.Timeout.TotalSeconds} seconds elapsing.",
                new TimeoutException(error.Message, error), error.CancellationToken);
        }
    }

    private static List<Uri> GetUrlList(IReadOnlyList<Uri>? chain, Uri url)
    {
        var urlList = new List<Uri>();
        if (chain is not null)
        {
            if (chain.Count == 0) throw Error(TransportError.InvalidRequest, "A redirect chain has at least one URL.");
            foreach (var item in chain)
            {
                if (item is null || !IsFetchable(item)) throw Error(TransportError.InvalidRequest, "Every redirect chain URL must be an absolute HTTP(S) URL.");
                urlList.Add(WithCanonicalHost(item));
            }
        }
        urlList.Add(url);
        return urlList;
    }

    // Moves tainting away from basic at the first cross-origin URL; same-origin mode fails there instead.
    private static ResponseTainting Taint(RequestContext context, Origin? requestOrigin, Uri current, ResponseTainting tainting)
    {
        var crossOrigin = IsCrossOrigin(requestOrigin, current);
        if (context.Mode == RequestMode.SameOrigin && crossOrigin)
            throw Error(TransportError.SameOriginViolation, $"{current} is not same-origin with {requestOrigin}.");
        if (crossOrigin && tainting == ResponseTainting.Basic && context.Mode is RequestMode.NoCors or RequestMode.Cors)
            return context.Mode == RequestMode.NoCors ? ResponseTainting.Opaque : ResponseTainting.Cors;
        return tainting;
    }

    // Fetch's tainted origin: a cross-origin URL redirected to another origin.
    private static bool TaintsOrigin(Origin? requestOrigin, Uri current, Uri location) =>
        !Origin.FromUrl(current).IsSameOrigin(Origin.FromUrl(location)) && IsCrossOrigin(requestOrigin, current);

    // Fetch drops Authorization on a cross-origin redirect and recomputes Referer and the Sec- fetch metadata per hop.
    // Caller values describe the first URL, so they stop once the chain leaves its origin.
    private static void LeaveOrigin(List<KeyValuePair<string, string>> headers, Uri first, Uri current, Uri location)
    {
        var target = Origin.FromUrl(location);
        if (!Origin.FromUrl(current).IsSameOrigin(target)) headers.RemoveAll(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
        if (!Origin.FromUrl(first).IsSameOrigin(target))
            headers.RemoveAll(h => h.Key.Equals("Referer", StringComparison.OrdinalIgnoreCase) || h.Key.StartsWith("Sec-", StringComparison.OrdinalIgnoreCase));
    }

    private async ValueTask PreflightAsync(Uri url, string method, List<KeyValuePair<string, string>> headers, string origin,
        bool includeCredentials, bool async, CancellationToken cancellationToken)
    {
        // Fetch "CORS-preflight fetch": no credentials, no body, no redirects, no cache.
        var unsafeNames = FetchHeaders.GetCorsUnsafeRequestHeaderNames(headers);
        var message = CreateMessage("OPTIONS", url, [], null);
        message.Headers.TryAddWithoutValidation("Accept", "*/*");
        message.Headers.TryAddWithoutValidation("Access-Control-Request-Method", method);
        if (unsafeNames.Count > 0) message.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", string.Join(',', unsafeNames));
        message.Headers.TryAddWithoutValidation("Origin", origin);
        using var response = await InvokeAsync(message, async, cancellationToken).ConfigureAwait(false);
        if ((int)response.StatusCode is < 200 or > 299 || !PassesCorsCheck(response, origin, includeCredentials))
            throw Error(TransportError.Cors, $"The CORS preflight to {url} failed.");
        var methods = FetchHeaders.ParseTokenList(GetValues(response, "Access-Control-Allow-Methods"));
        var allowedHeaders = FetchHeaders.ParseTokenList(GetValues(response, "Access-Control-Allow-Headers"));
        if (methods is null || allowedHeaders is null) throw Error(TransportError.Cors, $"The CORS preflight to {url} returned invalid allow lists.");
        if (!methods.Contains(method, StringComparer.Ordinal) && !FetchHeaders.IsCorsSafelistedMethod(method) &&
            (includeCredentials || !methods.Contains("*")))
            throw Error(TransportError.Cors, $"The CORS preflight to {url} does not allow {method}.");
        var wildcard = !includeCredentials && allowedHeaders.Contains("*");
        foreach (var name in unsafeNames)
            if (!allowedHeaders.Contains(name, StringComparer.OrdinalIgnoreCase) && (!wildcard || name == "authorization"))
                throw Error(TransportError.Cors, $"The CORS preflight to {url} does not allow the {name} header.");
    }

    private async ValueTask<HttpResponseMessage> InvokeAsync(HttpRequestMessage message, bool async, CancellationToken cancellationToken) =>
        async ? await _invoker.SendAsync(message, cancellationToken).ConfigureAwait(false) : _invoker.Send(message, cancellationToken);

    private HttpRequestMessage CreateMessage(string method, Uri url, List<KeyValuePair<string, string>> headers, byte[]? body)
    {
        var message = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Version = BroilerHttpProtocol.Version,
            VersionPolicy = BroilerHttpProtocol.VersionPolicy,
            Content = body is null ? null : new ByteArrayContent(body),
        };
        foreach (var (name, value) in headers)
            if (!message.Headers.TryAddWithoutValidation(name, value)) message.Content?.Headers.TryAddWithoutValidation(name, value);
        if (!headers.Exists(h => h.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase)))
            message.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        return message;
    }

    private List<KeyValuePair<string, string>> GetAuthorHeaders(HttpRequestMessage request, RequestContext context)
    {
        var headers = new List<KeyValuePair<string, string>>();
        var fields = request.Content is null ? request.Headers.NonValidated : request.Headers.NonValidated.Concat(request.Content.Headers.NonValidated);
        foreach (var (name, values) in fields)
            if (!OwnedHeaders.Contains(name))
                foreach (var raw in values)
                {
                    // A CR, LF or NUL would put extra header lines (or a second request) on the wire.
                    var value = FetchHeaders.NormalizeHeaderValue(raw);
                    if (!FetchHeaders.IsHeaderValue(value)) throw Error(TransportError.InvalidRequest, $"The {name} header value is not a header value.");
                    headers.Add(new(name, value));
                }
        if (context.Mode == RequestMode.NoCors) FetchHeaders.RemoveNoCorsUnsafe(headers);
        // Fetch "fetch": the user agent's Accept and Accept-Language, which a preflight then sees as well.
        if (!Contains(headers, "Accept")) headers.Add(new("Accept", DefaultAccept(context.Destination)));
        if (_options.AcceptLanguage is { } language && !Contains(headers, "Accept-Language")) headers.Add(new("Accept-Language", language));
        return headers;
    }

    private static bool Contains(List<KeyValuePair<string, string>> headers, string name) =>
        headers.Exists(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string DefaultAccept(RequestDestination destination) => destination switch
    {
        RequestDestination.Document or RequestDestination.IFrame or RequestDestination.Frame => "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
        RequestDestination.Image => "image/png,image/svg+xml,image/*;q=0.8,*/*;q=0.5",
        RequestDestination.Style => "text/css,*/*;q=0.1",
        _ => "*/*",
    };

    // Content headers such as Content-Disposition or Expires, which HttpRequestHeaders refuses.
    private static bool NeedsContent(string name)
    {
        using var probe = new HttpRequestMessage();
        return !probe.Headers.TryAddWithoutValidation(name, "");
    }

    private static async ValueTask<byte[]> ReadBodyAsync(HttpContent content, bool async, CancellationToken cancellationToken)
    {
        if (async) return await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        using var stream = content.ReadAsStream(cancellationToken);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static Uri ResolveLocation(Uri current, HeaderStringValues locations)
    {
        if (locations.Count != 1 || !Uri.TryCreate(current, locations.First(), out var location))
            throw Error(TransportError.RedirectScheme, $"{current} sent an invalid Location.");
        // Fetch "location URL": a target without a fragment inherits the request's.
        if (!locations.First().Contains('#') && current.Fragment.Length > 0) location = new Uri(location.AbsoluteUri + current.Fragment);
        if (!IsFetchable(location)) throw Error(TransportError.RedirectScheme, $"{current} redirected to a URL that cannot be fetched: {location}.");
        location = WithCanonicalHost(location);
        if (BadPorts.Contains(location.Port)) throw Error(TransportError.RedirectScheme, $"{current} redirected to blocked port {location.Port}.");
        return location;
    }

    // Fetch "CORS check".
    private static bool PassesCorsCheck(HttpResponseMessage response, string origin, bool includeCredentials)
    {
        if (GetCombined(response, "Access-Control-Allow-Origin") is not { } allowOrigin) return false;
        if (!includeCredentials && allowOrigin == "*") return true;
        if (allowOrigin != origin) return false;
        return !includeCredentials || GetCombined(response, "Access-Control-Allow-Credentials") == "true";
    }

    private static string? GetCombined(HttpResponseMessage response, string name) =>
        response.Headers.NonValidated.TryGetValues(name, out var values) ? string.Join(", ", values) : null;

    private static IEnumerable<string> GetValues(HttpResponseMessage response, string name) =>
        response.Headers.NonValidated.TryGetValues(name, out var values) ? values : [];

    private SameSiteStatus GetSameSite(RequestContext context, Origin? containerSite, Origin? clientSite, List<Uri> urlList)
    {
        // Every URL in the redirect chain must be same-site with a site for cookies; no site (no client) is same-site.
        bool SameSiteWith(Origin? site) => site is null || (!site.IsOpaque && urlList.TrueForAll(u => Sites.IsSameSite(Origin.FromUrl(u), site)));
        // 6265bis 5.2: the client (the initiator) decides, or the recorded status for a UI reload. A frame's own
        // ancestors must be same-site too, as Chromium requires, so no initiator can make a cross-site frame first-party.
        var sameSite = SameSiteWith(containerSite) && (context.IsUserReload ? context.ReloadWasSameSite : SameSiteWith(clientSite));
        return sameSite ? SameSiteStatus.SameSite : SameSiteStatus.CrossSite;
    }

    private CookiePartitionKey? GetPartitionKey(RequestContext context, DocumentRequestContext? holder, Origin? holderSite, Uri current)
    {
        if (context.IsTopLevelNavigation) return Sites.GetSite(current) is { } site ? new(site, false) : null;
        if (holder is null || holderSite is null) return null;
        // The ancestor bit describes where the response is used, not the redirect chain that led there.
        return Sites.GetSite(TopLevelOrigin(holder.TopLevel)) is { } top
            ? new(top, holderSite.IsOpaque || !Sites.IsSameSite(Origin.FromUrl(current), holderSite)) : null;
    }

    private static Origin TopLevelOrigin(DocumentRequestContext top) => DocumentCookieAccess.TopLevelOrigin(top);

    private static bool IsCrossOrigin(Origin? requestOrigin, Uri url) => requestOrigin is not null && !requestOrigin.IsSameOrigin(Origin.FromUrl(url));

    // Fetch "append a request Origin header" under the default referrer policy (strict-origin-when-cross-origin).
    private static bool IsDowngrade(Origin requestOrigin, Uri url) => requestOrigin.Scheme == "https" && url.Scheme != "https";

    private static bool IsRedirect(HttpStatusCode status) => (int)status is 301 or 302 or 303 or 307 or 308;

    private static bool IsFetchable(Uri url)
    {
        try { return url.IsAbsoluteUri && url.Scheme is "http" or "https" && HostNames.TryGetHttpHost(url, out _); }
        catch (UriFormatException) { return false; }
    }

    // URL Standard hosts: a DNS-typed name that parses as IPv4 ("127.0.0.1.", "127.1.", full-width digits) is that
    // address on the wire too, so the connection and proxy decision match the cookie and origin identity.
    private static Uri WithCanonicalHost(Uri url) =>
        url.HostNameType == UriHostNameType.Dns && HostNames.TryGetHttpHost(url, out var host) && HostNames.IsIp(host)
            ? new UriBuilder(url) { Host = host }.Uri : url;

    private static TransportException Error(TransportError error, string message) => new(error, message);

    internal static HttpMessageHandler CreateHandler(BrowserNetworkSessionOptions options) =>
        new LoopbackRouting.RoutingHandler(CreateSocketsHandler(options, direct: true), CreateSocketsHandler(options, direct: false));

    private static SocketsHttpHandler CreateSocketsHandler(BrowserNetworkSessionOptions options, bool direct) => new()
    {
        UseCookies = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = options.PooledConnectionLifetime,
        ConnectTimeout = options.ConnectTimeout,
        // Loopback traffic never uses a proxy and localhost names never reach DNS; other hosts use the system proxy.
        UseProxy = !direct,
        ConnectCallback = direct ? LoopbackRouting.ConnectAsync : null,
        // Header fields are byte strings: Latin-1 keeps every octet of Cookie and Set-Cookie intact. Location keeps
        // the handler's UTF-8 decoding, as browsers resolve redirect targets.
        RequestHeaderEncodingSelector = (_, _) => Encoding.Latin1,
        ResponseHeaderEncodingSelector = (name, _) => name.Equals("Location", StringComparison.OrdinalIgnoreCase) ? null : Encoding.Latin1,
    };
}

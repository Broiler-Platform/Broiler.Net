// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   41
// Annotated:        41/41
// Exempt:           7
// Human-reviewed:   0/41
// IP risk:          Low
// Security risk:    High
// Criteria:         40/31
// Resource impact:  7/10 max
// Unverified:       41
//
// GENERATED - DO NOT EDIT MANUALLY

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
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=915513
// Broiler-Falsified-If: a Cookie header is attached to a request whose credentials mode is omit, or to the cross-origin hop of a same-origin-credentials request
// Broiler-Human:        PENDING
public sealed class BrowserNetworkSession : IBrowserRequestTransport, IDocumentCookieAccess, IDisposable
{
    // Cookie, Host and Origin, plus the message framing and connection headers the session and handler control.
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=02CEF0
    // Broiler-Falsified-If: a caller-set Cookie, Host, Origin or Transfer-Encoding header reaches the wire in place of the value the session or handler writes
    // Broiler-Human:        PENDING
    private static readonly HashSet<string> OwnedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cookie", "Cookie2", "Host", "Origin", "Content-Length", "Transfer-Encoding", "Connection", "Keep-Alive", "TE",
        "Trailer", "Upgrade", "Expect",
    };
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=1; Fingerprint=91A619
    // Broiler-Falsified-If: a Content-Language or Content-Encoding header survives a 303 redirect that turned a POST into a GET
    // Broiler-Human:        PENDING
    private static readonly HashSet<string> RequestBodyHeaders = new(StringComparer.OrdinalIgnoreCase)
        { "Content-Encoding", "Content-Language", "Content-Location", "Content-Type" };
    // Fetch "bad port".
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.9; IP=None; Security=High; Resources=1; Fingerprint=FE5CE8
    // Broiler-Falsified-If: a port on the Fetch bad port list, such as 25, 6697 or 10080, is missing from the set, so a request to it is sent
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=BAB4B0
    // Broiler-Falsified-If: options whose Sites differ from the supplied cookie store's resolver construct a session instead of throwing ArgumentException
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=5AA4DE
    // Broiler-Falsified-If: a call made after Dispose, or with a null request or context, reaches SendCore instead of throwing before any work starts
    // Broiler-Human:        PENDING
    public Task<TransportResponse> SendAsync(HttpRequestMessage request, RequestContext context, CancellationToken cancellationToken = default)
    {
        CheckSend(request, context);
        return SendCore(request, context, async: true, cancellationToken).AsTask();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=7D34F0
    // Broiler-Falsified-If: the synchronous path awaits real asynchronous work, so GetResult is called on a ValueTask that has not completed
    // Broiler-Human:        PENDING
    public TransportResponse Send(HttpRequestMessage request, RequestContext context, CancellationToken cancellationToken = default)
    {
        CheckSend(request, context);
        var pending = SendCore(request, context, async: false, cancellationToken);
        // With async false nothing awaits an incomplete operation, so the ValueTask has already completed.
        return pending.GetAwaiter().GetResult();
    }

    /// <summary>The document.cookie view of the profile store, for script bindings.</summary>
    public DocumentCookieAccess DocumentCookies { get; }

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=5; Fingerprint=19158B
    // Broiler-Falsified-If: document.cookie read through the session differs from DocumentCookies.TryGetCookie for the same document
    // Broiler-Human:        PENDING
    public bool TryGetCookie(DocumentRequestContext document, out string cookie) => DocumentCookies.TryGetCookie(document, out cookie);
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=5; Fingerprint=F52297
    // Broiler-Falsified-If: a document.cookie write through the session lands in a store other than Cookies, so the next request does not carry it
    // Broiler-Human:        PENDING
    public bool TrySetCookie(DocumentRequestContext document, string value) => DocumentCookies.TrySetCookie(document, value);

    /// <inheritdoc cref="DocumentCookieAccess.GetSiteForCookies"/>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=5; Fingerprint=923121
    // Broiler-Falsified-If: it returns a site for cookies that differs from DocumentCookies.GetSiteForCookies for the same document
    // Broiler-Human:        PENDING
    public Origin GetSiteForCookies(DocumentRequestContext document) => DocumentCookies.GetSiteForCookies(document);

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=3AB337
    // Broiler-Falsified-If: two threads calling Dispose at the same time both dispose the invoker, or a second Dispose call throws
    // Broiler-Human:        PENDING
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) _invoker.Dispose();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=1434B7
    // Broiler-Falsified-If: a Send or SendAsync call made after Dispose has returned passes through without an ObjectDisposedException
    // Broiler-Human:        PENDING
    private void CheckSend(HttpRequestMessage request, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=5F9B73
    // Broiler-Falsified-If: a non-navigation request that names a Container, or omits its Client, passes without an InvalidRequest error
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=FETCH s4.5; IP=Low; Security=High; Resources=7; Fingerprint=1D97EA
    // Broiler-Falsified-If: a same-origin-credentials cors request redirected to another origin sends the Cookie header on the cross-origin hop
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=3751F0
    // Broiler-Falsified-If: an empty redirect chain, or one holding a relative or non-HTTP(S) URL, is accepted instead of raising InvalidRequest
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=FETCH s4.1; IP=Low; Security=High; Resources=3; Fingerprint=F7C906
    // Broiler-Falsified-If: a cors-mode request that reaches a cross-origin URL keeps basic tainting, so its response skips the CORS check
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=FETCH s4.5; IP=Low; Security=High; Resources=3; Fingerprint=FC161B
    // Broiler-Falsified-If: a redirect from a cross-origin URL to a third origin is not reported as tainting, so the next hop sends the real Origin instead of null
    // Broiler-Human:        PENDING
    private static bool TaintsOrigin(Origin? requestOrigin, Uri current, Uri location) =>
        !Origin.FromUrl(current).IsSameOrigin(Origin.FromUrl(location)) && IsCrossOrigin(requestOrigin, current);

    // Fetch drops Authorization on a cross-origin redirect and recomputes Referer and the Sec- fetch metadata per hop.
    // Caller values describe the first URL, so they stop once the chain leaves its origin.
    // Broiler-AI:           Origin=AI; Spec=FETCH s4.5; IP=Low; Security=High; Resources=3; Fingerprint=FD5725
    // Broiler-Falsified-If: an Authorization header survives a redirect whose location is not same-origin with the current URL
    // Broiler-Human:        PENDING
    private static void LeaveOrigin(List<KeyValuePair<string, string>> headers, Uri first, Uri current, Uri location)
    {
        var target = Origin.FromUrl(location);
        if (!Origin.FromUrl(current).IsSameOrigin(target)) headers.RemoveAll(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
        if (!Origin.FromUrl(first).IsSameOrigin(target))
            headers.RemoveAll(h => h.Key.Equals("Referer", StringComparison.OrdinalIgnoreCase) || h.Key.StartsWith("Sec-", StringComparison.OrdinalIgnoreCase));
    }

    // Broiler-AI:           Origin=AI; Spec=FETCH s4.8; IP=Low; Security=High; Resources=5; Fingerprint=71D68F
    // Broiler-Falsified-If: a credentialed preflight answered with Access-Control-Allow-Headers: * lets a header that is not CORS-safelisted through
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=AE8DA5
    // Broiler-Falsified-If: with async false the returned ValueTask is still pending when the call returns
    // Broiler-Human:        PENDING
    private async ValueTask<HttpResponseMessage> InvokeAsync(HttpRequestMessage message, bool async, CancellationToken cancellationToken)
    {
        if (async) return await _invoker.SendAsync(message, cancellationToken).ConfigureAwait(false);

        // SocketsHttpHandler.Send rejects HTTP/2. Keep the synchronous browser API, but run
        // the asynchronous transport on the pool so a handler cannot capture the blocked
        // UI/JavaScript synchronization context. GetResult unwraps cancellation and errors.
        return Task.Run(() => _invoker.SendAsync(message, cancellationToken), cancellationToken)
            .GetAwaiter().GetResult();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=EF52D2
    // Broiler-Falsified-If: a message built from a header list that already names User-Agent carries the session's default User-Agent as well
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=B2D115
    // Broiler-Falsified-If: a caller-set header value with a CR or LF between other characters is copied into the list instead of raising InvalidRequest
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=3; Fingerprint=0D8EC1
    // Broiler-Falsified-If: a header named accept-language in lower case is not found when looking for Accept-Language
    // Broiler-Human:        PENDING
    private static bool Contains(List<KeyValuePair<string, string>> headers, string name) =>
        headers.Exists(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase));

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=0550BA
    // Broiler-Falsified-If: an iframe or frame navigation is sent with the */* Accept value instead of the document one
    // Broiler-Human:        PENDING
    private static string DefaultAccept(RequestDestination destination) => destination switch
    {
        RequestDestination.Document or RequestDestination.IFrame or RequestDestination.Frame => "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
        RequestDestination.Image => "image/png,image/svg+xml,image/*;q=0.8,*/*;q=0.5",
        RequestDestination.Style => "text/css,*/*;q=0.1",
        _ => "*/*",
    };

    // Content headers such as Content-Disposition or Expires, which HttpRequestHeaders refuses.
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=EBE614
    // Broiler-Falsified-If: a header that HttpRequestHeaders accepts, such as Accept, is reported as needing content and dropped on a POST-to-GET redirect
    // Broiler-Human:        PENDING
    private static bool NeedsContent(string name)
    {
        using var probe = new HttpRequestMessage();
        return !probe.Headers.TryAddWithoutValidation(name, "");
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=7; Fingerprint=F330A0
    // Broiler-Falsified-If: the synchronous path returns fewer bytes than the content stream yields, so a 307 replay sends a truncated body
    // Broiler-Human:        PENDING
    private static async ValueTask<byte[]> ReadBodyAsync(HttpContent content, bool async, CancellationToken cancellationToken)
    {
        if (async) return await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        using var stream = content.ReadAsStream(cancellationToken);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    // Broiler-AI:           Origin=AI; Spec=FETCH s4.5; IP=Low; Security=High; Resources=3; Fingerprint=F3C82B
    // Broiler-Falsified-If: a Location that resolves to a file:, data: or other non-HTTP(S) URL is returned instead of raising RedirectScheme
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=FETCH s4.10; IP=Low; Security=High; Resources=3; Fingerprint=72F265
    // Broiler-Falsified-If: a credentialed response with Access-Control-Allow-Origin: * passes the check
    // Broiler-Human:        PENDING
    private static bool PassesCorsCheck(HttpResponseMessage response, string origin, bool includeCredentials)
    {
        if (GetCombined(response, "Access-Control-Allow-Origin") is not { } allowOrigin) return false;
        if (!includeCredentials && allowOrigin == "*") return true;
        if (allowOrigin != origin) return false;
        return !includeCredentials || GetCombined(response, "Access-Control-Allow-Credentials") == "true";
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=CECEB8
    // Broiler-Falsified-If: a response with two Access-Control-Allow-Origin fields yields one of them alone, so the duplicated header can pass the CORS check
    // Broiler-Human:        PENDING
    private static string? GetCombined(HttpResponseMessage response, string name) =>
        response.Headers.NonValidated.TryGetValues(name, out var values) ? string.Join(", ", values) : null;

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=83B1F0
    // Broiler-Falsified-If: an allow list split over two Access-Control-Allow-Methods fields yields the values of only one field
    // Broiler-Human:        PENDING
    private static IEnumerable<string> GetValues(HttpResponseMessage response, string name) =>
        response.Headers.NonValidated.TryGetValues(name, out var values) ? values : [];

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.2; IP=Low; Security=High; Resources=5; Fingerprint=962270
    // Broiler-Falsified-If: a request whose redirect chain passes through a cross-site URL is given SameSite status, so Strict cookies go with it
    // Broiler-Human:        PENDING
    private SameSiteStatus GetSameSite(RequestContext context, Origin? containerSite, Origin? clientSite, List<Uri> urlList)
    {
        // Every URL in the redirect chain must be same-site with a site for cookies; no site (no client) is same-site.
        bool SameSiteWith(Origin? site) => site is null || (!site.IsOpaque && urlList.TrueForAll(u => Sites.IsSameSite(Origin.FromUrl(u), site)));
        // 6265bis 5.2: the client (the initiator) decides, or the recorded status for a UI reload. A frame's own
        // ancestors must be same-site too, as Chromium requires, so no initiator can make a cross-site frame first-party.
        var sameSite = SameSiteWith(containerSite) && (context.IsUserReload ? context.ReloadWasSameSite : SameSiteWith(clientSite));
        return sameSite ? SameSiteStatus.SameSite : SameSiteStatus.CrossSite;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=AC11E5
    // Broiler-Falsified-If: a request used by an iframe that is cross-site with its top-level document gets a partition key whose cross-site ancestor bit is false
    // Broiler-Human:        PENDING
    private CookiePartitionKey? GetPartitionKey(RequestContext context, DocumentRequestContext? holder, Origin? holderSite, Uri current)
    {
        if (context.IsTopLevelNavigation) return Sites.GetSite(current) is { } site ? new(site, false) : null;
        if (holder is null || holderSite is null) return null;
        // The ancestor bit describes where the response is used, not the redirect chain that led there.
        return Sites.GetSite(TopLevelOrigin(holder.TopLevel)) is { } top
            ? new(top, holderSite.IsOpaque || !Sites.IsSameSite(Origin.FromUrl(current), holderSite)) : null;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=3; Fingerprint=ABD447
    // Broiler-Falsified-If: it returns an origin that differs from DocumentCookieAccess.TopLevelOrigin for the same document
    // Broiler-Human:        PENDING
    private static Origin TopLevelOrigin(DocumentRequestContext top) => DocumentCookieAccess.TopLevelOrigin(top);

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=8D4B53
    // Broiler-Falsified-If: a URL that differs from the request origin only in scheme or port is reported as not cross-origin
    // Broiler-Human:        PENDING
    private static bool IsCrossOrigin(Origin? requestOrigin, Uri url) => requestOrigin is not null && !requestOrigin.IsSameOrigin(Origin.FromUrl(url));

    // Fetch "append a request Origin header" under the default referrer policy (strict-origin-when-cross-origin).
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=E06518
    // Broiler-Falsified-If: an https request origin paired with an http URL returns false, so a POST from https to http carries the full Origin
    // Broiler-Human:        PENDING
    private static bool IsDowngrade(Origin requestOrigin, Uri url) => requestOrigin.Scheme == "https" && url.Scheme != "https";

    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.3; IP=Low; Security=Medium; Resources=0; Fingerprint=D1A685
    // Broiler-Falsified-If: a 307 or 308 response is returned as final rather than followed
    // Broiler-Human:        PENDING
    private static bool IsRedirect(HttpStatusCode status) => (int)status is 301 or 302 or 303 or 307 or 308;

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=524C27
    // Broiler-Falsified-If: a relative URL, or a file:, data: or ftp: URL, is reported as fetchable
    // Broiler-Human:        PENDING
    private static bool IsFetchable(Uri url)
    {
        try { return url.IsAbsoluteUri && url.Scheme is "http" or "https" && HostNames.TryGetHttpHost(url, out _); }
        catch (UriFormatException) { return false; }
    }

    // URL Standard hosts: a DNS-typed name that parses as IPv4 ("127.0.0.1.", "127.1.", full-width digits) is that
    // address on the wire too, so the connection and proxy decision match the cookie and origin identity.
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=E2B91F
    // Broiler-Falsified-If: a URL whose host is 127.1. or 127.0.0.1. with its trailing dot reaches the handler with that DNS-typed host rather than 127.0.0.1
    // Broiler-Human:        PENDING
    private static Uri WithCanonicalHost(Uri url) =>
        url.HostNameType == UriHostNameType.Dns && HostNames.TryGetHttpHost(url, out var host) && HostNames.IsIp(host)
            ? new UriBuilder(url) { Host = host }.Uri : url;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=164793
    // Broiler-Human:        PENDING
    private static TransportException Error(TransportError error, string message) => new(error, message);

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=8E88B0
    // Broiler-Falsified-If: the direct and proxied handlers are passed in swapped order, so loopback traffic goes through the system proxy
    // Broiler-Human:        PENDING
    internal static HttpMessageHandler CreateHandler(BrowserNetworkSessionOptions options) =>
        new LoopbackRouting.RoutingHandler(CreateSocketsHandler(options, direct: true), CreateSocketsHandler(options, direct: false));

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=C473CC
    // Broiler-Falsified-If: a handler built here follows redirects or sends stored cookies on its own, so a hop skips the session's CORS and cookie rules
    // Broiler-Human:        PENDING
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

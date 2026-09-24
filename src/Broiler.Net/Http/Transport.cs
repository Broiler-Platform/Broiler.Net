using Broiler.Net.Cookies;

namespace Broiler.Net.Http;

/// <summary>
/// Profile-scoped request transport. It owns the Cookie header, redirects and the per-hop cookie boundary,
/// credentials modes, CORS and response tainting. There is no process-global fallback profile.
/// </summary>
public interface IBrowserRequestTransport
{
    /// <summary>Sends a request and follows redirects per <see cref="RequestContext.Redirect"/>. Network errors
    /// (including CORS failures) throw <see cref="TransportException"/>; HTTP error statuses are responses.</summary>
    Task<TransportResponse> SendAsync(HttpRequestMessage request, RequestContext context, CancellationToken cancellationToken = default);

    /// <summary>Synchronous variant for renderer loaders; never blocks on a captured synchronization context.</summary>
    TransportResponse Send(HttpRequestMessage request, RequestContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// The "non-HTTP" cookie API a document binding needs (document.cookie). It cannot reach HttpOnly cookies
/// or claim HTTP privileges; give bindings this interface, not the transport's cookie store.
/// </summary>
public interface IDocumentCookieAccess
{
    /// <summary>The global cookie setting that navigator.cookieEnabled reflects.</summary>
    bool CookiesEnabled { get; }

    /// <summary>False when the document's origin is opaque (the binding throws SecurityError). Cookie-averse
    /// documents succeed with an empty string.</summary>
    bool TryGetCookie(DocumentRequestContext document, out string cookie);

    /// <summary>False when the document's origin is opaque. Cookie-averse documents succeed without effect.</summary>
    bool TrySetCookie(DocumentRequestContext document, string value);
}

/// <summary>A network error: the request produced no response (Fetch "network error").</summary>
public sealed class TransportException : HttpRequestException
{
    public TransportException(TransportError error, string message) : base(message) => Error = error;
    public TransportError Error { get; }
}

public sealed record BrowserNetworkSessionOptions
{
    /// <summary>The profile's cookie store; a new in-memory store when null.</summary>
    public CookieStore? Cookies { get; init; }
    /// <summary>The resolver for a new store. With <see cref="Cookies"/> it must be null or that store's <see cref="CookieStore.Sites"/>.</summary>
    public Sites.ISiteResolver? Sites { get; init; }
    public string UserAgent { get; init; } = BroilerUserAgent.Value;
    /// <summary>Accept-Language for requests that do not set one (for example "en-US,en;q=0.9"); none when null.</summary>
    public string? AcceptLanguage { get; init; }
    /// <summary>Overall budget for one request including redirects; callers may cancel earlier.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(100);
    public int MaximumRedirects { get; init; } = 20;
    /// <summary>When false no cookie is sent, stored or exposed to documents.</summary>
    public bool CookiesEnabled { get; init; } = true;
    public TimeSpan PooledConnectionLifetime { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(15);
    /// <summary>Test seam. It must not manage cookies or follow redirects, and must support synchronous Send.</summary>
    public HttpMessageHandler? Handler { get; init; }
    /// <summary>Receives cookie observer failures; they never fail the request.</summary>
    public Action<CookieObserverException>? CookieObserverError { get; init; }
}

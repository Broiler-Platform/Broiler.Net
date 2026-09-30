// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           13
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    High
// Criteria:         7/6
// Resource impact:  0/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Net.Cookies;

namespace Broiler.Net.Http;

/// <summary>
/// Profile-scoped request transport. It owns the Cookie header, redirects and the per-hop cookie boundary,
/// credentials modes, CORS and response tainting. There is no process-global fallback profile.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=A9B41A
// Broiler-Falsified-If: an implementation falls back to a process-global cookie store when it has no profile, so two profiles share cookies
// Broiler-Human:        PENDING
public interface IBrowserRequestTransport
{
    /// <summary>Sends a request and follows redirects per <see cref="RequestContext.Redirect"/>. Network errors
    /// (including CORS failures) throw <see cref="TransportException"/>; HTTP error statuses are responses.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=B27437
    // Broiler-Falsified-If: SendAsync returns a response for a request whose CORS check failed instead of throwing TransportException with TransportError.Cors
    // Broiler-Human:        PENDING
    Task<TransportResponse> SendAsync(HttpRequestMessage request, RequestContext context, CancellationToken cancellationToken = default);

    /// <summary>Synchronous variant for renderer loaders; never blocks on a captured synchronization context.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=59A3D6
    // Broiler-Falsified-If: Send follows a redirect for a context whose Redirect is RedirectMode.Error instead of throwing TransportException
    // Broiler-Human:        PENDING
    TransportResponse Send(HttpRequestMessage request, RequestContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// The "non-HTTP" cookie API a document binding needs (document.cookie). It cannot reach HttpOnly cookies
/// or claim HTTP privileges; give bindings this interface, not the transport's cookie store.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=C108B0
// Broiler-Falsified-If: an implementation hands the binding the transport's CookieStore, through which document.cookie reads an HttpOnly cookie
// Broiler-Human:        PENDING
public interface IDocumentCookieAccess
{
    /// <summary>The global cookie setting that navigator.cookieEnabled reflects.</summary>
    bool CookiesEnabled { get; }

    /// <summary>False when the document's origin is opaque (the binding throws SecurityError). Cookie-averse
    /// documents succeed with an empty string.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=397E15
    // Broiler-Falsified-If: TryGetCookie includes an HttpOnly cookie in the string it returns for document.cookie
    // Broiler-Human:        PENDING
    bool TryGetCookie(DocumentRequestContext document, out string cookie);

    /// <summary>False when the document's origin is opaque. Cookie-averse documents succeed without effect.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=079888
    // Broiler-Falsified-If: a document.cookie write through TrySetCookie replaces an existing HttpOnly cookie of the same name, domain and path
    // Broiler-Human:        PENDING
    bool TrySetCookie(DocumentRequestContext document, string value);
}

/// <summary>A network error: the request produced no response (Fetch "network error").</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=417575
// Broiler-Human:        PENDING
public sealed class TransportException : HttpRequestException
{
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=CF977B
    // Broiler-Human:        PENDING
    public TransportException(TransportError error, string message) : base(message) => Error = error;
    public TransportError Error { get; }
}

// Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=33E385
// Broiler-Falsified-If: default options let a session follow more than 20 redirects for one request
// Broiler-Human:        PENDING
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

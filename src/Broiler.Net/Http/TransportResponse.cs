using Broiler.Net.Cookies;

namespace Broiler.Net.Http;

/// <summary>
/// The final response of a request. <see cref="Headers"/> and <see cref="StatusCode"/> are privileged: Headers holds
/// every received field, including Set-Cookie, with repeated fields preserved. Give pages only
/// <see cref="GetScriptVisibleHeaders"/> and <see cref="ScriptVisibleStatusCode"/>, and no body when
/// <see cref="Tainting"/> is <see cref="ResponseTainting.Opaque"/> or <see cref="ResponseTainting.OpaqueRedirect"/>.
/// </summary>
public sealed class TransportResponse : IDisposable
{
    public TransportResponse(HttpResponseMessage message, IReadOnlyList<Uri> urlList, ResponseTainting tainting,
        CredentialsMode credentials = CredentialsMode.Include, SameSiteStatus sameSite = SameSiteStatus.CrossSite)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(urlList);
        if (urlList.Count == 0) throw new ArgumentException("A response has at least one URL.", nameof(urlList));
        (Message, UrlList, Tainting, Credentials, SameSite) = (message, urlList, tainting, credentials, sameSite);
        Headers = Array.AsReadOnly(message.Headers.NonValidated
            .Concat(message.Content.Headers.NonValidated)
            .SelectMany(h => h.Value.Select(v => new KeyValuePair<string, string>(h.Key, v)))
            .ToArray());
    }

    /// <summary>The final HTTP response; its Content is the body.</summary>
    public HttpResponseMessage Message { get; }
    public int StatusCode => (int)Message.StatusCode;
    public Uri FinalUrl => UrlList[^1];
    /// <summary>The request URL followed by every redirect target.</summary>
    public IReadOnlyList<Uri> UrlList { get; }
    public bool Redirected => UrlList.Count > 1;
    public ResponseTainting Tainting { get; }
    /// <summary>The request's credentials mode (include for navigations).</summary>
    public CredentialsMode Credentials { get; }
    /// <summary>6265bis 5.2 status of the final hop. Keep it with a navigation's history entry for <see cref="RequestContext.ReloadWasSameSite"/>.</summary>
    public SameSiteStatus SameSite { get; }
    public IReadOnlyList<KeyValuePair<string, string>> Headers { get; }

    /// <summary>The status a page sees: 0 for opaque and opaque-redirect responses.</summary>
    public int ScriptVisibleStatusCode => Tainting is ResponseTainting.Opaque or ResponseTainting.OpaqueRedirect ? 0 : StatusCode;

    /// <summary>Fetch's filtered-response header list for this tainting and credentials mode (never Set-Cookie or Set-Cookie2).</summary>
    public IReadOnlyList<KeyValuePair<string, string>> GetScriptVisibleHeaders() =>
        FetchHeaders.FilterResponseHeaders(Headers, Tainting, Credentials == CredentialsMode.Include);

    public void Dispose() => Message.Dispose();
}

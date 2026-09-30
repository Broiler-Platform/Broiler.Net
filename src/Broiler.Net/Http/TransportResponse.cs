// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           6
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    High
// Criteria:         5/3
// Resource impact:  5/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Net.Cookies;

namespace Broiler.Net.Http;

/// <summary>
/// The final response of a request. <see cref="Headers"/> and <see cref="StatusCode"/> are privileged: Headers holds
/// every received field, including Set-Cookie, with repeated fields preserved. Give pages only
/// <see cref="GetScriptVisibleHeaders"/> and <see cref="ScriptVisibleStatusCode"/>, and no body when
/// <see cref="Tainting"/> is <see cref="ResponseTainting.Opaque"/> or <see cref="ResponseTainting.OpaqueRedirect"/>.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=3B9949
// Broiler-Falsified-If: a member here other than Headers, StatusCode and Message returns a Set-Cookie value or an opaque response's real status
// Broiler-Human:        PENDING
public sealed class TransportResponse : IDisposable
{
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=7A9453
    // Broiler-Falsified-If: a response with a repeated field, such as two Link headers, yields one Headers entry with the values joined instead of one entry per value
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=E305BA
    // Broiler-Human:        PENDING
    public int StatusCode => (int)Message.StatusCode;
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=835F06
    // Broiler-Falsified-If: FinalUrl returns an entry other than the last of UrlList, so a redirected document takes the origin of a URL it did not load from
    // Broiler-Human:        PENDING
    public Uri FinalUrl => UrlList[^1];
    /// <summary>The request URL followed by every redirect target.</summary>
    public IReadOnlyList<Uri> UrlList { get; }
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=3E7095
    // Broiler-Human:        PENDING
    public bool Redirected => UrlList.Count > 1;
    public ResponseTainting Tainting { get; }
    /// <summary>The request's credentials mode (include for navigations).</summary>
    public CredentialsMode Credentials { get; }
    /// <summary>6265bis 5.2 status of the final hop. Keep it with a navigation's history entry for <see cref="RequestContext.ReloadWasSameSite"/>.</summary>
    public SameSiteStatus SameSite { get; }
    public IReadOnlyList<KeyValuePair<string, string>> Headers { get; }

    /// <summary>The status a page sees: 0 for opaque and opaque-redirect responses.</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.6; IP=Low; Security=High; Resources=0; Fingerprint=131ED6
    // Broiler-Falsified-If: ScriptVisibleStatusCode returns the real status for an OpaqueRedirect response instead of 0
    // Broiler-Human:        PENDING
    public int ScriptVisibleStatusCode => Tainting is ResponseTainting.Opaque or ResponseTainting.OpaqueRedirect ? 0 : StatusCode;

    /// <summary>Fetch's filtered-response header list for this tainting and credentials mode (never Set-Cookie or Set-Cookie2).</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.6; IP=None; Security=High; Resources=5; Fingerprint=5FC432
    // Broiler-Falsified-If: a Cors response to a request with credentials mode include exposes every header when Access-Control-Expose-Headers is *
    // Broiler-Human:        PENDING
    public IReadOnlyList<KeyValuePair<string, string>> GetScriptVisibleHeaders() =>
        FetchHeaders.FilterResponseHeaders(Headers, Tainting, Credentials == CredentialsMode.Include);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=0B643F
    // Broiler-Human:        PENDING
    public void Dispose() => Message.Dispose();
}

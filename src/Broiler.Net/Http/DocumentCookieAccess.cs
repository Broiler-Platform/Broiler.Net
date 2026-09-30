// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           4
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    High
// Criteria:         6/6
// Resource impact:  3/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Net.Cookies;
using Broiler.Net.Sites;

namespace Broiler.Net.Http;

/// <summary>
/// document.cookie over a cookie store: HTML's cookie-averse and opaque-origin rules, 6265bis 5.8.2 same-site
/// status and the CHIPS partition key. <see cref="BrowserNetworkSession"/> uses it for its profile store; hosts
/// without a network session can give a binding one over a private in-memory store.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=E3E68C
// Broiler-Falsified-If: script in an iframe that is cross-site with its top-level document reads a SameSite Strict or Lax cookie through document.cookie
// Broiler-Human:        PENDING
public sealed class DocumentCookieAccess : IDocumentCookieAccess
{
    private readonly Action<CookieObserverException>? _observerError;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=86160A
    // Broiler-Human:        PENDING
    public DocumentCookieAccess(CookieStore cookies, bool cookiesEnabled = true, Action<CookieObserverException>? observerError = null)
    {
        ArgumentNullException.ThrowIfNull(cookies);
        (Cookies, CookiesEnabled, _observerError) = (cookies, cookiesEnabled, observerError);
    }

    public CookieStore Cookies { get; }
    public ISiteResolver Sites => Cookies.Sites;
    public bool CookiesEnabled { get; }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=FD08EB
    // Broiler-Falsified-If: a document with an opaque origin and an http URL gets its URL's cookies instead of a false return
    // Broiler-Human:        PENDING
    public bool TryGetCookie(DocumentRequestContext document, out string cookie)
    {
        ArgumentNullException.ThrowIfNull(document);
        cookie = "";
        if (document.IsCookieAverse) return true;
        if (document.Origin.IsOpaque) return false;
        if (CookiesEnabled) cookie = Cookies.GetDocumentCookies(GetDocumentContext(document));
        return true;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=8F6D3F
    // Broiler-Falsified-If: a document whose URL is not http or https, such as a data: or file: document, writes a cookie into the store
    // Broiler-Human:        PENDING
    public bool TrySetCookie(DocumentRequestContext document, string value)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(value);
        if (document.IsCookieAverse) return true;
        if (document.Origin.IsOpaque) return false;
        if (!CookiesEnabled) return true;
        try { Cookies.SetDocumentCookie(value, GetDocumentContext(document)); }
        catch (CookieObserverException error) { _observerError?.Invoke(error); }
        return true;
    }

    /// <summary>
    /// 6265bis 5.2.1 "site for cookies" of a document: its top-level origin (the URL's origin for a sandboxed top-level
    /// document), or a new opaque origin when the document or an ancestor is cross-site or has an opaque origin.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.2.1; IP=Low; Security=High; Resources=2; Fingerprint=2E479E
    // Broiler-Falsified-If: a frame same-site with the top-level document but nested inside a cross-site frame gets the top-level origin instead of an opaque one
    // Broiler-Human:        PENDING
    public Origin GetSiteForCookies(DocumentRequestContext document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var topOrigin = TopLevelOrigin(document.TopLevel);
        // Deliberate deviation from 6265bis-22 5.2.1 step 4.1, following Chromium and the pinned WPT
        // (cookies/samesite/sandbox-iframe-*): only the top-level document counts with its URL's origin, so a
        // sandboxed frame is cross-site.
        for (var item = document; item.Parent is not null; item = item.Parent)
            if (!Sites.IsSameSite(item.Origin, topOrigin)) return Origin.CreateOpaque();
        return topOrigin;
    }

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.8.2; IP=Low; Security=High; Resources=2; Fingerprint=EB5344
    // Broiler-Falsified-If: a document whose site for cookies is opaque is given SameSite status, so script in a cross-site frame reads Strict cookies
    // Broiler-Human:        PENDING
    private CookieDocumentContext GetDocumentContext(DocumentRequestContext document)
    {
        // 6265bis 5.8.2: a non-HTTP API is same-site when the document's site for cookies is not opaque.
        var sameSite = GetSiteForCookies(document).IsOpaque ? SameSiteStatus.CrossSite : SameSiteStatus.SameSite;
        var partition = Sites.GetSite(TopLevelOrigin(document.TopLevel)) is { } top
            ? new CookiePartitionKey(top, sameSite != SameSiteStatus.SameSite) : null;
        return new CookieDocumentContext(document.DocumentUrl, sameSite, partition);
    }

    // 6265bis 5.2.1 step 2: a sandboxed top-level document counts with the origin of its URL.
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.2.1; IP=Low; Security=High; Resources=1; Fingerprint=6867D4
    // Broiler-Falsified-If: a sandboxed top-level document with an opaque origin yields that opaque origin instead of the origin of its URL
    // Broiler-Human:        PENDING
    internal static Origin TopLevelOrigin(DocumentRequestContext top) =>
        top.Origin.IsOpaque ? Origin.FromUrl(top.DocumentUrl) : top.Origin;
}

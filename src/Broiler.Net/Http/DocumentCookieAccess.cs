using Broiler.Net.Cookies;
using Broiler.Net.Sites;

namespace Broiler.Net.Http;

/// <summary>
/// document.cookie over a cookie store: HTML's cookie-averse and opaque-origin rules, 6265bis 5.8.2 same-site
/// status and the CHIPS partition key. <see cref="BrowserNetworkSession"/> uses it for its profile store; hosts
/// without a network session can give a binding one over a private in-memory store.
/// </summary>
public sealed class DocumentCookieAccess : IDocumentCookieAccess
{
    private readonly Action<CookieObserverException>? _observerError;

    public DocumentCookieAccess(CookieStore cookies, bool cookiesEnabled = true, Action<CookieObserverException>? observerError = null)
    {
        ArgumentNullException.ThrowIfNull(cookies);
        (Cookies, CookiesEnabled, _observerError) = (cookies, cookiesEnabled, observerError);
    }

    public CookieStore Cookies { get; }
    public ISiteResolver Sites => Cookies.Sites;
    public bool CookiesEnabled { get; }

    public bool TryGetCookie(DocumentRequestContext document, out string cookie)
    {
        ArgumentNullException.ThrowIfNull(document);
        cookie = "";
        if (document.IsCookieAverse) return true;
        if (document.Origin.IsOpaque) return false;
        if (CookiesEnabled) cookie = Cookies.GetDocumentCookies(GetDocumentContext(document));
        return true;
    }

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

    private CookieDocumentContext GetDocumentContext(DocumentRequestContext document)
    {
        // 6265bis 5.8.2: a non-HTTP API is same-site when the document's site for cookies is not opaque.
        var sameSite = GetSiteForCookies(document).IsOpaque ? SameSiteStatus.CrossSite : SameSiteStatus.SameSite;
        var partition = Sites.GetSite(TopLevelOrigin(document.TopLevel)) is { } top
            ? new CookiePartitionKey(top, sameSite != SameSiteStatus.SameSite) : null;
        return new CookieDocumentContext(document.DocumentUrl, sameSite, partition);
    }

    // 6265bis 5.2.1 step 2: a sandboxed top-level document counts with the origin of its URL.
    internal static Origin TopLevelOrigin(DocumentRequestContext top) =>
        top.Origin.IsOpaque ? Origin.FromUrl(top.DocumentUrl) : top.Origin;
}

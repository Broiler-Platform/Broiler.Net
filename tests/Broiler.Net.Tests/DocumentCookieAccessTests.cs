using Broiler.Net.Cookies;
using Broiler.Net.Http;
using Broiler.Net.Sites;

namespace Broiler.Net.Tests;

public sealed class DocumentCookieAccessTests
{
    private readonly TestClock _clock = new();
    private static DocumentRequestContext Top(string url) => DocumentRequestContext.CreateTopLevel(new(url));

    [Fact]
    public void StandaloneAccessFollowsDocumentCookieRules()
    {
        var store = new CookieStore(clock: _clock);
        var access = new DocumentCookieAccess(store);
        var page = Top("https://www.example.test/a/page");
        Assert.True(access.TrySetCookie(page, "a=1; Path=/"));
        Assert.True(access.TrySetCookie(page, "h=1; HttpOnly"));
        store.ReceiveResponseCookie("server=1; HttpOnly; Path=/", new(page.DocumentUrl, SameSiteStatus.SameSite,
            PartitionKey: new(SiteResolver.Default.GetSite(page.DocumentUrl)!)));
        Assert.True(access.TryGetCookie(page, out var cookie));
        Assert.Equal("a=1", cookie);

        Assert.True(access.TryGetCookie(Top("file:///c:/page.html"), out cookie));
        Assert.Equal("", cookie);
        Assert.True(access.TrySetCookie(Top("about:blank"), "x=1"));
        Assert.False(access.TryGetCookie(DocumentRequestContext.CreateTopLevel(page.DocumentUrl, Origin.CreateOpaque()), out _));
        Assert.False(access.TrySetCookie(DocumentRequestContext.CreateTopLevel(page.DocumentUrl, Origin.CreateOpaque()), "x=1"));
        Assert.Equal(["a", "server"], store.Snapshot().Select(c => c.Name).Order());
    }

    [Fact]
    public void CrossSiteFramesGetNoLaxCookiesAndDisabledCookiesAreInvisible()
    {
        var store = new CookieStore(clock: _clock);
        var access = new DocumentCookieAccess(store);
        var embedded = Top("https://news.test/").CreateChild(new("https://embed.test/frame"));
        Assert.True(access.TrySetCookie(Top("https://embed.test/"), "lax=1; SameSite=Lax"));
        Assert.True(access.TrySetCookie(embedded, "none=1; SameSite=None; Secure"));
        Assert.True(access.TryGetCookie(embedded, out var cookie));
        Assert.Equal("none=1", cookie);
        Assert.True(access.TryGetCookie(Top("https://embed.test/"), out cookie));
        Assert.Equal("lax=1; none=1", cookie);

        var disabled = new DocumentCookieAccess(store, cookiesEnabled: false);
        Assert.False(disabled.CookiesEnabled);
        Assert.True(disabled.TryGetCookie(Top("https://embed.test/"), out cookie));
        Assert.Equal("", cookie);
        Assert.True(disabled.TrySetCookie(Top("https://embed.test/"), "ignored=1"));
        Assert.DoesNotContain(store.Snapshot(), c => c.Name == "ignored");
    }

    [Fact]
    public void SessionDocumentCookiesShareTheProfileStore()
    {
        using var session = new BrowserNetworkSession();
        var page = Top("https://www.example.test/");
        Assert.True(session.TrySetCookie(page, "a=1"));
        Assert.Same(session.Cookies, session.DocumentCookies.Cookies);
        Assert.True(session.DocumentCookies.TryGetCookie(page, out var cookie));
        Assert.Equal("a=1", cookie);
    }
}

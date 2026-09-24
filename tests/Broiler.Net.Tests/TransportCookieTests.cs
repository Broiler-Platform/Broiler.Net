using Broiler.Net.Cookies;
using Broiler.Net.Http;
using Broiler.Net.Sites;
using static Broiler.Net.Tests.TransportTest;

namespace Broiler.Net.Tests;

public sealed class TransportCookieTests : IDisposable
{
    private const string All = "strict=1; lax=1; none=1", LaxAndNone = "lax=1; none=1", NoneOnly = "none=1";
    private readonly ScriptedHandler _handler = new();
    private readonly BrowserNetworkSession _session;

    public TransportCookieTests()
    {
        _session = Session(_handler);
        Seed(_session.Cookies, "https://a.test/", "strict=1; SameSite=Strict; Path=/", "lax=1; SameSite=Lax; Path=/", "none=1; SameSite=None; Secure; Path=/");
    }

    public void Dispose() => _session.Dispose();

    private async Task<string?> CookieSent(HttpRequestMessage request, RequestContext context)
    {
        using (await _session.SendAsync(request, context)) { }
        return _handler.Hops[^1].Cookie();
    }

    private Task<string?> CookieSent(string url, RequestContext context) => CookieSent(Get(url), context);

    [Fact]
    public async Task SameSiteSubresourcesGetEveryCookie()
    {
        Assert.Equal(All, await CookieSent("https://a.test/img", RequestContext.Subresource(Top("https://a.test/"), RequestDestination.Image)));
        Assert.Equal(All, await CookieSent("https://a.test/img", RequestContext.Subresource(Top("https://www.a.test/"), RequestDestination.Image)));
    }

    [Fact]
    public async Task CrossSiteSubresourcesGetOnlySameSiteNone()
    {
        Assert.Equal(NoneOnly, await CookieSent("https://a.test/img", RequestContext.Subresource(Top("https://b.test/"), RequestDestination.Image)));
        // Schemeful: http://a.test is another site.
        Assert.Equal(NoneOnly, await CookieSent("https://a.test/img", RequestContext.Subresource(Top("http://a.test/"), RequestDestination.Image)));
    }

    [Fact]
    public async Task CrossSiteTopLevelNavigationsGetLaxOnlyForSafeMethods()
    {
        var b = Top("https://b.test/");
        Assert.Equal(LaxAndNone, await CookieSent("https://a.test/", RequestContext.TopLevelNavigation(b)));
        Assert.Equal(NoneOnly, await CookieSent(Request("POST", "https://a.test/", "x"), RequestContext.TopLevelNavigation(b)));
        Assert.Equal(All, await CookieSent("https://a.test/", RequestContext.TopLevelNavigation(Top("https://a.test/other"))));
    }

    [Fact]
    public async Task ClientlessNavigationsAreSameSite() =>
        Assert.Equal(All, await CookieSent(Request("POST", "https://a.test/", "x"), RequestContext.TopLevelNavigation(null)));

    [Fact]
    public async Task UserReloadsUseTheRecordedSameSiteness()
    {
        var reload = RequestContext.TopLevelNavigation(Top("https://b.test/")) with { IsUserReload = true };
        Assert.Equal(All, await CookieSent("https://a.test/", reload with { ReloadWasSameSite = true }));
        Assert.Equal(LaxAndNone, await CookieSent("https://a.test/", RequestContext.TopLevelNavigation(null) with { IsUserReload = true }));
        // A frame reload also needs a same-site container chain.
        var frame = Top("https://b.test/").CreateChild(new Uri("https://a.test/frame"));
        Assert.Equal(NoneOnly, await CookieSent("https://a.test/frame",
            RequestContext.NestedNavigation(frame.Parent!, null) with { IsUserReload = true, ReloadWasSameSite = true }));
    }

    [Fact]
    public async Task ResponsesReportTheSameSiteStatusToRecordForReloads()
    {
        _handler.Redirect("https://a.test/bounce", 302, "https://b.test/mid");
        _handler.Redirect("https://b.test/mid", 302, "https://a.test/end");
        async Task<SameSiteStatus> StatusOf(string url, RequestContext context)
        {
            using var response = await _session.SendAsync(Get(url), context);
            return response.SameSite;
        }
        Assert.Equal(SameSiteStatus.SameSite, await StatusOf("https://a.test/", RequestContext.TopLevelNavigation(Top("https://www.a.test/"))));
        Assert.Equal(SameSiteStatus.SameSite, await StatusOf("https://a.test/", RequestContext.TopLevelNavigation(null)));
        Assert.Equal(SameSiteStatus.CrossSite, await StatusOf("https://a.test/", RequestContext.TopLevelNavigation(Top("https://b.test/"))));
        Assert.Equal(SameSiteStatus.CrossSite, await StatusOf("https://a.test/bounce", RequestContext.TopLevelNavigation(Top("https://a.test/"))));
    }

    [Fact]
    public async Task ACrossSiteUrlInTheRedirectChainMakesLaterHopsCrossSite()
    {
        _handler.Redirect("https://a.test/start", 302, "https://b.test/mid");
        _handler.Redirect("https://b.test/mid", 302, "https://a.test/end");
        using (await _session.SendAsync(Get("https://a.test/start"), RequestContext.Subresource(Top("https://a.test/"), RequestDestination.Script))) { }
        Assert.Equal([All, null, NoneOnly], _handler.Hops.Select(h => h.Cookie()));
        using (await _session.SendAsync(Get("https://a.test/start"), RequestContext.TopLevelNavigation(Top("https://a.test/")))) { }
        Assert.Equal([All, null, LaxAndNone], _handler.Hops.Skip(3).Select(h => h.Cookie()));
    }

    [Fact]
    public async Task CrossSiteAncestorsMakeFrameRequestsCrossSite()
    {
        var top = Top("https://a.test/");
        var inner = top.CreateChild(new Uri("https://b.test/frame")).CreateChild(new Uri("https://a.test/inner"));
        Assert.Equal(NoneOnly, await CookieSent("https://a.test/img", RequestContext.Subresource(inner, RequestDestination.Image)));
        Assert.Equal(NoneOnly, await CookieSent("https://a.test/frame", RequestContext.NestedNavigation(inner.Parent!, inner.Parent!)));
        var sameSiteFrame = top.CreateChild(new Uri("https://www.a.test/frame"));
        Assert.Equal(All, await CookieSent("https://a.test/img", RequestContext.Subresource(sameSiteFrame, RequestDestination.Image)));
        Assert.Equal(All, await CookieSent("https://a.test/frame", RequestContext.NestedNavigation(sameSiteFrame, sameSiteFrame)));
    }

    [Fact]
    public async Task FrameNavigationsTakeTheOriginAndSameSiteFromTheInitiator()
    {
        var top = Top("https://a.test/");
        var ad = top.CreateChild(new Uri("https://b.test/ad"));
        // The cross-site frame submits a form into itself (WPT cookies/samesite/iframe.https.html: cross-site).
        Assert.Equal(NoneOnly, await CookieSent(Request("POST", "https://a.test/transfer", "x"), RequestContext.NestedNavigation(top, ad)));
        Assert.Equal("https://b.test", _handler.Hops[^1].Header("Origin"));
        // The container's own src attribute or form is same-site.
        Assert.Equal(All, await CookieSent(Request("POST", "https://a.test/transfer", "x"), RequestContext.NestedNavigation(top, top)));
        Assert.Equal("https://a.test", _handler.Hops[^1].Header("Origin"));
        // A same-site initiator cannot make a frame under a cross-site ancestor first-party, nor can browser UI.
        Assert.Equal(NoneOnly, await CookieSent("https://a.test/deep", RequestContext.NestedNavigation(ad, top)));
        Assert.Equal(NoneOnly, await CookieSent("https://a.test/deep", RequestContext.NestedNavigation(ad, null)));
        Assert.Equal(All, await CookieSent("https://a.test/frame", RequestContext.NestedNavigation(top, null)));
        Assert.Null(_handler.Hops[^1].Header("Origin"));
    }

    [Fact]
    public async Task CrossSiteInitiatedFrameNavigationsCannotSetSameSiteCookies()
    {
        // WPT cookies/samesite/setcookie-navigation.https.html: a cross-site to same-site iframe navigation.
        var top = Top("https://a.test/");
        _handler.On("https://a.test/set", 200, ("Set-Cookie", "s2=1; SameSite=Strict; Path=/"), ("Set-Cookie", "l2=1; SameSite=Lax; Path=/"),
            ("Set-Cookie", "d2=1; Path=/"), ("Set-Cookie", "n2=1; SameSite=None; Secure; Path=/"));
        using (await _session.SendAsync(Get("https://a.test/set"), RequestContext.NestedNavigation(top, top.CreateChild(new Uri("https://b.test/ad"))))) { }
        Assert.Equal(["strict", "lax", "none", "n2"], _session.Cookies.Snapshot().Select(c => c.Name));
        using (await _session.SendAsync(Get("https://a.test/set"), RequestContext.NestedNavigation(top, top))) { }
        Assert.Equal(["strict", "lax", "none", "n2", "s2", "l2", "d2"], _session.Cookies.Snapshot().Select(c => c.Name));
    }

    [Fact]
    public async Task SandboxedFramesAreCrossSite()
    {
        // WPT cookies/samesite/sandbox-iframe-subresource.https.html and sandbox-iframe-nested.https.html.
        var sandboxed = Top("https://a.test/").CreateChild(new Uri("https://a.test/sandboxed"), Origin.CreateOpaque());
        var context = RequestContext.Subresource(sandboxed, RequestDestination.Image);
        Assert.Equal(NoneOnly, await CookieSent("https://a.test/img", context));
        Assert.Equal(NoneOnly, await CookieSent(Request("POST", "https://a.test/api", "x"), RequestContext.Fetch(sandboxed, RequestMode.NoCors, CredentialsMode.Include)));
        Assert.Equal(NoneOnly, await CookieSent("https://a.test/frame", RequestContext.NestedNavigation(sandboxed, sandboxed)));
        _handler.On("https://a.test/p", 200, ("Set-Cookie", "p=1; Secure; Partitioned; SameSite=None; Path=/"), ("Set-Cookie", "s2=1; SameSite=Strict"));
        using (var response = await _session.SendAsync(Get("https://a.test/p"), context)) Assert.Equal(ResponseTainting.Opaque, response.Tainting);
        Assert.Equal(new CookiePartitionKey(Site("https://a.test/"), true), PartitionOf("p"));
        Assert.DoesNotContain(_session.Cookies.Snapshot(), c => c.Name == "s2");
    }

    [Fact]
    public void SiteForCookiesAuditsTheInclusiveAncestorChain()
    {
        var top = Top("https://a.test/");
        Assert.Same(top.Origin, _session.GetSiteForCookies(top));
        Assert.Same(top.Origin, _session.GetSiteForCookies(top.CreateChild(new Uri("https://www.a.test/"))));
        Assert.True(_session.GetSiteForCookies(top.CreateChild(new Uri("https://b.test/"))).IsOpaque);
        Assert.True(_session.GetSiteForCookies(top.CreateChild(new Uri("https://b.test/")).CreateChild(new Uri("https://a.test/"))).IsOpaque);
        Assert.True(_session.GetSiteForCookies(top.CreateChild(new Uri("http://a.test/"))).IsOpaque);
        var sandboxedTop = Top("https://a.test/", Origin.CreateOpaque());
        Assert.Equal(Origin.FromUrl(new Uri("https://a.test/")), _session.GetSiteForCookies(sandboxedTop));
        Assert.True(_session.GetSiteForCookies(top.CreateChild(new Uri("https://a.test/s"), Origin.CreateOpaque())).IsOpaque);
        Assert.True(_session.GetSiteForCookies(top.CreateChild(new Uri("https://a.test/s"), Origin.CreateOpaque()).CreateChild(new Uri("https://a.test/c"))).IsOpaque);
        var dataTop = Top("data:text/html,x");
        Assert.True(_session.GetSiteForCookies(dataTop).IsOpaque);
        Assert.True(_session.GetSiteForCookies(dataTop.CreateChild(new Uri("https://a.test/"))).IsOpaque);
    }

    private CookiePartitionKey? PartitionOf(string name) => _session.Cookies.Snapshot().Single(c => c.Name == name).PartitionKey;

    [Fact]
    public async Task PartitionKeysFollowTheTopLevelSite()
    {
        const string Partitioned = "=1; Secure; Partitioned; SameSite=None; Path=/";
        var a = Top("https://a.test/");
        var frame = a.CreateChild(new Uri("https://b.test/frame"));
        _handler.On("https://b.test/nav", 200, ("Set-Cookie", "nav" + Partitioned));
        _handler.On("https://b.test/sub", 200, ("Set-Cookie", "sub" + Partitioned));
        _handler.On("https://www.a.test/same", 200, ("Set-Cookie", "same" + Partitioned));
        _handler.On("https://b.test/frame", 200, ("Set-Cookie", "frame" + Partitioned));
        _handler.On("https://b.test/inframe", 200, ("Set-Cookie", "inframe" + Partitioned));
        using (await _session.SendAsync(Get("https://b.test/nav"), RequestContext.TopLevelNavigation(a))) { }
        using (await _session.SendAsync(Get("https://b.test/sub"), RequestContext.Subresource(a, RequestDestination.Script))) { }
        using (await _session.SendAsync(Get("https://www.a.test/same"), RequestContext.Subresource(a, RequestDestination.Script))) { }
        using (await _session.SendAsync(Get("https://b.test/frame"), RequestContext.NestedNavigation(a, a))) { }
        using (await _session.SendAsync(Get("https://b.test/inframe"), RequestContext.Subresource(frame, RequestDestination.Script))) { }
        Assert.Equal(new CookiePartitionKey(Site("https://b.test/"), false), PartitionOf("nav"));
        Assert.Equal(new CookiePartitionKey(Site("https://a.test/"), true), PartitionOf("sub"));
        Assert.Equal(new CookiePartitionKey(Site("https://a.test/"), false), PartitionOf("same"));
        Assert.Equal(new CookiePartitionKey(Site("https://a.test/"), true), PartitionOf("frame"));
        Assert.Equal(new CookiePartitionKey(Site("https://a.test/"), true), PartitionOf("inframe"));

        Assert.Equal("sub=1; frame=1; inframe=1", await CookieSent("https://b.test/x", RequestContext.Subresource(a, RequestDestination.Image)));
        Assert.Null(await CookieSent("https://b.test/x", RequestContext.Subresource(Top("https://c.test/"), RequestDestination.Image)));
        Assert.Equal("nav=1", await CookieSent("https://b.test/x", RequestContext.TopLevelNavigation(a)));
    }

    [Fact]
    public async Task PartitionKeysIgnoreTheRedirectChain()
    {
        const string Partitioned = "=1; Secure; Partitioned; SameSite=None; Path=/";
        var a = Top("https://a.test/");
        Assert.True(_session.TrySetCookie(a, "fp" + Partitioned));
        _handler.Redirect("https://a.test/start", 302, "https://b.test/mid");
        _handler.Redirect("https://b.test/mid", 302, "https://a.test/end");
        _handler.On("https://a.test/end", 200, ("Set-Cookie", "p" + Partitioned));
        using (await _session.SendAsync(Get("https://a.test/start"), RequestContext.Subresource(a, RequestDestination.Script))) { }
        // The bounce makes the last hop cross-site for SameSite, but its response still belongs to a.test's own partition.
        Assert.Equal("none=1; fp=1", _handler.Hops[^1].Cookie());
        Assert.Equal(new CookiePartitionKey(Site("https://a.test/"), false), PartitionOf("p"));
        Assert.True(_session.TryGetCookie(a, out var cookie));
        Assert.Equal("strict=1; lax=1; none=1; fp=1; p=1", cookie);
    }

    [Fact]
    public async Task ManualRedirectContinuationsKeepTheChain()
    {
        var top = Top("https://a.test/");
        _handler.Redirect("https://b.test/r", 302, "https://a.test/landing");
        var context = RequestContext.NestedNavigation(top, top) with { Redirect = RedirectMode.Manual };
        using var redirect = await _session.SendAsync(Get("https://b.test/r"), context);
        Assert.Equal(302, redirect.StatusCode);
        // A navigation sees the redirect itself (Fetch makes only other requests opaque-redirects).
        Assert.Equal(ResponseTainting.Basic, redirect.Tainting);
        // Following Location as a new request would launder the cross-site hop out of the chain.
        Assert.Equal(All, await CookieSent("https://a.test/landing", context));
        using (var landing = await _session.SendAsync(Get("https://a.test/landing"), context with { RedirectChain = redirect.UrlList }))
        {
            Assert.Equal(["https://b.test/r", "https://a.test/landing"], landing.UrlList.Select(u => u.AbsoluteUri));
            Assert.Equal(SameSiteStatus.CrossSite, landing.SameSite);
        }
        Assert.Equal(NoneOnly, _handler.Hops[^1].Cookie());
    }

    [Fact]
    public async Task OpaqueTopLevelSitesHaveNoPartition()
    {
        _handler.On("https://b.test/p", 200, ("Set-Cookie", "p=1; Secure; Partitioned; SameSite=None"), ("Set-Cookie", "u=1; Secure; SameSite=None"));
        using (await _session.SendAsync(Get("https://b.test/p"), RequestContext.Subresource(Top("data:text/html,x"), RequestDestination.Image))) { }
        Assert.DoesNotContain(_session.Cookies.Snapshot(), c => c.Name == "p");
        Assert.Null(PartitionOf("u"));
    }

    [Fact]
    public void DocumentsSeeNeitherHttpOnlyNorCrossSiteRestrictedCookies()
    {
        Seed(_session.Cookies, "https://a.test/", "http=1; HttpOnly; Path=/");
        Assert.True(_session.TryGetCookie(Top("https://a.test/"), out var cookie));
        Assert.Equal(All, cookie);
        var frame = Top("https://b.test/").CreateChild(new Uri("https://a.test/frame"));
        Assert.True(_session.TryGetCookie(frame, out cookie));
        Assert.Equal(NoneOnly, cookie);
        Assert.True(_session.TrySetCookie(frame, "s2=1; SameSite=Strict; Path=/"));
        Assert.True(_session.TrySetCookie(frame, "n2=1; SameSite=None; Secure; Path=/"));
        Assert.True(_session.TrySetCookie(frame, "h2=1; HttpOnly; Path=/"));
        Assert.Equal(["strict", "lax", "none", "http", "n2"], _session.Cookies.Snapshot().Select(c => c.Name));
    }

    [Fact]
    public void DocumentCookiesRoundTripUtf8AndPartitionByTopLevelSite()
    {
        var top = Top("https://a.test/");
        Assert.True(_session.TrySetCookie(top, "u=caf\u00e9; Path=/"));
        Assert.True(_session.TryGetCookie(top, out var cookie));
        Assert.EndsWith("u=caf\u00e9", cookie);
        var frame = top.CreateChild(new Uri("https://b.test/"));
        Assert.True(_session.TrySetCookie(frame, "p=1; Secure; Partitioned; SameSite=None"));
        Assert.Equal(new CookiePartitionKey(Site("https://a.test/"), true), PartitionOf("p"));
        Assert.True(_session.TryGetCookie(frame, out cookie));
        Assert.Equal("p=1", cookie);
        Assert.True(_session.TryGetCookie(Top("https://b.test/"), out cookie));
        Assert.Equal("", cookie);
    }

    [Theory]
    [InlineData("file:///c:/page.html")]
    [InlineData("about:blank")]
    [InlineData("data:text/html,x")]
    public void CookieAverseDocumentsSucceedWithoutEffect(string url)
    {
        var document = Top(url);
        Assert.True(_session.TryGetCookie(document, out var cookie));
        Assert.Equal("", cookie);
        Assert.True(_session.TrySetCookie(document, "x=1"));
        Assert.Equal(3, _session.Cookies.Snapshot().Count);
    }

    [Fact]
    public void OpaqueOriginDocumentsFail()
    {
        var sandboxed = Top("https://a.test/", Origin.CreateOpaque());
        Assert.False(_session.TryGetCookie(sandboxed, out var cookie));
        Assert.Equal("", cookie);
        Assert.False(_session.TrySetCookie(sandboxed, "x=1"));
        Assert.Equal(3, _session.Cookies.Snapshot().Count);
    }

    [Fact]
    public async Task DisabledCookiesAreNeitherSentStoredNorExposed()
    {
        var store = new CookieStore(clock: new TestClock());
        Seed(store, "https://a.test/", "a=1");
        using var session = Session(_handler, new() { Cookies = store, CookiesEnabled = false });
        _handler.On("https://a.test/", 200, ("Set-Cookie", "b=1"));
        using (await session.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null))) { }
        Assert.Null(_handler.Hops[^1].Cookie());
        Assert.False(session.CookiesEnabled);
        Assert.True(session.TryGetCookie(Top("https://a.test/"), out var cookie));
        Assert.Equal("", cookie);
        Assert.True(session.TrySetCookie(Top("https://a.test/"), "c=1"));
        Assert.Equal(["a"], store.Snapshot().Select(c => c.Name));
        Assert.False(session.TrySetCookie(Top("https://a.test/", Origin.CreateOpaque()), "c=1"));
    }

    [Fact]
    public async Task ObserverFailuresDoNotFailTheRequest()
    {
        var reported = new List<CookieObserverException>();
        var store = new CookieStore(clock: new TestClock());
        using var session = Session(_handler, new() { Cookies = store, CookieObserverError = reported.Add });
        store.Changed += (_, batch) => { if (batch.Changes.Any(c => c.Current?.Name is "x" or "d")) throw new IOException("disk full"); };
        _handler.On("https://a.test/", 200, ("Set-Cookie", "x=1"), ("Set-Cookie", "y=2"));
        using (var response = await session.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null)))
            Assert.Equal(200, response.StatusCode);
        Assert.Equal(["x", "y"], store.Snapshot().Select(c => c.Name));
        Assert.True(session.TrySetCookie(Top("https://a.test/"), "d=1"));
        Assert.Equal(2, reported.Count);
        Assert.Contains(store.Snapshot(), c => c.Name == "d");
        using var unreported = Session(_handler, new() { Cookies = store });
        using (await unreported.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null))) { }
    }
}

using System.Net;
using System.Text;
using Broiler.Net.Cookies;
using Broiler.Net.Http;
using Broiler.Net.Sites;
using static Broiler.Net.Tests.TransportTest;

namespace Broiler.Net.Tests;

public sealed class TransportRequestTests
{
    private readonly ScriptedHandler _handler = new();

    [Theory]
    [InlineData("ftp://a.test/file")]
    [InlineData("file:///c:/x.txt")]
    [InlineData("data:,x")]
    [InlineData("about:blank")]
    public async Task NonHttpUrlsAreInvalidBeforeAnyIo(string url)
    {
        using var session = Session(_handler);
        Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get(url), RequestContext.TopLevelNavigation(null)))).Error);
        Assert.Equal(TransportError.InvalidRequest,
            Assert.Throws<TransportException>(() => session.Send(Get(url), RequestContext.TopLevelNavigation(null))).Error);
        Assert.Empty(_handler.Hops);
    }

    [Fact]
    public async Task RelativeUrlAndBlockedPortsAreInvalid()
    {
        using var session = Session(_handler);
        var relative = new HttpRequestMessage(HttpMethod.Get, new Uri("/x", UriKind.Relative));
        Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(relative, RequestContext.TopLevelNavigation(null)))).Error);
        Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get("https://a.test:6000/"), RequestContext.TopLevelNavigation(null)))).Error);
        Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get("http://a.test:25/"), RequestContext.TopLevelNavigation(null)))).Error);
        Assert.Empty(_handler.Hops);
    }

    [Theory]
    [InlineData("TRACE")]
    [InlineData("connect")]
    [InlineData("Track")]
    public async Task ForbiddenMethodsAreInvalid(string method)
    {
        using var session = Session(_handler);
        var error = await Fails(session.SendAsync(Request(method, "https://a.test/"), RequestContext.TopLevelNavigation(null)));
        Assert.Equal(TransportError.InvalidRequest, error.Error);
        Assert.Empty(_handler.Hops);
    }

    [Fact]
    public async Task NoCorsModeAllowsOnlySafelistedMethods()
    {
        using var session = Session(_handler);
        var context = RequestContext.Subresource(Top("https://a.test/"), RequestDestination.Image);
        foreach (var method in new[] { "PUT", "DELETE", "PATCH", "OPTIONS" })
            Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Request(method, "https://a.test/x"), context))).Error);
        Assert.Empty(_handler.Hops);
        using var ok = await session.SendAsync(Request("post", "https://b.test/x", "body"), context);
        Assert.Equal("POST", Assert.Single(_handler.Hops).Method);
    }

    [Fact]
    public async Task OnlyNavigationsMayOmitTheClient()
    {
        using var session = Session(_handler);
        var subresource = new RequestContext { Destination = RequestDestination.Script };
        var nested = new RequestContext { Destination = RequestDestination.IFrame, Mode = RequestMode.Navigate };
        Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get("https://a.test/"), subresource))).Error);
        Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get("https://a.test/"), nested))).Error);
        Assert.Empty(_handler.Hops);
        using (var response = await session.SendAsync(Get("https://a.test/"), new RequestContext { Destination = RequestDestination.Document, Mode = RequestMode.Navigate }))
            Assert.Equal(200, response.StatusCode);
        using (var frame = await session.SendAsync(Get("https://a.test/"), RequestContext.NestedNavigation(Top("https://a.test/"), null)))
            Assert.Equal(200, frame.StatusCode);
    }

    [Fact]
    public async Task NavigationOnlyInputsAreValidated()
    {
        using var session = Session(_handler);
        var top = Top("https://a.test/");
        var frame = top.CreateChild(new Uri("https://b.test/"));
        Assert.Throws<ArgumentOutOfRangeException>(() => RequestContext.NestedNavigation(top, frame, RequestDestination.Document));
        Assert.Throws<ArgumentOutOfRangeException>(() => RequestContext.NestedNavigation(top, frame, RequestDestination.Script));
        Assert.Throws<ArgumentNullException>(() => RequestContext.NestedNavigation(null!, frame));
        RequestContext[] invalid =
        [
            // A Document destination with a container would claim a top-level partition and the Lax exception.
            RequestContext.TopLevelNavigation(frame) with { Container = top },
            new() { Destination = RequestDestination.Image, Mode = RequestMode.Navigate, Client = top },
            RequestContext.Subresource(frame, RequestDestination.Image) with { Container = top },
            // Reload flags apply to navigations only.
            RequestContext.Fetch(frame, RequestMode.NoCors, CredentialsMode.Include) with { IsUserReload = true, ReloadWasSameSite = true },
            RequestContext.TopLevelNavigation(null) with { ReloadWasSameSite = true },
        ];
        foreach (var context in invalid)
            Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get("https://a.test/"), context))).Error);
        Assert.Empty(_handler.Hops);
    }

    [Theory]
    [InlineData("a\r\nCookie: injected=1")]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    [InlineData("a\0b")]
    [InlineData("Ā")]
    public async Task HeaderValuesThatAreNotHeaderValuesFailBeforeAnyIo(string value)
    {
        using var session = Session(_handler);
        var context = RequestContext.Fetch(Top("https://a.test/"), credentials: CredentialsMode.Omit);
        Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get("https://a.test/x", ("X-Foo", value)), context))).Error);
        var body = Request("POST", "https://a.test/x", "x");
        body.Content!.Headers.TryAddWithoutValidation("Content-Disposition", value);
        Assert.Equal(TransportError.InvalidRequest, Assert.Throws<TransportException>(() => session.Send(body, context)).Error);
        Assert.Empty(_handler.Hops);
    }

    [Fact]
    public async Task HeaderValuesAreNormalized()
    {
        using var session = Session(_handler);
        using (await session.SendAsync(Get("https://a.test/", ("X-Foo", " \ta b\r\n")), RequestContext.TopLevelNavigation(null))) { }
        Assert.Equal("a b", Assert.Single(_handler.Hops).Header("X-Foo"));
    }

    [Fact]
    public async Task FramingAndConnectionHeadersBelongToTheSession()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/form", 303, "/done");
        var request = Request("POST", "https://a.test/form", "x", ("Connection", "keep-alive, Upgrade"), ("Upgrade", "h2c"), ("Keep-Alive", "timeout=5"),
            ("TE", "trailers"), ("Trailer", "X-Sum"), ("Expect", "100-continue"));
        request.Headers.TransferEncodingChunked = true;
        using (await session.SendAsync(request, RequestContext.TopLevelNavigation(null))) { }
        Assert.Equal(["POST https://a.test/form", "GET https://a.test/done"], _handler.Hops.Select(h => h.Target));
        foreach (var name in new[] { "Transfer-Encoding", "Connection", "Upgrade", "Keep-Alive", "TE", "Trailer", "Expect" })
            Assert.All(_handler.Hops, h => Assert.Null(h.Header(name)));
    }

    [Fact]
    public async Task NoCorsRequestsCarryOnlySafelistedAndUserAgentHeaders()
    {
        using var session = Session(_handler);
        Seed(session.Cookies, "https://bank.test/", "sid=1; SameSite=None; Secure");
        var request = Request("POST", "https://bank.test/transfer", "{}", ("X-Requested-With", "XMLHttpRequest"), ("Content-Type", "application/json"),
            ("Authorization", "Basic eA=="), ("Accept", "text/html"), ("Content-Language", "de"), ("Referer", "https://a.test/"), ("User-Agent", "UA/1"),
            ("Range", "bytes=0-"));
        using (await session.SendAsync(request, RequestContext.Fetch(Top("https://a.test/"), RequestMode.NoCors, CredentialsMode.Include))) { }
        var hop = Assert.Single(_handler.Hops);
        Assert.Equal("POST", hop.Method);
        foreach (var name in new[] { "X-Requested-With", "Content-Type", "Authorization" }) Assert.Null(hop.Header(name));
        Assert.Equal(["text/html", "de", "https://a.test/", "UA/1", "bytes=0-", "sid=1"],
            new[] { "Accept", "Content-Language", "Referer", "User-Agent", "Range", "Cookie" }.Select(hop.Header));
    }

    [Theory]
    [InlineData(RequestDestination.Document, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
    [InlineData(RequestDestination.IFrame, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
    [InlineData(RequestDestination.Frame, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
    [InlineData(RequestDestination.Image, "image/png,image/svg+xml,image/*;q=0.8,*/*;q=0.5")]
    [InlineData(RequestDestination.Style, "text/css,*/*;q=0.1")]
    [InlineData(RequestDestination.Script, "*/*")]
    [InlineData(RequestDestination.Font, "*/*")]
    [InlineData(RequestDestination.Empty, "*/*")]
    public async Task AcceptFollowsTheDestination(RequestDestination destination, string accept)
    {
        using var session = Session(_handler, new() { AcceptLanguage = "de-DE,de;q=0.9" });
        var top = Top("https://a.test/");
        var context = destination switch
        {
            RequestDestination.Document => RequestContext.TopLevelNavigation(null),
            RequestDestination.IFrame or RequestDestination.Frame => RequestContext.NestedNavigation(top, top, destination),
            RequestDestination.Empty => RequestContext.Fetch(top),
            _ => RequestContext.Subresource(top, destination),
        };
        using (await session.SendAsync(Get("https://a.test/x"), context)) { }
        using (await session.SendAsync(Get("https://a.test/x", ("Accept", "a/b"), ("Accept-Language", "fr")), context)) { }
        Assert.Equal([accept, "a/b"], _handler.Hops.Select(h => h.Header("Accept")));
        Assert.Equal(["de-DE,de;q=0.9", "fr"], _handler.Hops.Select(h => h.Header("Accept-Language")));
        using var plain = Session(_handler);
        using (await plain.SendAsync(Get("https://a.test/x"), context)) { }
        Assert.Null(_handler.Hops[^1].Header("Accept-Language"));
    }

    [Theory]
    [InlineData("http://127.0.0.1.:5555/p?q#f", "http://127.0.0.1:5555/p?q#f")]
    [InlineData("http://127.1.:5555/", "http://127.0.0.1:5555/")]
    [InlineData("http://１２７.0.0.1:5555/", "http://127.0.0.1:5555/")]
    [InlineData("http://127。0。0。1:5555/", "http://127.0.0.1:5555/")]
    [InlineData("http://192.168.1.1.:8080/x", "http://192.168.1.1:8080/x")]
    public async Task DnsTypedIpv4HostsAreSentToTheirAddress(string url, string expected)
    {
        using var session = Session(_handler);
        Seed(session.Cookies, "http://127.0.0.1:5555/", "sid=secret; Secure; HttpOnly; Path=/");
        using (var response = await session.SendAsync(Get(url), RequestContext.TopLevelNavigation(null)))
            Assert.Equal(expected, response.FinalUrl.AbsoluteUri);
        var hop = Assert.Single(_handler.Hops);
        Assert.Equal(UriHostNameType.IPv4, hop.Url.HostNameType);
        Assert.Equal(expected, hop.Url.AbsoluteUri);
        // The loopback identity the cookie was stored under is also where it is sent.
        Assert.Equal(hop.Url.IsLoopback ? "sid=secret" : null, hop.Cookie());
        _handler.Redirect("https://a.test/", 302, url);
        using (var redirected = await session.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null)))
            Assert.Equal(expected, redirected.FinalUrl.AbsoluteUri);
        Assert.Equal(UriHostNameType.IPv4, _handler.Hops[^1].Url.HostNameType);
    }

    [Fact]
    public void DefaultHandlerSendsOnlyLoopbackAroundTheSystemProxy()
    {
        using var handler = Assert.IsType<LoopbackRouting.RoutingHandler>(BrowserNetworkSession.CreateHandler(new()));
        var direct = Assert.IsType<SocketsHttpHandler>(handler.Direct);
        var other = Assert.IsType<SocketsHttpHandler>(handler.Other);
        Assert.False(direct.UseProxy);
        Assert.NotNull(direct.ConnectCallback);
        // The platform proxy stays unwrapped, so its PAC/WPAD failover (IMultiWebProxy) keeps working.
        Assert.True(other.UseProxy);
        Assert.Null(other.Proxy);
        Assert.Null(other.ConnectCallback);
        foreach (var url in new[] { "http://localhost:1/", "http://app.localhost./", "http://127.0.0.1/", "http://127.9.9.9/", "http://[::1]/" })
            Assert.True(LoopbackRouting.IsDirect(new Uri(url)), url);
        foreach (var url in new[] { "http://example.test/", "http://localhost.example.test/", "http://10.0.0.1/", "http://127.0.0.1./" })
            Assert.False(LoopbackRouting.IsDirect(new Uri(url)), url);
    }

    [Fact]
    public void SitesComeFromTheCookieStore()
    {
        var custom = new SiteResolver(new StringReader("test\nshared.test"));
        var store = new CookieStore(new CookiePolicy(custom));
        using (var session = new BrowserNetworkSession(new() { Cookies = store, Handler = _handler }))
            Assert.Same(custom, session.Sites);
        using (new BrowserNetworkSession(new() { Cookies = store, Sites = custom, Handler = _handler })) { }
        Assert.Throws<ArgumentException>(() => new BrowserNetworkSession(new() { Cookies = new CookieStore(), Sites = custom, Handler = _handler }));
        using var owned = new BrowserNetworkSession(new() { Sites = custom, Handler = _handler });
        Assert.Same(custom, owned.Cookies.Sites);
    }

    [Fact]
    public async Task CrossOriginNoCorsRequestsMustFollowRedirects()
    {
        using var session = Session(_handler);
        var top = Top("https://a.test/");
        foreach (var mode in new[] { RedirectMode.Error, RedirectMode.Manual })
        {
            var context = RequestContext.Fetch(top, RequestMode.NoCors, redirect: mode);
            Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get("https://b.test/"), context))).Error);
            using var sameOrigin = await session.SendAsync(Get("https://a.test/"), context);
            Assert.Equal(ResponseTainting.Basic, sameOrigin.Tainting);
        }
        Assert.Equal(2, _handler.Hops.Count);
    }

    [Fact]
    public async Task MethodsAreNormalizedAndProtocolAndUserAgentApplied()
    {
        using var session = Session(_handler);
        using (await session.SendAsync(Request("post", "https://a.test/1", "x"), RequestContext.TopLevelNavigation(null))) { }
        using (await session.SendAsync(Request("patch", "https://a.test/2", "x"), RequestContext.TopLevelNavigation(null))) { }
        using (await session.SendAsync(Get("https://a.test/3", ("User-Agent", "Custom/1")), RequestContext.TopLevelNavigation(null))) { }
        var hops = _handler.Hops;
        Assert.Equal(["POST", "patch", "GET"], hops.Select(h => h.Method));
        Assert.All(hops, h => Assert.Equal(HttpVersion.Version11, h.Version));
        Assert.Equal(BroilerUserAgent.Value, hops[0].Header("User-Agent"));
        Assert.Equal("Custom/1", hops[2].Header("User-Agent"));

        using var custom = Session(_handler, new() { UserAgent = "Agent/2" });
        using (await custom.SendAsync(Get("https://a.test/4"), RequestContext.TopLevelNavigation(null))) { }
        Assert.Equal("Agent/2", _handler.Hops[^1].Header("User-Agent"));
    }

    [Fact]
    public async Task SessionOwnsCookieHostAndOriginHeaders()
    {
        using var session = Session(_handler);
        HttpRequestMessage Forged() => Get("https://a.test/", ("Cookie", "evil=1"), ("Cookie2", "$Version=1"), ("Host", "evil.test"),
            ("Origin", "https://evil.test"));
        using (await session.SendAsync(Forged(), RequestContext.Subresource(Top("https://a.test/"), RequestDestination.Image))) { }
        Seed(session.Cookies, "https://a.test/", "a=1");
        using (await session.SendAsync(Forged(), RequestContext.Subresource(Top("https://a.test/"), RequestDestination.Image))) { }
        var (empty, stored) = (_handler.Hops[0], _handler.Hops[1]);
        Assert.Null(empty.Cookie());
        Assert.Equal("a=1", stored.Cookie());
        Assert.All(_handler.Hops, h =>
        {
            Assert.Null(h.Header("Cookie2"));
            Assert.Null(h.Header("Host"));
            Assert.Null(h.Header("Origin"));
        });
    }

    [Fact]
    public async Task RequestBodyAndContentHeadersAreSent()
    {
        using var session = Session(_handler);
        var request = Request("POST", "https://a.test/form", "hello", ("Content-Language", "de"), ("X-Custom", "1"));
        using (await session.SendAsync(request, RequestContext.TopLevelNavigation(null))) { }
        var hop = Assert.Single(_handler.Hops);
        Assert.Equal("hello", Encoding.UTF8.GetString(hop.Body!));
        Assert.Equal("text/plain; charset=utf-8", hop.Header("Content-Type"));
        Assert.Equal("de", hop.Header("Content-Language"));
        Assert.Equal("1", hop.Header("X-Custom"));
    }

    [Fact]
    public async Task NavigationForcesIncludedCredentials()
    {
        using var session = Session(_handler);
        Seed(session.Cookies, "https://a.test/", "a=1");
        _handler.On("https://a.test/", 200, ("Set-Cookie", "b=2"));
        var context = RequestContext.TopLevelNavigation(null) with { Credentials = CredentialsMode.Omit };
        using (await session.SendAsync(Get("https://a.test/"), context)) { }
        Assert.Equal("a=1", Assert.Single(_handler.Hops).Cookie());
        Assert.Contains(session.Cookies.Snapshot(), c => c.Name == "b");
    }

    [Fact]
    public async Task OmittedCredentialsNeitherSendNorStore()
    {
        using var session = Session(_handler);
        Seed(session.Cookies, "https://a.test/", "a=1");
        _handler.On("https://a.test/", 200, ("Set-Cookie", "b=2"));
        using var response = await session.SendAsync(Get("https://a.test/"), RequestContext.Fetch(Top("https://a.test/"), credentials: CredentialsMode.Omit));
        Assert.Equal(200, response.StatusCode);
        Assert.Null(Assert.Single(_handler.Hops).Cookie());
        Assert.Equal(["a"], session.Cookies.Snapshot().Select(c => c.Name));
    }

    [Fact]
    public async Task SameOriginCredentialsApplyOnlyWhileTaintingIsBasic()
    {
        using var session = Session(_handler);
        Seed(session.Cookies, "https://a.test/", "a=1");
        Seed(session.Cookies, "https://b.test/", "b=1; SameSite=None; Secure");
        _handler.On("https://a.test/", 200, ("Set-Cookie", "a2=2"));
        _handler.On("https://b.test/", 200, ("Set-Cookie", "b2=2; SameSite=None; Secure"), ("Access-Control-Allow-Origin", "https://a.test"));
        var context = RequestContext.Fetch(Top("https://a.test/"));
        using (await session.SendAsync(Get("https://a.test/"), context)) { }
        using (var cors = await session.SendAsync(Get("https://b.test/"), context)) Assert.Equal(ResponseTainting.Cors, cors.Tainting);
        Assert.Equal(["a=1", null], _handler.Hops.Select(h => h.Cookie()));
        Assert.Equal(["a", "b", "a2"], session.Cookies.Snapshot().Select(c => c.Name));
    }

    [Fact]
    public async Task IncludedCredentialsCrossOriginRequireCorsCredentials()
    {
        using var session = Session(_handler);
        Seed(session.Cookies, "https://b.test/", "b=1; SameSite=None; Secure");
        _handler.On("https://b.test/", 200, ("Set-Cookie", "b2=2; SameSite=None; Secure"), ("Access-Control-Allow-Origin", "https://a.test"),
            ("Access-Control-Allow-Credentials", "true"));
        using (await session.SendAsync(Get("https://b.test/"), RequestContext.Fetch(Top("https://a.test/"), credentials: CredentialsMode.Include))) { }
        Assert.Equal("b=1", Assert.Single(_handler.Hops).Cookie());
        Assert.Contains(session.Cookies.Snapshot(), c => c.Name == "b2");
    }

    [Fact]
    public async Task NoCorsCrossOriginIsOpaqueAndHonoursCredentialsMode()
    {
        using var session = Session(_handler);
        Seed(session.Cookies, "https://b.test/", "b=1; SameSite=None; Secure");
        _handler.On("https://b.test/img", 200, ("Set-Cookie", "seen=1; SameSite=None; Secure"), ("X-Secret", "1"));
        _handler.On("https://b.test/anon", 200, ("Set-Cookie", "anon=1; SameSite=None; Secure"));
        var top = Top("https://a.test/");
        using (var image = await session.SendAsync(Get("https://b.test/img"), RequestContext.Subresource(top, RequestDestination.Image)))
        {
            Assert.Equal(ResponseTainting.Opaque, image.Tainting);
            Assert.Empty(image.GetScriptVisibleHeaders());
            Assert.Equal(0, image.ScriptVisibleStatusCode);
            Assert.Contains(image.Headers, h => h.Key == "X-Secret");
        }
        var sameOriginCredentials = new RequestContext
            { Destination = RequestDestination.Image, Client = top, Mode = RequestMode.NoCors, Credentials = CredentialsMode.SameOrigin };
        using (var opaque = await session.SendAsync(Get("https://b.test/anon"), sameOriginCredentials)) Assert.Equal(ResponseTainting.Opaque, opaque.Tainting);
        Assert.Equal(["b=1", null], _handler.Hops.Select(h => h.Cookie()));
        Assert.Equal(["b", "seen"], session.Cookies.Snapshot().Select(c => c.Name));
    }

    [Fact]
    public async Task OriginHeaderFollowsFetchRules()
    {
        using var session = Session(_handler);
        var a = Top("https://a.test/");
        _handler.On("https://b.test/", 200, ("Access-Control-Allow-Origin", "https://a.test"));
        _handler.On("http://b.test/", 200, ("Access-Control-Allow-Origin", "https://a.test"));
        async Task<string?> OriginOf(HttpRequestMessage request, RequestContext context)
        {
            using (await session.SendAsync(request, context)) { }
            return _handler.Hops[^1].Header("Origin");
        }
        Assert.Null(await OriginOf(Get("https://a.test/"), RequestContext.Fetch(a)));
        Assert.Equal("https://a.test", await OriginOf(Request("POST", "https://a.test/", "x"), RequestContext.Fetch(a)));
        Assert.Equal("https://a.test", await OriginOf(Get("https://b.test/"), RequestContext.Fetch(a)));
        Assert.Equal("https://a.test", await OriginOf(Request("POST", "https://b.test/", "x"), RequestContext.Subresource(a, RequestDestination.Image)));
        Assert.Null(await OriginOf(Get("https://b.test/"), RequestContext.Subresource(a, RequestDestination.Image)));
        Assert.Equal("https://a.test", await OriginOf(Request("POST", "https://b.test/", "x"), RequestContext.TopLevelNavigation(a)));
        Assert.Null(await OriginOf(Get("https://b.test/"), RequestContext.TopLevelNavigation(a)));
        Assert.Null(await OriginOf(Request("POST", "https://b.test/", "x"), RequestContext.TopLevelNavigation(null)));
        var sandboxed = Top("https://a.test/", Origin.CreateOpaque());
        Assert.Equal("null", await OriginOf(Request("POST", "https://a.test/", "x"), RequestContext.Subresource(sandboxed, RequestDestination.Image)));
        // Default referrer policy: a non-CORS request from https to http sends "null"; a CORS request keeps its origin.
        Assert.Equal("null", await OriginOf(Request("POST", "http://b.test/", "x"), RequestContext.TopLevelNavigation(a)));
        Assert.Equal("https://a.test", await OriginOf(Get("http://b.test/"), RequestContext.Fetch(a)));
    }

    [Fact]
    public async Task SyncSendUsesTheHandlersSynchronousPathForEveryHop()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/1", 302, "https://b.test/2");
        _handler.On("https://b.test/2", r => ScriptedHandler.Reply(r.Method == HttpMethod.Options ? 204 : 200,
            ("Access-Control-Allow-Origin", "https://a.test"), ("Access-Control-Allow-Headers", "x-custom")));
        using var response = session.Send(Get("https://a.test/1", ("X-Custom", "1")), RequestContext.Fetch(Top("https://a.test/")));
        Assert.Equal(["GET https://a.test/1", "OPTIONS https://b.test/2", "GET https://b.test/2"], _handler.Hops.Select(h => h.Target));
        Assert.All(_handler.Hops, h => Assert.True(h.Sync));
        Assert.Equal(200, response.StatusCode);

        using (await session.SendAsync(Get("https://a.test/3"), RequestContext.TopLevelNavigation(null))) { }
        Assert.False(_handler.Hops[^1].Sync);
    }

    [Fact]
    public void SendAsyncNeverResumesOnTheCallersSynchronizationContext()
    {
        var context = new CountingContext();
        var previous = SynchronizationContext.Current;
        using var session = new BrowserNetworkSession(new() { Handler = new YieldingHandler(), Cookies = new CookieStore(clock: new TestClock()) });
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            // Blocking on purpose: a captured context would receive the continuation as a Post.
#pragma warning disable xUnit1031
            using var response = session.SendAsync(Request("POST", "https://a.test/start", "body"), RequestContext.TopLevelNavigation(null))
                .GetAwaiter().GetResult();
#pragma warning restore xUnit1031
            Assert.Equal("https://a.test/end", response.FinalUrl.AbsoluteUri);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        Assert.Equal(0, context.Posts);
    }

    [Fact]
    public async Task TimeoutSurfacesLikeHttpClient()
    {
        _handler.Intercept = (_, token, _) =>
        {
            token.WaitHandle.WaitOne();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException();
        };
        using var session = Session(_handler, new() { Timeout = TimeSpan.FromMilliseconds(50) });
        var asyncError = await Assert.ThrowsAsync<TaskCanceledException>(() => session.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null)));
        Assert.IsType<TimeoutException>(asyncError.InnerException);
        var syncError = Assert.Throws<TaskCanceledException>(() => session.Send(Get("https://a.test/"), RequestContext.TopLevelNavigation(null)));
        Assert.IsType<TimeoutException>(syncError.InnerException);
    }

    [Fact]
    public async Task CallerCancellationIsNotReportedAsTimeout()
    {
        using var session = Session(_handler);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            session.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null), canceled.Token));
        Assert.IsNotType<TimeoutException>(error.InnerException);
        Assert.ThrowsAny<OperationCanceledException>(() => session.Send(Get("https://a.test/"), RequestContext.TopLevelNavigation(null), canceled.Token));
        Assert.Empty(_handler.Hops);
    }

    [Fact]
    public async Task ConcurrentRequestsShareOneSession()
    {
        using var session = Session(_handler);
        _handler.Intercept = (request, _, _) => ScriptedHandler.Reply(200, ("Set-Cookie", $"c{request.RequestUri!.Segments[^1]}=1"));
        const int hosts = 16, perHost = 25;
        var work = Enumerable.Range(0, hosts).Select(h => Task.Run(async () =>
        {
            for (var i = 0; i < perHost; i++)
            {
                var request = Get($"https://h{h}.test/{i}");
                var context = RequestContext.Subresource(Top($"https://h{h}.test/"), RequestDestination.Script);
                using var response = i % 2 == 0 ? await session.SendAsync(request, context) : session.Send(request, context);
                Assert.Equal(200, response.StatusCode);
            }
        }));
        await Task.WhenAll(work);
        Assert.Equal(hosts * perHost, _handler.Hops.Count);
        Assert.Equal(hosts * perHost, session.Cookies.Snapshot().Count);
        Assert.All(_handler.Hops.Where(h => h.Url.AbsolutePath == $"/{perHost - 1}"),
            h => Assert.Equal(perHost - 1, h.Cookie()!.Split("; ").Length));
    }

    [Fact]
    public async Task DisposedSessionRejectsRequests()
    {
        var session = Session(_handler);
        session.Dispose();
        session.Dispose();
        Assert.Throws<ObjectDisposedException>(() => { _ = session.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null)); });
        Assert.Throws<ObjectDisposedException>(() => session.Send(Get("https://a.test/"), RequestContext.TopLevelNavigation(null)));
        // A caller-supplied handler is not disposed with the session.
        using var other = Session(_handler);
        using (await other.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null))) { }
    }

    [Fact]
    public void OptionsAreValidatedAndDefaultsWired()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrowserNetworkSession(new() { Timeout = TimeSpan.Zero }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrowserNetworkSession(new() { MaximumRedirects = -1 }));
        Assert.Throws<ArgumentNullException>(() => new BrowserNetworkSession(new() { UserAgent = null! }));
        Assert.Throws<ArgumentException>(() => new BrowserNetworkSession(new() { UserAgent = "UA\r\nCookie: x=1" }));
        Assert.Throws<ArgumentException>(() => new BrowserNetworkSession(new() { AcceptLanguage = "en\n" }));
        using (new BrowserNetworkSession(new() { Timeout = Timeout.InfiniteTimeSpan, Handler = _handler })) { }
        using var session = new BrowserNetworkSession();
        Assert.Same(SiteResolver.Default, session.Sites);
        Assert.NotNull(session.Cookies);
        Assert.True(session.CookiesEnabled);
        Assert.Throws<ArgumentNullException>(() => session.Send(null!, RequestContext.TopLevelNavigation(null)));
        Assert.Throws<ArgumentNullException>(() => { _ = session.SendAsync(Get("https://a.test/"), null!); });
    }

    private sealed class CountingContext : SynchronizationContext
    {
        private int _posts;
        public int Posts => Volatile.Read(ref _posts);
        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _posts);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }
    }

    /// <summary>Completes every hop asynchronously; /start redirects to /end with 307.</summary>
    private sealed class YieldingHandler : HttpMessageHandler
    {
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => throw new NotSupportedException();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
            return request.RequestUri!.AbsolutePath == "/start"
                ? ScriptedHandler.Reply(307, ("Location", "/end"))
                : ScriptedHandler.Reply(200);
        }
    }
}

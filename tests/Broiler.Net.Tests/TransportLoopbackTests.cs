using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Broiler.Net.Cookies;
using Broiler.Net.Http;

namespace Broiler.Net.Tests;

/// <summary>End-to-end checks through the default SocketsHttpHandler against a real loopback socket.</summary>
public sealed class TransportLoopbackTests
{
    private static BrowserNetworkSession Session() => new(new() { Cookies = new CookieStore(clock: new TestClock()) });

    [Fact]
    public async Task SetCookieFieldsKeepCommasAndLatin1Octets()
    {
        using var server = new LoopbackServer(request => request.Target == "/set"
            ? LoopbackServer.Response(200, [("Set-Cookie", "a=1, b=2; Path=/"), ("Set-Cookie", "lat=café; Path=/"),
                ("Set-Cookie", "exp=3; Expires=Wed, 21 Oct 2037 07:28:00 GMT; Path=/")])
            : LoopbackServer.Response(200, []));
        using var session = Session();
        using (await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, server.Url("/set")), RequestContext.TopLevelNavigation(null))) { }
        var cookies = session.Cookies.Snapshot();
        Assert.Equal(["a", "lat", "exp"], cookies.Select(c => c.Name));
        Assert.Equal(["1, b=2", "café", "3"], cookies.Select(c => c.Value));
        // The comma inside Expires did not split the field (the date is then capped at 400 days).
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(400), cookies[2].Expires);

        using (await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, server.Url("/echo")), RequestContext.TopLevelNavigation(null))) { }
        var echo = server.Requests.Last();
        Assert.Equal(1, echo.HeaderCount("Cookie"));
        Assert.Equal("a=1, b=2; lat=café; exp=3", echo.Header("Cookie"));
    }

    [Fact]
    public async Task RedirectChainStoresCookiesOnEveryHop()
    {
        using var server = new LoopbackServer(request => request.Target switch
        {
            "/r1" => LoopbackServer.Response(302, [("Location", "/r2"), ("Set-Cookie", "h1=1; Path=/")]),
            "/r2" => LoopbackServer.Response(307, [("Location", "/r3"), ("Set-Cookie", "h2=2; Path=/")]),
            _ => LoopbackServer.Response(200, [("Set-Cookie", "h3=3; Path=/")], Encoding.UTF8.GetBytes(request.Method + " " + Encoding.UTF8.GetString(request.Body))),
        });
        using var session = Session();
        var request = new HttpRequestMessage(HttpMethod.Post, server.Url("/r1")) { Content = new StringContent("form") };
        using var response = await session.SendAsync(request, RequestContext.TopLevelNavigation(null));
        Assert.Equal(3, response.UrlList.Count);
        Assert.Equal("GET ", await response.Message.Content.ReadAsStringAsync());
        Assert.Equal([null, "h1=1", "h1=1; h2=2"], server.Requests.Select(r => r.Header("Cookie")));
        Assert.Equal(["POST", "GET", "GET"], server.Requests.Select(r => r.Method));
        Assert.Equal(["h1", "h2", "h3"], session.Cookies.Snapshot().Select(c => c.Name));
    }

    [Fact]
    public void SynchronousSendWorksThroughTheSocketsHandler()
    {
        using var server = new LoopbackServer(request => request.Target == "/start"
            ? LoopbackServer.Response(308, [("Location", "/end")])
            : LoopbackServer.Response(200, [("Content-Type", "text/plain")], request.Body));
        using var session = Session();
        var request = new HttpRequestMessage(HttpMethod.Put, server.Url("/start")) { Content = new ByteArrayContent([1, 2, 3]) };
        using var response = session.Send(request, RequestContext.TopLevelNavigation(null));
        using var body = new MemoryStream();
        response.Message.Content.ReadAsStream().CopyTo(body);
        Assert.Equal([1, 2, 3], body.ToArray());
        Assert.Equal(server.Url("/end"), response.FinalUrl.AbsoluteUri);
        Assert.All(server.Requests, r => Assert.Equal(BroilerUserAgent.Value, r.Header("User-Agent")));
    }

    [Fact]
    public async Task CompressedResponsesAreDecoded()
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true)) gzip.Write(Encoding.UTF8.GetBytes("hello gzip"));
        using var server = new LoopbackServer(_ => LoopbackServer.Response(200, [("Content-Encoding", "gzip")], compressed.ToArray()));
        using var session = Session();
        using var response = await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, server.Url("/")), RequestContext.TopLevelNavigation(null));
        Assert.Equal("hello gzip", await response.Message.Content.ReadAsStringAsync());
        Assert.Contains("gzip", Assert.Single(server.Requests).Header("Accept-Encoding"));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("app.localhost")]
    [InlineData("App.LocalHost.")]
    public async Task LocalhostNamesAlwaysConnectToLoopback(string host)
    {
        using var server = new LoopbackServer(_ => LoopbackServer.Response(200, [("Set-Cookie", "s=1; Secure; Path=/")]));
        using var session = Session();
        var url = $"http://{host}:{server.Port}/";
        using (await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), RequestContext.TopLevelNavigation(null))) { }
        Assert.StartsWith(host.TrimEnd('.').ToLowerInvariant(), Assert.Single(server.Requests).Header("Host")!.ToLowerInvariant());
        // Localhost is a secure context for cookies, and the request really stayed on this machine.
        Assert.True(Assert.Single(session.Cookies.Snapshot()).Secure);
    }

    [Ipv6Fact]
    public async Task LocalhostPrefersIpv6Loopback()
    {
        // Both loopback addresses listen on one port; ::1 must answer before 127.0.0.1 is even tried.
        var (ipv6, ipv4) = ListenOnBothLoopbacks(address => LoopbackServer.Response(200, [], Encoding.ASCII.GetBytes(address)));
        using (ipv6)
        using (ipv4)
        {
            using var session = Session();
            using var response = await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{ipv6.Port}/"), RequestContext.TopLevelNavigation(null));
            Assert.Equal("::1", await response.Message.Content.ReadAsStringAsync());
            Assert.Single(ipv6.Requests);
            Assert.Empty(ipv4.Requests);
        }
    }

    private static (LoopbackServer Ipv6, LoopbackServer Ipv4) ListenOnBothLoopbacks(Func<string, byte[]> respond)
    {
        for (var attempt = 0; ; attempt++)
        {
            var ipv6 = new LoopbackServer(_ => respond("::1"), IPAddress.IPv6Loopback);
            try { return (ipv6, new LoopbackServer(_ => respond("127.0.0.1"), IPAddress.Loopback, ipv6.Port)); }
            catch (SocketException) when (attempt < 20) { ipv6.Dispose(); }
        }
    }

    [Theory]
    [InlineData("127.0.0.1.")]
    [InlineData("127.1.")]
    [InlineData("１２７.0.0.1")]
    [InlineData("127。0。0。1")]
    public async Task DnsTypedLoopbackHostsConnectToTheAddress(string host)
    {
        using var server = new LoopbackServer(_ => LoopbackServer.Response(200, []));
        using var session = Session();
        TransportTest.Seed(session.Cookies, server.Url("/"), "sid=secret; Secure; HttpOnly; SameSite=None; Path=/");
        // Without the canonical address the name would go to DNS or the system proxy, with the Secure cookie.
        using var response = await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"http://{host}:{server.Port}/"),
            RequestContext.TopLevelNavigation(DocumentRequestContext.CreateTopLevel(new Uri("https://evil.example/"))));
        Assert.Equal(server.Url("/"), response.FinalUrl.AbsoluteUri);
        var request = Assert.Single(server.Requests);
        Assert.Equal($"127.0.0.1:{server.Port}", request.Header("Host"));
        Assert.Equal("sid=secret", request.Header("Cookie"));
    }

    [Fact]
    public async Task CallerFramingHeadersDoNotBreakRedirects()
    {
        using var server = new LoopbackServer(request => request.Target == "/form"
            ? LoopbackServer.Response(303, [("Location", "/done")])
            : LoopbackServer.Response(200, [], Encoding.UTF8.GetBytes(request.Method)));
        using var session = Session();
        var request = new HttpRequestMessage(HttpMethod.Post, server.Url("/form")) { Content = new StringContent("a=1") };
        request.Headers.TransferEncodingChunked = true;
        request.Headers.ConnectionClose = true;
        using var response = session.Send(request, RequestContext.TopLevelNavigation(null));
        Assert.Equal("GET", await response.Message.Content.ReadAsStringAsync());
        var (post, get) = (server.Requests.First(), server.Requests.Last());
        Assert.Equal("a=1", Encoding.UTF8.GetString(post.Body));
        Assert.Equal("3", post.Header("Content-Length"));
        Assert.All(server.Requests, r => Assert.Null(r.Header("Transfer-Encoding")));
        Assert.Equal("GET", get.Method);
    }

    [Fact]
    public async Task LineBreaksInHeaderValuesNeverReachTheWire()
    {
        using var server = new LoopbackServer(_ => LoopbackServer.Response(200, []));
        using var session = Session();
        var request = new HttpRequestMessage(HttpMethod.Get, server.Url("/x"));
        Assert.True(request.Headers.TryAddWithoutValidation("X-Foo", "a\r\nCookie: injected=1"));
        var error = await Assert.ThrowsAsync<TransportException>(() =>
            session.SendAsync(request, RequestContext.Fetch(DocumentRequestContext.CreateTopLevel(new Uri(server.Url("/"))), credentials: CredentialsMode.Omit)));
        Assert.Equal(TransportError.InvalidRequest, error.Error);
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task Utf8LocationsResolveLikeBrowsers()
    {
        using var server = new LoopbackServer(request => request.Target == "/start"
            ? LoopbackServer.Response(302, [("Location", Encoding.Latin1.GetString(Encoding.UTF8.GetBytes("/café")))])
            : LoopbackServer.Response(200, []));
        using var session = Session();
        using var response = await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, server.Url("/start")), RequestContext.TopLevelNavigation(null));
        Assert.Equal(server.Url("/caf%C3%A9"), response.FinalUrl.AbsoluteUri);
        Assert.Equal("/caf%C3%A9", server.Requests.Last().Target);
    }
}

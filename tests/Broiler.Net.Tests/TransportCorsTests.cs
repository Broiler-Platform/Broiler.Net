using Broiler.Net.Http;
using static Broiler.Net.Tests.TransportTest;

namespace Broiler.Net.Tests;

public sealed class TransportCorsTests
{
    private const string A = "https://a.test";
    private readonly ScriptedHandler _handler = new();
    private readonly DocumentRequestContext _top = Top(A + "/");

    private RequestContext Anonymous => RequestContext.Fetch(_top);
    private RequestContext Credentialed => RequestContext.Fetch(_top, credentials: CredentialsMode.Include);
    private static (string, string) AllowOrigin(string value = A) => ("Access-Control-Allow-Origin", value);
    private static (string, string) AllowCredentials(string value = "true") => ("Access-Control-Allow-Credentials", value);

    /// <summary>Answers OPTIONS with <paramref name="preflight"/> and other methods with <paramref name="actual"/>.</summary>
    private void Route(string url, (int Status, (string, string)[] Headers) preflight, params (string, string)[] actual) =>
        _handler.On(url, r => r.Method == HttpMethod.Options ? ScriptedHandler.Reply(preflight.Status, preflight.Headers) : ScriptedHandler.Reply(200, actual));

    private async Task<TransportError> CorsError(HttpRequestMessage request, RequestContext context)
    {
        using var session = Session(_handler);
        return (await Fails(session.SendAsync(request, context))).Error;
    }

    [Fact]
    public async Task MatchingAllowOriginPasses()
    {
        using var session = Session(_handler);
        _handler.On("https://b.test/", 200, AllowOrigin());
        using var response = await session.SendAsync(Get("https://b.test/"), Anonymous);
        Assert.Equal(ResponseTainting.Cors, response.Tainting);
        Assert.Equal(A, Assert.Single(_handler.Hops).Header("Origin"));
    }

    [Fact]
    public async Task WildcardPassesOnlyWithoutCredentials()
    {
        _handler.On("https://b.test/", 200, AllowOrigin("*"), AllowCredentials());
        using var session = Session(_handler);
        using (var response = await session.SendAsync(Get("https://b.test/"), Anonymous)) Assert.Equal(200, response.StatusCode);
        Assert.Equal(TransportError.Cors, await CorsError(Get("https://b.test/"), Credentialed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("TRUE")]
    [InlineData("true, true")]
    public async Task CredentialedResponsesNeedAllowCredentialsTrue(string? value)
    {
        _handler.On("https://b.test/", 200, value is null ? [AllowOrigin()] : [AllowOrigin(), AllowCredentials(value)]);
        Assert.Equal(TransportError.Cors, await CorsError(Get("https://b.test/"), Credentialed));
        _handler.On("https://b.test/", 200, AllowOrigin(), AllowCredentials());
        using var session = Session(_handler);
        using var response = await session.SendAsync(Get("https://b.test/"), Credentialed);
        Assert.Equal(200, response.StatusCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(A + "/", null)]
    [InlineData("https://A.test", null)]
    [InlineData("null", null)]
    [InlineData(A, A)]
    [InlineData("*", "*")]
    public async Task AllowOriginMustBeOneExactValue(string? first, string? second)
    {
        _handler.On("https://b.test/", 200, new[] { first, second }.OfType<string>().Select(v => AllowOrigin(v)).ToArray());
        Assert.Equal(TransportError.Cors, await CorsError(Get("https://b.test/"), Anonymous));
        Assert.True(((TrackingContent)Assert.Single(_handler.Responses).Content).Disposed);
    }

    [Fact]
    public async Task CookiesAreStoredBeforeTheCorsCheckFails()
    {
        using var session = Session(_handler);
        _handler.On("https://b.test/", 200, ("Set-Cookie", "x=1; SameSite=None; Secure"));
        Assert.Equal(TransportError.Cors, (await Fails(session.SendAsync(Get("https://b.test/"), Credentialed))).Error);
        Assert.Equal("b.test", Assert.Single(session.Cookies.Snapshot()).Domain);
    }

    [Fact]
    public async Task RedirectResponsesMustPassTheCorsCheck()
    {
        _handler.Redirect("https://b.test/1", 302, "/2");
        Assert.Equal(TransportError.Cors, await CorsError(Get("https://b.test/1"), Anonymous));
        Assert.Single(_handler.Hops);
        _handler.Redirect("https://b.test/1", 302, "/2", AllowOrigin());
        Assert.Equal(TransportError.Cors, await CorsError(Get("https://b.test/1"), Anonymous));
        _handler.On("https://b.test/2", 200, AllowOrigin());
        using var session = Session(_handler);
        using var response = await session.SendAsync(Get("https://b.test/1"), Anonymous);
        Assert.Equal("https://b.test/2", response.FinalUrl.AbsoluteUri);
    }

    [Fact]
    public async Task SimpleRequestsSkipThePreflight()
    {
        using var session = Session(_handler);
        _handler.On("https://b.test/", 200, AllowOrigin());
        using (await session.SendAsync(Get("https://b.test/", ("Accept", "text/html"), ("Accept-Language", "de"), ("Range", "bytes=0-")), Anonymous)) { }
        using (await session.SendAsync(Request("POST", "https://b.test/", "text", ("Content-Language", "en")), Anonymous)) { }
        using (await session.SendAsync(Request("HEAD", "https://b.test/"), Anonymous)) { }
        Assert.Equal(["GET", "POST", "HEAD"], _handler.Hops.Select(h => h.Method));
    }

    [Fact]
    public async Task SameOriginRequestsNeverPreflight()
    {
        using var session = Session(_handler);
        using var response = await session.SendAsync(Request("PUT", A + "/", "x", ("X-Custom", "1")), Anonymous);
        Assert.Equal(ResponseTainting.Basic, response.Tainting);
        Assert.Equal("PUT", Assert.Single(_handler.Hops).Method);
    }

    [Fact]
    public async Task PreflightAnnouncesTheUnsafeParts()
    {
        using var session = Session(_handler);
        Seed(session.Cookies, "https://b.test/", "b=1; SameSite=None; Secure");
        Route("https://b.test/api", (204, [AllowOrigin(), AllowCredentials(), ("Access-Control-Allow-Methods", "GET, PUT"),
            ("Access-Control-Allow-Headers", "X-Custom, x-another"), ("Access-Control-Allow-Headers", "content-type"), ("Set-Cookie", "pre=1")]),
            AllowOrigin(), AllowCredentials());
        var request = Request("PUT", "https://b.test/api", "{}", ("X-Custom", "1"), ("x-another", "2"), ("Content-Type", "application/json"));
        using var response = await session.SendAsync(request, Credentialed);
        var (preflight, actual) = (_handler.Hops[0], _handler.Hops[1]);
        Assert.Equal("OPTIONS", preflight.Method);
        Assert.Equal("PUT", preflight.Header("Access-Control-Request-Method"));
        Assert.Equal("content-type,x-another,x-custom", preflight.Header("Access-Control-Request-Headers"));
        Assert.Equal(A, preflight.Header("Origin"));
        Assert.Equal("*/*", preflight.Header("Accept"));
        Assert.Equal(BroilerUserAgent.Value, preflight.Header("User-Agent"));
        Assert.Null(preflight.Cookie());
        Assert.Null(preflight.Body);
        Assert.Null(preflight.Header("X-Custom"));
        Assert.Equal("PUT", actual.Method);
        Assert.Equal("b=1", actual.Cookie());
        Assert.Equal("{}", System.Text.Encoding.UTF8.GetString(actual.Body!));
        Assert.DoesNotContain(session.Cookies.Snapshot(), c => c.Name == "pre");
    }

    [Fact]
    public async Task RepeatedSafelistedHeadersAreJudgedOnTheirCombinedValue()
    {
        using var session = Session(_handler);
        Route("https://b.test/", (204, [AllowOrigin(), ("Access-Control-Allow-Headers", "accept")]), AllowOrigin());
        var value = new string('a', 100);
        using (await session.SendAsync(Get("https://b.test/", ("Accept", value + value[..28]), ("Accept-Language", "de")), Anonymous)) { }
        Assert.Equal(["GET"], _handler.Hops.Select(h => h.Method));
        using (await session.SendAsync(Get("https://b.test/", ("Accept", value), ("Accept", value), ("Accept-Language", "de")), Anonymous)) { }
        Assert.Equal("accept", _handler.Hops[1].Header("Access-Control-Request-Headers"));
        Assert.Equal("GET", _handler.Hops[2].Method);
    }

    public static TheoryData<int, (string, string)[]> FailingPreflights => new()
    {
        { 403, [AllowOrigin(), ("Access-Control-Allow-Methods", "PUT"), ("Access-Control-Allow-Headers", "x-custom")] },
        { 302, [AllowOrigin(), ("Location", "/elsewhere"), ("Access-Control-Allow-Methods", "PUT"), ("Access-Control-Allow-Headers", "x-custom")] },
        { 204, [("Access-Control-Allow-Methods", "PUT"), ("Access-Control-Allow-Headers", "x-custom")] },
        { 204, [AllowOrigin(), ("Access-Control-Allow-Methods", "GET, POST"), ("Access-Control-Allow-Headers", "x-custom")] },
        { 204, [AllowOrigin(), ("Access-Control-Allow-Methods", "put"), ("Access-Control-Allow-Headers", "x-custom")] },
        { 204, [AllowOrigin(), ("Access-Control-Allow-Methods", "PUT")] },
        { 204, [AllowOrigin(), ("Access-Control-Allow-Methods", "PU T"), ("Access-Control-Allow-Headers", "x-custom")] },
        { 204, [AllowOrigin(), ("Access-Control-Allow-Methods", "PUT"), ("Access-Control-Allow-Headers", "x-custom;")] },
    };

    [Theory]
    [MemberData(nameof(FailingPreflights))]
    public async Task FailedPreflightsStopTheRequest(int status, (string, string)[] headers)
    {
        Route("https://b.test/", (status, headers), AllowOrigin());
        Assert.Equal(TransportError.Cors, await CorsError(Request("PUT", "https://b.test/", "x", ("X-Custom", "1")), Anonymous));
        Assert.Equal("OPTIONS", Assert.Single(_handler.Hops).Method);
    }

    [Fact]
    public async Task WildcardAllowListsApplyOnlyWithoutCredentials()
    {
        (string, string)[] wildcard = [AllowOrigin(), AllowCredentials(), ("Access-Control-Allow-Methods", "*"), ("Access-Control-Allow-Headers", "*")];
        Route("https://b.test/", (204, wildcard), AllowOrigin(), AllowCredentials());
        using (var session = Session(_handler))
        using (var response = await session.SendAsync(Request("PUT", "https://b.test/", "x", ("X-Custom", "1")), Anonymous))
            Assert.Equal(200, response.StatusCode);
        Assert.Equal(TransportError.Cors, await CorsError(Request("PUT", "https://b.test/", "x"), Credentialed));
        Route("https://b.test/", (204, [AllowOrigin(), AllowCredentials(), ("Access-Control-Allow-Methods", "PUT"), ("Access-Control-Allow-Headers", "*")]),
            AllowOrigin(), AllowCredentials());
        Assert.Equal(TransportError.Cors, await CorsError(Request("PUT", "https://b.test/", "x", ("X-Custom", "1")), Credentialed));
    }

    [Fact]
    public async Task AuthorizationIsNeverCoveredByTheWildcard()
    {
        Route("https://b.test/", (204, [AllowOrigin(), ("Access-Control-Allow-Headers", "*")]), AllowOrigin());
        Assert.Equal(TransportError.Cors, await CorsError(Get("https://b.test/", ("Authorization", "Basic eA==")), Anonymous));
        Route("https://b.test/", (204, [AllowOrigin(), ("Access-Control-Allow-Headers", "*, Authorization")]), AllowOrigin());
        using var session = Session(_handler);
        using var response = await session.SendAsync(Get("https://b.test/", ("Authorization", "Basic eA==")), Anonymous);
        Assert.Equal("Basic eA==", _handler.Hops[^1].Header("Authorization"));
    }

    [Fact]
    public async Task EveryCorsHopIsPreflightedAndTaintedOriginsSerializeAsNull()
    {
        using var session = Session(_handler);
        _handler.On("https://b.test/1", r => r.Method == HttpMethod.Options
            ? ScriptedHandler.Reply(204, AllowOrigin(), ("Access-Control-Allow-Methods", "PUT"))
            : ScriptedHandler.Reply(307, AllowOrigin(), ("Location", "https://c.test/2")));
        Route("https://c.test/2", (204, [AllowOrigin("null"), ("Access-Control-Allow-Methods", "PUT")]), AllowOrigin("null"));
        using var response = await session.SendAsync(Request("PUT", "https://b.test/1", "x"), Anonymous);
        Assert.Equal(["OPTIONS https://b.test/1", "PUT https://b.test/1", "OPTIONS https://c.test/2", "PUT https://c.test/2"],
            _handler.Hops.Select(h => h.Target));
        Assert.Equal([A, A, "null", "null"], _handler.Hops.Select(h => h.Header("Origin")));
        Assert.Equal("x", System.Text.Encoding.UTF8.GetString(_handler.Hops[3].Body!));
    }

    [Fact]
    public async Task ReturningToTheRequestOriginStaysCorsWithANullOrigin()
    {
        _handler.Redirect("https://b.test/1", 302, A + "/2", AllowOrigin());
        _handler.On(A + "/2", 200, AllowOrigin());
        Assert.Equal(TransportError.Cors, await CorsError(Get("https://b.test/1"), Anonymous));
        Assert.Equal("null", _handler.Hops[^1].Header("Origin"));
        _handler.On(A + "/2", 200, AllowOrigin("null"));
        using var session = Session(_handler);
        using var response = await session.SendAsync(Get("https://b.test/1"), Anonymous);
        Assert.Equal(ResponseTainting.Cors, response.Tainting);
    }

    [Fact]
    public async Task SameOriginHopsDoNotTaintTheOrigin()
    {
        using var session = Session(_handler);
        _handler.Redirect(A + "/1", 302, "https://b.test/2");
        _handler.Redirect("https://b.test/2", 302, "/3", AllowOrigin());
        _handler.On("https://b.test/3", 200, AllowOrigin());
        using var response = await session.SendAsync(Get(A + "/1"), Anonymous);
        Assert.Equal([null, A, A], _handler.Hops.Select(h => h.Header("Origin")));
    }

    [Fact]
    public async Task CorsRedirectTargetsMustNotCarryCredentials()
    {
        _handler.Redirect(A + "/1", 302, "https://user:pw@b.test/");
        Assert.Equal(TransportError.Cors, await CorsError(Get(A + "/1"), Anonymous));
        _handler.Redirect("https://b.test/1", 302, "https://u@b.test/2", AllowOrigin());
        Assert.Equal(TransportError.Cors, await CorsError(Get("https://b.test/1"), Anonymous));
        _handler.Redirect(A + "/1", 302, "https://user@a.test/2");
        using var session = Session(_handler);
        using var response = await session.SendAsync(Get(A + "/1"), Anonymous);
        Assert.Equal("user", response.FinalUrl.UserInfo);
    }

    [Fact]
    public async Task ScriptVisibleHeadersFollowTheTainting()
    {
        using var session = Session(_handler);
        _handler.On("https://b.test/", 200, AllowOrigin(), ("Access-Control-Expose-Headers", "X-Exposed"), ("X-Exposed", "1"), ("X-Hidden", "1"),
            ("Set-Cookie", "c=1; SameSite=None; Secure"));
        using var response = await session.SendAsync(Get("https://b.test/"), Anonymous);
        var visible = response.GetScriptVisibleHeaders().Select(h => h.Key).ToArray();
        Assert.Contains("X-Exposed", visible);
        Assert.DoesNotContain("X-Hidden", visible);
        Assert.DoesNotContain("Set-Cookie", visible);
        Assert.Contains(response.Headers, h => h.Key == "Set-Cookie");
    }

    [Fact]
    public async Task WildcardExposureFollowsTheRequestsCredentialsMode()
    {
        using var session = Session(_handler);
        _handler.On("https://b.test/", 200, AllowOrigin(), AllowCredentials(), ("Access-Control-Expose-Headers", "*"), ("X-Internal", "1"));
        using (var credentialed = await session.SendAsync(Get("https://b.test/"), Credentialed))
        {
            Assert.Equal(CredentialsMode.Include, credentialed.Credentials);
            Assert.DoesNotContain(credentialed.GetScriptVisibleHeaders(), h => h.Key == "X-Internal");
        }
        using (var anonymous = await session.SendAsync(Get("https://b.test/"), Anonymous))
            Assert.Contains(anonymous.GetScriptVisibleHeaders(), h => h.Key == "X-Internal");
    }

    [Fact]
    public async Task ForbiddenHeadersNeverTriggerAPreflight()
    {
        using var session = Session(_handler);
        Route("https://b.test/font", (204, [AllowOrigin(), ("Access-Control-Allow-Headers", "x-custom")]), AllowOrigin());
        var font = RequestContext.Subresource(_top, RequestDestination.Font);
        (string, string)[] userAgentHeaders = [("Referer", A + "/page"), ("Sec-Fetch-Mode", "cors"), ("Accept-Encoding", "br"), ("DNT", "1")];
        using (await session.SendAsync(Get("https://b.test/font", userAgentHeaders), font)) { }
        Assert.Equal(["GET https://b.test/font"], _handler.Hops.Select(h => h.Target));
        Assert.Equal(A + "/page", _handler.Hops[0].Header("Referer"));
        using (await session.SendAsync(Get("https://b.test/font", [.. userAgentHeaders, ("X-Custom", "1")]), font)) { }
        Assert.Equal("x-custom", _handler.Hops[1].Header("Access-Control-Request-Headers"));
    }
}

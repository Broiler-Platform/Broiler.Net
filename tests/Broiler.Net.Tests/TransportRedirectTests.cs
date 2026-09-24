using System.Text;
using Broiler.Net.Http;
using static Broiler.Net.Tests.TransportTest;

namespace Broiler.Net.Tests;

public sealed class TransportRedirectTests
{
    private readonly ScriptedHandler _handler = new();

    [Theory]
    [InlineData(301, "POST", "GET")]
    [InlineData(302, "POST", "GET")]
    [InlineData(303, "POST", "GET")]
    [InlineData(303, "PUT", "GET")]
    [InlineData(303, "DELETE", "GET")]
    [InlineData(301, "PUT", "PUT")]
    [InlineData(302, "DELETE", "DELETE")]
    [InlineData(307, "POST", "POST")]
    [InlineData(308, "POST", "POST")]
    [InlineData(308, "PUT", "PUT")]
    public async Task RedirectMethodAndBodyRules(int status, string method, string expected)
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/start", status, "/end");
        var request = Request(method, "https://a.test/start", "payload", ("Content-Language", "en"), ("Content-Location", "/x"), ("X-Keep", "1"));
        request.Content!.Headers.ContentEncoding.Add("identity");
        using var response = await session.SendAsync(request, RequestContext.Fetch(Top("https://a.test/")));
        var hop = _handler.Hops[1];
        Assert.Equal(expected, hop.Method);
        Assert.Equal("1", hop.Header("X-Keep"));
        var keepsBody = method == expected;
        Assert.Equal(keepsBody ? "payload" : null, hop.Body is null ? null : Encoding.UTF8.GetString(hop.Body));
        foreach (var name in new[] { "Content-Type", "Content-Language", "Content-Location", "Content-Encoding" })
            Assert.Equal(keepsBody, hop.Header(name) is not null);
        Assert.Equal(["https://a.test/start", "https://a.test/end"], response.UrlList.Select(u => u.AbsoluteUri));
        Assert.True(response.Redirected);
    }

    [Theory]
    [InlineData(303, "HEAD")]
    [InlineData(303, "GET")]
    [InlineData(301, "HEAD")]
    public async Task SafeMethodsSurviveRedirects(int status, string method)
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/start", status, "/end");
        using (await session.SendAsync(Request(method, "https://a.test/start"), RequestContext.TopLevelNavigation(null))) { }
        Assert.Equal(method, _handler.Hops[1].Method);
    }

    [Fact]
    public async Task AuthorizationIsDroppedOnCrossOriginHops()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/1", 302, "/2");
        _handler.Redirect("https://a.test/2", 302, "https://b.test/3");
        _handler.Redirect("https://b.test/3", 302, "https://a.test/4");
        using (await session.SendAsync(Get("https://a.test/1", ("Authorization", "Basic eA==")), RequestContext.TopLevelNavigation(null))) { }
        Assert.Equal(["Basic eA==", "Basic eA==", null, null], _handler.Hops.Select(h => h.Header("Authorization")));
    }

    [Fact]
    public async Task RedirectLimitCountsFollowedRedirects()
    {
        using var session = Session(_handler, new() { MaximumRedirects = 2 });
        _handler.Redirect("https://a.test/0", 302, "/1");
        _handler.Redirect("https://a.test/1", 302, "/2");
        _handler.Redirect("https://a.test/2", 302, "/3");
        using (var twice = await session.SendAsync(Get("https://a.test/1"), RequestContext.TopLevelNavigation(null)))
            Assert.Equal(3, twice.UrlList.Count);
        Assert.Equal(TransportError.RedirectLimit, (await Fails(session.SendAsync(Get("https://a.test/0"), RequestContext.TopLevelNavigation(null)))).Error);
        Assert.Equal(6, _handler.Hops.Count);
        Assert.All(_handler.Responses.Skip(3), r => Assert.True(((TrackingContent)r.Content).Disposed));

        using var none = Session(_handler, new() { MaximumRedirects = 0 });
        Assert.Equal(TransportError.RedirectLimit, (await Fails(none.SendAsync(Get("https://a.test/2"), RequestContext.TopLevelNavigation(null)))).Error);
    }

    [Theory]
    [InlineData("ftp://a.test/x")]
    [InlineData("data:,x")]
    [InlineData("file:///c:/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://[bad")]
    [InlineData("https://a.test:22/")]
    public async Task RedirectTargetsMustBeFetchable(string location)
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/", 302, location);
        Assert.Equal(TransportError.RedirectScheme, (await Fails(session.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null)))).Error);
        Assert.True(((TrackingContent)Assert.Single(_handler.Responses).Content).Disposed);
    }

    [Fact]
    public async Task MultipleLocationFieldsAreANetworkError()
    {
        using var session = Session(_handler);
        _handler.On("https://a.test/", 302, ("Location", "/x"), ("Location", "/y"));
        Assert.Equal(TransportError.RedirectScheme, (await Fails(session.SendAsync(Get("https://a.test/"), RequestContext.TopLevelNavigation(null)))).Error);
    }

    [Fact]
    public async Task ErrorModeFailsAfterStoringCookies()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/", 302, "/next", ("Set-Cookie", "r=1"));
        var context = RequestContext.Fetch(Top("https://a.test/"), redirect: RedirectMode.Error);
        Assert.Equal(TransportError.RedirectDisallowed, (await Fails(session.SendAsync(Get("https://a.test/"), context))).Error);
        Assert.Single(_handler.Hops);
        Assert.Contains(session.Cookies.Snapshot(), c => c.Name == "r");
        Assert.True(((TrackingContent)_handler.Responses[0].Content).Disposed);
    }

    [Fact]
    public async Task ManualModeReturnsAnOpaqueRedirectUnlessNavigating()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/", 301, "https://idp.test/cb?code=SECRET", ("X-Leak", "1"));
        using (var response = await session.SendAsync(Get("https://a.test/"), RequestContext.Fetch(Top("https://a.test/"), redirect: RedirectMode.Manual)))
        {
            Assert.Equal(ResponseTainting.OpaqueRedirect, response.Tainting);
            Assert.Equal(0, response.ScriptVisibleStatusCode);
            Assert.Empty(response.GetScriptVisibleHeaders());
            // The privileged view keeps the redirect for the host.
            Assert.Equal(301, response.StatusCode);
            Assert.False(response.Redirected);
            Assert.Contains(response.Headers, h => h.Key == "Location" && h.Value == "https://idp.test/cb?code=SECRET");
        }
        var navigation = RequestContext.TopLevelNavigation(null) with { Redirect = RedirectMode.Manual };
        using (var response = await session.SendAsync(Get("https://a.test/"), navigation))
        {
            Assert.Equal(ResponseTainting.Basic, response.Tainting);
            Assert.Equal(301, response.ScriptVisibleStatusCode);
        }
        Assert.Equal(2, _handler.Hops.Count);
    }

    [Fact]
    public async Task RedirectModeIsAppliedBeforeLocationIsRead()
    {
        using var session = Session(_handler);
        _handler.On("https://a.test/", 302);
        var top = Top("https://a.test/");
        using (var response = await session.SendAsync(Get("https://a.test/"), RequestContext.Fetch(top)))
            Assert.Equal(302, response.StatusCode);
        Assert.Equal(TransportError.RedirectDisallowed, (await Fails(session.SendAsync(Get("https://a.test/"), RequestContext.Fetch(top, redirect: RedirectMode.Error)))).Error);
        using (var response = await session.SendAsync(Get("https://a.test/"), RequestContext.Fetch(top, redirect: RedirectMode.Manual)))
            Assert.Equal(ResponseTainting.OpaqueRedirect, response.Tainting);
    }

    [Fact]
    public async Task ManualContinuationsCountTowardsTheRedirectLimit()
    {
        using var session = Session(_handler, new() { MaximumRedirects = 2 });
        _handler.Redirect("https://a.test/2", 302, "/3");
        var chain = new[] { new Uri("https://a.test/0"), new Uri("https://a.test/1") };
        var context = RequestContext.TopLevelNavigation(null) with { RedirectChain = chain };
        Assert.Equal(TransportError.RedirectLimit, (await Fails(session.SendAsync(Get("https://a.test/2"), context))).Error);
        Assert.Single(_handler.Hops);
        Assert.Equal(TransportError.RedirectLimit, (await Fails(session.SendAsync(Get("https://a.test/3"), context with { RedirectChain = [.. chain, new("https://a.test/2")] }))).Error);
        Assert.Single(_handler.Hops);
        Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get("https://a.test/"), context with { RedirectChain = [] }))).Error);
        Assert.Equal(TransportError.InvalidRequest, (await Fails(session.SendAsync(Get("https://a.test/"), context with { RedirectChain = [new("ftp://a.test/")] }))).Error);
    }

    [Fact]
    public async Task ContinuationsReplayTheTaintedOriginAndAuthorizationRules()
    {
        using var session = Session(_handler);
        _handler.On("https://c.test/3", 200, ("Access-Control-Allow-Origin", "null"));
        var context = RequestContext.Fetch(Top("https://a.test/"), redirect: RedirectMode.Manual) with
            { RedirectChain = [new("https://b.test/1"), new("https://b.test/2")] };
        using (var response = await session.SendAsync(Get("https://c.test/3", ("Authorization", "Basic eA==")), context))
            Assert.Equal(ResponseTainting.Cors, response.Tainting);
        var hop = Assert.Single(_handler.Hops);
        Assert.Equal("null", hop.Header("Origin"));
        Assert.Null(hop.Header("Authorization"));
    }

    [Fact]
    public async Task HopPolicyRunsBeforeEveryHop()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/img", 302, "https://tracker.test/pixel");
        var seen = new List<(string, int)>();
        var context = RequestContext.Subresource(Top("https://a.test/"), RequestDestination.Image) with
            { HopPolicy = (url, redirects) => { seen.Add((url.AbsoluteUri, redirects)); return url.Host != "tracker.test"; } };
        Assert.Equal(TransportError.Blocked, (await Fails(session.SendAsync(Get("https://a.test/img"), context))).Error);
        Assert.Equal([("https://a.test/img", 0), ("https://tracker.test/pixel", 1)], seen);
        Assert.Equal(["https://a.test/img"], _handler.Hops.Select(h => h.Url.AbsoluteUri));
        Assert.True(((TrackingContent)Assert.Single(_handler.Responses).Content).Disposed);
        Assert.Equal(TransportError.Blocked, (await Fails(session.SendAsync(Get("https://tracker.test/"), context))).Error);
        Assert.Single(_handler.Hops);
    }

    [Fact]
    public async Task ContentOnlyHeadersLeaveWithTheBody()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/p", 303, "https://c.test/q");
        _handler.On("https://c.test/q", 200, ("Access-Control-Allow-Origin", "https://a.test"));
        var request = Request("POST", "https://a.test/p", "x");
        request.Content!.Headers.TryAddWithoutValidation("Content-Disposition", "inline");
        request.Content.Headers.TryAddWithoutValidation("Expires", "0");
        using (await session.SendAsync(request, RequestContext.Fetch(Top("https://a.test/")))) { }
        // Neither preflighted nor silently missing: the GET to c.test is simple.
        Assert.Equal(["POST https://a.test/p", "GET https://c.test/q"], _handler.Hops.Select(h => h.Target));
        Assert.Equal("inline", _handler.Hops[0].Header("Content-Disposition"));
        Assert.Null(_handler.Hops[1].Header("Content-Disposition"));
        Assert.Null(_handler.Hops[1].Header("Expires"));
    }

    [Fact]
    public async Task RefererAndFetchMetadataStayWithTheFirstOrigin()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/1", 302, "/2");
        _handler.Redirect("https://a.test/2", 302, "http://b.test/3");
        _handler.Redirect("http://b.test/3", 302, "https://a.test/4");
        using (await session.SendAsync(Get("https://a.test/1", ("Referer", "https://a.test/account?ssn=123"), ("Sec-Fetch-Site", "same-origin")),
            RequestContext.TopLevelNavigation(Top("https://a.test/")))) { }
        Assert.Equal(["https://a.test/account?ssn=123", "https://a.test/account?ssn=123", null, null], _handler.Hops.Select(h => h.Header("Referer")));
        Assert.Equal(["same-origin", "same-origin", null, null], _handler.Hops.Select(h => h.Header("Sec-Fetch-Site")));
    }

    [Theory]
    [InlineData("/next", "https://a.test/next#frag")]
    [InlineData("/next#own", "https://a.test/next#own")]
    [InlineData("?q=1", "https://a.test/start?q=1#frag")]
    [InlineData("https://b.test/", "https://b.test/#frag")]
    public async Task LocationsResolveAgainstTheCurrentUrlAndInheritItsFragment(string location, string expected)
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/start", 302, location);
        using var response = await session.SendAsync(Get("https://a.test/start#frag"), RequestContext.TopLevelNavigation(null));
        Assert.Equal(expected, response.FinalUrl.AbsoluteUri);
        Assert.Equal("https://a.test/start#frag", response.UrlList[0].AbsoluteUri);
    }

    [Fact]
    public async Task CookiesAreStoredAndSentOnEveryHop()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/1", 302, "https://b.test/2", ("Set-Cookie", "s1=1; Path=/"));
        _handler.Redirect("https://b.test/2", 307, "https://a.test/3", ("Set-Cookie", "s2=2; Path=/"));
        _handler.On("https://a.test/3", 500, ("Set-Cookie", "s3=3; Path=/"));
        using (var response = await session.SendAsync(Get("https://a.test/1"), RequestContext.TopLevelNavigation(null)))
        {
            Assert.Equal(500, response.StatusCode);
            Assert.Equal(["s1=1; Path=/", "s2=2; Path=/", "s3=3; Path=/"],
                _handler.Responses.SelectMany(r => r.Headers.NonValidated["Set-Cookie"]));
        }
        Assert.Equal([null, null, "s1=1"], _handler.Hops.Select(h => h.Cookie()));
        Assert.Equal(["a.test", "b.test", "a.test"], session.Cookies.Snapshot().Select(c => c.Domain));
        using (await session.SendAsync(Get("https://a.test/again"), RequestContext.TopLevelNavigation(null))) { }
        Assert.Equal("s1=1; s3=3", _handler.Hops[^1].Cookie());
    }

    [Fact]
    public async Task IntermediateResponsesAreDisposedAndTheFinalIsNot()
    {
        using var session = Session(_handler);
        _handler.Redirect("https://a.test/1", 302, "/2");
        _handler.Redirect("https://a.test/2", 308, "/3");
        using var response = await session.SendAsync(Get("https://a.test/1"), RequestContext.TopLevelNavigation(null));
        Assert.Equal([true, true, false], _handler.Responses.Select(r => ((TrackingContent)r.Content).Disposed));
        Assert.Same(_handler.Responses[^1], response.Message);
    }

    [Fact]
    public async Task SameOriginModeRejectsCrossOriginUrlsBeforeSending()
    {
        using var session = Session(_handler);
        var context = RequestContext.Fetch(Top("https://a.test/"), RequestMode.SameOrigin);
        Assert.Equal(TransportError.SameOriginViolation, (await Fails(session.SendAsync(Get("https://b.test/"), context))).Error);
        Assert.Empty(_handler.Hops);
        _handler.Redirect("https://a.test/", 302, "https://b.test/");
        Assert.Equal(TransportError.SameOriginViolation, (await Fails(session.SendAsync(Get("https://a.test/"), context))).Error);
        Assert.Single(_handler.Hops);
        Assert.True(((TrackingContent)_handler.Responses[0].Content).Disposed);
        _handler.Redirect("https://a.test/", 302, "/same");
        using var same = await session.SendAsync(Get("https://a.test/"), context);
        Assert.Equal(ResponseTainting.Basic, same.Tainting);
    }
}

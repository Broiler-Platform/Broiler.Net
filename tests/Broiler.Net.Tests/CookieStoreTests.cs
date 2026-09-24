using Broiler.Net.Cookies;
using Broiler.Net.Sites;

namespace Broiler.Net.Tests;

public sealed class CookieStoreTests
{
    private readonly TestClock _clock = new();
    private CookieStore Store(CookieStoreOptions? options = null) => new(clock: _clock, options: options);
    internal static CookieRequestContext Request(string url = "https://www.example.test/a/page", SameSiteStatus site = SameSiteStatus.SameSite,
        string method = "GET", bool navigation = false, CookiePartitionKey? partition = null) => new(new(url), site, method, navigation, partition);
    internal static CookieDocumentContext Document(string url = "https://www.example.test/a/page", SameSiteStatus site = SameSiteStatus.SameSite,
        CookiePartitionKey? partition = null) => new(new(url), site, partition);
    internal static CookiePartitionKey Partition(string top, bool ancestor = false) => new(SiteResolver.Default.GetSite(new(top))!, ancestor);

    [Fact]
    public void HostOnlyDomainAndPathScopeRemainDistinct()
    {
        var store = Store();
        store.ReceiveResponseCookie("host=h", Request());
        store.ReceiveResponseCookie("domain=d;Domain=example.test;Path=/", Request());
        Assert.Equal("host=h; domain=d", store.BuildRequestHeader(Request()));
        Assert.Equal("domain=d", store.BuildRequestHeader(Request("https://other.example.test/a/page")));
        Assert.Equal("domain=d", store.BuildRequestHeader(Request("https://www.example.test/ab/page")));
        Assert.Equal("", store.BuildRequestHeader(Request("https://notexample.test/a/page")));
        Assert.Equal("", store.BuildRequestHeader(Request("https://www.example.test./a/page")));
    }

    [Fact]
    public void IdentityIncludesHostOnlyFlagAndDomainCookieCanBeDeletedIndependently()
    {
        var store = Store();
        store.ReceiveResponseCookie("id=host;Path=/", Request());
        store.ReceiveResponseCookie("id=domain;Domain=www.example.test;Path=/", Request());
        Assert.Equal("id=host; id=domain", store.BuildRequestHeader(Request()));
        store.ReceiveResponseCookie("id=;Domain=www.example.test;Path=/;Max-Age=0", Request());
        Assert.Equal("id=host", store.BuildRequestHeader(Request()));
    }

    [Theory]
    [InlineData("a=b;Domain=evil.test", CookieDecision.RejectedDomain)]
    [InlineData("a=b;Domain=test", CookieDecision.RejectedPublicSuffix)]
    [InlineData("a=b;Domain=..example.test", CookieDecision.RejectedDomain)]
    [InlineData("a=b;Domain=example.test.", CookieDecision.RejectedDomain)]
    [InlineData("a=b;Domain=éxample.test", CookieDecision.RejectedDomain)]
    [InlineData("a=b;Domain=example.test:80", CookieDecision.RejectedDomain)]
    public void RejectsUnsafeDomains(string field, CookieDecision decision) => Assert.Equal(decision, Store().ReceiveResponseCookie(field, Request()).Decision);

    [Fact]
    public void PublicSuffixExactHostBecomesHostOnlyAndPrivateSuffixCannotBeShared()
    {
        var store = Store();
        Assert.True(store.ReceiveResponseCookie("a=b;Domain=github.io", Request("https://github.io")).Accepted);
        Assert.True(Assert.Single(store.Snapshot()).HostOnly);
        Assert.Equal(CookieDecision.RejectedPublicSuffix, store.ReceiveResponseCookie("x=y;Domain=github.io", Request("https://user.github.io")).Decision);
        Assert.Equal("", store.BuildRequestHeader(Request("https://user.github.io")));
    }

    [Fact]
    public void CanonicalizesIdnAndIpv6AndDoesNotSuffixMatchAnIp()
    {
        var store = Store();
        Assert.True(store.ReceiveResponseCookie("a=b;Domain=xn--bcher-kva.test", Request("https://bücher.test")).Accepted);
        Assert.Equal("a=b", store.BuildRequestHeader(Request("https://xn--bcher-kva.test")));
        Assert.True(store.ReceiveResponseCookie("ip=v6", Request("http://[::1]")).Accepted);
        Assert.Equal("ip=v6", store.BuildRequestHeader(Request("http://[0:0:0:0:0:0:0:1]:8080")));
        Assert.Equal(CookieDecision.RejectedDomain, store.ReceiveResponseCookie("ip=x;Domain=0.0.1", Request("http://127.0.0.1")).Decision);
    }

    [Theory]
    [InlineData("/", "/")][InlineData("/a", "/")][InlineData("/a/", "/a")]
    [InlineData("/a/b?q=x", "/a")][InlineData("/a%2Fb/c", "/a%2Fb")]
    public void UsesDefaultPathFromRequestUrl(string target, string path) => Assert.Equal(path, CookiePolicy.DefaultPath(new("https://example.test" + target)));

    [Theory]
    [InlineData("/a", "/a", true)][InlineData("/a/b", "/a", true)]
    [InlineData("/ab", "/a", false)][InlineData("/a", "/a/", false)]
    [InlineData("/a%2Fb", "/a", false)][InlineData("/a/b", "/", true)]
    public void PathMatchUsesBoundariesNotJustPrefix(string request, string cookie, bool matches) => Assert.Equal(matches, CookiePolicy.PathMatches(request, cookie));

    [Fact]
    public void ReplacementPreservesCreationOrderAndLongestPathSortsFirst()
    {
        var store = Store();
        store.ReceiveResponseCookie("first=1;Path=/", Request());
        _clock.Advance(TimeSpan.FromSeconds(1));
        store.ReceiveResponseCookie("second=2;Path=/", Request());
        store.ReceiveResponseCookie("deep=3;Path=/a", Request());
        var original = store.Snapshot()[0];
        _clock.Advance(TimeSpan.FromSeconds(1));
        store.ReceiveResponseCookie("first=updated;Path=/", Request());
        Assert.Equal("deep=3; first=updated; second=2", store.BuildRequestHeader(Request()));
        Assert.Equal(original.Created, store.Snapshot()[0].Created);
        Assert.Equal(original.CreationOrder, store.Snapshot()[0].CreationOrder);
        Assert.All(store.Snapshot(), c => Assert.Equal(_clock.GetUtcNow(), c.LastAccessed));
    }

    [Fact]
    public void NamesAreCaseSensitiveAndNamelessCookiesSerializeWithoutEquals()
    {
        var store = Store();
        store.ReceiveResponseCookies(["ID=1", "id=2", "token", "empty="], Request());
        Assert.Equal("ID=1; id=2; token; empty=", store.BuildRequestHeader(Request()));
    }

    [Fact]
    public void ResponseFieldsWithExpiresCommasRemainSeparate()
    {
        var store = Store();
        var results = store.ReceiveResponseCookies(["a=1;Expires=Wed, 09 Jun 2026 10:18:14 GMT", "b=2"], Request());
        Assert.All(results, r => Assert.Equal(CookieDecision.Stored, r.Decision));
        Assert.Equal("a=1; b=2", store.BuildRequestHeader(Request()));
    }

    [Fact]
    public void ExpiryIsExactAndMaxAgeWinsOverExpires()
    {
        var store = Store();
        store.ReceiveResponseCookie("a=1;Max-Age=10;Expires=Thu, 01 Jan 1970 00:00:00 GMT", Request());
        store.ReceiveResponseCookie("session=2", Request());
        _clock.Advance(TimeSpan.FromSeconds(9));
        Assert.Equal("a=1; session=2", store.BuildRequestHeader(Request()));
        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal("session=2", store.BuildRequestHeader(Request()));
        Assert.False(Assert.Single(store.Snapshot()).Persistent);
        Assert.Equal(CookieDecision.Deleted, store.ReceiveResponseCookie("absent=;Max-Age=0", Request()).Decision);
    }

    [Fact]
    public void SecureAndHttpOnlyBoundariesCoverBothReadingAndWriting()
    {
        var store = Store();
        Assert.Equal(CookieDecision.RejectedSecure, store.ReceiveResponseCookie("a=b;Secure", Request("http://www.example.test/a")).Decision);
        store.ReceiveResponseCookie("secret=s;Secure;HttpOnly", Request());
        Assert.Equal("secret=s", store.BuildRequestHeader(Request()));
        Assert.Equal("", store.BuildRequestHeader(Request("http://www.example.test/a/page")));
        Assert.Equal("", store.GetDocumentCookies(Document()));
        Assert.Equal(CookieDecision.RejectedHttpOnly, store.SetDocumentCookie("secret=x", Document()).Decision);
        Assert.Equal(CookieDecision.RejectedHttpOnly, store.SetDocumentCookie("secret=;Max-Age=0", Document()).Decision);
        Assert.Equal(CookieDecision.RejectedHttpOnly, store.SetDocumentCookie("other=x;HttpOnly", Document()).Decision);
        Assert.True(store.SetDocumentCookie("visible=v;Secure", Document()).Accepted);
        Assert.Equal("visible=v", store.GetDocumentCookies(Document()));
    }

    [Theory]
    [InlineData("/login", false)][InlineData("/login/en", false)][InlineData("/", true)][InlineData("/foo", true)]
    public void InsecureOverlayUsesAsymmetricPathRule(string path, bool accepted)
    {
        var store = Store();
        store.ReceiveResponseCookie("id=secure;Secure;Domain=example.test;Path=/login", Request());
        var result = store.ReceiveResponseCookie("id=unsafe;Path=" + path, Request("http://www.example.test/login"));
        Assert.Equal(accepted, result.Accepted);
        if (!accepted) Assert.Equal(CookieDecision.RejectedSecureOverlay, result.Decision);
    }

    [Fact]
    public void ExpiredSecureCookieDoesNotBlockNewInsecureCookie()
    {
        var store = Store();
        store.ReceiveResponseCookie("id=old;Secure;Max-Age=1", Request());
        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(store.ReceiveResponseCookie("id=new", Request("http://www.example.test/a/page")).Accepted);
    }

    [Theory]
    [InlineData("__Secure-id=x", false)]
    [InlineData("__SECURE-id=x;Secure", true)]
    [InlineData("__Host-id=x;Secure;Path=/", true)]
    [InlineData("__host-id=x;Secure", false)]
    [InlineData("__Host-id=x;Secure;Path=/;Domain=example.test", false)]
    [InlineData("__Host-id=x;Secure;Path=/a", false)]
    [InlineData("=__Host-id=x;Secure;Path=/", false)]
    [InlineData("__secure-id;Secure", false)]
    [InlineData("__http-id;Secure;HttpOnly", false)]
    [InlineData("__Host-Http-id;Secure;HttpOnly", false)]
    public void EnforcesCaseInsensitivePrefixesAndNamelessProtection(string text, bool accepted) =>
        Assert.Equal(accepted, Store().ReceiveResponseCookie(text, Request()).Accepted);

    // layered-cookies-02 5.4.3 and WPT cookies/prefix/__Http.https.html, __Host-Http.https.html (e35344a).
    [Theory]
    [InlineData("__Http-id=x;Path=/", false, false)]
    [InlineData("__Http-id=x;Secure;Path=/", false, false)]
    [InlineData("__Http-id=x;Secure;Path=/;httponly", true, false)]
    [InlineData("__Http-id=x;Secure;Path=/cookies/;httponly", true, false)]
    [InlineData("__HTTP-id=x;Secure;Path=/", false, false)]
    [InlineData("__Host-Http-id=x;Secure;Path=/", false, false)]
    [InlineData("__Host-Http-id=x;Secure;Path=/;httponly", true, false)]
    [InlineData("__host-http-id=x;Secure;Path=/;httponly", true, false)]
    [InlineData("__Host-Http-id=x;Secure;Path=/cookies/;httponly", false, false)]
    [InlineData("__Host-Http-id=x;Path=/;httponly", false, false)]
    [InlineData("__Host-Http-id=x;Secure;Path=/;httponly;Domain=www.example.test", false, false)]
    public void HttpPrefixesRequireSecureHttpOnlyAndAnHttpSource(string text, bool viaHttp, bool viaDocument)
    {
        var url = "https://www.example.test/cookies/page";
        Assert.Equal(viaHttp, Store().ReceiveResponseCookie(text, Request(url)).Accepted);
        Assert.Equal(viaDocument, Store().SetDocumentCookie(text, Document(url)).Accepted);
    }

    [Theory]
    [InlineData("")][InlineData(";Path=")][InlineData(";Path")][InlineData(";Path=relative")]
    [InlineData(";Path=/;Path=")][InlineData(";Path=/;Path=relative")]
    public void HostPrefixRequiresExplicitSlashEvenWhenDefaultPathIsRoot(string attributes)
    {
        var store = Store();
        Assert.Equal(CookieDecision.RejectedPrefix,
            store.ReceiveResponseCookie("__Host-id=x;Secure" + attributes, Request("https://www.example.test/")).Decision);
        Assert.Equal(CookieDecision.RejectedPrefix,
            store.SetDocumentCookie("__Host-id=x;Secure" + attributes, Document("https://www.example.test/")).Decision);
    }

    [Theory]
    [InlineData("Strict", false, "GET", false)]
    [InlineData("Strict", true, "GET", false)]
    [InlineData("Lax", true, "GET", true)]
    [InlineData("Lax", true, "POST", false)]
    [InlineData("Lax", false, "GET", false)]
    [InlineData("None", false, "POST", true)]
    [InlineData("bogus", true, "POST", false)]
    [InlineData("bogus", true, "HEAD", true)]
    public void CrossSiteRetrievalHasNoRecentUnsafeGrace(string sameSite, bool navigation, string method, bool sent)
    {
        var store = Store();
        store.ReceiveResponseCookie($"id=x;Secure;SameSite={sameSite}", Request());
        Assert.Equal(sent ? "id=x" : "", store.BuildRequestHeader(Request(site: SameSiteStatus.CrossSite, method: method, navigation: navigation)));
    }

    [Fact]
    public void CrossSiteSettingDistinguishesNavigationFromDocumentAndSubresource()
    {
        var store = Store();
        Assert.Equal(CookieDecision.RejectedSameSite, store.ReceiveResponseCookie("id=x", Request(site: SameSiteStatus.CrossSite)).Decision);
        Assert.True(store.ReceiveResponseCookie("id=x;SameSite=Strict", Request(site: SameSiteStatus.CrossSite, method: "POST", navigation: true)).Accepted);
        Assert.Equal("", store.GetDocumentCookies(Document(site: SameSiteStatus.CrossSite)));
        Assert.Equal(CookieDecision.RejectedSameSite, store.SetDocumentCookie("id=x", Document(site: SameSiteStatus.CrossSite)).Decision);
        Assert.Equal(CookieDecision.RejectedSameSite, store.ReceiveResponseCookie("id=x;SameSite=None", Request()).Decision);
        Assert.True(store.SetDocumentCookie("third=x;SameSite=None;Secure", Document(site: SameSiteStatus.CrossSite)).Accepted);
    }

    [Fact]
    public void PartitionedAndUnpartitionedCookiesCoexistWithoutLeakingAcrossKeys()
    {
        var store = Store();
        var a = Partition("https://a.test"); var b = Partition("https://b.test");
        var withAncestor = Partition("https://a.test", ancestor: true);
        Assert.Equal(CookieDecision.RejectedPartition, store.ReceiveResponseCookie("id=bad;Secure;Partitioned", Request()).Decision);
        Assert.Equal(CookieDecision.RejectedPartition, store.ReceiveResponseCookie("id=bad;Partitioned", Request(partition: a)).Decision);
        store.ReceiveResponseCookie("id=global;Secure;SameSite=None", Request());
        store.ReceiveResponseCookie("id=a;Secure;SameSite=None;Partitioned", Request(partition: a));
        store.ReceiveResponseCookie("id=b;Secure;SameSite=None;Partitioned", Request(partition: b));
        Assert.Equal("id=global; id=a", store.BuildRequestHeader(Request(partition: a)));
        Assert.Equal("id=global; id=b", store.GetDocumentCookies(Document(partition: b)));
        Assert.Equal("id=global", store.BuildRequestHeader(Request(partition: withAncestor)));
        store.SetDocumentCookie("id=;Secure;SameSite=None;Partitioned;Max-Age=0", Document(partition: a));
        Assert.Equal("id=global", store.BuildRequestHeader(Request(partition: a)));
        Assert.Equal("id=global; id=b", store.BuildRequestHeader(Request(partition: b)));
    }

    [Fact]
    public void DocumentUnicodeRoundTripsAsUtf8WhileHttpUsesOctets()
    {
        var store = Store();
        Assert.True(store.SetDocumentCookie("greeting=Grüße🍪", Document()).Accepted);
        Assert.Equal("greeting=Grüße🍪", store.GetDocumentCookies(Document()));
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes("greeting=Grüße🍪"), System.Text.Encoding.Latin1.GetBytes(store.BuildRequestHeader(Request())));
        Assert.Equal(CookieDecision.RejectedSize, store.SetDocumentCookie("n=" + new string('é', 2048), Document()).Decision);
    }

    [Theory]
    [InlineData("GET", true)][InlineData("get", true)][InlineData("Head", true)][InlineData("options", true)]
    [InlineData("TRACE", true)][InlineData("trace", false)][InlineData("post", false)][InlineData("", false)][InlineData(null, false)]
    public void SafeMethodCheckNormalizesStandardMethodsLikeFetch(string? method, bool sent)
    {
        var store = Store();
        store.ReceiveResponseCookie("lax=1;SameSite=Lax", Request());
        Assert.Equal(sent ? "lax=1" : "", store.BuildRequestHeader(Request(site: SameSiteStatus.CrossSite, method: method!, navigation: true)));
    }

    [Fact]
    public void ResultingPathIncludingADefaultedOneIsBoundedTo1024Octets()
    {
        var store = Store();
        var longest = "https://www.example.test/" + new string('a', 1023) + "/page";
        var tooLong = "https://www.example.test/" + new string('a', 1024) + "/page";
        Assert.Equal(CookieDecision.Stored, store.ReceiveResponseCookie("a=1", Request(longest)).Decision);
        Assert.Equal(CookieDecision.RejectedSize, store.ReceiveResponseCookie("b=1", Request(tooLong)).Decision);
        Assert.Equal(CookieDecision.RejectedSize, store.ReceiveResponseCookie("b=1;Path=" + new string('/', 1025), Request(tooLong)).Decision);
        Assert.Equal(CookieDecision.RejectedSize, store.SetDocumentCookie("b=1", Document(tooLong)).Decision);
        Assert.Equal(CookieDecision.Stored, store.ReceiveResponseCookie("c=1;Path=/", Request(tooLong)).Decision);
    }

    [Fact]
    public void PastExpiresWithoutMaxAgeDeletesTheCookie()
    {
        var store = Store();
        store.ReceiveResponseCookie("e=1;Path=/", Request());
        Assert.Equal(CookieDecision.Deleted, store.ReceiveResponseCookie("e=;Path=/;Expires=Thu, 01 Jan 1970 00:00:00 GMT", Request()).Decision);
        Assert.Equal("", store.BuildRequestHeader(Request()));
        store.SetDocumentCookie("d=1;Path=/", Document());
        Assert.Equal(CookieDecision.Deleted, store.SetDocumentCookie("d=;Path=/;Expires=Thu, 01 Jan 1970 00:00:00 GMT", Document()).Decision);
        Assert.Equal("", store.GetDocumentCookies(Document()));
    }

    [Fact]
    public void InsecureOverlayChecksDomainMatchInBothDirectionsForHttpAndDocument()
    {
        var store = Store();
        store.ReceiveResponseCookie("id=secure;Secure;Path=/", Request("https://www.x.test/"));
        Assert.Equal(CookieDecision.RejectedSecureOverlay, store.ReceiveResponseCookie("id=a;Domain=x.test;Path=/", Request("http://www.x.test/")).Decision);
        Assert.Equal(CookieDecision.RejectedSecureOverlay, store.SetDocumentCookie("id=b;Domain=x.test;Path=/", Document("http://www.x.test/")).Decision);
        Assert.Equal(CookieDecision.RejectedSecureOverlay, store.SetDocumentCookie("id=c;Path=/", Document("http://www.x.test/")).Decision);
        Assert.True(store.ReceiveResponseCookie("id=d;Path=/", Request("http://other.x.test/")).Accepted);
    }

    [Fact]
    public void NamelessCookieReplacesTheSameNamelessIdentity()
    {
        var store = Store();
        store.ReceiveResponseCookie("tok1", Request());
        store.ReceiveResponseCookie("=tok2", Request());
        Assert.Equal("tok2", store.BuildRequestHeader(Request()));
        Assert.Single(store.Snapshot());
    }

    [Fact]
    public void CrossSiteDocumentReadsOnlySameSiteNoneCookies()
    {
        var store = Store();
        store.ReceiveResponseCookie("none=1;Secure;SameSite=None", Request());
        store.ReceiveResponseCookie("lax=1;SameSite=Lax", Request());
        store.ReceiveResponseCookie("default=1", Request());
        Assert.Equal("none=1", store.GetDocumentCookies(Document(site: SameSiteStatus.CrossSite)));
        Assert.Equal("none=1; lax=1; default=1", store.GetDocumentCookies(Document()));
    }

    [Fact]
    public void UrisThatFailIdnaDoNotThrowAndHaveNoCookieHost()
    {
        var url = new Uri("http://a\u200Db.com/");
        var store = Store();
        Assert.Equal(CookieDecision.RejectedScheme, store.ReceiveResponseCookie("z=1", new(url, SameSiteStatus.SameSite)).Decision);
        Assert.Equal(CookieDecision.RejectedScheme, store.SetDocumentCookie("z=1", new(url, SameSiteStatus.SameSite)).Decision);
        Assert.Equal("", store.BuildRequestHeader(new(url, SameSiteStatus.SameSite)));
        Assert.Equal("", store.GetDocumentCookies(new(url, SameSiteStatus.SameSite)));
    }

    [Fact]
    public void FileCookiesAreRejectedAndSeparateStoresAreIsolated()
    {
        var a = Store(); var b = Store();
        Assert.Equal(CookieDecision.RejectedScheme, a.SetDocumentCookie("id=x", Document("file:///test.html")).Decision);
        a.ReceiveResponseCookie("id=x", Request());
        Assert.Equal("", b.BuildRequestHeader(Request()));
        Assert.Equal("", a.GetDocumentCookies(Document("file:///test.html")));
        Assert.DoesNotContain("id=x", Assert.Single(a.Snapshot()).ToString());
    }
}

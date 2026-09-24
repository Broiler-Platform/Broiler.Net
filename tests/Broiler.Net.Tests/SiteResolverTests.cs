using Broiler.Net.Sites;

namespace Broiler.Net.Tests;

public sealed class SiteResolverTests
{
    [Theory]
    [InlineData("https://www.example.co.uk/a", "example.co.uk")]
    [InlineData("https://a.b.github.io", "b.github.io")]
    [InlineData("https://www.食狮.com.cn", "xn--85x722f.com.cn")]
    [InlineData("http://127.0.0.1", "127.0.0.1")]
    [InlineData("http://[::1]", "::1")]
    [InlineData("http://localhost", "localhost")]
    [InlineData("https://www.example.com.", "example.com.")]
    public void ResolvesPinnedPublicAndPrivateSuffixes(string url, string host) =>
        Assert.Equal(host, SiteResolver.Default.GetSite(new(url))!.Host);

    [Theory]
    [InlineData("com", true, null)]
    [InlineData("co.uk", true, null)]
    [InlineData("github.io", true, null)]
    [InlineData("a.github.io", false, "a.github.io")]
    [InlineData("a.ck", true, null)]
    [InlineData("b.a.ck", false, "b.a.ck")]
    [InlineData("www.ck", false, "www.ck")]
    [InlineData("x.www.ck", false, "www.ck")]
    [InlineData("foo.unknown-tld", false, "foo.unknown-tld")]
    [InlineData("localhost", true, null)]
    [InlineData("127.0.0.1", false, null)]
    public void ImplementsLongestRuleWildcardAndException(string host, bool suffix, string? registrable)
    {
        Assert.Equal(suffix, SiteResolver.Default.IsPublicSuffix(host));
        Assert.Equal(registrable, SiteResolver.Default.GetRegistrableDomain(host));
    }

    [Fact]
    public void SitesAreSchemefulAndIgnorePortsButKeepTrailingDot()
    {
        var sites = SiteResolver.Default;
        Assert.Equal(sites.GetSite(new("https://a.example.test:8443")), sites.GetSite(new("https://b.example.test")));
        Assert.NotEqual(sites.GetSite(new("http://example.test")), sites.GetSite(new("https://example.test")));
        Assert.NotEqual(sites.GetSite(new("https://example.test.")), sites.GetSite(new("https://example.test")));
        Assert.Null(sites.GetSite(new("file:///test.html")));
        Assert.Null(sites.GetSite(new("relative", UriKind.Relative)));
    }

    [Theory]
    [InlineData("https://example.test", true)]
    [InlineData("http://example.test", false)]
    [InlineData("http://localhost", true)]
    [InlineData("http://sub.localhost.", true)]
    [InlineData("http://127.0.0.2", true)]
    [InlineData("http://[::1]", true)]
    [InlineData("http://127.0.0.1.", true)]
    [InlineData("http://0x7f.1", true)]
    [InlineData("http://[::ffff:127.0.0.1]", false)]
    [InlineData("http://[::ffff:7f00:1]", false)]
    [InlineData("http://128.0.0.1", false)]
    [InlineData("http://localhost.example.test", false)]
    [InlineData("http://192.168.1.1", false)]
    [InlineData("file:///test.html", false)]
    public void SecureOriginsIncludeLoopbackOnly(string url, bool secure) => Assert.Equal(secure, HostNames.IsSecure(new(url)));

    [Theory]
    [InlineData("")][InlineData(".")][InlineData("..example.test")][InlineData("example..test")]
    [InlineData("example.test..")][InlineData("a/b")][InlineData("a@b")][InlineData("a%20b")]
    public void RejectsInvalidHostNames(string input) => Assert.False(HostNames.TryCanonicalize(input, out _));

    // URL Standard host parser: ASCII short-circuit, forbidden domain code points, "ends in a number".
    [Theory]
    [InlineData("Foo-.Example.COM", "foo-.example.com")]
    [InlineData("-foo.example.com", "-foo.example.com")]
    [InlineData("a_b.example.com", "a_b.example.com")]
    [InlineData("xn--bcher-kva.test", "xn--bcher-kva.test")]
    [InlineData("bücher.test", "xn--bcher-kva.test")]
    [InlineData("www.example.com.", "www.example.com.")]
    [InlineData("10.0.0.1.", "10.0.0.1")]
    [InlineData("0x7f.1", "127.0.0.1")]
    [InlineData("0x7F.0.0.1", "127.0.0.1")]
    [InlineData("0177.0.0.1", "127.0.0.1")]
    [InlineData("1", "0.0.0.1")]
    [InlineData("1.2.3", "1.2.0.3")]
    [InlineData("4294967295", "255.255.255.255")]
    [InlineData("0x", "0.0.0.0")]
    [InlineData("１２７.0.0.1", "127.0.0.1")]
    [InlineData("[::1]", "::1")]
    [InlineData("::ffff:1.2.3.4", "::ffff:1.2.3.4")]
    public void CanonicalizesHostsLikeTheUrlStandard(string input, string expected)
    {
        Assert.True(HostNames.TryCanonicalize(input, out var host));
        Assert.Equal(expected, host);
    }

    [Theory]
    [InlineData("1.2.3.256")][InlineData("example.123")][InlineData("1.2.3.4.5")][InlineData("256.1.1.1")]
    [InlineData("08.1.1.1")][InlineData("0xg.1")][InlineData("4294967296")][InlineData("99999999999999999999999")]
    [InlineData("a b.test")][InlineData("a#b.test")][InlineData("a^b.test")][InlineData("a|b.test")][InlineData("a\\b.test")]
    [InlineData("[::1")][InlineData("[fe80::1%1]")][InlineData("example.test:80")]
    public void RejectsHostsTheUrlStandardFails(string input) => Assert.False(HostNames.TryCanonicalize(input, out _));

    [Fact]
    public void LongLabelsAndEdgeHyphensAreAcceptedWithoutDnsLengthChecks()
    {
        var label = new string('a', 64);
        Assert.True(HostNames.TryGetHttpHost(new("http://" + label + ".com/"), out var host));
        Assert.Equal(label + ".com", host);
        Assert.Equal("foo-.github.io", SiteResolver.Default.GetSite(new("http://foo-.github.io/"))!.Host);
        Assert.True(new CookieStoreFixture().Accepts("h=1", "http://foo-.example.com/"));
    }

    [Theory]
    [InlineData("foo-.xn--bcher-kva.test", "foo-.xn--bcher-kva.test")]
    [InlineData("foo-.bücher.test", "foo-.xn--bcher-kva.test")]
    [InlineData("-x-.XN--BCHER-KVA.test.", "-x-.xn--bcher-kva.test.")]
    [InlineData("bücher。test", "xn--bcher-kva.test")]
    [InlineData("bücher．test｡", "xn--bcher-kva.test.")]
    public void AsciiLabelsNextToIdnLabelsKeepTheLenientRules(string input, string expected)
    {
        Assert.True(HostNames.TryCanonicalize(input, out var host));
        Assert.Equal(expected, host);
    }

    [Fact]
    public void IdnHostsWithLongOrHyphenEdgedAsciiLabelsAreFetchable()
    {
        var label = new string('a', 64);
        Assert.True(HostNames.TryGetHttpHost(new("http://" + label + ".xn--bcher-kva.test/"), out var host));
        Assert.Equal(label + ".xn--bcher-kva.test", host);
        Assert.Equal("xn--bcher-kva.test", SiteResolver.Default.GetSite(new("http://foo-.xn--bcher-kva.test/"))!.Host);
        // (System.Uri itself refuses the Unicode spelling of this host, so URLs carry the xn-- form.)
        Assert.True(new CookieStoreFixture().Accepts("h=1", "http://foo-.xn--bcher-kva.test/"));
        // IDN labels themselves are still validated.
        Assert.False(HostNames.TryCanonicalize("foo.xn--a.test", out _));
        Assert.False(HostNames.TryCanonicalize("a․b.test", out _));
    }

    [Fact]
    public void TrailingDotIpv4HostsAreIpAddresses()
    {
        var sites = SiteResolver.Default;
        Assert.Equal(sites.GetSite(new("http://10.0.0.1/")), sites.GetSite(new("http://10.0.0.1./")));
        Assert.NotEqual(sites.GetSite(new("http://10.0.0.1./")), sites.GetSite(new("http://20.0.0.1./")));
        Assert.Null(sites.GetSite(new("http://1.2.3.256/")));
        Assert.False(new CookieStoreFixture().Accepts("a=1;Domain=0.0.1.", "http://10.0.0.1./"));
        Assert.False(new CookieStoreFixture().Accepts("a=1;Domain=0.1.", "http://10.0.0.1./"));
        Assert.True(new CookieStoreFixture().Accepts("s=1;Secure", "http://127.0.0.1./"));
    }

    [Fact]
    public void UrisWhoseIdnHostThrowsAreNotHttpHosts()
    {
        foreach (var text in new[] { "http://a\u200Db.com/", "https://a\u200Cb.com/", "http://\u2488.com/" })
        {
            var url = new Uri(text);
            Assert.False(HostNames.TryGetHttpHost(url, out _));
            Assert.False(HostNames.IsSecure(url));
            Assert.Null(SiteResolver.Default.GetSite(url));
        }
    }

    [Fact]
    public void PublicSuffixLinesAreReadUpToTheFirstWhitespace()
    {
        var sites = new SiteResolver(new StringReader("com\n  foo.com extra words\nbar.com\t// comment\n// baz.com\n\t\n"));
        Assert.True(sites.IsPublicSuffix("foo.com"));
        Assert.True(sites.IsPublicSuffix("bar.com"));
        Assert.False(sites.IsPublicSuffix("baz.com"));
        Assert.Equal("a.foo.com", sites.GetRegistrableDomain("www.a.foo.com"));
    }

    private sealed class CookieStoreFixture
    {
        private readonly Broiler.Net.Cookies.CookieStore _store = new();
        public bool Accepts(string field, string url) =>
            _store.ReceiveResponseCookie(field, new(new(url), Broiler.Net.Cookies.SameSiteStatus.SameSite)).Accepted;
    }
}

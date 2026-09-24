using Broiler.Net.Http;

namespace Broiler.Net.Tests;

public sealed class HttpFetchHeadersTests
{
    [Theory]
    [InlineData("Accept-Charset")]
    [InlineData("Accept-Encoding")]
    [InlineData("Access-Control-Request-Headers")]
    [InlineData("Access-Control-Request-Method")]
    [InlineData("Connection")]
    [InlineData("Content-Length")]
    [InlineData("Cookie")]
    [InlineData("cookie2")]
    [InlineData("Date")]
    [InlineData("DNT")]
    [InlineData("Expect")]
    [InlineData("HOST")]
    [InlineData("Keep-Alive")]
    [InlineData("Origin")]
    [InlineData("Referer")]
    [InlineData("Set-Cookie")]
    [InlineData("TE")]
    [InlineData("Trailer")]
    [InlineData("Transfer-Encoding")]
    [InlineData("Upgrade")]
    [InlineData("Via")]
    [InlineData("Proxy-Authorization")]
    [InlineData("proxy-")]
    [InlineData("Sec-Fetch-Site")]
    [InlineData("sec-ch-ua")]
    public void ForbiddenRequestHeaderNames(string name) => Assert.True(FetchHeaders.IsForbiddenRequestHeader(name, "x"));

    [Theory]
    [InlineData("Accept")]
    [InlineData("Content-Type")]
    [InlineData("User-Agent")]
    [InlineData("Authorization")]
    [InlineData("Referrer-Policy")]
    [InlineData("X-Proxy")]
    [InlineData("Secure")]
    [InlineData("X-HTTP-Method")]
    public void OrdinaryRequestHeaderNames(string name) => Assert.False(FetchHeaders.IsForbiddenRequestHeader(name, "GET"));

    [Theory]
    [InlineData("X-HTTP-Method", "TRACE", true)]
    [InlineData("X-HTTP-Method-Override", "get, connect", true)]
    [InlineData("x-method-override", " \ttrack ", true)]
    [InlineData("X-HTTP-Method-Override", "GET, POST, PATCH", false)]
    [InlineData("X-HTTP-Method-Override", "\"TRACE\"", false)]
    [InlineData("X-HTTP-Method-Override", "\"a,TRACE\"", false)]
    [InlineData("X-HTTP-Method-Override", "\"a\",TRACE", true)]
    [InlineData("X-HTTP-Method-Override", "\"a\\\",TRACE\"", false)]
    [InlineData("X-HTTP-Method-Override", "TRACEX", false)]
    [InlineData("X-Other-Override", "TRACE", false)]
    public void MethodOverrideHeadersAreForbiddenForForbiddenMethods(string name, string value, bool forbidden) =>
        Assert.Equal(forbidden, FetchHeaders.IsForbiddenRequestHeader(name, value));

    [Theory]
    [InlineData("CONNECT", true)]
    [InlineData("connect", true)]
    [InlineData("Trace", true)]
    [InlineData("TRACK", true)]
    [InlineData("GET", false)]
    [InlineData("PATCH", false)]
    [InlineData("TRACE ", false)]
    public void ForbiddenMethods(string method, bool forbidden) => Assert.Equal(forbidden, FetchHeaders.IsForbiddenMethod(method));

    [Theory]
    [InlineData("get", "GET")]
    [InlineData("pOsT", "POST")]
    [InlineData("head", "HEAD")]
    [InlineData("put", "PUT")]
    [InlineData("Delete", "DELETE")]
    [InlineData("options", "OPTIONS")]
    [InlineData("patch", "patch")]
    [InlineData("PATCH", "PATCH")]
    [InlineData("custom", "custom")]
    public void NormalizesOnlyTheStandardMethods(string method, string expected) => Assert.Equal(expected, FetchHeaders.NormalizeMethod(method));

    [Theory]
    [InlineData("GET", true)]
    [InlineData("HEAD", true)]
    [InlineData("POST", true)]
    [InlineData("get", false)]
    [InlineData("PUT", false)]
    [InlineData("PATCH", false)]
    public void CorsSafelistedMethodsAreCaseSensitive(string method, bool safelisted) =>
        Assert.Equal(safelisted, FetchHeaders.IsCorsSafelistedMethod(method));

    [Theory]
    [InlineData("Accept", "text/html, */*;q=0.8", true)]
    [InlineData("accept", "tab\tis fine", true)]
    [InlineData("Accept", "café", true)]
    [InlineData("Accept", "a\"b", false)]
    [InlineData("Accept", "a(b", false)]
    [InlineData("Accept", "a:b", false)]
    [InlineData("Accept", "a{b}", false)]
    [InlineData("Accept", "x\u007f", false)]
    [InlineData("Accept", "x\u0001", false)]
    [InlineData("Accept", "Ā", false)]
    [InlineData("Accept-Language", "en-US,en;q=0.9", true)]
    [InlineData("Accept-Language", "en_US", false)]
    [InlineData("Content-Language", "de-DE, *", true)]
    [InlineData("Content-Language", "de/DE", false)]
    [InlineData("Content-Type", "text/plain", true)]
    [InlineData("Content-Type", "Text/Plain; charset=utf-8", true)]
    [InlineData("Content-Type", " multipart/form-data; boundary=x ", true)]
    [InlineData("Content-Type", "application/x-www-form-urlencoded", true)]
    [InlineData("Content-Type", "text/plain;garbage", true)]
    [InlineData("Content-Type", "application/json", false)]
    [InlineData("Content-Type", "text/plain; charset=\"utf-8\"", false)]
    [InlineData("Content-Type", "text /plain", false)]
    [InlineData("Content-Type", "text/plain garbage", false)]
    [InlineData("Content-Type", "text", false)]
    [InlineData("Content-Type", "/plain", false)]
    [InlineData("Content-Type", "text/", false)]
    [InlineData("Range", "bytes=0-", true)]
    [InlineData("Range", "bytes=10-20", true)]
    [InlineData("Range", "bytes=007-7", true)]
    [InlineData("Range", "bytes=99999999999999999999-100000000000000000000", true)]
    [InlineData("Range", "bytes=20-10", false)]
    [InlineData("Range", "bytes=100000000000000000000-99999999999999999999", false)]
    [InlineData("Range", "bytes=-5", false)]
    [InlineData("Range", "bytes=0-1,3-4", false)]
    [InlineData("Range", "bytes = 0-", false)]
    [InlineData("Range", "Bytes=0-", false)]
    [InlineData("Range", "bytes=", false)]
    [InlineData("Range", "bytes=a-", false)]
    [InlineData("X-Custom", "a", false)]
    [InlineData("Authorization", "Basic eA==", false)]
    [InlineData("User-Agent", "x", false)]
    public void CorsSafelistedRequestHeaders(string name, string value, bool safelisted) =>
        Assert.Equal(safelisted, FetchHeaders.IsCorsSafelistedRequestHeader(name, value));

    [Fact]
    public void SafelistedValuesAreLimitedTo128Characters()
    {
        Assert.True(FetchHeaders.IsCorsSafelistedRequestHeader("Accept", new string('a', 128)));
        Assert.False(FetchHeaders.IsCorsSafelistedRequestHeader("Accept", new string('a', 129)));
        Assert.False(FetchHeaders.IsCorsSafelistedRequestHeader("Content-Language", new string('a', 129)));
    }

    [Theory]
    [InlineData("Set-Cookie", true)]
    [InlineData("set-cookie2", true)]
    [InlineData("Set-Cookie3", false)]
    [InlineData("Cookie", false)]
    public void ForbiddenResponseHeaderNames(string name, bool forbidden) => Assert.Equal(forbidden, FetchHeaders.IsForbiddenResponseHeaderName(name));

    private static readonly KeyValuePair<string, string>[] Received =
    [
        new("Content-Type", "text/plain"), new("Set-Cookie", "a=1"), new("set-cookie2", "b=2"), new("X-Custom", "1"),
        new("Cache-Control", "no-cache"), new("Set-Cookie", "c=3"), new("X-Other", "2"), new("x-hidden", "3"),
        new("Content-Length", "2"), new("Expires", "0"), new("Last-Modified", "x"), new("Pragma", "no-cache"), new("Content-Language", "en"),
    ];

    private static string[] Names(IEnumerable<KeyValuePair<string, string>> headers) => headers.Select(h => h.Key).ToArray();

    private static KeyValuePair<string, string>[] With(params string[] expose) =>
        [.. Received, .. expose.Select(v => new KeyValuePair<string, string>("Access-Control-Expose-Headers", v))];

    [Fact]
    public void BasicFilteringDropsOnlySetCookieAndKeepsOrder() =>
        Assert.Equal(["Content-Type", "X-Custom", "Cache-Control", "X-Other", "x-hidden", "Content-Length", "Expires", "Last-Modified", "Pragma", "Content-Language"],
            Names(FetchHeaders.FilterResponseHeaders(Received, ResponseTainting.Basic, credentialed: true)));

    [Fact]
    public void OpaqueFilteringExposesNothing() =>
        Assert.Empty(FetchHeaders.FilterResponseHeaders(Received, ResponseTainting.Opaque, credentialed: false));

    [Fact]
    public void CorsFilteringKeepsSafelistedAndExposedNames()
    {
        string[] safelisted = ["Content-Type", "Cache-Control", "Content-Length", "Expires", "Last-Modified", "Pragma", "Content-Language"];
        Assert.Equal(safelisted, Names(FetchHeaders.FilterResponseHeaders(Received, ResponseTainting.Cors, false)));
        Assert.Equal(["Content-Type", "X-Custom", "Cache-Control", "X-Other", "Content-Length", "Expires", "Last-Modified", "Pragma", "Content-Language"],
            Names(FetchHeaders.FilterResponseHeaders(With("x-custom", " , X-OTHER"), ResponseTainting.Cors, true)));
        // Set-Cookie is never exposed, even when listed.
        Assert.Equal(safelisted, Names(FetchHeaders.FilterResponseHeaders(With("Set-Cookie, Set-Cookie2"), ResponseTainting.Cors, false)));
        // An invalid list exposes nothing extra.
        Assert.Equal(safelisted, Names(FetchHeaders.FilterResponseHeaders(With("X-Custom", "x other"), ResponseTainting.Cors, false)));
    }

    [Fact]
    public void OpaqueRedirectFilteringExposesNothing() =>
        Assert.Empty(FetchHeaders.FilterResponseHeaders(With("*"), ResponseTainting.OpaqueRedirect, credentialed: false));

    [Theory]
    [InlineData("X-Custom", true)]
    [InlineData("!#$%&'*+-.^_`|~09az", true)]
    [InlineData("", false)]
    [InlineData("Bad Name", false)]
    [InlineData("a:b", false)]
    [InlineData("café", false)]
    public void HeaderNamesAreTokens(string name, bool valid) => Assert.Equal(valid, FetchHeaders.IsHeaderName(name));

    [Theory]
    [InlineData("", true)]
    [InlineData("a b", true)]
    [InlineData("caféÿ", true)]
    [InlineData("a\tb", true)]
    [InlineData(" a", false)]
    [InlineData("a\t", false)]
    [InlineData("a\r\nCookie: x=1", false)]
    [InlineData("a\nb", false)]
    [InlineData("a\rb", false)]
    [InlineData("a\0b", false)]
    [InlineData("Ā", false)]
    public void HeaderValuesExcludeLineBreaksNulAndEdgeWhitespace(string value, bool valid) => Assert.Equal(valid, FetchHeaders.IsHeaderValue(value));

    [Fact]
    public void NormalizationStripsHttpWhitespaceAtTheEdgesOnly() =>
        Assert.Equal("a \r\n b", FetchHeaders.NormalizeHeaderValue("\r\n\t a \r\n b \t\n"));

    [Theory]
    [InlineData("Accept", "text/html", true)]
    [InlineData("accept-language", "de", true)]
    [InlineData("Content-Language", "en", true)]
    [InlineData("Content-Type", "text/plain", true)]
    [InlineData("Content-Type", "application/json", false)]
    [InlineData("Range", "bytes=0-", false)]
    [InlineData("X-Requested-With", "XMLHttpRequest", false)]
    [InlineData("Authorization", "Basic eA==", false)]
    public void NoCorsSafelistedRequestHeaders(string name, string value, bool safelisted) =>
        Assert.Equal(safelisted, FetchHeaders.IsNoCorsSafelistedRequestHeader(name, value));

    [Fact]
    public void ForbiddenNamesAreNeverCorsUnsafe()
    {
        KeyValuePair<string, string>[] headers =
        [
            new("Referer", "https://a.test/"), new("Sec-Fetch-Mode", "cors"), new("Accept-Encoding", "br"), new("DNT", "1"), new("X-Custom", "1"),
        ];
        Assert.Equal(["x-custom"], FetchHeaders.GetCorsUnsafeRequestHeaderNames(headers));
    }

    [Fact]
    public void TheNoCorsGuardKeepsSafelistedAndUserAgentHeaders()
    {
        var headers = new List<KeyValuePair<string, string>>
        {
            new("Accept", "text/html"), new("X-Requested-With", "x"), new("Content-Type", "application/json"), new("Referer", "https://a.test/"),
            new("User-Agent", "UA/1"), new("Range", "bytes=0-"), new("Cache-Control", "no-cache"), new("Authorization", "Basic eA=="),
            new("Accept-Language", new string('a', 100)), new("Accept-Language", new string('a', 100)),
        };
        FetchHeaders.RemoveNoCorsUnsafe(headers);
        // Repeated names are judged on their combined value, which is over the 128-character limit here.
        Assert.Equal(["Accept", "Referer", "User-Agent", "Range", "Cache-Control"], headers.Select(h => h.Key));
    }

    [Theory]
    [InlineData(null, CorsSetting.None)]
    [InlineData("", CorsSetting.Anonymous)]
    [InlineData("anonymous", CorsSetting.Anonymous)]
    [InlineData("invalid", CorsSetting.Anonymous)]
    [InlineData("use-credentials", CorsSetting.UseCredentials)]
    [InlineData("USE-Credentials", CorsSetting.UseCredentials)]
    [InlineData("use-credentialſ", CorsSetting.Anonymous)]
    public void CorsSettingsAttributeParsing(string? value, CorsSetting expected) => Assert.Equal(expected, CorsSettings.Parse(value));

    [Theory]
    [InlineData(RequestDestination.Image, CorsSetting.None, RequestMode.NoCors, CredentialsMode.Include)]
    [InlineData(RequestDestination.Image, CorsSetting.Anonymous, RequestMode.Cors, CredentialsMode.SameOrigin)]
    [InlineData(RequestDestination.Script, CorsSetting.UseCredentials, RequestMode.Cors, CredentialsMode.Include)]
    [InlineData(RequestDestination.Font, CorsSetting.None, RequestMode.Cors, CredentialsMode.SameOrigin)]
    [InlineData(RequestDestination.Font, CorsSetting.UseCredentials, RequestMode.Cors, CredentialsMode.Include)]
    public void SubresourcesMapTheCorsSetting(RequestDestination destination, CorsSetting setting, RequestMode mode, CredentialsMode credentials)
    {
        var context = RequestContext.Subresource(DocumentRequestContext.CreateTopLevel(new Uri("https://a.test/")), destination, setting);
        Assert.Equal((mode, credentials), (context.Mode, context.Credentials));
    }

    [Fact]
    public void CorsWildcardExposureRequiresUncredentialedRequest()
    {
        var all = Names(FetchHeaders.FilterResponseHeaders(With("*"), ResponseTainting.Cors, false));
        Assert.Contains("x-hidden", all);
        Assert.Contains("Access-Control-Expose-Headers", all);
        Assert.DoesNotContain("Set-Cookie", all);
        Assert.DoesNotContain("set-cookie2", all);
        var credentialed = Names(FetchHeaders.FilterResponseHeaders(With("*, X-Other"), ResponseTainting.Cors, true));
        Assert.Contains("X-Other", credentialed);
        Assert.DoesNotContain("x-hidden", credentialed);
        Assert.DoesNotContain("X-Custom", credentialed);
    }
}

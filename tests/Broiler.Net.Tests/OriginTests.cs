using Broiler.Net.Http;
using Broiler.Net.Sites;
using static Broiler.Net.Tests.TransportTest;

namespace Broiler.Net.Tests;

public sealed class OriginTests
{
    [Theory]
    [InlineData("https://a.test/x", "https://a.test")]
    [InlineData("https://a.test:443/", "https://a.test")]
    [InlineData("http://a.test:80/", "http://a.test")]
    [InlineData("http://a.test:8080/", "http://a.test:8080")]
    [InlineData("http://[::1]:8080/", "http://[::1]:8080")]
    [InlineData("https://B\u00dcCHER.test/", "https://xn--bcher-kva.test")]
    [InlineData("http://127.0.0.1./", "http://127.0.0.1")]
    [InlineData("blob:https://a.test/uuid", "https://a.test")]
    [InlineData("blob:https://a.test:8443/uuid?q#f", "https://a.test:8443")]
    public void TupleOriginsSerializeLikeTheOriginHeader(string url, string serialized) =>
        Assert.Equal(serialized, Origin.FromUrl(new Uri(url)).ToString());

    [Theory]
    [InlineData("blob:null/uuid")]
    [InlineData("blob:file:///c:/x")]
    [InlineData("blob:blob:https://a.test/uuid")]
    [InlineData("data:text/html,x")]
    [InlineData("about:blank")]
    [InlineData("file:///c:/x")]
    public void OtherUrlsHaveNewOpaqueOrigins(string url)
    {
        var origin = Origin.FromUrl(new Uri(url));
        Assert.True(origin.IsOpaque);
        Assert.False(origin.IsSameOrigin(Origin.FromUrl(new Uri(url))));
        Assert.Equal("null", origin.ToString());
    }

    [Fact]
    public void BlobUrlsAreSameOriginWithTheirCreator()
    {
        Assert.True(Origin.FromUrl(new Uri("blob:https://a.test/uuid")).IsSameOrigin(Origin.FromUrl(new Uri("https://a.test/page"))));
        Assert.Throws<ArgumentNullException>(() => Origin.FromUrl(null!));
        Assert.True(Origin.FromUrl(new Uri("/relative", UriKind.Relative)).IsOpaque);
    }

    [Fact]
    public async Task BlobFramesFetchTheirCreatorSameOrigin()
    {
        var handler = new ScriptedHandler();
        using var session = Session(handler);
        Seed(session.Cookies, "https://a.test/", "a=1");
        var frame = Top("https://a.test/").CreateChild(new Uri("blob:https://a.test/uuid"));
        using var response = await session.SendAsync(Get("https://a.test/data"), RequestContext.Fetch(frame));
        Assert.Equal(ResponseTainting.Basic, response.Tainting);
        Assert.Equal("a=1", handler.Hops[0].Cookie());
    }
}

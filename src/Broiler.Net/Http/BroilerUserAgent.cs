using System.Net;

namespace Broiler.Net.Http;

/// <summary>
/// The product token sent as User-Agent and reported by navigator.userAgent: one string, so the network
/// and the page are told the same thing. Moved here from Broiler.Layout.Net, which keeps an identical
/// copy until its next release removes it.
/// </summary>
public static class BroilerUserAgent
{
    public const string Value = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Broiler/1.0";

    /// <summary>Makes <see cref="Value"/> the client's default User-Agent; a request's own value wins.</summary>
    public static HttpClient Apply(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Value);
        return client;
    }
}

/// <summary>
/// The HTTP version Broiler requests and the matching PerformanceResourceTiming.nextHopProtocol value.
/// Moved here from Broiler.Layout.Net.
/// </summary>
public static class BroilerHttpProtocol
{
    public static readonly Version Version = HttpVersion.Version11;
    public const HttpVersionPolicy VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    public const string NextHopProtocol = "http/1.1";

    public static HttpClient Apply(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.DefaultRequestVersion = Version;
        client.DefaultVersionPolicy = VersionPolicy;
        return client;
    }
}

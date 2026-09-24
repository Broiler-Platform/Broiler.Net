using System.Text.RegularExpressions;
using Broiler.Net.Sites;

namespace Broiler.Net.Tests;

public sealed class PublicSuffixFixtureTests
{
    public static IEnumerable<object?[]> Cases()
    {
        foreach (var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "test_psl.txt")))
        {
            var match = Regex.Match(line, "^checkPublicSuffix\\((null|'[^']*'), (null|'[^']*')\\);$");
            if (!match.Success) continue;
            var host = Read(match.Groups[1].Value);
            var expected = Read(match.Groups[2].Value);
            // Upstream expresses IDN results in Unicode; this API always returns canonical ASCII.
            if (expected is not null) { Assert.True(HostNames.TryCanonicalize(expected, out var ascii)); expected = ascii; }
            yield return [host, expected];
        }
        static string? Read(string token) => token == "null" ? null : token[1..^1];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesPinnedUpstreamFixture(string? host, string? expected) =>
        Assert.Equal(expected, SiteResolver.Default.GetRegistrableDomain(host!));
}

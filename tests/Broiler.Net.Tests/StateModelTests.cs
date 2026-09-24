using System.Security.Cryptography;
using System.Text.Json;
using Broiler.Net.Cookies;
using Broiler.Net.Sites;
using static Broiler.Net.Tests.CookieStoreTests;

namespace Broiler.Net.Tests;

public sealed class StateModelTests
{
    [Fact]
    public void SeededSessionAndPersistentOperationsMatchIndependentModel()
    {
        var clock = new TestClock(); var store = new CookieStore(clock: clock);
        var random = new Random(20260924);
        var model = new Dictionary<string, (string Value, long Order, DateTimeOffset? Expiry)>();
        long order = 0;
        for (var i = 0; i < 2000; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(random.Next(0, 3)));
            var now = clock.GetUtcNow();
            foreach (var key in model.Where(x => x.Value.Expiry <= now).Select(x => x.Key).ToArray()) model.Remove(key);
            var name = "n" + random.Next(32); var value = i.ToString();
            var operation = random.Next(5);
            if (operation == 0)
            {
                store.ReceiveResponseCookie(name + "=;Path=/;Max-Age=0", Request()); model.Remove(name);
            }
            else if (operation is 1 or 2)
            {
                var age = operation == 1 ? random.Next(1, 20) : (int?)null;
                var expiry = age.HasValue ? now.AddSeconds(age.Value) : (DateTimeOffset?)null;
                var creation = model.TryGetValue(name, out var previous) ? previous.Order : ++order;
                model[name] = (value, creation, expiry);
                store.ReceiveResponseCookie($"{name}={value};Path=/" + (age.HasValue ? ";Max-Age=" + age : ""), Request());
            }
            else if (operation == 3)
            {
                Assert.False(store.ReceiveResponseCookie(name + "=bad\r\n;Path=/", Request()).Accepted);
            }
            else if (random.Next(10) == 0) { store.Clear(); model.Clear(); }
            var expected = string.Join("; ", model.OrderBy(x => x.Value.Order).Select(x => x.Key + "=" + x.Value.Value));
            Assert.Equal(expected, store.BuildRequestHeader(Request()));
            Assert.Equal(model.Count, store.Snapshot().Count);
        }
    }

    [Fact]
    public void EmbeddedListAndUpstreamFixtureMatchRecordedHashes()
    {
        using var metadata = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "public-suffix-list.json")));
        using var list = typeof(SiteResolver).Assembly.GetManifestResourceStream("Broiler.Net.public_suffix_list.dat")!;
        Assert.Equal(metadata.RootElement.GetProperty("sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(list)));
        using var fixture = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "test_psl.txt"));
        Assert.Equal(metadata.RootElement.GetProperty("fixtureSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(fixture)));
        Assert.Matches("^[0-9a-f]{40}$", metadata.RootElement.GetProperty("revision").GetString()!);
        // Guards the case regex against fixture format drift; the updater records the upstream count.
        Assert.Equal(metadata.RootElement.GetProperty("fixtureCaseCount").GetInt32(), PublicSuffixFixtureTests.Cases().Count());
    }
}

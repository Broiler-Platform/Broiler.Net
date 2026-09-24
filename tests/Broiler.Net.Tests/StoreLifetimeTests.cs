using System.Collections.Concurrent;
using Broiler.Net.Cookies;
using Broiler.Net.Sites;
using static Broiler.Net.Tests.CookieStoreTests;

namespace Broiler.Net.Tests;

public sealed class StoreLifetimeTests
{
    [Fact]
    public void SiteQuotaEvictsInsecureBeforeSecureThenLeastRecentlyUsed()
    {
        var clock = new TestClock();
        var store = new CookieStore(clock: clock, options: new() { MaximumCookiesPerSite = 2 });
        store.ReceiveResponseCookie("secure=old;Secure;Path=/", Request());
        clock.Advance(TimeSpan.FromSeconds(1));
        store.ReceiveResponseCookie("insecure=new;Path=/", Request());
        clock.Advance(TimeSpan.FromSeconds(1));
        store.ReceiveResponseCookie("secure2=newest;Secure;Path=/", Request());
        Assert.Equal("secure=old; secure2=newest", store.BuildRequestHeader(Request()));

        // An insecure newcomer cannot displace either secure record in the same full site.
        Assert.Equal(CookieDecision.Evicted, store.ReceiveResponseCookie("insecure2=x;Path=/", Request()).Decision);
        Assert.Equal(2, store.Snapshot().Count);
    }

    [Fact]
    public void GlobalQuotaUsesLastAccessAndExpiredCookiesGoFirst()
    {
        var clock = new TestClock();
        var store = new CookieStore(clock: clock, options: new() { MaximumCookies = 2 });
        store.ReceiveResponseCookie("a=1", Request("https://a.test"));
        clock.Advance(TimeSpan.FromSeconds(1));
        store.ReceiveResponseCookie("b=2", Request("https://b.test"));
        clock.Advance(TimeSpan.FromSeconds(1));
        store.BuildRequestHeader(Request("https://a.test"));
        store.ReceiveResponseCookie("c=3;Max-Age=1", Request("https://c.test"));
        Assert.Equal(new[] { "a", "c" }, store.Snapshot().Select(c => c.Name));
        clock.Advance(TimeSpan.FromSeconds(1));
        store.ReceiveResponseCookie("d=4", Request("https://d.test"));
        Assert.Equal(new[] { "a", "d" }, store.Snapshot().Select(c => c.Name));
    }

    [Fact]
    public void GlobalQuotaIsLeastRecentlyUsedWhetherSecureOrNot()
    {
        // 6265bis-22 5.7 priority 4 ("all cookies"): the earliest last-access time goes first.
        var clock = new TestClock();
        var store = new CookieStore(clock: clock, options: new() { MaximumCookies = 2 });
        store.ReceiveResponseCookie("old=1;Secure", Request("https://a.test"));
        clock.Advance(TimeSpan.FromSeconds(1));
        store.ReceiveResponseCookie("plain=2", Request("https://b.test"));
        clock.Advance(TimeSpan.FromSeconds(1));
        store.ReceiveResponseCookie("new=3;Secure", Request("https://c.test"));
        Assert.Equal(new[] { "plain", "new" }, store.Snapshot().Select(c => c.Name));
    }

    [Fact]
    public void AStoreFullOfSecureCookiesStillAcceptsFreshInsecureOnes()
    {
        var clock = new TestClock();
        var store = new CookieStore(clock: clock, options: new() { MaximumCookies = 40, MaximumCookiesPerSite = 10 });
        store.ReceiveResponseCookie("session=victim", Request("http://shop.test"));
        for (var i = 0; i < 40; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            store.ReceiveResponseCookie($"x{i}=1;Secure;SameSite=None", Request($"https://a{i % 4}.evil.test{i / 4}"));
        }
        clock.Advance(TimeSpan.FromDays(3));
        // The newest cookie displaces the least recently used flood cookie instead of being evicted itself.
        Assert.Equal(CookieDecision.Stored, store.ReceiveResponseCookie("session=new", Request("http://shop.test")).Decision);
        Assert.Equal(CookieDecision.Stored, store.ReceiveResponseCookie("prefs=2", Request("https://news.test")).Decision);
        Assert.Equal("session=new", store.BuildRequestHeader(Request("http://shop.test")));
        Assert.Equal(40, store.Snapshot().Count);
        Assert.DoesNotContain(store.Snapshot(), c => c.Name is "x0" or "x1");
    }

    [Fact]
    public void EmbeddedSitesHaveSeparatePartitionBudgetsAndSubdomainsShareOne()
    {
        var clock = new TestClock();
        var store = new CookieStore(clock: clock, options: new() { MaximumCookiesPerSite = 4 });
        var news = Partition("https://news.test"); var other = Partition("https://other.test");
        store.ReceiveResponseCookie("global=1;Secure;SameSite=None", Request("https://chat.test"));
        store.ReceiveResponseCookie("sid=1;Secure;SameSite=None;Partitioned", Request("https://chat.test", SameSiteStatus.CrossSite, partition: news));
        store.ReceiveResponseCookie("sid=2;Secure;SameSite=None;Partitioned", Request("https://chat.test", SameSiteStatus.CrossSite, partition: other));
        for (var i = 0; i < 12; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            store.ReceiveResponseCookie($"t{i}=x;Secure;SameSite=None;Partitioned;Domain=tracker.test",
                Request($"https://s{i % 3}.tracker.test", SameSiteStatus.CrossSite, partition: news));
        }

        // The tracker cannot evict (or learn about) chat.test's partitioned cookie, and spreading its
        // cookies over subdomains does not multiply its budget.
        Assert.Equal("global=1; sid=1", store.BuildRequestHeader(Request("https://chat.test", SameSiteStatus.CrossSite, partition: news)));
        Assert.Equal("global=1; sid=2", store.BuildRequestHeader(Request("https://chat.test", SameSiteStatus.CrossSite, partition: other)));
        Assert.Equal("t8=x; t9=x; t10=x; t11=x",
            store.BuildRequestHeader(Request("https://s0.tracker.test", SameSiteStatus.CrossSite, partition: news)));
        Assert.Equal(4, store.Snapshot().Count(c => c.Domain == "tracker.test"));
    }

    [Fact]
    public void PartitionedByteBudgetIsPerEmbeddedSite()
    {
        var clock = new TestClock();
        var store = new CookieStore(clock: clock);
        var news = Partition("https://news.test");
        var value = new string('v', 1020);
        for (var i = 0; i < 11; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            store.ReceiveResponseCookie($"p{i:D2}={value};Secure;SameSite=None;Partitioned", Request("https://a.tracker.test", SameSiteStatus.CrossSite, partition: news));
            store.ReceiveResponseCookie($"q{i:D2}={value};Secure;SameSite=None;Partitioned", Request("https://b.tracker.test", SameSiteStatus.CrossSite, partition: news));
            store.ReceiveResponseCookie($"u{i:D2}={value}", Request("https://a.tracker.test"));
            store.ReceiveResponseCookie($"o{i:D2}={value};Secure;SameSite=None;Partitioned", Request("https://other.test", SameSiteStatus.CrossSite, partition: news));
        }
        var snapshot = store.Snapshot();
        // 22 cookies of 1024 octets across two subdomains share the 10240-octet budget of tracker.test.
        Assert.Equal(Enumerable.Range(6, 5).SelectMany(i => new[] { $"p{i:D2}", $"q{i:D2}" }),
            snapshot.Where(c => c.PartitionKey is not null && c.Domain.EndsWith("tracker.test", StringComparison.Ordinal)).Select(c => c.Name));
        Assert.Equal(11, snapshot.Count(c => c.Name.StartsWith('u')));
        Assert.Equal(10, snapshot.Count(c => c.Name.StartsWith('o')));
    }

    [Fact]
    public void SubdomainSprayingCannotExceedTheSiteBudgetOrFlushOtherSites()
    {
        var clock = new TestClock();
        var store = new CookieStore(clock: clock, options: new() { MaximumCookiesPerSite = 3, MaximumCookies = 8 });
        store.ReceiveResponseCookie("session=secret;Secure;HttpOnly", Request("https://bank.test"));
        for (var i = 0; i < 40; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            store.ReceiveResponseCookie($"x{i}=1", Request($"http://s{i}.evil.test"));
            store.ReceiveResponseCookie($"y{i}=1;Domain=evil.test", Request($"http://s{i}.evil.test"));
        }
        Assert.Equal("session=secret", store.BuildRequestHeader(Request("https://bank.test")));
        Assert.Equal(3, store.Snapshot().Count(c => c.Domain.EndsWith("evil.test", StringComparison.Ordinal)));
    }

    [Fact]
    public void SprayingInsecureSubdomainCookiesCannotEvictASecureCookieToOverlayIt()
    {
        var clock = new TestClock();
        var store = new CookieStore(clock: clock, options: new() { MaximumCookiesPerSite = 3, MaximumCookies = 5 });
        store.ReceiveResponseCookie("sid=good;Secure;Domain=victim.test;Path=/", Request("https://www.victim.test"));
        for (var i = 0; i < 20; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            store.ReceiveResponseCookie($"x{i}=1;Path=/", Request($"http://x{i}.victim.test"));
        }
        Assert.Equal(CookieDecision.RejectedSecureOverlay, store.ReceiveResponseCookie("sid=fixated;Path=/", Request("http://www.victim.test")).Decision);
        Assert.Equal("sid=good", store.BuildRequestHeader(Request("https://www.victim.test/")));
    }

    [Fact]
    public void EqualTimeEvictionIsDeterministic()
    {
        var store = new CookieStore(clock: new TestClock(), options: new() { MaximumCookiesPerSite = 2 });
        store.ReceiveResponseCookies(["first=1", "second=2", "third=3"], Request());
        Assert.Equal("second=2; third=3", store.BuildRequestHeader(Request()));
    }

    [Fact]
    public void LargeStoreAcrossManySitesRetrievesOnlyMatchingCookies()
    {
        var clock = new TestClock();
        var store = new CookieStore(clock: clock);
        for (var site = 0; site < 60; site++)
        {
            var url = $"https://www.site{site}.test/app/page";
            for (var i = 0; i < 100; i++)
                store.ReceiveResponseCookie($"c{i}={site}" + (i % 4) switch
                {
                    0 => $";Domain=site{site}.test;Path=/",
                    1 => ";Path=/app",
                    2 => ";Secure;Max-Age=60",
                    _ => "",
                }, Request(url));
            clock.Advance(TimeSpan.FromMilliseconds(1));
        }
        Assert.Equal(6000, store.Snapshot().Count);
        static string Expected(int site, Func<int, bool> include) => string.Join("; ",
            Enumerable.Range(0, 100).Where(i => i % 4 != 0 && include(i)).Concat(Enumerable.Range(0, 100).Where(i => i % 4 == 0 && include(i)))
                .Select(i => $"c{i}={site}"));

        Assert.Equal(Expected(7, _ => true), store.BuildRequestHeader(Request("https://www.site7.test/app/page")));
        Assert.Equal(Expected(7, i => i % 4 == 0), store.BuildRequestHeader(Request("https://other.site7.test/app/page")));
        Assert.Equal(Expected(7, i => i % 4 != 2), store.BuildRequestHeader(Request("http://www.site7.test/app/page")));
        Assert.Equal("", store.BuildRequestHeader(Request("https://www.site7.example/app/page")));

        // Global overflow evicts the least recently used insecure cookie: site0's first one.
        var revision = store.Revision;
        Assert.Equal(CookieDecision.Stored, store.ReceiveResponseCookie("extra=1", Request("https://extra.test")).Decision);
        Assert.Equal(revision + 1, store.Revision);
        Assert.Equal(6000, store.Snapshot().Count);
        Assert.Equal(Expected(0, i => i != 0), store.BuildRequestHeader(Request("https://www.site0.test/app/page")));

        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.Equal(Expected(7, i => i % 4 != 2), store.BuildRequestHeader(Request("https://www.site7.test/app/page")));
        Assert.Equal(4500, store.Snapshot().Count);
        Assert.Equal(revision + 1, store.Revision);
        Assert.Equal(1500, store.PruneExpired());
        Assert.Equal(0, store.PruneExpired());
    }

    [Fact]
    public void SnapshotsRemainUnchangedAfterMutationAndCannotBeWrittenThrough()
    {
        var store = new CookieStore();
        store.ReceiveResponseCookie("a=1", Request());
        var snapshot = store.Snapshot();
        store.ReceiveResponseCookie("a=2", Request());
        Assert.Equal("1", snapshot[0].Value);
        Assert.Throws<NotSupportedException>(() => ((IList<CookieRecord>)snapshot).Clear());
        Assert.Equal(1, store.Clear());
        Assert.Equal(0, store.Clear());
        Assert.Empty(store.Snapshot());
        Assert.Single(snapshot);
    }

    [Fact]
    public void SnapshotReportsTheRevisionItReflects()
    {
        var clock = new TestClock(); var store = new CookieStore(clock: clock);
        Assert.Equal(0, store.Revision);
        store.ReceiveResponseCookie("a=1;Max-Age=1", Request());
        store.ReceiveResponseCookie("b=2", Request());
        Assert.Equal(2, store.Snapshot(out var revision).Count);
        Assert.Equal(2, revision);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal("b=2", store.BuildRequestHeader(Request()));
        Assert.Equal("b", Assert.Single(store.Snapshot(out revision)).Name);
        Assert.Equal(2, revision);
        store.PruneExpired();
        store.Snapshot(out revision);
        Assert.Equal(3, revision);
    }

    [Fact]
    public async Task NotificationsAreOutsideLockAndSupportReentrantMutations()
    {
        var store = new CookieStore();
        var batches = new ConcurrentQueue<CookieChangeBatch>();
        Task? read = null;
        store.Changed += (_, batch) =>
        {
            batches.Enqueue(batch);
            read = Task.Run(() => store.Snapshot());
            // Intentionally wait inside the synchronous observer: an async observer would return,
            // release an incorrectly held store lock, and conceal precisely the bug under test.
#pragma warning disable xUnit1031
            Assert.True(read.Wait(TimeSpan.FromSeconds(5)), "A notification must not hold the store lock.");
#pragma warning restore xUnit1031
            if (batch.Changes[0].Current?.Name == "first") store.ReceiveResponseCookie("second=2", Request());
        };
        store.ReceiveResponseCookie("first=1", Request());
        await read!;
        Assert.Equal(new long[] { 1, 2 }, batches.Select(b => b.Revision));
        Assert.Equal("first=1; second=2", store.BuildRequestHeader(Request()));
    }

    [Fact]
    public void ObserverErrorsDoNotRollbackOrPreventOtherObservers()
    {
        var store = new CookieStore();
        var observed = false;
        store.Changed += (_, _) => throw new InvalidOperationException("observer failure");
        store.Changed += (_, _) => observed = true;
        var error = Assert.Throws<CookieObserverException>(() => store.ReceiveResponseCookie("a=1", Request()));
        Assert.IsType<InvalidOperationException>(Assert.Single(error.InnerExceptions));
        Assert.True(observed);
        Assert.Equal("a=1", store.BuildRequestHeader(Request()));
    }

    [Fact]
    public void ReadsSkipExpiredCookiesWithoutPublishingOrThrowingObserverErrors()
    {
        var clock = new TestClock(); var store = new CookieStore(clock: clock);
        store.ReceiveResponseCookie("short=1;Max-Age=1", Request());
        store.ReceiveResponseCookie("session=2", Request());
        var batches = new List<CookieChangeBatch>();
        store.Changed += (_, batch) => { batches.Add(batch); throw new IOException("disk full"); };
        clock.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal("session=2", store.BuildRequestHeader(Request()));
        Assert.Equal("session=2", store.GetDocumentCookies(Document()));
        Assert.Equal("session", Assert.Single(store.Snapshot()).Name);
        Assert.Empty(batches);

        // The next mutation removes and publishes the expired record, then reports the observer failure.
        Assert.Throws<CookieObserverException>(() => store.PruneExpired());
        Assert.Equal(CookieChangeKind.Expired, Assert.Single(Assert.Single(batches).Changes).Kind);
        Assert.Equal(0, store.PruneExpired());
    }

    [Fact]
    public void EveryResponseFieldIsStoredBeforeObserverFailuresAreThrown()
    {
        var store = new CookieStore();
        store.Changed += (_, batch) => throw new IOException((batch.Changes[0].Current ?? batch.Changes[0].Previous)!.Name);
        var error = Assert.Throws<CookieObserverException>(() => store.ReceiveResponseCookies(["first=1", "second=2", "bad\u0001", "third=3"], Request()));
        Assert.Equal(new[] { "first", "second", "third" }, error.InnerExceptions.Select(e => e.Message));
        Assert.Equal("first=1; second=2; third=3", store.BuildRequestHeader(Request()));
        Assert.Throws<CookieObserverException>(() => store.SetDocumentCookie("doc=1", Document()));
        Assert.Throws<CookieObserverException>(() => store.Clear());
        Assert.Empty(store.Snapshot());
    }

    [Fact]
    public void ExpiryReplacementDeletionAndClearHaveOrderedRevisions()
    {
        var clock = new TestClock(); var store = new CookieStore(clock: clock);
        var batches = new List<CookieChangeBatch>(); store.Changed += (_, batch) => batches.Add(batch);
        store.ReceiveResponseCookie("a=1", Request());
        store.ReceiveResponseCookie("a=2;Max-Age=1", Request());
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, store.PruneExpired());
        Assert.Equal(0, store.PruneExpired());
        store.ReceiveResponseCookie("b=1", Request());
        store.ReceiveResponseCookie("b=;Max-Age=0", Request());
        store.ReceiveResponseCookie("c=1", Request());
        store.Clear();
        Assert.Equal(Enumerable.Range(1, 7).Select(i => (long)i), batches.Select(b => b.Revision));
        Assert.Equal(new[] { CookieChangeKind.Inserted, CookieChangeKind.Replaced, CookieChangeKind.Expired,
            CookieChangeKind.Inserted, CookieChangeKind.Deleted, CookieChangeKind.Inserted, CookieChangeKind.Cleared },
            batches.SelectMany(b => b.Changes).Select(c => c.Kind));
    }

    [Fact]
    public void ClearReportsExpiredRecordsSeparately()
    {
        var clock = new TestClock(); var store = new CookieStore(clock: clock);
        var batches = new List<CookieChangeBatch>(); store.Changed += (_, batch) => batches.Add(batch);
        store.ReceiveResponseCookie("gone=1;Max-Age=1", Request());
        store.ReceiveResponseCookie("kept=1", Request());
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, store.Clear());
        Assert.Equal(new[] { CookieChangeKind.Expired, CookieChangeKind.Cleared }, batches[^1].Changes.Select(c => c.Kind));
    }

    [Fact]
    public void ConcurrentReplacementHasOneIdentityAndNotificationsAccountForAllWrites()
    {
        var store = new CookieStore(clock: new TestClock());
        var revisions = new ConcurrentBag<long>();
        store.Changed += (_, batch) => revisions.Add(batch.Revision);
        Parallel.For(0, 2000, i =>
        {
            Assert.True(store.ReceiveResponseCookie($"same={i}", Request()).Accepted);
            Assert.StartsWith("same=", store.BuildRequestHeader(Request()));
            Assert.Single(store.Snapshot());
        });
        Assert.Single(store.Snapshot());
        Assert.Equal(Enumerable.Range(1, 2000).Select(i => (long)i), revisions.Order());
    }

    [Fact]
    public void ConcurrentDomainsPartitionsClearAndExpiryStayBounded()
    {
        var clock = new TestClock();
        var store = new CookieStore(clock: clock, options: new() { MaximumCookies = 32, MaximumCookiesPerSite = 8 });
        Parallel.For(0, 1000, i =>
        {
            var partition = Partition($"https://top{i % 3}.test");
            var context = Request($"https://www{i % 2}.host{i % 5}.test/a", partition: partition);
            store.ReceiveResponseCookie($"id{i % 17}=x;Secure;Partitioned;Max-Age=2", context);
            if (i % 13 == 0) clock.Advance(TimeSpan.FromSeconds(1));
            if (i % 31 == 0) store.Clear();
            store.BuildRequestHeader(context);
            var snapshot = store.Snapshot();
            Assert.InRange(snapshot.Count, 0, 32);
            Assert.Equal(snapshot.Count, snapshot.Select(c => c.Key).Distinct().Count());
            Assert.All(snapshot.GroupBy(c => (SiteResolver.Default.GetRegistrableDomain(c.Domain), c.PartitionKey)), g => Assert.InRange(g.Count(), 1, 8));
        });
        clock.Advance(TimeSpan.FromSeconds(3));
        Assert.Empty(store.Snapshot());
    }

    [Fact]
    public void ChangedPublicSuffixPolicyPreventsRetrievalOfFormerDomainCookie()
    {
        var sites = new ReplaceableSites(); var store = new CookieStore(new CookiePolicy(sites));
        store.ReceiveResponseCookie("domain=1;Domain=example.test", Request());
        store.ReceiveResponseCookie("host=2", Request());
        sites.Current = new SiteResolver(new StringReader("test\nexample.test"));
        Assert.Equal("host=2", store.BuildRequestHeader(Request()));
    }

    private sealed class ReplaceableSites : ISiteResolver
    {
        public ISiteResolver Current { get; set; } = SiteResolver.Default;
        public SchemefulSite? GetSite(Uri url) => Current.GetSite(url);
        public bool IsPublicSuffix(string host) => Current.IsPublicSuffix(host);
        public string? GetRegistrableDomain(string host) => Current.GetRegistrableDomain(host);
    }

    [Fact]
    public void InvalidBudgetsFailAtConstruction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CookieStore(options: new() { MaximumCookies = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CookieStore(options: new() { MaximumCookiesPerSite = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CookieStore(options: new() { MaximumPartitionedBytesPerSite = 0 }));
    }
}

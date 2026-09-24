using System.Text;
using Broiler.Net.Sites;

namespace Broiler.Net.Cookies;

/// <summary>
/// Profile-owned, bounded in-memory store. HTTP/header strings preserve bytes using Latin-1;
/// document access uses UTF-8. Host code must apply Fetch credentials and user settings before
/// calling this service. This type does not derive browser navigation/frame context.
/// </summary>
public sealed class CookieStore : ICookieService
{
    private readonly object _sync = new();
    private readonly Dictionary<CookieKey, Entry> _cookies = [];
    private readonly Dictionary<string, HashSet<Entry>> _byDomain = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<Entry>> _secureByName = new(StringComparer.Ordinal);
    private readonly Dictionary<BucketKey, Bucket> _buckets = [];
    private readonly CookiePolicy _policy;
    private readonly TimeProvider _clock;
    private readonly CookieStoreOptions _options;
    private long _creationOrder, _revision;
    private DateTimeOffset _nextExpiry = DateTimeOffset.MaxValue;

    /// <summary>
    /// Committed mutations only, delivered outside the lock. Concurrent batches may arrive out of
    /// order: use Revision. Reads never publish. Mutating calls throw <see cref="CookieObserverException"/>
    /// after all of their work when observers fail (all observers are called). Host observers must not
    /// forward these privileged records directly to JavaScript.
    /// </summary>
    public event EventHandler<CookieChangeBatch>? Changed;

    public CookieStore(CookiePolicy? policy = null, TimeProvider? clock = null, CookieStoreOptions? options = null)
    {
        _policy = policy ?? new CookiePolicy();
        _clock = clock ?? TimeProvider.System;
        _options = options ?? new CookieStoreOptions();
        _options.Validate();
    }

    /// <summary>The policy's site resolver, which decides Domain and public-suffix acceptance and site buckets.</summary>
    public ISiteResolver Sites => _policy.Sites;

    /// <summary>Revision of the last committed change batch (0 before any change).</summary>
    public long Revision { get { lock (_sync) return _revision; } }

    public CookieResult ReceiveResponseCookie(string header, CookieRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        List<Exception>? errors = null;
        var result = ReceiveField(header, context, ref errors);
        ThrowIfObserversFailed(errors);
        return result;
    }

    /// <summary>
    /// Each supplied string is one field value; commas are preserved. Every field is processed before
    /// observer failures are thrown as one <see cref="CookieObserverException"/>.
    /// </summary>
    public IReadOnlyList<CookieResult> ReceiveResponseCookies(IEnumerable<string> headers, CookieRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(context);
        List<Exception>? errors = null;
        var results = new List<CookieResult>();
        foreach (var header in headers) results.Add(ReceiveField(header, context, ref errors));
        ThrowIfObserversFailed(errors);
        return results.AsReadOnly();
    }

    public CookieResult SetDocumentCookie(string assignment, CookieDocumentContext context)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(context);
        if (assignment.Length > CookieParser.MaximumFieldBytes || Encoding.UTF8.GetByteCount(assignment) > CookieParser.MaximumFieldBytes)
            return new(CookieDecision.RejectedSize);
        List<Exception>? errors = null;
        var result = Receive(CookieParser.Parse(Encoding.UTF8.GetBytes(assignment), _clock.GetUtcNow()),
            new(context.Url, context.SameSite, PartitionKey: context.PartitionKey), document: true, ref errors);
        ThrowIfObserversFailed(errors);
        return result;
    }

    private CookieResult ReceiveField(string header, CookieRequestContext context, ref List<Exception>? errors) =>
        Receive(CookieParser.Parse(header, _clock.GetUtcNow()), context, document: false, ref errors);

    private CookieResult Receive(CookieParseResult parse, CookieRequestContext context, bool document, ref List<Exception>? errors)
    {
        if (!parse.Success) return new(parse.Rejection!.Value);
        var parsed = parse.Cookie!;
        var rejection = _policy.Accept(parsed, context, document, out var key);
        if (rejection.HasValue) return new(rejection.Value);
        var overlayCheck = !parsed.Secure && !HostNames.IsSecure(context.Url);
        var changes = new List<CookieChange>();
        CookieResult result;
        CookieChangeBatch? batch;
        lock (_sync)
        {
            // Expiry is calculated at receipt, but a thread waiting for the lock must not use
            // that earlier instant to retain expired records or move last-access time backward.
            var now = _clock.GetUtcNow();
            PurgeExpired(now, changes);
            result = Store(now);
            batch = Batch(changes);
        }
        Publish(batch, ref errors);
        return result;

        CookieResult Store(DateTimeOffset now)
        {
            // Like Chromium, only secure cookies in the new cookie's partition block an insecure overlay.
            if (overlayCheck && _secureByName.TryGetValue(parsed.Name, out var named) &&
                named.Any(e => e.Record.PartitionKey == key!.PartitionKey &&
                    (HostNames.DomainMatches(e.Record.Domain, key.Domain) || HostNames.DomainMatches(key.Domain, e.Record.Domain)) &&
                    CookiePolicy.PathMatches(key.Path, e.Record.Path)))
                return new(CookieDecision.RejectedSecureOverlay);

            _cookies.TryGetValue(key!, out var old);
            if (document && old is { Record.HttpOnly: true }) return new(CookieDecision.RejectedHttpOnly);
            if (parsed.Expires <= now)
            {
                if (old is not null) Remove(old, CookieChangeKind.Deleted, changes);
                return new(CookieDecision.Deleted);
            }
            var cookie = new CookieRecord(key!, parsed, now, old?.Record.CreationOrder ?? ++_creationOrder);
            if (old is not null)
            {
                cookie = cookie with { Created = old.Record.Created };
                Unindex(old);
            }
            var entry = Add(cookie);
            changes.Add(new(old is null ? CookieChangeKind.Inserted : CookieChangeKind.Replaced, old?.Record, cookie));
            EnforceLimits(entry, changes);
            return new(_cookies.GetValueOrDefault(key!) == entry ? CookieDecision.Stored : CookieDecision.Evicted);
        }
    }

    public string BuildRequestHeader(CookieRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Retrieve(context, document: false);
    }

    public string GetDocumentCookies(CookieDocumentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var wire = Retrieve(new(context.Url, context.SameSite, PartitionKey: context.PartitionKey), document: true);
        return Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(wire));
    }

    // Reads skip expired records without removing them, so they never publish or throw observer errors.
    private string Retrieve(CookieRequestContext context, bool document)
    {
        if (!CookiePolicy.TryBeginRetrieval(context, document, out var scope)) return "";
        lock (_sync)
        {
            var now = _clock.GetUtcNow();
            var selected = new List<Entry>();
            foreach (var domain in HostNames.DomainMatchCandidates(scope.Host))
            {
                if (!_byDomain.TryGetValue(domain, out var entries)) continue;
                bool? publicSuffix = null;
                foreach (var entry in entries)
                    if (Live(entry.Record, now) && _policy.CanRetrieve(entry.Record, scope, ref publicSuffix)) selected.Add(entry);
            }
            selected.Sort(RetrievalOrder);
            var header = new StringBuilder();
            foreach (var entry in selected)
            {
                var cookie = entry.Record = entry.Record with { LastAccessed = now };
                if (header.Length > 0) header.Append("; ");
                if (cookie.Name.Length > 0) header.Append(cookie.Name).Append('=');
                header.Append(cookie.Value);
            }
            return header.ToString();
        }
    }

    // 6265bis-22 5.8.3 step 2: longer paths first, then earlier creation times.
    private static int RetrievalOrder(Entry x, Entry y)
    {
        var (a, b) = (x.Record, y.Record);
        var order = b.Path.Length.CompareTo(a.Path.Length);
        if (order == 0) order = a.Created.CompareTo(b.Created);
        return order != 0 ? order : a.CreationOrder.CompareTo(b.CreationOrder);
    }

    /// <summary>Privileged immutable snapshot for host administration; includes HttpOnly values.</summary>
    public IReadOnlyList<CookieRecord> Snapshot() => Snapshot(out _);

    /// <summary>The snapshot and the <see cref="Revision"/> it reflects, taken atomically.</summary>
    public IReadOnlyList<CookieRecord> Snapshot(out long revision)
    {
        lock (_sync)
        {
            var now = _clock.GetUtcNow();
            revision = _revision;
            return Array.AsReadOnly(_cookies.Values.Select(e => e.Record).Where(c => Live(c, now)).OrderBy(c => c.CreationOrder).ToArray());
        }
    }

    /// <summary>Removes every cookie; returns the number of unexpired cookies cleared.</summary>
    public int Clear()
    {
        CookieChangeBatch? batch;
        int count;
        lock (_sync)
        {
            var changes = new List<CookieChange>();
            PurgeExpired(_clock.GetUtcNow(), changes);
            count = _cookies.Count;
            changes.AddRange(_cookies.Values.Select(e => e.Record).OrderBy(c => c.CreationOrder).Select(c => new CookieChange(CookieChangeKind.Cleared, c, null)));
            _cookies.Clear(); _byDomain.Clear(); _secureByName.Clear(); _buckets.Clear();
            _nextExpiry = DateTimeOffset.MaxValue;
            batch = Batch(changes);
        }
        List<Exception>? errors = null;
        Publish(batch, ref errors);
        ThrowIfObserversFailed(errors);
        return count;
    }

    /// <summary>Removes expired records, which reads skip but only mutations remove.</summary>
    public int PruneExpired()
    {
        var changes = new List<CookieChange>();
        CookieChangeBatch? batch;
        lock (_sync) { PurgeExpired(_clock.GetUtcNow(), changes); batch = Batch(changes); }
        List<Exception>? errors = null;
        Publish(batch, ref errors);
        ThrowIfObserversFailed(errors);
        return changes.Count;
    }

    private static bool Live(CookieRecord cookie, DateTimeOffset now) => cookie.Expires is not { } expires || expires > now;

    private void PurgeExpired(DateTimeOffset now, List<CookieChange> changes)
    {
        if (now < _nextExpiry) return;
        var next = DateTimeOffset.MaxValue;
        List<Entry>? expired = null;
        foreach (var entry in _cookies.Values)
        {
            if (entry.Record.Expires is not { } expires) continue;
            if (expires <= now) (expired ??= []).Add(entry);
            else if (expires < next) next = expires;
        }
        _nextExpiry = next;
        if (expired is null) return;
        foreach (var entry in expired.OrderBy(e => e.Record.CreationOrder)) Remove(entry, CookieChangeKind.Expired, changes);
    }

    // 6265bis-22 5.7 "remove excess cookies": expired records are already gone, then the over-quota site
    // bucket loses insecure cookies first, then its least recently accessed ones; global overflow ("all
    // cookies") removes the least recently accessed, secure or not. Ties use creation order.
    private void EnforceLimits(Entry inserted, List<CookieChange> changes)
    {
        var bucket = inserted.Bucket;
        while (bucket.Members.Count > _options.MaximumCookiesPerSite ||
            (bucket.Key.Partition is not null && bucket.Bytes > _options.MaximumPartitionedBytesPerSite))
            Remove(Victim(bucket.Members, preferInsecure: true), CookieChangeKind.Evicted, changes);
        while (_cookies.Count > _options.MaximumCookies)
            Remove(Victim(_cookies.Values, preferInsecure: false), CookieChangeKind.Evicted, changes);
    }

    private static Entry Victim(IEnumerable<Entry> scope, bool preferInsecure)
    {
        Entry? victim = null;
        foreach (var entry in scope)
        {
            if (victim is null) { victim = entry; continue; }
            var (a, b) = (entry.Record, victim.Record);
            if (preferInsecure && a.Secure != b.Secure ? !a.Secure
                : a.LastAccessed != b.LastAccessed ? a.LastAccessed < b.LastAccessed : a.CreationOrder < b.CreationOrder)
                victim = entry;
        }
        return victim!;
    }

    private Entry Add(CookieRecord cookie)
    {
        var key = new BucketKey(_policy.Sites.GetRegistrableDomain(cookie.Domain) ?? cookie.Domain, cookie.PartitionKey);
        if (!_buckets.TryGetValue(key, out var bucket)) _buckets.Add(key, bucket = new(key));
        var entry = new Entry(cookie, bucket);
        _cookies.Add(cookie.Key, entry);
        bucket.Members.Add(entry);
        bucket.Bytes += cookie.Name.Length + cookie.Value.Length;
        Members(_byDomain, cookie.Domain).Add(entry);
        if (cookie.Secure) Members(_secureByName, cookie.Name).Add(entry);
        if (cookie.Expires < _nextExpiry) _nextExpiry = cookie.Expires.Value;
        return entry;

        static HashSet<Entry> Members(Dictionary<string, HashSet<Entry>> index, string name) =>
            index.TryGetValue(name, out var set) ? set : (index[name] = []);
    }

    private void Remove(Entry entry, CookieChangeKind kind, List<CookieChange> changes)
    {
        Unindex(entry);
        changes.Add(new(kind, entry.Record, null));
    }

    private void Unindex(Entry entry)
    {
        var cookie = entry.Record;
        _cookies.Remove(cookie.Key);
        var bucket = entry.Bucket;
        bucket.Members.Remove(entry);
        bucket.Bytes -= cookie.Name.Length + cookie.Value.Length;
        if (bucket.Members.Count == 0) _buckets.Remove(bucket.Key);
        Drop(_byDomain, cookie.Domain);
        if (cookie.Secure) Drop(_secureByName, cookie.Name);

        void Drop(Dictionary<string, HashSet<Entry>> index, string name)
        {
            if (index.TryGetValue(name, out var set) && set.Remove(entry) && set.Count == 0) index.Remove(name);
        }
    }

    private CookieChangeBatch? Batch(List<CookieChange> changes) => changes.Count == 0 ? null :
        new(++_revision, Array.AsReadOnly(changes.ToArray()));

    private void Publish(CookieChangeBatch? batch, ref List<Exception>? errors)
    {
        if (batch is null || Changed is not { } observers) return;
        foreach (EventHandler<CookieChangeBatch> observer in observers.GetInvocationList())
        {
            try { observer(this, batch); }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
    }

    private static void ThrowIfObserversFailed(List<Exception>? errors)
    {
        if (errors is not null) throw new CookieObserverException(errors);
    }

    private readonly record struct BucketKey(string Site, CookiePartitionKey? Partition);

    private sealed class Bucket(BucketKey key)
    {
        public BucketKey Key { get; } = key;
        public HashSet<Entry> Members { get; } = [];
        public long Bytes { get; set; }
    }

    // Mutable holder so last-access updates do not touch the indexes.
    private sealed class Entry(CookieRecord record, Bucket bucket)
    {
        public CookieRecord Record { get; set; } = record;
        public Bucket Bucket { get; } = bucket;
    }
}

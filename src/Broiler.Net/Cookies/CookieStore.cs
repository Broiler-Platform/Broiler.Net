// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   30
// Annotated:        30/30
// Exempt:           16
// Human-reviewed:   0/30
// IP risk:          Low
// Security risk:    High
// Criteria:         28/22
// Resource impact:  5/10 max
// Unverified:       30
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Text;
using Broiler.Net.Sites;

namespace Broiler.Net.Cookies;

/// <summary>
/// Profile-owned, bounded in-memory store. HTTP/header strings preserve bytes using Latin-1;
/// document access uses UTF-8. Host code must apply Fetch credentials and user settings before
/// calling this service. This type does not derive browser navigation/frame context.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.7; IP=Low; Security=High; Resources=5; Fingerprint=5D7083
// Broiler-Falsified-If: a store field or index is read or written outside lock(_sync), or a Changed observer runs while _sync is held
// Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=F27A3A
    // Broiler-Falsified-If: a handler added or removed on one thread while Publish runs on another is lost from Changed or makes that Publish throw
    // Broiler-Human:        PENDING
    public event EventHandler<CookieChangeBatch>? Changed;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=3; Fingerprint=9E8256
    // Broiler-Falsified-If: a CookieStoreOptions with a zero or negative quota is accepted and the store is constructed
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=C93C34
    // Broiler-Falsified-If: Revision reads _revision without holding _sync, so a reader can see a value no committed batch carried
    // Broiler-Human:        PENDING
    public long Revision { get { lock (_sync) return _revision; } }

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.7; IP=Low; Security=High; Resources=5; Fingerprint=D1C390
    // Broiler-Falsified-If: a Set-Cookie field reaches the store without CookiePolicy.Accept running against this request's URL and same-site status
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.7; IP=Low; Security=High; Resources=5; Fingerprint=8E3D73
    // Broiler-Falsified-If: a failing Changed observer on one field stops a later field in the same call from being stored
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.8.2; IP=Low; Security=High; Resources=5; Fingerprint=5E3101
    // Broiler-Falsified-If: a script assignment is stored with document set to false, so it can create or overwrite an HttpOnly cookie
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=5; Fingerprint=B3B001
    // Broiler-Falsified-If: a response field reaches Receive without passing through CookieParser.Parse, or flagged as a document assignment
    // Broiler-Human:        PENDING
    private CookieResult ReceiveField(string header, CookieRequestContext context, ref List<Exception>? errors) =>
        Receive(CookieParser.Parse(header, _clock.GetUtcNow()), context, document: false, ref errors);

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.7; IP=Low; Security=High; Resources=5; Fingerprint=DE4F62
    // Broiler-Falsified-If: a Set-Cookie without Secure from an insecure origin is stored while a Secure cookie of the same name, a domain-matching Domain and a matching path exists in its partition
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.8.3; IP=None; Security=High; Resources=5; Fingerprint=5109F7
    // Broiler-Falsified-If: the header for a request carries a cookie that CookiePolicy.CanRetrieve rejects for that request's context
    // Broiler-Human:        PENDING
    public string BuildRequestHeader(CookieRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Retrieve(context, document: false);
    }

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.8.2; IP=Low; Security=High; Resources=5; Fingerprint=1741FE
    // Broiler-Falsified-If: an HttpOnly cookie that matches the document URL appears in the returned string
    // Broiler-Human:        PENDING
    public string GetDocumentCookies(CookieDocumentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var wire = Retrieve(new(context.Url, context.SameSite, PartitionKey: context.PartitionKey), document: true);
        return Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(wire));
    }

    // Reads skip expired records without removing them, so they never publish or throw observer errors.
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.8.3; IP=Low; Security=High; Resources=5; Fingerprint=23A798
    // Broiler-Falsified-If: a record whose Expires is at or before the read's clock reading is included in the returned header
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.8.3; IP=Low; Security=Medium; Resources=0; Fingerprint=6C9506
    // Broiler-Falsified-If: a cookie with a shorter Path is serialized before one with a longer Path in the same header
    // Broiler-Human:        PENDING
    private static int RetrievalOrder(Entry x, Entry y)
    {
        var (a, b) = (x.Record, y.Record);
        var order = b.Path.Length.CompareTo(a.Path.Length);
        if (order == 0) order = a.Created.CompareTo(b.Created);
        return order != 0 ? order : a.CreationOrder.CompareTo(b.CreationOrder);
    }

    /// <summary>Privileged immutable snapshot for host administration; includes HttpOnly values.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=4; Fingerprint=18A15A
    // Broiler-Falsified-If: Snapshot() leaves out a live cookie, HttpOnly ones included, that Snapshot(out long) returns for the same store state
    // Broiler-Human:        PENDING
    public IReadOnlyList<CookieRecord> Snapshot() => Snapshot(out _);

    /// <summary>The snapshot and the <see cref="Revision"/> it reflects, taken atomically.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=4; Fingerprint=64C1BE
    // Broiler-Falsified-If: the returned revision is not the revision of the last batch whose changes the returned records reflect
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=4; Fingerprint=2783CB
    // Broiler-Falsified-If: a record that had already expired is counted in the return value or published as Cleared instead of Expired
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.7; IP=Low; Security=High; Resources=4; Fingerprint=3EB1C3
    // Broiler-Falsified-If: a Changed observer runs while PruneExpired still holds _sync
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=AC3BC5
    // Broiler-Falsified-If: a persistent cookie whose Expires equals the current instant is treated as live
    // Broiler-Human:        PENDING
    private static bool Live(CookieRecord cookie, DateTimeOffset now) => cookie.Expires is not { } expires || expires > now;

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.7; IP=Low; Security=Medium; Resources=4; Fingerprint=6F6BCA
    // Broiler-Falsified-If: after a purge, _nextExpiry is later than the earliest Expires among the remaining records, so a later mutation leaves an expired record in place
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.7; IP=Low; Security=High; Resources=5; Fingerprint=AE7EE5
    // Broiler-Falsified-If: after an insert returns, one site bucket holds more than MaximumCookiesPerSite records, or the store holds more than MaximumCookies
    // Broiler-Human:        PENDING
    private void EnforceLimits(Entry inserted, List<CookieChange> changes)
    {
        var bucket = inserted.Bucket;
        while (bucket.Members.Count > _options.MaximumCookiesPerSite ||
            (bucket.Key.Partition is not null && bucket.Bytes > _options.MaximumPartitionedBytesPerSite))
            Remove(Victim(bucket.Members, preferInsecure: true), CookieChangeKind.Evicted, changes);
        while (_cookies.Count > _options.MaximumCookies)
            Remove(Victim(_cookies.Values, preferInsecure: false), CookieChangeKind.Evicted, changes);
    }

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.7; IP=Low; Security=High; Resources=4; Fingerprint=F12C65
    // Broiler-Falsified-If: with preferInsecure set, a Secure cookie is chosen while the scope still holds a cookie without Secure
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=890D91
    // Broiler-Falsified-If: two cookies in the same partition whose domains share a registrable domain are charged to different site buckets
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=C7F6D1
    // Broiler-Falsified-If: a removed record is published without its Previous record, or with a Current record
    // Broiler-Human:        PENDING
    private void Remove(Entry entry, CookieChangeKind kind, List<CookieChange> changes)
    {
        Unindex(entry);
        changes.Add(new(kind, entry.Record, null));
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=D2A58B
    // Broiler-Falsified-If: a record removed from _cookies is still found by Retrieve through the _byDomain index
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=3; Fingerprint=B4971C
    // Broiler-Falsified-If: two published batches carry the same revision, or a batch is created for an empty change list
    // Broiler-Human:        PENDING
    private CookieChangeBatch? Batch(List<CookieChange> changes) => changes.Count == 0 ? null :
        new(++_revision, Array.AsReadOnly(changes.ToArray()));

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=0B8EB7
    // Broiler-Falsified-If: an observer that throws prevents a later observer in the invocation list from being called
    // Broiler-Human:        PENDING
    private void Publish(CookieChangeBatch? batch, ref List<Exception>? errors)
    {
        if (batch is null || Changed is not { } observers) return;
        foreach (EventHandler<CookieChangeBatch> observer in observers.GetInvocationList())
        {
            try { observer(this, batch); }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=3D791A
    // Broiler-Falsified-If: a non-null error list returns without throwing CookieObserverException
    // Broiler-Human:        PENDING
    private static void ThrowIfObserversFailed(List<Exception>? errors)
    {
        if (errors is not null) throw new CookieObserverException(errors);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=AD4675
    // Broiler-Falsified-If: two keys with the same Site and different Partition values compare equal
    // Broiler-Human:        PENDING
    private readonly record struct BucketKey(string Site, CookiePartitionKey? Partition);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=2010E4
    // Broiler-Human:        PENDING
    private sealed class Bucket(BucketKey key)
    {
        public BucketKey Key { get; } = key;
        public HashSet<Entry> Members { get; } = [];
        public long Bytes { get; set; }
    }

    // Mutable holder so last-access updates do not touch the indexes.
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=45A4C4
    // Broiler-Human:        PENDING
    private sealed class Entry(CookieRecord record, Bucket bucket)
    {
        public CookieRecord Record { get; set; } = record;
        public Bucket Bucket { get; } = bucket;
    }
}

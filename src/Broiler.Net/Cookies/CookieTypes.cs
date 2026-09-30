// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   23
// Annotated:        23/23
// Exempt:           45
// Human-reviewed:   0/23
// IP risk:          Low
// Security risk:    High
// Criteria:         13/8
// Resource impact:  1/10 max
// Unverified:       23
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Net.Sites;

namespace Broiler.Net.Cookies;

// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=4C1665
// Broiler-Human:        PENDING
public enum CookieSameSite { Default, Lax, Strict, None }
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=8E3E82
// Broiler-Human:        PENDING
public enum SameSiteStatus { CrossSite, SameSite }

/// <summary>Host-derived partition identity; never infer it from the destination alone.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=8A0D78
// Broiler-Falsified-If: two keys with the same top-level site and different HasCrossSiteAncestor values compare equal
// Broiler-Human:        PENDING
public sealed record CookiePartitionKey(SchemefulSite TopLevelSite, bool HasCrossSiteAncestor = false);

/// <summary>Trusted host inputs. Same-site status must include ancestor and redirect context.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=E2EDBD
// Broiler-Falsified-If: a context built without IsTopLevelNavigation lets a Lax cookie through on a cross-site request
// Broiler-Human:        PENDING
public sealed record CookieRequestContext(
    Uri Url, SameSiteStatus SameSite, string Method = "GET", bool IsTopLevelNavigation = false,
    CookiePartitionKey? PartitionKey = null);

/// <summary>Uses the effective cookie URL, not a document's resource-resolution base URL.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=A5F3E0
// Broiler-Human:        PENDING
public sealed record CookieDocumentContext(Uri Url, SameSiteStatus SameSite, CookiePartitionKey? PartitionKey = null);

// Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=1A6E29
// Broiler-Falsified-If: two keys that differ only in HostOnly or PartitionKey compare equal, so one cookie replaces the other
// Broiler-Human:        PENDING
public sealed record CookieKey(string Name, string Domain, bool HostOnly, string Path, CookiePartitionKey? PartitionKey);

/// <summary>An immutable stored cookie. Only the store can construct or change a record.</summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=31692A
// Broiler-Falsified-If: code outside Broiler.Net can construct a CookieRecord or change its Value through a with expression
// Broiler-Human:        PENDING
public sealed record CookieRecord
{
    public CookieKey Key { get; internal init; }
    public string Name => Key.Name;
    public string Domain => Key.Domain;
    public bool HostOnly => Key.HostOnly;
    public string Path => Key.Path;
    public CookiePartitionKey? PartitionKey => Key.PartitionKey;
    public string Value { get; internal init; }
    public bool Secure { get; internal init; }
    public bool HttpOnly { get; internal init; }
    public CookieSameSite SameSite { get; internal init; }
    public DateTimeOffset? Expires { get; internal init; }
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=86E4CA
    // Broiler-Human:        PENDING
    public bool Persistent => Expires.HasValue;
    public DateTimeOffset Created { get; internal init; }
    public DateTimeOffset LastAccessed { get; internal init; }
    public long CreationOrder { get; internal init; }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=25FB1C
    // Broiler-Falsified-If: a record is built whose Secure, HttpOnly or SameSite differs from the parsed cookie it came from
    // Broiler-Human:        PENDING
    internal CookieRecord(CookieKey key, ParsedCookie parsed, DateTimeOffset now, long order)
    {
        Key = key; Value = parsed.Value; Secure = parsed.Secure; HttpOnly = parsed.HttpOnly;
        SameSite = parsed.SameSite; Expires = parsed.Expires;
        Created = LastAccessed = now; CreationOrder = order;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=C9568A
    // Broiler-Falsified-If: the returned text contains the cookie's name or value
    // Broiler-Human:        PENDING
    public override string ToString() => $"Cookie({Domain}, {Path}, Secure={Secure}, HttpOnly={HttpOnly})";
}

// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=958609
// Broiler-Human:        PENDING
public enum CookieDecision
{
    Stored, Deleted, RejectedControlCharacter, RejectedSize, RejectedEncoding, RejectedEmpty,
    RejectedScheme, RejectedDomain, RejectedPublicSuffix, RejectedSecure, RejectedHttpOnly,
    RejectedSecureOverlay, RejectedSameSite, RejectedPrefix, RejectedPartition, Evicted
}

// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=54C6A0
// Broiler-Human:        PENDING
public readonly record struct CookieResult(CookieDecision Decision)
{
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=ED7442
    // Broiler-Falsified-If: a decision whose name starts with Rejected reports Accepted as true
    // Broiler-Human:        PENDING
    public bool Accepted => Decision is CookieDecision.Stored or CookieDecision.Deleted or CookieDecision.Evicted;
}

// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=88898F
// Broiler-Human:        PENDING
public enum CookieChangeKind { Inserted, Replaced, Deleted, Expired, Evicted, Cleared }
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=F917A3
// Broiler-Human:        PENDING
public sealed record CookieChange(CookieChangeKind Kind, CookieRecord? Previous, CookieRecord? Current);
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=FCD11C
// Broiler-Human:        PENDING
public sealed record CookieChangeBatch(long Revision, IReadOnlyList<CookieChange> Changes);

/// <summary>
/// Quotas. A site bucket is (registrable domain of the cookie's Domain, or the host itself for IP addresses
/// and public suffixes; partition key), so subdomains share one budget and embedded sites never share one.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=24ED08
// Broiler-Human:        PENDING
public sealed record CookieStoreOptions
{
    /// <summary>Records per store.</summary>
    public int MaximumCookies { get; init; } = 6000;
    /// <summary>Records per site bucket.</summary>
    public int MaximumCookiesPerSite { get; init; } = 180;
    /// <summary>Name plus value octets per partitioned site bucket (the CHIPS per-partition budget).</summary>
    public int MaximumPartitionedBytesPerSite { get; init; } = 10240;
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=FAE531
    // Broiler-Falsified-If: a quota of zero or below passes Validate without an exception
    // Broiler-Human:        PENDING
    internal void Validate()
    {
        if (MaximumCookies <= 0 || MaximumCookiesPerSite <= 0 || MaximumPartitionedBytesPerSite <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaximumCookies), "Cookie quotas must be positive.");
    }
}

// Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=30F158
// Broiler-Falsified-If: an implementation serves the document methods with HTTP privileges, so GetDocumentCookies returns an HttpOnly cookie
// Broiler-Human:        PENDING
public interface ICookieService
{
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=B0E6BB
    // Broiler-Falsified-If: an implementation stores a Set-Cookie field whose Domain attribute the host of the context URL does not domain-match
    // Broiler-Human:        PENDING
    CookieResult ReceiveResponseCookie(string header, CookieRequestContext context);
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=F7FB5D
    // Broiler-Falsified-If: the header an implementation builds carries a cookie whose Domain or Path does not match the context URL
    // Broiler-Human:        PENDING
    string BuildRequestHeader(CookieRequestContext context);
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=A4646E
    // Broiler-Falsified-If: an implementation lets a document assignment create a cookie with the HttpOnly attribute or replace an existing HttpOnly cookie
    // Broiler-Human:        PENDING
    CookieResult SetDocumentCookie(string assignment, CookieDocumentContext context);
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=AB33B7
    // Broiler-Falsified-If: an implementation includes an HttpOnly cookie in the string it returns for document.cookie
    // Broiler-Human:        PENDING
    string GetDocumentCookies(CookieDocumentContext context);
}

using Broiler.Net.Sites;

namespace Broiler.Net.Cookies;

public enum CookieSameSite { Default, Lax, Strict, None }
public enum SameSiteStatus { CrossSite, SameSite }

/// <summary>Host-derived partition identity; never infer it from the destination alone.</summary>
public sealed record CookiePartitionKey(SchemefulSite TopLevelSite, bool HasCrossSiteAncestor = false);

/// <summary>Trusted host inputs. Same-site status must include ancestor and redirect context.</summary>
public sealed record CookieRequestContext(
    Uri Url, SameSiteStatus SameSite, string Method = "GET", bool IsTopLevelNavigation = false,
    CookiePartitionKey? PartitionKey = null);

/// <summary>Uses the effective cookie URL, not a document's resource-resolution base URL.</summary>
public sealed record CookieDocumentContext(Uri Url, SameSiteStatus SameSite, CookiePartitionKey? PartitionKey = null);

public sealed record CookieKey(string Name, string Domain, bool HostOnly, string Path, CookiePartitionKey? PartitionKey);

/// <summary>An immutable stored cookie. Only the store can construct or change a record.</summary>
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
    public bool Persistent => Expires.HasValue;
    public DateTimeOffset Created { get; internal init; }
    public DateTimeOffset LastAccessed { get; internal init; }
    public long CreationOrder { get; internal init; }

    internal CookieRecord(CookieKey key, ParsedCookie parsed, DateTimeOffset now, long order)
    {
        Key = key; Value = parsed.Value; Secure = parsed.Secure; HttpOnly = parsed.HttpOnly;
        SameSite = parsed.SameSite; Expires = parsed.Expires;
        Created = LastAccessed = now; CreationOrder = order;
    }

    public override string ToString() => $"Cookie({Domain}, {Path}, Secure={Secure}, HttpOnly={HttpOnly})";
}

public enum CookieDecision
{
    Stored, Deleted, RejectedControlCharacter, RejectedSize, RejectedEncoding, RejectedEmpty,
    RejectedScheme, RejectedDomain, RejectedPublicSuffix, RejectedSecure, RejectedHttpOnly,
    RejectedSecureOverlay, RejectedSameSite, RejectedPrefix, RejectedPartition, Evicted
}

public readonly record struct CookieResult(CookieDecision Decision)
{
    public bool Accepted => Decision is CookieDecision.Stored or CookieDecision.Deleted or CookieDecision.Evicted;
}

public enum CookieChangeKind { Inserted, Replaced, Deleted, Expired, Evicted, Cleared }
public sealed record CookieChange(CookieChangeKind Kind, CookieRecord? Previous, CookieRecord? Current);
public sealed record CookieChangeBatch(long Revision, IReadOnlyList<CookieChange> Changes);

/// <summary>
/// Quotas. A site bucket is (registrable domain of the cookie's Domain, or the host itself for IP addresses
/// and public suffixes; partition key), so subdomains share one budget and embedded sites never share one.
/// </summary>
public sealed record CookieStoreOptions
{
    /// <summary>Records per store.</summary>
    public int MaximumCookies { get; init; } = 6000;
    /// <summary>Records per site bucket.</summary>
    public int MaximumCookiesPerSite { get; init; } = 180;
    /// <summary>Name plus value octets per partitioned site bucket (the CHIPS per-partition budget).</summary>
    public int MaximumPartitionedBytesPerSite { get; init; } = 10240;
    internal void Validate()
    {
        if (MaximumCookies <= 0 || MaximumCookiesPerSite <= 0 || MaximumPartitionedBytesPerSite <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaximumCookies), "Cookie quotas must be positive.");
    }
}

public interface ICookieService
{
    CookieResult ReceiveResponseCookie(string header, CookieRequestContext context);
    string BuildRequestHeader(CookieRequestContext context);
    CookieResult SetDocumentCookie(string assignment, CookieDocumentContext context);
    string GetDocumentCookies(CookieDocumentContext context);
}

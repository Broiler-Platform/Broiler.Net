namespace Broiler.Net.Sites;

/// <summary>Schemeful same-site comparisons for origins (HTML "same site").</summary>
public static class SiteMatching
{
    /// <summary>The schemeful site of a tuple origin; null for opaque origins.</summary>
    public static SchemefulSite? GetSite(this ISiteResolver sites, Origin origin)
    {
        ArgumentNullException.ThrowIfNull(sites);
        ArgumentNullException.ThrowIfNull(origin);
        return origin.IsOpaque ? null : new SchemefulSite(origin.Scheme, sites.GetRegistrableDomain(origin.Host) ?? origin.Host);
    }

    /// <summary>Opaque origins are same site only with themselves; tuple origins compare schemeful sites.</summary>
    public static bool IsSameSite(this ISiteResolver sites, Origin a, Origin b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return a.IsOpaque || b.IsOpaque ? a.Equals(b) : sites.GetSite(a) == sites.GetSite(b);
    }
}

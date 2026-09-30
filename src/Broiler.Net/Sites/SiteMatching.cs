// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           0
// Human-reviewed:   0/3
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  5/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.Net.Sites;

/// <summary>Schemeful same-site comparisons for origins (HTML "same site").</summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=94027E
// Broiler-Falsified-If: a member here compares sites without their scheme, so http://a.example and https://a.example are same-site
// Broiler-Human:        PENDING
public static class SiteMatching
{
    /// <summary>The schemeful site of a tuple origin; null for opaque origins.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=535C1C
    // Broiler-Falsified-If: GetSite returns equal sites for two different IP-address origins, such as https://192.0.2.1 and https://192.0.2.2
    // Broiler-Human:        PENDING
    public static SchemefulSite? GetSite(this ISiteResolver sites, Origin origin)
    {
        ArgumentNullException.ThrowIfNull(sites);
        ArgumentNullException.ThrowIfNull(origin);
        return origin.IsOpaque ? null : new SchemefulSite(origin.Scheme, sites.GetRegistrableDomain(origin.Host) ?? origin.Host);
    }

    /// <summary>Opaque origins are same site only with themselves; tuple origins compare schemeful sites.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=827F64
    // Broiler-Falsified-If: IsSameSite returns true when one origin is opaque and the other is a tuple origin
    // Broiler-Human:        PENDING
    public static bool IsSameSite(this ISiteResolver sites, Origin a, Origin b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return a.IsOpaque || b.IsOpaque ? a.Equals(b) : sites.GetSite(a) == sites.GetSite(b);
    }
}

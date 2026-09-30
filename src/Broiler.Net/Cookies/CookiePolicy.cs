// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   10
// Annotated:        10/10
// Exempt:           1
// Human-reviewed:   0/10
// IP risk:          Low
// Security risk:    High
// Criteria:         9/8
// Resource impact:  3/10 max
// Unverified:       10
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Net.Sites;

namespace Broiler.Net.Cookies;

/// <summary>Pure acceptance and retrieval rules; browser context derivation is the host's job.</summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=BCBB23
// Broiler-Falsified-If: a cookie accepted from sub.example.com without a Domain attribute is returned for a request to example.com
// Broiler-Human:        PENDING
public sealed class CookiePolicy
{
    public ISiteResolver Sites { get; }
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=3; Fingerprint=9295F7
    // Broiler-Falsified-If: a policy constructed without a resolver accepts a Domain attribute of co.uk from www.example.co.uk
    // Broiler-Human:        PENDING
    public CookiePolicy(ISiteResolver? sites = null) => Sites = sites ?? SiteResolver.Default;

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.7; IP=Low; Security=High; Resources=3; Fingerprint=CF7D1A
    // Broiler-Falsified-If: a Set-Cookie received from a.example.com with a Domain attribute of b.example.com produces a key instead of RejectedDomain
    // Broiler-Human:        PENDING
    internal CookieDecision? Accept(ParsedCookie parsed, CookieRequestContext context, bool document, out CookieKey? key)
    {
        key = null;
        if (!HostNames.TryGetHttpHost(context.Url, out var host)) return CookieDecision.RejectedScheme;
        var domain = parsed.Domain;
        if (domain.Any(c => c > 127)) return CookieDecision.RejectedDomain;
        if (domain.Length > 0)
        {
            // Do not IDNA-map an attribute: the wire Domain must already be ASCII. Canonicalization
            // must not expand IPv4 shorthands or remove an extra leading dot from the attribute.
            if (!HostNames.TryCanonicalize(domain, out var canonical) || canonical != domain)
                return CookieDecision.RejectedDomain;
            if (Sites.IsPublicSuffix(domain))
            {
                if (domain != host) return CookieDecision.RejectedPublicSuffix;
                domain = "";
            }
        }
        var hostOnly = domain.Length == 0;
        if (hostOnly) domain = host;
        else if (!HostNames.DomainMatches(host, domain)) return CookieDecision.RejectedDomain;

        var path = parsed.Path is { Length: > 0 } p && p[0] == '/' ? p : DefaultPath(context.Url);
        // A defaulted path gets the same bound as the Path attribute, so record size stays bounded.
        if (path.Length > CookieParser.MaximumAttributeValueBytes) return CookieDecision.RejectedSize;
        var secure = HostNames.IsSecure(context.Url.Scheme, host);
        if (parsed.Secure && !secure) return CookieDecision.RejectedSecure;
        if (document && parsed.HttpOnly) return CookieDecision.RejectedHttpOnly;
        if (parsed.SameSite == CookieSameSite.None && !parsed.Secure) return CookieDecision.RejectedSameSite;
        if (parsed.SameSite != CookieSameSite.None && context.SameSite != SameSiteStatus.SameSite &&
            (document || !context.IsTopLevelNavigation)) return CookieDecision.RejectedSameSite;
        if (Prefix(parsed.Name, "__Secure-") && !parsed.Secure) return CookieDecision.RejectedPrefix;
        // An empty/invalid Path can default to '/', but does not explicitly establish the prefix's scope.
        if (Prefix(parsed.Name, "__Host-") && (!parsed.Secure || !hostOnly || parsed.Path != "/"))
            return CookieDecision.RejectedPrefix;
        // layered-cookies-02 extension: __Http- and __Host-Http- need Secure, HttpOnly and an HTTP source.
        if ((Prefix(parsed.Name, "__Http-") || Prefix(parsed.Name, "__Host-Http-")) && (!parsed.Secure || !parsed.HttpOnly || document))
            return CookieDecision.RejectedPrefix;
        if (parsed.Name.Length == 0 && (Prefix(parsed.Value, "__Secure-") || Prefix(parsed.Value, "__Host-") || Prefix(parsed.Value, "__Http-")))
            return CookieDecision.RejectedPrefix;
        if (parsed.Partitioned && (!parsed.Secure || context.PartitionKey is null || context.PartitionKey.TopLevelSite is null))
            return CookieDecision.RejectedPartition;
        key = new CookieKey(parsed.Name, domain, hostOnly, path, parsed.Partitioned ? context.PartitionKey : null);
        return null;
    }

    /// <summary>Request facts derived once per retrieval, not once per cookie.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=37F043
    // Broiler-Human:        PENDING
    internal readonly record struct RetrievalScope(string Host, bool Secure, string Path, CookieRequestContext Context, bool Document, bool LaxAllowed);

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.8; IP=Low; Security=High; Resources=3; Fingerprint=FFEAA3
    // Broiler-Falsified-If: a cross-site top-level POST navigation yields a scope that lets Lax cookies through
    // Broiler-Human:        PENDING
    internal static bool TryBeginRetrieval(CookieRequestContext context, bool document, out RetrievalScope scope)
    {
        scope = default;
        if (!HostNames.TryGetHttpHost(context.Url, out var host)) return false;
        scope = new(host, HostNames.IsSecure(context.Url.Scheme, host), context.Url.AbsolutePath, context, document,
            !document && context.IsTopLevelNavigation && IsSafeMethod(context.Method));
        return true;
    }

    /// <summary><paramref name="publicSuffix"/> caches the public-suffix test for the cookie's Domain.</summary>
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.8; IP=Low; Security=High; Resources=2; Fingerprint=C82973
    // Broiler-Falsified-If: a SameSite Strict cookie is returned for a cross-site top-level GET navigation
    // Broiler-Human:        PENDING
    internal bool CanRetrieve(CookieRecord cookie, in RetrievalScope scope, ref bool? publicSuffix)
    {
        if (cookie.HostOnly ? scope.Host != cookie.Domain :
            !HostNames.DomainMatches(scope.Host, cookie.Domain) || (publicSuffix ??= Sites.IsPublicSuffix(cookie.Domain)))
            return false;
        if (!PathMatches(scope.Path, cookie.Path)) return false;
        if (cookie.Secure && !scope.Secure) return false;
        if (scope.Document && cookie.HttpOnly) return false;
        if (cookie.PartitionKey is not null && cookie.PartitionKey != scope.Context.PartitionKey) return false;
        if (cookie.SameSite != CookieSameSite.None && scope.Context.SameSite != SameSiteStatus.SameSite)
            return scope.LaxAllowed && cookie.SameSite is CookieSameSite.Lax or CookieSameSite.Default;
        return true;
    }

    // Fetch normalizes the six standard methods to upper case; any other method keeps its case.
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=91C767
    // Broiler-Falsified-If: the method POST, in any letter case, is classed as safe, so Lax cookies ride a cross-site top-level form submission
    // Broiler-Human:        PENDING
    private static bool IsSafeMethod(string? method) => method is "TRACE" ||
        method is not null && (method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) || method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase));

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.4; IP=None; Security=High; Resources=0; Fingerprint=495678
    // Broiler-Falsified-If: a cookie named __host-id in lower case is stored with a Domain attribute because the prefix comparison is case-sensitive
    // Broiler-Human:        PENDING
    private static bool Prefix(string value, string prefix) => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.1.4; IP=Low; Security=High; Resources=3; Fingerprint=267F8A
    // Broiler-Falsified-If: a cookie without a Path set by a response for /docs/page gets a default path other than /docs
    // Broiler-Human:        PENDING
    public static string DefaultPath(Uri url)
    {
        var path = url.AbsolutePath;
        var slash = path.LastIndexOf('/');
        return slash <= 0 ? "/" : path[..slash];
    }

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.1.4; IP=Low; Security=High; Resources=2; Fingerprint=42CA58
    // Broiler-Falsified-If: a cookie path of /foo matches the request path /foobar
    // Broiler-Human:        PENDING
    public static bool PathMatches(string requestPath, string cookiePath) =>
        requestPath == cookiePath || (requestPath.StartsWith(cookiePath, StringComparison.Ordinal) &&
            (cookiePath.EndsWith('/') || (requestPath.Length > cookiePath.Length && requestPath[cookiePath.Length] == '/')));
}

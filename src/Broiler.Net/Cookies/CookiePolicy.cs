using Broiler.Net.Sites;

namespace Broiler.Net.Cookies;

/// <summary>Pure acceptance and retrieval rules; browser context derivation is the host's job.</summary>
public sealed class CookiePolicy
{
    public ISiteResolver Sites { get; }
    public CookiePolicy(ISiteResolver? sites = null) => Sites = sites ?? SiteResolver.Default;

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
    internal readonly record struct RetrievalScope(string Host, bool Secure, string Path, CookieRequestContext Context, bool Document, bool LaxAllowed);

    internal static bool TryBeginRetrieval(CookieRequestContext context, bool document, out RetrievalScope scope)
    {
        scope = default;
        if (!HostNames.TryGetHttpHost(context.Url, out var host)) return false;
        scope = new(host, HostNames.IsSecure(context.Url.Scheme, host), context.Url.AbsolutePath, context, document,
            !document && context.IsTopLevelNavigation && IsSafeMethod(context.Method));
        return true;
    }

    /// <summary><paramref name="publicSuffix"/> caches the public-suffix test for the cookie's Domain.</summary>
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
    private static bool IsSafeMethod(string? method) => method is "TRACE" ||
        method is not null && (method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) || method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase));

    private static bool Prefix(string value, string prefix) => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    public static string DefaultPath(Uri url)
    {
        var path = url.AbsolutePath;
        var slash = path.LastIndexOf('/');
        return slash <= 0 ? "/" : path[..slash];
    }

    public static bool PathMatches(string requestPath, string cookiePath) =>
        requestPath == cookiePath || (requestPath.StartsWith(cookiePath, StringComparison.Ordinal) &&
            (cookiePath.EndsWith('/') || (requestPath.Length > cookiePath.Length && requestPath[cookiePath.Length] == '/')));
}

using System.Buffers;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Broiler.Net.Sites;

/// <summary>A schemeful site. Ports belong to origins, not sites.</summary>
public sealed record SchemefulSite
{
    public string Scheme { get; }
    public string Host { get; }
    internal SchemefulSite(string scheme, string host) => (Scheme, Host) = (scheme, host);
    public override string ToString() => $"{Scheme}://{Host}";
}

public interface ISiteResolver
{
    SchemefulSite? GetSite(Uri url);
    bool IsPublicSuffix(string canonicalHost);
    string? GetRegistrableDomain(string canonicalHost);
}

/// <summary>Immutable PSL resolver including private domains, wildcard rules and exceptions.</summary>
public sealed class SiteResolver : ISiteResolver
{
    private readonly HashSet<string> _exact = new(StringComparer.Ordinal);
    private readonly HashSet<string> _wildcards = new(StringComparer.Ordinal);
    private readonly HashSet<string> _exceptions = new(StringComparer.Ordinal);
    private static readonly Lazy<SiteResolver> Bundled = new(LoadBundled);
    public static SiteResolver Default => Bundled.Value;

    /// <summary>Reads the PSL format: each line up to its first whitespace; lines starting with // are comments.</summary>
    public SiteResolver(TextReader rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        while (rules.ReadLine() is { } text)
        {
            text = text.TrimStart();
            var end = 0;
            while (end < text.Length && !char.IsWhiteSpace(text[end])) end++;
            var line = text[..end];
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) continue;
            var set = line.StartsWith('!') ? _exceptions : line.StartsWith("*.", StringComparison.Ordinal) ? _wildcards : _exact;
            var rule = line.StartsWith('!') ? line[1..] : line.StartsWith("*.", StringComparison.Ordinal) ? line[2..] : line;
            if (HostNames.TryCanonicalize(rule, out var host)) set.Add(host);
            else throw new FormatException($"Invalid public suffix rule: {line}");
        }
    }

    private static SiteResolver LoadBundled()
    {
        using var stream = typeof(SiteResolver).Assembly.GetManifestResourceStream("Broiler.Net.public_suffix_list.dat")
            ?? throw new InvalidOperationException("Bundled Public Suffix List is missing.");
        using var reader = new StreamReader(stream);
        return new SiteResolver(reader);
    }

    public SchemefulSite? GetSite(Uri url)
    {
        if (!HostNames.TryGetHttpHost(url, out var host)) return null;
        return new SchemefulSite(url.Scheme, GetRegistrableDomain(host) ?? host);
    }

    public bool IsPublicSuffix(string canonicalHost)
    {
        if (!HostNames.TryCanonicalize(canonicalHost, out var host) || HostNames.IsIp(host)) return false;
        return host.TrimEnd('.').Split('.').Length == SuffixLabels(host);
    }

    public string? GetRegistrableDomain(string canonicalHost)
    {
        if (!HostNames.TryCanonicalize(canonicalHost, out var host) || HostNames.IsIp(host)) return null;
        var labels = host.TrimEnd('.').Split('.');
        var count = SuffixLabels(host);
        if (labels.Length <= count) return null;
        return string.Join('.', labels.AsSpan(labels.Length - count - 1).ToArray()) + (host.EndsWith('.') ? "." : "");
    }

    private int SuffixLabels(string host)
    {
        var labels = host.TrimEnd('.').Split('.');
        var best = 1; // The PSL's implicit wildcard.
        for (var i = 0; i < labels.Length; i++)
        {
            var suffix = string.Join('.', labels.AsSpan(i).ToArray());
            var length = labels.Length - i;
            if (_exceptions.Contains(suffix)) return length - 1;
            if (_exact.Contains(suffix)) best = Math.Max(best, length);
            if (i > 0 && _wildcards.Contains(suffix)) best = Math.Max(best, length + 1);
        }
        return best;
    }
}

/// <summary>URL Standard host parsing for http(s) URLs, plus cookie domain matching.</summary>
public static class HostNames
{
    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdefABCDEF");

    /// <summary>False for non-HTTP(S) URLs and for hosts that are URL failures; never throws for an absolute Uri.</summary>
    public static bool TryGetHttpHost(Uri? uri, out string host)
    {
        host = "";
        if (uri is not { IsAbsoluteUri: true } || uri.Scheme is not ("http" or "https")) return false;
        string idnHost;
        // IdnHost is computed lazily and throws for some hosts that Uri accepted (e.g. a ZWJ between letters).
        try { idnHost = uri.IdnHost; }
        catch (UriFormatException) { return false; }
        return TryCanonicalize(idnHost, out host);
    }

    /// <summary>
    /// Canonical ASCII host: IPv6 without brackets, IPv4 as a dotted quad (URL Standard IPv4 parser, also for
    /// hosts that end in a number), otherwise a lowercase domain that may keep one trailing dot. ASCII labels
    /// without xn-- are only lowercased, also next to IDN labels; the host is then checked for forbidden code
    /// points and empty labels.
    /// </summary>
    public static bool TryCanonicalize(string? input, out string host)
    {
        host = "";
        if (string.IsNullOrEmpty(input)) return false;
        if (input.StartsWith('[') || input.Contains(':'))
        {
            var raw = input.StartsWith('[') && input.EndsWith(']') ? input[1..^1] : input;
            // Scoped IPv6 literals ('%') are not web origins.
            if (raw.Length == 0 || !raw.All(c => char.IsAsciiHexDigit(c) || c is ':' or '.') ||
                !IPAddress.TryParse(raw, out var ip) || ip.AddressFamily != AddressFamily.InterNetworkV6) return false;
            host = ip.ToString();
            return true;
        }
        // Label by label, so ASCII labels keep the URL Standard's lenient rules (no hyphen or DNS length checks)
        // while xn-- and non-ASCII labels get UTS46 mapping and validation. The UTS46 full stops separate labels too.
        var labels = input.Replace('。', '.').Replace('．', '.').Replace('｡', '.').Split('.');
        IdnMapping? idn = null;
        for (var i = 0; i < labels.Length; i++)
        {
            if (labels[i].All(char.IsAscii) && !labels[i].StartsWith("xn--", StringComparison.OrdinalIgnoreCase))
            {
                labels[i] = labels[i].ToLowerInvariant();
                continue;
            }
            try { labels[i] = (idn ??= new IdnMapping()).GetAscii(labels[i]).ToLowerInvariant(); }
            catch (ArgumentException) { return false; }
        }
        var ascii = string.Join('.', labels);
        var name = ascii.EndsWith('.') ? ascii[..^1] : ascii;
        if (name.Length == 0 || ascii.Any(IsForbiddenDomainCodePoint) || name.Split('.').Any(label => label.Length == 0)) return false;
        if (EndsInNumber(name)) return TryParseIPv4(name, out host);
        host = ascii;
        return true;
    }

    public static bool IsIp(string host) => IPAddress.TryParse(host, out _);

    public static bool DomainMatches(string host, string domain) =>
        host == domain || (!IsIp(host) && host.EndsWith('.' + domain, StringComparison.Ordinal));

    /// <summary>
    /// HTTPS, or HTTP to 127.0.0.0/8, ::1, localhost or *.localhost. The localhost names qualify only because
    /// the host's transport must resolve them to loopback.
    /// </summary>
    public static bool IsSecure(Uri uri) => TryGetHttpHost(uri, out var host) && IsSecure(uri.Scheme, host);

    internal static bool IsSecure(string scheme, string host)
    {
        if (scheme == "https") return true;
        var name = host.TrimEnd('.');
        if (name == "localhost" || name.EndsWith(".localhost", StringComparison.Ordinal)) return true;
        return IPAddress.TryParse(host, out var ip) && (ip.AddressFamily == AddressFamily.InterNetwork
            ? ip.GetAddressBytes()[0] == 127 : ip.Equals(IPAddress.IPv6Loopback));
    }

    /// <summary>The canonical request host, then (for domains) every parent domain it can domain-match.</summary>
    internal static IEnumerable<string> DomainMatchCandidates(string host)
    {
        yield return host;
        if (IsIp(host)) yield break;
        for (var dot = host.IndexOf('.'); dot >= 0 && dot < host.Length - 1; dot = host.IndexOf('.', dot + 1))
            yield return host[(dot + 1)..];
    }

    private static bool IsForbiddenDomainCodePoint(char c) =>
        c <= ' ' || c is '#' or '%' or '/' or ':' or '<' or '>' or '?' or '@' or '[' or '\\' or ']' or '^' or '|' or '\u007f';

    private static bool EndsInNumber(string name)
    {
        var last = name.AsSpan(name.LastIndexOf('.') + 1);
        return !last.ContainsAnyExceptInRange('0', '9') ||
            (last.Length >= 2 && last[0] == '0' && last[1] is 'x' or 'X' && !last[2..].ContainsAnyExcept(HexDigits));
    }

    private static bool TryParseIPv4(string name, out string host)
    {
        host = "";
        var parts = name.Split('.');
        if (parts.Length > 4) return false;
        var numbers = new ulong[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            if (!TryParseIPv4Number(parts[i], out numbers[i])) return false;
        if (numbers[..^1].Any(n => n > 255) || numbers[^1] >= 1UL << (8 * (5 - numbers.Length))) return false;
        var address = numbers[^1];
        for (var i = 0; i < numbers.Length - 1; i++) address += numbers[i] << (8 * (3 - i));
        host = $"{address >> 24}.{(address >> 16) & 255}.{(address >> 8) & 255}.{address & 255}";
        return true;
    }

    private static bool TryParseIPv4Number(string text, out ulong value)
    {
        value = 0;
        if (text.Length == 0) return false;
        var radix = 10u;
        if (text.Length >= 2 && text[0] == '0' && text[1] is 'x' or 'X') (text, radix) = (text[2..], 16);
        else if (text.Length >= 2 && text[0] == '0') (text, radix) = (text[1..], 8);
        foreach (var c in text)
        {
            var digit = char.IsAsciiDigit(c) ? (uint)(c - '0') : char.IsAsciiHexDigit(c) ? (uint)((c | 0x20) - 'a' + 10) : uint.MaxValue;
            if (digit >= radix) return false;
            // Saturate: every value of 2^32 or more fails the range checks.
            value = Math.Min(value * radix + digit, 1UL << 32);
        }
        return true;
    }
}

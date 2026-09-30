// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   29
// Annotated:        29/29
// Exempt:           5
// Human-reviewed:   0/29
// IP risk:          Low
// Security risk:    High
// Criteria:         28/25
// Resource impact:  5/10 max
// Unverified:       29
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Buffers;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Broiler.Net.Sites;

/// <summary>A schemeful site. Ports belong to origins, not sites.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=0248F9
// Broiler-Falsified-If: two SchemefulSite values with the same host but the schemes "http" and "https" compare equal
// Broiler-Human:        PENDING
public sealed record SchemefulSite
{
    public string Scheme { get; }
    public string Host { get; }
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=A308D5
    // Broiler-Human:        PENDING
    internal SchemefulSite(string scheme, string host) => (Scheme, Host) = (scheme, host);
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=81E8BC
    // Broiler-Falsified-If: a site with scheme https and host example.com prints anything but "https://example.com"
    // Broiler-Human:        PENDING
    public override string ToString() => $"{Scheme}://{Host}";
}

// Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=6633F4
// Broiler-Falsified-If: an implementation returns a public suffix such as co.uk as the registrable domain of a host below it
// Broiler-Human:        PENDING
public interface ISiteResolver
{
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=40D4C8
    // Broiler-Falsified-If: an implementation returns equal sites for https://a.example/ and http://a.example/
    // Broiler-Human:        PENDING
    SchemefulSite? GetSite(Uri url);
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=4F36F7
    // Broiler-Falsified-If: an implementation reports co.uk as not a public suffix, so a cookie with a Domain attribute of co.uk is accepted
    // Broiler-Human:        PENDING
    bool IsPublicSuffix(string canonicalHost);
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=C32265
    // Broiler-Falsified-If: an implementation returns co.uk instead of example.co.uk as the registrable domain of www.example.co.uk
    // Broiler-Human:        PENDING
    string? GetRegistrableDomain(string canonicalHost);
}

/// <summary>Immutable PSL resolver including private domains, wildcard rules and exceptions.</summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=3793E1
// Broiler-Falsified-If: with the bundled list GetRegistrableDomain("a.b.example.co.uk") returns anything other than "example.co.uk"
// Broiler-Human:        PENDING
public sealed class SiteResolver : ISiteResolver
{
    private readonly HashSet<string> _exact = new(StringComparer.Ordinal);
    private readonly HashSet<string> _wildcards = new(StringComparer.Ordinal);
    private readonly HashSet<string> _exceptions = new(StringComparer.Ordinal);
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=3; Fingerprint=9617D0
    // Broiler-Falsified-If: the Lazy is built in a mode other than ExecutionAndPublication, so concurrent first reads of Default can run LoadBundled more than once
    // Broiler-Human:        PENDING
    private static readonly Lazy<SiteResolver> Bundled = new(LoadBundled);
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=3; Fingerprint=C4EB23
    // Broiler-Falsified-If: two threads reading Default at the same time on first use receive different SiteResolver instances
    // Broiler-Human:        PENDING
    public static SiteResolver Default => Bundled.Value;

    /// <summary>Reads the PSL format: each line up to its first whitespace; lines starting with // are comments.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=4; Fingerprint=0EFE1F
    // Broiler-Falsified-If: the exception rule line "!www.ck" is stored anywhere other than as "www.ck" in the exception set
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=9C2F7D
    // Broiler-Falsified-If: the resource is not decoded as UTF-8, so IsPublicSuffix("xn--p1ai") on Default returns false although the list carries that TLD as a Unicode rule
    // Broiler-Human:        PENDING
    private static SiteResolver LoadBundled()
    {
        using var stream = typeof(SiteResolver).Assembly.GetManifestResourceStream("Broiler.Net.public_suffix_list.dat")
            ?? throw new InvalidOperationException("Bundled Public Suffix List is missing.");
        using var reader = new StreamReader(stream);
        return new SiteResolver(reader);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=C4D380
    // Broiler-Falsified-If: with the bundled list, GetSite for https://a.example.co.uk/ and https://b.example.co.uk/ returns unequal sites
    // Broiler-Human:        PENDING
    public SchemefulSite? GetSite(Uri url)
    {
        if (!HostNames.TryGetHttpHost(url, out var host)) return null;
        return new SchemefulSite(url.Scheme, GetRegistrableDomain(host) ?? host);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=4DE49B
    // Broiler-Falsified-If: IsPublicSuffix("www.ck") returns true although the bundled list has the exception rule !www.ck
    // Broiler-Human:        PENDING
    public bool IsPublicSuffix(string canonicalHost)
    {
        if (!HostNames.TryCanonicalize(canonicalHost, out var host) || HostNames.IsIp(host)) return false;
        return host.TrimEnd('.').Split('.').Length == SuffixLabels(host);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=22AA41
    // Broiler-Falsified-If: under the bundled wildcard rule *.ck, GetRegistrableDomain("a.b.foo.ck") returns anything but "b.foo.ck"
    // Broiler-Human:        PENDING
    public string? GetRegistrableDomain(string canonicalHost)
    {
        if (!HostNames.TryCanonicalize(canonicalHost, out var host) || HostNames.IsIp(host)) return null;
        var labels = host.TrimEnd('.').Split('.');
        var count = SuffixLabels(host);
        if (labels.Length <= count) return null;
        return string.Join('.', labels.AsSpan(labels.Length - count - 1).ToArray()) + (host.EndsWith('.') ? "." : "");
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=DF17A7
    // Broiler-Falsified-If: the single-label host "ck", matched only by the wildcard rule *.ck, gets a suffix count of 2 instead of 1
    // Broiler-Human:        PENDING
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
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=863BF4
// Broiler-Falsified-If: a host containing a forbidden domain code point, such as "a^b.example", canonicalizes successfully
// Broiler-Human:        PENDING
public static class HostNames
{
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=1; Fingerprint=3CBAC5
    // Broiler-Falsified-If: a hex letter from a to f is missing from the set, so the host "1.0xff" stays a domain instead of canonicalizing to "1.0.0.255"
    // Broiler-Human:        PENDING
    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdefABCDEF");

    /// <summary>False for non-HTTP(S) URLs and for hosts that are URL failures; never throws for an absolute Uri.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=4; Fingerprint=10A1E5
    // Broiler-Falsified-If: the absolute URL ws://example.com/ returns true with a host
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=URL s3.5; IP=Low; Security=High; Resources=4; Fingerprint=775591
    // Broiler-Falsified-If: the host "0x7f.1" canonicalizes to anything other than "127.0.0.1"
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=FED054
    // Broiler-Falsified-If: the canonical IPv4 host "10.0.0.1" is reported as not an IP, so GetRegistrableDomain returns "0.1" for it
    // Broiler-Human:        PENDING
    public static bool IsIp(string host) => IPAddress.TryParse(host, out _);

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.1.3; IP=Low; Security=High; Resources=2; Fingerprint=9B8A00
    // Broiler-Falsified-If: DomainMatches("evilexample.com", "example.com") returns true
    // Broiler-Human:        PENDING
    public static bool DomainMatches(string host, string domain) =>
        host == domain || (!IsIp(host) && host.EndsWith('.' + domain, StringComparison.Ordinal));

    /// <summary>
    /// HTTPS, or HTTP to 127.0.0.0/8, ::1, localhost or *.localhost. The localhost names qualify only because
    /// the host's transport must resolve them to loopback.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=4; Fingerprint=485A05
    // Broiler-Falsified-If: http://localhost.evil.example/ is reported as a secure context
    // Broiler-Human:        PENDING
    public static bool IsSecure(Uri uri) => TryGetHttpHost(uri, out var host) && IsSecure(uri.Scheme, host);

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=8BA7B0
    // Broiler-Falsified-If: an http host that is not loopback, such as "notlocalhost" or "128.0.0.1", returns true
    // Broiler-Human:        PENDING
    internal static bool IsSecure(string scheme, string host)
    {
        if (scheme == "https") return true;
        var name = host.TrimEnd('.');
        if (name == "localhost" || name.EndsWith(".localhost", StringComparison.Ordinal)) return true;
        return IPAddress.TryParse(host, out var ip) && (ip.AddressFamily == AddressFamily.InterNetwork
            ? ip.GetAddressBytes()[0] == 127 : ip.Equals(IPAddress.IPv6Loopback));
    }

    /// <summary>The canonical request host, then (for domains) every parent domain it can domain-match.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=5; Fingerprint=327BBC
    // Broiler-Falsified-If: the IP host "10.0.0.1" yields a parent candidate such as "0.0.1"
    // Broiler-Human:        PENDING
    internal static IEnumerable<string> DomainMatchCandidates(string host)
    {
        yield return host;
        if (IsIp(host)) yield break;
        for (var dot = host.IndexOf('.'); dot >= 0 && dot < host.Length - 1; dot = host.IndexOf('.', dot + 1))
            yield return host[(dot + 1)..];
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=D00D51
    // Broiler-Falsified-If: one of the characters '^', '%' or U+007F returns false
    // Broiler-Human:        PENDING
    private static bool IsForbiddenDomainCodePoint(char c) =>
        c <= ' ' || c is '#' or '%' or '/' or ':' or '<' or '>' or '?' or '@' or '[' or '\\' or ']' or '^' or '|' or '\u007f';

    // Broiler-AI:           Origin=AI; Spec=URL s3.5; IP=Low; Security=High; Resources=1; Fingerprint=C93B17
    // Broiler-Falsified-If: the host "a.0x1f" is kept as a domain because its last label is not seen as a number
    // Broiler-Human:        PENDING
    private static bool EndsInNumber(string name)
    {
        var last = name.AsSpan(name.LastIndexOf('.') + 1);
        return !last.ContainsAnyExceptInRange('0', '9') ||
            (last.Length >= 2 && last[0] == '0' && last[1] is 'x' or 'X' && !last[2..].ContainsAnyExcept(HexDigits));
    }

    // Broiler-AI:           Origin=AI; Spec=URL s3.5; IP=Low; Security=High; Resources=3; Fingerprint=FC9FA6
    // Broiler-Falsified-If: a four-part host with a part above 255, such as "256.0.0.1", yields an address instead of failing
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=URL s3.5; IP=Low; Security=High; Resources=3; Fingerprint=36D153
    // Broiler-Falsified-If: an octal part containing 8 or 9, such as "09", parses instead of failing
    // Broiler-Human:        PENDING
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

using System.Buffers;
using System.Text;

namespace Broiler.Net.Http;

/// <summary>Fetch Standard header rules shared by the transport and script bindings.</summary>
public static class FetchHeaders
{
    private static readonly HashSet<string> ForbiddenRequestHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept-Charset", "Accept-Encoding", "Access-Control-Request-Headers", "Access-Control-Request-Method", "Connection",
        "Content-Length", "Cookie", "Cookie2", "Date", "DNT", "Expect", "Host", "Keep-Alive", "Origin", "Referer", "Set-Cookie",
        "TE", "Trailer", "Transfer-Encoding", "Upgrade", "Via",
    };
    private static readonly HashSet<string> MethodOverrideHeaders = new(StringComparer.OrdinalIgnoreCase)
        { "X-HTTP-Method", "X-HTTP-Method-Override", "X-Method-Override" };
    private static readonly HashSet<string> SafelistedResponseHeaders = new(StringComparer.OrdinalIgnoreCase)
        { "Cache-Control", "Content-Language", "Content-Length", "Content-Type", "Expires", "Last-Modified", "Pragma" };
    private static readonly HashSet<string> NoCorsSafelistedNames = new(StringComparer.OrdinalIgnoreCase)
        { "Accept", "Accept-Language", "Content-Language", "Content-Type" };
    // Headers Fetch itself appends to no-cors requests that are not forbidden names (Range is its privileged no-CORS header).
    private static readonly HashSet<string> NoCorsUserAgentHeaders = new(StringComparer.OrdinalIgnoreCase)
        { "User-Agent", "Range", "Cache-Control", "Pragma" };
    private static readonly string[] StandardMethods = ["DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT"];
    private const string Alphanumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private static readonly SearchValues<char> TokenChars = SearchValues.Create("!#$%&'*+-.^_`|~" + Alphanumeric);
    private static readonly SearchValues<char> LanguageChars = SearchValues.Create(" *,-.;=" + Alphanumeric);
    private const string HttpWhitespace = " \t\r\n";
    private const int MaximumSafelistedValueLength = 128, MaximumSafelistedTotalLength = 1024;

    /// <summary>Fetch "forbidden request-header": bindings drop these from script-authored requests.</summary>
    public static bool IsForbiddenRequestHeader(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        if (ForbiddenRequestHeaders.Contains(name) || name.StartsWith("proxy-", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("sec-", StringComparison.OrdinalIgnoreCase)) return true;
        return MethodOverrideHeaders.Contains(name) && GetDecodeSplit(value).Any(IsForbiddenMethod);
    }

    /// <summary>Fetch "header name": a non-empty token.</summary>
    public static bool IsHeaderName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Length > 0 && !name.AsSpan().ContainsAnyExcept(TokenChars);
    }

    /// <summary>Fetch "header value" as Latin-1 octets: no leading or trailing tab or space, and no NUL, CR, LF or
    /// character above U+00FF. Bindings normalize first (<see cref="NormalizeHeaderValue"/>) and throw a TypeError otherwise.</summary>
    public static bool IsHeaderValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > 0 && (value[0] is ' ' or '\t' || value[^1] is ' ' or '\t')) return false;
        foreach (var c in value)
            if (c is '\0' or '\r' or '\n' or > '\xFF') return false;
        return true;
    }

    /// <summary>Fetch "normalize": strips leading and trailing HTTP whitespace (tab, space, CR, LF).</summary>
    public static string NormalizeHeaderValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.AsSpan().Trim(HttpWhitespace).ToString();
    }

    /// <summary>Fetch "no-CORS-safelisted request-header": Accept, Accept-Language, Content-Language or Content-Type with a
    /// CORS-safelisted value. Bindings keep only these on script-authored no-cors requests (the request-no-cors guard).</summary>
    public static bool IsNoCorsSafelistedRequestHeader(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        return NoCorsSafelistedNames.Contains(name) && IsCorsSafelistedRequestHeader(name, value);
    }

    /// <summary>Set-Cookie and Set-Cookie2 (Fetch "forbidden response-header name").</summary>
    public static bool IsForbiddenResponseHeaderName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) || name.Equals("Set-Cookie2", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>CONNECT, TRACE and TRACK (case-insensitive).</summary>
    public static bool IsForbiddenMethod(string method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method.Equals("CONNECT", StringComparison.OrdinalIgnoreCase) || method.Equals("TRACE", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("TRACK", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Upper-cases DELETE, GET, HEAD, OPTIONS, POST and PUT case-insensitively; other methods are unchanged.</summary>
    public static string NormalizeMethod(string method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return Array.Find(StandardMethods, m => m.Equals(method, StringComparison.OrdinalIgnoreCase)) ?? method;
    }

    /// <summary>GET, HEAD and POST.</summary>
    public static bool IsCorsSafelistedMethod(string method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method is "GET" or "HEAD" or "POST";
    }

    /// <summary>Fetch "CORS-safelisted request-header" for one name/value pair.</summary>
    public static bool IsCorsSafelistedRequestHeader(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > MaximumSafelistedValueLength) return false;
        return name.ToLowerInvariant() switch
        {
            "accept" => !HasCorsUnsafeByte(value),
            "accept-language" or "content-language" => !value.AsSpan().ContainsAnyExcept(LanguageChars),
            "content-type" => !HasCorsUnsafeByte(value) &&
                GetMimeEssence(value) is "application/x-www-form-urlencoded" or "multipart/form-data" or "text/plain",
            "range" => IsSafelistedRange(value),
            _ => false,
        };
    }

    /// <summary>Fetch filtered responses: basic drops forbidden response-header names; cors keeps safelisted names
    /// plus Access-Control-Expose-Headers ('*' only when not credentialed); opaque and opaque-redirect expose nothing.</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> FilterResponseHeaders(
        IReadOnlyList<KeyValuePair<string, string>> headers, ResponseTainting tainting, bool credentialed)
    {
        ArgumentNullException.ThrowIfNull(headers);
        switch (tainting)
        {
            case ResponseTainting.Basic:
                return Array.AsReadOnly(headers.Where(h => !IsForbiddenResponseHeaderName(h.Key)).ToArray());
            case ResponseTainting.Cors:
                var exposed = ParseTokenList(headers.Where(h => h.Key.Equals("Access-Control-Expose-Headers", StringComparison.OrdinalIgnoreCase))
                    .Select(h => h.Value)) ?? [];
                var all = !credentialed && exposed.Contains("*");
                return Array.AsReadOnly(headers.Where(h => !IsForbiddenResponseHeaderName(h.Key) &&
                    (all || SafelistedResponseHeaders.Contains(h.Key) || exposed.Contains(h.Key, StringComparer.OrdinalIgnoreCase))).ToArray());
            default:
                return [];
        }
    }

    /// <summary>Fetch "CORS-unsafe request-header names": sorted, lower-cased and distinct. Repeated names are
    /// judged on their combined value, which is what goes on the wire. Forbidden names are the user agent's own
    /// headers (Referer, Sec-Fetch-*), which Fetch appends after the preflight decision, so they never count.</summary>
    internal static IReadOnlyList<string> GetCorsUnsafeRequestHeaderNames(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var combined = Combine(headers.Where(h => !IsForbiddenRequestHeader(h.Key, h.Value)));
        var unsafeNames = new List<string>();
        var safelisted = new List<string>();
        var safelistedLength = 0;
        foreach (var (name, value) in combined)
        {
            if (!IsCorsSafelistedRequestHeader(name, value)) unsafeNames.Add(name);
            else { safelisted.Add(name); safelistedLength += value.Length; }
        }
        if (safelistedLength > MaximumSafelistedTotalLength) unsafeNames.AddRange(safelisted);
        return unsafeNames.Select(n => n.ToLowerInvariant()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    /// <summary>Fetch's request-no-cors guard as a transport backstop: drops every header that is neither
    /// no-CORS-safelisted (by combined value) nor the user agent's own (forbidden names, User-Agent, Range,
    /// Cache-Control, Pragma), so a no-cors request never carries headers that would need a preflight.</summary>
    internal static void RemoveNoCorsUnsafe(List<KeyValuePair<string, string>> headers)
    {
        var combined = Combine(headers);
        headers.RemoveAll(h => !IsNoCorsSafelistedRequestHeader(h.Key, combined[h.Key]) &&
            !IsForbiddenRequestHeader(h.Key, h.Value) && !NoCorsUserAgentHeaders.Contains(h.Key));
    }

    private static Dictionary<string, string> Combine(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var combined = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in headers)
            combined[name] = combined.TryGetValue(name, out var existing) ? existing + ", " + value : value;
        return combined;
    }

    /// <summary>A #token list (Access-Control-Allow-Methods/-Headers, -Expose-Headers); null when any item is not a token.</summary>
    internal static List<string>? ParseTokenList(IEnumerable<string> values)
    {
        var items = new List<string>();
        foreach (var value in values)
            foreach (var item in value.Split(','))
            {
                var token = item.Trim(' ', '\t');
                if (token.Length == 0) continue;
                if (token.AsSpan().ContainsAnyExcept(TokenChars)) return null;
                items.Add(token);
            }
        return items;
    }

    /// <summary>Fetch "get, decode, and split": commas inside quoted strings do not split.</summary>
    internal static List<string> GetDecodeSplit(string input)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var position = 0;
        while (true)
        {
            var start = position;
            while (position < input.Length && input[position] is not ('"' or ',')) position++;
            value.Append(input, start, position - start);
            if (position < input.Length && input[position] == '"')
            {
                AppendQuotedString(input, ref position, value);
                if (position < input.Length) continue;
            }
            values.Add(value.ToString().Trim(' ', '\t'));
            value.Clear();
            if (position >= input.Length) return values;
            position++;
        }
    }

    // Fetch "collect an HTTP quoted string" with extract-value false: the quotes and escapes are kept.
    private static void AppendQuotedString(string input, ref int position, StringBuilder value)
    {
        var start = position++;
        while (true)
        {
            while (position < input.Length && input[position] is not ('"' or '\\')) position++;
            if (position >= input.Length) break;
            if (input[position++] != '\\') break;
            if (position >= input.Length) break;
            position++;
        }
        value.Append(input, start, position - start);
    }

    private static bool HasCorsUnsafeByte(string value)
    {
        foreach (var c in value)
            if (c is (< ' ' and not '\t') or '"' or '(' or ')' or ':' or '<' or '>' or '?' or '@' or '[' or '\\' or ']' or '{' or '}' or '\x7F' or > '\xFF')
                return true;
        return false;
    }

    // MIME Sniffing "parse a MIME type", reduced to the essence; parameters never make parsing fail.
    private static string? GetMimeEssence(string value)
    {
        var input = value.AsSpan().Trim(HttpWhitespace);
        var slash = input.IndexOf('/');
        if (slash <= 0) return null;
        var type = input[..slash];
        var rest = input[(slash + 1)..];
        var semicolon = rest.IndexOf(';');
        var subtype = (semicolon < 0 ? rest : rest[..semicolon]).TrimEnd(HttpWhitespace);
        if (type.ContainsAnyExcept(TokenChars) || subtype.IsEmpty || subtype.ContainsAnyExcept(TokenChars)) return null;
        return string.Concat(type, "/", subtype).ToLowerInvariant();
    }

    // Fetch "parse a single range header value" without whitespace; suffix ranges (bytes=-N) are not safelisted.
    private static bool IsSafelistedRange(string value)
    {
        if (!value.StartsWith("bytes=", StringComparison.Ordinal)) return false;
        var range = value.AsSpan(6);
        var dash = range.IndexOf('-');
        if (dash <= 0) return false;
        var start = range[..dash];
        var end = range[(dash + 1)..];
        if (start.ContainsAnyExceptInRange('0', '9') || end.ContainsAnyExceptInRange('0', '9')) return false;
        return end.IsEmpty || CompareDecimal(start, end) <= 0;
    }

    private static int CompareDecimal(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        a = a.TrimStart('0');
        b = b.TrimStart('0');
        return a.Length != b.Length ? a.Length.CompareTo(b.Length) : a.SequenceCompareTo(b);
    }
}

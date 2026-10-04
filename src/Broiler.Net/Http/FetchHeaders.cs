// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   32
// Annotated:        32/32
// Exempt:           0
// Human-reviewed:   0/32
// IP risk:          Low
// Security risk:    High
// Criteria:         32/29
// Resource impact:  5/10 max
// Unverified:       32
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Buffers;
using System.Text;

namespace Broiler.Net.Http;

/// <summary>Fetch Standard header rules shared by the transport and script bindings.</summary>
// Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=5; Fingerprint=5255E0
// Broiler-Falsified-If: a header name differing only in letter case from a listed one, such as cOOKIE, is classified differently from its canonical spelling
// Broiler-Human:        PENDING
public static class FetchHeaders
{
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=None; Security=High; Resources=1; Fingerprint=4F997C
    // Broiler-Falsified-If: a name on Fetch's forbidden request-header list, such as Access-Control-Request-Method or Keep-Alive, is missing from the set
    // Broiler-Human:        PENDING
    private static readonly HashSet<string> ForbiddenRequestHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept-Charset", "Accept-Encoding", "Access-Control-Request-Headers", "Access-Control-Request-Method", "Connection",
        "Content-Length", "Cookie", "Cookie2", "Date", "DNT", "Expect", "Host", "Keep-Alive", "Origin", "Referer", "Set-Cookie",
        "TE", "Trailer", "Transfer-Encoding", "Upgrade", "Via",
    };
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=None; Security=High; Resources=1; Fingerprint=C35A55
    // Broiler-Falsified-If: X-HTTP-Method, X-HTTP-Method-Override or X-Method-Override is missing from the set, so a TRACE override passes as an ordinary header
    // Broiler-Human:        PENDING
    private static readonly HashSet<string> MethodOverrideHeaders = new(StringComparer.OrdinalIgnoreCase)
        { "X-HTTP-Method", "X-HTTP-Method-Override", "X-Method-Override" };
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=None; Security=High; Resources=1; Fingerprint=EA8CA6
    // Broiler-Falsified-If: a name other than Cache-Control, Content-Language, Content-Length, Content-Type, Expires, Last-Modified and Pragma is in the set, so a CORS response exposes it without Access-Control-Expose-Headers
    // Broiler-Human:        PENDING
    private static readonly HashSet<string> SafelistedResponseHeaders = new(StringComparer.OrdinalIgnoreCase)
        { "Cache-Control", "Content-Language", "Content-Length", "Content-Type", "Expires", "Last-Modified", "Pragma" };
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=None; Security=High; Resources=1; Fingerprint=D0302E
    // Broiler-Falsified-If: a name other than Accept, Accept-Language, Content-Language and Content-Type is in the set, so a no-cors request keeps a header that needs a preflight
    // Broiler-Human:        PENDING
    private static readonly HashSet<string> NoCorsSafelistedNames = new(StringComparer.OrdinalIgnoreCase)
        { "Accept", "Accept-Language", "Content-Language", "Content-Type" };
    // Headers Fetch itself appends to no-cors requests that are not forbidden names (Range is its privileged no-CORS header).
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=EB37B1
    // Broiler-Falsified-If: a name other than User-Agent, Range, Cache-Control and Pragma is in the set, so a no-cors request keeps it whatever its value
    // Broiler-Human:        PENDING
    private static readonly HashSet<string> NoCorsUserAgentHeaders = new(StringComparer.OrdinalIgnoreCase)
        { "User-Agent", "Range", "Cache-Control", "Pragma" };
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.1; IP=None; Security=None; Resources=1; Fingerprint=C8C07F
    // Broiler-Falsified-If: a method other than DELETE, GET, HEAD, OPTIONS, POST and PUT is in the array, so NormalizeMethod upper-cases it
    // Broiler-Human:        PENDING
    private static readonly string[] StandardMethods = ["DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT"];
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=672C2C
    // Broiler-Falsified-If: a character other than an ASCII digit or letter is in the string, widening both the token and the language character sets
    // Broiler-Human:        PENDING
    private const string Alphanumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    // Broiler-AI:           Origin=AI; Spec=RFC-9110 s5.6.2; IP=None; Security=High; Resources=1; Fingerprint=462BE1
    // Broiler-Falsified-If: a character outside tchar, such as ':', '/' or a space, is in the set, so IsHeaderName accepts a name carrying it
    // Broiler-Human:        PENDING
    private static readonly SearchValues<char> TokenChars = SearchValues.Create("!#$%&'*+-.^_`|~" + Alphanumeric);
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=None; Security=High; Resources=1; Fingerprint=593472
    // Broiler-Falsified-If: a character outside digits, letters, space and the marks *,-.;= such as a double quote is in the set, so an Accept-Language value carrying it skips the preflight
    // Broiler-Human:        PENDING
    private static readonly SearchValues<char> LanguageChars = SearchValues.Create(" *,-.;=" + Alphanumeric);
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=5F478C
    // Broiler-Falsified-If: the string holds anything other than space, tab, CR and LF, so NormalizeHeaderValue strips a character that belongs to the value
    // Broiler-Human:        PENDING
    private const string HttpWhitespace = " \t\r\n";
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=None; Security=High; Resources=0; Fingerprint=B0BA12
    // Broiler-Falsified-If: either limit differs from Fetch's 128-byte limit on one value or its 1024-byte limit on the combined safelisted values
    // Broiler-Human:        PENDING
    private const int MaximumSafelistedValueLength = 128, MaximumSafelistedTotalLength = 1024;

    /// <summary>Fetch "forbidden request-header": bindings drop these from script-authored requests.</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=3; Fingerprint=CDBBC3
    // Broiler-Falsified-If: X-HTTP-Method-Override with the value 'GET, trace' is reported not forbidden
    // Broiler-Human:        PENDING
    public static bool IsForbiddenRequestHeader(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        if (ForbiddenRequestHeaders.Contains(name) || name.StartsWith("proxy-", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("sec-", StringComparison.OrdinalIgnoreCase)) return true;
        return MethodOverrideHeaders.Contains(name) && GetDecodeSplit(value).Any(IsForbiddenMethod);
    }

    /// <summary>Fetch "header name": a non-empty token.</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=3; Fingerprint=E960E4
    // Broiler-Falsified-If: a name containing ':' or a space is accepted as a header name
    // Broiler-Human:        PENDING
    public static bool IsHeaderName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Length > 0 && !name.AsSpan().ContainsAnyExcept(TokenChars);
    }

    /// <summary>Fetch "header value" as Latin-1 octets: no leading or trailing tab or space, and no NUL, CR, LF or
    /// character above U+00FF. Bindings normalize first (<see cref="NormalizeHeaderValue"/>) and throw a TypeError otherwise.</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=3; Fingerprint=04A208
    // Broiler-Falsified-If: a value with an embedded LF is accepted, which would put a second header line on the wire
    // Broiler-Human:        PENDING
    public static bool IsHeaderValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > 0 && (value[0] is ' ' or '\t' || value[^1] is ' ' or '\t')) return false;
        foreach (var c in value)
            if (c is '\0' or '\r' or '\n' or > '\xFF') return false;
        return true;
    }

    /// <summary>Fetch "normalize": strips leading and trailing HTTP whitespace (tab, space, CR, LF).</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=3; Fingerprint=334CB0
    // Broiler-Falsified-If: a value ending in CR LF still ends in CR after normalization
    // Broiler-Human:        PENDING
    public static string NormalizeHeaderValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.AsSpan().Trim(HttpWhitespace).ToString();
    }

    /// <summary>Fetch "no-CORS-safelisted request-header": Accept, Accept-Language, Content-Language or Content-Type with a
    /// CORS-safelisted value. Bindings keep only these on script-authored no-cors requests (the request-no-cors guard).</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=2; Fingerprint=C95AFE
    // Broiler-Falsified-If: a safelisted Range value such as bytes=0-10 is reported no-CORS-safelisted
    // Broiler-Human:        PENDING
    public static bool IsNoCorsSafelistedRequestHeader(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        return NoCorsSafelistedNames.Contains(name) && IsCorsSafelistedRequestHeader(name, value);
    }

    /// <summary>Set-Cookie and Set-Cookie2 (Fetch "forbidden response-header name").</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=0; Fingerprint=F98A02
    // Broiler-Falsified-If: a lower-case set-cookie2 response header name is not reported forbidden
    // Broiler-Human:        PENDING
    public static bool IsForbiddenResponseHeaderName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) || name.Equals("Set-Cookie2", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>CONNECT, TRACE and TRACK (case-insensitive).</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.1; IP=Low; Security=High; Resources=0; Fingerprint=0922C4
    // Broiler-Falsified-If: the method 'track' in lower case is not reported forbidden
    // Broiler-Human:        PENDING
    public static bool IsForbiddenMethod(string method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method.Equals("CONNECT", StringComparison.OrdinalIgnoreCase) || method.Equals("TRACE", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("TRACK", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Upper-cases DELETE, GET, HEAD, OPTIONS, POST and PUT case-insensitively; other methods are unchanged.</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.1; IP=Low; Security=Medium; Resources=1; Fingerprint=38A699
    // Broiler-Falsified-If: normalizing 'patch' returns 'PATCH' rather than leaving it unchanged
    // Broiler-Human:        PENDING
    public static string NormalizeMethod(string method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return Array.Find(StandardMethods, m => m.Equals(method, StringComparison.OrdinalIgnoreCase)) ?? method;
    }

    /// <summary>GET, HEAD and POST.</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.1; IP=Low; Security=High; Resources=0; Fingerprint=1A2CA8
    // Broiler-Falsified-If: a lower-case 'post' is reported CORS-safelisted
    // Broiler-Human:        PENDING
    public static bool IsCorsSafelistedMethod(string method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method is "GET" or "HEAD" or "POST";
    }

    /// <summary>Fetch "CORS-safelisted request-header" for one name/value pair.</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=3; Fingerprint=1D24E1
    // Broiler-Falsified-If: a Content-Type of application/json is reported CORS-safelisted
    // Broiler-Human:        PENDING
    public static bool IsCorsSafelistedRequestHeader(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > MaximumSafelistedValueLength) return false;
        return name.ToLowerInvariant() switch
        {
            "accept" => !HasCorsUnsafeByte(value),
            "accept-language" or "content-language" => !value.AsSpan().ContainsAnyExcept(LanguageChars),
            "content-type" => !HasCorsUnsafeByte(value) && MimeType.TryParse(value, out var mimeType) &&
                mimeType.Essence is "application/x-www-form-urlencoded" or "multipart/form-data" or "text/plain",
            "range" => IsSafelistedRange(value),
            _ => false,
        };
    }

    /// <summary>Fetch filtered responses: basic drops forbidden response-header names; cors keeps safelisted names
    /// plus Access-Control-Expose-Headers ('*' only when not credentialed); opaque and opaque-redirect expose nothing.</summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.6; IP=Low; Security=High; Resources=5; Fingerprint=A2891A
    // Broiler-Falsified-If: a credentialed CORS response whose Access-Control-Expose-Headers is '*' exposes a non-safelisted header to script
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=5; Fingerprint=FE0830
    // Broiler-Falsified-If: two Accept headers of 100 characters each are not listed as CORS-unsafe although their combined value exceeds 128
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=5; Fingerprint=CBAA8A
    // Broiler-Falsified-If: a no-cors header list keeps a script-set Content-Type of application/json
    // Broiler-Human:        PENDING
    internal static void RemoveNoCorsUnsafe(List<KeyValuePair<string, string>> headers)
    {
        var combined = Combine(headers);
        headers.RemoveAll(h => !IsNoCorsSafelistedRequestHeader(h.Key, combined[h.Key]) &&
            !IsForbiddenRequestHeader(h.Key, h.Value) && !NoCorsUserAgentHeaders.Contains(h.Key));
    }

    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=5; Fingerprint=88867B
    // Broiler-Falsified-If: two headers whose names differ only in letter case produce two entries rather than one value joined with a comma and a space
    // Broiler-Human:        PENDING
    private static Dictionary<string, string> Combine(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var combined = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in headers)
            combined[name] = combined.TryGetValue(name, out var existing) ? existing + ", " + value : value;
        return combined;
    }

    /// <summary>A #token list (Access-Control-Allow-Methods/-Headers, -Expose-Headers); null when any item is not a token.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=9F080E
    // Broiler-Falsified-If: an Access-Control-Allow-Headers item with a space inside it, such as 'X A', yields a list instead of null
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=3; Fingerprint=4A6D27
    // Broiler-Falsified-If: a comma inside a double-quoted string splits the value
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=3; Fingerprint=D7CBC7
    // Broiler-Falsified-If: an unterminated quoted string that ends in a backslash throws or is read past the end of the input
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=3; Fingerprint=4D94DF
    // Broiler-Falsified-If: a value containing 0x7F is reported free of CORS-unsafe request-header bytes
    // Broiler-Human:        PENDING
    private static bool HasCorsUnsafeByte(string value)
    {
        foreach (var c in value)
            if (c is (< ' ' and not '\t') or '"' or '(' or ')' or ':' or '<' or '>' or '?' or '@' or '[' or '\\' or ']' or '{' or '}' or '\x7F' or > '\xFF')
                return true;
        return false;
    }

    // Fetch "parse a single range header value" without whitespace; suffix ranges (bytes=-N) are not safelisted.
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2.2; IP=Low; Security=High; Resources=3; Fingerprint=FC37EF
    // Broiler-Falsified-If: a range whose first position exceeds its last, such as bytes=10-9, is reported safelisted
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=40BFF3
    // Broiler-Falsified-If: '010' compares as smaller than '9'
    // Broiler-Human:        PENDING
    private static int CompareDecimal(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        a = a.TrimStart('0');
        b = b.TrimStart('0');
        return a.Length != b.Length ? a.Length.CompareTo(b.Length) : a.SequenceCompareTo(b);
    }
}

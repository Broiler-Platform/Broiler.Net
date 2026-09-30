// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   20
// Annotated:        20/20
// Exempt:           11
// Human-reviewed:   0/20
// IP risk:          Low
// Security risk:    High
// Criteria:         19/11
// Resource impact:  4/10 max
// Unverified:       20
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Broiler.Net.Cookies;

/// <summary>Byte-preserving parsed fields; strings represent octets using Latin-1.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=939D75
// Broiler-Falsified-If: ToString on a parsed cookie returns text containing the cookie's name or value
// Broiler-Human:        PENDING
public sealed record ParsedCookie
{
    public string Name { get; internal init; } = "";
    public string Value { get; internal init; } = "";
    public string Domain { get; internal init; } = "";
    public string? Path { get; internal init; }
    public bool Secure { get; internal init; }
    public bool HttpOnly { get; internal init; }
    public bool Partitioned { get; internal init; }
    public CookieSameSite SameSite { get; internal init; }
    public DateTimeOffset? Expires { get; internal init; }
    internal ParsedCookie() { }
    public override string ToString() => "ParsedCookie (values omitted)";
}

// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=D63254
// Broiler-Human:        PENDING
public readonly record struct CookieParseResult(ParsedCookie? Cookie, CookieDecision? Rejection)
{
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=6892E9
    // Broiler-Falsified-If: a result with a null Cookie and a Rejection reports Success as true
    // Broiler-Human:        PENDING
    public bool Success => Cookie is not null;
}

/// <summary>6265bis-22 parser. Each call consumes exactly one Set-Cookie field, never a comma list.</summary>
// Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.6; IP=Low; Security=High; Resources=4; Fingerprint=A3F7A7
// Broiler-Falsified-If: a field holding two comma-separated name-value pairs yields a cookie named after the second pair instead of one cookie whose value contains the comma
// Broiler-Human:        PENDING
public static partial class CookieParser
{
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=0B4C5A
    // Broiler-Falsified-If: a Set-Cookie field of 65537 octets is parsed instead of rejected with RejectedSize
    // Broiler-Human:        PENDING
    public const int MaximumFieldBytes = 65536;
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.6; IP=None; Security=Low; Resources=0; Fingerprint=786AB4
    // Broiler-Falsified-If: a cookie whose name and value total 4097 octets is parsed instead of rejected with RejectedSize
    // Broiler-Human:        PENDING
    public const int MaximumNameValueBytes = 4096;
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.6; IP=None; Security=Low; Resources=0; Fingerprint=97AD9B
    // Broiler-Falsified-If: an attribute value of 1025 octets, such as a long Path, is applied to the cookie instead of ignored
    // Broiler-Human:        PENDING
    public const int MaximumAttributeValueBytes = 1024;
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.5; IP=None; Security=Low; Resources=0; Fingerprint=613B35
    // Broiler-Falsified-If: an Expires or Max-Age more than 400 days after the parse time is kept without being clamped
    // Broiler-Human:        PENDING
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromDays(400);

    /// <summary>Accepts an HTTP field decoded with Latin-1 (one character per wire octet).</summary>
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.6; IP=Low; Security=High; Resources=3; Fingerprint=B4FB87
    // Broiler-Falsified-If: a header string containing a character above U+00FF is parsed into a cookie instead of rejected with RejectedEncoding
    // Broiler-Human:        PENDING
    public static CookieParseResult Parse(string header, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(header);
        if (header.Length > MaximumFieldBytes) return Reject(CookieDecision.RejectedSize);
        if (header.Any(c => c > 255)) return Reject(CookieDecision.RejectedEncoding);
        return Parse(Encoding.Latin1.GetBytes(header), now);
    }

    /// <summary>Parses and validates only; storing a cookie needs the store's Latin-1 string API.</summary>
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.6; IP=Low; Security=High; Resources=4; Fingerprint=2A50CF
    // Broiler-Falsified-If: a field containing a NUL, CR, LF or DEL octet yields a parsed cookie instead of RejectedControlCharacter
    // Broiler-Human:        PENDING
    public static CookieParseResult Parse(ReadOnlySpan<byte> field, DateTimeOffset now)
    {
        // The overflow guards compare instants, so the arithmetic must run on UTC clock time too.
        now = now.ToUniversalTime();
        if (field.Length > MaximumFieldBytes) return Reject(CookieDecision.RejectedSize);
        foreach (var octet in field)
            if (octet is <= 8 or >= 10 and <= 31 or 127) return Reject(CookieDecision.RejectedControlCharacter);

        var text = Encoding.Latin1.GetString(field);
        var parts = text.Split(';');
        var equals = parts[0].IndexOf('=');
        var name = equals < 0 ? "" : Trim(parts[0][..equals]);
        var value = Trim(equals < 0 ? parts[0] : parts[0][(equals + 1)..]);
        if (name.Length + value.Length == 0) return Reject(CookieDecision.RejectedEmpty);
        if (name.Length + value.Length > MaximumNameValueBytes) return Reject(CookieDecision.RejectedSize);

        var parsed = new ParsedCookie { Name = name, Value = value };
        DateTimeOffset? expires = null, maxAge = null;
        var limit = now > DateTimeOffset.MaxValue - MaximumLifetime ? DateTimeOffset.MaxValue : now + MaximumLifetime;
        foreach (var part in parts.AsSpan(1))
        {
            equals = part.IndexOf('=');
            var attribute = Trim(equals < 0 ? part : part[..equals]);
            var attributeValue = equals < 0 ? "" : Trim(part[(equals + 1)..]);
            if (attributeValue.Length > MaximumAttributeValueBytes) continue;
            switch (attribute.ToLowerInvariant())
            {
                case "domain":
                    parsed = parsed with { Domain = (attributeValue.StartsWith('.') ? attributeValue[1..] : attributeValue).ToLowerInvariant() };
                    break;
                case "path": parsed = parsed with { Path = attributeValue }; break;
                case "secure": parsed = parsed with { Secure = true }; break;
                case "httponly": parsed = parsed with { HttpOnly = true }; break;
                case "partitioned": parsed = parsed with { Partitioned = true }; break;
                case "samesite":
                    parsed = parsed with { SameSite = attributeValue.ToLowerInvariant() switch
                    { "strict" => CookieSameSite.Strict, "lax" => CookieSameSite.Lax, "none" => CookieSameSite.None, _ => CookieSameSite.Default } };
                    break;
                case "expires":
                    if (TryParseDate(attributeValue, out var date)) expires = date > limit ? limit : date;
                    break;
                case "max-age":
                    if (TryMaxAge(attributeValue, out var seconds))
                        maxAge = seconds <= 0 ? DateTimeOffset.MinValue :
                            now > DateTimeOffset.MaxValue.AddSeconds(-seconds) ? DateTimeOffset.MaxValue : now.AddSeconds(seconds);
                    break;
            }
        }
        return new(parsed with { Expires = maxAge ?? expires }, null);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=2CE582
    // Broiler-Falsified-If: a rejection result carries a non-null Cookie
    // Broiler-Human:        PENDING
    private static CookieParseResult Reject(CookieDecision reason) => new(null, reason);
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.6; IP=None; Security=High; Resources=2; Fingerprint=F5A18B
    // Broiler-Falsified-If: a cookie name sent with a leading space before __Host-id keeps the space, so the __Host- prefix rules do not apply to it
    // Broiler-Human:        PENDING
    private static string Trim(string value) => value.Trim(' ', '\t');

    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.6.2; IP=Low; Security=High; Resources=1; Fingerprint=0DB779
    // Broiler-Falsified-If: a Max-Age of twenty or more digits wraps the long accumulator to a negative or short lifetime instead of stopping at the 400-day cap
    // Broiler-Human:        PENDING
    private static bool TryMaxAge(string text, out long seconds)
    {
        seconds = 0;
        if (text.Length == 0) return false;
        var negative = text[0] == '-';
        var digits = negative ? text.AsSpan(1) : text.AsSpan();
        if (digits.Length == 0) return false;
        foreach (var c in digits)
        {
            if (!char.IsAsciiDigit(c)) return false;
            seconds = Math.Min((long)MaximumLifetime.TotalSeconds, seconds * 10 + c - '0');
        }
        if (negative) seconds = 0;
        return true;
    }

    // Cookie dates deliberately do not use culture-sensitive DateTime parsing or RFC1123 alone.
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.1.1; IP=Low; Security=High; Resources=2; Fingerprint=FB28D2
    // Broiler-Falsified-If: a cookie date naming 30 February returns true or throws instead of returning false
    // Broiler-Human:        PENDING
    public static bool TryParseDate(string text, out DateTimeOffset result)
    {
        ArgumentNullException.ThrowIfNull(text);
        result = default;
        if (text.Length > MaximumAttributeValueBytes) return false;
        int? day = null, month = null, year = null, hour = null;
        var minute = 0; var second = 0;
        foreach (var token in DateDelimiters().Split(text))
        {
            if (hour is null && TimeToken().Match(token) is { Success: true } time)
            {
                hour = Number(time.Groups[1].Value); minute = Number(time.Groups[2].Value); second = Number(time.Groups[3].Value);
                continue;
            }
            if (day is null && DayToken().Match(token) is { Success: true } dayMatch)
            { day = Number(dayMatch.Groups[1].Value); continue; }
            if (month is null && token.Length >= 3)
            {
                var index = Array.IndexOf(Months, token[..3].ToLowerInvariant());
                if (index >= 0) { month = index + 1; continue; }
            }
            if (year is null && YearToken().Match(token) is { Success: true } yearMatch)
                year = Number(yearMatch.Groups[1].Value);
        }
        if (year is >= 70 and <= 99) year += 1900;
        else if (year is >= 0 and <= 69) year += 2000;
        if (day is null or < 1 or > 31 || month is null || year is null or < 1601 || hour is null or > 23 || minute > 59 || second > 59)
            return false;
        try { result = new DateTimeOffset(year.Value, month.Value, day.Value, hour.Value, minute, second, TimeSpan.Zero); return true; }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=078ECE
    // Broiler-Falsified-If: a digit group captured by the date regexes makes Number throw, so TryParseDate raises an exception instead of returning false
    // Broiler-Human:        PENDING
    private static int Number(string text) => int.Parse(text, CultureInfo.InvariantCulture);
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.1.1; IP=None; Security=None; Resources=0; Fingerprint=C362EA
    // Broiler-Falsified-If: a month token such as sep or SEPT resolves to a month other than 9
    // Broiler-Human:        PENDING
    private static readonly string[] Months = ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.1.1; IP=Low; Security=High; Resources=2; Fingerprint=9C1A74
    // Broiler-Falsified-If: an octet the cookie-date grammar lists as a delimiter, such as @ (0x40) or ~ (0x7E), stays inside a token instead of separating tokens
    // Broiler-Human:        PENDING
    [GeneratedRegex("[\\x09\\x20-\\x2F\\x3B-\\x40\\x5B-\\x60\\x7B-\\x7E]+", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex DateDelimiters();
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.1.1; IP=Low; Security=High; Resources=2; Fingerprint=AF658F
    // Broiler-Falsified-If: a token with a three-digit field such as 100:00:00 is accepted as the time of day
    // Broiler-Human:        PENDING
    [GeneratedRegex("^([0-9]{1,2}):([0-9]{1,2}):([0-9]{1,2})(?:[^0-9]|$)", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex TimeToken();
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.1.1; IP=Low; Security=High; Resources=2; Fingerprint=35DA3C
    // Broiler-Falsified-If: a three-digit token such as 123 is accepted as the day of month
    // Broiler-Human:        PENDING
    [GeneratedRegex("^([0-9]{1,2})(?:[^0-9]|$)", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex DayToken();
    // Broiler-AI:           Origin=AI; Spec=RFC-6265bis s5.1.1; IP=Low; Security=High; Resources=2; Fingerprint=CE2D4B
    // Broiler-Falsified-If: a five-digit token such as 20250 is accepted as the year
    // Broiler-Human:        PENDING
    [GeneratedRegex("^([0-9]{2,4})(?:[^0-9]|$)", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex YearToken();
}

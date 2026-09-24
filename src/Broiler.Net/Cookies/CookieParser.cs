using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Broiler.Net.Cookies;

/// <summary>Byte-preserving parsed fields; strings represent octets using Latin-1.</summary>
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

public readonly record struct CookieParseResult(ParsedCookie? Cookie, CookieDecision? Rejection)
{
    public bool Success => Cookie is not null;
}

/// <summary>6265bis-22 parser. Each call consumes exactly one Set-Cookie field, never a comma list.</summary>
public static partial class CookieParser
{
    public const int MaximumFieldBytes = 65536;
    public const int MaximumNameValueBytes = 4096;
    public const int MaximumAttributeValueBytes = 1024;
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromDays(400);

    /// <summary>Accepts an HTTP field decoded with Latin-1 (one character per wire octet).</summary>
    public static CookieParseResult Parse(string header, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(header);
        if (header.Length > MaximumFieldBytes) return Reject(CookieDecision.RejectedSize);
        if (header.Any(c => c > 255)) return Reject(CookieDecision.RejectedEncoding);
        return Parse(Encoding.Latin1.GetBytes(header), now);
    }

    /// <summary>Parses and validates only; storing a cookie needs the store's Latin-1 string API.</summary>
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

    private static CookieParseResult Reject(CookieDecision reason) => new(null, reason);
    private static string Trim(string value) => value.Trim(' ', '\t');

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

    private static int Number(string text) => int.Parse(text, CultureInfo.InvariantCulture);
    private static readonly string[] Months = ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];
    [GeneratedRegex("[\\x09\\x20-\\x2F\\x3B-\\x40\\x5B-\\x60\\x7B-\\x7E]+", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex DateDelimiters();
    [GeneratedRegex("^([0-9]{1,2}):([0-9]{1,2}):([0-9]{1,2})(?:[^0-9]|$)", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex TimeToken();
    [GeneratedRegex("^([0-9]{1,2})(?:[^0-9]|$)", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex DayToken();
    [GeneratedRegex("^([0-9]{2,4})(?:[^0-9]|$)", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex YearToken();
}

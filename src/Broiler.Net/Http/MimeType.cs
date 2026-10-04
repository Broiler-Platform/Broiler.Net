// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   17
// Annotated:        17/17
// Exempt:           5
// Human-reviewed:   0/17
// IP risk:          Low
// Security risk:    High
// Criteria:         15/15
// Resource impact:  3/10 max
// Unverified:       17
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Broiler.Net.Http;

/// <summary>
/// A MIME type as the MIME Sniffing Standard models it: a type, a subtype and parameters, read with
/// <see cref="TryParse"/> from a <c>Content-Type</c> value, a <c>data:</c> URL or any other MIME type
/// string, and written back with <see cref="ToString"/>.
/// </summary>
/// <remarks>
/// <para>
/// The type, the subtype and parameter names are ASCII-lowercased; parameter values keep their case.
/// Parsing is as forgiving as a browser's: a parameter that is malformed, or that repeats one already
/// read, is dropped rather than failing the whole type, so <c>text/html;charset=utf-8;charset=x</c>
/// reads as <c>text/html;charset=utf-8</c>. Only a missing or malformed type or subtype fails.
/// </para>
/// <para>
/// Case is folded for ASCII letters only, as the standard specifies, and never by the culture-aware or
/// Unicode-wide case mappings of .NET: those fold the Kelvin sign into <c>k</c> and the dotless i into
/// <c>I</c>, which would make names out of strings the standard rejects.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.4; IP=Low; Security=High; Resources=3; Fingerprint=3E1EB0
// Broiler-Falsified-If: a parameter name holding the Kelvin sign, which .NET lowercases to k, is kept as the parameter key
// Broiler-Human:        PENDING
public sealed class MimeType
{
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2; IP=None; Security=High; Resources=0; Fingerprint=3F35BE
    // Broiler-Falsified-If: the string holds a character other than tab, line feed, carriage return and space, such as a form feed, so a type padded with it parses
    // Broiler-Human:        PENDING
    private const string HttpWhitespace = "\t\n\r ";

    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s3; IP=None; Security=High; Resources=1; Fingerprint=F79DD0
    // Broiler-Falsified-If: a character outside the HTTP token code points, such as a colon, a slash or a space, is in the set, so a type or subtype carrying it parses
    // Broiler-Human:        PENDING
    private static readonly SearchValues<char> HttpTokenCodePoints =
        SearchValues.Create("!#$%&'*+-.^_`|~0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.6; IP=None; Security=High; Resources=1; Fingerprint=3A2CFD
    // Broiler-Falsified-If: an essence the standard does not list, such as text/javascript1.6, is in the array, so a response of that type counts as script
    // Broiler-Human:        PENDING
    private static readonly string[] JavaScriptEssences =
    [
        "application/ecmascript", "application/javascript", "application/x-ecmascript", "application/x-javascript",
        "text/ecmascript", "text/javascript", "text/javascript1.0", "text/javascript1.1", "text/javascript1.2",
        "text/javascript1.3", "text/javascript1.4", "text/javascript1.5", "text/jscript", "text/livescript",
        "text/x-ecmascript", "text/x-javascript",
    ];

    private string? _serialization;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=45E2A6
    // Broiler-Human:        PENDING
    private MimeType(string type, string subtype, OrderedDictionary<string, string> parameters)
    {
        Type = type;
        Subtype = subtype;
        Essence = type + "/" + subtype;
        Parameters = new ReadOnlyDictionary<string, string>(parameters);
    }

    /// <summary>The type, such as <c>text</c> in <c>text/html</c>, in ASCII lowercase.</summary>
    public string Type { get; }

    /// <summary>The subtype, such as <c>html</c> in <c>text/html</c>, in ASCII lowercase.</summary>
    public string Subtype { get; }

    /// <summary>The type and subtype without parameters, such as <c>text/html</c>.</summary>
    public string Essence { get; }

    /// <summary>
    /// The parameters, keyed by their ASCII-lowercase names, in the order they were written. A value
    /// written as a quoted string is held without its quotes and escapes.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>The <c>charset</c> parameter as written, or <see langword="null"/> when there is none.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=B52923
    // Broiler-Human:        PENDING
    public string? Charset => Parameters.TryGetValue("charset", out var charset) ? charset : null;

    /// <summary>Whether this is an image MIME type: its type is <c>image</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.6; IP=None; Security=High; Resources=0; Fingerprint=7206AF
    // Broiler-Falsified-If: image/svg+xml is not reported an image MIME type, or imagex/png is
    // Broiler-Human:        PENDING
    public bool IsImage => Type == "image";

    /// <summary>Whether this is an HTML MIME type: its essence is <c>text/html</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.6; IP=None; Security=High; Resources=0; Fingerprint=B9429B
    // Broiler-Falsified-If: text/html;charset=utf-8 is not reported an HTML MIME type, or application/xhtml+xml is
    // Broiler-Human:        PENDING
    public bool IsHtml => Essence == "text/html";

    /// <summary>
    /// Whether this is an XML MIME type: its subtype ends in <c>+xml</c>, or its essence is
    /// <c>text/xml</c> or <c>application/xml</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.6; IP=None; Security=High; Resources=0; Fingerprint=5E58A7
    // Broiler-Falsified-If: application/xhtml+xml is not reported an XML MIME type, or application/xml-dtd is
    // Broiler-Human:        PENDING
    public bool IsXml => Subtype.EndsWith("+xml", StringComparison.Ordinal) || Essence is "text/xml" or "application/xml";

    /// <summary>
    /// Whether this is a JSON MIME type: its subtype ends in <c>+json</c>, or its essence is
    /// <c>application/json</c> or <c>text/json</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.6; IP=None; Security=High; Resources=0; Fingerprint=518FD9
    // Broiler-Falsified-If: application/ld+json is not reported a JSON MIME type, or application/jsonp is
    // Broiler-Human:        PENDING
    public bool IsJson => Subtype.EndsWith("+json", StringComparison.Ordinal) || Essence is "application/json" or "text/json";

    /// <summary>
    /// Whether this is a JavaScript MIME type: its essence is one of the sixteen the standard lists,
    /// such as <c>text/javascript</c> or <c>application/x-ecmascript</c>. Parameters do not matter.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.6; IP=None; Security=High; Resources=1; Fingerprint=2EA5BD
    // Broiler-Falsified-If: text/javascript;charset=utf-8 is not reported a JavaScript MIME type because of its parameter
    // Broiler-Human:        PENDING
    public bool IsJavaScript => Array.IndexOf(JavaScriptEssences, Essence) >= 0;

    /// <summary>
    /// Whether <paramref name="value"/> is a JavaScript MIME type essence match: an ASCII
    /// case-insensitive match for one of those sixteen essences, as written, without parsing. This is
    /// how HTML reads a <c>&lt;script type&gt;</c>, where <c>text/javascript; charset=utf-8</c> is no match.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.6; IP=Low; Security=High; Resources=1; Fingerprint=347BFA
    // Broiler-Falsified-If: a string that matches an essence only under Unicode case folding, such as text/ecmascript spelled with a long s, is reported a match
    // Broiler-Human:        PENDING
    public static bool IsJavaScriptEssenceMatch(string? value)
    {
        if (value is null)
            return false;

        foreach (var essence in JavaScriptEssences)
        {
            if (Ascii.EqualsIgnoreCase(value, essence))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Parses <paramref name="input"/> by the MIME Sniffing Standard's "parse a MIME type":
    /// <see langword="true"/> with the MIME type, or <see langword="false"/> when the type or the
    /// subtype is missing, empty or holds anything but HTTP token code points.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.4; IP=Low; Security=High; Resources=3; Fingerprint=923060
    // Broiler-Falsified-If: a repeated parameter, as in text/html;charset=utf-8;charset=x, keeps the later value instead of the first
    // Broiler-Human:        PENDING
    public static bool TryParse(string? input, [NotNullWhen(true)] out MimeType? mimeType)
    {
        mimeType = null;
        if (input is null)
            return false;

        // 1. Leading and trailing HTTP whitespace is removed.
        var text = input.AsSpan().Trim(HttpWhitespace);

        // 2-6. The type is everything before the first '/', which must be there; it is not empty and
        // holds HTTP token code points only.
        var slash = text.IndexOf('/');
        if (slash <= 0 || text[..slash].ContainsAnyExcept(HttpTokenCodePoints))
            return false;

        var type = text[..slash];

        // 7-9. The subtype runs up to the first ';', less trailing HTTP whitespace; it is not empty
        // and holds HTTP token code points only.
        var position = slash + 1;
        var semicolon = text[position..].IndexOf(';');
        var end = semicolon < 0 ? text.Length : position + semicolon;
        var subtype = text[position..end].TrimEnd(HttpWhitespace);
        if (subtype.IsEmpty || subtype.ContainsAnyExcept(HttpTokenCodePoints))
            return false;

        // 10. Both are ASCII-lowercased. They hold token code points only, which are all ASCII, so
        // the invariant mapping folds nothing else.
        var parameters = new OrderedDictionary<string, string>(StringComparer.Ordinal);
        position = end;

        // 11. Each parameter: ';', optional HTTP whitespace, a name, '=', and a value.
        while (position < text.Length)
        {
            position++;
            while (position < text.Length && HttpWhitespace.Contains(text[position]))
                position++;

            var nameStart = position;
            while (position < text.Length && text[position] is not (';' or '='))
                position++;

            var name = text[nameStart..position];
            if (position < text.Length)
            {
                // A name with no '=' has no value, and the parameter is dropped.
                if (text[position] == ';')
                    continue;

                position++;
            }

            if (position >= text.Length)
                break;

            string value;
            if (text[position] == '"')
            {
                value = CollectQuotedStringValue(text, ref position);

                // Whatever follows the closing quote, up to the next ';', is ignored.
                while (position < text.Length && text[position] != ';')
                    position++;
            }
            else
            {
                var valueStart = position;
                while (position < text.Length && text[position] != ';')
                    position++;

                var unquoted = text[valueStart..position].TrimEnd(HttpWhitespace);
                if (unquoted.IsEmpty)
                    continue;

                value = unquoted.ToString();
            }

            // A name that is empty or not a token, a value outside the quoted-string token code points,
            // or a name already read drops the parameter. The name is lowercased after the token check,
            // which leaves only ASCII for the invariant mapping to fold.
            if (name.IsEmpty || name.ContainsAnyExcept(HttpTokenCodePoints) || !IsQuotedStringTokenText(value))
                continue;

            parameters.TryAdd(name.ToString().ToLowerInvariant(), value);
        }

        mimeType = new MimeType(type.ToString().ToLowerInvariant(), subtype.ToString().ToLowerInvariant(), parameters);
        return true;
    }

    /// <summary>
    /// The MIME Sniffing Standard's serialization: the essence, then <c>;name=value</c> for each parameter
    /// in order, with a value that is empty or not a token quoted and its <c>"</c> and <c>\</c> escaped.
    /// Serializing a parsed type and parsing that again gives the same type.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.5; IP=Low; Security=High; Resources=3; Fingerprint=239C03
    // Broiler-Falsified-If: two calls on one instance return different strings, or a serialization does not parse back to the same type
    // Broiler-Human:        PENDING
    public override string ToString() => _serialization ??= Serialize();

    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s4.5; IP=Low; Security=High; Resources=3; Fingerprint=A5B26C
    // Broiler-Falsified-If: a parameter value holding a double quote or a backslash is written without its escape, so the serialization parses back to different parameters
    // Broiler-Human:        PENDING
    private string Serialize()
    {
        var serialization = new StringBuilder(Essence);
        foreach (var (name, value) in Parameters)
        {
            serialization.Append(';').Append(name).Append('=');
            if (value.Length > 0 && !value.AsSpan().ContainsAnyExcept(HttpTokenCodePoints))
            {
                serialization.Append(value);
                continue;
            }

            serialization.Append('"');
            foreach (var c in value)
            {
                if (c is '"' or '\\')
                    serialization.Append('\\');

                serialization.Append(c);
            }

            serialization.Append('"');
        }

        return serialization.ToString();
    }

    /// <summary>
    /// Fetch's "collect an HTTP quoted string" with the extract-value flag: the text between the quotes
    /// at <paramref name="position"/>, each backslash escape resolved. An unterminated string runs to
    /// the end of the input, and a backslash that ends the input is kept.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s2.2; IP=Low; Security=High; Resources=3; Fingerprint=94D532
    // Broiler-Falsified-If: an unterminated quoted string that ends in a backslash throws or loses the backslash
    // Broiler-Human:        PENDING
    private static string CollectQuotedStringValue(ReadOnlySpan<char> input, ref int position)
    {
        var value = new StringBuilder();
        position++;
        while (true)
        {
            var start = position;
            while (position < input.Length && input[position] is not ('"' or '\\'))
                position++;

            value.Append(input[start..position]);
            if (position >= input.Length)
                break;

            if (input[position++] == '"')
                break;

            if (position >= input.Length)
            {
                value.Append('\\');
                break;
            }

            value.Append(input[position++]);
        }

        return value.ToString();
    }

    /// <summary>Whether every code point is an HTTP quoted-string token code point: a tab, U+0020 to U+007E, or U+0080 to U+00FF.</summary>
    // Broiler-AI:           Origin=AI; Spec=MIMESNIFF s3; IP=None; Security=High; Resources=3; Fingerprint=6FAED7
    // Broiler-Falsified-If: a value holding U+0100 or a control other than tab is accepted as a parameter value
    // Broiler-Human:        PENDING
    private static bool IsQuotedStringTokenText(string value)
    {
        foreach (var c in value)
        {
            if (c is not ('\t' or (>= ' ' and <= '~') or (>= '\u0080' and <= 'ÿ')))
                return false;
        }

        return true;
    }
}

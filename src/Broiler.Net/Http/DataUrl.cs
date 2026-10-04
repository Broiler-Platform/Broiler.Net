// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           4
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    High
// Criteria:         9/9
// Resource impact:  4/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Broiler.Net.Http;

/// <summary>
/// A <c>data:</c> URL as the Fetch Standard's <c>data:</c> URL processor reads it: the MIME type it
/// declares and the bytes it carries, percent-encoded or in base64.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here throws. A URL that is not a <c>data:</c> URL, has no comma, or declares a base64 body
/// that does not decode answers <see langword="false"/>, which a loader reports as a failed load, as
/// a browser fails it. A base64 body is decoded with <see cref="ForgivingBase64"/>, so a body without
/// its <c>=</c> padding decodes as it does in a browser.
/// </para>
/// <para>
/// The URL is not run through the URL parser first. Only the parser's steps that change what reaches
/// the processor are applied. Leading and trailing C0 controls and spaces are trimmed, and ASCII tabs
/// and newlines are removed throughout. The declared type has its C0 controls and non-ASCII code points
/// percent-encoded, as the parser leaves them, so <c>data:†/†,X</c> declares a type of its own where
/// the bare <c>†</c> would not parse. A URL the parser would reject outright, such as
/// <c>data://h:x/,X</c> with its invalid port, still decodes here.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=FETCH s6; IP=Low; Security=High; Resources=4; Fingerprint=70C4C9
// Broiler-Falsified-If: data:;base64,YQ, whose base64 body has no padding, is not decoded to the single byte 0x61 a browser reads from it
// Broiler-Human:        PENDING
public sealed class DataUrl
{
    // Broiler-AI:           Origin=AI; Spec=FETCH s6; IP=None; Security=High; Resources=1; Fingerprint=0A2E8A
    // Broiler-Falsified-If: the fallback serializes as anything but text/plain;charset=US-ASCII, so data:,X declares another type than the Fetch standard gives it
    // Broiler-Human:        PENDING
    private static readonly MimeType TextPlainUsAscii =
        MimeType.TryParse("text/plain;charset=US-ASCII", out var fallback) ? fallback : throw new UnreachableException();

    private readonly byte[] _body;

    private DataUrl(MimeType mimeType, byte[] body)
    {
        MimeType = mimeType;
        _body = body;
    }

    /// <summary>
    /// The MIME type the URL declares, without its <c>;base64</c> marker. A URL that declares
    /// parameters only is <c>text/plain</c> with them, and one whose type does not parse, or that
    /// declares none, is <c>text/plain;charset=US-ASCII</c>.
    /// </summary>
    public MimeType MimeType { get; }

    /// <summary>The decoded body.</summary>
    public ReadOnlyMemory<byte> Body => _body;

    /// <summary>
    /// Runs the <c>data:</c> URL processor over <paramref name="url"/>: <see langword="true"/> with
    /// its MIME type and body, or <see langword="false"/> when it is not a <c>data:</c> URL, has no
    /// comma, or declares a base64 body that does not decode.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s6; IP=Low; Security=High; Resources=4; Fingerprint=1E6710
    // Broiler-Falsified-If: a body is base64-decoded although ;base64 is not the last parameter, as in data:x/x;base64;charset=x,WA
    // Broiler-Human:        PENDING
    public static bool TryParse(string? url, [NotNullWhen(true)] out DataUrl? dataUrl)
    {
        dataUrl = null;
        if (url is null)
            return false;

        var input = PrepareAsUrlParserWould(url);
        if (input.Length < 5 || !Ascii.EqualsIgnoreCase(input.AsSpan(0, 5), "data:"))
            return false;

        input = input[5..];

        // The fragment is not part of the URL's path, so it is not part of the body either.
        var hash = input.IndexOf('#');
        if (hash >= 0)
            input = input[..hash];

        var comma = input.IndexOf(',');
        if (comma < 0)
            return false;

        var declared = PercentEncodeAsUrlParserWould(input[..comma]).AsSpan().Trim("\t\n\f\r ").ToString();
        var body = PercentDecode(input[(comma + 1)..]);

        if (TryFindBase64Marker(declared, out var marker))
        {
            // The body is isomorphic-decoded, one byte to one code point, for the base64 decode.
            if (!ForgivingBase64.TryDecode(Encoding.Latin1.GetString(body), out body))
                return false;

            declared = declared[..marker];
        }

        if (declared.StartsWith(';'))
            declared = "text/plain" + declared;

        dataUrl = new DataUrl(MimeType.TryParse(declared, out var mimeType) ? mimeType : TextPlainUsAscii, body);
        return true;
    }

    /// <summary>
    /// The body as text by the Encoding Standard's UTF-8 decode: a leading byte order mark is dropped,
    /// and a malformed sequence becomes U+FFFD. The <c>charset</c> parameter is not consulted; a
    /// caller that honours it reads <see cref="MimeType.Charset"/> and decodes <see cref="Body"/> itself.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ENCODING s6; IP=Low; Security=High; Resources=3; Fingerprint=6EB242
    // Broiler-Falsified-If: a body that starts with the UTF-8 byte order mark decodes to text that still starts with U+FEFF
    // Broiler-Human:        PENDING
    public string DecodeUtf8()
    {
        var body = _body.AsSpan();
        if (body.StartsWith(Encoding.UTF8.Preamble))
            body = body[Encoding.UTF8.Preamble.Length..];

        return Encoding.UTF8.GetString(body);
    }

    /// <summary>
    /// Finds a declared MIME type's trailing <c>;base64</c> marker: a <c>;</c>, any spaces, and
    /// <c>base64</c> in any ASCII case, at the very end. Only the last parameter can mark the body as
    /// base64, so <c>image/png;base64;charset=x</c> carries its body as written.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=FETCH s6; IP=Low; Security=High; Resources=3; Fingerprint=77AF28
    // Broiler-Falsified-If: a tab between the semicolon and base64 is taken for the base64 marker, where only spaces may stand there
    // Broiler-Human:        PENDING
    private static bool TryFindBase64Marker(string declared, out int marker)
    {
        marker = -1;
        const string Base64 = "base64";
        if (declared.Length < Base64.Length || !Ascii.EqualsIgnoreCase(declared.AsSpan(declared.Length - Base64.Length), Base64))
            return false;

        var position = declared.Length - Base64.Length - 1;
        while (position >= 0 && declared[position] == ' ')
            position--;

        if (position < 0 || declared[position] != ';')
            return false;

        marker = position;
        return true;
    }

    /// <summary>
    /// The URL parser's two input steps that reach the processor whole: leading and trailing C0
    /// controls and spaces are trimmed, and ASCII tabs and newlines are removed throughout.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=B41641
    // Broiler-Falsified-If: a literal tab or newline inside a data: URL survives into the decoded body, where the URL parser removes it
    // Broiler-Human:        PENDING
    private static string PrepareAsUrlParserWould(string url)
    {
        var start = 0;
        var end = url.Length;
        while (start < end && url[start] <= ' ')
            start++;
        while (end > start && url[end - 1] <= ' ')
            end--;

        var trimmed = url[start..end];
        if (trimmed.AsSpan().IndexOfAny('\t', '\n', '\r') < 0)
            return trimmed;

        var builder = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            if (c is not ('\t' or '\n' or '\r'))
                builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// The declared type as the URL parser leaves it in the URL: C0 controls and code points above
    /// U+007E percent-encoded as their UTF-8 bytes, and the rest as written.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=8DEDF7
    // Broiler-Falsified-If: a form feed ending the declared type leaves it text/plain, where the URL parser's percent-encoding makes it text/plain%0c
    // Broiler-Human:        PENDING
    private static string PercentEncodeAsUrlParserWould(string declared)
    {
        if (!declared.AsSpan().ContainsAnyExceptInRange(' ', '~'))
            return declared;

        var builder = new StringBuilder(declared.Length * 3);
        foreach (var b in Encoding.UTF8.GetBytes(declared))
        {
            if (b is >= (byte)' ' and <= (byte)'~')
                builder.Append((char)b);
            else
                builder.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    /// <summary>
    /// The URL Standard's percent-decode of a string: its UTF-8 bytes, with every <c>%</c> followed by
    /// two hex digits replaced by the byte they name. Any other <c>%</c> stays as it is.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=URL s1.3; IP=Low; Security=High; Resources=4; Fingerprint=F92E9B
    // Broiler-Falsified-If: a percent sign followed by a single hex digit at the end of the body, as in data:,%4, is dropped or decoded instead of kept as written
    // Broiler-Human:        PENDING
    private static byte[] PercentDecode(string input)
    {
        var encoded = Encoding.UTF8.GetBytes(input);
        if (Array.IndexOf(encoded, (byte)'%') < 0)
            return encoded;

        var output = new List<byte>(encoded.Length);
        for (var i = 0; i < encoded.Length; i++)
        {
            if (encoded[i] == '%' && i + 2 < encoded.Length
                && HexValue(encoded[i + 1]) is var high and >= 0
                && HexValue(encoded[i + 2]) is var low and >= 0)
            {
                output.Add((byte)((high << 4) | low));
                i += 2;
            }
            else
            {
                output.Add(encoded[i]);
            }
        }

        return [.. output];
    }

    // Broiler-AI:           Origin=AI; Spec=URL s1.3; IP=None; Security=High; Resources=0; Fingerprint=7C6F3A
    // Broiler-Falsified-If: a byte outside 0-9, a-f and A-F, such as G, is given a value, so %4G is percent-decoded
    // Broiler-Human:        PENDING
    private static int HexValue(byte b) => b switch
    {
        >= (byte)'0' and <= (byte)'9' => b - '0',
        >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
        >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
        _ => -1,
    };
}

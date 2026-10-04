// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           0
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  3/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Diagnostics.CodeAnalysis;

namespace Broiler.Net.Http;

/// <summary>
/// The Infra Standard's forgiving-base64 decode: how a browser decodes the base64 body of a
/// <c>data:</c> URL and the argument of <c>atob</c>.
/// </summary>
/// <remarks>
/// It disagrees with <see cref="Convert.FromBase64String"/> where pages differ from what an encoder
/// emits. It accepts a body without its <c>=</c> padding, and strips form feed along with the other
/// ASCII whitespace. It rejects a vertical tab, a non-ASCII space and a one-character tail.
/// <see cref="Convert.FromBase64String"/> throws on unpadded input, which made every unpadded
/// <c>data:</c> URL a page carried a <see cref="FormatException"/> and a dropped resource.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=INFRA s7; IP=Low; Security=High; Resources=3; Fingerprint=513410
// Broiler-Falsified-If: YQ fails to decode, where a browser decodes it to the single byte 0x61
// Broiler-Human:        PENDING
public static class ForgivingBase64
{
    /// <summary>
    /// Decodes <paramref name="input"/>: <see langword="true"/> with the bytes, or
    /// <see langword="false"/> when, once ASCII whitespace is removed, it holds anything outside the
    /// base64 alphabet, padding anywhere but the end of a whole number of quanta, or a length that
    /// leaves a single character over. Bits left after the last whole byte are discarded, whatever
    /// their value. Nothing here throws.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=INFRA s7; IP=Low; Security=High; Resources=3; Fingerprint=02EADB
    // Broiler-Falsified-If: input holding a vertical tab or a no-break space decodes instead of failing
    // Broiler-Human:        PENDING
    public static bool TryDecode(ReadOnlySpan<char> input, [NotNullWhen(true)] out byte[]? bytes)
    {
        bytes = null;

        // ASCII whitespace anywhere is removed: tab, newline, form feed, carriage return and space.
        var data = new char[input.Length];
        var length = 0;
        foreach (var c in input)
        {
            if (c is not ('\t' or '\n' or '\f' or '\r' or ' '))
                data[length++] = c;
        }

        // One or two '=' are removed only from a whole number of quanta; anywhere else '=' is a
        // character outside the alphabet.
        if (length % 4 == 0 && length > 0 && data[length - 1] == '=')
        {
            length--;
            if (length > 0 && data[length - 1] == '=')
                length--;
        }

        // A single leftover character holds six bits, which encode no byte.
        if (length % 4 == 1)
            return false;

        var output = new byte[length * 3 / 4];
        var written = 0;
        var buffer = 0;
        var bits = 0;
        for (var i = 0; i < length; i++)
        {
            var value = data[i] switch
            {
                >= 'A' and <= 'Z' => data[i] - 'A',
                >= 'a' and <= 'z' => data[i] - 'a' + 26,
                >= '0' and <= '9' => data[i] - '0' + 52,
                '+' => 62,
                '/' => 63,
                _ => -1,
            };
            if (value < 0)
                return false;

            // At most 13 bits are pending before a byte is taken off, so 16 are kept.
            buffer = ((buffer << 6) | value) & 0xFFFF;
            bits += 6;
            if (bits >= 8)
            {
                bits -= 8;
                output[written++] = (byte)(buffer >> bits);
            }
        }

        bytes = output;
        return true;
    }
}

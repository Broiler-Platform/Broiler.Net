// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           0
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Low
// Criteria:         8/0
// Resource impact:  0/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Net;

namespace Broiler.Net.Http;

/// <summary>
/// The product token sent as User-Agent and reported by navigator.userAgent: one string, so the network
/// and the page are told the same thing. Moved here from Broiler.Layout.Net, which keeps an identical
/// copy until its next release removes it.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=D759AA
// Broiler-Falsified-If: navigator.userAgent and the User-Agent request header of a client passed through Apply report different strings
// Broiler-Human:        PENDING
public static class BroilerUserAgent
{
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=2208F9
    // Broiler-Falsified-If: the string holds a NUL, CR, LF, a leading or trailing space, or a character above U+00FF, so FetchHeaders.IsHeaderValue rejects it
    // Broiler-Human:        PENDING
    public const string Value = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Broiler/1.0";

    /// <summary>Makes <see cref="Value"/> the client's default User-Agent; a request's own value wins.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=2EB622
    // Broiler-Falsified-If: a request message that carries its own User-Agent goes out through the client with Value in place of, or beside, its own
    // Broiler-Human:        PENDING
    public static HttpClient Apply(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Value);
        return client;
    }
}

/// <summary>
/// The preferred HTTP version Broiler requests, with fallback for HTTP/1.1 servers.
/// Moved here from Broiler.Layout.Net.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=4D89F9
// Broiler-Falsified-If: a client passed through Apply cannot negotiate HTTP/2 or fall back to HTTP/1.1
// Broiler-Human:        PENDING
public static class BroilerHttpProtocol
{
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=1631AE
    // Broiler-Falsified-If: the preferred version is not HTTP/2
    // Broiler-Human:        PENDING
    public static readonly Version Version = HttpVersion.Version20;
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=AB5D46
    // Broiler-Falsified-If: a request made with this policy upgrades to HTTP/3 or refuses HTTP/1.1 fallback
    // Broiler-Human:        PENDING
    public const HttpVersionPolicy VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=3DF1C0
    // Broiler-Falsified-If: callers treat this legacy fallback token as a negotiated protocol measurement
    // Broiler-Human:        PENDING
    // Retained for binary/source compatibility. New timing consumers must read the response version.
    public const string NextHopProtocol = "http/1.1";

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=981B51
    // Broiler-Falsified-If: a client passed through Apply still sends requests with its previous DefaultRequestVersion or DefaultVersionPolicy
    // Broiler-Human:        PENDING
    public static HttpClient Apply(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.DefaultRequestVersion = Version;
        client.DefaultVersionPolicy = VersionPolicy;
        return client;
    }
}

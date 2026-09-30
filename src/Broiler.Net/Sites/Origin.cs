// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           5
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    High
// Criteria:         8/6
// Resource impact:  3/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Runtime.CompilerServices;

namespace Broiler.Net.Sites;

/// <summary>
/// A web origin: a (scheme, canonical host, port) tuple, or an opaque origin that is same-origin only
/// with itself. Ports are explicit, including the scheme default.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=A81584
// Broiler-Falsified-If: an Origin holds a host that TryGetHttpHost did not canonicalize, so https://A.example and https://a.example compare unequal
// Broiler-Human:        PENDING
public sealed class Origin : IEquatable<Origin>
{
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=3A39FE
    // Broiler-Human:        PENDING
    private Origin(string scheme, string host, int port) => (Scheme, Host, Port) = (scheme, host, port);
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=11C0C8
    // Broiler-Falsified-If: the parameterless constructor leaves IsOpaque false, so two distinct opaque origins compare equal through their empty scheme and host
    // Broiler-Human:        PENDING
    private Origin() => (Scheme, Host, Port, IsOpaque) = ("", "", -1, true);

    public string Scheme { get; }
    public string Host { get; }
    public int Port { get; }
    public bool IsOpaque { get; }

    /// <summary>Creates a new opaque origin that is distinct from every other origin.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=09C481
    // Broiler-Falsified-If: CreateOpaque returns a cached instance, so two separately created opaque origins are same-origin
    // Broiler-Human:        PENDING
    public static Origin CreateOpaque() => new();

    /// <summary>
    /// HTTP(S) URLs yield tuple origins, and blob: URLs the origin of the HTTP(S) URL in their path (URL Standard,
    /// for a blob URL without an entry; hosts pass an entry's origin themselves). Every other scheme (file:, data:,
    /// about:) yields a new opaque origin.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=86FBEE
    // Broiler-Falsified-If: FromUrl of a blob: URL wrapping a data: or file: URL returns a tuple origin instead of a new opaque origin
    // Broiler-Human:        PENDING
    public static Origin FromUrl(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        if (url.IsAbsoluteUri && url.Scheme == "blob" &&
            Uri.TryCreate(url.GetComponents(UriComponents.Path, UriFormat.UriEscaped), UriKind.Absolute, out var path) &&
            path.Scheme is "http" or "https")
            url = path;
        return HostNames.TryGetHttpHost(url, out var host) ? new(url.Scheme, host, url.Port) : new();
    }

    public bool IsSameOrigin(Origin other) => Equals(other);

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=3BB49C
    // Broiler-Falsified-If: Equals returns true for tuple origins that differ only in port, such as https://a.example and https://a.example:8443
    // Broiler-Human:        PENDING
    public bool Equals(Origin? other) => other is not null && (ReferenceEquals(this, other) ||
        !IsOpaque && !other.IsOpaque && Scheme == other.Scheme && Host == other.Host && Port == other.Port);

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=41A6C8
    // Broiler-Falsified-If: Equals(object) disagrees with Equals(Origin?) for the same Origin argument
    // Broiler-Human:        PENDING
    public override bool Equals(object? obj) => Equals(obj as Origin);
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=B13A6D
    // Broiler-Falsified-If: two tuple origins that Equals reports equal return different hash codes
    // Broiler-Human:        PENDING
    public override int GetHashCode() => IsOpaque ? RuntimeHelpers.GetHashCode(this) : HashCode.Combine(Scheme, Host, Port);

    /// <summary>The ASCII serialization used by the Origin header; "null" for opaque origins.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=59477B
    // Broiler-Falsified-If: ToString of https://a.example:8443 omits :8443, so a response allowing https://a.example passes the CORS check for that page
    // Broiler-Human:        PENDING
    public override string ToString()
    {
        if (IsOpaque) return "null";
        var host = Host.Contains(':') ? $"[{Host}]" : Host;
        return Port == (Scheme == "https" ? 443 : 80) ? $"{Scheme}://{host}" : $"{Scheme}://{host}:{Port}";
    }
}

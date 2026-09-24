using System.Runtime.CompilerServices;

namespace Broiler.Net.Sites;

/// <summary>
/// A web origin: a (scheme, canonical host, port) tuple, or an opaque origin that is same-origin only
/// with itself. Ports are explicit, including the scheme default.
/// </summary>
public sealed class Origin : IEquatable<Origin>
{
    private Origin(string scheme, string host, int port) => (Scheme, Host, Port) = (scheme, host, port);
    private Origin() => (Scheme, Host, Port, IsOpaque) = ("", "", -1, true);

    public string Scheme { get; }
    public string Host { get; }
    public int Port { get; }
    public bool IsOpaque { get; }

    /// <summary>Creates a new opaque origin that is distinct from every other origin.</summary>
    public static Origin CreateOpaque() => new();

    /// <summary>
    /// HTTP(S) URLs yield tuple origins, and blob: URLs the origin of the HTTP(S) URL in their path (URL Standard,
    /// for a blob URL without an entry; hosts pass an entry's origin themselves). Every other scheme (file:, data:,
    /// about:) yields a new opaque origin.
    /// </summary>
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

    public bool Equals(Origin? other) => other is not null && (ReferenceEquals(this, other) ||
        !IsOpaque && !other.IsOpaque && Scheme == other.Scheme && Host == other.Host && Port == other.Port);

    public override bool Equals(object? obj) => Equals(obj as Origin);
    public override int GetHashCode() => IsOpaque ? RuntimeHelpers.GetHashCode(this) : HashCode.Combine(Scheme, Host, Port);

    /// <summary>The ASCII serialization used by the Origin header; "null" for opaque origins.</summary>
    public override string ToString()
    {
        if (IsOpaque) return "null";
        var host = Host.Contains(':') ? $"[{Host}]" : Host;
        return Port == (Scheme == "https" ? 443 : 80) ? $"{Scheme}://{host}" : $"{Scheme}://{host}:{Port}";
    }
}

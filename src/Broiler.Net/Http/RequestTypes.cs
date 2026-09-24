using System.Text;

namespace Broiler.Net.Http;

/// <summary>Fetch request destination. <see cref="Empty"/> is fetch(), XHR and sendBeacon.</summary>
public enum RequestDestination { Empty, Document, IFrame, Frame, Object, Embed, Script, Style, Image, Font }

/// <summary>Fetch request mode.</summary>
public enum RequestMode { Navigate, SameOrigin, NoCors, Cors }

/// <summary>Fetch credentials mode. It gates both sending and storing cookies.</summary>
public enum CredentialsMode { Omit, SameOrigin, Include }

/// <summary>Fetch redirect mode. Manual returns the redirect: an opaque redirect unless the request is a navigation.</summary>
public enum RedirectMode { Follow, Error, Manual }

/// <summary>
/// Fetch response tainting; it only ever moves away from <see cref="Basic"/>. <see cref="OpaqueRedirect"/> is Fetch's
/// opaque-redirect filtered response: a manual-mode redirect of a request that is not a navigation.
/// </summary>
public enum ResponseTainting { Basic, Cors, Opaque, OpaqueRedirect }

/// <summary>An element's CORS settings attribute (crossorigin).</summary>
public enum CorsSetting { None, Anonymous, UseCredentials }

/// <summary>HTML's CORS settings attribute.</summary>
public static class CorsSettings
{
    /// <summary>A missing attribute is <see cref="CorsSetting.None"/>, "use-credentials" (ASCII case-insensitive) is
    /// <see cref="CorsSetting.UseCredentials"/>, and every other value, invalid ones included, is <see cref="CorsSetting.Anonymous"/>.</summary>
    public static CorsSetting Parse(string? value) => value is null ? CorsSetting.None
        : Ascii.EqualsIgnoreCase(value, "use-credentials") ? CorsSetting.UseCredentials : CorsSetting.Anonymous;
}

/// <summary>Why the transport returned a network error instead of a response.</summary>
public enum TransportError { InvalidRequest, SameOriginViolation, Cors, RedirectLimit, RedirectScheme, RedirectDisallowed, Blocked }

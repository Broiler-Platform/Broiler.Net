// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           34
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  1/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Text;

namespace Broiler.Net.Http;

/// <summary>Fetch request destination. <see cref="Empty"/> is fetch(), XHR and sendBeacon.</summary>
// Broiler-AI:           Origin=AI; Spec=FETCH s2.2.5; IP=None; Security=None; Resources=0; Fingerprint=3229B5
// Broiler-Human:        PENDING
public enum RequestDestination { Empty, Document, IFrame, Frame, Object, Embed, Script, Style, Image, Font }

/// <summary>Fetch request mode.</summary>
// Broiler-AI:           Origin=AI; Spec=FETCH s2.2.5; IP=None; Security=None; Resources=0; Fingerprint=C5DA04
// Broiler-Human:        PENDING
public enum RequestMode { Navigate, SameOrigin, NoCors, Cors }

/// <summary>Fetch credentials mode. It gates both sending and storing cookies.</summary>
// Broiler-AI:           Origin=AI; Spec=FETCH s2.2.5; IP=None; Security=None; Resources=0; Fingerprint=FD24CB
// Broiler-Human:        PENDING
public enum CredentialsMode { Omit, SameOrigin, Include }

/// <summary>Fetch redirect mode. Manual returns the redirect: an opaque redirect unless the request is a navigation.</summary>
// Broiler-AI:           Origin=AI; Spec=FETCH s2.2.5; IP=None; Security=None; Resources=0; Fingerprint=F46C04
// Broiler-Human:        PENDING
public enum RedirectMode { Follow, Error, Manual }

/// <summary>
/// Fetch response tainting; it only ever moves away from <see cref="Basic"/>. <see cref="OpaqueRedirect"/> is Fetch's
/// opaque-redirect filtered response: a manual-mode redirect of a request that is not a navigation.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=FETCH s2.2.5; IP=None; Security=None; Resources=0; Fingerprint=DF1FDF
// Broiler-Human:        PENDING
public enum ResponseTainting { Basic, Cors, Opaque, OpaqueRedirect }

/// <summary>An element's CORS settings attribute (crossorigin).</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=9BB874
// Broiler-Human:        PENDING
public enum CorsSetting { None, Anonymous, UseCredentials }

/// <summary>HTML's CORS settings attribute.</summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=D5B94C
// Broiler-Falsified-If: a member here reads a missing crossorigin attribute as anything other than CorsSetting.None, so an element without the attribute is fetched in cors mode
// Broiler-Human:        PENDING
public static class CorsSettings
{
    /// <summary>A missing attribute is <see cref="CorsSetting.None"/>, "use-credentials" (ASCII case-insensitive) is
    /// <see cref="CorsSetting.UseCredentials"/>, and every other value, invalid ones included, is <see cref="CorsSetting.Anonymous"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=96CB78
    // Broiler-Falsified-If: Parse returns UseCredentials for a value that is not an ASCII case-insensitive match of use-credentials, such as one with a trailing space
    // Broiler-Human:        PENDING
    public static CorsSetting Parse(string? value) => value is null ? CorsSetting.None
        : Ascii.EqualsIgnoreCase(value, "use-credentials") ? CorsSetting.UseCredentials : CorsSetting.Anonymous;
}

/// <summary>Why the transport returned a network error instead of a response.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=7B96A2
// Broiler-Human:        PENDING
public enum TransportError { InvalidRequest, SameOriginViolation, Cors, RedirectLimit, RedirectScheme, RedirectDisallowed, Blocked }

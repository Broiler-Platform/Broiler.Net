// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           10
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    High
// Criteria:         8/7
// Resource impact:  1/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.Net.Http;

/// <summary>
/// Trusted host inputs for one request (and its redirects). Construct it in host code; never from script-authored
/// values. <see cref="Client"/> is the document that issues the request, which for a navigation is the initiator
/// (the source document). It is null only for browser-initiated navigations (address bar, bookmarks, history,
/// UI reloads), which have no client.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=445DA9
// Broiler-Falsified-If: a RequestContext initialized with only Destination and Client is not no-cors with credentials include and redirect follow
// Broiler-Human:        PENDING
public sealed record RequestContext
{
    public required RequestDestination Destination { get; init; }
    public DocumentRequestContext? Client { get; init; }

    /// <summary>
    /// For a nested navigation, and only there: the document that contains the navigated frame. Its ancestor chain
    /// gives the frame's site for cookies and partition key, while <see cref="Client"/> (the initiator: the container,
    /// the frame's own document or another one) gives the Origin header and must be same-site too.
    /// </summary>
    public DocumentRequestContext? Container { get; init; }

    public RequestMode Mode { get; init; } = RequestMode.NoCors;
    public CredentialsMode Credentials { get; init; } = CredentialsMode.Include;
    public RedirectMode Redirect { get; init; } = RedirectMode.Follow;

    /// <summary>A navigation reload triggered through browser UI (6265bis section 5.2 rule 1).</summary>
    public bool IsUserReload { get; init; }

    /// <summary>For <see cref="IsUserReload"/>: the reloaded document's recorded <see cref="TransportResponse.SameSite"/>.</summary>
    public bool ReloadWasSameSite { get; init; }

    /// <summary>
    /// For a request that follows a <see cref="RedirectMode.Manual"/> redirect itself (HTML "process the next manual
    /// redirect"): the previous response's <see cref="TransportResponse.UrlList"/>. Same-site status, tainting, the
    /// tainted origin and the redirect limit then cover the whole chain. Null for a new request.
    /// </summary>
    public IReadOnlyList<Uri>? RedirectChain { get; init; }

    /// <summary>
    /// Host policy (CSP, mixed content, blocked hosts) checked before every hop, the first included, with the URL
    /// about to be fetched and the number of redirects that led to it. False fails the request with
    /// <see cref="TransportError.Blocked"/> before anything is sent to that URL.
    /// </summary>
    public Func<Uri, int, bool>? HopPolicy { get; init; }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=8EC5E0
    // Broiler-Falsified-If: a request whose Mode is not Navigate reports IsNavigation true, so SendCore forces its credentials mode to include
    // Broiler-Human:        PENDING
    public bool IsNavigation => Mode == RequestMode.Navigate;
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=B02FFE
    // Broiler-Falsified-If: a navigate-mode request with an iframe destination reports IsTopLevelNavigation true, so SameSite=Lax cookies go with a cross-site frame load
    // Broiler-Human:        PENDING
    public bool IsTopLevelNavigation => Mode == RequestMode.Navigate && Destination == RequestDestination.Document;

    /// <summary>A top-level navigation; <paramref name="initiator"/> is the source document, or null for browser UI.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=1F0EB2
    // Broiler-Falsified-If: TopLevelNavigation drops the initiator from Client, so a navigation started by a cross-site page is sent as same-site with its SameSite=Strict cookies
    // Broiler-Human:        PENDING
    public static RequestContext TopLevelNavigation(DocumentRequestContext? initiator) => new()
    {
        Destination = RequestDestination.Document, Client = initiator,
        Mode = RequestMode.Navigate, Credentials = CredentialsMode.Include,
    };

    /// <summary>
    /// A navigation of a frame inside <paramref name="container"/>. <paramref name="initiator"/> is the source
    /// document: the container for a src attribute, the frame's own document when it navigates itself (a form,
    /// location or a link inside it), or null for browser UI. <paramref name="destination"/> is iframe, frame,
    /// object or embed.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=DD0FA7
    // Broiler-Falsified-If: NestedNavigation stores the initiator rather than the container argument as Container, so a frame inside a cross-site container is sent as same-site
    // Broiler-Human:        PENDING
    public static RequestContext NestedNavigation(DocumentRequestContext container, DocumentRequestContext? initiator,
        RequestDestination destination = RequestDestination.IFrame)
    {
        ArgumentNullException.ThrowIfNull(container);
        if (!IsFrameDestination(destination))
            throw new ArgumentOutOfRangeException(nameof(destination), "A nested navigation targets an iframe, frame, object or embed.");
        return new()
        {
            Destination = destination, Client = initiator, Container = container,
            Mode = RequestMode.Navigate, Credentials = CredentialsMode.Include,
        };
    }

    /// <summary>
    /// A subresource with HTML's CORS settings mapping (see <see cref="CorsSettings.Parse"/>): no attribute is
    /// no-cors/include, anonymous is cors/same-origin, use-credentials is cors/include. Fonts treat a missing
    /// setting as anonymous, as CSS Fonts fetches them; module scripts pass <see cref="CorsSetting.Anonymous"/>
    /// when the element has no attribute.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=E33E46
    // Broiler-Falsified-If: Subresource with CorsSetting.Anonymous yields credentials include instead of same-origin, so cookies go with an anonymous cross-origin request
    // Broiler-Human:        PENDING
    public static RequestContext Subresource(DocumentRequestContext client, RequestDestination destination, CorsSetting crossOrigin = CorsSetting.None)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (destination == RequestDestination.Font && crossOrigin == CorsSetting.None) crossOrigin = CorsSetting.Anonymous;
        return new()
        {
            Destination = destination, Client = client,
            Mode = crossOrigin == CorsSetting.None ? RequestMode.NoCors : RequestMode.Cors,
            Credentials = crossOrigin switch
            {
                CorsSetting.None or CorsSetting.UseCredentials => CredentialsMode.Include,
                _ => CredentialsMode.SameOrigin,
            },
        };
    }

    /// <summary>fetch(), XHR and sendBeacon; the defaults are fetch()'s.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=B365A9
    // Broiler-Falsified-If: Fetch accepts RequestMode.Navigate, so a script fetch() is treated as a navigation and sends credentials include
    // Broiler-Human:        PENDING
    public static RequestContext Fetch(DocumentRequestContext client, RequestMode mode = RequestMode.Cors,
        CredentialsMode credentials = CredentialsMode.SameOrigin, RedirectMode redirect = RedirectMode.Follow)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (mode == RequestMode.Navigate) throw new ArgumentException("fetch() cannot use navigate mode.", nameof(mode));
        return new() { Destination = RequestDestination.Empty, Client = client, Mode = mode, Credentials = credentials, Redirect = redirect };
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=3840AF
    // Broiler-Falsified-If: IsFrameDestination is true for a destination other than iframe, frame, object or embed, such as Document or Script
    // Broiler-Human:        PENDING
    internal static bool IsFrameDestination(RequestDestination destination) =>
        destination is RequestDestination.IFrame or RequestDestination.Frame or RequestDestination.Object or RequestDestination.Embed;
}

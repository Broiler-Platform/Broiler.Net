# Broiler.Net

A dependency-neutral net10.0 networking package for Broiler hosts: a cookie engine
(`Broiler.Net.Cookies`), site resolution (`Broiler.Net.Sites`) and a policy-aware HTTP
transport (`Broiler.Net.Http`). The runtime package depends only on the .NET base class
library. Broiler.HTML, Broiler.HtmlBridge and Broiler.Browser use it through one
profile-owned `BrowserNetworkSession` (stage 3 of the browser cookie support plan; see
Broiler.Browser `docs/cookie-support-stage-3.md`).

```csharp
using Broiler.Net.Cookies;
using Broiler.Net.Sites;

var cookies = new CookieStore(); // Own one per profile; keep it across documents.
var url = new Uri("https://app.example.test/account");
// Always pass the top-level site's partition key, also for top-level documents.
var partition = new CookiePartitionKey(SiteResolver.Default.GetSite(url)!);
var request = new CookieRequestContext(url, SameSiteStatus.SameSite, PartitionKey: partition);
cookies.ReceiveResponseCookie("session=example; Secure; HttpOnly; Path=/", request);
string header = cookies.BuildRequestHeader(request); // session=example
string visible = cookies.GetDocumentCookies(new(url, SameSiteStatus.SameSite, partition)); // empty
```

Same-site classification and partition keys are trusted host inputs. The host must
derive them from the actual document, ancestors, navigation and redirect chain; a URL
alone cannot answer those questions. Hosts must always pass the partition key of the
top-level site (with `HasCrossSiteAncestor` for cross-site frames); a null key means
"no partitioning available", so Partitioned cookies are then rejected and never sent.
Fetch credentials, CORS, user cookie settings, opaque-origin errors and lifecycle
cancellation must also be enforced by the host or transport; the cookie engine alone
does not implement them. `BrowserNetworkSession` (see [HTTP transport](#http-transport))
derives same-site status and partition keys and enforces the rest for HTTP requests and
document.cookie; lifecycle cancellation still comes from the host's cancellation tokens.
Hosts that call the store directly must do all of it themselves.

Responsibilities:

- `CookieParser` is pure and parses one response field at a time: name/value, attributes,
  cookie dates, Max-Age/Expires lifetime and the octet limits.
- `CookiePolicy` holds the acceptance and retrieval rules: host and Domain checks, public
  suffixes, default path, Secure, HttpOnly, SameSite, prefixes, partitioning, path and
  domain matching. Its rules are exercised through the store.
- `CookieStore` is the synchronized, bounded memory store: replacement, the insecure
  overlay check, ordering, serialization, last-access times, quotas, eviction and change
  events.
- `SiteResolver` embeds a versioned Public Suffix List including private suffixes;
  `HostNames` parses hosts; `Origin` and `SiteMatching` compare origins and schemeful sites.
- `BrowserNetworkSession` sends requests and serves document.cookie on top of the store.

Parsing is based on [6265bis-22](https://datatracker.ietf.org/doc/html/draft-ietf-httpbis-rfc6265bis-22)
with the extensions and deviations listed below. Partitioned (CHIPS) cookies are stored
under the host-supplied partition key.

The string response-header API accepts a Latin-1 representation of wire octets.
Non-Latin-1 characters are rejected. The parser's byte-span overload only parses and
validates; storing a cookie still goes through `ReceiveResponseCookie(s)` with a Latin-1
string. A transport must preserve field values individually and use compatible
response-header decoding. Never split Set-Cookie on commas. Document assignment encodes
UTF-8; document reads decode UTF-8. Stored and HTTP cookie strings preserve octets through
Latin-1. Raw names/values are not percent-decoded, unquoted, or silently sanitized.

Implementation policy:

- Cookie name/value together: at most 4096 octets; attribute values over 1024 octets
  are ignored, and a cookie whose resulting path (explicit or defaulted from the URL) is
  over 1024 octets is rejected (`RejectedSize`). A whole field over 64 KiB is rejected as
  an additional resource bound.
- Persistent lifetime is capped at 400 days. Unspecified SameSite behaves as Lax, with no
  recent-cookie unsafe-method grace period. Safe methods are GET, HEAD, OPTIONS and TRACE;
  the six standard methods match case-insensitively (Fetch method normalization).
- Quotas (configurable): 6000 records per store and 180 per site bucket. A site bucket is
  the registrable domain of the cookie's Domain (the host itself for IP addresses and public
  suffixes) plus the partition key, so subdomains share one budget and one embedded site
  cannot evict or observe another's partitioned cookies. Partitioned buckets also hold at
  most 10240 name+value octets (the CHIPS per-site budget). There is no cross-party
  per-partition cap. Count bounds plus the name/value, path and Domain limits bound memory;
  host-only domains are as long as the request URL's host.
- Eviction follows 6265bis-22 5.7: expired cookies first; then, in the over-quota site
  bucket, insecure cookies before secure ones, least recently accessed first. Global
  overflow ("all cookies") removes the least recently accessed cookie, secure or not, so a
  store full of secure cookies still accepts a fresh insecure one. Ties use creation order.
- Reads (`BuildRequestHeader`, `GetDocumentCookies`, `Snapshot`) skip expired records
  without removing them. The next mutation or `PruneExpired` removes and publishes them.
  Retrieval only visits the request host and its parent domains, so its cost does not
  grow with unrelated sites.
- HTTPS, and HTTP to 127.0.0.0/8, ::1, `localhost` and `*.localhost`, are secure cookie
  origins. IPv4-mapped IPv6 loopback is not. The localhost names qualify only because
  `BrowserNetworkSession` pins them to loopback; a host using another transport must do
  the same. file: has no cookie storage.
- Hosts follow the URL Standard host parser, label by label: ASCII labels without `xn--`
  are only lowercased (no hyphen or DNS length rules, also next to IDN labels); other labels
  use the platform's IDNA mapping (`IdnMapping`, ICU by default), which still applies its
  hyphen, length and per-label Bidi checks to them. The whole host is then checked for
  forbidden domain code points and empty labels. A host that ends in a number must be a
  valid IPv4 address (decimal, octal or hex parts) and becomes a dotted quad, so `10.0.0.1.`
  is `10.0.0.1`; `BrowserNetworkSession` connects to that address too. Other trailing-dot
  hosts retain their distinct cookie identity. URLs whose IDNA mapping fails are not HTTP
  hosts; the host APIs never throw for them. `Origin.FromUrl` gives a `blob:` URL the origin
  of the HTTP(S) URL in its path; hosts pass a blob URL entry's origin themselves.
- Known limitation: request paths come from `Uri.AbsolutePath`. System.Uri decodes
  percent-encoded unreserved characters (`/%7Euser` becomes `/~user`), while stored
  paths keep the raw Path attribute, so such cookies may not match. Hosts can build Uris
  with `UriCreationOptions.DangerousDisablePathAndQueryCanonicalization` (and remove dot
  segments themselves) to avoid it.
- Prefixes (name, case-insensitive): `__Secure-` and `__Host-` per 6265bis-22. As an
  extension beyond -22, `__Http-` and `__Host-Http-` follow draft-ietf-httpbis-layered-cookies-02
  5.4.3 and the WPT `cookies/prefix/__Http.https.html` and `__Host-Http.https.html` tests
  (web-platform-tests e35344a20f922751f5ea8cc678cfd79d65056d5c): they need Secure, HttpOnly
  and an HTTP (not document) source, and `__Host-Http-` also needs the `__Host-` rules.
  (That draft's 5.1.2.1 says "http-only is false"; its 4.1.3.3 and WPT require HttpOnly.)
  Nameless cookies whose value starts with any of these prefixes are rejected.
- Deliberate deviation: `__Host-` requires the last Path attribute to be literally `/`.
  6265bis-22 5.7 step 21.3 would accept an empty, valueless or relative Path that defaults
  to `/`; the engine follows layered-cookies (has-path) and WPT `__host.explicit-path`
  instead. Rejecting a `__Host-` cookie whose earlier Path was `/` but whose last Path is
  empty or invalid (`Path=/; Path=`) follows Chromium; neither draft requires it.
- Partitioned overlay: the insecure-overlay check (6265bis-22 5.7 step 16) only considers
  secure cookies in the new cookie's partition, as Chromium does. An insecure cookie can
  never be partitioned, so secure partitioned cookies do not block it.
- Snapshots/records are immutable and administrative: they include HttpOnly values.
  Never expose them directly to page scripts or ordinary diagnostics.
- Change events run outside the store lock and carry increasing revision numbers.
  Concurrent delivery can be out of order: `Snapshot(out long revision)` returns a
  snapshot and its revision atomically (also `Revision`), so a mirror can drop batches
  at or below its baseline. Reentrant observers are allowed. Reads never publish, so they
  never throw observer errors. A mutating call publishes and, when observers failed,
  throws one `CookieObserverException` after all of its work (`ReceiveResponseCookies`
  stores every field first); the committed state is unaffected, but that call's return
  value is lost. Observers should catch their own errors. Last-access touches do not
  emit cookie-change events.
- The store has no disk persistence. Ownership/end-of-session disposal and persistence
  integration belong to later browser stages.

## HTTP transport

`BrowserNetworkSession` is one profile's network session: the profile's `CookieStore`, a
pooled `SocketsHttpHandler` with automatic cookies and redirects disabled, and Fetch's
request policy. It is thread-safe; share one per profile. It implements
`IBrowserRequestTransport` (`SendAsync`, and `Send` for synchronous renderer loaders,
which uses the handler's synchronous path) and `IDocumentCookieAccess` (document.cookie).
Give script bindings `IDocumentCookieAccess`, never the store.

```csharp
using Broiler.Net.Http;

using var session = new BrowserNetworkSession(new() { Cookies = cookies });
var page = DocumentRequestContext.CreateTopLevel(new Uri("https://app.example.test/"));
using var response = await session.SendAsync(new(HttpMethod.Get, "https://cdn.example.test/app.js"),
    RequestContext.Subresource(page, RequestDestination.Script));
var headers = response.GetScriptVisibleHeaders(); // no-cors: opaque, empty
```

The host supplies, from trusted state and never from script values:

- a `DocumentRequestContext` per document: the URL whose cookies it uses (the creator's
  URL for about:blank and srcdoc, never the base URL), its origin (opaque when sandboxed
  or data:) and its parent (`CreateTopLevel`, `CreateChild`);
- a `RequestContext` per request: destination, the client document, mode, credentials and
  redirect mode. For a navigation the client is the initiator (the source document), null
  only for browser UI (address bar, bookmarks, history, UI reloads). A nested navigation
  also names the `Container`, the document that contains the frame:
  `NestedNavigation(container, initiator)` takes the container for a src attribute and the
  frame's own document when the frame navigates itself (a form, `location`, a link inside
  it). UI reloads add `IsUserReload` and `ReloadWasSameSite` (the reloaded document's
  recorded `TransportResponse.SameSite`); the session rejects reload flags, containers and
  navigate mode where they do not apply. `TopLevelNavigation`, `NestedNavigation`,
  `Subresource` (HTML's crossorigin mapping; `CorsSettings.Parse` reads the attribute) and
  `Fetch` build the common cases;
- script-authored headers normalized and filtered like Fetch's `Headers`:
  `FetchHeaders.IsHeaderName`/`IsHeaderValue` (after `NormalizeHeaderValue`), no
  `FetchHeaders.IsForbiddenRequestHeader` names, and on no-cors requests only
  `FetchHeaders.IsNoCorsSafelistedRequestHeader` ones. The session rejects values with
  NUL, CR, LF or characters above U+00FF (`InvalidRequest`), drops what a no-cors request
  may not carry (keeping forbidden names, `User-Agent`, `Range`, `Cache-Control` and
  `Pragma` as the user agent's own), and drops caller `Cookie`, `Cookie2`, `Host`,
  `Origin`, `Content-Length`, `Transfer-Encoding`, `Connection`, `Keep-Alive`, `TE`,
  `Trailer`, `Upgrade` and `Expect` values. Forbidden names the host adds itself (such as
  `Referer` or `Sec-Fetch-*`) never trigger a preflight; `Referer` and `Sec-*` values stop
  once a redirect leaves the first URL's origin, because Fetch recomputes them per hop;
- per-hop policy (CSP, mixed content, blocked hosts) through `RequestContext.HopPolicy`,
  which sees every URL before it is fetched, redirects included;
- body and header exposure per `TransportResponse.Tainting`: pages get
  `GetScriptVisibleHeaders()` and `ScriptVisibleStatusCode`, and no body for opaque and
  opaque-redirect responses; and a SecurityError when `TryGetCookie`/`TrySetCookie`
  return false (opaque-origin documents).

The session guarantees:

- Per-hop cookies: every hop, including each redirect, gets its own cookie context. The
  `Cookie` header comes from the store; every `Set-Cookie` field is stored individually,
  with its Latin-1 octets, for any status (redirects and errors too) and before a CORS
  failure is raised. Request and response fields use Latin-1; `Location` decodes UTF-8.
- Credentials: the mode gates both sending and storing. Navigations always include
  credentials; `same-origin` credentials stop once the request is tainted.
  `CookiesEnabled = false` sends, stores and exposes nothing.
- Redirects: 301/302/303/307/308 in follow, error and manual modes, at most
  `MaximumRedirects` (20) redirects, http(s) targets outside Fetch's bad ports only,
  fragment inheritance, POST-to-GET rewrites that drop the body and its headers (including
  content headers such as `Content-Disposition` that .NET only sends with a body), and
  Authorization dropped on cross-origin hops. The body is buffered once, so 307/308
  resend it. `UrlList` records the chain. The mode applies before `Location` is read: error
  mode fails on any redirect status, and manual mode returns an opaque redirect
  (`ResponseTainting.OpaqueRedirect`: status 0, no headers, no body for pages) unless the
  request is a navigation. Only follow mode returns a redirect without `Location` as the
  final response. A navigation that follows a manual redirect itself passes the previous
  `UrlList` as `RedirectChain`, so the chain keeps its same-site status, tainting, tainted
  origin and redirect count.
- Headers: `Accept` defaults to Fetch's value for the destination (HTML for documents and
  frames, images, styles, otherwise `*/*`) and `Accept-Language` to the
  `AcceptLanguage` option when set; request values always win.
- CORS: tainting moves from basic to cors or opaque at the first cross-origin hop and
  never back; `same-origin` mode fails on any cross-origin hop. Every cors hop, redirects
  included, gets the CORS check and, for non-safelisted methods or headers, its own
  uncached preflight. `Origin` is `null` once a cross-origin URL redirects to another
  origin (Fetch's tainted origin), and on an https-to-http request that is neither cors
  nor GET/HEAD (the default referrer policy).
- Same-site and CHIPS: a document's site for cookies comes from its inclusive ancestor
  chain: the top-level origin (a sandboxed top-level document counts with its URL's
  origin), or opaque when any document below the top is cross-site with it or has an
  opaque origin. Sandboxed frames are therefore cross-site, a deliberate deviation from
  6265bis-22 5.2.1 step 4.1 that follows Chromium and the pinned WPT
  (`cookies/samesite/sandbox-iframe-*`). A hop is same-site only when every URL in its
  redirect chain is same-site with the client's site for cookies and, for a nested
  navigation, also with the container's (as Chromium requires), so a cross-site frame
  that navigates itself is cross-site. `TransportResponse.SameSite` reports the final
  hop's status. The partition key is the hop URL's site for a top-level navigation,
  otherwise the top-level site of the container (nested navigations) or client, with
  `HasCrossSiteAncestor` set when that document's site for cookies is opaque or
  cross-site with the hop URL (a redirect bounce does not move a response to another
  partition); document.cookie derives both the same way.
- Localhost pinning: `localhost` and `*.localhost` connect only to ::1 and 127.0.0.1
  (::1 first; 127.0.0.1 joins after 250 ms), never through DNS or a proxy. Loopback
  addresses also go direct. They use their own handler, so other hosts keep the system
  proxy unwrapped, including Windows PAC/WPAD failover. A DNS-typed host that is an IPv4
  address under the URL Standard (`127.0.0.1.`, `127.1.`, full-width digits) is sent to
  that address, so the connection matches its cookie and origin identity.
- `TransportResponse.Headers` and `StatusCode` are privileged; Headers includes
  Set-Cookie. `GetScriptVisibleHeaders()` applies Fetch's filtered-response rules for the
  tainting and the request's credentials mode (recorded as `Credentials`) and never
  returns Set-Cookie or Set-Cookie2.
- `Sites` is always the cookie store's resolver (`CookieStore.Sites`); passing a different
  one with `Cookies` is an error.
- Network errors, including CORS and redirect failures, throw `TransportException` with a
  `TransportError`; HTTP error statuses are responses. `Timeout` (100 s) covers every hop
  up to the final response headers and surfaces as HttpClient's does (a
  `TaskCanceledException` wrapping `TimeoutException`). Cookie observer failures never
  fail a request; they go to `CookieObserverError`.

Not implemented: HTTP and preflight caches, referrer policy (the session never sets
Referer), mixed-content blocking and CSP (hosts apply the last two through `HopPolicy`).

## Build and test

Build, test, and create an unpublished local package:

```powershell
dotnet test Broiler.Net.slnx -c Release
./eng/pack.ps1 -Output artifacts/packages
```

The package carries the suite's packaging metadata (`eng/Broiler.Packaging.props`),
icon, XML documentation, symbols and the bundled PSL data under `data/`. Its license
expression is `Apache-2.0 AND MPL-2.0`: Apache-2.0 for Broiler.Net's code and MPL-2.0
for the bundled Public Suffix List. CI (`.github/workflows/ci.yml`) builds, tests and
packs on Windows and Linux, and checks the bundled data, notices and license expression.

Tests include deterministic time, table-driven algorithm cases, 10,000 seeded
malformed fields, a seeded state-machine model, concurrency, quota and eviction cases,
a 6000-cookie store, event-lock and observer-failure checks, the pinned upstream PSL
fixture, and transport tests against a scripted handler and an in-process loopback
HTTP/1.1 server. They are component tests, not a WPT browser conformance run. No test
needs an external web server; package restore is the only network requirement.

For PSL updates, select an upstream commit on publicsuffix/list main, inspect its
diff/license, obtain the SHA-256 of the list and its test fixture from that review,
then run:

```powershell
./scripts/update-public-suffix-list.ps1 -Revision <40-hex-commit> -ListSha256 <64-hex-hash> -FixtureSha256 <64-hex-hash>
dotnet test Broiler.Net.slnx -c Release
```

The updater refuses revisions that the GitHub compare API does not report as on upstream
main (`behind` or `identical`), for example unmerged fork commits. It verifies both
downloads before replacing either tracked file and records the revision, hashes and
fixture case count (which the tests check). Review and commit data, fixture and metadata
together; release a new package version when publishing a changed dataset. The library
does not update from the network at runtime. See `THIRD_PARTY_NOTICES.md` for data
licensing and `LICENSE` for the package license.

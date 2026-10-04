# Human Review: Broiler.Net

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.Net`, which rewrites this file,
`CODE-ASSURANCE.md`, `assurance.manifest.json` and every generated source header from the
product tree.

> **Status: PENDING.** Human-reviewed: 0 of 297 relevant units. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Net --release`
> fails while any relevant unit is without a decision bound to its current fingerprint.

## 1. How To Use This File

Read it; do not edit it. A decision about a code unit is the `// Broiler-Human:` line on that
unit's declaration, and every table below is read out of those lines. There is nothing here
to fill in and nothing here to leave blank.

## 2. How A Review Is Recorded

In one place: the `// Broiler-Human:` line of the assurance annotation that sits on the
declaration being read. Nothing in this file is edited by hand, no second document carries a
per-item checklist, and no list of permitted aliases exists to be added to.

```csharp
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=4A3BFD
// Broiler-Falsified-If: a negative value reaches the running total
// Broiler-Human:        PENDING
```

The last line has four shapes. A human writes three of them; the generator writes the fourth
and may never invent an alias, which the check asserts in both directions.

| Line | Meaning |
|---|---|
| `PENDING` | Nobody has recorded a decision for this unit. The generator leaves it exactly as it stands. |
| `<alias>` | A human states their own alias and leaves the machine field to the generator, which fills it with the declaration's fingerprint at the next run. |
| `<alias>; Fingerprint=<six hex>` | A decision bound to one exact version of one declaration. |
| `STALE; Previous=<alias>@<fingerprint>` | Written by the generator when the code moved after a decision. Only a human clears it, by stating their alias again. |

A human may state their own `IP=`, `Security=` and `Resources=` assessment beside their alias,
which is how a reader disagrees with the machine assessment on the line above: an assessment is
a comment and moves no fingerprint, so there is nowhere else to say it.

**No branch, commit or tag is recorded in this file.** Each decision names the fingerprint of
the declaration it was made against, and the state machine compares that value with the
declaration as it now stands. A commit says a tree moved; a fingerprint says whether this unit
did, which is the narrower and the more useful of the two.

## 3. Summary

| Metric | Value |
|---|---:|
| Files scanned | 21 |
| Code units | 469 |
| Relevant | 297 |
| Exempt | 172 |
| Assessed | 297 of 297 (100%) |
| Human reviewed | 0 of 297 (0%) |
| Unverified | 297 |
| Aliases naming a decision | 0 |

## 4. Review States

One row per state of the machine that reads the two lines. The states are computed from the
annotations and the current fingerprints; nothing stores them.

| State | Units |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 297 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 172 |

## 5. Aliases In The Tree

No alias appears on a human line anywhere in the product tree. Nobody has recorded a
decision about any unit of this component.

## 6. Coverage By File

One row per covered file, carrying that file's generated header. `Unverified` counts the
relevant units in a state that blocks a release.

| File | Units | Relevant | Exempt | Unverified | IP risk | Security risk | Criteria |
|---|---:|---:|---:|---:|---|---|---:|
| `src/Broiler.Net/Cookies/CookieObserverException.cs` | 2 | 2 | 0 | 2 | None | Low | 1/0 |
| `src/Broiler.Net/Cookies/CookieParser.cs` | 31 | 20 | 11 | 20 | Low | High | 19/11 |
| `src/Broiler.Net/Cookies/CookiePolicy.cs` | 11 | 10 | 1 | 10 | Low | High | 9/8 |
| `src/Broiler.Net/Cookies/CookieStore.cs` | 46 | 30 | 16 | 30 | Low | High | 28/22 |
| `src/Broiler.Net/Cookies/CookieTypes.cs` | 68 | 23 | 45 | 23 | Low | High | 13/8 |
| `src/Broiler.Net/Http/BroilerUserAgent.cs` | 8 | 8 | 0 | 8 | Low | Low | 8/0 |
| `src/Broiler.Net/Http/BrowserNetworkSession.cs` | 48 | 41 | 7 | 41 | Low | High | 40/31 |
| `src/Broiler.Net/Http/DataUrl.cs` | 13 | 9 | 4 | 9 | Low | High | 9/9 |
| `src/Broiler.Net/Http/DocumentCookieAccess.cs` | 11 | 7 | 4 | 7 | Low | High | 6/6 |
| `src/Broiler.Net/Http/DocumentRequestContext.cs` | 11 | 8 | 3 | 8 | Low | High | 7/5 |
| `src/Broiler.Net/Http/FetchHeaders.cs` | 32 | 32 | 0 | 32 | Low | High | 32/29 |
| `src/Broiler.Net/Http/ForgivingBase64.cs` | 2 | 2 | 0 | 2 | Low | High | 2/2 |
| `src/Broiler.Net/Http/LoopbackRouting.cs` | 16 | 13 | 3 | 13 | Medium | High | 12/11 |
| `src/Broiler.Net/Http/MimeType.cs` | 22 | 17 | 5 | 17 | Low | High | 15/15 |
| `src/Broiler.Net/Http/RequestContext.cs` | 18 | 8 | 10 | 8 | Low | High | 8/7 |
| `src/Broiler.Net/Http/RequestTypes.cs` | 43 | 9 | 34 | 9 | Low | High | 2/2 |
| `src/Broiler.Net/Http/Transport.cs` | 22 | 9 | 13 | 9 | Low | High | 7/6 |
| `src/Broiler.Net/Http/TransportResponse.cs` | 14 | 8 | 6 | 8 | Low | High | 5/3 |
| `src/Broiler.Net/Sites/Origin.cs` | 14 | 9 | 5 | 9 | Low | High | 8/7 |
| `src/Broiler.Net/Sites/SiteMatching.cs` | 3 | 3 | 0 | 3 | Low | High | 3/3 |
| `src/Broiler.Net/Sites/SiteResolver.cs` | 34 | 29 | 5 | 29 | Low | High | 28/25 |

## 7. Decisions Recorded

No unit in this component carries a decision on its human line. Every one of them reads
`PENDING`.

## 8. Decisions The Code Has Outrun

No unit carries a decision that the code has since moved past.

## 9. Where A Decision Is Required First

The units at the top of the security vocabulary, with the observation that would show each
one wrong and the human line it carries. The set is read from the assessments rather than
written out, so a unit that becomes `High` joins it at the next generation.

- `Broiler.Net.Cookies.CookieParser` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.6, `A3F7A7`, PENDING
  - Falsified if: a field holding two comma-separated name-value pairs yields a cookie named after the second pair instead of one cookie whose value contains the comma
- `Broiler.Net.Cookies.CookieParser.Parse(string, DateTimeOffset)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.6, `B4FB87`, PENDING
  - Falsified if: a header string containing a character above U+00FF is parsed into a cookie instead of rejected with RejectedEncoding
- `Broiler.Net.Cookies.CookieParser.Parse(ReadOnlySpan<byte>, DateTimeOffset)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.6, `2A50CF`, PENDING
  - Falsified if: a field containing a NUL, CR, LF or DEL octet yields a parsed cookie instead of RejectedControlCharacter
- `Broiler.Net.Cookies.CookieParser.Trim(string)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.6, `F5A18B`, PENDING
  - Falsified if: a cookie name sent with a leading space before __Host-id keeps the space, so the __Host- prefix rules do not apply to it
- `Broiler.Net.Cookies.CookieParser.TryMaxAge(string, out long)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.6.2, `0DB779`, PENDING
  - Falsified if: a Max-Age of twenty or more digits wraps the long accumulator to a negative or short lifetime instead of stopping at the 400-day cap
- `Broiler.Net.Cookies.CookieParser.TryParseDate(string, out DateTimeOffset)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.1.1, `FB28D2`, PENDING
  - Falsified if: a cookie date naming 30 February returns true or throws instead of returning false
- `Broiler.Net.Cookies.CookieParser.Number(string)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=none cited, `078ECE`, PENDING
  - Falsified if: a digit group captured by the date regexes makes Number throw, so TryParseDate raises an exception instead of returning false
- `Broiler.Net.Cookies.CookieParser.DateDelimiters()` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.1.1, `9C1A74`, PENDING
  - Falsified if: an octet the cookie-date grammar lists as a delimiter, such as @ (0x40) or ~ (0x7E), stays inside a token instead of separating tokens
- `Broiler.Net.Cookies.CookieParser.TimeToken()` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.1.1, `AF658F`, PENDING
  - Falsified if: a token with a three-digit field such as 100:00:00 is accepted as the time of day
- `Broiler.Net.Cookies.CookieParser.DayToken()` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.1.1, `35DA3C`, PENDING
  - Falsified if: a three-digit token such as 123 is accepted as the day of month
- `Broiler.Net.Cookies.CookieParser.YearToken()` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, Spec=RFC-6265bis s5.1.1, `CE2D4B`, PENDING
  - Falsified if: a five-digit token such as 20250 is accepted as the year
- `Broiler.Net.Cookies.CookiePolicy` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, Spec=none cited, `BCBB23`, PENDING
  - Falsified if: a cookie accepted from sub.example.com without a Domain attribute is returned for a request to example.com
- `Broiler.Net.Cookies.CookiePolicy.Accept(ParsedCookie, CookieRequestContext, bool, out CookieKey?)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, Spec=RFC-6265bis s5.7, `CF7D1A`, PENDING
  - Falsified if: a Set-Cookie received from a.example.com with a Domain attribute of b.example.com produces a key instead of RejectedDomain
- `Broiler.Net.Cookies.CookiePolicy.TryBeginRetrieval(CookieRequestContext, bool, out RetrievalScope)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, Spec=RFC-6265bis s5.8, `FFEAA3`, PENDING
  - Falsified if: a cross-site top-level POST navigation yields a scope that lets Lax cookies through
- `Broiler.Net.Cookies.CookiePolicy.CanRetrieve(CookieRecord, in RetrievalScope, ref bool?)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, Spec=RFC-6265bis s5.8, `C82973`, PENDING
  - Falsified if: a SameSite Strict cookie is returned for a cross-site top-level GET navigation
- `Broiler.Net.Cookies.CookiePolicy.IsSafeMethod(string?)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, Spec=none cited, `91C767`, PENDING
  - Falsified if: the method POST, in any letter case, is classed as safe, so Lax cookies ride a cross-site top-level form submission
- `Broiler.Net.Cookies.CookiePolicy.Prefix(string, string)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, Spec=RFC-6265bis s5.4, `495678`, PENDING
  - Falsified if: a cookie named __host-id in lower case is stored with a Domain attribute because the prefix comparison is case-sensitive
- `Broiler.Net.Cookies.CookiePolicy.DefaultPath(Uri)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, Spec=RFC-6265bis s5.1.4, `267F8A`, PENDING
  - Falsified if: a cookie without a Path set by a response for /docs/page gets a default path other than /docs
- `Broiler.Net.Cookies.CookiePolicy.PathMatches(string, string)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, Spec=RFC-6265bis s5.1.4, `42CA58`, PENDING
  - Falsified if: a cookie path of /foo matches the request path /foobar
- `Broiler.Net.Cookies.CookieStore` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.7, `5D7083`, PENDING
  - Falsified if: a store field or index is read or written outside lock(_sync), or a Changed observer runs while _sync is held
- `Broiler.Net.Cookies.CookieStore.Changed` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `F27A3A`, PENDING
  - Falsified if: a handler added or removed on one thread while Publish runs on another is lost from Changed or makes that Publish throw
- `Broiler.Net.Cookies.CookieStore.Revision` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `C93C34`, PENDING
  - Falsified if: Revision reads _revision without holding _sync, so a reader can see a value no committed batch carried
- `Broiler.Net.Cookies.CookieStore.ReceiveResponseCookie(string, CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.7, `D1C390`, PENDING
  - Falsified if: a Set-Cookie field reaches the store without CookiePolicy.Accept running against this request's URL and same-site status
- `Broiler.Net.Cookies.CookieStore.ReceiveResponseCookies(IEnumerable<string>, CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.7, `8E3D73`, PENDING
  - Falsified if: a failing Changed observer on one field stops a later field in the same call from being stored
- `Broiler.Net.Cookies.CookieStore.SetDocumentCookie(string, CookieDocumentContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.8.2, `5E3101`, PENDING
  - Falsified if: a script assignment is stored with document set to false, so it can create or overwrite an HttpOnly cookie
- `Broiler.Net.Cookies.CookieStore.ReceiveField(string, CookieRequestContext, ref List<Exception>?)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `B3B001`, PENDING
  - Falsified if: a response field reaches Receive without passing through CookieParser.Parse, or flagged as a document assignment
- `Broiler.Net.Cookies.CookieStore.Receive(CookieParseResult, CookieRequestContext, bool, ref List<Exception>?)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.7, `DE4F62`, PENDING
  - Falsified if: a Set-Cookie without Secure from an insecure origin is stored while a Secure cookie of the same name, a domain-matching Domain and a matching path exists in its partition
- `Broiler.Net.Cookies.CookieStore.BuildRequestHeader(CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.8.3, `5109F7`, PENDING
  - Falsified if: the header for a request carries a cookie that CookiePolicy.CanRetrieve rejects for that request's context
- `Broiler.Net.Cookies.CookieStore.GetDocumentCookies(CookieDocumentContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.8.2, `1741FE`, PENDING
  - Falsified if: an HttpOnly cookie that matches the document URL appears in the returned string
- `Broiler.Net.Cookies.CookieStore.Retrieve(CookieRequestContext, bool)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.8.3, `23A798`, PENDING
  - Falsified if: a record whose Expires is at or before the read's clock reading is included in the returned header
- `Broiler.Net.Cookies.CookieStore.Snapshot()` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `18A15A`, PENDING
  - Falsified if: Snapshot() leaves out a live cookie, HttpOnly ones included, that Snapshot(out long) returns for the same store state
- `Broiler.Net.Cookies.CookieStore.Snapshot(out long)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `64C1BE`, PENDING
  - Falsified if: the returned revision is not the revision of the last batch whose changes the returned records reflect
- `Broiler.Net.Cookies.CookieStore.Clear()` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `2783CB`, PENDING
  - Falsified if: a record that had already expired is counted in the return value or published as Cleared instead of Expired
- `Broiler.Net.Cookies.CookieStore.PruneExpired()` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.7, `3EB1C3`, PENDING
  - Falsified if: a Changed observer runs while PruneExpired still holds _sync
- `Broiler.Net.Cookies.CookieStore.Live(CookieRecord, DateTimeOffset)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `AC3BC5`, PENDING
  - Falsified if: a persistent cookie whose Expires equals the current instant is treated as live
- `Broiler.Net.Cookies.CookieStore.EnforceLimits(Entry, List<CookieChange>)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.7, `AE7EE5`, PENDING
  - Falsified if: after an insert returns, one site bucket holds more than MaximumCookiesPerSite records, or the store holds more than MaximumCookies
- `Broiler.Net.Cookies.CookieStore.Victim(IEnumerable<Entry>, bool)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=RFC-6265bis s5.7, `F12C65`, PENDING
  - Falsified if: with preferInsecure set, a Secure cookie is chosen while the scope still holds a cookie without Secure
- `Broiler.Net.Cookies.CookieStore.Add(CookieRecord)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `890D91`, PENDING
  - Falsified if: two cookies in the same partition whose domains share a registrable domain are charged to different site buckets
- `Broiler.Net.Cookies.CookieStore.Unindex(Entry)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `D2A58B`, PENDING
  - Falsified if: a record removed from _cookies is still found by Retrieve through the _byDomain index
- `Broiler.Net.Cookies.CookieStore.Publish(CookieChangeBatch?, ref List<Exception>?)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `0B8EB7`, PENDING
  - Falsified if: an observer that throws prevents a later observer in the invocation list from being called
- `Broiler.Net.Cookies.CookieStore.BucketKey` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, Spec=none cited, `AD4675`, PENDING
  - Falsified if: two keys with the same Site and different Partition values compare equal
- `Broiler.Net.Cookies.CookiePartitionKey` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, Spec=none cited, `8A0D78`, PENDING
  - Falsified if: two keys with the same top-level site and different HasCrossSiteAncestor values compare equal
- `Broiler.Net.Cookies.CookieRequestContext` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, Spec=none cited, `E2EDBD`, PENDING
  - Falsified if: a context built without IsTopLevelNavigation lets a Lax cookie through on a cross-site request
- `Broiler.Net.Cookies.CookieKey` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, Spec=none cited, `1A6E29`, PENDING
  - Falsified if: two keys that differ only in HostOnly or PartitionKey compare equal, so one cookie replaces the other
- `Broiler.Net.Cookies.ICookieService` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, Spec=none cited, `30F158`, PENDING
  - Falsified if: an implementation serves the document methods with HTTP privileges, so GetDocumentCookies returns an HttpOnly cookie
- `Broiler.Net.Cookies.ICookieService.ReceiveResponseCookie(string, CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, Spec=none cited, `B0E6BB`, PENDING
  - Falsified if: an implementation stores a Set-Cookie field whose Domain attribute the host of the context URL does not domain-match
- `Broiler.Net.Cookies.ICookieService.BuildRequestHeader(CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, Spec=none cited, `F7FB5D`, PENDING
  - Falsified if: the header an implementation builds carries a cookie whose Domain or Path does not match the context URL
- `Broiler.Net.Cookies.ICookieService.SetDocumentCookie(string, CookieDocumentContext)` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, Spec=none cited, `A4646E`, PENDING
  - Falsified if: an implementation lets a document assignment create a cookie with the HttpOnly attribute or replace an existing HttpOnly cookie
- `Broiler.Net.Cookies.ICookieService.GetDocumentCookies(CookieDocumentContext)` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, Spec=none cited, `AB33B7`, PENDING
  - Falsified if: an implementation includes an HttpOnly cookie in the string it returns for document.cookie
- `Broiler.Net.Http.BrowserNetworkSession` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `915513`, PENDING
  - Falsified if: a Cookie header is attached to a request whose credentials mode is omit, or to the cross-origin hop of a same-origin-credentials request
- `Broiler.Net.Http.BrowserNetworkSession.OwnedHeaders` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `02CEF0`, PENDING
  - Falsified if: a caller-set Cookie, Host, Origin or Transfer-Encoding header reaches the wire in place of the value the session or handler writes
- `Broiler.Net.Http.BrowserNetworkSession.BadPorts` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=FETCH s2.9, `FE5CE8`, PENDING
  - Falsified if: a port on the Fetch bad port list, such as 25, 6697 or 10080, is missing from the set, so a request to it is sent
- `Broiler.Net.Http.BrowserNetworkSession.BrowserNetworkSession(BrowserNetworkSessionOptions?)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `BAB4B0`, PENDING
  - Falsified if: options whose Sites differ from the supplied cookie store's resolver construct a session instead of throwing ArgumentException
- `Broiler.Net.Http.BrowserNetworkSession.SendAsync(HttpRequestMessage, RequestContext, CancellationToken)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `5AA4DE`, PENDING
  - Falsified if: a call made after Dispose, or with a null request or context, reaches SendCore instead of throwing before any work starts
- `Broiler.Net.Http.BrowserNetworkSession.Send(HttpRequestMessage, RequestContext, CancellationToken)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `7D34F0`, PENDING
  - Falsified if: the synchronous path awaits real asynchronous work, so GetResult is called on a ValueTask that has not completed
- `Broiler.Net.Http.BrowserNetworkSession.TryGetCookie(DocumentRequestContext, out string)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `19158B`, PENDING
  - Falsified if: document.cookie read through the session differs from DocumentCookies.TryGetCookie for the same document
- `Broiler.Net.Http.BrowserNetworkSession.TrySetCookie(DocumentRequestContext, string)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `F52297`, PENDING
  - Falsified if: a document.cookie write through the session lands in a store other than Cookies, so the next request does not carry it
- `Broiler.Net.Http.BrowserNetworkSession.GetSiteForCookies(DocumentRequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `923121`, PENDING
  - Falsified if: it returns a site for cookies that differs from DocumentCookies.GetSiteForCookies for the same document
- `Broiler.Net.Http.BrowserNetworkSession.Dispose()` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `3AB337`, PENDING
  - Falsified if: two threads calling Dispose at the same time both dispose the invoker, or a second Dispose call throws
- `Broiler.Net.Http.BrowserNetworkSession.CheckSend(HttpRequestMessage, RequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `1434B7`, PENDING
  - Falsified if: a Send or SendAsync call made after Dispose has returned passes through without an ObjectDisposedException
- `Broiler.Net.Http.BrowserNetworkSession.CheckContext(RequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `5F9B73`, PENDING
  - Falsified if: a non-navigation request that names a Container, or omits its Client, passes without an InvalidRequest error
- `Broiler.Net.Http.BrowserNetworkSession.SendCore(HttpRequestMessage, RequestContext, bool, CancellationToken)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=FETCH s4.5, `1D97EA`, PENDING
  - Falsified if: a same-origin-credentials cors request redirected to another origin sends the Cookie header on the cross-origin hop
- `Broiler.Net.Http.BrowserNetworkSession.Taint(RequestContext, Origin?, Uri, ResponseTainting)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=FETCH s4.1, `F7C906`, PENDING
  - Falsified if: a cors-mode request that reaches a cross-origin URL keeps basic tainting, so its response skips the CORS check
- `Broiler.Net.Http.BrowserNetworkSession.TaintsOrigin(Origin?, Uri, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=FETCH s4.5, `FC161B`, PENDING
  - Falsified if: a redirect from a cross-origin URL to a third origin is not reported as tainting, so the next hop sends the real Origin instead of null
- `Broiler.Net.Http.BrowserNetworkSession.LeaveOrigin(List<KeyValuePair<string, string>>, Uri, Uri, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=FETCH s4.5, `FD5725`, PENDING
  - Falsified if: an Authorization header survives a redirect whose location is not same-origin with the current URL
- `Broiler.Net.Http.BrowserNetworkSession.PreflightAsync(Uri, string, List<KeyValuePair<string, string>>, string, bool, bool, CancellationToken)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=FETCH s4.8, `71D68F`, PENDING
  - Falsified if: a credentialed preflight answered with Access-Control-Allow-Headers: * lets a header that is not CORS-safelisted through
- `Broiler.Net.Http.BrowserNetworkSession.GetAuthorHeaders(HttpRequestMessage, RequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `B2D115`, PENDING
  - Falsified if: a caller-set header value with a CR or LF between other characters is copied into the list instead of raising InvalidRequest
- `Broiler.Net.Http.BrowserNetworkSession.ResolveLocation(Uri, HeaderStringValues)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=FETCH s4.5, `F3C82B`, PENDING
  - Falsified if: a Location that resolves to a file:, data: or other non-HTTP(S) URL is returned instead of raising RedirectScheme
- `Broiler.Net.Http.BrowserNetworkSession.PassesCorsCheck(HttpResponseMessage, string, bool)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=FETCH s4.10, `72F265`, PENDING
  - Falsified if: a credentialed response with Access-Control-Allow-Origin: * passes the check
- `Broiler.Net.Http.BrowserNetworkSession.GetCombined(HttpResponseMessage, string)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `CECEB8`, PENDING
  - Falsified if: a response with two Access-Control-Allow-Origin fields yields one of them alone, so the duplicated header can pass the CORS check
- `Broiler.Net.Http.BrowserNetworkSession.GetValues(HttpResponseMessage, string)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `83B1F0`, PENDING
  - Falsified if: an allow list split over two Access-Control-Allow-Methods fields yields the values of only one field
- `Broiler.Net.Http.BrowserNetworkSession.GetSameSite(RequestContext, Origin?, Origin?, List<Uri>)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=RFC-6265bis s5.2, `962270`, PENDING
  - Falsified if: a request whose redirect chain passes through a cross-site URL is given SameSite status, so Strict cookies go with it
- `Broiler.Net.Http.BrowserNetworkSession.GetPartitionKey(RequestContext, DocumentRequestContext?, Origin?, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `AC11E5`, PENDING
  - Falsified if: a request used by an iframe that is cross-site with its top-level document gets a partition key whose cross-site ancestor bit is false
- `Broiler.Net.Http.BrowserNetworkSession.TopLevelOrigin(DocumentRequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `ABD447`, PENDING
  - Falsified if: it returns an origin that differs from DocumentCookieAccess.TopLevelOrigin for the same document
- `Broiler.Net.Http.BrowserNetworkSession.IsCrossOrigin(Origin?, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `8D4B53`, PENDING
  - Falsified if: a URL that differs from the request origin only in scheme or port is reported as not cross-origin
- `Broiler.Net.Http.BrowserNetworkSession.IsDowngrade(Origin, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `E06518`, PENDING
  - Falsified if: an https request origin paired with an http URL returns false, so a POST from https to http carries the full Origin
- `Broiler.Net.Http.BrowserNetworkSession.IsFetchable(Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `524C27`, PENDING
  - Falsified if: a relative URL, or a file:, data: or ftp: URL, is reported as fetchable
- `Broiler.Net.Http.BrowserNetworkSession.WithCanonicalHost(Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `E2B91F`, PENDING
  - Falsified if: a URL whose host is 127.1. or 127.0.0.1. with its trailing dot reaches the handler with that DNS-typed host rather than 127.0.0.1
- `Broiler.Net.Http.BrowserNetworkSession.CreateHandler(BrowserNetworkSessionOptions)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `8E88B0`, PENDING
  - Falsified if: the direct and proxied handlers are passed in swapped order, so loopback traffic goes through the system proxy
- `Broiler.Net.Http.BrowserNetworkSession.CreateSocketsHandler(BrowserNetworkSessionOptions, bool)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, Spec=none cited, `C473CC`, PENDING
  - Falsified if: a handler built here follows redirects or sends stored cookies on its own, so a hop skips the session's CORS and cookie rules
- `Broiler.Net.Http.DataUrl` in `src/Broiler.Net/Http/DataUrl.cs` - Security=High, Spec=FETCH s6, `70C4C9`, PENDING
  - Falsified if: data:;base64,YQ, whose base64 body has no padding, is not decoded to the single byte 0x61 a browser reads from it
- `Broiler.Net.Http.DataUrl.TextPlainUsAscii` in `src/Broiler.Net/Http/DataUrl.cs` - Security=High, Spec=FETCH s6, `0A2E8A`, PENDING
  - Falsified if: the fallback serializes as anything but text/plain;charset=US-ASCII, so data:,X declares another type than the Fetch standard gives it
- `Broiler.Net.Http.DataUrl.TryParse(string?, out DataUrl?)` in `src/Broiler.Net/Http/DataUrl.cs` - Security=High, Spec=FETCH s6, `1E6710`, PENDING
  - Falsified if: a body is base64-decoded although ;base64 is not the last parameter, as in data:x/x;base64;charset=x,WA
- `Broiler.Net.Http.DataUrl.DecodeUtf8()` in `src/Broiler.Net/Http/DataUrl.cs` - Security=High, Spec=ENCODING s6, `6EB242`, PENDING
  - Falsified if: a body that starts with the UTF-8 byte order mark decodes to text that still starts with U+FEFF
- `Broiler.Net.Http.DataUrl.TryFindBase64Marker(string, out int)` in `src/Broiler.Net/Http/DataUrl.cs` - Security=High, Spec=FETCH s6, `77AF28`, PENDING
  - Falsified if: a tab between the semicolon and base64 is taken for the base64 marker, where only spaces may stand there
- `Broiler.Net.Http.DataUrl.PrepareAsUrlParserWould(string)` in `src/Broiler.Net/Http/DataUrl.cs` - Security=High, Spec=none cited, `B41641`, PENDING
  - Falsified if: a literal tab or newline inside a data: URL survives into the decoded body, where the URL parser removes it
- `Broiler.Net.Http.DataUrl.PercentEncodeAsUrlParserWould(string)` in `src/Broiler.Net/Http/DataUrl.cs` - Security=High, Spec=none cited, `8DEDF7`, PENDING
  - Falsified if: a form feed ending the declared type leaves it text/plain, where the URL parser's percent-encoding makes it text/plain%0c
- `Broiler.Net.Http.DataUrl.PercentDecode(string)` in `src/Broiler.Net/Http/DataUrl.cs` - Security=High, Spec=URL s1.3, `F92E9B`, PENDING
  - Falsified if: a percent sign followed by a single hex digit at the end of the body, as in data:,%4, is dropped or decoded instead of kept as written
- `Broiler.Net.Http.DataUrl.HexValue(byte)` in `src/Broiler.Net/Http/DataUrl.cs` - Security=High, Spec=URL s1.3, `7C6F3A`, PENDING
  - Falsified if: a byte outside 0-9, a-f and A-F, such as G, is given a value, so %4G is percent-decoded
- `Broiler.Net.Http.DocumentCookieAccess` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, Spec=none cited, `E3E68C`, PENDING
  - Falsified if: script in an iframe that is cross-site with its top-level document reads a SameSite Strict or Lax cookie through document.cookie
- `Broiler.Net.Http.DocumentCookieAccess.TryGetCookie(DocumentRequestContext, out string)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, Spec=none cited, `FD08EB`, PENDING
  - Falsified if: a document with an opaque origin and an http URL gets its URL's cookies instead of a false return
- `Broiler.Net.Http.DocumentCookieAccess.TrySetCookie(DocumentRequestContext, string)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, Spec=none cited, `8F6D3F`, PENDING
  - Falsified if: a document whose URL is not http or https, such as a data: or file: document, writes a cookie into the store
- `Broiler.Net.Http.DocumentCookieAccess.GetSiteForCookies(DocumentRequestContext)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, Spec=RFC-6265bis s5.2.1, `2E479E`, PENDING
  - Falsified if: a frame same-site with the top-level document but nested inside a cross-site frame gets the top-level origin instead of an opaque one
- `Broiler.Net.Http.DocumentCookieAccess.GetDocumentContext(DocumentRequestContext)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, Spec=RFC-6265bis s5.8.2, `EB5344`, PENDING
  - Falsified if: a document whose site for cookies is opaque is given SameSite status, so script in a cross-site frame reads Strict cookies
- `Broiler.Net.Http.DocumentCookieAccess.TopLevelOrigin(DocumentRequestContext)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, Spec=RFC-6265bis s5.2.1, `6867D4`, PENDING
  - Falsified if: a sandboxed top-level document with an opaque origin yields that opaque origin instead of the origin of its URL
- `Broiler.Net.Http.DocumentRequestContext` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, Spec=none cited, `C641B7`, PENDING
  - Falsified if: a context built by CreateChild resolves TopLevel to anything other than the context at the root of its Parent chain
- `Broiler.Net.Http.DocumentRequestContext.TopLevel` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, Spec=none cited, `1A950A`, PENDING
  - Falsified if: for a frame nested two levels deep TopLevel returns its parent instead of the context that has no Parent
- `Broiler.Net.Http.DocumentRequestContext.IsCookieAverse` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, Spec=none cited, `A356BD`, PENDING
  - Falsified if: a document at a file: URL reports IsCookieAverse false, so document.cookie reaches the cookie store for it
- `Broiler.Net.Http.DocumentRequestContext.CreateTopLevel(Uri, Origin?)` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, Spec=none cited, `F012DD`, PENDING
  - Falsified if: an opaque origin passed for a sandboxed top-level document is replaced by the origin of its URL
- `Broiler.Net.Http.DocumentRequestContext.CreateChild(Uri, Origin?)` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, Spec=none cited, `E290C4`, PENDING
  - Falsified if: the returned context has no Parent, so a cross-site frame is treated as a top-level document for cookies
- `Broiler.Net.Http.FetchHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `5255E0`, PENDING
  - Falsified if: a header name differing only in letter case from a listed one, such as cOOKIE, is classified differently from its canonical spelling
- `Broiler.Net.Http.FetchHeaders.ForbiddenRequestHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `4F997C`, PENDING
  - Falsified if: a name on Fetch's forbidden request-header list, such as Access-Control-Request-Method or Keep-Alive, is missing from the set
- `Broiler.Net.Http.FetchHeaders.MethodOverrideHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `C35A55`, PENDING
  - Falsified if: X-HTTP-Method, X-HTTP-Method-Override or X-Method-Override is missing from the set, so a TRACE override passes as an ordinary header
- `Broiler.Net.Http.FetchHeaders.SafelistedResponseHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `EA8CA6`, PENDING
  - Falsified if: a name other than Cache-Control, Content-Language, Content-Length, Content-Type, Expires, Last-Modified and Pragma is in the set, so a CORS response exposes it without Access-Control-Expose-Headers
- `Broiler.Net.Http.FetchHeaders.NoCorsSafelistedNames` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `D0302E`, PENDING
  - Falsified if: a name other than Accept, Accept-Language, Content-Language and Content-Type is in the set, so a no-cors request keeps a header that needs a preflight
- `Broiler.Net.Http.FetchHeaders.NoCorsUserAgentHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=none cited, `EB37B1`, PENDING
  - Falsified if: a name other than User-Agent, Range, Cache-Control and Pragma is in the set, so a no-cors request keeps it whatever its value
- `Broiler.Net.Http.FetchHeaders.Alphanumeric` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=none cited, `672C2C`, PENDING
  - Falsified if: a character other than an ASCII digit or letter is in the string, widening both the token and the language character sets
- `Broiler.Net.Http.FetchHeaders.TokenChars` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=RFC-9110 s5.6.2, `462BE1`, PENDING
  - Falsified if: a character outside tchar, such as ':', '/' or a space, is in the set, so IsHeaderName accepts a name carrying it
- `Broiler.Net.Http.FetchHeaders.LanguageChars` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `593472`, PENDING
  - Falsified if: a character outside digits, letters, space and the marks *,-.;= such as a double quote is in the set, so an Accept-Language value carrying it skips the preflight
- `Broiler.Net.Http.FetchHeaders.MaximumSafelistedValueLength, MaximumSafelistedTotalLength` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `B0BA12`, PENDING
  - Falsified if: either limit differs from Fetch's 128-byte limit on one value or its 1024-byte limit on the combined safelisted values
- `Broiler.Net.Http.FetchHeaders.IsForbiddenRequestHeader(string, string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `CDBBC3`, PENDING
  - Falsified if: X-HTTP-Method-Override with the value 'GET, trace' is reported not forbidden
- `Broiler.Net.Http.FetchHeaders.IsHeaderName(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `E960E4`, PENDING
  - Falsified if: a name containing ':' or a space is accepted as a header name
- `Broiler.Net.Http.FetchHeaders.IsHeaderValue(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `04A208`, PENDING
  - Falsified if: a value with an embedded LF is accepted, which would put a second header line on the wire
- `Broiler.Net.Http.FetchHeaders.NormalizeHeaderValue(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `334CB0`, PENDING
  - Falsified if: a value ending in CR LF still ends in CR after normalization
- `Broiler.Net.Http.FetchHeaders.IsNoCorsSafelistedRequestHeader(string, string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `C95AFE`, PENDING
  - Falsified if: a safelisted Range value such as bytes=0-10 is reported no-CORS-safelisted
- `Broiler.Net.Http.FetchHeaders.IsForbiddenResponseHeaderName(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `F98A02`, PENDING
  - Falsified if: a lower-case set-cookie2 response header name is not reported forbidden
- `Broiler.Net.Http.FetchHeaders.IsForbiddenMethod(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.1, `0922C4`, PENDING
  - Falsified if: the method 'track' in lower case is not reported forbidden
- `Broiler.Net.Http.FetchHeaders.IsCorsSafelistedMethod(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.1, `1A2CA8`, PENDING
  - Falsified if: a lower-case 'post' is reported CORS-safelisted
- `Broiler.Net.Http.FetchHeaders.IsCorsSafelistedRequestHeader(string, string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `1D24E1`, PENDING
  - Falsified if: a Content-Type of application/json is reported CORS-safelisted
- `Broiler.Net.Http.FetchHeaders.FilterResponseHeaders(IReadOnlyList<KeyValuePair<string, string>>, ResponseTainting, bool)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.6, `A2891A`, PENDING
  - Falsified if: a credentialed CORS response whose Access-Control-Expose-Headers is '*' exposes a non-safelisted header to script
- `Broiler.Net.Http.FetchHeaders.GetCorsUnsafeRequestHeaderNames(IEnumerable<KeyValuePair<string, string>>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `FE0830`, PENDING
  - Falsified if: two Accept headers of 100 characters each are not listed as CORS-unsafe although their combined value exceeds 128
- `Broiler.Net.Http.FetchHeaders.RemoveNoCorsUnsafe(List<KeyValuePair<string, string>>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `CBAA8A`, PENDING
  - Falsified if: a no-cors header list keeps a script-set Content-Type of application/json
- `Broiler.Net.Http.FetchHeaders.Combine(IEnumerable<KeyValuePair<string, string>>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `88867B`, PENDING
  - Falsified if: two headers whose names differ only in letter case produce two entries rather than one value joined with a comma and a space
- `Broiler.Net.Http.FetchHeaders.ParseTokenList(IEnumerable<string>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=none cited, `9F080E`, PENDING
  - Falsified if: an Access-Control-Allow-Headers item with a space inside it, such as 'X A', yields a list instead of null
- `Broiler.Net.Http.FetchHeaders.GetDecodeSplit(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `4A6D27`, PENDING
  - Falsified if: a comma inside a double-quoted string splits the value
- `Broiler.Net.Http.FetchHeaders.AppendQuotedString(string, ref int, StringBuilder)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `D7CBC7`, PENDING
  - Falsified if: an unterminated quoted string that ends in a backslash throws or is read past the end of the input
- `Broiler.Net.Http.FetchHeaders.HasCorsUnsafeByte(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `4D94DF`, PENDING
  - Falsified if: a value containing 0x7F is reported free of CORS-unsafe request-header bytes
- `Broiler.Net.Http.FetchHeaders.IsSafelistedRange(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=FETCH s2.2.2, `FC37EF`, PENDING
  - Falsified if: a range whose first position exceeds its last, such as bytes=10-9, is reported safelisted
- `Broiler.Net.Http.FetchHeaders.CompareDecimal(ReadOnlySpan<char>, ReadOnlySpan<char>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, Spec=none cited, `40BFF3`, PENDING
  - Falsified if: '010' compares as smaller than '9'
- `Broiler.Net.Http.ForgivingBase64` in `src/Broiler.Net/Http/ForgivingBase64.cs` - Security=High, Spec=INFRA s7, `513410`, PENDING
  - Falsified if: YQ fails to decode, where a browser decodes it to the single byte 0x61
- `Broiler.Net.Http.ForgivingBase64.TryDecode(ReadOnlySpan<char>, out byte[]?)` in `src/Broiler.Net/Http/ForgivingBase64.cs` - Security=High, Spec=INFRA s7, `02EADB`, PENDING
  - Falsified if: input holding a vertical tab or a no-break space decodes instead of failing
- `Broiler.Net.Http.LoopbackRouting` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `284B7A`, PENDING
  - Falsified if: a request to a name under .localhost reaches DNS or the system proxy instead of a loopback address
- `Broiler.Net.Http.LoopbackRouting.IsLocalhost(string)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `8C74B6`, PENDING
  - Falsified if: 'localhost.example' is reported as a localhost name
- `Broiler.Net.Http.LoopbackRouting.ConnectAsync(SocketsHttpConnectionContext, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `CC3478`, PENDING
  - Falsified if: a connection to app.localhost is made to an address from DNS instead of ::1 or 127.0.0.1
- `Broiler.Net.Http.LoopbackRouting.ConnectLoopbackAsync(int, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=RFC-8305 s5, `86AD32`, PENDING
  - Falsified if: when both ::1 and 127.0.0.1 accept, the losing connection's socket stays open after the winner is returned
- `Broiler.Net.Http.LoopbackRouting.Release(Task<Stream>, Stream?)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `F955C3`, PENDING
  - Falsified if: a losing attempt that fails leaves its exception unobserved and raises UnobservedTaskException
- `Broiler.Net.Http.LoopbackRouting.ConnectAsync(EndPoint, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `7734B3`, PENDING
  - Falsified if: a connect that fails or is cancelled leaves its Socket undisposed
- `Broiler.Net.Http.LoopbackRouting.IsDirect(Uri)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `2289A9`, PENDING
  - Falsified if: a URL whose host is sub.localhost is not reported direct and so goes through the system proxy
- `Broiler.Net.Http.LoopbackRouting.RoutingHandler` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `5DA485`, PENDING
  - Falsified if: a request to a loopback address is sent through the handler that uses the system proxy
- `Broiler.Net.Http.LoopbackRouting.RoutingHandler.SendAsync(HttpRequestMessage, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `9FDDED`, PENDING
  - Falsified if: an asynchronous request to http://localhost/ is sent through the proxy-using invoker
- `Broiler.Net.Http.LoopbackRouting.RoutingHandler.Send(HttpRequestMessage, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `9FBC00`, PENDING
  - Falsified if: a synchronous request to http://localhost/ is sent through the proxy-using invoker
- `Broiler.Net.Http.LoopbackRouting.RoutingHandler.Route(HttpRequestMessage)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, Spec=none cited, `9003C7`, PENDING
  - Falsified if: a request to http://[::1]:8080/ is given the proxy-using invoker
- `Broiler.Net.Http.MimeType` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.4, `3E1EB0`, PENDING
  - Falsified if: a parameter name holding the Kelvin sign, which .NET lowercases to k, is kept as the parameter key
- `Broiler.Net.Http.MimeType.HttpWhitespace` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=FETCH s2.2, `3F35BE`, PENDING
  - Falsified if: the string holds a character other than tab, line feed, carriage return and space, such as a form feed, so a type padded with it parses
- `Broiler.Net.Http.MimeType.HttpTokenCodePoints` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s3, `F79DD0`, PENDING
  - Falsified if: a character outside the HTTP token code points, such as a colon, a slash or a space, is in the set, so a type or subtype carrying it parses
- `Broiler.Net.Http.MimeType.JavaScriptEssences` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.6, `3A2CFD`, PENDING
  - Falsified if: an essence the standard does not list, such as text/javascript1.6, is in the array, so a response of that type counts as script
- `Broiler.Net.Http.MimeType.IsImage` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.6, `7206AF`, PENDING
  - Falsified if: image/svg+xml is not reported an image MIME type, or imagex/png is
- `Broiler.Net.Http.MimeType.IsHtml` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.6, `B9429B`, PENDING
  - Falsified if: text/html;charset=utf-8 is not reported an HTML MIME type, or application/xhtml+xml is
- `Broiler.Net.Http.MimeType.IsXml` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.6, `5E58A7`, PENDING
  - Falsified if: application/xhtml+xml is not reported an XML MIME type, or application/xml-dtd is
- `Broiler.Net.Http.MimeType.IsJson` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.6, `518FD9`, PENDING
  - Falsified if: application/ld+json is not reported a JSON MIME type, or application/jsonp is
- `Broiler.Net.Http.MimeType.IsJavaScript` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.6, `2EA5BD`, PENDING
  - Falsified if: text/javascript;charset=utf-8 is not reported a JavaScript MIME type because of its parameter
- `Broiler.Net.Http.MimeType.IsJavaScriptEssenceMatch(string?)` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.6, `347BFA`, PENDING
  - Falsified if: a string that matches an essence only under Unicode case folding, such as text/ecmascript spelled with a long s, is reported a match
- `Broiler.Net.Http.MimeType.TryParse(string?, out MimeType?)` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.4, `923060`, PENDING
  - Falsified if: a repeated parameter, as in text/html;charset=utf-8;charset=x, keeps the later value instead of the first
- `Broiler.Net.Http.MimeType.ToString()` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.5, `239C03`, PENDING
  - Falsified if: two calls on one instance return different strings, or a serialization does not parse back to the same type
- `Broiler.Net.Http.MimeType.Serialize()` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s4.5, `A5B26C`, PENDING
  - Falsified if: a parameter value holding a double quote or a backslash is written without its escape, so the serialization parses back to different parameters
- `Broiler.Net.Http.MimeType.CollectQuotedStringValue(ReadOnlySpan<char>, ref int)` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=FETCH s2.2, `94D532`, PENDING
  - Falsified if: an unterminated quoted string that ends in a backslash throws or loses the backslash
- `Broiler.Net.Http.MimeType.IsQuotedStringTokenText(string)` in `src/Broiler.Net/Http/MimeType.cs` - Security=High, Spec=MIMESNIFF s3, `6FAED7`, PENDING
  - Falsified if: a value holding U+0100 or a control other than tab is accepted as a parameter value
- `Broiler.Net.Http.RequestContext` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, Spec=none cited, `445DA9`, PENDING
  - Falsified if: a RequestContext initialized with only Destination and Client is not no-cors with credentials include and redirect follow
- `Broiler.Net.Http.RequestContext.IsNavigation` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, Spec=none cited, `8EC5E0`, PENDING
  - Falsified if: a request whose Mode is not Navigate reports IsNavigation true, so SendCore forces its credentials mode to include
- `Broiler.Net.Http.RequestContext.IsTopLevelNavigation` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, Spec=none cited, `B02FFE`, PENDING
  - Falsified if: a navigate-mode request with an iframe destination reports IsTopLevelNavigation true, so SameSite=Lax cookies go with a cross-site frame load
- `Broiler.Net.Http.RequestContext.TopLevelNavigation(DocumentRequestContext?)` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, Spec=none cited, `1F0EB2`, PENDING
  - Falsified if: TopLevelNavigation drops the initiator from Client, so a navigation started by a cross-site page is sent as same-site with its SameSite=Strict cookies
- `Broiler.Net.Http.RequestContext.NestedNavigation(DocumentRequestContext, DocumentRequestContext?, RequestDestination)` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, Spec=none cited, `DD0FA7`, PENDING
  - Falsified if: NestedNavigation stores the initiator rather than the container argument as Container, so a frame inside a cross-site container is sent as same-site
- `Broiler.Net.Http.RequestContext.Subresource(DocumentRequestContext, RequestDestination, CorsSetting)` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, Spec=none cited, `E33E46`, PENDING
  - Falsified if: Subresource with CorsSetting.Anonymous yields credentials include instead of same-origin, so cookies go with an anonymous cross-origin request
- `Broiler.Net.Http.RequestContext.Fetch(DocumentRequestContext, RequestMode, CredentialsMode, RedirectMode)` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, Spec=none cited, `B365A9`, PENDING
  - Falsified if: Fetch accepts RequestMode.Navigate, so a script fetch() is treated as a navigation and sends credentials include
- `Broiler.Net.Http.CorsSettings` in `src/Broiler.Net/Http/RequestTypes.cs` - Security=High, Spec=none cited, `D5B94C`, PENDING
  - Falsified if: a member here reads a missing crossorigin attribute as anything other than CorsSetting.None, so an element without the attribute is fetched in cors mode
- `Broiler.Net.Http.CorsSettings.Parse(string?)` in `src/Broiler.Net/Http/RequestTypes.cs` - Security=High, Spec=none cited, `96CB78`, PENDING
  - Falsified if: Parse returns UseCredentials for a value that is not an ASCII case-insensitive match of use-credentials, such as one with a trailing space
- `Broiler.Net.Http.IBrowserRequestTransport` in `src/Broiler.Net/Http/Transport.cs` - Security=High, Spec=none cited, `A9B41A`, PENDING
  - Falsified if: an implementation falls back to a process-global cookie store when it has no profile, so two profiles share cookies
- `Broiler.Net.Http.IBrowserRequestTransport.SendAsync(HttpRequestMessage, RequestContext, CancellationToken)` in `src/Broiler.Net/Http/Transport.cs` - Security=High, Spec=none cited, `B27437`, PENDING
  - Falsified if: SendAsync returns a response for a request whose CORS check failed instead of throwing TransportException with TransportError.Cors
- `Broiler.Net.Http.IBrowserRequestTransport.Send(HttpRequestMessage, RequestContext, CancellationToken)` in `src/Broiler.Net/Http/Transport.cs` - Security=High, Spec=none cited, `59A3D6`, PENDING
  - Falsified if: Send follows a redirect for a context whose Redirect is RedirectMode.Error instead of throwing TransportException
- `Broiler.Net.Http.IDocumentCookieAccess` in `src/Broiler.Net/Http/Transport.cs` - Security=High, Spec=none cited, `C108B0`, PENDING
  - Falsified if: an implementation hands the binding the transport's CookieStore, through which document.cookie reads an HttpOnly cookie
- `Broiler.Net.Http.IDocumentCookieAccess.TryGetCookie(DocumentRequestContext, out string)` in `src/Broiler.Net/Http/Transport.cs` - Security=High, Spec=none cited, `397E15`, PENDING
  - Falsified if: TryGetCookie includes an HttpOnly cookie in the string it returns for document.cookie
- `Broiler.Net.Http.IDocumentCookieAccess.TrySetCookie(DocumentRequestContext, string)` in `src/Broiler.Net/Http/Transport.cs` - Security=High, Spec=none cited, `079888`, PENDING
  - Falsified if: a document.cookie write through TrySetCookie replaces an existing HttpOnly cookie of the same name, domain and path
- `Broiler.Net.Http.TransportResponse` in `src/Broiler.Net/Http/TransportResponse.cs` - Security=High, Spec=none cited, `3B9949`, PENDING
  - Falsified if: a member here other than Headers, StatusCode and Message returns a Set-Cookie value or an opaque response's real status
- `Broiler.Net.Http.TransportResponse.ScriptVisibleStatusCode` in `src/Broiler.Net/Http/TransportResponse.cs` - Security=High, Spec=FETCH s2.2.6, `131ED6`, PENDING
  - Falsified if: ScriptVisibleStatusCode returns the real status for an OpaqueRedirect response instead of 0
- `Broiler.Net.Http.TransportResponse.GetScriptVisibleHeaders()` in `src/Broiler.Net/Http/TransportResponse.cs` - Security=High, Spec=FETCH s2.2.6, `5FC432`, PENDING
  - Falsified if: a Cors response to a request with credentials mode include exposes every header when Access-Control-Expose-Headers is *
- `Broiler.Net.Sites.Origin` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, Spec=none cited, `A81584`, PENDING
  - Falsified if: an Origin holds a host that TryGetHttpHost did not canonicalize, so https://A.example and https://a.example compare unequal
- `Broiler.Net.Sites.Origin.Origin()` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, Spec=none cited, `11C0C8`, PENDING
  - Falsified if: the parameterless constructor leaves IsOpaque false, so two distinct opaque origins compare equal through their empty scheme and host
- `Broiler.Net.Sites.Origin.CreateOpaque()` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, Spec=none cited, `09C481`, PENDING
  - Falsified if: CreateOpaque returns a cached instance, so two separately created opaque origins are same-origin
- `Broiler.Net.Sites.Origin.FromUrl(Uri)` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, Spec=none cited, `86FBEE`, PENDING
  - Falsified if: FromUrl of a blob: URL wrapping a data: or file: URL returns a tuple origin instead of a new opaque origin
- `Broiler.Net.Sites.Origin.Equals(Origin?)` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, Spec=none cited, `3BB49C`, PENDING
  - Falsified if: Equals returns true for tuple origins that differ only in port, such as https://a.example and https://a.example:8443
- `Broiler.Net.Sites.Origin.Equals(object?)` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, Spec=none cited, `41A6C8`, PENDING
  - Falsified if: Equals(object) disagrees with Equals(Origin?) for the same Origin argument
- `Broiler.Net.Sites.Origin.ToString()` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, Spec=none cited, `59477B`, PENDING
  - Falsified if: ToString of https://a.example:8443 omits :8443, so a response allowing https://a.example passes the CORS check for that page
- `Broiler.Net.Sites.SiteMatching` in `src/Broiler.Net/Sites/SiteMatching.cs` - Security=High, Spec=none cited, `94027E`, PENDING
  - Falsified if: a member here compares sites without their scheme, so http://a.example and https://a.example are same-site
- `Broiler.Net.Sites.SiteMatching.GetSite(this ISiteResolver, Origin)` in `src/Broiler.Net/Sites/SiteMatching.cs` - Security=High, Spec=none cited, `535C1C`, PENDING
  - Falsified if: GetSite returns equal sites for two different IP-address origins, such as https://192.0.2.1 and https://192.0.2.2
- `Broiler.Net.Sites.SiteMatching.IsSameSite(this ISiteResolver, Origin, Origin)` in `src/Broiler.Net/Sites/SiteMatching.cs` - Security=High, Spec=none cited, `827F64`, PENDING
  - Falsified if: IsSameSite returns true when one origin is opaque and the other is a tuple origin
- `Broiler.Net.Sites.SchemefulSite` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `0248F9`, PENDING
  - Falsified if: two SchemefulSite values with the same host but the schemes "http" and "https" compare equal
- `Broiler.Net.Sites.ISiteResolver` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `6633F4`, PENDING
  - Falsified if: an implementation returns a public suffix such as co.uk as the registrable domain of a host below it
- `Broiler.Net.Sites.ISiteResolver.GetSite(Uri)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `40D4C8`, PENDING
  - Falsified if: an implementation returns equal sites for https://a.example/ and http://a.example/
- `Broiler.Net.Sites.ISiteResolver.IsPublicSuffix(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `4F36F7`, PENDING
  - Falsified if: an implementation reports co.uk as not a public suffix, so a cookie with a Domain attribute of co.uk is accepted
- `Broiler.Net.Sites.ISiteResolver.GetRegistrableDomain(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `C32265`, PENDING
  - Falsified if: an implementation returns co.uk instead of example.co.uk as the registrable domain of www.example.co.uk
- `Broiler.Net.Sites.SiteResolver` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `3793E1`, PENDING
  - Falsified if: with the bundled list GetRegistrableDomain("a.b.example.co.uk") returns anything other than "example.co.uk"
- `Broiler.Net.Sites.SiteResolver.Bundled` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `9617D0`, PENDING
  - Falsified if: the Lazy is built in a mode other than ExecutionAndPublication, so concurrent first reads of Default can run LoadBundled more than once
- `Broiler.Net.Sites.SiteResolver.Default` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `C4EB23`, PENDING
  - Falsified if: two threads reading Default at the same time on first use receive different SiteResolver instances
- `Broiler.Net.Sites.SiteResolver.SiteResolver(TextReader)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `0EFE1F`, PENDING
  - Falsified if: the exception rule line "!www.ck" is stored anywhere other than as "www.ck" in the exception set
- `Broiler.Net.Sites.SiteResolver.GetSite(Uri)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `C4D380`, PENDING
  - Falsified if: with the bundled list, GetSite for https://a.example.co.uk/ and https://b.example.co.uk/ returns unequal sites
- `Broiler.Net.Sites.SiteResolver.IsPublicSuffix(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `4DE49B`, PENDING
  - Falsified if: IsPublicSuffix("www.ck") returns true although the bundled list has the exception rule !www.ck
- `Broiler.Net.Sites.SiteResolver.GetRegistrableDomain(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `22AA41`, PENDING
  - Falsified if: under the bundled wildcard rule *.ck, GetRegistrableDomain("a.b.foo.ck") returns anything but "b.foo.ck"
- `Broiler.Net.Sites.SiteResolver.SuffixLabels(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `DF17A7`, PENDING
  - Falsified if: the single-label host "ck", matched only by the wildcard rule *.ck, gets a suffix count of 2 instead of 1
- `Broiler.Net.Sites.HostNames` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `863BF4`, PENDING
  - Falsified if: a host containing a forbidden domain code point, such as "a^b.example", canonicalizes successfully
- `Broiler.Net.Sites.HostNames.TryGetHttpHost(Uri?, out string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `10A1E5`, PENDING
  - Falsified if: the absolute URL ws://example.com/ returns true with a host
- `Broiler.Net.Sites.HostNames.TryCanonicalize(string?, out string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=URL s3.5, `775591`, PENDING
  - Falsified if: the host "0x7f.1" canonicalizes to anything other than "127.0.0.1"
- `Broiler.Net.Sites.HostNames.IsIp(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `FED054`, PENDING
  - Falsified if: the canonical IPv4 host "10.0.0.1" is reported as not an IP, so GetRegistrableDomain returns "0.1" for it
- `Broiler.Net.Sites.HostNames.DomainMatches(string, string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=RFC-6265bis s5.1.3, `9B8A00`, PENDING
  - Falsified if: DomainMatches("evilexample.com", "example.com") returns true
- `Broiler.Net.Sites.HostNames.IsSecure(Uri)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `485A05`, PENDING
  - Falsified if: http://localhost.evil.example/ is reported as a secure context
- `Broiler.Net.Sites.HostNames.IsSecure(string, string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `8BA7B0`, PENDING
  - Falsified if: an http host that is not loopback, such as "notlocalhost" or "128.0.0.1", returns true
- `Broiler.Net.Sites.HostNames.DomainMatchCandidates(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `327BBC`, PENDING
  - Falsified if: the IP host "10.0.0.1" yields a parent candidate such as "0.0.1"
- `Broiler.Net.Sites.HostNames.IsForbiddenDomainCodePoint(char)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=none cited, `D00D51`, PENDING
  - Falsified if: one of the characters '^', '%' or U+007F returns false
- `Broiler.Net.Sites.HostNames.EndsInNumber(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=URL s3.5, `C93B17`, PENDING
  - Falsified if: the host "a.0x1f" is kept as a domain because its last label is not seen as a number
- `Broiler.Net.Sites.HostNames.TryParseIPv4(string, out string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=URL s3.5, `FC9FA6`, PENDING
  - Falsified if: a four-part host with a part above 255, such as "256.0.0.1", yields an address instead of failing
- `Broiler.Net.Sites.HostNames.TryParseIPv4Number(string, out ulong)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, Spec=URL s3.5, `36D153`, PENDING
  - Falsified if: an octal part containing 8 or 9, such as "09", parses instead of failing

## 10. What This Record Does Not Say

It is not an approval of the component, and a full table above would not be one either. It
records which declarations somebody stated a decision about, and against which version of
each. It does not record what they read, how long they spent, or whether they were right.

A fingerprint is six hex characters of SHA-256 over a declaration's token texts. It answers
whether a unit changed since a decision was recorded against it. It is not a collision-free
identifier across units and it is not a cryptographic commitment, so it detects a change and
does not resist a forger with commit access.

An assessment is a comment, so changing one moves no fingerprint anywhere, and nothing
mechanical checks that it is right; the check holds its values to their vocabularies and no
further.

297 of the 297 assessed units declare `Origin=AI`. Reading a declaration is the only thing
that makes it read.

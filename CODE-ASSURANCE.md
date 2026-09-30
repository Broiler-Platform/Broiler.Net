# Broiler.Net Code Assurance

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.Net`, which rewrites this file,
`HUMAN_REVIEW.md`, `assurance.manifest.json` and every generated source header from the
product tree.

**No code unit in this component carries a decision on its human line yet.** This report
records that absence precisely. It is not a claim that the code is reviewed, assured or safe,
and the figures below are the measurement of how far from that claim the per-unit record is.

## Summary

| Metric | Value |
|---|---:|
| Files scanned | 18 |
| Files not covered | 0 |
| Files carrying an annotation | 18 |
| Code units | 433 |
| Relevant | 270 |
| Exempt by predicate | 163 |
| Annotated | 270 of 270 (100%) |
| Human reviewed | 0 of 270 (0%) |
| Unverified | 270 |

## Review states

| State | Count |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 270 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 163 |

## IP risk

| Value | Units |
|---|---:|
| None | 108 |
| Low | 160 |
| Medium | 2 |
| High | 0 |
| Unknown | 0 |
| *not annotated* | 0 |

## Security risk

| Value | Units |
|---|---:|
| None | 30 |
| Low | 38 |
| Medium | 17 |
| High | 185 |
| Critical | 0 |
| *not annotated* | 0 |

## Resource impact

| Metric | Value |
|---|---:|
| Maximum | 7 / 10 |
| Average over annotated units | 1.9 / 10 |
| Units scored | 270 |

## High-security review areas

- `Broiler.Net.Cookies.CookieParser` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.Parse(string, DateTimeOffset)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.Parse(ReadOnlySpan<byte>, DateTimeOffset)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.Trim(string)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.TryMaxAge(string, out long)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.TryParseDate(string, out DateTimeOffset)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.Number(string)` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.DateDelimiters()` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.TimeToken()` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.DayToken()` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieParser.YearToken()` in `src/Broiler.Net/Cookies/CookieParser.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookiePolicy` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookiePolicy.Accept(ParsedCookie, CookieRequestContext, bool, out CookieKey?)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookiePolicy.TryBeginRetrieval(CookieRequestContext, bool, out RetrievalScope)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookiePolicy.CanRetrieve(CookieRecord, in RetrievalScope, ref bool?)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookiePolicy.IsSafeMethod(string?)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookiePolicy.Prefix(string, string)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookiePolicy.DefaultPath(Uri)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookiePolicy.PathMatches(string, string)` in `src/Broiler.Net/Cookies/CookiePolicy.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Changed` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Revision` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.ReceiveResponseCookie(string, CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.ReceiveResponseCookies(IEnumerable<string>, CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.SetDocumentCookie(string, CookieDocumentContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.ReceiveField(string, CookieRequestContext, ref List<Exception>?)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Receive(CookieParseResult, CookieRequestContext, bool, ref List<Exception>?)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.BuildRequestHeader(CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.GetDocumentCookies(CookieDocumentContext)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Retrieve(CookieRequestContext, bool)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Snapshot()` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Snapshot(out long)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Clear()` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.PruneExpired()` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Live(CookieRecord, DateTimeOffset)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.EnforceLimits(Entry, List<CookieChange>)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Victim(IEnumerable<Entry>, bool)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Add(CookieRecord)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Unindex(Entry)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.Publish(CookieChangeBatch?, ref List<Exception>?)` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieStore.BucketKey` in `src/Broiler.Net/Cookies/CookieStore.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookiePartitionKey` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieRequestContext` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.CookieKey` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.ICookieService` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.ICookieService.ReceiveResponseCookie(string, CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.ICookieService.BuildRequestHeader(CookieRequestContext)` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.ICookieService.SetDocumentCookie(string, CookieDocumentContext)` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Cookies.ICookieService.GetDocumentCookies(CookieDocumentContext)` in `src/Broiler.Net/Cookies/CookieTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.OwnedHeaders` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.BadPorts` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.BrowserNetworkSession(BrowserNetworkSessionOptions?)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.SendAsync(HttpRequestMessage, RequestContext, CancellationToken)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.Send(HttpRequestMessage, RequestContext, CancellationToken)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.TryGetCookie(DocumentRequestContext, out string)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.TrySetCookie(DocumentRequestContext, string)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.GetSiteForCookies(DocumentRequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.Dispose()` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.CheckSend(HttpRequestMessage, RequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.CheckContext(RequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.SendCore(HttpRequestMessage, RequestContext, bool, CancellationToken)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.Taint(RequestContext, Origin?, Uri, ResponseTainting)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.TaintsOrigin(Origin?, Uri, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.LeaveOrigin(List<KeyValuePair<string, string>>, Uri, Uri, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.PreflightAsync(Uri, string, List<KeyValuePair<string, string>>, string, bool, bool, CancellationToken)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.GetAuthorHeaders(HttpRequestMessage, RequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.ResolveLocation(Uri, HeaderStringValues)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.PassesCorsCheck(HttpResponseMessage, string, bool)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.GetCombined(HttpResponseMessage, string)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.GetValues(HttpResponseMessage, string)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.GetSameSite(RequestContext, Origin?, Origin?, List<Uri>)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.GetPartitionKey(RequestContext, DocumentRequestContext?, Origin?, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.TopLevelOrigin(DocumentRequestContext)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.IsCrossOrigin(Origin?, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.IsDowngrade(Origin, Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.IsFetchable(Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.WithCanonicalHost(Uri)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.CreateHandler(BrowserNetworkSessionOptions)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.BrowserNetworkSession.CreateSocketsHandler(BrowserNetworkSessionOptions, bool)` in `src/Broiler.Net/Http/BrowserNetworkSession.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentCookieAccess` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentCookieAccess.TryGetCookie(DocumentRequestContext, out string)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentCookieAccess.TrySetCookie(DocumentRequestContext, string)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentCookieAccess.GetSiteForCookies(DocumentRequestContext)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentCookieAccess.GetDocumentContext(DocumentRequestContext)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentCookieAccess.TopLevelOrigin(DocumentRequestContext)` in `src/Broiler.Net/Http/DocumentCookieAccess.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentRequestContext` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentRequestContext.TopLevel` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentRequestContext.IsCookieAverse` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentRequestContext.CreateTopLevel(Uri, Origin?)` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.DocumentRequestContext.CreateChild(Uri, Origin?)` in `src/Broiler.Net/Http/DocumentRequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.ForbiddenRequestHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.MethodOverrideHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.SafelistedResponseHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.NoCorsSafelistedNames` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.NoCorsUserAgentHeaders` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.Alphanumeric` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.TokenChars` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.LanguageChars` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.MaximumSafelistedValueLength, MaximumSafelistedTotalLength` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.IsForbiddenRequestHeader(string, string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.IsHeaderName(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.IsHeaderValue(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.NormalizeHeaderValue(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.IsNoCorsSafelistedRequestHeader(string, string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.IsForbiddenResponseHeaderName(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.IsForbiddenMethod(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.IsCorsSafelistedMethod(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.IsCorsSafelistedRequestHeader(string, string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.FilterResponseHeaders(IReadOnlyList<KeyValuePair<string, string>>, ResponseTainting, bool)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.GetCorsUnsafeRequestHeaderNames(IEnumerable<KeyValuePair<string, string>>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.RemoveNoCorsUnsafe(List<KeyValuePair<string, string>>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.Combine(IEnumerable<KeyValuePair<string, string>>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.ParseTokenList(IEnumerable<string>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.GetDecodeSplit(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.AppendQuotedString(string, ref int, StringBuilder)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.HasCorsUnsafeByte(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.GetMimeEssence(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.IsSafelistedRange(string)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.FetchHeaders.CompareDecimal(ReadOnlySpan<char>, ReadOnlySpan<char>)` in `src/Broiler.Net/Http/FetchHeaders.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.IsLocalhost(string)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.ConnectAsync(SocketsHttpConnectionContext, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.ConnectLoopbackAsync(int, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.Release(Task<Stream>, Stream?)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.ConnectAsync(EndPoint, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.IsDirect(Uri)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.RoutingHandler` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.RoutingHandler.SendAsync(HttpRequestMessage, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.RoutingHandler.Send(HttpRequestMessage, CancellationToken)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.LoopbackRouting.RoutingHandler.Route(HttpRequestMessage)` in `src/Broiler.Net/Http/LoopbackRouting.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.RequestContext` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.RequestContext.IsNavigation` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.RequestContext.IsTopLevelNavigation` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.RequestContext.TopLevelNavigation(DocumentRequestContext?)` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.RequestContext.NestedNavigation(DocumentRequestContext, DocumentRequestContext?, RequestDestination)` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.RequestContext.Subresource(DocumentRequestContext, RequestDestination, CorsSetting)` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.RequestContext.Fetch(DocumentRequestContext, RequestMode, CredentialsMode, RedirectMode)` in `src/Broiler.Net/Http/RequestContext.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.CorsSettings` in `src/Broiler.Net/Http/RequestTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.CorsSettings.Parse(string?)` in `src/Broiler.Net/Http/RequestTypes.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.IBrowserRequestTransport` in `src/Broiler.Net/Http/Transport.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.IBrowserRequestTransport.SendAsync(HttpRequestMessage, RequestContext, CancellationToken)` in `src/Broiler.Net/Http/Transport.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.IBrowserRequestTransport.Send(HttpRequestMessage, RequestContext, CancellationToken)` in `src/Broiler.Net/Http/Transport.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.IDocumentCookieAccess` in `src/Broiler.Net/Http/Transport.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.IDocumentCookieAccess.TryGetCookie(DocumentRequestContext, out string)` in `src/Broiler.Net/Http/Transport.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.IDocumentCookieAccess.TrySetCookie(DocumentRequestContext, string)` in `src/Broiler.Net/Http/Transport.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.TransportResponse` in `src/Broiler.Net/Http/TransportResponse.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.TransportResponse.ScriptVisibleStatusCode` in `src/Broiler.Net/Http/TransportResponse.cs` - Security=High, human line PENDING
- `Broiler.Net.Http.TransportResponse.GetScriptVisibleHeaders()` in `src/Broiler.Net/Http/TransportResponse.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.Origin` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.Origin.Origin()` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.Origin.CreateOpaque()` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.Origin.FromUrl(Uri)` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.Origin.Equals(Origin?)` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.Origin.Equals(object?)` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.Origin.ToString()` in `src/Broiler.Net/Sites/Origin.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteMatching` in `src/Broiler.Net/Sites/SiteMatching.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteMatching.GetSite(this ISiteResolver, Origin)` in `src/Broiler.Net/Sites/SiteMatching.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteMatching.IsSameSite(this ISiteResolver, Origin, Origin)` in `src/Broiler.Net/Sites/SiteMatching.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SchemefulSite` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.ISiteResolver` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.ISiteResolver.GetSite(Uri)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.ISiteResolver.IsPublicSuffix(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.ISiteResolver.GetRegistrableDomain(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteResolver` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteResolver.Bundled` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteResolver.Default` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteResolver.SiteResolver(TextReader)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteResolver.GetSite(Uri)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteResolver.IsPublicSuffix(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteResolver.GetRegistrableDomain(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.SiteResolver.SuffixLabels(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.TryGetHttpHost(Uri?, out string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.TryCanonicalize(string?, out string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.IsIp(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.DomainMatches(string, string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.IsSecure(Uri)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.IsSecure(string, string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.DomainMatchCandidates(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.IsForbiddenDomainCodePoint(char)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.EndsInNumber(string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.TryParseIPv4(string, out string)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING
- `Broiler.Net.Sites.HostNames.TryParseIPv4Number(string, out ulong)` in `src/Broiler.Net/Sites/SiteResolver.cs` - Security=High, human line PENDING

## Falsification criteria

| Metric | Value |
|---|---:|
| Units carrying a criterion | 237 |
| Units required to carry one | 185 |
| Required and missing | 0 |

A `Broiler-Falsified-If:` line states, at the declaration, the observation that would make
the unit wrong. `Security=High` says a unit is risky, which is a set and not a test; the
criterion is the test. It is required where `Security` is `High` or `Critical`, permitted
elsewhere, and `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Net` names every unit that owes one and carries none.

The line is a comment, so it is outside every fingerprint by construction: rewording a
criterion moves no recorded value here, in a file header or in
`assurance.manifest.json`, and invalidates nothing. That is the intended reading - a
criterion is an instruction to whoever reads the unit, not part of what a review is bound to.

## Exemption

Exemption is decided by one predicate in `CSharpAssuranceScanner`, not per unit, so
that the rule is reviewable in one place rather than in several hundred.

| Case | Units |
|---|---:|
| TrivialPropertyOrAccessor | 36 |
| ParameterAssigningConstructor | 1 |
| TrivialExpressionBodiedMember | 2 |
| CompilerSuppliedRecordOrEnumMember | 44 |
| DelegatingOverrideOrOperator | 0 |
| InsideAssemblyMarker | 0 |
| FieldDeclaringStorage | 18 |
| EnumMemberOfADeclaredVocabulary | 62 |
| DeclaredInSource | 0 |

## Per-unit exemptions

| Metric | Value |
|---|---:|
| Per-unit exemptions | 0 |

A per-unit `EXEMPT=<reason>` line exempts one unit by a reason a human wrote, for what the
predicate cannot see. Nothing mechanical checks that the reason is true, that it describes
the unit it sits on, or that it says anything at all, so every use is counted and named
here.

No unit in this component states a per-unit exemption.

## Files not covered

No file under a covered project's directory, and no file a covered project compiles in
through a `<Compile Include>` it states, is left out of the record.

## Change detection

`assurance.manifest.json` lists **every** code unit in the covered assembly -
433 of them, exempt and relevant alike - with the fingerprint of its declaration.
This manifest is a change-detection record, not a review. A unit listed there is watched, not reviewed:
the entry records what the declaration's tokens hashed to when the generator last ran, and
nothing else. What the manifest adds is that a unit the exemption predicate treats as
trivial is no longer invisible: a semantic change to one moves a value in a generated file
the check compares byte for byte. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Net` holds the manifest to the tree.

Beside the units it lists **every covered file** - 18 of them - with a
fingerprint over the complete token stream of its compilation unit. A unit entry exists only
for a declaration kind the scanner enumerates, and an enumeration is a whitelist: an
`[assembly: ...]` attribute is a member of nothing and can be in no unit at all.
Nothing in a covered file can change without something moving here, whatever kind of declaration it is. Comments are outside the stream, because a token's
text is its own characters, so the generated header above and the annotation lines below move
no file fingerprint - which is what lets one generation be a fixed point.

## Verification

The generator and the check are one computation: `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Net` works out what the
generator would write and compares it with the tree byte for byte, so a record edited by
hand, or left behind by code that moved, is reported rather than trusted.

| Mode | Command | Effect |
|---|---|---|
| Generate | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.Net` | Fills every `Fingerprint=TBF`, refreshes a decision the code has outrun into `STALE; Previous=...`, rewrites the generated headers, `HUMAN_REVIEW.md`, `assurance.manifest.json` and this file. |
| Check | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Net` | Reports every generated artefact that is not byte-identical to what the generator would produce, every relevant unit with no annotation, every annotation this system cannot read, every fingerprint out of date and every unit at the top of the security vocabulary without a criterion. |
| Release | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.Net --release` | The check, and additionally every relevant unit left in a state that blocks a release. |

The fingerprint is six hex characters - 24 bits - of SHA-256 over the declaration's token
texts, joined by single spaces. Trivia is excluded because a token's text is its own
characters and never the comments or whitespace around it, so `dotnet format` moves no
fingerprint and an annotation is never part of what it describes. The value answers whether a
unit changed since it was reviewed. It is not a collision-free identifier across units and it
is not a cryptographic commitment.

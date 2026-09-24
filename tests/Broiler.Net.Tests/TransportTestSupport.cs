using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using Broiler.Net.Cookies;
using Broiler.Net.Http;
using Broiler.Net.Sites;

namespace Broiler.Net.Tests;

/// <summary>One request as the handler saw it.</summary>
internal sealed record RecordedHop(string Method, Uri Url, Version Version, IReadOnlyList<KeyValuePair<string, string>> Headers, byte[]? Body, bool Sync)
{
    public string? Header(string name)
    {
        var values = Headers.Where(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Select(h => h.Value).ToArray();
        return values.Length == 0 ? null : string.Join(", ", values);
    }

    public string Target => $"{Method} {Url.GetLeftPart(UriPartial.Query)}";
}

/// <summary>A body that reports disposal, to prove the transport releases responses it does not return.</summary>
internal sealed class TrackingContent(string body = "ok") : ByteArrayContent(Encoding.UTF8.GetBytes(body))
{
    public bool Disposed { get; private set; }
    protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
}

/// <summary>Scripted handler overriding both Send and SendAsync; routes by URL without fragment.</summary>
internal sealed class ScriptedHandler : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new();
    private readonly List<RecordedHop> _hops = [];
    private readonly List<HttpResponseMessage> _responses = [];

    public IReadOnlyList<RecordedHop> Hops { get { lock (_hops) return _hops.ToArray(); } }
    public IReadOnlyList<HttpResponseMessage> Responses { get { lock (_responses) return _responses.ToArray(); } }
    public Func<HttpRequestMessage, CancellationToken, bool, HttpResponseMessage>? Intercept { get; set; }

    public ScriptedHandler On(string url, Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _routes[url] = respond;
        return this;
    }

    public ScriptedHandler On(string url, int status, params (string Name, string Value)[] headers) => On(url, _ => Reply(status, headers));

    public ScriptedHandler Redirect(string url, int status, string location, params (string Name, string Value)[] headers) =>
        On(url, status, [("Location", location), .. headers]);

    public static HttpResponseMessage Reply(int status, params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage((HttpStatusCode)status) { Content = new TrackingContent() };
        foreach (var (name, value) in headers)
            if (!response.Headers.TryAddWithoutValidation(name, value)) response.Content.Headers.TryAddWithoutValidation(name, value);
        return response;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Handle(request, cancellationToken, sync: true);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try { return Task.FromResult(Handle(request, cancellationToken, sync: false)); }
        catch (OperationCanceledException error) { return Task.FromCanceled<HttpResponseMessage>(error.CancellationToken); }
    }

    private HttpResponseMessage Handle(HttpRequestMessage request, CancellationToken cancellationToken, bool sync)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<KeyValuePair<string, HeaderStringValues>> fields = request.Headers.NonValidated;
        if (request.Content is not null) fields = fields.Concat(request.Content.Headers.NonValidated);
        var headers = fields.SelectMany(h => h.Value.Select(v => new KeyValuePair<string, string>(h.Key, v))).ToArray();
        byte[]? body = null;
        if (request.Content is not null)
        {
            using var stream = request.Content.ReadAsStream(cancellationToken);
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            body = buffer.ToArray();
        }
        lock (_hops) _hops.Add(new(request.Method.Method, request.RequestUri!, request.Version, headers, body, sync));
        var response = Intercept?.Invoke(request, cancellationToken, sync) ??
            (_routes.TryGetValue(request.RequestUri!.GetLeftPart(UriPartial.Query), out var respond) ? respond(request) : Reply(200));
        response.RequestMessage = request;
        lock (_responses) _responses.Add(response);
        return response;
    }
}

internal static class TransportTest
{
    public static readonly ISiteResolver Sites = SiteResolver.Default;

    public static BrowserNetworkSession Session(ScriptedHandler handler, BrowserNetworkSessionOptions? options = null)
    {
        options ??= new();
        return new(options with { Handler = handler, Cookies = options.Cookies ?? new CookieStore(clock: new TestClock()) });
    }

    public static DocumentRequestContext Top(string url, Origin? origin = null) => DocumentRequestContext.CreateTopLevel(new Uri(url), origin);

    public static HttpRequestMessage Request(string method, string url, string? body = null, params (string Name, string Value)[] headers)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (body is not null) request.Content = new StringContent(body);
        foreach (var (name, value) in headers)
        {
            if (request.Headers.TryAddWithoutValidation(name, value) || request.Content is null) continue;
            request.Content.Headers.Remove(name);
            request.Content.Headers.TryAddWithoutValidation(name, value);
        }
        return request;
    }

    public static HttpRequestMessage Get(string url, params (string Name, string Value)[] headers) => Request("GET", url, null, headers);

    /// <summary>Stores cookies as a same-site top-level response from <paramref name="url"/> would.</summary>
    public static void Seed(CookieStore store, string url, params string[] fields)
    {
        foreach (var field in fields)
            Assert.True(store.ReceiveResponseCookie(field, new(new Uri(url), SameSiteStatus.SameSite)).Accepted, field);
    }

    public static string? Cookie(this RecordedHop hop) => hop.Header("Cookie");

    public static async Task<TransportException> Fails(Task task) => await Assert.ThrowsAsync<TransportException>(() => task);

    public static SchemefulSite Site(string url) => Sites.GetSite(new Uri(url))!;
}

/// <summary>A fact that is skipped where the OS has no IPv6 stack.</summary>
internal sealed class Ipv6FactAttribute : FactAttribute
{
    public Ipv6FactAttribute()
    {
        if (!Socket.OSSupportsIPv6) Skip = "IPv6 is not available.";
    }
}

/// <summary>A received request on the loopback server; header values are Latin-1 decoded octets.</summary>
internal sealed record LoopbackRequest(string Method, string Target, IReadOnlyList<KeyValuePair<string, string>> Headers, byte[] Body)
{
    public string? Header(string name)
    {
        var values = Headers.Where(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Select(h => h.Value).ToArray();
        return values.Length == 0 ? null : string.Join(", ", values);
    }

    public int HeaderCount(string name) => Headers.Count(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Minimal HTTP/1.1 server on a loopback address: one request per connection, raw Latin-1 response heads.</summary>
internal sealed class LoopbackServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<LoopbackRequest, byte[]> _respond;
    private readonly CancellationTokenSource _stop = new();

    public LoopbackServer(Func<LoopbackRequest, byte[]> respond, IPAddress? address = null, int port = 0)
    {
        _respond = respond;
        _listener = new(address ?? IPAddress.Loopback, port);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _ = AcceptAsync();
    }

    public int Port { get; }
    public ConcurrentQueue<LoopbackRequest> Requests { get; } = new();
    public string Url(string path) => $"http://127.0.0.1:{Port}{path}";

    public static byte[] Response(int status, IEnumerable<(string Name, string Value)> headers, byte[]? body = null)
    {
        body ??= [];
        var head = new StringBuilder($"HTTP/1.1 {status} Status\r\n");
        foreach (var (name, value) in headers) head.Append(name).Append(": ").Append(value).Append("\r\n");
        head.Append($"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        return [.. Encoding.Latin1.GetBytes(head.ToString()), .. body];
    }

    private async Task AcceptAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(_stop.Token); }
            catch (Exception) { return; }
            _ = Task.Run(() => ServeAsync(client));
        }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var buffer = new List<byte>();
                var chunk = new byte[4096];
                int end;
                while ((end = IndexOfHeadEnd(buffer)) < 0)
                {
                    var read = await stream.ReadAsync(chunk, _stop.Token);
                    if (read == 0) return;
                    buffer.AddRange(chunk.AsSpan(0, read).ToArray());
                }
                var lines = Encoding.Latin1.GetString(buffer.GetRange(0, end).ToArray()).Split("\r\n");
                var start = lines[0].Split(' ');
                var headers = lines.Skip(1).Where(l => l.Length > 0)
                    .Select(l => new KeyValuePair<string, string>(l[..l.IndexOf(':')], l[(l.IndexOf(':') + 1)..].Trim())).ToArray();
                var length = headers.Where(h => h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    .Select(h => int.Parse(h.Value)).FirstOrDefault();
                var body = buffer.Skip(end + 4).ToList();
                while (body.Count < length)
                {
                    var read = await stream.ReadAsync(chunk, _stop.Token);
                    if (read == 0) return;
                    body.AddRange(chunk.AsSpan(0, read).ToArray());
                }
                var request = new LoopbackRequest(start[0], start[1], headers, body.ToArray());
                Requests.Enqueue(request);
                await stream.WriteAsync(_respond(request), _stop.Token);
            }
            catch (Exception) when (_stop.IsCancellationRequested) { }
        }
    }

    private static int IndexOfHeadEnd(List<byte> buffer)
    {
        for (var i = 0; i + 3 < buffer.Count; i++)
            if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n') return i;
        return -1;
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
    }
}

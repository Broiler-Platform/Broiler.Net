using System.Net;
using System.Net.Sockets;

namespace Broiler.Net.Http;

/// <summary>
/// Keeps localhost names and loopback addresses on the loopback interface, so <see cref="Sites.HostNames.IsSecure(Uri)"/>'s
/// loopback rules hold on the wire: they never reach DNS or a proxy.
/// </summary>
internal static class LoopbackRouting
{
    /// <summary>RFC 8305's connection attempt delay: ::1 goes first, 127.0.0.1 joins if ::1 has not answered.</summary>
    internal static readonly TimeSpan AttemptDelay = TimeSpan.FromMilliseconds(250);

    public static bool IsLocalhost(string host)
    {
        var name = host.TrimEnd('.');
        return name.Equals("localhost", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }

    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var endpoint = context.DnsEndPoint;
        if (!IsLocalhost(endpoint.Host)) return await ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        return await ConnectLoopbackAsync(endpoint.Port, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Stream> ConnectLoopbackAsync(int port, CancellationToken cancellationToken)
    {
        // A refused connect on Windows takes about two seconds, so an IPv4-only server must not wait for ::1 to fail.
        using var attempts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ipv6 = ConnectAsync(new IPEndPoint(IPAddress.IPv6Loopback, port), attempts.Token).AsTask();
        Task<Stream>? ipv4 = null;
        Stream? winner = null;
        try
        {
            await Task.WhenAny(ipv6, Task.Delay(AttemptDelay, attempts.Token)).ConfigureAwait(false);
            if (!ipv6.IsCompletedSuccessfully)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ipv4 = ConnectAsync(new IPEndPoint(IPAddress.Loopback, port), attempts.Token).AsTask();
                // When the first finisher failed, the other one decides (and throws if it fails too).
                if (await Task.WhenAny(ipv6, ipv4).ConfigureAwait(false) is { IsCompletedSuccessfully: false } failed)
                    await (failed == ipv6 ? ipv4 : ipv6).ConfigureAwait(false);
            }
            return winner = ipv6.IsCompletedSuccessfully ? ipv6.Result : ipv4!.Result;
        }
        finally
        {
            attempts.Cancel();
            Release(ipv6, winner);
            if (ipv4 is not null) Release(ipv4, winner);
        }
    }

    /// <summary>Closes a losing attempt's socket whenever it completes, and observes its failure.</summary>
    private static void Release(Task<Stream> attempt, Stream? winner) =>
        attempt.ContinueWith(t => { if (!t.IsCompletedSuccessfully) _ = t.Exception; else if (t.Result != winner) t.Result.Dispose(); },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    private static async ValueTask<Stream> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken)
    {
        // Mirrors SocketsHttpHandler's default connect.
        var socket = endpoint is IPEndPoint ip
            ? new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            : new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            socket.NoDelay = true;
            await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public static bool IsDirect(Uri url) => url.IsLoopback || IsLocalhost(url.IdnHost);

    /// <summary>
    /// Sends localhost names and loopback addresses to <see cref="Direct"/> (no proxy, pinned connects) and every other
    /// host to <see cref="Other"/>, which keeps the system proxy unwrapped: a wrapper would hide the platform proxy's
    /// PAC/WPAD failover list.
    /// </summary>
    internal sealed class RoutingHandler(HttpMessageHandler direct, HttpMessageHandler other) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _direct = new(direct), _other = new(other);

        public HttpMessageHandler Direct { get; } = direct;
        public HttpMessageHandler Other { get; } = other;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Route(request).SendAsync(request, cancellationToken);

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Route(request).Send(request, cancellationToken);

        private HttpMessageInvoker Route(HttpRequestMessage request) => IsDirect(request.RequestUri!) ? _direct : _other;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _direct.Dispose();
                _other.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

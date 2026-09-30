// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   13
// Annotated:        13/13
// Exempt:           3
// Human-reviewed:   0/13
// IP risk:          Medium
// Security risk:    High
// Criteria:         12/11
// Resource impact:  2/10 max
// Unverified:       13
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Net;
using System.Net.Sockets;

namespace Broiler.Net.Http;

/// <summary>
/// Keeps localhost names and loopback addresses on the loopback interface, so <see cref="Sites.HostNames.IsSecure(Uri)"/>'s
/// loopback rules hold on the wire: they never reach DNS or a proxy.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Medium; Security=High; Resources=2; Fingerprint=284B7A
// Broiler-Falsified-If: a request to a name under .localhost reaches DNS or the system proxy instead of a loopback address
// Broiler-Human:        PENDING
internal static class LoopbackRouting
{
    /// <summary>RFC 8305's connection attempt delay: ::1 goes first, 127.0.0.1 joins if ::1 has not answered.</summary>
    // Broiler-AI:           Origin=AI; Spec=RFC-8305 s5; IP=None; Security=None; Resources=0; Fingerprint=5C2C1E
    // Broiler-Human:        PENDING
    internal static readonly TimeSpan AttemptDelay = TimeSpan.FromMilliseconds(250);

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=8C74B6
    // Broiler-Falsified-If: 'localhost.example' is reported as a localhost name
    // Broiler-Human:        PENDING
    public static bool IsLocalhost(string host)
    {
        var name = host.TrimEnd('.');
        return name.Equals("localhost", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=CC3478
    // Broiler-Falsified-If: a connection to app.localhost is made to an address from DNS instead of ::1 or 127.0.0.1
    // Broiler-Human:        PENDING
    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var endpoint = context.DnsEndPoint;
        if (!IsLocalhost(endpoint.Host)) return await ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        return await ConnectLoopbackAsync(endpoint.Port, cancellationToken).ConfigureAwait(false);
    }

    // Broiler-AI:           Origin=AI; Spec=RFC-8305 s5; IP=Low; Security=High; Resources=2; Fingerprint=86AD32
    // Broiler-Falsified-If: when both ::1 and 127.0.0.1 accept, the losing connection's socket stays open after the winner is returned
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=F955C3
    // Broiler-Falsified-If: a losing attempt that fails leaves its exception unobserved and raises UnobservedTaskException
    // Broiler-Human:        PENDING
    private static void Release(Task<Stream> attempt, Stream? winner) =>
        attempt.ContinueWith(t => { if (!t.IsCompletedSuccessfully) _ = t.Exception; else if (t.Result != winner) t.Result.Dispose(); },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    // Broiler-AI:           Origin=AI; IP=Medium; Security=High; Resources=1; Fingerprint=7734B3
    // Broiler-Falsified-If: a connect that fails or is cancelled leaves its Socket undisposed
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=2289A9
    // Broiler-Falsified-If: a URL whose host is sub.localhost is not reported direct and so goes through the system proxy
    // Broiler-Human:        PENDING
    public static bool IsDirect(Uri url) => url.IsLoopback || IsLocalhost(url.IdnHost);

    /// <summary>
    /// Sends localhost names and loopback addresses to <see cref="Direct"/> (no proxy, pinned connects) and every other
    /// host to <see cref="Other"/>, which keeps the system proxy unwrapped: a wrapper would hide the platform proxy's
    /// PAC/WPAD failover list.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=5DA485
    // Broiler-Falsified-If: a request to a loopback address is sent through the handler that uses the system proxy
    // Broiler-Human:        PENDING
    internal sealed class RoutingHandler(HttpMessageHandler direct, HttpMessageHandler other) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _direct = new(direct), _other = new(other);

        public HttpMessageHandler Direct { get; } = direct;
        public HttpMessageHandler Other { get; } = other;

        // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=9FDDED
        // Broiler-Falsified-If: an asynchronous request to http://localhost/ is sent through the proxy-using invoker
        // Broiler-Human:        PENDING
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Route(request).SendAsync(request, cancellationToken);

        // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=9FBC00
        // Broiler-Falsified-If: a synchronous request to http://localhost/ is sent through the proxy-using invoker
        // Broiler-Human:        PENDING
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Route(request).Send(request, cancellationToken);

        // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=9003C7
        // Broiler-Falsified-If: a request to http://[::1]:8080/ is given the proxy-using invoker
        // Broiler-Human:        PENDING
        private HttpMessageInvoker Route(HttpRequestMessage request) => IsDirect(request.RequestUri!) ? _direct : _other;

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=5F64E9
        // Broiler-Falsified-If: disposing the handler leaves the direct or the proxied inner handler undisposed
        // Broiler-Human:        PENDING
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

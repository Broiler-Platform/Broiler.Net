using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Broiler.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Broiler.Net.Tests;

public sealed class Http2TransportTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task NegotiatesHttp2OrFallsBackIncludingSyncRedirectsAndBodies(bool synchronous, bool http2)
    {
        using var key = RSA.Create(2048);
        var certificateRequest = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var generated = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        // Schannel needs a key container, rather than CreateSelfSigned's ephemeral key.
        using var certificate = X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, listen =>
        {
            listen.Protocols = http2 ? HttpProtocols.Http1AndHttp2 : HttpProtocols.Http1;
            listen.UseHttps(certificate);
        }));
        await using var server = builder.Build();
        server.MapPost("/start", context =>
        {
            context.Response.StatusCode = 307;
            context.Response.Headers.Location = "/final";
            context.Response.Headers.SetCookie = "redirect=kept; Path=/; Secure";
            return Task.CompletedTask;
        });
        server.MapPost("/final", async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            await context.Response.WriteAsync($"{context.Request.Protocol}|{await reader.ReadToEndAsync()}|{context.Request.Headers.Cookie}");
        });
        await server.StartAsync();

        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            UseProxy = false,
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, remote, _, _) => remote?.GetCertHashString() == certificate.GetCertHashString(),
            },
        };
        using var session = new BrowserNetworkSession(new() { Handler = handler });
        using var request = new HttpRequestMessage(HttpMethod.Post, server.Urls.Single() + "/start") { Content = new StringContent("posted") };
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var response = synchronous
            ? session.Send(request, RequestContext.TopLevelNavigation(null), deadline.Token)
            : await session.SendAsync(request, RequestContext.TopLevelNavigation(null), deadline.Token);

        Assert.Equal(http2 ? HttpVersion.Version20 : HttpVersion.Version11, response.Message.Version);
        string body;
        if (synchronous)
        {
            // Renderer loaders also consume response streams synchronously.
            using var reader = new StreamReader(response.Message.Content.ReadAsStream(deadline.Token));
            body = reader.ReadToEnd();
        }
        else body = await response.Message.Content.ReadAsStringAsync(deadline.Token);
        Assert.Equal($"{(http2 ? "HTTP/2" : "HTTP/1.1")}|posted|redirect=kept", body);
        Assert.Equal(2, response.UrlList.Count);
    }
}

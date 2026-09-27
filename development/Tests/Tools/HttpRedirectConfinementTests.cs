using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using AgentFrameworkToolkit.Tools.Common;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.Tests.Tools;

public class HttpRedirectConfinementTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("HEAD")]
    public async Task HttpClientTools_RejectsRedirectOutsideAllowedDomainAsync(string methodName)
    {
        RecordingHandler handler = new((request, _) => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("http://169.254.169.254/latest/meta-data/") }
        });
        using HttpClient client = new(handler);
        HttpClientToolsOptions options = new() { ConfinedToTheseDomains = ["allowed.example.com"] };

        await Assert.ThrowsAsync<InvalidOperationException>(() => SendHttpClientToolAsync(client, methodName, options));
        Assert.Equal(["allowed.example.com"], handler.RequestHosts);
    }

    [Fact]
    public async Task WebsiteTools_RejectsCustomClientBeforeNetworkAsync()
    {
        bool factoryCalled = false;
        RecordingHandler handler = new((request, _) => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("http://localhost/private") }
        });
        GetContentOfPageOptions options = new()
        {
            ConfinedToTheseDomains = ["allowed.example.com"],
            HttpClientFactory = () =>
            {
                factoryCalled = true;
                return new HttpClient(handler);
            }
        };

        AIFunction tool = (AIFunction)WebsiteTools.GetContentOfPage(options);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await tool.InvokeAsync(new AIFunctionArguments { ["url"] = "http://allowed.example.com/start" }, TestContext.Current.CancellationToken));
        Assert.False(factoryCalled);
        Assert.Empty(handler.RequestHosts);
    }

    [Fact]
    public async Task WebsiteTools_DefaultClientRejectsRedirectOutsideAllowedDomainAsync()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        Task serverTask = RespondWithRedirectAsync(listener, port, timeout.Token);
        GetContentOfPageOptions options = new() { ConfinedToTheseDomains = ["127.0.0.1"] };
        AIFunction tool = (AIFunction)WebsiteTools.GetContentOfPage(options);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await tool.InvokeAsync(new AIFunctionArguments { ["url"] = $"http://127.0.0.1:{port}/start" }, TestContext.Current.CancellationToken));
        await serverTask;
    }

    [Fact]
    public async Task HttpClientTools_RejectsCustomClientBeforeNetworkAsync()
    {
        bool factoryCalled = false;
        HttpClientToolsOptions options = new()
        {
            ConfinedToTheseDomains = ["allowed.example.com"],
            HttpClientFactory = () =>
            {
                factoryCalled = true;
                return new HttpClient();
            }
        };
        AIFunction tool = (AIFunction)HttpClientTools.Get(options);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await tool.InvokeAsync(new AIFunctionArguments { ["url"] = "http://allowed.example.com/start" }, TestContext.Current.CancellationToken));
        Assert.False(factoryCalled);
    }

    [Fact]
    public async Task HttpClientTools_FollowsAllowedRedirectAndRewritesPostToGetAsync()
    {
        RecordingHandler handler = new((request, count) => count == 1
            ? new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("/next", UriKind.Relative) } }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("safe") });
        using HttpClient client = new(handler);
        HttpClientToolsOptions options = new() { ConfinedToTheseDomains = ["allowed.example.com"] };

        HttpResponseMessage response = await SendHttpClientToolAsync(client, "POST", options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["POST", "GET"], handler.RequestMethods);
        Assert.Equal(["allowed.example.com", "allowed.example.com"], handler.RequestHosts);
    }

    [Fact]
    public async Task HttpClientTools_RejectsDisallowedFinalUriFromCustomClientAsync()
    {
        RecordingHandler handler = new((request, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/private"),
            Content = new StringContent("secret")
        });
        using HttpClient client = new(handler);
        HttpClientToolsOptions options = new() { ConfinedToTheseDomains = ["allowed.example.com"] };

        await Assert.ThrowsAsync<InvalidOperationException>(() => SendHttpClientToolAsync(client, "GET", options));
    }

    private static async Task<HttpResponseMessage> SendHttpClientToolAsync(HttpClient client, string methodName, HttpClientToolsOptions options)
    {
        MethodInfo method = typeof(HttpClientTools).GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        Func<HttpContent?>? contentFactory = methodName is "POST" or "PUT" or "PATCH" ? () => new StringContent("body") : null;
        Task<HttpResponseMessage> task = (Task<HttpResponseMessage>)method.Invoke(null, [client, "http://allowed.example.com/start", new HttpMethod(methodName), contentFactory, options])!;
        return await task;
    }

    private static async Task RespondWithRedirectAsync(TcpListener listener, int port, CancellationToken cancellationToken)
    {
        using TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new(stream, Encoding.ASCII, leaveOpen: true);
        while (!string.IsNullOrEmpty(await reader.ReadLineAsync(cancellationToken)))
        {
        }
        byte[] response = Encoding.ASCII.GetBytes($"HTTP/1.1 302 Found\r\nLocation: http://localhost:{port}/private\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(response, cancellationToken);
        listener.Stop();
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, int, HttpResponseMessage> respond) : HttpMessageHandler
    {
        internal List<string> RequestHosts { get; } = [];
        internal List<string> RequestMethods { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestHosts.Add(request.RequestUri!.Host);
            RequestMethods.Add(request.Method.Method);
            return Task.FromResult(respond(request, RequestHosts.Count));
        }
    }
}

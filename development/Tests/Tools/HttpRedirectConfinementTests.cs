using System.Net;
using System.Reflection;
using AgentFrameworkToolkit.Tools.Common;

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
    public async Task WebsiteTools_RejectsRedirectOutsideAllowedDomainAsync()
    {
        RecordingHandler handler = new((request, _) => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("http://localhost/private") }
        });
        GetContentOfPageOptions options = new()
        {
            ConfinedToTheseDomains = ["allowed.example.com"],
            HttpClientFactory = () => new HttpClient(handler)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => GetWebsiteContentAsync("http://allowed.example.com/start", options));
        Assert.Equal(["allowed.example.com"], handler.RequestHosts);
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

    private static async Task<string> GetWebsiteContentAsync(string url, GetContentOfPageOptions options)
    {
        MethodInfo method = typeof(WebsiteTools).GetMethod("GetContentAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        Task<string> task = (Task<string>)method.Invoke(null, [url, options])!;
        return await task;
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

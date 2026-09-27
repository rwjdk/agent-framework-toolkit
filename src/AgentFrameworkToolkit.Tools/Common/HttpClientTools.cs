using System.Net.Http.Headers;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.Tools.Common;

/// <summary>
/// Tools for HTTP client operations
/// </summary>
[PublicAPI]
public static class HttpClientTools
{
    /// <summary>
    /// Get All HTTP Client Tools with their default settings
    /// </summary>
    /// <param name="options">Optional options</param>
    /// <returns>Tools</returns>
    public static IList<AITool> All(HttpClientToolsOptions? options = null)
    {
        return
        [
            Get(options),
            Post(options),
            Put(options),
            Patch(options),
            Delete(options),
            Head(options)
        ];
    }

    /// <summary>
    /// Send HTTP GET request
    /// </summary>
    /// <param name="options">Optional options</param>
    /// <param name="toolName">Name of tool</param>
    /// <param name="toolDescription">Description of Tool</param>
    /// <returns>Tool</returns>
    public static AITool Get(HttpClientToolsOptions? options = null, string? toolName = null, string? toolDescription = null)
    {
        return AIFunctionFactory.Create(async (string url, string? acceptHeader = null) =>
        {
            HttpClient httpClient = GetHttpClient(options);
            GuardThatOperationsAreWithinConfinedDomains(url, httpClient.BaseAddress, options);
            if (!string.IsNullOrWhiteSpace(acceptHeader))
            {
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));
            }
            HttpResponseMessage response = await SendAsync(httpClient, url, HttpMethod.Get, null, options);
            return await FormatResponseAsync(response, options);
        }, toolName ?? "http_get", toolDescription ?? "Send an HTTP GET request to retrieve data from a URL");
    }

    /// <summary>
    /// Send HTTP POST request
    /// </summary>
    /// <param name="options">Optional options</param>
    /// <param name="toolName">Name of tool</param>
    /// <param name="toolDescription">Description of Tool</param>
    /// <returns>Tool</returns>
    public static AITool Post(HttpClientToolsOptions? options = null, string? toolName = null, string? toolDescription = null)
    {
        return AIFunctionFactory.Create(async (string url, string body, string contentType = "application/json") =>
        {
            HttpClient httpClient = GetHttpClient(options);
            GuardThatOperationsAreWithinConfinedDomains(url, httpClient.BaseAddress, options);
            HttpResponseMessage response = await SendAsync(httpClient, url, HttpMethod.Post, () => new StringContent(body, Encoding.UTF8, contentType), options);
            return await FormatResponseAsync(response, options);
        }, toolName ?? "http_post", toolDescription ?? "Send an HTTP POST request to submit data to a URL");
    }

    /// <summary>
    /// Send HTTP PUT request
    /// </summary>
    /// <param name="options">Optional options</param>
    /// <param name="toolName">Name of tool</param>
    /// <param name="toolDescription">Description of Tool</param>
    /// <returns>Tool</returns>
    public static AITool Put(HttpClientToolsOptions? options = null, string? toolName = null, string? toolDescription = null)
    {
        return AIFunctionFactory.Create(async (string url, string body, string contentType = "application/json") =>
        {
            HttpClient httpClient = GetHttpClient(options);
            GuardThatOperationsAreWithinConfinedDomains(url, httpClient.BaseAddress, options);
            HttpResponseMessage response = await SendAsync(httpClient, url, HttpMethod.Put, () => new StringContent(body, Encoding.UTF8, contentType), options);
            return await FormatResponseAsync(response, options);
        }, toolName ?? "http_put", toolDescription ?? "Send an HTTP PUT request to update data at a URL");
    }

    /// <summary>
    /// Send HTTP PATCH request
    /// </summary>
    /// <param name="options">Optional options</param>
    /// <param name="toolName">Name of tool</param>
    /// <param name="toolDescription">Description of Tool</param>
    /// <returns>Tool</returns>
    public static AITool Patch(HttpClientToolsOptions? options = null, string? toolName = null, string? toolDescription = null)
    {
        return AIFunctionFactory.Create(async (string url, string body, string contentType = "application/json") =>
        {
            HttpClient httpClient = GetHttpClient(options);
            GuardThatOperationsAreWithinConfinedDomains(url, httpClient.BaseAddress, options);
            HttpResponseMessage response = await SendAsync(httpClient, url, HttpMethod.Patch, () => new StringContent(body, Encoding.UTF8, contentType), options);
            return await FormatResponseAsync(response, options);
        }, toolName ?? "http_patch", toolDescription ?? "Send an HTTP PATCH request to partially update data at a URL");
    }

    /// <summary>
    /// Send HTTP DELETE request
    /// </summary>
    /// <param name="options">Optional options</param>
    /// <param name="toolName">Name of tool</param>
    /// <param name="toolDescription">Description of Tool</param>
    /// <returns>Tool</returns>
    public static AITool Delete(HttpClientToolsOptions? options = null, string? toolName = null, string? toolDescription = null)
    {
        return AIFunctionFactory.Create(async (string url) =>
        {
            HttpClient httpClient = GetHttpClient(options);
            GuardThatOperationsAreWithinConfinedDomains(url, httpClient.BaseAddress, options);
            HttpResponseMessage response = await SendAsync(httpClient, url, HttpMethod.Delete, null, options);
            return await FormatResponseAsync(response, options);
        }, toolName ?? "http_delete", toolDescription ?? "Send an HTTP DELETE request to remove data at a URL");
    }

    /// <summary>
    /// Send HTTP HEAD request
    /// </summary>
    /// <param name="options">Optional options</param>
    /// <param name="toolName">Name of tool</param>
    /// <param name="toolDescription">Description of Tool</param>
    /// <returns>Tool</returns>
    public static AITool Head(HttpClientToolsOptions? options = null, string? toolName = null, string? toolDescription = null)
    {
        return AIFunctionFactory.Create(async (string url) =>
        {
            HttpClient httpClient = GetHttpClient(options);
            GuardThatOperationsAreWithinConfinedDomains(url, httpClient.BaseAddress, options);
            HttpResponseMessage response = await SendAsync(httpClient, url, HttpMethod.Head, null, options);
            return await FormatResponseAsync(response, options, includeBody: false);
        }, toolName ?? "http_head", toolDescription ?? "Send an HTTP HEAD request to retrieve headers from a URL without the body");
    }

    private static HttpClient GetHttpClient(HttpClientToolsOptions? options)
    {
        return options?.HttpClientFactory?.Invoke() ?? (options?.ConfinedToTheseDomains == null ? new HttpClient() : ConfinedHttpRedirects.CreateClient());
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string url, HttpMethod method, Func<HttpContent?>? contentFactory, HttpClientToolsOptions? options)
    {
        if (options?.ConfinedToTheseDomains != null)
        {
            Uri uri = GetAbsoluteUri(url, client.BaseAddress) ?? throw new InvalidOperationException($"Operations on URL '{url}' cannot be validated against allowed domains.");
            return await ConfinedHttpRedirects.SendAsync(client, uri, method, contentFactory, HttpCompletionOption.ResponseContentRead,
                target => GuardThatOperationsAreWithinConfinedDomains(target.ToString(), null, options));
        }

        using HttpRequestMessage request = new(method, url);
        request.Content = contentFactory?.Invoke();
        return await client.SendAsync(request);
    }

    private static void GuardThatOperationsAreWithinConfinedDomains(string url, Uri? baseAddress, HttpClientToolsOptions? options)
    {
        if (options?.ConfinedToTheseDomains == null)
        {
            return; //No confinements defined
        }

        Uri? uri = GetAbsoluteUri(url, baseAddress);
        if (uri == null)
        {
            throw new InvalidOperationException($"Operations on URL '{url}' cannot be validated against allowed domains.");
        }

        string normalizedHost = uri.Host.TrimEnd('.');
        foreach (string domain in options.ConfinedToTheseDomains)
        {
            string normalizedDomain = NormalizeDomain(domain);
            if (string.Equals(normalizedHost, normalizedDomain, StringComparison.OrdinalIgnoreCase))
            {
                return; //Allowed domain
            }
        }

        //If we reach here, then it means that we are not in an allowed domain
        throw new InvalidOperationException($"Operations on URL '{url}' is not defined as an allowed domain");
    }

    private static Uri? GetAbsoluteUri(string url, Uri? baseAddress)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absoluteUri))
        {
            return absoluteUri;
        }

        if (baseAddress != null && Uri.TryCreate(baseAddress, url, out Uri? uriWithBaseAddress))
        {
            return uriWithBaseAddress;
        }

        return null;
    }

    private static string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(domain, UriKind.Absolute, out Uri? domainUri))
        {
            return domainUri.Host.TrimEnd('.');
        }

        if (Uri.TryCreate($"https://{domain}", UriKind.Absolute, out Uri? domainAsUri))
        {
            return domainAsUri.Host.TrimEnd('.');
        }

        return domain.Trim().TrimEnd('.');
    }

    private static async Task<string> FormatResponseAsync(HttpResponseMessage response, HttpClientToolsOptions? options, bool includeBody = true)
    {
        StringBuilder result = new();
        result.AppendLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");

        if (options?.IncludeHeaders ?? false)
        {
            result.AppendLine("\nHeaders:");
            foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
            {
                result.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            if (response.Content.Headers.Any())
            {
                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
                {
                    result.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }
        }

        if (includeBody)
        {
            string content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                result.AppendLine("\nBody:");
                result.AppendLine(content);
            }
        }

        if (!response.IsSuccessStatusCode && (options?.ThrowOnError ?? false))
        {
            throw new HttpRequestException($"HTTP request failed with status {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        return result.ToString();
    }
}

/// <summary>
/// Options for HTTP Client Tools
/// </summary>
[PublicAPI]
public class HttpClientToolsOptions
{
    /// <summary>
    /// If set all operations will be checked if they happen on these exact domain names (if not set then no restrictions apply)
    /// </summary>
    public IList<string>? ConfinedToTheseDomains { get; set; }

    /// <summary>
    /// HTTP Client Factory (if not specified a new HttpClient is generated)
    /// </summary>
    public Func<HttpClient>? HttpClientFactory { get; set; }

    /// <summary>
    /// Include response headers in the output (Default: false)
    /// </summary>
    public bool IncludeHeaders { get; set; } = false;

    /// <summary>
    /// Throw an exception when the HTTP response indicates an error status code (Default: false)
    /// </summary>
    public bool ThrowOnError { get; set; } = false;
}

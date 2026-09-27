using JetBrains.Annotations;
using Microsoft.Extensions.AI;
using System.Net;
using System.Text.RegularExpressions;

namespace AgentFrameworkToolkit.Tools.Common;

/// <summary>
/// Tools Related to Website Content
/// </summary>
[PublicAPI]
public static class WebsiteTools
{
    /// <summary>
    /// Get All Website Tools
    /// </summary>
    /// <returns>The Tools</returns>
    public static IList<AITool> All(GetContentOfPageOptions? getContentOfPageOptions = null)
    {
        return
        [
            GetContentOfPage(getContentOfPageOptions)
        ];
    }

    /// <summary>
    /// Get the raw content of a website
    /// <param name="options">Optional options for tool</param>
    /// <param name="toolName">Name of tool</param>
    /// <param name="toolDescription">Description of Tool</param>
    /// </summary>
    /// <returns></returns>
    public static AITool GetContentOfPage(GetContentOfPageOptions? options = null, string? toolName = null, string? toolDescription = null)
    {
        GetContentOfPageOptions optionToUse = options ?? new GetContentOfPageOptions();
        return AIFunctionFactory.Create(async (string url) => await GetContentAsync(url, optionToUse), toolName ?? "get_content_of_url", toolDescription ?? "Get the content of a webpage from a URL");
    }

    private static async Task<string> GetContentAsync(string url, GetContentOfPageOptions options)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or whitespace.", nameof(url));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("URL must be an absolute HTTP/HTTPS URL.", nameof(url));
        }

        GuardThatOperationsAreWithinConfinedDomains(uri, options);

        HttpClient httpClient = GetHttpClient(options);
        HttpResponseMessage response = options.ConfinedToTheseDomains == null
            ? await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false)
            : await ConfinedHttpRedirects.SendAsync(httpClient, uri, HttpMethod.Get, null, HttpCompletionOption.ResponseHeadersRead,
                target => GuardThatOperationsAreWithinConfinedDomains(target, options)).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (options.StripMarkup)
        {
            content = StripMarkup(content);
        }

        return content;
    }

    private static string StripMarkup(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        string withoutScripts = Regex.Replace(html, "<(script|style)[^>]*?>.*?</\\1>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        string withoutTags = Regex.Replace(withoutScripts, "<[^>]+>", " ");
        string decoded = WebUtility.HtmlDecode(withoutTags);
        string normalizedWhitespace = Regex.Replace(decoded, "\\s+", " ").Trim();

        return normalizedWhitespace;
    }

    private static HttpClient GetHttpClient(GetContentOfPageOptions options)
    {
        if (options.ConfinedToTheseDomains != null && options.HttpClientFactory != null)
        {
            throw new InvalidOperationException("HttpClientFactory cannot be used with ConfinedToTheseDomains because its redirect behavior cannot be verified.");
        }

        return options.HttpClientFactory?.Invoke() ?? (options.ConfinedToTheseDomains == null ? new HttpClient() : ConfinedHttpRedirects.CreateClient());
    }

    private static void GuardThatOperationsAreWithinConfinedDomains(Uri uri, GetContentOfPageOptions options)
    {
        if (options.ConfinedToTheseDomains == null)
        {
            return; //No confinements defined
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
        throw new InvalidOperationException($"Operations on URL '{uri}' is not defined as an allowed domain");
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
}

/// <summary>
/// Options of GetContentOfPageTool Tool
/// </summary>
[PublicAPI]
public class GetContentOfPageOptions
{
    /// <summary>
    /// If set all operations will be checked if they happen on these exact domain names (if not set then no restrictions apply)
    /// </summary>
    public IList<string>? ConfinedToTheseDomains { get; set; }

    /// <summary>
    /// HTTP Client Factory (if not specified a new HttpClient is generated). Cannot be used with ConfinedToTheseDomains.
    /// </summary>
    public Func<HttpClient>? HttpClientFactory { get; set; }

    /// <summary>
    /// If tool should Strip away markup (HTML, JS and CSS) leaving only raw text (Default = true)
    /// </summary>
    public bool StripMarkup { get; set; } = true;
}

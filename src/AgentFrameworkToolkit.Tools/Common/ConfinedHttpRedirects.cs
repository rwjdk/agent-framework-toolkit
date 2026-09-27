using System.Net;

namespace AgentFrameworkToolkit.Tools.Common;

internal static class ConfinedHttpRedirects
{
    internal static async Task<HttpResponseMessage> SendAsync(HttpClient client, Uri uri, HttpMethod method, Func<HttpContent?>? contentFactory, HttpCompletionOption completionOption, Action<Uri> guard)
    {
        for (int redirectCount = 0; ; redirectCount++)
        {
            guard(uri);
            using HttpRequestMessage request = new(method, uri);
            request.Content = contentFactory?.Invoke();
            HttpResponseMessage response = await client.SendAsync(request, completionOption).ConfigureAwait(false);
            try
            {
                guard(response.RequestMessage?.RequestUri ?? uri);
            }
            catch
            {
                response.Dispose();
                throw;
            }

            if (!IsRedirect(response.StatusCode) || response.Headers.Location == null)
            {
                return response;
            }

            Uri nextUri = new(uri, response.Headers.Location);
            if (uri.Scheme == Uri.UriSchemeHttps && nextUri.Scheme == Uri.UriSchemeHttp)
            {
                return response;
            }

            if (redirectCount == 50)
            {
                response.Dispose();
                throw new HttpRequestException("The HTTP request exceeded the redirect limit of 50.");
            }

            try
            {
                guard(nextUri);
            }
            catch
            {
                response.Dispose();
                throw;
            }
            if (response.StatusCode == HttpStatusCode.SeeOther && method != HttpMethod.Head ||
                (response.StatusCode == HttpStatusCode.MovedPermanently || response.StatusCode == HttpStatusCode.Redirect) && method == HttpMethod.Post)
            {
                method = HttpMethod.Get;
                contentFactory = null;
            }

            response.Dispose();
            uri = nextUri;
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
    }

    internal static HttpClient CreateClient()
    {
        return new(new HttpClientHandler { AllowAutoRedirect = false });
    }
}

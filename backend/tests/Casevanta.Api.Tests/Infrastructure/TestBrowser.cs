using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Casevanta.Api.Tests.Infrastructure;

/// <summary>
/// A minimal cookie-aware client: it keeps a cookie jar like a browser, sends a single Cookie
/// header, and leaves the raw Set-Cookie headers on the response for attribute assertions.
/// </summary>
public sealed class TestBrowser(HttpClient client) : IDisposable
{
    private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Cookies => _cookies;

    public void SetCookie(string name, string value) => _cookies[name] = value;

    public Task<HttpResponseMessage> GetAsync(string path, Action<HttpRequestMessage>? configure = null) =>
        SendAsync(HttpMethod.Get, path, configure);

    public Task<HttpResponseMessage> PostAsync(string path, Action<HttpRequestMessage>? configure = null) =>
        SendAsync(HttpMethod.Post, path, configure);

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Action<HttpRequestMessage>? configure = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (_cookies.Count > 0)
        {
            request.Headers.Add(HeaderNames.Cookie, string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
        }

        configure?.Invoke(request);
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        foreach (var cookie in SetCookies(response))
        {
            var name = cookie.Name.Value!;
            var expired = cookie.Expires is { } expires && expires <= DateTimeOffset.UtcNow;
            if (expired || StringSegment.IsNullOrEmpty(cookie.Value))
            {
                _cookies.Remove(name);
            }
            else
            {
                _cookies[name] = cookie.Value.Value!;
            }
        }

        return response;
    }

    /// <summary>The value of the readable antiforgery request-token cookie, as the SPA would send it.</summary>
    public string RequestToken => Uri.UnescapeDataString(_cookies["XSRF-RequestToken"]);

    public static IList<SetCookieHeaderValue> SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues(HeaderNames.SetCookie, out var values)
            ? SetCookieHeaderValue.ParseList(values.ToList())
            : [];

    public void Dispose() => client.Dispose();
}

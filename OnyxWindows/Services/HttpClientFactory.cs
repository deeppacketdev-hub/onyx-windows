using System;
using System.Net.Http;

namespace OnyxWindows.Services;

/// <summary>
/// Shared HttpClient factory with sensible timeouts.
/// Replaces the macOS URLSession.onyxSession.
/// </summary>
public static class HttpClientFactory
{
    private static readonly Lazy<HttpClient> _instance = new(() =>
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(120) // resource timeout
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("OnyxLauncher/1.0");

        return client;
    });

    /// <summary>
    /// Shared HttpClient instance with OnyxLauncher/1.0 User-Agent and 120s timeout.
    /// </summary>
    public static HttpClient Shared => _instance.Value;
}

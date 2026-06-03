using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OnyxWindows.Services;

public class NewsFeed
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("entries")] public List<NewsEntry> Entries { get; set; } = new();
}

public class NewsEntry
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("date")] public string Date { get; set; } = "";
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("playPageImage")] public NewsImage? PlayPageImage { get; set; }
    [JsonPropertyName("newsPageImage")] public NewsImage? NewsPageImage { get; set; }
    [JsonPropertyName("readMoreLink")] public string ReadMoreLink { get; set; } = "";
    [JsonPropertyName("newsType")] public List<string>? NewsType { get; set; }

    public string? ImageUrl
    {
        get
        {
            var relativePath = NewsPageImage?.Url ?? PlayPageImage?.Url;
            if (string.IsNullOrEmpty(relativePath)) return null;
            return "https://launchercontent.mojang.com" + relativePath;
        }
    }

    public string FormattedDate
    {
        get
        {
            if (DateTime.TryParse(Date, out var parsedDate))
            {
                return parsedDate.ToString("yyyy-MM-dd");
            }
            return Date;
        }
    }
}

public class NewsImage
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
}

public class NewsService : Helpers.ObservableBase
{
    private List<NewsEntry> _news = new();
    public List<NewsEntry> News { get => _news; set => SetProperty(ref _news, value); }

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    private string? _error;
    public string? Error { get => _error; set => SetProperty(ref _error, value); }

    private const string NewsUrl = "https://launchercontent.mojang.com/news.json";

    public async Task FetchNews()
    {
        IsLoading = true;
        Error = null;

        try
        {
            var response = await HttpClientFactory.Shared.GetAsync(NewsUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var feed = JsonSerializer.Deserialize<NewsFeed>(json);

            if (feed != null)
            {
                News = feed.Entries;
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            Console.WriteLine($"[NewsService] Error fetching news: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

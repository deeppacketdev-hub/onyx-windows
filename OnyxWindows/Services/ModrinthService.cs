using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OnyxWindows.Services;

// ── Modrinth DTOs ──

public class ModrinthProject
{
    [JsonPropertyName("project_id")] public string ProjectId { get; set; } = "";
    [JsonPropertyName("slug")] public string Slug { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("icon_url")] public string? IconUrl { get; set; }
    [JsonPropertyName("downloads")] public int Downloads { get; set; }
    [JsonPropertyName("project_type")] public string ProjectType { get; set; } = "";
    [JsonPropertyName("categories")] public List<string> Categories { get; set; } = new();
    [JsonPropertyName("versions")] public List<string> Versions { get; set; } = new();
    [JsonPropertyName("author")] public string? Author { get; set; }
}

public class ModrinthVersion
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("version_number")] public string VersionNumber { get; set; } = "";
    [JsonPropertyName("loaders")] public List<string> Loaders { get; set; } = new();
    [JsonPropertyName("game_versions")] public List<string> GameVersions { get; set; } = new();
    [JsonPropertyName("files")] public List<ModrinthFile> Files { get; set; } = new();
    [JsonPropertyName("date_published")] public string DatePublished { get; set; } = "";

    [JsonIgnore]
    public string PrimaryGameVersion => GameVersions.Count > 0 ? GameVersions[0] : "";
}

public class ModrinthFile
{
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("filename")] public string Filename { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("primary")] public bool Primary { get; set; }
}

public class ModrinthSearchResult
{
    [JsonPropertyName("hits")] public List<ModrinthProject> Hits { get; set; } = new();
    [JsonPropertyName("total_hits")] public int TotalHits { get; set; }
    [JsonPropertyName("limit")] public int Limit { get; set; }
    [JsonPropertyName("offset")] public int Offset { get; set; }
}

/// <summary>
/// Modrinth API client for searching and downloading mods.
/// </summary>
public class ModrinthService
{
    private const string BaseUrl = "https://api.modrinth.com/v2";

    public async Task<ModrinthSearchResult> Search(string query, string? mcVersion = null,
        string? loader = null, string? projectType = null, int limit = 20, int offset = 0)
    {
        var facets = new List<string>();
        if (!string.IsNullOrEmpty(mcVersion))
            facets.Add($"[\"versions:{mcVersion}\"]");
        if (!string.IsNullOrEmpty(loader))
            facets.Add($"[\"categories:{loader}\"]");
        if (!string.IsNullOrEmpty(projectType))
            facets.Add($"[\"project_type:{projectType}\"]");

        var facetsParam = facets.Count > 0 ? $"&facets=[{string.Join(",", facets)}]" : "";
        var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}&limit={limit}&offset={offset}{facetsParam}";

        var json = await HttpClientFactory.Shared.GetStringAsync(url);
        return JsonSerializer.Deserialize<ModrinthSearchResult>(json) ?? new();
    }

    public async Task<List<ModrinthVersion>> GetVersions(string projectId, string? mcVersion = null, string? loader = null)
    {
        var url = $"{BaseUrl}/project/{projectId}/version";
        var qp = new List<string>();
        if (mcVersion != null) qp.Add($"game_versions=[\"{mcVersion}\"]");
        if (loader != null) qp.Add($"loaders=[\"{loader}\"]");
        if (qp.Count > 0) url += "?" + string.Join("&", qp);

        var json = await HttpClientFactory.Shared.GetStringAsync(url);
        return JsonSerializer.Deserialize<List<ModrinthVersion>>(json) ?? new();
    }

    public async Task<byte[]> DownloadFile(string url)
    {
        return await HttpClientFactory.Shared.GetByteArrayAsync(url);
    }

    public async Task DownloadFile(ModrinthFile file, string destinationDir)
    {
        var data = await DownloadFile(file.Url);
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }
        var filePath = Path.Combine(destinationDir, file.Filename);
        await File.WriteAllBytesAsync(filePath, data);
    }
}

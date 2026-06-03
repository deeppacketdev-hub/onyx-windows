using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

// ── CurseForge DTOs ──

public class CFSearchResponse
{
    [JsonPropertyName("data")] public List<CFMod> Data { get; set; } = new();
    [JsonPropertyName("pagination")] public CFPagination Pagination { get; set; } = new();
}

public class CFFilesResponse
{
    [JsonPropertyName("data")] public List<CFFile> Data { get; set; } = new();
    [JsonPropertyName("pagination")] public CFPagination Pagination { get; set; } = new();
}

public class CFPagination
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("pageSize")] public int PageSize { get; set; }
    [JsonPropertyName("resultCount")] public int ResultCount { get; set; }
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
}

public class CFMod
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("gameId")] public int GameId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("slug")] public string Slug { get; set; } = "";
    [JsonPropertyName("links")] public CFLinks Links { get; set; } = new();
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
    [JsonPropertyName("downloadCount")] public int DownloadCount { get; set; }
    [JsonPropertyName("logo")] public CFImage? Logo { get; set; }
    [JsonPropertyName("authors")] public List<CFAuthor> Authors { get; set; } = new();
    [JsonPropertyName("categories")] public List<CFCategory> Categories { get; set; } = new();
    [JsonPropertyName("allowModDistribution")] public bool? AllowModDistribution { get; set; }

    public string FormattedDownloads
    {
        get
        {
            if (DownloadCount >= 1_000_000)
                return $"{(double)DownloadCount / 1_000_000:F1}M";
            if (DownloadCount >= 1_000)
                return $"{(double)DownloadCount / 1_000:F1}K";
            return DownloadCount.ToString();
        }
    }

    public string? AuthorName => Authors.Count > 0 ? Authors[0].Name : null;
}

public class CFLinks
{
    [JsonPropertyName("websiteUrl")] public string? WebsiteUrl { get; set; }
    [JsonPropertyName("wikiUrl")] public string? WikiUrl { get; set; }
    [JsonPropertyName("issuesUrl")] public string? IssuesUrl { get; set; }
    [JsonPropertyName("sourceUrl")] public string? SourceUrl { get; set; }
}

public class CFImage
{
    [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
}

public class CFAuthor
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("url")] public string? Url { get; set; }
}

public class CFCategory
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("slug")] public string Slug { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("iconUrl")] public string IconUrl { get; set; } = "";
}

public class CFFile
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("modId")] public int ModId { get; set; }
    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("fileName")] public string FileName { get; set; } = "";
    [JsonPropertyName("fileLength")] public int FileLength { get; set; }
    [JsonPropertyName("downloadUrl")] public string? DownloadUrl { get; set; }
    [JsonPropertyName("gameVersions")] public List<string> GameVersions { get; set; } = new();
    [JsonPropertyName("fileDate")] public string FileDate { get; set; } = "";

    public List<string> MinecraftVersions
    {
        get
        {
            var loaderNames = new HashSet<string> { "Forge", "Fabric", "NeoForge", "Quilt", "Bukkit", "Client", "Server" };
            return GameVersions.FindAll(v => !loaderNames.Contains(v) && !v.Contains("snapshot"));
        }
    }

    public List<string> Loaders
    {
        get
        {
            var loaderNames = new HashSet<string> { "Forge", "Fabric", "NeoForge", "Quilt" };
            return GameVersions.FindAll(v => loaderNames.Contains(v));
        }
    }
}

public enum ModrinthSortIndex { Relevance, Downloads, Follows, Newest, Updated }

public class CurseForgeService : Helpers.ObservableBase
{
    private List<CFMod> _searchResults = new();
    public List<CFMod> SearchResults { get => _searchResults; set => SetProperty(ref _searchResults, value); }

    private bool _isSearching;
    public bool IsSearching { get => _isSearching; set => SetProperty(ref _isSearching, value); }

    private bool _isLoadingMore;
    public bool IsLoadingMore { get => _isLoadingMore; set => SetProperty(ref _isLoadingMore, value); }

    private bool _hasMoreResults = true;
    public bool HasMoreResults { get => _hasMoreResults; set => SetProperty(ref _hasMoreResults, value); }

    private string? _error;
    public string? Error { get => _error; set => SetProperty(ref _error, value); }

    private List<CFFile> _modFiles = new();
    public List<CFFile> ModFiles { get => _modFiles; set => SetProperty(ref _modFiles, value); }

    private bool _isLoadingFiles;
    public bool IsLoadingFiles { get => _isLoadingFiles; set => SetProperty(ref _isLoadingFiles, value); }

    private int _currentOffset;
    private const string BaseUrl = "https://api.curseforge.com/v1";
    private const string ApiKey = "$2a$10$GqkoXk2OSamTNQaAabwTweQ4znpO7YfZIxsyB7wjA2R38JTEc0kY6";
    private const int MinecraftGameId = 432;

    private string _lastQuery = "";
    private string? _lastGameVersion;
    private ModLoaderType? _lastLoader;
    private int _lastClassId = 6;
    private int _lastSortField = 2;
    private int? _lastCategoryId;

    private int GetLoaderId(ModLoaderType loader)
    {
        return loader switch
        {
            ModLoaderType.Forge => 1,
            ModLoaderType.Fabric => 4,
            ModLoaderType.Quilt => 5,
            ModLoaderType.Neoforge => 6,
            _ => 1
        };
    }

    public static int CfSortField(ModrinthSortIndex modrinthSort)
    {
        return modrinthSort switch
        {
            ModrinthSortIndex.Relevance => 1,
            ModrinthSortIndex.Downloads => 6,
            ModrinthSortIndex.Follows => 2,
            ModrinthSortIndex.Newest => 3,
            ModrinthSortIndex.Updated => 3,
            _ => 2
        };
    }

    public async Task Search(
        string query,
        string? gameVersion = null,
        ModLoaderType? loader = null,
        int classId = 6,
        int limit = 40,
        bool isLoadMore = false,
        int sortField = 2,
        int? categoryId = null)
    {
        if (isLoadMore)
        {
            if (!HasMoreResults || IsLoadingMore) return;
            IsLoadingMore = true;
        }
        else
        {
            IsSearching = true;
            _currentOffset = 0;
            SearchResults = new();
            HasMoreResults = true;
        }

        Error = null;
        _lastQuery = query;
        _lastGameVersion = gameVersion;
        _lastLoader = loader;
        _lastClassId = classId;
        _lastSortField = sortField;
        _lastCategoryId = categoryId;

        try
        {
            var queryParams = new List<string>
            {
                $"gameId={MinecraftGameId}",
                $"classId={classId}",
                $"sortField={sortField}",
                $"sortOrder=desc",
                $"pageSize={limit}",
                $"index={_currentOffset}"
            };

            if (!string.IsNullOrEmpty(query))
                queryParams.Add($"searchFilter={Uri.EscapeDataString(query)}");

            if (gameVersion != null)
                queryParams.Add($"gameVersion={Uri.EscapeDataString(gameVersion)}");

            if (loader != null && classId == 6)
                queryParams.Add($"modLoaderType={GetLoaderId(loader.Value)}");

            if (categoryId != null)
                queryParams.Add($"categoryId={categoryId}");

            var url = $"{BaseUrl}/mods/search?{string.Join("&", queryParams)}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", ApiKey);
            request.Headers.Add("Accept", "application/json");

            var response = await HttpClientFactory.Shared.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<CFSearchResponse>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (searchResponse != null)
            {
                if (isLoadMore)
                {
                    var updated = new List<CFMod>(SearchResults);
                    updated.AddRange(searchResponse.Data);
                    SearchResults = updated;
                }
                else
                {
                    SearchResults = searchResponse.Data;
                }

                _currentOffset += searchResponse.Data.Count;
                HasMoreResults = searchResponse.Data.Count == limit;
            }
        }
        catch (Exception ex)
        {
            Error = $"Search failed: {ex.Message}";
            Console.WriteLine($"[CurseForgeService] {Error}");
        }
        finally
        {
            IsSearching = false;
            IsLoadingMore = false;
        }
    }

    public async Task LoadMore()
    {
        await Search(
            _lastQuery,
            _lastGameVersion,
            _lastLoader,
            _lastClassId,
            isLoadMore: true,
            sortField: _lastSortField,
            categoryId: _lastCategoryId
        );
    }

    public async Task GetFiles(int modId, string? gameVersion = null, ModLoaderType? loader = null)
    {
        IsLoadingFiles = true;
        ModFiles = new();

        try
        {
            ModFiles = await FetchFiles(modId, gameVersion, loader);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CurseForgeService] Files failed: {ex}");
        }
        finally
        {
            IsLoadingFiles = false;
        }
    }

    public async Task<List<CFFile>> FetchFiles(int modId, string? gameVersion = null, ModLoaderType? loader = null)
    {
        var queryParams = new List<string> { "pageSize=20" };

        if (gameVersion != null)
            queryParams.Add($"gameVersion={Uri.EscapeDataString(gameVersion)}");

        if (loader != null)
            queryParams.Add($"modLoaderType={GetLoaderId(loader.Value)}");

        var url = $"{BaseUrl}/mods/{modId}/files?{string.Join("&", queryParams)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-api-key", ApiKey);
        request.Headers.Add("Accept", "application/json");

        var response = await HttpClientFactory.Shared.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var filesResponse = JsonSerializer.Deserialize<CFFilesResponse>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return filesResponse?.Data ?? new List<CFFile>();
    }

    private string? GetCdnFallbackUrl(int fileId, string fileName)
    {
        var idStr = fileId.ToString();
        if (idStr.Length < 4) return null;
        var part1 = idStr.Substring(0, 4);
        var part2 = idStr.Substring(4);
        return $"https://edge.forgecdn.net/files/{part1}/{part2}/{Uri.EscapeDataString(fileName)}";
    }

    public async Task<string> DownloadFile(CFFile file, string directory)
    {
        string? url = null;
        if (!string.IsNullOrEmpty(file.DownloadUrl))
        {
            url = file.DownloadUrl;
        }
        else
        {
            url = GetCdnFallbackUrl(file.Id, file.FileName);
        }

        if (url == null)
        {
            throw new Exception("This mod does not allow 3rd party distribution");
        }

        var destination = Path.Combine(directory, file.FileName);
        if (File.Exists(destination))
        {
            return destination;
        }

        Directory.CreateDirectory(directory);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/octet-stream");

        var response = await HttpClientFactory.Shared.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var destStream = File.Create(destination);
        await response.Content.CopyToAsync(destStream);

        return destination;
    }
}

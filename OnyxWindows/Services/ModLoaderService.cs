using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OnyxWindows.Services;

/// <summary>
/// Lists available mod loader versions (Fabric, Quilt, Forge, NeoForge).
/// </summary>
public class ModLoaderService
{
    public async Task<List<string>> GetFabricVersions(string mcVersion)
    {
        var url = $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}";
        return await FetchLoaderVersions(url, "version");
    }

    public async Task<List<string>> GetQuiltVersions(string mcVersion)
    {
        var url = $"https://meta.quiltmc.org/v3/versions/loader/{mcVersion}";
        return await FetchLoaderVersions(url, "version");
    }

    public async Task<List<string>> GetForgeVersions(string mcVersion)
    {
        var url = $"https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";
        try
        {
            var json = await HttpClientFactory.Shared.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var promos = doc.RootElement.GetProperty("promos");
            var versions = new List<string>();

            foreach (var prop in promos.EnumerateObject())
            {
                if (prop.Name.StartsWith($"{mcVersion}-"))
                {
                    var ver = prop.Value.GetString();
                    if (ver != null && !versions.Contains(ver))
                        versions.Add(ver);
                }
            }
            return versions;
        }
        catch { return new List<string>(); }
    }

    public async Task<List<string>> GetNeoForgeVersions(string mcVersion)
    {
        try
        {
            var json = await HttpClientFactory.Shared.GetStringAsync("https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge");
            using var doc = JsonDocument.Parse(json);
            var versions = new List<string>();

            if (doc.RootElement.TryGetProperty("versions", out var versionsArray))
            {
                foreach (var v in versionsArray.EnumerateArray())
                {
                    var ver = v.GetString();
                    if (ver != null && ver.StartsWith(mcVersion.Replace("1.", "").Split('.')[0] + "."))
                        versions.Add(ver);
                }
            }
            versions.Reverse();
            return versions;
        }
        catch { return new List<string>(); }
    }

    private async Task<List<string>> FetchLoaderVersions(string url, string versionKey)
    {
        try
        {
            var json = await HttpClientFactory.Shared.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var versions = new List<string>();

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("loader", out var loader) &&
                    loader.TryGetProperty(versionKey, out var v))
                {
                    versions.Add(v.GetString()!);
                }
            }
            return versions;
        }
        catch { return new List<string>(); }
    }
}

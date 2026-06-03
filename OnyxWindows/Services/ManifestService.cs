using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

/// <summary>
/// Fetches and caches the Mojang version manifest.
/// </summary>
public class ManifestService
{
    private const string ManifestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";

    public VersionManifest? Manifest { get; private set; }

    public async Task LoadManifest(string metaDirectory)
    {
        var cachedPath = Path.Combine(metaDirectory, "version_manifest_v2.json");

        try
        {
            var response = await HttpClientFactory.Shared.GetAsync(ManifestUrl);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            Manifest = JsonSerializer.Deserialize<VersionManifest>(json);

            // Cache to disk
            Directory.CreateDirectory(metaDirectory);
            await File.WriteAllTextAsync(cachedPath, json);
        }
        catch
        {
            // Fall back to cached
            if (File.Exists(cachedPath))
            {
                var json = await File.ReadAllTextAsync(cachedPath);
                Manifest = JsonSerializer.Deserialize<VersionManifest>(json);
            }
        }
    }

    public VersionEntry? GetVersion(string id)
    {
        return Manifest?.Versions.Find(v => v.Id == id);
    }
}

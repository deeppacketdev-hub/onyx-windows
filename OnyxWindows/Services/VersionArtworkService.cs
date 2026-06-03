using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OnyxWindows.Services;

public class VersionArtworkService : Helpers.ObservableBase
{
    private readonly Dictionary<string, string> _artworkCache = new();
    private readonly HashSet<string> _activeDownloads = new();

    private readonly List<(List<int> version, string versionString, string url)> _officialKeyArts;

    public VersionArtworkService()
    {
        var raw = new Dictionary<string, string>
        {
            ["26.1"] = "https://minecraft.wiki/images/Tiny_Takeover_Key_Art.png",
            ["1.21.11"] = "https://minecraft.wiki/images/Mounts_of_Mayhem_Key_Art.png",
            ["1.21.9"] = "https://minecraft.wiki/images/The_Copper_Age_Key_Art.png",
            ["1.21.6"] = "https://minecraft.wiki/images/Chase_the_Skies_Key_Art.jpg",
            ["1.21.5"] = "https://minecraft.wiki/images/Spring_to_Life_Key_Art.jpg",
            ["1.21.4"] = "https://minecraft.wiki/images/The_Garden_Awakens_Key_Art.png",
            ["1.21.2"] = "https://minecraft.wiki/images/Bundles_of_Bravery_Key_Art.png",
            ["1.21"] = "https://minecraft.wiki/images/Tricky_Trials_Key_Art.png",
            ["1.20"] = "https://minecraft.wiki/images/Trails_%26_Tales_key_art.png",
            ["1.19"] = "https://minecraft.wiki/images/Wild_key_art.png",
            ["1.18"] = "https://minecraft.wiki/images/Caves_%26_Cliffs_Part_II.png",
            ["1.17"] = "https://minecraft.wiki/images/Caves_%26_Cliffs_cover_art.png",
            ["1.16"] = "https://minecraft.wiki/images/NetherUpdateArtwork.png",
            ["1.15"] = "https://minecraft.wiki/images/Buzzy_Bees.png",
            ["1.14"] = "https://minecraft.wiki/images/Village_%26_Pillage_banner.png",
            ["1.13"] = "https://minecraft.wiki/images/Update_Aquatic.png",
            ["1.12"] = "https://minecraft.wiki/images/World_of_Color_Update.png",
            ["1.11"] = "https://minecraft.wiki/images/ExplorationUpdateFull.jpg",
            ["1.9"] = "https://minecraft.wiki/images/Combat_Update.png",
            ["1.6"] = "https://minecraft.wiki/images/Horse_Update_Wallpaper.jpg"
        };

        _officialKeyArts = raw.Select(kp =>
        {
            var parts = ParseVersion(kp.Key);
            return (version: parts, versionString: kp.Key, url: kp.Value);
        })
        .OrderByDescending(x => x.version, new VersionComparer())
        .ToList();
    }

    private string ArtMappingFingerprint
    {
        get
        {
            var combined = string.Join("|", _officialKeyArts.Select(x => $"{x.versionString}={x.url}"));
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }

    public string? GetArtworkPath(string version, string iconsDir)
    {
        lock (_artworkCache)
        {
            if (_artworkCache.TryGetValue(version, out var cached))
            {
                if (File.Exists(cached)) return cached;
                _artworkCache.Remove(version);
            }
        }

        var filePath = GetArtworkFile(version, iconsDir);
        if (File.Exists(filePath))
        {
            lock (_artworkCache)
            {
                _artworkCache[version] = filePath;
            }
            return filePath;
        }

        return null;
    }

    public async Task FetchArtwork(string version, string iconsDir)
    {
        if (GetArtworkPath(version, iconsDir) != null) return;

        lock (_activeDownloads)
        {
            if (_activeDownloads.Contains(version)) return;
            _activeDownloads.Add(version);
        }

        try
        {
            var imageUrl = FindImageUrl(version);
            if (imageUrl == null)
            {
                Console.WriteLine($"[VersionArtwork] No artwork found for version {version}");
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await HttpClientFactory.Shared.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[VersionArtwork] Failed to download artwork for {version}: bad response");
                return;
            }

            var data = await response.Content.ReadAsByteArrayAsync();

            var artworkDir = Path.Combine(iconsDir, "version_artwork");
            Directory.CreateDirectory(artworkDir);

            var ext = Path.GetExtension(imageUrl);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";

            var destFile = Path.Combine(artworkDir, $"{version}{ext}");
            await File.WriteAllBytesAsync(destFile, data);

            lock (_artworkCache)
            {
                _artworkCache[version] = destFile;
            }
            Console.WriteLine($"[VersionArtwork] ✅ Saved artwork for {version} (from key art)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VersionArtwork] Failed to download artwork for {version}: {ex.Message}");
        }
        finally
        {
            lock (_activeDownloads)
            {
                _activeDownloads.Remove(version);
            }
        }
    }

    public async Task PrefetchArtwork(List<string> versions, string iconsDir)
    {
        InvalidateStaleCacheIfNeeded(iconsDir);

        var tasks = new List<Task>();
        foreach (var version in versions)
        {
            if (GetArtworkPath(version, iconsDir) == null)
            {
                tasks.Add(FetchArtwork(version, iconsDir));
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private void InvalidateStaleCacheIfNeeded(string iconsDir)
    {
        var artworkDir = Path.Combine(iconsDir, "version_artwork");
        var fingerprintFile = Path.Combine(artworkDir, ".fingerprint");
        var currentFingerprint = ArtMappingFingerprint;

        if (File.Exists(fingerprintFile))
        {
            try
            {
                var stored = File.ReadAllText(fingerprintFile);
                if (stored == currentFingerprint) return;
            }
            catch { }
        }

        Console.WriteLine("[VersionArtwork] Art mapping changed, clearing stale cache...");
        lock (_artworkCache)
        {
            _artworkCache.Clear();
        }

        if (Directory.Exists(artworkDir))
        {
            try
            {
                foreach (var file in Directory.GetFiles(artworkDir))
                {
                    if (Path.GetFileName(file) != ".fingerprint")
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { }
        }

        try
        {
            Directory.CreateDirectory(artworkDir);
            File.WriteAllText(fingerprintFile, currentFingerprint);
        }
        catch { }
    }

    private string? FindImageUrl(string version)
    {
        var targetParts = ParseVersion(version);
        if (targetParts.Count == 0) return null;

        foreach (var entry in _officialKeyArts)
        {
            if (IsVersionLessThanOrEqual(entry.version, targetParts))
            {
                return entry.url;
            }
        }

        return null;
    }

    private static List<int> ParseVersion(string version)
    {
        return version.Split('.')
            .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
            .Where(v => v != null)
            .Select(v => v!.Value)
            .ToList();
    }

    private static bool IsVersionLessThanOrEqual(List<int> v1, List<int> v2)
    {
        var maxCount = Math.Max(v1.Count, v2.Count);
        for (int i = 0; i < maxCount; i++)
        {
            var p1 = i < v1.Count ? v1[i] : 0;
            var p2 = i < v2.Count ? v2[i] : 0;
            if (p1 < p2) return true;
            if (p1 > p2) return false;
        }
        return true;
    }

    private string GetArtworkFile(string version, string iconsDir)
    {
        var artworkDir = Path.Combine(iconsDir, "version_artwork");
        if (Directory.Exists(artworkDir))
        {
            foreach (var file in Directory.GetFiles(artworkDir))
            {
                if (Path.GetFileNameWithoutExtension(file) == version)
                    return file;
            }
        }
        return Path.Combine(artworkDir, $"{version}.jpg");
    }

    private class VersionComparer : IComparer<List<int>>
    {
        public int Compare(List<int>? x, List<int>? y)
        {
            if (x == null || y == null) return 0;
            var maxCount = Math.Max(x.Count, y.Count);
            for (int i = 0; i < maxCount; i++)
            {
                var a = i < x.Count ? x[i] : 0;
                var b = i < y.Count ? y[i] : 0;
                if (a != b) return a.CompareTo(b);
            }
            return 0;
        }
    }
}

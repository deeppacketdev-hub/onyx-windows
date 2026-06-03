using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OnyxWindows.Services;

/// <summary>
/// Fabric/Quilt mod loader installer. Downloads profile and libraries.
/// </summary>
public class FabricInstaller
{
    private readonly AppDataManager _appData;

    public FabricInstaller(AppDataManager appData) => _appData = appData;

    public async Task<(string MainClass, List<string> JvmArgs, List<string> Classpath)> PrepareLoader(
        string mcVersion, string loaderVersion, bool isQuilt)
    {
        var metaUrl = isQuilt
            ? $"https://meta.quiltmc.org/v3/versions/loader/{mcVersion}/{loaderVersion}/profile/json"
            : $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}/{loaderVersion}/profile/json";

        var request = new HttpRequestMessage(HttpMethod.Get, metaUrl);
        var response = await HttpClientFactory.Shared.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var mainClass = root.GetProperty("mainClass").GetString()!;

        var jvmArgs = new List<string>();
        if (root.TryGetProperty("arguments", out var args) && args.TryGetProperty("jvm", out var jvm))
        {
            foreach (var item in jvm.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String)
                    jvmArgs.Add(item.GetString()!);
        }

        var classpath = new List<string>();
        var downloadTasks = new List<(string url, string dest)>();

        foreach (var lib in root.GetProperty("libraries").EnumerateArray())
        {
            var name = lib.GetProperty("name").GetString()!;
            var url = lib.GetProperty("url").GetString()!;
            var parts = name.Split(':');
            if (parts.Length < 3) continue;

            var group = parts[0].Replace('.', Path.DirectorySeparatorChar);
            var artifact = parts[1];
            var version = parts[2];
            var relPath = Path.Combine(group, artifact, version, $"{artifact}-{version}.jar");
            var targetPath = Path.Combine(_appData.LibrariesDirectory, relPath);
            classpath.Add(targetPath);

            if (!File.Exists(targetPath))
            {
                var downloadUrl = $"{url.TrimEnd('/')}/{relPath.Replace(Path.DirectorySeparatorChar, '/')}";
                downloadTasks.Add((downloadUrl, targetPath));
            }
        }

        // Download missing libraries
        if (downloadTasks.Count > 0)
        {
            await Parallel.ForEachAsync(downloadTasks, new ParallelOptions { MaxDegreeOfParallelism = 4 },
                async (task, ct) =>
                {
                    var dir = Path.GetDirectoryName(task.dest)!;
                    Directory.CreateDirectory(dir);

                    using var resp = await HttpClientFactory.Shared.GetAsync(task.url, ct);
                    resp.EnsureSuccessStatusCode();
                    var data = await resp.Content.ReadAsByteArrayAsync(ct);
                    await File.WriteAllBytesAsync(task.dest, data, ct);
                });
        }

        // Ensure intermediary mappings are present
        var hasIntermediary = json.Contains("intermediary");
        if (!hasIntermediary)
        {
            var intGroup = $"net{Path.DirectorySeparatorChar}fabricmc";
            var intPath = Path.Combine(intGroup, "intermediary", mcVersion, $"intermediary-{mcVersion}.jar");
            var intTarget = Path.Combine(_appData.LibrariesDirectory, intPath);
            classpath.Add(intTarget);

            if (!File.Exists(intTarget))
            {
                var intUrl = $"https://maven.fabricmc.net/{intPath.Replace(Path.DirectorySeparatorChar, '/')}";
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(intTarget)!);
                    var data = await HttpClientFactory.Shared.GetByteArrayAsync(intUrl);
                    await File.WriteAllBytesAsync(intTarget, data);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine($"[FabricInstaller] Warning: Could not download intermediary for {mcVersion}");
                }
            }
        }

        return (mainClass, jvmArgs, classpath);
    }
}

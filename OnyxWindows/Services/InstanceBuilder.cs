using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

/// <summary>
/// Parses a version's JSON and builds download lists, classpath, and native library info.
/// All platform-specific logic uses Windows rules (natives-windows, os.name == "windows").
/// </summary>
public class InstanceBuilder
{
    private readonly AppDataManager _appData;

    public InstanceBuilder(AppDataManager appData)
    {
        _appData = appData;
    }

    /// <summary>
    /// Build a complete list of DownloadItems for a Minecraft version.
    /// </summary>
    public List<DownloadItem> BuildDownloadList(VersionDetail versionDetail)
    {
        var items = new List<DownloadItem>();

        // 1. Client JAR
        var clientJarDir = Path.Combine(_appData.VersionsDirectory, versionDetail.Id);
        var clientJar = Path.Combine(clientJarDir, $"{versionDetail.Id}.jar");

        if (versionDetail.Downloads != null)
        {
            items.Add(new DownloadItem(
                versionDetail.Downloads.Client.Url,
                clientJar,
                versionDetail.Downloads.Client.Sha1,
                versionDetail.Downloads.Client.Size,
                $"{versionDetail.Id}.jar"
            ));
        }

        // 2. Libraries (filtered for Windows)
        items.AddRange(BuildLibraryList(versionDetail.Libraries));

        // 3. Asset index
        if (versionDetail.AssetIndex != null)
        {
            var assetIndexFile = Path.Combine(_appData.AssetsIndexesDirectory, $"{versionDetail.AssetIndex.Id}.json");
            items.Add(new DownloadItem(
                versionDetail.AssetIndex.Url,
                assetIndexFile,
                versionDetail.AssetIndex.Sha1,
                versionDetail.AssetIndex.Size,
                $"Asset Index {versionDetail.AssetIndex.Id}"
            ));
        }

        return items;
    }

    /// <summary>
    /// Build download items for asset objects (called after asset index is downloaded).
    /// </summary>
    public List<DownloadItem> BuildAssetDownloadList(string assetIndexId)
    {
        var indexFile = Path.Combine(_appData.AssetsIndexesDirectory, $"{assetIndexId}.json");
        var json = File.ReadAllText(indexFile);
        var assetIndex = System.Text.Json.JsonSerializer.Deserialize<AssetIndex>(json);
        if (assetIndex == null) return new List<DownloadItem>();

        return assetIndex.Objects.Select(kvp =>
        {
            var objectPath = Path.Combine(_appData.AssetsObjectsDirectory, kvp.Value.HashPrefix, kvp.Value.Hash);
            var url = $"https://resources.download.minecraft.net/{kvp.Value.HashPrefix}/{kvp.Value.Hash}";
            return new DownloadItem(url, objectPath, kvp.Value.Hash, kvp.Value.Size, kvp.Key);
        }).ToList();
    }

    /// <summary>
    /// Get the required Java major version for a version detail.
    /// </summary>
    public int RequiredJavaVersion(VersionDetail detail) => detail.JavaVersion?.MajorVersion ?? 8;

    /// <summary>
    /// Build the classpath string for launching (Windows uses ';' separator).
    /// </summary>
    public string BuildClasspath(VersionDetail detail)
    {
        var paths = new List<string>();

        foreach (var library in detail.Libraries)
        {
            if (!ShouldIncludeLibrary(library)) continue;

            if (library.Downloads?.Artifact != null)
            {
                paths.Add(Path.Combine(_appData.LibrariesDirectory, library.Downloads.Artifact.Path.Replace('/', Path.DirectorySeparatorChar)));
            }
            else if (library.Url != null || library.Downloads == null)
            {
                paths.Add(_appData.LibraryPath(library.Name));
            }
        }

        // Add client JAR at the end
        var clientJar = Path.Combine(_appData.VersionsDirectory, detail.Id, $"{detail.Id}.jar");
        paths.Add(clientJar);

        return string.Join(';', paths);
    }

    /// <summary>
    /// Get native libraries that need to be extracted (Windows-specific).
    /// </summary>
    public List<LibraryArtifact> NativeLibraries(VersionDetail detail)
    {
        var nativeArtifacts = new List<LibraryArtifact>();

        foreach (var library in detail.Libraries)
        {
            if (!ShouldIncludeLibrary(library)) continue;

            // Check for natives-windows classifier
            if (library.Natives != null)
            {
                string? classifierKey = null;
                if (library.Natives.TryGetValue("windows", out var key))
                    classifierKey = key;

                if (classifierKey != null && library.Downloads?.Classifiers != null)
                {
                    // Replace ${arch} placeholder
                    classifierKey = classifierKey.Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32");

                    if (library.Downloads.Classifiers.TryGetValue(classifierKey, out var nativeArtifact))
                        nativeArtifacts.Add(nativeArtifact);
                }
            }

            // Modern LWJGL natives (embedded in artifact name)
            if (library.Downloads?.Artifact != null &&
                (library.Name.Contains("natives-windows") || library.Name.Contains("natives-win")))
            {
                nativeArtifacts.Add(library.Downloads.Artifact);
            }
        }

        return nativeArtifacts;
    }

    // ── Private ──

    private List<DownloadItem> BuildLibraryList(List<Library> libraries)
    {
        var items = new List<DownloadItem>();

        foreach (var library in libraries)
        {
            if (!ShouldIncludeLibrary(library)) continue;

            // Main artifact
            if (library.Downloads?.Artifact != null)
            {
                var dest = Path.Combine(_appData.LibrariesDirectory, library.Downloads.Artifact.Path.Replace('/', Path.DirectorySeparatorChar));
                items.Add(new DownloadItem(
                    library.Downloads.Artifact.Url,
                    dest,
                    library.Downloads.Artifact.Sha1,
                    library.Downloads.Artifact.Size,
                    library.Name
                ));
            }

            // Native classifiers (Windows)
            if (library.Natives != null && library.Natives.TryGetValue("windows", out var classifierKey))
            {
                classifierKey = classifierKey.Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32");

                if (library.Downloads?.Classifiers != null &&
                    library.Downloads.Classifiers.TryGetValue(classifierKey, out var nativeArtifact))
                {
                    var dest = Path.Combine(_appData.LibrariesDirectory, nativeArtifact.Path.Replace('/', Path.DirectorySeparatorChar));
                    items.Add(new DownloadItem(
                        nativeArtifact.Url,
                        dest,
                        nativeArtifact.Sha1,
                        nativeArtifact.Size,
                        $"{library.Name} (native)"
                    ));
                }
            }
        }

        return items;
    }

    /// <summary>
    /// Check if a library should be included based on its rules (Windows filter).
    /// </summary>
    private bool ShouldIncludeLibrary(Library library)
    {
        if (library.Rules == null || library.Rules.Count == 0)
            return true;

        bool allowed = false;

        foreach (var rule in library.Rules)
        {
            bool ruleMatches;

            if (rule.Os != null)
            {
                var nameMatches = rule.Os.Name == null || rule.Os.Name == "windows";
                var archMatches = rule.Os.Arch == null ||
                                  rule.Os.Arch == "x86" ||
                                  rule.Os.Arch == "x86_64" ||
                                  rule.Os.Arch == "amd64" ||
                                  (rule.Os.Arch == "x64");
                ruleMatches = nameMatches && archMatches;
            }
            else
            {
                ruleMatches = true;
            }

            if (ruleMatches)
                allowed = rule.Action == "allow";
        }

        return allowed;
    }
}

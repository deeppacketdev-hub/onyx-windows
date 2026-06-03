using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using OnyxWindows.Helpers;

namespace OnyxWindows.Services;

/// <summary>
/// Manages Java installations. Downloads Adoptium JDKs automatically.
/// Uses Windows .zip packages and javaw.exe.
/// </summary>
public class JavaManager : ObservableBase
{
    private readonly string _javaDirectory;

    public JavaManager(string javaDirectory)
    {
        _javaDirectory = javaDirectory;
    }

    /// <summary>
    /// Check if a Java version is already installed.
    /// </summary>
    public bool IsJavaAvailable(int majorVersion)
    {
        var javaExe = FindInstalledJava(majorVersion);
        return javaExe != null && File.Exists(javaExe);
    }

    /// <summary>
    /// Get or download a Java executable for the given major version.
    /// Returns the path to javaw.exe.
    /// </summary>
    public async Task<string> GetJavaExecutable(int majorVersion)
    {
        var existing = FindInstalledJava(majorVersion);
        if (existing != null) return existing;

        return await DownloadJava(majorVersion);
    }

    /// <summary>
    /// List installed Java versions.
    /// </summary>
    public List<(int version, string path)> ListInstalledJava()
    {
        var result = new List<(int, string)>();
        if (!Directory.Exists(_javaDirectory)) return result;

        foreach (var dir in Directory.GetDirectories(_javaDirectory))
        {
            var name = Path.GetFileName(dir);
            if (name.StartsWith("java-"))
            {
                if (int.TryParse(name.Replace("java-", ""), out var ver))
                {
                    var exe = Path.Combine(dir, "bin", "javaw.exe");
                    if (File.Exists(exe))
                        result.Add((ver, exe));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Delete a specific Java version.
    /// </summary>
    public void DeleteJava(int majorVersion)
    {
        var dir = Path.Combine(_javaDirectory, $"java-{majorVersion}");
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    // ── Private ──

    private string? FindInstalledJava(int majorVersion)
    {
        var dir = Path.Combine(_javaDirectory, $"java-{majorVersion}");
        if (!Directory.Exists(dir)) return null;

        // Look for javaw.exe recursively (Adoptium extracts with a versioned subfolder)
        var javaw = Directory.GetFiles(dir, "javaw.exe", SearchOption.AllDirectories).FirstOrDefault();
        return javaw;
    }

    private async Task<string> DownloadJava(int majorVersion)
    {
        var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "aarch64" : "x64";
        var url = $"https://api.adoptium.net/v3/binary/latest/{majorVersion}/ga/windows/{arch}/jdk/hotspot/normal/eclipse?" +
                  "project=jdk";

        System.Diagnostics.Debug.WriteLine($"[JavaManager] Downloading Java {majorVersion} from Adoptium ({arch})...");

        var targetDir = Path.Combine(_javaDirectory, $"java-{majorVersion}");
        Directory.CreateDirectory(targetDir);

        var zipPath = Path.Combine(_javaDirectory, $"java-{majorVersion}.zip");

        try
        {
            using var response = await HttpClientFactory.Shared.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
            await contentStream.CopyToAsync(fileStream);

            // Extract ZIP
            ZipFile.ExtractToDirectory(zipPath, targetDir, overwriteFiles: true);

            // Clean up ZIP
            File.Delete(zipPath);

            // Find the javaw.exe
            var javaw = Directory.GetFiles(targetDir, "javaw.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (javaw == null)
                throw new FileNotFoundException("javaw.exe not found after extraction");

            System.Diagnostics.Debug.WriteLine($"[JavaManager] Java {majorVersion} installed at {javaw}");
            return javaw;
        }
        catch
        {
            // Cleanup on failure
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            throw;
        }
    }
}

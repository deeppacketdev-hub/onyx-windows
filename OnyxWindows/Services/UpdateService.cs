using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OnyxWindows.Services;

public class AppcastRelease
{
    public string Version { get; set; } = "";
    public Dictionary<string, string> ReleaseNotes { get; set; } = new();
    public string DownloadUrl { get; set; } = "";
}

public enum UpdateState
{
    Idle,
    Checking,
    Available,
    Downloading,
    ReadyToInstall,
    UpToDate,
    Error
}

public class UpdateService : Helpers.ObservableBase
{
    private UpdateState _state = UpdateState.Idle;
    public UpdateState State { get => _state; set => SetProperty(ref _state, value); }

    private double _downloadProgress;
    public double DownloadProgress { get => _downloadProgress; set => SetProperty(ref _downloadProgress, value); }

    private AppcastRelease? _latestRelease;
    public AppcastRelease? LatestRelease { get => _latestRelease; set => SetProperty(ref _latestRelease, value); }

    private DateTime? _lastCheckedAt;
    public DateTime? LastCheckedAt { get => _lastCheckedAt; set => SetProperty(ref _lastCheckedAt, value); }

    private readonly string _currentVersion = "1.0.0";
    private readonly string _appcastUrl = "https://raw.githubusercontent.com/onyx-launcher/updates/main/appcast.json";

    public async Task CheckForUpdates()
    {
        State = UpdateState.Checking;
        LastCheckedAt = DateTime.Now;

        try
        {
            // Simulate check or check URL
            var response = await HttpClientFactory.Shared.GetAsync(_appcastUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<AppcastRelease>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                if (release != null && IsNewerVersion(release.Version, _currentVersion))
                {
                    LatestRelease = release;
                    State = UpdateState.Available;
                }
                else
                {
                    State = UpdateState.UpToDate;
                }
            }
            else
            {
                // Fallback / simulation for demo
                await Task.Delay(1000);
                State = UpdateState.UpToDate;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UpdateService] Check update failed: {ex}");
            State = UpdateState.Error;
        }
    }

    public async Task DownloadAndInstall()
    {
        if (LatestRelease == null || string.IsNullOrEmpty(LatestRelease.DownloadUrl)) return;

        State = UpdateState.Downloading;
        DownloadProgress = 0.0;

        try
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"OnyxUpdate_{LatestRelease.Version}.exe");

            using (var response = await HttpClientFactory.Shared.GetAsync(LatestRelease.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    var totalRead = 0L;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes != -1)
                        {
                            DownloadProgress = (double)totalRead / totalBytes;
                        }
                    }
                }
            }

            State = UpdateState.ReadyToInstall;

            // Trigger installer execution on Windows
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });

            // Shutdown the launcher to allow installer to run
            Microsoft.UI.Xaml.Application.Current.Exit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UpdateService] Download failed: {ex}");
            State = UpdateState.Error;
        }
    }

    private bool IsNewerVersion(string newVer, string currentVer)
    {
        try
        {
            var v1 = new Version(newVer);
            var v2 = new Version(currentVer);
            return v1 > v2;
        }
        catch
        {
            return false;
        }
    }
}

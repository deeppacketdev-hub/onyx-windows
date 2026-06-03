using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

/// <summary>
/// Parallel file downloader with SHA1 verification and progress reporting.
/// </summary>
public class DownloadManager : ObservableBase
{
    private bool _isDownloading;
    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetProperty(ref _isDownloading, value);
    }

    private int _totalFiles;
    public int TotalFiles
    {
        get => _totalFiles;
        set => SetProperty(ref _totalFiles, value);
    }

    private int _completedFiles;
    public int CompletedFiles
    {
        get => _completedFiles;
        set => SetProperty(ref _completedFiles, value);
    }

    private int _cachedFiles;
    public int CachedFiles
    {
        get => _cachedFiles;
        set => SetProperty(ref _cachedFiles, value);
    }

    private int _errorCount;
    public int ErrorCount
    {
        get => _errorCount;
        set => SetProperty(ref _errorCount, value);
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    private string _currentFile = "";
    public string CurrentFile
    {
        get => _currentFile;
        set => SetProperty(ref _currentFile, value);
    }

    private CancellationTokenSource? _cts;

    private const int MaxConcurrentDownloads = 8;

    /// <summary>
    /// Downloads all items concurrently with progress reporting.
    /// Returns true if all downloads succeeded.
    /// </summary>
    public async Task<bool> DownloadAll(List<DownloadItem> items, CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsDownloading = true;
        TotalFiles = items.Count;
        CompletedFiles = 0;
        CachedFiles = 0;
        ErrorCount = 0;
        Progress = 0;

        var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);
        var tasks = new List<Task>();

        foreach (var item in items)
        {
            if (_cts.Token.IsCancellationRequested) break;

            await semaphore.WaitAsync(_cts.Token);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await DownloadItem(item, _cts.Token);
                }
                finally
                {
                    semaphore.Release();
                }
            }, _cts.Token));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) { }

        IsDownloading = false;
        return ErrorCount == 0;
    }

    /// <summary>
    /// Cancel the current download operation.
    /// </summary>
    public void Cancel()
    {
        _cts?.Cancel();
    }

    private async Task DownloadItem(DownloadItem item, CancellationToken ct)
    {
        try
        {
            // Check if file already exists and matches SHA1
            if (File.Exists(item.Destination) && item.ExpectedSha1 != null)
            {
                var existingSha1 = ComputeSha1(item.Destination);
                if (string.Equals(existingSha1, item.ExpectedSha1, StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref _cachedFiles);
                    OnPropertyChanged(nameof(CachedFiles));
                    UpdateProgress();
                    return;
                }
            }
            else if (File.Exists(item.Destination) && item.ExpectedSha1 == null)
            {
                // No SHA1 to verify, assume cached
                Interlocked.Increment(ref _cachedFiles);
                OnPropertyChanged(nameof(CachedFiles));
                UpdateProgress();
                return;
            }

            CurrentFile = item.Label;

            // Ensure destination directory exists
            var dir = Path.GetDirectoryName(item.Destination);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Download
            using var response = await HttpClientFactory.Shared.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var tempFile = item.Destination + ".tmp";
            await using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
            await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await contentStream.CopyToAsync(fileStream, ct);
            }

            // Verify SHA1
            if (item.ExpectedSha1 != null)
            {
                var downloadedSha1 = ComputeSha1(tempFile);
                if (!string.Equals(downloadedSha1, item.ExpectedSha1, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(tempFile);
                    throw new InvalidDataException($"SHA1 mismatch for {item.Label}");
                }
            }

            // Move to final location
            if (File.Exists(item.Destination))
                File.Delete(item.Destination);
            File.Move(tempFile, item.Destination);

            Interlocked.Increment(ref _completedFiles);
            OnPropertyChanged(nameof(CompletedFiles));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[DownloadManager] Error downloading {item.Label}: {ex.Message}");
            Interlocked.Increment(ref _errorCount);
            OnPropertyChanged(nameof(ErrorCount));
        }

        UpdateProgress();
    }

    private void UpdateProgress()
    {
        var total = _totalFiles;
        if (total > 0)
            Progress = (double)(_completedFiles + _cachedFiles) / total;
    }

    private static string ComputeSha1(string filePath)
    {
        using var sha1 = SHA1.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha1.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

public class ScreenshotItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilePath { get; set; } = "";
    public Guid InstanceId { get; set; }
    public string InstanceName { get; set; } = "";
    public DateTime CreationDate { get; set; }
    public string FileSize { get; set; } = "";
    public string Resolution { get; set; } = "";
}

public class ScreenshotService : Helpers.ObservableBase
{
    private List<ScreenshotItem> _screenshots = new();
    public List<ScreenshotItem> Screenshots { get => _screenshots; set => SetProperty(ref _screenshots, value); }

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    private ScreenshotItem? _viewerSelectedItem;
    public ScreenshotItem? ViewerSelectedItem { get => _viewerSelectedItem; set => SetProperty(ref _viewerSelectedItem, value); }

    public async Task LoadScreenshots(List<Instance> instances, string instancesRoot)
    {
        IsLoading = true;

        var items = await Task.Run(() =>
        {
            var foundItems = new List<ScreenshotItem>();

            foreach (var instance in instances)
            {
                var screenshotsDir = Path.Combine(instancesRoot, instance.DirectoryName, ".minecraft", "screenshots");
                if (!Directory.Exists(screenshotsDir)) continue;

                foreach (var file in Directory.GetFiles(screenshotsDir, "*.png"))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        var sizeStr = GetFileSizeString(info.Length);
                        var resolution = GetImageResolution(file);

                        foundItems.Add(new ScreenshotItem
                        {
                            FilePath = file,
                            InstanceId = instance.Id,
                            InstanceName = instance.Name,
                            CreationDate = info.CreationTime,
                            FileSize = sizeStr,
                            Resolution = resolution
                        });
                    }
                    catch
                    {
                        // Ignore corrupted / locked files
                    }
                }
            }

            // Sort descending by date
            return foundItems.OrderByDescending(s => s.CreationDate).ToList();
        });

        Screenshots = items;
        IsLoading = false;
    }

    public void DeleteScreenshot(ScreenshotItem item)
    {
        try
        {
            if (File.Exists(item.FilePath))
            {
                File.Delete(item.FilePath);
            }
            var updated = new List<ScreenshotItem>(Screenshots);
            updated.RemoveAll(s => s.FilePath == item.FilePath);
            Screenshots = updated;

            if (ViewerSelectedItem?.FilePath == item.FilePath)
            {
                ViewerSelectedItem = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScreenshotService] Failed to delete screenshot: {ex}");
        }
    }

    private static string GetFileSizeString(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB" };
        int i;
        double dblBytes = bytes;
        for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            dblBytes = bytes / 1024.0;
        }
        return $"{dblBytes:F1} {suffix[i]}";
    }

    private static string GetImageResolution(string filePath)
    {
        try
        {
            // Lightweight resolution reader using standard metadata or file stream
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    reader.ReadBytes(8); // PNG signature
                    var ihdr = reader.ReadBytes(8); // Chunk length + IHDR type
                    if (EncodingToAscii(ihdr).Contains("IHDR"))
                    {
                        var wBytes = reader.ReadBytes(4);
                        var hBytes = reader.ReadBytes(4);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(wBytes);
                            Array.Reverse(hBytes);
                        }
                        var w = BitConverter.ToInt32(wBytes, 0);
                        var h = BitConverter.ToInt32(hBytes, 0);
                        return $"{w} × {h}";
                    }
                }
            }
        }
        catch { }
        return "Unknown";
    }

    private static string EncodingToAscii(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes);
    }
}

# TASK-009: DownloadManager

## Metadata
- **Phase**: 2 | **Dependencies**: TASK-006 | **LOC**: ~200
- **Source**: [DownloadManager.swift](../../onyx/Onyx/Services/DownloadManager.swift) — 160 рядків
- **Output**: `src/OnyxWindows/Services/DownloadManager.cs`

## Objective
Паралельний завантажувач файлів з SHA1 верифікацією. Max 32 concurrent downloads.

## Key Changes
- `CryptoKit.Insecure.SHA1` → `System.Security.Cryptography.SHA1`
- `URLSession` → `HttpClient`
- `TaskGroup` → `SemaphoreSlim(32)` + `Task.WhenAll`
- `InputStream` streaming SHA1 → `FileStream` + `IncrementalHash`

## Classes
```csharp
public record DownloadItem(Uri Url, string Destination, string? ExpectedSha1, int? ExpectedSize, string Label);

public class DownloadProgress : ObservableObject
{
    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private int _completedFiles;
    [ObservableProperty] private int _skippedFiles;
    [ObservableProperty] private int _failedFiles;
    [ObservableProperty] private string _currentFile = "";
    [ObservableProperty] private long _downloadedBytes;
    public double Fraction => TotalFiles > 0 ? (double)(CompletedFiles + SkippedFiles) / TotalFiles : 0;
    public bool IsComplete => (CompletedFiles + SkippedFiles + FailedFiles) >= TotalFiles;
}
```

## Acceptance Criteria
- [ ] 32 паралельні завантаження працюють
- [ ] SHA1 верифікація існуючих файлів (skip якщо OK)
- [ ] SHA1 верифікація завантажених файлів
- [ ] Cancellation підтримується
- [ ] Progress оновлюється на UI thread

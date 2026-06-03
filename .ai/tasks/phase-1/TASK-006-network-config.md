# TASK-006: NetworkConfig

## Metadata
- **Phase**: 1 | **Dependencies**: TASK-001 | **LOC**: ~30
- **Source**: [NetworkConfig.swift](../../onyx/Onyx/Services/NetworkConfig.swift) — 10 рядків
- **Output**: `src/OnyxWindows/Services/NetworkConfig.cs`

## Objective
Налаштувати HttpClient factory з правильними timeouts та User-Agent.

## Implementation
Реєстрація через `IHttpClientFactory` в App.xaml.cs (вже зроблено в TASK-001).
Цей файл — допоміжний клас для створення pre-configured HttpClient:

```csharp
public static class NetworkConfig
{
    public const int RequestTimeoutSeconds = 30;
    public const int ResourceTimeoutSeconds = 120;
    public const string UserAgent = "OnyxLauncher/1.0";
    public const int MaxConcurrentDownloads = 32;
}
```

## Acceptance Criteria
- [ ] HttpClient створюється з правильними timeouts
- [ ] User-Agent встановлений

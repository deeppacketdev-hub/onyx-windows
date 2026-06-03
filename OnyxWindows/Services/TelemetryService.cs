using System;
using System.Collections.Generic;

namespace OnyxWindows.Services;

/// <summary>
/// Service to handle secure and anonymous analytics.
/// Logs signals to the launcher log in Windows.
/// </summary>
public class TelemetryService : Helpers.ObservableBase
{
    private static TelemetryService? _shared;
    public static TelemetryService Shared => _shared ??= new TelemetryService();

    private bool _isInitialized;
    private const string AppId = "0A9B1085-83A0-473B-B950-A2DAB71B5A63";

    private TelemetryService() { }

    public void Initialize(bool enableTelemetry)
    {
        if (!enableTelemetry) return;
        if (_isInitialized) return;

        _isInitialized = true;
        Console.WriteLine($"[TelemetryService] ✅ Telemetry initialized successfully (Mock Mode, ID: {AppId}).");
    }

    public void SendSignal(string name, Dictionary<string, string>? parameters = null, bool enableTelemetry = true)
    {
        if (!enableTelemetry) return;

        if (!_isInitialized)
        {
            Initialize(true);
        }

        var paramStr = parameters != null ? string.Join(", ", parameters) : "None";
        Console.WriteLine($"[TelemetryService] 🚀 Sent signal '{name}' with params: {paramStr}");
    }
}

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OnyxWindows.Services;

public class DiscordRpcService : Helpers.ObservableBase
{
    private bool _isConnected;
    public bool IsConnected { get => _isConnected; set => SetProperty(ref _isConnected, value); }

    private NamedPipeClientStream? _pipeClient;
    private const string ClientId = "1506949090498318437"; // Onyx Launcher Discord App ID
    private int _nonce;

    public void SetActivity(string instanceName, string mcVersion, string? modLoader, DateTime? startTime = null)
    {
        Task.Run(async () =>
        {
            try
            {
                await ConnectAsync();

                if (_pipeClient == null || !_pipeClient.IsConnected) return;

                var finalStartTime = startTime ?? DateTime.UtcNow;
                var unixStart = (long)(finalStartTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

                var detailParts = new List<string> { $"Minecraft {mcVersion}" };
                if (!string.IsNullOrEmpty(modLoader))
                {
                    detailParts.Add(modLoader);
                }

                var activity = new Dictionary<string, object>
                {
                    ["state"] = $"Збірка: {instanceName}",
                    ["details"] = string.Join(" • ", detailParts),
                    ["timestamps"] = new Dictionary<string, object>
                    {
                        ["start"] = unixStart
                    },
                    ["assets"] = new Dictionary<string, object>
                    {
                        ["large_image"] = "onyx_logo",
                        ["large_text"] = "Onyx Launcher",
                        ["small_image"] = "minecraft_icon",
                        ["small_text"] = $"Minecraft {mcVersion}"
                    }
                };

                var payload = new Dictionary<string, object>
                {
                    ["cmd"] = "SET_ACTIVITY",
                    ["args"] = new Dictionary<string, object>
                    {
                        ["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id,
                        ["activity"] = activity
                    },
                    ["nonce"] = NextNonce()
                };

                await SendPayloadAsync(1, payload);
                IsConnected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DiscordRPC] Failed to set activity: {ex}");
            }
        });
    }

    public void ClearActivity()
    {
        Task.Run(async () =>
        {
            try
            {
                if (_pipeClient == null || !_pipeClient.IsConnected) return;

                var payload = new Dictionary<string, object>
                {
                    ["cmd"] = "SET_ACTIVITY",
                    ["args"] = new Dictionary<string, object>
                    {
                        ["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id
                    },
                    ["nonce"] = NextNonce()
                };

                await SendPayloadAsync(1, payload);
                Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DiscordRPC] Failed to clear activity: {ex}");
            }
        });
    }

    private async Task ConnectAsync()
    {
        if (_pipeClient != null && _pipeClient.IsConnected) return;

        // Try discord-ipc-0 through discord-ipc-9
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var pipeName = $"discord-ipc-{i}";
                var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                
                // Connect with 100ms timeout
                await client.ConnectAsync(100);

                _pipeClient = client;

                // Handshake
                var handshake = new Dictionary<string, object>
                {
                    ["v"] = 1,
                    ["client_id"] = ClientId
                };

                await SendPayloadAsync(0, handshake);

                // Read handshake response
                if (await ReadResponseAsync())
                {
                    Console.WriteLine($"[DiscordRPC] ✅ Connected via Named Pipe: {pipeName}");
                    return;
                }
                else
                {
                    Disconnect();
                }
            }
            catch
            {
                // Try next pipe
            }
        }

        Console.WriteLine("[DiscordRPC] Discord not found or not running");
    }

    private void Disconnect()
    {
        if (_pipeClient != null)
        {
            try { _pipeClient.Dispose(); } catch { }
            _pipeClient = null;
        }
        IsConnected = false;
        Console.WriteLine("[DiscordRPC] Disconnected");
    }

    private async Task SendPayloadAsync(uint opcode, object payload)
    {
        if (_pipeClient == null || !_pipeClient.IsConnected) return;

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        var header = new byte[8];
        BitConverter.TryWriteBytes(header.AsSpan(0, 4), opcode);
        BitConverter.TryWriteBytes(header.AsSpan(4, 4), (uint)bytes.Length);

        await _pipeClient.WriteAsync(header, 0, header.Length);
        await _pipeClient.WriteAsync(bytes, 0, bytes.Length);
        await _pipeClient.FlushAsync();
    }

    private async Task<bool> ReadResponseAsync()
    {
        if (_pipeClient == null || !_pipeClient.IsConnected) return false;

        var header = new byte[8];
        int bytesRead = await _pipeClient.ReadAsync(header, 0, 8);
        if (bytesRead != 8) return false;

        uint length = BitConverter.ToUInt32(header, 4);
        if (length == 0 || length > 65536) return false;

        var body = new byte[length];
        bytesRead = await _pipeClient.ReadAsync(body, 0, (int)length);
        return bytesRead == (int)length;
    }

    private string NextNonce()
    {
        _nonce++;
        return $"onyx-{_nonce}-{Guid.NewGuid().ToString().Substring(0, 8)}";
    }
}

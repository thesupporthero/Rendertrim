using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RenderTrim.Util;

/// <summary>
/// Cross-process broadcast for multibox. Each RenderTrim instance writes/watches a
/// shared file at %TEMP%\RenderTrim\broadcast.json. Sending writes the file with a
/// fresh timestamp; all watching instances (FFXIV processes on the same machine)
/// pick up the change and apply.
///
/// File-based was chosen over named pipes / TinyIpc to avoid bundling dependencies.
/// FileSystemWatcher fires reliably for cross-process writes on Windows.
/// </summary>
public sealed class BroadcastService : IDisposable
{
    public sealed class Payload
    {
        public DateTime Timestamp { get; set; }
        public int SenderPid { get; set; }
        public Dictionary<string, bool> EnabledTrims { get; set; } = new();
        public RenderSkipMode RenderSkipMode { get; set; }
    }

    private readonly string _path;
    private readonly FileSystemWatcher _watcher;
    private readonly Action<Payload> _onReceive;
    private DateTime _lastSeen = DateTime.MinValue;

    public BroadcastService(Action<Payload> onReceive)
    {
        _onReceive = onReceive;

        var dir = Path.Combine(Path.GetTempPath(), "RenderTrim");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "broadcast.json");

        _watcher = new FileSystemWatcher(dir, "broadcast.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnFsChanged;
        _watcher.Created += OnFsChanged;
    }

    public void Send(Dictionary<string, bool> enabledTrims, RenderSkipMode renderSkipMode)
    {
        var payload = new Payload
        {
            Timestamp = DateTime.UtcNow,
            SenderPid = Environment.ProcessId,
            EnabledTrims = new Dictionary<string, bool>(enabledTrims),
            RenderSkipMode = renderSkipMode,
        };
        try
        {
            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            File.WriteAllText(_path, json);
            DalamudApi.PluginLog.Info($"[Broadcast] sent to {_path} ({payload.EnabledTrims.Count} trims)");
        }
        catch (Exception ex)
        {
            DalamudApi.PluginLog.Error(ex, "[Broadcast] send failed");
        }
    }

    private void OnFsChanged(object _, FileSystemEventArgs e)
    {
        // FileSystemWatcher often fires multiple events for a single write.
        // Debounce on file's last-write timestamp.
        try
        {
            if (!File.Exists(e.FullPath)) return;
            var ts = File.GetLastWriteTimeUtc(e.FullPath);
            if (ts <= _lastSeen) return;
            _lastSeen = ts;

            string json;
            // The writer may still hold the file briefly; retry-read with a tiny wait.
            for (int i = 0; ; i++)
            {
                try { json = File.ReadAllText(e.FullPath); break; }
                catch (IOException) when (i < 5) { System.Threading.Thread.Sleep(20); }
            }
            var payload = JsonConvert.DeserializeObject<Payload>(json);
            if (payload is null) return;

            // Marshal to the framework thread — Apply/Revert touch hooks which must
            // happen on the game thread.
            DalamudApi.Framework.RunOnFrameworkThread(() =>
            {
                try { _onReceive(payload); }
                catch (Exception ex) { DalamudApi.PluginLog.Error(ex, "[Broadcast] handler failed"); }
            });
        }
        catch (Exception ex)
        {
            DalamudApi.PluginLog.Error(ex, "[Broadcast] receive failed");
        }
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFsChanged;
        _watcher.Created -= OnFsChanged;
        _watcher.Dispose();
    }
}

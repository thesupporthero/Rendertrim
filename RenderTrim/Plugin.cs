using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using RenderTrim.Commands;
using RenderTrim.Trims;
using RenderTrim.UI;
using RenderTrim.Util;

namespace RenderTrim;

public sealed class Plugin : IDalamudPlugin
{
    public static Configuration Config { get; private set; } = null!;

    private readonly TrimRegistry _registry;
    private readonly CommandHandler _commands;
    private readonly WindowSystem _windowSystem = new("RenderTrim");
    private readonly MainWindow _mainWindow;
    private readonly EmergencyBackoutWindow _emergencyWindow;
    private readonly DebugWindow _debugWindow;
    private readonly BroadcastService _broadcast;

    private bool _shuttingDown;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<DalamudApi>();

        Config = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _registry = new TrimRegistry();

        _broadcast = new BroadcastService(OnBroadcastReceived);

        _emergencyWindow = new EmergencyBackoutWindow(_registry, Config, OnEmergencyResolved);
        _mainWindow = new MainWindow(_registry, Config, BroadcastConfig, OnMainWindowClosed);
        _debugWindow = new DebugWindow(_registry, Config, BroadcastConfig);

        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_emergencyWindow);
        _windowSystem.AddWindow(_debugWindow);

        // Default open state: MainWindow visible always (it's the user-facing entry point);
        // DebugWindow only auto-opens in dev mode; EmergencyBackout stays closed until needed.
        _mainWindow.IsOpen = true;
        _debugWindow.IsOpen = DalamudApi.PluginInterface.IsDev || Config.ShowDebugWindowOnLoad;
        _emergencyWindow.IsOpen = false;

        _commands = new CommandHandler(_registry, Config, ToggleMainWindow, ToggleDebugWindow);

        DalamudApi.PluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        DalamudApi.PluginInterface.UiBuilder.OpenMainUi += ToggleMainWindow;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += ToggleMainWindow;

        if (Config.RestoreOnLoad)
            _registry.RestoreFromConfig(Config);

        DalamudApi.PluginLog.Info(
            $"RenderTrim loaded. dev={DalamudApi.PluginInterface.IsDev}  pid={System.Environment.ProcessId}  " +
            $"trims_resolved={CountResolved()}/{_registry.Trims.Count}");
    }

    private int CountResolved()
    {
        int n = 0;
        foreach (var t in _registry.Trims) if (t.IsResolved) n++;
        return n;
    }

    private void ToggleMainWindow() => _mainWindow.IsOpen = !_mainWindow.IsOpen;
    private void ToggleDebugWindow() => _debugWindow.IsOpen = !_debugWindow.IsOpen;

    private void OnMainWindowClosed()
    {
        if (_shuttingDown) return;
        // Only show the safety net if there's actually something to back out from.
        // Closing the main UI with no trims active is a normal "I'm done" action.
        if (!_registry.Trims.Any(t => t.IsApplied)) return;
        _emergencyWindow.IsOpen = true;
    }

    private void OnEmergencyResolved()
    {
        _emergencyWindow.IsOpen = false;
        _mainWindow.IsOpen = true;
    }

    private void BroadcastConfig()
    {
        _registry.PersistTo(Config);
        Config.Save();
        _broadcast.Send(Config.EnabledTrims, Config.RenderSkipMode);
        DalamudApi.ChatGui.Print("[RenderTrim] broadcast sent to peer clients");
    }

    private void OnBroadcastReceived(BroadcastService.Payload payload)
    {
        if (payload.SenderPid == System.Environment.ProcessId)
        {
            DalamudApi.PluginLog.Info("[Broadcast] received own broadcast, skipping apply");
            return;
        }

        DalamudApi.PluginLog.Info($"[Broadcast] received from pid={payload.SenderPid}, applying");
        Config.RenderSkipMode = payload.RenderSkipMode;

        foreach (var t in _registry.Trims)
        {
            var shouldBeOn = payload.EnabledTrims.TryGetValue(t.Id, out var v) && v;
            try
            {
                if (shouldBeOn && !t.IsApplied && t.IsResolved) t.Apply();
                else if (!shouldBeOn && t.IsApplied) t.Revert();
            }
            catch (System.Exception ex)
            {
                DalamudApi.PluginLog.Warning($"[Broadcast] {t.Id} sync failed: {ex.Message}");
            }
        }

        _registry.PersistTo(Config);
        Config.Save();
        DalamudApi.ChatGui.Print($"[RenderTrim] applied broadcast from pid {payload.SenderPid}");
    }

    public void Dispose()
    {
        _shuttingDown = true;
        DalamudApi.PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        DalamudApi.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainWindow;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainWindow;
        _windowSystem.RemoveAllWindows();

        _broadcast.Dispose();
        _commands.Dispose();
        _registry.Dispose();
    }
}

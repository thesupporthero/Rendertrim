using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RenderTrim.Trims;

namespace RenderTrim.UI;

/// <summary>
/// User-facing main window. Single checkbox + broadcast button. Configuring which
/// trims are in the "enabled" set is done via the debug window (/rendertrim debug).
/// Closing this window opens EmergencyBackoutWindow as a safety net so the user
/// can always revert from a broken-render state.
/// </summary>
public sealed class MainWindow : Window
{
    private readonly TrimRegistry _registry;
    private readonly Configuration _config;
    private readonly Action _broadcast;
    private readonly Action _onUserClosed;

    public MainWindow(TrimRegistry registry, Configuration config, Action broadcast, Action onUserClosed)
        : base("RenderTrim", ImGuiWindowFlags.AlwaysAutoResize)
    {
        _registry = registry;
        _config = config;
        _broadcast = broadcast;
        _onUserClosed = onUserClosed;
        Size = new Vector2(340, 0);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void OnClose()
    {
        // User closed the window via X (or programmatic IsOpen=false). Hand off
        // to the safety-net window so they can always recover.
        _onUserClosed?.Invoke();
    }

    public override void Draw()
    {
        var anyApplied = _registry.Trims.Any(t => t.IsApplied);
        var enabled = anyApplied;
        if (ImGui.Checkbox("Trims active", ref enabled))
        {
            if (enabled) ApplyAll();
            else RevertAll();
        }

        var resolvedCount = _registry.Trims.Count(t => t.IsResolved);
        var appliedCount = _registry.Trims.Count(t => t.IsApplied);
        ImGui.SameLine();
        ImGui.TextDisabled($"  ({appliedCount} of {resolvedCount} active)");

        ImGui.Separator();

        if (ImGui.Button("Broadcast to other clients", new Vector2(-1, 0)))
            _broadcast();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Sends the current trim state to every other RenderTrim instance on this machine.");

        ImGui.Spacing();
        ImGui.TextDisabled("Configure individual trims: /rendertrim debug");
    }

    private void ApplyAll()
    {
        foreach (var t in _registry.Trims)
            if (t.IsResolved && !t.IsApplied)
                t.Apply();
        _registry.PersistTo(_config);
        _config.Save();
    }

    private void RevertAll()
    {
        foreach (var t in _registry.Trims)
            if (t.IsApplied) t.Revert();
        _registry.PersistTo(_config);
        _config.Save();
    }
}

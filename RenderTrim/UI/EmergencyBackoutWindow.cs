using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RenderTrim.Trims;

namespace RenderTrim.UI;

/// <summary>
/// Safety-net window that opens when MainWindow is closed. Single button to
/// revert all currently-applied trims and reopen the main window. Prevents the
/// user from getting stuck in a broken-render state with no UI to fix it.
/// </summary>
public sealed class EmergencyBackoutWindow : Window
{
    private readonly TrimRegistry _registry;
    private readonly Configuration _config;
    private readonly Action _onResolved;

    public EmergencyBackoutWindow(TrimRegistry registry, Configuration config, Action onResolved)
        : base("RenderTrim — Emergency Backout",
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        _registry = registry;
        _config = config;
        _onResolved = onResolved;
        Size = new Vector2(380, 0);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var appliedCount = _registry.Trims.Count(t => t.IsApplied);
        ImGui.TextWrapped(
            "The RenderTrim main window was closed. Active trims may be suppressing " +
            $"rendering or holding system state. Currently applied: {appliedCount} trim(s).");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var btnColor = new Vector4(0.85f, 0.35f, 0.35f, 1.0f);
        ImGui.PushStyleColor(ImGuiCol.Button, btnColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.95f, 0.45f, 0.45f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.75f, 0.25f, 0.25f, 1.0f));
        if (ImGui.Button("Disable everything and reopen main UI", new Vector2(-1, 48)))
            DoBackout();
        ImGui.PopStyleColor(3);

        ImGui.Spacing();
        ImGui.TextDisabled("You can also reopen the main window with /rendertrim.");
    }

    private void DoBackout()
    {
        foreach (var t in _registry.Trims)
        {
            if (!t.IsApplied) continue;
            try { t.Revert(); }
            catch (Exception ex) { DalamudApi.PluginLog.Warning($"[Backout] {t.Id} revert failed: {ex.Message}"); }
        }
        _registry.PersistTo(_config);
        _config.Save();
        DalamudApi.ChatGui.Print("[RenderTrim] emergency backout complete");
        _onResolved();
    }
}

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RenderTrim.Trims;

namespace RenderTrim.UI;

public sealed class DebugWindow : Window
{
    private readonly TrimRegistry _registry;
    private readonly Configuration _config;
    private readonly System.Action _broadcast;

    public DebugWindow(TrimRegistry registry, Configuration config, System.Action broadcast)
        : base("RenderTrim — Debug",
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        _registry = registry;
        _config = config;
        _broadcast = broadcast;
        Size = new Vector2(560, 0);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        DrawHeader();
        ImGui.Separator();
        DrawCategory("Render-skip patches", TrimCategory.RenderSkip);
        DrawCategory("Update loops", TrimCategory.UpdateLoop);
        DrawCategory("Renderer passes", TrimCategory.RendererPass);
        DrawCategory("System tuning", TrimCategory.SystemTuning);
        ImGui.Separator();
        DrawBulkActions();
    }

    private void DrawHeader()
    {
        ImGui.TextWrapped(
            "Per-subsystem render trimming. Toggle individual trims to identify which combinations " +
            "yield CPU savings without breaking gameplay. Default: all OFF on plugin load.");

        var restore = _config.RestoreOnLoad;
        if (ImGui.Checkbox("Restore enabled trims on plugin load", ref restore))
        {
            _config.RestoreOnLoad = restore;
            _config.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(otherwise, all OFF after restart)");

        var modeIdx = (int)_config.RenderSkipMode;
        if (ImGui.Combo("RenderSkip mode", ref modeIdx, "BytePatch (BTB-compatible)\0DirectFieldWrite (no code mod)\0"))
        {
            _config.RenderSkipMode = (RenderSkipMode)modeIdx;
            _config.Save();
        }

        ImGui.TextDisabled(
            "Note: reverting trims may not fully restore mid-session state. The game's render\n" +
            "subsystems aren't designed to pause/resume — characters, VFX, etc. may stay frozen\n" +
            "until a zone transition or plugin reload.");
    }

    private void DrawCategory(string title, TrimCategory cat)
    {
        ImGui.TextColored(new Vector4(0.7f, 0.85f, 1.0f, 1.0f), title);
        ImGui.Indent();
        foreach (var t in _registry.ByCategory(cat))
            DrawTrimRow(t);
        ImGui.Unindent();
        ImGui.Spacing();
    }

    private void DrawTrimRow(TrimBase t)
    {
        ImGui.PushID(t.Id);
        var on = t.IsApplied;
        ImGui.BeginDisabled(!t.IsResolved);
        if (ImGui.Checkbox("##toggle", ref on))
        {
            try
            {
                if (on) t.Apply();
                else t.Revert();
                _registry.PersistTo(_config);
                _config.Save();
            }
            catch (System.Exception ex)
            {
                DalamudApi.PluginLog.Error(ex, $"Toggle {t.Id} failed");
                DalamudApi.ChatGui.PrintError($"[RenderTrim] {t.Id} toggle failed: {ex.Message}");
            }
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        DrawRiskBadge(t.Risk);
        ImGui.SameLine();
        ImGui.Text(t.Name);

        if (t.IsResolved)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($" @ 0x{t.ResolvedAddress:X}");
        }
        else
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.3f, 1.0f), $" UNRESOLVED: {t.FailureReason}");
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(t.Description);

        ImGui.PopID();
    }

    private static void DrawRiskBadge(TrimRisk risk)
    {
        var (label, color) = risk switch
        {
            TrimRisk.Safe             => ("SAFE",      new Vector4(0.4f, 0.9f, 0.4f, 1.0f)),
            TrimRisk.Untested         => ("UNTESTED",  new Vector4(0.9f, 0.85f, 0.4f, 1.0f)),
            TrimRisk.Risky            => ("RISKY",     new Vector4(0.95f, 0.6f, 0.3f, 1.0f)),
            TrimRisk.Unsafe           => ("UNSAFE",    new Vector4(1.0f, 0.3f, 0.3f, 1.0f)),
            TrimRisk.Tradeoff => ("TRADEOFF", new Vector4(0.85f, 0.5f, 1.0f, 1.0f)),
            _ => ("?", new Vector4(0.7f, 0.7f, 0.7f, 1.0f)),
        };
        ImGui.TextColored(color, $"[{label}]");
    }

    private void DrawBulkActions()
    {
        if (ImGui.Button("Apply: Safe-only set"))
        {
            // Safe-only never includes Tradeoff trims — those are situationally useful and
            // should be opted into individually based on whether you're CPU- or GPU-bound.
            foreach (var t in _registry.Trims)
                if (t.Risk == TrimRisk.Safe && t.IsResolved && !t.IsApplied) t.Apply();
            _registry.PersistTo(_config);
            _config.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Revert all"))
        {
            foreach (var t in _registry.Trims)
                if (t.IsApplied) t.Revert();
            _registry.PersistTo(_config);
            _config.Save();
        }

        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.7f, 0.85f, 1.0f, 1.0f), "Multibox");
        if (ImGui.Button("Broadcast current state to all clients"))
            _broadcast();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(
                "Sends the current trim selection + RenderSkip mode to every other RenderTrim\n" +
                "instance running on this machine via a shared file in %TEMP%\\RenderTrim. Each\n" +
                "peer applies the broadcast on its own framework thread.");
    }
}

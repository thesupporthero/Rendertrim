using System;
using System.Collections.Generic;
using System.Linq;

namespace RenderTrim.Trims;

public sealed class TrimRegistry : IDisposable
{
    public IReadOnlyList<TrimBase> Trims { get; }

    public TrimRegistry()
    {
        Trims = new List<TrimBase>
        {
            // === RenderSkip family (byte patch / direct field write) ===
            new RenderSkipTrim(),
            new RenderSkipPostEffectTrim(),

            // === Update loops ===
            new CharaAnimationsTrim(),

            // === Renderer passes ===
            new ModelRendererTrim(),
            new RenderHumanTrim(),
            new RenderCharaBaseTrim(),
            new RenderCharaBaseMatTrim(),
            new RenderVfxObjectTrim(),
            new RenderTerrainTrim(),
            new RenderWaterTrim(),
            new RenderLightsTrim(),
            new GeometryRendererTrim(),
            new CameraSetMatricesTrim(),

            // === System tuning (Windows-side, not FFXIV hooks) ===
            new WorkingSetTrim(),
        };

        foreach (var t in Trims)
        {
            DalamudApi.PluginLog.Info(
                t.IsResolved
                    ? $"[Registry] {t.Id} resolved @ 0x{t.ResolvedAddress:X}"
                    : $"[Registry] {t.Id} unresolved: {t.FailureReason}");
        }
    }

    public TrimBase? Find(string id) =>
        Trims.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<TrimBase> ByCategory(TrimCategory cat) =>
        Trims.Where(t => t.Category == cat);

    public void RestoreFromConfig(Configuration cfg)
    {
        foreach (var t in Trims)
        {
            if (cfg.EnabledTrims.TryGetValue(t.Id, out var on) && on && t.IsResolved)
                t.Apply();
        }
    }

    public void PersistTo(Configuration cfg)
    {
        cfg.EnabledTrims.Clear();
        foreach (var t in Trims)
            cfg.EnabledTrims[t.Id] = t.IsApplied;
    }

    public void Dispose()
    {
        foreach (var t in Trims)
        {
            try { t.Dispose(); }
            catch (Exception ex) { DalamudApi.PluginLog.Warning($"[Registry] {t.Id} dispose failed: {ex.Message}"); }
        }
    }
}

using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using RenderTrim.Util;

namespace RenderTrim.Trims;

/// <summary>
/// Suppresses the main render view dispatch.
///
/// Two modes:
///   BytePatch          — patches imm8 of the `cmp dword [r13+0x3834C], -1` site at 0x1402B9F7D
///                        from -1 to 1, causing the following jne to fire on every frame.
///                        Same mechanism as BardToolbox / MasterOfPuppets.
///   DirectFieldWrite   — writes 0 to Render::Manager+0x3834C every frame via IFramework.Update.
///                        No code modification; survives ASLR/patches better but races with the
///                        game's own writes to the field — may be less reliable.
/// </summary>
public sealed class RenderSkipTrim : TrimBase
{
    public override string Id => "render-skip";
    public override string Name => "Render Skip (main view)";
    public override string Description =>
        "Suppresses the render dispatch's main-view check. Patches cmp imm8 at +0x3834C site, " +
        "or writes the field directly. Same effect as BardToolbox derender.";
    public override TrimCategory Category => TrimCategory.RenderSkip;
    public override TrimRisk Risk => TrimRisk.Safe;

    private const string Sig = "41 83 BD ?? ?? ?? ?? ?? 0F 85 ?? ?? ?? ?? 48 89 AC 24";
    private const int FieldOffset = 0x3834C;

    private MemoryReplacement? _bytePatch;
    private bool _fieldWriteHooked;

    protected override void Resolve()
    {
        if (DalamudApi.SigScanner.TryScanText(Sig, out var addr))
            ResolvedAddress = addr;
    }

    public override void Apply()
    {
        if (!IsResolved || IsApplied) return;
        var mode = Plugin.Config.RenderSkipMode;
        if (mode == RenderSkipMode.BytePatch)
        {
            _bytePatch = new MemoryReplacement(ResolvedAddress + 7, new byte[] { 0x1 });
            _bytePatch.Enable();
        }
        else
        {
            DalamudApi.Framework.Update += OnFrameworkUpdate;
            _fieldWriteHooked = true;
        }
        IsApplied = true;
        DalamudApi.PluginLog.Info($"[Trim:{Id}] applied via {mode} at 0x{ResolvedAddress:X}");
    }

    public override void Revert()
    {
        _bytePatch?.Disable();
        _bytePatch = null;
        if (_fieldWriteHooked)
        {
            DalamudApi.Framework.Update -= OnFrameworkUpdate;
            _fieldWriteHooked = false;
            // Write -1 (the "render this view" sentinel) so the dispatch's cmp
            // succeeds and rendering resumes. Without this the field stays at 0
            // and ALL FFXIV rendering remains suppressed (world AND UI/HUD).
            RestoreFieldSentinel();
        }
        IsApplied = false;
    }

    private static unsafe void OnFrameworkUpdate(IFramework _)
    {
        var mgr = Manager.Instance();
        if (mgr is null) return;
        *(int*)((nint)mgr + FieldOffset) = 0;
    }

    private static unsafe void RestoreFieldSentinel()
    {
        var mgr = Manager.Instance();
        if (mgr is null) return;
        *(int*)((nint)mgr + FieldOffset) = -1;
    }

    public override void Dispose()
    {
        // Always try the sentinel restore on dispose, regardless of whether
        // _fieldWriteHooked tracking is intact (e.g. plugin unload during a
        // partially-applied state).
        try { RestoreFieldSentinel(); } catch { /* ignore — best effort */ }
        Revert();
        _bytePatch?.Dispose();
    }
}

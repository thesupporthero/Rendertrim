using RenderTrim.Util;

namespace RenderTrim.Trims;

/// <summary>
/// Suppresses the post-effect pass within the render dispatch.
/// Same field as RenderSkipTrim (Render::Manager + 0x3834C), second cmp site at 0x1402BA5D4.
/// Byte-patch only — direct field write of the parent site already covers this one.
/// </summary>
public sealed class RenderSkipPostEffectTrim : TrimBase
{
    public override string Id => "render-skip-post";
    public override string Name => "Render Skip (post effect)";
    public override string Description =>
        "Companion patch to render-skip. Suppresses the second cmp site (post-effect pass) " +
        "in the same render dispatch function.";
    public override TrimCategory Category => TrimCategory.RenderSkip;
    public override TrimRisk Risk => TrimRisk.Safe;

    private const string Sig = "41 83 BD ?? ?? ?? ?? ?? 75 ?? 48 8B 0D";

    private MemoryReplacement? _bytePatch;

    protected override void Resolve()
    {
        if (DalamudApi.SigScanner.TryScanText(Sig, out var addr))
            ResolvedAddress = addr;
    }

    public override void Apply()
    {
        if (!IsResolved || IsApplied) return;
        _bytePatch = new MemoryReplacement(ResolvedAddress + 7, new byte[] { 0x1 });
        _bytePatch.Enable();
        IsApplied = true;
        DalamudApi.PluginLog.Info($"[Trim:{Id}] applied at 0x{ResolvedAddress:X}");
    }

    public override void Revert()
    {
        _bytePatch?.Disable();
        _bytePatch = null;
        IsApplied = false;
    }

    public override void Dispose()
    {
        Revert();
        _bytePatch?.Dispose();
    }
}

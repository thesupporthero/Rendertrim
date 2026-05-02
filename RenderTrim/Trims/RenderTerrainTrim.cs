namespace RenderTrim.Trims;

/// <summary>
/// No-ops terrain render at sub_1402C2ED0.
/// Sig disambiguated from sub_1402937B0 (vtable-only / never directly called) via tighter prologue match —
/// stack-frame size 0x30 and rbx-save offset 0x18 differ between the two candidates.
/// </summary>
public sealed class RenderTerrainTrim : NoOpHookTrim<RenderTerrainTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr);

    public override string Id => "render-terrain";
    public override string Name => "Render Terrain";
    public override string Description => "Skips terrain render dispatch (sub_1402C2ED0). Empirically verified safe under stationary use.";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Safe;

    // Tightened from zunetrix's broad sig (`48 89 5C 24 ?? 57 48 83 EC ?? ...`) — fixing
    // the stack offsets pins it to sub_1402C2ED0, the live render-call path.
    protected override string Signature =>
        "48 89 5C 24 18 57 48 83 EC 30 65 48 8B 04 25 ?? ?? ?? ?? 48 8B F9 8B 15 ?? ?? ?? ?? 48 8B 1C ?? B8 ?? ?? ?? ?? 80 3C ?? ?? 75 ?? E8";

    private static void NoOp(nint _) { }
    protected override Delegate BuildDetour() => NoOp;
}

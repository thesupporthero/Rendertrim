namespace RenderTrim.Trims;

/// <summary>
/// No-ops water render at sub_1402991E0.
/// </summary>
public sealed class RenderWaterTrim : NoOpHookTrim<RenderWaterTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr);

    public override string Id => "render-water";
    public override string Name => "Render Water";
    public override string Description => "Skips water render passes (sub_1402991E0). Empirically verified safe under stationary use.";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Safe;

    protected override string Signature =>
        "4C 8B DC 55 57 49 8D AB ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 80 B9 ?? ?? ?? ?? ?? 48 8B F9 0F 84";

    private static void NoOp(nint _) { }
    protected override Delegate BuildDetour() => NoOp;
}

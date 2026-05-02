namespace RenderTrim.Trims;

/// <summary>
/// No-ops dynamic lighting render at sub_14026AFF0.
/// 3-arg signature: (thisPtr, secondPtr, float).
/// </summary>
public sealed class RenderLightsTrim : NoOpHookTrim<RenderLightsTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr, nint a2, float a3);

    public override string Id => "render-lights";
    public override string Name => "Render Lights";
    public override string Description => "Skips dynamic lighting render (sub_14026AFF0). Empirically verified safe under stationary use.";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Safe;

    protected override string Signature =>
        "40 53 48 83 EC ?? 48 8B 05 ?? ?? ?? ?? 48 8B D9 F3 0F 10 05";

    private static void NoOp(nint _, nint _2, float _3) { }
    protected override Delegate BuildDetour() => NoOp;
}

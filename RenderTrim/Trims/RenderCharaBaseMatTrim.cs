namespace RenderTrim.Trims;

/// <summary>
/// No-ops character material render at sub_14043BE80.
/// </summary>
public sealed class RenderCharaBaseMatTrim : NoOpHookTrim<RenderCharaBaseMatTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr);

    public override string Id => "render-chara-base-mat";
    public override string Name => "Render CharaBase Material";
    public override string Description => "Skips character material render (sub_14043BE80). Empirically verified safe under stationary use.";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Safe;

    protected override string Signature =>
        "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC ?? 4C 89 7C 24";

    private static void NoOp(nint _) { }
    protected override Delegate BuildDetour() => NoOp;
}

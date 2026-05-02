namespace RenderTrim.Trims;

/// <summary>
/// No-ops base character render entry at sub_140433320.
/// </summary>
public sealed class RenderCharaBaseTrim : NoOpHookTrim<RenderCharaBaseTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr);

    public override string Id => "render-chara-base";
    public override string Name => "Render CharaBase";
    public override string Description => "Skips base character render entry (sub_140433320). Empirically verified safe under stationary use.";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Safe;

    protected override string Signature =>
        "48 89 5C 24 ?? 57 48 83 EC ?? 33 FF 48 8B D9 89 B9 ?? ?? ?? ?? 40 88 B9";

    private static void NoOp(nint _) { }
    protected override Delegate BuildDetour() => NoOp;
}

namespace RenderTrim.Trims;

/// <summary>
/// No-ops humanoid character render entry at sub_140441BC0.
/// </summary>
public sealed class RenderHumanTrim : NoOpHookTrim<RenderHumanTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr);

    public override string Id => "render-human";
    public override string Name => "Render Human";
    public override string Description =>
        "Wrapper around sub_140433320 (RenderCharaBase) with Human-specific setup. " +
        "TRADEOFF: saves CPU but adds ~3-4% GPU when enabled in isolation — the no-op " +
        "skips state setup and downstream renderers fall back to a slower GPU path. " +
        "Worth enabling on multibox / CPU-bound clients; skip on single GPU-bound clients.";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Tradeoff;

    protected override string Signature =>
        "40 53 48 83 EC ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 83 BB";

    private static void NoOp(nint _) { }
    protected override Delegate BuildDetour() => NoOp;
}

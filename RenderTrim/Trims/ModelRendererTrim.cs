namespace RenderTrim.Trims;

/// <summary>
/// No-ops the ModelRenderer entry at sub_140281A30.
/// Standard render-pass function. Untested in production; default OFF.
/// </summary>
public sealed class ModelRendererTrim : NoOpHookTrim<ModelRendererTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr);

    public override string Id => "model-renderer";
    public override string Name => "Model Renderer";
    public override string Description => "Skips the ModelRenderer dispatch (sub_140281A30). Empirically verified safe under stationary use; per-model render submission.";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Safe;

    protected override string Signature =>
        "40 53 48 83 EC ?? 65 48 8B 04 25 ?? ?? ?? ?? 48 8B D9 8B 15 ?? ?? ?? ?? 48 89 74 24";

    private static void NoOp(nint _) { }
    protected override Delegate BuildDetour() => NoOp;
}

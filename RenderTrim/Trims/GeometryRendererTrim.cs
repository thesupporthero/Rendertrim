namespace RenderTrim.Trims;

/// <summary>
/// No-ops geometry pass at sub_1402864C0.
/// </summary>
public sealed class GeometryRendererTrim : NoOpHookTrim<GeometryRendererTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr);

    public override string Id => "geometry-renderer";
    public override string Name => "Geometry Renderer";
    public override string Description => "Skips the geometry render pass (sub_1402864C0). Empirically verified safe under stationary use.";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Safe;

    protected override string Signature =>
        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 33 DB 4C 8D 99";

    private static void NoOp(nint _) { }
    protected override Delegate BuildDetour() => NoOp;
}

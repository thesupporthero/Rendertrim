namespace RenderTrim.Trims;

/// <summary>
/// Audited safe — no-ops VFX object update at sub_14045C7E0.
/// Reads [rdi+0x60..0x78] (position/scale), writes derived AABB to [rdi+0x2C0..0x334],
/// transitions internal state byte at [rdi+0x89]. All effects confined to the VFX
/// object's own memory; no global writes, no callbacks, no IPC. Animation timers driven
/// by VFX completion live in the VFX manager update path, not here.
/// </summary>
public sealed class RenderVfxObjectTrim : NoOpHookTrim<RenderVfxObjectTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr);

    public override string Id => "render-vfx-object";
    public override string Name => "Render VFX Object";
    public override string Description =>
        "Skips per-VFX-object transform/AABB cache update (sub_14045C7E0). Audited safe.";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Safe;

    protected override string Signature =>
        "48 89 7C 24 ?? 55 48 8B EC 48 83 EC ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 0F B6 41";

    private static void NoOp(nint _) { }
    protected override Delegate BuildDetour() => NoOp;
}

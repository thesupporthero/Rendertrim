namespace RenderTrim.Trims;

/// <summary>
/// No-ops the camera SetMatrices function at sub_140260E40 (entry-direct sig).
///
/// History: previous version of this trim resolved via a call-site sig
/// `E8 ?? ?? ?? ?? 0F 10 43 ?? C6 83` at 0x14025F04B and decoded the rel32. That crashed
/// in production with AccessViolation inside HookFromAddress because another loaded
/// plugin had already rewritten the call-site to point at its own trampoline (so the
/// rel32 we decoded pointed into trampoline memory that wasn't safe to install over).
/// Switching to a direct-entry sig avoids the call-site rewriting failure mode entirely.
/// </summary>
public sealed class CameraSetMatricesTrim : NoOpHookTrim<CameraSetMatricesTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr, nint a2);

    public override string Id => "camera-matrices";
    public override string Name => "Camera Set Matrices";
    public override string Description =>
        "Skips camera matrix update (sub_140260E40). May break camera-derived calcs " +
        "(target visibility, distance/cone checks).";
    public override TrimCategory Category => TrimCategory.RendererPass;
    public override TrimRisk Risk => TrimRisk.Risky;

    protected override string Signature =>
        "48 89 5C 24 10 57 48 81 EC E0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 D0 00 00 00 0F 28 02 48 8B D9 0F 11 41 10";

    private static void NoOp(nint _, nint _2) { }
    protected override Delegate BuildDetour() => NoOp;
}

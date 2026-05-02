namespace RenderTrim.Trims;

/// <summary>
/// Audited safe — no-ops the per-frame skeleton/animation update at sub_140AF94D0.
/// Function is a 100-iteration loop calling sub_14090BF00 on each non-null entry of
/// [thisPtr+0x50 + i*8]. Inner is purely object-internal (writes only [rbx+disp]),
/// no global state or callbacks. Outer has no post-loop work.
/// Skipping this stops all skeleton math per frame — biggest single CPU win.
/// </summary>
public sealed class CharaAnimationsTrim : NoOpHookTrim<CharaAnimationsTrim.Delegate>
{
    public delegate void Delegate(nint thisPtr);

    public override string Id => "chara-animations";
    public override string Name => "Chara Animations Update";
    public override string Description =>
        "Skips per-frame skeleton/animation update (100-iteration loop). Audited safe; " +
        "characters won't visually animate but no game-logic state diverges. Highest CPU win.";
    public override TrimCategory Category => TrimCategory.UpdateLoop;
    public override TrimRisk Risk => TrimRisk.Safe;

    protected override string Signature =>
        "48 89 5C 24 ?? 57 48 83 EC ?? 48 8D 59 ?? BF ?? ?? ?? ?? 48 8B 0B 48 85 C9 74 ?? E8";

    private static void NoOp(nint _) { }
    protected override Delegate BuildDetour() => NoOp;
}

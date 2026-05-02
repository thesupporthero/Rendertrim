using System;

namespace RenderTrim.Trims;

public abstract class TrimBase : IDisposable
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract TrimCategory Category { get; }

    public virtual TrimRisk Risk => TrimRisk.Untested;

    public bool IsResolved { get; protected set; }
    public bool IsApplied { get; protected set; }
    public string? FailureReason { get; protected set; }
    public nint ResolvedAddress { get; protected set; }

    protected TrimBase()
    {
        try
        {
            Resolve();
            IsResolved = ResolvedAddress != nint.Zero;
            if (!IsResolved && FailureReason is null)
                FailureReason = "Sig resolved to null address";
        }
        catch (Exception ex)
        {
            IsResolved = false;
            FailureReason = ex.Message;
            DalamudApi.PluginLog.Warning($"[Trim:{Id}] resolve failed: {ex.Message}");
        }
    }

    protected abstract void Resolve();

    public abstract void Apply();
    public abstract void Revert();

    public void Toggle()
    {
        if (IsApplied) Revert();
        else Apply();
    }

    public virtual void Dispose() => Revert();
}

public enum TrimCategory
{
    RenderSkip,
    RendererPass,
    UpdateLoop,
    SystemTuning,
}

public enum TrimRisk
{
    Safe,
    Untested,
    Risky,
    Unsafe,
    /// <summary>
    /// Hook installs cleanly but trades one resource for another — typically saves CPU
    /// while *increasing* GPU load (or vice versa). Common for wrapper functions whose
    /// no-op skips state setup and pushes work onto a fallback path.
    /// Whether to enable depends on which resource is actually constrained for your use
    /// case: multibox CPU-bound users may want it on; single-client GPU-bound users off.
    /// </summary>
    Tradeoff,
}

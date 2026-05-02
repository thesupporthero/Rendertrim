using System;
using Dalamud.Hooking;
using RenderTrim.Util;

namespace RenderTrim.Trims;

public abstract class NoOpHookTrim<T> : TrimBase where T : Delegate
{
    private Hook<T>? _hook;
    private T? _detour;

    protected abstract string Signature { get; }
    protected abstract T BuildDetour();

    protected override void Resolve()
    {
        if (!DalamudApi.SigScanner.TryScanText(Signature, out var addr))
            return;
        if (!AddressValidator.IsInsideMainModule(addr))
        {
            FailureReason = $"sig matched 0x{addr:X} but address is outside ffxiv_dx11.exe " +
                            "(likely matched a hook trampoline planted by another plugin)";
            return;
        }
        ResolvedAddress = addr;
    }

    public override void Apply()
    {
        if (!IsResolved || IsApplied) return;
        try
        {
            if (_hook is null)
            {
                _detour = BuildDetour();
                _hook = DalamudApi.GameInteropProvider.HookFromAddress<T>(ResolvedAddress, _detour);
            }
            _hook.Enable();
            IsApplied = true;
            DalamudApi.PluginLog.Info($"[Trim:{Id}] applied at 0x{ResolvedAddress:X}");
        }
        catch (Exception ex)
        {
            DalamudApi.PluginLog.Error(ex, $"[Trim:{Id}] hook installation failed");
            FailureReason = $"hook install failed: {ex.Message}";
            IsApplied = false;
        }
    }

    public override void Revert()
    {
        if (_hook is null || !IsApplied) return;
        _hook.Disable();
        IsApplied = false;
        DalamudApi.PluginLog.Info($"[Trim:{Id}] reverted");
    }

    public override void Dispose()
    {
        _hook?.Dispose();
        _hook = null;
        IsApplied = false;
    }
}

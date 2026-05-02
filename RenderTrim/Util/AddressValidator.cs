using System;
using System.Diagnostics;

namespace RenderTrim.Util;

/// <summary>
/// Sanity-checks resolved addresses before they reach Reloaded.Hooks. A bogus address
/// (e.g. a sig that matched a trampoline rewritten by another plugin) crashes the game
/// with an uncatchable AccessViolationException inside HookFromAddress's FollowJmp read.
/// Rejecting unresolved-looking addresses up front is the only reliable guard.
/// </summary>
internal static class AddressValidator
{
    private static readonly Lazy<(nint Lo, nint Hi)> MainModuleRange = new(GetMainModuleRange);

    public static bool IsInsideMainModule(nint addr)
    {
        var (lo, hi) = MainModuleRange.Value;
        return addr >= lo && addr < hi;
    }

    private static (nint Lo, nint Hi) GetMainModuleRange()
    {
        try
        {
            var mod = Process.GetCurrentProcess().MainModule;
            if (mod is null) return (nint.Zero, nint.Zero);
            var lo = mod.BaseAddress;
            var hi = lo + mod.ModuleMemorySize;
            DalamudApi.PluginLog.Info($"[AddressValidator] main module: 0x{lo:X} - 0x{hi:X}");
            return (lo, hi);
        }
        catch (Exception ex)
        {
            DalamudApi.PluginLog.Warning($"[AddressValidator] could not determine main module range: {ex.Message}");
            return (nint.Zero, nint.Zero);
        }
    }
}

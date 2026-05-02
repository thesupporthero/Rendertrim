using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace RenderTrim.Trims;

/// <summary>
/// Periodically calls EmptyWorkingSet on the FFXIV process to push idle pages out
/// of the active working set. Doesn't free physical RAM directly — Windows moves
/// the pages to the standby list, where other processes can reclaim them.
///
/// For AFK / background multibox clients this is a near-free win: the pages don't
/// get touched anyway, and Task Manager footprint drops dramatically (often 50-70%).
/// When the client is interacted with again, pages fault back in (soft fault if
/// still on standby, hard from pagefile if not).
///
/// Avoid enabling this on your active/foreground client — the periodic trim can
/// cause perceptible hitches if pages get touched immediately after eviction.
/// Recommended: enable on background instances only via the per-client config,
/// and broadcast a separate "background-mode" trim set to those instances.
/// </summary>
public sealed class WorkingSetTrim : TrimBase
{
    public override string Id => "working-set-trim";
    public override string Name => "Working Set Trim";
    public override string Description =>
        "Periodically calls EmptyWorkingSet to push idle pages out of the FFXIV " +
        "process's active working set. Reduces Task Manager RAM footprint dramatically; " +
        "physical RAM relief depends on whether other processes need the freed pages. " +
        "Enable on background/AFK clients only — periodic trim can cause hitches on the " +
        "foreground client when pages fault back in.";
    public override TrimCategory Category => TrimCategory.SystemTuning;
    public override TrimRisk Risk => TrimRisk.Tradeoff;

    private const int IntervalMs = 60_000;

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(nint hProcess);

    [DllImport("kernel32.dll")]
    private static extern nint GetCurrentProcess();

    private Timer? _timer;

    protected override void Resolve()
    {
        // No sigscan needed — this trim doesn't hook into FFXIV. We use a sentinel
        // non-zero address so the validator/IsResolved checks pass.
        ResolvedAddress = (nint)1;
    }

    public override void Apply()
    {
        if (IsApplied) return;
        // Trim immediately, then on a 60s tick.
        _timer = new Timer(_ => Trim(), null, 0, IntervalMs);
        IsApplied = true;
        DalamudApi.PluginLog.Info($"[Trim:{Id}] active, trimming every {IntervalMs / 1000}s");
    }

    public override void Revert()
    {
        _timer?.Dispose();
        _timer = null;
        IsApplied = false;
        DalamudApi.PluginLog.Info($"[Trim:{Id}] reverted");
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
        IsApplied = false;
    }

    private static void Trim()
    {
        try
        {
            if (!EmptyWorkingSet(GetCurrentProcess()))
                DalamudApi.PluginLog.Debug($"[WorkingSet] EmptyWorkingSet failed, win32err={Marshal.GetLastWin32Error()}");
        }
        catch (Exception ex)
        {
            DalamudApi.PluginLog.Warning($"[WorkingSet] trim threw: {ex.Message}");
        }
    }
}

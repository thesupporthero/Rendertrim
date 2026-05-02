using System;
using Dalamud.Game.Command;
using RenderTrim.Trims;

namespace RenderTrim.Commands;

public sealed class CommandHandler : IDisposable
{
    private const string PrimaryCommand = "/rendertrim";
    private const string AliasCommand = "/rt";

    private readonly TrimRegistry _registry;
    private readonly Configuration _config;
    private readonly Action _toggleMainWindow;
    private readonly Action _toggleDebugWindow;

    public CommandHandler(TrimRegistry registry, Configuration config,
        Action toggleMainWindow, Action toggleDebugWindow)
    {
        _registry = registry;
        _config = config;
        _toggleMainWindow = toggleMainWindow;
        _toggleDebugWindow = toggleDebugWindow;

        DalamudApi.CommandManager.AddHandler(PrimaryCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "RenderTrim controls. Use /rendertrim help for details.",
            ShowInHelp = true,
        });
        DalamudApi.CommandManager.AddHandler(AliasCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "Short alias for /rendertrim.",
            ShowInHelp = true,
        });
    }

    private void OnCommand(string cmd, string args)
    {
        var argv = args.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (argv.Length == 0) { _toggleMainWindow(); return; }

        switch (argv[0].ToLowerInvariant())
        {
            case "help":
                PrintHelp();
                break;
            case "list":
                ListTrims();
                break;
            case "on":
                Bulk(true, argv.Length > 1 ? argv[1] : null);
                break;
            case "off":
                Bulk(false, argv.Length > 1 ? argv[1] : null);
                break;
            case "toggle":
                if (argv.Length < 2) { DalamudApi.ChatGui.Print("Usage: /rendertrim toggle <id>"); break; }
                ToggleSingle(argv[1]);
                break;
            case "debug":
            case "dev":
                _toggleDebugWindow();
                break;
            case "ui":
            case "window":
            case "main":
                _toggleMainWindow();
                break;
            default:
                ToggleSingle(argv[0]);
                break;
        }
    }

    private void PrintHelp()
    {
        var c = DalamudApi.ChatGui;
        c.Print("RenderTrim commands:");
        c.Print("  /rendertrim                     — toggle main window");
        c.Print("  /rendertrim debug               — toggle debug/dev window");
        c.Print("  /rendertrim list                — list all trims and state");
        c.Print("  /rendertrim toggle <id>         — toggle a specific trim");
        c.Print("  /rendertrim <id>                — same as toggle");
        c.Print("  /rendertrim on [safe|all]       — apply safe-only or all resolved trims");
        c.Print("  /rendertrim off                 — revert all applied trims");
    }

    private void ListTrims()
    {
        var c = DalamudApi.ChatGui;
        foreach (var t in _registry.Trims)
        {
            var state = !t.IsResolved ? "UNRESOLVED" : (t.IsApplied ? "ON" : "off");
            c.Print($"  [{t.Risk,-13}] {t.Id,-24} {state,-10}  {t.Name}");
        }
    }

    private void ToggleSingle(string id)
    {
        var t = _registry.Find(id);
        if (t is null) { DalamudApi.ChatGui.PrintError($"Unknown trim: {id}"); return; }
        if (!t.IsResolved) { DalamudApi.ChatGui.PrintError($"{id} unresolved: {t.FailureReason}"); return; }
        try
        {
            t.Toggle();
            _registry.PersistTo(_config);
            _config.Save();
            DalamudApi.ChatGui.Print($"[RenderTrim] {t.Id} → {(t.IsApplied ? "ON" : "off")}");
        }
        catch (Exception ex)
        {
            DalamudApi.ChatGui.PrintError($"[RenderTrim] {id} toggle failed: {ex.Message}");
        }
    }

    private void Bulk(bool apply, string? selector)
    {
        var safeOnly = string.Equals(selector, "safe", StringComparison.OrdinalIgnoreCase);
        foreach (var t in _registry.Trims)
        {
            if (!t.IsResolved) continue;
            if (apply && safeOnly && t.Risk != TrimRisk.Safe) continue;
            try
            {
                if (apply && !t.IsApplied) t.Apply();
                else if (!apply && t.IsApplied) t.Revert();
            }
            catch (Exception ex)
            {
                DalamudApi.PluginLog.Warning($"Bulk {(apply ? "apply" : "revert")} {t.Id} failed: {ex.Message}");
            }
        }
        _registry.PersistTo(_config);
        _config.Save();
        DalamudApi.ChatGui.Print($"[RenderTrim] bulk {(apply ? "apply" : "revert")} complete");
    }

    public void Dispose()
    {
        DalamudApi.CommandManager.RemoveHandler(PrimaryCommand);
        DalamudApi.CommandManager.RemoveHandler(AliasCommand);
    }
}

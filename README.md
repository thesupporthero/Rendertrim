# RenderTrim

Per-subsystem render hook trimming for FFXIV. Disables individual rendering
subsystems (animations, models, vfx, water, lights, etc.) to reduce CPU and GPU
load on idle multibox clients beyond what a basic render-skip plugin achieves.
Includes a working-set RAM trim and a multibox broadcast feature.

Built for the AFK / stationary-multibox use case (e.g. bard ensembles, parked
alts). Not intended for active gameplay.

## Install

In Dalamud:

1. `/xlsettings` → **Experimental** tab.
2. Add to **Custom Plugin Repositories**:

   ```
   https://raw.githubusercontent.com/thesupporthero/Rendertrim/main/pluginmaster.json
   ```

3. Save and close. `/xlplugins` → search **RenderTrim** → install.

## Usage

- `/rendertrim` — main window (master toggle + broadcast button).
- `/rt` — short alias.
- `/rendertrim debug` — per-trim power-user grid.
- `/rendertrim help` — full command list.

Tick **Trims active** to apply all 13 trims; untick to revert. Click
**Broadcast to other clients** to push the same state to every other RenderTrim
instance running on the machine.

If you accidentally close the main window while trims are active, an
**Emergency Backout** window appears with a single button to disable everything.

## Building from source

```powershell
dotnet build RenderTrim/RenderTrim.csproj -c Release
.\deploy.ps1                    # copy to %AppData%\XIVLauncher\devPlugins\RenderTrim
```

## Releasing

```powershell
.\package.ps1                   # build + zip + regenerate pluginmaster.json
git add . && git commit -m 'release vX.Y.Z' && git push
```

## Compatibility

- Targets Dalamud API 15 (FFXIV 7.5+).
- Tested on FFXIV `ffxiv_dx11.exe` timestamped 2026-04-20. Sigs may drift on
  patch days; rebuild after re-verifying signatures against the new binary.

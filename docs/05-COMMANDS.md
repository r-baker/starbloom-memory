# Common Commands & Workflows

Quick reference so routine work can be done without a Claude Code
session — useful when daily token budget is spent.

**Claude Code: keep this file updated.** Whenever you use a command
more than once, or discover a non-obvious workflow, add it here with a
one-line explanation of *why*, not just the syntax.

---

## Paths (fill these in for this machine)

```
GAME_ROOT   = F:\Steam\steamapps\common\BATTLETECH
MOD_DIR     = %GAME_ROOT%\Mods\GundamUC-Units
MODTEK_LOG  = %GAME_ROOT%\Mods\.modtek\ModTek.log
GAME_LOG    = %GAME_ROOT%\BattleTech_Data\output_log.txt
STOCK_DATA  = %GAME_ROOT%\BattleTech_Data\StreamingAssets\data
REFERENCE   = %GAME_ROOT%\reference-mod        (BEX / RogueTech / etc,
                                                deliberately OUTSIDE Mods\)
```

Note: `reference-mod/` must stay outside `Mods\` — ModTek's recursive
scan will otherwise load it as active content. This already broke the
mod once (see `03-TECHNICAL-NOTES.md`).

---

## Build the Harmony patch DLL

The C# source lives in `dev-source/GundamUCArrivalPatch/`.

```powershell
cd <MOD_DIR>\dev-source\GundamUCArrivalPatch
dotnet build -c Release
```

Then copy the built DLL into the mod folder where `mod.json`'s `DLL`
field expects it. Confirm the destination path matches `mod.json`
exactly — a mismatch fails silently at load.

If `dotnet build` complains about missing references, the project needs
paths to the game's own assemblies (`Assembly-CSharp.dll`,
`0Harmony.dll`, UnityEngine DLLs) under
`%GAME_ROOT%\BattleTech_Data\Managed\`.

---

## Decompile game code (to find what to patch)

Needed whenever a Harmony patch targets a method whose real signature
or behaviour isn't documented anywhere. This is how the
`timeLapse == 0` and null-`sim` issues in the arrival patch were found.

**Tool:** ILSpy (GUI) or dnSpy. Open:
```
%GAME_ROOT%\BattleTech_Data\Managed\Assembly-CSharp.dll
```
Search for the class/method of interest (e.g. `SimGameState`,
`FlashpointDayPassed`, `OnDayPassed`).

`dotnet-ildasm` / `ilspycmd` are CLI alternatives if a GUI isn't wanted.

---

## Log checking (first stop for any "it didn't work")

```powershell
# Did ModTek load our mod at all, and did anything throw?
Select-String -Path "<MODTEK_LOG>" -Pattern "GundamUC" -Context 2,2

# All errors/exceptions this run
Select-String -Path "<MODTEK_LOG>" -Pattern "Exception|ERROR|WARNING"

# Deeper Unity-side errors ModTek didn't catch
Select-String -Path "<GAME_LOG>" -Pattern "Exception" -Context 0,5
```

Silently-caught MDD indexing exceptions have been the cause of
**multiple** past bugs (bad `WeaponSubType` enums, sparse JSON). They
do not crash the game — they just make content vanish or hang. Always
grep for `Exception` even when the game "seems fine."

---

## Force a clean cache rebuild

Do this after any manifest change, or when a change appears to have no
effect:

```powershell
Remove-Item -Recurse -Force "<GAME_ROOT>\Mods\.modtek\Cache"
Remove-Item -Recurse -Force "<GAME_ROOT>\Mods\.modtek\Database"
```

ModTek regenerates both on next launch. A stale cache has masked real
fixes before.

---

## Validate all mod JSON before launching

Faster than finding a typo via a frozen intro video.

```powershell
Get-ChildItem -Recurse -Filter *.json "<MOD_DIR>\StreamingAssets" |
  ForEach-Object {
    try { Get-Content $_.FullName -Raw | ConvertFrom-Json > $null; "OK   $($_.Name)" }
    catch { "FAIL $($_.Name): $_" }
  }
```

---

## Verify every mod.json manifest path resolves

A path that doesn't exist fails quietly.

```powershell
cd "<MOD_DIR>"
$m = Get-Content mod.json -Raw | ConvertFrom-Json
$m.Manifest | ForEach-Object {
  if (Test-Path $_.Path) { "OK   $($_.Path)" } else { "MISSING $($_.Path)" }
}
```

---

## Search stock game data

Most questions ("what does a real MechDef look like?", "which units use
this tag?") are answered fastest by grepping the game's own files.

```powershell
# Find which stock files use a given field or tag
Select-String -Path "<STOCK_DATA>\*\*.json" -Pattern "unit_release" | Select -First 20

# Find a specific definition by Id
Select-String -Path "<STOCK_DATA>\*\*.json" -Pattern '"Id"\s*:\s*"mechdef_locust'
```

**Reminder:** DLC content (Flashpoint / Urban Warfare / Heavy Metal) is
**not** loose JSON — it lives inside `.assetbundle` files and won't
appear in these searches. See `03-TECHNICAL-NOTES.md`.

---

## Package a release build

```powershell
# From the parent of the mod folder
Compress-Archive -Path .\GundamUC-Units -DestinationPath .\GundamUC-Units.zip -Force
```

**Before shipping publicly**, exclude dev-only folders:
- `dev-reference/` — private research notes, never ships
  (`04-COPYRIGHT-GUIDELINES.md`)
- `dev-source/` — C# source; optional to ship, but the built DLL is
  what's required
- `reference-mod/` — never; that's other people's mods

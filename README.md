# ST Texture Memory Fix

A small BepInEx plugin for **Supermarket Together** that reduces the unusually large Windows committed-memory footprint associated with the game's textures.

## What it does

The plugin sets Unity's `QualitySettings.globalTextureMipmapLimit` for eligible mipmapped textures.

| Mode | Test-system process commit | Description |
|---|---:|---|
| 0 | ~12.5–12.9 GB | Full resolution / vanilla |
| 1 | ~5.2–5.4 GB | Half resolution / recommended |
| 2 | ~3.5–3.6 GB | Quarter resolution / aggressive |

These are measurements from one test system, not guaranteed savings. Exact results may vary by game version, save/store, hardware, graphics API and other factors.

Mode **1** is the default. During testing it produced the large memory reduction without meaningful visual degradation being noticed over a full in-game working day. Mode **2** saved more memory but visibly pixelated some in-game screens, cash registers and vending machines.

## Requirements

- Supermarket Together
- BepInEx 5.x

Tested with BepInEx 5.4.23.5 on Windows 11 / Direct3D 11.

## Installation

1. Install BepInEx 5 for Supermarket Together.
2. Copy `STTextureMemoryFix.dll` to `Supermarket Together\BepInEx\plugins\`.
3. Launch the game.

The configuration is created under `BepInEx\config\`.

## Hotkeys

- **F6**: better texture quality / more memory (`2 -> 1 -> 0`)
- **F7**: lower texture quality / less memory (`0 -> 1 -> 2`)

A small message at the top of the screen shows the active mode.

**Runtime changes are not instantaneous.** In particular, moving toward higher texture quality (`2 -> 1` or `1 -> 0`) can take a while to settle while Unity recreates/repopulates texture resources.

The selected mode is saved and reused on the next launch.

## Configuration

```ini
[Texture Quality]
MipmapLimit = 1

[Hotkeys]
DecreaseMipmapLimit = F6
IncreaseMipmapLimit = F7
```

`MipmapLimit` accepts:
- `0` = full / vanilla
- `1` = half / recommended
- `2` = quarter / aggressive

## What it does NOT change

The plugin does not modify autosave, save data, gameplay logic, PlayMaker behavior, employees, inventory, or mipmap streaming.

## Building

The project targets `netstandard2.1`. The supplied project file references the default Steam installation:

`C:\Program Files (x86)\Steam\steamapps\common\Supermarket Together`

Edit the `HintPath` entries if your installation is elsewhere.

```cmd
dotnet build -c Release
```

The DLL is produced under `bin\Release\netstandard2.1\`.

## Technical background

See [`docs/INVESTIGATION.md`](docs/INVESTIGATION.md) for the investigation and measurements that led to the fix.

## License

MIT.

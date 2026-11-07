# Leviathan Spear

![version](https://img.shields.io/badge/version-0.1.0-blue) ![valheim](https://img.shields.io/badge/Valheim-1.0.21-orange) ![client](https://img.shields.io/badge/client--side-only-green)

A BepInEx plugin for Valheim.

Throw your spear, then call it back. When a spear you've thrown lands, its icon floats above it with a ring that counts down. Once the ring is full, dodge roll anywhere nearby and the spear returns to an empty slot in your hotbar.

## Features
- Icon over every spear you've thrown, shown while you're within 20 m of it.
- Radial countdown ring (10 s by default); it turns green when the spear is ready.
- Dodge roll within 20 m of a ready spear to recall it into the first empty hotbar slot. Needs a free hotbar slot; you're told if there isn't one.
- Only your own thrown spears are tracked.
- Client-side only: works on vanilla servers.

## Configuration (`BepInEx/config/jordan.valheim.leviathanspear.cfg`)
| Section | Key | Default | What it does |
|---|---|---|---|
| General | Enabled | true | Turn the whole mod on or off. |
| General | Radius | 20 | Metres within which the icon shows and recall works. |
| Recall | Enabled | true | Allow recalling spears with a dodge roll. |
| Recall | Seconds | 10 | Countdown after landing before a spear can be recalled. |
| Look | IconSize | 48 | Icon size in pixels. |
| Look | HeightAboveSpear | 0.6 | How far above the spear the icon floats, in metres. |
| Look | RingColor | gold | Ring colour while counting down. |
| Look | RingReadyColor | green | Ring colour once the spear is ready. |
| Look | RingBackgroundColor | dark | Ring track colour. |

## Install

### Easiest: Valheim Mod Installer
Download the [**Valheim Mod Installer**](https://github.com/Glutoblop/ValheimPlugins) and run it (`Start-Installer.bat` on Windows, `start-installer.sh` on Linux/macOS), point it at your Valheim folder, tick **Leviathan Spear** and click **Install / Update**. It installs BepInEx for you if needed.

### Manual
Download from the [latest release](https://github.com/Glutoblop/LeviathanSpear/releases/latest) ([`LeviathanSpear.zip`](https://github.com/Glutoblop/LeviathanSpear/releases/latest/download/LeviathanSpear.zip): unzip into your Valheim folder), or:

1. Install [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) (5.4.2360 or newer) into your Valheim folder.
2. Download [`dist/LeviathanSpear.dll`](https://github.com/Glutoblop/LeviathanSpear/raw/main/dist/LeviathanSpear.dll).
3. Copy it to `<Valheim>/BepInEx/plugins/LeviathanSpear/LeviathanSpear.dll`.
4. Launch the game once; settings are written to `BepInEx/config/jordan.valheim.leviathanspear.cfg`.

## Compatibility

| | |
|---|---|
| Plugin version | **0.1.0** |
| Last tested on Valheim | **1.0.21** (network version 41, Steam build 26249040) on 2026-11-07 |
| BepInExPack_Valheim | 5.4.2360 |
| Multiplayer | Client-side only: works on vanilla servers, other players don't need it |

Machine-readable versions live in [`plugin.json`](plugin.json). The installer reads it straight from `main` to tell users when an update is available, or when the mod hasn't been tested against their game version.

## Building

Requires the .NET SDK (8+) and a Valheim install with BepInEx.

```
dotnet build -c Release -p:GameDir="C:\Path\To\Valheim"
```

The build copies the DLL into `<GameDir>/BepInEx/plugins/LeviathanSpear` for testing, and into `dist/` (which is committed, and is what the installer downloads).

## Releasing an update

Run `release\Release.bat` (or `powershell -File release\release.ps1 [-Bump patch|minor|major] [-Changes "text"]`). It checks the repo is pushed, bumps the version in `plugin.json` and `src/Plugin.cs`, rebuilds, commits, pushes and publishes the GitHub release with the zip. Use `-BuildOnly` to just build the package.

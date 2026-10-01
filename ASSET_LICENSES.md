# ECHO — Asset & Third-Party License Register

Every asset or library in ECHO that was not created for this project must be listed here
**before** it is used. If an asset's license is unclear, it is not used.

Rules:
- Only licenses that allow redistribution inside a WebGL game (e.g. CC0, CC-BY, MIT, or the Standard Unity Asset Store EULA, which allows assets inside a built game).
- No assets from commercial games, no copyrighted characters, music or logos.
- CC-BY assets must also be credited in the in-game Credits screen.
- Assets are stored locally in the project (no runtime downloads).

## Third-party code / plugins

| Name | Version | Source | License | Used for | Location |
|------|---------|--------|---------|----------|----------|
| GamePix Unity Plugin | 6.4 (Unity 2023.2–6000.4) | GamePix developer portal (my.gamepix.com) | Provided by GamePix for integrating games published on GamePix. No separate license file ships with the plugin. | WebGL build pipeline, platform pause/resume, ads, storage | `Assets/Plugins/GamePix/` |
| SharpZipLib (bundled with the GamePix plugin) | bundled | https://github.com/icsharpcode/SharpZipLib | MIT (see `Assets/Plugins/GamePix/SharpZipLib/LICENSE.txt`) | Editor-only: packing the `.gpx` archive | `Assets/Plugins/GamePix/SharpZipLib/` |

## Unity packages

Unity Registry packages (URP, Input System, uGUI/TextMeshPro, Timeline, 2D Sprite, 2D Tilemap,
Test Framework, IDE integrations) are covered by the Unity Companion License / Unity Terms of Service.
See `Packages/manifest.json` for the exact list and versions.

## Art

| Asset | Source | License | Author / credit | Used in |
|-------|--------|---------|-----------------|---------|
| _(none yet: all visuals so far are created in-project)_ | | | | |

## Audio

| Asset | Source | License | Author / credit | Used in |
|-------|--------|---------|-----------------|---------|
| _(none yet)_ | | | | |

## Fonts

| Font | Source | License | Used in |
|------|--------|---------|---------|
| _(none yet)_ | | | |

## VFX / Shaders

| Asset | Source | License | Used in |
|-------|--------|---------|---------|
| _(none yet)_ | | | |

## Generated assets

Assets produced with generation tools are listed here with the tool, the date and the tool's
usage terms that allow commercial distribution.

| Asset | Tool | Date | Terms | Used in |
|-------|------|------|-------|---------|
| Placeholder shapes: Square, RoundedRect, Circle, Diamond, DiamondOutline (`Assets/Art/Sprites/Generated/`) | Drawn in code by `EchoSpriteFactory` (this project) | 2026-10-01 | Original work, no third-party content | Graybox levels, player, energy pickups, HUD pips |
| Touch control icons: TouchRing, IconArrow, IconJump, IconDash, IconPause (`Assets/Art/UI/Touch/`) | Drawn in code by `EchoSpriteFactory` (this project) | 2026-10-01 | Original work, no third-party content | On-screen touch buttons, main menu placeholder |

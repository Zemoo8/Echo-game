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

All third-party art is copied in by `Tools/import_assets.py`, which also applies ECHO's
recolors (listed per row) so the changes are reproducible. Each pack folder keeps the author's
original license file as `LICENSE.txt`.

| Asset | Source | License | Author / credit | ECHO changes | Used in |
|-------|--------|---------|-----------------|--------------|---------|
| **Warped City**: player animations (idle, run, jump, back-jump, crouch, hurt), drone and turret enemies, enemy explosion, shot hit, lab props cut from the tileset (`Assets/ThirdParty/Ansimuz/WarpedCity/`) | https://opengameart.org/content/warped-city | CC0 / public domain ("free to use on whatever you want, personal or commercial") | Luis Zuno (@ansimuz). Credit not required; planned for the Credits screen | Player jacket yellow/orange → white/cool grey, red accent → cyan (yellow reads as danger in ECHO). Enemy cyan eyes → danger red. Prop screens green → energy cyan; `LabDoor_Exit` keeps its green. **The pack's music (by Pascal Belisle) is not used: it requires credit and is not CC0.** | Player; drones and turrets (Level 2); lab props |
| **Sideview Sci-Fi - Patreon Collection**: cyberpunk corridor segments and city backdrop, cryo pod, computers, terminals, server cabinets, elevator, machinery background and tileset, bulkhead walls (`Assets/ThirdParty/Ansimuz/SideviewSciFi/`) | https://opengameart.org/content/sideview-sci-fi-patreon-collection | CC0 ("free to use in whatever you want, credit is not required") | Luis Zuno (@ansimuz) | Corridor pieces cut from the sheet. Amber screens → energy cyan; corridor EXIT sign magenta → exit green; bulkhead walls warm brown/amber → cold steel blue | Level 1 lab backdrop and props; security wing and energy core backdrops (design showcase now, levels 2–3 later) |
| **Particle Pack** 1.1: 13 textures (sparks, stars, lights, smoke, trace, twirl) (`Assets/ThirdParty/Kenney/Particles/`) | https://kenney.nl/assets/particle-pack | CC0 | Kenney (www.kenney.nl) | Downscaled to 64/128 px | Glow halos and flashes |

## Audio

| Asset | Source | License | Author / credit | ECHO changes | Used in |
|-------|--------|---------|-----------------|--------------|---------|
| 35 sound effects from **Sci-Fi Sounds**, **Impact Sounds**, **Interface Sounds** and **Digital Audio** (`Assets/ThirdParty/Kenney/Audio/`) | https://kenney.nl/assets (packs: sci-fi-sounds, impact-sounds, interface-sounds, digital-audio) | CC0 | Kenney (www.kenney.nl) | None (original .ogg files) | All sound effects and ambience beds (`Assets/Settings/Audio/AudioLibrary.asset`) |
| **Dark Sci-Fi Audio Pack**: sector, airy, pulse, urgent, transmission, title, victory (`Assets/ThirdParty/SRG774/DarkSciFiAudio/`) | https://opengameart.org/content/dark-sci-fi-audio-pack | CC0 1.0 | SRG774 | Re-encoded to Ogg Vorbis q3; `title.ogg` trimmed to 96 s with fades | Menu, level, boss and ending music; level-complete sting |

## Fonts

| Font | Source | License | Used in |
|------|--------|---------|---------|
| Kenney Future, Kenney Future Narrow (`Assets/ThirdParty/Kenney/Fonts/`) | https://kenney.nl/assets/kenney-fonts | CC0 (Kenney) | All UI text (Kenney Future); the ECHO logo is drawn from its letters. Kenney Future Narrow is imported but unused |

## VFX / Shaders

| Asset | Source | License | Used in |
|-------|--------|---------|---------|
| `Echo/Additive` shader (`Assets/Art/Shaders/EchoAdditive.shader`) | Written for ECHO | Original work | Dash trail, afterimages, sparks, glows |
| Particle effect prefabs (`Assets/Prefabs/VFX/`) | Built in code by `EchoArtFactory` (this project) | Original work, using the textures listed above | Every effect in `VfxLibrary` |

## Generated assets

Assets produced with generation tools are listed here with the tool, the date and the tool's
usage terms that allow commercial distribution.

| Asset | Tool | Date | Terms | Used in |
|-------|------|------|-------|---------|
| Placeholder shapes: Square, RoundedRect, Circle, Diamond, DiamondOutline (`Assets/Art/Sprites/Generated/`) | Drawn in code by `EchoSpriteFactory` (this project) | 2026-10-01 | Original work, no third-party content | Square: UI fills and dividers. The other shapes were for the graybox and are no longer used |
| Touch control icons: TouchRing, IconArrow, IconJump, IconDash, IconPause (`Assets/Art/UI/Touch/`) | Drawn in code by `EchoSpriteFactory` (this project) | 2026-10-01 | Original work, no third-party content | On-screen touch buttons |
| UI gradients: Vignette, ShadeLeft (`Assets/Art/UI/Generated/`) | Drawn in code by `EchoSpriteFactory` (this project) | 2026-10-02 | Original work, no third-party content | Death flash, main menu backdrop shade |
| ECHO UI pixel art: panel, selection bar, HUD energy cells, bolt and chevron icons, ECHO logo (`Assets/Art/Pixel/UI/`) | Drawn in code by `Tools/make_echo_art.py` (this project); the logo's letters are rendered from Kenney Future (CC0) | 2026-10-02 | Original work; the font is CC0 | HUD, menus, level cards |
| ECHO pixel art: lab block and platform tiles, depth shade, hazard stripes, pit glow, energy cells, energy barrier and exit fields, barrier emitter, pixel/ring particle textures (`Assets/Art/Pixel/`) | Drawn in code by `Tools/make_echo_art.py` (this project), colors sampled from the Ansimuz lab palette | 2026-10-01 | Original work, no third-party content | Level 1 look, pickups, barriers, VFX |
| ECHO biome pixel art: security and core ground tiles, energy conduit (4 frames), reactor (calm and overload), security scanner lamp (`Assets/Art/Pixel/Security/`, `Assets/Art/Pixel/Core/`) | Drawn in code by `Tools/make_echo_art.py` (this project), colors sampled from the recolored bulkheads and the Ansimuz machinery wall | 2026-10-02 | Original work, no third-party content | Security wing, energy core and reactor kits, design showcase |
| ECHO gameplay pixel art: checkpoint beacon (off and on), laser emitter, laser beam (3 frames), turret bolt, console (locked and used), security shutter, steel platform (`Assets/Art/Pixel/Gameplay/`, `Assets/Art/Pixel/Security/SecurityPlatform.png`) | Drawn in code by `Tools/make_echo_art.py` (this project) | 2026-10-02 | Original work, no third-party content | Checkpoints, laser gates, turrets, consoles and shutters (Levels 1–2) |

# ECHO: Visual Overhaul Plan

Goal: the first 10 seconds of play should look intentional and polished. This is a dark
sci-fi facility with one controlled neon language, not a Unity prototype. Gameplay systems stay
exactly as they are; this pass replaces art, animation, VFX, audio, UI and presentation only.

## 1. Placeholder inventory (before the overhaul)

| # | What | Where | Placeholder today |
|---|------|-------|-------------------|
| 1 | Player | `Prefabs/Player/Player.prefab` | White rounded rectangle + cyan bar "visor", no animation |
| 2 | Floors, walls, ceilings, steps | `Sandbox_Movement` blocks | Flat navy squares stretched to size |
| 3 | Energy pickups | `Sandbox_Movement` | Flat cyan diamond |
| 4 | Breakable wall / exit door | `Sandbox_Movement` | Translucent cyan / green rectangles |
| 5 | Pits | `KillZone` | Invisible trigger, nothing marks the danger |
| 6 | Background | every scene | Solid camera clear color, no depth |
| 7 | Main menu | `MainMenu` | Pulsing triangle on black |
| 8 | HUD | Systems canvas | Five diamond pips |
| 9 | Touch controls | Systems canvas | Plain rings + icons (functional, unstyled) |
| 10 | VFX | none | No effects at all |
| 11 | Audio | none | Silent game |
| 12 | Lighting / post-processing | Template volume profile | No 2D lights, no bloom tuned for the style |
| 13 | Transitions / screens | SceneLoader | Hard cuts; no pause, death or level-complete screens |
| 14 | Camera | `CameraFollow` | Follow + look-ahead only; no shake, no impact feedback |

## 2. Art direction

**Dark navy/black world · cyan = energy · one warm accent = danger · white UI · strong silhouettes.**

| Role | Color | Rule |
|------|-------|------|
| World structure | `#070A12` → `#1B2A35` navy/teal | Low contrast, never glows except small light fixtures |
| Player | White jacket, magenta hair, dark legs | Brightest silhouette on screen, always readable |
| Energy | Cyan `#36E2FF` | Only energy glows cyan: cells, HUD, dash, dash-breakable barriers |
| Danger | Warm orange-red `#FF5A36` → `#FF3355` | Lasers, pits, enemy eyes, turrets, the boss. Nothing friendly is warm |
| Exit | Green `#3DFF8F` | Exit doors and goal lights only |
| UI | White text, cyan accents, dark translucent panels | Font: Kenney Future |

**Rendering:** pixel art at **32 px per unit**. The world renders at **640×360** through URP's Pixel
Perfect Camera and is upscaled to the browser (1.5× at 960×540, 3× at 1080p). The UI stays at native
resolution. 2D lights and a restrained bloom give the neon glow, reserved for light sources and energy.

## 3. Asset sources (all verified CC0 / public domain)

| Source | Used for |
|--------|----------|
| Ansimuz: **Warped City** (OpenGameArt, CC0 art; its music is excluded because it needs credit) | Player animations (recolored), drone + turret enemies, explosion, energy hit ring, lab props (doors, pods, vents, ladders, monitors) |
| Ansimuz: **Sideview Sci-Fi Patreon Collection** (OpenGameArt, CC0) | Lab corridor backgrounds, cryo pods, computers, server racks, terminals, elevator, bulkhead walls, machinery backdrops, robot units |
| Kenney: **Particle Pack**, **UI Pack Sci-Fi**, **Kenney Fonts** (CC0) | VFX textures, UI panels/bars, Kenney Future font |
| Kenney: **Sci-fi Sounds**, **Impact Sounds**, **Interface Sounds**, **Digital Audio** (CC0) | All sound effects |
| SRG774: **Dark Sci-Fi Audio Pack** (OpenGameArt, CC0 1.0) | Menu, level, boss and ending music; victory sting |
| ECHO originals | Platform/wall tile set drawn in the Ansimuz palette, energy cells, energy barrier, VFX sprites, HUD |

Not used: the Unity Asset Store (importing needs the Unity account login inside the editor, which
I can't do for you), AI image generators (the connected one has no API key; others have unclear
commercial terms), and anything with a credit-required or unclear license.

## 4. Swap-ability

All art is referenced through a few data assets, so any piece can be replaced later without touching gameplay:

- `PlayerVisuals`: animation frames and timing per state
- `EnvironmentKit` (one per biome): tile sprites, background layers, props, light colors
- `VfxLibrary`: every effect prefab by name
- `AudioLibrary`: every sound and music track by name

## 5. Systems built in this pass

| System | What it does |
|--------|--------------|
| `PlayerAnimator` | Sprite animation state machine (idle, run, jump, apex flip, fall, land, dash, hurt) driven by movement state, with squash and stretch |
| Dash VFX | Afterimage ghosts, cyan trail, start burst, speed lines, impact flash on barriers, short hit-stop, camera shake |
| `CameraFeedback` | Shake (trauma-based), small kick on landing, punch on barrier break |
| `ScreenFader` | Fade transitions for scene loads, death and respawn |
| Environment | Tiled platform panels, 3-layer parallax per biome, props, flickering lights, dust motes |
| Pickups / barriers | Animated energy cells (bob + glow + pickup burst); energy barriers with flicker and a shatter effect |
| `AudioManager` | Pooled SFX, music with crossfade, ambience bed; pauses with GamePix |
| UI | Energy meter (segmented cells, dash-ready glow, low-energy pulse), main menu, pause menu, death flash, level-complete card |

## 6. Biome kits

| Biome | Look | Main sources |
|-------|------|--------------|
| Level 1: Abandoned research lab | Dark teal corridors, cryo pods, dead monitors, dim cyan ceiling lights, dust | Ansimuz corridor + lab props |
| Level 2: Security facility | Cold steel-blue bulkheads, security doors, warning stripes, red scanner lights | Ansimuz bulkhead walls (recolored), Warped City doors and turrets |
| Level 3: Unstable energy core | Deep navy machinery, cyan energy conduits, pulsing reactor glow, sparks | Ansimuz machinery backdrop, ECHO energy art |
| Boss: Central reactor | Huge circular reactor, warm danger light during attacks | ECHO reactor art, Kenney particles |

## 7. Order of work

1. **Pipeline:** import packs into `Assets/ThirdParty`, set up the pixel-perfect camera, bloom, 2D lights and data assets.
2. **Player:** recolored animated protagonist, dash VFX, camera feedback, jump and landing dust.
3. **Level 1 look:** tiles, parallax, props, lights, particles, pickups, barriers, pit warnings, applied to the movement sandbox.
4. **UI:** font, HUD, main menu, pause menu, transitions, death and level-complete screens.
5. **Audio:** SFX, music, ambience.
6. **Biome kits 2–4** and the Phase 3 design showcase.

WebGL budget: textures packed into atlases, max 2048 px; music re-encoded to ~96 kbps; pooled
particles with low caps; no full-screen effects besides bloom and a vignette.

## 8. Progress

Updated 2026-10-02. Rebuild everything with **ECHO > Setup > Apply Visual Overhaul**.

| Step | Status | Notes |
|------|--------|-------|
| 1. Pipeline | Done | Packs imported (`Tools/import_assets.py`), import rules by folder, pixel-perfect camera (640x360, 32 px/unit, stretched to the screen), bloom 0.5, data assets (`PlayerVisuals`, `VfxLibrary`, `AudioLibrary`) |
| 2. Player | Done | Animated hero (idle, run, rise, apex flip, fall, land, dash, victory) with squash and stretch; dash trail, afterimages and burst; jump, landing and step dust; camera shake; hit-stop on barrier breaks; soft personal light |
| 3. Level 1 look | Done (movement sandbox) | Lab panel blocks with depth shading, corridor backdrop, city through the windows (parallax), upper machinery, props, flickering ceiling lights, pit warning stripes and red glow, animated energy cells, scrolling barrier fields, dust motes |
| 4. UI | Not started | HUD still uses the graybox pips; main menu is still the placeholder |
| 5. Audio | Wired, not yet heard in a test | 16 sound effects, 6 music tracks with ambience beds, crossfades; the sandbox plays the Lab track, the menu the Menu track |
| 6. Biome kits 2–4 | Not started | Security and Core art is imported and recolored, waiting for levels 2–3 |

**Verified:**
- 19/19 automated tests pass (9 EditMode, 10 PlayMode).
- The WebGL build loads in Chrome. The menu leads to the sandbox, which shows the new look at the right scale. A dash showed its trail, afterimages and burst, and the energy HUD dropped from 2 to 1.
- Stills are in `Docs/Screenshots/`.

**Not yet checked in the browser:**
- Jump flip and landing dust.
- Pickup burst, barrier shatter, exit shatter, victory pose.
- Sound and music.
- Frame rate on a phone.

**Follow-ups for the WebGL budget:**
- The `.gpx` grew from 20.7 MB to 32.2 MB. Music is about 10 MB (Vorbis q3); q1–q2 would roughly halve it.
- URP's default post-process data pulls in about 13 MB of film-grain and noise textures that ECHO never uses. A trimmed `PostProcessData` asset would remove them.

**Presentation note:** with the Animation, VFX and CameraFeedback features off (Phase 4), the
new art still shows, but the player holds a still frame and there are no effects or shake.

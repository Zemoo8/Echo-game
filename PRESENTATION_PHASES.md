# ECHO — Presentation Phases

The professor reviews ECHO in stages. This file explains how each stage is shown **without
removing any later work** from the project.

## Ground rules

1. **The complete game always stays in the repository.** Nothing is deleted or stubbed out to
   make an earlier stage look simpler.
2. **Presentation mode is a runtime switch.** It changes which features are *turned on* when the
   game runs. It does not change which code or assets *exist*.
3. **Git history is never faked.** Tags only mark real commits made at that point in development.
   The checkpoint log below records what actually existed and when.
4. **`Everything` is the normal setting.** Switch back to it after every presentation.

## How it works

| Piece | Location | Role |
|-------|----------|------|
| `PresentationConfig` asset | `Assets/Resources/PresentationConfig.asset` | Stores the active phase, plus optional per-feature force on/off overrides. |
| `PresentationPhase` / `Feature` | `Assets/Scripts/Presentation/PresentationPhase.cs` | The 8 phases, and the list of features a phase can hide. |
| `PresentationMode.IsEnabled(Feature.X)` | `Assets/Scripts/Presentation/PresentationConfig.cs` | What gameplay code asks before using a feature (e.g. dash, VFX, boss). |
| `PresentationGate` component | `Assets/Scripts/Presentation/PresentationGate.cs` | Put on any scene object; it switches itself off when its feature is hidden. |
| Boot routing | `BootSceneController` | Picks the first scene for the phase (concept showcase, design showcase or main menu). |
| Editor menu | **ECHO > Presentation** | One click per phase. The active phase has a checkmark. |
| Debug overlay | `DevOverlay` (backquote key) | Shows FPS / scene / state / phase. Visible from the start in Phase 6. |

### Switching phase

- **In Unity:** `ECHO > Presentation > Phase N` (or `Show Everything`), then press Play or make a build.
- **Fine-tuning a demo:** open `PresentationConfig` and use *Force Enabled* / *Force Disabled*
  to add or remove single features without changing the phase.
- **Before a GamePix release build:** set `Phase 8 - GamePix` (enables ads, hides the debug overlay).

## The phases

Status key: **Ready** = can be demonstrated now · **Partial** = some parts exist · **Planned** = not built yet.

| # | Phase | What is presented | In-engine start scene | Features on at runtime | Status |
|---|-------|-------------------|------------------------|------------------------|--------|
| 1 | Conception | Pitch: "Every dash is a decision", core loop, story premise, target platform (GamePix/WebGL), scope | `Showcase_Concept` | Concept showcase only | Planned |
| 2 | Diagrams / Schemas | Architecture & class diagrams, game-state machine, boss state machine, scene flow, energy economy per level (in `Docs/`) | `Showcase_Concept` | Concept showcase only | Planned |
| 3 | Design | Visual style guide (palette, player, enemies, tiles), level layouts, UI mockups | `Showcase_Design` | Showcases + main menu | Ready: `Showcase_Design` is a nine-slide deck in the engine (intro, color language, the hero's animations, threats and tools, the four biomes with their light and music, the interface kit), browsed with A/D, the arrows or the on-screen buttons; Esc goes to the main menu. Stills of every slide are in `Docs/Screenshots/design_*.png`; art direction and sources in `Docs/VISUAL_OVERHAUL_PLAN.md`. |
| 4 | Development | Playable core: movement, jump, dash, energy, hazards, enemies, switches, breakable doors, checkpoints, HUD, pause, save, touch controls. Levels 1–2. Placeholder visuals, no juice. | `MainMenu` | Core gameplay + game shell + Levels 1–2 + basic audio | Ready: **Levels 1–2** are playable from the main menu (Play, then Level 1 leads into Level 2, and Level 2 back to the menu: Phase 4 ends there): movement, jump, dash, energy, pits, timed laser gates, drones and turrets (a dash destroys them), consoles that open shutters and switch lasers off, dash-only barriers and exit doors, checkpoints, context hints for keyboard, gamepad and touch, the HUD, pause menu, and saved progress (Continue / New game, sound switches). Design notes: `Docs/LEVEL_DESIGN.md`. For a Phase 4 demo the polish (animation, VFX, music) is switched off by the phase, as intended. |
| 5 | 2D Animation | Phase 4 plus player/enemy animations, dash trail & afterimages, particles, camera feedback. Level 3. | `MainMenu` | Phase 4 + animation, VFX, camera feedback, Level 3 | Partial: animated player, dash trail and afterimages, particles, camera shake and hit-stop, animated drones, and **Level 3 (Energy Core)**, playable after Level 2: cracked plates that give way, core locks that open only while you hold enough charge, the reactor. Turret firing animation not built yet. |
| 6 | Testing | Phase 5 plus boss and music, debug overlay on, test plan & results, bug log, performance numbers (in `Docs/`) | `MainMenu` | Phase 5 + boss, music, debug overlay | Partial: 43 automated tests (10 EditMode + 33 PlayMode) cover the presentation phases, energy, jump, coyote time, jump buffer, dash, air-dash limit, barriers, pickups, the UI flow (menu to level, title card, energy meter, pause, death and respawn, level complete, saved sound settings), the design showcase, Levels 1–3 (checkpoints, lasers, hints, drones, turrets, consoles, shutters, cracked plates, core locks, and a bot that plays each level from start to exit), and which levels each phase leads into. |
| 7 | Final Product | The complete game end to end, including the ending cinematic | `MainMenu` | Everything except debug overlay and ads | Planned |
| 8 | GamePix | Final game built through the GamePix plugin, tested in browser, `.gpx` package | `MainMenu` | Phase 7 + GamePix ads | Planned |
| – | Everything | Development default: every feature on (debug overlay available with the backquote key) | `MainMenu` | All | Default |

If a phase's start scene has not been built yet, Boot falls back to `MainMenu` and logs a warning.

## Commands

| Command | What happens |
|---------|--------------|
| `SHOW PHASE N` | Set the config to phase N, check that its scenes and docs are ready, and run it once to confirm. Nothing is removed. |
| `SHOW EVERYTHING` | Set the config back to `Everything`, so the full game runs with all features. Nothing is removed. |
| `PREPARE FOR NEXT SESSION` | Work out the next phase from our conversation, update its status in this file, finish or polish what that phase needs, set the config, and commit. All later work stays intact. |

## Git checkpoint log

Tags are created only on real commits at the moment a milestone is actually finished.

| Date | Commit / tag | Milestone | Notes |
|------|--------------|-----------|-------|
| 2026-10-01 | `13a1bdc` | Baseline | Untouched Unity 6000.2.7f2 URP project with the GamePix 6.4 plugin. |
| 2026-10-01 | `a9eb981` | M0 foundation | 2D renderer, layers, presentation system, boot flow, docs. |
| 2026-10-01 | tag `m0-foundation` | M0 complete | GamePix WebGL build tested in Chrome: Boot → MainMenu, systems spawn, dev overlay works, no blocking errors. |
| 2026-10-01 | `d1f090b` | M1 player feel | Movement, jump, dash, energy, pickups, dash-only doors, HUD, touch controls, movement sandbox, 19 automated tests. |
| 2026-10-01 | tag `m1-player-feel` | M1 complete | WebGL build played in Chrome at 60 fps: menu → sandbox → exit door → reload loop with no errors; dash measured at 30 u/s as tuned. |
| 2026-10-02 | `093d1f8` | M2 visual overhaul, part 1 | Art pipeline, animated player, dash VFX, Level 1 lab look in the sandbox, audio wired. 19 tests pass; WebGL build loads in Chrome. |
| 2026-10-02 | `078336f` | M2 visual overhaul, part 2 | Title screen, energy meter, pause menu, level cards, death flash, fades, sound switches. 26 tests pass; the WebGL UI was checked in Chrome (see `Docs/VISUAL_OVERHAUL_PLAN.md`, section 8). |
| 2026-10-02 | `a674be7` | M2 visual overhaul, part 3 | Environment kits for all four biomes, Phase 3 design showcase scene, fixed dash length (counted in physics steps). 27 tests pass; showcase slides rendered in the editor. |
| 2026-10-02 | `2905f59` | M3 Level 1 | Level 1 (Awakening) playable from the menu with checkpoints, laser gates, context hints and Continue. 32 tests pass, including a bot that completes the level. |
| 2026-10-02 | `44edd6c` | M3 Level 2 | Level 2 playable after Level 1, with machines a dash destroys, consoles, shutters and a laser console. 37 tests pass, including a bot that completes both levels. |
| 2026-10-02 | "M3: Level 3 (energy core): cracked plates, core locks" | M3 Level 3 | Level 3 playable after Level 2, with plates that give way and locks that need held charge; levels lead only into what the active phase presents. 43 tests pass, including a bot that completes all three levels. |

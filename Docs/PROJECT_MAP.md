# ECHO — Project Map

Where everything is and how the game behaves, for answering questions during a presentation.
All paths start at the project folder (`Echo-game/`).

## 1. Where everything is

| Folder / file | What is in it |
|---|---|
| `Assets/Scenes/` | `Boot` and `MainMenu`, plus `Levels/` (3 levels, boss, ending), `Presentation/` (the two slide decks) and `Dev/` (movement test room) |
| `Assets/Scripts/` | All game code, one folder per area: `Core`, `Player`, `Gameplay`, `UI`, `Visuals`, `Audio`, `Presentation`, `Platform`, `DevTools`, `Editor` |
| `Assets/Settings/` | Data edited in the Inspector, no code needed: `Gameplay/PlayerTuning`, `Visuals/PlayerVisuals`, `Visuals/VfxLibrary`, `Visuals/Kits/` (Lab, Security, Core, Reactor), `Audio/AudioLibrary`, `Input/EchoControls` |
| `Assets/Resources/` | `PresentationConfig.asset` (the active phase) and `Systems.prefab` (the managers and the persistent UI) |
| `Assets/Prefabs/` | `Player/` (the player) and `VFX/` (13 particle effects) |
| `Assets/Art/Pixel/` | ECHO's own pixel art (tiles, energy cells, barriers, gameplay objects, the Guardian, UI), drawn by `Tools/make_echo_art.py` |
| `Assets/Art/Shaders/`, `Assets/Art/Materials/` | The `Echo/Additive` glow shader and its 5 materials |
| `Assets/ThirdParty/` | Free CC0 packs: `Ansimuz/` (player, enemies, backdrops), `Kenney/` (sounds, font, particle textures), `SRG774/` (music) |
| `Assets/Plugins/GamePix/` | The GamePix plugin 6.4 (WebGL build, ads, storage) |
| `Assets/Tests/` | Automated tests: `EditMode/` and `PlayMode/` |
| `Tools/` | Python scripts: `import_assets.py` (copies and recolors the free packs), `make_echo_art.py` (draws ECHO's pixel art), `make_diagrams.py` (the UML diagrams), `make_video.py` (cuts the presentation video from the captured footage) |
| `Docs/` | `LEVEL_DESIGN.md`, `VISUAL_OVERHAUL_PLAN.md`, `Screenshots/` |
| `html5/` | Output of the GamePix build (`index.html`, `Build/`, the `.gpx` package). Not committed to git |
| Project root | `README.md` (controls, how to work on it), `PRESENTATION_PHASES.md` (the phases), `ASSET_LICENSES.md` (every asset and its license) |

## 2. Scenes, in play order

| Scene | File | What happens there |
|---|---|---|
| Boot | `Assets/Scenes/Boot.unity` | Always first. Sends the game to the first scene of the active phase |
| MainMenu | `Assets/Scenes/MainMenu.unity` | Logo, Continue (if there is a save), Play / New game, music and sound switches, credits |
| Level1_Awakening | `Assets/Scenes/Levels/` | The lab: movement, jump, dash, pits, timed laser gates, checkpoints, hints |
| Level2_Facility | `Assets/Scenes/Levels/` | Security wing: drones, turrets, consoles that open shutters or switch lasers off |
| Level3_Core | `Assets/Scenes/Levels/` | Energy core: cracked plates that fall, core locks that need held charge, the reactor |
| Boss_Guardian | `Assets/Scenes/Levels/` | The Guardian fight |
| Ending | `Assets/Scenes/Levels/` | Captions over the calm reactor, the rescue ship answers, THE END, back to the menu |
| Showcase_Concept | `Assets/Scenes/Presentation/` | Slide deck for Phases 1 and 2 |
| Showcase_Design | `Assets/Scenes/Presentation/` | Slide deck for Phase 3 |
| Sandbox_Movement | `Assets/Scenes/Dev/` | Test room for tuning movement and dash |

The scenes are built by code, not by hand: `Assets/Scripts/Editor/EchoLevelFactory.cs` (levels),
`EchoLevelFactory.Finale.cs` (boss, ending), `EchoShowcaseFactory*.cs` (decks). The
`ECHO > Setup > Rebuild ...` menu items run them and overwrite the scene, so do not click those
during a demo.

## 3. How the game works

Script paths are inside `Assets/Scripts/`.

| System | Script | How it behaves |
|---|---|---|
| Start-up | `Core/GameBootstrap.cs`, `Core/BootSceneController.cs` | The Systems prefab is created before the first scene, so every scene has its managers. Boot then loads the phase's first scene |
| Game state, pause | `Core/GameManager.cs` | Holds the state (menu, playing, paused...) and the time scale; also the short freeze on big hits |
| Scene changes | `Core/SceneLoader.cs`, `UI/ScreenFader.cs` | One scene at a time, loaded in the background with a fade out and in |
| Input | `Core/GameInput.cs` | Keyboard, gamepad and touch buttons all feed the same actions, so gameplay never checks the device |
| Player | `Player/PlayerController.cs`, `Player/PlayerMovement.cs` | Run and jump. Tap = short hop, hold = full jump. Forgiving: coyote time and jump buffering |
| Dash | `Player/PlayerDash.cs` | Short horizontal burst with no gravity. Costs 1 energy. One air dash per jump. Hits barriers and the boss core in its path |
| Energy | `Player/EnergySystem.cs`, `Gameplay/EnergyPickup.cs` | Whole charges. Each level sets the start amount and the maximum. A cell is not taken while energy is full, so none is wasted |
| Level rules | `Gameplay/LevelController.cs` | Spawns the player, respawns at the last checkpoint after a death (or R), saves progress, loads the next scene when the exit door breaks |
| Checkpoints | `Gameplay/Checkpoint.cs` | Saves the energy on arrival and the level's state; after a death, cells, barriers and machines used later come back |
| Barriers, exit doors | `Gameplay/BreakableBarrier.cs` | Only a dash breaks them. Every exit door is one, which is why a charge must be kept in reserve |
| Enemies | `Gameplay/Machine.cs`, `Drone.cs`, `Turret.cs`, `TurretBolt.cs` | Touching one kills. A dash destroys it but uses the charge. Drones patrol between two points; turrets fire on a fixed rhythm with a warning |
| Hazards | `Gameplay/LaserGate.cs`, `KillZone.cs` | Lasers switch on and off on a rhythm and flicker before coming back. Pits kill |
| Consoles, shutters | `Gameplay/ControlConsole.cs`, `SecurityDoor.cs` | A console opens a steel shutter or switches a laser off |
| Level 3 objects | `Gameplay/CollapsingPlatform.cs`, `ChargeLock.cs` | A cracked plate falls 0.5 s after you stand on it and re-forms after 2.5 s. A core lock opens only while you hold enough charges; a dash does not break it |
| Boss | `Gameplay/Guardian.cs`, `GuardianShockwave.cs`, `GuardianStrike.cs`, `CellRespawner.cs` | See the boss table below |
| Camera | `Gameplay/CameraFollow.cs`, `Visuals/CameraFeedback.cs` | Follows the player, looks ahead, stays inside the level; shakes on impacts |
| HUD and menus | `UI/` (`EnergyHud`, `PauseMenu`, `MainMenu`, `LevelIntroCard`, `LevelCompleteCard`, `BossBar`, `TouchControls`) | Energy meter, pause, title cards, boss health, on-screen touch buttons. Built by `Editor/EchoUiFactory.cs` |
| Animation, effects | `Visuals/PlayerAnimator.cs`, `VfxManager.cs`, `DashEffects.cs` | Animation is picked from what the movement is doing, with squash and stretch. Effects come from pools, so nothing is allocated during play |
| Biome look | `Visuals/EnvironmentKit.cs` + `Assets/Settings/Visuals/Kits/` | One data asset per biome (Lab, Security, Core, Reactor) sets the tiles, backdrop, light and music |
| Audio | `Audio/AudioManager.cs`, `AudioLibrary.cs`, `SoundSettings.cs` | Sound effects from a pool, music with crossfades, an ambience loop. The player's music and sound switches are saved |
| Save | `Core/SaveData.cs` | Stores which level Continue starts, in PlayerPrefs (GamePix keeps that in the browser's localStorage). Finishing the story clears it |
| Phases | `Presentation/PresentationConfig.cs`, `PresentationPhase.cs`, `PresentationGate.cs` | Each phase is a list of features that are switched on. Code asks `PresentationMode.IsEnabled(Feature.X)`. Nothing is deleted, only switched off |
| GamePix | `Platform/GamePixManager.cs` | Thin wrapper on the plugin: reports platform pauses and shows an ad between levels (Phase 8 only) |
| Debug overlay | `DevTools/DevOverlay.cs` | Backquote key: FPS, scene, state, phase, live player data |

### The Guardian

| Rule | Value |
|---|---|
| Hits to win | 6 (3 phases, 2 hits each; each phase is faster) |
| When it can be hit | Only while its shell is open (core cyan), with a dash |
| Dash into the closed shell (core red) | Bounces off and the charge is lost |
| Touching it without dashing | Kills |
| Its attacks | A floor wave to jump or dash through; a ceiling beam that warns first with a thin line; bolts from the wall ports |
| Dying | Restarts the current phase, not the whole fight |
| Energy in the arena | Cells come back 4 s after being taken |

## 4. Key numbers

Set in `Assets/Settings/Gameplay/PlayerTuning.asset` (defaults in `Assets/Scripts/Player/PlayerTuning.cs`).

| Value | Number |
|---|---|
| Run speed | 8.5 units/s |
| Full jump height | 3.2 units |
| Dash | 4.5 units in 0.15 s (30 units/s), 0.25 s cooldown |
| Air dashes per jump | 1 |
| Coyote time / jump buffer / dash buffer | 0.10 s / 0.12 s / 0.10 s |
| Max fall speed | 22 units/s |
| Art scale | 32 pixels per unit |
| Dash cost | 1 energy, always |

## 5. Controls

| Action | Keyboard | Gamepad | Touch |
|---|---|---|---|
| Move | A / D or arrows | Left stick, D-pad | Arrow buttons |
| Jump (hold for higher) | Space, W, Up, Z | South button | Jump button |
| Dash | Shift, X, J | West button, RB, RT | Dash button |
| Pause | Esc, P | Start | Pause button |
| Back to last checkpoint | R | Select | Pause, then Restart (restarts the level) |
| Debug overlay | ` (backquote) | | |

## 6. Where the assets come from

Full list with links: `ASSET_LICENSES.md`.

| Asset | Source | License | Location |
|---|---|---|---|
| Player, drone, turret, explosions, lab props | Warped City by Luis Zuno (ansimuz), recolored | CC0 | `Assets/ThirdParty/Ansimuz/WarpedCity/` |
| Corridors, backdrops, machinery | Sideview Sci-Fi by Luis Zuno (ansimuz), recolored | CC0 | `Assets/ThirdParty/Ansimuz/SideviewSciFi/` |
| 35 sound effects | Kenney audio packs | CC0 | `Assets/ThirdParty/Kenney/Audio/` |
| Music (7 tracks) | Dark Sci-Fi Audio Pack by SRG774 | CC0 | `Assets/ThirdParty/SRG774/DarkSciFiAudio/` |
| UI font | Kenney Future | CC0 | `Assets/ThirdParty/Kenney/Fonts/` |
| Particle textures | Kenney Particle Pack | CC0 | `Assets/ThirdParty/Kenney/Particles/` |
| Tiles, cells, barriers, gameplay objects, Guardian, UI, logo | Drawn in code for ECHO (`Tools/make_echo_art.py`) | Original work | `Assets/Art/Pixel/` |
| Glow shader, particle effects | Written and built for ECHO | Original work | `Assets/Art/Shaders/`, `Assets/Prefabs/VFX/` |
| GamePix plugin | GamePix developer portal | Provided by GamePix | `Assets/Plugins/GamePix/` |

## 7. Tests

52 automated tests in `Assets/Tests/` (10 EditMode, 42 PlayMode). Run them in Unity with
`Window > General > Test Runner`.

| File | Tests | What it checks |
|---|---|---|
| `EditMode/EnergySystemTests.cs` | 4 | Energy spending and refilling |
| `EditMode/PresentationConfigTests.cs` | 6 | Which features each phase switches on |
| `PlayMode/PlayerMovementTests.cs` | 10 | Jump, coyote time, jump buffer, dash, air-dash limit, barriers, pickups |
| `PlayMode/UiFlowTests.cs` | 7 | Menu to level, title card, energy meter, pause, death, level complete, sound settings |
| `PlayMode/Level1Tests.cs`, `Level2Tests.cs`, `Level3Tests.cs` | 5 each | Each level's objects, and a bot that plays the level from start to exit |
| `PlayMode/BossTests.cs` | 6 | The Guardian's rules, and a bot that wins the fight |
| `PlayMode/DesignShowcaseTests.cs` | 3 | The slide decks for Phases 1, 2 and 3 |
| `PlayMode/FullGameTests.cs` | 1 | Bots play the whole game |
| `PlayMode/TrailerCapture.cs` | 6 (explicit, not in the 52) | Films the presentation video's footage: bots play each part while every frame is saved to `Recordings/` |

## 8. Technology

| | |
|---|---|
| Engine | Unity 6000.2.7f2, URP 2D Renderer |
| Language | C# |
| Target | WebGL in the browser, through the GamePix plugin 6.4 |
| Input | Unity Input System (keyboard, gamepad, touch) |
| Art tools | Python scripts in `Tools/` |
| Version control | Git; the milestone log is in `PRESENTATION_PHASES.md` |

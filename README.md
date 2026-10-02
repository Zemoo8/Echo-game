# ECHO

A futuristic 2D platformer built in Unity for the browser (GamePix / WebGL).

**Every dash is a decision.** Each dash costs one energy charge, and every level ends with a door
that only a dash can break, so the player always has to keep one charge in reserve.

## Project

| | |
|---|---|
| Engine | Unity 6000.2.7f2, URP 2D Renderer |
| Platform | WebGL through the GamePix plugin 6.4 (`Assets/Plugins/GamePix`) |
| Input | Keyboard, gamepad and on-screen touch controls |
| Code | `Assets/Scripts` (assembly `Echo`), editor tools in `Assets/Scripts/Editor` |
| Tests | `Assets/Tests` (EditMode + PlayMode) |

## Controls

| Action | Keyboard | Gamepad | Touch |
|--------|----------|---------|-------|
| Move | A / D or ← / → | Left stick, D-pad | ◀ ▶ buttons |
| Jump (hold for higher) | Space, W, ↑, Z | South button (A / ✕) | ⌃ button |
| Dash | Shift, X, J | West button (X / □), RB, RT | » button |
| Pause | Esc, P | Start | ❚❚ button |
| Back to last checkpoint | R | Select | ❚❚ then Restart (restarts the level) |
| Menus | ↑ ↓ (or W / S), Enter or Space, Esc to go back | D-pad / stick, South, East | tap |
| Dev overlay | ` (backquote) | | |

## Working on the project

- **First open / after pulling:** `ECHO > Setup > Run Project Setup` re-applies settings and creates
  any missing generated assets. It never overwrites hand-edited scenes.
- **Feel tuning:** edit `Assets/Settings/Gameplay/PlayerTuning.asset` in the Inspector, even while playing.
- **Levels:** `Assets/Scenes/Levels/` (built by `ECHO > Setup > Rebuild Level 1`); the movement test chamber is `Assets/Scenes/Dev/Sandbox_Movement.unity`.
- **Tests:** Window > General > Test Runner, or headless:
  `Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results.xml`
- **GamePix build:** `ECHO > Build > GamePix Package` (output in `html5/`, which is not committed).

### Art pipeline

| Step | How |
|------|-----|
| Third-party art and audio | `python Tools/import_assets.py <downloads> [pack ...]` copies the CC0 packs into `Assets/ThirdParty/` with ECHO's recolors (see `Tools/README.md`) |
| ECHO's own pixel art | `python Tools/make_echo_art.py` draws tiles, energy cells, barriers, FX textures and the UI (panels, HUD cells, icons, logo) into `Assets/Art/Pixel/` |
| Import settings | Automatic, by folder (`EchoImportRules`): 32 px per unit, point filtering, compressed audio |
| Build the look | `ECHO > Setup > Apply Visual Overhaul` makes the effect materials, the player animation set, every particle effect, the audio library, and rebuilds the player prefab, the persistent UI (`EchoUiFactory`), the main menu, the movement sandbox and the design showcase |
| Swap art later | Edit the data assets, not code: `Settings/Visuals/PlayerVisuals` (animation frames), `Settings/Visuals/VfxLibrary` (effects), `Settings/Visuals/Kits/*` (one look per biome), `Settings/Audio/AudioLibrary` (sounds and music) |
| Screenshots | `ECHO > Tools > Capture Sandbox Screenshots` and `ECHO > Tools > Capture Design Showcase Slides` write stills to `Docs/Screenshots/` |

## Documents

- [PRESENTATION_PHASES.md](PRESENTATION_PHASES.md): staged demos for the course, plus the checkpoint log
- [ASSET_LICENSES.md](ASSET_LICENSES.md): every external or generated asset and its license
- [Docs/VISUAL_OVERHAUL_PLAN.md](Docs/VISUAL_OVERHAUL_PLAN.md): art direction, asset sources and the visual overhaul's progress
- [Docs/LEVEL_DESIGN.md](Docs/LEVEL_DESIGN.md): movement metrics, Level 1 section by section, checkpoints and hazards

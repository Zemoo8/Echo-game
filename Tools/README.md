# ECHO tools

Python scripts that produce ECHO's art inputs. They need Python 3 with Pillow (`pip install pillow`);
`import_assets.py` also needs `ffmpeg` on the PATH for the music.

| Script | What it does |
|--------|--------------|
| `import_assets.py <downloads>` | Copies the third-party packs into `Assets/ThirdParty/`, applying ECHO's recolors and crops |
| `make_echo_art.py` | Draws ECHO's original pixel art into `Assets/Art/Pixel/` |

After either script runs, open Unity (or run it headless) so the files import with the rules in
`Assets/Scripts/Editor/EchoImportRules.cs`, then run **ECHO > Setup > Apply Visual Overhaul**.

## Downloads for `import_assets.py`

Extract each archive into the downloads folder with this layout:

| Folder | Download | License |
|--------|----------|---------|
| `warped/warped city files/` | Warped City by ansimuz: https://opengameart.org/content/warped-city | CC0 |
| `ansimuz/Sideview Sci-Fi - Patreon Collection/` | Sideview Sci-Fi - Patreon Collection by ansimuz: https://opengameart.org/content/sideview-sci-fi-patreon-collection | CC0 |
| `kenney/kenney_particle-pack/` | https://kenney.nl/assets/particle-pack | CC0 |
| `kenney/kenney_kenney-fonts/` | https://kenney.nl/assets/kenney-fonts | CC0 |
| `kenney/kenney_sci-fi-sounds/`, `kenney_impact-sounds/`, `kenney_interface-sounds/`, `kenney_digital-audio/` | https://kenney.nl/assets (one page per pack) | CC0 |
| `music/darkscifi/ogg/` | Dark Sci-Fi Audio Pack by SRG774: https://opengameart.org/content/dark-sci-fi-audio-pack | CC0 |

Every asset and its license is also listed in `ASSET_LICENSES.md`.

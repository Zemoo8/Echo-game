"""
Copies the third-party assets ECHO uses into the Unity project, applying ECHO's art-direction
changes. Every source is CC0 / public domain (see ASSET_LICENSES.md); the modifications are
listed here so they are reproducible.

Usage:
    python Tools/import_assets.py <downloads-folder> [pack ...]
    (packs: warped, sideview, kenney, music; default: all)

The downloads folder must contain the extracted packs (see Tools/README.md for the URLs):
    ansimuz/Sideview Sci-Fi - Patreon Collection/
    warped/warped city files/
    kenney/kenney_<pack>/
    music/darkscifi/ogg/
"""
import colorsys
import os
import shutil
import subprocess
import sys
from PIL import Image

PROJECT = os.path.join(os.path.dirname(__file__), "..")
THIRD = os.path.join(PROJECT, "Assets", "ThirdParty")


def out(*parts):
    path = os.path.join(THIRD, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    return path


# --- color rules -----------------------------------------------------------------------------

# The hero's yellow jacket would read as "danger" in ECHO's color language: white jacket, cool
# shading, cyan accent. Hair, skin, outline and legs are untouched.
PLAYER_MAP = {
    (0xFF, 0xD8, 0x00): (0xF1, 0xF5, 0xFF),
    (0xEC, 0x78, 0x09): (0x9A, 0xAA, 0xC6),
    (0xFF, 0x22, 0x45): (0x36, 0xE2, 0xFF),
}


def remap(img, mapping):
    img = img.convert("RGBA")
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in mapping:
                px[x, y] = (*mapping[(r, g, b)], a)
    return img


def hue_shift(img, hue_min, hue_max, new_hue, min_saturation=0.25):
    """Moves every pixel whose hue (degrees) is in [hue_min, hue_max] to new_hue."""
    img = img.convert("RGBA")
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            deg = h * 360
            if s >= min_saturation and hue_min <= deg <= hue_max:
                nr, ng, nb = colorsys.hsv_to_rgb(new_hue / 360, s, v)
                px[x, y] = (round(nr * 255), round(ng * 255), round(nb * 255), a)
    return img


GREEN_TO_CYAN = (80, 160, 188)     # lab screens and pods glow with energy, not exit green
CYAN_TO_DANGER = (165, 205, 12)    # enemy eyes and sensors read as danger
MAGENTA_TO_EXIT = (270, 335, 140)  # the corridor EXIT sign uses ECHO's exit green
AMBER_TO_CYAN = (25, 65, 188)      # background screens: energy cyan, not warning amber


def warm_to_steel(img, hue=205, saturation=0.55):
    """Turns warm browns and ambers into cold steel blue (the security wing). Bright, pale lamp
    cores lose most of their color so lamps read as cold white light."""
    img = img.convert("RGBA")
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            deg = h * 360
            if deg <= 70 or deg >= 340:
                s = s * (0.25 if v > 0.8 and s < 0.5 else saturation)
                nr, ng, nb = colorsys.hsv_to_rgb(hue / 360, s, v)
                px[x, y] = (round(nr * 255), round(ng * 255), round(nb * 255), a)
    return img


# --- packs -----------------------------------------------------------------------------------

def warped_city(src):
    root = os.path.join(src, "warped", "warped city files")
    sprites = os.path.join(root, "SPRITES")

    animations = {"idle": 4, "run": 8, "jump": 4, "hurt": 0, "crouch": 0, "back-jump": 7}
    for anim, count in animations.items():
        names = [f"{anim}.png"] if count == 0 else [f"{anim}-{i}.png" for i in range(1, count + 1)]
        for i, name in enumerate(names, 1):
            frame = remap(Image.open(os.path.join(sprites, "player", anim, name)), PLAYER_MAP)
            frame.save(out("Ansimuz", "WarpedCity", "Player", anim, f"{anim}_{i:02d}.png"))

    for enemy, count in (("drone", 4), ("turret", 6)):
        for i in range(1, count + 1):
            frame = hue_shift(Image.open(os.path.join(sprites, "misc", enemy, f"{enemy}-{i}.png")), *CYAN_TO_DANGER)
            frame.save(out("Ansimuz", "WarpedCity", "Enemies", enemy, f"{enemy}_{i:02d}.png"))

    for fx, count in (("enemy-explosion", 6), ("shot-hit", 3)):
        for i in range(1, count + 1):
            shutil.copy(os.path.join(sprites, "misc", fx, f"{fx}-{i}.png"),
                        out("Ansimuz", "WarpedCity", "FX", fx, f"{fx}_{i:02d}.png"))

    # Lab props cut from the environment sheet (rects found by connected-component detection).
    props = {
        "WallPillar": (16, 16, 32, 64), "LabDoor": (48, 16, 96, 80), "WallPillar_B": (112, 16, 128, 64),
        "WallPanelTall": (144, 16, 176, 80), "PillarMachine": (192, 16, 208, 80), "ShutterWindow": (224, 16, 272, 80),
        "WallPanel": (288, 16, 320, 80), "Ladder": (32, 96, 48, 128), "PipeLight": (64, 96, 80, 144),
        "HangingLamp": (112, 100, 144, 160), "MachineryPipes": (160, 96, 208, 128), "ScreenPod": (224, 96, 272, 160),
        "ServerRack": (288, 96, 304, 128), "Monitor": (320, 96, 336, 128), "PipePanel": (352, 96, 368, 128),
        "FanUnit": (176, 144, 208, 176), "Terminal": (64, 160, 80, 192), "HoverPlatform": (105, 176, 151, 192),
        "EnergyPod": (224, 176, 272, 240),
    }
    sheet = Image.open(os.path.join(root, "ENVIRONMENT", "tileset.png")).convert("RGBA")
    for name, rect in props.items():
        piece = sheet.crop(rect)
        hue_shift(piece, *GREEN_TO_CYAN).save(out("Ansimuz", "WarpedCity", "Props", f"{name}.png"))
    # The exit keeps its green glow.
    sheet.crop(props["LabDoor"]).save(out("Ansimuz", "WarpedCity", "Props", "LabDoor_Exit.png"))

    shutil.copy(os.path.join(root, "public-license.txt"), out("Ansimuz", "WarpedCity", "LICENSE.txt"))


def sideview_scifi(src):
    root = os.path.join(src, "ansimuz", "Sideview Sci-Fi - Patreon Collection")
    env = os.path.join(root, "Environments")

    corridor = os.path.join(env, "cyberpunk-corridor-files", "PNG", "layers")
    pieces = {"Corridor_A": (16, 16, 128, 192), "Corridor_Machinery": (144, 16, 368, 192),
              "Corridor_Terminal": (16, 208, 128, 384), "Corridor_RoomEnd": (144, 208, 480, 384),
              "Corridor_Shutter": (16, 416, 144, 592), "Corridor_Exit": (160, 416, 496, 592)}
    sheet = Image.open(os.path.join(corridor, "cyberpunk-corridor.png")).convert("RGBA")
    for name, rect in pieces.items():
        piece = sheet.crop(rect)
        if name == "Corridor_Exit":
            piece = hue_shift(piece, *MAGENTA_TO_EXIT)
        piece.save(out("Ansimuz", "SideviewSciFi", "Lab", f"{name}.png"))
    shutil.copy(os.path.join(corridor, "back.png"), out("Ansimuz", "SideviewSciFi", "Lab", "Corridor_Back.png"))
    shutil.copy(os.path.join(corridor, "cyberpunk-corridor-foreground.png"),
                out("Ansimuz", "SideviewSciFi", "Lab", "Corridor_Foreground.png"))

    props = os.path.join(env, "cyberpunk-detective-prop-files", "PNG")
    for name in ("cryo-pod", "big-computer", "hanging-terminal", "server-gabinetes", "small-terminal", "elevator", "elevator-track"):
        target = {"server-gabinetes": "ServerCabinets"}.get(name, "".join(p.title() for p in name.split("-")))
        prop = Image.open(os.path.join(props, f"{name}.png")).convert("RGBA")
        # Amber screens would read as danger; lab screens glow with energy cyan instead.
        prop = hue_shift(prop, *AMBER_TO_CYAN)
        prop.save(out("Ansimuz", "SideviewSciFi", "Lab", f"{target}.png"))

    machinery = os.path.join(env, "sci-fi-interior-paltform", "PNG")
    shutil.copy(os.path.join(machinery, "background.png"), out("Ansimuz", "SideviewSciFi", "Core", "Machinery_Back.png"))
    shutil.copy(os.path.join(machinery, "tile-set-sci-fi-interior-platform.png"),
                out("Ansimuz", "SideviewSciFi", "Core", "Machinery_Tileset.png"))

    # Security wing: the warm bulkheads become cold steel blue (warm is reserved for danger).
    bulkhead = os.path.join(env, "bulkhead-walls", "PNG")
    for name in ("bg-wall-with-supports", "bg-wall", "floor", "foreground"):
        piece = warm_to_steel(Image.open(os.path.join(bulkhead, f"{name}.png")))
        piece.save(out("Ansimuz", "SideviewSciFi", "Security", f"Bulkhead_{name}.png"))

    shutil.copy(os.path.join(root, "public-license.txt"), out("Ansimuz", "SideviewSciFi", "LICENSE.txt"))


def kenney(src):
    base = os.path.join(src, "kenney")

    particles = os.path.join(base, "kenney_particle-pack", "PNG (Transparent)")
    for name, size in (("spark_04", 64), ("spark_05", 64), ("spark_06", 64), ("star_04", 64), ("star_06", 64),
                       ("light_01", 128), ("light_03", 128), ("circle_05", 128), ("flare_01", 128),
                       ("smoke_04", 128), ("smoke_07", 128), ("trace_01", 128), ("twirl_02", 128)):
        Image.open(os.path.join(particles, f"{name}.png")).convert("RGBA").resize((size, size), Image.LANCZOS) \
            .save(out("Kenney", "Particles", f"{name}.png"))
    shutil.copy(os.path.join(base, "kenney_particle-pack", "License.txt"), out("Kenney", "Particles", "LICENSE.txt"))

    fonts = os.path.join(base, "kenney_kenney-fonts")
    for name in ("Kenney Future.ttf", "Kenney Future Narrow.ttf"):
        found = [os.path.join(d, name) for d, _, files in os.walk(fonts) if name in files]
        shutil.copy(found[0], out("Kenney", "Fonts", name.replace(" ", "")))
    license_file = [os.path.join(d, f) for d, _, files in os.walk(fonts) for f in files if f.lower().startswith("license")]
    shutil.copy(license_file[0], out("Kenney", "Fonts", "LICENSE.txt"))

    sounds = {
        "sci-fi-sounds": ["forceField_000", "forceField_002", "forceField_004", "explosionCrunch_000",
                          "explosionCrunch_002", "lowFrequency_explosion_000", "spaceEngineLow_000",
                          "computerNoise_000", "doorOpen_001", "laserSmall_000", "laserLarge_000", "impactMetal_002"],
        "impact-sounds": ["footstep_concrete_000", "footstep_concrete_001", "footstep_concrete_002",
                          "footstep_concrete_003", "footstep_concrete_004", "impactSoft_medium_000",
                          "impactGlass_heavy_002", "impactGlass_medium_001", "impactPunch_medium_000"],
        "interface-sounds": ["select_002", "confirmation_002", "back_002", "open_002", "close_002",
                             "error_004", "glitch_002", "switch_005"],
        "digital-audio": ["phaseJump2", "powerUp2", "powerUp5", "phaserUp3", "zapThreeToneUp", "lowDown"],
    }
    for pack, names in sounds.items():
        pack_dir = os.path.join(base, f"kenney_{pack}")
        for name in names:
            found = [os.path.join(d, f"{name}.ogg") for d, _, files in os.walk(pack_dir) if f"{name}.ogg" in files]
            shutil.copy(found[0], out("Kenney", "Audio", f"{name}.ogg"))
    shutil.copy(os.path.join(base, "kenney_sci-fi-sounds", "License.txt"), out("Kenney", "Audio", "LICENSE.txt"))


def music(src):
    tracks = os.path.join(src, "music", "darkscifi", "ogg")
    encode = ["ffmpeg", "-y", "-loglevel", "error", "-i"]
    for name in ("sector", "airy", "pulse", "urgent", "transmission", "victory"):
        subprocess.run(encode + [os.path.join(tracks, f"{name}.ogg"), "-c:a", "libvorbis", "-q:a", "3",
                                 out("SRG774", "DarkSciFiAudio", f"{name}.ogg")], check=True)
    # The 7-minute title piece is cut to a 96 s menu loop with soft edges.
    subprocess.run(encode + [os.path.join(tracks, "title.ogg"), "-t", "96",
                             "-af", "afade=t=in:d=1.5,afade=t=out:st=91:d=5",
                             "-c:a", "libvorbis", "-q:a", "3", out("SRG774", "DarkSciFiAudio", "title.ogg")], check=True)
    with open(out("SRG774", "DarkSciFiAudio", "LICENSE.txt"), "w") as f:
        f.write("Dark Sci-Fi Audio Pack by SRG774\n"
                "https://opengameart.org/content/dark-sci-fi-audio-pack\n"
                "License: CC0 1.0 Universal (public domain dedication).\n"
                "ECHO changes: re-encoded to Ogg Vorbis q3; title.ogg trimmed to 96 s with fades.\n")


if __name__ == "__main__":
    downloads = sys.argv[1]
    packs = {"warped": warped_city, "sideview": sideview_scifi, "kenney": kenney, "music": music}
    for name in sys.argv[2:] or packs:
        packs[name](downloads)
    print("Imported into", os.path.abspath(THIRD))

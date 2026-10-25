"""
Draws ECHO's original pixel art: the pieces no CC0 pack provided in a matching style.
Palette is sampled from the Ansimuz lab corridor so everything sits in one world.

Usage:  python Tools/make_echo_art.py
Output: Assets/Art/Pixel/...
The logo is drawn with Kenney Future, so run Tools/import_assets.py first.
"""
import math
import os
import random
from PIL import Image, ImageChops, ImageDraw, ImageFont

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Pixel")
LOGO_FONT = os.path.join(os.path.dirname(__file__), "..", "Assets", "ThirdParty", "Kenney", "Fonts", "KenneyFuture.ttf")


def hexc(value, alpha=255):
    value = value.lstrip("#")
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), alpha)


# Lab palette (from the corridor art) + ECHO roles.
VOID = hexc("000410")
DEEP = hexc("000A10")
BODY = hexc("00222A")
SEAM = hexc("00161C")
BODY_LIGHT = hexc("002A32")
RIVET = hexc("004A4A")
BEVEL = hexc("003A3A")
LIP_DARK = hexc("00403C")
LIP = hexc("1E8C7C")
EDGE = hexc("39C9AE")      # walkable top edge: teal, dimmer and greener than energy cyan
ENERGY = hexc("36E2FF")
ENERGY_LIGHT = hexc("B4F6FF")
ENERGY_DEEP = hexc("0E7FA8")
METAL_DARK = hexc("0B1E2A")
METAL = hexc("1C3A4C")
METAL_LIGHT = hexc("3B6A84")
EXIT = hexc("3DFF8F")
EXIT_LIGHT = hexc("C8FFDD")
DANGER = hexc("FF5A2A")
DANGER_DARK = hexc("1A0A06")
WHITE = hexc("F1F5FF")
PANEL_FILL = (4, 14, 24, 215)
PANEL_RIM = (0, 4, 16, 200)
PANEL_EDGE = hexc("1F5F74")
PANEL_EDGE_LIGHT = hexc("36A9C4")
CELL_EMPTY_EDGE = hexc("2A5C70")
CELL_EMPTY_FILL = (0, 18, 28, 190)
CELL_LOW = hexc("1FB8E0")
CLEAR = (0, 0, 0, 0)


def save(img, *parts):
    path = os.path.join(OUT, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)


LAB_PANELS = dict(body=BODY, seam=SEAM, light=BODY_LIGHT, rivet=RIVET, bevel=BEVEL,
                  lip_dark=LIP_DARK, lip=LIP, edge=EDGE, deep=DEEP, void=VOID, indicator=None)

# Security wing: the steel of the recolored bulkheads, a pale cold edge.
SECURITY_PANELS = dict(body=hexc("1B1F22"), seam=hexc("14181A"), light=hexc("22282C"), rivet=hexc("40515E"),
                       bevel=hexc("293238"), lip_dark=hexc("343D44"), lip=hexc("526D80"), edge=hexc("8FB3CC"),
                       deep=hexc("0E1113"), void=hexc("07090A"), indicator=None)

# Energy core: deep navy machinery with a periwinkle edge and small energy indicator lights.
CORE_PANELS = dict(body=hexc("141B33"), seam=hexc("0C1124"), light=hexc("1A2340"), rivet=hexc("2F3F6B"),
                   bevel=hexc("212C4F"), lip_dark=hexc("2A3A66"), lip=hexc("4A63A8"), edge=hexc("8EA2FF"),
                   deep=hexc("080C1C"), void=hexc("04060F"), indicator=ENERGY_DEEP)


def panel_block(folder, name, c):
    """48x48, sliced with 16 px borders: lit top edge, bevelled sides, riveted panels."""
    img = Image.new("RGBA", (48, 48), c["body"])
    px = img.load()
    rng = random.Random(7)
    for y in range(48):
        for x in range(48):
            lx, ly = x % 16, y % 16
            color = c["body"]
            if lx == 15 or ly == 15:
                color = c["seam"]
            elif (lx, ly) in ((2, 2), (13, 2), (2, 13), (13, 13)):
                color = c["rivet"]
            elif rng.random() < 0.05:
                color = c["light"]
            px[x, y] = color
    if c["indicator"]:
        for x0 in range(4, 48, 16):
            for x in range(x0, x0 + 3):
                px[x, 24] = c["indicator"]
            px[x0 + 1, 24] = ENERGY
    for x in range(48):
        px[x, 0], px[x, 1], px[x, 2], px[x, 3] = c["edge"], c["lip"], c["lip_dark"], c["deep"]
        px[x, 46], px[x, 47] = c["deep"], c["void"]
    for y in range(1, 46):
        px[0, y], px[1, y] = c["void"], c["bevel"]
        px[46, y], px[47, y] = c["deep"], c["void"]
    px[0, 0], px[47, 0] = c["lip"], c["lip"]
    save(img, folder, name)


def lab_block():
    panel_block("Lab", "LabBlock.png", LAB_PANELS)


def lab_platform():
    """48x16 catwalk, sliced with 16 px side borders: lit edge over a grated underside."""
    img = Image.new("RGBA", (48, 16), CLEAR)
    px = img.load()
    for x in range(48):
        px[x, 0], px[x, 1], px[x, 2] = EDGE, LIP, LIP_DARK
        for y in range(3, 10):
            px[x, y] = METAL_DARK if (x + y) % 4 else METAL
        px[x, 10] = DEEP
        if x % 8 in (0, 1):
            for y in range(11, 14):
                px[x, y] = METAL_DARK
    for y in range(0, 11):
        px[0, y], px[47, y] = VOID, VOID
    save(img, "Lab", "LabPlatform.png")


def hazard_stripe():
    """16x8 diagonal warning stripes for pit edges: the warm danger accent."""
    img = Image.new("RGBA", (16, 8), DANGER_DARK)
    px = img.load()
    for y in range(8):
        for x in range(16):
            if (x + y) % 8 < 4:
                px[x, y] = DANGER
    save(img, "Hazard", "HazardStripe.png")


def pit_glow():
    """32x64 dithered warm glow rising out of pits (fades to transparent at the top)."""
    img = Image.new("RGBA", (32, 64), CLEAR)
    px = img.load()
    bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    for y in range(64):
        strength = (y / 63) ** 1.6
        for x in range(32):
            if strength * 16 > bayer[y % 4][x % 4]:
                px[x, y] = (255, 51 + int(40 * strength), 60, int(70 + 120 * strength))
    save(img, "Hazard", "PitGlow.png")


def energy_cell(frame, frames=4):
    """16x24 energy cell: metal caps, glass core whose glow pulses over the frames."""
    img = Image.new("RGBA", (16, 24), CLEAR)
    px = img.load()
    pulse = 0.5 + 0.5 * math.sin(frame / frames * math.tau)
    for x in range(3, 13):
        for y in (0, 1, 2):
            px[x, y] = METAL_LIGHT if y == 0 else METAL
        for y in (21, 22, 23):
            px[x, y] = METAL if y < 23 else METAL_DARK
    for x in range(4, 12):
        for y in range(3, 21):
            edge = x in (4, 11)
            core_distance = abs(x - 7.5) / 3.5
            if edge:
                color = ENERGY_DEEP
            elif core_distance < 0.35 + 0.25 * pulse:
                color = ENERGY_LIGHT
            else:
                color = ENERGY
            px[x, y] = color
    for y in range(5, 19, 4):
        px[5, y] = ENERGY_LIGHT
    save(img, "Energy", f"EnergyCell_{frame:02d}.png")


def barrier_field(frame, frames=4, base=ENERGY, light=ENERGY_LIGHT, name="BarrierField"):
    """16x64 energy barrier: translucent field with scanlines that scroll over the frames.
    Cyan for dash-breakable walls, exit green for the level's exit door."""
    img = Image.new("RGBA", (16, 64), CLEAR)
    px = img.load()
    offset = frame * 4
    for y in range(64):
        for x in range(16):
            center = 1 - abs(x - 7.5) / 8
            alpha = int(40 + 110 * center)
            color = base
            if (y + offset) % 16 in (0, 1):
                color, alpha = light, min(255, alpha + 80)
            if x in (0, 15):
                color, alpha = light, 220
            px[x, y] = (color[0], color[1], color[2], alpha)
    save(img, "Energy", f"{name}_{frame:02d}.png")


def barrier_emitter():
    """24x8 metal emitter cap that projects the field."""
    img = Image.new("RGBA", (24, 8), CLEAR)
    px = img.load()
    for x in range(24):
        for y in range(8):
            px[x, y] = METAL_LIGHT if y == 0 else METAL if y < 6 else METAL_DARK
    for x in range(8, 16):
        px[x, 6], px[x, 7] = ENERGY, ENERGY_LIGHT
    for x in (0, 23):
        for y in range(8):
            px[x, y] = VOID
    save(img, "Energy", "BarrierEmitter.png")


def depth_shade():
    """4x128 dithered fade from clear to the void color: darkens solid ground below the walkable
    surface so the level reads as a lit edge over deep structure."""
    img = Image.new("RGBA", (4, 128), CLEAR)
    px = img.load()
    bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    for y in range(128):
        strength = min(1.0, y / 96) ** 0.8
        for x in range(4):
            if strength * 16 > bayer[y % 4][x % 4]:
                px[x, y] = (VOID[0], VOID[1], VOID[2], 215)
    save(img, "Lab", "DepthShade.png")


def particle_textures():
    square = Image.new("RGBA", (4, 4), (255, 255, 255, 255))
    save(square, "FX", "PixelSquare.png")

    ring = Image.new("RGBA", (32, 32), CLEAR)
    px = ring.load()
    for y in range(32):
        for x in range(32):
            d = math.hypot(x - 15.5, y - 15.5)
            if 13.5 <= d <= 15.5:
                px[x, y] = (255, 255, 255, 255)
    save(ring, "FX", "Ring.png")


# --- UI ----------------------------------------------------------------------------------------
# Shown at 3 screen pixels per art pixel at 960x540 (the canvas uses 96 reference px per unit).

def ui_panel():
    """16x16 panel, sliced with 6 px borders: translucent navy, dark rim, teal edge lit on the
    top and left, chamfered top-left and bottom-right corners."""
    size = 16
    img = Image.new("RGBA", (size, size), CLEAR)
    px = img.load()

    def inside(x, y, inset):
        chamfer = 4 - inset
        if x < inset or y < inset or x > size - 1 - inset or y > size - 1 - inset:
            return False
        if (x - inset) + (y - inset) < chamfer:
            return False
        return (size - 1 - inset - x) + (size - 1 - inset - y) >= chamfer

    for y in range(size):
        for x in range(size):
            if inside(x, y, 2):
                px[x, y] = PANEL_FILL
            elif inside(x, y, 1):
                px[x, y] = PANEL_EDGE_LIGHT if y <= 3 or x <= 3 else PANEL_EDGE
            elif inside(x, y, 0):
                px[x, y] = PANEL_RIM
    save(img, "UI", "Panel.png")


def ui_select_bar():
    """12x12 highlight behind the selected menu entry, sliced with 4 px borders: cyan wash,
    brighter top and bottom lines, solid bar on the left."""
    img = Image.new("RGBA", (12, 12), CLEAR)
    px = img.load()
    for y in range(12):
        for x in range(12):
            px[x, y] = ENERGY[:3] + (140 if y in (0, 11) else 70,)
        px[0, y], px[1, y] = ENERGY_LIGHT, ENERGY
    save(img, "UI", "SelectBar.png")


def ui_energy_cells():
    """8x14 HUD energy cells: full (cyan, lit stripe) and empty (dark glass, readable outline)."""
    for name, full in (("CellFull", True), ("CellEmpty", False)):
        img = Image.new("RGBA", (8, 14), CLEAR)
        px = img.load()
        for y in range(14):
            for x in range(8):
                edge = x in (0, 7) or y in (0, 13)
                if x in (0, 7) and y in (0, 13):
                    continue
                if full:
                    color = ENERGY_DEEP if edge else ENERGY_LIGHT if x == 2 else CELL_LOW if y >= 10 else ENERGY
                else:
                    color = CELL_EMPTY_EDGE if edge else CELL_EMPTY_FILL
                px[x, y] = color
        save(img, "UI", f"{name}.png")


def ui_icon(name, rows, color, light=None, light_rows=0):
    img = Image.new("RGBA", (len(rows[0]), len(rows)), CLEAR)
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == "#":
                px[x, y] = light if light and y < light_rows else color
    save(img, "UI", f"{name}.png")


def ui_icons():
    # White, tinted at runtime: energy cyan normally, danger red when the meter is empty.
    ui_icon("Bolt", ["....###", "...###.", "..###..", ".######", "######.", "..###..", ".###...", ".##....", "#......"],
            hexc("C8D7E1"), WHITE, 4)
    ui_icon("Chevron", ["#...", "##..", "###.", "####", "###.", "##..", "#..."], WHITE)


def _dilate(mask, steps, diagonal=False):
    offsets = [(1, 0), (-1, 0), (0, 1), (0, -1)]
    if diagonal:
        offsets += [(1, 1), (1, -1), (-1, 1), (-1, -1)]
    for _ in range(steps):
        grown = mask.copy()
        for dx, dy in offsets:
            grown = ImageChops.lighter(grown, ImageChops.offset(mask, dx, dy))
        mask = grown
    return mask


def ui_logo():
    """The ECHO wordmark: hard-edged Kenney Future letters, white over pale cyan with a scan line,
    a dark outline and two cyan rings spreading outward like an echo."""
    font = ImageFont.truetype(LOGO_FONT, 32)
    letters = []
    for ch in "ECHO":
        glyph = Image.new("L", (80, 60), 0)
        draw = ImageDraw.Draw(glyph)
        draw.fontmode = "1"  # no anti-aliasing: pure pixels
        draw.text((2, 2), ch, font=font, fill=255)
        letters.append(glyph.crop(glyph.getbbox()))

    spacing, pad = 5, 7
    height = max(g.height for g in letters)
    width = sum(g.width for g in letters) + spacing * (len(letters) - 1)
    base = Image.new("L", (width + pad * 2, height + pad * 2), 0)
    x = pad
    for glyph in letters:
        base.paste(glyph, (x, pad + height - glyph.height))
        x += glyph.width + spacing

    img = Image.new("RGBA", base.size, CLEAR)
    px = img.load()
    rings = [
        (ImageChops.subtract(_dilate(base, 7, True), _dilate(base, 6, True)), ENERGY[:3] + (70,)),
        (ImageChops.subtract(_dilate(base, 4, True), _dilate(base, 3, True)), ENERGY[:3] + (170,)),
        (ImageChops.subtract(_dilate(base, 1), base), VOID),
    ]
    for mask, color in rings:
        mp = mask.load()
        for y in range(img.height):
            for x in range(img.width):
                if mp[x, y]:
                    px[x, y] = color

    bp = base.load()
    scan_line = pad + int(height * 0.55)
    for y in range(img.height):
        for x in range(img.width):
            if bp[x, y]:
                px[x, y] = ENERGY_DEEP if y == scan_line else WHITE if y < scan_line else ENERGY_LIGHT
    save(img, "UI", "Logo.png")


# --- biomes 2-4 ---------------------------------------------------------------------------------

def biome_blocks():
    panel_block("Security", "SecurityBlock.png", SECURITY_PANELS)
    panel_block("Core", "CoreBlock.png", CORE_PANELS)


def conduit(frame, frames=4):
    """8x32 vertical energy conduit for the core's walls: metal pipe, cyan core whose bright
    pulses travel upward over the frames (tiles seamlessly)."""
    img = Image.new("RGBA", (8, 32), CLEAR)
    px = img.load()
    for y in range(32):
        px[0, y], px[7, y] = VOID, VOID
        px[1, y], px[6, y] = METAL, METAL_DARK
        px[2, y], px[5, y] = ENERGY_DEEP, ENERGY_DEEP
        bright = (y + frame * 4) % 16 < 3
        px[3, y] = ENERGY_LIGHT if bright else ENERGY
        px[4, y] = ENERGY if bright else ENERGY_DEEP
        if y % 16 in (0, 1):
            for x in range(1, 7):
                px[x, y] = METAL_LIGHT if y % 16 == 0 else METAL
    save(img, "Core", f"Conduit_{frame:02d}.png")


def reactor(name, core, core_light, core_deep):
    """128x128 reactor for the Guardian's arena: bolted housing, vented ring, dark glass and a
    dithered glowing core. Drawn calm (energy) and overloaded (danger)."""
    size = 128
    img = Image.new("RGBA", (size, size), CLEAR)
    px = img.load()
    bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - c, y - c)
            angle = math.degrees(math.atan2(y - c, x - c)) % 360
            if d > 63.5:
                continue
            if d > 60:
                color = METAL_DARK if d > 62.5 else METAL_LIGHT if y < c else METAL
            elif d > 56:
                color = METAL
                if any(abs(angle - a) < 2.2 for a in range(0, 360, 45)) and 57 < d < 59:
                    color = METAL_LIGHT  # bolts
            elif d > 44:
                vent = int(angle / 15) % 2 == 0 and 46 < d < 54
                color = core_deep if vent else METAL_DARK
            elif d > 41:
                color = VOID
            elif d > 30:
                color = hexc("0B1420")
                if (x + y) % 9 == 0 and y < c:
                    color = hexc("1D3346")  # glass glint
            else:
                t = d / 30
                level = (1 - t) * 16
                b = bayer[y % 4][x % 4]
                color = core_light if level > b + 9 else core if level > b * 0.45 else core_deep
            px[x, y] = color
    save(img, "Core", f"{name}.png")


def scanner_lamp():
    """12x6 ceiling scanner for the security wing: metal housing with a red lens."""
    img = Image.new("RGBA", (12, 6), CLEAR)
    px = img.load()
    for x in range(12):
        for y in range(4):
            px[x, y] = METAL_LIGHT if y == 0 else METAL if y < 3 else METAL_DARK
    for x in range(4, 8):
        px[x, 4] = DANGER
        px[x, 5] = hexc("FFB09A")
    px[0, 0], px[11, 0] = CLEAR, CLEAR
    save(img, "Security", "ScannerLamp.png")


# --- gameplay objects (M3) ----------------------------------------------------------------------

CHECK_OFF = hexc("3A4652")
CHECK_ON = hexc("F1F5FF")
CHECK_GLOW = hexc("B8C8D8")


def checkpoint_beacon():
    """12x28 checkpoint beacon: a metal post with a lamp head. Dark when unused; white (the
    interface color, since checkpoints are neither energy, danger nor exit) once activated."""
    for name, lamp, glow in (("Checkpoint_Off", CHECK_OFF, METAL_DARK), ("Checkpoint_On", CHECK_ON, CHECK_GLOW)):
        img = Image.new("RGBA", (12, 28), CLEAR)
        px = img.load()
        for y in range(10, 28):
            for x in range(4, 8):
                px[x, y] = METAL_LIGHT if x == 4 else METAL if x < 7 else METAL_DARK
        for x in range(2, 10):
            px[x, 26], px[x, 27] = METAL, METAL_DARK  # foot
        for y in range(1, 10):
            for x in range(2, 10):
                edge = x in (2, 9) or y in (1, 9)
                px[x, y] = METAL_DARK if edge else (glow if x in (3, 8) or y in (2, 8) else lamp)
        px[5, 0], px[6, 0] = METAL_LIGHT, METAL_LIGHT
        save(img, "Gameplay", f"{name}.png")


LASER = hexc("FF3355")
LASER_CORE = hexc("FFD6DC")
LASER_EDGE = hexc("8A1A22")


def laser_parts():
    """Laser gate: a 16x8 emitter (metal, red lens) and a 8x16 beam tile in 3 shimmering frames."""
    img = Image.new("RGBA", (16, 8), CLEAR)
    px = img.load()
    for x in range(16):
        for y in range(6):
            px[x, y] = METAL_LIGHT if y == 0 else METAL if y < 5 else METAL_DARK
    for x in range(5, 11):
        px[x, 6] = LASER
        px[x, 7] = LASER_CORE if 6 <= x <= 9 else LASER
    for x in (0, 15):
        for y in range(6):
            px[x, y] = VOID
    save(img, "Gameplay", "LaserEmitter.png")

    for frame in range(3):
        img = Image.new("RGBA", (8, 16), CLEAR)
        px = img.load()
        for y in range(16):
            wobble = (y + frame * 5) % 16 < 3
            px[1, y] = (LASER_EDGE[0], LASER_EDGE[1], LASER_EDGE[2], 140)
            px[6, y] = (LASER_EDGE[0], LASER_EDGE[1], LASER_EDGE[2], 140)
            px[2, y] = LASER
            px[5, y] = LASER
            px[3, y] = LASER_CORE if not wobble else hexc("FFFFFF")
            px[4, y] = LASER_CORE
        save(img, "Gameplay", f"LaserBeam_{frame:02d}.png")


# --- security wing objects (Level 2) -----------------------------------------------------------

STEEL_DARK = hexc("14181A")
STEEL = hexc("343D44")
STEEL_LIGHT = hexc("64859C")
STEEL_EDGE = hexc("8FB3CC")


def turret_bolt():
    """8x4 turret shot: a hot red slug with a white core (danger is always warm)."""
    img = Image.new("RGBA", (8, 4), CLEAR)
    px = img.load()
    for x in range(8):
        for y in range(4):
            if (x in (0, 7)) and y in (0, 3):
                continue
            px[x, y] = LASER_EDGE if x == 0 else LASER if y in (0, 3) or x < 3 else LASER_CORE
    save(img, "Gameplay", "TurretBolt.png")


def console():
    """16x24 wall console that opens doors and switches lasers off: red screen while locked,
    green (the way-out color) once used."""
    for name, screen, glow in (("Console_Locked", DANGER, hexc("8A1A22")), ("Console_Open", EXIT, hexc("1E7A48"))):
        img = Image.new("RGBA", (16, 24), CLEAR)
        px = img.load()
        for y in range(24):
            for x in range(16):
                edge = x in (0, 15) or y in (0, 23)
                px[x, y] = STEEL_DARK if edge else STEEL_LIGHT if y == 1 else STEEL
        for y in range(4, 12):
            for x in range(3, 13):
                px[x, y] = glow if x in (3, 12) or y in (4, 11) else screen
        for x in (5, 8, 11):  # keys
            for y in (15, 16):
                px[x, y] = STEEL_EDGE
        for x in range(2, 14):
            px[x, 20], px[x, 21] = STEEL_LIGHT, STEEL_DARK
        save(img, "Gameplay", f"{name}.png")


def shutter():
    """16x16 security shutter tile (tiled up the door): steel slats with a red lock stripe."""
    img = Image.new("RGBA", (16, 16), STEEL)
    px = img.load()
    for y in range(16):
        for x in range(16):
            if y % 4 == 0:
                px[x, y] = STEEL_LIGHT
            elif y % 4 == 3:
                px[x, y] = STEEL_DARK
        px[0, y], px[15, y] = STEEL_DARK, STEEL_DARK
    for y in range(6, 10):
        px[7, y], px[8, y] = LASER, LASER_CORE if y in (7, 8) else LASER
    save(img, "Gameplay", "Shutter.png")


def security_platform():
    """48x16 steel girder platform, sliced with 16 px side borders (like the lab catwalk)."""
    img = Image.new("RGBA", (48, 16), CLEAR)
    px = img.load()
    for x in range(48):
        px[x, 0], px[x, 1], px[x, 2] = STEEL_EDGE, STEEL_LIGHT, STEEL
        for y in range(3, 10):
            px[x, y] = STEEL_DARK if (x + y) % 4 else STEEL
        px[x, 10] = hexc("0E1113")
        if x % 8 in (0, 1):
            for y in range(11, 14):
                px[x, y] = STEEL_DARK
    for y in range(0, 11):
        px[0, y], px[47, y] = hexc("07090A"), hexc("07090A")
    save(img, "Security", "SecurityPlatform.png")


CORE_EDGE = hexc("8EA2FF")
CORE_LIP = hexc("4A63A8")
CORE_BODY = hexc("141B33")
CORE_BODY_LIGHT = hexc("1A2340")
CORE_SEAM = hexc("0C1124")
CORE_DEEP = hexc("080C1C")
CRACK_EMBER = hexc("7A2E1E")   # calm cracks: a dim warm glow, since warm means danger
CRACK_HOT = DANGER
CRACK_HOT_LIGHT = hexc("FFD0C0")

# Crack paths across the plate body as (x, y) pixels, one per side border and one in the tiled middle.
PLATE_CRACKS = [
    [(9, 1), (9, 2), (10, 3), (10, 4), (11, 5), (10, 6), (10, 7), (11, 8)],
    [(22, 1), (23, 2), (23, 3), (22, 4), (22, 5), (23, 6), (24, 7), (24, 8), (25, 4), (26, 5)],
    [(38, 1), (37, 2), (37, 3), (38, 4), (38, 5), (37, 6), (36, 7), (36, 8)],
]


def unstable_platform(hot):
    """48x16 cracked core plate (sliced with 16 px side borders). Calm: dim ember cracks in the
    navy plate. Hot (flashed while it gives way): the cracks flare danger orange."""
    img = Image.new("RGBA", (48, 16), CLEAR)
    px = img.load()
    for x in range(48):
        px[x, 0], px[x, 1] = CORE_EDGE, CORE_LIP
        for y in range(2, 9):
            px[x, y] = CORE_BODY_LIGHT if (x + 2 * y) % 5 == 0 else CORE_BODY
        px[x, 9] = CORE_SEAM
        if x % 16 in (6, 7, 8, 9):  # hanging keel under each section
            for y in range(10, 13):
                px[x, y] = CORE_DEEP if y < 12 else CORE_SEAM
    for x in (0, 47):
        for y in range(0, 10):
            px[x, y] = CORE_DEEP
    for crack in PLATE_CRACKS:
        for x, y in crack:
            px[x, y] = CRACK_HOT if hot else CRACK_EMBER
            if hot and y in (3, 4, 5):
                px[x, y] = CRACK_HOT_LIGHT
        top_x = crack[0][0]
        px[top_x, 0] = CRACK_HOT if hot else CRACK_EMBER  # the crack nicks the top edge
    save(img, "Core", "UnstableGlowPlatform.png" if hot else "UnstablePlatform.png")


def lock_emitter():
    """24x8 metal emitter cap for a core lock: like the barrier emitter, with a steel-blue lens."""
    img = Image.new("RGBA", (24, 8), CLEAR)
    px = img.load()
    for x in range(24):
        for y in range(8):
            px[x, y] = METAL_LIGHT if y == 0 else METAL if y < 6 else METAL_DARK
    for x in range(8, 16):
        px[x, 6], px[x, 7] = CORE_EDGE, WHITE
    for x in (0, 23):
        for y in range(8):
            px[x, y] = VOID
    save(img, "Energy", "LockEmitter.png")


def lock_lights():
    """Core lock readout: a 12 px wide metal housing with 1-3 slots, and 8x8 charge lights
    (empty slot, or energy cyan for a charge the player holds)."""
    for slots in (1, 2, 3):
        height = 2 + 10 * slots
        img = Image.new("RGBA", (12, height), METAL)
        px = img.load()
        for y in range(height):
            px[0, y], px[11, y] = METAL_LIGHT, METAL_DARK
        for x in range(12):
            px[x, 0], px[x, height - 1] = METAL_LIGHT, METAL_DARK
        for i in range(slots):
            top = 2 + 10 * i
            for y in range(top, top + 8):
                for x in range(2, 10):
                    px[x, y] = VOID
        save(img, "Gameplay", f"LockPanel_{slots}.png")

    for name, fill, core, edge in (("LockLight_Off", CELL_EMPTY_FILL, CELL_EMPTY_FILL, CELL_EMPTY_EDGE),
                                   ("LockLight_On", ENERGY, ENERGY_LIGHT, ENERGY_DEEP)):
        img = Image.new("RGBA", (8, 8), CLEAR)
        px = img.load()
        for y in range(8):
            for x in range(8):
                border = x in (0, 7) or y in (0, 7)
                inner = 2 <= x <= 5 and 2 <= y <= 5
                px[x, y] = edge if border else core if inner and name.endswith("On") else fill
        save(img, "Gameplay", f"{name}.png")


def guardian():
    """The Guardian: a 64x30 armored half-shell (drawn for the top, mirrored for the bottom with
    thrusters), a 30x30 core in calm cyan and overloaded red, and a white 44x2 seam tinted at
    runtime (red while it attacks). The halves part to show the core."""
    c = 31.5
    for half in ("Top", "Bottom"):
        img = Image.new("RGBA", (64, 30), CLEAR)
        px = img.load()
        for y in range(30):
            for x in range(64):
                # Distance from the middle of the seam; the dome is slightly flattened.
                dy = (29.5 - y) if half == "Top" else (y + 0.5)
                d = math.hypot(x - c, dy * 1.12)
                if d > 31.5:
                    continue
                if d > 30:
                    color = METAL_DARK
                elif d > 27.5:
                    color = METAL_LIGHT if (half == "Top" and x < c) else METAL
                elif d > 26:
                    color = METAL_DARK
                elif abs(d - 17) < 0.7:
                    color = METAL_DARK  # inner plate seam
                else:
                    angle = math.degrees(math.atan2(dy, x - c))
                    radial = d > 17 and any(abs(angle - a) < 1.6 for a in (45, 90, 135))
                    color = METAL_DARK if radial else (METAL if (x + y) % 6 else METAL_LIGHT)
                px[x, y] = color
        for a_deg in (25, 65, 115, 155):  # rivets on the outer band
            rx = int(round(c + math.cos(math.radians(a_deg)) * 28.7))
            r_up = int(round(math.sin(math.radians(a_deg)) * 28.7 / 1.12))
            ry = 29 - r_up if half == "Top" else r_up
            if 0 <= ry < 30:
                px[rx, ry] = METAL_LIGHT
        if half == "Top":  # sensor fin with a red tip
            for y in range(0, 3):
                for x in range(30, 34):
                    px[x, y] = METAL_LIGHT if x == 30 else METAL
            px[31, 0], px[32, 0] = DANGER, DANGER
        else:  # three thrusters with a cyan glow
            for tx in (18, 30, 42):
                for y in range(23, 28):
                    for x in range(tx, tx + 4):
                        px[x, y] = METAL_DARK if x in (tx, tx + 3) else VOID
                px[tx + 1, 27], px[tx + 2, 27] = ENERGY_DEEP, ENERGY_DEEP
        save(img, "Core", f"Guardian_Shell{half}.png")

    bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    for name, core, light, deep in (("Guardian_CoreCalm", ENERGY, ENERGY_LIGHT, ENERGY_DEEP),
                                    ("Guardian_CoreOverload", DANGER, hexc("FFD0C0"), hexc("8A1A22"))):
        img = Image.new("RGBA", (30, 30), CLEAR)
        px = img.load()
        cc = 14.5
        for y in range(30):
            for x in range(30):
                d = math.hypot(x - cc, y - cc)
                if d > 14.5:
                    continue
                if d > 13:
                    color = METAL_DARK
                elif d > 11.5:
                    color = deep
                else:
                    level = (1 - d / 11.5) * 16
                    b = bayer[y % 4][x % 4]
                    color = light if level > b + 8 else core if level > b * 0.4 else deep
                px[x, y] = color
        for y in range(9, 21):  # a slit pupil, so the core reads as an eye
            px[14, y] = px[15, y] = VOID if name.endswith("Overload") else deep
        save(img, "Core", f"{name}.png")

    save(Image.new("RGBA", (44, 2), WHITE), "Core", "Guardian_Seam.png")


def shockwave(frame):
    """20x28 floor wave: a crest of danger-orange energy, brightest at its front (it faces right
    and is mirrored at runtime)."""
    img = Image.new("RGBA", (20, 28), CLEAR)
    px = img.load()
    for x in range(20):
        t = x / 19  # 0 at the tail, 1 at the front
        height = int(6 + 20 * math.sin(t * math.pi * 0.92) ** 1.4) + (2 if (x + frame * 3) % 5 == 0 else 0)
        height = min(28, height)
        for y in range(28 - height, 28):
            depth = (y - (28 - height)) / max(1, height)
            color = hexc("FFD0C0") if depth < 0.12 else DANGER if depth < 0.45 else hexc("8A1A22")
            px[x, y] = (color[0], color[1], color[2], int(255 * (0.35 + 0.65 * t)))
    save(img, "Core", f"Shockwave_{frame:02d}.png")


def wall_port():
    """12x10 wall-mounted bolt port for the Guardian's arena (pale lens, tinted red as it fires)."""
    img = Image.new("RGBA", (12, 10), CLEAR)
    px = img.load()
    for y in range(10):
        for x in range(12):
            edge = x in (0, 11) or y in (0, 9)
            px[x, y] = METAL_DARK if edge else METAL if y > 2 else METAL_LIGHT
    for y in range(3, 7):
        for x in range(4, 8):
            px[x, y] = WHITE if y == 4 and x in (5, 6) else hexc("C8C8D0")
    save(img, "Core", "WallPort.png")


def ui_arrowhead():
    """9x9 white arrowhead pointing right, for the concept deck's diagrams (tinted at runtime)."""
    img = Image.new("RGBA", (9, 9), CLEAR)
    px = img.load()
    for y in range(9):
        reach = 8 - abs(y - 4) * 2
        for x in range(0, reach + 1):
            px[x, y] = WHITE
    save(img, "UI", "Arrowhead.png")


def ui_boss_cells():
    """14x6 boss health segments: full (danger red, lit top line) and empty (dark glass)."""
    for name, full in (("BossCellFull", True), ("BossCellEmpty", False)):
        img = Image.new("RGBA", (14, 6), CLEAR)
        px = img.load()
        for y in range(6):
            for x in range(14):
                if x in (0, 13) and y in (0, 5):
                    continue
                edge = x in (0, 13) or y in (0, 5)
                if full:
                    color = hexc("8A1A22") if edge else hexc("FFD0C0") if y == 1 else DANGER
                else:
                    color = CELL_EMPTY_EDGE if edge else CELL_EMPTY_FILL
                px[x, y] = color
        save(img, "UI", f"{name}.png")


if __name__ == "__main__":
    lab_block()
    lab_platform()
    hazard_stripe()
    pit_glow()
    for i in range(4):
        energy_cell(i)
        barrier_field(i)
        barrier_field(i, base=EXIT, light=EXIT_LIGHT, name="ExitField")
    barrier_emitter()
    depth_shade()
    particle_textures()
    ui_panel()
    ui_select_bar()
    ui_energy_cells()
    ui_icons()
    ui_logo()
    biome_blocks()
    for i in range(4):
        conduit(i)
    reactor("Reactor_Calm", ENERGY, ENERGY_LIGHT, ENERGY_DEEP)
    reactor("Reactor_Overload", DANGER, hexc("FFD0C0"), hexc("8A1A22"))
    scanner_lamp()
    checkpoint_beacon()
    laser_parts()
    turret_bolt()
    console()
    shutter()
    security_platform()
    unstable_platform(False)
    unstable_platform(True)
    for i in range(4):
        barrier_field(i, base=CORE_EDGE, light=WHITE, name="LockField")
    lock_emitter()
    lock_lights()
    guardian()
    for i in range(3):
        shockwave(i)
    wall_port()
    ui_boss_cells()
    ui_arrowhead()
    print("Wrote ECHO pixel art to", os.path.abspath(OUT))

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


def lab_block():
    """48x48, sliced with 16 px borders: lit top edge, bevelled sides, riveted panels."""
    img = Image.new("RGBA", (48, 48), BODY)
    px = img.load()
    rng = random.Random(7)
    for y in range(48):
        for x in range(48):
            lx, ly = x % 16, y % 16
            color = BODY
            if lx == 15 or ly == 15:
                color = SEAM
            elif (lx, ly) in ((2, 2), (13, 2), (2, 13), (13, 13)):
                color = RIVET
            elif rng.random() < 0.05:
                color = BODY_LIGHT
            px[x, y] = color
    for x in range(48):
        px[x, 0], px[x, 1], px[x, 2], px[x, 3] = EDGE, LIP, LIP_DARK, DEEP
        px[x, 46], px[x, 47] = DEEP, VOID
    for y in range(1, 46):
        px[0, y], px[1, y] = VOID, BEVEL
        px[46, y], px[47, y] = DEEP, VOID
    px[0, 0], px[47, 0] = LIP, LIP
    save(img, "Lab", "LabBlock.png")


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
    print("Wrote ECHO pixel art to", os.path.abspath(OUT))

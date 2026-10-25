"""
Draws ECHO's UML diagrams: the activity diagram (the whole game flow) and the use case diagram.
Every shape is placed by hand below; the script measures the text, draws, and warns when
shapes overlap or two lines cross.

Usage:  python Tools/make_diagrams.py
Output: Docs/Diagrams/*.svg, plus *.png when Edge or Chrome is installed (it rasterizes the SVG).
"""
import math
import os
import shutil
import subprocess
import sys
import tempfile
from xml.sax.saxutils import escape

try:
    from PIL import ImageFont
except ImportError:  # text sizes are estimated instead
    ImageFont = None

OUT = os.path.join(os.path.dirname(__file__), "..", "Docs", "Diagrams")

INK = "#1E293B"
LINE = "#334155"
MUTED = "#64748B"
PAPER = "#FFFFFF"
LANE = "#F4F7FB"
LANE_EDGE = "#CBD5E1"
HEADER = "#0B1220"          # the game's void navy
HEADER_NOTE = "#9FB6CC"
GAME_FILL = "#FFFFFF"
PLAYER_FILL = "#D6F5FC"     # a tint of the game's energy cyan
PLAYER_EDGE = "#0B7F9E"
DECISION_FILL = "#FFF1BF"

TEXT = 15
GUARD = 13.5
LINE_HEIGHT = 19

_fonts = {}


def text_width(string, size, bold=False):
    if ImageFont is not None:
        try:
            if bold not in _fonts:
                _fonts[bold] = ImageFont.truetype("arialbd.ttf" if bold else "arial.ttf", 100)
            return _fonts[bold].getlength(string) * size / 100
        except OSError:
            pass
    return len(string) * size * (0.58 if bold else 0.53)


def num(value):
    return f"{value:.1f}".rstrip("0").rstrip(".")


def point(p):
    return f"{num(p[0])},{num(p[1])}"


def unit(a, b):
    length = math.hypot(b[0] - a[0], b[1] - a[1]) or 1.0
    return ((b[0] - a[0]) / length, (b[1] - a[1]) / length)


class Node:
    """A placed shape. Lines attach to its four ports, or to its outline towards a point."""

    def __init__(self, cx, cy, w, h, round_shape=False):
        self.cx, self.cy, self.w, self.h = cx, cy, w, h
        self.round = round_shape

    left = property(lambda self: self.cx - self.w / 2)
    right = property(lambda self: self.cx + self.w / 2)
    top = property(lambda self: self.cy - self.h / 2)
    bottom = property(lambda self: self.cy + self.h / 2)
    north = property(lambda self: (self.cx, self.top))
    south = property(lambda self: (self.cx, self.bottom))
    west = property(lambda self: (self.left, self.cy))
    east = property(lambda self: (self.right, self.cy))
    box = property(lambda self: (self.left, self.top, self.right, self.bottom))

    def edge_towards(self, target):
        """Where a straight line from the centre to the target leaves the shape."""
        dx, dy = target[0] - self.cx, target[1] - self.cy
        if self.round:
            scale = 1.0 / math.hypot(dx / (self.w / 2), dy / (self.h / 2))
        else:
            scale = min(self.w / 2 / abs(dx) if dx else math.inf, self.h / 2 / abs(dy) if dy else math.inf)
        return (self.cx + dx * scale, self.cy + dy * scale)

    def contains(self, p, margin=0.0):
        if self.round:
            return math.hypot((p[0] - self.cx) / (self.w / 2 + margin), (p[1] - self.cy) / (self.h / 2 + margin)) <= 1.0
        return (self.left - margin <= p[0] <= self.right + margin
                and self.top - margin <= p[1] <= self.bottom + margin)


class Diagram:
    def __init__(self, width, height, name):
        self.width, self.height, self.name = width, height, name
        self.back, self.lines, self.shapes, self.words = [], [], [], []
        # Kept for the layout check.
        self.nodes, self.labels, self.segments = [], [], []
        self.line_count = 0

    # --- text --------------------------------------------------------------------------------

    def text(self, x, y, string, size=TEXT, anchor="middle", bold=False, fill=INK, track=True,
             halo=False, spacing=0.0, on_line=None):
        width = text_width(string, size, bold) + spacing * len(string)
        left = x - width / 2 if anchor == "middle" else x - width if anchor == "end" else x
        if track:
            self.labels.append((string, (left, y - size * 0.78, left + width, y + size * 0.22), on_line))
        extra = ""
        if bold:
            extra += ' font-weight="bold"'
        if spacing:
            extra += f' letter-spacing="{num(spacing)}"'
        if halo:
            extra += f' paint-order="stroke" stroke="{PAPER}" stroke-width="5" stroke-linejoin="round"'
        self.words.append(f'<text x="{num(x)}" y="{num(y)}" font-size="{num(size)}" text-anchor="{anchor}" '
                          f'fill="{fill}"{extra}>{escape(string)}</text>')
        return width

    def title(self, heading, note):
        self.text(32, 42, heading, 26, "start", bold=True, track=False)
        self.text(32, 68, note, 14.5, "start", fill=MUTED, track=False)

    def paragraph(self, node, string):
        lines = string.split("\n")
        for i, line in enumerate(lines):
            self.text(node.cx, node.cy + (i - (len(lines) - 1) / 2) * LINE_HEIGHT + 5.3, line, track=False)

    # --- activity diagram shapes -------------------------------------------------------------

    def lane(self, x0, x1, y0, y1, heading, note=None):
        self.back.append(f'<rect x="{x0}" y="{y0}" width="{x1 - x0}" height="{y1 - y0}" fill="{LANE}" '
                         f'stroke="{LANE_EDGE}"/>')
        self.back.append(f'<rect x="{x0}" y="{y0}" width="{x1 - x0}" height="30" fill="{HEADER}"/>')
        width = self.text(x0 + 16, y0 + 20, heading, 13, "start", bold=True, fill=PAPER, track=False, spacing=0.8)
        if note:
            self.text(x0 + 30 + width, y0 + 20, note, 13, "start", fill=HEADER_NOTE, track=False)

    def action(self, string, y, cx=None, left=None, player=False):
        lines = string.split("\n")
        w = math.ceil((max(text_width(line, TEXT) for line in lines) + 30) / 2) * 2
        h = max(40, len(lines) * LINE_HEIGHT + 20)
        node = Node(cx if cx is not None else left + w / 2, y, w, h)
        fill, edge = (PLAYER_FILL, PLAYER_EDGE) if player else (GAME_FILL, LINE)
        self.shapes.append(f'<rect x="{num(node.left)}" y="{num(node.top)}" width="{w}" height="{h}" rx="13" '
                           f'fill="{fill}" stroke="{edge}" stroke-width="1.6"/>')
        self.paragraph(node, string)
        self.nodes.append(node)
        return node

    def diamond(self, cx, cy, r=20, merge=False):
        node = Node(cx, cy, r * 2, r * 2)
        corners = " ".join(point(p) for p in (node.north, node.east, node.south, node.west))
        self.shapes.append(f'<polygon points="{corners}" fill="{PAPER if merge else DECISION_FILL}" '
                           f'stroke="{LINE}" stroke-width="1.6" stroke-linejoin="round"/>')
        self.nodes.append(node)
        return node

    def initial(self, cx, cy, r=11):
        node = Node(cx, cy, r * 2, r * 2, True)
        self.shapes.append(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{INK}"/>')
        self.nodes.append(node)
        return node

    def final(self, cx, cy, r=13):
        node = Node(cx, cy, r * 2, r * 2, True)
        self.shapes.append(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{PAPER}" stroke="{INK}" stroke-width="1.6"/>'
                           f'<circle cx="{cx}" cy="{cy}" r="{r - 5}" fill="{INK}"/>')
        self.nodes.append(node)
        return node

    def flow(self, *points, guard=None, at=None, anchor="middle", start=None, end="arrow"):
        """A control flow along the points (right angles only). A guard is the [condition] on it.
        "join" marks an end that meets another flow's line instead of a shape."""
        points = [tuple(p) for p in points]
        for a, b in zip(points, points[1:]):
            if abs(a[0] - b[0]) > 0.01 and abs(a[1] - b[1]) > 0.01:
                print(f"  ! {self.name}: slanted flow {a} -> {b}")
        self.polyline(points)
        if end == "arrow":
            self.lines.append(f'<polygon points="{arrowhead(points[-1], points[-2])}" fill="{LINE}"/>')
        for where, kind in ((points[0], start), (points[-1], end)):
            if kind == "join":
                self.lines.append(f'<circle cx="{num(where[0])}" cy="{num(where[1])}" r="3.2" fill="{LINE}"/>')
        if guard:
            for i, line in enumerate(guard.split("\n")):
                self.text(at[0], at[1] + i * 16, line, GUARD, anchor)

    def polyline(self, points, dashed=False):
        self.line_count += 1
        for a, b in zip(points, points[1:]):
            self.segments.append((self.line_count, a, b, points[0], points[-1]))
        path = [f"M{point(points[0])}"]
        for before, corner, after in zip(points, points[1:], points[2:]):
            r = min(8.0, math.dist(before, corner) / 2, math.dist(corner, after) / 2)
            u, v = unit(corner, before), unit(corner, after)
            path.append(f"L{point((corner[0] + u[0] * r, corner[1] + u[1] * r))} "
                        f"Q{point(corner)} {point((corner[0] + v[0] * r, corner[1] + v[1] * r))}")
        path.append(f"L{point(points[-1])}")
        dash = ' stroke-dasharray="7 5"' if dashed else ""
        self.lines.append(f'<path d="{" ".join(path)}" fill="none" stroke="{LINE}" stroke-width="1.6" '
                          f'stroke-linejoin="round"{dash}/>')

    # --- use case diagram shapes -------------------------------------------------------------

    def boundary(self, x0, y0, x1, y1, heading):
        self.back.append(f'<rect x="{x0}" y="{y0}" width="{x1 - x0}" height="{y1 - y0}" rx="6" fill="{LANE}" '
                         f'stroke="{LINE}" stroke-width="1.6"/>')
        self.text((x0 + x1) / 2, y0 + 30, heading, 18, bold=True, track=False)

    def actor(self, cx, cy, name):
        node = Node(cx, cy, 56, 96)
        head, neck, hip = cy - 34, cy - 22, cy + 14
        self.shapes.append(
            f'<g fill="none" stroke="{INK}" stroke-width="2" stroke-linecap="round">'
            f'<circle cx="{cx}" cy="{head}" r="12" fill="{PAPER}"/>'
            f'<path d="M{cx},{neck} V{hip} M{cx - 22},{cy - 8} H{cx + 22} '
            f'M{cx},{hip} L{cx - 18},{cy + 44} M{cx},{hip} L{cx + 18},{cy + 44}"/></g>')
        for i, line in enumerate(name.split("\n")):
            self.text(cx, cy + 68 + i * 19, line, 16, bold=True)
        self.nodes.append(node)
        return node

    def use_case(self, string, cx, cy, main=False):
        lines = string.split("\n")
        rx = max(88, math.ceil(max(text_width(line, TEXT, main) for line in lines) / 2 + 30))
        ry = 30 if len(lines) == 1 else 39
        node = Node(cx, cy, rx * 2, ry * 2, True)
        fill, edge, weight = (PLAYER_FILL, PLAYER_EDGE, 2.2) if main else (GAME_FILL, LINE, 1.6)
        self.shapes.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{fill}" stroke="{edge}" '
                           f'stroke-width="{weight}"/>')
        for i, line in enumerate(lines):
            self.text(cx, cy + (i - (len(lines) - 1) / 2) * LINE_HEIGHT + 5.3, line, bold=main, track=False)
        self.nodes.append(node)
        return node

    def between(self, a, b):
        return [a.edge_towards((b.cx, b.cy)), b.edge_towards((a.cx, a.cy))]

    def association(self, a, b):
        self.polyline(self.between(a, b))

    def dependency(self, a, b, stereotype, along=0.5):
        """Dashed arrow from a to b: «include» (a always uses b) or «extend» (a may add to b)."""
        p, q = self.between(a, b)
        self.polyline([p, q], dashed=True)
        u = unit(q, p)
        side = (-u[1], u[0])
        wings = [(q[0] + u[0] * 11 + side[0] * s, q[1] + u[1] * 11 + side[1] * s) for s in (5, -5)]
        self.lines.append(f'<path d="M{point(wings[0])} L{point(q)} L{point(wings[1])}" fill="none" '
                          f'stroke="{LINE}" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>')
        x, y = p[0] + (q[0] - p[0]) * along, p[1] + (q[1] - p[1]) * along
        self.text(x, y + 4.5, f"«{stereotype}»", 12.5, fill=MUTED, halo=True, on_line=self.line_count)

    def generalization(self, a, b):
        p, q = self.between(a, b)
        u = unit(q, p)
        base = (q[0] + u[0] * 14, q[1] + u[1] * 14)
        self.polyline([p, base])
        self.lines.append(f'<polygon points="{arrowhead(q, p, 14, 7)}" fill="{PAPER}" stroke="{LINE}" '
                          f'stroke-width="1.6" stroke-linejoin="round"/>')

    # --- output ------------------------------------------------------------------------------

    def svg(self):
        return "\n".join(
            [f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.width}" height="{self.height}" '
             f'viewBox="0 0 {self.width} {self.height}" font-family="Arial, Helvetica, sans-serif">',
             f'<rect width="{self.width}" height="{self.height}" fill="{PAPER}"/>']
            + self.back + self.lines + self.shapes + self.words + ["</svg>", ""])

    def check(self):
        """Warns about overlapping shapes, lines through shapes or labels, and crossing lines."""
        problems = []
        for i, a in enumerate(self.nodes):
            for b in self.nodes[i + 1:]:
                if boxes_overlap(a.box, b.box, 4):
                    problems.append(f"shapes overlap near ({a.cx:.0f}, {a.cy:.0f}) and ({b.cx:.0f}, {b.cy:.0f})")
        for line, p, q, first, last in self.segments:
            for node in self.nodes:
                if node.contains(first, 2) or node.contains(last, 2):
                    continue
                if segment_hits(p, q, node):
                    problems.append(f"line {p} -> {q} runs through the shape at ({node.cx:.0f}, {node.cy:.0f})")
            for string, box, on_line in self.labels:
                if line != on_line and segment_hits_box(p, q, grow(box, -1)):
                    problems.append(f"line {p} -> {q} runs through the label '{string}'")
        for i, (string, box, _) in enumerate(self.labels):
            for node in self.nodes:
                if boxes_overlap(box, node.box, 0):
                    problems.append(f"label '{string}' touches the shape at ({node.cx:.0f}, {node.cy:.0f})")
            for other, other_box, _ in self.labels[i + 1:]:
                if boxes_overlap(box, other_box, 0):
                    problems.append(f"labels '{string}' and '{other}' overlap")
        for i, (line, a, b, _, _) in enumerate(self.segments):
            for other, c, d, _, _ in self.segments[i + 1:]:
                if line != other and (segments_cross(a, b, c, d) or segments_share(a, b, c, d)):
                    problems.append(f"lines cross: {a} -> {b} and {c} -> {d}")
        for problem in problems:
            print(f"  ! {self.name}: {problem}")
        return not problems


def arrowhead(tip, origin, length=10.0, half=4.3):
    u = unit(tip, origin)
    side = (-u[1], u[0])
    base = (tip[0] + u[0] * length, tip[1] + u[1] * length)
    return " ".join(point(p) for p in (tip, (base[0] + side[0] * half, base[1] + side[1] * half),
                                       (base[0] - side[0] * half, base[1] - side[1] * half)))


def grow(box, by):
    return (box[0] - by, box[1] - by, box[2] + by, box[3] + by)


def boxes_overlap(a, b, gap):
    return a[0] < b[2] + gap and b[0] < a[2] + gap and a[1] < b[3] + gap and b[1] < a[3] + gap


def segment_hits_box(p, q, box):
    dx, dy = q[0] - p[0], q[1] - p[1]
    enter, leave = 0.0, 1.0
    for step, room in ((-dx, p[0] - box[0]), (dx, box[2] - p[0]), (-dy, p[1] - box[1]), (dy, box[3] - p[1])):
        if step == 0:
            if room < 0:
                return False
        elif step < 0:
            enter = max(enter, room / step)
        else:
            leave = min(leave, room / step)
    return enter <= leave


def segment_hits(p, q, node):
    if not node.round:
        return segment_hits_box(p, q, grow(node.box, -1))
    # Ellipse: scale it to a unit circle and measure the centre's distance to the segment.
    a = ((p[0] - node.cx) / (node.w / 2), (p[1] - node.cy) / (node.h / 2))
    b = ((q[0] - node.cx) / (node.w / 2), (q[1] - node.cy) / (node.h / 2))
    dx, dy = b[0] - a[0], b[1] - a[1]
    t = max(0.0, min(1.0, -(a[0] * dx + a[1] * dy) / ((dx * dx + dy * dy) or 1.0)))
    return math.hypot(a[0] + dx * t, a[1] + dy * t) < 0.98


def turn(o, a, b):
    return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])


def segments_cross(a, b, c, d):
    """True when the two segments cross away from their ends (a line ending on another is a join)."""
    d1, d2, d3, d4 = turn(c, d, a), turn(c, d, b), turn(a, b, c), turn(a, b, d)
    return d1 * d2 < -1.0 and d3 * d4 < -1.0


def segments_share(a, b, c, d):
    """True when two straight runs lie on top of each other."""
    for axis, other in ((0, 1), (1, 0)):
        if max(abs(a[axis] - b[axis]), abs(c[axis] - d[axis]), abs(a[axis] - c[axis])) < 1.0:
            low = max(min(a[other], b[other]), min(c[other], d[other]))
            high = min(max(a[other], b[other]), max(c[other], d[other]))
            if high - low > 2.0:
                return True
    return False


# --- the activity diagram ------------------------------------------------------------------------

def activity_diagram():
    """The whole game as LevelController, MainMenu, PauseMenu, PlayerDash, Guardian and
    EndingSequence run it. One lane per stage; the middle lane is the loop every level shares."""
    d = Diagram(2150, 1196, "activity_game_flow")
    d.title("ECHO · Activity diagram",
            "The whole game flow, from start-up to the ending (diagramme d'activité)")

    top, bottom = 88, 1082
    d.lane(30, 560, top, bottom, "START-UP AND MAIN MENU")
    d.lane(560, 1800, top, bottom, "PLAYING A LEVEL",
           "the same flow runs for Level 1, Level 2, Level 3 and the Guardian arena")
    d.lane(1800, 2120, top, bottom, "LEVEL COMPLETE AND ENDING")

    # Start-up and main menu.
    x = 250
    start = d.initial(x, 146)
    systems = d.action("Start the game systems\n(managers, audio, interface)", 218, cx=x)
    load_menu = d.action("Load the main menu scene", 302, cx=x)
    menu_in = d.diamond(x, 380, 16, merge=True)
    show_menu = d.action("Show the main menu (CONTINUE\nonly if a level is saved)", 462, cx=x)
    choose = d.action("Choose a menu option", 547, cx=x, player=True)
    option = d.diamond(x, 634)
    settings = d.action("Switch the sound\n(saved), or show\nthe credits", 507, cx=468)
    end = d.final(95, 634)
    which = d.diamond(x, 732, 18)
    saved = d.action("Take the\nsaved level", 817, cx=150)
    first = d.action("Take Level 1\n(Awakening)", 817, cx=420)
    start_in = d.diamond(x, 907, 14, merge=True)

    d.flow(start.south, systems.north)
    d.flow(systems.south, load_menu.north)
    d.flow(load_menu.south, menu_in.north)
    d.flow(menu_in.south, show_menu.north)
    d.flow(show_menu.south, choose.north)
    d.flow(choose.south, option.north)
    d.flow(option.east, (468, 634), settings.south, guard="[MUSIC, EFFECTS\nor CREDITS]", at=(372, 611))
    d.flow(settings.north, (468, 380), menu_in.east)
    d.flow(option.west, end.east, guard="[closes the page]", at=(168, 627))
    d.flow(option.south, which.north, guard="[starts a game]", at=(260, 690), anchor="start")
    d.flow(which.west, (150, 732), saved.north, guard="[CONTINUE]", at=(190, 725))
    d.flow(which.east, (420, 732), first.north, guard="[PLAY or NEW GAME]", at=(345, 725))
    d.flow(saved.south, (150, 907), start_in.west)
    d.flow(first.south, (420, 907), start_in.east)

    # Playing a level: set-up along the top, then one row per thing that can happen.
    row, rail, bus, trunk = 240, 1690, 690, 300
    next_level, loop_back = 144, 178
    level_in = d.diamond(640, row, 16, merge=True)
    load = d.action("Load the level scene\n(fade out, fade in)", row, left=690)
    setup = d.action("Set the starting energy, place the player\nat the start, save this level for CONTINUE,\n"
                     "show the title card", row, left=load.right + 35)
    loop_in = d.diamond(setup.right + 50, row, 16, merge=True)
    play = d.action("Run and jump\nthrough the level", row, left=loop_in.right + 34, player=True)
    event = d.diamond(play.right + 55, row)
    exit_check = d.diamond(rail, row)

    d.flow(start_in.south, (x, 952), (590, 952), (590, row), level_in.west)
    d.flow(level_in.east, load.west)
    d.flow(load.east, setup.west)
    d.flow(setup.east, loop_in.west)
    d.flow(loop_in.east, play.west)
    d.flow(play.east, event.west)

    dash_y, deny_y, cell_y, save_y, death_y, reset_y, pause_y, menu_y = 372, 458, 544, 626, 714, 800, 890, 976

    energy = d.diamond(820, dash_y)
    dash = d.action("Spend 1 energy and dash:\nit breaks the barrier, machine\nor exit door in its path", dash_y, left=940)
    target = d.diamond(dash.right + 50, dash_y)
    guardian = d.action("The Guardian takes a hit.\nAt 6 hits it is destroyed and\nthe blast door frees the exit",
                        dash_y, left=target.right + 140)
    deny = d.action("Refuse the dash\n(the meter flashes)", deny_y, cx=energy.cx)
    d.flow((bus, dash_y), energy.west, guard="[presses DASH]", at=(745, dash_y - 7), start="join")
    d.flow(energy.east, dash.west, guard="[energy ≥ 1]", at=(890, dash_y - 7))
    d.flow(dash.east, target.west)
    d.flow(target.east, guardian.west, guard="[hits the Guardian,\nshell open]", at=(target.right + 70, dash_y - 23))
    d.flow(guardian.east, (rail, dash_y), end="join")
    d.flow(energy.south, deny.north, guard="[energy = 0]", at=(energy.cx + 8, 415), anchor="start")
    d.flow(deny.east, (rail, deny_y), end="join")
    d.flow(target.south, (target.cx, deny_y), guard="[else]", at=(target.cx + 8, 428), anchor="start", end="join")

    cell = d.action("Energy +1, the cell disappears", cell_y, left=975)
    d.flow((bus, cell_y), cell.west, guard="[touches an energy cell, meter not full]", at=(702, cell_y - 7),
           anchor="start", start="join")
    d.flow(cell.east, (rail, cell_y), end="join")

    checkpoint = d.action("Save the checkpoint: the energy held\nand the state of the level", save_y, left=890)
    d.flow((bus, save_y), checkpoint.west, guard="[reaches a new checkpoint]", at=(702, save_y - 7),
           anchor="start", start="join")
    d.flow(checkpoint.east, (rail, save_y), end="join")

    die = d.action("Kill the player,\ncount the death", death_y, left=965)
    has_checkpoint = d.diamond(die.right + 50, death_y, 18)
    respawn = d.action("Respawn at that checkpoint:\nits energy, the level as it was", death_y,
                       left=has_checkpoint.right + 150)
    reset = d.action("Respawn at the level start:\nstarting energy, level reset", reset_y, left=respawn.left)
    d.flow((bus, death_y), die.west, guard="[touches a laser, a machine or a shot,\nfalls into a pit, or presses R]",
           at=(702, death_y - 24), anchor="start", start="join")
    d.flow(die.east, has_checkpoint.west)
    d.flow(has_checkpoint.east, respawn.west, guard="[checkpoint reached]", at=(has_checkpoint.right + 75, death_y - 7))
    d.flow(has_checkpoint.south, (has_checkpoint.cx, reset_y), reset.west, guard="[no checkpoint yet]",
           at=(has_checkpoint.right + 60, reset_y - 7))
    d.flow(respawn.east, (rail, death_y), end="join")
    d.flow(reset.east, (rail, reset_y), end="join")

    freeze = d.action("Freeze the game,\nshow the pause menu", pause_y, left=815)
    pick = d.action("Choose a pause option", pause_y, left=freeze.right + 42, player=True)
    pause_option = d.diamond(pick.cx, menu_y)
    to_menu = d.diamond(pick.cx, 1044, 14, merge=True)
    d.flow(event.south, (event.cx, trunk), (bus, trunk), (bus, pause_y), freeze.west, guard="[presses PAUSE]",
           at=(752, pause_y - 7))
    d.flow(freeze.east, pick.west)
    d.flow(pick.south, pause_option.north)
    d.flow(pause_option.east, (rail, menu_y), exit_check.south, guard="[RESUME]",
           at=(pause_option.right + 50, menu_y - 7))
    d.flow(pause_option.west, (level_in.cx, menu_y), level_in.south, guard="[RESTART]",
           at=(pause_option.left - 55, menu_y - 7))
    d.flow(pause_option.south, to_menu.north, guard="[MAIN MENU]", at=(pick.cx + 9, 1018), anchor="start")
    d.flow(to_menu.west, (52, 1044), (52, 380), menu_in.west)

    # Level complete and ending.
    x = 1955
    card = d.action("Show the level-complete card:\ntime, deaths, dashes used\n(3.5 s, or skip with a key)", row, cx=x)
    ads = d.diamond(x, 327, 18)
    ad = d.action("Show an ad (GamePix\npauses the game)", 412, cx=x)
    ads_out = d.diamond(x, 492, 14, merge=True)
    more = d.diamond(x, 562)
    clear = d.action("Clear the saved level\n(CONTINUE disappears)", 660, cx=x)
    ending = d.action("Play the ending: captions,\nthe rescue ship's message,\nTHE END", 765, cx=x)
    press = d.action("Press a key or tap", 866, cx=x, player=True)

    d.flow(exit_check.east, card.west, guard="[exit door broken]", at=(1770, row - 7))
    d.flow(exit_check.north, (rail, loop_back), (loop_in.cx, loop_back), loop_in.north,
           guard="[exit door still closed]", at=(rail - 12, loop_back - 7), anchor="end")
    d.flow(card.south, ads.north)
    d.flow(ads.south, ad.north, guard="[GamePix ads on]", at=(x + 8, 368), anchor="start")
    d.flow(ads.west, (1820, 327), (1820, 492), ads_out.west, guard="[ads off]", at=(1878, 320))
    d.flow(ad.south, ads_out.north)
    d.flow(ads_out.south, more.north)
    d.flow(more.east, (2104, 562), (2104, next_level), (level_in.cx, next_level), level_in.north,
           guard="[a level follows]", at=(2040, 555))
    d.text(1000, next_level - 7, "next level: Level 2, then Level 3, then the Guardian arena", 13, fill=MUTED)
    d.flow(more.south, clear.north, guard="[the Guardian\nis beaten]", at=(x - 9, 603), anchor="end")
    d.flow(clear.south, ending.north)
    d.flow(ending.south, press.north)
    d.flow(press.south, (x, 1044), to_menu.east)

    activity_legend(d, 1128)
    return d


def activity_legend(d, y):
    x = 44
    d.shapes.append(f'<circle cx="{x}" cy="{y}" r="8" fill="{INK}"/>')
    x += 16 + d.text(x + 16, y + 5, "Start", 13.5, "start", track=False) + 30
    d.shapes.append(f'<circle cx="{x}" cy="{y}" r="9" fill="{PAPER}" stroke="{INK}" stroke-width="1.5"/>'
                    f'<circle cx="{x}" cy="{y}" r="5" fill="{INK}"/>')
    x += 17 + d.text(x + 17, y + 5, "End", 13.5, "start", track=False) + 30
    for fill, edge, label in ((GAME_FILL, LINE, "Action done by the game"),
                              (PLAYER_FILL, PLAYER_EDGE, "Action done by the player")):
        d.shapes.append(f'<rect x="{num(x - 8)}" y="{y - 11}" width="40" height="22" rx="8" fill="{fill}" '
                        f'stroke="{edge}" stroke-width="1.5"/>')
        x += 40 + d.text(x + 40, y + 5, label, 13.5, "start", track=False) + 30
    for fill, label in ((DECISION_FILL, "Decision: the flow takes the path whose [condition] is true"),
                        (PAPER, "Merge: paths come back together")):
        d.shapes.append(f'<polygon points="{x},{y - 11} {x + 11},{y} {x},{y + 11} {x - 11},{y}" fill="{fill}" '
                        f'stroke="{LINE}" stroke-width="1.5" stroke-linejoin="round"/>')
        x += 19 + d.text(x + 19, y + 5, label, 13.5, "start", track=False) + 30
    d.shapes.append(f'<circle cx="{x}" cy="{y}" r="3.2" fill="{LINE}"/>')
    d.text(x + 12, y + 5, "A flow joins or leaves a shared line", 13.5, "start", track=False)
    d.text(36, y + 40, "The rule behind every level: a dash always costs 1 energy, and the exit door only opens to a dash.",
           13.5, "start", fill=MUTED, track=False)


# --- the use case diagram ------------------------------------------------------------------------

def use_case_diagram():
    """What the player can do in the build, and where GamePix takes part (saves, ads)."""
    d = Diagram(1640, 950, "use_case")
    d.title("ECHO · Use case diagram",
            "What the player can do, and what the GamePix platform does (diagramme de cas d'utilisation)")
    d.boundary(310, 100, 1350, 890, "ECHO")

    player = d.actor(150, 520, "Player")
    gamepix = d.actor(1500, 420, "GamePix\nplatform")

    new_game = d.use_case("Start a new game", 470, 170)
    continue_game = d.use_case("Continue the saved game", 530, 285)
    credits = d.use_case("View the credits", 500, 490)
    pause = d.use_case("Pause the game", 500, 640)
    sound = d.use_case("Change the sound settings", 500, 820)

    play = d.use_case("Play a level", 840, 290, main=True)
    boss = d.use_case("Fight the Guardian", 860, 440)
    ending = d.use_case("Watch the ending", 860, 560)
    restart = d.use_case("Restart the level", 880, 700)
    to_menu = d.use_case("Return to the main menu", 880, 790)

    move = d.use_case("Run and jump", 1170, 160)
    dash = d.use_case("Dash (costs 1 energy)", 1170, 240)
    save = d.use_case("Save progress", 1170, 320)
    cells = d.use_case("Collect energy cells", 1170, 410)
    respawn = d.use_case("Respawn at the\nlast checkpoint", 1170, 505)
    ad = d.use_case("Show an ad\nbetween levels", 1170, 610)

    for use_case in (new_game, continue_game, play, pause, sound, credits):
        d.association(player, use_case)
    for use_case in (save, ad):
        d.association(gamepix, use_case)

    d.dependency(new_game, play, "include")
    d.dependency(continue_game, play, "include")
    for part in (move, dash, save):
        d.dependency(play, part, "include")
    for extra in (cells, respawn, ad, pause):
        d.dependency(extra, play, "extend")
    d.generalization(boss, play)
    d.dependency(ending, boss, "extend")
    for extra in (restart, to_menu, sound):
        d.dependency(extra, pause, "extend")

    use_case_legend(d, 40, 770)
    return d


def use_case_legend(d, x, y):
    rows = (("association", "takes part in"),
            ("include", "always part of it"),
            ("extend", "sometimes added to it"),
            ("kind", "is a kind of"))
    for i, (kind, label) in enumerate(rows):
        line_y = y + i * 30
        a, b = (x, line_y), (x + 58, line_y)
        if kind == "kind":
            d.shapes.append(f'<path d="M{point(a)} L{x + 44},{line_y}" stroke="{LINE}" stroke-width="1.6"/>'
                            f'<polygon points="{arrowhead(b, a, 14, 7)}" fill="{PAPER}" stroke="{LINE}" stroke-width="1.6"/>')
        else:
            dash = "" if kind == "association" else ' stroke-dasharray="7 5"'
            d.shapes.append(f'<path d="M{point(a)} L{point(b)}" stroke="{LINE}" stroke-width="1.6"{dash}/>')
            if kind != "association":
                d.shapes.append(f'<path d="M{x + 47},{line_y - 5} L{point(b)} L{x + 47},{line_y + 5}" fill="none" '
                                f'stroke="{LINE}" stroke-width="1.6" stroke-linecap="round"/>')
        prefix = f"«{kind}»  " if kind in ("include", "extend") else ""
        d.text(x + 72, line_y + 4.5, prefix + label, 13, "start", fill=MUTED, track=False)


# --- output --------------------------------------------------------------------------------------

BROWSERS = (
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    "msedge", "google-chrome", "chromium", "chrome",
)


def export_png(svg_path, width, height, scale=2):
    """Rasterizes the SVG with a headless browser, at twice the size so it prints sharply."""
    browser = next((b for b in BROWSERS if os.path.isfile(b) or shutil.which(b)), None)
    if browser is None:
        print("  (no Edge or Chrome found: PNG skipped)")
        return
    png_path = os.path.splitext(svg_path)[0] + ".png"
    with tempfile.TemporaryDirectory() as profile:
        subprocess.run([browser, "--headless", "--disable-gpu", "--hide-scrollbars", f"--user-data-dir={profile}",
                        f"--force-device-scale-factor={scale}", f"--window-size={width},{height}",
                        f"--screenshot={png_path}", "file:///" + svg_path.replace(os.sep, "/")],
                       check=False, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120)
    print("  " + os.path.relpath(png_path) if os.path.isfile(png_path) else "  (the browser wrote no PNG)")


if __name__ == "__main__":
    sys.stdout.reconfigure(errors="replace")  # « » and ≥ in a warning must not stop a legacy console
    os.makedirs(OUT, exist_ok=True)
    for build in (activity_diagram, use_case_diagram):
        diagram = build()
        diagram.check()
        path = os.path.abspath(os.path.join(OUT, diagram.name + ".svg"))
        with open(path, "w", encoding="utf-8") as handle:
            handle.write(diagram.svg())
        print(os.path.relpath(path))
        export_png(path, diagram.width, diagram.height)

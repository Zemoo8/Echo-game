"""Builds ECHO's short presentation video (French captions) from the captured gameplay.

1. Capture the footage in Unity (bots play every part, 30 fps, frames to Recordings/<part>/):
   Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Echo.Tests.TrailerCapture
2. python Tools/make_video.py [output.mp4]

Needs ffmpeg on the PATH and Pillow. Title cards and captions are drawn here; the music is the
game's own CC0 soundtrack (SRG774). Work files go to Recordings/_video/.
"""
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
REC = ROOT / "Recordings"
WORK = REC / "_video"
MUSIC = ROOT / "Assets/ThirdParty/SRG774/DarkSciFiAudio"
UI = ROOT / "Assets/Art/Pixel/UI"
W, H, FPS = 1280, 720, 30
PART_FPS = {"boss": 60}  # the boss fight is captured at 60 frames per second

VOID = (7, 10, 18)
STRUCT = (35, 44, 63)
ENERGY = (54, 226, 255)
HAZARD = (255, 51, 85)
EXIT = (61, 255, 143)
INK = (234, 248, 255)
SOFT = (170, 190, 210)

FONT = "C:/Windows/Fonts/bahnschrift.ttf"


def font(size, weight=None):
    f = ImageFont.truetype(FONT, size)
    if weight:
        f.set_variation_by_name(weight)
    return f


def pixel_image(name, scale):
    im = Image.open(UI / name).convert("RGBA")
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def backdrop(still=None):
    """Dark card background: a blurred, darkened game still (or plain void) with a cyan rule."""
    if still and Path(still).exists():
        bg = Image.open(still).convert("RGB").resize((W, H)).filter(ImageFilter.GaussianBlur(6))
        bg = Image.blend(bg, Image.new("RGB", (W, H), VOID), 0.78)
    else:
        bg = Image.new("RGB", (W, H), VOID)
    d = ImageDraw.Draw(bg)
    for y in range(0, H, 4):  # faint scanlines, as on the in-game panels
        d.line([(0, y), (W, y)], fill=tuple(max(0, c - 4) for c in VOID), width=1)
    return bg


def text_block(d, xy, text, f, fill, width, spacing=10):
    """Draws wrapped text; returns the y below it."""
    x, y = xy
    words, line = text.split(), ""
    for word in words:
        trial = (line + " " + word).strip()
        if d.textlength(trial, font=f) > width and line:
            d.text((x, y), line, font=f, fill=fill)
            y += f.size + spacing
            line = word
        else:
            line = trial
    if line:
        d.text((x, y), line, font=f, fill=fill)
        y += f.size + spacing
    return y


def kicker(d, x, y, label, color=ENERGY):
    d.rectangle([x, y + 4, x + 6, y + 26], fill=color)
    d.text((x + 18, y), label, font=font(24, "SemiBold"), fill=color)


def card_concept(path, still):
    im = backdrop(still)
    d = ImageDraw.Draw(im)
    kicker(d, 110, 120, "LE CONCEPT")
    d.text((110, 160), "CHAQUE DASH EST UNE DÉCISION", font=font(58, "Bold"), fill=INK)
    cell = pixel_image("CellFull.png", 4)
    y = 280
    rows = [
        (ENERGY, "Le dash traverse les grands vides, brise les barrières d'énergie et détruit les machines."),
        (ENERGY, "Mais chaque dash coûte une charge d'énergie, et la combinaison en contient trois au maximum."),
        (EXIT, "Chaque niveau se termine par une porte que seul un dash peut briser : il faut toujours garder une charge."),
    ]
    for color, text in rows:
        d.rectangle([112, y + 10, 120, y + 18], fill=color)
        y = text_block(d, (140, y), text, font(30), INK, 1000, 8) + 22
    for i in range(3):
        im.paste(cell, (W - 110 - (3 - i) * (cell.width + 14), 128), cell)
    im.save(path)


def card_story(path, still):
    im = backdrop(still)
    d = ImageDraw.Draw(im)
    kicker(d, 110, 170, "L'HISTOIRE")
    d.text((110, 210), "UN CENTRE DE RECHERCHE DANS LE NOIR", font=font(50, "Bold"), fill=INK)
    text_block(d, (110, 300),
               "Elle se réveille seule dans une capsule de cryogénie. Les systèmes de sécurité tournent "
               "encore. Tout en bas, le réacteur qui fait vivre le centre s'éteint, protégé par la machine "
               "construite pour le garder. Apaiser le cœur, c'est pouvoir appeler à l'aide.",
               font(32), SOFT, 1060, 12)
    im.save(path)


def card_tech(path, still, tests):
    im = backdrop(still)
    d = ImageDraw.Draw(im)
    kicker(d, 110, 90, "LA TECHNIQUE")
    d.text((110, 130), "CONÇU POUR LE NAVIGATEUR", font=font(54, "Bold"), fill=INK)
    items = [
        ("Unity 6 · URP 2D · C#", "Pixel art à 32 px par unité, caméra « pixel perfect »"),
        (f"{tests} tests automatisés", "Des bots jouent chaque niveau et battent le Gardien"),
        ("Scènes générées par code", "Niveaux, interface et effets reconstruits depuis l'éditeur"),
        ("WebGL avec le plugin GamePix", "Pause automatique, sauvegarde dans le navigateur"),
        ("Clavier, manette, tactile", "Les aides affichent les touches de l'appareil utilisé"),
        ("Ressources CC0 + art original", "Toutes les licences dans ASSET_LICENSES.md"),
    ]
    for i, (title, line) in enumerate(items):
        x = 110 + (i % 2) * 540
        y = 250 + (i // 2) * 130
        d.rectangle([x, y, x + 500, y + 108], fill=(12, 18, 30), outline=STRUCT, width=2)
        d.rectangle([x, y, x + 5, y + 108], fill=ENERGY)
        d.text((x + 24, y + 16), title, font=font(30, "SemiBold"), fill=INK)
        text_block(d, (x + 24, y + 58), line, font(21), SOFT, 460, 4)
    im.save(path)


def card_outro(path, still):
    im = backdrop(still)
    d = ImageDraw.Draw(im)
    logo = pixel_image("Logo.png", 6)
    im.paste(logo, ((W - logo.width) // 2, 150), logo)
    tag = "CHAQUE DASH EST UNE DÉCISION"
    f = font(30, "SemiBold")
    d.text(((W - d.textlength(tag, font=f)) // 2, 380), tag, font=f, fill=ENERGY)
    for text, size, color, y in [
        ("Ahmed Baghouli", 34, INK, 460),
        ("Code source : github.com/Zemoo8/Echo-game", 26, SOFT, 515),
        ("Musique : SRG774 · Graphismes : Luis Zuno (ansimuz) · Sons et police : Kenney (CC0)", 20, SOFT, 620),
    ]:
        ff = font(size)
        d.text(((W - d.textlength(text, font=ff)) // 2, y), text, font=ff, fill=color)
    im.save(path)


def caption(path, kick, line, color=ENERGY):
    """Caption on a transparent canvas, overlaid on the footage. It sits in the top-right corner:
    the game draws its energy meter top left, title cards and the boss bar top centre, and its
    hints and the ending's captions along the bottom."""
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    fk, fl = font(17, "SemiBold"), font(22)
    words, lines, current = line.split(), [], ""
    for word in words:
        trial = (current + " " + word).strip()
        if d.textlength(trial, font=fl) > 330 and current:
            lines.append(current)
            current = word
        else:
            current = trial
    lines.append(current)
    width = int(max([d.textlength(kick, font=fk)] + [d.textlength(t, font=fl) for t in lines])) + 40
    height = 40 + 28 * len(lines)
    x, y = W - 24 - width, 20
    d.rectangle([x, y, x + width, y + height], fill=(7, 10, 18, 220))
    d.rectangle([x + width - 4, y, x + width, y + height], fill=color + (255,))
    d.text((x + 16, y + 10), kick, font=fk, fill=color + (255,))
    for i, text in enumerate(lines):
        d.text((x + 16, y + 36 + 28 * i), text, font=fl, fill=INK + (255,))
    im.save(path)


def ffmpeg(*args):
    subprocess.run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error", *args], check=True)


ENCODE = ["-c:v", "libx264", "-preset", "medium", "-crf", "19", "-pix_fmt", "yuv420p", "-r", str(FPS)]


def card_clip(out, png, seconds):
    ffmpeg("-loop", "1", "-t", str(seconds), "-i", str(png),
           "-vf", f"fade=in:st=0:d=0.5,fade=out:st={seconds - 0.5}:d=0.5", *ENCODE, str(out))


def footage_clip(out, part, start, seconds, speed=1.0, cap=None, fade_in=0.4, fade_out=0.4):
    """`seconds` of output from Recordings/<part>, starting `start` seconds into the capture."""
    rate = PART_FPS.get(part, FPS)
    frames = sorted((REC / part).glob("f_*.jpg"))
    first = int(start * rate) + 1
    need = int(seconds * speed * rate)
    assert first + need - 1 <= len(frames), f"{part}: only {len(frames)} frames, wanted up to {first + need - 1}"
    chain = f"[0:v]setpts=PTS/{speed},fps={FPS}"
    chain += f",fade=in:st=0:d={fade_in}" if fade_in else ""
    chain += f",fade=out:st={seconds - fade_out}:d={fade_out}" if fade_out else ""
    inputs = ["-framerate", str(rate), "-start_number", str(first), "-i", str(REC / part / "f_%05d.jpg")]
    if cap:
        inputs += ["-loop", "1", "-i", str(cap)]
        chain += "[v];[1:v]format=rgba,fade=in:st=0.4:d=0.4:alpha=1,"                  f"fade=out:st={seconds - 1.0}:d=0.4:alpha=1[c];[v][c]overlay=0:0:shortest=1"
    ffmpeg(*inputs, "-filter_complex", chain, "-t", str(seconds), *ENCODE, str(out))


def music_track(out, cues, total):
    """Concatenates (file, start, seconds) cues with short crossfades, faded out at the end."""
    inputs, chains = [], []
    for i, (name, start, seconds) in enumerate(cues):
        inputs += ["-ss", str(start), "-t", str(seconds + 1.5), "-i", str(MUSIC / name)]
        chains.append(f"[{i}:a]aresample=48000,afade=in:st=0:d=0.6[a{i}]")
    label = "a0"
    for i in range(1, len(cues)):
        chains.append(f"[{label}][a{i}]acrossfade=d=1.5[x{i}]")
        label = f"x{i}"
    chains.append(f"[{label}]atrim=0:{total},afade=out:st={total - 2.5}:d=2.5,volume=0.85[m]")
    ffmpeg(*inputs, "-filter_complex", ";".join(chains), "-map", "[m]", "-c:a", "aac", "-b:a", "192k", str(out))


def duration(path):
    r = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(path)],
                       capture_output=True, text=True, check=True)
    return float(r.stdout.strip())


# The edit, in play order: (music, kind, ...). Footage starts are seconds into each part's
# capture; tune them to the moments worth showing after a new capture. Consecutive steps with
# the same music share one cue: the menu theme, the sector theme, the boss theme, the transmission.
EDIT = [
    ("title", "footage", "menu", 0.3, 6.0, 1.0, ("UN JEU DE PLATEFORME 2D EN PIXEL ART", "Ahmed Baghouli · Unity 6 · navigateur (GamePix)")),
    ("title", "card", "concept", 8.0),
    ("title", "card", "story", 7.0),
    ("sector", "footage", "level1", 0.0, 11.0, 1.0, ("NIVEAU 1 · ÉVEIL", "Courir, sauter, et le premier dash à travers une barrière")),
    ("sector", "footage", "level1", 12.0, 10.0, 1.3, ("NIVEAU 1 · ÉVEIL", "Attendre les lasers, et garder une charge pour la sortie")),
    ("sector", "footage", "level2", 0.5, 17.0, 1.5, ("NIVEAU 2 · AILE DE SÉCURITÉ", "Drones et tourelles : un dash les détruit, mais coûte une charge")),
    ("sector", "footage", "level3", 0.5, 18.0, 1.6, ("NIVEAU 3 · CŒUR ÉNERGÉTIQUE", "Des plaques qui cèdent, des verrous qui veulent des charges gardées")),
    ("boss", "footage", "boss", 0.0, 9.0, 1.0, ("LE GARDIEN", "Le gardien du réacteur se réveille", HAZARD)),
    ("boss", "footage", "boss", 9.0, 9.5, 1.2, ("LE GARDIEN", "Dasher dans son cœur ouvert : 6 coups en 3 phases", HAZARD)),
    ("end", "footage", "ending", 3.5, 14.0, 1.6, ("LA FIN", "Le réacteur s'apaise, un vaisseau de secours répond", EXIT)),
    ("end", "card", "tech", 8.0),
    ("end", "card", "outro", 6.0),
]
CUES = {"title": ("title.ogg", 0), "sector": ("sector.ogg", 0), "boss": ("urgent.ogg", 4), "end": ("transmission.ogg", 0)}


def main():
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "Recordings/ECHO_Presentation.mp4"
    tests = sys.argv[2] if len(sys.argv) > 2 else "52"
    WORK.mkdir(parents=True, exist_ok=True)
    still = lambda part, n: next(iter(sorted((REC / part).glob("f_*.jpg"))[n:n + 1]), None)

    cards = {
        "concept": lambda p: card_concept(p, still("level1", 300)),
        "story": lambda p: card_story(p, still("level3", 300)),
        "tech": lambda p: card_tech(p, still("level2", 600), tests),
        "outro": lambda p: card_outro(p, still("menu", 60)),
    }

    clips, tags = [], []
    for i, step in enumerate(EDIT):
        clip = WORK / f"clip_{i:02d}.mp4"
        if step[1] == "card":
            png = WORK / f"card_{step[2]}.png"
            cards[step[2]](png)
            card_clip(clip, png, step[3])
        else:
            _, _, part, start, seconds, speed, cap = step
            cap_png = WORK / f"cap_{i:02d}.png"
            caption(cap_png, *cap)
            footage_clip(clip, part, start, seconds, speed, cap_png)
        clips.append(clip)
        tags.append(step[0])
        print("clip", i, step[1:3])

    listing = WORK / "clips.txt"
    listing.write_text("".join(f"file '{c.as_posix()}'\n" for c in clips), encoding="utf-8")
    silent = WORK / "video_silent.mp4"
    ffmpeg("-f", "concat", "-safe", "0", "-i", str(listing), "-c", "copy", str(silent))
    total = duration(silent)

    cues = []
    for tag, clip in zip(tags, clips):
        if cues and cues[-1][0] == tag:
            cues[-1][1] += duration(clip)
        else:
            cues.append([tag, duration(clip)])
    music = WORK / "music.m4a"
    music_track(music, [(CUES[tag][0], CUES[tag][1], seconds) for tag, seconds in cues], total)

    ffmpeg("-i", str(silent), "-i", str(music), "-map", "0:v", "-map", "1:a", "-c:v", "copy", "-c:a", "copy",
           "-movflags", "+faststart", "-shortest", str(out))
    print(f"{out} ({total:.1f} s)")


if __name__ == "__main__":
    main()

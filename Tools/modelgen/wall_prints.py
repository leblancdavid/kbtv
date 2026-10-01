"""Rebuild the station's printable wall ephemera: python Tools/modelgen/wall_prints.py.

Pillow is the only dependency. The artwork is deterministic and uses no external images.
"""

from pathlib import Path
import math
import random

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "assets" / "textures" / "world3d" / "wall_prints"
INK = (48, 47, 45)
FADED = (105, 94, 83)
RED = (121, 57, 53)
TEAL = (65, 99, 100)
CREAM = (211, 197, 166)
W, H = 512, 704
SEED = 1968


def font(size, bold=False, mono=False):
    names = (["consolab.ttf", "cour.ttf"] if mono else
             ["georgiab.ttf", "arialbd.ttf"] if bold else ["georgia.ttf", "arial.ttf"])
    for name in names:
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def paper(name, width=W, height=H, tone=(207, 195, 169)):
    rng = random.Random(SEED + sum(ord(ch) for ch in name))
    image = Image.new("RGB", (width, height), tone)
    pix = image.load()
    for y in range(height):
        for x in range(width):
            grain = rng.randint(-5, 5) - int(9 * abs(y / height - .45))
            edge = int(11 * max(0, 1 - min(x, y, width-1-x, height-1-y) / 22))
            pix[x, y] = tuple(max(0, min(255, c + grain - edge)) for c in tone)
    draw = ImageDraw.Draw(image)
    draw.rectangle((12, 12, width-13, height-13), outline=(145, 131, 109), width=2)
    return image, draw


def text(draw, xy, value, size, fill=INK, bold=False, mono=False):
    draw.text(xy, value, font=font(size, bold, mono), fill=fill)


def centered(draw, y, value, size, fill=INK, bold=False, mono=False, width=W):
    face = font(size, bold, mono)
    box = draw.textbbox((0, 0), value, font=face)
    draw.text(((width - (box[2] - box[0])) // 2, y), value, font=face, fill=fill)


def rule(draw, y, fill=FADED, margin=42):
    draw.line((margin, y, W - margin, y), fill=fill, width=3)


def seal(draw, x, y, label="KBTV"):
    draw.ellipse((x-28, y-28, x+28, y+28), outline=RED, width=4)
    draw.ellipse((x-23, y-23, x+23, y+23), outline=RED, width=1)
    text(draw, (x-22, y-9), label, 15, RED, True, True)


def save(image, name):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    image.save(OUTPUT / f"{name}.png", optimize=True)


def station_poster():
    image, d = paper("station_poster", tone=(196, 177, 143))
    d.rectangle((28, 28, 484, 676), outline=INK, width=7)
    centered(d, 48, "KBTV", 96, RED, True)
    centered(d, 156, "BEYOND THE VEIL", 33, INK, True)
    rule(d, 216, RED)
    # Transmitter tower and its radiating arcs are composed of printable line art.
    d.polygon(((256, 258), (189, 552), (323, 552)), outline=INK, fill=(87, 82, 70))
    for y in (320, 387, 456, 521):
        span = int((y - 258) * .22)
        d.line((256-span, y, 256+span, y), fill=CREAM, width=6)
    d.ellipse((247, 239, 265, 257), fill=RED)
    for radius in (75, 115):
        d.arc((256-radius, 355-radius, 256+radius, 355+radius), 197, 343, fill=TEAL, width=6)
    centered(d, 571, "THE NIGHT HAS A VOICE", 26, INK, True)
    centered(d, 617, "AFTER DARK  /  ON THE AIR", 19, RED, mono=True)
    save(image, "station_poster")


def ufo_flyer():
    image, d = paper("ufo_flyer")
    centered(d, 50, "HAVE YOU SEEN", 39, INK, True)
    centered(d, 108, "THE LIGHTS?", 45, RED, True)
    rule(d, 184, RED)
    d.rectangle((59, 208, 453, 475), outline=FADED, width=3)
    d.ellipse((80, 225, 430, 455), outline=FADED, width=2)
    d.ellipse((143, 250, 370, 430), outline=FADED, width=2)
    d.line((256, 224, 256, 460), fill=FADED, width=2)
    d.line((75, 341, 440, 341), fill=FADED, width=2)
    for x, y, r in ((156, 285, 12), (317, 306, 19), (347, 399, 8)):
        d.ellipse((x-r, y-r, x+r, y+r), fill=RED)
        d.ellipse((x-2*r, y-2*r, x+2*r, y+2*r), outline=RED, width=3)
    text(d, (66, 495), "DATE / TIME / DIRECTION", 24, INK, mono=True)
    text(d, (66, 544), "Write down what you saw.", 24)
    text(d, (66, 583), "Call the KBTV night desk.", 24)
    rule(d, 629)
    centered(d, 645, "KEEP LOOKING UP", 18, RED, True, True)
    save(image, "ufo_flyer")


def ghost_poster():
    image, d = paper("ghost_poster", tone=(173, 179, 169))
    d.rectangle((30, 30, 482, 674), fill=(60, 69, 69))
    d.rectangle((46, 46, 466, 658), outline=CREAM, width=3)
    centered(d, 62, "NIGHT SIGNAL", 46, CREAM, True)
    centered(d, 121, "voices between stations", 23, (182, 196, 181))
    # A ghostly waveform that resolves into a radio dial.
    points = []
    for x in range(65, 450, 3):
        t = (x - 65) / 384
        envelope = 15 + 68 * math.exp(-((t - .52) / .19) ** 2)
        y = 345 + math.sin(t * 85) * envelope * (0.4 + 0.6 * math.sin(t * 31) ** 2)
        points.append((x, int(y)))
    d.line(points, fill=(192, 208, 191), width=4, joint="curve")
    d.ellipse((143, 433, 369, 629), outline=CREAM, width=5)
    d.arc((178, 461, 334, 601), 180, 360, fill=RED, width=8)
    d.line((256, 535, 326, 478), fill=CREAM, width=5)
    centered(d, 579, "AM  /  AFTER MIDNIGHT", 18, CREAM, mono=True)
    save(image, "ghost_poster")


def cryptid_poster():
    image, d = paper("cryptid_poster", tone=(193, 191, 165))
    centered(d, 48, "THE TREE LINE", 44, INK, True)
    centered(d, 104, "HAS EYES", 45, RED, True)
    rule(d, 173, TEAL)
    d.rectangle((48, 191, 464, 534), fill=(64, 76, 70))
    for i in range(13):
        x = 46 + i * 34
        top = 245 + ((i * 37) % 94)
        d.polygon(((x, 517), (x + 19, 517), (x + 13, top), (x + 7, top)), fill=(27, 43, 41))
        d.polygon(((x-19, top+67), (x+38, top+67), (x+10, top-56)), fill=(31, 53, 49))
    d.ellipse((230, 367, 239, 373), fill=(218, 176, 100))
    d.ellipse((265, 367, 274, 373), fill=(218, 176, 100))
    centered(d, 559, "CALL IN.  TELL IT STRAIGHT.", 21, INK, True, True)
    centered(d, 610, "KBTV / NIGHT REPORTS", 20, RED, mono=True)
    save(image, "cryptid_poster")


def schedule():
    image, d = paper("schedule", height=640, tone=(218, 207, 184))
    d.rectangle((28, 30, 484, 120), fill=(66, 76, 72))
    text(d, (47, 43), "KBTV  /  NIGHT SHIFT", 31, CREAM, True)
    text(d, (49, 84), "CONTROL ROOM   -   KEEP POSTED", 16, CREAM, mono=True)
    rows = [("21:30", "Line checks + levels"), ("22:00", "Opening signal"),
            ("23:15", "Listener call window"), ("00:30", "Station ID / ads"),
            ("02:00", "Close & archive tapes")]
    for index, (when, what) in enumerate(rows):
        y = 159 + index * 74
        d.rectangle((42, y-9, 469, y+55), outline=(167, 152, 129), width=2)
        text(d, (55, y), when, 26, RED, True, True)
        text(d, (181, y+2), what, 22)
    d.line((43, 540, 465, 540), fill=RED, width=4)
    text(d, (50, 555), "PRODUCER: verify every source", 20, INK, mono=True)
    text(d, (50, 585), "before it goes on air.", 20, INK, mono=True)
    save(image, "night_shift")


def calendar():
    image, d = paper("calendar", height=640, tone=(221, 207, 180))
    d.rectangle((28, 28, 484, 124), fill=(105, 57, 54))
    text(d, (52, 43), "OCTOBER", 45, CREAM, True)
    text(d, (366, 60), "KBTV", 23, CREAM, True)
    text(d, (43, 154), "SUN   MON   TUE   WED   THU   FRI   SAT", 20, INK, mono=True)
    for row in range(5):
        for column in range(7):
            x0, y0 = 39 + column * 62, 195 + row * 62
            d.rectangle((x0, y0, x0+59, y0+59), outline=(160, 148, 130), width=1)
            day = row * 7 + column - 3
            if 1 <= day <= 31:
                text(d, (x0+9, y0+7), str(day).zfill(2), 22, INK, mono=True)
            if day in (8, 22):
                d.ellipse((x0+4, y0+2, x0+51, y0+52), outline=RED, width=4)
    text(d, (54, 534), "8 - TRANSMITTER CHECK", 23, RED, True, True)
    text(d, (54, 573), "22 - NIGHT SIGNAL", 23, RED, True, True)
    save(image, "station_calendar")


def field_notes():
    image, d = paper("field_notes", height=640, tone=(220, 210, 189))
    for y in range(79, 612, 39):
        d.line((28, y, 482, y), fill=(159, 178, 176), width=2)
    d.line((89, 22, 89, 617), fill=(182, 105, 96), width=2)
    text(d, (110, 35), "CALLER / 03:12 AM", 26, INK, True, True)
    for y, line in ((87, "3 lights over the ridge"), (128, "no engine noise - same path"),
                    (205, "check weather report"), (246, "ask about radio static"),
                    (323, "second witness?"), (402, "DO NOT AIR YET")):
        text(d, (108, y), line, 23, RED if y == 402 else INK, mono=True)
    d.line((110, 188, 401, 188), fill=RED, width=3)
    d.ellipse((108, 389, 415, 449), outline=RED, width=4)
    d.line((105, 480, 390, 562), fill=FADED, width=3)
    d.line((388, 480, 106, 561), fill=FADED, width=3)
    seal(d, 434, 569)
    save(image, "caller_field_notes")


def breakroom_notice():
    image, d = paper("breakroom_notice", height=640)
    centered(d, 46, "STATION NOTICE", 43, INK, True)
    rule(d, 114, RED)
    centered(d, 165, "IF IT HAPPENS", 34, RED, True)
    centered(d, 213, "ON THE AIR", 34, RED, True)
    centered(d, 265, "IT GOES IN THE LOG.", 27, INK, True)
    d.rectangle((65, 336, 447, 497), outline=TEAL, width=4)
    text(d, (86, 356), "DATE: _______________", 24, INK, mono=True)
    text(d, (86, 405), "TIME: _______________", 24, INK, mono=True)
    text(d, (86, 454), "TAPE: _______________", 24, INK, mono=True)
    centered(d, 543, "KEEP THE ORIGINAL RECORDING", 20, RED, mono=True)
    seal(d, 258, 604)
    save(image, "station_notice")


def signal_chart():
    image, d = paper("signal_chart", height=640, tone=(205, 199, 177))
    centered(d, 37, "THE NIGHT FREQUENCY", 34, INK, True)
    centered(d, 94, "FIELD LOG  /  UNIDENTIFIED", 20, RED, mono=True)
    rule(d, 143, TEAL)
    d.rectangle((52, 168, 461, 445), outline=INK, width=2)
    for x in range(83, 461, 50):
        d.line((x, 169, x, 444), fill=(161, 157, 139), width=1)
    for y in range(201, 445, 40):
        d.line((52, y, 461, y), fill=(161, 157, 139), width=1)
    points = []
    for x in range(54, 460, 2):
        t = (x - 54) / 405
        y = 306 + 47 * math.sin(15 * t) + 32 * math.sin(58 * t) * math.exp(-((t-.7)/.17)**2)
        points.append((x, int(y)))
    d.line(points, fill=RED, width=5)
    d.ellipse((325, 203, 384, 262), outline=RED, width=5)
    d.line((350, 249, 348, 308), fill=RED, width=3)
    text(d, (62, 469), "SOURCE: unknown", 24, INK, mono=True)
    text(d, (62, 518), "DO NOT ERASE MASTER", 23, RED, True, True)
    centered(d, 580, "KBTV   /   ARCHIVE COPY", 18, TEAL, mono=True)
    save(image, "signal_chart")


def main():
    for render in (station_poster, ufo_flyer, ghost_poster, cryptid_poster,
                   schedule, calendar, field_notes, breakroom_notice, signal_chart):
        render()
    print(f"Generated 9 wall prints in {OUTPUT}")


if __name__ == "__main__":
    main()

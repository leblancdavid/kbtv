"""Generate original found-footage sources and compose printable KBTV wall art.

Requires Pillow and OPENAI_API_KEY only for generation. Run from the project root:
  python Tools/modelgen/generate_photo_prints.py bigfoot
  python Tools/modelgen/generate_photo_prints.py --compose-only
Each subject is one paid image request; existing sources are never regenerated.
"""

import argparse
import base64
from io import BytesIO
import json
import os
from pathlib import Path
import sys
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from PIL import Image, ImageDraw, ImageOps

from wall_prints import OUTPUT, CREAM, INK, RED, font, paper


MODEL = "gpt-image-2.5-flare"
SOURCES = OUTPUT / "photo_sources"
SUBJECTS = {
    "bigfoot": (
        "1024x1536", "medium",
        "Original documentary-looking 1970s 16mm film still: a distant, tall, dark "
        "shaggy bipedal forest creature crossing a rocky creek from left to right "
        "in a dense Pacific Northwest conifer forest. Full-body subject at mid-distance, "
        "partly hidden by foliage, uneven posture and natural anatomy. Handheld shaky "
        "camera, soft focus, heavy analog film grain, muted moss greens and faded color, "
        "imperfect exposure, unsettling but ambiguous found footage. No text, logos, "
        "framing, watermark, drawing, animation, or existing famous photograph."
    ),
    "alien": (
        "1024x1536", "medium",
        "Original 1980s analog flash photograph taken through the doorway of a dark "
        "abandoned farmhouse at night. A small gray humanoid with a slender body and "
        "large black eyes stands in the middle distance, partially obscured by the "
        "doorframe and tall weeds. Creepy ambiguous evidence photo, grainy high-ISO "
        "film, uneven flash falloff, washed shadow detail, imperfect focus, realistic "
        "camera optics, not a costume, not glamorous. No text, logos, watermark, "
        "framing, illustration, or movie reference."
    ),
    "ufo_infrared": (
        "1536x1024", "medium",
        "Original black-and-white airborne infrared camera still: a small, distant, "
        "dark oblong unidentified aerial object over rolling cloud layers, centered "
        "slightly below midframe. Authentic-looking low resolution thermal sensor "
        "noise, atmospheric haze, ambiguous shape, washed gray palette, high contrast "
        "object against pale sky, imperfect analog capture. No targeting reticle, "
        "telemetry text, labels, logo, watermark, poster layout or drawn art."
    ),
    "ufo_daylight": (
        "1536x1024", "medium",
        "Original photorealistic vintage daylight photograph of a metallic disc-shaped "
        "unidentified craft hovering above dry rolling hills and a distant scrubland "
        "valley. Three-quarter view of the underside, believable scale, bright hazy "
        "afternoon light and mild motion blur, sun-faded 1970s color film, softly "
        "grainy, restrained blue-beige palette, plausible documentary shot rather "
        "than polished science-fiction concept art. No people, writing, border, "
        "watermark, existing movie design or illustration."
    ),
}

LABELS = {
    "bigfoot": ("FOREST CROSSING", "TAPE 04  /  NORTH RIDGE"),
    "alien": ("NIGHT VISITOR", "CALLER SUBMISSION  /  UNVERIFIED"),
    "ufo_infrared": ("UNKNOWN CONTACT", "AIRBORNE SENSOR  /  FILE 12"),
    "ufo_daylight": ("SKY OVER THE VALLEY", "DAYLIGHT SIGHTING  /  FILE 27"),
}


def generate(subject):
    source = SOURCES / f"{subject}.png"
    if source.exists():
        print(f"Source already exists; skipping paid request: {source}")
        return True
    key = os.environ.get("OPENAI_API_KEY", "").strip()
    if not key:
        print("OPENAI_API_KEY is missing from this process; no image request sent.")
        return False

    size, quality, prompt = SUBJECTS[subject]
    body = json.dumps({
        "model": MODEL, "prompt": prompt, "n": 1, "size": size,
        "quality": quality, "background": "opaque", "output_format": "png"
    }).encode("utf-8")
    request = Request("https://api.openai.com/v1/images/generations", data=body,
                      headers={"Authorization": f"Bearer {key}",
                               "Content-Type": "application/json"}, method="POST")
    print(f"Generating {subject} with {MODEL} ({size}, {quality}); one paid request...")
    try:
        with urlopen(request, timeout=300) as response:
            data = json.load(response)
        encoded = data["data"][0]["b64_json"]
        raw = base64.b64decode(encoded, validate=True)
        with Image.open(BytesIO(raw)) as image:
            image.verify()
    except HTTPError as error:
        try:
            detail = json.load(error).get("error", {})
            print(f"Image API HTTP {error.code}: {detail.get('code') or detail.get('type') or 'request rejected'}")
        except (ValueError, OSError):
            print(f"Image API HTTP {error.code} (request rejected)")
        return False
    except (URLError, TimeoutError, KeyError, ValueError, OSError) as error:
        print(f"Image generation failed: {type(error).__name__}; check billing before retrying.")
        return False

    SOURCES.mkdir(parents=True, exist_ok=True)
    source.write_bytes(raw)
    print(f"Saved original source: {source}")
    return True


def compose(subject):
    source = SOURCES / f"{subject}.png"
    if not source.exists():
        return False
    title, caption = LABELS[subject]
    sheet, draw = paper(f"photo_{subject}", tone=(202, 188, 162))
    draw.rectangle((28, 27, 484, 108), fill=(65, 64, 62))
    face = font(31, bold=True)
    bounds = draw.textbbox((0, 0), title, font=face)
    draw.text(((512 - (bounds[2] - bounds[0])) // 2, 45), title, font=face, fill=CREAM)
    area = (44, 141, 468, 584)
    draw.rectangle((area[0] - 7, area[1] - 7, area[2] + 7, area[3] + 7),
                   fill=(52, 49, 45))
    with Image.open(source) as photo:
        photo = ImageOps.contain(photo.convert("RGB"), (area[2]-area[0], area[3]-area[1]),
                                 method=Image.Resampling.LANCZOS)
        x = area[0] + (area[2] - area[0] - photo.width) // 2
        y = area[1] + (area[3] - area[1] - photo.height) // 2
        sheet.paste(photo, (x, y))
    draw.text((49, 616), caption, font=font(17, mono=True), fill=INK)
    draw.text((49, 652), "KBTV  /  EVIDENCE ARCHIVE", font=font(15, mono=True), fill=RED)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    target = OUTPUT / f"photo_{subject}.png"
    sheet.save(target, optimize=True)
    print(f"Composed paper print: {target}")
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("subject", nargs="?", choices=tuple(SUBJECTS))
    parser.add_argument("--compose-only", action="store_true")
    args = parser.parse_args()
    subjects = (args.subject,) if args.subject else tuple(SUBJECTS)
    for subject in subjects:
        if args.compose_only or generate(subject):
            compose(subject)
        else:
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())

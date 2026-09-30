"""Package an existing preview_vern.gd --review directory. Requires Pillow.

Usage: python Tools/modelgen/package_vern_review.py <review-directory>
"""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageSequence


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    manifest = json.loads((args.directory / "manifest.json").read_text())
    gifs = []
    for clip in manifest["clips"]:
        for view in manifest.get("views", ("side", "threequarter", "contact")):
            stem = f'{clip["requested"]}_{view}'
            paths = sorted(args.directory.glob(stem + "_[0-9][0-9][0-9].png"))
            if not paths:
                raise RuntimeError(f"Missing frames for {stem}")
            frames = []
            for path in paths:
                with Image.open(path) as image:
                    frames.append(image.convert("RGB"))
            output = args.directory / (stem + ".gif")
            # GIF durations have 10 ms precision. Distribute rounding so the
            # 24 fps review does not silently play at 25 fps (or 8 at 8.33).
            durations = [10 * (round((i + 1) * 100 / manifest["fps"])
                               - round(i * 100 / manifest["fps"]))
                         for i in range(len(frames))]
            frames[0].save(output, save_all=True, append_images=frames[1:],
                           duration=durations, loop=0)
            with Image.open(output) as result:
                actual_ms = sum(frame.info.get("duration", 0)
                                for frame in ImageSequence.Iterator(result))
            if actual_ms != sum(durations):
                raise RuntimeError(f"Playback duration changed for {stem}: {actual_ms} ms")
            gifs.append(output.name)
    manifest["gifs"] = gifs
    (args.directory / "manifest.json").write_text(json.dumps(manifest, indent=2))
    print(f"Packaged {len(gifs)} GIFs in {args.directory}")


if __name__ == "__main__":
    main()

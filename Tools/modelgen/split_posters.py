"""Split the user's ten-panel poster sheet without resizing or altering the source.

Run from anywhere: python Tools/modelgen/split_posters.py
"""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "assets" / "textures" / "posters.png"
OUTPUT = ROOT / "assets" / "textures" / "world3d" / "wall_prints" / "poster_set"
# The poster frames are irregular: their edges shift by several pixels between
# columns and rows. Bounds are measured at each panel's outer printed border;
# the near-uniform gray between frames is deliberately excluded. PIL box ends
# are exclusive. Keep these per-poster measurements instead of dividing by 5.
POSTERS = (
    ("believe_ufo", (0, 0, 311, 484)),
    ("bigfoot", (317, 0, 617, 484)),
    ("lake_creature", (624, 0, 919, 485)),
    ("alien_coffee", (927, 0, 1224, 486)),
    ("truth_cat", (1232, 0, 1536, 484)),
    ("ghost_snacks", (0, 493, 311, 1000)),
    ("question_everything", (317, 493, 616, 1000)),
    ("mothman", (622, 494, 916, 1000)),
    ("abduction", (923, 493, 1226, 1000)),
    ("kbtv_talk", (1232, 497, 1536, 1000)),
)


def main():
    with Image.open(SOURCE) as sheet:
        if sheet.size != (1536, 1024):
            raise ValueError(f"Unexpected source size {sheet.size}; inspect the grid before cropping")
        OUTPUT.mkdir(parents=True, exist_ok=True)
        for name, bounds in POSTERS:
            image = sheet.crop(bounds)
            destination = OUTPUT / f"{name}.png"
            image.save(destination, optimize=True)
            print(f"{name}: {bounds} -> {image.width}x{image.height}")


if __name__ == "__main__":
    main()

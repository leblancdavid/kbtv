"""Pack Godot-rendered talk_calm_mpfb frames into a feed GIF + contact sheet."""
import sys
from pathlib import Path
from PIL import Image, ImageDraw


def pack(source: Path, output: Path, prefix: str = 'vern') -> None:
    feed = sorted(source.glob('talk_calm_mpfb_feed_*.png'))
    frames = [Image.open(p).convert('RGB') for p in feed]
    frames[0].save(output / f'{prefix}_talk_calm_mpfb_feed.gif', save_all=True,
        append_images=frames[1:], duration=83, loop=0, optimize=False)

    front = sorted(source.glob('talk_calm_mpfb_front_*.png'))
    selected = [front[round(i * (len(front) - 1) / 7)] for i in range(8)]
    cell_w = 320
    cell_h = 204
    image_h = 180
    sheet = Image.new('RGB', (cell_w * 2, 4 * cell_h), '#242730')
    draw = ImageDraw.Draw(sheet)
    for i, path in enumerate(selected):
        image = Image.open(path).convert('RGB').resize((cell_w, image_h), Image.Resampling.LANCZOS)
        sheet.paste(image, ((i % 2) * cell_w, (i // 2) * cell_h + 24))
        draw.text(((i % 2) * cell_w + 8, (i // 2) * cell_h + 5),
            f'talk_calm_mpfb | {int(path.stem.split("_")[-1]) / 12:.2f}s', fill='white')
    sheet.save(output / f'{prefix}_talk_calm_mpfb_sheet.png')


if __name__ == '__main__':
    pack(Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3] if len(sys.argv) > 3 else 'vern')

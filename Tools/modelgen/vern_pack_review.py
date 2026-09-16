"""Package Blender render frames; requires Pillow in system Python."""
import sys
from pathlib import Path
from PIL import Image, ImageDraw


def pack(source, output, prefix='vern'):
    for clip in ('idle_breathing', 'talking_default', 'smoking', 'drink_coffee'):
        for view in ('wide', 'feed'):
            paths = sorted(source.glob(f'{clip}_{view}_*.png'))
            frames = [Image.open(p).convert('RGB') for p in paths]
            if clip in ('idle_breathing', 'talking_default'):
                frames = frames[:-1]*2
            frames[0].save(output / f'{prefix}_{clip}_{view}.gif', save_all=True,
                append_images=frames[1:], duration=[83, 83, 84]*(len(frames)//3)+[83]*(len(frames)%3),
                loop=0, optimize=False)
        paths = sorted(source.glob(f'{clip}_front_*.png'))
        if not paths:
            paths = sorted(source.glob(f'{clip}_feed_*.png'))
            selected = [paths[round(i*(len(paths)-1)/7)] for i in range(8)]
            sheet = Image.new('RGB', (640, 4*204), '#242730')
            draw = ImageDraw.Draw(sheet)
            for i, path in enumerate(selected):
                sheet.paste(Image.open(path), ((i%2)*320, (i//2)*204+24))
                draw.text(((i%2)*320+8, (i//2)*204+5), f'{clip} | {int(path.stem.split("_")[-1])/12:.2f}s', fill='white')
            sheet.save(output / f'{prefix}_{clip}_sheet.png')
            continue
        sheet = Image.new('RGB', (960, 388*len(paths)), '#242730')
        draw = ImageDraw.Draw(sheet)
        for row, front in enumerate(paths):
            for column, view in enumerate(('front', 'side')):
                path = front.with_name(front.name.replace('_front_', f'_{view}_'))
                image = Image.open(path).convert('RGB')
                sheet.paste(image, (column*480, row*388+28))
                time = int(front.stem.split('_')[-1])/12
                draw.text((column*480+10, row*388+7), f'{clip} | {view} | {time:.3f}s', fill='white')
        sheet.save(output / f'vern_{clip}_contacts.png')


if __name__ == '__main__':
    pack(Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3] if len(sys.argv) > 3 else 'vern')

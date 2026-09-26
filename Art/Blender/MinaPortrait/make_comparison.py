"""Side-by-side board: the painted NpcPortrait.png (cropped exactly as OutgameUI shows it)
next to the lowpoly Mina_Portrait.png, at 2x (808x784) and at the 404x391 display size.

    python Art/Blender/MinaPortrait/make_comparison.py [--render PNG] [--out PNG]

Default output: Art/Blender/previews/mina-vs-painting.png. OutgameUI.cs crops the
1448x1086 painting to the 404:391 slot with a centred uvRect; the same crop is used
here. The display-size pair sits on the portrait frame colour (PrepWhite) of the
preparation screen. This script only arranges existing PNGs (Pillow).
"""
from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PAINTING = ROOT/'Assets/CottonCircuit/Resources/Progression/NpcPortrait.png'
RENDER = ROOT/'Assets/CottonCircuit/Sprites/Characters/Mina_Portrait.png'
OUT = HERE.parent/'previews'/'mina-vs-painting.png'
SLOT = (404, 391)
PAPER, CARD, TEXT, MUTED = '#F5F0E7', '#FFFDF8', '#29324D', '#737482'
FRAME = '#FFFCF5'  # OutgameUI PrepWhite behind the portrait slot


def font(size: int, bold: bool = False):
    for name in (('segoeuib.ttf', 'arialbd.ttf') if bold else ('segoeui.ttf', 'arial.ttf')):
        path = Path('C:/Windows/Fonts')/name
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default(size=size)


def slot_crop(image: Image.Image) -> Image.Image:
    """The centred uvRect crop of OutgameUI.cs for a texture shown in the 404x391 slot."""
    ratio = SLOT[0]/SLOT[1]/(image.width/image.height)
    if ratio <= 1:
        width = image.width*ratio
        box = ((image.width-width)/2, 0, (image.width+width)/2, image.height)
    else:
        height = image.height/ratio
        box = (0, (image.height-height)/2, image.width, (image.height+height)/2)
    return image.crop(tuple(round(v) for v in box))


def board(painting: Image.Image, render: Image.Image) -> Image.Image:
    big = (SLOT[0]*2, SLOT[1]*2)
    left = slot_crop(painting.convert('RGB')).resize(big, Image.Resampling.LANCZOS)
    right = render.convert('RGB').resize(big, Image.Resampling.LANCZOS) if render.size != big else render.convert('RGB')
    small = [im.resize(SLOT, Image.Resampling.LANCZOS) for im in (left, right)]
    pad, gap = 28, 24
    width = pad*2+big[0]*2+gap
    height = 96+big[1]+56+SLOT[1]+14*2+60
    canvas = Image.new('RGB', (width, height), PAPER)
    draw = ImageDraw.Draw(canvas)
    draw.text((pad, 22), 'MINA  |  PAINTED PORTRAIT vs LOWPOLY RENDER', fill='#58796F', font=font(20, True))
    draw.text((pad, 52), 'Left: NpcPortrait.png with the OutgameUI slot crop. Right: Mina_Portrait.png. '
              'Top 2x (808x784), bottom display size 404x391 on the portrait frame.', fill=MUTED, font=font(15))
    y = 96
    for i, (image, label) in enumerate(((left, 'painting, slot crop, 2x'), (right, 'lowpoly render, 2x'))):
        x = pad+i*(big[0]+gap)
        canvas.paste(image, (x, y))
        draw.text((x, y+big[1]+8), label, fill=TEXT, font=font(16, True))
    y += big[1]+56
    for i, (image, label) in enumerate(((small[0], 'painting 404x391'), (small[1], 'render 404x391'))):
        x = pad+i*(big[0]+gap)+(big[0]-SLOT[0]-28)//2
        draw.rectangle((x, y, x+SLOT[0]+13, y+SLOT[1]+13), fill=FRAME)
        canvas.paste(image, (x+7, y+7))
        draw.text((x, y+SLOT[1]+22), label, fill=TEXT, font=font(15, True))
    return canvas


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--render', type=Path, default=RENDER)
    parser.add_argument('--out', type=Path, default=OUT)
    args = parser.parse_args(argv)
    with Image.open(PAINTING) as painting, Image.open(args.render) as render:
        image = board(painting, render)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    image.save(args.out, optimize=True)
    print('MINA_COMPARISON %s (%d x %d)' % (args.out, image.width, image.height))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())

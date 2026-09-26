"""Review boards for the UI sprites: every catalog sprite at 2x (its canvas) and at every display size.

    python Art/Blender/preview_ui_sprites.py                    -> Art/Blender/previews/all.png
    python Art/Blender/preview_ui_sprites.py --category items   -> Art/Blender/previews/items.png

Transparent sprites are shown on the cream #FFF6E7 and ink #29324D review backgrounds;
trait icons additionally sit on all six category disc colours with the 64 px disc /
42 px icon geometry of OutgameTraitsUI. Display sizes use aspect-preserving contain
fit, centred in a box of the slot size. Missing sprites are labelled grey boxes.
The board composites in sRGB; Unity blends in linear space, so the final call on
readability is a game capture. This script only arranges existing PNGs.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_spec as spec

CATEGORIES = ('customers', 'mina', 'items', 'shop', 'locations', 'machines', 'icons')
CREAM, INK = spec.REVIEW_BACKGROUNDS['cream'], spec.REVIEW_BACKGROUNDS['ink']
PAPER, TEXT, MUTED, RULE = '#F5F0E7', '#29324D', '#737482', '#E5DED1'
CARD, MISSING, PIVOT = '#FFFDF8', '#C9C6C0', '#C2577E'
BOARD_WIDTH, GAP, PAD, INSET = 2400, 16, 14, 6
DISC, ICON, ICON_OFFSET = 64, 42, (11, 8)  # OutgameTraitsUI: disc (56,0,64,64), icon (67,8,42,42)


def font(size: int, bold: bool = False):
    for name in (('segoeuib.ttf', 'arialbd.ttf') if bold else ('segoeui.ttf', 'arial.ttf')):
        path = Path('C:/Windows/Fonts')/name
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default(size=size)


def contain(image: Image.Image, box: tuple[int, int]) -> Image.Image:
    scale = min(box[0]/image.width, box[1]/image.height)
    size = (max(1, round(image.width*scale)), max(1, round(image.height*scale)))
    return image if size == image.size else image.resize(size, Image.Resampling.LANCZOS)


def panel(image: Image.Image | None, box: tuple[int, int], background: str, label: str,
          pivot: tuple[float, float] | None = None) -> Image.Image:
    """One background swatch holding `image` contain-fitted and centred in a box of `box` pixels."""
    width, height = box[0]+2*INSET, box[1]+2*INSET
    face = font(12)
    text = ImageDraw.Draw(Image.new('RGBA', (1, 1))).textlength(label, font=face)
    tile = Image.new('RGBA', (max(width, 96, int(text)+8), height+22), CARD)
    left = (tile.width-width)//2
    draw = ImageDraw.Draw(tile)
    draw.rectangle((left, 0, left+width-1, height-1), fill=background)
    if image is None:
        mark = font(13 if box[0] >= 64 else 9, True)
        draw.text((left+(width-draw.textlength('MISSING', font=mark))/2, height/2-9), 'MISSING', fill=TEXT, font=mark)
    else:
        fitted = contain(image, box)
        x = left+INSET+(box[0]-fitted.width)//2
        y = INSET+(box[1]-fitted.height)//2
        tile.alpha_composite(fitted, (x, y))
        if pivot:
            px, py = x+pivot[0]*fitted.width, y+(1-pivot[1])*fitted.height
            draw.line((px, height-INSET+1, px, height-1), fill=PIVOT, width=2)
            draw.line((left, py, left+INSET-2, py), fill=PIVOT, width=2)
            draw.line((left+width-INSET+1, py, left+width-1, py), fill=PIVOT, width=2)
    draw.text(((tile.width-text)/2, height+4), label, fill=MUTED, font=face)
    return tile


def disc_tile(icon: Image.Image, category: str) -> Image.Image:
    size = DISC+2*INSET
    tile = Image.new('RGBA', (max(size, 84), size+22), CARD)
    left = (tile.width-size)//2
    ImageDraw.Draw(tile).rectangle((left, 0, left+size-1, size-1), fill=CREAM)
    scale = 4  # draw the disc supersampled so its edge is smooth like the UI sprite
    disc = Image.new('RGBA', (DISC*scale, DISC*scale), (0, 0, 0, 0))
    ImageDraw.Draw(disc).ellipse((0, 0, DISC*scale-1, DISC*scale-1), fill=spec.DISC_COLORS[category])
    tile.alpha_composite(disc.resize((DISC, DISC), Image.Resampling.LANCZOS), (left+INSET, INSET))
    tile.alpha_composite(contain(icon, (ICON, ICON)), (left+INSET+ICON_OFFSET[0], INSET+ICON_OFFSET[1]))
    draw = ImageDraw.Draw(tile)
    face = font(12)
    draw.text(((tile.width-draw.textlength(category, font=face))/2, size+4), category, fill=MUTED, font=face)
    return tile


def row(tiles: list[Image.Image], gap: int = 8) -> Image.Image:
    strip = Image.new('RGBA', (sum(t.width for t in tiles)+gap*(len(tiles)-1), max(t.height for t in tiles)), CARD)
    x = 0
    for tile in tiles:
        strip.alpha_composite(tile, (x, 0))
        x += tile.width+gap
    return strip


def card(entry: dict, root: Path) -> Image.Image:
    path = root/spec.sprite_path(entry)
    displays = ', '.join('%dx%d' % tuple(d) for d in entry['displays'])
    subtitle = '%s  |  canvas %dx%d  |  display %s' % (entry['kind'], *entry['canvas'], displays)
    rows = []
    if not path.is_file():
        rows.append(panel(None, tuple(entry['displays'][0]), MISSING, spec.sprite_path(entry)))
    else:
        with Image.open(path) as source:
            sprite = source.convert('RGBA')
        opaque = spec.KIND_RULES[entry['kind']]['opaque']
        backgrounds = ((CREAM, 'cream'),) if opaque else ((CREAM, 'cream'), (INK, 'ink'))
        tiles = [panel(sprite, sprite.size, color, '2x %dx%d %s' % (*sprite.size, name), entry['pivot'])
                 for color, name in backgrounds]
        for display in entry['displays']:
            tiles += [panel(sprite, tuple(display), color, '%dx%d %s' % (*display, name))
                      for color, name in backgrounds]
        rows.append(row(tiles))
        if entry['kind'] == 'icon':
            rows.append(row([disc_tile(sprite, category) for category in spec.DISC_COLORS]))
    width = max([r.width for r in rows]+[ImageDraw.Draw(Image.new('RGBA', (1, 1))).textlength(subtitle, font=font(12))])
    height = 50+sum(r.height+10 for r in rows)
    image = Image.new('RGBA', (int(width)+2*PAD, height+PAD), CARD)
    draw = ImageDraw.Draw(image)
    draw.text((PAD, PAD-2), entry['id'], fill=TEXT, font=font(17, True))
    draw.text((PAD, PAD+22), subtitle, fill=MUTED, font=font(12))
    y = PAD+46
    for strip in rows:
        image.alpha_composite(strip, (PAD, y))
        y += strip.height+10
    return image


def board(categories: list[str], root: Path) -> Image.Image:
    sections = []
    for category in categories:
        entries = spec.owned_by(category)
        present = sum((root/spec.sprite_path(e)).is_file() for e in entries)
        sections.append(('%s  |  %d sprites, %d present' % (category.upper(), len(entries), present),
                         [card(e, root) for e in entries]))
    width = max([BOARD_WIDTH]+[c.width+2*GAP for _, cards in sections for c in cards])
    placements, y = [], 96
    for title, cards in sections:
        placements.append(('title', title, (GAP, y)))
        y += 40
        x, row_height = GAP, 0
        for tile in cards:
            if x > GAP and x+tile.width > width-GAP:
                x, y, row_height = GAP, y+row_height+GAP, 0
            placements.append(('card', tile, (x, y)))
            x += tile.width+GAP
            row_height = max(row_height, tile.height)
        y += row_height+2*GAP
    canvas = Image.new('RGBA', (width, y), PAPER)
    draw = ImageDraw.Draw(canvas)
    draw.text((GAP, 18), 'COTTON CIRCUIT  |  UI SPRITE REVIEW', fill='#58796F', font=font(17, True))
    draw.text((GAP, 44), 'Canvas (2x UI) and every display slot on cream %s and ink %s; icons on the six trait discs.'
              % (CREAM, INK), fill=MUTED, font=font(16))
    for kind, item, (x, top) in placements:
        if kind == 'title':
            draw.line((GAP, top-6, width-GAP, top-6), fill=RULE, width=2)
            draw.text((x, top), item, fill=TEXT, font=font(20, True))
        else:
            canvas.alpha_composite(item, (x, top))
    return canvas.convert('RGB')


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--category', choices=CATEGORIES)
    parser.add_argument('--root', type=Path, default=ROOT, help=argparse.SUPPRESS)
    parser.add_argument('--out-dir', type=Path, default=ART/'previews', help=argparse.SUPPRESS)
    args = parser.parse_args(argv)
    categories = [args.category] if args.category else list(CATEGORIES)
    image = board(categories, args.root)
    args.out_dir.mkdir(parents=True, exist_ok=True)
    path = args.out_dir/((args.category or 'all') + '.png')
    image.save(path, optimize=True)
    print('UI_SPRITE_PREVIEW %s (%d x %d)' % (path, image.width, image.height))
    return 0


if __name__ == '__main__':
    sys.exit(main())

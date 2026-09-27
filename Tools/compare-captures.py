"""Compare smoke-test captures pixel by pixel (Pillow + numpy).

Folder mode:

    python Tools/compare-captures.py <before_dir> <after_dir> <out_dir> [--threshold N] [--regions]

For every PNG name present in both folders the script writes <out_dir>/<name>, a
side-by-side image (before on the left, after on the right, a label strip on top),
and prints one tab-separated row:

    name  size  mean_abs_rgb  changed_pct  changed_bbox

- mean_abs_rgb: mean absolute difference over every pixel and RGB channel, on the
  0-255 scale (alpha is ignored; the captures are opaque).
- changed_pct: percentage of pixels whose largest channel difference exceeds
  --threshold (default 0, so any change counts).
- changed_bbox: left,top,right,bottom of the changed pixels, or '-' when none.

Pairs with different sizes are still written side by side and reported as
'size mismatch'; names found in only one folder are listed at the end. The same
table is written to <out_dir>/summary.tsv. With --regions the changed pixels of
every same-size pair are grouped on an 8 px grid (cells that touch in any of the
8 directions form one region) and <out_dir>/regions.tsv lists each region as
name, left, top, right, bottom, changed pixels. Use it to check that the changes
stay inside the art slots; a --threshold such as 8 ignores anti-aliasing noise.

Board mode (the documentation images):

    python Tools/compare-captures.py --board <out.png> [--scale S] [--title TEXT]
        <before.png> <after.png> [<before.png> <after.png> ...]

Stacks one before | after row per pair into a single PNG. A path may end in
@x,y,w,h to use only that rectangle of the capture, and the before path may be '-'
when no before capture exists (the row then shows only the after panel).
--scale resizes every panel (Lanczos; 1 keeps the game pixels). Each row is
labelled with the two file names and, when the pair has the same size, its
mean_abs_rgb and changed_pct.

Both modes only read their inputs and exit 0 once they have run (bad arguments exit 2).
"""
from __future__ import annotations

import argparse
import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

GAP = 12
STRIP = 30
CELL = 8
PAPER = (245, 240, 231)
INK = (41, 50, 77)
MUTED = (115, 116, 130)
HEADER = ('name', 'size', 'mean_abs_rgb', 'changed_pct', 'changed_bbox')
REGION_HEADER = ('name', 'left', 'top', 'right', 'bottom', 'changed_px')


def font(size: int):
    for path in (Path('C:/Windows/Fonts/malgun.ttf'), Path('C:/Windows/Fonts/arial.ttf'),
                 Path('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf')):
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def load_rgb(path: Path) -> Image.Image:
    with Image.open(path) as image:
        return image.convert('RGB')


def changed_mask(before: Image.Image, after: Image.Image, threshold: int) -> tuple[np.ndarray, np.ndarray]:
    delta = np.abs(np.asarray(before, dtype=np.int16) - np.asarray(after, dtype=np.int16))
    return delta, delta.max(axis=2) > threshold


def difference(before: Image.Image, after: Image.Image, threshold: int) -> tuple[float, float, str]:
    """Mean absolute RGB difference, changed-pixel percentage and changed bounding box."""
    delta, changed = changed_mask(before, after, threshold)
    if not changed.any():
        return float(delta.mean()), 0.0, '-'
    rows, cols = np.nonzero(changed.any(axis=1))[0], np.nonzero(changed.any(axis=0))[0]
    bbox = '%d,%d,%d,%d' % (cols[0], rows[0], cols[-1]+1, rows[-1]+1)
    return float(delta.mean()), 100.0*float(changed.mean()), bbox


def changed_regions(before: Image.Image, after: Image.Image, threshold: int) -> list[tuple[int, ...]]:
    """(left, top, right, bottom, changed pixels) for each 8-connected group of changed CELL x CELL cells."""
    _, changed = changed_mask(before, after, threshold)
    height, width = changed.shape
    rows, cols = -(-height//CELL), -(-width//CELL)
    padded = np.zeros((rows*CELL, cols*CELL), dtype=bool)
    padded[:height, :width] = changed
    grid = padded.reshape(rows, CELL, cols, CELL).any(axis=(1, 3))
    seen = np.zeros_like(grid)
    regions = []
    for start in zip(*np.nonzero(grid)):
        if seen[start]:
            continue
        seen[start] = True
        queue, cells = deque([start]), []
        while queue:
            y, x = queue.popleft()
            cells.append((y, x))
            for ny in range(max(y-1, 0), min(y+2, rows)):
                for nx in range(max(x-1, 0), min(x+2, cols)):
                    if grid[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        queue.append((ny, nx))
        ys, xs = [int(cell[0]) for cell in cells], [int(cell[1]) for cell in cells]
        box = (min(xs)*CELL, min(ys)*CELL, min((max(xs)+1)*CELL, width), min((max(ys)+1)*CELL, height))
        regions.append((*box, int(changed[box[1]:box[3], box[0]:box[2]].sum())))
    return sorted(regions, key=lambda region: (region[1], region[0]))


def side_by_side(before: Image.Image | None, after: Image.Image, labels: tuple[str, str]) -> Image.Image:
    """before | after with a label strip; a missing before keeps only its label."""
    face = font(16)
    texts = [face.getbbox(text)[2]+16 for text in labels]
    left = max(before.width, texts[0]) if before else texts[0]
    width = left+GAP+max(after.width, texts[1])
    board = Image.new('RGB', (width, STRIP+max(before.height if before else 0, after.height)), PAPER)
    draw = ImageDraw.Draw(board)
    for x, image, text in ((0, before, labels[0]), (left+GAP, after, labels[1])):
        draw.text((x+8, 6), text, fill=INK if image else MUTED, font=face)
        if image:
            board.paste(image, (x, STRIP))
    return board


def compare(before_dir: Path, after_dir: Path, out_dir: Path, threshold: int,
            regions: list[tuple] | None = None) -> list[tuple[str, ...]]:
    """Write the side-by-side images and return the table rows; collect changed regions when asked."""
    before_names = {path.name for path in before_dir.glob('*.png')}
    after_names = {path.name for path in after_dir.glob('*.png')}
    out_dir.mkdir(parents=True, exist_ok=True)
    rows = []
    for name in sorted(before_names & after_names):
        before, after = load_rgb(before_dir/name), load_rgb(after_dir/name)
        labels = ('before  %s/%s' % (before_dir.name, name), 'after  %s/%s' % (after_dir.name, name))
        side_by_side(before, after, labels).save(out_dir/name, optimize=True)
        size = '%dx%d' % after.size
        if before.size != after.size:
            rows.append((name, '%dx%d -> %s' % (*before.size, size), 'size mismatch', '-', '-'))
            continue
        mean, percent, bbox = difference(before, after, threshold)
        rows.append((name, size, '%.3f' % mean, '%.3f' % percent, bbox))
        if regions is not None:
            regions.extend((name, *region) for region in changed_regions(before, after, threshold))
    for name in sorted(before_names ^ after_names):
        rows.append((name, '-', 'only in ' + ('before' if name in before_names else 'after'), '-', '-'))
    return rows


def panel(spec: str) -> tuple[str, Image.Image | None]:
    """'path[@x,y,w,h]' -> (label, image); '-' -> ('no before capture', None)."""
    if spec == '-':
        return 'no before capture', None
    path, _, rect = spec.partition('@')
    image = load_rgb(Path(path))
    label = '/'.join(Path(path).parts[-2:])
    if rect:
        x, y, w, h = (int(value) for value in rect.split(','))
        if x < 0 or y < 0 or w <= 0 or h <= 0 or x+w > image.width or y+h > image.height:
            print('crop %s is outside %s (%dx%d)' % (rect, path, image.width, image.height), file=sys.stderr)
            raise SystemExit(2)
        image = image.crop((x, y, x+w, y+h))
        label += ' @' + rect
    return label, image


def board(specs: list[str], out: Path, scale: float, title: str | None, threshold: int) -> list[str]:
    rows, report = [], []
    for before_spec, after_spec in zip(specs[::2], specs[1::2]):
        (before_label, before), (after_label, after) = panel(before_spec), panel(after_spec)
        stats = ''
        if before is not None and before.size == after.size:
            mean, percent, _ = difference(before, after, threshold)
            stats = '   mean %.2f, changed %.1f%%' % (mean, percent)
        report.append('%s -> %s%s' % (before_label, after_label, stats))
        if scale != 1:
            resize = lambda image: image.resize((round(image.width*scale), round(image.height*scale)),
                                                Image.Resampling.LANCZOS)
            before, after = (before and resize(before)), resize(after)
        rows.append(side_by_side(before, after, ('before  ' + before_label, 'after  ' + after_label + stats)))
    top = 44 if title else 0
    canvas = Image.new('RGB', (max(row.width for row in rows),
                               top+sum(row.height for row in rows)+GAP*(len(rows)-1)), PAPER)
    if title:
        ImageDraw.Draw(canvas).text((8, 8), title, fill=INK, font=font(22))
    y = top
    for row in rows:
        canvas.paste(row, (0, y))
        y += row.height+GAP
    out.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(out, optimize=True)
    return report+['COMPARE_BOARD %s (%dx%d, %d rows)' % (out, canvas.width, canvas.height, len(rows))]


def write_table(path: Path, header: tuple[str, ...], rows: list[tuple]) -> list[str]:
    lines = ['\t'.join(header)]+['\t'.join(str(value) for value in row) for row in rows]
    path.write_text('\n'.join(lines)+'\n', encoding='utf-8')
    return lines


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('paths', nargs='+', help='before_dir after_dir out_dir, or with --board: before after pairs')
    parser.add_argument('--threshold', type=int, default=0,
                        help='a pixel counts as changed when a channel differs by more than this (default 0)')
    parser.add_argument('--regions', action='store_true', help='folder mode: also write <out_dir>/regions.tsv')
    parser.add_argument('--board', type=Path, help='write one stacked before | after board to this PNG')
    parser.add_argument('--scale', type=float, default=1.0, help='board panel scale (default 1)')
    parser.add_argument('--title', help='board title line')
    args = parser.parse_args(argv)
    if args.board:
        if len(args.paths) % 2 or args.scale <= 0:
            parser.error('--board needs before/after pairs and a positive --scale')
        print('\n'.join(board(args.paths, args.board, args.scale, args.title, args.threshold)))
        return 0
    if len(args.paths) != 3:
        parser.error('folder mode needs before_dir after_dir out_dir')
    before_dir, after_dir, out_dir = (Path(path) for path in args.paths)
    for folder in (before_dir, after_dir):
        if not folder.is_dir():
            parser.error('not a folder: %s' % folder)
    regions = [] if args.regions else None
    rows = compare(before_dir, after_dir, out_dir, args.threshold, regions)
    print('\n'.join(write_table(out_dir/'summary.tsv', HEADER, rows)))
    pairs = sum(1 for row in rows if not row[2].startswith('only in'))
    print('COMPARE_CAPTURES pairs=%d unmatched=%d out=%s' % (pairs, len(rows)-pairs, out_dir))
    if regions is not None:
        write_table(out_dir/'regions.tsv', REGION_HEADER, regions)
        print('COMPARE_REGIONS regions=%d out=%s' % (len(regions), out_dir/'regions.tsv'))
    return 0


if __name__ == '__main__':
    sys.exit(main())

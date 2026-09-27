"""Measure the Customer_V1 polish points on the rendered sprites (plain Python, no Blender).

    python Art/Blender/GameCustomerChild/measure_child.py [--sprites DIR] [--manifest FILE]

1. Brow strength: at the 82x140 display size, how many grey levels the angry sprite is
   darker than the neutral one, per brow (left/right half of the changed pixels). The
   review's operator is a box (area) downscale and the mean of R, G and B; it gives 99 for
   V0 and 85 for V2. Two other operators (box and Lanczos with Rec.601 luma) are printed
   for comparison, with the summed darkening ("ink", in fully dark pixels).
2. Height: the child's alpha>128 height over the male's (unit test rule: below 0.85).
3. Head parts from manifest.json: no hood ears, no bowl dome or cowlick, wedge hair locks.
Exits 1 when a check fails. --sprites/--manifest point at another copy (e.g. the pre-polish files).
"""
import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
SPRITES = ROOT/'Assets/CottonCircuit/Sprites/Customers'
MANIFEST = Path(__file__).resolve().parent/'manifest.json'
DISPLAY, MIN_BROW, MAX_HEIGHT = (82, 140), 80, .85
OPERATORS = {'box mean-RGB (review)': (Image.Resampling.BOX, lambda a: a[..., :3].mean(-1)),
             'box Rec.601 luma': (Image.Resampling.BOX, lambda a: a[..., :3] @ [.299, .587, .114]),
             'lanczos Rec.601 luma': (Image.Resampling.LANCZOS, lambda a: a[..., :3] @ [.299, .587, .114])}


def sprite(folder, variant, expression):
    return Image.open(folder/('Customer_V%d_%s.png' % (variant, expression))).convert('RGBA')


def brows(folder, variant, resample, grey):
    """(left max, right max, summed darkening) of angry vs neutral on the cream review background."""
    flat = []
    for expression in ('Neutral', 'Angry'):
        base = Image.new('RGBA', DISPLAY, (0xFF, 0xF6, 0xE7, 255))
        base.alpha_composite(sprite(folder, variant, expression).resize(DISPLAY, resample))
        flat.append(grey(np.asarray(base, dtype=np.float64)))
    darker = flat[0]-flat[1]
    xs = np.nonzero(darker > 24)[1]
    mid = int(np.median(xs))
    return darker[:, :mid].max(), darker[:, mid:].max(), np.clip(darker, 0, None).sum()/255


def height(folder, variant):
    return int(np.ptp(np.nonzero(np.asarray(sprite(folder, variant, 'Neutral'))[..., 3] > 128)[0]))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--sprites', type=Path, default=SPRITES, help='folder holding the V1 PNGs')
    parser.add_argument('--manifest', type=Path, default=MANIFEST)
    args = parser.parse_args()
    failures = []
    for name, (resample, grey) in OPERATORS.items():
        row = []
        for variant in (0, 1, 2):
            left, right, ink = brows(args.sprites if variant == 1 else SPRITES, variant, resample, grey)
            row.append('V%d L%3.0f R%3.0f ink %4.1f' % (variant, left, right, ink))
            if variant == 1 and name.endswith('(review)') and min(left, right) < MIN_BROW:
                failures.append('V1 brows darken by %.0f/%.0f < %d grey levels' % (left, right, MIN_BROW))
        print('brows %-22s %s' % (name, ' | '.join(row)))
    ratio = height(args.sprites, 1)/height(SPRITES, 0)
    print('height V1/V0 %.3f' % ratio)
    if ratio >= MAX_HEIGHT:
        failures.append('child height ratio %.3f >= %.2f' % (ratio, MAX_HEIGHT))
    head = json.loads(args.manifest.read_text(encoding='utf-8'))['collections']['Head_Hair']
    hoodie = json.loads(args.manifest.read_text(encoding='utf-8'))['collections']['Hoodie']
    ears = [n for n in hoodie+head if 'Hood_Ear' in n]
    dome = [n for n in head if 'Bowl' in n or 'Cowlick' in n]
    locks = [n for n in head if n.startswith(('CH_Hair_Crown', 'CH_Hair_Side', 'CH_Hair_Fringe'))]
    print('hood ears %s | dome/cowlick %s | wedge locks %d' % (ears or 'none', dome or 'none', len(locks)))
    if ears:
        failures.append('hood ears present: %s' % ears)
    if dome or len(locks) < 8:
        failures.append('haircut is not the wedge-lock cut (dome %s, %d locks)' % (dome, len(locks)))
    for failure in failures:
        print('FAIL', failure)
    print('CHILD_POLISH %s' % ('FAIL' if failures else 'PASS'))
    return 1 if failures else 0


if __name__ == '__main__':
    sys.exit(main())

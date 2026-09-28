"""Derive the 6- and 9-clip fan stands from the 12-clip CottonCandyFanStand.png (Pillow + numpy).

    python Tools/fan-stand-variants.py

The business rack shows one clip per owned shelf slot: 6 at the start, 9 and 12 after
each shelf expansion. The 12-clip stand is the generated source. This script removes
whole arms (clip and wire) from it so the other two stands keep the same metal,
lighting and clip positions, and the USS slot positions keep matching the clips:

- 6 clips keep U2 U4 U6 (upper row) and L1 L3 L5 (lower row), the clips that
  stock0-5 use in Business.uss.
- 9 clips add U3 U5 L6, the clips of stock6-8.

Where a removed clip hid a kept wire (L2 over L1's wire, L4 over U6's wire) the hidden
stretch is repainted along a Hermite curve with the wire's own cross-section, blended
into the original wire at both ends. Erased pixels take the surrounding faint backdrop
so no hard edge is left. The script is deterministic; rerun it when the source changes.
"""
from collections import deque
import os
import sys

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, 'Assets', 'CottonCircuit', 'UI', 'Art')
SOURCE = os.path.join(ART, 'CottonCandyFanStand.png')

src = np.array(Image.open(SOURCE).convert('RGBA')).astype(np.float64)
H, W = src.shape[:2]
ALPHA = src[:, :, 3] / 255.0
PREMULTIPLIED = src.copy()
PREMULTIPLIED[:, :, :3] *= PREMULTIPLIED[:, :, 3:4] / 255.0
yy, xx = np.mgrid[0:H, 0:W]
VISIBLE = ALPHA > 0.004
COLLAR = (xx >= 857) & (xx <= 918) & (yy >= 594)

# Clip centres in the source, upper row U1-U6 and lower row L1-L6 from left to right.
CLIP_ANCHORS = {
    'U1': (173, 310), 'U2': (458, 256), 'U3': (750, 229), 'U4': (1024, 230), 'U5': (1316, 256), 'U6': (1601, 310),
    'L1': (231, 601), 'L2': (490, 559), 'L3': (762, 549), 'L4': (1012, 547), 'L5': (1284, 559), 'L6': (1543, 601),
}
# A point on each visible wire stretch and its direction toward the hub. A wire that
# passes behind another clip is split there (U1 behind L3, U6 behind L4, L1 behind L2,
# L6 behind L5); L12 and L56 are the stretches shared by two lower clips.
WIRES = {
    'U1a': ((240, 385.5), (1, .6)), 'U1b': ((790, 561), (1, .55)),
    'U2': ((490, 305), (1, .9)), 'U3': ((772, 275), (.3, 1)), 'U4': ((1003, 275), (-.3, 1)), 'U5': ((1285, 305), (-1, .9)),
    'U6a': ((1535, 384.5), (-1, .6)), 'U6b': ((985, 560), (-1, .55)),
    'L1a': ((290, 634), (1, -.2)), 'L12': ((530, 584), (1, -.2)), 'L3': ((790, 584.5), (1, .4)), 'L4': ((985, 584.5), (-1, .4)),
    'L6a': ((1480, 634), (-1, -.2)), 'L56': ((1245, 583.5), (-1, -.12)),
}
VARIANTS = {
    'CottonCandyFanStand6.png': (['U1', 'U3', 'U5', 'L2', 'L4', 'L6'], ['U1a', 'U1b', 'U3', 'U5', 'L4', 'L6a']),
    'CottonCandyFanStand9.png': (['U1', 'L2', 'L4'], ['U1a', 'U1b', 'L4']),
}
BRIDGES = [('L1a', 'L12'), ('U6a', 'U6b')]


def label(mask):
    """8-connected component labels of a boolean mask."""
    result = np.zeros(mask.shape, np.int32)
    count = 0
    for y0, x0 in zip(*np.nonzero(mask)):
        if result[y0, x0]:
            continue
        count += 1
        result[y0, x0] = count
        queue = deque([(y0, x0)])
        while queue:
            y, x = queue.popleft()
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    ny, nx = y + dy, x + dx
                    if 0 <= ny < H and 0 <= nx < W and mask[ny, nx] and not result[ny, nx]:
                        result[ny, nx] = count
                        queue.append((ny, nx))
    return result, count


def morph(mask, radius, op):
    """Binary erosion or dilation with a disc."""
    out = np.ones_like(mask) if op == 'erode' else np.zeros_like(mask)
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            if dx * dx + dy * dy > radius * radius:
                continue
            shifted = np.zeros_like(mask)
            shifted[max(0, -dy):H + min(0, -dy), max(0, -dx):W + min(0, -dx)] = \
                mask[max(0, dy):H + min(0, dy), max(0, dx):W + min(0, dx)]
            out = (out & shifted) if op == 'erode' else (out | shifted)
    return out


def sample(image, x, y):
    x0, y0 = int(np.floor(x)), int(np.floor(y))
    if x0 < 0 or y0 < 0 or x0 + 1 >= W or y0 + 1 >= H:
        return 0.0 * image[0, 0]
    fx, fy = x - x0, y - y0
    return (image[y0, x0] * (1 - fx) * (1 - fy) + image[y0, x0 + 1] * fx * (1 - fy) +
            image[y0 + 1, x0] * (1 - fx) * fy + image[y0 + 1, x0 + 1] * fx * fy)


# Thick parts survive an opening: the 12 clip bodies and the pole with its foot.
thick, thick_count = label(morph(morph(src[:, :, 3] > 100, 6, 'erode'), 6, 'dilate'))
sizes = np.bincount(thick.ravel())
BASE_BODY = thick == np.argmax(sizes[1:]) + 1
centres = {i: (xx[thick == i].mean(), yy[thick == i].mean()) for i in range(1, thick_count + 1)}
CLIP_BODY = {name: thick == min(centres, key=lambda i: np.hypot(centres[i][0] - x, centres[i][1] - y))
             for name, (x, y) in CLIP_ANCHORS.items()}
if thick_count != 13 or any((body & BASE_BODY).any() for body in CLIP_BODY.values()):
    raise SystemExit('Expected 12 clip bodies and one base in the source stand; found %d parts.' % thick_count)
ANY_CLIP = np.logical_or.reduce(list(CLIP_BODY.values()))
CLIP_AREA = {name: morph(body, 5, 'dilate') & VISIBLE for name, body in CLIP_BODY.items()}
BASE = morph(BASE_BODY, 2, 'dilate') & VISIBLE & (((xx >= 853) & (xx <= 921)) | (yy >= 640))
BASE |= (xx >= 853) & (xx <= 921) & (yy >= 594) & VISIBLE


def inside(point, mask):
    x, y = int(round(point[0])), int(round(point[1]))
    return 0 <= x < W and 0 <= y < H and mask[y, x]


def track(start, direction, to_hub, step=1.5):
    """Follow a wire centreline until it enters a clip or, toward the hub, the collar."""
    p = np.array(start, float)
    d = np.array(direction, float) / np.linalg.norm(direction)
    points, snapped = [p.copy()], [True]
    offsets = np.arange(-9, 9.01, .5)
    for _ in range(3000):
        q = p + d * step
        n = np.array([-d[1], d[0]])
        if inside(q, ANY_CLIP) or (to_hub and inside(q, COLLAR)):
            break
        profile = np.array([sample(ALPHA, *(q + n * t)) for t in offsets])
        on = profile > .5
        middle = int(np.argmin(np.abs(offsets)))
        hit = False
        if on[middle]:
            a = b = middle
            while a > 0 and on[a - 1]:
                a -= 1
            while b < len(on) - 1 and on[b + 1]:
                b += 1
            width, centre = (b - a + 1) * .5, (offsets[a] + offsets[b]) / 2
            if 5 <= width <= 11 and abs(centre) < 3.5:
                q, hit = q + n * centre, True
        heading = (q - p) / np.linalg.norm(q - p)
        d = .75 * d + .25 * heading
        d /= np.linalg.norm(d)
        p = q
        points.append(p.copy())
        snapped.append(hit)
        if not (0 < p[0] < W and 0 < p[1] < H) or profile.max() < .2:
            break
    return np.array(points), snapped


def trace(start, direction):
    forward, forward_hits = track(start, direction, True)
    backward, backward_hits = track(start, (-direction[0], -direction[1]), False)
    return np.vstack([backward[::-1], forward[1:]]), backward_hits[::-1] + forward_hits[1:]


def near(points, radius):
    """Pixels within radius of a polyline."""
    mask = np.zeros((H, W), bool)
    x0, x1 = int(max(0, points[:, 0].min() - radius - 2)), int(min(W, points[:, 0].max() + radius + 3))
    y0, y1 = int(max(0, points[:, 1].min() - radius - 2)), int(min(H, points[:, 1].max() + radius + 3))
    sx, sy = xx[y0:y1, x0:x1], yy[y0:y1, x0:x1]
    best = np.full(sx.shape, 1e9)
    for a, b in zip(points[:-1], points[1:]):
        v = b - a
        t = np.clip(((sx - a[0]) * v[0] + (sy - a[1]) * v[1]) / (v @ v), 0, 1)
        best = np.minimum(best, (sx - a[0] - t * v[0]) ** 2 + (sy - a[1] - t * v[1]) ** 2)
    mask[y0:y1, x0:x1] = best <= radius * radius
    return mask


PATHS = {name: trace(*spec) for name, spec in WIRES.items()}
OBJECTS = morph(thick > 0, 8, 'dilate') | np.logical_or.reduce([near(p, 7.5) for p, _ in PATHS.values()])


def box_sum(values, radius):
    c = np.pad(values, ((radius + 1, radius), (radius + 1, radius)), mode='edge').cumsum(0).cumsum(1)
    k = 2 * radius + 1
    return c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]


def backdrop(excluded):
    """The faint matte around the stand, averaged from nearby pixels that hold no object."""
    free = ~OBJECTS & ~excluded
    weight = np.maximum(box_sum(free.astype(float), 14), 1e-6)
    alpha = box_sum(src[:, :, 3] * free, 14) / weight
    out = np.zeros_like(src)
    for c in range(3):
        out[:, :, c] = np.where(alpha > 0, box_sum(PREMULTIPLIED[:, :, c] * free, 14) / weight / np.maximum(alpha / 255, 1e-6), 0)
    out[:, :, 3] = alpha
    return np.clip(out, 0, 255)


def hermite(p0, t0, p1, t1, n=200):
    s = np.linspace(0, 1, n)[:, None]
    length = np.linalg.norm(p1 - p0)
    return ((2 * s ** 3 - 3 * s ** 2 + 1) * p0 + (s ** 3 - 2 * s ** 2 + s) * t0 * length +
            (-2 * s ** 3 + 3 * s ** 2) * p1 + (s ** 3 - s ** 2) * t1 * length)


def bridge_curve(first, second):
    a, b = PATHS[first][0], PATHS[second][0]
    t0, t1 = a[-1] - a[-13], b[12] - b[0]
    return hermite(a[-21], t0 / np.linalg.norm(t0), b[20], t1 / np.linalg.norm(t1))


def arc(curve):
    return np.r_[0, np.cumsum(np.linalg.norm(np.diff(curve, axis=0), axis=1))]


def cross_section(name):
    """Median premultiplied RGBA across the wire, centred on its alpha centroid."""
    points, hits = PATHS[name]
    offsets, fine = np.arange(-7, 7.01, .25), np.arange(-9, 9.01, .25)
    rows = []
    for i in [i for i in range(len(points)) if hits[i]][-90:-30]:
        d = points[min(i + 3, len(points) - 1)] - points[max(i - 3, 0)]
        d /= np.linalg.norm(d)
        n = np.array([-d[1], d[0]])
        weights = np.array([sample(ALPHA, *(points[i] + n * t)) for t in fine])
        centre = (fine * weights).sum() / max(weights.sum(), 1e-6)
        rows.append([sample(PREMULTIPLIED, *(points[i] + n * (t + centre))) for t in offsets])
    return offsets, np.median(np.array(rows), axis=0)


def paint(out, curve, offsets, profile, ramp=10.0):
    along = arc(curve)
    for y in range(int(curve[:, 1].min() - 9), int(curve[:, 1].max() + 10)):
        for x in range(int(curve[:, 0].min() - 9), int(curve[:, 0].max() + 10)):
            p = np.array([x, y], float)
            i = int(np.argmin(((curve - p) ** 2).sum(1)))
            if i == 0 or i == len(curve) - 1:
                continue
            d = curve[i + 1] - curve[i - 1]
            d /= np.linalg.norm(d)
            t = (p - curve[i]) @ np.array([-d[1], d[0]])
            if abs(t) > 7:
                continue
            colour = np.array([np.interp(t, offsets, profile[:, k]) for k in range(4)])
            u = min(along[i], along[-1] - along[i]) / ramp
            w = 1.0 if u >= 1 else u * u * (3 - 2 * u)
            a = colour[3] / 255 * w
            if a <= 0:
                continue
            below = out[y, x, 3] / 255
            total = a + below * (1 - a)
            out[y, x, :3] = (colour[:3] * w + out[y, x, :3] * below * (1 - a)) / max(total, 1e-6)
            out[y, x, 3] = total * 255


def build(remove_clips, remove_wires):
    keep_clips = [k for k in CLIP_ANCHORS if k not in remove_clips]
    keep_wires = [k for k in PATHS if k not in remove_wires]
    bridged = {w for pair in BRIDGES for w in pair}
    removed_wire = np.logical_or.reduce([near(PATHS[k][0], 5.5) for k in remove_wires])
    kept_wire = {k: near(PATHS[k][0], 5.0) & VISIBLE for k in keep_wires}
    any_kept_wire = np.logical_or.reduce(list(kept_wire.values()))
    # A kept clip loses the pieces of a removed wire that poke out beside it.
    kept_clip = {k: CLIP_AREA[k] & ~(removed_wire & ~morph(CLIP_BODY[k], 1, 'dilate')) for k in keep_clips}
    any_kept_clip = np.logical_or.reduce(list(kept_clip.values()))
    keep = BASE | any_kept_clip | any_kept_wire
    keep &= ~((yy < 596) & removed_wire & ~any_kept_wire)  # removed stubs above the collar rim
    kill = np.logical_or.reduce([morph(CLIP_BODY[k], 8, 'dilate') for k in remove_clips]) & VISIBLE
    kill |= np.logical_or.reduce([near(PATHS[k][0], 6.5) for k in remove_wires]) & VISIBLE
    # A removed clip goes entirely, including where a bridged wire ran behind it.
    under = np.logical_or.reduce([morph(CLIP_BODY[k], 8, 'dilate') for k in remove_clips])
    for k in keep_wires:
        if k not in bridged:
            under &= ~kept_wire[k]
    under &= ~np.logical_or.reduce([CLIP_AREA[k] for k in keep_clips])
    erase = ((kill & ~keep) | (under & ~BASE)) & ~any_kept_clip
    protected = np.logical_or.reduce([CLIP_AREA[k] for k in keep_clips] + [BASE] +
                                     [kept_wire[k] for k in keep_wires if k not in bridged])
    for first, second in BRIDGES:
        curve = bridge_curve(first, second)
        along = arc(curve)
        erase |= near(curve[(along >= 16.5) & (along <= along[-1] - 16.5)], 6.5) & ~protected
    out = src.copy()
    out[erase] = backdrop(erase)[erase]
    for first, second in BRIDGES:
        paint(out, bridge_curve(first, second), *cross_section(first))
    parts, _ = label(out[:, :, 3] > 40)
    counts = np.bincount(parts.ravel())
    main = np.argmax(counts[1:]) + 1
    stray = (parts > 0) & (parts != main)
    if stray.any():
        stray = morph(stray, 3, 'dilate') & ~morph(parts == main, 1, 'dilate')
        out[stray] = backdrop(erase | stray)[stray]
    return Image.fromarray(np.clip(out, 0, 255).round().astype(np.uint8), 'RGBA')


if __name__ == '__main__':
    target = sys.argv[1] if len(sys.argv) > 1 else ART
    for file_name, (clips, wires) in VARIANTS.items():
        build(clips, wires).save(os.path.join(target, file_name), optimize=True)
        print('wrote', os.path.join(target, file_name))

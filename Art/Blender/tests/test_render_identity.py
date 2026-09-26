"""Blender script: ui_sprite_render.render_sprite must reproduce an approved customer PNG exactly.

    blender -b --factory-startup --python-exit-code 1 -P Art/Blender/tests/test_render_identity.py
    ... -P Art/Blender/tests/test_render_identity.py -- --customer explorer --wrapper

Opens the approved .blend read-only, renders it at 164x280 with DEFAULT_SHADOW into
%TEMP%/ui_sprite_identity and compares the decoded RGBA with the approved PNG.
Prints `RENDER_IDENTITY max_abs_diff=<n> differing_pixels=<n>` per check and exits 1
unless both numbers are 0. `--wrapper` also renders through the
create_ui_sprites.render_contact_sprite entry point that the approved generators call.
Nothing is saved: the .blend and the approved folders stay untouched.
"""
try:
    import bpy
except ImportError:  # plain Python (unittest discovery): this file only runs inside Blender
    bpy = None

import argparse
import os
import sys
import tempfile
import time
from pathlib import Path

ART = Path(__file__).resolve().parents[1]
CUSTOMERS = {
    'faceted': (ART/'GameCustomerFaceted/GameCustomerFaceted.blend', 'Game_Customer_Faceted',
                ART/'GameCustomerFaceted/Customer_01_Faceted.png'),
    'explorer': (ART/'GameCustomerFemaleExplorer/GameCustomerFemaleExplorer.blend', 'Game_Customer_Female_Explorer',
                 ART/'GameCustomerFemaleExplorer/Customer_02_Explorer.png'),
}
SIZE = (164, 280)

if bpy is None:
    import unittest

    class RenderIdentityTest(unittest.TestCase):
        @unittest.skip('Blender script: blender -b --factory-startup --python-exit-code 1 -P '
                       'Art/Blender/tests/test_render_identity.py')
        def test_render_identity(self):
            pass


def decoded(path):
    import numpy as np
    image = bpy.data.images.load(str(path), check_existing=False)
    try:
        values = np.empty(image.size[0]*image.size[1]*4, dtype=np.float32)
        image.pixels.foreach_get(values)
        return tuple(image.size), np.rint(values*255).astype(np.int16).reshape(-1, 4)
    finally:
        bpy.data.images.remove(image)


def compare(label, rendered, approved):
    size_a, a = decoded(rendered)
    size_b, b = decoded(approved)
    if size_a != size_b:
        print('%s size mismatch %s != %s' % (label, size_a, size_b))
        return False
    diff = abs(a-b)
    max_diff, changed = int(diff.max()), int((diff.max(axis=1) > 0).sum())
    print('%s max_abs_diff=%d differing_pixels=%d' % (label, max_diff, changed))
    return max_diff == 0 and changed == 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--customer', choices=sorted(CUSTOMERS), default='faceted')
    parser.add_argument('--wrapper', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    os.environ.pop('UI_SPRITE_THREADS', None)
    if str(ART) not in sys.path:
        sys.path.insert(0, str(ART))
    blend, scene_name, approved = CUSTOMERS[args.customer]
    scratch = Path(tempfile.gettempdir())/'ui_sprite_identity'
    scratch.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(blend), load_ui=False)
    import ui_sprite_render as r
    scene = bpy.data.scenes[scene_name]
    scene.render.resolution_x, scene.render.resolution_y = SIZE
    print('RENDER_IDENTITY_SETUP customer=%s threads=%s/%d scratch=%s' % (
        args.customer, scene.render.threads_mode, scene.render.threads, scratch))
    started = time.perf_counter()
    r.render_sprite(scene, 'identity_check', scratch/'identity.png', scratch/'passes', r.DEFAULT_SHADOW, SIZE)
    print('RENDER_IDENTITY_TIME render_sprite %.1fs' % (time.perf_counter()-started))
    ok = compare('RENDER_IDENTITY', scratch/'identity.png', approved)
    passes = approved.parent/'ui-sprite-passes'
    for kind in ('beauty', 'catcher'):
        compare('RENDER_IDENTITY_PASS %s' % kind, scratch/'passes'/('identity_check-%s.png' % kind),
                passes/('%s-%s.png' % (approved.stem, kind)))
    if args.wrapper:
        import create_ui_sprites as renderer
        renderer.ART = scratch
        renderer.render_contact_sprite(scene, {'id': 'wrapper_check', 'render_size': list(SIZE)}, scratch/'wrapper.png')
        ok = compare('RENDER_IDENTITY_WRAPPER', scratch/'wrapper.png', approved) and ok
    if bpy.data.is_dirty:
        print('RENDER_IDENTITY note: session is dirty in memory only; nothing is saved')
    sys.exit(0 if ok else 1)


if bpy is not None and __name__ == '__main__':
    main()

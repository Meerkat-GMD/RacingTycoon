"""Render one UI sprite, optionally with the soft contact shadow of the approved customers.

Blender only. `render_sprite` is the single place that renders sprites for
build_ui_sprites.py, render_customer_sprites.py and, through the
create_ui_sprites.render_contact_sprite wrapper, the two approved customer
generators. With DEFAULT_SHADOW it must stay pixel-identical to the Python loop
of Codex's original render_contact_sprite (commit 966cf73), which
tests/test_render_identity.py checks against Customer_01_Faceted.png.
"""
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

ART = Path(__file__).resolve().parent
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_common as c

# Ground point under the subject, ellipse radii as fractions of the canvas, strongest shadow alpha.
DEFAULT_SHADOW = {'anchor': (.07, .10, 0), 'radii': (.30, .068), 'max_alpha': .32}


def render_sprite(scene, sprite_id, output, passes_dir, shadow, size):
    """Render `scene` at `size` into the PNG `output`.

    shadow=None renders a plain still (transparent or opaque as the scene sets
    it). Otherwise the scene must contain one shadow-catcher mesh: the catcher
    and beauty passes are written to passes_dir/<sprite_id>-catcher.png and
    -beauty.png, and only the catcher's shadow (Navy, alpha <= max_alpha, soft
    elliptical falloff around `anchor`) is laid under the untouched beauty pixels.
    """
    output = Path(output)
    output.parent.mkdir(parents=True, exist_ok=True)
    w, h = size
    scene.render.resolution_x, scene.render.resolution_y = w, h
    if shadow is None:
        scene.render.filepath = str(output)
        bpy.ops.render.render(write_still=True, scene=scene.name)
        return
    _render_passes(scene, sprite_id, output, Path(passes_dir))
    _composite(scene, sprite_id, output, Path(passes_dir), shadow, (w, h))


def _render_passes(scene, sprite_id, output, passes_dir):
    passes_dir.mkdir(parents=True, exist_ok=True)
    floor = next(o for o in scene.objects if o.type == 'MESH' and o.is_shadow_catcher)
    try:
        floor.hide_render = False
        scene.render.filepath = str(passes_dir/(sprite_id+'-catcher.png'))
        bpy.ops.render.render(write_still=True, scene=scene.name)
        floor.hide_render = True
        scene.render.filepath = str(passes_dir/(sprite_id+'-beauty.png'))
        bpy.ops.render.render(write_still=True, scene=scene.name)
    finally:
        floor.hide_render = False
        scene.render.filepath = str(output)


def _composite(scene, sprite_id, output, passes_dir, shadow, size):
    """Same formula, operation order and float64 precision as the original per-pixel loop.

    Pixels are Blender's byte-image values in bottom-up rows. Every step is a
    plain IEEE operation; the two squared distance terms are taken with Python's
    own ** per column and per row so they round exactly as the original did.
    """
    from bpy_extras.object_utils import world_to_camera_view
    w, h = size
    combined = bpy.data.images.load(str(passes_dir/(sprite_id+'-catcher.png')), check_existing=False)
    beauty = bpy.data.images.load(str(passes_dir/(sprite_id+'-beauty.png')), check_existing=False)
    final = None
    try:
        for image in (combined, beauty):
            if tuple(image.size) != (w, h):
                raise ValueError('%s pass is %s, expected %s' % (image.name, tuple(image.size), (w, h)))
        cp = np.empty(w*h*4, dtype=np.float32)
        bp = np.empty(w*h*4, dtype=np.float32)
        combined.pixels.foreach_get(cp)
        beauty.pixels.foreach_get(bp)
        cp = cp.astype(np.float64).reshape(h, w, 4)
        bp = bp.astype(np.float64).reshape(h, w, 4)
        pos = world_to_camera_view(scene, scene.camera, Vector(shadow['anchor']))
        cx, cy = pos.x*w, pos.y*h
        rx, ry = shadow['radii'][0]*w, shadow['radii'][1]*h
        dx = np.array([((x+.5-cx)/rx)**2 for x in range(w)], dtype=np.float64)
        dy = np.array([((y+.5-cy)/ry)**2 for y in range(h)], dtype=np.float64)
        distance = np.sqrt(dx[None, :]+dy[:, None])
        t = np.minimum(1.0, np.maximum(0.0, (distance-.25)/.75))
        falloff = 1-t*t*(3-2*t)
        a = bp[..., 3]
        amount = np.maximum(0.0, (cp[..., 3]-a)/np.maximum(1-a, .00001))
        amount = np.minimum(shadow['max_alpha'], amount)*falloff
        final_alpha = a+amount*(1-a)
        ink = c.linear(c.PALETTE['Navy'])
        out = np.empty((h, w, 4), dtype=np.float64)
        for channel in range(3):
            out[..., channel] = (bp[..., channel]*a+ink[channel]*amount*(1-a))/np.maximum(final_alpha, .00001)
        out[..., 3] = final_alpha
        final = bpy.data.images.new(sprite_id+'_Composite', width=w, height=h, alpha=True)
        final.alpha_mode = 'STRAIGHT'
        final.pixels.foreach_set(out.astype(np.float32).ravel())
        # The passes are already display-transformed PNGs; save without a second view transform.
        final.filepath_raw = str(output)
        final.file_format = 'PNG'
        final.save()
    finally:
        for image in (combined, beauty, final):
            if image is not None:
                bpy.data.images.remove(image)

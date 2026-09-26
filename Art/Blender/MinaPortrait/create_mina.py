"""Mina_Portrait: the shop companion's lowpoly bust in front of a cotton-candy shop wall.

    blender -b --factory-startup --python-exit-code 1 -P Art/Blender/MinaPortrait/create_mina.py
        [-- --preview PATH]   quick 50 % / 16-sample still to PATH; nothing else is written
        [-- --no-render]      build, check framing and coverage, save .blend and manifest only

Default: build, check that the head, hands, candy and apron bib sit inside the frame and that
geometry covers every pixel (a low-res transparent render must be alpha 255
everywhere), save MinaPortrait.blend and manifest.json, and render the 808x784
opaque PNG to Assets/CottonCircuit/Sprites/Characters/Mina_Portrait.png through
ui_sprite_render.render_sprite(shadow=None). Set UI_SPRITE_THREADS=4.

Modelled in the approved male customer's authored units and scaled by .5 like
GameCustomerFaceted, so the shared studio rig (lights aimed at (0,0,1.25)) stays
valid. Z up, -Y front. Every material comes from toy_kit.Kit('MN_'): palette
colours only, Principled BSDF with Roughness .85 / Specular IOR Level .08.
"""
import argparse
import importlib
import json
import math
import random
import sys
import tempfile
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

HERE = Path(__file__).resolve().parent
ART = HERE.parent
ROOT = ART.parents[1]
for folder in (ART, HERE):
    if str(folder) not in sys.path:
        sys.path.insert(0, str(folder))
import ui_sprite_spec as spec
import ui_sprite_common as c
import ui_sprite_render as renderer
import toy_kit
import build_ui_sprites as driver
import mina_head as head
for module in (c, renderer, toy_kit, head):
    importlib.reload(module)

SPRITE_ID = 'Mina_Portrait'
SCENE = 'MN_Mina_Portrait'
BLEND = HERE/'MinaPortrait.blend'
MANIFEST = HERE/'manifest.json'
MODEL_SCALE = .5
# Model-space camera (after the .5 scale). Frame: awning scallops at the top,
# apron waistband at the bottom, window left, jar shelves and machine right.
CAMERA = {'target': (.02, 0, 1.72), 'scale': 1.62, 'yaw': -20, 'elevation': 15}
STICK = (-.40, -.70)          # authored x, y of the vertical candy stick
FLOSS_Z = 3.22                # authored centre height of the cotton-candy floss
FLOSS_SCALE = .58             # cotton() of commit 966cf73 scaled to Mina's hands
WALL_Y = 2.45                 # authored wall depth: far enough back that the rim light still reaches Mina


def resample(points, count):
    """`count` points evenly spaced along the polyline `points`."""
    pts = [Vector(p) for p in points]
    lengths = [0.]
    for a, b in zip(pts, pts[1:]):
        lengths.append(lengths[-1]+(b-a).length)
    out = []
    for k in range(count):
        target = lengths[-1]*k/(count-1)
        i = max(j for j in range(len(pts)-1) if lengths[j] <= target+1e-9) if k < count-1 else len(pts)-2
        t = (target-lengths[i])/max(lengths[i+1]-lengths[i], 1e-9)
        out.append(pts[i].lerp(pts[i+1], min(1., t)))
    return out


def sheet(kit, col, name, grid, color, depth=.04):
    """Thick sheet through a grid of front points (rows x cols); broad quads, closed sides."""
    rows, cols = len(grid), len(grid[0])
    front = [p for row in grid for p in row]
    verts = front+[(x, y+depth, z) for x, y, z in front]
    n = len(front)
    idx = lambda r, k: r*cols+k  # noqa: E731
    faces = []
    for r in range(rows-1):
        for k in range(cols-1):
            quad = (idx(r, k), idx(r, k+1), idx(r+1, k+1), idx(r+1, k))
            faces.append(quad)
            faces.append(tuple(v+n for v in reversed(quad)))
    ring = [idx(0, k) for k in range(cols)]+[idx(r, cols-1) for r in range(1, rows)]
    ring += [idx(rows-1, k) for k in range(cols-2, -1, -1)]+[idx(r, 0) for r in range(rows-2, 0, -1)]
    faces += [(ring[i], ring[(i+1) % len(ring)], ring[(i+1) % len(ring)]+n, ring[i]+n) for i in range(len(ring))]
    return kit.mesh(col, name, verts, faces, color)


def jar(kit, col, name, x, y, z, flavor):
    # Candy fill in the flavour colour below, White glass shoulder above: less colour on the wall.
    kit.loft(col, name+'_Candy', [(z, x, y, .135, .135), (z+.05, x, y, .165, .165),
                                  (z+.21, x, y, .165, .165)], flavor)
    kit.loft(col, name+'_Glass', [(z+.21, x, y, .167, .167), (z+.30, x, y, .167, .167),
                                  (z+.355, x, y, .115, .115), (z+.40, x, y, .115, .115)], 'White')
    kit.loft(col, name+'_Lid', [(z+.395, x, y, .14, .14), (z+.455, x, y, .14, .14)], 'Gold')
    kit.box(col, name+'_Knob', (x, y, z+.48), (.07, .07, .05), 'Gold', .012)


# ----------------------------------------------------------------- Mina's body

def shirt(kit, col):
    kit.loft(col, 'Shirt_Torso', [(1.60, 0, .04, .47, .30), (2.05, 0, .03, .45, .29),
                                  (2.40, 0, .03, .44, .285), (2.85, 0, .03, .52, .31),
                                  (3.15, 0, .04, .56, .31), (3.36, 0, .04, .49, .28),
                                  (3.48, 0, .04, .32, .23)], 'Cream')
    kit.loft(col, 'Shirt_CollarStand', [(3.38, 0, .035, .215, .205), (3.55, 0, .035, .205, .195)], 'Cream')
    for side, s in (('Left', -1), ('Right', 1)):
        # Turned-down pointed collar, after the male customer's thick collar planes.
        kit.panel(col, 'Shirt_Collar_'+side, [(s*.30, -.25, 3.47), (s*.14, -.30, 3.52), (s*.02, -.355, 3.30),
                                             (s*.15, -.385, 3.17), (s*.37, -.29, 3.33)], 'Cream', .06)


def apron(kit, col):
    # Bib with a soft vertical crease so it catches the key and fill light on two planes.
    grid = [[(-.33, -.322, 3.10), (0, -.352, 3.10), (.33, -.322, 3.10)],
            [(-.40, -.330, 2.60), (0, -.368, 2.60), (.40, -.330, 2.60)],
            [(-.46, -.336, 2.05), (0, -.376, 2.05), (.46, -.336, 2.05)]]
    sheet(kit, col, 'Apron_Bib', grid, 'Mint', .04)
    kit.loft(col, 'Apron_Waistband', [(1.94, 0, .03, .49, .33), (2.09, 0, .03, .48, .325)], 'Mint')
    kit.panel(col, 'Apron_Pocket', [(-.22, -.382, 2.36), (.22, -.382, 2.36), (.23, -.386, 2.10),
                                    (-.23, -.386, 2.10)], 'Mint', .02)
    for side, s in (('Left', -1), ('Right', 1)):
        path = [(s*.33, -.322, 3.10), (s*.33, -.31, 3.26), (s*.33, -.25, 3.38), (s*.33, -.13, 3.47),
                (s*.33, .02, 3.50), (s*.33, .16, 3.46)]
        inner = [(s*(abs(x)-.10), y, z) for x, y, z in path]
        head.strip(kit, col, 'Apron_Strap_'+side, path, inner, 'Mint', .03)
        # Ruffled edge (explorer _strip): scalloped outer contour and alternating pleat depth.
        edge = [(s*.46, -.336, 2.07), (s*.40, -.330, 2.60), (s*.33, -.322, 3.10), (s*.34, -.30, 3.28),
                (s*.345, -.22, 3.42), (s*.345, -.08, 3.50), (s*.34, .06, 3.51)]
        base = resample(edge, 31)
        outer = []
        for k, p in enumerate(base):
            lift = min(1., max(0., (p.z-3.15)/.30))
            out = Vector((s, .30*(1-lift), .55*lift)).normalized()
            wave = math.sin(math.pi*k/6)**2          # rounded scallop every six samples
            width = .085+.075*wave
            pleat = Vector((0, .008-.024*wave, 0))
            outer.append(tuple(p+out*width+pleat))
        head.strip(kit, col, 'Apron_Ruffle_'+side, outer, [tuple(p) for p in base], 'Mint', .02)
    # Small Vanilla bow pinned to the bib beside the candy, as on the painted apron.
    bx, by, bz = .25, -.380, 2.50
    for side, s in (('L', -1), ('R', 1)):
        head.slab(kit, col, 'Apron_Bow_'+side, [(bx+s*.012, by, bz+.012), (bx+s*.085, by+.004, bz+.055),
                                              (bx+s*.10, by+.004, bz-.04), (bx+s*.012, by, bz-.012)],
                  'Vanilla', .025)
    kit.box(col, 'Apron_Bow_Knot', (bx, by-.004, bz), (.04, .03, .045), 'Vanilla', .008)


def arms(kit, col):
    sx, sy = STICK
    hands = {-1: (sx-.015, sy, 2.44), 1: (sx+.015, sy, 2.62)}
    elbows = {-1: (-.74, -.06, 2.04), 1: (.72, -.12, 2.06)}
    for side, s in (('Left', -1), ('Right', 1)):
        ex, ey, ez = elbows[s]
        kit.loft(col, side+'_Upper_Sleeve', [(ez-.06, ex, ey, .17, .18), (2.42, s*.745, (ey+.03)/2, .20, .21),
                                             (2.85, s*.71, .03, .215, .225), (3.20, s*.58, .04, .215, .225),
                                             (3.40, s*.45, .04, .15, .19)], 'Cream')
        hx, hy, hz = hands[s]
        wrist = Vector((hx+s*.13, hy+.06, hz-.03))
        elbow = Vector((ex, ey, ez))
        direction = (wrist-elbow).normalized()
        kit.segment(col, side+'_Forearm', elbow, wrist-direction*.05, .25, .27, 'Cream', .03)
        kit.segment(col, side+'_Shirt_Cuff', wrist-direction*.16, wrist+direction*.01, .275, .295, 'Cream', .025)
        kit.box(col, side+'_Hand', (hx, hy, hz), (.26, .23, .19), 'Skin1', .05)
        # Two curled finger bands per fist, tilted differently so the stacked hands do not
        # repeat one pattern; the thumb rests on top of each fist.
        for i in range(2):
            finger = kit.box(col, side+'_Finger_%d' % i, (hx-s*.025, hy-.118, hz-.036+i*.074),
                             (.20-.03*i, .04, .062), 'Skin1', .018)
            finger.rotation_euler.y = s*(.10+.08*i)
        thumb = kit.box(col, side+'_Thumb', (hx-s*.05, hy-.04, hz+.10), (.11, .09, .06), 'Skin3', .016)
        thumb.rotation_euler.y = s*-.25


def candy(kit, col):
    """cotton() of commit 966cf73 (seeded floss core + 20 lobes, striped Cream stick), scaled."""
    sx, sy = STICK
    k = FLOSS_SCALE
    rng = random.Random(spec.SEED)

    def at(x, y, z):
        return (sx+k*x, sy+.02+k*y, FLOSS_Z+k*(z-1.68))
    kit.ico(col, 'Candy_Floss_Core', at(0, 0, 1.68), (.68*k, .56*k, .60*k), 'Strawberry', 2)
    # Twelve silhouette lobes round the core and eight on its faces, as in cotton(). The
    # portrait shows the floss four times larger than the item sprite, so lobe angles and
    # sizes spread wider and read as an uneven cloud instead of an even berry.
    positions = []
    for i in range(12):
        a = math.tau*i/12+rng.uniform(-.14, .14)
        positions.append((math.sin(a)*.52, rng.uniform(-.09, .09), 1.68+math.cos(a)*.44))
    for i in range(8):
        a = math.tau*(i % 4)/4+.45+rng.uniform(-.3, .3)
        positions.append((math.sin(a)*.32, -.30 if i < 4 else .30, 1.68+math.cos(a)*.28))
    for i, pos in enumerate(positions):
        r = rng.uniform(.19, .32)
        puff = kit.ico(col, 'Candy_Floss_Lobe_%02d' % (i+1), at(*pos),
                       (k*r*rng.uniform(.94, 1.13), k*r*.90, k*r*rng.uniform(.96, 1.13)), 'Strawberry', 2)
        puff.rotation_euler = [rng.uniform(-.30, .30) for _ in range(3)]
        for v in puff.data.vertices:
            v.co *= rng.uniform(.95, 1.05)
    kit.loft(col, 'Candy_Cream_Stick', [(2.26, sx, sy, .036, .036), (FLOSS_Z-.10, sx, sy, .036, .036)], 'Cream', 10)
    for i, z in enumerate((2.29, 2.71)):
        kit.loft(col, 'Candy_Stick_Stripe_%d' % i, [(z, sx, sy, .039, .039), (z+.04, sx, sy, .039, .039)],
                 'Strawberry', 10)
    # Tiny strawberry with a Mint calyx on the floss, the painted candy's topping.
    bx, by, bz = sx+.20, sy-.24, FLOSS_Z+.31
    kit.ico(col, 'Candy_Berry', (bx, by, bz), (.078, .072, .090), 'Strawberry', 1)
    kit.ico(col, 'Candy_Berry_Calyx', (bx, by, bz+.072), (.072, .064, .022), 'Mint', 1)
    kit.box(col, 'Candy_Berry_Stem', (bx, by, bz+.104), (.02, .02, .045), 'Mint', .005)


# ------------------------------------------------------------------- backdrop

def wall(kit, col):
    y = WALL_Y
    for i, x in enumerate(np.arange(-3.0, 5.01, .5)):
        kit.box(col, 'Wall_Board_%02d' % i, (x, y, 2.9), (.5, .1, 4.2), 'Cream' if i % 2 else 'White', .02)
    kit.box(col, 'Wall_Wainscot', (1.0, y-.07, 1.75), (8.4, .06, 1.90), 'Base', .015)
    kit.box(col, 'Wall_Wainscot_Rail', (1.0, y-.11, 2.72), (8.4, .10, .08), 'White', .02)


def window(kit, col):
    y = WALL_Y-.06
    x0, x1, z0, z1 = -.80, .06, 2.95, 3.95
    xm = (x0+x1)/2
    kit.box(col, 'Window_Sky', (xm, y, (z0+z1)/2), (x1-x0, .02, z1-z0), 'White', 0)
    head.slab(kit, col, 'Window_Hills', [(x0, y-.02, z0), (x1, y-.02, z0), (x1, y-.02, 3.40), (-.18, y-.02, 3.46),
                                         (-.42, y-.02, 3.36), (-.62, y-.02, 3.43), (x0, y-.02, 3.36)], 'Mint', .01)
    head.slab(kit, col, 'Window_Road', [(x0, y-.03, 3.02), (-.60, y-.03, z0), (x1, y-.03, 3.18), (x1, y-.03, 3.29),
                                        (-.34, y-.03, 3.19), (x0, y-.03, 3.13)], 'Strawberry', .01)
    head.slab(kit, col, 'Window_Road_Line', [(-.66, y-.04, 3.055), (-.56, y-.04, 3.055), (-.20, y-.04, 3.20),
                                             (-.28, y-.04, 3.20)], 'White', .01)
    kit.box(col, 'Window_Flag_Pole', (-.58, y-.05, 3.52), (.03, .02, .60), 'Cream', 0)
    for r in range(3):
        for q in range(4):
            kit.box(col, 'Window_Flag_%d%d' % (r, q), (-.535+q*.066, y-.05, 3.785-r*.066), (.066, .012, .066),
                    'Navy' if (r+q) % 2 else 'White', 0)
    t, d = .075, .14
    kit.box(col, 'Window_Frame_Top', (xm, y-.03, z1+t/2), (x1-x0+2*t, d, t), 'Base', .015)
    kit.box(col, 'Window_Frame_Left', (x0-t/2, y-.03, (z0+z1)/2), (t, d, z1-z0), 'Base', .015)
    kit.box(col, 'Window_Frame_Right', (x1+t/2, y-.03, (z0+z1)/2), (t, d, z1-z0), 'Base', .015)
    kit.box(col, 'Window_Sill', (xm, y-.11, z0-.04), (x1-x0+.30, .30, .08), 'Wood', .02)


def shelves(kit, col):
    y = WALL_Y-.27
    flavors = spec.FLAVORS
    kit.box(col, 'Shelf_Upright', (1.36, y, 3.05), (.08, .46, 1.60), 'Wood', .02)
    for row, z in enumerate((2.62, 3.46)):
        kit.box(col, 'Shelf_Board_%d' % row, (2.40, y, z), (2.06, .46, .07), 'Wood', .02)
        for i, x in enumerate((1.66, 2.06, 2.46, 2.86)):
            jar(kit, col, 'Shelf_Jar_%d%d' % (row, i), x, y-.02, z+.035, flavors[(i+row) % 3])


def awning(kit, col):
    y, top, band = WALL_Y-.30, 4.64, 4.32
    kit.box(col, 'Awning_Rail', (1.0, y+.02, top+.05), (8.6, .12, .10), 'Wood', .02)
    for i, x0 in enumerate(np.arange(-3.3, 5.3, .34)):
        x1, xm = x0+.34, x0+.17
        scallop = [(x1, y, band), (x1-.05, y, band-.09), (xm+.06, y, band-.145), (xm-.06, y, band-.145),
                   (x0+.05, y, band-.09), (x0, y, band)]
        head.slab(kit, col, 'Awning_Scallop_%02d' % i, [(x0, y, top), (x1, y, top)]+scallop,
                  'Strawberry' if i % 2 else 'White', .05)


def machine(kit, col):
    """Cotton-candy machine at the right edge: Soda base and bowl, pink floss under a Soda rib dome."""
    x, y = 1.70, .18
    kit.loft(col, 'Machine_Base', [(1.30, x, y, .30, .30), (1.92, x, y, .30, .30)], 'Soda')
    kit.loft(col, 'Machine_Bowl', [(1.90, x, y, .34, .34), (2.14, x, y, .52, .52), (2.20, x, y, .54, .54)], 'Soda')
    kit.loft(col, 'Machine_Rim', [(2.19, x, y, .565, .565), (2.26, x, y, .565, .565)], 'Cream')
    kit.ico(col, 'Machine_Floss', (x, y, 2.27), (.40, .40, .20), 'Strawberry', 2)
    for i in range(5):
        a = math.tau*i/5+.3
        kit.ico(col, 'Machine_Floss_Lobe_%d' % i, (x+.24*math.sin(a), y-.24*math.cos(a), 2.33),
                (.15, .15, .12), 'Strawberry', 1)
    ribs = 6
    for i in range(ribs):
        a = math.tau*i/(ribs*2)+math.pi/2
        arc = [(x+.55*math.cos(t)*math.cos(a), y-.55*math.cos(t)*math.sin(a), 2.25+.46*math.sin(t))
               for t in (0, .5, 1.0, math.pi/2)]
        for j in range(len(arc)-1):
            kit.segment(col, 'Machine_Dome_Rib_%d%d' % (i, j), arc[j], arc[j+1], .035, .035, 'Soda', .006)
    kit.ico(col, 'Machine_Knob', (x, y, 2.74), (.06, .06, .045), 'Gold', 1)
    kit.box(col, 'Machine_Badge', (x-.12, y-.29, 1.62), (.18, .03, .12), 'White', .01)


def stick_cup(kit, col):
    """Counter corner at the lower left: a White cup of striped paper sticks."""
    x, y = -1.30, -.05
    kit.box(col, 'Counter_Shelf', (x, y+.1, 2.02), (1.2, .7, .08), 'Wood', .02)
    kit.loft(col, 'Counter_Cup', [(2.06, x, y, .13, .13), (2.40, x, y, .15, .15)], 'White')
    for i, (dx, dy, lean) in enumerate(((-.05, .02, -.16), (.0, -.03, .04), (.05, .02, .2), (.02, .05, -.05))):
        start, end = Vector((x+dx, y+dy, 2.15)), Vector((x+dx+lean*.4, y+dy, 2.78-abs(lean)*.3))
        kit.segment(col, 'Counter_Stick_%d' % i, start, end, .03, .03, 'Cream', .004)
        stripe = start.lerp(end, .82)
        kit.box(col, 'Counter_Stick_Stripe_%d' % i, stripe, (.038, .038, .04),
                ('Strawberry', 'Soda', 'Vanilla', 'Strawberry')[i], .004)


# -------------------------------------------------------------------- build

def remove_previous():
    old = bpy.data.scenes.get(SCENE)
    if old:
        if len(bpy.data.scenes) == 1:
            bpy.data.scenes.new('MN_Temporary')
        bpy.data.scenes.remove(old)
    for blocks in (bpy.data.collections, bpy.data.objects, bpy.data.meshes, bpy.data.cameras,
                   bpy.data.lights, bpy.data.worlds, bpy.data.materials):
        for data in list(blocks):
            if data.name.startswith('MN_') and data.users == 0:
                blocks.remove(data)


def build():
    startup = [s for s in bpy.data.scenes if s.name != SCENE]
    remove_previous()
    rig, world = c.studio()
    rig.name, world.name = 'MN_Shared_Studio', 'MN_Shared_Environment'
    for ob in rig.objects:
        ob.name = 'MN_'+ob.name.removeprefix('UI_')
        ob.data.name = ob.name+'_Data'
    entry = spec.by_id()[SPRITE_ID]
    scene = c.setup_scene(SCENE, rig, world, entry['canvas'], spec.SEED)
    kit = toy_kit.Kit('MN_')
    names = ('Head_Hair', 'Shirt_Collar', 'Apron', 'Arms_Hands', 'Cotton_Candy', 'Shop_Backdrop')
    cols = {name: kit.collection(scene, 'MN_'+name) for name in names}
    head.build_head(kit, cols['Head_Hair'])
    shirt(kit, cols['Shirt_Collar'])
    apron(kit, cols['Apron'])
    arms(kit, cols['Arms_Hands'])
    candy(kit, cols['Cotton_Candy'])
    for part in (wall, window, shelves, awning, machine, stick_cup):
        part(kit, cols['Shop_Backdrop'])
    for col in cols.values():
        for ob in col.objects:
            ob.location *= MODEL_SCALE
            ob.scale *= MODEL_SCALE
        driver.recalc_normals(col)
    bare = sorted(ob.name for col in cols.values() for ob in col.objects if ob.type == 'MESH' and not ob.data.materials)
    if bare:
        raise ValueError('meshes without a palette material: %s' % ', '.join(bare))
    camera = c.camera(scene, **CAMERA)
    scene.camera.name, scene.camera.data.name = 'MN_Orthographic_Camera', 'MN_Camera_Data'
    scene.render.film_transparent = False
    scene['sprite_id'] = SPRITE_ID
    scene['display_size'] = list(entry['displays'][0])
    scene['model_scale'] = MODEL_SCALE
    scene.render.filepath = str(ROOT/spec.sprite_path(entry))
    for old in startup:
        if old.name in bpy.data.scenes and old != scene:
            bpy.data.scenes.remove(old)
    temporary = bpy.data.scenes.get('MN_Temporary')
    if temporary:
        bpy.data.scenes.remove(temporary)
    scene.view_layers[0].update()
    return scene, cols, camera, entry


def framing(scene, cols):
    """Normalised camera-space bounds of Mina's head, hands, candy and apron bib (all must lie in 0..1)."""
    from bpy_extras.object_utils import world_to_camera_view
    depsgraph = bpy.context.evaluated_depsgraph_get()
    groups = {'head_hair': [o for o in cols['Head_Hair'].objects],
              'hands': [o for o in cols['Arms_Hands'].objects if '_Hand' in o.name or '_Finger' in o.name
                        or '_Thumb' in o.name],
              'candy': list(cols['Cotton_Candy'].objects),
              'apron_bib': [cols['Apron'].objects['MN_Apron_Bib']]}
    report = {}
    for group, objects in groups.items():
        xs, ys = [], []
        for ob in objects:
            evaluated = ob.evaluated_get(depsgraph)
            mesh = evaluated.to_mesh()
            for v in mesh.vertices:
                p = world_to_camera_view(scene, scene.camera, evaluated.matrix_world @ v.co)
                xs.append(p.x)
                ys.append(p.y)
            evaluated.to_mesh_clear()
        report[group] = {'x': [round(min(xs), 4), round(max(xs), 4)], 'y': [round(min(ys), 4), round(max(ys), 4)]}
    inside = all(0 < b['x'][0] and b['x'][1] < 1 and 0 < b['y'][0] and b['y'][1] < 1 for b in report.values())
    return report, inside


def coverage(scene):
    """Render a small transparent still: every pixel must be covered by geometry (alpha 255)."""
    saved = (scene.render.film_transparent, scene.render.resolution_percentage, scene.cycles.samples,
             scene.cycles.use_denoising, scene.render.filepath)
    path = Path(tempfile.gettempdir())/'mina_portrait_coverage.png'
    try:
        scene.render.film_transparent = True
        scene.render.resolution_percentage = 25
        scene.cycles.samples = 4
        scene.cycles.use_denoising = False
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True, scene=scene.name)
    finally:
        (scene.render.film_transparent, scene.render.resolution_percentage, scene.cycles.samples,
         scene.cycles.use_denoising, scene.render.filepath) = saved
    image = bpy.data.images.load(str(path), check_existing=False)
    try:
        pixels = np.empty(image.size[0]*image.size[1]*4, dtype=np.float32)
        image.pixels.foreach_get(pixels)
        alpha = np.round(pixels[3::4]*255)
        result = {'size': list(image.size), 'pixels_below_255': int((alpha < 255).sum())}
    finally:
        bpy.data.images.remove(image)
    return result


def write_outputs(scene, cols, camera, entry, frame, cover):
    texts = set()
    for path in (Path(__file__), HERE/'mina_head.py'):
        name = 'MinaPortrait/'+path.name
        old = bpy.data.texts.get(name)
        if old:
            bpy.data.texts.remove(old)
        text = bpy.data.texts.new(name)
        text.write(path.read_text(encoding='utf-8-sig'))
        texts.add(text)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
    materials = {m.name: m for col in cols.values() for ob in col.objects if ob.type == 'MESH'
                 for m in ob.data.materials if m}
    manifest = {
        'category': 'mina',
        'module': driver.relative(Path(__file__), ROOT),
        'source_spec': 'docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md',
        'blender_version': bpy.app.version_string,
        'blend': driver.relative(BLEND, ROOT),
        'render': driver.render_settings(scene),
        'palette': spec.PALETTE,
        'lighting': spec.LIGHTING,
        'material_recipe': spec.MATERIAL,
        'coordinate_system': {'up': '+Z', 'front': '-Y', 'units': 'meters'},
        'model_scale': MODEL_SCALE,
        'identity': 'Mina, the shop companion: short wavy Magenta bob with side-swept bangs, Navy dot eyes, '
                    'open happy smile, Cream long-sleeve shirt with a turned-down collar, Mint apron bib with '
                    'ruffled straps, both hands holding a Strawberry cotton candy with a tiny berry',
        'painting_reference': 'Assets/CottonCircuit/Resources/Progression/NpcPortrait.png',
        'assets': [{
            'id': SPRITE_ID, 'file': spec.sprite_path(entry), 'kind': entry['kind'],
            'canvas': list(entry['canvas']), 'displays': [list(d) for d in entry['displays']],
            'camera': camera, 'shadow': None, 'film_transparent': scene.render.film_transparent,
            'framing': frame, 'coverage_check': cover,
            'params': {'stick_xy': list(STICK), 'floss_z': FLOSS_Z, 'floss_scale': FLOSS_SCALE,
                       'floss_seed': spec.SEED, 'wall_y': WALL_Y},
            'collections': {k: sorted(o.name for o in v.objects) for k, v in cols.items()},
            'objects': sorted(o.name for v in cols.values() for o in v.objects),
            'materials': [driver.material_record(m) for _, m in sorted(materials.items())]}],
    }
    MANIFEST.write_text(json.dumps(manifest, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
    print('MINA_SAVED', BLEND, MANIFEST)


def parse_args(argv):
    parser = argparse.ArgumentParser(prog='create_mina.py')
    parser.add_argument('--preview', type=Path)
    parser.add_argument('--no-render', action='store_true')
    return parser.parse_args(argv[argv.index('--')+1:] if '--' in argv else [])


def main():
    args = parse_args(sys.argv)
    scene, cols, camera, entry = build()
    frame, inside = framing(scene, cols)
    print('MINA_FRAMING', json.dumps(frame), 'inside' if inside else 'OUTSIDE')
    if args.preview:
        scene.render.resolution_percentage = 50
        scene.cycles.samples = 16
        scene.render.filepath = str(args.preview)
        bpy.ops.render.render(write_still=True, scene=scene.name)
        print('MINA_PREVIEW', args.preview)
        return
    if not inside:
        raise RuntimeError('Head, hands, candy or apron bib leave the frame: %s' % frame)
    cover = coverage(scene)
    print('MINA_COVERAGE', json.dumps(cover))
    if cover['pixels_below_255']:
        raise RuntimeError('Backdrop geometry leaves %d transparent pixels' % cover['pixels_below_255'])
    write_outputs(scene, cols, camera, entry, frame, cover)
    if args.no_render:
        return
    output = ROOT/spec.sprite_path(entry)
    renderer.render_sprite(scene, SPRITE_ID, output, HERE/'passes', None, tuple(entry['canvas']))
    print('UI_SPRITE_RENDERED', SPRITE_ID, output)


if __name__ == '__main__':
    main()

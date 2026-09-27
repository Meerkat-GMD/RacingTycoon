"""Shop sprites (Task 5): street storefront, trash bin and the two speech-bubble emotes.

Built and rendered by ../build_ui_sprites.py -- --category shop (Blender; Z up, -Y front).
Emote check after rendering (plain Python + Pillow, no Blender):
    python Art/Blender/ui_sprites/shop.py   -> Art/Blender/previews/shop-emotes-bubble.png

Storefront: a cotton-candy kiosk re-authored from the Kiosk in create_assets.py
(Wood platform, Cream back wall and pillars, Strawberry/Cream striped awning,
Strawberry counter, candy jars, a cotton candy on the roof). The parent UI writes
"솜사탕" on the sign and "COTTON SHOP" on the counter front, so both carry one
flat White face that covers the spec's label band (sign 71.5-92.5 % of the
canvas height from the bottom, counter 18-32 %, both over x 15-85 %).
The kiosk front runs along the camera's right axis (its footprint is a 70 degree
parallelogram): under the shared -20/15 degree camera the front and the text
faces stay level for the horizontal UI text, while the side wall, awning, counter
top and platform still recede like every other 3/4 sprite.

Trash: a Mint bin with Base foot and rim, a Cream swing-lid hood whose flap is
pushed open by one crumpled Strawberry wrapper.

Emotes: a chunky faceted Strawberry heart with a White highlight facet, and one
large, centred four-armed anger "vein" mark (wide flat Strawberry tops, Plum side
facets, a clear gap between the arms). The mark had a small Navy steam puff that read
as a dark smudge at 72x66 inside the speech bubble; it is gone and the mark is bigger
instead. Emotes use a near-frontal camera (|yaw| <= 10), lean back toward the key
light and have no shadow.
"""
import argparse
import math
import random
import sys
from pathlib import Path

try:
    from mathutils import Vector
except ImportError:  # plain Python: only the emote bubble preview at the end of this file is usable
    Vector = SEED = None
else:
    from ui_sprite_spec import SEED

CATEGORY = 'shop'
SHOP_YAW = -20
FLAP_OPEN_DEG = 40
COUNTER_BOARD_LEAN_DEG = 24
LABEL_FACE = 'White'
EMOTE_TILT_DEG = 22
# Anger vein mark: arm corners `gap` from the centre, arms reaching `arm`, tips flaring `flare` further out;
# ribbon widths from tip to corner to tip, and the flat Strawberry top as a share of each width.
VEIN = {'centre': (0, 1.285), 'gap': .12, 'arm': .33, 'flare': .1, 'widths': (.07, .15, .18, .15, .07), 'top': .62}
KIOSK_FRONT_TURN = math.radians(SHOP_YAW)

ASSETS = [
    {'id': 'Storefront',
     'camera': {'target': (0, 0, 1.607), 'scale': 3.4, 'yaw': SHOP_YAW, 'elevation': 15},
     'shadow': {'anchor': (0, .26, 0), 'radii': (.46, .05), 'max_alpha': .32},
     'params': {'source': 'Art/Blender/create_assets.py:116-136 Kiosk (structure only)',
                'label_bands_from_bottom': {'sign': [.715, .925], 'counter': [.18, .32]},
                'label_bands_x': [.15, .85], 'label_face_material': LABEL_FACE,
                'counter_board_lean_deg': COUNTER_BOARD_LEAN_DEG,
                'front_turn_deg': SHOP_YAW,
                'roof_candy': 'Strawberry, core + 20 jittered lobes on a Cream stick with Strawberry stripes'}},
    {'id': 'Trash',
     'camera': {'target': (0, 0, .6), 'scale': 1.58, 'yaw': SHOP_YAW, 'elevation': 15},
     'shadow': {'anchor': (0, 0, 0), 'radii': (.36, .075), 'max_alpha': .32},
     'params': {'body': 'Mint', 'trim': 'Base', 'lid': 'Cream swing hood, flap pushed open 40 deg',
                'wrapper': 'crumpled Strawberry'}},
    {'id': 'Emote_Heart',
     'camera': {'target': (0, 0, 1.25), 'scale': 1.24, 'yaw': -8, 'elevation': 15},
     'shadow': None,
     'params': {'heart': 'Strawberry, 14-point hand-authored outline, raised bevel ring and centre fan',
                'highlight': 'one White bevel facet on the upper-left lobe', 'tilt_back_deg': EMOTE_TILT_DEG}},
    {'id': 'Emote_Angry',
     'camera': {'target': (0, 0, 1.29), 'scale': 1.16, 'yaw': -8, 'elevation': 15},
     'shadow': None,
     'params': {'mark': 'one centred vein mark: four flared L arms, flat Strawberry tops, Plum side facets, no steam puff',
                'vein': VEIN, 'tilt_back_deg': EMOTE_TILT_DEG}},
]


# ---------------------------------------------------------------- shared helpers

def bevel(obj, width):
    if width:
        mod = obj.modifiers.new('Single flat chamfer', 'BEVEL')
        mod.width = width
        mod.segments = 1
    return obj


def hexa(kit, col, name, corners, material, width=.015):
    """Closed six-sided solid from 8 corners: bottom ring (4) then top ring (4), same winding."""
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return bevel(kit.mesh(col, name, [tuple(c) for c in corners], faces, material), width)


def jitter(obj, rng, amount):
    for vertex in obj.data.vertices:
        vertex.co *= rng.uniform(1-amount, 1+amount)


# Cotton candy floss, same tiered recipe as the item candies (spec: a slightly larger
# core with about 20 random jittered lobes): count, elevation range (deg), radius range.
FLOSS_CORE = (.50, .45, .48)
FLOSS_EGG = .12
FLOSS_TIERS = ((8, (36, 66), (.27, .34)), (7, (-2, 28), (.25, .31)), (5, (-50, -20), (.19, .25)))


def _egg_reach(n):
    widen = 1 + FLOSS_EGG*n[2]
    radii = (FLOSS_CORE[0]*widen, FLOSS_CORE[1]*widen, FLOSS_CORE[2])
    return 1/math.sqrt(sum((c/r)**2 for c, r in zip(n, radii)))


def floss(kit, col, name, center, s, flavor, rng):
    """Faceted egg core plus 20 flattened, jittered lobes; outer radius about .74*s."""
    from mathutils import Quaternion
    center = Vector(center)
    core = kit.ico(col, name + '_Core', tuple(center), tuple(r*s for r in FLOSS_CORE), flavor, 2)
    for v in core.data.vertices:
        v.co.x *= 1 + FLOSS_EGG*v.co.z
        v.co.y *= 1 + FLOSS_EGG*v.co.z
    core.rotation_euler.z = rng.uniform(0, math.tau)
    jitter(core, rng, .04)
    index = 0
    for count, elevation, radius_range in FLOSS_TIERS:
        offset = rng.uniform(0, 1)
        for i in range(count):
            az = (i + offset + rng.uniform(-.22, .22))*math.tau/count
            el = math.radians(rng.uniform(*elevation))
            n = Vector((math.cos(el)*math.cos(az), math.cos(el)*math.sin(az), math.sin(el)))
            radius = rng.uniform(*radius_range)
            index += 1
            lobe = kit.ico(col, '%s_Lobe_%02d' % (name, index), tuple(center + n*_egg_reach(n)*rng.uniform(.80, .90)*s),
                           (radius*s*rng.uniform(.95, 1.12), radius*s*rng.uniform(.95, 1.12), radius*s*.8), flavor, 2)
            lobe.rotation_euler = (n.to_track_quat('Z', 'Y') @ Quaternion((0, 0, 1), rng.uniform(0, math.tau))).to_euler()
            jitter(lobe, rng, .05)


def candy_stick(kit, col, name, base, top, radius, flavor, stripes=(.25, .5, .75)):
    """Cream paper stick with flavour stripe rings (spec)."""
    base, top = Vector(base), Vector(top)
    axis = (top - base).normalized()
    kit.segment(col, name + '_Stick', base, top, radius*2, radius*2, 'Cream', radius*.25)
    for i, t in enumerate(stripes):
        p = base.lerp(top, t)
        kit.segment(col, '%s_Stick_Stripe_%d' % (name, i+1), p - axis*radius*.9, p + axis*radius*.9,
                    radius*2.3, radius*2.3, flavor, radius*.25)


# ---------------------------------------------------------------- storefront

def W(u, v, z):
    """Kiosk frame -> world. u runs along the front (the camera's right axis), v straight back (+Y), z up."""
    return (u*math.cos(KIOSK_FRONT_TURN), u*math.sin(KIOSK_FRONT_TURN) + v, z)


def WF(u, d, z):
    """Camera-facing frame: u along the front, d toward the camera (perpendicular to the front), z up."""
    a = KIOSK_FRONT_TURN
    return (u*math.cos(a) + d*math.sin(a), u*math.sin(a) - d*math.cos(a), z)


def kbox(kit, col, name, u, v, z, material, width=.02):
    """Box in the kiosk frame; u, v, z are (min, max) pairs."""
    (u0, u1), (v0, v1), (z0, z1) = u, v, z
    corners = [W(u0, v0, z0), W(u1, v0, z0), W(u1, v1, z0), W(u0, v1, z0),
               W(u0, v0, z1), W(u1, v0, z1), W(u1, v1, z1), W(u0, v1, z1)]
    return hexa(kit, col, name, corners, material, width)


def jar(kit, col, name, u, v, z, flavor, rng, height=.25, radius=.085):
    x, y, _ = W(u, v, z)
    kit.loft(col, name + '_Glass', [(z, x, y, radius*.86, radius*.86), (z + .03, x, y, radius, radius),
                                    (z + height*.78, x, y, radius, radius),
                                    (z + height*.9, x, y, radius*.78, radius*.78)], 'White')
    kit.loft(col, name + '_Lid_Band', [(z + height*.86, x, y, radius*.84, radius*.84),
                                       (z + height, x, y, radius*.84, radius*.84)], 'Gold')
    top = z + height + radius*.5
    kit.ico(col, name + '_Candy_Core', (x, y, top), (radius*.8, radius*.72, radius*.72), flavor, 2)
    for i in range(5):
        a = math.tau*i/5 + rng.uniform(-.2, .2)
        lobe = kit.ico(col, '%s_Candy_Lobe_%d' % (name, i+1),
                       (x + math.sin(a)*radius*.62, y - radius*.18 + rng.uniform(-.03, .03), top + math.cos(a)*radius*.42),
                       (radius*.42, radius*.38, radius*.4), flavor, 1)
        jitter(lobe, rng, .04)


def storefront(asset, col, kit):
    """Solids touch or interpenetrate but never share a coplanar face (shadow acne)."""
    rng = random.Random(SEED + 5)
    # Wood platform and the Strawberry counter.
    kbox(kit, col, 'Platform', (-.99, .99), (-.10, .62), (0, .13), 'Wood', .02)
    kbox(kit, col, 'Counter', (-.955, .955), (0, .56), (.125, 1.10), 'Strawberry', .02)
    # The counter label is a White menu board leaning back on a Wood ledge: tilted toward the key
    # light it is as bright as the sign, and leaning along the camera-facing frame keeps its
    # edges level and vertical on screen.
    lean, low, high, thick = math.radians(COUNTER_BOARD_LEAN_DEG), .48, 1.06, .035
    reach = (high - low)*math.tan(lean)
    inward = Vector(WF(0, -math.cos(lean), -math.sin(lean)))
    face = [Vector(WF(u, d, z)) for u, d, z in ((-.935, reach + .01, low), (.935, reach + .01, low),
                                                (.935, .01, high), (-.935, .01, high))]
    hexa(kit, col, 'Counter_Label_Board', face + [p + inward*thick for p in face], LABEL_FACE, .006)
    ledge = [WF(u, d, z) for z in (low - .045, low)
             for u, d in ((-.95, -.01), (.95, -.01), (.95, reach + .06), (-.95, reach + .06))]
    hexa(kit, col, 'Counter_Board_Ledge', [ledge[0], ledge[3], ledge[2], ledge[1], ledge[4], ledge[7], ledge[6], ledge[5]],
         'Wood', .01)
    kbox(kit, col, 'Counter_Kick', (-.95, .95), (-.014, .02), (.125, .205), 'Wood', .008)
    kbox(kit, col, 'Counter_Top', (-.985, .985), (-.05, .58), (1.095, 1.17), 'Wood', .02)
    # Booth: Cream side walls and back wall, proud Cream pillars on Wood feet, Cream roof.
    for side, sign in (('Left', -1), ('Right', 1)):
        outer, inner = sorted((sign*.955, sign*.905))
        kbox(kit, col, side + '_Side_Wall', (outer, inner), (.1, .56), (1.165, 2.275), 'Cream', .012)
        post = sorted((sign*.962, sign*.858))
        kbox(kit, col, side + '_Pillar', post, (-.012, .1), (1.165, 2.25), 'Cream', .015)  # top hidden in the awning
        kbox(kit, col, side + '_Pillar_Foot', (post[0] - .012, post[1] + .012), (-.024, .104), (1.165, 1.22), 'Wood', .01)
    kbox(kit, col, 'Back_Wall', (-.91, .91), (.5, .555), (1.165, 2.275), 'Cream', .012)
    kbox(kit, col, 'Roof', (-.985, .985), (.02, .62), (2.265, 2.35), 'Cream', .015)
    # Back shelf with two small cotton candies standing in a Wood block.
    kbox(kit, col, 'Back_Shelf', (-.62, .72), (.36, .5), (1.30, 1.34), 'Wood', .01)
    for i, (u, flavor) in enumerate(((-.19, 'Soda'), (.37, 'Vanilla'))):
        kbox(kit, col, 'Shelf_Stand_%d' % (i+1), (u - .05, u + .05), (.4, .47), (1.335, 1.39), 'Wood', .008)
        x, y, _ = W(u, .435, 0)
        candy_stick(kit, col, 'Shelf_Candy_%d' % (i+1), (x, y, 1.36), (x, y, 1.5), .012, flavor)
        floss(kit, col, 'Shelf_Candy_%d_Floss' % (i+1), (x, y, 1.55), .115, flavor, rng)
    # Candy jars on the counter.
    for i, (u, flavor) in enumerate(((-.56, 'Strawberry'), (0, 'Soda'), (.56, 'Vanilla'))):
        jar(kit, col, 'Counter_Jar_%d' % (i+1), u, .2, 1.168, flavor, rng, .27, .095)
    # Striped awning: eight sloped stripes and a scalloped valance.
    count, u_left, u_right = 8, -.985, .985
    step = (u_right - u_left)/count
    back_z, front_z, front_v, thick = 2.26, 2.03, -.30, .03
    for i in range(count):
        u0, u1 = u_left + i*step, u_left + (i+1)*step
        material = 'Strawberry' if i % 2 == 0 else 'Cream'
        top = [W(u0, 0, back_z), W(u1, 0, back_z), W(u1, front_v, front_z), W(u0, front_v, front_z)]
        low = [(x, y, z - thick) for x, y, z in top]
        hexa(kit, col, 'Awning_Stripe_%02d' % (i+1), low + top, material, .006)
        mid = (u0 + u1)/2
        lip = [(u0, front_z), (u1, front_z), (u1, 1.935), (u1 - step*.2, 1.905), (mid, 1.893),
               (u0 + step*.2, 1.905), (u0, 1.935)]
        kit.panel(col, 'Awning_Valance_%02d' % (i+1), [W(u, front_v - .012, z) for u, z in lip], material, .028)
    # Sign on the roof front: Strawberry backing, flat White face for the parent label.
    kbox(kit, col, 'Sign_Backing', (-.985, .985), (0, .07), (2.255, 3.21), 'Strawberry', .02)
    kbox(kit, col, 'Sign_Label_Face', (-.94, .93), (-.016, .004), (2.30, 3.17), LABEL_FACE, .006)
    # A cotton candy planted on the awning's right corner, its floss at the sign's top right.
    x0, y0, _ = W(.965, -.07, 0)
    x1, y1, _ = W(1.033, -.07, 0)
    candy_stick(kit, col, 'Roof_Candy', (x0, y0, 2.17), (x1, y1, 3.02), .024, 'Strawberry', (.3, .52, .74))
    floss(kit, col, 'Roof_Candy_Floss', (x1, y1, 3.14), .215, 'Strawberry', rng)


# ---------------------------------------------------------------- trash bin

def trash(asset, col, kit):
    rng = random.Random(SEED + 7)
    kit.box(col, 'Foot', (0, 0, .04), (.72, .64, .08), 'Base', .018)
    body = [(-.32, -.28, .07), (.32, -.28, .07), (.32, .28, .07), (-.32, .28, .07),
            (-.37, -.325, .84), (.37, -.325, .84), (.37, .325, .84), (-.37, .325, .84)]
    hexa(kit, col, 'Body', body, 'Mint', .022)
    for i, x in enumerate((-.15, .15)):
        # two shallow raised ribs catch the key light on the front
        hexa(kit, col, 'Front_Rib_%d' % (i+1),
             [(x - .045, -.29, .16), (x + .045, -.29, .16), (x + .045, -.27, .16), (x - .045, -.27, .16),
              (x - .045, -.33, .74), (x + .045, -.33, .74), (x + .045, -.31, .74), (x - .045, -.31, .74)],
             'Mint', .008)
    kit.box(col, 'Rim', (0, 0, .87), (.80, .71, .07), 'Base', .018)
    # Cream swing-lid hood: a faceted arch profile extruded across the bin.
    profile = [(-.335, .90), (-.335, 1.05), (-.25, 1.17), (-.09, 1.225), (.09, 1.225), (.25, 1.17), (.335, 1.05), (.335, .90)]
    n = len(profile)
    verts = [(-.375, y, z) for y, z in profile] + [(.375, y, z) for y, z in profile]
    faces = [tuple(range(n)), tuple(range(2*n - 1, n - 1, -1))]
    faces += [(i, i + n, i + 1 + n, i + 1) for i in range(n - 1)]
    faces.append((n - 1, 2*n - 1, n, 0))
    bevel(kit.mesh(col, 'Hood', verts, faces, 'Cream'), .018)
    kit.box(col, 'Hood_Knob', (0, .0, 1.24), (.16, .1, .04), 'Base', .012)
    # Dark mouth on the hood front and the flap pushed outward by the wrapper.
    kit.box(col, 'Mouth', (0, -.335, .985), (.42, .02, .13), 'Tire', .006)
    hinge_y, hinge_z, length, angle, t = -.345, 1.058, .15, math.radians(FLAP_OPEN_DEG), .022
    down = Vector((0, -math.sin(angle), -math.cos(angle)))
    normal = Vector((0, -math.cos(angle), math.sin(angle)))
    flap = [tuple(Vector((x, hinge_y, hinge_z)) + down*d + off)
            for off in (Vector((0, 0, 0)), normal*t)
            for x, d in ((-.225, 0), (.225, 0), (.225, length), (-.225, length))]
    hexa(kit, col, 'Swing_Flap', flap, 'Cream', .007)
    kit.segment(col, 'Hinge_Pin', (-.235, hinge_y, hinge_z), (.235, hinge_y, hinge_z), .03, .03, 'Base', .005)
    # One crumpled Strawberry wrapper wedged under the flap: two fused, strongly jittered lumps.
    for i, (loc, radii, rot) in enumerate((((.02, -.39, .945), (.115, .095, .085), (.35, .2, -.4)),
                                           ((.1, -.41, .985), (.07, .06, .055), (-.5, .6, .9)))):
        paper = kit.ico(col, 'Wrapper_Crumple_%d' % (i+1), loc, radii, 'Strawberry', 1)
        paper.rotation_euler = rot
        jitter(paper, rng, .2)


# ---------------------------------------------------------------- emotes

# Hand-authored low-poly heart (x, z), width 1: bottom point, left side, left lobe, cleft, right half.
HEART = [(0, -.50), (-.27, -.18), (-.44, .02), (-.50, .20), (-.44, .37), (-.29, .47), (-.13, .45),
         (0, .33), (.13, .45), (.29, .47), (.44, .37), (.50, .20), (.44, .02), (.27, -.18)]
HEART_HIGHLIGHT = 4  # bevel facet from outline point 4 to 5: the upper-left lobe, facing the key light


def tilt(col, degrees, pivot):
    """Lean every object of the emote back about the X axis through pivot so the faces meet the key light."""
    from mathutils import Matrix
    m = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(-degrees), 4, 'X') @ Matrix.Translation(-Vector(pivot))
    for obj in col.objects:  # unparented, so the basis matrix is the world matrix
        obj.matrix_basis = m @ obj.matrix_basis


def heart(asset, col, kit):
    width, cz0 = .98, 1.25
    outline = [(x*width, cz0 + z*width) for x, z in HEART]
    n = len(outline)
    cx, cz = 0, cz0 + .06*width
    front, ring, back = -.09, -.19, .09
    verts = [(x, front, z) for x, z in outline]
    verts += [(cx + (x - cx)*.6, ring, cz + (z - cz)*.6) for x, z in outline]
    verts += [(x, back, z) for x, z in outline]
    verts.append((cx, ring - .05, cz))
    center = len(verts) - 1
    faces, highlight = [], None
    for i in range(n):
        j = (i + 1) % n
        if i == HEART_HIGHLIGHT:
            highlight = len(faces)
        faces.append((i, j, n + j, n + i))                  # bevel ring facet
        faces.append((n + i, n + j, center))                # centre fan facet
        faces.append((j, i, 2*n + i, 2*n + j))              # side wall
    faces.append(tuple(range(3*n - 1, 2*n - 1, -1)))        # flat back
    obj = kit.mesh(col, 'Heart', verts, faces, 'Strawberry')
    obj.data.materials.append(kit.mat('White'))
    obj.data.polygons[highlight].material_index = 1
    tilt(col, EMOTE_TILT_DEG, (0, 0, cz0))


def ribbon(kit, col, name, path, widths, height=.05, depth=.08, top=.46):
    """Bar along an XZ polyline with a trapezoid cross-section: a flat Strawberry top strip
    (`top` of the width, raised `height` toward the camera) between two sloped Plum side
    facets; back, walls and end caps are Plum too."""
    pts = [Vector((x, 0, z)) for x, z in path]
    n = len(pts)
    sides_at = []
    for i, p in enumerate(pts):
        if i in (0, n - 1):
            d = (pts[1] - p) if i == 0 else (p - pts[i - 1])
            d.normalize()
            side = Vector((-d.z, 0, d.x))
        else:
            d0 = (p - pts[i - 1]).normalized()
            d1 = (pts[i + 1] - p).normalized()
            s0, s1 = Vector((-d0.z, 0, d0.x)), Vector((-d1.z, 0, d1.x))
            side = (s0 + s1).normalized()
            side /= max(.35, side.dot(s1))
        sides_at.append(side)
    up, back = Vector((0, -height, 0)), Vector((0, depth, 0))
    verts = []
    for p, side, w in zip(pts, sides_at, widths):
        half = side*w/2
        verts += [tuple(p + half), tuple(p + half*top + up), tuple(p - half*top + up), tuple(p - half),
                  tuple(p + half + back), tuple(p - half + back)]
    faces, plum = [], []
    for i in range(n - 1):
        a, b = 6*i, 6*(i + 1)
        faces.append((a + 1, b + 1, b + 2, a + 2))                     # flat top strip
        plum += [(a, b, b + 1, a + 1), (a + 2, b + 2, b + 3, a + 3),  # sloped side facets
                 (a, a + 4, b + 4, b), (a + 3, b + 3, b + 5, a + 5),  # walls
                 (a + 4, a + 5, b + 5, b + 4)]                        # back
    last = 6*(n - 1)
    plum += [(0, 1, 2, 3, 5, 4), (last + 4, last + 5, last + 3, last + 2, last + 1, last)]
    obj = kit.mesh(col, name, verts, faces + plum, 'Strawberry')
    obj.data.materials.append(kit.mat('Plum'))
    for index in range(len(faces), len(faces) + len(plum)):
        obj.data.polygons[index].material_index = 1
    return obj


def angry(asset, col, kit):
    (ox, oz), gap, arm, flare = VEIN['centre'], VEIN['gap'], VEIN['arm'], VEIN['flare']
    for q, (sx, sz) in enumerate(((1, 1), (-1, 1), (-1, -1), (1, -1))):
        # corner near the centre, one arm along each gap edge, tips flaring away from the gap
        path = [(sx*(gap + flare*.8), sz*(arm + flare)), (sx*gap, sz*arm), (sx*gap, sz*gap),
                (sx*arm, sz*gap), (sx*(arm + flare), sz*(gap + flare*.8))]
        path = [(ox + x, oz + z) for x, z in path]
        ribbon(kit, col, 'Vein_Arm_%d' % (q + 1), path, list(VEIN['widths']), top=VEIN['top'])
    tilt(col, EMOTE_TILT_DEG, (0, 0, 1.25))


BUILDERS = {'Storefront': storefront, 'Trash': trash, 'Emote_Heart': heart, 'Emote_Angry': angry}


def build(asset, col, kit):
    BUILDERS[asset['id']](asset, col, kit)


# ---------------------------------------------------------------- emote bubble preview (no Blender)
ROOT = Path(__file__).resolve().parents[3]
SPRITES = 'Assets/CottonCircuit/Sprites'
INK, WHITE, PINK, CREAM_BG = '#29324D', '#FFF9ED', '#F48DAB', '#FFF6E7'   # ShopStreetGraphic colours, spec review cream
BUBBLE_SIZE, EMOTE_RECT = (132, 130), (30, 16, 72, 66)                    # ShopStreetUI: bubble and emote rects
REACTIONS = {'Emote_Heart': (False, '고마워요!'), 'Emote_Angry': (True, '주문이 달라요!')}


def _font(size, bold=False):
    from PIL import ImageFont
    for name in (('malgunbd.ttf', 'segoeuib.ttf') if bold else ('malgun.ttf', 'segoeui.ttf')):
        path = Path('C:/Windows/Fonts')/name
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default(size=size)


def _bubble(angry, ss=4):
    """ShopStreetGraphic.DrawSpeechBubble at 132x130, supersampled; P() takes bottom-up y."""
    from PIL import Image, ImageDraw
    w, h = BUBBLE_SIZE[0]*ss, BUBBLE_SIZE[1]*ss
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    def P(x, y):
        return x*w, (1 - y)*h
    outline = PINK if angry else INK
    draw.polygon([P(.22, .17), P(.35, .17), P(.20, .015), P(.20, .17)], fill=outline)
    draw.rounded_rectangle([P(.035, .975), P(.965, .14)], radius=.13*h, fill=outline)
    draw.rounded_rectangle([P(.055, .955), P(.945, .16)], radius=.115*h, fill=WHITE)
    draw.polygon([P(.24, .18), P(.33, .18), P(.215, .055), P(.215, .18)], fill=WHITE)
    if angry:
        for a, b in ((P(.075, .825), P(.14, .89)), (P(.89, .84), P(.94, .89))):
            draw.line([a, b], fill=PINK, width=max(1, round(.012*min(w, h))))   # Line(): thickness x min side
    return img.resize(BUBBLE_SIZE, Image.Resampling.LANCZOS)


def _bubble_tile(emote, sprite_id, background):
    """One game-size bubble (1x) with the emote at its game rect and the reaction label, on `background`."""
    from PIL import Image, ImageDraw
    angry, text = REACTIONS[sprite_id]
    tile = Image.new('RGBA', (BUBBLE_SIZE[0] + 16, BUBBLE_SIZE[1] + 16), background)
    tile.alpha_composite(_bubble(angry), (8, 8))
    x, y, w, h = EMOTE_RECT
    tile.alpha_composite(emote.resize((w, h), Image.Resampling.LANCZOS), (8 + x, 8 + y))
    ImageDraw.Draw(tile).text((8 + 66, 8 + 84 + 11), text, fill=INK, font=_font(12, True), anchor='mm')
    return tile


def emote_board(root=ROOT, out=None):
    """Both emotes in the game speech bubble (1x and 3x nearest) and bare at 72x66 and 144x132, on cream and ink."""
    from PIL import Image, ImageDraw
    out = Path(out) if out else root/'Art/Blender/previews/shop-emotes-bubble.png'
    rows = []
    for sprite_id in REACTIONS:
        with Image.open(root/SPRITES/('Shop/%s.png' % sprite_id)) as source:
            emote = source.convert('RGBA')
        tiles = []
        for name, background in (('cream', CREAM_BG), ('ink', INK)):
            tile = _bubble_tile(emote, sprite_id, background)
            tiles += [('bubble 1x ' + name, tile),
                      ('bubble 3x ' + name, tile.resize((tile.width*3, tile.height*3), Image.Resampling.NEAREST))]
        for size in ((72, 66), (144, 132)):
            for name, background in (('cream', CREAM_BG), ('ink', INK)):
                tile = Image.new('RGBA', size, background)
                tile.alpha_composite(emote.resize(size, Image.Resampling.LANCZOS) if size != emote.size else emote)
                tiles.append(('%dx%d %s' % (size + (name,)), tile))
        rows.append((sprite_id, tiles))
    gap, head, label = 12, 56, 22
    width = gap + max(sum(t.width + gap for _, t in tiles) for _, tiles in rows)
    heights = [max(t.height for _, t in tiles) + label + 26 for _, tiles in rows]
    board = Image.new('RGBA', (width, head + sum(heights) + gap), '#F5F0E7')
    draw = ImageDraw.Draw(board)
    draw.text((gap, 12), 'SHOP  |  Emotes in the order speech bubble (ShopStreetUI rects: bubble 132x130, emote 30,16,72,66)',
              fill='#58796F', font=_font(17, True))
    draw.text((gap, 36), 'Heart in the neutral bubble, Angry in the angry (Strawberry) bubble. Composited in sRGB; '
              'Unity blends in linear space.', fill='#737482', font=_font(12))
    y = head
    for (sprite_id, tiles), height in zip(rows, heights):
        draw.text((gap, y), sprite_id, fill=INK, font=_font(14, True))
        x = gap
        for name, tile in tiles:
            board.alpha_composite(tile, (x, y + 22))
            draw.text((x, y + 26 + tile.height), name, fill='#737482', font=_font(11))
            x += tile.width + gap
        y += height
    out.parent.mkdir(parents=True, exist_ok=True)
    board.convert('RGB').save(out, optimize=True)
    print('SHOP_EMOTE_BOARD %s (%d x %d)' % (out, board.width, board.height))
    return out


def main(argv=None):
    parser = argparse.ArgumentParser(description='Emote speech-bubble preview for the shop sprites (plain Python).')
    parser.add_argument('--root', type=Path, default=ROOT, help='project root holding Assets/ and Art/')
    parser.add_argument('--out', type=Path, help='PNG path (default Art/Blender/previews/shop-emotes-bubble.png)')
    args = parser.parse_args(argv)
    emote_board(args.root, args.out)
    return 0


if __name__ == '__main__':
    sys.exit(main())

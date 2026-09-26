"""Items: loose cotton candy, bagged cotton candy and sugar bags (21 UI sprites).

Built and rendered by ../build_ui_sprites.py:

    blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py -- --category items

Blender coordinates: +Z up, -Y front, metres. Every candy stands on its Cream stick
with the stick base at the world origin. Floss = one faceted core icosphere plus
LOBES jittered lobe icospheres, seeded from ui_sprite_spec.SEED and the flavour, so
the three sizes of a flavour are the same cloud scaled about FLOSS_CENTER (Large is
LARGE_RATIO x Small). The bag is the same candy (same stick, same floss) inside a
crinkled White cellophane film tied on the stick with a flavour ribbon.

Pivots (ui_sprite_spec.CATALOG): the loose candy's stick base and the bagged candy's
ribbon knot must land on the catalog pivot. `pivot_target` solves the orthographic
camera target that projects that world point to the pivot, so the pivot is exact
by construction; all nine sprites of a family share that one camera.
"""
import math
import random
import sys
from pathlib import Path

ART = Path(__file__).resolve().parents[1]
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_spec as spec

CATEGORY = 'items'
YAW, ELEVATION = -20, 15

# Candy layout shared by the loose and the bagged candy.
STICK_RADIUS = .046
STICK_TOP = 1.14                      # hidden inside the Small floss core
STRIPES = (.10, .24, .38, .52)        # stripe centres, all below the Large floss
STRIPE_HEIGHT = .06
FLOSS_CENTER = (0, 0, 1.34)
CORE = (.50, .45, .48)                # Small core radii
EGG = .12                             # core widens toward the top: x, y *= 1 + EGG * z
# Lobe tiers: count, elevation range (deg), radius range (Small).
LOBE_TIERS = ((8, (36, 66), (.27, .34)), (7, (-2, 28), (.25, .31)), (5, (-50, -20), (.19, .25)))
LOBES = sum(t[0] for t in LOBE_TIERS)
LARGE_RATIO = 1.35
FLOSS_SCALE = {'Small': 1.0, 'Medium': math.sqrt(LARGE_RATIO), 'Large': LARGE_RATIO}

# Bag: ribbon knot on the stick, facing the camera, at KNOT_Z above the ground.
KNOT_Z = .27
KNOT_RADIUS = .085
CELLOPHANE_ALPHA = .35
# Film rings over the Small floss (it spans z -.52..+.64 and r <= .66 about FLOSS_CENTER):
# z, rx, ry, pleat, tilt; scaled with the floss. The top closes in a flat, slanted seal.
BAG_PROFILE = ((-.60, .34, .32, .10, 0), (-.46, .56, .53, .04, 0), (-.28, .66, .62, 0, 0),
               (-.06, .74, .68, 0, 0), (.16, .76, .69, 0, .02), (.40, .72, .62, 0, .04),
               (.60, .70, .44, 0, .05), (.74, .72, .20, 0, .05), (.80, .72, .035, 0, .05))

LOOSE_SCALE = 176/72                  # 72 px per metre on the 156x176 canvas
BAG_SCALE = 184/64                    # 64 px per metre on the 148x184 canvas
SUGAR_SCALE = 200/120                 # 120 px per metre on the 164x200 canvas

# The pivots sit 10.6 px (loose) and 25.8 px (bag) above the canvas bottom, too low
# for the default ellipse (ry = 6.8 % of the canvas) centred on the stick. The
# default radii and strength are kept; the anchor moves back and right, under the
# floss shadow cast by the key light, so the whole ellipse stays inside the canvas.
CANDY_SHADOW = {'anchor': (.28, .42, 0), 'radii': (.30, .068), 'max_alpha': .32}
# The pouch hides the default ellipse (about 49 x 14 px around its footprint centre)
# completely, and the key light casts its shadow behind it, so the default leaves no
# visible shadow. A larger ellipse anchored on the footprint near the front edge keeps
# the catcher shadow along the whole visible base edge; strength stays default.
SUGAR_SHADOW = {'anchor': (-.05, -.20, 0), 'radii': (.50, .08), 'max_alpha': .32}


def _camera_axes(yaw, elevation):
    az, el = math.radians(yaw), math.radians(elevation)
    right = (math.cos(az), math.sin(az), 0.0)
    up = (-math.sin(az)*math.sin(el), math.cos(az)*math.sin(el), math.cos(el))
    return right, up


def pivot_target(point, pivot, canvas, scale, yaw=YAW, elevation=ELEVATION):
    """Camera target that makes ui_sprite_common.camera project `point` onto `pivot`.

    `pivot` is normalised with the origin at the bottom-left, like Unity's sprite
    pivot. The orthographic scale spans the larger canvas side (sensor fit AUTO).
    """
    w, h = canvas
    span_x, span_y = (scale, scale*h/w) if w >= h else (scale*w/h, scale)
    right, up = _camera_axes(yaw, elevation)
    dx, dy = (pivot[0]-.5)*span_x, (pivot[1]-.5)*span_y
    return tuple(round(p - dx*r - dy*u, 6) for p, r, u in zip(point, right, up))


def knot_point():
    """Centre of the ribbon knot: on the stick's neck, on the side facing the camera."""
    az = math.radians(YAW)
    return (KNOT_RADIUS*math.sin(az), -KNOT_RADIUS*math.cos(az), KNOT_Z)


def _asset(sprite_id, camera, shadow, params):
    return {'id': sprite_id, 'camera': dict(camera, yaw=YAW, elevation=ELEVATION),
            'shadow': shadow, 'params': params}


def _assets():
    catalog = spec.by_id()
    loose = catalog['CottonCandy_Strawberry_Small']
    bag = catalog['BaggedCandy_Strawberry_Small']
    loose_camera = {'target': pivot_target((0, 0, 0), loose['pivot'], loose['canvas'], LOOSE_SCALE),
                    'scale': round(LOOSE_SCALE, 6)}
    bag_camera = {'target': pivot_target(knot_point(), bag['pivot'], bag['canvas'], BAG_SCALE),
                  'scale': round(BAG_SCALE, 6)}
    sugar_camera = {'target': (0, 0, .62), 'scale': round(SUGAR_SCALE, 6)}
    shadow_note = 'default radii and max_alpha; anchor moved back under the floss shadow to fit the low pivot'
    assets = []
    for kind, camera in (('CottonCandy', loose_camera), ('BaggedCandy', bag_camera)):
        for flavor in spec.FLAVORS:
            for size in spec.SIZES:
                sprite_id = '%s_%s_%s' % (kind, flavor, size)
                params = {'kind': 'loose' if kind == 'CottonCandy' else 'bagged', 'flavor': flavor,
                          'size': size, 'floss_scale': round(FLOSS_SCALE[size], 4),
                          'floss_seed': '%d:%s' % (spec.SEED, flavor), 'lobes': LOBES,
                          'pivot': list(catalog[sprite_id]['pivot']),
                          'pivot_world': [round(v, 4) for v in ((0, 0, 0) if kind == 'CottonCandy' else knot_point())],
                          'pivot_feature': 'stick base' if kind == 'CottonCandy' else 'ribbon knot',
                          'shadow_note': shadow_note}
                if kind == 'BaggedCandy':
                    params.update(cellophane_alpha=CELLOPHANE_ALPHA, cellophane_casts_shadow=False)
                assets.append(_asset(sprite_id, camera, CANDY_SHADOW, params))
    emblems = {'Strawberry': 'strawberry', 'Soda': 'three bubbles', 'Vanilla': 'small flower'}
    for flavor in spec.FLAVORS:
        assets.append(_asset('SugarBag_%s' % flavor, sugar_camera, SUGAR_SHADOW,
                             {'kind': 'sugar bag', 'flavor': flavor, 'emblem': emblems[flavor],
                              'shadow_note': 'larger ellipse, default max_alpha; anchor on the pouch'
                                             ' footprint near the front edge so the shadow shows'
                                             ' under the base instead of hiding behind the pouch'}))
    return assets


ASSETS = _assets()


# --- Blender geometry ---------------------------------------------------------------

def build(asset, col, kit):
    params = asset['params']
    if params['kind'] == 'loose':
        candy(col, kit, params['flavor'], params['size'])
    elif params['kind'] == 'bagged':
        candy(col, kit, params['flavor'], params['size'])
        bag(col, kit, params['flavor'], params['size'])
    else:
        sugar_bag(col, kit, params['flavor'])


def _jitter(obj, rng, amount):
    """Scale each vertex radially by 1 +- amount (object space, unit primitive)."""
    for v in obj.data.vertices:
        v.co *= 1 + rng.uniform(-amount, amount)


def _egg_reach(n):
    """Distance from the core centre to the (egg-widened) core surface along unit vector n."""
    widen = 1 + EGG*n[2]
    radii = (CORE[0]*widen, CORE[1]*widen, CORE[2])
    return 1/math.sqrt(sum((c/r)**2 for c, r in zip(n, radii)))


def floss(col, kit, flavor, size):
    """Faceted egg core plus LOBES flattened, jittered lobes in three tiers (big puffs on the
    crown, medium round the middle, small ones tapering to the stick). The random stream
    depends only on SEED and the flavour, so every size is the same cloud scaled by s."""
    from mathutils import Quaternion, Vector
    rng = random.Random('%d:%s' % (spec.SEED, flavor))
    s = FLOSS_SCALE[size]
    center = Vector(FLOSS_CENTER)
    core = kit.ico(col, 'Floss_Core', FLOSS_CENTER, tuple(r*s for r in CORE), flavor, subdivisions=2)
    for v in core.data.vertices:
        v.co.x *= 1 + EGG*v.co.z
        v.co.y *= 1 + EGG*v.co.z
    core.rotation_euler.z = rng.uniform(0, math.tau)
    _jitter(core, rng, .04)
    index = 0
    for count, elevation, radius_range in LOBE_TIERS:
        offset = rng.uniform(0, 1)
        for i in range(count):
            az = (i + offset + rng.uniform(-.22, .22))*math.tau/count
            el = math.radians(rng.uniform(*elevation))
            n = Vector((math.cos(el)*math.cos(az), math.cos(el)*math.sin(az), math.sin(el)))
            radius = rng.uniform(*radius_range)
            depth = _egg_reach(n)*rng.uniform(.80, .90)
            index += 1
            lobe = kit.ico(col, 'Floss_Lobe_%02d' % index, tuple(center + n*depth*s),
                           (radius*s*rng.uniform(.95, 1.12), radius*s*rng.uniform(.95, 1.12), radius*s*.8),
                           flavor, subdivisions=2)
            twist = Quaternion((0, 0, 1), rng.uniform(0, math.tau))
            lobe.rotation_euler = (n.to_track_quat('Z', 'Y') @ twist).to_euler()
            _jitter(lobe, rng, .05)


def stick(col, kit, flavor):
    """Cream paper stick from the ground into the floss core, with raised flavour stripe rings."""
    r, h, rs = STICK_RADIUS, STRIPE_HEIGHT/2, STICK_RADIUS*1.14
    kit.loft(col, 'Cream_Paper_Stick', [(0, 0, 0, r, r), (STICK_TOP, 0, 0, r, r)], 'Cream', sides=10)
    for i, z in enumerate(STRIPES):
        kit.loft(col, '%s_Stick_Stripe_%02d' % (flavor, i+1), [(z-h, 0, 0, rs, rs), (z+h, 0, 0, rs, rs)],
                 flavor, sides=10)


def candy(col, kit, flavor, size):
    stick(col, kit, flavor)
    floss(col, kit, flavor, size)


def _cellophane(kit):
    """Palette White with the shared recipe and Principled alpha CELLOPHANE_ALPHA."""
    import bpy
    key = kit.prefix + 'White_Cellophane'
    material = bpy.data.materials.get(key)
    if material:
        return material
    material = kit.mat('White').copy()
    material.name = key
    material.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value = CELLOPHANE_ALPHA
    material.diffuse_color[3] = CELLOPHANE_ALPHA
    if hasattr(material, 'surface_render_method'):
        material.surface_render_method = 'BLENDED'
    material['alpha'] = CELLOPHANE_ALPHA
    return material


def _ring_mesh(col, kit, name, rings, sides, material, rng, crinkle=0.0, phase=0.0):
    """Closed crinkled tube. rings: (z, cx, cy, rx, ry, pleat, tilt); pleat pulls every
    other vertex in (gathered film), tilt slants the ring (z += tilt * x). Triangles only,
    so every crinkle catches the light as its own facet."""
    verts = []
    for z, cx, cy, rx, ry, pleat, tilt in rings:
        for i in range(sides):
            a = phase + i*math.tau/sides
            k = (1 - pleat*(i % 2))*(1 + rng.uniform(-crinkle, crinkle))
            x = cx + rx*k*math.sin(a)
            y = cy - ry*k*math.cos(a)
            verts.append((x, y, z + tilt*x + rng.uniform(-crinkle, crinkle)*.25*max(rx, ry)))
    faces = [tuple(reversed(range(sides)))]
    for r in range(len(rings)-1):
        for i in range(sides):
            a, b = r*sides + i, r*sides + (i+1) % sides
            c, d = a + sides, b + sides
            faces += [(a, b, d), (a, d, c)] if (i + r) % 2 else [(a, b, c), (b, d, c)]
    faces.append(tuple(range((len(rings)-1)*sides, len(rings)*sides)))
    obj = kit.mesh(col, name, verts, faces, None)
    obj.data.materials.append(material)
    # Clear film: its alpha is the visible haze, it does not dim the candy behind it.
    obj.visible_shadow = False
    return obj


def bag(col, kit, flavor, size):
    """Cellophane bag over the floss, gathered on the stick and tied with a ribbon bow."""
    from mathutils import Vector
    rng = random.Random('%d:%s:bag' % (spec.SEED, flavor))
    film = _cellophane(kit)
    s = FLOSS_SCALE[size]
    c = FLOSS_CENTER[2]
    neck = KNOT_Z + .07
    bottom = c + BAG_PROFILE[0][0]*s
    rings = [(KNOT_Z - .015, 0, 0, .062, .062, .30, 0), (neck, 0, 0, .085, .085, .34, 0),
             (neck + (bottom-neck)*.5, 0, 0, .085 + BAG_PROFILE[0][1]*s*.4, .085 + BAG_PROFILE[0][2]*s*.4, .15, 0)]
    rings += [(c + z*s, 0, 0, rx*s, ry*s, pleat, tilt) for z, rx, ry, pleat, tilt in BAG_PROFILE]
    _ring_mesh(col, kit, 'Cellophane_Bag', rings, 14, film, rng, crinkle=.04)
    # Short gathered skirt of film under the ribbon, around the stick.
    _ring_mesh(col, kit, 'Cellophane_Tail', [(KNOT_Z - .005, 0, 0, .064, .064, .25, 0),
                                              (KNOT_Z - .09, 0, 0, .105, .10, .45, 0),
                                              (KNOT_Z - .13, 0, 0, .12, .11, .50, 0)],
               12, film, rng, crinkle=.05, phase=.2)
    # Ribbon: a wrap around the neck, the knot facing the camera, two loops and two tails.
    kit.loft(col, flavor + '_Ribbon_Wrap', [(KNOT_Z - .032, 0, 0, .078, .078), (KNOT_Z + .032, 0, 0, .078, .078)],
             flavor, sides=10)
    az = math.radians(YAW)
    front, side = Vector((math.sin(az), -math.cos(az), 0)), Vector((math.cos(az), math.sin(az), 0))
    knot = Vector(knot_point())
    knot_ob = kit.ico(col, flavor + '_Ribbon_Knot', tuple(knot), (.07, .05, .062), flavor, subdivisions=1)
    knot_ob.rotation_euler.z = az
    for sign, label in ((-1, 'Left'), (1, 'Right')):
        loop_center = knot + side*sign*.15 + front*.01 + Vector((0, 0, .04))
        loop = kit.ico(col, '%s_Ribbon_Loop_%s' % (flavor, label), tuple(loop_center), (.14, .05, .08),
                       flavor, subdivisions=1)
        loop.rotation_euler = (0, -sign*.42, az)
        tail_end = knot + side*sign*.11 + front*.01 + Vector((0, 0, -.21))
        kit.band(col, '%s_Ribbon_Tail_%s' % (flavor, label), tuple(knot + front*.005), tuple(tail_end), .06,
                 flavor, .02)


def _prism(col, kit, name, rings, material):
    """Chamfered-rectangle prism. rings: (z, half_width, half_depth, chamfer, y_offset)."""
    verts = []
    for z, hw, hd, ch, oy in rings:
        verts += [(-hw+ch, oy-hd, z), (hw-ch, oy-hd, z), (hw, oy-hd+ch, z), (hw, oy+hd-ch, z),
                  (hw-ch, oy+hd, z), (-hw+ch, oy+hd, z), (-hw, oy+hd-ch, z), (-hw, oy-hd+ch, z)]
    faces = [tuple(reversed(range(8)))]
    for r in range(len(rings)-1):
        faces += [(r*8+i, r*8+(i+1) % 8, (r+1)*8+(i+1) % 8, (r+1)*8+i) for i in range(8)]
    faces.append(tuple(range((len(rings)-1)*8, len(rings)*8)))
    return kit.mesh(col, name, verts, faces, material)


def _octagon(cx, y, cz, hw, hh, ch):
    return [(cx-hw+ch, y, cz-hh), (cx+hw-ch, y, cz-hh), (cx+hw, y, cz-hh+ch), (cx+hw, y, cz+hh-ch),
            (cx+hw-ch, y, cz+hh), (cx-hw+ch, y, cz+hh), (cx-hw, y, cz+hh-ch), (cx-hw, y, cz-hh+ch)]


SUGAR_FRONT = -.30          # front face of the pouch body at label height
LABEL = {'z': .52, 'hw': .345, 'hh': .31, 'ch': .095}
EMBLEM = 1.25               # emblem size on the label, read at the 82x100 card size


def sugar_bag(col, kit, flavor):
    """Flavour paper pouch with a folded-over top and a Cream label carrying the flavour emblem."""
    _prism(col, kit, 'Paper_Pouch', [
        (0, .47, .29, .07, 0), (.06, .49, .31, .07, 0), (.90, .48, .30, .07, 0),
        (1.10, .47, .22, .06, .02), (1.20, .46, .08, .04, .04)], flavor)
    # The top is folded forward twice: a flat roll along the top and a flap over the front.
    roll = kit.box(col, 'Paper_Top_Roll', (0, -.02, 1.20), (.97, .20, .13), flavor, .035)
    roll.rotation_euler.x = math.radians(-12)
    flap = kit.box(col, 'Paper_Folded_Flap', (0, -.235, 1.06), (.99, .07, .26), flavor, .025)
    flap.rotation_euler.x = math.radians(-14)
    y = SUGAR_FRONT - .042               # label front; its back touches the pouch front
    kit.panel(col, 'Cream_Label', _octagon(0, y, LABEL['z'], LABEL['hw'], LABEL['hh'], LABEL['ch']), 'Cream', .03)
    {'Strawberry': _strawberry, 'Soda': _bubbles, 'Vanilla': _flower}[flavor](col, kit, y, LABEL['z'], EMBLEM)


def _strawberry(col, kit, y, z, k):
    berry = kit.ico(col, 'Strawberry_Emblem_Berry', (0, y - .03*k, z - .04*k), (.15*k, .055*k, .17*k),
                    'Strawberry', 2)
    for v in berry.data.vertices:          # taper the lower half into a berry point
        if v.co.z < 0:
            v.co.x *= 1 + v.co.z*.55
            v.co.y *= 1 + v.co.z*.35
    for i in range(5):                     # a fan of sepals, outer ones drooping
        t = (i-2)/2
        leaf = kit.ico(col, 'Mint_Emblem_Leaf_%02d' % (i+1), (t*.08*k, y - .065*k, z + (.115 - abs(t)*.03)*k),
                       (.07*k, .022*k, .028*k), 'Mint', 1)
        leaf.rotation_euler.y = t*.5
    kit.segment(col, 'Mint_Emblem_Stem', (0, y - .07*k, z + .13*k), (.02*k, y - .07*k, z + .21*k),
                .03*k, .03*k, 'Mint', .006)


def _bubbles(col, kit, y, z, k):
    for i, (dx, dz, r) in enumerate(((-.09, -.07, .12), (.11, .03, .085), (-.02, .15, .06))):
        kit.ico(col, 'Soda_Emblem_Bubble_%02d' % (i+1), (dx*k, y - r*.45*k, z + dz*k), (r*k, r*.55*k, r*k), 'Soda', 2)


def _flower(col, kit, y, z, k):
    for i in range(5):
        a = math.tau*i/5
        petal = kit.ico(col, 'Gold_Emblem_Petal_%02d' % (i+1), (math.sin(a)*.095*k, y - .03*k, z + math.cos(a)*.095*k),
                        (.075*k, .03*k, .055*k), 'Gold', 2)
        petal.rotation_euler.y = -a + math.pi/2
    kit.ico(col, 'Wood_Emblem_Flower_Centre', (0, y - .055*k, z), (.052*k, .03*k, .052*k), 'Wood', 2)

"""Items: loose cotton candy, bagged cotton candy and sugar bags (21 UI sprites).

Built and rendered by ../build_ui_sprites.py:

    blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py -- --category items

Blender coordinates: +Z up, -Y front, metres. Every candy stands on its Cream stick
with the stick base at the world origin. Floss = one faceted core icosphere plus
LOBES jittered lobe icospheres, seeded from ui_sprite_spec.SEED and the flavour, so
the three sizes of a flavour are the same cloud scaled about FLOSS_CENTER (Large is
LARGE_RATIO x Small). The bag is the same candy (same stick, same floss) inside a
clear White cellophane bag tied on the stick with a flavour ribbon (see `bag`).

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
# Floss shade colour. Plain Vanilla floss rendered caramel under the shared exposure (its shaded
# faces turn brown), and alternating Cream and Vanilla lobes read as caramel spots on a grey ball.
# So a Vanilla floss face stays Vanilla where the key light reaches it and turns Cream in the
# floss's own shade: the colour change follows the shading, and the floss reads as one pale
# vanilla-yellow mass, yellow where lit and pale cream where shaded. Palette colours only.
FLOSS_SHADE = {'Vanilla': 'Cream'}
SHADE_BELOW = .45                     # key-light exposure below which a floss face is in shade (see _shade)
KEY_SAMPLES = 24                      # points on the key light's disk
RAY_START = .08                       # shadow rays start this far toward the light, past lobe intersections
# Stripe ring radius over the stick radius. Vanilla stripes have almost no lightness contrast
# against the shaded Cream stick, so their rings stand further out and read as wider bands.
STRIPE_RAISE = {'Strawberry': 1.14, 'Soda': 1.14, 'Vanilla': 1.3}

# Bag: ribbon knot on the stick, facing the camera, at KNOT_Z above the ground.
KNOT_Z = .27
KNOT_RADIUS = .085
# Film rings over the Small floss (it spans z -.52..+.64 and r <= .66 about FLOSS_CENTER):
# z, rx, ry, pleat, tilt; scaled with the floss. The top closes in a flat, slanted heat seal,
# the band between the last two rings.
BAG_PROFILE = ((-.60, .34, .32, .10, 0), (-.46, .56, .53, .04, 0), (-.28, .66, .62, 0, 0),
               (-.06, .74, .68, 0, 0), (.16, .76, .69, 0, .02), (.40, .72, .62, 0, .04),
               (.60, .70, .44, 0, .05), (.74, .72, .20, 0, .05), (.77, .72, .09, 0, .05),
               (.80, .72, .035, 0, .05))
FILM_SIDES = 14
FILM_CRINKLE = .04
VEIL_ALPHA = .25                      # exact alpha of the film's back wall (see _veil)
EDGE_WIDTH = .09                      # silhouette band, metres across as seen by the camera
PLEATS = (-40, 0, 40)                 # gathered-neck pleats, degrees from the camera-facing side
PLEAT_WIDTH = .035
# Glossy streaks: azimuth (deg, same angle as the film rings), z range (Small), width (deg).
GLINTS = ((-62, (-.28, .60), 7), (24, (-.06, .40), 5))

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
                          'stripe_raise': STRIPE_RAISE[flavor], 'shadow_note': shadow_note}
                if flavor in FLOSS_SHADE:
                    params['floss_shade'] = {
                        'colour': FLOSS_SHADE[flavor], 'below': SHADE_BELOW, 'key_samples': KEY_SAMPLES,
                        'ray_start': RAY_START,
                        'rule': 'faces whose key-light exposure (mean cos over the key disk, floss-shadowed'
                                ' samples 0) is below `below` take the shade colour'}
                if kind == 'BaggedCandy':
                    params['film'] = {
                        'model': 'clear: opaque White silhouette-edge, heat-seal, pleat and glint facets over'
                                 ' a back-wall veil of exact alpha (Holdout mix); no film in front of the floss',
                        'veil_alpha': VEIL_ALPHA, 'edge_width': EDGE_WIDTH, 'pleats': list(PLEATS),
                        'glints': [list(g) for g in GLINTS], 'casts_shadow': False}
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
    depends only on SEED and the flavour, so every size is the same cloud scaled by s.
    A flavour in FLOSS_SHADE takes its shade colour on the faces in shade (see _shade)."""
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
    parts = [core]
    for count, elevation, radius_range in LOBE_TIERS:
        offset = rng.uniform(0, 1)
        for i in range(count):
            az = (i + offset + rng.uniform(-.22, .22))*math.tau/count
            el = math.radians(rng.uniform(*elevation))
            n = Vector((math.cos(el)*math.cos(az), math.cos(el)*math.sin(az), math.sin(el)))
            radius = rng.uniform(*radius_range)
            depth = _egg_reach(n)*rng.uniform(.80, .90)
            lobe = kit.ico(col, 'Floss_Lobe_%02d' % len(parts), tuple(center + n*depth*s),
                           (radius*s*rng.uniform(.95, 1.12), radius*s*rng.uniform(.95, 1.12), radius*s*.8),
                           flavor, subdivisions=2)
            twist = Quaternion((0, 0, 1), rng.uniform(0, math.tau))
            lobe.rotation_euler = (n.to_track_quat('Z', 'Y') @ twist).to_euler()
            _jitter(lobe, rng, .05)
            parts.append(lobe)
    if flavor in FLOSS_SHADE:
        _shade(parts, kit, FLOSS_SHADE[flavor])


def _world_verts(obj):
    """World-space vertices of an object whose transform the depsgraph has not evaluated yet."""
    from mathutils import Matrix
    matrix = Matrix.LocRotScale(obj.location, obj.rotation_euler.to_quaternion(), obj.scale)
    return [matrix @ v.co for v in obj.data.vertices]


def _key_samples():
    """KEY_SAMPLES points spread evenly (golden-angle spiral) over the key light's disk, which
    ui_sprite_common.studio aims at LIGHTING['aim']."""
    from mathutils import Vector
    key = spec.LIGHTING['key']
    centre = Vector(key['location'])
    aim = (Vector(spec.LIGHTING['aim']) - centre).normalized()
    u = aim.orthogonal().normalized()
    v = aim.cross(u)
    points = []
    for i in range(KEY_SAMPLES):
        r, a = key['size']/2*math.sqrt((i + .5)/KEY_SAMPLES), i*math.pi*(3 - math.sqrt(5))
        points.append(centre + (u*math.cos(a) + v*math.sin(a))*r)
    return points


def _shade(parts, kit, colour):
    """Give the floss faces in shade the `colour` material (second slot).

    A face's key-light exposure is the mean over the key disk samples of cos(normal, light),
    counting 0 where the light is behind the face or another floss face blocks the ray. Rays
    start RAY_START toward the light so the lobes' own intersections do not count as shade.
    Faces below SHADE_BELOW are in shade."""
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    worlds = [_world_verts(part) for part in parts]
    verts, polys = [], []
    for part, world in zip(parts, worlds):
        polys += [[len(verts) + i for i in p.vertices] for p in part.data.polygons]
        verts += world
    tree, lights = BVHTree.FromPolygons(verts, polys), _key_samples()
    for part, world in zip(parts, worlds):
        part.data.materials.append(kit.mat(colour))
        for poly in part.data.polygons:
            p = [world[i] for i in poly.vertices]
            centre = sum(p, Vector())/len(p)
            normal = (p[1] - p[0]).cross(p[2] - p[0]).normalized()
            exposure = 0
            for light in lights:
                ray = (light - centre).normalized()
                if normal.dot(ray) > 0 and tree.ray_cast(centre + ray*RAY_START, ray)[0] is None:
                    exposure += normal.dot(ray)
            poly.material_index = 1 if exposure/len(lights) < SHADE_BELOW else 0


def stick(col, kit, flavor):
    """Cream paper stick from the ground into the floss core, with raised flavour stripe rings
    (radius STRIPE_RAISE x the stick's)."""
    r, h, rs = STICK_RADIUS, STRIPE_HEIGHT/2, STICK_RADIUS*STRIPE_RAISE[flavor]
    kit.loft(col, 'Cream_Paper_Stick', [(0, 0, 0, r, r), (STICK_TOP, 0, 0, r, r)], 'Cream', sides=10)
    for i, z in enumerate(STRIPES):
        kit.loft(col, '%s_Stick_Stripe_%02d' % (flavor, i+1), [(z-h, 0, 0, rs, rs), (z+h, 0, 0, rs, rs)],
                 flavor, sides=10)


def candy(col, kit, flavor, size):
    stick(col, kit, flavor)
    floss(col, kit, flavor, size)


def _toward_camera():
    """Unit vector from the camera target toward the camera (ui_sprite_common.camera)."""
    az, el = math.radians(YAW), math.radians(ELEVATION)
    return (math.sin(az)*math.cos(el), -math.cos(az)*math.cos(el), math.sin(el))


def _surface(ring, a, k=1.0):
    """Point at angle a on a film ring (z, cx, cy, rx, ry, pleat, tilt), at k times its radius.
    a = 0 faces -Y; the side facing the camera is a = YAW. tilt slants the ring: z += tilt * x."""
    z, cx, cy, rx, ry, _, tilt = ring
    x, y = cx + rx*k*math.sin(a), cy - ry*k*math.cos(a)
    return (x, y, z + tilt*x)


def _film_mesh(col, kit, name, verts, faces, material=None):
    """Film object in palette White, or in the given material. Clear film does not shade the candy."""
    obj = kit.mesh(col, name, verts, faces, None if material else 'White')
    if material:
        obj.data.materials.append(material)
    obj.visible_shadow = False


def _strip(col, kit, name, rows):
    """Flat White facets between consecutive (a, b) vertex pairs."""
    verts = [v for row in rows for v in row]
    faces = [(2*j, 2*j + 1, 2*j + 3, 2*j + 2) for j in range(len(rows) - 1)]
    _film_mesh(col, kit, name, verts, faces)


def _veil_material(kit):
    """Palette White with the shared recipe, mixed with a Holdout at 1 - VEIL_ALPHA.

    Cycles resolves a partly transparent Principled BSDF by sampling, which left a sand-like
    alpha speckle at 64 samples. A Holdout is not sampled: every sample writes the same
    transparency, so the veil's alpha is exactly VEIL_ALPHA and the film stays smooth."""
    import bpy
    key = kit.prefix + 'White_Cellophane_Veil'
    material = bpy.data.materials.get(key)
    if material:
        return material
    material = kit.mat('White').copy()
    material.name = key
    nodes, links = material.node_tree.nodes, material.node_tree.links
    output = next(node for node in nodes if node.type == 'OUTPUT_MATERIAL')
    mix = nodes.new('ShaderNodeMixShader')
    mix.inputs['Fac'].default_value = VEIL_ALPHA
    links.new(nodes.new('ShaderNodeHoldout').outputs[0], mix.inputs[1])
    links.new(nodes['Principled BSDF'].outputs['BSDF'], mix.inputs[2])
    links.new(mix.outputs[0], output.inputs['Surface'])
    return material


def _veil(col, kit, rings, rng):
    """Back wall of the crinkled film tube: its triangles that face away from the camera.

    A Holdout also hides whatever lies behind it, so the front wall is left out: the candy
    stands in front of the back wall in its own colour, and around the candy the veil shows
    the bag. pleat pulls every other vertex in (gathered film); triangles only, so every
    crinkle catches the light as its own facet."""
    from mathutils import Vector
    sides, crinkle = FILM_SIDES, FILM_CRINKLE
    verts = []
    for ring in rings:
        for i in range(sides):
            k = (1 - ring[5]*(i % 2))*(1 + rng.uniform(-crinkle, crinkle))
            x, y, z = _surface(ring, i*math.tau/sides, k)
            verts.append((x, y, z + rng.uniform(-crinkle, crinkle)*.25*max(ring[3], ring[4])))
    view = Vector(_toward_camera())
    faces = []
    for r in range(len(rings) - 1):
        for i in range(sides):
            a, b = r*sides + i, r*sides + (i+1) % sides
            c, d = a + sides, b + sides
            for tri in ([(a, b, d), (a, d, c)] if (i + r) % 2 else [(a, b, c), (b, d, c)]):
                p = [Vector(verts[j]) for j in tri]
                centre = (p[0] + p[1] + p[2])/3
                normal = (p[1] - p[0]).cross(p[2] - p[0])
                if normal.dot(Vector((centre.x, centre.y, 0))) < 0:
                    normal = -normal
                if normal.dot(view) < 0:
                    faces.append(tri)
    used = sorted({j for tri in faces for j in tri})
    index = {j: n for n, j in enumerate(used)}
    _film_mesh(col, kit, 'Cellophane_Veil', [verts[j] for j in used],
               [tuple(index[j] for j in tri) for tri in faces], _veil_material(kit))


def _edges(col, kit, rings, rng):
    """Silhouette bands, where clear film is seen edge-on and shows most: per ring the
    silhouette point and a point about EDGE_WIDTH further in (+-20 %), both crinkled."""
    az = math.radians(YAW)
    dx, dy = math.sin(az), -math.cos(az)          # toward the camera, in the ground plane
    for side, label in ((0, 'Left'), (1, 'Right')):
        rows = []
        for ring in rings:
            rx, ry = ring[3], ring[4]
            a = math.atan2(rx*dy, ry*dx) + side*math.pi       # ring tangent parallel to the view
            reach = math.hypot(rx*math.cos(az), ry*math.sin(az))
            width = math.acos(max(1 - EDGE_WIDTH*rng.uniform(.8, 1.2)/reach, math.cos(math.radians(75))))
            inner = a + width if side == 0 else a - width
            rows.append((_surface(ring, a, 1.01 + rng.uniform(-FILM_CRINKLE, FILM_CRINKLE)),
                         _surface(ring, inner, 1.01 + rng.uniform(-FILM_CRINKLE, FILM_CRINKLE))))
        _strip(col, kit, 'Cellophane_Edge_' + label, rows)


def _seal(col, kit, rings, rng):
    """Heat seal: the band between the last two film rings, closed on top."""
    sides = FILM_SIDES
    verts = [_surface(ring, i*math.tau/sides, 1 + rng.uniform(-.03, .03)) for ring in rings for i in range(sides)]
    faces = [(i, (i+1) % sides, sides + (i+1) % sides, sides + i) for i in range(sides)]
    faces.append(tuple(range(sides, 2*sides)))
    _film_mesh(col, kit, 'Cellophane_Seal', verts, faces)


def _pleats(col, kit, rings, rng):
    """Folds of the gathered neck: thin White strips on the PLEATS angles, into the ribbon."""
    for n, offset in enumerate(PLEATS):
        a = math.radians(YAW + offset)
        rows = []
        for ring in rings:
            half = min(PLEAT_WIDTH/max(ring[3], ring[4]), math.radians(20))/2*rng.uniform(.8, 1.2)
            rows.append((_surface(ring, a - half, 1.015), _surface(ring, a + half, 1.015)))
        _strip(col, kit, 'Cellophane_Pleat_%d' % (n+1), rows)


def _glints(col, kit, rings, s):
    """Glossy streaks: thin White strips on the film over the GLINTS rings, widest mid-way."""
    c = FLOSS_CENTER[2]
    for n, (azimuth, (z0, z1), width) in enumerate(GLINTS):
        span = [ring for ring in rings if c + z0*s - 1e-6 <= ring[0] <= c + z1*s + 1e-6]
        rows = []
        for j, ring in enumerate(span):
            half = math.radians(width*(.4 + .6*math.sin(math.pi*j/(len(span) - 1))))/2
            rows.append(tuple(_surface(ring, math.radians(azimuth) + d, 1.02) for d in (-half, half)))
        _strip(col, kit, 'Cellophane_Glint_%d' % (n+1), rows)


def bag(col, kit, flavor, size):
    """Clear cellophane bag over the floss, gathered on the stick and tied with a ribbon bow.

    Palette White facets show the film only where clear film shows: edge-on along the
    silhouette, the heat seal on top, the pleats of the gathered neck and two glossy streaks,
    over a smooth back-wall veil. No film lies in front of the floss, so it keeps its colour."""
    from mathutils import Vector
    rng = random.Random('%d:%s:bag' % (spec.SEED, flavor))
    s = FLOSS_SCALE[size]
    c = FLOSS_CENTER[2]
    neck = KNOT_Z + .07
    bottom = c + BAG_PROFILE[0][0]*s
    # Three neck rings from the knot up to the bag's bottom ring, then the bag profile.
    rings = [(KNOT_Z - .015, 0, 0, .062, .062, .30, 0), (neck, 0, 0, .085, .085, .34, 0),
             (neck + (bottom-neck)*.5, 0, 0, .085 + BAG_PROFILE[0][1]*s*.4, .085 + BAG_PROFILE[0][2]*s*.4, .15, 0)]
    rings += [(c + z*s, 0, 0, rx*s, ry*s, pleat, tilt) for z, rx, ry, pleat, tilt in BAG_PROFILE]
    _veil(col, kit, rings, random.Random('%d:%s:veil' % (spec.SEED, flavor)))
    _edges(col, kit, rings[:-1], rng)
    _seal(col, kit, rings[-2:], rng)
    _pleats(col, kit, rings[:4], rng)             # the neck rings and the bag's bottom ring
    _glints(col, kit, rings, s)
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

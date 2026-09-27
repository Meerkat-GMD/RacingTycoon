"""Cotton-candy machine illustrations for the equipment page cards (Task 7).

    blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py -- --category machines

OutgameUI shows Machine_<i> at 126x126 on MachineCard_<i> and at 35 % alpha while the
machine is locked. The build follows the racing-scene `Spinner` in create_assets.py
(Pedestal, PedestalTop, rings, DriveHousing, CenterStick): an octagonal pedestal with a
drive neck, a bowl, a cotton-candy puff of floss rising out of the bowl and the central
spinner head, whose banded drum and cap stand up through a crater in the floss so every
tier shows it. The tiers grow in size and add one feature each:

    0 basic    small Strawberry bowl on a Cream pedestal, plain White floss
    1 soda     larger Soda bowl under a see-through bubble dome of White facets, soda bubbles
    2 premium  largest, Plum pedestal, Gold trims and a Gold star on top of the spinner head

All three share one camera and each machine is scaled by its `size` about the ground
centre, so the size step between the tiers is real and the ground line matches on the cards.
"""
import math
import random
import sys
from pathlib import Path

ART = Path(__file__).resolve().parents[1]
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
from ui_sprite_spec import SEED  # noqa: E402

CATEGORY = 'machines'
CAMERA = {'target': (0, 0, 1.16), 'scale': 2.86, 'yaw': -20, 'elevation': 15}
SHADOW = {'anchor': (.07, .10, 0), 'radii': (.30, .068), 'max_alpha': .32}
TURN = math.pi/8  # rotates an 8-sided loft so one flat face points to the front (-Y)

MACHINES = [
    {'tier': 'basic', 'size': 1.38,
     'pedestal': {'radius': .34, 'height': .40, 'taper': .04, 'body': 'Cream', 'plinth': 'Tire',
                  'collar': 'Strawberry', 'knobs': ['Strawberry']},
     'neck': {'radius': .13, 'height': .10, 'color': 'Tire'},
     'bowl': {'radius': .40, 'height': .26, 'color': 'Strawberry', 'rim': None, 'sides': 10},
     'head': {'radius': .15, 'drum': .22, 'rise': .09, 'color': 'Strawberry', 'band': 'Tire', 'cap': 'Tire'},
     'floss': {'color': 'White', 'core': (.23, .16), 'lift': .04, 'lobes': 22, 'lobe': .10, 'crater': 40},
     'dome': None, 'star': None},
    {'tier': 'soda', 'size': 1.06,
     'pedestal': {'radius': .44, 'height': .52, 'taper': .05, 'body': 'Mint', 'plinth': 'Tire',
                  'collar': 'White', 'knobs': ['Soda', 'Strawberry']},
     'neck': {'radius': .15, 'height': .10, 'color': 'Tire'},
     'bowl': {'radius': .60, 'height': .30, 'color': 'Soda', 'rim': None, 'sides': 10},
     'head': {'radius': .16, 'drum': .24, 'rise': .10, 'color': 'Strawberry', 'band': 'Tire', 'cap': 'Soda'},
     'floss': {'color': 'Soda', 'core': (.30, .22), 'lift': .05, 'lobes': 26, 'lobe': .12, 'crater': 40},
     'dome': {'sectors': 10, 'bands': (0, 24, 48, 70, 90), 'stretch': 1.08, 'ring': 'Soda', 'cap': 'Soda',
              'glints': [(-47, 2, 7, 26, 66), (-34, 1.5, 4, 48, 64)], 'bubbles': 6},
     'star': None},
    {'tier': 'premium', 'size': 1.1,
     'pedestal': {'radius': .52, 'height': .62, 'taper': .06, 'body': 'Plum', 'plinth': 'Gold',
                  'collar': 'Gold', 'knobs': ['Gold', 'Gold', 'Gold']},
     'neck': {'radius': .18, 'height': .11, 'color': 'Gold'},
     'bowl': {'radius': .72, 'height': .34, 'color': 'Cream', 'rim': 'Gold', 'sides': 10},
     'head': {'radius': .17, 'drum': .24, 'rise': .08, 'color': 'Plum', 'band': 'Gold', 'cap': 'Gold'},
     'floss': {'color': 'Strawberry', 'core': (.36, .36), 'lift': .10, 'lobes': 32, 'lobe': .16, 'crater': 40},
     'dome': None,
     'star': {'outer': .25, 'inner': .11, 'depth': .08, 'rise': .16, 'spire': 'Gold', 'color': 'Gold'}},
]

ASSETS = [{'id': 'Machine_%d' % index, 'camera': dict(CAMERA), 'shadow': dict(SHADOW), 'params': params}
          for index, params in enumerate(MACHINES)]


def chamfer(ob, width):
    mod = ob.modifiers.new('Single flat chamfer', 'BEVEL')
    mod.width = width
    mod.segments = 1
    mod.limit_method = 'ANGLE'
    return ob


def octa(kit, col, name, profile, color, sides=8, bevel=.012):
    """Faceted lathe: profile is [(z, radius), ...]; both ends are capped."""
    ob = kit.loft(col, name, [(z, 0, 0, r, r) for z, r in profile], color, sides)
    ob.rotation_euler.z = math.pi/sides
    return chamfer(ob, bevel) if bevel else ob


def ring(kit, col, name, z0, z1, r_in, r_out, color, sides=8):
    """Closed faceted annulus (a trim band that can sit around an open bowl)."""
    step = math.tau/sides
    corners = [(z0, r_out), (z1, r_out), (z1, r_in), (z0, r_in)]
    verts = [(r*math.sin(i*step), -r*math.cos(i*step), z) for z, r in corners for i in range(sides)]
    faces = [(k*sides+i, k*sides+(i+1) % sides, (k+1) % 4*sides+(i+1) % sides, (k+1) % 4*sides+i)
             for k in range(4) for i in range(sides)]
    ob = kit.mesh(col, name, verts, faces, color)
    ob.rotation_euler.z = math.pi/sides
    return chamfer(ob, .008)


def pedestal(kit, col, p):
    """Plinth, tapered body, collar and a front control plate; returns the collar top z."""
    r, h, taper = p['radius'], p['height'], p['taper']
    plinth = .08
    octa(kit, col, 'Pedestal_Plinth', [(0, r+.05), (plinth, r+.05)], p['plinth'])
    octa(kit, col, 'Pedestal_Body', [(plinth, r), (plinth+h, r-taper)], p['body'])
    top = plinth+h
    octa(kit, col, 'Pedestal_Collar', [(top, r-taper+.04), (top+.07, r-taper+.04)], p['collar'])
    # The plate lies on the front face, which leans back with the taper.
    face = math.cos(TURN)
    zc = plinth+h*.48
    distance = face*(r-taper*(zc-plinth)/h)
    tilt = math.atan(taper*face/h)
    width, height = min(.36, r*.9), h*.42
    plate = kit.box(col, 'Control_Plate', (0, -distance-.006, zc), (width, .03, height), 'Navy', .012)
    plate.rotation_euler.x = -tilt
    knobs = p['knobs']
    gap = width/(len(knobs)+1)
    for i, color in enumerate(knobs):
        x = -width/2+gap*(i+1)
        knob = kit.ico(col, 'Knob_%02d' % (i+1), (x, -distance-.035, zc+.012), (.042, .03, .042), color)
        knob.rotation_euler.x = -tilt
    return top+.07


def bowl(kit, col, p, z0):
    """Open faceted bowl: outer wall, rim and inner wall down to a floor; returns (rim z, floor z)."""
    radius, height, sides = p['radius'], p['height'], p['sides']
    wall, lip = .045, .035
    rim = z0+height+lip
    floor = z0+.07
    octa(kit, col, 'Bowl', [
        (z0, radius*.42), (z0+height*.45, radius*.82), (z0+height, radius), (rim, radius),
        (rim, radius-wall), (z0+height*.5, radius*.82-wall*1.2), (floor, radius*.42-wall)],
        p['color'], sides, .01)
    if p['rim']:
        ring(kit, col, 'Bowl_Rim_Trim', z0+height-.035, rim+.018, radius-wall*.7, radius+.035, p['rim'], sides)
    return rim, floor


def head(kit, col, p, floor, floss_top):
    """Central spinner head (the Spinner's drive housing on its centre stick): a spindle from the
    bowl floor carries a faceted drum up through the floss crater, so the drum, its band and its
    cap stand above the floss (`rise` above its top; `drum` is the drum height). Returns the cap top z."""
    r, top = p['radius'], floss_top+p['rise']
    bottom = top-p['drum']
    octa(kit, col, 'Spinner_Head', [(floor, .06), (bottom-.03, .06), (bottom, r*.9), (top-.035, r),
                                    (top, r*.78)], p['color'], 8, .01)
    ring(kit, col, 'Spinner_Head_Band', top-p['drum']*.55, top-p['drum']*.3, r-.01, r+.02, p['band'])
    cap = r*.3
    kit.ico(col, 'Spinner_Head_Cap', (0, 0, top+cap*.4), (r*.48, r*.48, cap), p['cap'])
    return top+cap*1.4


def lobe(kit, col, name, loc, radius, color, rng):
    ob = kit.ico(col, name, loc, (radius*rng.uniform(.95, 1.12), radius*.92, radius*rng.uniform(.9, 1.05)), color, 2)
    ob.rotation_euler = [rng.uniform(-.3, .3) for _ in range(3)]
    for v in ob.data.vertices:
        v.co *= rng.uniform(.96, 1.04)
    return ob


def floss(kit, col, p, rim, rng):
    """Cotton-candy puff rising out of the bowl, built like the spec's cotton candy: a slightly
    larger core with many part-buried lobes of mixed size and jittered vertices. The lobes leave
    a crater of `crater` degrees around the top so the spinner head can rise through it.
    Returns its top z."""
    rx, rz = p['core']
    centre = rim+p['lift']
    lobe(kit, col, 'Floss_Core', (0, 0, centre), 1, p['color'], rng).scale = (rx, rx*.92, rz)
    first = math.cos(math.radians(p['crater']))
    for i in range(p['lobes']):
        k = (i+.5)/p['lobes']
        polar = math.acos(first-(first+.7)*k)  # golden-angle spiral from the crater down to ~134 deg
        azimuth = i*2.39996+rng.uniform(-.2, .2)
        d = .74+rng.uniform(-.06, .06)
        loc = (rx*d*math.sin(polar)*math.sin(azimuth), -rx*d*math.sin(polar)*math.cos(azimuth),
               centre+rz*d*math.cos(polar))
        lobe(kit, col, 'Floss_Lobe_%02d' % (i+1), loc, p['lobe']*rng.uniform(.55, 1.0), p['color'], rng)
    return centre+rz+p['lobe']*.5


def dome(kit, col, p, bowl_radius, rim, rng):
    """See-through bubble dome: White facets on the far half and the crown, open front with glints."""
    sectors, bands, stretch = p['sectors'], p['bands'], p['stretch']
    radius = bowl_radius+.012
    step = math.tau/sectors
    camera = math.radians(CAMERA['yaw'])

    def point(azimuth, elevation, r):
        e = math.radians(elevation)
        return (r*math.cos(e)*math.sin(azimuth), -r*math.cos(e)*math.cos(azimuth), rim+r*math.sin(e)*stretch)

    verts, faces = [], []

    def facet(a0, a1, e0, e1, top=None):
        # One flat facet as a thin closed slab (outer quad, inner quad, four sides); `top` tapers it.
        t0, t1 = top or (a0, a1)
        quad = [(a0, e0), (a1, e0), (t1, e1), (t0, e1)]
        n = len(verts)
        verts.extend(point(a, e, radius) for a, e in quad)
        verts.extend(point(a, e, radius-.022) for a, e in quad)
        faces.extend([(n, n+1, n+2, n+3), (n+7, n+6, n+5, n+4)])
        faces.extend((n+i, n+4+i, n+4+(i+1) % 4, n+(i+1) % 4) for i in range(4))

    offset = step/2  # flat facet faces the front, like the pedestal
    for b in range(len(bands)-1):
        for s in range(sectors):
            a0, a1 = s*step-offset, (s+1)*step-offset
            facing = math.cos((a0+a1)/2-camera)
            crown = b == len(bands)-2
            if facing < .35 or crown:
                facet(a0, a1, bands[b], bands[b+1])
    kit.mesh(col, 'Dome_Shell', verts, faces, 'White')
    verts, faces = [], []
    # Narrow tapered glints on the front-left suggest glass catching the key light.
    for azimuth, bottom, top, e0, e1 in p['glints']:
        a, b, t = math.radians(azimuth), math.radians(bottom)/2, math.radians(top)/2
        facet(a-b, a+b, e0, e1, (a-t, a+t))
    kit.mesh(col, 'Dome_Glints', verts, faces, 'White')
    ring(kit, col, 'Dome_Base_Ring', rim-.01, rim+.05, radius-.03, radius+.03, p['ring'], sectors)
    top = rim+radius*stretch
    octa(kit, col, 'Dome_Vent', [(top-.03, .11), (top+.035, .09), (top+.06, .05)], p['cap'], 8, .006)
    kit.ico(col, 'Dome_Knob', (0, 0, top+.1), (.055, .055, .055), p['cap'])
    # Soda bubbles rising past the dome's right shoulder.
    bubbles = [((1.02, -.05, .56), .07, 'White'), ((1.16, 0, .78), .05, 'Soda'),
               ((.96, -.10, .96), .056, 'White'), ((1.14, .02, 1.12), .038, 'Soda'),
               ((-1.06, -.05, .60), .048, 'Soda'), ((-1.0, -.08, .80), .034, 'White')][:p['bubbles']]
    for i, ((x, y, dz), r, color) in enumerate(bubbles):
        b = kit.ico(col, 'Bubble_%02d' % (i+1), (x*radius, y, rim+dz*radius), (r, r, r), color, 2)
        b.rotation_euler = [rng.uniform(0, math.pi) for _ in range(3)]


def star(kit, col, p, base, height):
    """Faceted five-point Gold star on a Gold spire rising from the spinner head cap, facing the camera."""
    top = base+height
    octa(kit, col, 'Star_Spire', [(base, .035), (top-p['outer']*.6, .028)], p['spire'], 8, 0)
    outer, inner, depth = p['outer'], p['inner'], p['depth']
    outline = [((outer if i % 2 == 0 else inner)*math.sin(i*math.pi/5), 0,
                (outer if i % 2 == 0 else inner)*math.cos(i*math.pi/5)) for i in range(10)]
    verts = outline+[(0, -depth, 0), (0, depth, 0)]
    faces = [(i, (i+1) % 10, 10) for i in range(10)]+[((i+1) % 10, i, 11) for i in range(10)]
    ob = kit.mesh(col, 'Star', verts, faces, p['color'])
    ob.location = (0, 0, top)
    ob.rotation_euler = (0, math.radians(-6), math.radians(CAMERA['yaw']))


def build(asset, col, kit):
    p = asset['params']
    rng = random.Random(SEED+int(asset['id'][-1]))
    z = pedestal(kit, col, p['pedestal'])
    n = p['neck']
    octa(kit, col, 'Drive_Neck', [(z-.01, n['radius']), (z+n['height'], n['radius']*.9)], n['color'])
    rim, floor = bowl(kit, col, p['bowl'], z+n['height']-.02)
    top = head(kit, col, p['head'], floor, floss(kit, col, p['floss'], rim, rng))
    if p['dome']:
        dome(kit, col, p['dome'], p['bowl']['radius'], rim, rng)
    if p['star']:
        star(kit, col, p['star'], top-.03, p['star']['rise'])
    for ob in col.objects:  # tier size step, scaled about the ground centre
        ob.location *= p['size']
        ob.scale *= p['size']

"""Trait icons: the 23 `Icons/Trait_*` sprites drawn on the growth-map category discs.

    blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py -- --category icons

Spec: docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md, section "특성 아이콘".
OutgameTraitsUI shows each icon at 42x42 on a 64 px category disc (84x84 canvas), at 55 %
alpha while the node is unreachable. Every icon is one faceted toy object seen from yaw
-8 deg and elevation 15 deg, without a shadow. Each camera frames the larger side of the
silhouette at 70 % of the canvas (78 % for the long kart, megaphone, spinning stick and
spoon, so they keep enough solid pixels).

Colour rule: the disc colour comes from the node category in
Assets/CottonCircuit/Scripts/Core/Progression.cs (`ui_sprite_spec.DISC_COLORS`). Each icon
pairs a Navy, Wood or Gold key shape with light facets so it reads on cream, on ink, on
its own disc and faded, and no icon is dominated by a colour close to its disc.

Each builder models its object around the local origin (Z up, -Y towards the camera);
build() parents every part to one pose empty at the studio aim point (0, 0, 1.25).
"""
import math
import random
import sys
from pathlib import Path

import bpy

ART = Path(__file__).resolve().parents[1]
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_spec as spec

CATEGORY = 'icons'
YAW, ELEVATION = -8, 15
AIM = (0, 0, 1.25)
FRONT = (math.pi/2, 0, 0)  # rotates a Z-axis cylinder so its cap faces the camera (-Y)

# id, subject, trait node ids (Progression.cs), node category, camera target, orthographic scale
ICONS = [
    ('Trait_Hours', 'wall clock', ('hours',), 'business', (0, 0.003, 1.261), 1.874),
    ('Trait_Patience', 'hourglass', ('patience',), 'business', (0.011, -0.002, 1.25), 1.874),
    ('Trait_Ads', 'megaphone', ('ads',), 'business', (0.088, -0.024, 1.206), 1.905),
    ('Trait_RepeatAds', 'heart speech bubble', ('repeat_ads',), 'business', (-0.002, -0.012, 1.206), 1.745),
    ('Trait_Shelf', 'display shelf', ('shelf',), 'business', (-0.001, -0.006, 1.228), 1.745),
    ('Trait_Sales', 'shop signboard', ('sales',), 'sales', (0.001, 0.009, 1.283), 1.939),
    ('Trait_PriceTag', 'price tag', ('flavor_price', 'location_price'), 'sales', (0.089, -0.018, 1.228), 1.745),
    ('Trait_Engine', 'engine', ('engine',), 'equipment', (0.068, -0.001, 1.283), 2.068),
    ('Trait_Handling', 'steering wheel', ('handling',), 'equipment', (0, 0, 1.25), 1.81),
    ('Trait_Kart', 'classic kart', ('coupe',), 'equipment', (0.076, -0.031, 1.174), 2.059),
    ('Trait_StickSpeed', 'spinning stick with motion arcs', ('stick_speed',), 'production',
     (-0.02, 0.018, 1.305), 1.943),
    ('Trait_Spoon', 'measuring spoon', ('stick_saving', 'sugar_saving'), 'production',
     (-0.033, 0.013, 1.281), 1.661),
    ('Trait_Ribbon', 'ribbon cotton candy', ('stick_quality', 'quality_focus'), 'production',
     (0.024, 0.008, 1.294), 2.638),
    ('Trait_Sugar2', 'sugar bag with 2 Gold stars', ('sugar_2',), 'production', (0, -0.003, 1.239), 1.648),
    ('Trait_Sugar3', 'sugar bag with 3 Gold stars', ('sugar_3',), 'production', (0, -0.003, 1.239), 1.648),
    ('Trait_Machine', 'cotton-candy machine', ('machine_2', 'machine_3'), 'equipment', (0.015, 0.027, 1.359), 2.327),
    ('Trait_FlavorSoda', 'Soda cotton candy', ('flavor_soda',), 'production', (0.048, 0.014, 1.326), 2.488),
    ('Trait_FlavorVanilla', 'Vanilla cotton candy', ('flavor_vanilla',), 'production', (0.059, 0.012, 1.326), 2.488),
    ('Trait_Worker', 'worker with apron', ('worker_1', 'worker_2'), 'staff', (0.057, 0.004, 1.293), 1.783),
    ('Trait_GradCap', 'graduation cap', ('worker_grade_2', 'worker_grade_3'), 'staff', (-0.004, -0.015, 1.195), 2.044),
    ('Trait_Glove', 'work glove', ('worker_speed',), 'staff', (0.081, 0.009, 1.326), 1.648),
    ('Trait_MapPin', 'map pin', ('location_1', 'location_2', 'location_3'), 'location', (0.008, -0.022, 1.174), 2.036),
    ('Trait_Group', 'customer group', ('group_visit',), 'business', (-0.041, 0.035, 1.359), 2.133),
]

ASSETS = [{'id': sprite_id,
           'camera': {'target': target, 'scale': scale, 'yaw': YAW, 'elevation': ELEVATION},
           'shadow': None,
           'params': {'subject': subject, 'traits': list(traits), 'category': category,
                      'disc': spec.DISC_COLORS[category]}}
          for sprite_id, subject, traits, category, target, scale in ICONS]


# ---------------------------------------------------------------- primitives

def chamfer(obj, width):
    if width > 0:
        mod = obj.modifiers.new('Single flat chamfer', 'BEVEL')
        mod.width = width
        mod.segments = 1
    return obj


def cyl(kit, col, name, loc, radius, depth, material, vertices=12, rot=(0, 0, 0), bevel=.02):
    """Faceted cylinder along local Z (rot=FRONT turns its cap towards the camera)."""
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = kit.place(bpy.context.object, col, name, loc, material)
    obj.rotation_euler = rot
    return chamfer(obj, min(bevel, depth*.3, radius*.3))


def ring(kit, col, name, loc, major, minor, material, segments=16, sides=6, rot=FRONT):
    bpy.ops.mesh.primitive_torus_add(major_segments=segments, minor_segments=sides,
                                     major_radius=major, minor_radius=minor)
    obj = kit.place(bpy.context.object, col, name, loc, material)
    obj.rotation_euler = rot
    return obj


def puff(kit, col, name, pts, depth, material, bulge=.05, loc=(0, 0, 0), rot=(0, 0, 0),
         inset=None, center=None):
    """Closed slab from an XZ outline. The front rises to a centre point (inset=None, faceted
    like a toy star) or to a flat cap inset towards the centre; the back rises half as much."""
    n = len(pts)
    cx, cz = center or (sum(x for x, _ in pts)/n, sum(z for _, z in pts)/n)
    verts = [(x, -depth/2, z) for x, z in pts] + [(x, depth/2, z) for x, z in pts]
    faces = [(i, (i+1) % n, (i+1) % n+n, i+n) for i in range(n)]
    if inset is None:
        verts += [(cx, -depth/2-bulge, cz), (cx, depth/2+bulge*.5, cz)]
        faces += [((i+1) % n, i, 2*n) for i in range(n)]
        faces += [(i+n, (i+1) % n+n, 2*n+1) for i in range(n)]
    else:
        verts += [(cx+(x-cx)*inset, -depth/2-bulge, cz+(z-cz)*inset) for x, z in pts]
        faces += [(i, (i+1) % n, 2*n+(i+1) % n, 2*n+i) for i in range(n)]
        faces += [tuple(range(2*n, 3*n)), tuple(range(n, 2*n))]
    obj = kit.mesh(col, name, verts, faces, material)
    obj.location, obj.rotation_euler = loc, rot
    return obj


def arc(kit, col, name, center, radius, start, end, width, depth, material, segments=8,
        horizontal=False, taper=1.0):
    """Curved band from `start` to `end` degrees: in the XZ plane facing the camera, or
    horizontal around the Z axis. Its width grows from taper*width to width."""
    cx, cy, cz = center

    def point(r, a, off):
        if horizontal:
            return (cx+r*math.cos(a), cy+r*math.sin(a), cz+off)
        return (cx+r*math.cos(a), cy+off, cz+r*math.sin(a))
    verts = []
    for off in (-depth/2, depth/2):
        for i in range(segments+1):
            a = math.radians(start+(end-start)*i/segments)
            half = width*(taper+(1-taper)*i/segments)/2
            verts += [point(radius-half, a, off), point(radius+half, a, off)]
    m = 2*(segments+1)
    faces = []
    for i in range(segments):
        a, b, c, d = 2*i, 2*i+1, 2*i+3, 2*i+2
        faces += [(a, b, c, d), (d+m, c+m, b+m, a+m), (b, b+m, c+m, c), (a, d, d+m, a+m)]
    faces += [(0, m, m+1, 1), (m-2, m-1, 2*m-1, 2*m-2)]
    return kit.mesh(col, name, verts, faces, material)


def group(col, kit, name, objects, loc=(0, 0, 0), rot=(0, 0, 0)):
    """Parent `objects` to an empty and pose them together (rot in degrees)."""
    empty = bpy.data.objects.new(kit.prefix + name, None)
    col.objects.link(empty)
    for ob in objects:
        ob.parent = empty
    empty.location = loc
    empty.rotation_euler = [math.radians(a) for a in rot]
    return empty


def made_since(col, before):
    return [ob for ob in col.objects if ob not in before and ob.parent is None]


def star_outline(radius, inner=.45, points=5):
    return [((radius if i % 2 == 0 else radius*inner)*math.cos(math.pi/2+math.pi*i/points),
             (radius if i % 2 == 0 else radius*inner)*math.sin(math.pi/2+math.pi*i/points))
            for i in range(points*2)]


def star(kit, col, name, loc, radius, material, depth=.08, rot=(0, 0, 0)):
    return puff(kit, col, name, star_outline(radius), depth, material, radius*.32, loc, rot, center=(0, 0))


def heart_outline(width, n=20):
    return [(16*math.sin(t)**3*width/32,
             (13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t))*width/32)
            for t in (math.tau*i/n for i in range(n))]


def rounded_rect(width, height, corner, steps=2):
    pts = []
    for cx, cz, a0 in ((width/2-corner, height/2-corner, 0), (-width/2+corner, height/2-corner, 90),
                       (-width/2+corner, -height/2+corner, 180), (width/2-corner, -height/2+corner, 270)):
        for i in range(steps+1):
            a = math.radians(a0+90*i/steps)
            pts.append((cx+corner*math.cos(a), cz+corner*math.sin(a)))
    return pts


def jitter(obj, rng, amount=.03):
    for v in obj.data.vertices:
        v.co *= rng.uniform(1-amount, 1+amount)


def cotton(col, kit, floss, stripe, seed, lobes=None):
    """Spec cotton candy: one core and 20 jittered lobes on a Cream stick with stripes.
    `lobes` optionally colours the lobes in turn (the floss core keeps `floss`)."""
    rng = random.Random(spec.SEED + seed)
    kit.ico(col, 'Floss_Core', (0, 0, .30), (.44, .38, .48), floss, 2)
    positions = [(math.sin(a)*.40, rng.uniform(-.07, .07), .30+math.cos(a)*.44)
                 for a in (math.tau*i/12 for i in range(12))]
    positions += [(math.sin(a)*.26, -.28 if i < 4 else .28, .30+math.cos(a)*.30)
                  for i, a in enumerate(math.tau*(i % 4)/4+.45 for i in range(8))]
    for i, pos in enumerate(positions):
        r = rng.uniform(.20, .25)
        lobe = kit.ico(col, 'Floss_Lobe_%02d' % (i+1), pos,
                       (r*rng.uniform(.94, 1.12), r*.9, r*rng.uniform(.96, 1.12)),
                       lobes[i % len(lobes)] if lobes else floss, 2)
        lobe.rotation_euler = [rng.uniform(-.3, .3) for _ in range(3)]
        jitter(lobe, rng)
    cyl(kit, col, 'Stick', (0, 0, -.38), .052, .82, 'Cream', 8, bevel=.01)
    for i, z in enumerate((-.70, -.56, -.42)):
        cyl(kit, col, 'Stick_Stripe_%d' % (i+1), (0, 0, z), .056, .06, stripe, 8, bevel=.008)


def head(col, kit, name, center, s, skin, hair, cap=None):
    """Toy head in the approved customers' language: chamfered block face, two small Navy
    dot eyes, Strawberry blush, faceted hair (or a visor cap in `cap`)."""
    x, y, z = center
    kit.box(col, name+'_Face', (x, y, z), (s, .9*s, .92*s), skin, .15*s)
    for side, tag in ((-1, 'L'), (1, 'R')):
        kit.box(col, '%s_Eye_%s' % (name, tag), (x+side*.21*s, y-.45*s, z-.04*s),
                (.11*s, .04*s, .16*s), 'Navy', .015*s)
        kit.box(col, '%s_Blush_%s' % (name, tag), (x+side*.34*s, y-.445*s, z-.21*s),
                (.15*s, .03*s, .07*s), 'Strawberry', 0)
    kit.box(col, name+'_Hair_Back', (x, y+.30*s, z+.04*s), ((1.0 if cap else 1.08)*s, .38*s, .84*s),
            hair, .08*s)
    if cap:
        kit.loft(col, name+'_Cap_Crown', [(z+.26*s, x, y+.02*s, .56*s, .5*s),
                                          (z+.5*s, x, y+.04*s, .54*s, .48*s),
                                          (z+.64*s, x, y+.06*s, .36*s, .32*s)], cap, 8)
        visor = kit.box(col, name+'_Cap_Visor', (x, y-.68*s, z+.32*s), (.9*s, .56*s, .09*s), cap, .03*s)
        visor.rotation_euler.x = math.radians(12)  # front edge dips
        kit.ico(col, name+'_Cap_Button', (x, y+.06*s, z+.66*s), (.08*s, .08*s, .05*s), cap, 1)
        return
    kit.box(col, name+'_Hair_Crown', (x, y+.04*s, z+.42*s), (1.1*s, 1.0*s, .3*s), hair, .1*s)
    fringe = [(-.56, .40), (.30, .40), (.52, .22), (.10, .30), (-.18, .12), (-.52, .06)]
    puff(kit, col, name+'_Hair_Fringe', [(x+px*s, z+pz*s) for px, pz in fringe], .16*s, hair,
         .05*s, (0, y-.43*s, 0))


def bust(col, kit, name, center, s, skin, hair, shirt, cap=None, shoulders=.66):
    x, y, z = center
    w = shoulders
    kit.loft(col, name+'_Torso', [(z-1.05*s, x, y, w*s, .40*s), (z-.62*s, x, y, w*s, .40*s),
                                  (z-.46*s, x, y, (w-.1)*s, .34*s), (z-.40*s, x, y, .22*s, .18*s)], shirt, 8)
    head(col, kit, name+'_Head', (x, y, z), s, skin, hair, cap)


# ---------------------------------------------------------------- the 23 icons

def clock(col, kit):
    """Re-authored 966cf73 sample clock with the toy_kit material recipe."""
    cyl(kit, col, 'Clock_Case', (0, .035, 0), .64, .18, 'Soda', 16, FRONT, .035)
    cyl(kit, col, 'Clock_Cream_Bezel', (0, -.065, 0), .575, .055, 'Cream', 16, FRONT, .016)
    cyl(kit, col, 'Clock_Dial', (0, -.101, 0), .516, .029, 'White', 16, FRONT, .008)
    for i in range(4):
        a = math.pi*i/2
        marker = kit.box(col, 'Dial_Marker_%02d' % i, (math.sin(a)*.427, -.125, math.cos(a)*.427),
                         (.05, .022, .095), 'Gold', .008)
        marker.rotation_euler.y = a
    kit.segment(col, 'Minute_Hand_12', (0, -.151, 0), (0, -.151, .36), .065, .035, 'Navy')
    kit.segment(col, 'Hour_Hand_4_30', (0, -.17, 0), (.268, -.17, -.125), .075, .038, 'Navy')
    kit.ico(col, 'Hands_Hub', (0, -.19, 0), (.064, .034, .064), 'Navy', 2)


def hourglass(col, kit):
    for name, z in (('Top', .52), ('Bottom', -.52)):
        cyl(kit, col, name+'_Plate', (0, 0, z), .42, .10, 'Navy', 8, (0, 0, math.pi/8), .025)
    for side in (-1, 1):
        cyl(kit, col, 'Post_%s' % ('L' if side < 0 else 'R'), (side*.34, -.06, 0), .04, .96, 'Navy', 6, bevel=0)
    kit.loft(col, 'Glass_Upper', [(.24, 0, 0, .22, .22), (.36, 0, 0, .29, .29), (.47, 0, 0, .27, .27)], 'Soda', 8)
    kit.loft(col, 'Sand_Upper', [(.04, 0, 0, .05, .05), (.13, 0, 0, .12, .12), (.24, 0, 0, .22, .22)], 'Vanilla', 8)
    kit.loft(col, 'Glass_Lower', [(-.26, 0, 0, .24, .24), (-.13, 0, 0, .12, .12), (-.04, 0, 0, .05, .05)], 'Soda', 8)
    kit.loft(col, 'Sand_Lower', [(-.47, 0, 0, .27, .27), (-.37, 0, 0, .29, .29), (-.26, 0, 0, .24, .24)], 'Vanilla', 8)
    cyl(kit, col, 'Sand_Stream', (0, 0, -.1), .022, .2, 'Vanilla', 6, bevel=0)


def megaphone(col, kit):
    before = set(col.objects)
    kit.loft(col, 'Horn', [(-.40, 0, 0, .12, .12), (-.26, 0, 0, .14, .14), (.34, 0, 0, .40, .40)], 'Strawberry', 10)
    kit.loft(col, 'Bell_Rim', [(.30, 0, 0, .40, .40), (.38, 0, 0, .46, .46)], 'Cream', 10)
    cyl(kit, col, 'Bell_Mouth', (0, 0, .385), .38, .02, 'Navy', 10, bevel=0)
    cyl(kit, col, 'Mouthpiece', (0, 0, -.47), .1, .14, 'Navy', 10, bevel=.02)
    kit.box(col, 'Grip', (.28, 0, -.06), (.34, .14, .12), 'Navy', .03)
    group(col, kit, 'Horn_Pose', made_since(col, before), (-.12, 0, -.08), (0, 72, -34))
    for i, (radius, material) in enumerate(((.34, 'Soda'), (.52, 'Soda'))):
        arc(kit, col, 'Sound_Wave_%d' % (i+1), (.22, -.25, .05), radius, -38, 48, .07, .06, material, 6)


def heart_bubble(col, kit):
    ellipse = [(.60*math.cos(math.radians(a)), .44*math.sin(math.radians(a))) for a in range(250, 576, 25)]
    puff(kit, col, 'Speech_Bubble', ellipse + [(-.56, -.66)], .20, 'Soda', .05, (0, .04, .06), inset=.8)
    puff(kit, col, 'Heart', heart_outline(.62), .14, 'Strawberry', .09, (0, -.14, .08), center=(0, -.02))


def mini_candy(col, kit, name, loc, flavor, s=1.0, stick=True):
    """A small four-lobe cotton-candy puff (emblem and shelf stock)."""
    x, y, z = loc
    rng = random.Random(spec.SEED + len(name))
    for i, (dx, dz, r) in enumerate(((0, .02, .15), (-.1, -.04, .11), (.1, -.03, .11), (0, .12, .1))):
        lobe = kit.ico(col, '%s_Lobe_%d' % (name, i), (x+dx*s, y-.02*s*(i == 3), z+dz*s),
                       (r*s, r*s*.9, r*s), flavor, 2)
        jitter(lobe, rng)
    if stick:
        cyl(kit, col, name+'_Stick', (x, y, z-.19*s), .025*s, .2*s, 'Cream', 6, bevel=0)


def shelf(col, kit):
    for side in (-1, 1):
        kit.box(col, 'Post_%s' % ('L' if side < 0 else 'R'), (side*.53, 0, -.02), (.1, .36, 1.1), 'Navy', .025)
    for name, z in (('Bottom', -.52), ('Middle', -.06), ('Top', .44)):
        kit.box(col, name+'_Board', (0, 0, z), (1.16, .38, .08), 'Wood', .02)
    for row, (z, flavors) in enumerate(((-.31, ('Soda', 'Vanilla', 'Strawberry')),
                                        (.15, ('Strawberry', 'Soda', 'Vanilla')))):
        for i, flavor in enumerate(flavors):
            mini_candy(col, kit, 'Candy_%d%d' % (row, i), ((i-1)*.31, -.02, z), flavor, 1.12, stick=False)


def signboard(col, kit):
    puff(kit, col, 'Board_Frame', rounded_rect(1.34, .74, .12), .12, 'Navy', .02, (0, 0, -.12), inset=.9)
    puff(kit, col, 'Board_Face', rounded_rect(1.14, .56, .08), .06, 'Cream', .02, (0, -.08, -.12), inset=.92)
    kit.box(col, 'Hanger_Bar', (0, .0, .52), (1.34, .1, .08), 'Navy', .02)
    for side in (-1, 1):
        kit.box(col, 'Hanger_Rod_%d' % (side+1), (side*.42, 0, .37), (.05, .05, .24), 'Gold', .01)
    mini_candy(col, kit, 'Emblem', (-.3, -.14, -.07), 'Strawberry', 1.25)
    for i, (width, z) in enumerate(((.44, 0), (.3, -.2))):
        kit.box(col, 'Letter_Bar_%d' % i, (.08+width/2, -.13, z-.08), (width, .04, .09), 'Navy', .012)


def price_tag(col, kit):
    before = set(col.objects)
    outline = [(-.62, 0), (-.38, .32), (.52, .32), (.52, -.32), (-.38, -.32)]
    puff(kit, col, 'Tag', outline, .1, 'Soda', .03, inset=.86)
    cyl(kit, col, 'Grommet', (-.38, -.06, 0), .1, .05, 'Cream', 10, FRONT, .01)
    cyl(kit, col, 'Grommet_Hole', (-.38, -.085, 0), .05, .03, 'Navy', 8, FRONT, 0)
    cyl(kit, col, 'Coin', (.12, -.08, 0), .21, .07, 'Gold', 14, FRONT, .015)
    star(kit, col, 'Coin_Star', (.12, -.13, 0), .12, 'Gold', .04)
    group(col, kit, 'Tag_Pose', made_since(col, before), (.08, 0, -.05), (0, 24, 0))
    arc(kit, col, 'String', (-.30, -.02, .30), .2, -81, 200, .045, .045, 'Navy', 10)  # starts at the hole


def engine(col, kit):
    """Side view of the engine pictogram: block, head, top intake, pulley and exhaust."""
    kit.box(col, 'Block', (0, 0, -.1), (.92, .5, .46), 'Strawberry', .05)
    kit.box(col, 'Oil_Pan', (.04, 0, -.39), (.7, .42, .14), 'Navy', .03)
    kit.box(col, 'Cylinder_Head', (0, 0, .2), (.84, .46, .16), 'Navy', .03)
    kit.box(col, 'Valve_Cover', (0, 0, .32), (.7, .38, .1), 'Cream', .025)
    kit.box(col, 'Intake_Neck', (0, 0, .43), (.12, .14, .14), 'Navy', .015)
    kit.box(col, 'Intake_Bar', (0, 0, .52), (.46, .18, .08), 'Navy', .02)
    kit.box(col, 'Fan_Housing', (-.54, 0, -.06), (.18, .38, .32), 'Navy', .03)
    cyl(kit, col, 'Pulley', (-.3, -.27, -.14), .14, .05, 'Gold', 12, FRONT, .012)
    cyl(kit, col, 'Pulley_Hub', (-.3, -.3, -.14), .05, .04, 'Navy', 8, FRONT, .008)
    for i, x in enumerate((-.02, .14, .30)):
        cyl(kit, col, 'Bore_Plug_%d' % i, (x, -.265, -.02), .055, .04, 'Cream', 8, FRONT, .008)
    kit.box(col, 'Exhaust_Port', (.53, 0, -.02), (.16, .32, .22), 'Navy', .03)
    kit.segment(col, 'Exhaust_Pipe', (.58, 0, -.06), (.74, 0, -.3), .1, .1, 'Gold', .015)


def steering_wheel(col, kit):
    before = set(col.objects)
    ring(kit, col, 'Rim', (0, 0, 0), .52, .09, 'Wood', 16, 6)  # a classic wooden rim
    kit.box(col, 'Top_Marker', (0, -.01, .52), (.1, .2, .19), 'Strawberry', .02)
    for name, a in (('Left', 180), ('Right', 0), ('Bottom', 270)):
        r = math.radians(a)
        kit.segment(col, 'Spoke_'+name, (math.cos(r)*.12, -.02, math.sin(r)*.12),
                    (math.cos(r)*.46, -.01, math.sin(r)*.46), .13, .07, 'Cream', .015)
    cyl(kit, col, 'Hub', (0, -.03, 0), .19, .12, 'Navy', 12, FRONT, .03)
    star(kit, col, 'Hub_Star', (0, -.11, 0), .1, 'Gold', .04)
    group(col, kit, 'Wheel_Pose', made_since(col, before), (0, 0, 0), (-18, 0, 0))


def kart(col, kit):
    before = set(col.objects)
    kit.box(col, 'Chassis', (0, 0, -.22), (1.22, .56, .06), 'Navy', .015)
    kit.box(col, 'Body', (-.08, 0, -.1), (.92, .5, .2), 'Strawberry', .04)
    nose = [(.38, -.25, -.2), (.38, .25, -.2), (.38, .25, .0), (.38, -.25, .0),
            (.74, -.17, -.2), (.74, .17, -.2), (.74, .17, -.12), (.74, -.17, -.12)]
    chamfer(kit.mesh(col, 'Nose', nose, [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3),
                                         (3, 7, 4, 0)], 'Strawberry'), .02)
    kit.box(col, 'Bumper', (.78, 0, -.18), (.08, .62, .07), 'Cream', .02)
    kit.box(col, 'Seat_Back', (-.26, 0, .12), (.1, .34, .34), 'Navy', .03).rotation_euler.y = math.radians(-18)
    kit.box(col, 'Seat', (-.14, 0, .02), (.3, .34, .08), 'Navy', .02)
    kit.segment(col, 'Steering_Column', (.36, 0, -.02), (.16, 0, .22), .05, .05, 'Tire', .01)
    ring(kit, col, 'Steering_Wheel', (.16, 0, .24), .1, .025, 'Tire', 10, 4, (0, math.radians(-70), 0))
    kit.box(col, 'Engine', (-.5, .05, .02), (.2, .3, .2), 'Vanilla', .03)
    cyl(kit, col, 'Exhaust', (-.64, .1, .06), .04, .14, 'Gold', 8, (0, math.pi/2, 0), .008)
    cyl(kit, col, 'Number_Plate', (.05, -.26, -.1), .09, .02, 'Cream', 10, FRONT, 0)
    for x in (-.42, .44):
        for y in (-.36, .36):
            tag = '%s%s' % ('R' if x < 0 else 'F', 'L' if y < 0 else 'R')
            cyl(kit, col, 'Wheel_'+tag, (x, y, -.2), .2 if x < 0 else .17, .16, 'Tire', 12, FRONT, .03)
            cyl(kit, col, 'Hubcap_'+tag, (x, y*1.24, -.2), .08, .03, 'Cream', 8, FRONT, .008)
    group(col, kit, 'Kart_Pose', made_since(col, before), (0, 0, 0), (0, 0, -24))


def prism(kit, col, name, pts, z, height, material):
    """Horizontal slab from an XY outline between z and z+height."""
    n = len(pts)
    verts = [(x, y, z) for x, y in pts] + [(x, y, z+height) for x, y in pts]
    faces = [tuple(range(n)), tuple(range(n, 2*n))] + [(i, (i+1) % n, (i+1) % n+n, i+n) for i in range(n)]
    return kit.mesh(col, name, verts, faces, material)


def stick_speed(col, kit):
    cyl(kit, col, 'Stick', (0, 0, -.1), .09, 1.2, 'Cream', 8, bevel=.018)
    for i, z in enumerate((-.6, -.45, -.3)):
        cyl(kit, col, 'Stick_Stripe_%d' % (i+1), (0, 0, z), .095, .07, 'Navy', 8, bevel=.01)
    rng = random.Random(spec.SEED + 11)
    for i, (dx, dz, r) in enumerate(((0, .5, .21), (-.16, .42, .16), (.16, .43, .16), (.03, .68, .15),
                                     (-.1, .6, .13), (.12, .62, .13))):
        jitter(kit.ico(col, 'Floss_Wisp_%d' % i, (dx, -.03*(i % 2), dz), (r, r*.9, r), 'White', 2), rng)
    # A circular arrow around the stick, tilted towards the camera so it reads as a ring.
    before = set(col.objects)
    radius, width, height, end = .5, .15, .11, 610
    arc(kit, col, 'Spin_Arrow', (0, 0, 0), radius, 300, end, width, height, 'Soda', 16, True, .3)
    a = math.radians(end)
    tangent = (-math.sin(a), math.cos(a))
    normal = (math.cos(a), math.sin(a))
    base = (radius*normal[0], radius*normal[1])
    head_pts = [(base[0]+normal[0]*.18, base[1]+normal[1]*.18),
                (base[0]+tangent[0]*.26, base[1]+tangent[1]*.26),
                (base[0]-normal[0]*.18, base[1]-normal[1]*.18)]
    prism(kit, col, 'Spin_Arrow_Head', head_pts, -height/2, height, 'Soda')
    group(col, kit, 'Arrow_Pose', made_since(col, before), (0, 0, .02), (24, 0, 0))


def spoon(col, kit):
    """Measuring spoon seen from the side: deep round bowl heaped with sugar, long handle."""
    before = set(col.objects)
    kit.loft(col, 'Bowl', [(-.33, 0, 0, .1, .1), (-.28, 0, 0, .28, .28), (-.16, 0, 0, .39, .39),
                           (0, 0, 0, .43, .43)], 'Soda', 12)
    kit.loft(col, 'Sugar_Heap', [(-.01, 0, 0, .41, .41), (.1, 0, 0, .34, .34), (.22, 0, 0, .22, .22),
                                 (.3, 0, 0, .07, .07)], 'White', 12)
    rise = math.radians(-14)  # the handle climbs to the right
    handle = kit.box(col, 'Handle', (.72, 0, .09), (.7, .2, .08), 'Soda', .02)
    label = kit.box(col, 'Measure_Label', (.66, 0, .11), (.24, .13, .04), 'Navy', .006)
    end = cyl(kit, col, 'Handle_End', (1.08, 0, .18), .13, .08, 'Soda', 10, (0, rise, 0), .015)
    hole = cyl(kit, col, 'Handle_Hole', (1.09, 0, .18), .05, .09, 'Navy', 8, (0, rise, 0), 0)
    for ob in (handle, label, end, hole):
        ob.rotation_euler.y = rise
    group(col, kit, 'Spoon_Pose', made_since(col, before), (-.3, 0, -.2), (2, -36, -8))  # side view of the cup


def ribbon_candy(col, kit):
    """A decorated candy: plain White/Cream floss tied with a big Navy bow and a Gold knot."""
    cotton(col, kit, 'White', 'Strawberry', 13, lobes=('White', 'Cream'))
    for side in (-1, 1):
        s = side*1.3
        loop = [(0, .06), (s*.2, .24), (s*.42, .26), (s*.5, .1), (s*.42, -.1), (s*.2, -.14), (0, -.06)]
        puff(kit, col, 'Bow_Loop_%d' % (side+1), loop, .12, 'Navy', .06, (0, -.26, -.42))
        tail = [(s*.02, -.02), (s*.2, -.34), (s*.28, -.28), (s*.1, 0)]
        puff(kit, col, 'Bow_Tail_%d' % (side+1), tail, .07, 'Navy', .02, (0, -.24, -.46))
    kit.box(col, 'Bow_Knot', (0, -.32, -.42), (.18, .12, .19), 'Gold', .03)
    star(kit, col, 'Sparkle', (.52, -.1, .74), .15, 'Gold', .06)


def sugar_bag(col, kit, stars):
    body = kit.box(col, 'Pouch', (0, 0, -.1), (.84, .46, .86), 'Wood', .05)
    for v in body.data.vertices:
        if v.co.z > 0:
            v.co.x *= .9
    kit.box(col, 'Folded_Top', (0, 0, .38), (.8, .38, .16), 'Cream', .03)
    kit.box(col, 'Top_Crimp', (0, 0, .5), (.72, .16, .08), 'Wood', .02)
    puff(kit, col, 'Label', rounded_rect(.7, .44, .07), .04, 'Navy', .015, (0, -.24, -.12), inset=.9)
    radius = .16 if stars == 2 else .14
    xs = (-.17, .17) if stars == 2 else (-.24, 0, .24)
    for i, x in enumerate(xs):
        lift = .04 if stars == 3 and i == 1 else 0  # the middle of three stars sits a little higher
        star(kit, col, 'Grade_Star_%d' % (i+1), (x, -.29, -.12+lift), radius, 'Gold', .07)


def machine(col, kit):
    """Cabinet with a control panel under a wide spinning tub (the create_assets.py Spinner
    structure: base, bowl, spinner), with a candy being wound out of the floss."""
    before = set(col.objects)
    kit.box(col, 'Cabinet', (0, 0, -.36), (.74, .56, .42), 'Cream', .04)
    kit.box(col, 'Control_Panel', (0, -.285, -.36), (.54, .03, .26), 'Navy', .012)
    cyl(kit, col, 'Dial', (-.12, -.31, -.36), .075, .04, 'Gold', 10, FRONT, .008)
    kit.box(col, 'Switch', (.14, -.31, -.36), (.12, .05, .08), 'Strawberry', .01)
    for x in (-.3, .3):
        kit.box(col, 'Foot_%d' % (x > 0), (x, 0, -.6), (.12, .5, .08), 'Navy', .015)
    kit.box(col, 'Neck', (0, 0, -.12), (.26, .26, .1), 'Navy', .02)
    kit.loft(col, 'Tub', [(-.12, 0, 0, .32, .32), (.04, 0, 0, .5, .5), (.22, 0, 0, .6, .6),
                          (.26, 0, 0, .62, .62)], 'Strawberry', 12)
    ring(kit, col, 'Tub_Rim', (0, 0, .26), .61, .05, 'Cream', 16, 4, (0, 0, 0))
    rng = random.Random(spec.SEED + 16)
    for i in range(9):  # the floss web that collects against the tub wall
        a = math.tau*i/9+.2
        r = rng.uniform(.15, .19)
        jitter(kit.ico(col, 'Floss_Web_%d' % i, (math.cos(a)*.36, math.sin(a)*.36, .3), (r*1.2, r, r),
                       'White', 2), rng)
    # A candy being wound on its stick rises out of the tub.
    kit.segment(col, 'Candy_Stick', (.02, 0, .2), (.2, -.04, .78), .05, .05, 'Cream', .01)
    for i, (x, z, r) in enumerate(((.2, .76, .2), (.06, .7, .15), (.33, .7, .15), (.2, .9, .15), (.12, .86, .12),
                                   (.3, .88, .12))):
        jitter(kit.ico(col, 'Candy_Floss_%d' % i, (x, -.05-.04*(i % 2), z), (r, r*.9, r), 'Strawberry', 2), rng)
    group(col, kit, 'Machine_Pose', made_since(col, before), (0, 0, 0), (12, 0, 0))


def flavor_candy(flavor, seed):
    def build_candy(col, kit):
        before = set(col.objects)
        cotton(col, kit, flavor, flavor, seed)
        group(col, kit, 'Candy_Pose', made_since(col, before), (0, 0, 0), (0, 10, 0))
    return build_candy


def worker(col, kit):
    before = set(col.objects)
    bust(col, kit, 'Worker', (0, 0, .2), .64, 'Skin1', 'Hair2', 'Cream', cap='Strawberry', shoulders=.8)
    bib = [(-.19, -.5), (.19, -.5), (.19, -.2), (-.19, -.2)]
    puff(kit, col, 'Apron_Bib', bib, .04, 'Mint', .012, (0, -.275, 0), inset=.85)
    kit.box(col, 'Apron_Waist_Tie', (0, -.24, -.4), (.66, .06, .07), 'Mint', .012)
    for side in (-1, 1):
        kit.band(col, 'Apron_Strap_%d' % (side+1), (side*.16, -.262, -.21), (side*.22, -.24, -.1), .06, 'Mint', .03)
    kit.box(col, 'Apron_Pocket', (0, -.3, -.4), (.2, .03, .09), 'Strawberry', .01)
    group(col, kit, 'Worker_Pose', made_since(col, before), (0, 0, 0), (0, 0, 22))  # 3/4 view shows the visor


def grad_cap(col, kit):
    before = set(col.objects)
    kit.loft(col, 'Skull_Cap', [(-.3, 0, 0, .36, .33), (-.02, 0, 0, .38, .35)], 'Navy', 8)
    board = kit.box(col, 'Board', (0, 0, .025), (.9, .9, .07), 'Navy', .02)
    trim = kit.box(col, 'Board_Gold_Trim', (0, 0, .0), (1.0, 1.0, .05), 'Gold', .015)  # outlines it on ink
    board.rotation_euler.z = trim.rotation_euler.z = math.pi/4
    cyl(kit, col, 'Button', (0, 0, .07), .06, .04, 'Gold', 8, bevel=.01)
    kit.segment(col, 'Tassel_Cord', (0, 0, .08), (.64, 0, .07), .03, .03, 'Gold', .005)
    kit.segment(col, 'Tassel_Drop', (.66, 0, .06), (.66, -.02, -.14), .03, .03, 'Gold', .005)
    kit.loft(col, 'Tassel', [(-.38, .66, -.02, .07, .07), (-.16, .66, -.02, .04, .04)], 'Gold', 6)
    group(col, kit, 'Cap_Pose', made_since(col, before), (0, 0, .1), (22, 0, 0))
    # A rolled Cream diploma keeps a light silhouette on the ink background.
    scroll = (0, math.radians(72), 0)
    cyl(kit, col, 'Diploma', (-.02, -.34, -.34), .1, .9, 'Cream', 10, scroll, .015)
    cyl(kit, col, 'Diploma_Ribbon', (-.02, -.34, -.34), .105, .08, 'Strawberry', 10, scroll, .01)


def glove(col, kit):
    kit.box(col, 'Back_Of_Hand', (0, 0, 0), (.52, .22, .5), 'Wood', .05)
    for i, (x, length) in enumerate(((-.19, .34), (-.065, .42), (.065, .40), (.19, .32))):
        finger = kit.box(col, 'Finger_%d' % (i+1), (x*1.08, 0, .24+length/2), (.115, .2, length), 'Wood', .04)
        finger.rotation_euler.y = math.radians(x*28)
    kit.segment(col, 'Thumb', (.2, -.02, -.1), (.44, -.02, .16), .16, .19, 'Wood', .04)
    kit.box(col, 'Cuff', (0, 0, -.36), (.6, .27, .22), 'Cream', .04)
    kit.box(col, 'Cuff_Band', (0, 0, -.26), (.62, .28, .05), 'Navy', .01)


def map_pin(col, kit):
    before = set(col.objects)
    for i, (x, z, tilt, material) in enumerate(((-.42, 0, 8, 'Cream'), (0, .03, 0, 'Vanilla'),
                                                (.42, 0, -8, 'Cream'))):  # a tri-fold zigzag
        panel = kit.box(col, 'Map_Panel_%d' % i, (x, 0, z), (.43, .72, .03), material, .01)
        panel.rotation_euler.y = math.radians(tilt)
    for i, (x, y) in enumerate(((-.5, .2), (-.34, .08), (-.18, -.02))):
        kit.box(col, 'Route_Dash_%d' % i, (x, y, .03), (.1, .05, .03), 'Navy', .006)
    group(col, kit, 'Map_Pose', made_since(col, before), (0, 0, -.42), (58, 0, 0))  # top face to camera
    kit.ico(col, 'Pin_Head', (0, 0, .34), (.3, .28, .3), 'Strawberry', 2)
    kit.loft(col, 'Pin_Point', [(-.42, 0, 0, .02, .02), (.08, 0, 0, .17, .16), (.28, 0, 0, .27, .25)], 'Strawberry', 8)
    cyl(kit, col, 'Pin_Dot', (0, -.29, .36), .11, .04, 'White', 10, FRONT, .01)


def customer_group(col, kit):
    bust(col, kit, 'Left', (-.4, .34, .36), .5, 'Skin2', 'Hair2', 'Strawberry')
    bust(col, kit, 'Right', (.4, .34, .38), .5, 'Skin3', 'Hair3', 'Pants2')
    bust(col, kit, 'Front', (0, -.06, .12), .6, 'Skin1', 'Hair1', 'Soda')


BUILDERS = {
    'Trait_Hours': clock, 'Trait_Patience': hourglass, 'Trait_Ads': megaphone,
    'Trait_RepeatAds': heart_bubble, 'Trait_Shelf': shelf, 'Trait_Sales': signboard,
    'Trait_PriceTag': price_tag, 'Trait_Engine': engine, 'Trait_Handling': steering_wheel,
    'Trait_Kart': kart, 'Trait_StickSpeed': stick_speed, 'Trait_Spoon': spoon,
    'Trait_Ribbon': ribbon_candy, 'Trait_Sugar2': lambda col, kit: sugar_bag(col, kit, 2),
    'Trait_Sugar3': lambda col, kit: sugar_bag(col, kit, 3), 'Trait_Machine': machine,
    'Trait_FlavorSoda': flavor_candy('Soda', 17), 'Trait_FlavorVanilla': flavor_candy('Vanilla', 18),
    'Trait_Worker': worker, 'Trait_GradCap': grad_cap, 'Trait_Glove': glove,
    'Trait_MapPin': map_pin, 'Trait_Group': customer_group,
}


def build(asset, col, kit):
    BUILDERS[asset['id']](col, kit)
    group(col, kit, 'Pose', [ob for ob in col.objects if ob.parent is None], AIM)

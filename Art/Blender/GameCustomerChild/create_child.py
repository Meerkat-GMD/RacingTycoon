"""Child street customer (Customer_V1): Strawberry hoodie, short Hair2 wedge-lock cut, Mint coin purse.

Background Blender (from the project root):
    blender -b --factory-startup --python-exit-code 1 -P Art/Blender/GameCustomerChild/create_child.py
CH_ACTION='build' (default) builds GameCustomerChild.blend and manifest.json; 'render'
re-renders the current scene (keeping in-memory edits) through render_customer_sprites;
'all' does both. The catalog sprites come from
    blender ... -P Art/Blender/render_customer_sprites.py -- --only V1 --large
Coordinate system: Z up, -Y front, the approved male's authored units and .5 model
scale, camera and shadow catcher, so the feet share his baseline and the child reads shorter.
"""
import importlib
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh

HERE = Path(__file__).resolve().parent
COMMON = HERE.parent
for folder in (COMMON, HERE):
    if str(folder) not in sys.path:
        sys.path.insert(0, str(folder))
import toy_kit
import ui_sprite_common as c
import ui_sprite_spec as spec
importlib.reload(toy_kit)
importlib.reload(c)

SEED = spec.SEED
SCENE = 'Game_Customer_Child'
MODEL_SCALE = .5
THREADS = 8   # every customer sprite renders with the approved customers' fixed 8 threads
CAMERA = ((0, -.015, 1.22), 2.85)   # the approved male's camera call
k = toy_kit.Kit('CH_')


def hood(col):
    """Lowered hood: a thick rolled horseshoe around the back of the neck with Cream drawstrings.

    No hood ears: on a lowered hood they lie behind the big head in the 3/4 view, and the
    only visible spots (the roll beside the jaw) made them read as a ball and an earring.
    """
    cy, rx, ry, sections = .07, .47, .33, 11
    verts = []
    for i in range(sections):
        theta = math.radians(-140+280*i/(sections-1))   # 0 = centre back (+Y)
        sx, sy = math.sin(theta), math.cos(theta)
        top = 2.37+.19*(sy+1)/2
        for r, z in ((.78, 2.10), (1.20, 2.10), (1.20, top), (.82, top-.05)):
            verts.append((rx*r*sx, cy+ry*r*sy, z))
    faces = []
    for i in range(sections-1):
        a, b = 4*i, 4*(i+1)
        faces += [(a+j, b+j, b+(j+1) % 4, a+(j+1) % 4) for j in range(4)]
    last = 4*(sections-1)
    faces += [(3, 2, 1, 0), (last, last+1, last+2, last+3)]
    roll = k.mesh(col, 'Lowered_Hood_Roll', verts, faces, 'Strawberry')
    # A Cream lining on the inner wall outlines the hood opening behind the neck.
    roll.data.materials.append(k.mat('Cream'))
    for i, polygon in enumerate(roll.data.polygons[:4*(sections-1)]):
        polygon.material_index = 1 if i % 4 == 3 else 0
    # Cream drawstrings hang from the hood opening, each ending in a small aglet.
    for side, s in (('Left', -1), ('Right', 1)):
        k.band(col, side+'_Cream_Drawstring', (s*.09, -.335, 2.19), (s*.11, -.36, 1.87), .038, 'Cream', .02)
        k.ico(col, side+'_Drawstring_Aglet', (s*.111, -.366, 1.845), (.036, .028, .048), 'Cream', 1)


def hoodie(col):
    k.loft(col, 'Strawberry_Hoodie_Torso', [(1.22, 0, .03, .47, .30), (1.37, 0, .03, .48, .31),
                                            (1.95, 0, .03, .53, .315), (2.22, 0, .03, .42, .27)], 'Strawberry')
    k.loft(col, 'Hoodie_Hem_Rib', [(1.17, 0, .03, .485, .315), (1.29, 0, .03, .49, .32)], 'Strawberry')
    # Kangaroo pocket: one broad slab with slanted hand openings.
    k.panel(col, 'Kangaroo_Pocket', [(-.22, -.338, 1.74), (.22, -.338, 1.74), (.32, -.350, 1.53),
                                      (.30, -.352, 1.36), (-.30, -.352, 1.36), (-.32, -.350, 1.53)],
            'Strawberry', .035)
    for side, s in (('Left', -1), ('Right', 1)):
        k.band(col, side+'_Pocket_Opening', (s*.225, -.392, 1.73), (s*.32, -.392, 1.54), .026, 'Strawberry', .012)
    hood(col)


def arms(col):
    for side, s in (('Left', -1), ('Right', 1)):
        k.loft(col, side+'_Upper_Sleeve', [(1.67, s*.64, .02, .155, .175), (1.87, s*.625, .025, .175, .195),
                                           (2.09, s*.56, .03, .185, .205), (2.23, s*.44, .03, .135, .175)],
               'Strawberry')
        k.segment(col, side+'_Lower_Sleeve', (s*.64, .02, 1.73), (s*.675, -.04, 1.42), .26, .30, 'Strawberry', .03)
        k.segment(col, side+'_Sleeve_Cuff', (s*.675, -.04, 1.445), (s*.685, -.055, 1.33), .245, .285,
                  'Strawberry', .022)
        k.ico(col, side+'_Hand', (s*.69, -.07, 1.235), (.13, .13, .155), 'Skin2', 1)
        k.ico(col, side+'_Thumb', (s*.60, -.16, 1.25), (.055, .06, .08), 'Skin2', 1)


def shorts_shoes(col):
    k.box(col, 'Shorts_Waist', (0, .03, 1.20), (.84, .54, .20), 'Pants2', .04)
    for side, s in (('Left', -1), ('Right', 1)):
        x = s*.215
        k.loft(col, side+'_Shorts_Leg', [(.90, x*1.05, .03, .20, .225), (1.00, x*1.03, .03, .205, .23),
                                          (1.24, x*.92, .03, .235, .25)], 'Pants2')
        k.loft(col, side+'_Shorts_Cuff', [(.885, x*1.05, .03, .208, .233), (.955, x*1.05, .03, .21, .235)], 'Pants2')
        k.loft(col, side+'_Bare_Leg', [(.56, x, .0, .112, .122), (.75, x, .005, .118, .128),
                                        (.93, x, .015, .125, .135)], 'Skin2')
        k.loft(col, side+'_Cream_Sock', [(.27, x, -.005, .132, .142), (.60, x, -.005, .128, .138)], 'Cream')
        for i, (z0, z1) in enumerate(((.37, .43), (.48, .54))):
            k.loft(col, side+'_Sock_Stripe_%d' % i, [(z0, x, -.005, .138, .148), (z1, x, -.005, .136, .146)],
                   'Strawberry')
        shoe = k.box(col, side+'_Navy_Sneaker', (s*.225, -.10, .19), (.35, .58, .24), 'Navy', .05)
        sole = k.box(col, side+'_Cream_Sole', (s*.225, -.10, .065), (.37, .60, .13), 'Cream', .03)
        heel = k.box(col, side+'_Strawberry_Heel_Tab', (s*.218, .185, .25), (.24, .10, .10), 'Strawberry', .02)
        for ob in (shoe, sole, heel):
            ob.rotation_euler.z = s*.10
        for i, y in enumerate((-.13, -.23)):
            lace = k.box(col, side+'_Lace_%d' % i, (s*.225, y, .312), (.21, .04, .022), 'Cream', .006)
            lace.rotation_euler.z = s*.10


def purse(col):
    # Cream strap from the right shoulder across the chest to a Mint kiss-lock purse at the left hip.
    k.band(col, 'Cream_Purse_Strap', (.33, -.383, 2.20), (-.40, -.392, 1.47), .075, 'Cream', .03)
    k.box(col, 'Mint_Coin_Purse', (-.42, -.40, 1.33), (.30, .13, .22), 'Mint', .05)
    k.band(col, 'Purse_Gold_Frame', (-.565, -.468, 1.435), (-.275, -.468, 1.435), .04, 'Gold', .03)
    for i, x in enumerate((-.445, -.395)):
        k.ico(col, 'Purse_Gold_Clasp_%d' % i, (x, -.48, 1.478), (.03, .03, .032), 'Gold', 1)


def build():
    startup = list(bpy.data.scenes) if bpy.app.background and not bpy.data.filepath else []
    old = bpy.data.scenes.get(SCENE)
    if old:
        if len(bpy.data.scenes) == 1:
            bpy.data.scenes.new('CH_Temporary')
        bpy.data.scenes.remove(old)
    for blocks in (bpy.data.collections, bpy.data.objects, bpy.data.meshes,
                   bpy.data.cameras, bpy.data.lights, bpy.data.worlds):
        for data in list(blocks):
            if data.name.startswith('CH_') and data.users == 0:
                blocks.remove(data)
    rig, world = c.studio()
    rig.name = 'CH_Shared_Studio'
    world.name = 'CH_Shared_Environment'
    for ob in rig.objects:
        ob.name = 'CH_'+ob.name.removeprefix('UI_')
        ob.data.name = ob.name+'_Data'
    scene = c.setup_scene(SCENE, rig, world, (164, 280), SEED)
    scene.render.threads = THREADS
    cols = {name: k.collection(scene, 'CH_'+name)
            for name in ('Head_Hair', 'Hoodie', 'Arms_Hands', 'Shorts_Shoes', 'Purse')}
    hoodie(cols['Hoodie'])
    arms(cols['Arms_Hands'])
    shorts_shoes(cols['Shorts_Shoes'])
    purse(cols['Purse'])
    import child_head
    importlib.reload(child_head)
    child_head.build_head(cols['Head_Hair'])
    # Angry brows/frown for the Customer_V1_Angry sprite; hidden so the neutral render has none.
    for ob in child_head.build_angry_face(cols['Head_Hair']):
        ob.hide_render = True
        ob.hide_viewport = True
    for col in cols.values():
        for ob in col.objects:
            ob.location *= MODEL_SCALE
            ob.scale *= MODEL_SCALE
            bm = bmesh.new()
            bm.from_mesh(ob.data)
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
            bm.to_mesh(ob.data)
            bm.free()
    cam = c.camera(scene, CAMERA[0], CAMERA[1], yaw=-20, elevation=15)
    scene.camera.name = 'CH_Orthographic_Camera'
    scene.camera.data.name = 'CH_Camera_Data'
    ground = c.catcher(scene)
    ground.name = 'CH_Shadow_Catcher'
    ground.data.name = 'CH_Shadow_Catcher_Mesh'
    ground.users_collection[0].name = 'CH_Ground'
    scene['display_size'] = [82, 140]
    scene['seed'] = SEED
    scene['variant'] = 1
    scene['style'] = 'Child street customer: Strawberry hoodie, short Hair2 wedge-lock cut with fringe, Mint coin purse'
    scene.view_layers[0].update()
    for old in startup:
        if old != scene and old.name in bpy.data.scenes:
            bpy.data.scenes.remove(old)
    temporary = bpy.data.scenes.get('CH_Temporary')
    if temporary:
        bpy.data.scenes.remove(temporary)
    if startup:
        # Drop the factory-startup cube, camera, light, material, palette and image slots
        # so the saved .blend holds only the child's scene.
        for blocks in (bpy.data.objects, bpy.data.meshes, bpy.data.cameras, bpy.data.lights,
                       bpy.data.materials, bpy.data.palettes, bpy.data.images):
            for data in list(blocks):
                if not data.name.startswith(('CH_', 'UI_')):
                    blocks.remove(data)
    texts = set()
    for path in (Path(__file__), HERE/'child_head.py', COMMON/'toy_kit.py', COMMON/'ui_sprite_common.py'):
        name = 'GameCustomerChild/'+path.name
        old = bpy.data.texts.get(name)
        if old:
            bpy.data.texts.remove(old)
        text = bpy.data.texts.new(name)
        text.write(path.read_text(encoding='utf-8-sig'))
        texts.add(text)
    scene.render.filepath = str(COMMON.parents[1]/'Assets/CottonCircuit/Sprites/Customers/Customer_V1_Neutral.png')
    blend = HERE/'GameCustomerChild.blend'
    bpy.context.preferences.filepaths.save_version = 0
    if len(bpy.data.scenes) == 1:
        bpy.ops.wm.save_as_mainfile(filepath=str(blend), compress=True, copy=True)
    else:
        bpy.data.libraries.write(str(blend), {scene} | texts, fake_user=True, compress=True)
    meshes = [o for col in cols.values() for o in col.objects if o.type == 'MESH']
    used = sorted({m['palette_name'] for o in meshes for m in o.data.materials})
    manifest = {
        'scope': 'Customer_V1 child street customer for the Unity customer sprites (Variant % 3 == 1)',
        'identity': ('Strawberry hoodie with a lowered hood and Cream drawstrings; '
                     'short Hair2 haircut of chunky wedge locks with a fringe swept toward +x; '
                     'Skin2 face with a lit forehead facet and Hair2 neck shadow; '
                     'Pants2 shorts; Cream/Strawberry striped socks; Navy sneakers with Cream soles; '
                     'small Mint coin purse on a Cream strap across the body'),
        'style_reference': ['../GameCustomerFaceted/', '../GameCustomerFemaleExplorer/'],
        'proportion': ('About 3 heads: head scaled %.2f about the chin (chin at z %.2f authored) on a short body; '
                       'top of hair about 75%% of the approved male, feet on his baseline' % (
                           child_head.HEAD_SCALE, child_head.HEAD_TARGET[2])),
        'face': {'eyes': 'approved male dot eyes: Navy box .075 x .020 x .106 at x = +-.205, bevel .006, head scale 1',
                 'head_scale': child_head.HEAD_SCALE, 'blush': 'Strawberry', 'mouth': 'CH_Face_Smile (Navy)',
                 'forehead_tilt_deg': child_head.FOREHEAD_TILT,
                 'angry': ('CH_Face_Angry_Brow_L/R (Navy blocks %.2f x %.2f, 25 deg inner end down, front turned '
                           '%d deg down) and CH_Face_Angry_Frown, hidden by default' % (
                               child_head.BROW_LENGTH, child_head.BROW_THICKNESS, child_head.BROW_LEAN))},
        'model_scale': MODEL_SCALE,
        'palette': {name: spec.PALETTE[name] for name in used},
        'material_recipe': dict(spec.MATERIAL, shader='Principled BSDF'),
        'lighting': c.LIGHTING, 'camera': cam, 'seed': SEED,
        'render': {'engine': 'CYCLES', 'samples': 64, 'denoise': True, 'adaptive_sampling': False,
                   'view_transform': 'Standard', 'look': 'None', 'exposure': c.EXPOSURE, 'gamma': 1,
                   'film_transparent': True, 'color_mode': 'RGBA', 'threads': THREADS},
        'display_size': [82, 140],
        'outputs': [{'file': 'Assets/CottonCircuit/Sprites/Customers/Customer_V1_%s.png' % x, 'size': [164, 280]}
                    for x in ('Neutral', 'Angry')]
                   + [{'file': 'Art/Blender/GameCustomerChild/review/Customer_V1_%s-large.png' % x, 'size': [656, 1120]}
                      for x in ('Neutral', 'Angry')]
                   + [{'file': 'Art/Blender/GameCustomerChild/Customer_V1-large.png', 'size': [656, 1120]}],
        'renderer': 'Art/Blender/render_customer_sprites.py -- --only V1 --large',
        'shadow': 'Real Cycles catcher, navy tint, alpha <=0.32, soft elliptical contact falloff (DEFAULT_SHADOW)',
        'creation_transport': 'blender -b',
        'collections': {name: sorted(o.name for o in col.objects) for name, col in cols.items()},
    }
    (HERE/'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print('CHILD_CUSTOMER_BUILT', len(scene.objects), blend)


def render():
    import render_customer_sprites as rc
    importlib.reload(rc)
    for sid, entry in rc.render_customer(1, rc.CUSTOMERS[1], large=True).items():
        print('CHILD_CUSTOMER_RENDERED', sid, entry['file'])


if __name__ == '__main__':
    action = globals().get('CH_ACTION', 'build')
    if action in ('build', 'all'):
        build()
    if action in ('render', 'all'):
        render()

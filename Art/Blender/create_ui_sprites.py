"""Build/rebuild the three approved sample sprites. Also callable through MCP.

blender -b --factory-startup --python-exit-code 1 -P Art/Blender/create_ui_sprites.py
MCP: runpy.run_path(path, init_globals={'UI_ACTION':'build'}, run_name='__main__')
UI_ACTION may be build, render, or all (default). Render preserves model edits.
This creates independent UI_* scenes and never deletes other open scenes.
"""
import json
import math
import random
import sys
import importlib
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_common as c
importlib.reload(c)

SEED = 260927
BLEND = ART / 'UiSprites.blend'
MANIFEST = ART / 'ui-sprites-manifest.json'
ASSETS = [
    {'id': 'CottonCandy_Strawberry_Medium', 'folder': 'Items', 'display_size': [77, 88],
     'shadow': True, 'scene': 'UI_CottonCandy', 'collection': 'UI_CottonCandy_Medium'},
    {'id': 'Customer_01_Neutral', 'folder': 'Customers', 'display_size': [82, 140],
     'shadow': True, 'scene': 'UI_Customer', 'collection': 'UI_Customer_01'},
    {'id': 'Trait_Hours', 'folder': 'Icons', 'display_size': [42, 42],
     'shadow': False, 'scene': 'UI_Clock', 'collection': 'UI_Trait_Hours'},
]


def cotton(col):
    rng = random.Random(SEED)
    c.ico(col, 'Floss_Core', (0, 0, 1.68), (.58, .48, .66), 'Strawberry')
    # Twelve silhouette lobes and eight front/back lobes, all attached to the core.
    positions = []
    for i in range(12):
        a = math.tau*i/12
        positions.append((math.sin(a)*.48, rng.uniform(-.09, .09), 1.68+math.cos(a)*.53))
    for i in range(8):
        a = math.tau*(i % 4)/4 + .45
        positions.append((math.sin(a)*.31, -.34 if i < 4 else .33, 1.68+math.cos(a)*.37))
    for i, pos in enumerate(positions):
        r = rng.uniform(.245, .305)
        puff = c.ico(col, 'Floss_Lobe_%02d' % (i+1), pos,
                     (r*rng.uniform(.94, 1.13), r*.90, r*rng.uniform(.96, 1.13)), 'Strawberry')
        puff.rotation_euler = [rng.uniform(-.30, .30) for _ in range(3)]
        for v in puff.data.vertices:
            v.co *= rng.uniform(.97, 1.03)
    c.cylinder(col, 'Cream_Paper_Stick', (0, 0, .64), .039, 1.27, 'Cream', 10)
    # Thick enough to survive at77×88; material stripes are physical rings.
    for i, z in enumerate((.19, .40, .61, .82)):
        c.cylinder(col, 'Strawberry_Stick_Stripe_%02d' % (i+1), (0, 0, z), .040, .068, 'Strawberry', 10)


def customer(col):
    # Head including hair spans1.50..2.50; body0..1.50 =>2.5 heads.
    for side, x in (('Left', -.185), ('Right', .185)):
        c.box(col, side+'_Shoe_Sole', (x, -.10, .085), (.29, .43, .16), 'Navy', .05)
        c.box(col, side+'_Shoe_Cream_Upper', (x, -.12, .158), (.255, .36, .12), 'Cream', .04)
        c.box(col, side+'_Trouser_Leg', (x, .015, .43), (.23, .28, .52), 'Pants1', .025)
    shirt = c.box(col, 'Soda_Shirt', (0, .01, 1.045), (.70, .45, .83), 'Soda', .06)
    # Slightly wider hem, still a single chamfered box.
    for v in shirt.data.vertices:
        if v.co.z < 0:
            v.co.x *= 1.10
    c.ico(col, 'Cream_Oval_Badge', (.04, -.229, .985), (.077, .025, .064), 'Cream')
    c.cylinder(col, 'Neck', (0, 0, 1.47), .135, .16, 'Skin1', 10)
    for side, sign in (('Left', -1), ('Right', 1)):
        c.segment(col, side+'_Sleeve', (sign*.385, .015, 1.38), (sign*.485, -.015, .89),
                  .18, .23, 'Soda', .025)
        c.ico(col, side+'_Hand', (sign*.49, -.022, .805), (.100, .11, .12), 'Skin1', 1)
    c.ico(col, 'Face_Head', (0, -.025, 1.98), (.505, .405, .48), 'Skin1')
    for side, x in (('Left', -.475), ('Right', .475)):
        c.ico(col, side+'_Ear', (x, -.015, 1.91), (.115, .12, .15), 'Skin1', 1)
    hair = c.ico(col, 'Hair_Cap', (0, .025, 2.04), (.56, .45, .46), 'Hair1')
    # Trim the ico cap with a tilted plane, preserving its original triangle faces.
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = hair
    hair.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bm = bmesh.new()
    bm.from_mesh(hair.data)
    bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
                          dist=.00001, plane_co=(0, 0, -.035),
                          plane_no=(0, .8, 1), clear_inner=True)
    bm.to_mesh(hair.data)
    bm.free()
    c.ico(col, 'Hair_Left_Side', (-.437, .008, 1.92), (.13, .27, .30), 'Hair1', 1)
    c.ico(col, 'Hair_Right_Side', (.437, .018, 1.94), (.12, .26, .28), 'Hair1', 1)
    c.ico(col, 'Hair_Swept_Fringe', (-.13, -.30, 2.24), (.35, .16, .17), 'Hair1', 1)
    # Separate raised face parts avoid textures and retain editability.
    for side, x in (('Left', -.165), ('Right', .165)):
        c.ico(col, side+'_Dot_Eye', (x, -.414, 1.965), (.044, .024, .052), 'Navy')
    for side, x in (('Left', -.285), ('Right', .285)):
        c.ico(col, side+'_Blush', (x, -.370, 1.858), (.076, .022, .040), 'Strawberry')
    c.segment(col, 'Smile_Left', (-.057, -.408, 1.821), (0, -.422, 1.803), .019, .015, 'Navy', .004)
    c.segment(col, 'Smile_Right', (0, -.422, 1.803), (.057, -.408, 1.821), .019, .015, 'Navy', .004)


def clock(col):
    # A softly bevelled faceted case, no drawn outline or external category disc.
    c.cylinder(col, 'Clock_Case', (0, .035, 1.25), .64, .18, 'Soda', 20, True)
    c.cylinder(col, 'Clock_Cream_Bezel', (0, -.065, 1.25), .575, .055, 'Cream', 20, True)
    c.cylinder(col, 'Clock_Dial', (0, -.101, 1.25), .516, .029, 'White', 20, True)
    # Four large markers survive at42×42; twelve tiny ticks are unnecessary.
    for i in range(4):
        a = math.pi*i/2
        ob = c.box(col, 'Dial_Marker_%02d' % i, (math.sin(a)*.427, -.125, 1.25+math.cos(a)*.427),
                   (.045, .022, .085), 'Gold', .008)
        ob.rotation_euler.y = a
    c.segment(col, 'Minute_Hand_12', (0, -.151, 1.25), (0, -.151, 1.61), .063, .035, 'Navy')
    c.segment(col, 'Hour_Hand_4_30', (0, -.17, 1.25), (.268, -.17, 1.125), .073, .038, 'Navy')
    c.ico(col, 'Hands_Hub', (0, -.19, 1.25), (.062, .034, .062), 'Navy', 2)


def clear_generated():
    # Only data with our exact generated scene/prefix is removed on repeated builds.
    if all(scene.name in {a['scene'] for a in ASSETS} for scene in bpy.data.scenes):
        bpy.context.window.scene = bpy.data.scenes.new('_UI_Build_Workspace')
    for scene in list(bpy.data.scenes):
        if scene.name in {a['scene'] for a in ASSETS}:
            bpy.data.scenes.remove(scene)
    for col in list(bpy.data.collections):
        if col.name.startswith('UI_') and col.users == 0:
            bpy.data.collections.remove(col)
    for obj in list(bpy.data.objects):
        if obj.name.startswith('UI_') and obj.users == 0:
            bpy.data.objects.remove(obj)
    for blocks in (bpy.data.meshes, bpy.data.cameras, bpy.data.lights, bpy.data.worlds):
        for block in list(blocks):
            if block.name.startswith('UI_') and block.users == 0:
                blocks.remove(block)


def build():
    # A fresh command-line process has no user document to preserve.
    startup_scenes = list(bpy.data.scenes) if bpy.app.background and not bpy.data.filepath else []
    clear_generated()
    rig, world = c.studio()
    entries = []
    for index, template in enumerate(ASSETS):
        entry = dict(template)
        entry['render_size'] = [v*2 for v in entry['display_size']]
        entry['file'] = 'Assets/CottonCircuit/Sprites/%s/%s.png' % (entry['folder'], entry['id'])
        scene = c.setup_scene(entry['scene'], rig, world, entry['render_size'], SEED)
        col = c.collection(scene, entry['collection'])
        (cotton, customer, clock)[index](col)
        if entry['shadow']:
            c.catcher(scene)
        if index == 0:
            entry['camera'] = c.camera(scene, (.07, 0, 1.22), 2.90)
            entry['features'] = {'seed': SEED, 'core_count': 1, 'lobe_count': 20, 'size_tier': 1,
                                 'alternate_display_slot': [80, 77], 'alternate_fit': 'contain'}
        elif index == 1:
            entry['camera'] = c.camera(scene, (.035, 0, 1.22), 3.02)
            entry['features'] = {'variant': 0, 'expression': 'neutral small smile', 'head_units': 2.5}
        else:
            # Diameter~29px within the42px UI slot, matching the previous~28px graphic.
            entry['camera'] = c.camera(scene, (0, 0, 1.25), 1.86, yaw=-6)
            entry['features'] = {'trait_id': 'hours', 'hands': ['12', '4:30'], 'category_disc': False}
        entry['objects'] = [obj.name for obj in col.objects]
        entry['materials'] = sorted({m['palette_name'] for obj in col.objects for m in obj.data.materials})
        scene['sprite_id'] = entry['id']
        scene['display_size'] = entry['display_size']
        scene['seed'] = SEED
        scene.render.filepath = str(ROOT / entry['file'])
        Path(scene.render.filepath).parent.mkdir(parents=True, exist_ok=True)
        entries.append(entry)
    manifest = {
        'scope': 'Three representative samples only; no Unity UI wiring changes.',
        'source_spec': 'docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md',
        'blender_version': bpy.app.version_string,
        'blend': 'Art/Blender/UiSprites.blend',
        'palette': c.PALETTE, 'lighting': c.LIGHTING,
        'render': {'engine': 'CYCLES', 'samples': 64, 'denoise': True, 'view_transform': 'Standard',
                   'look': 'None', 'exposure': c.EXPOSURE, 'gamma': 1, 'transparent': True,
                   'format': 'PNG', 'color_mode': 'RGBA', 'bit_depth': 8, 'seed': SEED},
        'coordinate_system': {'up': '+Z', 'front': '-Y', 'units': 'meters'},
        'comparison_backgrounds': {'light': '#FFF6E7', 'dark': '#29324D'},
        'shadow_compositing': {
            'source': 'Cycles shadow catcher combined alpha, object alpha removed',
            'treatment': 'Navy-colored shadow alpha, maximum .32, smooth elliptical contact falloff',
            'mask_radius_fraction_of_canvas': [.30, .068], 'object_pixels': 'untouched separate Cycles beauty pass',
            'raw_passes': 'Art/Blender/ui-sprite-passes; all at native2x resolution',
        },
        'assets': entries,
    }
    MANIFEST.write_text(json.dumps(manifest, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
    for path in (Path(__file__), ART/'ui_sprite_common.py'):
        old = bpy.data.texts.get(path.name)
        if old:
            bpy.data.texts.remove(old)
        bpy.data.texts.load(str(path))
    for entry in entries:
        bpy.data.scenes[entry['scene']].view_layers[0].update()
    bpy.context.window.scene = bpy.data.scenes['UI_Customer']
    temporary = bpy.data.scenes.get('_UI_Build_Workspace')
    if temporary:
        bpy.data.scenes.remove(temporary)
    for startup in startup_scenes:
        if startup.name in bpy.data.scenes and startup.name not in {a['scene'] for a in ASSETS}:
            bpy.data.scenes.remove(startup)
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == 'VIEW_3D':
                area.spaces.active.region_3d.view_perspective = 'CAMERA'
    # Save a regular document when this session contains just our samples.
    # If an unrelated scene is open, isolate the sample dependencies instead.
    scenes = {bpy.data.scenes[a['scene']] for a in ASSETS}
    if set(bpy.data.scenes) == scenes:
        bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True, copy=True)
    else:
        bpy.data.libraries.write(str(BLEND), scenes | set(bpy.data.texts.get(n) for n in ('create_ui_sprites.py', 'ui_sprite_common.py')),
                                 fake_user=True, compress=True)
    print('UI_SPRITES_BUILT', str(BLEND))
    return manifest


def render(asset_id=None):
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
    for entry in manifest['assets']:
        if asset_id and entry['id'] != asset_id:
            continue
        scene = bpy.data.scenes[entry['scene']]
        bpy.context.window.scene = scene
        output = ROOT / entry['file']
        scene.render.filepath = str(output)
        if entry['shadow']:
            render_contact_sprite(scene, entry, output)
        else:
            bpy.ops.render.render(write_still=True, scene=scene.name)
        print('UI_SPRITE_RENDERED', entry['id'], scene.render.filepath)
    return manifest


def render_contact_sprite(scene, entry, output):
    """Keep real catcher contact shading without its infinite-plane alpha haze.

    Blender reads and composites its own two Cycles passes in linear space.
    Only the shadow layer receives a smooth mask; beauty geometry is unchanged.
    """
    from bpy_extras.object_utils import world_to_camera_view
    raw = ART/'ui-sprite-passes'
    raw.mkdir(exist_ok=True)
    floor = next(o for o in scene.objects if o.type == 'MESH' and o.is_shadow_catcher)
    combined_path = raw/(entry['id']+'-catcher.png')
    beauty_path = raw/(entry['id']+'-beauty.png')
    try:
        floor.hide_render = False
        scene.render.filepath = str(combined_path)
        bpy.ops.render.render(write_still=True, scene=scene.name)
        floor.hide_render = True
        scene.render.filepath = str(beauty_path)
        bpy.ops.render.render(write_still=True, scene=scene.name)
    finally:
        floor.hide_render = False
        scene.render.filepath = str(output)
    combined = bpy.data.images.load(str(combined_path), check_existing=False)
    beauty = bpy.data.images.load(str(beauty_path), check_existing=False)
    from array import array
    w, h = entry['render_size']
    cp = array('f', [0])*(w*h*4)
    bp = array('f', [0])*(w*h*4)
    combined.pixels.foreach_get(cp)
    beauty.pixels.foreach_get(bp)
    pos = world_to_camera_view(scene, scene.camera, Vector((.07, .10, 0)))
    cx, cy = pos.x*w, pos.y*h
    rx, ry = .30*w, .068*h
    ink = c.linear(c.PALETTE['Navy'])
    for y in range(h):
        for x in range(w):
            i = 4*(y*w+x)
            a = bp[i+3]
            distance = math.sqrt(((x+.5-cx)/rx)**2+((y+.5-cy)/ry)**2)
            t = min(1, max(0, (distance-.25)/.75))
            falloff = 1-t*t*(3-2*t)
            shadow = max(0, (cp[i+3]-a)/max(1-a, .00001))
            shadow = min(.32, shadow)*falloff
            final_alpha = a+shadow*(1-a)
            for channel in range(3):
                bp[i+channel] = (bp[i+channel]*a+ink[channel]*shadow*(1-a))/max(final_alpha, .00001)
            bp[i+3] = final_alpha
    final = bpy.data.images.new(entry['id']+'_Composite', width=w, height=h, alpha=True)
    final.alpha_mode = 'STRAIGHT'
    final.pixels.foreach_set(bp)
    # Data are already display-transformed by the raw PNG renders; save without a second view transform.
    final.filepath_raw = str(output)
    final.file_format = 'PNG'
    final.save()
    for image in (combined, beauty, final):
        bpy.data.images.remove(image)


if __name__ == '__main__':
    action = globals().get('UI_ACTION', 'all')
    if action in ('build', 'all'):
        build()
    if action in ('render', 'all'):
        render(globals().get('UI_ASSET_ID'))

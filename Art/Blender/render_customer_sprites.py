"""Render the six street-customer sprites Customer_V{0,1,2}_{Neutral,Angry}. Blender only.

    blender -b --factory-startup --python-exit-code 1 -P Art/Blender/render_customer_sprites.py -- [--only V0,V1,V2] [--large] [--replace-angry]

Variant mapping (Unity picks Variant % 3): V0 = approved male (GameCustomerFaceted),
V1 = child (GameCustomerChild), V2 = approved female explorer (GameCustomerFemaleExplorer).

For each customer the saved .blend is opened. When it holds no object whose name
contains `Face_Angry_`, the head module's build_angry_face(col) adds the brows and
frown to the head collection (the collection holding the `Face_*Smile*` mouth), scales
them by the model scale, recalculates normals, hides them and saves the .blend.
Every other object is fingerprinted (matrix, mesh data, materials, modifiers,
visibility, lights, camera, render settings) before the change, after it and after
re-opening the saved file; any difference aborts the run. `--replace-angry` removes
existing `Face_Angry_` objects first, so a brow revision can be saved the same way.

Then two stills are rendered with ui_sprite_render.render_sprite and DEFAULT_SHADOW
at 164x280 into Assets/CottonCircuit/Sprites/Customers/ (passes in
Art/Blender/ui-sprite-passes/customers/): Neutral (angry marks hidden, smile shown)
and Angry (angry marks shown, smile hidden). A V0/V2 neutral render must equal the
approved PNG pixel for pixel; if one value differs, the approved PNG and its passes
are copied instead and `neutral_source` records why. `--large` also renders 656x1120
into <customer folder>/review/<id>-large.png (the child's neutral one is also copied
to GameCustomerChild/Customer_V1-large.png) and, once all six exist, composes
Art/Blender/previews/customers-large.png. Each scene's resolution, filepath and face
visibility are restored; the .blend is never saved after rendering. The run record
is Art/Blender/ui-sprite-passes/customers/render-manifest.json.
Renders always use the .blend's fixed 8 threads so re-renders match the approved PNGs.
"""
import argparse
import hashlib
import importlib
import json
import os
import shutil
import sys
from pathlib import Path

import bpy
import bmesh
import numpy as np

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_render as r
import ui_sprite_spec as spec

OUT = ROOT/'Assets/CottonCircuit/Sprites/Customers'
PASSES = ART/'ui-sprite-passes'/'customers'
RECORD = PASSES/'render-manifest.json'
BOARD = ART/'previews'/'customers-large.png'
SIZE, LARGE = (164, 280), (656, 1120)
THREADS = 8
ANGRY, EXPRESSIONS = 'Face_Angry_', ('Neutral', 'Angry')
CUSTOMERS = {
    0: {'folder': 'GameCustomerFaceted', 'blend': 'GameCustomerFaceted.blend', 'scene': 'Game_Customer_Faceted',
        'head': 'customer_head', 'create': 'create_customer.py', 'scale': .5, 'approved': 'Customer_01_Faceted.png'},
    1: {'folder': 'GameCustomerChild', 'blend': 'GameCustomerChild.blend', 'scene': 'Game_Customer_Child',
        'head': 'child_head', 'create': 'create_child.py', 'scale': .5, 'approved': None},
    2: {'folder': 'GameCustomerFemaleExplorer', 'blend': 'GameCustomerFemaleExplorer.blend',
        'scene': 'Game_Customer_Female_Explorer', 'head': 'explorer_head', 'create': 'create_explorer.py',
        'scale': .48, 'approved': 'Customer_02_Explorer.png'},
}


def rel(path):
    return Path(path).resolve().relative_to(ROOT).as_posix()


def sprite_id(variant, expression):
    return 'Customer_V%d_%s' % (variant, expression)


def face_parts(scene):
    angry = sorted((o for o in scene.objects if ANGRY in o.name), key=lambda o: o.name)
    smiles = [o for o in scene.objects if 'Face_' in o.name and 'Smile' in o.name and ANGRY not in o.name]
    return angry, smiles


def material_record(m):
    record = {'name': m.name, 'diffuse': list(m.diffuse_color), 'palette': m.get('palette_srgb')}
    node = m.node_tree.nodes.get('Principled BSDF') if m.node_tree else None
    if node:
        record['bsdf'] = [list(node.inputs['Base Color'].default_value), node.inputs['Roughness'].default_value,
                          node.inputs['Specular IOR Level'].default_value]
    return record


def fingerprint(scene):
    """Hash of everything that can change the render, excluding the angry face marks."""
    scene.view_layers[0].update()
    records = []
    for ob in sorted(scene.objects, key=lambda o: o.name):
        if ANGRY in ob.name:
            continue
        data = {'name': ob.name, 'type': ob.type, 'matrix': [v for row in ob.matrix_world for v in row],
                'hide': [ob.hide_render, ob.hide_viewport],
                'collections': sorted(c.name for c in ob.users_collection),
                'parent': ob.parent.name if ob.parent else None}
        if ob.type == 'MESH':
            data['vertex_count'] = len(ob.data.vertices)
            data['vertices'] = [tuple(v.co) for v in ob.data.vertices]
            data['faces'] = [(tuple(p.vertices), p.material_index, p.use_smooth) for p in ob.data.polygons]
            data['materials'] = [material_record(m) for m in ob.data.materials]
            data['modifiers'] = [(m.name, m.type, getattr(m, 'width', None), getattr(m, 'segments', None))
                                 for m in ob.modifiers]
            data['shadow_catcher'] = ob.is_shadow_catcher
        elif ob.type == 'LIGHT':
            data['light'] = [ob.data.type, ob.data.energy, list(ob.data.color), ob.data.size,
                             getattr(ob.data, 'shape', None)]
        elif ob.type == 'CAMERA':
            data['camera'] = [ob.data.type, ob.data.ortho_scale, ob.data.lens]
        records.append(data)
    rs, cy, vs = scene.render, scene.cycles, scene.view_settings
    background = scene.world.node_tree.nodes.get('Background') if scene.world and scene.world.node_tree else None
    records.append({'render': [rs.engine, rs.resolution_x, rs.resolution_y, rs.resolution_percentage,
                               rs.film_transparent, rs.threads_mode, rs.threads, cy.samples, cy.seed,
                               cy.use_denoising, cy.use_adaptive_sampling, vs.view_transform, vs.look,
                               vs.exposure, vs.gamma, scene.camera.name if scene.camera else None],
                    'world': [list(background.inputs['Color'].default_value),
                              background.inputs['Strength'].default_value] if background else None})
    digest = hashlib.sha256(json.dumps(records, sort_keys=True).encode()).hexdigest()
    return digest, len(records)-1


def open_blend(cfg):
    blend = ART/cfg['folder']/cfg['blend']
    if not blend.is_file():
        raise FileNotFoundError('%s is missing; build it first' % rel(blend))
    bpy.ops.wm.open_mainfile(filepath=str(blend), load_ui=False)
    scene = bpy.data.scenes[cfg['scene']]
    window = bpy.context.window or (bpy.context.window_manager.windows[0]
                                    if bpy.context.window_manager.windows else None)
    if window:
        window.scene = scene
    return blend, scene


def head_module(cfg):
    folder = ART/cfg['folder']
    if str(folder) not in sys.path:
        sys.path.insert(0, str(folder))
    module = importlib.import_module(cfg['head'])
    return importlib.reload(module)


def ensure_angry(variant, cfg, replace=False):
    """Add the hidden angry marks to the saved .blend when missing.

    Returns {'angry_objects': [...]} plus a 'saved' proof (fingerprint, object count) when it saved.
    """
    blend, scene = open_blend(cfg)
    angry, smiles = face_parts(scene)
    if len(smiles) != 1:
        raise RuntimeError('V%d needs exactly one Face_*Smile* mouth, found %s' % (variant, [o.name for o in smiles]))
    if angry and not replace:
        return {'angry_objects': [o.name for o in angry]}
    before, count = fingerprint(scene)
    col = smiles[0].users_collection[0]
    removed = []
    for ob in angry:
        removed.append(ob.name)
        mesh = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if mesh is not None and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    marks = head_module(cfg).build_angry_face(col)
    if not marks or any(ANGRY not in ob.name for ob in marks):
        raise RuntimeError('build_angry_face must return objects named *%s*' % ANGRY)
    for ob in marks:
        ob.location *= cfg['scale']
        ob.scale *= cfg['scale']
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(ob.data)
        bm.free()
        ob.hide_render = True
        ob.hide_viewport = True
    after, after_count = fingerprint(scene)
    if (before, count) != (after, after_count):
        raise RuntimeError('V%d: an object outside the angry marks changed; nothing saved' % variant)
    names = sorted(ob.name for ob in marks)
    # Keep the source copies embedded in the .blend in step with the modules that built it.
    for source in (cfg['create'], cfg['head']+'.py'):
        text = bpy.data.texts.get(cfg['folder']+'/'+source)
        if text is not None:
            text.clear()
            text.write((ART/cfg['folder']/source).read_text(encoding='utf-8-sig'))
    bpy.context.preferences.filepaths.save_version = 0  # leave the ignored .blend1 backups alone
    bpy.ops.wm.save_as_mainfile(filepath=str(blend), compress=True)
    blend, scene = open_blend(cfg)
    reopened, reopened_count = fingerprint(scene)
    saved = sorted(o.name for o in face_parts(scene)[0])
    if (reopened, reopened_count) != (before, count) or saved != names:
        raise RuntimeError('V%d: the saved .blend differs outside the angry marks' % variant)
    if any(not (bpy.data.objects[n].hide_render and bpy.data.objects[n].hide_viewport) for n in saved):
        raise RuntimeError('V%d: saved angry marks must be hidden' % variant)
    print('CUSTOMER_ANGRY_SAVED V%d %s objects_unchanged=%d sha256=%s' % (variant, names, count, before))
    return {'angry_objects': names, 'saved': {'blend': rel(blend), 'replaced': removed, 'objects_unchanged': count,
                                              'fingerprint_sha256': before, 'reopened_identical': True}}


def decoded(path):
    image = bpy.data.images.load(str(path), check_existing=False)
    try:
        values = np.empty(image.size[0]*image.size[1]*4, dtype=np.float32)
        image.pixels.foreach_get(values)
        return tuple(image.size), np.rint(values*255).astype(np.int16).reshape(image.size[1], image.size[0], 4)
    finally:
        bpy.data.images.remove(image)


def difference(a, b):
    size_a, pa = decoded(a)
    size_b, pb = decoded(b)
    if size_a != size_b:
        return None, None
    diff = np.abs(pa-pb)
    return int(diff.max()), int((diff.max(axis=2) > 0).sum())


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def render_customer(variant, cfg, large):
    """Render Neutral and Angry for one customer from its (already opened) saved scene."""
    scene = bpy.data.scenes[cfg['scene']]
    if (scene.render.threads_mode, scene.render.threads) != ('FIXED', THREADS):
        raise RuntimeError('V%d scene must render with FIXED %d threads, has %s %d' % (
            variant, THREADS, scene.render.threads_mode, scene.render.threads))
    angry, smiles = face_parts(scene)
    if len(smiles) != 1 or len(angry) < 3:
        raise RuntimeError('V%d needs one smile and brows+frown, found %s / %s' % (
            variant, [o.name for o in smiles], [o.name for o in angry]))
    original = (scene.render.resolution_x, scene.render.resolution_y, scene.render.filepath)
    visibility = {o.name: (o.hide_render, o.hide_viewport) for o in angry+smiles}
    folder = ART/cfg['folder']
    entries = {}
    try:
        for expression in EXPRESSIONS:
            show_angry = expression == 'Angry'
            for ob in angry:
                ob.hide_render = ob.hide_viewport = not show_angry
            for ob in smiles:
                ob.hide_render = ob.hide_viewport = show_angry
            sid = sprite_id(variant, expression)
            output = OUT/(sid+'.png')
            r.render_sprite(scene, sid, output, PASSES, r.DEFAULT_SHADOW, SIZE)
            entry = {'file': rel(output), 'variant': variant, 'expression': expression,
                     'blend': rel(folder/cfg['blend']), 'scene': scene.name, 'size': list(SIZE),
                     'threads': scene.render.threads, 'shadow': {k: list(v) if isinstance(v, tuple) else v
                                                                 for k, v in r.DEFAULT_SHADOW.items()},
                     'visible_face_marks': sorted(o.name for o in angry+smiles if not o.hide_render),
                     'hidden_face_marks': sorted(o.name for o in angry+smiles if o.hide_render),
                     'passes': [rel(PASSES/(sid+'-catcher.png')), rel(PASSES/(sid+'-beauty.png'))]}
            if expression == 'Neutral' and cfg['approved']:
                approved = folder/cfg['approved']
                max_diff, changed = difference(output, approved)
                if max_diff == 0 and changed == 0:
                    entry['neutral_source'] = 'rendered from %s with angry marks hidden; pixel-identical to %s' % (
                        rel(folder/cfg['blend']), rel(approved))
                else:
                    shutil.copyfile(approved, output)
                    for kind in ('beauty', 'catcher'):
                        shutil.copyfile(folder/'ui-sprite-passes'/('%s-%s.png' % (approved.stem, kind)),
                                        PASSES/('%s-%s.png' % (sid, kind)))
                    entry['neutral_source'] = ('copied %s and its passes: the re-render differed '
                                               '(max_abs_diff=%s, differing_pixels=%s)' % (rel(approved), max_diff, changed))
                print('CUSTOMER_NEUTRAL_CHECK V%d max_abs_diff=%s differing_pixels=%s' % (variant, max_diff, changed))
            elif expression == 'Neutral':
                entry['neutral_source'] = 'rendered from %s with angry marks hidden' % rel(folder/cfg['blend'])
            if large:
                review = folder/'review'/(sid+'-large.png')
                r.render_sprite(scene, sid+'-large', review, PASSES, r.DEFAULT_SHADOW, LARGE)
                entry['large'] = rel(review)
                if variant == 1 and expression == 'Neutral':
                    shutil.copyfile(review, folder/'Customer_V1-large.png')
                print('CUSTOMER_RENDERED %s %s' % (sid+'-large', review))
            entry['sha256'] = sha256(output)
            entries[sid] = entry
            print('CUSTOMER_RENDERED %s %s' % (sid, output))
    finally:
        for ob in angry+smiles:
            ob.hide_render, ob.hide_viewport = visibility[ob.name]
        scene.render.resolution_x, scene.render.resolution_y, scene.render.filepath = original
    return entries


def compose_board():
    """3 columns (V0, V1, V2) x 2 rows (Neutral, Angry) of the 656x1120 reviews on the cream background."""
    paths = [[ART/CUSTOMERS[v]['folder']/'review'/(sprite_id(v, x)+'-large.png') for v in (0, 1, 2)]
             for x in EXPRESSIONS]
    if not all(p.is_file() for row in paths for p in row):
        return None
    gap, (w, h) = 24, LARGE
    width, height = 3*w+4*gap, 2*h+3*gap
    cream = spec.REVIEW_BACKGROUNDS['cream'].lstrip('#')
    board = np.empty((height, width, 4), dtype=np.float64)
    board[..., :3] = [int(cream[i:i+2], 16)/255 for i in (0, 2, 4)]
    board[..., 3] = 1
    for row, line in enumerate(paths):
        for col, path in enumerate(line):
            size, pixels = decoded(path)
            if size != LARGE:
                raise ValueError('%s is %s, expected %s' % (path, size, LARGE))
            src = pixels.astype(np.float64)/255  # bottom-up rows, straight alpha
            x0 = gap+col*(w+gap)
            y0 = height-(row+1)*(h+gap)  # bottom-up: row 0 is the top row
            cell = board[y0:y0+h, x0:x0+w]
            alpha = src[..., 3:4]
            cell[..., :3] = src[..., :3]*alpha+cell[..., :3]*(1-alpha)
    BOARD.parent.mkdir(parents=True, exist_ok=True)
    image = bpy.data.images.new('Customers_Large_Board', width=width, height=height, alpha=True)
    try:
        image.alpha_mode = 'STRAIGHT'
        image.pixels.foreach_set(board.astype(np.float32).ravel())
        image.filepath_raw = str(BOARD)
        image.file_format = 'PNG'
        image.save()
    finally:
        bpy.data.images.remove(image)
    print('CUSTOMER_BOARD %s (%dx%d)' % (BOARD, width, height))
    return BOARD


def main():
    parser = argparse.ArgumentParser(prog='render_customer_sprites.py')
    parser.add_argument('--only', default='V0,V1,V2', help='comma-separated variants, e.g. V0,V2')
    parser.add_argument('--large', action='store_true', help='also render the 656x1120 review images')
    parser.add_argument('--replace-angry', action='store_true',
                        help='rebuild the angry marks in the saved .blend even when they exist')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    try:
        variants = sorted({int(v.strip().upper().removeprefix('V')) for v in args.only.split(',') if v.strip()})
    except ValueError:
        parser.error('--only takes V0, V1 and/or V2')
    if not variants or any(v not in CUSTOMERS for v in variants):
        parser.error('--only takes V0, V1 and/or V2')
    os.environ.pop('UI_SPRITE_THREADS', None)
    record = json.loads(RECORD.read_text(encoding='utf-8')) if RECORD.is_file() else {}
    record.update({'script': rel(Path(__file__)), 'blender_version': bpy.app.version_string,
                   'renderer': 'ui_sprite_render.render_sprite', 'canvas': list(SIZE), 'large': list(LARGE)})
    sprites = record.setdefault('sprites', {})
    angry_log = record.setdefault('angry_marks', {})
    for variant in variants:
        cfg = CUSTOMERS[variant]
        marks = ensure_angry(variant, cfg, args.replace_angry)
        log = angry_log.setdefault('V%d' % variant, {})
        log['angry_objects'] = marks['angry_objects']
        if 'saved' in marks:  # keep the proof of the last save when a later run finds the marks present
            log['saved'] = marks['saved']
        open_blend(cfg)  # render from the file as saved, never from an in-memory edit
        for sid, entry in render_customer(variant, cfg, args.large).items():
            previous = sprites.get(sid, {})
            if 'large' in previous and 'large' not in entry:
                entry['large'] = previous['large']
            sprites[sid] = entry
    record['sprites'] = dict(sorted(sprites.items()))
    PASSES.mkdir(parents=True, exist_ok=True)
    RECORD.write_text(json.dumps(record, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
    if args.large:
        compose_board()
    if bpy.data.is_dirty:
        print('CUSTOMER_NOTE session changed in memory only (visibility/resolution restored); nothing saved')
    print('CUSTOMER_SPRITES_DONE %s' % ','.join('V%d' % v for v in variants))


if __name__ == '__main__':
    main()

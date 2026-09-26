"""Build and render one category of UI sprites (Blender, background mode).

    blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py -- --category items
        [--only id,id]     render only these ids (every asset of the category is still built and saved)
        [--out-root DIR]   write sprites, .blend, manifest and passes under DIR instead of the project root
        [--module-path DIR] load the category module from DIR (<DIR>/ui_sprites_<category>.py or <DIR>/<category>.py)
        [--no-render]      build, save the .blend and the manifest only

Every asset of `ui_sprites/<category>.py` becomes one scene `UIS_<id>` with the shared
studio rig, its own orthographic camera and, when the asset has a shadow, the shared
shadow catcher. Output paths, all relative to the out-root (default: project root):
    Assets/CottonCircuit/Sprites/<folder>/<id>.png         (ui_sprite_spec.sprite_path)
    Art/Blender/ui_sprites/<category>.blend
    Art/Blender/ui_sprites/<category>.manifest.json
    Art/Blender/ui-sprite-passes/<category>/<id>-{catcher,beauty}.png
Set UI_SPRITE_THREADS=4 in the environment to leave CPU for other work.
"""
import argparse
import importlib
import importlib.util
import json
import sys
from pathlib import Path

import bpy
import bmesh

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_spec as spec
import ui_sprite_common as c
import ui_sprite_render as renderer
import toy_kit


def parse_args(argv):
    parser = argparse.ArgumentParser(prog='build_ui_sprites.py', description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--category', required=True)
    parser.add_argument('--only', default='', help='comma-separated sprite ids to render')
    parser.add_argument('--out-root', type=Path, default=ROOT)
    parser.add_argument('--module-path', type=Path)
    parser.add_argument('--no-render', action='store_true')
    return parser.parse_args(argv[argv.index('--')+1:] if '--' in argv else [])


def load_module(category, module_path):
    if module_path is None:
        return importlib.reload(importlib.import_module('ui_sprites.' + category))
    for name in ('ui_sprites_' + category, category):
        path = Path(module_path).resolve()/(name + '.py')
        if path.is_file():
            loader = importlib.util.spec_from_file_location(name, path)
            module = importlib.util.module_from_spec(loader)
            loader.loader.exec_module(module)
            return module
    raise FileNotFoundError('No ui_sprites_%s.py or %s.py in %s' % (category, category, module_path))


def check_asset(asset, entry, owner):
    sprite_id = asset['id']
    if entry is None:
        raise ValueError('%s is not in the sprite catalog' % sprite_id)
    if entry['owner'] != owner:
        raise ValueError('%s belongs to %s, not %s' % (sprite_id, entry['owner'], owner))
    if bool(asset['shadow']) != entry['shadow']:
        raise ValueError('%s: kind %s requires shadow=%s' % (sprite_id, entry['kind'], entry['shadow']))
    max_yaw = spec.KIND_RULES[entry['kind']].get('max_yaw')
    if max_yaw is not None and abs(asset['camera'].get('yaw', -20)) > max_yaw:
        raise ValueError('%s: |yaw| must be <= %s for %s sprites' % (sprite_id, max_yaw, entry['kind']))


def recalc_normals(col):
    for ob in col.all_objects:
        if ob.type == 'MESH':
            bm = bmesh.new()
            bm.from_mesh(ob.data)
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
            bm.to_mesh(ob.data)
            bm.free()


def render_settings(scene):
    view = scene.view_settings
    return {'engine': scene.render.engine, 'samples': scene.cycles.samples,
            'denoise': scene.cycles.use_denoising, 'adaptive_sampling': scene.cycles.use_adaptive_sampling,
            'seed': scene.cycles.seed, 'view_transform': view.view_transform, 'look': view.look,
            'exposure': round(view.exposure, 4), 'gamma': round(view.gamma, 4), 'format': 'PNG',
            'color_mode': scene.render.image_settings.color_mode,
            'bit_depth': int(scene.render.image_settings.color_depth)}


def material_record(material):
    shader = material.node_tree.nodes.get('Principled BSDF') if material.node_tree else None
    value = (lambda name: round(shader.inputs[name].default_value, 4)) if shader else (lambda name: None)
    return {'name': material.name, 'palette_name': material.get('palette_name'),
            'palette_srgb': material.get('palette_srgb'),
            'roughness': value('Roughness'), 'specular_ior_level': value('Specular IOR Level')}


def relative(path, root):
    try:
        return Path(path).resolve().relative_to(Path(root).resolve()).as_posix()
    except ValueError:
        return Path(path).resolve().as_posix()


def build(module, out_root):
    catalog = spec.by_id()
    owner = getattr(module, 'OWNER', module.CATEGORY)
    ids = [asset['id'] for asset in module.ASSETS]
    if not ids or len(ids) != len(set(ids)):
        raise ValueError('%s.ASSETS must list each sprite id once' % module.CATEGORY)
    startup = [scene for scene in bpy.data.scenes if not scene.name.startswith('UIS_')]
    rig, world = c.studio()
    rig.name, world.name = 'UIS_Shared_Studio', 'UIS_Shared_Environment'
    built = []
    for asset in module.ASSETS:
        entry = catalog.get(asset['id'])
        check_asset(asset, entry, owner)
        sprite_id = asset['id']
        scene = c.setup_scene('UIS_' + sprite_id, rig, world, entry['canvas'], spec.SEED)
        kit = toy_kit.Kit(sprite_id + '_')
        col = kit.collection(scene, 'UIS_' + sprite_id)
        module.build(asset, col, kit)
        bare = sorted(ob.name for ob in col.all_objects if ob.type == 'MESH' and not any(ob.data.materials))
        if bare:
            raise ValueError('%s: meshes without a palette material: %s' % (sprite_id, ', '.join(bare)))
        recalc_normals(col)
        camera = c.camera(scene, **asset['camera'])
        if asset['shadow']:
            c.catcher(scene)
        opaque = bool(asset.get('opaque_backdrop'))
        if opaque and not spec.KIND_RULES[entry['kind']]['opaque']:
            raise ValueError('%s: opaque_backdrop is only for opaque kinds' % sprite_id)
        scene.render.film_transparent = not opaque
        scene['sprite_id'] = sprite_id
        scene.render.filepath = str(Path(out_root)/spec.sprite_path(entry))
        scene.view_layers[0].update()
        materials = {m.name: m for ob in col.all_objects if ob.type == 'MESH' for m in ob.data.materials if m}
        built.append({'scene': scene, 'asset': asset, 'entry': entry, 'record': {
            'id': sprite_id, 'file': spec.sprite_path(entry), 'kind': entry['kind'],
            'canvas': list(entry['canvas']), 'camera': camera, 'shadow': asset['shadow'],
            'film_transparent': scene.render.film_transparent, 'params': asset.get('params', {}),
            'objects': sorted(ob.name for ob in col.all_objects),
            'materials': [material_record(m) for _, m in sorted(materials.items())]}})
    for scene in startup:
        bpy.data.scenes.remove(scene)
    window = bpy.context.window or next(iter(bpy.context.window_manager.windows), None)
    if window:
        window.scene = built[0]['scene']
    settings = [render_settings(item['scene']) for item in built]
    if any(s != settings[0] for s in settings):
        raise ValueError('Category modules must not change the shared render settings')
    return built, settings[0] if settings else None


def main():
    args = parse_args(sys.argv)
    module = load_module(args.category, args.module_path)
    if module.CATEGORY != args.category:
        raise ValueError('Module CATEGORY %r does not match --category %r' % (module.CATEGORY, args.category))
    only = [i for i in args.only.split(',') if i]
    unknown = sorted(set(only) - {asset['id'] for asset in module.ASSETS})
    if unknown:
        raise ValueError('--only ids not in %s.ASSETS: %s' % (args.category, ', '.join(unknown)))
    out_root = args.out_root.resolve()
    folder = out_root/'Art/Blender/ui_sprites'
    folder.mkdir(parents=True, exist_ok=True)
    blend = folder/(args.category + '.blend')
    built, render = build(module, out_root)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend), compress=True)
    manifest = {
        'category': args.category,
        'module': relative(module.__file__, ROOT),
        'source_spec': 'docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md',
        'blender_version': bpy.app.version_string,
        'blend': relative(blend, out_root),
        'render': render,
        'palette': spec.PALETTE,
        'lighting': spec.LIGHTING,
        'material_recipe': spec.MATERIAL,
        'coordinate_system': {'up': '+Z', 'front': '-Y', 'units': 'meters'},
        'passes': relative(out_root/'Art/Blender/ui-sprite-passes'/args.category, out_root),
        'assets': [item['record'] for item in built],
    }
    manifest_path = folder/(args.category + '.manifest.json')
    manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print('UI_SPRITES_BUILT', args.category, len(built), blend)
    if args.no_render:
        return
    passes = out_root/'Art/Blender/ui-sprite-passes'/args.category
    for item in built:
        sprite_id = item['asset']['id']
        if only and sprite_id not in only:
            continue
        output = out_root/spec.sprite_path(item['entry'])
        renderer.render_sprite(item['scene'], sprite_id, output, passes, item['asset']['shadow'],
                               tuple(item['entry']['canvas']))
        print('UI_SPRITE_RENDERED', sprite_id, output)


if __name__ == '__main__':
    main()

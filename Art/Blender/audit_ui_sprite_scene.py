"""Read-only Blender scene audit for the three approved UI sprite samples.

Run after opening Art/Blender/UiSprites.blend (including through Blender MCP):
runpy.run_path(path, run_name='__main__')
Evaluates scene view layers; does not open, save, rebuild, render, or edit data.
"""
import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


EXPECTED = {
    'UI_CottonCandy': ('CottonCandy_Strawberry_Medium', (154, 176), (-20, 15), True),
    'UI_Customer': ('Customer_01_Neutral', (164, 280), (-20, 15), True),
    'UI_Clock': ('Trait_Hours', (84, 84), (-6, 15), False),
}
PALETTE = {
    'Strawberry': '#F48DAB', 'Soda': '#7ACDCE', 'Vanilla': '#F9D27D',
    'Navy': '#29324D', 'Cream': '#FFF1D4', 'White': '#FFF9ED',
    'Mint': '#99C4AE', 'Plum': '#6C577F', 'Gold': '#DBAE61',
    'Wood': '#D59C79', 'Tire': '#414059', 'Base': '#9DBBAF',
    'Skin1': '#F1C6A6', 'Skin2': '#B87A65', 'Skin3': '#E8AF88',
    'Hair1': '#574F59', 'Hair2': '#8D5B4A', 'Hair3': '#45475B',
    'Pants1': '#7C789D', 'Pants2': '#65688B', 'Pants3': '#5F968F',
}
LIGHTS = {
    'key': ((-2, -3, 7), 900, 4, '#FFF1DD'),
    'fill': ((4.5, -3.5, 3), 250, 5, '#DDE6FF'),
    'rim': ((1.5, 3, 5), 70, 3, '#FFFFFF'),
}
errors = []
checks = []
metrics = {}


def check(condition, description):
    (checks if condition else errors).append(description)


def near(a, b, epsilon=0.0001):
    return abs(a-b) <= epsilon


def same(a, b, epsilon=0.0001):
    return len(a) == len(b) and all(near(x, y, epsilon) for x, y in zip(a, b))


def linear(value):
    rgb = [int(value.lstrip('#')[i:i+2], 16)/255 for i in (0, 2, 4)]
    return tuple(c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4 for c in rgb)


def z_bounds(objects):
    values = [(obj.matrix_world @ Vector(v)).z for obj in objects for v in obj.bound_box]
    return min(values), max(values)


def audit():
    check(Path(bpy.data.filepath).name == 'UiSprites.blend', 'Audit runs against opened UiSprites.blend')
    check(set(bpy.data.scenes.keys()) == set(EXPECTED), 'Saved source contains exactly three approved scenes')
    check(all(name in bpy.data.texts for name in ('create_ui_sprites.py', 'ui_sprite_common.py')),
          'Saved source embeds generator and shared helpers')
    common_world = None
    common_lights = None
    seen_materials = set()
    for name, (sprite_id, size, angles, shadow) in EXPECTED.items():
        scene = bpy.data.scenes.get(name)
        if scene is None:
            check(False, name + ': scene exists')
            continue
        # Library files can leave inactive scenes' matrix_world values at identity
        # after opening. Evaluate their dependency graph before inspecting geometry.
        scene.view_layers[0].update()
        prefix = name + ': '
        check(scene.get('sprite_id') == sprite_id, prefix + 'sprite identity')
        check((scene.render.resolution_x, scene.render.resolution_y) == size and
              scene.render.resolution_percentage == 100, prefix + 'exact 2x render size')
        check(scene.render.engine == 'CYCLES' and scene.cycles.samples == 64 and
              scene.cycles.use_denoising, prefix + 'Cycles64 and denoising')
        check(not scene.cycles.use_adaptive_sampling and not scene.cycles.use_animated_seed,
              prefix + 'fixed sampling and seed')
        check(scene.view_settings.view_transform == 'Standard' and scene.view_settings.look == 'None' and
              near(scene.view_settings.gamma, 1), prefix + 'Standard / None / gamma1')
        check(scene.render.film_transparent and scene.render.image_settings.file_format == 'PNG' and
              scene.render.image_settings.color_mode == 'RGBA' and scene.render.image_settings.color_depth == '8',
              prefix + 'transparent RGBA8 PNG')
        check(not scene.render.use_freestyle, prefix + 'no Freestyle outlines')
        cam = scene.camera
        check(cam is not None and cam.data.type == 'ORTHO', prefix + 'orthographic camera')
        if cam is not None:
            backward = cam.matrix_world.to_quaternion() @ Vector((0, 0, 1))
            yaw = math.degrees(math.atan2(backward.x, -backward.y))
            elevation = math.degrees(math.asin(max(-1, min(1, backward.z))))
            check(same((yaw, elevation), angles, .01), prefix + 'actual camera yaw/elevation')
            metrics[name + '_camera_deg'] = [round(yaw, 4), round(elevation, 4)]
        catchers = [obj for obj in scene.objects if obj.type == 'MESH' and obj.is_shadow_catcher]
        check(len(catchers) == int(shadow), prefix + 'required shadow catcher count')
        meshes = [obj for obj in scene.objects if obj.type == 'MESH' and not obj.is_shadow_catcher]
        check(bool(meshes), prefix + 'editable meshes exist')
        check(all(not poly.use_smooth for obj in meshes for poly in obj.data.polygons), prefix + 'flat faces')
        check(all(mod.type not in {'SUBSURF', 'NODES', 'SOLIDIFY'} for obj in meshes for mod in obj.modifiers),
              prefix + 'no smoothing, procedural replacement, or outline-shell modifiers')
        for obj in meshes:
            if obj.get('primitive') == 'icosphere':
                subdivisions = obj.get('subdivisions')
                check(subdivisions in (1, 2), obj.name + ': ico subdivision1/2')
                if 'Hair_Cap' not in obj.name and subdivisions in (1, 2):
                    expected_faces = 20*4**(subdivisions-1)
                    check(len(obj.data.polygons) == expected_faces and all(len(p.vertices) == 3 for p in obj.data.polygons),
                          obj.name + ': actual ico topology matches metadata')
        if name == 'UI_Clock':
            check(not any('ground' in obj.name.lower() or 'shadow' in obj.name.lower() or
                          'disc' in obj.name.lower() for obj in scene.objects), prefix + 'no floor, shadow, or UI disc')
        for obj in scene.objects:
            if obj.type == 'MESH':
                seen_materials.update(obj.data.materials)
        light_objects = [obj for obj in scene.objects if obj.type == 'LIGHT']
        light_ids = {obj.as_pointer() for obj in light_objects}
        check(len(light_objects) == 3, prefix + 'exactly three studio lights')
        if common_lights is None:
            common_lights = light_ids
        check(light_ids == common_lights, prefix + 'shares identical light objects')
        for label, (location, energy, size_value, color) in LIGHTS.items():
            found = [obj for obj in light_objects if obj.name.lower() == 'ui_' + label]
            check(len(found) == 1, prefix + label + ' stable light name')
            if found:
                obj = found[0]
                check(obj.data.type == 'AREA' and same(tuple(obj.location), location) and
                      near(obj.data.energy, energy) and near(obj.data.size, size_value) and
                      same(tuple(obj.data.color), linear(color)), prefix + label + ' position/power/size/color')
        if common_world is None:
            common_world = scene.world
        check(scene.world is not None and scene.world == common_world, prefix + 'shares environment world')
        bg = scene.world.node_tree.nodes.get('Background') if scene.world and scene.world.use_nodes else None
        check(bg is not None and same(tuple(bg.inputs['Color'].default_value[:3]), linear('#F3ECF7')) and
              near(bg.inputs['Strength'].default_value, .75), prefix + 'environment palette and strength')
        metrics[name + '_mesh_count'] = len(meshes)
        metrics[name + '_exposure'] = scene.view_settings.exposure
    for mat in seen_materials:
        palette_name = mat.get('palette_name')
        check(palette_name in PALETTE, mat.name + ': material belongs to approved palette')
        if palette_name not in PALETTE:
            continue
        bsdf = mat.node_tree.nodes.get('Principled BSDF') if mat.use_nodes else None
        check(bsdf is not None and same(tuple(bsdf.inputs['Base Color'].default_value[:3]), linear(PALETTE[palette_name])),
              mat.name + ': actual node base color equals linearized approved hex')
        check(mat.get('palette_srgb') == PALETTE[palette_name], mat.name + ': palette metadata agrees')
    candy = bpy.data.scenes.get('UI_CottonCandy')
    if candy:
        lobes = [obj for obj in candy.objects if '_Floss_Lobe_' in obj.name]
        cores = [obj for obj in candy.objects if obj.name.endswith('_Floss_Core')]
        check(len(lobes) == 20 and len(cores) == 1, 'Candy: one core and exactly20 lobes')
        check(len([obj for obj in candy.objects if '_Strawberry_Stick_Stripe_' in obj.name]) == 4,
              'Candy: cream stick has four strawberry stripes')
    person = bpy.data.scenes.get('UI_Customer')
    if person:
        whole = [obj for obj in person.objects if obj.type == 'MESH' and not obj.is_shadow_catcher]
        head = [obj for obj in whole if '_Hair_' in obj.name or obj.name.endswith('_Face_Head')]
        if whole and head:
            lo, hi = z_bounds(whole)
            head_lo, head_hi = z_bounds(head)
            ratio = (hi-lo)/(head_hi-head_lo)
            metrics['customer_geometric_head_units'] = round(ratio, 4)
            check(2.25 <= ratio <= 2.75, 'Customer: actual mesh bounds approximately2.5 heads tall')
    print('UI_SPRITE_SCENE_AUDIT', json.dumps({'passed': not errors, 'checks': len(checks), 'errors': errors,
                                              'metrics': metrics}, ensure_ascii=False))
    if errors:
        raise AssertionError('\n'.join(errors))


if __name__ == '__main__':
    audit()

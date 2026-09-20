"""Validate source geometry, dimensions, material names, normals, and FBX round trips."""
import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Art' / 'Blender'
OUT = ROOT / 'Assets' / 'CottonCircuit' / 'Models'
manifest = json.loads((ART / 'racing-props-manifest.json').read_text(encoding='utf-8'))
assets = manifest['assets']
assert set(assets) == {'Chevron', 'Barrier', 'ShortcutGate'}
assert manifest['coordinate_system']['blender_forward'] == '-Y'
assert manifest['coordinate_system']['fbx_axis_forward'] == '-Z'
assert (ART / 'racing-props-preview.png').stat().st_size > 100_000

bpy.ops.wm.open_mainfile(filepath=str(ART / 'RacingProps.blend'))
assert 'Preview' in bpy.data.collections


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    low = [min(point[axis] for point in points) for axis in range(3)]
    high = [max(point[axis] for point in points) for axis in range(3)]
    return low, high


for name, entry in assets.items():
    bpy.ops.wm.open_mainfile(filepath=str(ART / 'RacingProps.blend'))
    assert name in bpy.data.collections
    objects = list(bpy.data.collections[name].objects)
    assert set(entry['objects']) == {obj.name for obj in objects}
    assert all(obj.type == 'MESH' and obj.data.polygons for obj in objects)
    source_low, source_high = bounds(objects)
    for axis in range(3):
        assert abs(source_low[axis] - entry['bounds_blender_xyz']['min'][axis]) < .01
        assert abs(source_high[axis] - entry['bounds_blender_xyz']['max'][axis]) < .01
    assert abs(source_low[2]) < .012, (name, source_low)
    assert entry['triangles'] < 4000, name
    source_materials = {mat.name for obj in objects for mat in obj.data.materials}
    assert source_materials == set(entry['materials'])
    assert source_materials <= set(manifest['materials'])
    for obj in objects:
        assert all(math.isfinite(value) for vertex in obj.data.vertices for value in vertex.co)
        assert all(face.area > 1e-8 for face in obj.data.polygons), obj.name
        assert all(abs(face.normal.length - 1) < 1e-4 for face in obj.data.polygons), obj.name

    filepath = OUT / entry['file']
    assert filepath.stat().st_size > 10_000, filepath
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(filepath), axis_forward='-Z', axis_up='Y')
    imported = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    assert len(imported) >= len(objects), (name, len(imported), len(objects))
    imported_materials = {mat.name.split('.')[0] for obj in imported for mat in obj.data.materials}
    assert source_materials <= imported_materials, (name, imported_materials)
    imported_low, imported_high = bounds(imported)
    source_dimensions = [source_high[i] - source_low[i] for i in range(3)]
    imported_dimensions = [imported_high[i] - imported_low[i] for i in range(3)]
    for original, roundtrip in zip(source_dimensions, imported_dimensions):
        assert abs(original - roundtrip) < .025, (name, source_dimensions, imported_dimensions)
    assert all(face.area > 1e-8 for obj in imported for face in obj.data.polygons), name
    print('VALID', name, 'dimensions', [round(v, 3) for v in imported_dimensions],
          'meshes', len(imported), 'triangles', entry['triangles'])


chevron = assets['Chevron']
assert 2.45 <= chevron['dimensions_blender_xyz'][0] <= 2.55
assert 1.95 <= chevron['dimensions_blender_xyz'][2] <= 2.05
assert len([name for name in chevron['objects'] if name.startswith('DirectionChevron')]) == 3
assert {'Navy', 'Vanilla'} <= set(chevron['materials'])

barrier = assets['Barrier']
assert 1.95 <= barrier['dimensions_blender_xyz'][0] <= 2.05
assert .57 <= barrier['dimensions_blender_xyz'][1] <= .68
assert .60 <= barrier['dimensions_blender_xyz'][2] <= .70

gate = assets['ShortcutGate']
assert 3.35 <= gate['dimensions_blender_xyz'][2] <= 3.45
assert 'GoldBolt' in gate['objects']
bpy.ops.wm.open_mainfile(filepath=str(ART / 'RacingProps.blend'))
posts = [obj for obj in bpy.data.collections['ShortcutGate'].objects if obj.name.startswith('GatePost')]
assert len(posts) == 2
left = min(posts, key=lambda obj: obj.location.x)
right = max(posts, key=lambda obj: obj.location.x)
left_right = max((left.matrix_world @ Vector(corner)).x for corner in left.bound_box)
right_left = min((right.matrix_world @ Vector(corner)).x for corner in right.bound_box)
assert abs((right_left - left_right) - 3.8) < .01
print('RACING_PROPS_VALIDATION_COMPLETE')

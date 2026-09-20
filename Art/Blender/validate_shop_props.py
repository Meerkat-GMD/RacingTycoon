"""Validate editable shop props, visible fronts, normals, and FBX round trips."""
import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Art' / 'Blender'
OUT = ROOT / 'Assets' / 'CottonCircuit' / 'Models'
manifest = json.loads((ART / 'shop-props-manifest.json').read_text(encoding='utf-8'))
assets = manifest['assets']
assert set(assets) == {'DisplayRack', 'OrderBoard', 'QueuePost'}
assert manifest['coordinate_system'] == {
    'units': 'meters', 'blender_forward': '-Y', 'unity_forward': '+Z',
    'fbx_axis_forward': '-Z', 'fbx_axis_up': 'Y', 'origin': 'ground center',
}
assert (ART / 'shop-props-preview.png').stat().st_size > 100_000


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    return ([min(point[axis] for point in points) for axis in range(3)],
            [max(point[axis] for point in points) for axis in range(3)])


def check_meshes(objects, stage):
    for obj in objects:
        assert obj.type == 'MESH' and obj.data.polygons, (stage, obj.name)
        assert all(math.isfinite(value) for vertex in obj.data.vertices
                   for value in vertex.co), (stage, obj.name)
        assert all(face.area > 1e-8 and abs(face.normal.length - 1) < 1e-4
                   for face in obj.data.polygons), (stage, obj.name)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        volume = bm.calc_volume(signed=True)
        bm.free()
        assert volume > 1e-8, (stage, obj.name, 'inward normals', volume)


def check_fronts(objects, stage):
    symbols = [obj for obj in objects if obj.name in
               {'BerrySymbol', 'BerryLeaf', 'CreamSymbol', 'SodaSymbol'}]
    assert len(symbols) == 4, (stage, [obj.name for obj in symbols])
    for obj in symbols:
        face_centers = [(obj.matrix_world @ face.center).y for face in obj.data.polygons]
        front_y = min(face_centers)
        cap = max((face for face, y in zip(obj.data.polygons, face_centers)
                   if y < front_y + .003), key=lambda face: face.area)
        normal = (obj.matrix_world.inverted().transposed().to_3x3() @ cap.normal).normalized()
        assert normal.y < -.9, (stage, obj.name, normal[:])


def compare_dimensions(name, entry, low, high):
    assert abs(low[2]) < .012, (name, low)
    for axis in range(3):
        assert abs(low[axis] - entry['bounds_blender_xyz']['min'][axis]) < .01
        assert abs(high[axis] - entry['bounds_blender_xyz']['max'][axis]) < .01
    expected = {
        'DisplayRack': (2.4, .9, 1.9),
        'OrderBoard': (1.6, None, 2.0),
        'QueuePost': (.4, .4, 1.0),
    }[name]
    dims = [high[i] - low[i] for i in range(3)]
    for actual, target in zip(dims, expected):
        if target is not None:
            assert abs(actual - target) < .015, (name, dims, expected)


for name, entry in assets.items():
    bpy.ops.wm.open_mainfile(filepath=str(ART / 'ShopProps.blend'))
    assert {'Preview', name} <= {col.name for col in bpy.data.collections}
    objects = list(bpy.data.collections[name].objects)
    assert {obj.name for obj in objects} == set(entry['objects'])
    check_meshes(objects, 'source')
    if name == 'OrderBoard':
        check_fronts(objects, 'source')
    source_low, source_high = bounds(objects)
    compare_dimensions(name, entry, source_low, source_high)
    source_materials = {mat.name for obj in objects for mat in obj.data.materials}
    assert source_materials == set(entry['materials'])
    assert source_materials <= set(manifest['materials'])
    assert entry['triangles'] < 4000, name
    if name == 'DisplayRack':
        assert len([obj for obj in objects if obj.name.startswith('Shelf')
                    and obj.name[5:].isdigit()]) == 2
    if name == 'OrderBoard':
        assert {'BerrySymbol', 'CreamSymbol', 'SodaSymbol'} <= set(entry['objects'])

    filepath = OUT / entry['file']
    assert filepath.stat().st_size > 10_000, filepath
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(filepath), axis_forward='-Z', axis_up='Y')
    imported = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    assert len(imported) >= len(objects), (name, len(imported), len(objects))
    check_meshes(imported, 'FBX')
    if name == 'OrderBoard':
        check_fronts(imported, 'FBX')
    imported_materials = {mat.name.split('.')[0] for obj in imported
                          for mat in obj.data.materials}
    assert source_materials <= imported_materials, (name, imported_materials)
    imported_low, imported_high = bounds(imported)
    imported_dims = [imported_high[i] - imported_low[i] for i in range(3)]
    source_dims = [source_high[i] - source_low[i] for i in range(3)]
    assert all(abs(a - b) < .025 for a, b in zip(imported_dims, source_dims)), \
        (name, source_dims, imported_dims)
    assert abs(imported_low[2]) < .025, (name, imported_low)
    print('VALID', name, 'dimensions', [round(v, 3) for v in imported_dims],
          'meshes', len(imported), 'triangles', entry['triangles'])

print('SHOP_PROPS_VALIDATION_COMPLETE')

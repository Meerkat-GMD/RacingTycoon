"""Validate map landmark sources and FBX round trips, including road clearance."""
import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Art' / 'Blender'
OUT = ROOT / 'Assets' / 'CottonCircuit' / 'Models'
assert (ART / 'MapLandmarks.blend').is_file(), 'Map landmark source is missing'
manifest = json.loads((ART / 'map-landmarks-manifest.json').read_text(encoding='utf-8'))
assert set(manifest['assets']) == {'CandyTunnel', 'FinishMarker'}
assert manifest['coordinate_system'] == {
    'units': 'meters', 'blender_forward': '-Y', 'unity_forward': '+Z',
    'fbx_axis_forward': '-Z', 'fbx_axis_up': 'Y', 'origin': 'ground center',
}
assert (ART / 'map-landmarks-preview.png').stat().st_size > 100_000


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    return ([min(p[a] for p in points) for a in range(3)],
            [max(p[a] for p in points) for a in range(3)])


def validate_meshes(objects, stage):
    for obj in objects:
        assert obj.type == 'MESH' and len(obj.data.polygons), (stage, obj.name)
        assert all(math.isfinite(v) for row in obj.matrix_world for v in row), obj.name
        assert all(math.isfinite(v) for vert in obj.data.vertices for v in vert.co), obj.name
        assert all(face.area > 1e-8 and abs(face.normal.length - 1) < 1e-4
                   for face in obj.data.polygons), (stage, obj.name, 'degenerate face')
        assert obj.data.materials and all(obj.data.materials[face.material_index]
                                         for face in obj.data.polygons), obj.name
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        assert all(edge.is_manifold for edge in bm.edges), (stage, obj.name, 'non-manifold')
        assert bm.calc_volume(signed=True) > 1e-7, (stage, obj.name, 'inward normals')
        bm.free()


def validate_clearance(objects, stage):
    """Sample the usable road cross section along the complete tunnel depth.

    A 9.6 m road with 2.3 m headroom must be fully open. Higher samples trace
    the 11 m wide / 8 m apex inner arch with a small numeric surface margin.
    """
    trees = []
    for obj in objects:
        verts = [obj.matrix_world @ v.co for v in obj.data.vertices]
        faces = [list(face.vertices) for face in obj.data.polygons]
        trees.append(BVHTree.FromPolygons(verts, faces))
    rays = [(x, z) for x in (-5.49, -4.8, -2.4, 0, 2.4, 4.8, 5.49)
            for z in (.4, 1.2, 2.3)]
    rays += [(x, 2.5 + math.sqrt(5.5 ** 2 - x ** 2) - .025)
             for x in (-4.8, -3, 0, 3, 4.8)]
    for x, z in rays:
        for tree in trees:
            hit = tree.ray_cast(Vector((x, -4.1, z)), Vector((0, 1, 0)), 8.2)[0]
            assert hit is None, (stage, 'blocked road clearance', x, z, hit)


for name, entry in manifest['assets'].items():
    bpy.ops.wm.open_mainfile(filepath=str(ART / 'MapLandmarks.blend'))
    objects = list(bpy.data.collections[name].objects)
    assert {obj.name for obj in objects} == set(entry['objects'])
    validate_meshes(objects, 'source')
    lo, hi = bounds(objects)
    expected = {'CandyTunnel': (12.92, 8.0, 8.96),
                'FinishMarker': (3.0, 1.5, 5.0)}[name]
    assert abs(lo[2]) < .005, (name, 'ground origin', lo)
    assert abs(lo[0] + hi[0]) < .005 and abs(lo[1] + hi[1]) < .005, (name, lo, hi)
    assert all(abs(hi[a] - lo[a] - expected[a]) < .015 for a in range(3)), (name, lo, hi)
    assert all(abs(lo[a] - entry['bounds_blender_xyz']['min'][a]) < .001 and
               abs(hi[a] - entry['bounds_blender_xyz']['max'][a]) < .001
               for a in range(3)), name
    materials = {mat.name for obj in objects for mat in obj.data.materials}
    assert materials == set(entry['materials']) and materials <= set(manifest['materials'])
    assert entry['triangles'] < 35000, (name, entry['triangles'])
    if name == 'CandyTunnel':
        assert entry['clearance'] == {'width': 11.0, 'apex_height': 8.0,
                                      'road_width': 9.6, 'road_headroom': 2.3}
        validate_clearance(objects, 'source')
    else:
        assert {'CandySwirl', 'RibbonLeft', 'RibbonRight', 'WinnerPodium'} <= set(entry['objects'])
        swirl = bpy.data.objects['CandySwirl']
        assert max((swirl.matrix_world @ v.co).y for v in swirl.data.vertices) < -.25

    source_dimensions = [hi[a] - lo[a] for a in range(3)]
    filepath = OUT / entry['file']
    assert filepath.stat().st_size > 10000, filepath
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(filepath), axis_forward='-Z', axis_up='Y')
    imported = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    assert len(imported) == len(objects), (name, len(imported), len(objects))
    validate_meshes(imported, 'FBX')
    imported_materials = {mat.name.split('.')[0] for obj in imported for mat in obj.data.materials}
    assert imported_materials == materials, (name, imported_materials)
    lo, hi = bounds(imported)
    assert abs(lo[2]) < .01, (name, lo)
    assert all(abs(hi[a] - lo[a] - source_dimensions[a]) < .01 for a in range(3)), (name, lo, hi)
    if name == 'CandyTunnel':
        validate_clearance(imported, 'FBX')
    print('VALID', name, 'dimensions', [round(hi[a] - lo[a], 3) for a in range(3)],
          'meshes', len(imported), 'triangles', entry['triangles'])

print('MAP_LANDMARKS_VALIDATION_COMPLETE')

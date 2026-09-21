"""Check the coupe's editable source and exported FBX integration contract."""
import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Art' / 'Blender'
OUT = ROOT / 'Assets' / 'CottonCircuit' / 'Models'
assert (ART / 'DownhillCoupe.blend').is_file(), 'Downhill coupe source is missing'
manifest = json.loads((ART / 'downhill-coupe-manifest.json').read_text(encoding='utf-8'))
entry = manifest['assets']['DownhillCoupe']
assert manifest['coordinate_system']['blender_forward'] == '-Y'
assert manifest['coordinate_system']['unity_wrapper_y_degrees'] == 180


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    return ([min(v[a] for v in points) for a in range(3)],
            [max(v[a] for v in points) for a in range(3)])


def validate(objects, stage):
    assert {obj.name for obj in objects} == set(entry['objects']), (stage, 'object names')
    materials = set()
    triangles = 0
    for obj in objects:
        assert obj.type == 'MESH' and obj.data.polygons, (stage, obj.name)
        assert all(math.isfinite(v) for row in obj.matrix_world for v in row), obj.name
        assert all(math.isfinite(v) for vert in obj.data.vertices for v in vert.co), obj.name
        assert all(face.area > 1e-9 and abs(face.normal.length - 1) < 1e-4
                   for face in obj.data.polygons), (stage, obj.name, 'degenerate face')
        assert obj.data.materials and all(obj.data.materials[p.material_index]
                                         for p in obj.data.polygons), obj.name
        materials.update(mat.name.split('.')[0] for mat in obj.data.materials)
        triangles += sum(len(p.vertices) - 2 for p in obj.data.polygons)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        assert all(edge.is_manifold and edge.is_contiguous for edge in bm.edges), (stage, obj.name, 'bad edges')
        # Check every disconnected closed part, including the joined wheel/hub geometry.
        unseen = set(bm.faces)
        while unseen:
            seed = unseen.pop()
            connected = {seed}
            pending = [seed]
            while pending:
                for edge in pending.pop().edges:
                    for face in edge.link_faces:
                        if face in unseen:
                            unseen.remove(face)
                            connected.add(face)
                            pending.append(face)
            signed_volume = 0
            for face in connected:
                vertices = [loop.vert.co for loop in face.loops]
                for i in range(1, len(vertices) - 1):
                    signed_volume += vertices[0].dot(vertices[i].cross(vertices[i + 1])) / 6
            assert signed_volume > 1e-9, (stage, obj.name, 'inward component')
        bm.free()
    assert materials == set(entry['materials']) == set(manifest['materials']), (stage, materials)
    assert triangles == entry['triangles'] and triangles < 15000, (stage, triangles)
    lo, hi = bounds(objects)
    assert all(abs(hi[a] - lo[a] - (1.55, 3.10, 1.05)[a]) < .006 for a in range(3)), (stage, lo, hi)
    assert abs(lo[2]) < .002 and abs(lo[0] + hi[0]) < .002 and abs(lo[1] + hi[1]) < .002, (stage, 'origin', lo, hi)
    assert all(abs(lo[a] - entry['bounds_blender_xyz']['min'][a]) < .001 and
               abs(hi[a] - entry['bounds_blender_xyz']['max'][a]) < .001
               for a in range(3)), (stage, 'manifest bounds')
    by_name = {obj.name: obj for obj in objects}
    assert {name for name in by_name if name.startswith('Wheel')} == {'WheelFL', 'WheelFR', 'WheelRL', 'WheelRR'}
    for name, expected in entry['wheel_centers_blender_xyz'].items():
        wheel = by_name[name]
        assert (wheel.matrix_world.translation - Vector(expected)).length < .002, (stage, name, 'pivot')
        axle = (wheel.matrix_world.to_3x3() @ Vector((1, 0, 0))).normalized()
        assert abs(axle.dot(Vector((1, 0, 0)))) > .9999, (stage, name, 'local X axle')
        center = wheel.matrix_world.translation
        assert (center.y < 0) == ('F' in name), (stage, name, 'front/rear')
        assert (center.x < 0) == name.endswith('L'), (stage, name, 'left/right')
    for side in ('L', 'R'):
        front = by_name['Headlight' + side]
        rear = by_name['Taillight' + side]
        assert bounds([front])[1][1] < -1.45 and bounds([rear])[0][1] > 1.45, (stage, 'nose direction')
        assert front.data.materials[0].name.split('.')[0] == 'White'
        assert rear.data.materials[0].name.split('.')[0] == 'Strawberry'
    assert by_name['CabinGlass'].data.materials[0].name.split('.')[0] == 'Navy'
    assert bounds([by_name['RearWing']])[0][1] > .9
    print('VALID', stage, 'DownhillCoupe', 'dimensions', [round(hi[a] - lo[a], 3) for a in range(3)],
          'meshes', len(objects), 'triangles', triangles, 'wheel pivots and front direction verified')


bpy.ops.wm.open_mainfile(filepath=str(ART / 'DownhillCoupe.blend'))
validate(list(bpy.data.collections['DownhillCoupe'].objects), 'source')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT / entry['file']), axis_forward='-Z', axis_up='Y')
validate([obj for obj in bpy.context.scene.objects if obj.type == 'MESH'], 'FBX')
assert (ART / 'downhill-coupe-preview.png').stat().st_size > 100_000
print('DOWNHILL_COUPE_VALIDATION_COMPLETE')

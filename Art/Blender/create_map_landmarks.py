"""Build open candy tunnel and finish sculpture; export editable Unity assets.

Run: blender -b -t 4 --python-exit-code 1 -P Art/Blender/create_map_landmarks.py
Blender -Y is the visible front; Unity's existing model wrapper rotates 180 Y.
"""
import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Art' / 'Blender'
OUT = ROOT / 'Assets' / 'CottonCircuit' / 'Models'
OUT.mkdir(parents=True, exist_ok=True)
COLORS = {
    'Strawberry': 'F48DAB', 'Cream': 'FFF1D4', 'Soda': '7ACDCE',
    'Vanilla': 'F9D27D', 'Navy': '29324D', 'White': 'FFF9ED', 'Gold': 'DBAE61',
}

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for old in list(bpy.data.collections):
    if old.name != 'Collection':
        bpy.data.collections.remove(old)
bpy.data.collections['Collection'].name = 'Scene'
bpy.context.scene.unit_settings.system = 'METRIC'
bpy.context.scene.unit_settings.scale_length = 1


def material(name):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    color = tuple(int(COLORS[name][i:i + 2], 16) / 255 for i in (0, 2, 4))
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    mat.node_tree.nodes.clear()
    shader = mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = .68
    output = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
    mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
    return mat


def collection(name):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def place(obj, col, name, mat, position):
    obj.name = name
    for previous in list(obj.users_collection):
        previous.objects.unlink(obj)
    col.objects.link(obj)
    obj.location = position
    obj.data.materials.append(material(mat))
    return obj


def soften(obj, radius):
    bevel = obj.modifiers.new('Soft candy edges', 'BEVEL')
    bevel.width = radius
    bevel.segments = 3
    bevel.affect = 'EDGES'
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj


def cube(col, name, position, dimensions, mat, radius=.025):
    bpy.ops.mesh.primitive_cube_add(size=1)
    obj = place(bpy.context.object, col, name, mat, position)
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if radius:
        soften(obj, min(radius, min(dimensions) * .3))
    return obj


def cylinder(col, name, position, radius, depth, mat, rotation=None, vertices=40):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = place(bpy.context.object, col, name, mat, position)
    if rotation:
        obj.rotation_euler = rotation
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    soften(obj, .018)
    return obj


def mesh_object(col, name, vertices, faces, mats):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    for mat in mats:
        mesh.materials.append(material(mat))
    return obj


def tube(col, name, points, radius, mat, stripe=None, pitch=1.9, sides=16):
    """Closed swept tube with a rotating material seam for actual spiral stripes."""
    points = [Vector(p) for p in points]
    vertices, faces, normals = [], [], []
    distance = 0
    for i, point in enumerate(points):
        tangent = (points[min(i + 1, len(points) - 1)] - points[max(0, i - 1)]).normalized()
        ref = Vector((0, -1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        u = (ref - tangent * ref.dot(tangent)).normalized()
        v = tangent.cross(u).normalized()
        if i:
            distance += (point - points[i - 1]).length
        twist = distance * 2 * math.pi / pitch if stripe else 0
        for j in range(sides):
            angle = j * 2 * math.pi / sides + twist
            normal = u * math.cos(angle) + v * math.sin(angle)
            vertices.append(tuple(point + radius * normal))
            normals.append(tuple(normal))
    for i in range(len(points) - 1):
        for j in range(sides):
            k = (j + 1) % sides
            faces.append((i * sides + j, i * sides + k,
                          (i + 1) * sides + k, (i + 1) * sides + j))
    faces.append(tuple(reversed(range(sides))))
    faces.append(tuple((len(points) - 1) * sides + j for j in range(sides)))
    obj = mesh_object(col, name, vertices, faces, [mat, stripe] if stripe else [mat])
    for i, face in enumerate(obj.data.polygons):
        face.use_smooth = len(face.vertices) == 4
        if stripe and i < (len(points) - 1) * sides:
            face.material_index = 1 if i % sides < sides * 3 // 8 else 0
    # Twisted quad interpolation otherwise causes corrugated shading despite a
    # round cross section. Preserve analytic tube normals through FBX export.
    split_normals = []
    for face in obj.data.polygons:
        for vertex in face.vertices:
            split_normals.append(normals[vertex] if len(face.vertices) == 4 else tuple(face.normal))
    obj.data.normals_split_custom_set(split_normals)
    return obj


def plaque(col, name, points, front_y, thickness, mat):
    n = len(points)
    vertices = [(x, front_y, z) for x, z in points]
    vertices += [(x, front_y + thickness, z) for x, z in points]
    faces = [tuple(range(n)), tuple(reversed(range(n, n * 2)))]
    faces += [(i, i + n, (i + 1) % n + n, (i + 1) % n) for i in range(n)]
    obj = mesh_object(col, name, vertices, faces, [mat])
    soften(obj, min(.016, thickness * .2))
    return obj


# Three candy arches form an open tunnel, with no walls, roof, or floor.
# Upright center 5.98 minus tube radius .48 leaves exactly 5.5 m per side.
# Arch spring height 2.5 + radius 5.98 - tube .48 gives 8 m at the apex.
tunnel = collection('CandyTunnel')
for index, y in enumerate((-3.52, 0, 3.52)):
    stripe = ('Strawberry', 'Soda', 'Vanilla')[index]
    points = [(-5.98, y, .25 + (2.5 - .25) * i / 18) for i in range(18)]
    points += [(-5.98 * math.cos(math.pi * i / 144), y,
                2.5 + 5.98 * math.sin(math.pi * i / 144)) for i in range(145)]
    points += [(5.98, y, 2.5 - (2.5 - .25) * i / 18) for i in range(1, 19)]
    tube(tunnel, 'SpiralArch%d' % (index + 1), points, .48, 'Cream', stripe)
    for x, side in ((-5.98, 'Left'), (5.98, 'Right')):
        cube(tunnel, 'Foot%d%s' % (index + 1, side), (x, y, .16),
             (.96, .96, .32), 'Navy', .09)
        cylinder(tunnel, 'GoldCuff%d%s' % (index + 1, side), (x, y, .44),
                 .48, .20, 'Gold', vertices=32)
for side in (-1, 1):
    # Slim longitudinal candy strings anchor the ribs while leaving side views open.
    tube(tunnel, 'SideString%s' % ('Left' if side < 0 else 'Right'),
         [(side * 6.02, -3.52 + 7.04 * i / 80, 2.1) for i in range(81)],
         .12, 'White', 'Strawberry', pitch=1.1, sides=12)


# A freestanding roadside winner sculpture: podium, two ribbon tails, candy swirl.
finish = collection('FinishMarker')
cube(finish, 'WinnerPodium', (0, 0, .18), (3, 1.5, .36), 'Navy', .12)
cube(finish, 'PodiumIcing', (0, 0, .435), (2.66, 1.27, .15), 'Cream', .055)
cube(finish, 'PodiumTop', (0, 0, .615), (2.22, 1.06, .21), 'Vanilla', .06)
cube(finish, 'PodiumFrontBand', (0, -.753 + .025, .20), (2.48, .045, .055), 'Gold', .012)
cylinder(finish, 'CandyStem', (0, .035, 1.93), .14, 2.42, 'Cream', vertices=24)
cylinder(finish, 'StemCollar', (0, .035, .81), .23, .18, 'Gold', vertices=32)
plaque(finish, 'RibbonLeft', [(-.12, 3.22), (-.63, 3.25), (-1.23, 1.75),
                            (-.72, 1.97), (-.47, 1.60), (.09, 2.93)], -.05, .13, 'Strawberry')
plaque(finish, 'RibbonRight', [(.12, 3.22), (.63, 3.25), (1.23, 1.75),
                             (.72, 1.97), (.47, 1.60), (-.09, 2.93)], -.045, .13, 'Soda')
plaque(finish, 'RibbonHighlightLeft', [(-.27, 3.04), (-.43, 3.04),
                                     (-.91, 1.97), (-.75, 2.05)], -.073, .015, 'Cream')
plaque(finish, 'RibbonHighlightRight', [(.27, 3.04), (.43, 3.04),
                                      (.91, 1.97), (.75, 2.05)], -.07, .015, 'White')
cylinder(finish, 'CandyRim', (0, 0, 3.66), 1.18, .40, 'Gold', rotation=(math.pi / 2, 0, 0), vertices=64)
cylinder(finish, 'CandyMedallion', (0, -.035, 3.66), 1.095, .41,
         'White', rotation=(math.pi / 2, 0, 0), vertices=64)
spiral = []
for i in range(177):
    t = i / 176
    angle = t * math.pi * 4.7
    radius = .055 + .915 * t
    spiral.append((radius * math.cos(angle), -.38, 3.66 + radius * math.sin(angle)))
tube(finish, 'CandySwirl', spiral, .061, 'Strawberry', sides=10)
cylinder(finish, 'CandyCenter', (0, -.375, 3.66), .09, .085,
         'Strawberry', rotation=(math.pi / 2, 0, 0), vertices=24)
star = []
for i in range(10):
    angle = math.pi / 2 + i * math.pi / 5
    radius = .22 if i % 2 == 0 else .105
    star.append((radius * math.cos(angle), 4.78 + radius * math.sin(angle)))
plaque(finish, 'VictoryStar', star, -.285, .13, 'Vanilla')


names = ('CandyTunnel', 'FinishMarker')
manifest = {
    'coordinate_system': {
        'units': 'meters', 'blender_forward': '-Y', 'unity_forward': '+Z',
        'fbx_axis_forward': '-Z', 'fbx_axis_up': 'Y', 'origin': 'ground center',
    },
    'materials': {name: '#' + value for name, value in COLORS.items()},
    'assets': {},
}
for name in names:
    for obj in list(bpy.data.collections[name].objects):
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target='MESH')
    objects = list(bpy.data.collections[name].objects)
    bpy.context.view_layer.update()
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    low = [min(point[axis] for point in points) for axis in range(3)]
    high = [max(point[axis] for point in points) for axis in range(3)]
    manifest['assets'][name] = {
        'file': name + '.fbx',
        'objects': sorted(obj.name for obj in objects),
        'bounds_blender_xyz': {'min': [round(v, 3) for v in low],
                               'max': [round(v, 3) for v in high]},
        'dimensions_blender_xyz': [round(high[i] - low[i], 3) for i in range(3)],
        'triangles': sum(len(face.vertices) - 2 for obj in objects for face in obj.data.polygons),
        'materials': sorted({mat.name for obj in objects for mat in obj.data.materials}),
    }
    if name == 'CandyTunnel':
        manifest['assets'][name]['clearance'] = {
            'width': 11.0, 'apex_height': 8.0, 'road_width': 9.6, 'road_headroom': 2.3,
        }
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=str(OUT / (name + '.fbx')), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
        bake_space_transform=True, add_leaf_bones=False,
        use_mesh_modifiers=True, path_mode='AUTO',
    )
    bpy.data.collections[name].hide_render = True


preview = collection('Preview')
for name, offset in {'CandyTunnel': (-3.3, 1.5, 0), 'FinishMarker': (6.9, -2.6, 0)}.items():
    for source in bpy.data.collections[name].objects:
        copy = source.copy()
        copy.data = source.data
        copy.name = 'Preview_' + source.name
        preview.objects.link(copy)
        copy.location = source.location + Vector(offset)
cube(preview, 'StudioFloor', (-1, 1, -.19), (25, 19, .35), 'Cream', .12)
# Road strip exists only in the preview; no surface is exported with the tunnel.
cube(preview, 'PreviewRoad', (-3.3, 1.5, -.004), (9.6, 13, .018), 'Navy', .005)
for y in (-3.6, -.4, 2.8, 6.0):
    cube(preview, 'PreviewRoadDash', (-3.3, y, .009), (.15, 1.3, .012), 'Cream', .003)


def light(name, position, energy, size):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.shape, data.size = energy, 'DISK', size
    obj = bpy.data.objects.new(name, data)
    preview.objects.link(obj)
    obj.location = position
    obj.rotation_euler = (Vector((-1, 1, 3)) - obj.location).to_track_quat('-Z', 'Y').to_euler()


light('Key', (-9, -12, 18), 9500, 12)
light('Fill', (10, 5, 15), 7500, 10)
bpy.ops.object.camera_add(location=(18, -29, 17))
camera = bpy.context.object
for old in list(camera.users_collection):
    old.objects.unlink(camera)
preview.objects.link(camera)
camera.rotation_euler = (Vector((-.6, .6, 3.4)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 25
scene = bpy.context.scene
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.render.resolution_x, scene.render.resolution_y = 1800, 1100
scene.render.resolution_percentage = 100
scene.world.color = (.8, .8, .8)
scene.view_settings.view_transform = 'AgX'
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(ART / 'map-landmarks-preview.png')
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ART / 'MapLandmarks.blend'))
(ART / 'map-landmarks-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
bpy.ops.render.render(write_still=True)
print('MAP_LANDMARKS_COMPLETE', json.dumps({name: manifest['assets'][name]['triangles'] for name in names}))

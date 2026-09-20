"""Build editable sugar-racing props and export their Unity-ready FBXs.

Run from any directory: blender -b -t 4 -P Art/Blender/create_racing_props.py
Blender -Y is the visual front. The Unity importer rotates these FBXs 180 degrees.
"""
import json
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Art' / 'Blender'
OUT = ROOT / 'Assets' / 'CottonCircuit' / 'Models'
OUT.mkdir(parents=True, exist_ok=True)

COLORS = {
    'Strawberry': 'F48DAB', 'Cream': 'FFF1D4', 'Soda': '7ACDCE',
    'Vanilla': 'F9D27D', 'Navy': '29324D', 'White': 'FFF9ED',
    'Gold': 'DBAE61',
}

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for old in list(bpy.data.collections):
    if old.name != 'Collection':
        bpy.data.collections.remove(old)
bpy.data.collections['Collection'].name = 'Scene'


def material(name):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    value = COLORS[name]
    color = tuple(int(value[i:i + 2], 16) / 255 for i in (0, 2, 4))
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
    for prior in list(obj.users_collection):
        prior.objects.unlink(obj)
    col.objects.link(obj)
    obj.location = position
    obj.data.materials.append(material(mat))
    return obj


def soften(obj, width=.025):
    bevel = obj.modifiers.new('Rounded edges', 'BEVEL')
    bevel.width = width
    bevel.segments = 2
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


def cylinder(col, name, position, radius, depth, mat, vertices=16, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = place(bpy.context.object, col, name, mat, position)
    if rotation:
        obj.rotation_euler = rotation
    soften(obj, .012)
    return obj


def plaque(col, name, points, front_y, thickness, mat):
    """Extrude a silhouette in the XZ plane toward the driver at -Y."""
    count = len(points)
    vertices = [(x, front_y, z) for x, z in points]
    vertices += [(x, front_y + thickness, z) for x, z in points]
    faces = [tuple(reversed(range(count))), tuple(range(count, 2 * count))]
    for i in range(count):
        nxt = (i + 1) % count
        faces.append((i, nxt, nxt + count, i + count))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    obj.data.materials.append(material(mat))
    soften(obj, .008)
    return obj


# The board is viewed from -Y. Large yellow marks remain readable at kart speed.
chevron = collection('Chevron')
for x, side in [(-.99, 'L'), (.99, 'R')]:
    cube(chevron, 'SignPost' + side, (x, .015, .50), (.105, .13, 1.0), 'Cream', .027)
    cube(chevron, 'SignFoot' + side, (x, .015, .055), (.24, .31, .11), 'Navy', .025)
cube(chevron, 'NavyBoard', (0, 0, 1.39), (2.50, .16, 1.20), 'Navy', .065)
cube(chevron, 'TopTrim', (0, -.087, 1.94), (2.32, .032, .045), 'Gold', .012)
cube(chevron, 'BottomTrim', (0, -.087, .84), (2.32, .032, .045), 'Gold', .012)
for i, center in enumerate((-.74, 0, .74)):
    plaque(chevron, 'DirectionChevron%d' % i, [
        (center - .27, 1.71), (center + .07, 1.39),
        (center - .27, 1.07), (center - .055, 1.07),
        (center + .285, 1.39), (center - .055, 1.71),
    ], -.104, .025, 'Vanilla')


# Segment-to-segment joins read as wrapped candy while preserving a solid rail.
barrier = collection('Barrier')
cube(barrier, 'CreamRail', (0, 0, .3125), (2.0, .60, .625), 'Cream', .105)
cube(barrier, 'TopIcing', (0, 0, .622), (1.98, .58, .055), 'White', .025)
cube(barrier, 'PinkLowerBand', (0, -.308, .205), (1.91, .03, .085), 'Strawberry', .022)
for i, x in enumerate((-.73, 0, .73)):
    cube(barrier, 'CandyWrapper%d' % i, (x, -.312, .405), (.21, .035, .33),
         'Strawberry' if i != 1 else 'Soda', .039)
for x, side in [(-.93, 'L'), (.93, 'R')]:
    cylinder(barrier, 'GoldStud' + side, (x, -.334, .43), .055, .025,
             'Gold', rotation=(1.5707963268, 0, 0))


# Post inner faces are at +/-1.9 m: the clear opening is exactly 3.8 m.
gate = collection('ShortcutGate')
for x, side in [(-2.05, 'L'), (2.05, 'R')]:
    cube(gate, 'GatePost' + side, (x, 0, 1.50), (.30, .36, 3.0), 'Cream', .09)
    cube(gate, 'GateFoot' + side, (x, 0, .12), (.30, .51, .24), 'Navy', .045)
    for j in range(5):
        cube(gate, 'CandyStripe' + side + str(j), (x, -.185, .49 + .45 * j),
             (.31, .024, .13), 'Strawberry' if j % 2 == 0 else 'Soda', .025)
    cylinder(gate, 'CandyMedallion' + side, (x, -.205, 2.72), .115, .04,
             'Gold', rotation=(1.5707963268, 0, 0))
cube(gate, 'GateBeam', (0, 0, 3.20), (4.40, .44, .40), 'Cream', .10)
cube(gate, 'GateBeamFront', (0, -.233, 3.20), (1.25, .028, .29), 'Navy', .04)
plaque(gate, 'GoldBolt', [
    (-.20, 3.31), (.035, 3.31), (-.07, 3.215), (.20, 3.215),
    (-.055, 3.08), (.005, 3.18), (-.20, 3.18),
], -.26, .028, 'Gold')
for i, x in enumerate((-1.62, -1.37, 1.37, 1.62)):
    cube(gate, 'BeamSweet%d' % i, (x, -.232, 3.2), (.12, .025, .20),
         'Strawberry' if i % 2 else 'Soda', .025)


names = ('Chevron', 'Barrier', 'ShortcutGate')
manifest = {
    'coordinate_system': {
        'units': 'meters', 'blender_forward': '-Y', 'unity_forward': '+Z',
        'fbx_axis_forward': '-Z', 'fbx_axis_up': 'Y', 'origin': 'ground center',
    },
    'materials': {name: '#' + value for name, value in COLORS.items()},
    'assets': {},
}
for name in names:
    objects = list(bpy.data.collections[name].objects)
    for obj in objects:
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target='MESH')
    objects = list(bpy.data.collections[name].objects)
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    low = [min(point[axis] for point in points) for axis in range(3)]
    high = [max(point[axis] for point in points) for axis in range(3)]
    triangles = sum(len(face.vertices) - 2 for obj in objects for face in obj.data.polygons)
    manifest['assets'][name] = {
        'file': name + '.fbx',
        'objects': sorted(obj.name for obj in objects),
        'bounds_blender_xyz': {
            'min': [round(v, 3) for v in low],
            'max': [round(v, 3) for v in high],
        },
        'dimensions_blender_xyz': [round(high[i] - low[i], 3) for i in range(3)],
        'triangles': triangles,
        'materials': sorted({mat.name for obj in objects for mat in obj.data.materials}),
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


# A lit showroom is kept in the .blend for a quick source-art inspection.
preview = collection('Preview')
positions = {'Chevron': (-3.7, 0, 0), 'Barrier': (0, 0, 0), 'ShortcutGate': (3.55, 0, 0)}
for name in names:
    for source in bpy.data.collections[name].objects:
        copy = source.copy()
        copy.data = source.data
        copy.name = 'Preview_' + source.name
        preview.objects.link(copy)
        copy.location = source.location + Vector(positions[name])
cube(preview, 'StudioFloor', (0, 0, -.14), (13.5, 5.6, .25), 'White', .02)


def light(name, position, energy, size):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy = energy
    data.shape = 'DISK'
    data.size = size
    obj = bpy.data.objects.new(name, data)
    preview.objects.link(obj)
    obj.location = position
    obj.rotation_euler = (Vector((0, 0, 1.5)) - obj.location).to_track_quat('-Z', 'Y').to_euler()


light('Key', (-5, -5, 8), 1700, 6)
light('Fill', (5, 1, 8), 1200, 6)
bpy.ops.object.camera_add(location=(3.2, -10.5, 6.6))
camera = bpy.context.object
for old in list(camera.users_collection):
    old.objects.unlink(camera)
preview.objects.link(camera)
camera.rotation_euler = (Vector((0, 0, 1.35)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 12.8
scene = bpy.context.scene
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.render.resolution_x = 1600
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.world.color = (.8, .8, .8)
scene.view_settings.view_transform = 'AgX'
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(ART / 'racing-props-preview.png')
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ART / 'RacingProps.blend'))
(ART / 'racing-props-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
bpy.ops.render.render(write_still=True)
print('RACING_PROPS_COMPLETE', json.dumps({name: manifest['assets'][name]['triangles'] for name in names}))

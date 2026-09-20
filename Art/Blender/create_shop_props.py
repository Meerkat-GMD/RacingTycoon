"""Build editable pastel shop furniture and export Unity-ready FBXs.

Run: blender -b -t 4 -P Art/Blender/create_shop_props.py
Blender -Y is the visible front; the Unity model importer rotates the FBX 180 degrees.
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


def soften(obj, width=.02):
    bevel = obj.modifiers.new('Rounded edges', 'BEVEL')
    bevel.width = width
    bevel.segments = 2
    bevel.affect = 'EDGES'
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj


def cube(col, name, position, dimensions, mat, radius=.02):
    bpy.ops.mesh.primitive_cube_add(size=1)
    obj = place(bpy.context.object, col, name, mat, position)
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if radius:
        soften(obj, min(radius, min(dimensions) * .3))
    return obj


def cylinder(col, name, position, radius, depth, mat, vertices=20, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = place(bpy.context.object, col, name, mat, position)
    if rotation:
        obj.rotation_euler = rotation
    soften(obj, .008)
    return obj


def plaque(col, name, points, front_y, thickness, mat):
    """Closed XZ silhouette; its front cap points outward toward Blender -Y."""
    count = len(points)
    vertices = [(x, front_y, z) for x, z in points]
    vertices += [(x, front_y + thickness, z) for x, z in points]
    signed_area = sum(points[i][0] * points[(i + 1) % count][1]
                      - points[(i + 1) % count][0] * points[i][1]
                      for i in range(count))
    front = list(range(count))
    if signed_area < 0:
        front.reverse()
    faces = [tuple(front), tuple(i + count for i in reversed(front))]
    for i in range(count):
        nxt = (i + 1) % count
        side = (i, i + count, nxt + count, nxt)
        faces.append(side if signed_area > 0 else tuple(reversed(side)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    obj.data.materials.append(material(mat))
    soften(obj, .006)
    return obj


# Open shelves allow runtime product meshes to appear on the rack. All furniture
# is centered on the ground and the 2.4 x .9 x 1.9 frame defines its bounds.
rack = collection('DisplayRack')
for x, side in ((-1.15, 'L'), (1.15, 'R')):
    for y, depth in ((-.4, 'Front'), (.4, 'Back')):
        cube(rack, 'Frame%s%s' % (side, depth), (x, y, .95),
             (.1, .1, 1.9), 'Cream', .022)
for level, z in enumerate((.42, 1.02)):
    cube(rack, 'Shelf%d' % level, (0, 0, z), (2.2, .8, .085), 'White', .018)
    cube(rack, 'ShelfFrontLip%d' % level, (0, -.402, z + .053),
         (2.2, .025, .034), 'Strawberry' if level == 0 else 'Soda', .008)
    cube(rack, 'ShelfBackStop%d' % level, (0, .402, z + .095),
         (2.2, .025, .13), 'Cream', .008)
cube(rack, 'BaseRail', (0, 0, .085), (2.2, .78, .12), 'Navy', .02)
cube(rack, 'Header', (0, 0, 1.82), (2.2, .8, .16), 'Cream', .028)
cube(rack, 'HeaderFrontBand', (0, -.411, 1.82), (1.96, .022, .08), 'Gold', .008)


# A single board carries three large, color-coded symbols without lettering.
board = collection('OrderBoard')
for x, side in ((-.71, 'L'), (.71, 'R')):
    cube(board, 'Leg' + side, (x, .025, .80), (.12, .14, 1.60), 'Cream', .025)
    cube(board, 'Foot' + side, (x, .025, .055), (.18, .28, .11), 'Navy', .018)
cube(board, 'BoardFace', (0, .025, 1.28), (1.6, .15, 1.44), 'Navy', .05)
cube(board, 'HeaderTrim', (0, -.057, 1.965), (1.46, .025, .07), 'Gold', .009)
cube(board, 'FooterTrim', (0, -.057, .595), (1.46, .025, .045), 'Gold', .009)
for x, name, color in ((-.49, 'Berry', 'Strawberry'),
                       (0, 'Cream', 'Vanilla'), (.49, 'Soda', 'Soda')):
    cylinder(board, name + 'Badge', (x, -.07, 1.31), .235, .035,
             'White', vertices=32, rotation=(1.5707963268, 0, 0))
    if name == 'Berry':
        plaque(board, 'BerrySymbol', [
            (x-.15, 1.38), (x-.09, 1.44), (x, 1.42), (x+.09, 1.44),
            (x+.15, 1.38), (x+.14, 1.26), (x, 1.12), (x-.14, 1.26),
        ], -.101, .016, color)
        plaque(board, 'BerryLeaf', [
            (x-.11, 1.46), (x-.02, 1.43), (x, 1.51),
            (x+.02, 1.43), (x+.11, 1.46), (x+.05, 1.52), (x-.05, 1.52),
        ], -.104, .018, 'Soda')
    elif name == 'Cream':
        plaque(board, 'CreamSymbol', [
            (x-.16, 1.19), (x+.16, 1.19), (x+.10, 1.28),
            (x+.14, 1.33), (x+.09, 1.39), (x+.025, 1.40),
            (x+.05, 1.48), (x-.04, 1.47), (x-.10, 1.39),
            (x-.14, 1.34), (x-.09, 1.28),
        ], -.101, .016, color)
    else:
        plaque(board, 'SodaSymbol', [
            (x, 1.51), (x+.065, 1.37), (x+.16, 1.31),
            (x+.065, 1.25), (x, 1.11), (x-.065, 1.25),
            (x-.16, 1.31), (x-.065, 1.37),
        ], -.101, .016, color)


# A compact post can be repeated by the runtime to mark the customer queue.
post = collection('QueuePost')
cylinder(post, 'WeightedBase', (0, 0, .055), .2, .11, 'Navy', vertices=32)
cylinder(post, 'Stem', (0, 0, .52), .048, .87, 'Cream', vertices=24)
cylinder(post, 'Collar', (0, 0, .927), .095, .05, 'Gold', vertices=24)
cylinder(post, 'TopCap', (0, 0, .976), .135, .048, 'Strawberry', vertices=32)


names = ('DisplayRack', 'OrderBoard', 'QueuePost')
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
        'bounds_blender_xyz': {'min': [round(v, 3) for v in low],
                               'max': [round(v, 3) for v in high]},
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


preview = collection('Preview')
positions = {'DisplayRack': (-2.4, 0, 0), 'OrderBoard': (.45, 0, 0),
             'QueuePost': (2.25, -.65, 0)}
for name in names:
    for source in bpy.data.collections[name].objects:
        copy = source.copy()
        copy.data = source.data
        copy.name = 'Preview_' + source.name
        preview.objects.link(copy)
        copy.location = source.location + Vector(positions[name])
cube(preview, 'StudioFloor', (0, 0, -.14), (8.3, 4.2, .25), 'White', .02)


def light(name, position, energy, size):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy = energy
    data.shape = 'DISK'
    data.size = size
    obj = bpy.data.objects.new(name, data)
    preview.objects.link(obj)
    obj.location = position
    obj.rotation_euler = (Vector((0, 0, 1)) - obj.location).to_track_quat('-Z', 'Y').to_euler()


light('Key', (-4, -4, 7), 1400, 5)
light('Fill', (4, 2, 6), 1100, 5)
bpy.ops.object.camera_add(location=(3, -7.5, 4.5))
camera = bpy.context.object
for old in list(camera.users_collection):
    old.objects.unlink(camera)
preview.objects.link(camera)
camera.rotation_euler = (Vector((0, 0, 1.0)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 7.9
scene = bpy.context.scene
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.render.resolution_x = 1500
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.world.color = (.8, .8, .8)
scene.view_settings.view_transform = 'AgX'
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(ART / 'shop-props-preview.png')
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ART / 'ShopProps.blend'))
(ART / 'shop-props-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
bpy.ops.render.render(write_still=True)
print('SHOP_PROPS_COMPLETE', json.dumps({name: manifest['assets'][name]['triangles'] for name in names}))

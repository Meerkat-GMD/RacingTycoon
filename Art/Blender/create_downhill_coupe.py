"""Build Cotton Circuit's original downhill coupe and deterministic FBX/preview.

Blender -Y is the nose; keep the existing Unity visual wrapper's 180-degree Y turn.
Run from the project root: blender -b -t 4 --python-exit-code 1 -P this-file.py
"""
import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Art' / 'Blender'
OUT = ROOT / 'Assets' / 'CottonCircuit' / 'Models'
COLORS = {'Cream': 'FFF1D4', 'Soda': '7ACDCE', 'Navy': '29324D',
          'Plum': '6C577F', 'Gold': 'DBAE61', 'Tire': '414059',
          'White': 'FFF9ED', 'Strawberry': 'F48DAB'}
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
car = bpy.data.collections.new('DownhillCoupe')
scene.collection.children.link(car)


def material(name):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    color = tuple(int(COLORS[name][i:i + 2], 16) / 255 for i in (0, 2, 4))
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = .32 if name == 'Navy' else .68
    return mat


def mesh(name, verts, faces, mat, col=car):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    col.objects.link(obj)
    data.materials.append(material(mat))
    return obj


def soften(obj, width=.025):
    mod = obj.modifiers.new('Soft candy edges', 'BEVEL')
    mod.width = width
    mod.segments = 2
    mod.affect = 'EDGES'
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj


def cube(name, position, dimensions, mat, bevel=.015, col=car):
    bpy.ops.mesh.primitive_cube_add(size=1)
    obj = bpy.context.object
    obj.name = name
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    col.objects.link(obj)
    obj.location = position
    obj.dimensions = dimensions
    obj.data.materials.append(material(mat))
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        soften(obj, min(bevel, min(dimensions) * .3))
    return obj


def hull(name, sections, mat, bevel=.01):
    """Closed longitudinal rings; each section is (Y, [(X, Z), ...])."""
    count = len(sections[0][1])
    verts = [(x, y, z) for y, ring in sections for x, z in ring]
    faces = [tuple(range(count - 1, -1, -1))]
    for ring in range(len(sections) - 1):
        for i in range(count):
            j = (i + 1) % count
            faces.append((ring * count + i, ring * count + j,
                          (ring + 1) * count + j, (ring + 1) * count + i))
    faces.append(tuple((len(sections) - 1) * count + i for i in range(count)))
    obj = mesh(name, verts, faces, mat)
    if bevel:
        soften(obj, bevel)
    return obj


def axle_solid(name, position, profile, mat, sides=24):
    """Revolved solid with an X axle and a pivot at its wheel center."""
    verts = [(x, math.cos(i * math.tau / sides) * r, math.sin(i * math.tau / sides) * r)
             for x, r in profile for i in range(sides)]
    faces = [tuple(range(sides - 1, -1, -1))]
    for ring in range(len(profile) - 1):
        for i in range(sides):
            j = (i + 1) % sides
            faces.append((ring * sides + i, ring * sides + j,
                          (ring + 1) * sides + j, (ring + 1) * sides + i))
    faces.append(tuple((len(profile) - 1) * sides + i for i in range(sides)))
    obj = mesh(name, verts, faces, mat)
    obj.location = position
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return obj


def beam(name, start, end, width, depth, mat):
    a, b = Vector(start), Vector(end)
    obj = cube(name, (a + b) / 2, (width, depth, (b - a).length), mat, .006)
    obj.rotation_euler = (b - a).to_track_quat('Z', 'Y').to_euler()
    return obj


def body_ring(halfwidth, top, bottom=.24):
    return [(-halfwidth * .76, bottom), (halfwidth * .76, bottom),
            (halfwidth * .88, top - .18), (halfwidth, top - .055),
            (halfwidth * .91, top), (-halfwidth * .91, top),
            (-halfwidth, top - .055), (-halfwidth * .88, top - .18)]


hull('Body', [(-1.47, body_ring(.61, .565)), (-1.17, body_ring(.66, .60)),
              (-.52, body_ring(.675, .645)), (.74, body_ring(.675, .645)),
              (1.43, body_ring(.65, .61))], 'Cream', .025)
cube('FrontBumper', (0, -1.475, .355), (1.30, .15, .17), 'Plum', .035)
cube('FrontLip', (0, -1.49, .26), (1.35, .12, .045), 'Gold', .012)
cube('RearBumper', (0, 1.475, .36), (1.32, .15, .19), 'Plum', .025)
cube('RearValance', (0, 1.445, .25), (1.22, .15, .055), 'Navy', .01)
hull('Hood', [(-1.425, [(-.55, .548), (.55, .548), (.55, .582), (-.55, .582)]),
              (-.55, [(-.605, .61), (.605, .61), (.605, .66), (-.605, .66)])], 'Soda', .016)
hull('HoodCreamInset', [(-1.35, [(-.085, .579), (.085, .579), (.085, .592), (-.085, .592)]),
                        (-.60, [(-.085, .645), (.085, .645), (.085, .662), (-.085, .662)])], 'Cream', .003)
cube('RearDeck', (0, 1.205, .617), (1.18, .48, .045), 'Soda', .018)

glass_sections = []
for y, lowerwidth, upperwidth, top in [(-.58, .60, .595, .652),
                                      (-.14, .61, .495, 1.015),
                                      (.55, .61, .50, 1.015),
                                      (1.015, .60, .585, .65)]:
    glass_sections.append((y, [(-lowerwidth, .605), (lowerwidth, .605),
                               (upperwidth, top), (-upperwidth, top)]))
hull('CabinGlass', glass_sections, 'Navy', .012)
hull('Roof', [(-.155, [(-.50, 1.002), (.50, 1.002), (.50, 1.05), (-.50, 1.05)]),
              (.575, [(-.505, 1.002), (.505, 1.002), (.505, 1.05), (-.505, 1.05)])], 'Cream', .012)
cube('RoofSodaInset', (0, .205, 1.044), (.16, .65, .012), 'Soda', .003)
for side, sign in [('L', -1), ('R', 1)]:
    beam('FrontPillar' + side, (sign * .605, -.57, .645), (sign * .50, -.145, 1.018), .045, .043, 'Cream')
    beam('RearPillar' + side, (sign * .505, .56, 1.02), (sign * .60, 1.003, .645), .075, .06, 'Cream')
    beam('CenterPillar' + side, (sign * .615, .36, .631), (sign * .505, .33, 1.014), .028, .031, 'Plum')
    cube('Door' + side, (sign * .658, .115, .492), (.045, 1.07, .18), 'Soda', .025)
    cube('DoorHandle' + side, (sign * .689, .355, .586), (.025, .125, .028), 'Gold', .005)
    cube('SideSkirt' + side, (sign * .632, -.005, .24), (.085, 1.31, .075), 'Plum', .018)
    cube('MirrorStem' + side, (sign * .656, -.453, .702), (.095, .05, .035), 'Gold', .005)
    cube('Mirror' + side, (sign * .72, -.458, .729), (.105, .158, .084), 'Cream', .018)
    cube('MirrorGlass' + side, (sign * .72, -.377, .73), (.075, .012, .05), 'Navy', .007)
    cube('Headlight' + side, (sign * .474, -1.492, .524), (.285, .075, .095), 'White', .019)
    cube('HeadlightBrow' + side, (sign * .474, -1.494, .584), (.305, .05, .023), 'Plum', .006)
    cube('Taillight' + side, (sign * .49, 1.501, .552), (.295, .067, .10), 'Strawberry', .015)
    cube('TaillightInset' + side, (sign * .55, 1.539, .552), (.09, .012, .051), 'White', .005)
    cube('WingPylon' + side, (sign * .45, 1.23, .737), (.065, .11, .21), 'Gold', .015)
    cube('WingEndplate' + side, (sign * .68, 1.235, .876), (.045, .315, .145), 'Plum', .016)
cube('FrontGrille', (0, -1.526, .434), (.63, .023, .105), 'Navy', .015)
cube('GrilleBar', (0, -1.543, .435), (.58, .012, .014), 'Gold', .003)
cube('RearWing', (0, 1.235, .863), (1.395, .31, .069), 'Plum', .017)
cube('WingTop', (0, 1.24, .897), (1.25, .26, .025), 'Soda', .006)
cube('RearCenterTrim', (0, 1.49, .554), (.57, .04, .095), 'Cream', .012)

wheel_centers = {}
for side, sign in [('L', -1), ('R', 1)]:
    for axle, y in [('F', -.97), ('R', .97)]:
        name = 'Wheel' + axle + side
        center = (sign * .665, y, .27)
        tire = axle_solid(name, center, [(-.10, .238), (-.075, .27),
                                        (.075, .27), (.10, .238)], 'Tire')
        parts = [tire]
        hubcenter = (center[0] + sign * .10, y, .27)
        parts.append(axle_solid('Hub' + axle + side, hubcenter,
                               [(-.01, .143), (.01, .143)], 'Gold'))
        parts.append(axle_solid('HubInset' + axle + side,
                               (center[0] + sign * .1105, y, .27),
                               [(-.0005, .103), (.0005, .103)], 'Plum'))
        # Inset petal spokes hint at wrapped sweets without a branded badge.
        for i in range(5):
            angle = math.tau * i / 5
            obj = cube('HubPetal', (center[0] + sign * .11,
                                    y + math.sin(angle) * .064,
                                    .27 + math.cos(angle) * .064),
                       (.002, .025, .071), 'Cream', .004)
            obj.rotation_euler.x = -angle
            parts.append(obj)
        for obj in parts:
            bpy.ops.object.select_all(action='DESELECT')
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.convert(target='MESH')
        bpy.ops.object.select_all(action='DESELECT')
        for obj in parts:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = tire
        bpy.ops.object.join()
        tire.name = name
        wheel_centers[name] = list(center)

# Apply all modifiers before the source/FBX counts and bounds are recorded.
for obj in list(car.objects):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')
bpy.context.view_layer.update()
objects = list(car.objects)
points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
lo = [min(v[a] for v in points) for a in range(3)]
hi = [max(v[a] for v in points) for a in range(3)]
triangles = sum(len(face.vertices) - 2 for obj in objects for face in obj.data.polygons)
entry = {'file': 'DownhillCoupe.fbx', 'objects': sorted(obj.name for obj in objects),
         'bounds_blender_xyz': {'min': [round(v, 6) for v in lo], 'max': [round(v, 6) for v in hi]},
         'dimensions_blender_xyz': [round(hi[a] - lo[a], 6) for a in range(3)],
         'triangles': triangles, 'materials': sorted({mat.name for obj in objects for mat in obj.data.materials}),
         'wheel_centers_blender_xyz': wheel_centers, 'wheel_axle_local': 'X',
         'visual_description': 'Original low pastel compact coupe; enclosed cabin, long hood, rear wing; no logos.'}
manifest = {'coordinate_system': {'units': 'meters', 'blender_forward': '-Y', 'unity_forward': '+Z',
             'fbx_axis_forward': '-Z', 'fbx_axis_up': 'Y', 'origin': 'ground center',
             'unity_wrapper_y_degrees': 180},
            'materials': {key: '#' + value for key, value in COLORS.items()}, 'assets': {'DownhillCoupe': entry}}
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects[0]
bpy.ops.export_scene.fbx(filepath=str(OUT / entry['file']), use_selection=True,
                         axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
                         bake_space_transform=True, add_leaf_bones=False,
                         use_mesh_modifiers=True, path_mode='AUTO')
(ART / 'downhill-coupe-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')

# Two render-only copies expose the nose and tail in a single inspectable preview.
preview = bpy.data.collections.new('Preview')
scene.collection.children.link(preview)
car.hide_render = True
for view, x, angle in [('Front', -1.85, 0), ('Rear', 1.85, math.pi)]:
    transform = Matrix.Translation(Vector((x, 0, 0))) @ Matrix.Rotation(angle, 4, 'Z')
    for source in objects:
        duplicate = source.copy()
        duplicate.data = source.data
        duplicate.name = 'Preview_' + view + '_' + source.name
        preview.objects.link(duplicate)
        duplicate.matrix_world = transform @ source.matrix_world
cube('StudioFloor', (0, 0, -.065), (200, 200, .12), 'White', 0, preview)
for name, position, power, size in [('Key', (-4, -5, 9), 1300, 6),
                                     ('Fill', (5, 0, 6), 900, 5),
                                     ('Rim', (0, 5, 7), 1100, 4)]:
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.shape, data.size = power, 'DISK', size
    obj = bpy.data.objects.new(name, data)
    preview.objects.link(obj)
    obj.location = position
    obj.rotation_euler = (Vector((0, 0, .4)) - obj.location).to_track_quat('-Z', 'Y').to_euler()
data = bpy.data.cameras.new('PreviewCamera')
camera = bpy.data.objects.new('PreviewCamera', data)
preview.objects.link(camera)
camera.location = (5.7, -9, 5.5)
camera.rotation_euler = (Vector((0, 0, .36)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
data.type, data.ortho_scale = 'ORTHO', 8.35
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.samples = 40
scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('StudioWorld')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.68, .73, .82, 1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .3
scene.view_settings.view_transform = 'AgX'
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(ART / 'downhill-coupe-preview.png')
# Keep the source easy to edit on opening, with render copies hidden in the viewport.
preview.hide_viewport = True
bpy.ops.object.select_all(action='DESELECT')
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ART / 'DownhillCoupe.blend'))
bpy.ops.render.render(write_still=True)
print('DOWNHILL_COUPE_CREATED', triangles, entry['dimensions_blender_xyz'])

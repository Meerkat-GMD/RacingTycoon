"""Shared pastel-toy UI palette, primitive builders and studio rig.

Blender coordinates: +Z up, -Y front. HEX values are sRGB, node values linear.
No textures, outlines, smooth normals or subdivision-surface modifiers.
"""
import math
import os
import bpy
from mathutils import Vector

from ui_sprite_spec import PALETTE, LIGHTING, EXPOSURE, MATERIAL  # noqa: re-exported for the approved generators


def linear(hex_color):
    value = hex_color.lstrip('#')
    rgb = [int(value[i:i+2], 16)/255 for i in (0, 2, 4)]
    return tuple(c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4 for c in rgb)


def material(name):
    key = 'UI_' + name
    found = bpy.data.materials.get(key)
    if found:
        return found
    mat = bpy.data.materials.new(key)
    mat.diffuse_color = (*linear(PALETTE[name]), 1)
    mat.use_nodes = True
    mat.node_tree.nodes.clear()
    bsdf = mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.name = 'Principled BSDF'
    output = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
    mat.node_tree.links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    bsdf.inputs['Base Color'].default_value = mat.diffuse_color
    bsdf.inputs['Roughness'].default_value = .83
    bsdf.inputs['Specular IOR Level'].default_value = .18
    mat['palette_srgb'] = PALETTE[name]
    mat['palette_name'] = name
    return mat


def collection(scene, name):
    col = bpy.data.collections.new(name)
    scene.collection.children.link(col)
    return col


def place(obj, col, name, xyz, mat):
    obj.name = col.name + '_' + name
    if obj.type == 'MESH':
        obj.data.name = obj.name + '_Mesh'
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    col.objects.link(obj)
    obj.location = xyz
    if mat:
        obj.data.materials.append(material(mat))
    if obj.type == 'MESH':
        for face in obj.data.polygons:
            face.use_smooth = False
    return obj


def ico(col, name, xyz, radii, mat, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1)
    obj = place(bpy.context.object, col, name, xyz, mat)
    obj.scale = radii
    obj['primitive'] = 'icosphere'
    obj['subdivisions'] = subdivisions
    return obj


def box(col, name, xyz, dims, mat, bevel=.035):
    bpy.ops.mesh.primitive_cube_add(size=1)
    obj = place(bpy.context.object, col, name, xyz, mat)
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Small flat chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
    obj['primitive'] = 'beveled_box'
    return obj


def cylinder(col, name, xyz, radius, depth, mat, vertices=16, front=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    obj = place(bpy.context.object, col, name, xyz, mat)
    if front:
        obj.rotation_euler.x = math.pi / 2
    bevel = obj.modifiers.new('Flat rim bevel', 'BEVEL')
    bevel.width = min(.025, depth*.2)
    bevel.segments = 1
    return obj


def segment(col, name, start, end, width, depth, mat, bevel=.012):
    a, b = Vector(start), Vector(end)
    obj = box(col, name, (a+b)/2, (width, depth, (b-a).length), mat, bevel)
    obj.rotation_euler = (b-a).to_track_quat('Z', 'Y').to_euler()
    return obj


def studio():
    col = bpy.data.collections.new('UI_Common_Studio')
    for name in ('key', 'fill', 'rim'):
        cfg = LIGHTING[name]
        data = bpy.data.lights.new('UI_' + name.title(), 'AREA')
        data.energy = cfg['power_w']
        data.shape = 'DISK'
        data.size = cfg['size']
        data.color = linear(cfg['color'])
        obj = bpy.data.objects.new('UI_' + name.title(), data)
        col.objects.link(obj)
        obj.location = cfg['location']
        obj.rotation_euler = (Vector(LIGHTING['aim'])-obj.location).to_track_quat('-Z', 'Y').to_euler()
    world = bpy.data.worlds.new('UI_Common_Environment')
    world.use_nodes = True
    world.node_tree.nodes.clear()
    bg = world.node_tree.nodes.new('ShaderNodeBackground')
    bg.name = 'Background'
    output = world.node_tree.nodes.new('ShaderNodeOutputWorld')
    world.node_tree.links.new(bg.outputs['Background'], output.inputs['Surface'])
    bg.inputs['Color'].default_value = (*linear(LIGHTING['environment']['color']), 1)
    bg.inputs['Strength'].default_value = LIGHTING['environment']['strength']
    return col, world


def setup_scene(name, rig, world, size, seed):
    scene = bpy.data.scenes.new(name)
    window = bpy.context.window or (bpy.context.window_manager.windows[0] if bpy.context.window_manager.windows else None)
    if window:
        window.scene = scene
    scene.collection.children.link(rig)
    scene.world = world
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 64
    scene.cycles.use_denoising = True
    scene.cycles.use_adaptive_sampling = False
    scene.cycles.seed = seed
    scene.cycles.use_animated_seed = False
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.color_depth = '8'
    scene.render.film_transparent = True
    scene.render.use_freestyle = False
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.view_settings.exposure = EXPOSURE
    scene.view_settings.gamma = 1
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 8  # the approved customer generators save their .blend with 8 threads
    return scene


def thread_override(scene):
    """Apply UI_SPRITE_THREADS (default 8) to a scene from setup_scene(). Only the new UI sprite
    builders call this (build_ui_sprites, MinaPortrait/create_mina); the approved customer
    generators keep the fixed 8 threads whatever the environment says."""
    scene.render.threads = int(os.environ.get('UI_SPRITE_THREADS', '8'))
    return scene


def camera(scene, target, scale, yaw=-20, elevation=15):
    data = bpy.data.cameras.new(scene.name + '_Camera')
    data.type = 'ORTHO'
    data.ortho_scale = scale
    data.lens = 50
    obj = bpy.data.objects.new(data.name, data)
    scene.collection.objects.link(obj)
    az, el = math.radians(yaw), math.radians(elevation)
    obj.location = Vector(target) + 10*Vector((math.sin(az)*math.cos(el), -math.cos(az)*math.cos(el), math.sin(el)))
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z', 'Y').to_euler()
    scene.camera = obj
    return {'yaw_deg': yaw, 'elevation_deg': elevation, 'orthographic_scale': scale, 'target': list(target)}


def catcher(scene):
    col = collection(scene, scene.name + '_Ground')
    bpy.ops.mesh.primitive_plane_add(size=200)
    obj = place(bpy.context.object, col, 'ShadowCatcher', (0, 0, -.012), 'Base')
    obj.is_shadow_catcher = True
    return obj

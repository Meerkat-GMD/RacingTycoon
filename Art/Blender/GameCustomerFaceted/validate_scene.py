"""Run inside Blender after opening GameCustomerFaceted.blend."""
import json
from pathlib import Path
import bpy
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

HERE=Path(__file__).resolve().parent
scene=bpy.data.scenes['Game_Customer_Faceted']
scene.view_layers[0].update()
meshes=[o for o in scene.objects if o.type=='MESH' and not o.is_shadow_catcher]
manifest=json.loads((HERE/'manifest.json').read_text(encoding='utf-8'))
palette=set(manifest['palette'].values())
materials={m for o in meshes for m in o.data.materials}
assert all(m.get('palette_srgb') in palette for m in materials)
assert all(not p.use_smooth for o in meshes for p in o.data.polygons)
assert scene.render.engine=='CYCLES' and scene.cycles.samples==64 and scene.cycles.use_denoising
assert scene.view_settings.view_transform=='Standard' and scene.view_settings.look=='None'
assert scene.camera.data.type=='ORTHO' and scene.render.film_transparent
assert scene.render.image_settings.color_mode=='RGBA'
assert (scene.render.resolution_x,scene.render.resolution_y)==(164,280)
assert len([o for o in scene.objects if o.type=='MESH' and o.is_shadow_catcher])==1
required=['GC_Cream_Oval_Badge','GC_Soda_Jacket_Torso','GC_Wood_Messenger_Bag','GC_Left_Tapered_Trousers','GC_Right_Tapered_Trousers']
assert all(n in scene.objects for n in required)
points=[world_to_camera_view(scene,scene.camera,o.matrix_world@Vector(v)) for o in meshes for v in o.bound_box]
bounds=[min(p.x for p in points),min(p.y for p in points),max(p.x for p in points),max(p.y for p in points)]
assert min(bounds[:2])>.025 and max(bounds[2:])<.975, bounds
lights={o.name:o.data.energy for o in scene.objects if o.type=='LIGHT'}
assert sorted(lights.values())==[70,250,900]
result={'passed':True,'scene_count':len(bpy.data.scenes),'mesh_objects':len(meshes),
        'vertices':sum(len(o.data.vertices) for o in meshes),
        'triangles_before_bevel':sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),
        'all_flat_shaded':True,'palette_only':True,'palette_materials':sorted(m.name for m in materials),
        'lights_watts':lights,'geometry_camera_bounds':bounds,'render_size':[164,280],
        'source_scene':scene.name,'packed_scripts':[t.name for t in bpy.data.texts if t.name.startswith('GameCustomerFaceted/')],
        'creation_transport':'Blender MCP execute_blender_code'}
(HERE/'scene-validation.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('GAME_CUSTOMER_SCENE_VERIFIED',result)

"""Run inside Blender after opening GameCustomerFemaleExplorer.blend."""
import json
from pathlib import Path
import bpy
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

HERE=Path(__file__).resolve().parent
scene=bpy.data.scenes['Game_Customer_Female_Explorer']
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
required=['EX_Left_Long_Pink_Coat_Tail','EX_Cream_Jacket_Torso','EX_Wood_Backpack','EX_Left_Bare_Leg','EX_Right_Hand_On_Strap']
assert all(n in scene.objects for n in required)
eyes=[scene.objects['EX_Face_Eye_'+side] for side in ('L','R')]
for eye in eyes:
    assert all(abs(actual-expected*.48)<1e-6 for actual,expected in zip(eye.dimensions,(.075,.020,.106)))
    assert eye.data.materials[0].get('palette_srgb')=='#29324D'
    assert any(m.type=='BEVEL' and m.segments==1 and abs(m.width-.006)<1e-6 for m in eye.modifiers)
old_eye_parts=('EyeWhite_','BrownIris_','EyePupil_','IrisWarmLower_','EyeCatchlight_','UpperLash_','Brow_')
assert not any(o.name.startswith(tuple('EX_Face_'+p for p in old_eye_parts)) for o in meshes)
assert bpy.data.images['EX_Attached_Reference'].packed_file
points=[world_to_camera_view(scene,scene.camera,o.matrix_world@Vector(v)) for o in meshes for v in o.bound_box]
bounds=[min(p.x for p in points),min(p.y for p in points),max(p.x for p in points),max(p.y for p in points)]
assert min(bounds[:2])>.025 and max(bounds[2:])<.975, bounds
lights={o.name:o.data.energy for o in scene.objects if o.type=='LIGHT'}
assert sorted(lights.values())==[70,250,900]
result={'passed':True,'scene_count':len(bpy.data.scenes),'mesh_objects':len(meshes),
        'vertices':sum(len(o.data.vertices) for o in meshes),
        'triangles_before_bevel':sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),
        'all_flat_shaded':True,'palette_only':True,'reference_packed':True,'palette_materials':sorted(m.name for m in materials),
        'male_matched_dot_eyes':True,'eye_objects':[o.name for o in eyes],
        'lights_watts':lights,'geometry_camera_bounds':bounds,'render_size':[164,280],
        'source_scene':scene.name,'packed_scripts':[t.name for t in bpy.data.texts if t.name.startswith('GameCustomerFemaleExplorer/')],
        'creation_transport':'Blender MCP execute_blender_code'}
(HERE/'scene-validation.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('GAME_CUSTOMER_SCENE_VERIFIED',result)



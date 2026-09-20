"""Headless validation of generated blend, manifest and exported FBXs."""
import bpy
import json
from pathlib import Path

root=Path(__file__).resolve().parents[2]
out=root/'Assets'/'CottonCircuit'/'Models'
manifest=json.loads((out/'asset-manifest.json').read_text())
assert len(manifest['assets'])==9
assert (out/'preview.png').stat().st_size>100_000
bpy.ops.wm.open_mainfile(filepath=str(root/'Art'/'Blender'/'CottonCircuit.blend'))
assert 'Preview' in bpy.data.collections
assert all(name in bpy.data.collections for name in manifest['assets'])
for name,entry in manifest['assets'].items():
    assert (out/entry['file']).stat().st_size>10000,name
    assert entry['objects'] and entry['materials'] and entry['triangles']>0,name
    assert all(v>0 for v in entry['dimensions_blender_xyz']),name
    assert all(m in manifest['materials'] for m in entry['materials']),name
    assert set(entry['objects'])=={o.name for o in bpy.data.collections[name].objects},name
    if name!='Kiosk': assert entry['triangles']<12000,name
assert manifest['assets']['Puff']['triangles']<=1800
assert manifest['assets']['Customer']['triangles']<6000
assert all(n in manifest['assets']['Kart']['objects'] for n in ['WheelFL','WheelFR','WheelRL','WheelRR'])
for name,entry in manifest['assets'].items():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(out/entry['file']),axis_forward='-Z',axis_up='Y')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert meshes and {m.name for o in meshes for m in o.data.materials},name
    assert len(meshes)>=len(entry['objects']),name
    print('VALID',name,'meshes',len(meshes),'triangles',entry['triangles'])
print('VALIDATION_COMPLETE')

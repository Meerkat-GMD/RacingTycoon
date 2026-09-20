"""Deterministic Cotton Circuit asset generator. Run from the project root with Blender -b -P."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Art' / 'Blender'
OUT = ROOT / 'Assets' / 'CottonCircuit' / 'Models'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for collection in list(bpy.data.collections):
    if collection.name != 'Collection':
        bpy.data.collections.remove(collection)
base = bpy.data.collections.get('Collection')
base.name = 'Scene'

COLORS = {
    'Strawberry':'F48DAB', 'Cream':'FFF1D4', 'Soda':'7ACDCE',
    'Vanilla':'F9D27D', 'Navy':'29324D', 'Plum':'6C577F',
    'White':'FFF9ED', 'Mint':'99C4AE', 'Gold':'DBAE61',
    'Tire':'414059', 'Wood':'D59C79'
}

def mat(name):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    h = COLORS[name]
    c = tuple(int(h[i:i+2],16)/255 for i in (0,2,4))
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*c, 1)
    m.use_nodes = True
    m.node_tree.nodes.clear()
    node = m.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    output = m.node_tree.nodes.new('ShaderNodeOutputMaterial')
    m.node_tree.links.new(node.outputs['BSDF'], output.inputs['Surface'])
    node.inputs['Base Color'].default_value = (*c, 1)
    node.inputs['Roughness'].default_value = .72
    return m

def collection(name):
    c = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(c)
    return c

def put(obj, col, name, material, loc):
    obj.name = name
    for c in list(obj.users_collection): c.objects.unlink(obj)
    col.objects.link(obj)
    obj.location = loc
    if material: obj.data.materials.append(mat(material))
    return obj

def bevel(obj, amount=.06, segments=2):
    mod = obj.modifiers.new('Soft edges', 'BEVEL')
    mod.width = amount
    mod.segments = segments
    mod.affect = 'EDGES'
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj

def cube(col, name, loc, dims, material, roundness=.04):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = put(bpy.context.object,col,name,material,loc)
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if roundness: bevel(o,roundness)
    return o

def sphere(col,name,loc,scale,material,segments=16,rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings)
    o=put(bpy.context.object,col,name,material,loc)
    o.scale=scale
    return o

def cylinder(col,name,loc,radius,depth,material,vertices=16,rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth)
    o=put(bpy.context.object,col,name,material,loc)
    if rotation: o.rotation_euler=rotation
    bevel(o,.025,2)
    return o

def torus(col,name,loc,major,minor,material,rotation=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=32,minor_segments=6,location=loc,major_radius=major,minor_radius=minor)
    o=put(bpy.context.object,col,name,material,loc)
    if rotation: o.rotation_euler=rotation
    return o

def cone(col,name,loc,r1,r2,depth,material,vertices=12):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=depth)
    return put(bpy.context.object,col,name,material,loc)

# Blender's -Y points forward; FBX conversion maps it to Unity +Z.
kart=collection('Kart')
cube(kart,'Body',(0,0,.48),(1.28,2.34,.42),'Strawberry',.18)
cube(kart,'Nose',(0,-.93,.63),(1.18,.52,.26),'Cream',.12)
cube(kart,'Floor',(0,.16,.3),(1.35,1.9,.13),'Cream',.06)
cube(kart,'SeatBase',(0,.31,.86),(.68,.7,.19),'Plum',.1)
cube(kart,'SeatBack',(0,.62,1.13),(.71,.18,.58),'Plum',.08)
cube(kart,'Dashboard',(0,-.38,.92),(1.01,.22,.23),'Cream',.08)
torus(kart,'SteeringWheel',(0,-.32,1.14),.2,.026,'Navy',(math.radians(65),0,0))
cylinder(kart,'SteeringStem',(0,-.4,.98),.03,.25,'Navy')
for side,sx in [('L',-.72),('R',.72)]:
    for axle,sy in [('F',-.78),('R',.78)]:
        wheel=cylinder(kart,'Wheel'+axle+side,(sx,sy,.32),.31,.18,'Tire',20,(0,math.pi/2,0))
        cylinder(kart,'Hub'+axle+side,(sx+(-.105 if sx<0 else .105),sy,.32),.12,.025,'Gold',16,(0,math.pi/2,0))
    sphere(kart,'Headlight'+side,(sx*.68,-1.19,.63),(.13,.075,.11),'White')
cylinder(kart,'SugarTank',(0,.92,.91),.26,.7,'Soda')
cone(kart,'CrystalTop',(0,.92,1.39),.17,0,.34,'Vanilla',8)
torus(kart,'TankRing',(0,.92,1.13),.26,.025,'Gold')
cylinder(kart,'FlagPole',(-.44,.75,1.17),.025,.65,'Gold')
cube(kart,'CanopyFlag',(-.29,.75,1.59),(.26,.035,.16),'Strawberry',.01)

kiosk=collection('Kiosk')
cube(kiosk,'Platform',(0,0,.13),(5,3,.26),'Wood',.06)
cube(kiosk,'BackWall',(0,1.34,2.0),(4.55,.24,3.5),'Cream',.13)
for x in (-2.15,2.15):
    cube(kiosk,'FrontPillar',(x,-1.3,2.1),(.22,.22,3.7),'Cream',.09)
    cube(kiosk,'SideShelf',(x*.87,.6,1.25),(.42,1.35,.1),'Wood',.04)
cube(kiosk,'CounterFront',(0,-1.14,.69),(4.55,.65,1.12),'Strawberry',.09)
cube(kiosk,'CounterTop',(0,-1.1,1.25),(4.8,.73,.13),'Wood',.06)
cube(kiosk,'Roof',(0,0,4.03),(4.9,3.0,.25),'Cream',.1)
cube(kiosk,'AwningLip',(0,-1.25,3.54),(5.0,.38,.44),'Cream',.12)
for i in range(10):
    x=-2.25+i*.5
    cube(kiosk,'AwningStripe%02d'%i,(x,-1.25,3.55),(.49,.4,.44),'Strawberry' if i%2==0 else 'Cream',.035)
for x in (-1.75,-.9,0,.9,1.75):
    cylinder(kiosk,'CandyJar',(x,-.92,1.45),.14,.31,'White',12)
    sphere(kiosk,'CandyInJar',(x,-.92,1.63),(.12,.12,.1),'Vanilla' if x<0 else 'Soda',12,6)
for x in (-1.85,1.85):
    cylinder(kiosk,'ShelfJar',(x,.6,1.65),.16,.32,'Strawberry')
    sphere(kiosk,'ShelfCandy',(x,.6,1.87),(.17,.17,.16),'Soda')
cylinder(kiosk,'RoofStick',(0,0,4.3),.045,.45,'Wood')
sphere(kiosk,'RoofCandy',(0,0,4.51),(.43,.28,.23),'Strawberry')

spinner=collection('Spinner')
cylinder(spinner,'Pedestal',(0,0,.33),.97,.66,'Plum',32)
cylinder(spinner,'PedestalTop',(0,0,.71),.82,.17,'Cream',32)
torus(spinner,'SodaRing',(0,0,.51),.93,.045,'Soda')
torus(spinner,'PinkRing',(0,0,.77),.8,.04,'Strawberry')
cylinder(spinner,'DriveHousing',(0,0,1.08),.45,.65,'Cream',24)
cylinder(spinner,'CenterStick',(0,0,3.91),.08,5.18,'Wood',16)
sphere(spinner,'StickTip',(0,0,6.5),(.09,.09,.09),'Cream',12,6)

puff=collection('Puff')
for i,(x,y,z,s) in enumerate([(-.2,0,0,.33),(.2,.05,.02,.32),(0,-.17,.13,.34),(0,.19,-.1,.3),(-.06,0,-.23,.27)]):
    lobe=sphere(puff,'Lobe%02d'%i,(x,y,z),(s,s*.9,s*.87),'White',12,6)
    for polygon in lobe.data.polygons:
        polygon.use_smooth=True

customer=collection('Customer')
for x,side in [(-.17,'L'),(.17,'R')]:
    cube(customer,'Shoe'+side,(x,-.025,.075),(.22,.34,.15),'Navy',.06)
    cylinder(customer,'Leg'+side,(x,0,.43),.115,.7,'Navy',12)
    sphere(customer,'Arm'+side,(x*2.05,0,1.05),(.11,.12,.37),'Soda',12,6)
sphere(customer,'Torso',(0,0,1.08),(.36,.24,.43),'Soda')
sphere(customer,'Head',(0,-.015,1.43),(.235,.22,.21),'Cream')
sphere(customer,'HairCap',(0,.035,1.54),(.25,.23,.11),'Navy',12,6)
for x,side in [(-.09,'L'),(.09,'R')]: sphere(customer,'Eye'+side,(x,-.215,1.45),(.025,.02,.025),'Navy',8,4)

crystal=collection('Crystal')
for i,(x,y,h,ma) in enumerate([(0,0,.7,'Vanilla'),(-.17,.07,.43,'Soda'),(.18,.05,.5,'Vanilla'),(.04,-.16,.35,'Soda')]):
    cone(crystal,'Facet%02d'%i,(x,y,h/2),.14 if i==0 else .11,0,h,ma,6)

arch=collection('Arch')
for x,side in [(-2.2,'L'),(2.2,'R')]:
    cube(arch,'Foot'+side,(x,0,.16),(.6,.9,.32),'Cream',.08)
    cube(arch,'Post'+side,(x,0,1.95),(.38,.43,3.65),'Cream',.14)
    for j in range(6):
        cube(arch,'PostStripe'+side+str(j),(x,0,.65+j*.53),(.39,.44,.15),'Strawberry',.045)
cube(arch,'Beam',(0,0,3.92),(4.75,.49,.42),'Cream',.17)
for j in range(7):
    cube(arch,'BeamStripe'+str(j),(-2.06+j*.68,0,3.93),(.25,.5,.43),'Strawberry',.06)

tree=collection('Tree')
cylinder(tree,'Trunk',(0,0,1.35),.16,2.7,'Wood',12)
for i,(x,y,z,s,ma) in enumerate([(0,0,3.18,.74,'Mint'),(-.5,.08,3.03,.53,'Mint'),(.48,.06,3.0,.55,'Strawberry'),(0,-.35,3.29,.49,'Mint')]):
    sphere(tree,'Canopy%02d'%i,(x,y,z),(s,s*.9,s*.78),ma,16,8)

lamp=collection('Lamp')
cylinder(lamp,'Base',(0,0,.15),.23,.3,'Navy',16)
cylinder(lamp,'Post',(0,0,1.5),.065,2.75,'Navy',12)
cylinder(lamp,'GoldCollar',(0,0,2.91),.18,.12,'Gold',16)
sphere(lamp,'Globe',(0,0,3.13),(.27,.27,.27),'White',16,8)
cone(lamp,'TopCap',(0,0,3.35),.18,0,.14,'Gold',12)

names=['Kart','Kiosk','Spinner','Puff','Customer','Crystal','Arch','Tree','Lamp']
manifest={'coordinate_system':{'units':'meters','blender_forward':'-Y','unity_forward':'+Z','fbx_axis_forward':'-Z','fbx_axis_up':'Y','origin':'ground center; Puff at center'},'materials':{'%s'%k:'#'+v for k,v in COLORS.items()},'assets':{}}
for name in names:
    col=bpy.data.collections[name]
    objects=list(col.objects)
    for o in objects:
        bpy.context.view_layer.objects.active=o
        o.select_set(True)
        bpy.ops.object.convert(target='MESH')
        o.select_set(False)
    objects=list(col.objects)
    verts=[]
    triangles=0
    slots=set()
    for o in objects:
        verts.extend([o.matrix_world @ Vector(c) for c in o.bound_box])
        triangles+=sum(len(p.vertices)-2 for p in o.data.polygons)
        slots.update(m.name for m in o.data.materials)
    lo=[min(v[i] for v in verts) for i in range(3)]
    hi=[max(v[i] for v in verts) for i in range(3)]
    manifest['assets'][name]={'file':name+'.fbx','objects':[o.name for o in objects],'bounds_blender_xyz':{'min':[round(x,3) for x in lo],'max':[round(x,3) for x in hi]},'dimensions_blender_xyz':[round(hi[i]-lo[i],3) for i in range(3)],'triangles':triangles,'materials':sorted(slots)}
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects: o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,use_mesh_modifiers=True,path_mode='AUTO')

preview=collection('Preview')
positions={'Kart':(-7,0,0),'Kiosk':(0,1,0),'Spinner':(6.5,1,0),'Puff':(-7,-4,1),'Customer':(-4.7,-4,0),'Crystal':(-2.5,-4,0),'Arch':(0,-4,0),'Tree':(4,-4,0),'Lamp':(7,-4,0)}
for name in names:
    for source in bpy.data.collections[name].objects:
        dup=source.copy()
        dup.data=source.data
        preview.objects.link(dup)
        dup.location=source.location+Vector(positions[name])
        dup.name='Preview_'+source.name

bpy.ops.object.select_all(action='DESELECT')
cube(preview,'StudioFloor',(0,0,-.15),(20,12,.2),'White',.01)
def area(name,loc,power,size):
    data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); preview.objects.link(obj); obj.location=loc
    direction=Vector((0,0,0))-obj.location
    obj.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
area('Key',(-7,-8,14),2200,10)
area('Fill',(7,3,13),1700,9)
bpy.ops.object.camera_add(location=(13,-18,17))
cam=bpy.context.object
for c in list(cam.users_collection): c.objects.unlink(cam)
preview.objects.link(cam)
direction=Vector((0,-1,1.2))-cam.location
cam.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO'; cam.data.ortho_scale=21
bpy.context.scene.camera=cam
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=1600; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.world.color=(.8,.8,.8)
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(ART/'preview.png')
scene.view_settings.view_transform='AgX'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'CottonCircuit.blend'))
(ART/'asset-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
bpy.ops.render.render(write_still=True)
print('ASSET_KIT_COMPLETE',json.dumps({n:manifest['assets'][n]['triangles'] for n in names}))

"""Reconstruct the user-provided adventurer as editable, flat-shaded geometry.

Run through Blender MCP with runpy.run_path(..., run_name='__main__').
RA_ACTION='build' builds and saves, 'render' renders existing edited geometry,
'all' (default) does both. Earlier UI sprite scenes and PNGs are untouched.
"""
import importlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

HERE=Path(__file__).resolve().parent
if str(HERE) not in sys.path: sys.path.insert(0,str(HERE))
import artlib as a
importlib.reload(a)


def coat(col):
    # Narrow upper coat, flared separate front panels, dark split rear tails.
    a.loft(col,'Coat_Torso',[(3.05,0,.06,.55,.34),(3.60,0,.035,.57,.36),
                           (4.25,0,.04,.76,.37),(4.49,0,.03,.68,.32)],'Coat',8)
    a.panel(col,'Dark_Shirt_Opening',[(-.17,-.355,4.45),(.18,-.355,4.45),(.15,-.37,3.0),(-.15,-.37,3.0)],'LeatherDark')
    skirt=a.loft(col,'Coat_Back_Skirt',[(1.58,0,.13,.95,.37),(1.67,0,.10,1.02,.39),
                                (2.52,0,.08,.69,.36),(3.27,0,.04,.56,.33)],'LeatherDark',10)
    # Open the front of the coat; the two front tails do not form a solid skirt.
    import bmesh
    bm=bmesh.new();bm.from_mesh(skirt.data)
    front=[f for f in bm.faces if f.calc_center_median().y<.10]
    bmesh.ops.delete(bm,geom=front,context='FACES');bm.to_mesh(skirt.data);bm.free()
    # The central dark opening must remain legible all the way down the coat.
    a.panel(col,'Rear_Left_Tail',[(-.80,.26,2.49),(-.31,.26,2.57),(-.30,.32,1.10),(-.82,.37,1.01),(-.97,.31,1.52)],'LeatherDark',.08)
    a.panel(col,'Rear_Right_Tail',[(.29,.27,2.59),(.78,.26,2.45),(.98,.34,1.46),(.80,.36,1.17),(.26,.31,1.14)],'LeatherDark',.08)
    for side,s in (('Left',-1),('Right',1)):
        outline=[(s*.14,-.416,3.24),(s*.59,-.37,3.18),(s*.77,-.39,2.29),
                 (s*1.00,-.47,1.38),(s*.28,-.53,1.20),(s*.18,-.48,2.02)]
        a.panel(col,side+'_Coat_Front',outline,'CoatPanel' if s<0 else 'CoatLight',.105)
        a.panel(col,side+'_Coat_Outer_Facet',[(s*.58,-.395,3.12),(s*.74,-.32,2.98),
                    (s*1.035,-.35,1.43),(s*.85,-.495,1.34)],'CoatDark',.055)
        a.band(col,side+'_Hem_Band',(s*.29,-.554,1.36),(s*.975,-.492,1.55),.13,'Leather',.032)
        a.band(col,side+'_Hem_Stitch',(s*.33,-.56,1.50),(s*.95,-.51,1.67),.055,'CoatDark',.023)
        a.band(col,side+'_Front_Seam',(s*.245,-.531,1.53),(s*.195,-.44,2.84),.045,'Leather',.024)
    a.panel(col,'Left_Chest_Lapel',[(-.67,-.35,4.40),(-.44,-.42,4.50),(-.11,-.439,3.43),(-.31,-.427,3.54)],'CoatLight',.04)
    a.panel(col,'Right_Chest_Lapel',[(.45,-.37,4.42),(.67,-.34,4.25),(.40,-.432,3.58),(.16,-.442,3.40)],'CoatDark',.045)


def legs(col):
    for name,s in (('Left',-1),('Right',1)):
        x=s*.59
        a.loft(col,name+'_Trouser_Leg',[(.84,x,0,.235,.25),(1.13,x,0,.29,.29),
                    (1.73,x*.85,.02,.31,.30),(2.58,x*.70,.045,.27,.28)],'Pants',8)
        a.panel(col,name+'_Knee_Flap',[(x-.27,-.28,1.39),(x+.27,-.28,1.40),
                                     (x+.24,-.34,1.13),(x-.20,-.34,1.08)],'LeatherDark',.08)
        boot=a.box(col,name+'_Boot_Foot',(s*.65,-.145,.15),(.46,.72,.28),'Boot',.035)
        boot.rotation_euler.z=s*.38
        sole=a.box(col,name+'_Boot_Sole',(s*.65,-.17,.055),(.48,.73,.105),'LeatherDark',.014)
        sole.rotation_euler.z=s*.38
        toe=a.box(col,name+'_Boot_Toe',(s*.74,-.38,.153),(.42,.31,.16),'Leather',.025)
        toe.rotation_euler.z=s*.38
        a.loft(col,name+'_Boot_Shaft',[(.20,s*.60,.045,.18,.22),(.76,s*.59,.055,.21,.23),
                                     (1.10,s*.59,.045,.23,.25)],'Boot',8)
        for i,z in enumerate((.38,.65,.94)):
            a.band(col,name+'_Shin_Wrap_%d'%i,(x-.21,-.24,z+.07),(x+.22,-.255,z-.035),.10,
                   'LeatherLight' if i!=1 else 'Leather',.035)
            if i==1:
                a.panel(col,name+'_Wrap_Tab_%d'%i,[(x+s*.21,-.13,z+.02),(x+s*.36,-.09,z-.06),
                                                (x+s*.20,-.12,z-.13)],'Leather',.05)


def arms(col):
    for name,s in (('Left',-1),('Right',1)):
        a.segment(col,name+'_Upper_Sleeve',(s*.75,.0,4.25),(s*.99,-.015,3.51),.38,.46,'CoatDark',.025)
        a.panel(col,name+'_Upper_Sleeve_Lit',[(s*.81,-.24,4.02),(s*1.00,-.21,3.95),
                    (s*1.095,-.23,3.52),(s*.89,-.268,3.50)],'Coat',.032)
        a.panel(col,name+'_Elbow_Cuff',[(s*.82,-.26,3.66),(s*1.13,-.22,3.62),
                    (s*1.18,-.23,3.43),(s*.80,-.30,3.49)],'LeatherDark',.10)
        a.segment(col,name+'_Forearm',(s*.995,-.02,3.46),(s*1.095,-.115,2.79),.285,.33,'Leather',.024)
        a.band(col,name+'_Exposed_Wrist_Strip',(s*.91,-.197,3.38),(s*1.135,-.19,3.40),.11,'SkinShade',.04)
        for i,z in enumerate((3.18,2.94)):
            a.band(col,name+'_Forearm_Wrap_%d'%i,(s*.96,-.215,z+.055),(s*1.19,-.22,z+.065),.12,'LeatherDark',.07)
        a.box(col,name+'_Cuff_Tab',(s*1.035,-.288,3.17),(.11,.045,.075),'Gold',.008)
        a.ico(col,name+'_Gloved_Hand',(s*1.12,-.115,2.65),(.22,.205,.265),'LeatherDark',1)
        a.panel(col,name+'_Bare_Fingers',[(s*.93,-.249,2.59),(s*1.18,-.255,2.53),
                    (s*1.27,-.20,2.66),(s*1.26,-.24,2.44),(s*1.04,-.296,2.39),(s*.92,-.28,2.48)],'Skin',.065)
        a.box(col,name+'_Fingerless_Glove_Back',(s*1.13,-.286,2.74),(.24,.04,.21),'Leather',.012)


def scarf(col):
    # Broad short shoulder shawl; five diagonal faceted courses, no long red cloak.
    sides=10
    angles=[math.tau*i/sides for i in range(sides)]
    rings=[(4.24,1.03,.47),(4.46,.90,.43),(4.66,.70,.35),(4.87,.57,.31)]
    verts=[]
    for r,(z,rx,ry) in enumerate(rings):
        for theta in angles:
            x=rx*math.sin(theta); y=-ry*math.cos(theta)
            # On the chest the wrap sweeps down toward the right shoulder.
            dz=-.20*x if y<.08 else .05
            verts.append((x,y-.025,z+dz))
    faces=[]
    for r in range(len(rings)-1):
        for i in range(sides):
            faces.append((r*sides+i,r*sides+(i+1)%sides,(r+1)*sides+(i+1)%sides,(r+1)*sides+i))
    obj=a.mesh(col,'Scarf_Shoulder_Wrap',verts,faces,'Scarf')
    obj.data.materials.append(a.mat('ScarfMid'))
    obj.data.materials.append(a.mat('ScarfLight'))
    for p in obj.data.polygons: p.material_index=(p.index//sides)%3
    # Collar rings: raised dark opening with warm outer folded bands.
    a.loft(col,'Scarf_High_Collar',[(4.66,0,-.005,.54,.34),(4.87,0,.005,.54,.31),
                 (4.98,0,.015,.48,.285)],'ScarfDark',8)
    a.panel(col,'Collar_Front_Fold',[(-.51,-.302,4.88),(-.28,-.377,4.82),(.49,-.303,4.93),
                 (.50,-.329,4.76),(.07,-.385,4.69),(-.50,-.327,4.74)],'Scarf',.065)
    a.panel(col,'Scarf_Diagonal_Light_Fold',[(-.68,-.375,4.66),(-.48,-.444,4.63),(.89,-.455,4.16),
                 (.92,-.463,4.03),(.27,-.548,4.19),(-.78,-.394,4.48)],'ScarfLight',.052)
    a.panel(col,'Scarf_Right_Drape',[(.29,-.475,4.41),(.93,-.333,4.29),(1.09,-.315,3.90),
                 (1.05,-.385,3.73),(.74,-.479,3.94),(.30,-.541,4.10)],'Scarf',.072)
    a.panel(col,'Scarf_Right_Fold',[(.43,-.542,4.12),(1.06,-.405,3.92),(1.10,-.405,3.80),
                 (.67,-.527,3.94)],'ScarfDark',.04)
    a.panel(col,'Scarf_Left_Front',[(-.88,-.31,4.40),(-.51,-.47,4.42),(-.33,-.519,4.13),
                 (-.49,-.50,3.96),(-1.07,-.36,3.99)],'Scarf',.065)
    for i in range(9):
        x=-1.02+i*.07
        z=4.08 + .18*(x+1.02)/.56
        length=(.25,.16,.31,.20,.35,.22,.26,.15,.21)[i]
        a.panel(col,'Scarf_Left_Fringe_%02d'%i,[(x,-.39-.17*(x+1.02),z),
                    (x+.047,-.40-.17*(x+1.02),z+.01),(x+.035,-.42-.17*(x+1.02),z-length),
                    (x-.012,-.415-.17*(x+1.02),z-length-.025)],'ScarfMid' if i%3==0 else 'Scarf',.025)
    for i in range(5):
        x=.34+i*.13; z=4.06-.20*(x-.34)
        a.panel(col,'Scarf_Right_Fringe_%02d'%i,[(x,-.548,z),(x+.067,-.535,z),
                    (x+.08,-.53,z-.16-(i%2)*.08),(x+.02,-.549,z-.14)],'ScarfDark',.03)


def accessories(col):
    a.loft(col,'Waist_Belt',[(3.14,0,.015,.60,.405),(3.32,0,.015,.595,.405)],'LeatherDark',8)
    a.band(col,'Belt_Front',(-.565,-.432,3.23),(.55,-.428,3.22),.16,'Leather',.04)
    # Squared octagonal gold buckle, open brown center.
    pts=[(-.21,-.468,3.15),(-.26,-.468,3.22),(-.20,-.468,3.32),(.15,-.468,3.32),
         (.22,-.468,3.22),(.15,-.468,3.14)]
    pts=[(x*.82,y,z) for x,y,z in pts]
    for i in range(6): a.band(col,'Waist_Buckle_Edge%d'%i,pts[i],pts[(i+1)%6],.035,'Gold',.04)
    # Broad diagonal strap is outside clothing, visible below the scarf edge.
    a.band(col,'Crossbody_Strap_Shadow',(-.64,-.57,4.45),(.52,-.54,3.20),.215,'LeatherDark',.055)
    a.band(col,'Crossbody_Strap',(-.66,-.611,4.47),(.51,-.58,3.23),.153,'LeatherLight',.035)
    a.buckle(col,'Shoulder_Strap_Buckle',(-.43,-.656,4.22),.205,.21,math.radians(39))
    for i,(x,z) in enumerate(((-.18,3.94),(.04,3.70))):
        a.buckle(col,'Strap_Fitting_%02d'%i,(x,-.645,z),.17,.16,math.radians(39))
    # Major bag on the right hip; small center-left flap pouch and utility tabs.
    bag=a.box(col,'Right_Hip_Satchel',(.56,-.48,2.94),(.39,.31,.51),'Pouch',.026)
    bag.rotation_euler.y=-.08
    a.box(col,'Right_Hip_Satchel_Flap',(.55,-.652,3.115),(.41,.055,.16),'LeatherLight',.012)
    a.band(col,'Satchel_Flap_Seam',(.365,-.689,3.045),(.74,-.689,3.045),.028,'LeatherDark')
    a.box(col,'Lower_Left_Pouch',(-.29,-.568,2.68),(.34,.27,.41),'Pouch',.015)
    a.panel(col,'Lower_Left_Pouch_Flap',[(-.49,-.724,2.89),(-.13,-.724,2.89),
                    (-.15,-.741,2.72),(-.30,-.756,2.66),(-.47,-.744,2.71)],'LeatherLight',.025)
    a.box(col,'Pouch_Clasp',(-.30,-.78,2.70),(.038,.018,.080),'GoldLight',.007)
    for i in range(2):
        a.band(col,'Right_Lower_Utility_Strap_%d'%i,(.46+i*.12,-.53,2.74),(.50+i*.12,-.56,2.34),.09,'LeatherDark',.045)
        a.box(col,'Right_Lower_Utility_Tab_%d'%i,(.53+i*.12,-.592,2.41+i*.06),(.16,.06,.08),'Leather',.008)
    a.band(col,'Left_Utility_Sling',(-.61,-.50,3.13),(-.79,-.55,2.12),.11,'LeatherDark',.04)
    for i in range(3):
        x=-.59-.053*i; z=2.90-.205*i
        pocket=a.box(col,'Utility_Pocket_%d'%i,(x,-.58,z),(.16,.17,.17),'Leather',.016)
        pocket.rotation_euler.y=-.17
        a.band(col,'Utility_Pocket_Flap_%d'%i,(x-.08,-.68,z+.03),(x+.08,-.68,z+.05),.046,'LeatherLight')
    a.band(col,'Coat_Left_Panel_Strap',(-.50,-.561,2.24),(-.38,-.566,1.98),.10,'Leather')
    a.buckle(col,'Coat_Left_Panel_Buckle',(-.39,-.610,2.04),.15,.17,.12)


def sword(col):
    # The hand holds an unsheathed blade pointed down at screen left.
    top=Vector((-1.155,-.155,2.57))
    tip=Vector((-1.46,-.14,.48))
    direction=(tip-top).normalized()
    across=Vector((direction.z,0,-direction.x)).normalized()
    mid=top+direction*.35
    tipbase=tip-direction*.34
    verts=[tuple(mid+across*.115),tuple(mid-Vector((0,.065,0))),tuple(mid-across*.115),
           tuple(tipbase+across*.095),tuple(tipbase-Vector((0,.045,0))),tuple(tipbase-across*.095),tuple(tip)]
    ob=a.mesh(col,'Sword_Blade',verts,[(0,1,4,3),(1,2,5,4),(3,4,6),(4,5,6)],'Steel')
    ob.data.materials.append(a.mat('SteelLight')); ob.data.materials.append(a.mat('SteelDark'))
    for p in ob.data.polygons:p.material_index=(1,2,1,2)[p.index]
    a.segment(col,'Sword_Grip',top-direction*.10,top+direction*.26,.12,.13,'LeatherDark',.016)
    guard=top+direction*.27
    a.segment(col,'Sword_Guard',guard-across*.29,guard+across*.29,.13,.20,'Leather',.014)
    a.segment(col,'Sword_Guard_Left_Cap',guard-across*.29,guard-across*.21,.17,.23,'SteelLight',.012)
    a.ico(col,'Sword_Pommel',top-direction*.16,(.10,.11,.12),'Leather',1)


def studio(scene):
    col=a.collection(scene,'RA_Studio')
    for name,loc,power,size in [('Key',(-3.5,-4.5,9),1000,4),('Fill',(4,-6,5),160,5),('Rim',(2,3,8),130,4)]:
        data=bpy.data.lights.new('RA_'+name,'AREA')
        data.energy=power;data.size=size;data.shape='DISK'
        obj=bpy.data.objects.new('RA_'+name,data);col.objects.link(obj);obj.location=loc
        obj.rotation_euler=(Vector((0,0,3.5))-obj.location).to_track_quat('-Z','Y').to_euler()
    world=bpy.data.worlds.new('RA_World');world.use_nodes=True;world.node_tree.nodes.clear()
    bg=world.node_tree.nodes.new('ShaderNodeBackground');out=world.node_tree.nodes.new('ShaderNodeOutputWorld')
    bg.inputs['Color'].default_value=(*a.linear('F4F0DB'),1);bg.inputs['Strength'].default_value=.6
    world.node_tree.links.new(bg.outputs['Background'],out.inputs['Surface']);scene.world=world
    data=bpy.data.cameras.new('RA_Camera');data.type='ORTHO';data.ortho_scale=6.85
    cam=bpy.data.objects.new('RA_Camera',data);col.objects.link(cam)
    target=Vector((-.08,0,3.16));yaw=math.radians(-3);elev=math.radians(12)
    cam.location=target+Vector((math.sin(yaw)*math.cos(elev),-math.cos(yaw)*math.cos(elev),math.sin(elev)))*16
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam


def build():
    startup_scenes=list(bpy.data.scenes) if bpy.app.background and not bpy.data.filepath else []
    existing=bpy.data.scenes.get('Reference_Adventurer')
    if existing:
        if len(bpy.data.scenes)==1:bpy.data.scenes.new('RA_Temporary')
        bpy.data.scenes.remove(existing)
    for col in list(bpy.data.collections):
        if col.name.startswith('RA_') and col.users==0:bpy.data.collections.remove(col)
    for ob in list(bpy.data.objects):
        if ob.name.startswith('RA_') and ob.users==0:bpy.data.objects.remove(ob)
    for blocks in (bpy.data.meshes,bpy.data.lights,bpy.data.cameras,bpy.data.worlds):
        for data in list(blocks):
            if data.name.startswith('RA_') and data.users==0:blocks.remove(data)
    scene=bpy.data.scenes.new('Reference_Adventurer');bpy.context.window.scene=scene
    cols={n:a.collection(scene,'RA_'+n) for n in ('Head_Hair','Coat','Arms_Gloves','Scarf','Belts_Pouches','Legs_Boots','Sword')}
    coat(cols['Coat']);legs(cols['Legs_Boots']);arms(cols['Arms_Gloves'])
    scarf(cols['Scarf']);accessories(cols['Belts_Pouches']);sword(cols['Sword'])
    import head
    importlib.reload(head);head.build_head(cols['Head_Hair'])
    ground=a.collection(scene,'RA_Contact_Ground')
    a.mesh(ground,'Ground_Footprint',[(-1.06,-.57,-.012),(-.81,-.85,-.012),(.81,-.85,-.012),
                (1.08,-.52,-.012),(1.04,.55,-.012),(.79,.76,-.012),(-.84,.76,-.012),(-1.08,.51,-.012)],
                [tuple(range(8))],'Ground')
    studio(scene)
    scene.render.engine='CYCLES';scene.cycles.samples=64;scene.cycles.use_denoising=True
    scene.cycles.seed=260927;scene.cycles.use_animated_seed=False;scene.cycles.use_adaptive_sampling=False
    scene.render.resolution_x=768;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.image_settings.color_depth='8'
    scene.render.film_transparent=True;scene.render.use_freestyle=False
    scene.render.threads_mode='FIXED';scene.render.threads=8
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=-1.05
    scene.render.filepath=str(HERE/'ReferenceAdventurer.png')
    scene.view_layers[0].update()
    for path in (Path(__file__),HERE/'head.py',HERE/'artlib.py'):
        key='ReferenceAdventurer/'+path.name
        old=bpy.data.texts.get(key)
        if old:bpy.data.texts.remove(old)
        text=bpy.data.texts.new(key);text.write(path.read_text(encoding='utf-8'))
    texts={t for t in bpy.data.texts if t.name.startswith('ReferenceAdventurer/')}
    reference=bpy.data.images.load(str(HERE/'reference.png'),check_existing=True)
    reference.name='RA_Reference';reference.pack();reference.use_fake_user=True
    for old in startup_scenes:
        if old.name in bpy.data.scenes and old!=scene:bpy.data.scenes.remove(old)
    temporary=bpy.data.scenes.get('RA_Temporary')
    if temporary:bpy.data.scenes.remove(temporary)
    if len(bpy.data.scenes)==1:
        bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'ReferenceAdventurer.blend'),compress=True,copy=True)
    else:
        bpy.data.libraries.write(str(HERE/'ReferenceAdventurer.blend'),{scene,reference}|texts,fake_user=True,compress=True)
    metadata={'reference':'reference.png','interpretation':'faithful standalone reconstruction; reference proportions and colors override previous UI sample palette and 2.5-head rule',
              'palette':a.PALETTE,'scene':scene.name,'render_size':[768,1024],
              'camera':{'type':'ORTHO','yaw_deg':-3,'elevation_deg':12,'scale':6.85},
              'render':{'engine':'CYCLES','samples':64,'denoise':True,'view_transform':'Standard','look':'None','exposure':-1.05},
              'collections':{k:[o.name for o in v.objects] for k,v in cols.items()},
              'reference_scope':'central character plus held sword only; other inventory props excluded'}
    (HERE/'manifest.json').write_text(json.dumps(metadata,indent=2)+'\n',encoding='utf-8')
    print('REFERENCE_ADVENTURER_BUILT',len(scene.objects))


def render():
    scene=bpy.data.scenes['Reference_Adventurer'];bpy.context.window.scene=scene
    bpy.ops.render.render(write_still=True,scene=scene.name)
    print('REFERENCE_ADVENTURER_RENDERED',scene.render.filepath)


if __name__=='__main__':
    action=globals().get('RA_ACTION','all')
    if action in ('build','all'):build()
    if action in ('render','all'):render()

"""Faceted Customer01: shared game colors, layered contemporary clothing.

Blender MCP: runpy.run_path(path, run_name='__main__').
GC_ACTION='build' or 'render' for geometry-preserving re-renders; default='all'.
"""
import importlib
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

HERE = Path(__file__).resolve().parent
COMMON = HERE.parent
for folder in (COMMON, HERE):
    if str(folder) not in sys.path: sys.path.insert(0, str(folder))
import customer_lib as a
import ui_sprite_common as c
importlib.reload(a)
importlib.reload(c)

SEED = 260927
SCENE = 'Game_Customer_Faceted'
OUTPUTS = [('Customer_01_Faceted.png', (164, 280)),
           ('Customer_01_Faceted-large.png', (656, 1120))]


def jacket(col):
    a.loft(col, 'Soda_Jacket_Torso', [(2.04, 0, .04, .49, .30),
            (2.28, 0, .03, .48, .31), (3.13, 0, .04, .64, .32),
            (3.40, 0, .035, .54, .28)], 'Soda')
    # An inset tee remains subordinate to the recognizable soda jacket.
    a.panel(col, 'Cream_Tee', [(-.18,-.326,3.43),(.18,-.326,3.43),
            (.16,-.339,2.10),(-.16,-.339,2.10)], 'Cream', .045)
    for side,s in (('Left',-1),('Right',1)):
        a.panel(col, side+'_Jacket_Front', [(s*.17,-.365,3.32),(s*.51,-.291,3.39),
                (s*.55,-.302,2.85),(s*.49,-.345,2.12),
                (s*.18,-.389,2.06),(s*.13,-.381,2.63)], 'Soda', .06)
        a.band(col, side+'_Front_Zip_Seam', (s*.165,-.400,2.11),
                (s*.15,-.397,3.12), .035, 'Cream', .023)
        a.panel(col, side+'_Hem_Rib', [(s*.18,-.416,2.23),(s*.50,-.369,2.25),
                (s*.49,-.379,2.09),(s*.18,-.420,2.07)], 'Soda', .035)
    # Two thick folded collars use actual planes and bevels to catch the light.
    a.loft(col, 'Hood_Back', [(3.17,0,.12,.53,.34),(3.49,0,.15,.46,.35),
                           (3.54,0,.17,.34,.24)], 'Soda')
    a.panel(col, 'Cream_Collar_Left', [(-.34,-.28,3.50),(-.17,-.36,3.54),
            (-.035,-.418,3.27),(-.19,-.454,3.15),(-.43,-.32,3.39)], 'Cream', .07)
    a.panel(col, 'Cream_Collar_Right', [(.19,-.34,3.55),(.38,-.29,3.48),
            (.35,-.40,3.31),(.16,-.443,3.18),(.045,-.426,3.27)], 'Cream', .07)
    # Badge sits clear of the diagonal strap, retaining Customer01's oval emblem.
    a.ico(col, 'Cream_Oval_Badge', (.33,-.393,2.93), (.111,.027,.096), 'Cream',2)
    a.panel(col, 'Badge_Check_Top', [(.298,-.424,2.98),(.342,-.424,2.98),
            (.342,-.426,2.935),(.298,-.426,2.935)], 'Soda', .01)
    a.panel(col, 'Badge_Check_Bottom', [(.342,-.426,2.935),(.386,-.424,2.935),
            (.386,-.424,2.89),(.342,-.426,2.89)], 'Soda', .01)
    a.band(col, 'Right_Pocket_Opening', (.24,-.412,2.51),(.43,-.375,2.57),.045,'Cream',.02)


def arms(col):
    for side,s in (('Left',-1),('Right',1)):
        a.loft(col, side+'_Upper_Sleeve', [(2.65,s*.77,.015,.175,.19),
                  (2.91,s*.73,.025,.19,.21),(3.21,s*.63,.035,.205,.22),
                  (3.39,s*.51,.04,.145,.19)],'Soda')
        a.segment(col, side+'_Lower_Sleeve', (s*.77,.015,2.70),
                  (s*.82,-.06,2.27),.27,.32,'Soda',.035)
        a.segment(col, side+'_Cream_Cuff', (s*.82,-.06,2.30),
                  (s*.834,-.085,2.15),.285,.33,'Cream',.025)
        a.ico(col, side+'_Hand', (s*.84,-.095,2.035), (.16,.16,.205),'Skin1',1)
        a.ico(col, side+'_Thumb', (s*.73,-.202,2.05), (.07,.075,.105),'Skin1',1)


def pants_shoes(col):
    a.box(col,'Trouser_Waist',(0,.04,2.06),(.84,.50,.30),'Pants1',.045)
    for side,s in (('Left',-1),('Right',1)):
        x=s*.29
        a.loft(col, side+'_Tapered_Trousers',[(.47,x,.025,.19,.21),
            (.72,x,.025,.20,.22),(1.28,x,.045,.235,.245),
            (1.77,x*.86,.04,.248,.25),(2.05,x*.81,.04,.245,.25)], 'Pants1')
        a.panel(col,side+'_Knee_Fold',[(x-.17,-.202,1.10),(x+.16,-.211,1.12),
                (x+.13,-.247,1.01),(x-.16,-.239,1.025)],'Pants1',.018)
        a.loft(col, side+'_Turnup',[(.46,x,.027,.205,.235),(.60,x,.027,.212,.235)],'Pants1')
        a.box(col, side+'_Sock', (x,.01,.385),(.27,.30,.19),'Cream',.026)
        shoe=a.box(col, side+'_Navy_Sneaker', (s*.31,-.115,.205),(.40,.68,.30),'Navy',.052)
        sole=a.box(col, side+'_Cream_Sole', (s*.31,-.115,.075),(.42,.70,.14),'Cream',.03)
        toe=a.box(col, side+'_Cream_Toe', (s*.336,-.32,.227),(.365,.29,.23),'Cream',.05)
        heel=a.box(col, side+'_Soda_Heel_Tab',(s*.297,.115,.285),(.30,.12,.10),'Soda',.02)
        for ob in (shoe,sole,toe,heel): ob.rotation_euler.z=s*.12
        for i,y in enumerate((-.10,-.215)):
            lace=a.box(col,side+'_Wide_Lace_%d'%i,(s*.31,y,.359),(.245,.043,.025),'Cream',.006)
            lace.rotation_euler.z=s*.12


def bag(col):
    # One compact canvas bag replaces the reference's many fantasy utility pouches.
    a.band(col,'Messenger_Strap',(.46,-.463,3.41),(-.48,-.465,2.12),.105,'Wood',.042)
    a.band(col,'Strap_Edge',(.499,-.511,3.415),(-.44,-.513,2.12),.022,'Cream',.022)
    a.buckle(col,'Strap_Adjuster',(.22,-.53,3.07),.17,.20,-.60)
    pouch=a.box(col,'Wood_Messenger_Bag',(-.53,-.376,1.99),(.49,.31,.52),'Wood',.047)
    pouch.rotation_euler.y=-.10
    a.panel(col,'Cream_Bag_Flap',[(-.81,-.56,2.26),(-.30,-.56,2.21),
              (-.315,-.571,2.06),(-.50,-.592,1.99),(-.79,-.582,2.04)],'Cream',.045)
    a.band(col,'Bag_Clasp_Tab',(-.535,-.622,2.17),(-.52,-.63,1.95),.071,'Wood',.02)
    a.box(col,'Gold_Bag_Clasp',(-.528,-.651,2.035),(.069,.032,.056),'Gold',.008)


def build():
    startup=list(bpy.data.scenes) if bpy.app.background and not bpy.data.filepath else []
    old=bpy.data.scenes.get(SCENE)
    if old:
        if len(bpy.data.scenes)==1:bpy.data.scenes.new('GC_Temporary')
        bpy.data.scenes.remove(old)
    for blocks in (bpy.data.collections,bpy.data.objects,bpy.data.meshes,
                   bpy.data.cameras,bpy.data.lights,bpy.data.worlds):
        for data in list(blocks):
            if data.name.startswith('GC_') and data.users==0:blocks.remove(data)
    rig,world=c.studio()
    rig.name='GC_Shared_Studio'
    world.name='GC_Shared_Environment'
    for ob in rig.objects:ob.name='GC_'+ob.name.removeprefix('UI_');ob.data.name=ob.name+'_Data'
    scene=c.setup_scene(SCENE,rig,world,OUTPUTS[0][1],SEED)
    cols={name:a.collection(scene,'GC_'+name) for name in ('Head_Hair','Soda_Jacket','Arms_Hands','Pants_Sneakers','Messenger_Bag')}
    jacket(cols['Soda_Jacket']);arms(cols['Arms_Hands']);pants_shoes(cols['Pants_Sneakers']);bag(cols['Messenger_Bag'])
    import customer_head
    importlib.reload(customer_head)
    customer_head.build_head(cols['Head_Hair'])
    # Angry brows/frown for the Customer_V0_Angry sprite; hidden so the neutral render is unchanged.
    for ob in customer_head.build_angry_face(cols['Head_Hair']):
        ob.hide_render=True;ob.hide_viewport=True
    # Model at half authored scale so the original shared light rig remains valid.
    for col in cols.values():
        for ob in col.objects:ob.location*=.5;ob.scale*=.5
    for col in cols.values():
        for ob in col.objects:
            bm=bmesh.new();bm.from_mesh(ob.data)
            bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
            bm.to_mesh(ob.data);bm.free()
    cam=c.camera(scene,(0,-.015,1.22),2.85,yaw=-20,elevation=15)
    scene.camera.name='GC_Orthographic_Camera';scene.camera.data.name='GC_Camera_Data'
    ground=c.catcher(scene);ground.name='GC_Shadow_Catcher'
    ground.data.name='GC_Shadow_Catcher_Mesh';ground.users_collection[0].name='GC_Ground'
    scene['display_size']=[82,140];scene['seed']=SEED
    scene['variant']=0;scene['style']='faceted reference adapted to contemporary game customer'
    scene.view_layers[0].update()
    for old in startup:
        if old!=scene and old.name in bpy.data.scenes:bpy.data.scenes.remove(old)
    temporary=bpy.data.scenes.get('GC_Temporary')
    if temporary:bpy.data.scenes.remove(temporary)
    texts=set()
    for path in (Path(__file__),HERE/'customer_lib.py',HERE/'customer_head.py',COMMON/'ui_sprite_common.py'):
        name='GameCustomerFaceted/'+path.name
        old=bpy.data.texts.get(name)
        if old:bpy.data.texts.remove(old)
        text=bpy.data.texts.new(name);text.write(path.read_text(encoding='utf-8-sig'));texts.add(text)
    scene.render.filepath=str(HERE/OUTPUTS[0][0])
    blend=HERE/'GameCustomerFaceted.blend'
    if len(bpy.data.scenes)==1:
        bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True,copy=True)
    else:bpy.data.libraries.write(str(blend),{scene}|texts,fake_user=True,compress=True)
    manifest={'scope':'Customer01 neutral style study only; no Unity layout or behavior changes',
        'identity':'Soda clothing, cream oval badge, Hair1 dark hair, Skin1, Pants1 lavender trousers, navy/cream sneakers',
        'style_reference':'../ReferenceAdventurer/ReferenceAdventurer.blend',
        'proportion':'Reference-led longer body (about 3.6 heads); explicit concept variation from older 2.5-head study',
        'palette':c.PALETTE,'lighting':c.LIGHTING,'camera':cam,'seed':SEED,
        'render':{'engine':'CYCLES','samples':64,'denoise':True,'view_transform':'Standard','look':'None',
                  'exposure':c.EXPOSURE,'gamma':1,'film_transparent':True,'color_mode':'RGBA'},
        'display_size':[82,140],'outputs':[{'file':n,'size':list(s)} for n,s in OUTPUTS],
        'shadow':'Real Cycles catcher, navy tint, alpha <=0.32, soft elliptical contact falloff',
        'creation_transport':'Blender MCP execute_blender_code',
        'collections':{k:[o.name for o in v.objects] for k,v in cols.items()}}
    (HERE/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('GAME_CUSTOMER_BUILT',len(scene.objects))


def render():
    import create_ui_sprites as renderer
    importlib.reload(renderer)
    renderer.ART=HERE
    scene=bpy.data.scenes[SCENE];bpy.context.window.scene=scene
    for filename,size in OUTPUTS:
        scene.render.resolution_x,scene.render.resolution_y=size
        entry={'id':Path(filename).stem,'render_size':list(size)}
        renderer.render_contact_sprite(scene,entry,HERE/filename)
        print('GAME_CUSTOMER_RENDERED',filename)
    scene.render.resolution_x,scene.render.resolution_y=OUTPUTS[0][1]
    scene.render.filepath=str(HERE/OUTPUTS[0][0])


if __name__=='__main__':
    action=globals().get('GC_ACTION','all')
    if action in ('build','all'):build()
    if action in ('render','all'):render()

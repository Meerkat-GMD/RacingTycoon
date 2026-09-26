"""Reference-led female explorer, built and rendered through Blender MCP.

EX_ACTION='build' creates geometry; 'render' preserves edits; default='all'.
Coordinate system: Z up, -Y front. Shared game palette and studio.
"""
import importlib
import json
import sys
from pathlib import Path
import bpy
import bmesh

HERE=Path(__file__).resolve().parent
COMMON=HERE.parent
for path in (COMMON,HERE):
    if str(path) not in sys.path:sys.path.insert(0,str(path))
import explorer_lib as a
import ui_sprite_common as c
importlib.reload(a);importlib.reload(c)

SCENE='Game_Customer_Female_Explorer'
SEED=260927
OUTPUTS=[('Customer_02_Explorer.png',(164,280)),('Customer_02_Explorer-large.png',(656,1120))]


def coat(col):
    a.loft(col,'Cream_Jacket_Torso',[(2.57,0,.06,.425,.29),(3.04,0,.055,.47,.30),
                (3.55,0,.07,.59,.33),(3.74,0,.065,.47,.27)],'Cream')
    a.panel(col,'Dark_Inner_Top',[(-.22,-.304,3.67),(.22,-.304,3.67),
              (.225,-.333,2.55),(-.225,-.333,2.55)],'Plum',.045)
    a.loft(col,'Rear_Pink_Coat',[(1.58,0,.15,.65,.35),(2.15,0,.135,.59,.33),
              (2.78,0,.105,.48,.30)],'Strawberry')
    # Cut away the front: two long outer tails frame a separate green skirt.
    obj=bpy.data.objects['EX_Rear_Pink_Coat']
    bm=bmesh.new();bm.from_mesh(obj.data)
    remove=[f for f in bm.faces if f.calc_center_median().y<.13]
    bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(obj.data);bm.free()
    a.loft(col,'Separate_Green_Skirt',[(1.57,0,.015,.51,.31),(1.65,0,.015,.53,.315),
              (2.21,0,.04,.43,.285),(2.62,0,.04,.405,.265)],'Pants3')
    a.panel(col,'Skirt_Front_Facet',[(-.06,-.327,2.54),(.14,-.318,2.54),
              (.22,-.323,1.585),(-.11,-.331,1.585)],'Mint',.022)
    a.loft(col,'Waist_Belt',[(2.57,0,.025,.448,.309),(2.70,0,.025,.444,.309)],'Navy')
    a.buckle(col,'Waist_Buckle',(.03,-.337,2.645),.15,.16)
    for side,s in (('Left',-1),('Right',1)):
        a.panel(col,side+'_Long_Pink_Coat_Tail',[(s*.18,-.374,2.96),(s*.44,-.345,2.94),
              (s*.51,-.37,2.39),(s*.66,-.393,1.68),(s*.26,-.445,1.66),
              (s*.17,-.42,2.16)],'Strawberry',.085)
        a.panel(col,side+'_Tail_Outer_Facet',[(s*.45,-.333,2.92),(s*.54,-.235,2.89),
              (s*.69,-.257,1.73),(s*.63,-.402,1.675)],'Strawberry',.06)
        a.panel(col,side+'_Cream_Coat_Front',[(s*.15,-.382,3.59),(s*.46,-.305,3.64),
              (s*.53,-.328,3.11),(s*.46,-.405,2.66),(s*.18,-.461,2.71),
              (s*.135,-.420,3.17)],'Cream',.08)
        a.band(col,side+'_Plum_Coat_Edge',(s*.18,-.484,2.74),(s*.45,-.432,2.72),.12,'Plum',.035)
    a.panel(col,'Left_Chest_Patch',[(-.42,-.367,3.31),(-.28,-.406,3.30),
              (-.29,-.432,3.07),(-.44,-.403,3.09)],'Strawberry',.025)


def scarf(col):
    a.loft(col,'High_Pink_Scarf',[(3.69,0,-.008,.44,.32),(3.89,0,.015,.405,.31),
              (3.99,0,.026,.35,.28)],'Strawberry')
    a.panel(col,'Scarf_Diagonal_Fold',[(-.425,-.31,3.87),(-.25,-.368,3.92),
              (.40,-.306,3.75),(.36,-.373,3.67),(-.04,-.419,3.76),(-.415,-.351,3.80)],'Strawberry',.052)
    a.band(col,'Scarf_Cream_Upper_Trim',(-.32,-.360,3.943),(.32,-.354,3.888),.047,'Cream',.025)
    a.panel(col,'Scarf_Front_End',[(-.12,-.448,3.80),(.18,-.429,3.80),
              (.23,-.504,3.30),(.055,-.557,3.17),(-.14,-.499,3.36)],'Strawberry',.052)
    for i,(x,z) in enumerate(((-.04,3.67),(.10,3.51))):
        a.box(col,'Scarf_Cream_Check_%d'%i,(x,-.552,z),(.145,.034,.11),'Cream',.008)
    a.band(col,'Scarf_Gold_Fastening',(-.032,-.587,3.38),(.035,-.58,3.18),.07,'Gold',.022)


def legs_boots(col):
    for side,s in (('Left',-1),('Right',1)):
        x=s*.275
        a.loft(col,side+'_Bare_Leg',[(.49,x,-.005,.132,.139),(.72,x,.0,.125,.137),
            (1.02,x*.96,.023,.132,.152),(1.24,x*.90,.038,.154,.165),
            (1.64,x*.85,.048,.164,.175),(1.80,x*.83,.05,.166,.18)],'Skin1')
        a.loft(col,side+'_Dark_Boot_Shaft',[(.16,x,-.005,.16,.195),(.52,x,.015,.174,.215)],'Pants3')
        a.box(col,side+'_Boot_Sole',(x,-.126,.055),(.40,.66,.105),'Navy',.018)
        a.box(col,side+'_Boot_Foot',(x,-.11,.16),(.365,.62,.25),'Pants3',.027)
        a.box(col,side+'_Pink_Toe',(x,-.31,.174),(.365,.29,.195),'Strawberry',.03)
        a.band(col,side+'_Toe_Cream_Trim',(x-.151,-.30,.286),(x+.151,-.30,.286),.031,'Cream',.075)
        a.loft(col,side+'_Pink_Boot_Wrap',[(.31,x,.02,.219,.234),(.51,x,.02,.215,.236)],'Strawberry')
        a.loft(col,side+'_Mint_Folded_Cuff',[(.505,x,.02,.225,.245),(.675,x,.02,.229,.25)],'Mint')
        a.band(col,side+'_Cuff_Split',(x,-.238,.52),(x,-.245,.657),.025,'Pants3',.016)


def arms(col):
    a.loft(col,'Left_Cream_Upper_Sleeve',[(2.87,-.72,.01,.185,.21),(3.16,-.68,.02,.20,.225),
              (3.56,-.56,.06,.218,.225),(3.71,-.465,.07,.17,.195)],'Cream')
    a.panel(col,'Left_Plum_Shoulder',[(-.44,-.132,3.68),(-.61,-.19,3.66),
              (-.79,-.167,3.48),(-.65,-.241,3.44),(-.53,-.214,3.53)],'Plum',.04)
    a.segment(col,'Left_Forearm_Sleeve',(-.72,.01,2.93),(-.74,-.10,2.38),.33,.39,'Cream',.031)
    a.segment(col,'Left_Plum_Sleeve_Band',(-.735,-.05,2.62),(-.739,-.069,2.48),.344,.408,'Plum',.009)
    a.segment(col,'Left_Green_Cuff',(-.742,-.10,2.40),(-.747,-.119,2.25),.34,.405,'Pants3',.017)
    a.box(col,'Left_Hand_Down',(-.744,-.13,2.11),(.24,.245,.29),'Skin1',.043)
    a.box(col,'Left_Thumb',(-.592,-.192,2.15),(.095,.125,.17),'Skin1',.018)
    # Asymmetrical pose: the opposite hand curls around the backpack strap.
    a.segment(col,'Right_Upper_Sleeve',(.55,.02,3.61),(.75,-.035,2.98),.37,.43,'Cream',.036)
    a.segment(col,'Right_Plum_Elbow',(.75,-.035,3.00),(.746,-.09,2.87),.395,.45,'Plum',.028)
    a.segment(col,'Right_Raised_Forearm',(.74,-.09,2.91),(.37,-.59,3.42),.325,.355,'Cream',.025)
    a.segment(col,'Right_Cream_Cuff',(.405,-.55,3.36),(.32,-.656,3.49),.355,.38,'Cream',.027)
    a.box(col,'Right_Hand_On_Strap',(.285,-.692,3.565),(.235,.175,.235),'Skin1',.038)
    a.box(col,'Right_Grip_Thumb',(.183,-.732,3.54),(.095,.092,.145),'Skin3',.016)
    for i in range(2):
        a.band(col,'Right_Grip_Finger_%d'%i,(.235,-.794,3.58+i*.05),(.351,-.787,3.58+i*.05),.028,'Skin1',.018)


def backpack(col):
    # A tall rigid pack projects to camera-left, as in the provided reference.
    a.box(col,'Wood_Backpack',(-.43,.48,2.875),(1.12,.64,1.54),'Wood',.055)
    a.box(col,'Backpack_Dark_Side',(-1.0,.48,2.875),(.095,.61,1.45),'Hair2',.02)
    a.box(col,'Backpack_Left_Edge',(-1.031,.47,2.86),(.074,.68,1.50),'Gold',.016)
    a.box(col,'Backpack_Bottom_Edge',(-.43,.482,2.10),(1.16,.68,.09),'Gold',.014)
    a.box(col,'Backpack_Top_Flap',(-.43,.463,3.60),(1.17,.705,.20),'Wood',.039)
    # Broad paper/fabric patches remain legible without fine texture noise.
    for i,(y,z,h,w) in enumerate(((.29,3.41,.13,.25),(.45,3.19,.21,.27),
                                (.30,2.86,.25,.19),(.54,2.50,.24,.18),(.33,2.23,.15,.21))):
        a.box(col,'Pack_Side_Cream_Patch_%02d'%i,(-1.083,y,z),(.019,w,h),'Cream',.003)
    for s in (-1,1):
        x=s*.37
        # Angular curved arch wraps from the pack over each shoulder.
        points=[(x,.60,3.59),(x,.38,3.82),(x,.05,3.90),
                (x,-.30,3.76),(x,-.45,3.50),(x,-.466,2.92)]
        for i in range(len(points)-1):
            a.segment(col,('Left' if s<0 else 'Right')+'_Backpack_Strap_%d'%i,
                      points[i],points[i+1],.14,.080,'Gold',.009)
        a.buckle(col,('Left' if s<0 else 'Right')+'_Pack_Buckle',(x,-.519,3.18),.16,.20)
    a.box(col,'Right_Wood_Hip_Pouch',(.62,-.20,2.43),(.34,.29,.44),'Wood',.035)
    a.panel(col,'Pouch_Cream_Flap',[(.44,-.365,2.65),(.81,-.365,2.65),
              (.79,-.39,2.49),(.63,-.411,2.44),(.45,-.39,2.49)],'Cream',.032)


def build():
    startup=list(bpy.data.scenes) if bpy.app.background and not bpy.data.filepath else []
    old=bpy.data.scenes.get(SCENE)
    if old:
        if len(bpy.data.scenes)==1:bpy.data.scenes.new('EX_Temporary')
        bpy.data.scenes.remove(old)
    for blocks in (bpy.data.collections,bpy.data.objects,bpy.data.meshes,bpy.data.cameras,bpy.data.lights,bpy.data.worlds):
        for data in list(blocks):
            if data.name.startswith('EX_') and data.users==0:blocks.remove(data)
    rig,world=c.studio();rig.name='EX_Shared_Studio';world.name='EX_Shared_Environment'
    for ob in rig.objects:ob.name='EX_'+ob.name.removeprefix('UI_');ob.data.name=ob.name+'_Data'
    scene=c.setup_scene(SCENE,rig,world,OUTPUTS[0][1],SEED)
    cols={n:a.collection(scene,'EX_'+n) for n in ('Head_Cap_Eyes','Layered_Coat_Skirt','Pink_Scarf','Arms_Pose','Legs_Boots','Backpack')}
    coat(cols['Layered_Coat_Skirt']);scarf(cols['Pink_Scarf']);arms(cols['Arms_Pose'])
    legs_boots(cols['Legs_Boots']);backpack(cols['Backpack'])
    import explorer_head
    importlib.reload(explorer_head);explorer_head.build_head(cols['Head_Cap_Eyes'])
    # Angry brows/frown for the Customer_V2_Angry sprite; hidden so the neutral render is unchanged.
    for ob in explorer_head.build_angry_face(cols['Head_Cap_Eyes']):
        ob.hide_render=True;ob.hide_viewport=True
    for col in cols.values():
        for ob in col.objects:
            ob.location*=.48;ob.scale*=.48
            bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
            bm.to_mesh(ob.data);bm.free()
    cam=c.camera(scene,(-.035,.015,1.275),2.97,yaw=-20,elevation=15)
    scene.camera.name='EX_Orthographic_Camera';scene.camera.data.name='EX_Camera_Data'
    floor=c.catcher(scene);floor.name='EX_Shadow_Catcher';floor.data.name='EX_Shadow_Catcher_Mesh'
    floor.users_collection[0].name='EX_Ground'
    scene['display_size']=[82,140];scene['seed']=SEED
    scene['variant']=1;scene['style']='Reference-led explorer female with male-matched dot eyes and backpack'
    scene.view_layers[0].update()
    for old in startup:
        if old!=scene and old.name in bpy.data.scenes:bpy.data.scenes.remove(old)
    temp=bpy.data.scenes.get('EX_Temporary')
    if temp:bpy.data.scenes.remove(temp)
    texts=set()
    for path in (Path(__file__),HERE/'explorer_lib.py',HERE/'explorer_head.py',COMMON/'ui_sprite_common.py'):
        name='GameCustomerFemaleExplorer/'+path.name
        old=bpy.data.texts.get(name)
        if old:bpy.data.texts.remove(old)
        t=bpy.data.texts.new(name);t.write(path.read_text(encoding='utf-8-sig'));texts.add(t)
    ref=bpy.data.images.load(str(HERE/'reference.png'),check_existing=True)
    ref.name='EX_Attached_Reference';ref.pack();ref.use_fake_user=True
    scene.render.filepath=str(HERE/OUTPUTS[0][0]);blend=HERE/'GameCustomerFemaleExplorer.blend'
    if len(bpy.data.scenes)==1:bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True,copy=True)
    else:bpy.data.libraries.write(str(blend),{scene,ref}|texts,fake_user=True,compress=True)
    manifest={'scope':'Female character redesign based on new attached explorer reference; no Unity behavior changes',
        'reference':'reference.png','preserved_cues':['small Navy dot eyes matching approved male','cream cap and warm visor','pink scarf',
             'cream jacket and separate long pink tails','green skirt','bare legs and folded boots','large brown backpack','raised hand gripping strap'],
        'palette':c.PALETTE,'lighting':c.LIGHTING,'camera':cam,'seed':SEED,
        'hair_revision':'Connected dark scalp, swept forehead tufts and twelve curved tapered locks; exposed ears and short nape',
        'eye_revision':{'style':'Approved male chamfered Navy dot eyes; no whites, iris, highlights, lashes or brows',
            'reference':'../GameCustomerFaceted/customer_head.py',
            'author_dimensions':[.075,.020,.106],'author_spacing':.410,'author_bevel':.006,
            'author_center_yz':[-.401,4.313],'model_scale':.48,'palette':'Navy #29324D'},
        'render':{'engine':'CYCLES','samples':64,'denoise':True,'view_transform':'Standard','look':'None','exposure':c.EXPOSURE,
             'gamma':1,'film_transparent':True,'color_mode':'RGBA'},'display_size':[82,140],
        'outputs':[{'file':n,'size':list(s)} for n,s in OUTPUTS],
        'shadow':'Real Cycles contact catcher; navy tinted soft ellipse, maximum alpha 0.32',
        'creation_transport':'Blender MCP execute_blender_code',
        'collections':{k:[o.name for o in v.objects] for k,v in cols.items()}}
    (HERE/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('EXPLORER_BUILT',len(scene.objects))


def render():
    import create_ui_sprites as renderer
    importlib.reload(renderer);renderer.ART=HERE
    scene=bpy.data.scenes[SCENE];bpy.context.window.scene=scene
    for filename,size in OUTPUTS:
        scene.render.resolution_x,scene.render.resolution_y=size
        renderer.render_contact_sprite(scene,{'id':Path(filename).stem,'render_size':list(size)},HERE/filename)
        print('EXPLORER_RENDERED',filename)
    scene.render.resolution_x,scene.render.resolution_y=OUTPUTS[0][1]
    scene.render.filepath=str(HERE/OUTPUTS[0][0])


if __name__=='__main__':
    action=globals().get('EX_ACTION','all')
    if action in ('build','all'):build()
    if action in ('render','all'):render()

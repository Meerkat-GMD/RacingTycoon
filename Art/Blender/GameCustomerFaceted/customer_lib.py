"""Faceted game customer helpers using the shared game palette. Z up, -Y front."""
import math
import bpy
from mathutils import Vector

from pathlib import Path
import sys
COMMON = Path(__file__).resolve().parent.parent
if str(COMMON) not in sys.path: sys.path.insert(0, str(COMMON))
from ui_sprite_common import PALETTE as SHARED_PALETTE
PALETTE = {k: v.lstrip('#') for k, v in SHARED_PALETTE.items()}

def linear(value):
    rgb=[int(value[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb)

def mat(name):
    key='GC_'+name
    m=bpy.data.materials.get(key)
    if m and m.get('palette_srgb')=='#'+PALETTE[name]: return m
    if not m:m=bpy.data.materials.new(key)
    m.use_nodes=True
    m.node_tree.nodes.clear()
    shader=m.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    shader.name='Principled BSDF'
    shader.inputs['Base Color'].default_value=(*linear(PALETTE[name]),1)
    shader.inputs['Roughness'].default_value=.85
    shader.inputs['Specular IOR Level'].default_value=.08
    out=m.node_tree.nodes.new('ShaderNodeOutputMaterial')
    m.node_tree.links.new(shader.outputs['BSDF'],out.inputs['Surface'])
    m.diffuse_color=(*linear(PALETTE[name]),1)
    m['palette_srgb']='#'+PALETTE[name]
    return m

def collection(scene,name):
    col=bpy.data.collections.new(name)
    scene.collection.children.link(col)
    return col

def place(obj,col,name,loc,material):
    obj.name='GC_'+name
    if obj.data: obj.data.name=obj.name+'_Data'
    for old in list(obj.users_collection): old.objects.unlink(obj)
    col.objects.link(obj)
    obj.location=loc
    if material: obj.data.materials.append(mat(material))
    if obj.type=='MESH':
        for p in obj.data.polygons: p.use_smooth=False
    return obj

def mesh(col,name,verts,faces,material):
    data=bpy.data.meshes.new('GC_'+name+'_Mesh')
    data.from_pydata(verts,[],faces)
    data.update()
    obj=bpy.data.objects.new('GC_'+name,data)
    col.objects.link(obj)
    if material: data.materials.append(mat(material))
    return obj

def box(col,name,loc,dims,material,bevel=.015):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1)
    obj=place(bpy.context.object,col,name,loc,material)
    obj.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=obj.modifiers.new('Single flat chamfer','BEVEL')
        mod.width=bevel; mod.segments=1
    return obj

def ico(col,name,loc,scale,material,subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions,radius=1)
    obj=place(bpy.context.object,col,name,loc,material)
    obj.scale=scale
    return obj

def segment(col,name,a,b,width,depth,material,bevel=.01):
    a,b=Vector(a),Vector(b)
    obj=box(col,name,(a+b)/2,(width,depth,(b-a).length),material,bevel)
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return obj

def loft(col,name,rings,material,sides=8):
    # Each ring: z, centerX, centerY, radiusX, radiusY. Front vertex starts -Y.
    verts=[]
    for z,x,y,rx,ry in rings:
        verts.extend((x+rx*math.sin(i*math.tau/sides),y-ry*math.cos(i*math.tau/sides),z) for i in range(sides))
    faces=[tuple(reversed(range(sides)))]
    for r in range(len(rings)-1):
        for i in range(sides):
            faces.append((r*sides+i,r*sides+(i+1)%sides,(r+1)*sides+(i+1)%sides,(r+1)*sides+i))
    faces.append(tuple(range((len(rings)-1)*sides,len(rings)*sides)))
    return mesh(col,name,verts,faces,material)

def panel(col,name,outline,material,depth=.035):
    # xyz front contour. Flat cloth slab, independent editable faces.
    n=len(outline)
    verts=list(outline)+[(x,y+depth,z) for x,y,z in outline]
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(col,name,verts,faces,material)

def band(col,name,a,b,width,material,depth=.035):
    # Flat strap in XZ plane; consistently keeps its face toward the camera.
    a,b=Vector(a),Vector(b)
    d=(b-a).normalized()
    n=Vector((-d.z,0,d.x))*width/2
    return panel(col,name,[tuple(a-n),tuple(a+n),tuple(b+n),tuple(b-n)],material,depth)

def buckle(col,name,center,width,height,angle=0):
    cx,cy,cz=center
    def p(x,z):
        return (cx+x*math.cos(angle)-z*math.sin(angle),cy,cz+x*math.sin(angle)+z*math.cos(angle))
    width*=.8; height*=.8
    border=.025
    outer=[p(-width/2,-height/2),p(width/2,-height/2),p(width/2,height/2),p(-width/2,height/2)]
    inner=[p(-width/2+border,-height/2+border),p(width/2-border,-height/2+border),
           p(width/2-border,height/2-border),p(-width/2+border,height/2-border)]
    verts=outer+inner+[(x,y+.03,z) for x,y,z in outer+inner]
    faces=[]
    for i in range(4):
        j=(i+1)%4
        faces += [(i,j,j+4,i+4),(i+8,i+12,j+12,j+8),(i,i+8,j+8,j),(i+4,j+4,j+12,i+12)]
    mesh(col,name+'_Frame',verts,faces,'Gold')
    band(col,name+'_Pin',p(-width*.05,-height/2+border),p(width*.10,height/2-border),.018,'Gold',.022)


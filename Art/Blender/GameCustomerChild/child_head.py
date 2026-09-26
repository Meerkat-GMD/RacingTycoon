"""Child customer head: chubby faceted face, Hair2 bowl cut with a cowlick.

Standalone part builder for create_child.py. Z is up, -Y faces forward. Every
part is authored in the approved male head's coordinates (face rings from z 3.55
to 4.375, dot eyes .075 x .020 x .106 at x = +-.205), so the adult eye, blush and
mouth rules apply unchanged at head scale 1. `_place` then scales the whole head
(eyes included) by HEAD_SCALE about the chin and lowers it onto the child's
shoulders, so build_head and build_angry_face return objects in the child's
authored body coordinates; the caller applies the .5 model scale.
"""
import math
import sys
from pathlib import Path

from mathutils import Vector

ART = Path(__file__).resolve().parent.parent
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
from toy_kit import Kit

kit = Kit('CH_')
HEAD_SCALE = 1.0               # adult face size on a short body: ~3 heads tall
HEAD_PIVOT = (0, 0, 3.55)      # chin of the male face rings
HEAD_TARGET = (0, 0, 2.29)     # chin of the child


def _place(objects):
    """Scale the head parts about the chin by HEAD_SCALE and lower them onto the body."""
    pivot, target = Vector(HEAD_PIVOT), Vector(HEAD_TARGET)
    for ob in objects:
        ob.location = (ob.location-pivot)*HEAD_SCALE+target
        ob.scale = ob.scale*HEAD_SCALE
    return objects


def _flat_mark(col, name, outline, color, depth=.012):
    """A shallow closed polygon for graphic facial marks without roundness."""
    n = len(outline)
    verts = list(outline) + [(x, y+depth, z) for x, y, z in outline]
    area = sum(outline[i][0]*outline[(i+1) % n][2]
               - outline[(i+1) % n][0]*outline[i][2] for i in range(n))
    order = list(range(n)) if area > 0 else list(reversed(range(n)))
    faces = [tuple(order), tuple(i+n for i in reversed(order))]
    faces += [(order[(i+1) % n], order[i], order[i]+n,
               order[(i+1) % n]+n) for i in range(n)]
    return kit.mesh(col, name, verts, faces, color)


def _lock(col, name, outline, ridge, depth=.15):
    """A solid Hair2 wedge: four broad planes meeting at a ridge in front of the outline."""
    n = len(outline)
    verts = list(outline) + [ridge] + [(x, y+depth, z) for x, y, z in outline]
    area = sum(outline[i][0]*outline[(i+1) % n][2]
               - outline[(i+1) % n][0]*outline[i][2] for i in range(n))
    order = list(range(n)) if area > 0 else list(reversed(range(n)))
    faces = [(n, order[i], order[(i+1) % n]) for i in range(n)]
    faces.append(tuple(i+n+1 for i in reversed(order)))
    faces += [(order[(i+1) % n], order[i], order[i]+n+1,
               order[(i+1) % n]+n+1) for i in range(n)]
    return kit.mesh(col, name, verts, faces, 'Hair2')


def _hair_clump(col, name, rows, tip):
    """Curved tapered solid lock (the approved explorer's technique) for the cowlick."""
    profile = [(-.50, 0), (-.29, -.68), (.055, -1), (.36, -.64), (.50, 0), (0, .34)]
    verts = []
    for i, (x, y, z, width, thickness) in enumerate(rows):
        previous = Vector(rows[max(0, i-1)][:3])
        following = Vector(rows[i+1][:3] if i+1 < len(rows) else tip)
        tangent = following-previous
        across = Vector((-tangent.z, 0, tangent.x)).normalized()
        for u, v in profile:
            verts.append(tuple(Vector((x, y, z))+across*(u*width)+Vector((0, v*thickness, 0))))
    faces = [tuple(reversed(range(6)))]
    for r in range(len(rows)-1):
        for j in range(6):
            faces.append((r*6+j, r*6+(j+1) % 6, (r+1)*6+(j+1) % 6, (r+1)*6+j))
    end = len(verts)
    verts.append(tip)
    for j in range(6):
        faces.append(((len(rows)-1)*6+j, (len(rows)-1)*6+(j+1) % 6, end))
    return kit.mesh(col, 'Hair_'+name, verts, faces, 'Hair2')


def _face(col):
    """Male face rings with fuller cheeks; Skin2 on every plane, Hair2 only for the neck shadow."""
    profile = [(-.355, -.410), (.355, -.410), (.510, -.207),
               (.445, .220), (.240, .352), (-.240, .352),
               (-.445, .220), (-.510, -.207)]
    rings = [(3.550, .70, .90), (3.690, .96, 1),
             (4.045, 1, 1), (4.375, .93, .96)]
    verts = [(x*sx, y*sy+.030, z) for z, sx, sy in rings for x, y in profile]
    faces = [tuple(reversed(range(8)))]
    for j in range(3):
        for i in range(8):
            faces.append((j*8+i, j*8+(i+1) % 8, (j+1)*8+(i+1) % 8, (j+1)*8+i))
    faces.append(tuple(range(24, 32)))
    parts = [kit.mesh(col, 'Head_ChubbyFace', verts, faces, 'Skin2'),
             kit.box(col, 'Head_Neck_Shadow', (0, .030, 3.485), (.370, .360, .270), 'Hair2', .018)]
    for side, suffix in ((-1, 'L'), (1, 'R')):
        parts.append(_flat_mark(col, 'Head_Ear_' + suffix,
                                [(side*.455, -.213, 4.070), (side*.554, -.208, 4.055),
                                 (side*.566, -.211, 3.887), (side*.491, -.220, 3.830),
                                 (side*.444, -.233, 3.883)], 'Skin2', .13))
        # The adult dot eye: about 2 by 3 pixels at the 82x140 display size.
        parts.append(kit.box(col, 'Face_Eye_' + suffix, (side*.205, -.395, 3.983),
                             (.075, .020, .106), 'Navy', .006))
        parts.append(_flat_mark(col, 'Face_Blush_' + suffix,
                                [(side*.245, -.395, 3.867), (side*.363, -.389, 3.867),
                                 (side*.354, -.390, 3.824), (side*.253, -.395, 3.824)],
                                'Strawberry', .010))
    parts.append(_flat_mark(col, 'Face_Smile',
                            [(-.057, -.399, 3.795), (-.038, -.400, 3.792),
                             (-.025, -.402, 3.777), (.025, -.402, 3.777),
                             (.038, -.400, 3.792), (.057, -.399, 3.795),
                             (.039, -.401, 3.760), (-.039, -.401, 3.760)],
                            'Navy', .009))
    return parts


def _bowl(col):
    """A rounded faceted dome with a blunt fringe and a small cowlick at the crown."""
    low = [(0, -.41, 4.32), (.40, -.33, 4.27), (.565, .03, 4.05), (.45, .37, 3.88),
           (0, .48, 3.80), (-.45, .37, 3.88), (-.565, .03, 4.05), (-.40, -.33, 4.25)]
    mid = [(0, -.45, 4.50), (.45, -.33, 4.47), (.615, .03, 4.38), (.48, .41, 4.30),
           (0, .53, 4.29), (-.48, .41, 4.30), (-.615, .03, 4.38), (-.45, -.33, 4.47)]
    top = [(0, -.27, 4.66), (.29, -.20, 4.65), (.41, .03, 4.62), (.31, .29, 4.59),
           (0, .37, 4.58), (-.31, .29, 4.59), (-.41, .03, 4.62), (-.29, -.20, 4.65)]
    verts = low+mid+top+[(-.03, .04, 4.72)]
    faces = [tuple(reversed(range(8)))]
    for j in range(2):
        for i in range(8):
            faces.append((j*8+i, j*8+(i+1) % 8, (j+1)*8+(i+1) % 8, (j+1)*8+i))
    faces += [(16+i, 16+(i+1) % 8, 24) for i in range(8)]
    parts = [kit.mesh(col, 'Hair_BowlDome', verts, faces, 'Hair2')]

    # Four blunt fringe wedges cut straight above the brow band (brows top out near z 4.15);
    # a shallow overhang keeps the forehead lit so the angry brows read on Skin2.
    fringe = [
        ('Hair_Fringe_OuterL', [(-.48, -.33, 4.47), (-.23, -.395, 4.52), (-.235, -.41, 4.245), (-.47, -.36, 4.265)],
         (-.36, -.46, 4.39)),
        ('Hair_Fringe_InnerL', [(-.26, -.395, 4.53), (.015, -.405, 4.54), (.01, -.415, 4.23), (-.25, -.412, 4.235)],
         (-.12, -.47, 4.41)),
        ('Hair_Fringe_InnerR', [(-.015, -.405, 4.54), (.26, -.395, 4.53), (.25, -.412, 4.24), (-.01, -.415, 4.225)],
         (.12, -.47, 4.41)),
        ('Hair_Fringe_OuterR', [(.23, -.395, 4.52), (.48, -.33, 4.47), (.47, -.36, 4.27), (.235, -.41, 4.25)],
         (.36, -.46, 4.39)),
    ]
    for name, outline, ridge in fringe:
        parts.append(_lock(col, name, outline, ridge))
    parts.append(_hair_clump(col, 'Cowlick', [
        (.02, .10, 4.68, .15, .060), (.05, .08, 4.765, .13, .055),
        (.035, .04, 4.835, .10, .045), (-.015, -.01, 4.88, .065, .030)],
        (-.085, -.05, 4.89)))
    return parts


def build_head(col):
    """Face, neck shadow, ears, dot eyes, blush, smile and the bowl cut, placed on the child."""
    return _place(_face(col)+_bowl(col))


def _brow_outline(side, center, y, length=.14, thickness=.045, tilt=25):
    """Corners of a straight brow slab whose inner end (toward the nose) sits lower."""
    t = math.radians(tilt)
    cx, cz = center
    dx, dz = side*math.cos(t)*length/2, math.sin(t)*length/2
    nx, nz = -side*math.sin(t)*thickness/2, math.cos(t)*thickness/2
    return [(cx-dx-nx, y, cz-dz-nz), (cx+dx-nx, y, cz+dz-nz),
            (cx+dx+nx, y, cz+dz+nz), (cx-dx+nx, y, cz-dz+nz)]


def build_angry_face(col):
    """Angry-sprite face marks: the adults' tilted Navy brows and down-turned frown.

    Returned in the child's authored body coordinates; the caller applies the
    .5 model scale and hides the smile. The brows are a little longer and
    thicker (.15 x .055) than the adults' so Navy still reads on the darker Skin2.
    """
    marks = []
    for side, suffix in ((-1, 'L'), (1, 'R')):
        marks.append(_flat_mark(col, 'Face_Angry_Brow_' + suffix,
                                _brow_outline(side, (side*.205, 4.105), -.392, .15, .055),
                                'Navy', .012))
    marks.append(_flat_mark(col, 'Face_Angry_Frown',
                            [(-.057, -.399, 3.779), (-.028, -.401, 3.797),
                             (.028, -.401, 3.797), (.057, -.399, 3.779),
                             (.057, -.399, 3.759), (.028, -.401, 3.777),
                             (-.028, -.401, 3.777), (-.057, -.399, 3.759)],
                            'Navy', .009))
    return _place(marks)

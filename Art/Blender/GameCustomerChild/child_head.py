"""Child customer head: chubby faceted face, short Hair2 wedge-lock haircut with a fringe.

Standalone part builder for create_child.py. Z is up, -Y faces forward. Every
part is authored in the approved male head's coordinates (face rings from z 3.55
to 4.375, dot eyes .075 x .020 x .106 at x = +-.205), so the adult eye, blush and
mouth rules apply unchanged at head scale 1. `_place` then scales the whole head
(eyes included) by HEAD_SCALE about the chin and lowers it onto the child's
shoulders, so build_head and build_angry_face return objects in the child's
authored body coordinates; the caller applies the .5 model scale.

Above the eyes the face's front plane leans back FOREHEAD_TILT degrees. The shared
key light comes from high above, so this forehead facet renders brighter than the
upright Skin2 cheeks; with the downward-turned Navy brow blocks this is what lets
the angry brows read on Skin2 as strongly as on the adults' Skin1.
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


FOREHEAD_TILT = 28             # degrees the forehead facet (z 4.03 -> 4.375) leans back
_CROWN_FRONT = (.41-.345*math.tan(math.radians(FOREHEAD_TILT)))/.41
# Face rings, chin to crown: (z, x scale, y scale of the front half, y scale of the back half).
FACE_RINGS = [(3.550, .70, .90, .90), (3.690, .96, 1, 1), (4.030, 1, 1, 1), (4.375, .93, _CROWN_FRONT, .96)]
# Front plane y of the approved male's upright face, against which the hair locks were drawn.
MALE_FRONT = [(3.550, -.41*.89+.030), (3.680, -.380), (4.045, -.380), (4.375, -.41*.96+.030)]
FACE_FRONT = [(z, -.41*front+.030) for z, _, front, _ in FACE_RINGS]
HAIR_FOLLOW_TOP = 4.25         # fringe tips: above this the hair moves back rigidly


def _front_y(z, line):
    """Y of the flat front plane of the face at height z, from (z, y) pairs (clamped at the ends)."""
    if z <= line[0][0]:
        return line[0][1]
    for (z0, y0), (z1, y1) in zip(line, line[1:]):
        if z <= z1:
            return y0+(y1-y0)*(z-z0)/(z1-z0)
    return line[-1][1]


def face_y(z):
    return _front_y(z, FACE_FRONT)


def _follow_forehead(verts):
    """Move the front hair vertices back as far as the leaning forehead moved at their height.

    Vertices at y <= -.30 move fully, those at y >= -.05 (crown and back) stay; the shift
    stops growing at HAIR_FOLLOW_TOP so the fringe keeps its drawn shape and only its tips
    are brought onto the forehead.
    """
    out = []
    for x, y, z in verts:
        h = min(z, HAIR_FOLLOW_TOP)
        weight = min(1, max(0, (-.05-y)/.25))
        out.append((x, y+weight*(face_y(h)-_front_y(h, MALE_FRONT)), z))
    return out


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
    """A solid Hair2 wedge: broad planes meeting at a ridge in front of the outline (male style)."""
    n = len(outline)
    verts = _follow_forehead(list(outline) + [ridge] + [(x, y+depth, z) for x, y, z in outline])
    area = sum(outline[i][0]*outline[(i+1) % n][2]
               - outline[(i+1) % n][0]*outline[i][2] for i in range(n))
    order = list(range(n)) if area > 0 else list(reversed(range(n)))
    faces = [(n, order[i], order[(i+1) % n]) for i in range(n)]
    faces.append(tuple(i+n+1 for i in reversed(order)))
    faces += [(order[(i+1) % n], order[i], order[i]+n+1,
               order[(i+1) % n]+n+1) for i in range(n)]
    return kit.mesh(col, name, verts, faces, 'Hair2')


def _face(col):
    """Male face rings with fuller cheeks and a leaning forehead; Skin2 everywhere, Hair2 neck shadow."""
    profile = [(-.355, -.410), (.355, -.410), (.510, -.207),
               (.445, .220), (.240, .352), (-.240, .352),
               (-.445, .220), (-.510, -.207)]
    verts = [(x*sx, y*(front if y < 0 else back)+.030, z)
             for z, sx, front, back in FACE_RINGS for x, y in profile]
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


def _scalp(col):
    """Faceted Hair2 scalp under the locks: the approved male's cap rings, hugging the head."""
    low = [(0, -.357, 4.265), (.372, -.297, 4.238), (.515, .024, 3.963), (.386, .318, 3.915),
           (0, .434, 3.916), (-.390, .320, 3.943), (-.521, .027, 3.984), (-.377, -.292, 4.241)]
    high = [(-.060, -.288, 4.600), (.287, -.207, 4.560), (.468, .039, 4.427), (.316, .319, 4.478),
            (-.012, .394, 4.570), (-.328, .299, 4.600), (-.469, .018, 4.491), (-.348, -.211, 4.600)]
    verts = low + high + [(-.080, .038, 4.700)]
    faces = []
    for i in range(8):
        faces.append((i, (i+1) % 8, (i+1) % 8+8, i+8))
        faces.append((i+8, (i+1) % 8+8, 16))
    return kit.mesh(col, 'Hair_Scalp', verts, faces, 'Hair2')


# Short child cut in the approved male's wedge-lock style (customer_head._lock): a low,
# rounded crown of broad clumps, side locks that stop over the tops of the ears, and a
# fringe of four chunky locks whose tips sweep toward +x above the brows. Outlines are
# drawn facing -Y against the male's upright face; _lock moves their front vertices back
# onto the leaning forehead. CROWN_LOCKS: (name, outline, ridge, depth); FRINGE_LOCKS use depth .15.
CROWN_LOCKS = [
    ('Hair_Crown_Back', [(-.50, .14, 4.58), (-.28, .14, 4.74), (.05, .14, 4.79), (.34, .14, 4.73),
                         (.52, .14, 4.56), (.30, -.02, 4.52), (-.30, -.02, 4.52)], (0, -.10, 4.74), .22),
    ('Hair_Crown_Left', [(-.62, .04, 4.58), (-.44, -.02, 4.70), (-.22, -.08, 4.78), (-.10, -.24, 4.62),
                         (-.44, -.22, 4.44)], (-.32, -.31, 4.64), .17),
    ('Hair_Crown_Mid', [(-.26, -.06, 4.80), (-.02, .02, 4.88), (.24, -.08, 4.78), (.18, -.28, 4.60),
                        (-.14, -.28, 4.62)], (0, -.35, 4.72), .17),
    ('Hair_Crown_Right', [(.14, -.06, 4.79), (.40, -.02, 4.72), (.64, .06, 4.58), (.46, -.22, 4.44),
                          (.12, -.26, 4.60)], (.32, -.32, 4.63), .17),
    ('Hair_Side_Left', [(-.62, 0, 4.48), (-.44, -.20, 4.46), (-.47, -.26, 4.12), (-.55, -.20, 4.00),
                        (-.60, -.03, 4.02)], (-.61, -.25, 4.26), .15),
    ('Hair_Side_Right', [(.44, -.20, 4.46), (.62, 0, 4.46), (.60, -.03, 4.02), (.56, -.19, 4.00),
                         (.47, -.26, 4.12)], (.59, -.26, 4.26), .15),
]
FRINGE_LOCKS = [
    ('Hair_Fringe_1', [(-.56, -.18, 4.48), (-.32, -.32, 4.66), (-.18, -.39, 4.42), (-.22, -.41, 4.25),
                       (-.27, -.41, 4.21), (-.44, -.37, 4.30)], (-.35, -.49, 4.40)),
    ('Hair_Fringe_2', [(-.30, -.34, 4.70), (-.02, -.36, 4.70), (.06, -.40, 4.42), (.03, -.42, 4.25),
                       (-.02, -.42, 4.20), (-.18, -.40, 4.32)], (-.10, -.50, 4.46)),
    ('Hair_Fringe_3', [(-.04, -.36, 4.70), (.24, -.35, 4.66), (.32, -.40, 4.38), (.29, -.42, 4.24),
                       (.24, -.42, 4.20), (.10, -.41, 4.32)], (.16, -.50, 4.45)),
    ('Hair_Fringe_4', [(.20, -.35, 4.66), (.46, -.27, 4.56), (.51, -.34, 4.30), (.50, -.37, 4.18),
                       (.47, -.39, 4.15), (.35, -.40, 4.30)], (.40, -.47, 4.40)),
]


def _haircut(col):
    """Scalp plus ten Hair2 wedge locks; no dome or cowlick, so the silhouette reads as hair."""
    parts = [_scalp(col)]
    for name, outline, ridge, depth in CROWN_LOCKS:
        parts.append(_lock(col, name, outline, ridge, depth))
    for name, outline, ridge in FRINGE_LOCKS:
        parts.append(_lock(col, name, outline, ridge, .15))
    return parts


def build_head(col):
    """Face, neck shadow, ears, dot eyes, blush, smile and the haircut, placed on the child."""
    return _place(_face(col)+_haircut(col))


def _brow_outline(side, center, y, length=.14, thickness=.045, tilt=25):
    """Corners of a straight brow slab whose inner end (toward the nose) sits lower."""
    t = math.radians(tilt)
    cx, cz = center
    dx, dz = side*math.cos(t)*length/2, math.sin(t)*length/2
    nx, nz = -side*math.sin(t)*thickness/2, math.cos(t)*thickness/2
    return [(cx-dx-nx, y, cz-dz-nz), (cx+dx-nx, y, cz+dz-nz),
            (cx+dx+nx, y, cz+dz+nz), (cx-dx+nx, y, cz-dz+nz)]


# Angry brows: .17 long and .08 thick (about 2 display pixels) at the adults' 25 degree
# inner-end-down angle, centred above the eyes on the bright forehead facet.
BROW_CENTER_Z, BROW_LENGTH, BROW_THICKNESS = 4.11, .17, .08
BROW_LEAN = 35                 # degrees the brow front turns down, away from the key light


def _brow_block(col, name, outline, lean=BROW_LEAN, depth=.018):
    """A Navy brow block standing on the forehead with its front turned `lean` degrees downward.

    `outline` comes from _brow_outline: bottom edge (0, 1), then top edge (2 over 1, 3 over 0).
    The back sits `depth` inside the skin; the front's bottom edge stands .012 in front of the
    face and its top edge leans further forward, so the lit top of the block stays thin.
    """
    t = math.tan(math.radians(lean))
    back = [(x, face_y(z)+depth, z) for x, _, z in outline]
    front = []
    for i, (x, _, z) in enumerate(outline):
        base = outline[3-i][2] if i >= 2 else z
        front.append((x, face_y(base)-.012-(z-base)*t, z))
    n = len(outline)
    area = sum(outline[i][0]*outline[(i+1) % n][2]
               - outline[(i+1) % n][0]*outline[i][2] for i in range(n))
    order = list(range(n)) if area > 0 else list(reversed(range(n)))
    faces = [tuple(order), tuple(i+n for i in reversed(order))]
    faces += [(order[(i+1) % n], order[i], order[i]+n, order[(i+1) % n]+n) for i in range(n)]
    return kit.mesh(col, name, front+back, faces, 'Navy')


def build_angry_face(col):
    """Angry-sprite face marks: two tilted Navy brow blocks and the adults' down-turned frown.

    Returned in the child's authored body coordinates; the caller applies the .5 model
    scale and hides the smile.
    """
    marks = []
    for side, suffix in ((-1, 'L'), (1, 'R')):
        outline = _brow_outline(side, (side*.205, BROW_CENTER_Z), 0, BROW_LENGTH, BROW_THICKNESS)
        marks.append(_brow_block(col, 'Face_Angry_Brow_' + suffix, outline))
    marks.append(_flat_mark(col, 'Face_Angry_Frown',
                            [(-.057, -.399, 3.779), (-.028, -.401, 3.797),
                             (.028, -.401, 3.797), (.057, -.399, 3.779),
                             (.057, -.399, 3.759), (.028, -.401, 3.777),
                             (-.028, -.401, 3.777), (-.057, -.399, 3.759)],
                            'Navy', .009))
    return _place(marks)

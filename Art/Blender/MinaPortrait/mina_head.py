"""Mina's head: faceted face, adult dot eyes, an open happy smile and a wavy Magenta bob.

Authored in the approved male customer's units (Z up, -Y front, face rings
3.55-4.36); create_mina.py scales every object by .5 like GameCustomerFaceted.
Face marks follow the shared adult rules (customer_head.py: Navy dot eyes of the
same size and spacing, Strawberry blush). The hair follows the approved female
technique (explorer_head._hair_clump): one continuous scalp shell plus curved,
tapered solid clumps with longitudinal facets. Only palette colours via the kit.
"""
import math

from mathutils import Vector

# Asymmetric six-sided clump cross section of explorer_head._hair_clump (u across, v depth).
CLUMP_PROFILE = [(-.50, 0), (-.29, -.68), (.055, -1), (.36, -.64), (.50, 0), (0, .34)]
HEAD_AXIS = (0, .03)          # x, y of the vertical axis the hair wraps around
HAIR = 'Magenta'


def slab(kit, col, name, outline, color, depth=.018, ridge=None, side_color=None):
    """Closed shallow shape (or a solid lock when `ridge` is given); explorer_head._slab."""
    n = len(outline)
    verts = list(outline) + [(x, y+depth, z) for x, y, z in outline]
    area = sum(outline[i][0]*outline[(i+1) % n][2] - outline[(i+1) % n][0]*outline[i][2] for i in range(n))
    order = list(range(n)) if area > 0 else list(reversed(range(n)))
    if ridge is None:
        faces = [tuple(order)]
    else:
        verts.append(ridge)
        faces = [(2*n, order[i], order[(i+1) % n]) for i in range(n)]
    front_count = len(faces)
    faces.append(tuple(i+n for i in reversed(order)))
    faces += [(order[(i+1) % n], order[i], order[i]+n, order[(i+1) % n]+n) for i in range(n)]
    obj = kit.mesh(col, name, verts, faces, color)
    if side_color:
        obj.data.materials.append(kit.mat(side_color))
        for polygon in obj.data.polygons[front_count:]:
            polygon.material_index = 1
    return obj


def strip(kit, col, name, outer, inner, color, depth=.018, side_color=None):
    """Closed strip between equal-length contours; explorer_head._strip (ruffles, straps, brims)."""
    n = len(outer)
    outline = list(outer) + list(reversed(inner))
    verts = outline + [(x, y+depth, z) for x, y, z in outline]
    faces = []
    for i in range(n-1):
        face = (i, i+1, 2*n-2-i, 2*n-1-i)
        area = sum(verts[face[j]][0]*verts[face[(j+1) % 4]][2] - verts[face[(j+1) % 4]][0]*verts[face[j]][2]
                   for j in range(4))
        if area < 0:
            face = tuple(reversed(face))
        faces.append(face)
    front_count = len(faces)
    faces += [tuple(v+2*n for v in reversed(face)) for face in list(faces)]
    edge_counts, directed = {}, {}
    for face in faces[:front_count]:
        for i in range(len(face)):
            edge = (face[i], face[(i+1) % len(face)])
            key = tuple(sorted(edge))
            edge_counts[key] = edge_counts.get(key, 0)+1
            directed[key] = edge
    for key, count in edge_counts.items():
        if count == 1:
            start, end = directed[key]
            faces.append((end, start, start+2*n, end+2*n))
    obj = kit.mesh(col, name, verts, faces, color)
    if side_color:
        obj.data.materials.append(kit.mat(side_color))
        for polygon in obj.data.polygons[front_count:]:
            polygon.material_index = 1
    return obj


def hair_clump(kit, col, name, rows, tip, color=HAIR):
    """Curved tapered solid lock (explorer_head._hair_clump) that follows the head.

    Rows are (x, y, z, width, thickness) along the centre line. The explorer
    clump always thickens toward -Y, which suits bangs; a bob also wraps the
    sides and back, so here the thickness points away from the head axis and
    the width runs across the lock on the head surface.
    """
    verts = []
    for i, (x, y, z, width, thickness) in enumerate(rows):
        centre = Vector((x, y, z))
        previous = Vector(rows[max(0, i-1)][:3])
        following = Vector(rows[i+1][:3] if i+1 < len(rows) else tip)
        tangent = (following-previous).normalized()
        out = Vector((x-HEAD_AXIS[0], y-HEAD_AXIS[1], 0))
        out = out.normalized() if out.length > 1e-6 else Vector((0, -1, 0))
        out = (out-tangent*out.dot(tangent)).normalized()
        across = out.cross(tangent).normalized()
        for u, v in CLUMP_PROFILE:
            verts.append(tuple(centre+across*(u*width)-out*(v*thickness)))
    faces = [tuple(reversed(range(6)))]
    for r in range(len(rows)-1):
        for j in range(6):
            faces.append((r*6+j, r*6+(j+1) % 6, (r+1)*6+(j+1) % 6, (r+1)*6+j))
    end = len(verts)
    verts.append(tuple(tip))
    for j in range(6):
        faces.append(((len(rows)-1)*6+j, (len(rows)-1)*6+(j+1) % 6, end))
    obj = kit.mesh(col, 'Hair_'+name, verts, faces, color)
    obj['hair_structure'] = 'curved tapered solid with longitudinal facets'
    obj['centerline_sections'] = len(rows)
    return obj


def _face(kit, col):
    # The explorer's face contour (slightly narrower chin) at the male face heights.
    xy = [(-.355, -.416), (.355, -.416), (.510, -.210), (.445, .220),
          (.240, .352), (-.240, .352), (-.445, .220), (-.510, -.210)]
    rings = [(3.550, .65, .91), (3.675, .94, 1), (4.045, 1, 1), (4.360, .92, .96)]
    verts = [(x*sx, y*sy+.030, z) for z, sx, sy in rings for x, y in xy]
    faces = [tuple(reversed(range(8)))]
    shade = [1]
    for j in range(3):
        for i in range(8):
            faces.append((j*8+i, j*8+(i+1) % 8, (j+1)*8+(i+1) % 8, (j+1)*8+i))
            shade.append(0 if i in (0, 1, 7) else 1)
    faces.append(tuple(range(24, 32)))
    shade.append(0)
    face = kit.mesh(col, 'Head_FacetedFace', verts, faces, 'Skin1')
    face.data.materials.append(kit.mat('Skin3'))
    for polygon, index in zip(face.data.polygons, shade):
        polygon.material_index = index
    kit.box(col, 'Head_Neck', (0, .03, 3.47), (.33, .32, .30), 'Skin3', .018)
    for side, suffix in ((-1, 'L'), (1, 'R')):
        slab(kit, col, 'Head_Ear_'+suffix,
             [(side*.455, -.213, 4.070), (side*.548, -.205, 4.052), (side*.562, -.208, 3.890),
              (side*.491, -.222, 3.830), (side*.440, -.238, 3.883)], 'Skin1', .13, side_color='Skin3')


def _marks(kit, col):
    for side, suffix in ((-1, 'L'), (1, 'R')):
        # Approved adult dot eye: male size, spacing and bevel (customer_head.py).
        kit.box(col, 'Face_Eye_'+suffix, (side*.205, -.395, 3.983), (.075, .020, .106), 'Navy', .006)
        slab(kit, col, 'Face_Blush_'+suffix,
             [(side*.245, -.395, 3.867), (side*.363, -.389, 3.867),
              (side*.354, -.390, 3.824), (side*.253, -.395, 3.824)], 'Strawberry', .010)
    # The explorer's small wedge nose, moved to the male face heights.
    slab(kit, col, 'Face_SmallNose',
         [(-.032, -.398, 3.878), (.038, -.398, 3.878), (.036, -.412, 3.815), (-.022, -.412, 3.811)],
         'Skin3', .024, (.013, -.448, 3.846))
    # Slightly open happy smile: a flat-topped Navy crescent with a small Strawberry tongue.
    slab(kit, col, 'Face_OpenSmile',
         [(-.082, -.401, 3.782), (-.050, -.404, 3.770), (.050, -.404, 3.770), (.082, -.401, 3.782),
          (.062, -.403, 3.744), (.030, -.405, 3.724), (-.030, -.405, 3.724), (-.062, -.403, 3.744)],
         'Navy', .009)
    slab(kit, col, 'Face_SmileTongue',
         [(-.036, -.408, 3.745), (.036, -.408, 3.745), (.024, -.409, 3.731), (-.024, -.409, 3.731)],
         'Strawberry', .004)


def _polar(theta_deg, radius, z, cy=HEAD_AXIS[1]):
    t = math.radians(theta_deg)
    return (radius*math.sin(t), cy-radius*math.cos(t)*.86, z)


def _scalp(kit, col):
    """Continuous faceted crown; its edge drops from the forehead to the nape."""
    sides = 12
    low = [4.44, 4.38, 4.18, 3.98, 3.84, 3.78, 3.76, 3.78, 3.84, 3.98, 4.18, 4.38]
    shape = [(.565, .465), (.625, .515), (.535, .445), (.320, .280)]
    heights = [None, 4.56, 4.76, 4.87]
    rings = []
    for r, (rx, ry) in enumerate(shape):
        ring = []
        for i in range(sides):
            t = i*math.tau/sides
            z = low[i] if r == 0 else heights[r]+.02*math.cos(t)
            ring.append((rx*math.sin(t), HEAD_AXIS[1]-ry*math.cos(t), z))
        rings.append(ring)
    verts = [p for ring in rings for p in ring]+[(-.05, .02, 4.92)]
    faces = [tuple(reversed(range(sides)))]
    for r in range(len(rings)-1):
        for i in range(sides):
            faces.append((r*sides+i, r*sides+(i+1) % sides, (r+1)*sides+(i+1) % sides, (r+1)*sides+i))
    apex = len(verts)-1
    top = (len(rings)-1)*sides
    faces += [(top+i, top+(i+1) % sides, apex) for i in range(sides)]
    shell = kit.mesh(col, 'Hair_ConnectedScalp', verts, faces, HAIR)
    shell['hair_structure'] = 'continuous crown shell under the bob clumps'


def _bob_mass(kit, col):
    """Continuous bell-shaped bob shell from Mina's right temple, around the back, to her left.

    The explorer's hair rests on one connected scalp; a bob needs that mass down to
    the jaw, so this solid shell carries the silhouette and the clumps above it only
    add waves and ends. Alternate columns step in and out (vertical wave facets)
    and the hem rises and falls between columns.
    """
    columns = 17
    angles = [64+(296-64)*i/(columns-1) for i in range(columns)]
    profile = [(4.50, .60), (4.30, .675), (4.08, .715), (3.88, .675), (3.72, .715)]
    thickness = .075
    outer, inner = [], []
    for z, radius in profile:
        for i, theta in enumerate(angles):
            r = radius+(.022 if i % 2 else -.012)
            zz = z+((.035 if i % 2 else -.03) if z < 3.8 else 0)
            outer.append(_polar(theta, r, zz))
            inner.append(_polar(theta, r-thickness, zz+.01))
    n = len(outer)
    verts = outer+inner
    idx = lambda r, i: r*columns+i  # noqa: E731
    faces = []
    for r in range(len(profile)-1):
        for i in range(columns-1):
            quad = (idx(r, i), idx(r, i+1), idx(r+1, i+1), idx(r+1, i))
            faces.append(quad)
            faces.append(tuple(v+n for v in reversed(quad)))
    last = len(profile)-1
    boundary = [idx(0, i) for i in range(columns)]+[idx(r, columns-1) for r in range(1, last+1)]
    boundary += [idx(last, i) for i in range(columns-2, -1, -1)]+[idx(r, 0) for r in range(last-1, 0, -1)]
    faces += [(boundary[k], boundary[(k+1) % len(boundary)], boundary[(k+1) % len(boundary)]+n, boundary[k]+n)
              for k in range(len(boundary))]
    shell = kit.mesh(col, 'Hair_BobMass', verts, faces, HAIR)
    shell['hair_structure'] = 'continuous bell-shaped bob shell with wave facets'


def _bob_lock(theta, z_start, length, end, width, thickness, swirl=1.):
    """Rows for one wavy lock laid on the bob shell at angle `theta` (0 = front, - = Mina's right).

    The radius swells to cheek level and tucks in above the ends; `end` > 0 flips
    the tip outward, `end` < 0 curls it under. The angle drifts so the lock S-curves.
    """
    radii = [.60, .71, .755, .70, .745]
    drift = [0, 7, 2, -6, 2]
    taper = [(1.0, 1.0), (1.05, .95), (.95, .90), (.82, .80), (.62, .62)]
    rows = []
    for k in range(5):
        z = z_start-length*k/4.4
        x, y, _ = _polar(theta+swirl*drift[k], radii[k], z)
        rows.append((x, y, z, width*taper[k][0], thickness*taper[k][1]))
    tip = _polar(theta+swirl*4, radii[-1]+end, z_start-length-.03)
    return rows, tip


def _bob(kit, col):
    _bob_mass(kit, col)
    # (name, angle, root z, length, end flip/curl, width, thickness, swirl)
    locks = [
        ('Bob_FrontRight', -60, 4.44, .80, .05, .27, .085, 1.0),
        ('Bob_CheekRight', -76, 4.48, .84, -.05, .30, .09, -1.0),
        ('Bob_SideRight', -94, 4.52, .82, .07, .31, .095, 1.0),
        ('Bob_BackRight', -116, 4.54, .82, -.04, .32, .095, -1.0),
        ('Bob_NapeRight', -140, 4.54, .80, .06, .32, .09, 1.0),
        ('Bob_NapeBack', -164, 4.52, .78, -.04, .31, .09, -1.0),
        ('Bob_NapeLeft', 164, 4.52, .78, .05, .31, .09, 1.0),
        ('Bob_BackLeft', 138, 4.54, .80, -.05, .32, .095, -1.0),
        ('Bob_SideLeft', 112, 4.54, .78, .06, .31, .095, 1.0),
        ('Bob_BehindEarLeft', 88, 4.50, .76, -.04, .28, .085, -1.0),
        ('Bob_FrontLeft', 64, 4.42, .72, .05, .24, .08, 1.0),
    ]
    for name, theta, z, length, end, width, thick, swirl in locks:
        rows, tip = _bob_lock(theta, z, length, end, width, thick, swirl)
        hair_clump(kit, col, name, rows, tip)


def _bangs(kit, col):
    """Side-swept bangs from a part over Mina's right eye toward her left temple."""
    clumps = [
        ('Bang_SweepMain', [
            (-.24, -.30, 4.80, .20, .050), (-.08, -.43, 4.64, .25, .070),
            (.12, -.47, 4.46, .24, .072), (.30, -.45, 4.28, .17, .055)],
         (.43, -.40, 4.13)),
        ('Bang_SweepUpper', [
            (-.12, -.32, 4.86, .18, .050), (.08, -.42, 4.74, .22, .065),
            (.30, -.44, 4.58, .20, .062), (.46, -.38, 4.40, .13, .045)],
         (.55, -.30, 4.24)),
        ('Bang_UnderCurl', [
            (-.18, -.39, 4.70, .14, .040), (-.04, -.46, 4.52, .15, .050),
            (.08, -.46, 4.38, .10, .040)],
         (.14, -.44, 4.27)),
        ('Bang_PartShort', [
            (-.26, -.33, 4.82, .16, .050), (-.38, -.41, 4.62, .18, .060),
            (-.46, -.40, 4.42, .14, .050)],
         (-.52, -.34, 4.22)),
        ('Bang_PartCrown', [
            (-.30, -.14, 4.90, .20, .055), (-.44, -.26, 4.76, .22, .065),
            (-.55, -.28, 4.58, .16, .055)],
         (-.62, -.24, 4.40)),
    ]
    for name, rows, tip in clumps:
        hair_clump(kit, col, name, rows, tip)


def build_head(kit, col):
    """Face, neck, ears, face marks, crown shell, bob shell, eleven bob locks and five bang clumps."""
    _face(kit, col)
    _marks(kit, col)
    _scalp(kit, col)
    _bob(kit, col)
    _bangs(kit, col)

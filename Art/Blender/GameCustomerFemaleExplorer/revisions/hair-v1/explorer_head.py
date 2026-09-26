"""Expressive explorer head reconstructed from the supplied cream-cap reference."""
import math
import explorer_lib as a


def _slab(col, name, outline, color, depth=.018, ridge=None, side_color=None):
    """Make a closed, shallow graphic shape or a solid faceted hair lock."""
    n = len(outline)
    verts = list(outline) + [(x, y+depth, z) for x, y, z in outline]
    area = sum(outline[i][0]*outline[(i+1) % n][2]
               - outline[(i+1) % n][0]*outline[i][2] for i in range(n))
    order = list(range(n)) if area > 0 else list(reversed(range(n)))
    if ridge is None:
        faces = [tuple(order)]
    else:
        verts.append(ridge)
        faces = [(2*n, order[i], order[(i+1) % n]) for i in range(n)]
    front_count = len(faces)
    faces.append(tuple(i+n for i in reversed(order)))
    faces += [(order[(i+1) % n], order[i], order[i]+n,
               order[(i+1) % n]+n) for i in range(n)]
    obj = a.mesh(col, name, verts, faces, color)
    if side_color:
        obj.data.materials.append(a.mat(side_color))
        for polygon in obj.data.polygons[front_count:]:
            polygon.material_index = 1
    return obj


def _ellipse(col, name, cx, y, cz, rx, rz, color, sides=12, depth=.010):
    return _slab(col, name,
                 [(cx+rx*math.cos(i*math.tau/sides), y,
                   cz+rz*math.sin(i*math.tau/sides)) for i in range(sides)],
                 color, depth)


def _strip(col, name, outer, inner, color, depth=.018, side_color=None):
    """Closed strip between equal-length contours, including a curled brim."""
    n = len(outer)
    outline = list(outer) + list(reversed(inner))
    verts = outline + [(x, y+depth, z) for x, y, z in outline]
    faces = []
    for i in range(n-1):
        face = (i, i+1, 2*n-2-i, 2*n-1-i)
        area = sum(verts[face[j]][0]*verts[face[(j+1) % 4]][2]
                   - verts[face[(j+1) % 4]][0]*verts[face[j]][2]
                   for j in range(4))
        if area < 0:
            face = tuple(reversed(face))
        faces.append(face)
    front_count = len(faces)
    faces += [tuple(v+2*n for v in reversed(face)) for face in list(faces)]
    # Derive the boundary directions from the actual front quads so every
    # side wall closes outward even when the strip curls around itself.
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
    obj = a.mesh(col, name, verts, faces, color)
    if side_color:
        obj.data.materials.append(a.mat(side_color))
        for polygon in obj.data.polygons[front_count:]:
            polygon.material_index = 1
    return obj


def _face(col):
    xy = [(-.355, -.416), (.355, -.416), (.510, -.210),
          (.445, .220), (.240, .352), (-.240, .352),
          (-.445, .220), (-.510, -.210)]
    rings = [(3.880, .65, .91), (4.005, .94, 1),
             (4.375, 1, 1), (4.690, .92, .96)]
    verts = [(x*sx, y*sy+.030, z) for z, sx, sy in rings for x, y in xy]
    faces = [tuple(reversed(range(8)))]
    shade = [1]
    for j in range(3):
        for i in range(8):
            faces.append((j*8+i, j*8+(i+1) % 8,
                          (j+1)*8+(i+1) % 8, (j+1)*8+i))
            shade.append(0 if i in (0, 1, 7) else 1)
    faces.append(tuple(range(24, 32)))
    shade.append(0)
    obj = a.mesh(col, 'Head_PeachAngularFace', verts, faces, 'Skin1')
    obj.data.materials.append(a.mat('Skin3'))
    for polygon, index in zip(obj.data.polygons, shade):
        polygon.material_index = index
    a.box(col, 'Head_Neck', (0, .03, 3.825), (.365, .355, .250), 'Skin3', .018)

    for side, suffix in ((-1, 'L'), (1, 'R')):
        _slab(col, 'Head_Ear_' + suffix,
              [(side*.455, -.230, 4.277), (side*.542, -.212, 4.254),
               (side*.568, -.213, 4.102), (side*.495, -.239, 4.025),
               (side*.430, -.260, 4.080)], 'Skin1', .13,
              side_color='Skin3')
        _slab(col, 'Head_EarInner_' + suffix,
              [(side*.493, -.247, 4.213), (side*.525, -.244, 4.199),
               (side*.526, -.248, 4.119), (side*.488, -.259, 4.106)],
              'Skin3', .012)

    # The eyes use independent white, iris and pupil planes. Navy is limited
    # to pupils and a thin upper lash; there is deliberately no enclosing ring.
    for side, suffix in ((-1, 'L'), (1, 'R')):
        cx, cz = side*.209, 4.343
        _ellipse(col, 'Face_EyeWhite_' + suffix, cx, -.419, cz,
                 .120, .146, 'White', sides=12)
        iris_x = cx+.012
        _ellipse(col, 'Face_BrownIris_' + suffix, iris_x, -.435, cz-.030,
                 .082, .109, 'Hair2', sides=12)
        _ellipse(col, 'Face_EyePupil_' + suffix, iris_x+.001, -.449, cz-.018,
                 .060, .086, 'Navy', sides=10)
        _slab(col, 'Face_IrisWarmLower_' + suffix,
              [(iris_x-.046, -.450, cz-.115), (iris_x-.023, -.450, cz-.132),
               (iris_x+.023, -.450, cz-.132), (iris_x+.046, -.450, cz-.115),
               (iris_x+.022, -.450, cz-.124), (iris_x-.022, -.450, cz-.124)],
              'Wood', .006)
        _ellipse(col, 'Face_EyeCatchlight_' + suffix,
                 iris_x-.018, -.459, cz+.020, .013, .018, 'White', sides=6,
                 depth=.006)
        angles = [math.radians(v) for v in (168, 138, 106, 74, 40, 15)]
        outer = [(cx+.133*math.cos(t), -.440, cz+.158*math.sin(t)) for t in angles]
        inner = [(cx+.118*math.cos(t), -.440, cz+.141*math.sin(t)) for t in angles]
        _strip(col, 'Face_UpperLash_' + suffix, outer, inner, 'Navy', .010)
        # Short lifted brows sit above the whites, under the cream cap.
        brow_x = [cx-.075, cx-.025, cx+.037, cx+.075]
        brow_z = [4.508, 4.526, 4.524, 4.505]
        _strip(col, 'Face_Brow_' + suffix,
               [(x, -.424, z+.011) for x, z in zip(brow_x, brow_z)],
               [(x, -.424, z-.009) for x, z in zip(brow_x, brow_z)],
               'Hair3', .010)
        _slab(col, 'Face_PinkCheek_' + suffix,
              [(side*.262, -.409, 4.169), (side*.362, -.399, 4.177),
               (side*.357, -.400, 4.113), (side*.269, -.410, 4.108)],
              'Strawberry', .010)

    # Small real wedge nose, with a short friendly smile below it.
    _slab(col, 'Face_SmallPeachNose',
          [(-.034, -.405, 4.210), (.041, -.405, 4.210),
           (.039, -.420, 4.137), (-.023, -.420, 4.132)],
          'Skin3', .027, (.016, -.490, 4.172))
    _strip(col, 'Face_SoftSmile',
           [(-.083, -.409, 4.061), (-.037, -.414, 4.047),
            (.037, -.414, 4.047), (.093, -.409, 4.065)],
           [(-.079, -.409, 4.045), (-.036, -.414, 4.031),
            (.039, -.414, 4.031), (.090, -.409, 4.049)],
           'Hair2', .010)


def _hair(col):
    low = [(0, -.331, 4.586), (.358, -.290, 4.531),
           (.521, .024, 4.024), (.390, .335, 4.024),
           (0, .450, 4.034), (-.395, .335, 4.022),
           (-.523, .024, 4.022), (-.358, -.294, 4.530)]
    high = [(0, -.272, 4.847), (.323, -.221, 4.794),
            (.480, .030, 4.658), (.336, .315, 4.704),
            (0, .416, 4.800), (-.337, .314, 4.701),
            (-.482, .030, 4.659), (-.328, -.222, 4.794)]
    verts = low+high
    faces = [tuple(reversed(range(8))), tuple(range(8, 16))]
    faces += [(i, (i+1) % 8, (i+1) % 8+8, i+8) for i in range(8)]
    a.mesh(col, 'Hair_ShortDarkUnderCap', verts, faces, 'Hair3')
    locks = [
        ('Hair_LeftUpperBang',
         [(-.355, -.332, 4.707), (-.119, -.409, 4.663),
          (-.191, -.451, 4.548), (-.412, -.355, 4.491)],
         (-.284, -.471, 4.608)),
        ('Hair_LeftTemple',
         [(-.425, -.299, 4.547), (-.339, -.405, 4.463),
          (-.365, -.405, 4.087), (-.472, -.303, 4.137)],
         (-.420, -.448, 4.320)),
        ('Hair_RightTemple',
         [(.337, -.405, 4.527), (.459, -.298, 4.497),
          (.472, -.309, 4.125), (.370, -.402, 4.095)],
         (.415, -.448, 4.316)),
    ]
    for name, outline, ridge in locks:
        _slab(col, name, outline, 'Hair3', .14, ridge, 'Hair1')


def _rear_flaps(col):
    _slab(col, 'Cap_PlumRearFlap_L',
          [(-.525, .124, 4.780), (-.687, .138, 4.686),
           (-.687, .142, 4.417), (-.760, .146, 4.393),
           (-.760, .151, 4.175), (-.683, .154, 4.074),
           (-.586, .148, 4.282), (-.542, .130, 4.525)],
          'Plum', .16)
    _slab(col, 'Cap_LavenderFlapInset_L',
          [(-.566, .102, 4.661), (-.702, .117, 4.635),
           (-.702, .120, 4.474), (-.626, .114, 4.426)],
          'Pants1', .04)
    _slab(col, 'Cap_PlumRearFlap_R',
          [(.540, .181, 4.738), (.692, .184, 4.631),
           (.711, .179, 4.456), (.735, .176, 4.304),
           (.638, .166, 4.155), (.565, .162, 4.411)],
          'Plum', .14)
    _slab(col, 'Cap_MintSideFlap_L',
          [(-.546, -.038, 4.716), (-.671, -.019, 4.651),
           (-.604, -.131, 4.441), (-.435, -.242, 4.517)],
          'Mint', .060, (-.569, -.161, 4.573))
    _slab(col, 'Cap_MintSideFlap_R',
          [(.528, -.026, 4.703), (.662, -.008, 4.632),
           (.620, -.116, 4.452), (.454, -.219, 4.520)],
          'Mint', .055, (.565, -.144, 4.587))


def _cap(col):
    # Four uneven octagonal rings produce a soft, slightly slouched crown.
    # Broad quads retain the reference's planes without cube-like steps.
    rings = [
        [(0, -.421, 4.782), (.415, -.324, 4.736), (.691, .026, 4.642),
         (.483, .378, 4.663), (0, .511, 4.693), (-.486, .378, 4.680),
         (-.690, .025, 4.672), (-.424, -.324, 4.759)],
        [(-.038, -.360, 5.025), (.380, -.274, 5.000), (.620, .040, 4.946),
         (.420, .366, 5.031), (-.041, .465, 5.073), (-.454, .350, 5.035),
         (-.639, .033, 4.959), (-.448, -.269, 5.024)],
        [(-.066, -.223, 5.186), (.274, -.161, 5.145), (.429, .046, 5.111),
         (.279, .284, 5.180), (-.089, .350, 5.218), (-.398, .271, 5.217),
         (-.492, .037, 5.188), (-.363, -.166, 5.231)],
        [(-.106, -.088, 5.239), (.083, -.035, 5.213), (.176, .079, 5.210),
         (.074, .207, 5.247), (-.135, .238, 5.271), (-.278, .161, 5.280),
         (-.310, .045, 5.270), (-.250, -.058, 5.264)],
    ]
    verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(8)))]
    shade = [0]
    for j in range(3):
        for i in range(8):
            faces.append((j*8+i, j*8+(i+1) % 8,
                          (j+1)*8+(i+1) % 8, (j+1)*8+i))
            shade.append(1 if i in (0, 7) and j in (1, 2) else 0)
    faces.append(tuple(range(24, 32)))
    shade.append(1)
    obj = a.mesh(col, 'Cap_SoftFacetedCreamCrown', verts, faces, 'Cream')
    obj.data.materials.append(a.mat('White'))
    for polygon, index in zip(obj.data.polygons, shade):
        polygon.material_index = index

    outer = [(-.602, -.236, 4.713), (-.429, -.364, 4.810),
             (-.117, -.438, 4.863), (.172, -.427, 4.839),
             (.447, -.340, 4.769), (.610, -.205, 4.677)]
    inner = [(x, y-.004, z-.084) for x, y, z in outer]
    _strip(col, 'Cap_StrawberryBrowBand', outer, inner, 'Strawberry', .041)
    _strip(col, 'Cap_WhiteBandPiping',
           [(x, y-.004, z+.019) for x, y, z in outer],
           [(x, y-.005, z+.002) for x, y, z in outer], 'White', .022)

    # The broad golden curl is the distinctive front silhouette of the
    # reference. It is a thick curved strip with a dark opening behind its tip.
    outer = [(-.055, -.475, 4.786), (.183, -.515, 4.845),
             (.411, -.494, 4.825), (.584, -.422, 4.757),
             (.637, -.445, 4.652), (.543, -.477, 4.571)]
    inner = [(-.024, -.566, 4.625), (.199, -.592, 4.708),
             (.391, -.583, 4.713), (.484, -.549, 4.666),
             (.491, -.563, 4.624), (.459, -.541, 4.602)]
    _strip(col, 'Cap_VanillaGoldenFrontCurl', outer, inner, 'Vanilla', .115, 'Gold')
    _slab(col, 'Cap_GoldenForelock',
          [(-.120, -.441, 4.808), (-.029, -.483, 4.797),
           (.025, -.548, 4.595), (-.076, -.514, 4.604)],
          'Vanilla', .105, (-.067, -.585, 4.722), 'Gold')
    _ellipse(col, 'Cap_FrontGoldBadge', .176, -.408, 4.968,
             .065, .037, 'Gold', sides=6, depth=.021)
    _ellipse(col, 'Cap_FrontBadgeCreamInset', .163, -.429, 4.981,
             .032, .014, 'Cream', sides=6, depth=.008)


def build_head(col):
    """Create all named face, hair, cap, curl and rear-flap mesh objects."""
    _face(col)
    _hair(col)
    _rear_flaps(col)
    _cap(col)

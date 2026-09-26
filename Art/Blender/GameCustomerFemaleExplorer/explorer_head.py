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


def _eyes(col):
    """Match the approved male customer's small chamfered Navy eye marks."""
    for side, suffix in ((-1, 'L'), (1, 'R')):
        # Male dimensions and spacing; +.330 follows this face's chin offset.
        # The explorer's face projects .006 farther forward than the male's.
        a.box(col, 'Face_Eye_' + suffix, (side*.205, -.401, 4.313),
              (.075, .020, .106), 'Navy', .006)


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

    _eyes(col)
    for side, suffix in ((-1, 'L'), (1, 'R')):
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


def _hair_clump(col, name, rows, tip):
    """A curved solid lock with longitudinal facets and a single tapered tip.

    Rows describe a center line plus width and thickness. The asymmetric
    six-sided cross section turns with that line, avoiding a flat card or
    a diamond-shaped ridge added to a rectangular outline.
    """
    from mathutils import Vector
    profile=[(-.50,0),(-.29,-.68),(.055,-1),(.36,-.64),(.50,0),(0,.34)]
    verts=[]
    for i,(x,y,z,width,thickness) in enumerate(rows):
        previous=Vector(rows[max(0,i-1)][:3])
        following=Vector(rows[i+1][:3] if i+1<len(rows) else tip)
        tangent=following-previous
        across=Vector((-tangent.z,0,tangent.x)).normalized()
        for u,v in profile:
            p=Vector((x,y,z))+across*(u*width)+Vector((0,v*thickness,0))
            verts.append(tuple(p))
    faces=[tuple(reversed(range(6)))]
    for r in range(len(rows)-1):
        for j in range(6):
            faces.append((r*6+j,r*6+(j+1)%6,(r+1)*6+(j+1)%6,(r+1)*6+j))
    end=len(verts);verts.append(tip)
    for j in range(6):
        faces.append(((len(rows)-1)*6+j,(len(rows)-1)*6+(j+1)%6,end))
    obj=a.mesh(col,'Hair_'+name,verts,faces,'Navy')
    obj['hair_structure']='curved tapered solid with longitudinal facets'
    obj['centerline_sections']=len(rows)
    return obj


def _hair(col):
    # The continuous under-cap mass follows the scalp. Its edge is raised
    # above each ear and drops only at the nape, not across the cheeks.
    low_z=[4.63,4.635,4.52,4.31,4.10,4.02,4.00,4.04,4.10,4.31,4.53,4.65]
    rings=[]
    for r,(rx,ry,z) in enumerate(((.533,.425,0),(.550,.431,4.76),(.472,.37,4.895))):
        rings.append([(rx*math.sin(i*math.tau/12),.03-ry*math.cos(i*math.tau/12),
                       low_z[i] if r==0 else z+.022*math.cos(i*math.tau/12)) for i in range(12)])
    verts=[p for ring in rings for p in ring]
    faces=[tuple(reversed(range(12)))]
    for r in range(2):
        for i in range(12):
            faces.append((r*12+i,r*12+(i+1)%12,(r+1)*12+(i+1)%12,(r+1)*12+i))
    faces.append(tuple(range(24,36)))
    shell=a.mesh(col,'Hair_ConnectedScalp',verts,faces,'Navy')
    shell['hair_structure']='continuous scalp with exposed ears and short nape'

    # Each broad root is buried under the cap or in the scalp. Short swept
    # bangs overlap in one direction; eyes and brows stay unobstructed.
    clumps=[
        ('SweptFront_Main',[
            (-.420,-.235,4.739,.176,.038),(-.326,-.375,4.671,.162,.050),
            (-.216,-.420,4.667,.166,.054),(-.114,-.437,4.621,.088,.037)],
            (-.040,-.440,4.584)),
        ('SweptFront_Left',[
            (-.535,-.145,4.719,.133,.035),(-.474,-.304,4.690,.146,.057),
            (-.409,-.366,4.624,.114,.050),(-.349,-.390,4.570,.062,.031)],
            (-.321,-.401,4.531)),
        ('SweptFront_UnderCurl',[
            (-.242,-.253,4.739,.123,.029),(-.141,-.372,4.684,.120,.039),
            (-.053,-.413,4.661,.078,.039)],
            (.022,-.429,4.613)),
        ('SweptFront_Right',[
            (.342,-.230,4.716,.112,.029),(.419,-.334,4.656,.116,.049),
            (.431,-.359,4.573,.076,.041)],
            (.398,-.380,4.506)),
        ('Temple_Left_Main',[
            (-.473,-.210,4.712,.166,.040),(-.493,-.255,4.577,.171,.057),
            (-.482,-.295,4.423,.132,.051),(-.440,-.323,4.294,.075,.036)],
            (-.410,-.338,4.230)),
        ('Temple_Left_Companion',[
            (-.526,-.131,4.676,.119,.039),(-.553,-.171,4.541,.126,.051),
            (-.549,-.201,4.411,.088,.041),(-.521,-.217,4.329,.048,.027)],
            (-.493,-.223,4.283)),
        ('Temple_Right_Main',[
            (.486,-.190,4.701,.158,.041),(.507,-.243,4.552,.154,.054),
            (.495,-.288,4.397,.116,.048),(.454,-.324,4.278,.062,.028)],
            (.425,-.340,4.214)),
        ('Temple_Right_Companion',[
            (.544,-.112,4.639,.115,.036),(.563,-.158,4.502,.123,.045),
            (.551,-.185,4.366,.073,.035)],
            (.505,-.207,4.278)),
        ('BehindEar_Left',[
            (-.551,.092,4.546,.164,.057),(-.549,.123,4.382,.160,.062),
            (-.509,.188,4.220,.126,.048),(-.458,.245,4.105,.075,.034)],
            (-.399,.280,4.049)),
        ('BehindEar_Right',[
            (.548,.105,4.523,.160,.050),(.542,.147,4.360,.149,.056),
            (.498,.206,4.207,.110,.045),(.448,.253,4.097,.061,.029)],
            (.392,.277,4.039)),
        ('Nape_Left',[
            (-.365,.349,4.418,.195,.055),(-.329,.390,4.237,.183,.056),
            (-.273,.407,4.089,.097,.037)],
            (-.211,.415,4.019)),
        ('Nape_Right',[
            (.344,.360,4.405,.176,.055),(.308,.403,4.222,.157,.052),
            (.250,.420,4.081,.085,.036)],
            (.184,.423,4.012)),
    ]
    for name,rows,tip in clumps:
        _hair_clump(col,name,rows,tip)


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

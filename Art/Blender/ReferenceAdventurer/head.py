"""Angular face and layered, irregular sandy hair for the reference adventurer.

Z is up; the face looks toward -Y.  Kept separate from the costume builder.
"""
from artlib import box, mat, mesh


def _materials(obj, names, indices):
    """Give broad individual planes an intentional painted low-poly value."""
    for name in names[1:]:
        obj.data.materials.append(mat(name))
    for polygon, index in zip(obj.data.polygons, indices):
        polygon.material_index = index
    return obj


def _lock(col, name, contour, ridge, colors, depth=.16):
    """A solid tapered hair wedge with a shallow, faceted front ridge.

    The hand-drawn contour is in clockwise XZ order.  Front points have
    negative Y, and the backing goes toward the skull.  Unequal polygons and
    varying ridge positions prevent a regular radial / hedgehog silhouette.
    """
    n = len(contour)
    verts = list(contour) + [ridge]
    verts += [(x, y + depth, z) for x, y, z in contour]
    faces = [(n, (i + 1) % n, i) for i in range(n)]
    faces += [tuple(range(n + 1, 2 * n + 1))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n + 1, i + n + 1)
              for i in range(n)]
    obj = mesh(col, name, verts, faces, colors[0])
    unique = list(dict.fromkeys(colors + ['HairShadow']))
    for color in unique[1:]:
        obj.data.materials.append(mat(color))
    for i, polygon in enumerate(obj.data.polygons):
        color = colors[i % len(colors)] if i < n else 'HairShadow'
        polygon.material_index = unique.index(color)
    return obj


def _ear(col, side):
    # A small peach pentagon behind the ashy temple lock, not a round ball.
    profile = [(0.435, -.277, 5.305), (0.535, -.258, 5.270),
               (0.550, -.241, 5.108), (0.474, -.264, 5.045),
               (0.421, -.289, 5.112)]
    verts = [(side*x, y, z) for x, y, z in profile]
    verts += [(x, y+.13, z) for x, y, z in verts]
    faces = [tuple(reversed(range(5))), tuple(range(5, 10))]
    faces += [(i, (i+1) % 5, (i+1) % 5+5, i+5) for i in range(5)]
    if side < 0:
        faces = [tuple(reversed(face)) for face in faces]
    obj = mesh(col, 'Ear_' + ('L' if side < 0 else 'R'), verts, faces, 'Skin')
    return _materials(obj, ['Skin', 'SkinShade'], [0, 1, 1, 1, 1, 0, 0])


def _face(col):
    # A broad flat face and small chamfered temples, tapering into the chin.
    profile = [(-.315, -.408), (.315, -.408), (.460, -.205),
               (.405, .205), (.230, .345), (-.230, .345),
               (-.405, .205), (-.460, -.205)]
    verts = []
    rings = [(4.870, .61, .90), (5.020, .93, 1),
             (5.410, 1, 1), (5.735, .91, .95)]
    for z, sx, sy in rings:
        verts += [(x*sx, y*sy, z) for x, y in profile]
    faces = [tuple(reversed(range(8)))]
    indices = [1]
    for j in range(len(rings)-1):
        for i in range(8):
            faces.append((j*8+i, j*8+(i+1) % 8,
                          (j+1)*8+(i+1) % 8, (j+1)*8+i))
            indices.append(0 if i in (0, 1, 7) else 1)
    faces.append(tuple(range(24, 32)))
    indices.append(0)
    obj = mesh(col, 'Face_AngularPeach', verts, faces, 'Skin')
    _materials(obj, ['Skin', 'SkinShade'], indices)

    box(col, 'Neck', (0, -.035, 4.835), (.36, .37, .38), 'SkinShade', .025)
    _ear(col, -1)
    _ear(col, 1)

    # No white, pupil, shine, mouth, nose or blush: the two reference marks
    # are solid rectangular black eyes with small inward-sloping upper marks.
    for side in (-1, 1):
        suffix = 'L' if side < 0 else 'R'
        box(col, 'Eye_' + suffix,
            (side*.216, -.424, 5.275), (.105, .024, .150), 'Eye', 0)
        brow = [(side*.277, -.445, 5.384),
                (side*.168, -.445, 5.348),
                (side*.173, -.445, 5.323),
                (side*.272, -.445, 5.356)]
        verts = brow + [(x, y+.020, z) for x, y, z in brow]
        faces = [(0, 1, 2, 3), (7, 6, 5, 4),
                 (1, 0, 4, 5), (2, 1, 5, 6),
                 (3, 2, 6, 7), (0, 3, 7, 4)]
        if side < 0:
            faces = [tuple(reversed(face)) for face in faces]
        mesh(col, 'Eye_UpperAngle_' + suffix, verts, faces, 'Eye')


def _hair_cap(col):
    lower = [(0, -.397, 5.580), (.360, -.316, 5.565),
             (.503, -.020, 5.425), (.366, .286, 5.400),
             (0, .390, 5.398), (-.372, .285, 5.422),
             (-.503, -.008, 5.423), (-.372, -.308, 5.554)]
    upper = [(-.045, -.293, 5.945), (.292, -.235, 5.951),
             (.440, .015, 5.843), (.300, .282, 5.855),
             (-.008, .328, 5.944), (-.310, .247, 5.927),
             (-.440, -.013, 5.846), (-.337, -.239, 5.925)]
    verts = lower + upper + [(-.055, .025, 6.074)]
    faces = []
    indices = []
    for i in range(8):
        faces.append((i, (i+1) % 8, (i+1) % 8+8, i+8))
        indices.append([0, 1, 2, 2, 2, 1, 2, 1][i])
        faces.append((i+8, (i+1) % 8+8, 16))
        indices.append([0, 0, 1, 1, 0, 0, 1, 0][i])
    obj = mesh(col, 'Hair_FacetedCrown', verts, faces, 'Hair')
    _materials(obj, ['Hair', 'HairMid', 'HairShadow'], indices)


def build_head(col):
    """Create the complete editable low-poly reference head in ``col``."""
    _face(col)
    _hair_cap(col)

    # The perimeter is deliberately lopsided, with only a few broad upper
    # spikes and lower, flattened side wedges like the illustrated original.
    locks = [
        ('Hair_TallCrown',
         [(-.205, -.085, 6.300), (.015, -.092, 6.199),
          (.116, -.167, 5.918), (-.217, -.170, 5.958)],
         (-.106, -.219, 6.062), ['HairLight', 'Hair', 'HairMid']),
        ('Hair_UpperRightFork',
         [(.251, -.040, 6.235), (.365, -.057, 6.115),
          (.146, -.228, 5.953), (-.057, -.160, 6.029)],
         (.157, -.256, 6.085), ['Hair', 'HairLight', 'HairMid']),
        ('Hair_BackLeftUpper',
         [(-.538, .012, 6.033), (-.342, -.081, 6.087),
          (-.077, -.204, 5.949), (-.390, -.233, 5.832)],
         (-.324, -.252, 5.981), ['HairMid', 'Hair', 'HairShadow']),
        ('Hair_LeftWidePoint',
         [(-.704, -.025, 5.891), (-.411, -.162, 5.997),
          (-.281, -.296, 5.765), (-.476, -.180, 5.687)],
         (-.457, -.279, 5.829), ['HairMid', 'Hair', 'HairShadow']),
        ('Hair_LeftMiddlePoint',
         [(-.418, -.168, 5.845), (-.340, -.308, 5.641),
          (-.699, -.095, 5.575), (-.588, -.106, 5.746)],
         (-.484, -.315, 5.707), ['Hair', 'HairMid', 'HairShadow']),
        ('Hair_LeftLowerPoint',
         [(-.442, -.179, 5.654), (-.353, -.300, 5.450),
          (-.643, -.086, 5.318), (-.603, -.098, 5.476)],
         (-.477, -.319, 5.479), ['HairShadow', 'HairMid', 'HairShadow']),
        ('Hair_RightUpperFan',
         [(.257, -.073, 6.016), (.563, -.057, 6.018),
          (.437, -.192, 5.804), (.141, -.275, 5.870)],
         (.359, -.275, 5.939), ['HairLight', 'Hair', 'HairMid']),
        ('Hair_RightWidePoint',
         [(.357, -.088, 5.925), (.687, -.012, 5.803),
          (.545, -.058, 5.670), (.303, -.288, 5.725)],
         (.462, -.278, 5.790), ['HairMid', 'Hair', 'HairShadow']),
        ('Hair_RightMiddlePoint',
         [(.455, -.076, 5.792), (.568, -.039, 5.711),
          (.691, .001, 5.470), (.349, -.270, 5.572)],
         (.508, -.269, 5.630), ['HairMid', 'HairShadow', 'HairMid']),
        ('Hair_RightLowerPoint',
         [(.403, -.128, 5.665), (.574, -.020, 5.523),
          (.654, .004, 5.292), (.387, -.263, 5.430)],
         (.469, -.275, 5.478), ['HairShadow', 'HairMid', 'HairShadow']),
    ]
    for name, contour, ridge, colors in locks:
        _lock(col, name, contour, ridge, colors, .18)

    # Long slate-gray strips frame the skin and end just above the ear tips.
    _lock(col, 'Hair_LeftTemple',
          [(-.439, -.359, 5.666), (-.321, -.435, 5.576),
           (-.357, -.434, 5.068), (-.456, -.360, 5.139)],
          (-.393, -.468, 5.365),
          ['HairShadow', 'HairShadow', 'HairMid'], .12)
    _lock(col, 'Hair_RightTemple',
          [(.305, -.430, 5.641), (.455, -.355, 5.591),
           (.455, -.359, 5.132), (.361, -.432, 5.063)],
          (.409, -.468, 5.384),
          ['HairMid', 'HairShadow', 'HairShadow'], .12)

    # Broad overlapping crown planes sweep from upper left toward the right.
    # Their contours remain chunky: a swept fringe, not many cone strands.
    front_locks = [
        ('Hair_LeftForelock',
         [(-.267, -.323, 6.000), (-.125, -.400, 5.885),
          (-.303, -.508, 5.461), (-.458, -.378, 5.644)],
         (-.288, -.559, 5.778), ['Hair', 'HairMid', 'HairShadow']),
        ('Hair_CrownSweep',
         [(-.259, -.361, 6.020), (.058, -.355, 5.956),
          (.447, -.410, 5.718), (.031, -.518, 5.733)],
         (-.083, -.547, 5.904), ['HairLight', 'Hair', 'HairLight']),
        ('Hair_RightSweep',
         [(.012, -.423, 5.940), (.261, -.363, 5.884),
          (.565, -.370, 5.611), (.181, -.504, 5.665)],
         (.239, -.551, 5.765), ['Hair', 'HairLight', 'HairMid']),
        ('Hair_RightBang',
         [(.193, -.472, 5.725), (.388, -.416, 5.700),
          (.395, -.489, 5.284), (.271, -.516, 5.427)],
         (.315, -.557, 5.567), ['Hair', 'HairMid', 'HairShadow']),
        ('Hair_LongCentralBang',
         [(-.236, -.472, 5.918), (.119, -.467, 5.724),
          (.125, -.543, 5.246), (-.105, -.538, 5.549)],
         (-.074, -.596, 5.724), ['HairLight', 'Hair', 'HairMid']),
        ('Hair_LeftSmallBang',
         [(-.276, -.473, 5.757), (-.171, -.540, 5.571),
          (-.253, -.551, 5.435), (-.360, -.472, 5.521)],
         (-.282, -.568, 5.589), ['HairMid', 'Hair', 'HairShadow']),
    ]
    for name, contour, ridge, colors in front_locks:
        _lock(col, name, contour, ridge, colors, .16)

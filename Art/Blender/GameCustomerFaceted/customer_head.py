"""Friendly game-shopper head: broad face planes and a swept faceted haircut.

Standalone part builder.  Z is up, -Y faces forward; all colors come from
the approved game palette supplied by customer_lib.
"""
import customer_lib as a


def _planes(obj, colors, assignments):
    for color in colors[1:]:
        obj.data.materials.append(a.mat(color))
    for polygon, index in zip(obj.data.polygons, assignments):
        polygon.material_index = index
    return obj


def _lock(col, name, outline, ridge, colors=('Hair1',), depth=.15):
    """An editable solid wedge with four broad, deliberately unequal planes."""
    n = len(outline)
    verts = list(outline) + [ridge]
    verts += [(x, y + depth, z) for x, y, z in outline]
    # Normalize the drawing's winding so every front triangle faces -Y.
    area = sum(outline[i][0] * outline[(i+1) % n][2]
               - outline[(i+1) % n][0] * outline[i][2] for i in range(n))
    order = list(range(n)) if area > 0 else list(reversed(range(n)))
    faces = [(n, order[i], order[(i+1) % n]) for i in range(n)]
    faces.append(tuple(i+n+1 for i in reversed(order)))
    faces += [(order[(i+1) % n], order[i], order[i]+n+1,
               order[(i+1) % n]+n+1) for i in range(n)]
    obj = a.mesh(col, name, verts, faces, colors[0])
    palette = list(dict.fromkeys(tuple(colors) + ('Hair3',)))
    for color in palette[1:]:
        obj.data.materials.append(a.mat(color))
    for i, polygon in enumerate(obj.data.polygons):
        color = colors[i % len(colors)] if i < n else 'Hair3'
        polygon.material_index = palette.index(color)
    return obj


def _flat_mark(col, name, outline, color, depth=.012):
    """A shallow closed polygon for graphic facial marks without roundness."""
    n = len(outline)
    verts = list(outline) + [(x, y+depth, z) for x, y, z in outline]
    area = sum(outline[i][0] * outline[(i+1) % n][2]
               - outline[(i+1) % n][0] * outline[i][2] for i in range(n))
    order = list(range(n)) if area > 0 else list(reversed(range(n)))
    faces = [tuple(order), tuple(i+n for i in reversed(order))]
    faces += [(order[(i+1) % n], order[i], order[i]+n,
               order[(i+1) % n]+n) for i in range(n)]
    return a.mesh(col, name, verts, faces, color)


def _face(col):
    profile = [(-.355, -.410), (.355, -.410), (.510, -.207),
               (.445, .220), (.240, .352), (-.240, .352),
               (-.445, .220), (-.510, -.207)]
    rings = [(3.550, .63, .89), (3.680, .92, 1),
             (4.045, 1, 1), (4.375, .93, .96)]
    verts = [(x*sx, y*sy+.030, z)
             for z, sx, sy in rings for x, y in profile]
    faces = [tuple(reversed(range(8)))]
    assignments = [1]
    for j in range(3):
        for i in range(8):
            faces.append((j*8+i, j*8+(i+1) % 8,
                          (j+1)*8+(i+1) % 8, (j+1)*8+i))
            assignments.append(0 if i in (0, 1, 7) else 1)
    faces.append(tuple(range(24, 32)))
    assignments.append(0)
    face = a.mesh(col, 'Head_FacetedFace', verts, faces, 'Skin1')
    _planes(face, ['Skin1', 'Skin3'], assignments)
    a.box(col, 'Head_Neck', (0, .030, 3.485), (.370, .360, .270), 'Skin3', .018)

    for side, suffix in ((-1, 'L'), (1, 'R')):
        _flat_mark(col, 'Head_Ear_' + suffix,
                   [(side*.455, -.213, 4.070), (side*.554, -.208, 4.055),
                    (side*.566, -.211, 3.887), (side*.491, -.220, 3.830),
                    (side*.444, -.233, 3.883)], 'Skin3', .13)

        # Each eye is about 2 by 3 pixels in the intended 82x140 thumbnail.
        a.box(col, 'Face_Eye_' + suffix, (side*.205, -.395, 3.983),
              (.075, .020, .106), 'Navy', .006)
        _flat_mark(col, 'Face_Blush_' + suffix,
                   [(side*.245, -.395, 3.867), (side*.363, -.389, 3.867),
                    (side*.354, -.390, 3.824), (side*.253, -.395, 3.824)],
                   'Strawberry', .010)

    # A tiny rising-corner smile; no brows or protruding nose are needed.
    _flat_mark(col, 'Face_TinySmile',
               [(-.057, -.399, 3.795), (-.038, -.400, 3.792),
                (-.025, -.402, 3.777), (.025, -.402, 3.777),
                (.038, -.400, 3.792), (.057, -.399, 3.795),
                (.039, -.401, 3.760), (-.039, -.401, 3.760)],
               'Navy', .009)


def _cap(col):
    low = [(0, -.357, 4.265), (.372, -.297, 4.238),
           (.515, .024, 3.963), (.386, .318, 3.915),
           (0, .434, 3.916), (-.390, .320, 3.943),
           (-.521, .027, 3.984), (-.377, -.292, 4.241)]
    high = [(-.085, -.288, 4.610), (.287, -.207, 4.564),
            (.468, .039, 4.427), (.316, .319, 4.478),
            (-.012, .394, 4.584), (-.328, .299, 4.621),
            (-.469, .018, 4.491), (-.348, -.211, 4.605)]
    verts = low + high + [(-.112, .038, 4.738)]
    faces, indices = [], []
    for i in range(8):
        faces.append((i, (i+1) % 8, (i+1) % 8+8, i+8))
        indices.append(0 if i in (0, 1, 7) else 1)
        faces.append((i+8, (i+1) % 8+8, 16))
        indices.append(0)
    cap = a.mesh(col, 'Hair_FacetedCap', verts, faces, 'Hair1')
    _planes(cap, ['Hair1', 'Hair3'], indices)


def build_head(col):
    """Build the shopper's face and fourteen large asymmetric hair locks."""
    _face(col)
    _cap(col)

    # Eight contour clumps: a contemporary swept silhouette with a raised
    # part at the left.  Secondary colors are restricted to a few lower faces.
    perimeter = [
        ('Hair_CrownLift',
         [(-.288, -.106, 4.900), (-.021, -.111, 4.811),
          (.103, -.226, 4.574), (-.245, -.192, 4.571)],
         (-.136, -.279, 4.717), ('Hair1',)),
        ('Hair_CrownRight',
         [(-.084, .003, 4.759), (.254, -.014, 4.799),
          (.352, -.160, 4.547), (.039, -.248, 4.563)],
         (.144, -.246, 4.690), ('Hair1',)),
        ('Hair_LeftUpper',
         [(-.533, .014, 4.683), (-.262, -.095, 4.758),
          (-.188, -.255, 4.533), (-.461, -.190, 4.441)],
         (-.354, -.278, 4.587), ('Hair1',)),
        ('Hair_LeftOuter',
         [(-.692, .010, 4.489), (-.478, -.141, 4.569),
          (-.319, -.296, 4.323), (-.556, -.155, 4.250)],
         (-.466, -.287, 4.423), ('Hair1', 'Hair1', 'Hair3')),
        ('Hair_LeftLower',
         [(-.529, -.083, 4.338), (-.387, -.251, 4.274),
          (-.465, -.212, 4.037), (-.635, -.012, 4.105)],
         (-.499, -.284, 4.206), ('Hair1', 'Hair3', 'Hair1')),
        ('Hair_RightUpper',
         [(.227, -.041, 4.631), (.558, .014, 4.616),
          (.473, -.164, 4.359), (.203, -.281, 4.446)],
         (.394, -.260, 4.520), ('Hair1',)),
        ('Hair_RightOuter',
         [(.420, -.033, 4.487), (.703, .036, 4.399),
          (.552, -.036, 4.167), (.331, -.238, 4.311)],
         (.513, -.246, 4.354), ('Hair1', 'Hair3', 'Hair1')),
        ('Hair_RightLower',
         [(.459, -.049, 4.264), (.604, .033, 4.161),
          (.518, -.009, 3.969), (.385, -.216, 4.089)],
         (.493, -.238, 4.134), ('Hair3', 'Hair1', 'Plum')),
    ]
    for name, outline, ridge, colors in perimeter:
        _lock(col, name, outline, ridge, colors, .17)

    # Six broad locks overlap in the same direction; the forehead opening
    # and clear eye line make the expression readable at game scale.
    fringe = [
        ('Hair_LeftTemple',
         [(-.445, -.300, 4.347), (-.335, -.402, 4.279),
          (-.395, -.411, 3.858), (-.486, -.321, 3.985)],
         (-.425, -.452, 4.149), ('Hair1', 'Hair3', 'Hair1')),
        ('Hair_LeftPart',
         [(-.295, -.308, 4.747), (-.130, -.385, 4.609),
          (-.309, -.465, 4.228), (-.492, -.321, 4.406)],
         (-.319, -.516, 4.520), ('Hair1',)),
        ('Hair_WideTopSweep',
         [(-.311, -.345, 4.736), (.024, -.350, 4.671),
          (.422, -.383, 4.422), (.031, -.486, 4.449)],
         (-.080, -.526, 4.628), ('Hair1',)),
        ('Hair_LongSideSweep',
         [(-.205, -.441, 4.599), (.155, -.434, 4.506),
          (.391, -.469, 4.073), (.086, -.505, 4.312)],
         (.050, -.555, 4.431), ('Hair1',)),
        ('Hair_RightFringe',
         [(.104, -.417, 4.524), (.392, -.352, 4.444),
          (.484, -.395, 4.106), (.276, -.487, 4.257)],
         (.325, -.521, 4.371), ('Hair1', 'Hair1', 'Hair3')),
        ('Hair_RightTemple',
         [(.398, -.314, 4.328), (.510, -.267, 4.236),
          (.474, -.334, 3.862), (.411, -.408, 3.990)],
         (.466, -.440, 4.126), ('Hair1', 'Hair3', 'Hair1')),
    ]
    for name, outline, ridge, colors in fringe:
        _lock(col, name, outline, ridge, colors, .15)

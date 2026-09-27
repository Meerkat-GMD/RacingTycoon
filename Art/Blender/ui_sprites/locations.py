"""Location sceneries: four lowpoly dioramas, each rendered with a Prep and a Street camera.

Category module for build_ui_sprites.py (`--category locations`). Catalog ids
`Location_{0..3}_{Prep,Street}`; index 0 동네 골목, 1 시장 앞, 2 강변 축제, 3 별빛 광장.

Stage frame. Every diorama is built in stage units: +X runs along the frame,
+Y goes into the picture, +Z is up. A root empty turns the stage by the camera
yaw (-20 deg) so the ground bands (sidewalk, lane, river) run level across the
frame, and scales it by STAGE_SCALE so the diorama stays near the aim point of
the shared studio lights (the rig is fixed at character scale). Buildings and
props are turned back by about +20 deg on the stage, so the camera sees them at
the same -20 deg / 15 deg three-quarter view as the approved characters.

Both cameras look the same way (orthographic, yaw -20, elevation 15), so the
Prep and Street sprites of one location are two windows on one projection of
one diorama (the Street copy is set-dressed for its overlays, below): a stage
point (x, y, z) lands at frame x = x and at frame height rise(y, z). Street
(596x314 in game) leaves the overlay zones empty, as the spec asks
("그 영역을 비워 두고"):
- storefront zone, left of the first customer target (x < 30 %): ground, hills
  and sky only;
- customer and bubble band (x 30-98 %, y 15-90 % from the top, STREET_BAND):
  no dark or detailed prop at all. Only the back-row landmarks (houses, the
  alley and its lamp post, market hall and stalls, festival booths, ferris
  wheel) and the calm yard wall or balustrade reach into it, and the ground
  below them is plain lane, square or river with nothing standing on it;
- back row: every landmark stands on the footing line BACK_Y, 40 % down the
  frame behind the bubble bottoms, and stays under the top edge;
- bottom 11 %: a plain Cream sidewalk band for the Navy status text.
The Street scene therefore leaves out the props that would stand in the band
(the plaza's lamp posts, crates, pots, laundry, bushes, hedges, trees,
topiaries, bunting poles, the ticket kiosk), hangs the bunting from the booths'
raised masts, puts the market hall door (with a narrower pediment), the alley's
lamp post (set back behind the yard wall, which hides its foot) and a smaller
ferris wheel under the middle speech bubble, and drops the two house windows
that showed in the gaps between bubbles. Everything else is built identically
for both framings (Stage.street), and the Prep scenes are unchanged.
Prep (588x354) is a closer window on the back row and on the pieces that stand
right of the Street frame (the riverside bridge, a market crate stack), so it
shows the landmark prominently. Both frames are filled with geometry (sky wall,
hills, ground), never with the world colour.

Overlay check after rendering (plain Python + Pillow, no Blender):
    python Art/Blender/ui_sprites/locations.py   -> Art/Blender/previews/locations-street-composite.png
"""
import argparse
import math
import random
import sys
from pathlib import Path

try:
    import bpy
    from mathutils import Vector
except ImportError:  # plain Python: only the Street overlay preview at the end of this file is usable
    bpy = Vector = SEED = None
else:
    from ui_sprite_spec import SEED

CATEGORY = 'locations'
YAW, ELEVATION = -20, 15
STAGE_TURN = YAW          # stage X follows the frame
PIECE_TURN = 20           # props keep the characters' three-quarter view
STAGE_SCALE = .5          # stage unit -> metre
NAMES = ('동네 골목', '시장 앞', '강변 축제', '별빛 광장')
KEYS = ('alley', 'market', 'riverside', 'starlight')
SIN_E, COS_E = math.sin(math.radians(ELEVATION)), math.cos(math.radians(ELEVATION))


def rise(y, z=0.0):
    """Height of a stage point in the picture plane shared by both cameras (stage units)."""
    return y*SIN_E + z*COS_E


# Game overlays on the Street sprite (x, y, w, h from the top-left of the 596x314 frame; ShopStreetUI).
STREET_SIZE, PREP_SIZE = (596, 314), (588, 354)
STREET_OVERLAYS = {'storefront': [6, 48, 169, 230], 'customer_targets': [[178 + s*136, 8, 132, 276] for s in range(3)],
                   'customer_art_in_target': [25, 132, 82, 140], 'speech_bubble_in_target': [0, 0, 132, 130],
                   'status_text': [13, 288, 570, 22]}
_TARGETS, _ART = STREET_OVERLAYS['customer_targets'], STREET_OVERLAYS['customer_art_in_target']
# Street zones as fractions of the frame (x from the left, y from the top).
STREET_ZONES = {'storefront_right': _TARGETS[0][0]/STREET_SIZE[0],
                'customers_right': (_TARGETS[-1][0] + _TARGETS[-1][2])/STREET_SIZE[0],
                'back_row_footing': .40,
                'heads_top': (_TARGETS[0][1] + _ART[1])/STREET_SIZE[1],
                'curb': .80,
                'feet': (_TARGETS[0][1] + _ART[1] + _ART[3])/STREET_SIZE[1]}
# Customer and bubble band (x0, x1, y0, y1 as fractions, y from the top): no dark or detailed prop in it.
STREET_BAND = (.30, .98, .15, .90)

STREET_WIDTH = 9.5        # stage units across the Street frame
STREET_HEIGHT = STREET_WIDTH*STREET_SIZE[1]/STREET_SIZE[0]
BACK_Y = 3.0              # footing line of the back row; nothing but ground stands in front of it
SIDEWALK_TOP = .08
SKY_Y = 6.0               # sky wall foot
SKY_LEAN = .5
HILL_Y = 5.55
AIM_Z = 1.0               # camera targets sit at this height on their view line
STREET_RISE = rise(BACK_Y) - (.5 - STREET_ZONES['back_row_footing'])*STREET_HEIGHT   # Street frame centre
TOP_RISE = STREET_RISE + STREET_HEIGHT/2       # Street top edge; the back row stays under it


def street_x(frac):
    return (frac - .5)*STREET_WIDTH


def street_ground_y(frac, z=0.0):
    """Stage y of ground at height z that the Street frame shows `frac` of the way down."""
    return (STREET_RISE + (.5 - frac)*STREET_HEIGHT - z*COS_E)/SIN_E


FEET_Y = street_ground_y(STREET_ZONES['feet'], SIDEWALK_TOP)    # customer/storefront footing line (89 %)
CURB_Y = street_ground_y(STREET_ZONES['curb'], SIDEWALK_TOP)    # back edge of the front sidewalk (80 %)
FRONT_Y = street_ground_y(1.0, SIDEWALK_TOP) - .8                 # the sidewalk band runs past the bottom edge
STOREFRONT_X = street_x(STREET_ZONES['storefront_right'])        # right edge of the storefront zone
TILE = STREET_WIDTH*(_TARGETS[1][0] - _TARGETS[0][0])/STREET_SIZE[0]              # one customer slot
TILE_JOINT = street_x((_TARGETS[0][0] + _TARGETS[0][2] + 2)/STREET_SIZE[0])       # joint between slots 0 and 1
SLOT_X = [street_x((x + w/2)/STREET_SIZE[0]) for x, _, w, _ in _TARGETS]          # centre of each customer slot

# Prep windows on the same projection: (centre x, centre rise, width) in stage units.
PREP_WINDOWS = {0: (1.75, 1.72, 6.4), 1: (2.45, 1.55, 6.3), 2: (4.9, 1.45, 7.4), 3: (1.6, 1.6, 5.6)}


def world(point):
    """Stage point -> Blender world point (turned by STAGE_TURN, scaled by STAGE_SCALE)."""
    x, y, z = (c*STAGE_SCALE for c in point)
    a = math.radians(STAGE_TURN)
    return (round(x*math.cos(a) - y*math.sin(a), 5), round(x*math.sin(a) + y*math.cos(a), 5), round(z, 5))


def windows(location):
    """Stage-space frames of one location: name -> (centre x, centre rise, width, height)."""
    x, v, width = PREP_WINDOWS[location]
    return {'street': (0.0, STREET_RISE, STREET_WIDTH, STREET_HEIGHT),
            'prep': (x, v, width, width*PREP_SIZE[1]/PREP_SIZE[0])}


def cut_by_frame(location, x, y, z, rx, rv=None):
    """True when a decoration reaching rx across and rv up/down from stage (x, y, z) straddles
    an edge of either frame (the Street frame or this location's Prep frame)."""
    v, rv = rise(y, z), rx if rv is None else rv
    for cx, cv, width, height in windows(location).values():
        dx, dv = abs(x - cx), abs(v - cv)
        inside = dx <= width/2 - rx and dv <= height/2 - rv
        outside = dx >= width/2 + rx or dv >= height/2 + rv
        if not (inside or outside):
            return True
    return False


def _asset(index, framing):
    x, v, width = (0.0, STREET_RISE, STREET_WIDTH) if framing == 'street' else PREP_WINDOWS[index]
    target = (x, (v - AIM_Z*COS_E)/SIN_E, AIM_Z)
    return {'id': 'Location_%d_%s' % (index, framing.title()),
            'camera': {'target': world(target), 'scale': round(width*STAGE_SCALE, 5), 'yaw': YAW, 'elevation': ELEVATION},
            'shadow': None,
            'opaque_backdrop': True,
            'params': {'location': index, 'name': NAMES[index], 'key': KEYS[index], 'framing': framing,
                       'stage_target': [round(c, 4) for c in target], 'stage_scale': width, 'stage_to_metre': STAGE_SCALE,
                       'stage_turn_deg': STAGE_TURN, 'piece_turn_deg': PIECE_TURN, 'back_row_stage_y': BACK_Y,
                       'overlays': STREET_OVERLAYS if framing == 'street' else None,
                       'street_zones': {k: round(f, 4) for k, f in STREET_ZONES.items()} if framing == 'street' else None,
                       'street_band': list(STREET_BAND) if framing == 'street' else None}}


ASSETS = [_asset(i, framing) for framing in ('prep', 'street') for i in range(4)]

FLAG_FACES = [(0, 2, 1), (3, 4, 5), (0, 1, 4, 3), (1, 2, 5, 4), (2, 0, 3, 5)]


class Stage:
    """Builds into one turned and scaled root; every coordinate here is in stage units."""

    def __init__(self, col, kit, location, framing='prep'):
        self.col, self.kit, self.location = col, kit, location
        self.street = framing == 'street'   # Street leaves the band props out (module docstring)
        self.rng = random.Random(SEED + location)
        self.used = {}
        self.root = self.empty('Stage', (0, 0, 0), turn=STAGE_TURN, parent=False)
        self.root.scale = (STAGE_SCALE,)*3

    def uid(self, name):
        n = self.used.get(name, 0)
        self.used[name] = n + 1
        return name if n == 0 else '%s_%02d' % (name, n)

    def empty(self, name, loc, turn=0.0, parent=None):
        ob = bpy.data.objects.new(self.kit.prefix + self.uid(name), None)
        ob.empty_display_size = .3
        self.col.objects.link(ob)
        ob.location = loc
        ob.rotation_euler.z = math.radians(turn)
        if parent is not False:
            ob.parent = parent or self.root
        return ob

    def adopt(self, ob, parent):
        ob.parent = parent or self.root
        return ob

    def box(self, name, loc, dims, color, parent=None, bevel=.03):
        return self.adopt(self.kit.box(self.col, self.uid(name), loc, dims, color, bevel), parent)

    def ico(self, name, loc, scale, color, parent=None, subdivisions=1):
        return self.adopt(self.kit.ico(self.col, self.uid(name), loc, scale, color, subdivisions), parent)

    def seg(self, name, a, b, width, depth, color, parent=None, bevel=.01):
        return self.adopt(self.kit.segment(self.col, self.uid(name), a, b, width, depth, color, bevel), parent)

    def mesh(self, name, verts, faces, colors, parent=None, bevel=0.0):
        """Flat-shaded mesh; `colors` is one palette name or one name per face."""
        per_face = [colors]*len(faces) if isinstance(colors, str) else list(colors)
        order = list(dict.fromkeys(per_face))
        ob = self.kit.mesh(self.col, self.uid(name), verts, faces, order[0])
        for extra in order[1:]:
            ob.data.materials.append(self.kit.mat(extra))
        for poly, color in zip(ob.data.polygons, per_face):
            poly.material_index = order.index(color)
            poly.use_smooth = False
        if bevel:
            mod = ob.modifiers.new('Single flat chamfer', 'BEVEL')
            mod.width = bevel
            mod.segments = 1
            mod.limit_method = 'ANGLE'
        return self.adopt(ob, parent)

    def prism(self, name, loc, radius, depth, color, parent=None, sides=8, axis='z', bevel=.015, top=None):
        """Faceted cylinder, or frustum when a `top` radius is given, along one axis."""
        top = radius if top is None else top
        ring = [(math.cos((i + .5)*math.tau/sides), math.sin((i + .5)*math.tau/sides)) for i in range(sides)]
        verts = []
        for r, h in ((radius, -depth/2), (top, depth/2)):
            for c, s in ring:
                verts.append({'z': (c*r, s*r, h), 'x': (h, c*r, s*r), 'y': (c*r, h, s*r)}[axis])
        faces = [tuple(reversed(range(sides))), tuple(range(sides, 2*sides))]
        faces += [(i, (i + 1) % sides, sides + (i + 1) % sides, sides + i) for i in range(sides)]
        ob = self.mesh(name, verts, faces, color, parent, bevel)
        ob.location = loc
        return ob

    def slab(self, name, a, b, width, thick, color, parent=None, bevel=.012):
        """Plank from a to b (in a YZ plane) whose thickness sits above the a-b line."""
        a, b = Vector(a), Vector(b)
        n = (b - a).cross(Vector((1, 0, 0)))
        if n.z < 0:
            n = -n
        n.normalize()
        return self.seg(name, a + n*thick/2, b + n*thick/2, width, thick, color, parent, bevel)

    def flag(self, name, a, b, drop, color, parent=None, thick=.025):
        """Triangular pennant hanging from the a-b edge."""
        a, b = Vector(a), Vector(b)
        tip = (a + b)/2 - Vector((0, 0, drop))
        verts = [tuple(a), tuple(b), tuple(tip)]
        verts += [(x, y + thick, z) for x, y, z in verts]
        return self.mesh(name, verts, FLAG_FACES, color, parent)

    # ------------------------------------------------------------------ backdrop
    def sky(self, bands, x0=-9.0, x1=12.0, z0=-.5, z1=2.6, nx=40, nz=12, jitter=.07):
        """Leaning faceted sky wall. `bands` lists (lowest z, colour) from the top down; each
        triangle takes the band of its centre, so band edges follow the jittered facet rows."""
        rng = self.rng
        row = (z1 - z0)/nz
        verts, faces, cols = [], [], []
        for j in range(nz + 1):
            for i in range(nx + 1):
                x = x0 + (x1 - x0)*i/nx + (rng.uniform(-.3, .3)*(x1 - x0)/nx if 0 < i < nx else 0)
                z = z0 + row*j + (rng.uniform(-.25, .25)*row if 0 < j < nz else 0)
                verts.append((x, self.sky_y(z) + rng.uniform(-jitter, jitter), z))
        for j in range(nz):
            for i in range(nx):
                a, b, c, d = j*(nx + 1) + i, j*(nx + 1) + i + 1, (j + 1)*(nx + 1) + i + 1, (j + 1)*(nx + 1) + i
                for tri in ([(a, b, c), (a, c, d)] if (i + j) % 2 == 0 else [(a, b, d), (b, c, d)]):
                    zc = sum(verts[k][2] for k in tri)/3
                    faces.append(tri)
                    cols.append(next(color for edge, color in bands if zc >= edge))
        return self.mesh('Sky_Wall', verts, faces, cols)

    @staticmethod
    def sky_y(z, z0=-.5):
        return SKY_Y + SKY_LEAN*(z - z0)

    def field(self, name, y0, y1, z, ny, color_at, x0=-9.0, x1=12.0, nx=12, jz=0.0, jxy=.2):
        """Faceted ground patch; `color_at(i, j, rng)` picks each triangle's colour from its grid cell."""
        rng = self.rng
        verts, faces, cols = [], [], []
        for j in range(ny + 1):
            for i in range(nx + 1):
                inner = 0 < i < nx and 0 < j < ny
                x = x0 + (x1 - x0)*i/nx + (rng.uniform(-jxy, jxy)*(x1 - x0)/nx if 0 < i < nx else 0)
                y = y0 + (y1 - y0)*j/ny + (rng.uniform(-jxy, jxy)*(y1 - y0)/ny if 0 < j < ny else 0)
                verts.append((x, y, z + (rng.uniform(0, jz) if inner else 0)))
        for j in range(ny):
            for i in range(nx):
                a, b, c, d = j*(nx + 1) + i, j*(nx + 1) + i + 1, (j + 1)*(nx + 1) + i + 1, (j + 1)*(nx + 1) + i
                for tri in ([(a, b, c), (a, c, d)] if (i + j) % 2 else [(a, b, d), (b, c, d)]):
                    faces.append(tri)
                    cols.append(color_at(i, j, rng))
        return self.mesh(name, verts, faces, cols)

    def hills(self, name, peaks, colors, x0=-9.5, x1=12.5, depth=.6, bulge=.25):
        """A row of faceted mounds in front of the sky wall's foot; `peaks` are their heights."""
        rng = self.rng
        step = (x1 - x0)/len(peaks)
        for k, h in enumerate(peaks):
            cx = x0 + step*(k + .5) + rng.uniform(-.15, .15)*step
            r = step*rng.uniform(.8, 1.0)
            sides = 6
            arc = [(cx - math.cos(math.pi*i/sides)*r, HILL_Y + rng.uniform(-.1, .1),
                    -.2 + (h + .2)*math.sin(math.pi*i/sides)**.8*(1 if i in (0, sides) else rng.uniform(.9, 1.05)))
                   for i in range(sides + 1)]
            front = [(cx + rng.uniform(-.2, .2)*r, HILL_Y - bulge, h*.42)] + arc
            back = [(x, yy + depth, z) for x, yy, z in front]
            n = len(front)
            faces = [(0, i, i + 1) for i in range(1, n - 1)] + [(0, n - 1, 1)]
            faces += [(n, n + i + 1, n + i) for i in range(1, n - 1)]
            faces += [(i, i + 1, n + i + 1, n + i) for i in range(1, n - 1)] + [(n - 1, 1, n + 1, 2*n - 1)]
            self.mesh('%s_%02d' % (name, k), front + back, faces, colors[k % len(colors)])

    def sidewalk(self, tile='Cream', grout='Base', curb='White', band='Cream', x0=-9.0, x1=12.0):
        """Front sidewalk: a plain band in front of the footing line (the Street status-text band),
        one row of slot-wide tiles behind it (joints fall between the customers) and a curb at CURB_Y."""
        self.box('Sidewalk_Bed', ((x0 + x1)/2, (FEET_Y + CURB_Y)/2, SIDEWALK_TOP/2 - .04),
                 (x1 - x0, CURB_Y - FEET_Y + .2, SIDEWALK_TOP), grout, bevel=0)
        self.box('Sidewalk_Band', ((x0 + x1)/2, (FEET_Y + FRONT_Y)/2 - .03, SIDEWALK_TOP/2),
                 (x1 - x0, FEET_Y - FRONT_Y - .06, SIDEWALK_TOP), band, bevel=.015)
        a, b = FEET_Y + .02, CURB_Y - .22
        x = TILE_JOINT - TILE*math.ceil((TILE_JOINT - x0)/TILE)
        while x < x1:
            self.box('Sidewalk_Tile', (x + TILE/2, (a + b)/2, SIDEWALK_TOP - .03), (TILE - .05, b - a - .05, .06), tile, bevel=.012)
            x += TILE
        self.box('Curb', ((x0 + x1)/2, CURB_Y - .1, SIDEWALK_TOP - .01), (x1 - x0, .2, .1), curb, bevel=.02)

    # ------------------------------------------------------------------ props
    def house(self, name, loc, w, d, h, wall, roof, turn=PIECE_TURN, roof_h=None, windows=2, door='Wood',
              chimney=False, side_window=True, glass='Soda', trim=None, parent=None):
        g = self.empty(name, loc, turn, parent)
        self.box(name + '_Walls', (0, 0, h/2), (w, d, h), wall, g, bevel=.035)
        if trim:
            self.box(name + '_Plinth', (0, 0, .08), (w + .05, d + .05, .16), trim, g, bevel=.02)
        rh = roof_h if roof_h is not None else d*.48
        gable = [(-w/2, -d/2, h), (w/2, -d/2, h), (w/2, d/2, h), (-w/2, d/2, h), (-w/2, 0, h + rh), (w/2, 0, h + rh)]
        self.mesh(name + '_Gable', gable, [(0, 1, 5, 4), (2, 3, 4, 5), (0, 4, 3), (1, 2, 5), (0, 3, 2, 1)], wall, g)
        overhang = .16
        slope = Vector((0, d/2, rh)).normalized()
        for side, sign in (('Front', -1), ('Back', 1)):
            eave = Vector((0, sign*d/2, h)) - Vector((0, sign*slope.y, slope.z))*overhang
            self.slab(name + '_Roof' + side, eave, Vector((0, sign*.03, h + rh + .02)), w + .3, .09, roof, g, bevel=.02)
        if chimney:
            self.box(name + '_Chimney', (w*.26, -d*.12, h + rh*.62), (.22, .22, rh*.7), wall, g, bevel=.02)
            self.box(name + '_ChimneyCap', (w*.26, -d*.12, h + rh*.99), (.3, .3, .07), roof, g, bevel=.015)
        front = -d/2 - .02
        if door:
            dx = -w*.24 if windows else 0
            self.box(name + '_Door', (dx, front, .34), (.3, .05, .6), door, g, bevel=.012)
            self.box(name + '_Step', (dx, front - .08, .04), (.44, .16, .08), 'White', g, bevel=.012)
            xs = {0: [], 1: [w*.18], 2: [w*.06, w*.3]}[windows]
        else:
            xs = [(-.5 + (k + .5)/windows)*w*.75 for k in range(windows)]
        for x in xs:
            self.window(name + '_Window', (x, front, h*.6), glass, g)
        if side_window:
            self.window(name + '_SideWindow', (-w/2 - .02, 0, h*.6), glass, g, side=True)
        return g

    def window(self, name, loc, glass, parent, side=False, size=(.24, .28)):
        g = self.empty(name, loc, -90 if side else 0, parent)
        self.box(name + '_Frame', (0, 0, 0), (size[0] + .08, .04, size[1] + .08), 'White', g, bevel=.01)
        self.box(name + '_Glass', (0, -.02, .01), (size[0], .03, size[1] - .02), glass, g, bevel=.006)
        self.box(name + '_Sill', (0, -.05, -size[1]/2 - .04), (size[0] + .14, .08, .05), 'Cream', g, bevel=.01)
        return g

    def lamp(self, name, loc, height=1.4, pole='Navy', globe='Vanilla', parent=None):
        g = self.empty(name, loc, PIECE_TURN, parent)
        self.prism(name + '_Foot', (0, 0, .08), .13, .16, pole, g, sides=8)
        self.prism(name + '_Pole', (0, 0, height/2), .045, height, pole, g, sides=6, bevel=.008)
        self.prism(name + '_Collar', (0, 0, height + .02), .1, .06, 'Gold', g, sides=8, bevel=.01)
        self.ico(name + '_Globe', (0, 0, height + .2), (.17, .17, .19), globe, g)
        self.prism(name + '_Cap', (0, 0, height + .4), .12, .1, pole, g, sides=8, top=.02, bevel=.008)
        return g

    def tree(self, name, loc, size=1.0, canopy=('Mint', 'Mint', 'Base', 'Mint'), parent=None):
        g = self.empty(name, loc, PIECE_TURN + self.rng.uniform(-25, 25), parent)
        self.prism(name + '_Trunk', (0, 0, .35*size), .07*size, .7*size, 'Wood', g, sides=6, top=.05*size, bevel=.01)
        blobs = [((0, 0, .95), .42), ((-.26, .05, .8), .3), ((.25, -.02, .82), .31), ((.03, -.12, 1.18), .27)]
        for k, ((x, y, z), r) in enumerate(blobs):
            self.ico(name + '_Leaves', (x*size, y*size, z*size), (r*size, r*size*.92, r*size*.85), canopy[k % len(canopy)], g)
        return g

    def bush(self, name, loc, size=1.0, color='Mint', parent=None):
        g = self.empty(name, loc, self.rng.uniform(0, 60), parent)
        for x, r in ((-.18, .2), (.05, .26), (.26, .18)):
            self.ico(name + '_Leaf', (x*size, 0, r*size*.7), (r*size, r*size*.9, r*size*.8), color, g)
        return g

    def pot(self, name, loc, color='Strawberry', leaf='Mint', parent=None):
        g = self.empty(name, loc, PIECE_TURN, parent)
        self.prism(name + '_Pot', (0, 0, .09), .1, .18, 'Wood', g, sides=6, top=.13, bevel=.008)
        self.ico(name + '_Leaves', (0, 0, .27), (.15, .15, .13), leaf, g)
        self.ico(name + '_Bloom', (.05, -.06, .36), (.06, .06, .05), color, g)
        return g

    def lantern(self, name, loc, color, parent=None):
        """Floating festival lantern: a Cream float with a small glowing-colour shade."""
        g = self.empty(name, loc, self.rng.uniform(0, 40), parent)
        self.prism(name + '_Float', (0, 0, .03), .16, .06, 'Cream', g, sides=6, bevel=.01)
        self.prism(name + '_Shade', (0, 0, .15), .09, .18, color, g, sides=6, top=.06, bevel=.01)
        self.prism(name + '_Cap', (0, 0, .26), .05, .04, 'Gold', g, sides=6, bevel=.005)
        return g

    def crate(self, name, loc, fruit=None, turn=PIECE_TURN, parent=None, size=(.46, .34, .28)):
        g = self.empty(name, loc, turn, parent)
        w, d, h = size
        self.box(name + '_Box', (0, 0, h/2), size, 'Wood', g, bevel=.02)
        for z in (h*.3, h*.72):
            self.box(name + '_Slat', (0, -d/2 - .012, z), (w - .04, .025, h*.2), 'Cream', g, bevel=.006)
        if fruit:
            for x, y in ((-.12, -.05), (.1, -.06), (0, .07), (-.13, .08), (.13, .07)):
                self.ico(name + '_Fruit', (x*w/.46, y*d/.34, h + .05), (.075*w/.46, .075*w/.46, .07*w/.46), fruit, g)
        return g

    def awning(self, name, x0, x1, y_front, y_back, z_front, z_back, colors, parent, stripes=6):
        """Striped sloping canopy with a scalloped front edge."""
        step = (x1 - x0)/stripes
        for k in range(stripes):
            color = colors[k % len(colors)]
            x = x0 + step*(k + .5)
            self.slab(name + '_Stripe', (x, y_front, z_front), (x, y_back, z_back), step + .002, .05, color, parent, bevel=.008)
            self.flag(name + '_Scallop', (x0 + step*k, y_front - .03, z_front + .03), (x0 + step*(k + 1), y_front - .03, z_front + .03),
                      .18, color, parent)

    def pennants(self, name, a, b, colors, count, sag=.25, drop=.26, parent=None, line='Navy'):
        """A sagging line with triangular flags (bunting)."""
        a, b = Vector(a), Vector(b)

        def at(t):
            p = a.lerp(b, t)
            p.z -= sag*4*t*(1 - t)
            return p
        n = count*2
        for i in range(n):
            self.seg(name + '_Line', at(i/n), at((i + 1)/n), .018, .018, line, parent, bevel=0)
        for k in range(count):
            self.flag(name + '_Flag', at((k + .12)/count), at((k + .88)/count), drop, colors[k % len(colors)], parent)

    def star(self, name, loc, size, color='Vanilla', parent=None, points=5):
        verts = []
        for i in range(points*2):
            r = size if i % 2 == 0 else size*.45
            a = math.pi/2 + i*math.pi/points
            verts.append((loc[0] + math.cos(a)*r, loc[1], loc[2] + math.sin(a)*r))
        n = len(verts)
        verts += [(x, y + .03, z) for x, y, z in verts]
        faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
        return self.mesh(name, verts, faces, color, parent)

    def cloud(self, name, x, z, size=1.0, parent=None):
        """Cloud in front of the sky wall at height z; never placed across a frame edge."""
        loc = (x, self.sky_y(z) - .3, z)
        if cut_by_frame(self.location, loc[0], loc[1], loc[2], .78*size, .5*size):
            raise ValueError('%s at x %.2f z %.2f is cut by a frame edge' % (name, x, z))
        g = self.empty(name, loc, 0, parent)
        for px, pz, r in ((-.42, 0, .3), (0, .1, .42), (.4, .02, .32), (.15, -.08, .3), (-.15, -.1, .28)):
            self.ico(name + '_Puff', (px*size, 0, pz*size), (r*size, r*size*.6, r*size*.78), 'White', g)
        return g


# Street-only set dressing (module docstring): booth masts that carry the bunting, and the ferris wheel's
# set-back (stage units behind BACK_Y) and scale.
BOOTH_MAST = 1.86
WHEEL_STREET_BACK, WHEEL_STREET_SCALE = 1.02, .92

# Sky bands, top down: (lowest stage z, colour). Street sees the sky wall up to z ~1.05, Prep up to ~1.7.
DAY_SKY = [(-9, 'Soda')]
DUSK_SKY = [(.72, 'Navy'), (.36, 'Plum'), (.12, 'Strawberry'), (-9, 'Vanilla')]


def plain(color):
    return lambda i, j, rng: color


# ---------------------------------------------------------------------- locations
# Every piece that is not ground stands at stage y >= BACK_Y and at x >= STOREFRONT_X, or right
# of the Street frame (x > STREET_WIDTH/2) where only the Prep window sees it.
def alley(s):
    """0 동네 골목: low houses with pastel roofs, a lamp post and a narrow alley."""
    s.sky(DAY_SKY)
    s.hills('Hill', [.5, .7, .45, .65, .5, .7, .55, .6], ['Mint', 'Base'])
    s.field('Lane', CURB_Y - .1, BACK_Y - .1, 0, 3, plain('Base'))
    s.field('Yard', BACK_Y - .1, SKY_Y + .6, 0, 4, plain('Mint'), jz=.04)
    s.sidewalk()
    # Street: one front window, so none shows in the gap between bubbles 1 and 2
    s.house('HouseA', (-.62, 4.02, 0), 1.8, 1.2, .95, 'Cream', 'Strawberry', windows=1 if s.street else 2, chimney=True)
    s.house('HouseD', (4.95, 4.0, 0), 1.6, 1.2, .95, 'Cream', 'Vanilla', windows=1)
    s.house('HouseE', (6.85, 4.0, 0), 1.6, 1.2, 1.0, 'White', 'Strawberry', windows=2, chimney=True)
    s.house('HouseW', (8.7, 4.0, 0), 1.5, 1.2, .95, 'White', 'Soda', windows=1)
    # the narrow alley: two deep houses and the path between them share the three-quarter turn,
    # so the passage recedes up-left from the lane; a Cream path climbs a few steps to the house at its end
    passage = s.empty('Alley', (2.0, 4.27, 0), PIECE_TURN)
    gap = .78
    s.house('HouseB', (-(gap/2 + .78), .22, 0), 1.56, 1.3, .9, 'White', 'Mint', turn=0, roof_h=.5, windows=1, parent=passage)
    # Street: no side window on HouseC's alley wall, which shows in the gap between bubbles 2 and 3
    s.house('HouseC', (gap/2 + .82, -.05, 0), 1.64, 1.3, .95, 'Vanilla', 'Plum', turn=0, roof_h=.44, windows=2, trim='Cream',
            side_window=not s.street, parent=passage)
    s.house('HouseFar', (0, 1.0, 0), 1.3, .7, .78, 'White', 'Vanilla', turn=0, windows=1, side_window=False, parent=passage)
    s.box('AlleyPath', (0, -.3, .02), (gap - .08, 1.76, .04), 'Cream', passage, bevel=.01)
    for k in range(3):
        s.box('AlleyStep', (0, .2 + k*.15, .07 + k*.06), (gap - .08, .15, .06), 'White', passage, bevel=.01)
    if not s.street:   # the pots and the laundry line would peek out between the Street bubbles
        s.pot('Pot', (-gap/2 + .12, -.9, 0), 'Strawberry', parent=passage)
        s.pot('Pot', (gap/2 - .12, -.7, 0), 'Vanilla', parent=passage)
        s.pot('Pot', (gap/2 - .1, .0, 0), 'Strawberry', parent=passage)
        line = [Vector((-gap/2, -.1, 1.12)), Vector((gap/2, -.2, 1.16))]
        s.seg('Laundry_Line', line[0], line[1], .015, .015, 'Navy', passage, bevel=0)
        for t, color in ((.25, 'Strawberry'), (.5, 'White'), (.75, 'Soda')):
            q = line[0].lerp(line[1], t)
            s.box('Laundry', (q.x, q.y, q.z - .11), (.15, .03, .18), color, passage, bevel=.008)
    for x0, x1 in ((STOREFRONT_X + .06, 1.98), (2.86, 10.4)):
        s.box('YardWall', ((x0 + x1)/2, BACK_Y + .08, .18), (x1 - x0, .16, .36), 'White', bevel=.02)
        for x in (x0 + .12, x1 - .12):
            s.box('WallPier', (x, BACK_Y + .08, .22), (.24, .22, .44), 'Cream', bevel=.02)
    # the lamp post, the alley's landmark. Street: set back behind the yard wall, which hides the foot that
    # showed as a dark dot under bubble 2; bubble 2 covers the rest while slot 1 has a customer (its cap
    # stays 3 px under the bubble's top edge)
    if s.street:
        s.lamp('Lamp', (1.3, BACK_Y + .3, 0), height=1.3)
    else:
        s.lamp('Lamp', (1.3, BACK_Y + .02, 0))
    s.tree('Tree', (.55, 4.75, 0), .95)
    s.tree('Tree', (3.85, 4.85, 0), .9)
    s.tree('Tree', (7.8, 4.8, 0), .95)
    if not s.street:
        s.bush('Bush', (-1.45, BACK_Y + .45, 0), .6)
        s.bush('Bush', (4.0, BACK_Y + .4, 0), .6, 'Base')
    s.cloud('Cloud', -3.35, .74, .42)
    s.cloud('Cloud', .1, 1.5, .6)
    s.cloud('Cloud', 3.9, 1.38, .5)


def market(s):
    """1 시장 앞: a row of market stalls with Strawberry/Soda/Vanilla awnings and crates."""
    s.sky(DAY_SKY)
    s.hills('Hill', [.55, .75, .6, .8, .55, .7, .6, .7], ['Mint', 'Base'])
    s.field('Square', CURB_Y - .1, SKY_Y + .6, 0, 6, plain('Cream'))
    s.sidewalk(tile='White', curb='Cream')
    # market hall behind the stalls, from the storefront zone's edge to past the Prep frame
    # the Street door and sign sit behind the middle stall, under the middle bubble, not in a bubble gap
    hall_x0, hall_x1, door_x = STOREFRONT_X + .3, 10.2, SLOT_X[1] if s.street else 2.33
    hall = s.empty('Hall', ((hall_x0 + hall_x1)/2, 4.75, 0))
    width = hall_x1 - hall_x0
    local = door_x - (hall_x0 + hall_x1)/2
    s.box('Hall_Body', (0, 0, .5), (width, 1.0, 1.0), 'Cream', hall, bevel=.04)
    s.slab('Hall_Roof', (0, -.66, .95), (0, .3, 1.24), width + .3, .08, 'Mint', hall, bevel=.02)
    # central pediment carrying the market sign, above the gap between the middle stalls (Prep); the
    # Street one is narrower so its trim ends stay behind the middle stall, out of the bubble gaps
    half = .8 if s.street else 1.2
    tri = [(local - half, -.56, .95), (local + half, -.56, .95), (local, -.56, 1.58)]
    s.mesh('Hall_Pediment', tri + [(x, y + .5, z) for x, y, z in tri], FLAG_FACES, 'Cream', hall)
    for a, b in (((local - half - .1, -.6, .92), (local, -.6, 1.63)), ((local + half + .1, -.6, .92), (local, -.6, 1.63))):
        s.seg('Hall_PedimentTrim', a, b, .08, .06, 'Strawberry', hall, bevel=.01)
    s.box('Hall_Sign', (local, -.6, 1.2), (1.0, .05, .34), 'Mint', hall, bevel=.02)
    s.box('Hall_SignBoard', (local, -.64, 1.2), (.84, .04, .2), 'White', hall, bevel=.01)
    for x in (() if s.street else (.12, 4.55, 6.7, 8.85)):   # Street: they would show in the bubble gaps
        s.box('Hall_WindowFrame', (x - (hall_x0 + hall_x1)/2, -.5, .62), (.5, .03, .42), 'White', hall, bevel=.01)
        s.box('Hall_Window', (x - (hall_x0 + hall_x1)/2, -.52, .62), (.4, .04, .32), 'Soda', hall, bevel=.01)
    s.box('Hall_Door', (local, -.52, .38), (.72, .05, .72), 'Wood', hall, bevel=.015)
    stalls = [(-.95, ('Strawberry', 'Cream'), 'Strawberry'), (1.2, ('Soda', 'White'), 'Vanilla'),
              (3.45, ('Vanilla', 'Cream'), 'Mint'), (5.75, ('Strawberry', 'White'), 'Soda'), (8.0, ('Soda', 'Cream'), 'Strawberry')]
    for k, (x, colors, fruit) in enumerate(stalls):
        g = s.empty('Stall', (x, BACK_Y + .6, 0), 14 if k % 2 else 20)
        s.box('Stall_Counter', (0, 0, .3), (1.5, .6, .6), 'Wood', g, bevel=.03)
        s.box('Stall_Top', (0, -.02, .63), (1.62, .72, .07), 'Cream', g, bevel=.02)
        s.box('Stall_Front', (0, -.31, .32), (1.3, .03, .32), colors[1], g, bevel=.01)
        for px in (-.72, .72):
            s.seg('Stall_Post', (px, -.32, .64), (px, -.32, 1.33), .06, .06, 'Cream', g)
            s.seg('Stall_Post', (px, .3, .64), (px, .3, 1.52), .06, .06, 'Cream', g)
        s.awning('Stall_Awning', -.86, .86, -.52, .42, 1.33, 1.6, colors, g)
        for j, fx in enumerate((-.42, .02, .45)):
            s.crate('Stall_Crate', (fx, -.06, .665), fruit if j != 1 else ('Vanilla' if fruit != 'Vanilla' else 'Strawberry'),
                    turn=0, parent=g, size=(.36, .3, .15))
    if not s.street:   # these two stacks peeked out between the Street bubbles
        for x, fruit in ((.12, 'Strawberry'), (door_x, 'Vanilla')):
            stack = s.crate('Crate', (x, BACK_Y + .3, 0), None)
            s.crate('Crate', (0, 0, .28), fruit, turn=10, parent=stack)
    # Prep-only crate stack in front of the fourth stall, right of the Street frame
    stack = s.crate('Crate', (5.25, 2.3, 0), None)
    s.crate('Crate', (0, 0, .28), 'Soda', turn=10, parent=stack)
    s.pennants('Garland', (STOREFRONT_X + .15, 4.2, 1.64), (10.0, 4.2, 1.64), ['Strawberry', 'Vanilla', 'Soda', 'White'], 30,
               sag=.12, drop=.2)
    s.cloud('Cloud', -3.35, .74, .42)
    s.cloud('Cloud', .0, 1.42, .55)
    s.cloud('Cloud', 4.4, 1.4, .45)


def riverside(s):
    """2 강변 축제: a Soda river with a small bridge, bunting lines and festival booths."""
    s.sky(DAY_SKY)
    s.hills('Hill', [.7, .55, .8, .6, .75, .55, .8, .6], ['Mint', 'Base'])
    s.field('NearBank', CURB_Y - .1, .35, 0, 2, plain('Mint'))
    s.field('River', .35, 2.55, -.14, 4, plain('Soda'), nx=18, jz=.03, jxy=.3)
    s.field('FarBank', 2.55, SKY_Y + .6, 0, 4, plain('Mint'), jz=.04)
    s.sidewalk()
    for side, y in (('Near', .35), ('Far', 2.55)):
        s.box('Bank' + side, (1.5, y, -.08), (21, .2, .24), 'Cream', bevel=.02)
    # the river stays plain inside the Street frame; ripples, reeds and lanterns only right of it
    for x, y, w in ((5.4, 2.2, .5), (7.9, .75, .55), (8.3, 2.25, .45)):
        s.box('Ripple', (x, y, -.1), (w, .035, .015), 'White', bevel=0)
    # small arched footbridge right of the Street frame, turned so its arch reads in profile (Prep)
    bridge = s.empty('Bridge', (6.75, 1.45, 0), -42)
    n = 10
    pts = [Vector((0, -1.75 + 3.5*i/n, .05 + .6*math.sin(math.pi*i/n))) for i in range(n + 1)]
    for i in range(n):
        s.slab('Bridge_Deck', pts[i], pts[i + 1], .9, .1, 'Wood', bridge, bevel=.01)
        for x in (-.43, .43):
            s.seg('Bridge_Rail', pts[i] + Vector((x, 0, .38)), pts[i + 1] + Vector((x, 0, .38)), .05, .05, 'Cream', bridge, bevel=.006)
    for i in range(0, n + 1, 2):
        for x in (-.43, .43):
            s.seg('Bridge_Post', pts[i] + Vector((x, 0, .04)), pts[i] + Vector((x, 0, .42)), .07, .07, 'White', bridge, bevel=.008)
    booths = [(-.95, 'Strawberry'), (1.2, 'Vanilla'), (3.4, 'Soda'), (5.6, 'Strawberry'), (7.8, 'Vanilla'), (10.0, 'Soda')]
    for k, (x, roof) in enumerate(booths):
        g = s.empty('Booth', (x, BACK_Y + .62 + .18*(k % 2), 0), PIECE_TURN - 5*(k % 3))
        s.box('Booth_Body', (0, 0, .42), (1.1, .9, .84), 'White', g, bevel=.03)
        s.box('Booth_Counter', (0, -.47, .46), (1.16, .14, .08), 'Wood', g, bevel=.015)
        s.box('Booth_Window', (0, -.46, .66), (.8, .03, .32), 'Wood', g, bevel=.01)
        s.box('Booth_Awning', (0, -.56, .86), (.9, .2, .04), roof, g, bevel=.01)
        base = [(-.68, -.58, .84), (.68, -.58, .84), (.68, .58, .84), (-.68, .58, .84), (0, 0, 1.42)]
        s.mesh('Booth_Roof', base, [(0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4), (3, 2, 1, 0)],
               [roof, 'Cream', roof, 'Cream', roof], g)
        s.seg('Booth_Flagpole', (0, 0, 1.38), (0, 0, BOOTH_MAST if s.street else 1.64), .03, .03, 'Gold', g)
        s.mesh('Booth_Flag', [(0, 0, 1.63), (.22, 0, 1.57), (0, 0, 1.51), (0, .02, 1.63), (.22, .02, 1.57), (0, .02, 1.51)],
               FLAG_FACES, 'Soda' if roof != 'Soda' else 'Strawberry', g)
    bunting = ['Strawberry', 'Vanilla', 'Soda', 'White']
    if s.street:
        # Street: the bunting hangs from the booths' raised masts, so no pole stands in a bubble gap
        masts = [(x, BACK_Y + .62 + .18*(k % 2), BOOTH_MAST - .05) for k, (x, _) in enumerate(booths)]
        for a, b in zip(masts, masts[1:]):
            s.pennants('Bunting', a, b, bunting, 7, sag=.2, drop=.2)
    else:
        # bunting poles stand in the gaps between the booths, on the far bank's front edge
        poles = [(STOREFRONT_X + .12, BACK_Y + .05)] + [((a + b)/2, BACK_Y + .05) for (a, _), (b, _) in zip(booths, booths[1:])]
        for x, y in poles:
            s.prism('BuntingPole', (x, y, .9), .045, 1.8, 'Wood', sides=6, bevel=.008)
            s.ico('BuntingKnob', (x, y, 1.84), (.07, .07, .07), 'Gold')
        for (xa, ya), (xb, yb) in zip(poles, poles[1:]):
            s.pennants('Bunting', (xa, ya - .06, 1.74), (xb, yb - .06, 1.74), bunting, 7, sag=.24, drop=.22)
        s.tree('Tree', (.1, 4.75, 0), .95)
        s.tree('Tree', (4.5, 4.8, 0), .9)
        s.tree('Tree', (8.9, 4.75, 0), .95)
    s.bush('Reed', (5.15, .12, 0), .5)
    s.bush('Reed', (8.25, .15, 0), .45, 'Base')
    for x, y, color in ((5.2, 1.85, 'Vanilla'), (8.1, 1.3, 'Strawberry'), (7.5, 2.2, 'Soda')):
        s.lantern('FloatLantern', (x, y, -.12), color)
    s.cloud('Cloud', -3.35, .74, .42)
    s.cloud('Cloud', 2.6, 1.45, .6)
    s.cloud('Cloud', 7.1, 1.4, .5)


def starlight(s):
    """3 별빛 광장: a dusk plaza (Plum/Navy sky facets, small Vanilla stars) with a ferris wheel."""
    s.sky(DUSK_SKY)
    rng = s.rng
    placed = 0
    while placed < 40:
        x, z, size = rng.uniform(-4.7, 7.5), rng.uniform(.5, 2.0), rng.uniform(.04, .08)
        y = s.sky_y(z) - .2
        if not cut_by_frame(s.location, x, y, z, size + .02):
            s.star('Star', (x, y, z), size)
            placed += 1
    for k in range(13):
        x = STOREFRONT_X + .1 + k
        s.box('Balustrade_Post', (x, 4.55, .22), (.12, .12, .44), 'White', bevel=.02)
        if k < 12:
            s.box('Balustrade_Rail', (x + .5, 4.55, .42), (1.0, .1, .07), 'Cream', bevel=.015)
    for x in (() if s.street else (-1.55, .6, 3.4, 5.4, 7.4)):   # Street: they stood in the band
        s.prism('Topiary_Trunk', (x, 4.85, .25), .05, .5, 'Wood', sides=6, bevel=.006)
        s.ico('Topiary', (x, 4.85, .72), (.3, .3, .34), 'Mint')
    s.field('Square', CURB_Y - .1, SKY_Y + .6, 0, 4, plain('Cream'), x1=STOREFRONT_X, nx=3)
    s.field('Square', CURB_Y - .1, BACK_Y - .1, 0, 4, plain('Cream'), x0=STOREFRONT_X, nx=9)
    s.field('Plaza', BACK_Y - .1, SKY_Y + .6, 0, 3, lambda i, j, rng: 'Cream' if (i + j) % 2 == 0 else 'White',
            x0=STOREFRONT_X, nx=10, jxy=0)
    s.sidewalk()
    # Street: the wheel stands under the middle bubble, set back and scaled so its Plum base stays
    # above the bubble bottoms and its rim under the top edge
    wheel = s.empty('Wheel', (SLOT_X[1], BACK_Y + WHEEL_STREET_BACK, 0) if s.street else (1.9, BACK_Y + .55, 0), 12)
    if s.street:
        wheel.scale = (WHEEL_STREET_SCALE,)*3
    hub = Vector((0, 0, 1.1))
    radius, rim_sides = .7, 16
    for x in (-.5, .5):
        for y in (-.24, .24):
            s.seg('Wheel_Leg', (x, y, .06), (x*.08, y*.35, hub.z), .07, .07, 'White', wheel, bevel=.01)
    s.prism('Wheel_Axle', tuple(hub), .04, .62, 'Gold', wheel, sides=8, axis='y')
    s.prism('Wheel_Hub', tuple(hub), .12, .36, 'Gold', wheel, sides=8, axis='y')
    rim = [hub + Vector((math.cos(i*math.tau/rim_sides)*radius, 0, math.sin(i*math.tau/rim_sides)*radius))
           for i in range(rim_sides)]
    for face_y in (-.14, .14):
        off = Vector((0, face_y, 0))
        for i in range(rim_sides):
            s.seg('Wheel_Rim', rim[i] + off, rim[(i + 1) % rim_sides] + off, .055, .055, 'Cream', wheel, bevel=.006)
        for i in range(0, rim_sides, 2):
            s.seg('Wheel_Spoke', hub + off, rim[i] + off, .025, .025, 'White', wheel, bevel=0)
    cabins = ['Strawberry', 'Soda', 'Vanilla', 'Mint']
    for i in range(0, rim_sides, 2):
        p = rim[i]
        s.seg('Wheel_Hanger', p, p + Vector((0, 0, -.09)), .02, .02, 'Gold', wheel, bevel=0)
        s.box('Wheel_Cabin', (p.x, p.y, p.z - .19), (.21, .19, .17), cabins[(i//2) % 4], wheel, bevel=.03)
        s.box('Wheel_CabinRoof', (p.x, p.y, p.z - .09), (.26, .24, .04), 'Cream', wheel, bevel=.01)
    s.box('Wheel_Base', (0, 0, .025), (1.5, .8, .05), 'Plum', wheel, bevel=.02)
    if s.street:   # the kiosk's Navy window, the lamp feet and the hedges all showed in the band
        return
    kiosk = s.empty('Ticket', (-.5, BACK_Y + .4, 0), PIECE_TURN)
    s.box('Ticket_Body', (0, 0, .42), (.72, .62, .84), 'White', kiosk, bevel=.03)
    s.box('Ticket_Window', (0, -.33, .52), (.44, .03, .26), 'Navy', kiosk, bevel=.01)
    s.box('Ticket_Roof', (0, 0, .89), (.86, .76, .1), 'Strawberry', kiosk, bevel=.02)
    s.star('Ticket_Star', (0, -.1, 1.1), .14, parent=kiosk)
    for x in (-1.62, 3.35, 5.3):
        s.lamp('Lamp', (x, BACK_Y + .12, 0), height=1.35)
    for x, y in ((.5, BACK_Y + .35), (3.95, BACK_Y + .4), (6.4, BACK_Y + .35)):
        s.bush('Hedge', (x, y, 0), .65)


BUILDERS = {0: alley, 1: market, 2: riverside, 3: starlight}


def build(asset, col, kit):
    location = asset['params']['location']
    BUILDERS[location](Stage(col, kit, location, asset['params']['framing']))


# ---------------------------------------------------------------------- Street overlay preview (no Blender)
ROOT = Path(__file__).resolve().parents[3]
SPRITES = 'Assets/CottonCircuit/Sprites'
APPROVED = {0: 'Art/Blender/GameCustomerFaceted/Customer_01_Faceted.png',
            2: 'Art/Blender/GameCustomerFemaleExplorer/Customer_02_Explorer.png'}
ORDERS = (('Strawberry', '딸기'), ('Soda', '소다'), ('Vanilla', '바닐라'))
INK, BUBBLE, PLACEHOLDER, BAND = '#29324D', '#FFF9ED', (150, 150, 150, 200), '#C2577E'   # bubble fill = palette White


def _font(size, bold=False):
    from PIL import ImageFont
    for name in (('malgunbd.ttf', 'segoeuib.ttf') if bold else ('malgun.ttf', 'segoeui.ttf')):
        path = Path('C:/Windows/Fonts')/name
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default(size=size)


def _speech_bubble(size=(132, 130), ss=4):
    """ShopStreetGraphic.DrawSpeechBubble (neutral), drawn supersampled; P() takes bottom-up y."""
    from PIL import Image, ImageDraw
    w, h = size[0]*ss, size[1]*ss
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    def P(x, y):
        return x*w, (1 - y)*h
    draw.polygon([P(.22, .17), P(.35, .17), P(.20, .015), P(.20, .17)], fill=INK)
    draw.rounded_rectangle([P(.035, .975), P(.965, .14)], radius=.13*h, fill=INK)
    draw.rounded_rectangle([P(.055, .955), P(.945, .16)], radius=.115*h, fill=BUBBLE)
    draw.polygon([P(.24, .18), P(.33, .18), P(.215, .055), P(.215, .18)], fill=BUBBLE)
    return img.resize(size, Image.Resampling.LANCZOS)


def _sprite(path, size, contain=False):
    from PIL import Image
    with Image.open(path) as source:
        img = source.convert('RGBA')
    if contain:
        scale = min(size[0]/img.width, size[1]/img.height)
        size = (max(1, round(img.width*scale)), max(1, round(img.height*scale)))
    return img.resize(size, Image.Resampling.LANCZOS)


def _placeholder(draw, rect, label):
    x, y, w, h = rect
    draw.rectangle([x, y, x + w - 1, y + h - 1], fill=PLACEHOLDER, outline=(90, 90, 90, 255))
    draw.text((x + 4, y + 4), label, fill=(40, 40, 40, 255), font=_font(10))


def overlay_street(street, root=ROOT):
    """A 596x314 Street image under the game's overlays (rects of GameUI.BuildShopStreet).

    Missing sprites become grey placeholders of the exact rects. Returns (image, sources used)."""
    from PIL import ImageDraw
    img = street.copy()
    draw = ImageDraw.Draw(img)
    used = []
    front = root/SPRITES/'Shop/Storefront.png'
    x, y, w, h = STREET_OVERLAYS['storefront']
    if front.is_file():
        img.alpha_composite(_sprite(front, (w, h)), (x, y))
        used.append('Shop/Storefront.png')
    else:
        _placeholder(draw, (x, y, w, h), 'Storefront (missing)')
        used.append('storefront placeholder')
    draw.text((34 + 113/2, 69 + 15), '솜사탕', fill=INK, font=_font(20, True), anchor='mm')
    draw.text((24 + 66, 211 + 9), 'COTTON SHOP', fill=INK, font=_font(11, True), anchor='mm')
    bubble = _speech_bubble()
    cx, cy, cw, ch = STREET_OVERLAYS['customer_art_in_target']
    for slot, (tx, ty, _, _) in enumerate(STREET_OVERLAYS['customer_targets']):
        person = root/SPRITES/('Customers/Customer_V%d_Neutral.png' % slot)
        approved = root/APPROVED[slot] if slot in APPROVED else None
        source = person if person.is_file() else approved if approved and approved.is_file() else None
        if source:
            img.alpha_composite(_sprite(source, (cw, ch)), (tx + cx, ty + cy))
            used.append(source.relative_to(root/SPRITES if person.is_file() else root).as_posix())
        else:
            _placeholder(draw, (tx + cx, ty + cy, cw, ch), 'V%d (missing)' % slot)
            used.append('customer V%d placeholder' % slot)
        img.alpha_composite(bubble, (tx, ty))
        flavor, name = ORDERS[slot]
        candy = root/SPRITES/('Items/CottonCandy_%s_Small.png' % flavor)
        if candy.is_file():
            art = _sprite(candy, (80, 77), contain=True)
            img.alpha_composite(art, (tx + 23 + (80 - art.width)//2, ty + 4 + (77 - art.height)//2))
        else:
            _placeholder(draw, (tx + 23, ty + 4, 80, 77), 'candy')
        draw.rectangle([tx + 94, ty + 17, tx + 118, ty + 43], fill=INK)
        draw.text((tx + 106, ty + 30), '소', fill='white', font=_font(18, True), anchor='mm')
        draw.text((tx + 66, ty + 89), name, fill=INK, font=_font(14, True), anchor='mm')
        draw.rectangle([tx + 18, ty + 104, tx + 18 + 96, ty + 107], fill='#7ACDCE')   # full patience (ShopStreetUI: 96x4)
    sx, sy, sw, sh = STREET_OVERLAYS['status_text']
    draw.text((sx + sw, sy + sh/2), '손님 3명  ·  다음 손님 4초 후', fill=INK, font=_font(12), anchor='rm')
    return img, used


def _dashed_box(draw, box, fill, dash=4):
    x0, y0, x1, y1 = box
    for x in range(round(x0), round(x1), 2*dash):
        for y in (y0, y1):
            draw.line([(x, y), (min(x + dash, x1), y)], fill=fill)
    for y in range(round(y0), round(y1), 2*dash):
        for x in (x0, x1):
            draw.line([(x, y), (x, min(y + dash, y1))], fill=fill)


def street_composite(root=ROOT, out=None):
    """Each Street sprite at game size (596x314), bare on the left (STREET_BAND dashed) and overlaid on the right."""
    from PIL import Image, ImageDraw
    out = Path(out) if out else root/'Art/Blender/previews/locations-street-composite.png'
    game, gap, top, label = STREET_SIZE, 16, 64, 26
    board = Image.new('RGBA', (2*game[0] + 3*gap, top + 4*(game[1] + label + gap)), '#F5F0E7')
    draw = ImageDraw.Draw(board)
    sources = set()
    for index in range(4):
        path = root/SPRITES/('Locations/Location_%d_Street.png' % index)
        y = top + index*(game[1] + label + gap)
        draw.text((gap, y), 'Location_%d_Street  %s' % (index, NAMES[index]), fill=INK, font=_font(14, True))
        if not path.is_file():
            draw.text((gap, y + label), 'MISSING ' + path.name, fill=INK, font=_font(14, True))
            continue
        with Image.open(path) as source:
            street = source.convert('RGBA').resize(game, Image.Resampling.LANCZOS)
        composed, used = overlay_street(street, root)
        sources.update(used)
        board.alpha_composite(street, (gap, y + label))
        board.alpha_composite(composed, (2*gap + game[0], y + label))
        bx0, bx1, by0, by1 = STREET_BAND
        _dashed_box(draw, (gap + bx0*game[0], y + label + by0*game[1], gap + bx1*game[0], y + label + by1*game[1]), BAND)
    draw.text((gap, 12), 'LOCATIONS  |  Street sprites at 596x314, bare with the customer and bubble band dashed (left) '
              'and under the game overlays (right)', fill='#58796F', font=_font(17, True))
    draw.text((gap, 38), 'Overlays: %s. Composited in sRGB; Unity blends in linear space.' % ', '.join(sorted(sources)),
              fill='#737482', font=_font(12))
    out.parent.mkdir(parents=True, exist_ok=True)
    board.convert('RGB').save(out, optimize=True)
    print('LOCATIONS_STREET_COMPOSITE %s (%d x %d)' % (out, board.width, board.height))
    return out


def main(argv=None):
    parser = argparse.ArgumentParser(description='Street overlay preview for the location sprites (plain Python).')
    parser.add_argument('--root', type=Path, default=ROOT, help='project root holding Assets/ and Art/')
    parser.add_argument('--out', type=Path, help='PNG path (default Art/Blender/previews/locations-street-composite.png)')
    args = parser.parse_args(argv)
    street_composite(args.root, args.out)
    return 0


if __name__ == '__main__':
    sys.exit(main())

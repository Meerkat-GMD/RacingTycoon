"""bpy-free source of truth for the pastel-toy UI sprites.

Imported by ui_sprite_common (inside Blender) and by the validator, preview and
tests (plain Python). See docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md.
"""
PALETTE = {
    'Strawberry': '#F48DAB', 'Soda': '#7ACDCE', 'Vanilla': '#F9D27D',
    'Navy': '#29324D', 'Cream': '#FFF1D4', 'White': '#FFF9ED',
    'Mint': '#99C4AE', 'Plum': '#6C577F', 'Gold': '#DBAE61',
    'Wood': '#D59C79', 'Tire': '#414059', 'Base': '#9DBBAF',
    'Skin1': '#F1C6A6', 'Skin2': '#B87A65', 'Skin3': '#E8AF88',
    'Hair1': '#574F59', 'Hair2': '#8D5B4A', 'Hair3': '#45475B',
    'Pants1': '#7C789D', 'Pants2': '#65688B', 'Pants3': '#5F968F',
    'Magenta': '#C2577E',
}
LIGHTING = {
    'key': {'location': [-2, -3, 7], 'power_w': 900, 'size': 4, 'color': '#FFF1DD'},
    'fill': {'location': [4.5, -3.5, 3], 'power_w': 250, 'size': 5, 'color': '#DDE6FF'},
    'rim': {'location': [1.5, 3, 5], 'power_w': 70, 'size': 3, 'color': '#FFFFFF'},
    'environment': {'color': '#F3ECF7', 'strength': .75},
    'aim': [0, 0, 1.25],
}
EXPOSURE = -1.8
SEED = 260927
MATERIAL = {'roughness': .85, 'specular_ior_level': .08}
RENDER = {'engine': 'CYCLES', 'samples': 64, 'denoise': True, 'view_transform': 'Standard',
          'look': 'None', 'exposure': EXPOSURE, 'gamma': 1}
REVIEW_BACKGROUNDS = {'cream': '#FFF6E7', 'ink': '#29324D'}
# Trait disc colours from OutgameUI.CategoryColor, used by the preview board and icon review.
DISC_COLORS = {'business': '#EBC997', 'production': '#EBA5B3', 'equipment': '#B8D9CC',
               'staff': '#C9BDE0', 'location': '#ACD2D9', 'sales': '#E2C2AD'}

KIND_RULES = {
    'character': {'shadow': True, 'opaque': False, 'margin': 2, 'occupancy': (.08, .85)},
    'prop':      {'shadow': True, 'opaque': False, 'margin': 2, 'occupancy': (.06, .90)},
    'emote':     {'shadow': False, 'opaque': False, 'margin': 2, 'occupancy': (.15, .90), 'max_yaw': 10},
    'icon':      {'shadow': False, 'opaque': False, 'margin': 2, 'occupancy': (.15, .90), 'max_yaw': 10},
    'scene':     {'shadow': False, 'opaque': True},
    'portrait':  {'shadow': False, 'opaque': True},
}
FLAVORS = ('Strawberry', 'Soda', 'Vanilla')
SIZES = ('Small', 'Medium', 'Large')
TRAIT_ICONS = [
    'Trait_Hours', 'Trait_Patience', 'Trait_Ads', 'Trait_RepeatAds', 'Trait_Shelf', 'Trait_Sales',
    'Trait_PriceTag', 'Trait_Engine', 'Trait_Handling', 'Trait_Kart', 'Trait_StickSpeed', 'Trait_Spoon',
    'Trait_Ribbon', 'Trait_Sugar2', 'Trait_Sugar3', 'Trait_Machine', 'Trait_FlavorVanilla',
    'Trait_Worker', 'Trait_GradCap', 'Trait_Glove', 'Trait_MapPin', 'Trait_Group',
]


def _e(id, folder, kind, canvas, displays, owner, pivot=None):
    return {'id': id, 'folder': folder, 'kind': kind, 'canvas': canvas, 'displays': displays,
            'owner': owner, 'shadow': KIND_RULES[kind]['shadow'], 'pivot': pivot}


CATALOG = (
    [_e('Customer_V%d_%s' % (v, x), 'Customers', 'character', (164, 280), [(82, 140)], 'customers')
     for v in range(3) for x in ('Neutral', 'Angry')]
    + [_e('Mina_Portrait', 'Characters', 'portrait', (808, 784), [(404, 391)], 'mina')]
    + [_e('CottonCandy_%s_%s' % (f, z), 'Items', 'prop', (156, 176), [(77, 88), (80, 77)], 'items', (0.5, 0.06))
       for f in FLAVORS for z in SIZES]
    + [_e('BaggedCandy_%s_%s' % (f, z), 'Items', 'prop', (148, 184),
          [(74, 92), (84, 98), (62, 86), (118, 124)], 'items', (0.5, 0.14)) for f in FLAVORS for z in SIZES]
    + [_e('SugarBag_%s' % f, 'Items', 'prop', (164, 200), [(82, 100), (118, 124)], 'items') for f in FLAVORS]
    + [_e('Storefront', 'Shop', 'prop', (340, 460), [(169, 230)], 'shop'),
       _e('Trash', 'Shop', 'prop', (136, 164), [(68, 82)], 'shop'),
       _e('Emote_Heart', 'Shop', 'emote', (144, 132), [(72, 66)], 'shop'),
       _e('Emote_Angry', 'Shop', 'emote', (144, 132), [(72, 66)], 'shop')]
    + [_e('Location_%d_Prep' % i, 'Locations', 'scene', (1176, 708), [(588, 354)], 'locations') for i in range(4)]
    + [_e('Location_%d_Street' % i, 'Locations', 'scene', (1192, 628), [(596, 314)], 'locations') for i in range(4)]
    + [_e('Machine_%d' % i, 'Machines', 'prop', (252, 252), [(126, 126)], 'machines') for i in range(3)]
    + [_e(i, 'Icons', 'icon', (84, 84), [(42, 42)], 'icons') for i in TRAIT_ICONS]
)


def by_id():
    return {e['id']: e for e in CATALOG}


def owned_by(owner):
    return [e for e in CATALOG if e['owner'] == owner]


def sprite_path(entry):
    return 'Assets/CottonCircuit/Sprites/%s/%s.png' % (entry['folder'], entry['id'])

"""Driver fixture: one catalog icon built as a plain Gold box (tests the category contract only).

blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py --
    --category fixture --out-root %TEMP%/ui_sprite_fixture --module-path Art/Blender/tests/fixtures
"""
CATEGORY = 'fixture'
OWNER = 'icons'  # the catalog owner of Trait_Hours; real category modules leave OWNER = CATEGORY
ASSETS = [{'id': 'Trait_Hours',
           'camera': {'target': (0, 0, 1.25), 'scale': 1.9, 'yaw': -6, 'elevation': 15},
           'shadow': None,
           'params': {'shape': 'gold box', 'purpose': 'driver fixture'}}]


def build(asset, col, kit):
    kit.box(col, 'Gold_Box', (0, 0, 1.25), (.9, .5, .9), 'Gold', .06)

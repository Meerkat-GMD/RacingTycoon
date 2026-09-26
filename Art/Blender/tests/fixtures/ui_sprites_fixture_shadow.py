"""Driver fixture for the contact-shadow path: one catalog prop built as a Base box on the catcher.

blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py --
    --category fixture_shadow --out-root %TEMP%/ui_sprite_fixture --module-path Art/Blender/tests/fixtures
"""
CATEGORY = 'fixture_shadow'
OWNER = 'shop'  # the catalog owner of Trash
ASSETS = [{'id': 'Trash',
           'camera': {'target': (0, 0, .75), 'scale': 2.2, 'yaw': -20, 'elevation': 15},
           'shadow': {'anchor': (.07, .10, 0), 'radii': (.30, .068), 'max_alpha': .32},
           'params': {'shape': 'base box', 'purpose': 'driver fixture'}}]


def build(asset, col, kit):
    kit.box(col, 'Base_Box', (0, 0, .6), (.8, .6, 1.2), 'Base', .06)
    kit.ico(col, 'Strawberry_Lid_Knob', (0, 0, 1.28), (.12, .12, .1), 'Strawberry')

"""Per-category UI sprite modules, built and rendered by ../build_ui_sprites.py.

Each module `ui_sprites/<category>.py` defines:

    CATEGORY = 'items'                  # equals the catalog owner in ui_sprite_spec
    ASSETS = [{'id': 'CottonCandy_Strawberry_Small',
               'camera': {'target': (0, 0, 1.2), 'scale': 2.9, 'yaw': -20, 'elevation': 15},
               'shadow': {'anchor': (.07, .10, 0), 'radii': (.30, .068), 'max_alpha': .32},  # or None
               'params': {...}}]        # free-form, recorded in the manifest
    def build(asset, col, kit):         # add all geometry for one asset into col
        ...

`kit` is a toy_kit.Kit whose prefix is '<sprite id>_'. Opaque kinds (scene,
portrait) set asset['opaque_backdrop'] = True once their geometry fills the frame.
The driver writes ui_sprites/<category>.blend and ui_sprites/<category>.manifest.json.
"""

"""Compatibility wrapper for the approved customer generators.

GameCustomerFaceted/create_customer.py and GameCustomerFemaleExplorer/create_explorer.py
set ART and call render_contact_sprite(scene, entry, output). New assets use
build_ui_sprites.py and ui_sprite_render.render_sprite directly.
"""
import importlib
import sys
from pathlib import Path

ART = Path(__file__).resolve().parent
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_render as r
importlib.reload(r)


def render_contact_sprite(scene, entry, output):
    r.render_sprite(scene, entry['id'], Path(output), ART / 'ui-sprite-passes', r.DEFAULT_SHADOW,
                    tuple(entry['render_size']))

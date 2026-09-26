"""Runs build_ui_sprites.py in background Blender on the two fixture categories.

Skipped when Blender is not installed (set BLENDER to its executable to override the default path).
Everything is written under a temporary out-root; Art/Blender/ui_sprites/ must stay untouched.
"""
import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image

ART = Path(__file__).resolve().parents[1]
BLENDER = Path(os.environ.get('BLENDER', 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe'))
sys.path.insert(0, str(ART))
import validate_ui_sprites as v


@unittest.skipUnless(BLENDER.is_file(), 'Blender not found at %s' % BLENDER)
class BuildDriverTests(unittest.TestCase):
    def run_driver(self, category, out_root):
        env = dict(os.environ, UI_SPRITE_THREADS='4')
        return subprocess.run(
            [str(BLENDER), '-b', '--factory-startup', '--python-exit-code', '1', '-P', str(ART/'build_ui_sprites.py'),
             '--', '--category', category, '--out-root', str(out_root), '--module-path', str(ART/'tests/fixtures')],
            capture_output=True, text=True, encoding='utf-8', errors='replace', env=env, timeout=600)

    def test_fixture_categories(self):
        listing = lambda: sorted(p.name for p in (ART/'ui_sprites').iterdir() if p.name != '__pycache__')
        before = listing()
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp).resolve()
            for category, sprite, size in (('fixture', 'Icons/Trait_Hours.png', (84, 84)),
                                           ('fixture_shadow', 'Shop/Trash.png', (136, 164))):
                result = self.run_driver(category, out)
                self.assertEqual(result.returncode, 0, result.stdout[-3000:] + result.stderr[-3000:])
                png = out/'Assets/CottonCircuit/Sprites'/sprite
                self.assertIn('UI_SPRITE_RENDERED %s %s' % (Path(sprite).stem, png), result.stdout)
                with Image.open(png) as image:
                    self.assertEqual((image.mode, image.size), ('RGBA', size))
                self.assertTrue((out/'Art/Blender/ui_sprites'/(category + '.blend')).is_file())
                manifest = json.loads((out/'Art/Blender/ui_sprites'/(category + '.manifest.json')).read_text('utf-8'))
                self.assertEqual([a['id'] for a in manifest['assets']], [Path(sprite).stem])
            passes = out/'Art/Blender/ui-sprite-passes/fixture_shadow'
            self.assertEqual(sorted(p.name for p in passes.iterdir()), ['Trash-beauty.png', 'Trash-catcher.png'])
            report = v.validate(out)
            self.assertEqual((report['errors'], report['present']), ([], 2))
        self.assertEqual(listing(), before)


if __name__ == '__main__':
    unittest.main()

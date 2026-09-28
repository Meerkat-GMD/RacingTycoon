"""Plain-Python tests for validate_ui_sprites.py on synthetic sprite trees."""
import contextlib
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import ui_sprite_spec as spec
import validate_ui_sprites as v


def blank(size, alpha=0):
    width, height = size
    image = np.zeros((height, width, 4), dtype=np.uint8)
    image[..., :3] = (120, 140, 160)
    image[..., 3] = alpha
    return image


def subject(size, box):
    image = blank(size)
    left, top, right, bottom = box
    image[top:bottom, left:right, 3] = 255
    return image


class ValidatorTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.catalog = spec.by_id()

    def tearDown(self):
        self.tmp.cleanup()

    def write(self, sprite_id, pixels, mode='RGBA'):
        path = self.root/spec.sprite_path(self.catalog[sprite_id])
        path.parent.mkdir(parents=True, exist_ok=True)
        Image.fromarray(pixels, 'RGBA').convert(mode).save(path)
        return path

    def manifest(self, **changes):
        data = {'category': 'icons', 'render': dict(spec.RENDER, seed=spec.SEED, adaptive_sampling=False),
                'palette': dict(spec.PALETTE),
                'assets': [{'id': 'Trait_Hours', 'file': spec.sprite_path(self.catalog['Trait_Hours']),
                            'canvas': [84, 84], 'camera': {'yaw_deg': -6},
                            'materials': [{'name': 'Trait_Hours_Gold', 'palette_name': 'Gold',
                                           'palette_srgb': '#DBAE61', 'roughness': .85,
                                           'specular_ior_level': .08}]}]}
        data.update(changes)
        path = self.root/v.MANIFESTS/'icons.manifest.json'
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(data), encoding='utf-8')
        return data

    def errors(self, complete=False, owners=None):
        return v.validate(self.root, complete, owners)['errors']

    def test_empty_tree_passes_unless_complete(self):
        report = v.validate(self.root)
        self.assertTrue(report['passed'])
        self.assertEqual((report['present'], report['total']), (0, 65))
        self.assertEqual(len(self.errors(complete=True)), 65)

    def test_good_icon_passes(self):
        self.write('Trait_Hours', subject((84, 84), (22, 22, 62, 62)))
        report = v.validate(self.root)
        self.assertEqual(report['errors'], [])
        self.assertEqual(report['present'], 1)

    def test_icon_needs_transparent_margin(self):
        self.write('Trait_Hours', subject((84, 84), (1, 22, 62, 62)))
        self.assertTrue(any('transparent px' in e for e in self.errors()))

    def test_occupancy_range(self):
        self.write('Trait_Hours', subject((84, 84), (40, 40, 44, 44)))
        self.assertTrue(any('occupancy' in e for e in self.errors()))

    def test_size_and_mode(self):
        self.write('Trait_Hours', subject((80, 84), (22, 22, 62, 62)))
        self.assertTrue(any('size must be 84x84' in e for e in self.errors()))
        self.write('Trait_Hours', subject((84, 84), (22, 22, 62, 62)), mode='RGB')
        self.assertTrue(any('mode must be RGBA' in e for e in self.errors()))

    def test_opaque_kind_needs_full_alpha(self):
        pixels = blank((1176, 708), 255)
        self.write('Location_0_Prep', pixels)
        self.assertEqual(self.errors(), [])
        pixels[5, 5, 3] = 254
        self.write('Location_0_Prep', pixels)
        self.assertTrue(any('alpha 255 everywhere' in e for e in self.errors()))

    def test_shadow_must_not_change_beauty_pixels(self):
        beauty = subject((136, 164), (40, 30, 96, 140))
        final = beauty.copy()
        final[142:150, 30:106, 3] = 60  # soft contact shadow below the subject
        self.write('Trash', final)
        passes = self.root/v.PASSES/'shop'
        passes.mkdir(parents=True)
        Image.fromarray(beauty, 'RGBA').save(passes/'Trash-beauty.png')
        self.assertEqual(self.errors(), [])
        final[50, 50, 0] = 0
        self.write('Trash', final)
        self.assertTrue(any('changed 1 opaque beauty pixels' in e for e in self.errors()))

    def test_stray_png_fails(self):
        path = self.root/v.SPRITES/'Items/CottonCandy_Old.png'
        path.parent.mkdir(parents=True)
        Image.fromarray(blank((8, 8)), 'RGBA').save(path)
        self.assertTrue(any('not in the sprite catalog' in e for e in self.errors()))

    def test_manifest_checks(self):
        self.manifest()
        self.assertEqual(self.errors(), [])
        self.manifest(render=dict(spec.RENDER, exposure=-1.5))
        self.assertTrue(any('render.exposure' in e for e in self.errors()))
        self.manifest(render=dict(spec.RENDER, gamma=1.2))
        self.assertTrue(any('render.gamma' in e for e in self.errors()))
        self.manifest(palette=dict(spec.PALETTE, Extra='#123456'))
        self.assertTrue(any('not a palette colour' in e for e in self.errors()))
        data = self.manifest()
        data['assets'][0]['materials'][0]['palette_srgb'] = '#123456'
        data['assets'][0]['camera']['yaw_deg'] = -20
        data['assets'][0]['materials'].append({'name': 'Old', 'palette_name': 'Soda', 'palette_srgb': '#7ACDCE',
                                               'roughness': .83, 'specular_ior_level': .18})
        self.manifest(assets=data['assets'])
        errors = self.errors()
        self.assertTrue(any('has no palette colour' in e for e in errors))
        self.assertTrue(any('|yaw|' in e for e in errors))
        self.assertTrue(any('roughness 0.85, got 0.83' in e for e in errors))

    def test_owner_filter_ignores_other_owners_broken_png(self):
        self.write('Trait_Hours', subject((84, 84), (22, 22, 62, 62)))          # icons: good
        self.write('Trash', subject((80, 84), (10, 10, 20, 20)))                # shop: wrong size, broken
        self.assertTrue(any('size must be' in e for e in self.errors()))
        report = v.validate(self.root, owners=['icons'])
        self.assertEqual(report['errors'], [])
        self.assertEqual((report['present'], report['total']), (1, len(spec.owned_by('icons'))))
        self.assertEqual(report['owners'], ['icons'])

    def test_complete_owner_filter_fails_only_on_missing_icons(self):
        self.write('Trait_Hours', subject((84, 84), (22, 22, 62, 62)))
        report = v.validate(self.root, complete=True, owners=['icons'])
        icon_ids = {e['id'] for e in spec.owned_by('icons')}
        self.assertEqual(report['total'], len(icon_ids))
        self.assertEqual(report['present'], 1)
        self.assertFalse(report['passed'])
        for message in report['errors']:
            self.assertIn('PNG is missing', message)
            sprite_id = message.split(':')[0]
            self.assertIn(sprite_id, icon_ids)
        self.assertEqual(len(report['errors']), len(icon_ids)-1)

    def test_unknown_owner_exits_2(self):
        with contextlib.redirect_stderr(io.StringIO()) as err:
            with self.assertRaises(SystemExit) as ctx:
                v.main(['--root', str(self.root), '--owner', 'nope'])
        self.assertEqual(ctx.exception.code, 2)
        self.assertIn('unknown --owner', err.getvalue())

    def test_manifest_without_seed_fails(self):
        self.manifest(render=dict(spec.RENDER, adaptive_sampling=False))  # no 'seed' key at all
        self.assertTrue(any('render must use seed' in e for e in self.errors()))

    def test_owner_filter_scopes_manifest_by_category(self):
        icons_data = self.manifest(render=dict(spec.RENDER, exposure=-1.5))  # icons.manifest.json, broken
        shop_data = dict(icons_data, category='shop',
                         render=dict(spec.RENDER, seed=spec.SEED, adaptive_sampling=False), assets=[])
        (self.root/v.MANIFESTS/'shop.manifest.json').write_text(json.dumps(shop_data), encoding='utf-8')
        self.assertTrue(any('render.exposure' in e for e in self.errors()))
        self.assertEqual(self.errors(owners=['shop']), [])

    def test_cli_report_path_and_exit_code(self):
        report = self.root/'reports/validation.json'
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            self.assertEqual(v.main(['--root', str(self.root), '--report', str(report)]), 0)
            self.assertTrue(json.loads(report.read_text(encoding='utf-8'))['passed'])
            self.assertEqual(v.main(['--root', str(self.root), '--report', str(report), '--complete']), 1)
        self.assertIn('UI_SPRITES_VALID 0/65', out.getvalue())
        self.assertEqual(out.getvalue().count('PNG is missing'), 65)


if __name__ == '__main__':
    unittest.main()

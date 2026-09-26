import sys, unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import ui_sprite_spec as s

class CatalogTests(unittest.TestCase):
    def test_count_and_unique_ids(self):
        ids = [e['id'] for e in s.CATALOG]
        self.assertEqual(len(ids), 66)
        self.assertEqual(len(set(ids)), 66)

    def test_owner_counts(self):
        counts = {o: len(s.owned_by(o)) for o in ('customers', 'mina', 'items', 'shop', 'locations', 'machines', 'icons')}
        self.assertEqual(counts, {'customers': 6, 'mina': 1, 'items': 21, 'shop': 4, 'locations': 8, 'machines': 3, 'icons': 23})

    def test_canvas_rule(self):
        for e in s.CATALOG:
            w, h = e['canvas']; dw, dh = e['displays'][0]
            self.assertEqual((w % 4, h % 4), (0, 0), e['id'])
            self.assertGreaterEqual(w, 2 * dw, e['id']); self.assertGreaterEqual(h, 2 * dh, e['id'])
            self.assertLess(w, 2 * dw + 4, e['id']); self.assertLess(h, 2 * dh + 4, e['id'])

    def test_folders_and_paths(self):
        folders = {'Customers', 'Shop', 'Items', 'Icons', 'Locations', 'Machines', 'Characters'}
        for e in s.CATALOG:
            self.assertIn(e['folder'], folders)
            self.assertEqual(s.sprite_path(e), 'Assets/CottonCircuit/Sprites/%s/%s.png' % (e['folder'], e['id']))

    def test_kinds_have_rules_and_shadow_policy(self):
        for e in s.CATALOG:
            self.assertIn(e['kind'], s.KIND_RULES)
            self.assertEqual(e['shadow'], s.KIND_RULES[e['kind']]['shadow'], e['id'])

    def test_trait_icons(self):
        self.assertEqual(len(s.TRAIT_ICONS), 23)
        self.assertEqual(sorted(s.TRAIT_ICONS), sorted(e['id'] for e in s.owned_by('icons')))

    def test_palette(self):
        self.assertEqual(s.PALETTE['Magenta'], '#C2577E')
        self.assertEqual(len(s.PALETTE), 22)
        self.assertEqual(s.EXPOSURE, -1.8)
        self.assertEqual(s.MATERIAL, {'roughness': .85, 'specular_ior_level': .08})

    def test_pivots(self):
        for e in s.CATALOG:
            if e['id'].startswith('CottonCandy_'): self.assertEqual(e['pivot'], (0.5, 0.06))
            elif e['id'].startswith('BaggedCandy_'): self.assertEqual(e['pivot'], (0.5, 0.14))
            else: self.assertIsNone(e['pivot'])

if __name__ == '__main__':
    unittest.main()

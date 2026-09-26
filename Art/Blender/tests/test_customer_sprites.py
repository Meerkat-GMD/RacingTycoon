import sys, unittest
from pathlib import Path
import numpy as np
from PIL import Image
ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'Assets/CottonCircuit/Sprites/Customers'
APPROVED = {0: ROOT / 'Art/Blender/GameCustomerFaceted/Customer_01_Faceted.png',
            2: ROOT / 'Art/Blender/GameCustomerFemaleExplorer/Customer_02_Explorer.png'}

def rgba(path):
    return np.asarray(Image.open(path).convert('RGBA'), dtype=np.int16)

class CustomerSpriteTests(unittest.TestCase):
    def test_all_six_exist_with_canvas(self):
        for v in range(3):
            for x in ('Neutral', 'Angry'):
                img = Image.open(OUT / ('Customer_V%d_%s.png' % (v, x)))
                self.assertEqual((img.mode, img.size), ('RGBA', (164, 280)))

    def test_approved_neutrals_are_pixel_identical(self):
        for v, path in APPROVED.items():
            self.assertTrue(np.array_equal(rgba(OUT / ('Customer_V%d_Neutral.png' % v)), rgba(path)), v)

    def test_angry_differs_only_in_head(self):
        for v in range(3):
            n, a = rgba(OUT / ('Customer_V%d_Neutral.png' % v)), rgba(OUT / ('Customer_V%d_Angry.png' % v))
            ys, xs = np.nonzero(n[..., 3] > 8)
            head_bottom = int(ys.min() + 0.38 * (ys.max() - ys.min()))   # head band of the character
            head = np.abs(n[:head_bottom] - a[:head_bottom]).max(axis=2) > 24
            self.assertGreaterEqual(int(head.sum()), 6, 'V%d brows/frown too small to read' % v)
            body = np.abs(n[head_bottom:] - a[head_bottom:])
            self.assertLessEqual(int(body.max()), 8, 'V%d angry changed the body beyond denoiser noise' % v)
            self.assertLess(float(body.mean()), 0.5, 'V%d angry changed the body beyond denoiser noise' % v)

    def test_child_is_shorter(self):
        male, child = rgba(OUT / 'Customer_V0_Neutral.png'), rgba(OUT / 'Customer_V1_Neutral.png')
        h = lambda im: np.ptp(np.nonzero(im[..., 3] > 128)[0])
        self.assertLess(h(child), 0.85 * h(male))

if __name__ == '__main__':
    unittest.main()

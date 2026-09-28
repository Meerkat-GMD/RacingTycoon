# Cotton candy fan stand

Generated for this project with the built-in image generation tool on 2026-09-28.
The user's vendor photograph guided the rack concept; no photograph or watermark is embedded in the game asset. The metal stand is a transparent raster image, not C# geometry. Bagged cotton candy sprites are separate authored UI elements.

Final asset: `CottonCandyFanStand.png`.

Prompt: Create one empty Korean street-vendor cotton candy display stand, with a polished slender silver central pole, twelve flexible stainless-steel wire arms fanning outward like a bouquet, tiny metal clips in two arching rows, and a small round weighted foot. Front orthographic view with a slightly elevated view of the foot; soft toy-like modeled metal with warm reflections for the game's pastel low-poly artwork. Horizontal 2:1 composition, hub at bottom center, six taller arms and six shorter arms. Genuine alpha transparency. No products, bags, people, text, watermark, background or frame. Keep the stand fully visible and leave room above the clips for separate cotton candy sprites.

## 6-clip and 9-clip stands

`CottonCandyFanStand6.png` and `CottonCandyFanStand9.png` are derived from the final asset by `Tools/fan-stand-variants.py` (2026-09-29). The business rack shows the stand whose clip count equals the owned shelf slots: 6 at the start, 9 and 12 after each shelf expansion. The script removes whole arms, clip and wire, so the remaining clips stay where the slot positions in `Business.uss` expect them. Where a removed clip hid a kept wire, that stretch is repainted with the wire's own cross-section. No new artwork or outside source is involved; rerun the script if the 12-clip stand changes.

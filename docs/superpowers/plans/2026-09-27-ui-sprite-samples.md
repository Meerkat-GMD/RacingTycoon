# UI sprite samples implementation plan

**Goal:** Deliver only medium strawberry cotton candy, neutral customer variant 0, and the hours wall clock as editable Blender models and transparent UI renders.

**Architecture:** Task-scoped Blender MCP controls Blender. A deterministic generator creates three asset scenes with semantic collections, shared palette and shared lighting. PNGs are delivered without changing Unity code, scenes, or UI bindings.

**Tech stack:** Blender 5.2, Blender MCP, Python, Cycles, Pillow for contact-sheet assembly and PNG validation.

## Constraints

- Source of truth: `docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md`; reference A supplies shapes only.
- Flat shading; subdivisions 1–2 icospheres and lightly beveled boxes; sRGB HEX values converted to Blender scene-linear values.
- Common key (-2,-3,7), 900 W, size 4, #FFF1DD; fill (4.5,-3.5,3), 250 W, size 5, #DDE6FF; world #F3ECF7, strength .75. Record weak rim choice.
- Orthographic yaw -20 degrees / elevation 15 degrees, icon yaw within 10 degrees. Cycles 64, denoise, Standard / None, RGBA transparent PNG.
- Native PNG sizes: candy 154×176 for77×88, customer164×280 for82×140, clock84×84 for42×42. Include transparent margins inside these exact canvases.
- Candy seed fixed, one core and20 lobes, cream stick and strawberry stripes. Customer variant0 soda shirt, cream badge, Hair1/Skin1/Pants1, dot eyes, blush, small smile, 2.5 heads. Clock retains warm face, dark hands at12 and about4:30; no disc or shadow.
- Preserve unrelated working tree changes and all existing art.

## Tasks

- [x] Read specification, inspect both images directly, inspect generation pipeline and UI source sizes.
- [x] Connect actual Blender MCP, inspect initial scene and record provenance.
- [x] Create `Art/Blender/create_ui_sprites.py`, common helpers, asset collections, three editable scenes, and `UiSprites.blend`.
- [x] Render three PNGs under `Assets/CottonCircuit/Sprites/{Items,Customers,Icons}` and `ui-sprites-manifest.json`.
- [x] Inspect silhouettes first, then final PNGs at native size. Fix clipping/exposure/readability.
- [x] Create `validate_ui_sprites.py` plus independent source audit, checking settings, palette, flat shading, asset count, PNG dimensions and alpha margins.
- [x] Create `ui-sprites-preview.png` with native scale on light #FFF6E7 and dark #29324D; document alternate order-slot containment.
- [x] Review reproducibility, run validation, record exact settings/commands and append README asset links.

## Verification

Run PNG validation in ordinary Python, and scene audit in Blender/MCP against saved source. Save a structured validation report. Inspect actual final comparison image. Check `git diff --stat` and status to ensure no game implementation was edited.

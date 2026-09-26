# Codex Character Trio Implementation Plan

**Goal:** Deliver three cute female characters as editable Blender meshes and transparent PNGs for comparison, following the approved pastel-toy art specification.

**Architecture:** A deterministic Blender generator creates three named character collections and a shared render rig. A separate Pillow packaging script makes reduced sprites and a comparison sheet. All deliverables live under `Art/Blender/CodexCharacters`; the game is not integrated during this comparison task.

**Tech Stack:** Blender 5.2.2 / Cycles, Python, Pillow.

## Constraints and design

- Follow `docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md`, approved direction A.
- User confirmed Blender source plus three transparent PNGs on 2026-09-27.
- Flat shaded, no outlines, approximately 2.5 heads tall, dot eyes, blush, small mouth.
- Existing palette only; three skin tones. Shared orthographic camera (20° left, 15° elevation), 64 samples, Standard/None, warm key/cool fill/weak rim.
- Mina: plum wavy bob, cream shirt, mint apron, strawberry cotton candy.
- Sora: brown twin tails, soda sporty jacket, plum shorts, cream trainers, greeting pose.
- Bomi: dark side braid, vanilla pinafore, cream sleeves, hands in front.
- Master renders 768×1280; reduced sprites 192×320 including transparent and shadow margins. Compare at 96×160 including margin, close to 82×140 visible character.

## Steps

- [x] Inspect the approved specification, two references, existing Blender pipeline and local Blender installation.
- [x] Confirm source/delivery format; obtain independent art direction critique.
- [x] Generate editable named meshes, exact palette materials, shared lights/camera and alpha shadow catcher in `create_characters.py`.
- [x] Save `Characters.blend`, `manifest.json`, and raw Cycles character/shadow passes. Run `finalize_renders.py` to produce `renders/{Mina,Sora,Bomi}.png` with bounded contact shadows.
- [x] Run `package_previews.py` to create `sprites/` and the large comparison/readability sheet.
- [x] Run `validate_characters.py` and `inspect_blend.py`; inspect final comparison, correct neck gaps, bright material clipping, hair accessory occlusion, shadow bounds and saved render path; record results in README.
- [x] Prepare comparison image and links to PNGs and Blender source for delivery.

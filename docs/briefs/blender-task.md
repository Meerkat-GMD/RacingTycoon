# Blender asset kit brief
Build the real 3D asset kit for Cotton Circuit, a pastel cotton-candy racing tycoon where a kart races concentric lanes inside a large candy machine and winds sugar around a central stick.

Work only in D:/UnityProjects/RacingTycoon. Own ONLY Art/Blender/** and Assets/CottonCircuit/Models/**. Do not change Unity scripts/settings or launch Unity. Other code is being implemented concurrently. Blender binary: C:/Program Files/Blender Foundation/Blender 5.2/blender.exe. Launch background commands through exec; hidden if Start-Process. Read tools/skills as required but no user questions: resolve art choices.

Deliver create_assets.py (re-runnable from project root), CottonCircuit.blend (all assets in named collections), asset-manifest.json (names, dimensions, materials, triangle counts), preview.png (clear asset-sheet render, ortho studio lighting) and separate FBX files named below. Validate mesh presence, sane dimensions, nonempty FBX files, named material slots and wheel objects. Run validation and inspect the actual preview. Do not install add-ons or use generated image tools.

Coordinate contract: FBX Unity Y up, +Z forward, meters. Use Blender -Y as forward and FBX axis_forward=-Z, axis_up=Y with baked transform if needed; VERIFY mapping from exported transform or state actual convention in manifest. Assets origin at ground center, local bounds without spread positions. Arrange preview via duplicated/linked objects in a Preview collection excluded from exports. Semantic material names: Strawberry (#F48DAB), Cream (#FFF1D4), Soda (#7ACDCE), Vanilla (#F9D27D), Navy (#29324D), Plum (#6C577F), White (#FFF9ED), Mint (#99C4AE), Gold (#DBAE61), Tire (#414059), Wood (#D59C79). No external textures. Rounded/beveled clean low-poly shapes, warm boutique toy aesthetic. Avoid excessive mesh/vertex counts (each item ideally below 12k triangles except kiosk).

Assets:
- Kart.fbx: 1.6m wide x 2.5m long x 1.7m high. Rounded pink/cream kart, visible separate four wheels with names WheelFL/FR/RL/RR and pivots at axle, steering wheel, seat, sugar tank with crystal topper/spiral exhaust, compact canopy flag, headlights. +Z front. No driver needed.
- Kiosk.fbx: 5m wide x 3m deep x 4.7m high. Cream boutique candy stall, pink and cream striped awning, wooden counter, rounded framing, rooftop cotton candy emblem or silhouette, physical small candy display details, side shelves. Front +Z, counter surface ~1.25m. No text geometry necessary (Unity adds signage).
- Spinner.fbx: pedestal ~2m diameter x 1.5m high plus stick to total 6.5m high. Cream/plum base, pink/soda ring details, tan/cream slender center stick. Candy will form between y=2.5 and 7.0 above this root. Max pedestal radius 1.8m.
- Puff.fbx: one fluffy cluster of smooth overlapping lobes 1m diameter centered at local origin (exception ground origin), White material for runtime tint, max 1800 triangles.
- Customer.fbx: charming simple person 1.65m tall, rounded limbs, dark hair/cap, Soda top, Navy trousers, facing +Z; feet at y=0. Below 6k triangles.
- Crystal.fbx: stylized sugar crystal cluster .7m tall, Vanilla/Soda facets.
- Arch.fbx: candy-striped start arch, 4m clear inner width, 4m high, axis passage +Z, wide bases. Cream/Strawberry.
- Tree.fbx: lollipop-like tree 3.8m high, trunk Wood, fluffy mint/pink rounded canopy.
- Lamp.fbx: warm park light 3.2m tall, Navy base/post, Gold/White globe.

Use bpy ops/mesh API, save source, export selected asset collections to FBX. Keep generation deterministic. Report tests/render findings in docs/briefs/blender-report.md. Commit only your owned files and report after verification; root controls all other commits. Final response short with commit ID, checks and any integration concerns.

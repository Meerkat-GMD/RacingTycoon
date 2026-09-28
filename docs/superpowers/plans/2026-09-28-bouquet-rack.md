# Cotton candy bouquet rack

Goal: restore the vendor-style inventory display requested in the user's photo: a central metal stand with radiating clips and individually draggable bagged cotton candy. Keep the existing game art and all inventory actions.

The user supplied the design reference and explicitly authorized rebuilding this rack. Use authored UXML/USS and a transparent raster stand asset; no C# lines, meshes, procedural textures, or runtime-generated visual trees. C# may bind capacity, occupied state and product data only. Preserve the 6/9/12 capacity limits, stable product IDs, quality labels, tutorial focus, trash, delivery and resume actions.

- [x] Create and inspect a transparent metal fan-rack asset; save final asset and provenance in the project.
- [x] Replace grid styling with fixed authored fan positions and product-only interaction targets. Empty positions have no tile backgrounds and do not intercept input.
- [x] Verify full and sparse shelves at capacities 6, 9 and 12, including a gap, stable identities, pointer capture and real delivery/resume/trash actions.
- [x] Inspect runtime screenshots at normal and reduced viewport sizes, build release, and record the result.

Verification: rack scenario 849 checks at both 1600×900 and 1280×720; full gameplay scenario 445 checks. Development and release builds succeeded. See `docs/bouquet-rack-verification.md`.

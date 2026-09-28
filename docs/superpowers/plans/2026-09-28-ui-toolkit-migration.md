# UI Toolkit migration

User-authorized scope: convert existing runtime screens to UI Toolkit and remove C#-drawn UI. Preserve game state, first-sale tutorial, worker-only automatic driving, pause-to-title, saves and existing business/progression actions.

## Implementation

- [x] Audit existing screens and public controller bindings.
- [x] Create UI Builder-editable UXML/USS documents; C# is limited to document loading, data, input and display binding.
- [x] Convert title, new-game confirmation, intro, tutorial and one-time growth hint.
- [x] Convert driving/shop HUD, sugar shaking, candy drag/drop, customers, machines and settlement.
- [x] Convert upgrade prerequisites, equipment/recipes/worker assignment and location preparation.
- [x] Convert older recipe/continuous modes and pause/return-to-title.
- [x] Remove old Canvas builders and custom UI geometry renderers.
- [x] Replace old uGUI-specific runtime tests with UI Toolkit interaction coverage.
- [x] Import/compile documents, inspect screenshots and verify interactions at multiple resolutions.
- [x] Build release, record results and remaining limitations.

## Visual approach

Retain existing title/story/lowpoly sprite artwork and cream, navy, pink and mint palette. Use native rounded UITK cards and asset icons instead of custom geometry. Upgrade cards retain all levels, prices, effects and prerequisite links; scrolling replaces custom graph drawing. Render the real course into the minimap rather than drawing UI road meshes.

## Assets

Kenney UI Pack 2.0 is CC0; Noto Sans KR is distributed under OFL. Keep source URLs and licenses in the project. Use authored visual trees and styles, not procedural textures, custom UI vertices, or runtime-built element trees.

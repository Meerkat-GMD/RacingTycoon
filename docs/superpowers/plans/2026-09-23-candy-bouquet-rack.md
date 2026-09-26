# Cotton Candy Bouquet Rack Implementation Plan

> **For agentic workers:** Use subagent-driven development for bounded graphics and verification work; the main agent owns integration and Unity builds.

**Goal:** Apply the approved low, wide metal bouquet stand with transparent bagged cotton candy to the existing shop UI.

**Architecture:** Keep the runtime uGUI construction and existing production, delivery, trash, and save models. A decorative stand graphic shares bag placements with the shelf UI, while each occupied clip binds one persistent product identity. Extend the existing procedural shop illustrations with a bagged candy variant used by both the shelf and drag ghost.

**Tech Stack:** Unity 6000.5.3f1, C#, uGUI custom Graphics, development-player smoke tests.

## Global Constraints

- Approved visual: central metal mast, radial thin branches, small clips, clear bags, visible pastel candy, compact grade/incomplete tags.
- Preserve the current shop street, race UI, sugar controls, customer/trash behavior and all existing user changes.
- Preserve capacities 6/9/12 and saved inventory; nine is the example capacity, not a new limit.
- Work in the existing `codex/cotton-circuit` checkout because the current game depends on its uncommitted files. Do not stage or commit unrelated work.
- No scene/prefab/YAML modification is required; the shelf is constructed from C#.

## Tasks

- [x] Add an isolated runtime rack scenario that checks real pointer targets, max capacity, stable placement after removal, bagged drag, cancellation and exact delivery/trash. Run against the current implementation to establish a failing baseline.
- [x] Add a non-raycasting `CandyRackGraphic` and shared `BagRect(index, capacity)` placements within the shelf footprint. Extend `ShopArtGraphic` with `BaggedCottonCandy`, preserving every existing art kind.
- [x] Replace shelf card rendering with bag hit areas and physical grade tags. Keep each product on its clip while other products sell, expose detail on hover/selection, and carry a bagged ghost while hiding the source product.
- [x] Build a development player; run the rack scenario and the existing shop and legacy smoke scenarios. Inspect full and sparse rendered screenshots, including 6/9/12 capacities and scaled resolutions.
- [x] Request an independent implementation review, resolve concrete findings, record verification and provide the playable build and a screenshot.

## Verification commands

```powershell
./Tools/test-shop-shift.ps1
./Tools/build-candy-rack.ps1
# Rack scenario: development player --shop-shift-smoke --candy-rack-smoke
./Tools/verify-player.ps1 -BuildFolder Builds/CandyRack -OutputFolder Logs/Smoke-candy-rack-shop -ShopShift
./Tools/verify-player.ps1 -BuildFolder Builds/CandyRack -OutputFolder Logs/Smoke-candy-rack-legacy
```

## Progress

- Context inspected: the live shop shelf is in `ShiftUI.cs`; `ShopStreetUI.cs` owns the customer area and is outside the change.
- Unity CLI and process checks found no running Editor. Existing batch build and offscreen runtime capture tooling are available.
- RED baseline compiled successfully and failed at the expected missing stand assertion (`Logs/Smoke-candy-rack-red/result.txt`). Existing production/shift model checks: 28 passed.
- Graphics implementation, integration, and independent review completed.
- `Tools/build-candy-rack.ps1` uses `CandyRackBuild.Build` to compile the current scene without the existing builder's scene/asset regeneration.
- Final dedicated rack checks: 2,412 passed; legacy mode checks: 623 passed. Tag bounds fixed and independently re-reviewed. Captures copied to `docs/screenshots/candy-bouquet-rack*.png`.
- Concurrent user task changed size labels to 소/중/대 and adds resume/reaction flows. Preserved their edits. Shared the full-suite RaceResumeDropTarget rendering timing failure with that task; their test now renders before raycasting, with no production workaround.
- Final combined shop smoke: 2,872 passed (`Logs/Smoke-candy-resume-release-check/result.txt`), including this plan's dedicated rack scenario and nine additional fixture save-validity checks. Integration verification complete. Final combined Release: `Builds/CandyResume/CottonCircuit.exe`; Release receipt verified.

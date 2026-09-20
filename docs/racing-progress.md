# Racing feel progress
Plan: docs/superpowers/plans/2026-09-21-racing-feel.md
Base: ed782d9. Approved design. Branch codex/cotton-circuit.

- Core driving: complete in 8b1d867 and dbf665c. Direct steering, drift/release boost, wall slide/recovery, asymmetric course, shortcut, forward-only cotton and lap timing. 22 economy/production and 17 driving tests pass.
- Blender props: complete in 980b3d0 and 32857f0. Chevron, Barrier and ShortcutGate imported into course. Dimensions, materials, front normals and FBX round trips validated; Unity screenshots inspected.
- Unity integration: complete. Full-width chase camera, motor/skid/boost sound, body lean, tire marks and boost trails. Race HUD, mini-map, lap times, live cotton preview, controls and wrong-way warning. Existing sales/upgrades/save retained.
- Review: two P2 shortcut defects fixed, scoped independent re-review approved. Additional backward seam and repeated recovery checks passed.
- Final validation: 17 editor checks and 106 runtime checks passed. Evidence: Logs/Smoke-racing-verified. Actual shortcut traversal, no false wall hit, production/sale/upgrade/save cycle, pause and multiple display ratios. No text overflow or runtime errors. Screenshots copied to docs/screenshots.
- Release: Tools/build.ps1 -Release exited 0; COTTON_RELEASE_SUCCESS 96646301. Builds/Windows/build-info.json confirms Release, Unity 6000.5.3f1. Executable at Builds/Windows/CottonCircuit.exe.
- No remaining implementation or required verification work. Player feedback can guide steering sensitivity and economy balance. Opponents/ghosts remain deferred as approved.

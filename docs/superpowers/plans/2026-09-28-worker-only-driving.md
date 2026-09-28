# Worker-only automatic driving

> **For agentic workers:** Use subagent-driven-development for core, UI and runtime test updates; parent integrates controller and release validation.

**Goal:** Remove player auto-driving completely. Only a hired worker assigned to a machine can operate that machine automatically, including while the player views it.

**Architecture:** Remove the player AutoDrive state and toggle/help methods. ShopShift validates eligible workers and runs one production path independent of selected view. Controller sends automatic driving inputs only to the selected worker's car; its visual movement cannot add production twice. UI shows status without a toggle and removes tutorial assistance.

**Tech Stack:** Existing Unity 6000.5.3f1, C#, uGUI, V8 save format, standalone core/runtime checks.

## Constraints

- The user's correction replaces the earlier manual-takeover design; hiding or disabling the old toggle is insufficient.
- Tutorial and unstaffed machines require manual driving. Keep the tutorial motion-based popup hiding, Skip and one-time settlement hint.
- Hiring without assignment does not automate any machine. Validate owned machine, worker count and education.
- Selected and offscreen worker production share the existing rate, cost, extraction and inventory limits.
- Staffed machines ignore manual driving/production actions; machine switching and selling remain usable.
- Keep existing saves and unrelated workspace work. Do not alter worker progression prices or production rate.

## Work

- [x] Reproduce the selected-worker exclusion in a core RED test.
- [x] Implement HasWorker / WorkerCanOperate and single worker production path, with core and save regression coverage.
- [x] Remove player AutoDrive / ToggleAutoDrive / TutorialAutoDrive and integrate worker-only vehicle animation.
- [x] Remove toggle/help UI and stale copy; show noninteractive ownership/status and guard staffed-machine controls.
- [x] Update runtime test driving fixtures to send explicit test-only manual inputs, and cover no-player-auto plus worker switching/waiting/reload.
- [x] Build and verify tutorial, worker driving, normal progression and legacy regressions; inspect actual screenshots.
- [x] Record results, update current docs, review integration and rebuild release.

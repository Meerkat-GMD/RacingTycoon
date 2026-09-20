# Cotton Circuit code review

Reviewed 2026-09-21 against `docs/briefs/code-review-task.md` and the approved design. Scope: Production, Economy, GameSession, GameController, SaveStore, KartController, CandyView, GameUI, WorldView, ProjectBuilder, and the provided test coverage. Read-only implementation review; no Unity process launched, source changed, staging, or commit performed.

## Important findings

### P2 — Preserve elapsed time instead of dropping every frame's time above 100 ms

**Location:** `Assets/CottonCircuit/Scripts/GameController.cs:54–59` (also the customer timer using `dt` in the same method).

`Tick` clamps the caller's elapsed time to 0.1 seconds and supplies that shortened time to both driving and `GameSession.Tick`. This is more than a stability bound on movement: it discards game-clock time. Any frame slower than 10 FPS, including transient stalls, extends the advertised 60-second run; sales and feedback timers slow down too. The production unit test passes because it bypasses this controller layer.

**Reproduction:** Start a stationary race, then call `game.Tick(0, 0, false, 0.2f)` 300 times. The inputs represent 60 seconds, but `Session.Remaining` is approximately 30 seconds and the game remains Racing. Equivalently, run at 5 FPS with vsync disabled and wait 60 seconds without accelerating. This is a source-derived reproduction; I did not launch Unity.

**Correction:** Preserve elapsed time for timers and process movement in bounded substeps where necessary. Ensure the final partial step observes the run deadline, and avoid processing shop time unexpectedly in the remainder of a frame that ends a race. Add a focused controller-level regression comparing equal total elapsed time in 0.05- and 0.2-second chunks.

### P2 — Keep upgrade controls inside a resized window

**Location:** `Assets/CottonCircuit/Scripts/GameUI.cs:30–35`; arbitrary resizing is enabled in `Assets/CottonCircuit/Editor/ProjectBuilder.cs:55–56`.

Every UI control is positioned using fixed coordinates in a 1600×900 design, anchored to the top left. `ScaleWithScreenSize` with `matchWidthOrHeight = 0.5` does not keep that entire fixed rectangle visible at other aspect ratios. In a narrower window the right side is clipped, including the upgrade purchase buttons. In a wider window the bottom controls are clipped. This prevents buying upgrades through the UI at ordinary resized dimensions, rather than merely changing appearance. The camera's fixed normalized 0.755 viewport also stops lining up with the sidebar. The existing screenshot checks only exercise 16:9 resolutions and cannot catch this.

**Reproduction:** Resize the window to 1200×900 (4:3). The scale factor is approximately 0.866 and the effective canvas width approximately 1385.6 design units. Upgrade buttons begin at design x=1240+207=1447, which becomes physical x≈1253, entirely outside the 1200-pixel window. Earn enough money for an upgrade and observe that its purchase button cannot be reached. This is a source-derived layout calculation; runtime visual confirmation is assigned to the root reviewer.

**Correction:** Fit/letterbox the fixed 1600×900 composition with a consistent camera viewport, or anchor and lay out the sidebar responsively. If supporting only 16:9 is deliberate, enforce that aspect instead of exposing unrestricted resizing. Verify at least 4:3, 16:10, and a wide aspect in addition to the existing 16:9 checks.

## Other reviewed behavior

No critical issue identified in the scoped source. Normal session transitions prevent duplicate finishing, inventory is capped, upgrade costs/caps are enforced, and sale removes its product before the next tick. The production capacity bounds generated samples, and the rendering code explicitly destroys replaced generated meshes while retaining shared asset materials. Saves use a temporary file and replacement with a backup, and invalid saves block overwrite until an explicit archive/reset. Tests and smoke runs select isolated save directories.

## Verification boundaries and minor observations

- The supplied 22 domain tests and editor/runtime checks were inspected, not rerun. Build success, real UI input, shader inclusion, and visual quality require the independent root validation.
- `WorldView.LateUpdate` continues the central spinner's decorative rotation while paused; economy, driving, and production remain frozen. This is a minor visual pause inconsistency, not a release-blocking gameplay defect.
- Initial funds are 80 coins while every first upgrade costs 120. This differs from the design sentence saying starting funds can buy an upgrade, but the intended drive/sell/upgrade loop is still playable. Treat this as a small balancing/spec alignment decision.
- AI opponents, mobile support, multiple tracks, and freeform shop placement are explicitly outside this version and are not findings.

## Scoped fix re-review — 2026-09-21

Reviewed `Logs/review-fixes.diff` and the current controller, UI, world, and smoke-test source. Read `Logs/Smoke-final/result.txt`, which reports `PASSED` and `Checks: 73`. The root reviewer separately reports inspecting the actual rendered images. No Unity process was launched and no implementation files were changed by this review.

- **Elapsed-time P2: addressed.** `GameController.Tick` now validates elapsed time and consumes it through steps of at most 0.05 seconds instead of dropping everything above 0.1 seconds. Production still receives the actual substep motion, and `GameSession.Tick` retains deadline clipping. Once a race ends, subsequent substeps stay in Results and cannot start selling inventory or stock the same product again. The 60-second per-call upper bound covers a complete bounded race and all six queued customer sales. The reported runtime pass includes a focused two-second hitch assertion. The additional 300 × 0.2-second stationary-run regression is present in source but was not part of the 73-check run; its execution remains part of the root's final build verification.
- **Window-aspect P2: addressed.** `CanvasScaler.Expand` selects the fit scale, and the centered 1600×900 composition retains all fixed-coordinate controls within the canvas. `WorldView.UpdateViewport` uses the same minimum width/height ratio and centering offsets, matching the camera's 1208-design-unit game region to the sidebar. The added background camera clears the uncovered letterbox area. The runtime result supports the root-reported 4:3, 16:10, and wide button-bound checks. No new layout defect was identified in this scoped source review.
- **Minor paused-spinner observation: addressed.** The spinner and central candy rotation now require `!AnimationPaused` and nonzero kart speed. Both explicit pause toggling and focus-loss pause set this flag, and reset clears it. The camera's smooth framing transition remains independent of this gameplay decoration; this does not advance production or economy.
- **Initial-funds observation:** the root reports aligning the design wording to the intended 80 starting coins / 120 first upgrade cost; no economy code change was required.

**Disposition:** Both Important findings are closed by the reviewed source changes and available focused verification. No additional Critical or Important issue found in these fixes. The final stronger slow-frame regression/build result remains a root verification step, not a reopened code finding.

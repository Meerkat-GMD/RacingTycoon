# First-sale tutorial implementation plan

> **Superseded assistance policy (2026-09-28):** Optional player automatic driving was subsequently removed at the user's request. The tutorial now requires manual driving; only hired, assigned workers operate their own machines automatically. The original automatic-help requirements below are historical. Story, tutorial skip/resume, protected practice, and the one-time settlement sentence remain. Current evidence: [worker driving verification](../../worker-driving-verification.md).

> **For agentic workers:** Use subagent-driven-development for the isolated core, UI, and runtime-test work; keep controller integration in the parent task.

**Goal:** After the new-game story, guide one strawberry cotton-candy sale, then show only “번 돈으로 가게를 성장시킬 수 있어요.” once after the first day's settlement.

**Architecture:** A persisted primitive tutorial stage belongs to Economy. ShopShift protects practice actions and freezes business/customer clocks while normal driving grows candy. A separate uGUI overlay reads stage and real target RectTransforms. GameController starts it only for title new games, preserves resume/legacy behavior, and consumes the growth hint after returning from the first results screen.

**Tech Stack:** Unity 6000.5.3f1, existing uGUI Text/Malgun Gothic, V8 JSON saves with backward-compatible default fields, Mono core checks and standalone Windows runtime checks.

## Global constraints

- Use the approved sequence: PourSugar → Drive → Extract → Deliver → Success → Complete. Disabled remains zero for existing saves and ordinary Initialize calls.
- Actual successful actions advance the tutorial. Sugar must suffice for one saleable candy (initially 50g). No automatic time limit.
- Only the business/customer timers pause; driving/production work in Drive. One fixed strawberry customer. Early extraction, wrong delivery, disposal, or emptying cannot lose the training candy.
- Offer optional automatic driving help, a top-right independent tutorial skip, and an explicit normal-business start after success.
- Save/resume practice stage. Skip before sale clears practice into a normal full first day; skip after sale retains the sale.
- First-day Results → Preparation shows one sentence and a close button, once per new game including reload. No purchase, growth-map highlight, or subsequent growth tutorial.
- Continue old saves without tutorial or new hint. Keep existing unrelated dirty workspace changes. Build through Tools/build.ps1; no live editor for this project is available.

## Task 1: Protected core and persisted progress

**Files:** new `Scripts/Core/Tutorial.cs`, `Tools/Tests/TutorialTests.cs`, `Tools/test-tutorial.ps1`; modify `Core/Economy.cs`, `Core/ShopShift.cs`, `Core/ProgressionShift.cs`, `SaveStore.cs`.

**Interface:** `Economy.TutorialStep`, `Economy.GrowthHintShown`; `Tutorial.Active`, `RequiredSugar`, `SugarProgress`, `Refresh`, `TryConsumeGrowthHint`; `ShopShift.BeginTutorial`, `CompleteTutorial`, `SkipTutorial`.

- [x] Add a behavioral test for explicit startup and observe failure before implementation (new-game tutorial could not begin).
- [x] Implement explicit opt-in, step transitions from Pour/Advance/Extract/Deliver, protected actions, frozen timers, clean skip, and one-shot hint consumption.
- [x] Validate new primitive fields while old V8 payloads default to Disabled. Test valid round trips, malformed stages, resuming, clocks, wrong actions, skip stages, first-day hint, and legacy behavior.
- [x] Run `./Tools/test-tutorial.ps1` and existing progression/shift/save checks. Tutorial 13/13, shop shift 46/46, progression shift 458/458, and progression save 20/20 passed. Logs are `Logs/tutorial-tests.txt`, `Logs/shop-shift-tests.txt`, `Logs/progression-shift-tests.txt`, and `Logs/progression-save-tests.txt`.

## Task 2: Controller and real-screen coaching

**Files:** new `Scripts/TutorialController.cs`, `Scripts/TutorialOverlayUI.cs`, `Scripts/TutorialTargetsUI.cs`; modify `GameController.cs`, `ShiftController.cs`, `ProgressionController.cs`, `TitleScreenUI.cs`.

**Interface:** `GameController.Initialize(..., bool tutorial = false)`; properties `TutorialStep`, `TutorialActive`, `GrowthHintVisible`; actions `SkipTutorial`, `CompleteTutorial`, `TutorialAutoDrive`, `DismissGrowthHint`. Overlay `Show(GameController)` and `Close()`.

- [x] Start ShopShift tutorial before UI.Initialize only after title new-game intro completion or skip; retain ordinary Initialize default.
- [x] Stop the vehicle outside Drive during practice. Keep production simulation, save stage changes, and refresh UI. Complete/skip restores normal actions/timing.
- [x] Show Mina/current action, actual target outline/arrows, sugar/production progress, Skip and auto-help. Keep drag targets and car view clear; hide coaching during pause. Success and growth have simple modal buttons. Implementation is present; layout and interaction evidence belongs to Task 3.
- [x] Consume and save the growth-hint flag on first-day ReturnToPreparation; show the exact approved sentence without follow-up walkthrough.

## Task 3: Runtime evidence, regression and delivery

**Files:** new `Tests/TutorialRuntimeSmoke.cs`, `Tools/verify-tutorial.ps1`, `docs/tutorial-verification.md`; update `TitleScreenRuntimeSmoke.cs`, README and relevant prior flow docs.

- [x] Observe missing runtime tutorial before controller/UI integration where possible; use reflection to keep tests compilable before the new API exists.
- [x] Build development player `./Tools/build.ps1 -BuildFolder Builds/Tutorial`.
- [x] Verify actual sugar drag, real driving, extraction, product-to-customer drag, clock protection, success/skip, reload, and exactly one post-settlement hint. Inspect native screenshots at 1600×900, 1280×960 and 1920×820. Fail black captures; use hidden launch with an initial width offset.
- [x] Update title tests for new intro → tutorial flow, while saved/corrupt Continue keeps prior behavior. Run existing progression and save regressions.
- [x] Read-only integration review, resolve substantive findings, build `Builds/Tutorial-Release`, and document exact results and limitations with screenshots.


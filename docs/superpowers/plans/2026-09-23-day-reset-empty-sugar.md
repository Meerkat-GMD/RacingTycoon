# Daily reset and empty sugar implementation plan

> **For agentic workers:** Use subagent-driven-development for core logic; the primary agent integrates Unity input/UI and verifies the built player.

**Goal:** Start every new business day with fresh production, stock and customers; provide a sugar-empty button and right-click shortcut.

**Architecture:** ShopShift owns all business mutations. Both UI and mouse input call GameController.EmptySugar(), which saves and refreshes the world. NextDay resets business state and inventory, and the controller resets transient driving/UI state.

**Tech Stack:** Existing C# model, Unity 6000.5.3f1, uGUI, legacy Input, Mono console tests and development-player smoke tests.

## Global constraints

- User authorized implementation on 2026-09-23. Preserve existing uncommitted work in this active project.
- Next day clears shelf products, loaded sugar/flavor, current candy distance/flavor/quality/identity, daily stats, old customers and reactions; clock restarts at 600 seconds and a fresh first customer arrives.
- Preserve coins, upgrades, shelf capacity and lifetime accounting. Increment day once. Never reopen while already open or paused.
- Empty sugar clears sugar grams/flavor only; preserve current candy, shelf, customer and clock. Reject while paused/closed, or when already empty.
- Button label `설탕 비우기`, visible shortcut `우클릭`; global right mouse down invokes the same action. Cancel active drag to avoid immediately refilling.
- Refresh/save immediately; clear stale selection, candy preview, rack and thread on new day. Reset kart to starting position on new day.
- No new dependencies, scene redesign, commits bundling pre-existing work, or changes to real player saves during tests.

## Task 1: Core business rules and meaningful model tests

Files: `Assets/CottonCircuit/Scripts/Core/ShopShift.cs`, `Tools/Tests/ShopShiftTests.cs`.

- [x] Replace carryover expectations with empty-day assertions; verify failure with `./Tools/test-shop-shift.ps1`.
- [x] Add `public bool EmptySugar()`; test preservation of candy identity/quality, inventory, customer and timer; paused/closed/empty rejection.
- [x] NextDay resets production fields, clears inventory/obsolete completed product IDs, resets daily counters/customer schedule and creates the usual fresh first customer. Preserve lifetime values and progression.
- [x] Run model tests and review scoped diff against `Logs/DayResetBaseline`.

## Task 2: Player input, UI, save and runtime verification

Files: `GameController.cs`, `ShiftController.cs`, `ShiftUI.cs`, `GameUI.cs`, `ShopShiftRuntimeSmoke.cs`, `ShopShiftSaveChecks.cs`, `README.md`.

- [x] Update runtime next-day assertions before implementation and run development-player checks to confirm the old behavior fails.
- [x] Add controller sugar-empty action, guarded by ShiftActionsAllowed, cancelling drag, updating flavor/thread, saving and refreshing.
- [x] Bind `Input.GetMouseButtonDown(1)` before Tick; add button beside F extraction, disabled for empty/inactive state; show shortcut and update help/end-of-day copy.
- [x] New day cancels drag/selection, reinitializes shift driving, updates shelf/preview/thread, and persists fresh state.
- [x] Verify button, drag cancellation, pause/closed guards, reset world presentation and save/reload behavior. Review shortcut binding in compiled source and, where available, exercise native right click.
- [x] Build and run `Tools/verify-player.ps1 -ShopShift`, inspect screenshots across supported resolutions, update README and verification record, and obtain independent review.

## Evidence and progress

- Baseline model tests: 36 passed, 0 failed.
- Baseline scoped source snapshot: `Logs/DayResetBaseline`.

- Completed: model 38/38; development runtime 3027 checks; independent core/integration review clean. See docs/day-reset-verification.md for known baseline legacy test failures and input verification limits.

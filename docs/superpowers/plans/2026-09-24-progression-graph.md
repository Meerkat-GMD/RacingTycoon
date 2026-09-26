# Outgame Progression Graph Implementation Plan

> **For agentic workers:** Use subagent-driven-development with bounded file ownership and review. User has authorized autonomous implementation through completion; do not pause for design or execution approvals.

**Goal:** Deliver the full money-funded progression graph, three concurrent machines, worker automation, paid sugar, locations, and an illustrated outgame UI in the existing playable Unity game.

**Architecture:** Preserve the current pure C# gameplay + runtime uGUI split. Optional `Economy.Progression` activates the new rules; legacy standalone core/test modes retain their rules. A catalog owns permanent progression, ShopShift owns business and machine production, controller owns transitions and driving, SaveStore owns versioned persistence, and an outgame UI owns graph/management/location navigation.

**Tech Stack:** Unity 6000.5.3f1, C#, uGUI, Mono pure-core test executables, Windows player runtime smoke verification.

## Global Constraints

- Use the current dirty `codex/cotton-circuit` checkout; a baseline backup is saved under `.superpowers/sdd/2026-09-24-progression-graph/baseline`. Preserve existing work and never stage unrelated files.
- Initial strawberry-only standard product, initial 180 seconds, basic strawberry sugar free, no separate talent points.
- Stat nodes have levels; feature nodes are one-time. Every listed predecessor must be at least level 1. Buying grants equipment/worker directly.
- Three machines can operate concurrently; direct control can switch during business. Assigned workers resume; unassigned machines pause. Production state persists across switching, clears between days.
- Worker grades gate machine tiers; assigned recipes repeat; workers have no daily wages. Insufficient funds or full shelf pauses relevant automation, without automatic recipe substitution.
- Shared sugar grades, equipment and material caps; pay only for accepted sugar. No refunds for intentional discarded sugar. Completed growth does not waste additional sugar.
- All requested upgrade families are present, including duration/patience/ads/shelves/kart/sticks/flavors/grades/machines/workers/locations.
- UI: large left action panel, right original NPC portrait/dialogue and navigation. Essential labels/numbers only; detailed explanations on hover. No copied reference-game art.
- Target initial content progression 2–4h; later 10–20h is an extension target, not a request to ship the extension now. Quantify initial balance with deterministic simulations and disclose human-playtest limitation.
- Record design assumptions and decisions requiring human follow-up in `docs/progression-design-notes.md`, continue work on reasonable defaults.

## Shared API contract

```csharp
public enum BusinessPhase { Preparation, Operating, Results }
[Serializable] public class NodePurchase { public string Id; public int Level; }
[Serializable] public class ProgressionState {
    public BusinessPhase Phase;
    public List<NodePurchase> Purchases = new List<NodePurchase>();
    public int SelectedMachine, SelectedLocation, CartStyle;
}
public class UpgradeNode {
    public string Id, Name, Description, Category, Icon;
    public string[] Parents;
    public int MaxLevel, BaseCost;
    public double CostGrowth;
    public float X, Y;
}
// Core Progression static API uses Economy e and string nodeId:
// Nodes, Find, Level, Cost, CanBuy, Buy, Enable, DaySeconds, PatienceSeconds,
// ArrivalSeconds, Capacity, MaxSugarGrade, OwnedMachines, WorkerCount,
// WorkerGrade, GrowthMultiplier, SugarMultiplier, QualityBonus,
// SalesMultiplier, SpeedMultiplier, SteeringMultiplier, WorkerMetersPerSecond,
// HasFlavor(e,flavor), HasLocation(e,location), MachineTier(index), MachineMap(index),
// MachineName(index), LocationName(index), LocationDescription(index), FlavorMachineTier(flavor).
// Controller API for UI:
// bool HasProgression, bool InPreparation, bool InBusiness
// void PurchaseNode(string id), BeginBusiness(), ReturnToPreparation()
// void ChooseMachine(int index), ChooseLocation(int index), ChooseCart(int style)
// void ToggleWorker(int machine), CycleRecipeFlavor(int machine), CycleRecipeSize(int machine)
// void CycleSugarGrade(int machine)
// MachineProduction Machine(int index) with WorkerAssigned, RecipeFlavor, RecipeSize,
// SugarGrade, SugarGrams, SugarFlavor, BatchMeters, BatchFlavor, BatchQuality, BatchProductId, BatchSugarGrade.
// GameUI partial hook: BuildOutgame(), RefreshOutgame() returns true if it consumes refresh.
```

### Task 1: Permanent progression and balance catalog

Files: new `Scripts/Core/Progression.cs`, modify `Core/Economy.cs`, new `Tools/Tests/ProgressionTests.cs`, `Tools/test-progression.ps1`.

- [x] Write core tests for locked child purchase, rank-one unlock, double-purchase/cap guards, actual coin deductions, grants, initial grade/flavor/time, stat effects. Run with Mono and record red result.
- [x] Implement 25–35 coherent catalog nodes with explicit parents, costs, levels, display positions, all upgrade families. Use a final location milestone as completion goal.
- [x] Preserve legacy Economy behavior when Progression is null; progression price/shelf/speed effects must be real.
- [x] Run new test runner and existing core suites; review catalog for cycles and unreachable grants.

### Task 2: Multi-machine production and business rules

Files: new `Core/MachineProduction.cs`, modify `Core/ShopShift.cs`, `Core/Production.cs`; new `Tools/Tests/ProgressionShiftTests.cs`, `Tools/test-progression-shift.ps1`.

- [x] Add failing tests for preparation not ticking, explicit begin, basic-only orders, paid accepted pours/insufficient funds, size caps, switching, workers and reset.
- [x] Give BusinessState three independent machine slots. Existing active fields mirror the selected slot for compatibility; synchronize at switching and persistence boundaries.
- [x] Use growth efficiency and sugar efficiency independently. Grade/tier caps clamp consumed progress; flavors and size eligibility follow unlocked capabilities. Free strawberry remains selectable.
- [x] Advance worker machines in bounded steps, auto-pour with the same transaction function, extract only at recipe size, pause on funds/shelf/qualification. Shared day and customers advance once.
- [x] Record material costs; inventory sales pay once; close routes to Results, preparation resets day only on explicit next begin.
- [x] Pass new and legacy pure-core tests.

### Task 3: Controller, saved state, maps and compact ingame controls

Files: new `ProgressionController.cs`, modify `GameController.cs`, `ShiftController.cs`, `SaveStore.cs`, `KartController.cs`, `WorldView.cs`, `Core/RaceCourse.cs`, `Editor/RaceCourseBuilder.cs`, `ShiftUI.cs`, `ShopStreetUI.cs`, `CandyRackUI.cs` as needed.

- [x] Add version 7 with presence marker, validate progression IDs/levels, grant invariants, phases and each machine; migrate V1–V6 through their existing validation and preserve permanent money/upgrades via explicit mapping. Clear old transient run into preparation on migration.
- [x] Default production boot enables progression and opens preparation; legacy smoke modes can explicitly request old mode. Save preparation, operating and results correctly.
- [x] Preserve per-machine driving model while switching, select corresponding built course, cancel active drag, guard state-changing actions during pause/closing.
- [x] Build a third distinct procedural course using existing builder conventions and selected machine map. Use Editor APIs, no raw scene YAML.
- [x] Add compact machine buttons, grade selector, unlocked sugar bags/costs, progress caps, end-day return. Convert existing long permanent instructions to hover hints.
- [x] Verify save/load round trips and legacy fixtures; compile all runtime/Editor code in Unity.

### Task 4: Outgame UI and art

Files: new `OutgameUI.cs`, `UpgradeGraphGraphic.cs`, `HoverHint.cs`, NPC texture under `Resources/Progression`, possibly location art helper. Parent integrates GameUI hooks.

- [x] Create a coherent warm cream/deep ink/pink/mint UI: left ~1100px actions and right ~450px NPC/short dialogue/navigation at 1600x900. Three destinations: graph, equipment/workers, locations/start.
- [x] Graph supports pan/scroll and clear edges, node icons/ranks/costs/states. Clicking buys atomically; hover shows effect/current-next/cost/prerequisites.
- [x] Management exposes owned machines, assigned worker, chosen recipe, sugar grade and cart. Location cards have arrows, eligibility and start action. Persistent short contextual NPC speech reacts to progress.
- [x] Use original generated portrait with suitable shop background, referenced via Resources. No reference-game imagery copying.
- [x] Verify no controls overlap at 1600x900 and 1280x720 and no verbose persistent tutorial copy.

### Task 5: Integration, pacing and release verification

Files: new `Tests/ProgressionRuntimeSmoke.cs`, Editor verification/build helper, `Tools/verify-progression.ps1`, balance simulation and reports; update README and decision log.

- [x] Runtime smoke exercises real scene/UI clicks from fresh preparation through purchase, management, switching, worker sales, close, preparation, save reload. Capture screenshots of all outgame pages, gameplay and hover.
- [x] Review screenshots, iterate visible issues, validate every node family affects actual gameplay rather than only labels.
- [x] Deterministic balance simulation reports early purchases and estimated full-tree timing at defined performance assumptions; adjust costs, never claim measured human 2–4h playtime.
- [x] Run appropriate existing pure suites, new core tests, Unity build and player smoke. Independent review, fix actionable findings and recheck relevant tests.
- [x] Deliver a playable Windows build, screenshots, implementation notes and any real unresolved user decisions. No merge/push required.

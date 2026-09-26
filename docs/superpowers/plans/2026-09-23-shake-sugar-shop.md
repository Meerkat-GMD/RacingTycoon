# Shake Sugar Shop Implementation Plan

> **For agentic workers:** Implement bounded tasks in the current session, with independent production, presentation, and persistence ownership. Review the complete integrated behavior before delivery.

**Goal:** A ten-minute shop day with shake-to-pour sugar, distance-sized cotton candy, F extraction, drag sales, angry wrong deliveries, and drag disposal.

**Architecture:** Add a pure C# ShopShift model alongside the existing legacy race session. Economy.Business stores the serializable shift state; distance products carry explicit distance/flavor fields. The existing controller chooses this model for the default continuous game, while legacy test entry points remain available. New uGUI partials and drag components display and operate this model.

**Tech Stack:** Unity 6000.5.3f1, C#, uGUI, Mono core tests, Windows development-player verification.

## Global constraints

- Day duration 600 seconds; pause freezes all gameplay timers.
- A = [1000,2000)m, B = [2000,3000)m, C >=3000m; (0,1000)m is disposable but unsellable.
- Only actual eligible forward meters consume sugar and grow a batch. Use ArcadeDrive.LastRewardDistance without boosted distance multipliers.
- Sugar capacity100g, pour10g per vertical round trip24 design pixels each way, consumption0.02g/m; free infinite bags.
- No lap product/reward/day increments in the new mode. Preserve existing dirty workspace changes and legacy mode behavior.
- Correct flavor AND size pays once. Wrong sellable delivery consumes product, no payment, angry reaction1.5s; invalid undersize delivery returns to shelf.
- One customer, next arrival2s; no individual patience or tips in new mode.
- Preserve products, sugar and current batch across days/reload. Refuse extraction when shelf full without losing batch.

## Shared interfaces

```csharp
// Add to Product: public bool DistanceBased; public double DistanceMeters; public int FlavorIndex;
// Add to Economy: public BusinessState Business;
// Serializable BusinessState:
// RemainingSeconds=600, Closed, SugarGrams, SugarFlavor=-1,
// BatchMeters, BatchFlavor=-1, BatchQuality=50,
// DayRevenue, DaySold, DayWrong, DayTrashed, NextCustomerIn, Customer.
// Serializable ShopCustomer: Id, Flavor, Size, Angry, ReactionRemaining.
public enum DeliveryResult { Rejected, Sold, Wrong }
// ShopShift(Economy), State, Paused, IsOpen
// Advance(double seconds,double forwardMeters,int skillDelta=0,int hitDelta=0)
// Pour(int flavor), Extract(), Deliver(string productId,string customerId), Discard(string productId), NextDay()
// static SizeForDistance(double), SizeOf(Product), Preview(double meters,int flavor)
// SugarShake.Reset(); bool SugarShake.Move(double x,double y,bool inside)
// GameController: Shift, PourSugar(int), ExtractCandy(), DeliverCandy(string,string), TrashCandy(string), StartNextDay()
```

## Task 1: Production and business model

Files: new Core/ShopShift.cs, Core/SugarShake.cs; modify Core/Production.cs and Core/Economy.cs; new Tools/Tests/ShopShiftTests.cs and Tools/test-shop-shift.ps1.

- [x] Write and run behavioral assertions against the missing model before implementation. Representative cases:
```csharp
var e = new Economy(); var shift = new ShopShift(e);
shift.Advance(10, 1000); Check(shift.State.BatchMeters == 0, "no sugar means no growth");
shift.Pour(1); shift.Advance(10, 1000);
Check(Math.Abs(shift.State.BatchMeters - 500) < .000001, "sugar only covers 500m");
var p = shift.Extract(); Check(ShopShift.SizeOf(p) == -1, "underweight can be extracted");
Check(e.Day == 1 && e.Coins == 80, "extraction gives no day or lap reward");
```
- [x] Implement the contracts with finite input validation, strict customer identity, one-time consumption, bounded rendering samples, carryover, and close-at-600 behavior.
- [x] Verify thresholds, pause, exhaustion mid-step, shelf full, wrong delivery delay, next day, and shake recognition.

## Task 2: Presentation and pointer input

Files: new ShiftUI.cs, ShopDragItem.cs, ShopDropTarget.cs, ShopArtGraphic.cs. Agent owns only these files.

- [x] Build new layout through existing GameUI helpers and partial-class private state. Methods: BuildShiftChrome(RectTransform), BuildShiftShop(), BuildShiftRace(), RefreshShift(), BuildShiftResults().
- [x] Drag sugar maintains a pointer ghost and calls SugarShake.Move in root design coordinates only inside race bounds. It calls game.PourSugar on accepted shake; a simple drop never pours.
- [x] Product drag stores product ID and current customer ID at pickup. Valid drop routes to DeliverCandy or TrashCandy. Cancel/outside restores without state mutation.
- [x] Render flavor-colored cotton candy, sugar bags, customer and trash through vector UI graphics, plus labels; do not introduce external bitmap dependency.
- [x] Display600-second timer, batch distance/tier, next-tier progress, sugar, customer reaction, shelf capacity, close-day summary, and next-day button. No legacy split controls in new mode.

## Task 3: Persistence

Files: SaveStore.cs and new save-focused runtime tests. Agent owns these; core field changes belong to Task1.

- [x] Extend envelope to V4; continue accepting V1-V3 and rejecting invalid files without overwriting originals.
- [x] Validate explicit distance products, bounded preview samples, business state, sugar flavor locks, daily counters, and customer phase.
- [x] Round-trip mid-production, angry customer, and closed day. Migrate existing stock to A/B when entering ShopShift without deleting coins/upgrades.

## Task 4: Integration and runtime scenarios

Files: GameController.cs, new ShiftController.cs, GameUI.cs, SplitUI.cs, WorldView.cs, new tests/ShopShiftRuntimeSmoke.cs, RuntimeSmoke.cs dispatch.

- [x] Add default shift mode with explicit legacy opt-out for earlier split smoke. Initialize Shift before building UI.
- [x] Route controller tick to eligible drive meters and model time, clamp final frame at closing, freeze effects on close, and persist mutations/periodic progress/focus loss.
- [x] Route F and pointer operations through one guarded action path. Refresh product preview after relevant changes.
- [x] Add player scenarios exercising real EventSystem drag handlers, UI bound checks, extraction, correct/wrong/trash delivery, save reload, pause, ten-minute closing, and next day.

## Task 5: Verification and delivery

- [x] Run Tools/test-shop-shift.ps1 and existing core/driving/order/recipe/continuous suites affected by shared product/economy state.
- [x] Build development player to Builds/ShakeShop, run --shop-shift-smoke with a separate save directory, inspect captured UI at1600x900 and1280x720 plus alternate aspect ratios.
- [x] Independent integration review; fix material findings and recheck changed paths.
- [x] Build release player, document controls and verified limits in README and docs/shake-shop-verification.md, deliver executable and screenshot links.

## Progress

- Planning: approved; 10-minute update applied.
- Implementation and independent integration review: complete.
- Verified: model20; editor55; new runtime250; legacy runtime623; shared core/driving/orders/recipes/continuous regression suites passed.
- Development screenshots checked at1600x900 and1280x720; input/layout also checked at1280x960 and1920x820.
- Release: Builds/ShakeShop/CottonCircuit.exe, exit0, configuration Release; verification details in docs/shake-shop-verification.md.

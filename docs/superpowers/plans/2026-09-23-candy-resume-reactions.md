# Candy Resume and Customer Reactions Implementation Plan

**Goal:** Rename sizes to 소/중/대; resume shelf candy by dropping it onto the race; slow arrivals and show emotion icons for successful, incorrect, and overdue orders.

**Architecture:** Keep the current street/shelf/race structure. ShopShift owns atomic product exchange and customer timers; SaveStore migrates prior saves. UI routes race drops and displays core states. Decorative vector emotes remain independent of gameplay.

**Tech stack:** Unity 6000.5.3f1, C#, uGUI, Windows standalone.

## Design and constraints
- User explicitly authorized size, resume and reaction changes, confirmed atomic exchange, and selected arrivals every 60 seconds with 90 seconds of patience.
- Size thresholds remain 1 / 1.5 / 2 laps; only visible names become 소 / 중 / 대 across HUD, shelf, orders, drag caption and help.
- Default resume policy: atomically exchange a stocked product with the currently growing batch. If no batch, remove the stocked product and load it. Preserve distance, flavor, quality and product identity; no payment, loss or duplication. Works with full shelf and undersize candy.
- Preserve loaded sugar on resume. If its flavor differs from the resumed candy, pause production until matching sugar is poured; do not mix flavors or silently discard sugar. The existing flavor-replacement pour rule remains explicit via the player's gesture.
- Arrival interval 60 seconds; each customer waits 90 seconds. First customer is immediate. Maximum three stable slots; full street holds the next arrival delay until space opens. Pending arrivals are not accelerated by a departure. Natural unserved flow usually has one or two customers, since 90-second patience is shorter than two arrival intervals. Pacing probe found production thresholds at 21.08/31.74/41.36 seconds in default auto driving.
- Correct delivery pays once, shows a heart for 1.5 seconds, then leaves. Wrong delivery consumes one sellable product without payment, shows angry icon for 1.5 seconds, then leaves. Waiting expiry shows angry icon for 1.5 seconds and leaves without consuming stock. Reactions cannot accept another product. Undersize delivery remains rejected.
- A small waiting gauge communicates timeout. Pause freezes production, arrivals, patience and reactions. Closing clears customers and keeps stock/batch/sugar. Resume is disallowed when paused or closed.
- Backward-compatible V6 save migrates V1–V5, preserves old angry reactions, gives old waiting customers a full waiting allowance, and expands the old 2-second pending-arrival fraction to the new 60-second interval.
- Preserve existing user/other task changes, including the current candy-rack shelf layout. No commits or unrelated refactors.

## Task 1: Core and persistence
Files: Core/ShopShift.cs, SaveStore.cs, Tools/Tests/ShopShiftTests.cs, Tests/ShopShiftSaveChecks.cs.
Interfaces: `ShopShift.ResumeProduct(string productId)` returns bool; `BusinessState.BatchProductId` and `DayMissed`; `ShopCustomer.Happy`, `TimedOut`, `PatienceRemaining`; `ShopShift.CustomerPatience=90`, `ArrivalDelay=60`, `ReactionDuration=1.5` (retain AngryDuration compatibility if useful).
- [x] Write failing behavioral tests for loading/exchanging full shelf, size/quality/flavor/ID preservation, repeated resume rejection, no wrong-sugar growth, paused/closed guards.
- [x] Implement atomic exchange, identity-preserving extraction and fuel matching.
- [x] Write failing tests for patience, heart state/payment once, wrong/timeout angry state, independent slots and coincident deadlines under long/small ticks.
- [x] Implement bounded customer event processing and daily missed accounting.
- [x] Update V6 validation/migration and Unity JsonUtility round-trip tests for resumed candy, sugar mismatch, happy/angry/timeout states, old V4/V5 values, malformed input.
- [x] Run model suite; parent runs Unity save checks.

## Task 2: Decorative emotes
File: ShopStreetGraphic.cs only.
- [x] Add `ShopStreetArtKind.AngryEmote` and `HeartEmote`, recognizable at 70×65 design pixels, with existing pastel/ink palette and no raycasts.

## Task 3: UI, controller, runtime integration
Files: ShiftUI.cs, ShopStreetUI.cs, ShiftController.cs, ShopDropTarget.cs, GameUI.cs, current candy-rack UI as needed, ShopShiftRuntimeSmoke.cs, IntegrationChecks.cs.
- [x] Change active UI size names to 소/중/대 and preserve thresholds.
- [x] Add `ShopDropKind.Race`, race-wide drop target with drag highlight, clear resume/swap guidance and controller save/preview refresh.
- [x] Show heart/angry pictograms in the customer's bubble, patience gauge for waiting customers, and reaction-specific short caption.
- [x] Update meaningful runtime checks for slower arrivals and expiring customers; test resume via actual EventSystem, swap, continued physical production, heart and timeout captures.
- [x] Verify current candy-rack placement tests remain intact; previous simultaneous shelf work is preserved.

## Task 4: Delivery
- [x] Independent review of timers, exchanges, save migration and UI targets; resolve findings.
- [x] Run required model checks, development build, shop/legacy player verification, inspect screenshots.
- [x] Build Release in Builds/CandyResume, verify receipt/test exclusion, update docs and provide executable plus screenshot.

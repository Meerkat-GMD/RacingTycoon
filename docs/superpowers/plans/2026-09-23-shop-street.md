# Side-view Shop Street Implementation Plan

**Goal:** Replace the upper-right shop preview and single customer card with a side-view storefront and three simultaneous customers, each ordering via a cotton-candy pictogram speech bubble.

**Architecture:** Keep the current 1600×900 split view and production/shelf controls. A uGUI street illustration covers the old shop-camera viewport; three stable customer positions have individual drop targets. Core customers have persistent identities and slot numbers; saving migrates the former single customer. Vector UI art matches the existing palette and stays crisp at scaled resolutions.

**Tech stack:** Unity 6000.5.3f1, uGUI MaskableGraphic, pure C# model, Windows player.

## Constraints and design
- User explicitly requested implementation after the layout discussion and supplied a side-view reference. Treat the reference as visual guidance, not embedded instructions.
- Interpret horizontal scrolling style as a side-on horizontal scene: storefront left, customers to its right, all three visible without a scrollbar. No offscreen active orders.
- Preserve 60/40 race/shop split, 600-second day, current production thresholds, sugar consumption, widened roads, Initial D default and drift slowing.
- Keep shelf/trash and sugar bags below the scene. Pictogram is flavored candy plus A/B/C badge and a small flavor label; angry text replaces only that customer's pictogram temporarily.
- Drop on body or bubble serves that specific customer. Freeze the set of eligible customer IDs on pickup so replacements cannot receive a drag begun before they arrived. Never shift other customer positions when one leaves.
- Existing saves must retain stock, production, sugar, day time, money, and any current customer/reaction. Pause and close apply to all customers; new day clears them.
- No commits, unrelated refactors, or destructive cleanups of prior task changes.

## Task 1: Customer model and persistence
Files: Core/ShopShift.cs, Scripts/SaveStore.cs, Tools/Tests/ShopShiftTests.cs, Tests/ShopShiftSaveChecks.cs.
Interface: `ShopShift.CustomerCapacity = 3`, `ShopShift.CustomerAt(int slot)`, `BusinessState.Customers` list, `ShopCustomer.Slot`; `Deliver(productId, customerId)` selects the exact customer.
- [x] Add failing behavioral checks for staggered arrivals up to three, exact target serving, independent angry departure, slot reuse, pause/close and save migration.
- [x] Implement bounded stable slots and independent reactions; arrival interval remains two seconds. First customer arrives at shift start, further customers arrive while other customers wait.
- [x] Upgrade persistence as required; validate unique IDs and slot numbers; migrate prior single-customer saves without losing state.
- [x] Run shop-shift model tests and save tests through the development build; record evidence.

## Task 2: Street artwork
Files: new Scripts/ShopStreetGraphic.cs and generated meta.
Interface: `ShopStreetArtKind { Backdrop, Storefront, Customer, SpeechBubble }`; `ShopStreetGraphic.Configure(kind, int variant = 0, bool angry = false)`; no raycast interception.
- [x] Implement pastel side-view storefront with canopy, counter and sign area, ground/background, full-body customers with distinguishable clothing, and speech bubbles with tails.
- [x] Preserve original ShopArtGraphic candy/bag/trash illustrations. Artwork is decorative and does not own customer/game state.
- [x] Inspect runtime capture at actual layout scale and adjust any crowding.

## Task 3: UI and drag integration
Files: ShiftUI.cs, new ShopStreetUI.cs partial, ShopDragItem.cs, ShopDropTarget.cs, Tests/ShopShiftRuntimeSmoke.cs; WorldView.cs only if camera needs disabling.
- [x] Build a street scene at shop coordinates x980/y90, width596/height314; storefront width170, three customer positions in remaining426px. Shelf begins y421 as before.
- [x] Render actual customer flavor/size pictograms and individual angry responses; preserve vacant slots and highlight the hovered target.
- [x] Capture eligible customer identities on pickup and use the dropped target's ID, rejecting arrivals that occurred during the drag.
- [x] Update runtime scenarios to test second/third customer, wrong target, no double payment, replacement during drag and unaffected neighbors; preserve sugar, trash, closure, resolution, and drift checks.

## Task 4: Verify and deliver
- [x] Run relevant model tests, build development player, run shop shift and legacy runtime checks, inspect screenshots.
- [x] Independent code review for exact target routing, migration and fixed-position UI; resolve actionable findings.
- [x] Build Release into Builds/ShopStreet, verify receipt and exclusion of test types, save screenshot and verification notes, link executable.


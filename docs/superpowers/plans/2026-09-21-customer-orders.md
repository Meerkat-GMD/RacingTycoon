# Customer orders implementation plan

> Use subagent-driven-development for bounded core/art tasks; parent integrates and verifies. User approved the presented design with “진행해”.

**Goal:** Customers request cotton candy; player hands over matching stock or drives to make it, then earns sale price and a waiting-time tip. One existing circuit, three flavors and two sizes.
**Architecture:** Pure C# order state and fulfillment alongside existing economy. Runtime controller drives customer time, deliberate inventory selection and race production. Unity UI and Blender furniture show queue, product choices and shelf expansion.
**Tech stack:** Unity 6000.5.3f1, built-in rendering/uGUI, Blender 5.2.2, bundled Mono core tests, Windows release.

## Constraints and agreed choices
- Preserve direct arcade steering/drift/boost, racing course and old saves. No other Unity projects touched; existing codex/cotton-circuit branch is the isolated feature branch.
- Remove automatic customer purchases from gameplay. Only an explicit Serve action consumes stock. Wrong product/order selection must not consume anything.
- At most two waiting customers; first arrives immediately. Later arrivals every 15 seconds while capacity allows. Patience 120 seconds, advances in Shop and Racing; pauses in Results and explicit pause. Expiry loses that customer, never coins or inventory.
- Orders use three dominant flavors and two sizes. Dominant flavor is greatest sample count, ties resolved by earliest flavor in sample history; existing mixed products remain usable. Small: 50–99g; large: >=100g. Runs aim for 60g or 120g, max 30 seconds, automatically finish at target. Partial stock remains visible; explicit two-click discard prevents full-stock deadlock.
- Same map: colored collection strip at main-road lateral <=-0.5, neutral bypass at lateral>-0.5. Color follows main course progress thirds. Shortcut is entirely neutral. Production only on collection strip and new forward progress, at 5x prior grams/distance (200g per full collection lap), plus 15% per sugar-tank upgrade level; existing 25% boosted-distance bonus stays. Small generally takes one appropriate flavor section, large two. No artificial recipe overwrite of driven sample colors.
- New shelf upgrade independent of old three levels: ShelfLevel 0..2, inventory capacity 6/9/12, costs 160/300 coins. Blender racks become visible with levels.
- Fulfillment saves immediately; orders, waiting time and shelf level persist. V1 saves migrate to V2 without losing coins/stock/old levels; malformed V2 must be protected.
- Menu preparation when no selected order is allowed; player chooses small/large target. New orders can arrive during driving; race HUD shows selected target/patience and current queue count.

## Task 1: Pure order/economy core (delegated)
Own Core/CustomerOrders.cs, Core/Economy.cs, Core/GameSession.cs, Tools/Tests/OrderTests.cs, Tools/test-core.ps1; report docs/briefs/order-core-report.md.
- [x] Write failing order matching/atomic handover/expiry/pause-safe time/queue cap/reload/shelf and targeted run tests.
- [x] Implement shared API below; run old core+driving and new order tests. Preserve old parameterless StartRun 60s API for original regression tests; overload is used by current game.
- [x] Commit owned files.

### Shared core API
`CustomerOrder`: serializable public fields `string Id; int Flavor; int Size; double Remaining;` const Patience=120.
`ServiceReceipt`: fields `int Price, Tip; double Satisfaction;` (0..1 satisfaction).
`CandyRecipe` static: `FlavorOf(Product)` dominant 0..2, -1 invalid; `SizeOf(Product)` -1 below50, 0 below100,1 otherwise; `TargetGrams(int size)` 60/120; `Matches(Product,CustomerOrder)`.
Economy additions: public ShelfLevel; public List<CustomerOrder> Orders initialized empty; public int OrderSerial, TotalTips, MissedOrders; public double NextCustomerIn, SatisfactionTotal; public int OrdersServed; `StockCapacity` 6+ShelfLevel*3; `ShelfCost` 160/300/0; `BuyShelf()` bool; `Discard(string productId)` bool. Existing TotalSold/LifetimeRevenue increment on order serving; keep old SellNext only for legacy tests, not gameplay. CompleteRun/GameSession inventory guards use StockCapacity.
`OrderManager(Economy)` stores reference, ensures valid empty fresh shop starts first order immediately; `Orders` list property; `Revision` changes on arrival/expiry/fulfillment; `Tick(double seconds)` bounded/finite, expires and fills queue fairly; `Serve(string orderId,string productId)` returns receipt or null, atomic matching/removal/credit. Deterministic sequence first order strawberry small then other flavors/sizes; first order after initialization must be able to request existing valid stock so old saves/full shelves can be served. Price Economy.Price(product); Tip round(Price*.25*Satisfaction), Satisfaction clamp(Remaining/120). Repeat serving ID earns nothing.
`GameSession.StartRun(int targetGrams, double duration)` starts capped Production(min(targetGrams,Economy.Capacity)), Remaining=duration; guards mode/full/invalid. Parameterless StartRun retains old60 behavior. Runtime calls60 or120 and30s.

## Task 2: Blender shop furniture (delegated)
Own Art/Blender/ShopProps.blend, create_shop_props.py, validate_shop_props.py, shop-props-manifest.json, shop-props-preview.png, Models/{DisplayRack,OrderBoard,QueuePost}.fbx and initial metas, docs/briefs/order-art-report.md. Match existing pastel materials and -Y Blender/+Z Unity corrected front. DisplayRack width2.4 depth.9 height1.9 with two shelves/open product display; OrderBoard width1.6 height2.0 with three flavor symbols and no text; QueuePost .4 diameter1.0high. Ground origins, clean outward normals, generator and FBX roundtrip validation.

## Task 3: Runtime and UI (parent)
- [x] Controller initializes OrderManager, ticks waiting during shop/race, removes old autosale, selects order and product IDs, guarded Serve/Discard/BuyShelf actions, targeted runs.
- [x] Neutral bypass/colored collection strip meshes and HUD hints match SugarCollection helper; new pure helper tested for positive/neutral/shortcut boundary semantics.
- [x] Shop UI left bottom order tickets (select and produce), stock selector/list, explicit handover; right upgrades + shelf expansion + size choice + make stock. Race selected order status, total queue; results inventory outcome, return to serve. Keyboard Enter retains start/finish/return, no automatic handover.
- [x] World two customer queue visuals, order board, base display rack plus expansion racks; show actual stock thumbnails.
- [x] Save V1 migration and V2 validation tests, UI fitting and runtime input behavior tests.

## Task 4: Validation and delivery
- [x] All pure tests; Unity editor build and integration checks; runtime smoke tests inventory-first serve, wrong product rejection, missing-product race→return→manual serve, expiry, pause, upgrades, persistence, flavor bypass, old racing functions, aspects/screenshots.
- [x] Independent review and fix real defects. Windows release. README, verification report, progress ledger and screenshots; commit changes.

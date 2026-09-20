# Customer-order core report

Implemented the Task 1 shared API in `CustomerOrders.cs`, `Economy.cs`, and `GameSession.cs`.

- Recipes use the most frequent sampled flavor and the first sampled flavor for ties. Products below 50g do not match orders.
- A fresh economy gets `order-1` immediately. If valid stock already exists, that first order requests the first usable product. Loaded waiting orders and their arrival countdown remain intact. Further IDs are sequential; flavor and size requests cycle deterministically.
- `Tick` advances patience and the arrival countdown only when called by the runtime. The queue holds at two customers, pauses the next-arrival countdown while full, and expires orders without touching stock or coins. Invalid time is ignored; one call advances at most one hour.
- `Serve` validates both IDs and the recipe before mutating state. A successful handover consumes one order and one product, pays the sale price plus the waiting-time tip, and updates sales, tips, revenue, and satisfaction. Failed or repeated handovers have no effect.
- Shelf upgrades cost 160 and 300 coins and raise capacity from 6 to 9 to 12; discard frees one slot. Targeted runs use the requested target capped by machine capacity and a finite positive duration. Parameterless `StartRun()` keeps the prior 60-second behavior.

Verification: `pwsh -NoProfile -File Tools/test-core.ps1` completed with 22 existing core tests, 17 existing driving tests, and 14 order tests passing. The order suite first failed against the missing API. A later invalid zero-gram recipe assertion failed before its fix.

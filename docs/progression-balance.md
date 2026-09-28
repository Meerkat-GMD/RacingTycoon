# Progression balance simulation

The current 30-node, 60-rank catalog reaches the final location and every maximum rank after **39 shifts, or 183.0 minutes (3.05 hours) of simulated business time**. This is inside the initial 2–4 hour content target under the active-play assumptions below. The catalog prices were **not adjusted** after this run.

This run follows the 2026-09-29 change that made soda a starting flavor on every machine and removed the soda trait. On the same strategy the previous 31-node catalog finished after 37 shifts (2.97 hours). Paid soda sugar raises material spending, while the second machine arrives slightly earlier.

| Milestone | Simulated business time | Shift completed |
| --- | ---: | ---: |
| First purchase | 0.05 h / 3.0 min | 1 |
| Second machine | 0.75 h / 45.0 min | 11 |
| Final location | 3.05 h / 183.0 min | 39 |
| All node ranks | 3.05 h / 183.0 min | 39 |

Across those shifts, the strategy sold 392 products, missed 2 orders, and had no shift without sales. It spent 29,030 coins on nodes and 6,524 on paid sugar, ending with 580 coins. The reproducible runner is `Tools/simulate-progression.ps1`, and its detailed daily output is written to `Logs/progression-balance.txt`.

The simulation calls the real pure C# `Progression` and `ShopShift` purchase, opening, pouring, growth, worker, extraction, delivery, and closing paths. It starts with 80 coins, strawberry and soda, and no unlocks. It buys affordable nodes in a fixed milestone-first order, always keeps at least 60 coins for paid sugar, and chooses the highest unlocked location before opening each day. The player operates the highest owned machine. Eligible workers are assigned during preparation to repeat basic strawberry on the first machine and medium soda on the second; their assignments and menus stay fixed throughout the day. Worker production pauses only under the game's normal rules, including insufficient material funds and a full shelf.

While operating, the simulation advances one-second steps at 18 meters per second of active driving distance, multiplied by the purchased engine effect. It serves the most urgent visible order and finishes an existing batch before changing orders. The active machine changes sugar grade only while both the batch and sugar reservoir are empty. Any leftover sugar is discarded without a refund before changing grades or flavors. When the shelf is full and a new order has no match, it discards the oldest product to make room. Guards fail the run if it attempts worker/menu configuration during business or a grade change in a nonempty machine. Purchases, pouring, extraction, delivery, and between-day management consume no simulated action time.

This is an optimistic, deterministic strategy benchmark, not measured human playtime. Driving errors, time spent in menus, slower routes, and different purchase or recipe choices can lengthen completion. The low missed-order count should not be treated as a player expectation. A human playtest is still needed to judge whether the pacing feels right.

# Progression balance simulation

The current 30-node, 58-rank catalog reaches the final location and every maximum rank after **36 shifts, or 173.3 minutes (2.89 hours) of simulated business time**. This is inside the initial 2–4 hour content target under the active-play assumptions below. The catalog prices were **not adjusted** after this run.

| Milestone | Simulated business time | Shift completed |
| --- | ---: | ---: |
| First purchase | 0.05 h / 3.0 min | 1 |
| Second machine | 0.81 h / 48.3 min | 11 |
| Final location | 2.89 h / 173.3 min | 36 |
| All node ranks | 2.89 h / 173.3 min | 36 |

Across those shifts, the strategy sold 381 products, missed 0 orders, and had no shift without sales. It spent 28,080 coins on nodes and 4,089 on paid sugar, ending with 938 coins. The reproducible runner is `Tools/simulate-progression.ps1`, and its detailed daily output is written to `Logs/progression-balance.txt`.

The simulation calls the real pure C# `Progression` and `ShopShift` purchase, opening, pouring, growth, worker, extraction, delivery, and closing paths. It starts with 80 coins and no unlocks. It buys affordable nodes in a fixed milestone-first order, always keeps at least 60 coins for paid sugar, and chooses the highest unlocked location before opening each day. The player operates the highest owned machine. Eligible workers are assigned during preparation to repeat basic strawberry on the first machine and medium soda on the second; their assignments and menus stay fixed throughout the day. Worker production pauses only under the game's normal rules, including insufficient material funds and a full shelf.

While operating, the simulation advances one-second steps at 18 meters per second of active driving distance, multiplied by the purchased engine effect. It serves the most urgent visible order and finishes an existing batch before changing orders. The active machine changes sugar grade only while both the batch and sugar reservoir are empty. Any leftover sugar is discarded without a refund before changing grades or flavors. When the shelf is full and a new order has no match, it discards the oldest product to make room. Guards fail the run if it attempts worker/menu configuration during business or a grade change in a nonempty machine. Purchases, pouring, extraction, delivery, and between-day management consume no simulated action time.

This is an optimistic, deterministic strategy benchmark, not measured human playtime. Driving errors, time spent in menus, slower routes, and different purchase or recipe choices can lengthen completion. The zero missed orders should not be treated as a player expectation. A human playtest is still needed to judge whether the pacing feels right.

# Progression balance simulation

The current 30-node, 60-rank catalog reaches every maximum rank after **25 shifts, or 120.6 minutes (2.01 hours) of simulated business time**, and the final location one shift earlier. This is at the short end of the initial 2–4 hour content target under the active-play assumptions below. The catalog prices were **not adjusted** after this run.

This run follows the 2026-09-29 tuning the user asked for so that traits arrive faster and customers feel urgent: the size tier price rose from 30 to 45 coins and base customer patience fell from 90 to 60 seconds (still +12 per patience rank). On the same strategy the previous tuning finished after 39 shifts (3.05 hours) with 2 missed orders; a 60-coin tier price would have finished after 19 shifts (1.52 hours).

| Milestone | Simulated business time | Shift completed |
| --- | ---: | ---: |
| First purchase | 0.05 h / 3.0 min | 1 |
| Second machine | 0.76 h / 45.6 min | 10 |
| Final location | 1.92 h / 115.2 min | 24 |
| All node ranks | 2.01 h / 120.6 min | 25 |

Across those shifts, the strategy sold 251 products, missed 32 orders, and had no shift without sales. It spent 29,030 coins on nodes and 4,752 on paid sugar, ending with 906 coins. The reproducible runner is `Tools/simulate-progression.ps1`, and its detailed daily output is written to `Logs/progression-balance.txt`.

The simulation calls the real pure C# `Progression` and `ShopShift` purchase, opening, pouring, growth, worker, extraction, delivery, and closing paths. It starts with 80 coins, strawberry and soda, and no unlocks. It buys affordable nodes in a fixed milestone-first order, always keeps at least 60 coins for paid sugar, and chooses the highest unlocked location before opening each day. The player operates the highest owned machine. Eligible workers are assigned during preparation to repeat basic strawberry on the first machine and medium soda on the second; their assignments and menus stay fixed throughout the day. Worker production pauses only under the game's normal rules, including insufficient material funds and a full shelf.

While operating, the simulation advances one-second steps at 18 meters per second of active driving distance, multiplied by the purchased engine effect. It serves the most urgent visible order and finishes an existing batch before changing orders. The active machine changes sugar grade only while both the batch and sugar reservoir are empty. Any leftover sugar is discarded without a refund before changing grades or flavors. When the shelf is full and a new order has no match, it discards the oldest product to make room. Guards fail the run if it attempts worker/menu configuration during business or a grade change in a nonempty machine. Purchases, pouring, extraction, delivery, and between-day management consume no simulated action time.

This is an optimistic, deterministic strategy benchmark, not measured human playtime. Driving errors, time spent in menus, slower routes, and different purchase or recipe choices can lengthen completion. The missed-order count comes from a tireless optimal driver and should not be treated as a player expectation; human players will miss more of the shorter-patience customers. A human playtest is still needed to judge whether the pacing feels right.

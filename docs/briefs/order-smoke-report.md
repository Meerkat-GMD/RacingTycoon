# Customer-order runtime smoke report

`Assets/CottonCircuit/Tests/RuntimeSmoke.cs` now checks the approved order
loop through the development player. The player uses a fresh isolated save and
writes `result.txt` plus screenshots to the requested `--smoke-dir`.

The smoke drives six independent kart runs: strawberry, vanilla, and soda,
each at small (60g) and large (120g) targets. Its test-only steering follows
the main course 7m ahead, drives the colored inner strip only in the chosen
sector, and uses the gray outer lane in the other sectors. It checks the
product's actual dominant flavor and size, so the recipe cannot pass through
an artificial flavor assignment.

The first order uses the actual Make, stock selection, and Serve buttons. It
checks that returning with stock does not sell automatically; the correct
handover pays once with a tip; wrong and repeated handovers pay nothing. It
also checks patience while driving, pause behavior, expiry preserving stock,
saved sales/tips, two shelf purchases, twelve-slot capacity, and a full-shelf
start guard. Drift, boost, recovery, and chase camera checks remain. Screenshots
cover the initial shop, race, results, help, handover, 4:3, and ultrawide shop.
Each captured layout checks active button bounds and text height.

Unity player verification: pending parent build and smoke run.

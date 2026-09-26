# Progression graph layout

Historical single-map layout. Superseded by the [crossing-free six-tab tree](tree-layout-verification.md) after the user required zero crossings.

The graph uses prerequisite adjacency for placement. The 24-node connected component is arranged around the two machine hubs, with short branches for production, staff, sales, and locations. The three independent business upgrades form a loose cluster at the upper left. The separate engine → handling → coupe chain bends below them. No decorative connections have been added.

## Geometry

Measurements use the 36 real prerequisite edges as straight center-to-center segments in UI units. Crossing counts exclude edges sharing an endpoint and count proper intersections; collinear overlaps are excluded.

| Measurement | Previous coordinates | New coordinates |
| --- | ---: | ---: |
| Nodes / prerequisite edges | 30 / 36 | 30 / 36 |
| Connected components | 5 | 5 |
| Mean edge length | 454.2 | 146.1 |
| Longest edge | 1,149.7 | 187.9 |
| Shortest edge | 175.0 | 110.3 |
| Edges between 110 and 160 units | 0 | 28 |
| Proper edge crossings | 53 | 4 |
| Minimum node-center separation | 130.0 | 109.2 |
| Minimum unrelated node-to-edge clearance in the main component | 0.0 | 57.3 |

The new center bounds are X = 70…960 and Y = 60…595. With the UI's right/bottom margins, the map is 1,026 × 667 units. The shortest center spacing leaves 45.2 units between 64-unit node discs. The closest unrelated edge remains 25.3 units outside a disc's radius.

The remaining crossings are:

- `stick_speed` → `machine_3` with `sugar_2` → `machine_2`.
- `sugar_2` → `machine_2` with `machine_3` → `worker_2`.
- `machine_2` → `flavor_soda` with `worker_1` → `worker_2`.
- `machine_2` → `location_1` with `worker_1` → `worker_grade_2`.

The existing prerequisite graph is non-planar. At least one crossing is unavoidable in a straight-line drawing. A one-crossing candidate was examined, but its minimum center spacing fell to 88.9 units and an unrelated line came within 33.4 units of a center. The selected four-crossing candidate gives the compact discs and captions more room while keeping every prerequisite next to its child.

## Scope and verification

Only the 30 node X/Y pairs and their coordinate comment changed in `Assets/CottonCircuit/Scripts/Core/Progression.cs`. An exact before/after source comparison, with only those pairs and that comment normalized, passed. IDs, names, descriptions, categories, icons, maximum ranks, base costs, growth factors, parent arrays, and all progression methods are unchanged.

The geometry was generated and checked with temporary, external Python tooling using NetworkX and SciPy. No graph-layout package or runtime layout code was added to the game.

## UI verification

- Unity Editor integration checks: 58 passed.
- `Tools/test-progression.ps1`: 14 passed.
- `Tools/verify-progression.ps1 -OutputFolder Logs/AdjacentGraphSmoke`: 103 passed, including wheel/button zoom, drag navigation, fit reset, purchase, save/reload, and the existing business loop.
- Captures at 1600×900 and 1280×720 were reviewed independently. Node discs and names remain separated, including the long worker-training names.
- Only incident edges are emphasized on hover. Prices and effects appear in the tooltip; duplicate description/effect lines are suppressed.
- Screenshots: [Overview](screenshots/progression-adjacent-graph.png), [Hover](screenshots/progression-adjacent-hover.png), [1280×720](screenshots/progression-adjacent-1280.png).

# Two course profiles

Task 2 of `docs/superpowers/plans/2026-09-21-map-recipes.md` is implemented in the existing feature workspace.

| Property | Map 1 / index 0 | Map 2 / index 1 |
|---|---:|---:|
| Main length | 809.8285 m | 1162.5093 m |
| Base kart clean lap | 46.02 s | 65.72 s |
| Maximum centerline radius | 154.5755 m | 188.2788 m |
| Shortcut length | 129.3988 m | 186.4190 m |
| Main distance bypassed | 153.1776 m | 226.1443 m |
| Distance saved | 23.7788 m | 39.7253 m |
| Shortcut progress entry / exit | 221.9060 / 375.0836 m | 660.5197 / 886.6640 m |

Map 1 uses a broad eastern sweep, a rolling northern section and a long southern opening. Map 2 has its own northern chicane, eastern bends and southwest dogleg. Their normalized paths are distinct rather than scaled copies. Both road ribbons, including their half-width, fit within radius 200.

`RaceCourse.ForMap(0/1)` returns cached profiles; invalid indices throw `ArgumentOutOfRangeException`. `Shared` remains map 0. `MapIndex`, `BoundsMin` and `BoundsMax` are public read-only properties; bounds include road width. Existing sample/project APIs and road widths are unchanged. Shortcut progress uses physical arc length between connected main-course progress endpoints.

`RaceCourseBuilder.Build` assigns two `Transform` roots to `WorldView.CourseRoots`, activates map 0 by default, and generates separately named meshes: `Racing surface 1/2` and `Sugar cut shortcut 1/2`. Main road colors are `InnerLane` and `MiddleLane`; shortcuts use `RoadNeutral`. The old flavor/collection strip is removed. Center dashes, barriers, chevrons, checkers and shortcut gates follow each actual path. Optional `CandyTunnel` and `FinishMarker` assets are placed on both courses when available. No modifications were made to `ArcadeDrive`.

Verification performed with the bundled Unity Mono compiler/runtime, without launching Unity or rebuilding the scene:

- RED: new map suite failed against the old 216.337 m circuit and missing map API (0 passed, 8 failed).
- GREEN: map suite 8/8, including finite bounds, seams, all waypoints, lane projection, map validation, base-speed clean laps and actual shortcut traversal followed by a counted lap on both maps.
- Existing core 22/22, driving 17/17, orders 14/14 and collection 9/9 passed.
- Existing driving fixtures changed only for new road coordinates, length/radius requirements and enough simulation time for the longer route. Physics, wall, reward, recovery, oscillation and shortcut assertions remain intact. The clearance overlap fixture now also checks that it genuinely overlaps a too-narrow shortcut edge.

One initially tight southwest bend on map 2 produced a single full-speed follower wall contact; diagnostic progress/position localized it, and moving that anchor outward made the unchanged follower complete both circuits with zero contacts.

Unity mesh/landmark appearance, imports, runtime root switching and complete recipe runs remain part of the parent's scene build and player verification.

# Driving feel: shorter courses and close landmarks

2026-09-21. Course slice of `docs/superpowers/plans/2026-09-21-driving-feel.md`.
Latest user preference is strong acceleration and speed, with Kart and Downhill
handling available for comparison; validation therefore uses 26 m/s.

## Courses

- Map 1: 560.81 m, preserving the east sweep and rolling north section.
- Map 2: 668.62 m, preserving the north chicane and southwest dogleg. The east
  bends and southwest transition are broader so increased speed stays usable.
- Main half-width remains 4.8 m; shortcut half-width remains 2.2 m. Both maps
  keep their indices, centerline projection, mapped shortcut progress and
  existing exploit guards.
- Conservative outer road radii including width are 108.63 / 117.54 m, below
  the 130 m budget. The nearest road edges remain at least 61.7 / 84.98 m from
  the center, beyond the 30 m spinner stage.
- Shortcuts save 13.0 / 16.6 m and support a clean counted lap in both styles.

## Reused scenery

No new art was created for this slice. Existing Blender Crystal props sit on
alternating sides every 12 m, at 2.4 scale; every third marker gets an opposite
Tree at 0.95 scale. Each prop's actual transformed renderer bounds determine
its conservative horizontal footprint. Placement is skipped if that footprint
would approach either road within 0.55 m, enter the central stage, or pass
radius 137. The same check covers corner boards and the finish landmark.

The CandyTunnel now sits 24 / 26 m after the start, wholly on the first straight.
Its existing 11 m opening leaves at least 0.66 m beyond the road's lateral
excursion across its 8 m depth. The shortcut gate moves one third down the
shortcut, beyond the road split, and widens to 1.4 on X. Both gate feet are
0.67 m outside the nearest ribbon before allowing for their approximately
0.33 m footprints. The starting arch widens to 2.75 on X so its feet clear the
unchanged 9.6 m road. Both course roots retain their independent scenery.

## Verification

The new length, radius and quick-lap targets were run against the old courses:
2 tests passed and 6 failed as expected (old lengths 809.83 / 1162.51 m).
New downhill coverage also failed first on late corner entry.

`Tools/test-maps.ps1`: 12 passed, 0 failed with updated physics. Main laps use
full throttle and no drift/braking at 26 m/s, with no wall hits and full earned
progress. The test driver aims 12 m ahead for Kart and 14 m for Downhill to
account for their different steering/tire response times. This is test-driver
anticipation only; no automatic steering was added to gameplay.

| Style | Map 1 lap | Map 2 lap |
| --- | --- | --- |
| Kart | 22.28 s | 26.44 s |
| Downhill | 22.50 s | 26.66 s |

Both styles also traverse each actual shortcut, merge without wall contact,
earn no discontinuous reward pulse, and finish a counted lap. Seam, finite
bounds, stage clearance, waypoint projection, distinct shapes and unsupported
map rejection remain covered. The DrivingTests overlap fixture corresponding
to the changed geometry is `(80.9, 19.0)`.

Pure geometry probes checked gate-foot clearance and tunnel opening above.
The parent integration owns Unity generation, rendered prop counts/clearance,
screenshots, player smoke tests and final release verification; this slice
does not claim that those checks have run.

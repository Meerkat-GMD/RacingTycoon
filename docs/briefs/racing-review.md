# Racing iteration review

Initial read-only independent review against base ed782d9 and approved racing-feel plan found two P2 defects:

1. Main/shortcut overlap could select the narrower road and collide despite sufficient main-road clearance. Reproduced at (26.9341, 0.0607). Fixed by selecting against clearance-adjusted ribbon widths; added exact-position and actual steering tests.
2. Shortcut projection discontinuities deliberately receive no cotton, but laps also used that reduced reward distance and completed late. Fixed by validating forward course-seam crossings independently from cotton rewards, retaining high-water protection. Added actual shortcut traversal through a finish crossing.

Scoped independent re-review approved the fixes in dbf665c. All 39 tests passed (22 production/economy and 17 driving). The reviewer additionally drove backward across the seam four times without earning a lap and repeated recovery 100 times without adding laps or cotton rewards. No further actionable issues were found.

Visual inspection also found Blender plaque front normals facing inward, exposed by Unity backface culling. Commit 32857f0 fixes generator winding from signed polygon area, regenerates source and FBXs, and adds source/FBX front-normal regression checks. Blender validation passed.

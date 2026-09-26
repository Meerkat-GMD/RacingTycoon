# Crossing-free trait tabs implementation plan

**Goal:** No crossing or overlapping prerequisite lines, with every purchase requirement visible and easy to reach.

**Architecture:** Six purpose-based tabs use small downward prerequisite trees. Every in-tab prerequisite is drawn as a directional edge. Every cross-tab prerequisite is a named, status-marked navigation button immediately below its target. The same existing node IDs, costs, effects, parent arrays and saves are used throughout. A prerequisite reference navigates, never purchases.

**Tech stack:** Existing Unity 6 uGUI, C# runtime-generated UI, standalone Mono rule/geometry checks, actual player EventSystem checks and render captures.

User already delegated layout decisions and autonomous implementation. No additional approval is needed. Preserve existing unrelated checkout changes; no commit or scene cleanup.

## Implementation

- [x] Add `Core/UpgradeTreeLayout.cs`: six tabs, exact-once coverage of the 30 nodes, compact three-row layouts. All same-tab prerequisite arrows point down and do not cross or overlap unrelated node/caption/requirement bounds. Test the actual prerequisite topology and positions in `Tools/test-upgrade-tree-layout.ps1`.
- [x] Update the existing progression workspace with six tabs, tab-local layout, short downward arrows, and direct cross-tab prerequisite links. Keep zoom/pan/fit controls. Show unmet versus met requirement state without adding prose. Clicking a requirement activates its tab and highlights the target without spending money. Hover lists all parents and their status, not just the missing ones.
- [x] Update `ProgressionRuntimeSmoke.cs`: real pointer navigation through all tabs, cross-tab prerequisite navigation without purchase, zoom/pan/fit, locked nodes, purchase state refresh, existing operating/save loop, and both 1600×900/1280×720 captures.
- [x] Independently review layout/code and actual captures. Run progression rule checks, geometry checks, Editor checks and actual player scenario. Build the updated Release player and record verification.

## Tab membership

1. 가게 운영: hours, patience, shelf, ads, repeat_ads.
2. 솜사탕 제작: stick_speed, stick_saving, stick_quality, sugar_2, sugar_3, sugar_saving, quality_focus.
3. 기계와 맛: machine_2, machine_3, flavor_soda, flavor_vanilla.
4. 알바: worker_1, worker_2, worker_grade_2, worker_grade_3, worker_speed.
5. 장소와 판매: sales, flavor_price, location_price, location_1, location_2, location_3.
6. 카트: engine, handling, coupe.

## Acceptance

- Every prerequisite is represented exactly once for its child: an in-tab arrow or a cross-tab requirement button.
- Zero line crossings, zero collinear overlaps, no line crossing an unrelated node or its text.
- Required purchases remain unchanged and all parents still require rank 1. References show their tab/name, attained state, and navigate to the actual purchase node.
- Independent categories use separate tabs. No all-node overview retaining the original crossing mesh.
- Right NPC panel and other game screens retain their existing layout.

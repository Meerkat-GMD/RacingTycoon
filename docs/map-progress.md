# Map recipe iteration

Plan: docs/superpowers/plans/2026-09-21-map-recipes.md
Baseline c20bf19, user approved implementation.

- Task 1 recipe/economy/save: completed b7e17ea; recipes17, core22, driving17, orders14 passed.
- Task 2 two longer maps: completed 4142f54; map tests8 passed, clean baseline laps46.02/65.72seconds.
- Task 3 Blender landmarks: completed6473ae3; source+FBX validation passed, preview inspected.
- Task 4 runtime/UI: both maps and three configurable flavors, one-lap completion, fixed sizes60/120g, quality, bonus, manual sales, two course roots and shop relocation integrated.
- Independent review found order-highlight/recipe mismatch; fixed by preserving stock selection. Reviewer verified closure.
- Fresh parent verification: core22 + driving17 + orders14 + recipes17 + maps8, Blender source/FBX validator, Unity50 and Windows runtime131 all passed.
- Final screenshots inspected; four aspect/resolution configurations checked for buttons and text overflow.
- Release BuildFolderMapRecipes completed exit0, COTTON_RELEASE_SUCCESS102916121. Existing old player PID34172 was left running; close it before launching latest release because save path is shared.
- README, verification report, screenshots and review report updated. Parent integration commit follows.

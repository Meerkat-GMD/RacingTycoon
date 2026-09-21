# Map recipe core report

Implemented Task 1 from the approved 2026-09-21 plan.

- `RaceRecipe` defines maps 0/1 as 60/120g, timeout 180/240s and par 50/75s, with neutral Korean map names.
- `GameSession.StartRecipe` fixes the configured map and flavor. `TickRecipe` uses progress-based winding and finishes only when its authoritative forward-lap count reaches one before the timeout. Filling the target alone keeps Racing. Completion fills any missing samples, stocks once, assigns quality and credits one bounded bonus. `FinishRun` aborts recipes without stock or payment.
- Quality is `clamp(50 + 5*clamp(boosts,0,10) - 10*max(hits,0) + 5*clamp(tankLevel,0,3),0,100)`. Completion bonus is rounded `min(1,par/elapsed)*20/40`. No sales occur automatically.
- Product quality adds up to 30% to the existing pricing calculation; quality zero preserves the existing price. Customer patience is 300 seconds.
- Save version 3 validates quality. V2 validates its original 120-second waiting range before scaling remaining time by 2.5, preserving waiting ratios and all existing economy progress. V1 retains its existing migration behavior. V1/V2 stock receives quality zero. Unknown or invalid saves remain rejected.
- Legacy `StartRun`/`Tick` timed and target-full semantics remain available. Each tick API ignores runs owned by the other mode.

## Verification

Observed the nine initial recipe behavior tests fail against the real legacy behavior, then pass after implementation. Observed four save/quality migration tests fail, then pass after implementation. The temporary baseline bridge was removed from the final tests.

Final commands:

- `Tools/test-recipes.ps1`: 17 passed, 0 failed. Includes all six configurations, target-full-before-lap, top-up samples, duplicate completion, abort, timeout, reverse lap count, invalid inputs, stock guards, pause, mode isolation, quality/bonus bounds, legacy price, customer satisfaction and V1/V2/V3 migration validation.
- `Tools/test-core.ps1`: Core 22/22, Driving 17/17, Orders 14/14 passed.
- `git diff --check`: no whitespace errors.

Recipe tests compile `SaveStore` with Unity managed references but execute only its pure migration/validation path, without launching Unity. Parent integration checks cover actual JSON serialization/loading and player wiring. No Unity or game process was launched for this task.

# Manual boost and sustained acceleration implementation

> Execute in the existing feature checkout, using bounded subagent implementation for the independent acceleration calculation and a separate code review. Parent owns input, booster inventory and runtime integration.

**Goal:** Give kart players a deliberate booster button and let Downhill drivers keep gaining speed on sustained throttle.

**Architecture:** `DriveAcceleration.Advance(double speed, double throttle, bool brake, DrivingStyle style, double maximumSpeed, int boostTier, double dt)` owns longitudinal dynamics. `ArcadeDrive` owns collision, slide and booster stock. Input travels through `GameController.Tick(..., bool boost = false)` and `KartController.Drive(..., bool boost = false)` into `ArcadeDrive.Step(..., bool boost = false)` once per outer tick.

**Stack:** Unity 6000.5, C#, existing Mono command-line tests and Windows smoke player.

- [x] Dynamics: write failing pure acceleration behavior tests; implement the calculation; preserve the Kart dynamics; verify full-throttle growth, coast/brake, upgrades, finite limits and timestep consistency.
- [x] Booster: add failing inventory/activation/refill tests. Add initial one/max two stock, valid Shift activation, clean-drift refill and no expiry shortening or resource loss on invalid activation. Wire the new acceleration calculation into ArcadeDrive.
- [x] Presentation/input: route one-shot Shift through Update/Tick, gate paused and non-racing states, show stock/key binding in race/help/shop UI, preserve sound/effects from boost events.
- [x] Integration: adapt the test driver to the faster Downhill car with explicit corner braking, keeping zero-wall/full-lap requirements. Add runtime checks for stored booster, pause and Downhill continuing acceleration.
- [x] Verify: core/driving/order/recipe/feel/map/dynamics tests, independent review, Unity development build and smoke player, inspect screenshots, release build to Builds/BoosterAcceleration.
- [x] Record results and deliver the new executable; parent commits this final verified state.

Complete: Core22/Driving17/Orders14/Recipes17/Feel21/Booster10/Acceleration12/Maps14 passed. Unity editor55 and development player198 passed. Independent review found no actionable issue in production or test-driver integration. Help, stored booster and Downhill screenshots inspected. Release build exited0 with a Release receipt; baseline edca400. Existing old Windows player and other Unity projects were left running.

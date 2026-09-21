# Driving feel implementation plan

> For agentic workers: use subagent-driven-development for the independent physics/course slices, then requesting-code-review for their integration. User feedback authorizes correction of the existing game; do not stop for redundant approval.

Goal: Shorter rounds with responsive acceleration and repeated satisfying corner/boost opportunities.
Architecture: Keep ArcadeDrive and RaceCourse pure. Unity presentation consumes the new boost duration/tier and steering state. Preserve recipes/saves/order economy.
Tech stack: Unity6000.5.3f1, Blender5.2 assets, Mono tests, Windows.
Spec: docs/superpowers/specs/2026-09-21-driving-feel-design.md. Workspace codex/cotton-circuit; baseline2f6ad73. No other project/process modifications.

## Task1 physics (independent agent)
Files owned: Scripts/Core/{ArcadeDrive.cs,Economy.cs}, Tools/Tests/DrivingTests.cs, Tools/Tests/FeelTests.cs(new), Tools/test-feel.ps1(new), docs/briefs/driving-feel-core.md.
- [x] RED meaningful behavior tests for reaching90% base speed within1s, two boost strengths/durations, immediate speed increase, gradual boost-expiry deceleration, no straight/stationary boost farming, braking/recovery clearing, stable timestep. Run against baseline first.
- [x] GREEN default/economy speed22 (+2.5 upgrade), acceleration~28, digital steering smoothing and controllable rates, grip recover. Preserve progress security and all existing public API.
- [x] Add public double SteeringInput; int BoostTier; double BoostDuration (active boost total) for presentation. Keep charge thresholds.32/.75, weak duration1.1 and strong1.7, speed caps+7/+11 and impulse+3/+5 are initial tuning values. No hidden auto-steer. Confirm default old driving tests with course agent; adapt only expected balance/geometry fixtures, retain assertions.
- [x] Fresh feel+core tests; report and commit only owned files. No Unity run.

## Task2 courses and landmarks (independent agent)
Files owned: Scripts/Core/RaceCourse.cs, Editor/RaceCourseBuilder.cs, Tools/Tests/MapTests.cs, docs/briefs/driving-feel-course.md.
- [x] RED test lengths520..620/640..750, baseline22 lap24..36, seam/bounds/shortcut checks. Then reduce gaps between corner anchors/handles, keep distinct layouts and road widths. Every main/shortcut waypoint remains valid; mainline22m/s clean follower; shortcuts actually traversable.
- [x] Both roads including width fit radius130; keep road away from center stage radius30. Reuse existing Blender props along driving edge for parallax, avoid geometry over road. Keep tunnel road clearance and both roots.
- [x] Preserve mapped progress, exploit guards and map indices. Report changed coordinates affecting DrivingTests to physics agent. Validate final maps with updated physics, commit owned files.

## Task3 parent presentation/integration
Files: KartController, WorldView, GameUI, AudioFeedback, ProjectBuilder, Core/RaceRecipe, RuntimeSmoke, docs.
- [x] Consume actual steering input and tier/duration. Add lightweight deterministic screen-edge speed lines; paused/shop/results inactive. Lower chase to~4.2m, distance~7.4m with widened boost FOV; use bounded smoothing, no shake.
- [x] Add wind layer and tier-sensitive boost tone, HUD two-tier feedback with true duration. Keep pause/mute lifecycle.
- [x] Resize common bowl radius140 with same Blender stage/candy, reposition peripheralprops; map sizes/flavors/saves unchanged, par30/36.
- [x] Runtime six-recipe smoke checks new24..36-ish lap timings; additional real driver input triggers boost and checks camera speed-line/mute/pause guards before abort. Preserve service/save/stock tests.
- [x] Run all core/maps/recipes/feel suites, Unity50+ checks, actual player smoke and screenshot inspection. Independent review, fix, release Builds/DrivingFeel, docs and commit.

## User steering accepted during work

Final physics targets supersede initial numbers:26m/s, Kart90%by0.8s, acceleration38, boost5/8impulse9/14cap. Map baseline26 lap19..32. RaceRecipe par24/30. User also requests bothKartRider-like andInitialD-like feel: enumDrivingStyle Kart/Downhill,Style property retainedbyReset, DriftCount/SkillCount forquality, DownhillnoautoBoost. Parent adds shopselection, style-specificHUD/camera, coupevisualswitch, bothmapssmokeunderDownhill. Core agentownsallnewstylephysics/tests. Blender coupe agentownsonlynewsource/export/validator/preview/model+meta/report. Task4car validatedandreviewedbeforebuild. No existing brandmarks orcarreplica.

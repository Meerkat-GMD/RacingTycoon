# Racing feel iteration

Approved by the user: direct arcade steering, drift release boosts, an asymmetric circuit inside the cotton-candy machine, a following camera and driving feedback. Preserve production → sale → upgrades. Ghosts and opponents are deferred.

## Constraints
- Existing Unity 6000.5.3f1 project and branch; Windows standalone, keyboard, Korean UI.
- W/up accelerate; S/down brake; A/D steer; Space hold to drift, release to boost; R recover; Esc pause.
- Driving controls position and heading freely. Track boundaries slow and slide the kart; no automatic orbit or automatic steering.
- Main circuit with straight, S bend and long corner, plus a narrow optional inside shortcut. Geometry and collisions share the same course definition.
- Award production only for new forward course progress. Reversing, resetting or oscillating must not farm cotton. Boost earns 25% more cotton per progress, capped by the existing tank.
- Preserve save schema, economy and existing cotton samples. Flavored track sectors determine color; lateral position maps to the existing winding radius 7.5–12.5.
- Blender source and exported props remain editable. No new AI racers.

## Tasks
1. Pure C# driving/course model and regression tests. Arc-length course sampling, connected shortcut, free position/velocity, steer/drift/boost, collision, recovery and monotonic rewarded progress.
2. Blender race props: directional chevron board, sugar barrier segment, shortcut gate. Export FBX and editable source, validate scale and orientation.
3. Integrate Unity controller, course rendering, chase camera, engine/skid/boost audio, lean/trails, race HUD/minimap and clear controls. Keep shop/result usable.
4. Extend integration and runtime smoke to drive the real new model, verify drift/boost, wall, recovery, pause, production and economic loop; inspect screenshots at several aspects.
5. Independent code review, fixes, development smoke and final Windows release. Update README and verification evidence.

## Shared core API contract
Namespace CottonCircuit. `RoadPoint` has public double X,Z; length/operator helpers optional.
`CourseSample`: Position, Tangent (RoadPoint), Progress, HalfWidth, Lateral (double), IsShortcut (bool).
`RaceCourse`: static readonly Shared; Length; MainPoints (RoadPoint[] closed implicit), ShortcutPoints (RoadPoint[] open); MainHalfWidth=4.8, ShortcutHalfWidth=2.2; Sample(double progress); Project(RoadPoint position) chooses closest drivable ribbon accounting for width.
`ArcadeDrive`: Course; Position; Heading (radians, 0=+Z, positive right); Speed; Velocity; MaximumSpeed=18; DriftCharge 0..1; BoostRemaining; IsDrifting; BoostCount; WallHits; Laps; BestLapSeconds (0 before first); LapSeconds; TotalProgress; LastRewardDistance; Sample (CourseSample).
Methods Reset() for a new run, Recover() nearest-course reset without reward or lost prior stats, Stop(), Step(double throttle,double steering,bool brake,bool drift,double dt). Heading uses atan2(tangent.X,tangent.Z). MaximumSpeed upgrade handled by Unity.
Step cannot award reverse/repeated progress; LastRewardDistance is new positive progress this step. Boost requires a sustained turn at speed, cancels on brake/wall/recovery, releases once on drift-key release. Keep inputs finite and substep dt <=.02 internally.

Course suggested anchors: (0,-25),(24,-25),(34,-14),(29,1),(35,15),(24,27),(7,30),(-5,21),(-22,24),(-33,12),(-31,-7),(-22,-24), smoothed loop. Shortcut connects the inward side of anchor 3 to anchor 5. Keep course within radius 44 so a radius 46 machine rim contains it.

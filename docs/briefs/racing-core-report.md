# Racing core implementation report

## Public API

All types are in `CottonCircuit` and use only `System`.

```csharp
public struct RoadPoint {
    public double X, Z;
    public RoadPoint(double x, double z);
    // +, -, and multiplication by double are available.
}
public struct CourseSample {
    public RoadPoint Position, Tangent;
    public double Progress, HalfWidth, Lateral;
    public bool IsShortcut;
}
public sealed class RaceCourse {
    public const double MainHalfWidth = 4.8, ShortcutHalfWidth = 2.2;
    public static readonly RaceCourse Shared;
    public readonly RoadPoint[] MainPoints, ShortcutPoints;
    public double Length { get; }
    public CourseSample Sample(double progress);
    public CourseSample Project(RoadPoint position);
}
public sealed class ArcadeDrive {
    public ArcadeDrive();
    public ArcadeDrive(RaceCourse course);
    public RaceCourse Course { get; }
    public RoadPoint Position { get; }
    public RoadPoint Velocity { get; }
    public double Heading { get; }
    public double Speed { get; }
    public double MaximumSpeed { get; set; }
    public double DriftCharge { get; }
    public double BoostRemaining { get; }
    public bool IsDrifting { get; }
    public int BoostCount { get; }
    public int WallHits { get; }
    public int Laps { get; }
    public double BestLapSeconds { get; }
    public double LapSeconds { get; }
    public double TotalProgress { get; }
    public double LastRewardDistance { get; }
    public double LastBoostedRewardDistance { get; };
    public CourseSample Sample { get; }
    public void Reset();
    public void Recover();
    public void Stop();
    public void Step(double throttle, double steering, bool brake, bool drift, double dt);
}
```

`RoadPoint` uses world X/Z meters. Heading is radians, zero toward +Z and positive toward +X. `MainPoints` is a sampled closed loop without a repeated end point; connect its last point to its first. `ShortcutPoints` is an open sampled path whose endpoints meet the main loop. `Length` is about 216.34 m. `Sample` wraps progress around the main loop. `Project` selects the closest drivable ribbon using both ribbon width and centerline distance. `CourseSample.Position` is the projected centerline point, `Tangent` is unit length, and `Lateral` is positive to its right.

`Step` clamps throttle to 0..1 and steering to -1..1, rejects nonfinite numeric inputs, and integrates in steps no longer than 0.02 s. Braking, a wall hit, `Stop`, and `Recover` cancel boost. A sustained steering drift at speed charges it; releasing the drift key triggers one 0.9 s boost. The kart keeps 0.8 m center clearance from road edges and slides with reduced velocity along a contacted wall. Wall impact does not rotate heading to the road tangent. `Stop` also clears the current reward pulse. `Recover` places the kart on the nearest centerline, stops it, and keeps run statistics and earned progress. `Reset` starts a new run, retaining `MaximumSpeed` so an applied upgrade persists.

`LastRewardDistance` is new forward course distance earned in the most recent `Step`, before boost weighting. `LastBoostedRewardDistance` is the subset earned during active boost, including frames where boost ends partway through. Both reset to zero each call. `TotalProgress` is cumulative eligible distance; a high-water mark prevents reversing, route switches, recovery, and oscillation from paying for repeated road. To preserve the existing production format, pass `2 * Math.PI * (LastRewardDistance + 0.25 * LastBoostedRewardDistance) / Course.Length` as the angle to `Production.Advance`, along with the chosen winding radius and flavor. `Laps` and lap times advance from uniquely rewarded progress.

## Verification

`pwsh -NoProfile -File Tools/test-core.ps1` compiles and runs both executables with the Unity bundled mcs/Mono: 22 existing core tests and 15 driving tests passed. The driving tests cover loop closure and rim bounds, shortcut projection, direct steering, acceleration/braking, drift release and boost accounting, wall clearance and sliding, steer-away and recovery, lap completion using a test-only course follower, backward travel without repeat rewards, stop pulse clearing, invalid inputs, and frame subdivision. Unity runtime and scene verification belong to the integration task.

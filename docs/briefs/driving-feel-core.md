# Driving feel core

The corrective pass prioritizes fast acceleration and a 26 m/s base kart, with two selectable styles for comparison. Both styles use the same course, progression guards, top speed and +2.5 m/s engine upgrades.

## Driving styles

`ArcadeDrive.Style` selects `DrivingStyle.Kart` (default) or `DrivingStyle.Downhill`. Select it before starting the race; `Reset()` preserves the selection and clears race statistics.

- Kart: 0.085 s steering response, normal grip 13, slide grip 2.7. Charged drift release delivers the tiered boost below. Braking cancels charge and boost.
- Downhill: 0.14 s steering response, normal grip 7.5, slide grip 2.2. Space initiates a slide; braking while turning also initiates one. Braking scrubs 25 m/s per second, allowing controlled corner entry. Releasing a slide with at least 0.32 charge while still moving at least 6 m/s increments `DriftCount`, with no boost or boosted production.

Both accelerate at 38 m/s² before rolling drag and reach at least 90% of base speed within 0.8 s. Kart braking removes 40 m/s per second. Steering is manually controlled, with a speed-dependent reduction in turn rate. Exponential wheel and velocity response reduces changes caused by caller frame rate.

## Kart boost

| Charge at release | Tier | Duration | Immediate speed impulse | Speed cap above normal |
| --- | --- | --- | --- | --- |
| 0.32 to below 0.75 | 1 | 1.1 s | +5 m/s | +9 m/s |
| 0.75 or more | 2 | 1.7 s | +8 m/s | +14 m/s |

An impulse cannot stack above that boost's speed cap. Above-cap speed after boost expiry decays at 9 m/s per second rather than snapping to the normal maximum. Boosted acceleration is 44 m/s² before drag.

Charge requires at least 6 m/s and applied steering magnitude of at least 0.22. Holding Space while stationary or travelling straight cannot charge. Walls, recovery and stopping clear active boost and drift state. Braking cancels Kart boost immediately and suppresses boosted production.

## Presentation state

- `SteeringInput`: smoothed steering actually applied to the kart.
- `BoostTier`: 0 when inactive, otherwise 1 or 2.
- `BoostDuration`: active boost's full duration; 0 when inactive.
- `BoostRemaining`: remaining active time, reduced by the current integration step after release.
- `BoostCount`: actual Kart boosts only.
- `DriftCount`: completed Downhill slides only.
- `SkillCount`: the appropriate count for the selected style.

`Stop()` and `Recover()` preserve earned progress and counts while clearing steering, drift, boost and reward pulses. `Reset()` also clears counts. Existing projection plausibility, visited-road high-water marks, seam-based laps and duplicate-reward protections remain in place.

## Validation

`Tools/test-feel.ps1` compiles the pure core with Unity Mono without starting Unity. The baseline produced the expected launch, steering and boost failures before implementation; adding style tests then exposed the missing selectable behavior. A later repeated-boost test reproduced and prevented an uncapped impulse.

The 21-test feel suite covers launch, engine upgrade speed, keyboard ramp/reversal, both boost tiers, immediate impulses, smooth expiry, boost stacking, cancellation, stationary/straight farming, frame partitioning, Downhill inertia, brake entry, slide completion and reset. It also rejects a slide released into a wall on the completion step. Existing driving tests retain wall clearance, recovery, reverse-motion, shortcut and lap assertions; only shorter-course geometry fixtures and drift hold duration were updated.

Fresh verification: feel 21/21, core 22/22, driving 17/17, orders 14/14 and maps 12/12. Both styles completed clean laps and shortcut routes; Kart/Downhill map-one times were 22.28/22.50 s and map-two times were 26.44/26.66 s with the test follower.

For an actual input smoke check at default speed, start with 0.4 s full throttle and zero steering. Then coast while holding drift, alternating steering +0.65 / -0.65 every 0.4 s until charge reaches 0.8. Release drift with full throttle. This produced a strong Kart boost and a completed Downhill slide without wall hits on the shortened starting straight. Release Downhill earlier at 0.4 charge for a shorter smoke check.

Human preference and game feel still require comparison in the player; numerical checks establish the mechanics and preserve progression safeguards.

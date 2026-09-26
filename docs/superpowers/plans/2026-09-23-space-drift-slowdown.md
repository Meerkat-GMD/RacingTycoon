# Space Drift Slowdown Implementation Plan

**Goal:** In the default Initial D / Downhill mode, holding Space should reduce speed even with W held, and releasing Space should restore normal acceleration.

**Cause:** Space changes steering and grip in ArcadeDrive, but DriveAcceleration currently receives no drift input and continues applying full engine pull.

**Design:** Explicit Space input uses progressive deceleration of `4 + 0.18 * speed` m/s². Regular braking keeps priority and is stronger. Speed is clamped at zero; holding Space at rest cannot launch the vehicle. Reduce the actual velocity vector along with scalar speed, retaining its direction, so physical travel and production slow too. Releasing Space resumes the normal acceleration path. Brake-induced slides already decelerate through S and must not receive an extra Space penalty. Kart mode keeps its existing boost mechanics.

**Tech stack:** Unity 6000.5.3f1, pure C# driving model, Windows development/release player.

- [x] Reproduce with model tests: full throttle plus Space must lose speed and actual motion without wall contact; release must resume acceleration; stop/low speed must remain finite; frame sizes must agree.
- [x] Add optional drift input to DriveAcceleration and wire it through ArcadeDrive, including real velocity deceleration. Keep normal driving and braking regressions passing.
- [x] Update manual/help text and add player checks for visible slowing and release behavior alongside the current shop scenarios.
- [x] Run driving/acceleration regressions, development-player verification, inspect the drift capture, then build Release to Builds/DriftSlowdown and update docs.

The user has explicitly requested implementation; the slowdown behavior was announced before editing.

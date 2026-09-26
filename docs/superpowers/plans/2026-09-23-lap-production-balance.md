# Lap Production Balance Implementation Plan

**Goal:** Apply the user's revised A/B/C production thresholds of 1/1.5/2 laps and increase sugar consumption.

**Design:** The shop currently uses course 0. Derive one production lap from `RaceCourse.Shared.Length`; retain eligible forward meters in the save and divide by that length for display. Sugarless travel, reversing, and recovery still do not grow a product. Consume 50g per production lap; each shake still supplies 10g and the tank holds 100g. Existing saved meters remain intact and use the revised tier thresholds. Legacy gram-based stock migrates to A/B as before. Day duration remains 600 seconds.

**Architecture:** Centralize lap length and tier distance boundaries in ShopShift. All sales, prices, UI progress, and legacy stock migration use those boundaries. Continue the existing in-place work without discarding unrelated changes.

**Tech stack:** Unity 6000.5.3f1, C#, uGUI, Mono behavioral checks, Windows player verification.

- [x] Core: update ShopShiftTests first and observe failures for one-shake exhaustion, exact lap boundaries, full-tank C production, prices, and legacy stock migration. Then implement `LapMeters`, `MetersForSize(int)`, and `SugarPerMeter = 50 / LapMeters` in ShopShift.
- [x] UI: update ShiftUI, GameUI help, and ShiftController messages to laps. Progress must cover 0→1, 1→1.5, and 1.5→2 intervals. Keep the cotton visual growing across tier changes.
- [x] Integration: update ShopShiftRuntimeSmoke and ShopShiftSaveChecks fixtures, verify actual driving at A/B/C boundaries and sugar use, and retain save round trips and drag interactions.
- [x] Delivery: run the model and affected legacy regression tests; build and run a development player, inspect captures, then build Release in Builds/LapBalance and update documentation.

The user explicitly requested the adjustment and implementation. Sugar consumption is the tuning default announced before changes; no additional approval is required.

# Wider Roads and Initial D Default Implementation Plan

**Goal:** Widen the roads and start the current shop mode with the existing Initial D / Downhill driving behavior and sports coupe.

**Design:** Main road width changes from 9.6m to 14.4m, shortcut width from 4.4m to 6.6m, on both maps. Centerlines and lap distances stay the same. Shared course widths control collision and generated visuals. Widen crossing landmarks and stripes too, and preserve roadside clearance. Default shop initialization uses Downhill; automatic driving stays enabled and can be switched to manual. Legacy explicit modes retain their own behavior.

**Architecture:** RaceCourse owns widths, RaceCourseBuilder regenerates roads and surrounding scene geometry. GameController chooses the default style; ShiftController applies it before resetting driving state. ShiftUI and help display the relevant controls. Regenerated road meshes and the scene are intentional deliverables and must be retained.

**Tech stack:** Unity 6000.5.3f1, C#, uGUI, Mono behavior tests, Windows development/release players.

- [x] Road behavior: first verify that a formerly out-of-bounds lateral lane should be usable, and that driving beyond the expanded boundary still collides. Update widths and map landmark generation. Exercise both maps and both driving styles.
- [x] Default mode: initialize Downhill and its coupe, reset after style selection, and show Initial D/manual controls without kart booster guidance.
- [x] Integration: assert default and reload style, visible vehicle, road mesh widths, manual continuous acceleration and no kart boost. Keep sugar/lap production and shop scenarios passing.
- [x] Delivery: build in Builds/WideRoadDownhill, run the appropriate core and player checks, inspect the rendered roads and UI, build Release, and update README/verification artifacts.

The user explicitly requested these changes. The 1.5x width is the tuning choice announced before implementation.

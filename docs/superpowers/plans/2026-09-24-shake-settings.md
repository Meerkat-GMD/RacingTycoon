# Adjustable sugar shake width

User requests direct tuning of sugar shake width. Prior delegation authorizes design choices and continuous implementation.

Design: add a setting in the existing pause/settings menu, available through Esc or the renamed Settings button. Slider and integer input select the vertical stroke required for10g:24–600 reference pixels, default132. Larger values pour less for the same gesture. Keep the12px dead zone and10g maximum. Provide a defaults button and hover explanation; avoid permanent explanatory paragraphs. Persist separately from game progression in the existing save directory, so new shops preserve input preferences and runtime smoke saves stay isolated.

Interfaces: SugarShake.FullStrokePixels property; DefaultFullStrokePixels/MinFullStrokePixels/MaxFullStrokePixels constants; NormalizeFullStrokePixels(double). SugarInputSettingsStore(directory).Load() returns double; Save(double) returns bool. Controller exposes SugarShakeFullStrokePixels, SetSugarShakeFullStrokePixels(double). UI BuildSugarShakeSettings(RectTransform,float) creates section, RefreshSugarShakeSettings syncs without replacing focused input. Stable objects SugarShakeWidthSlider, SugarShakeWidthInput, SugarShakeWidthReset.

- [x] Core: parameterize shake amount, reset unfinished stroke on changes, sanitize invalid/range values; add meaningful core and isolated file persistence tests.
- [x] UI: slider + numeric input + reset in pause, proportional preview mark/short10g label, hover details. Show only in sugar-shift mode. Settings button discoverable, existing pause controls still fit1600/1280.
- [x] Integration: controller load/save, sync gesture before processing, reset drag on setting change. No progress save schema change. Ensure saving failure has feedback.
- [x] Validation: actual UI slider/input/reset interactions and gameplay pour with adjusted value, reload persistence, no currency changes from settings; regression tests, screenshots, independent review, Development and Release builds.


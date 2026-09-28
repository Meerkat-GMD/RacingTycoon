# HUD cleanup implementation plan

Goal: apply the user's annotated screenshot directly: remove seven marked HUD elements and show the current cotton candy stage instead of accumulated laps.

Architecture: edit the authored UI Toolkit UXML/USS and remove obsolete C# bindings. Reuse `ShopShift.SizeForDistance(BatchMeters)` and the existing Korean tier names. Keep the next-stage distance, gameplay rules, Esc pause menu and title return.

The annotated image explicitly authorizes the design. No new visuals or UI framework changes are needed.

- [x] Remove driver badge, pause button, size-limit note, street customer count, shelf delivery tip, shelf detail line and driving controls footer from Business.uxml. Remove their USS rules and C# bindings.
- [x] Rename the big production label to businessBatchStage and bind 미완성 / 소 / 중 / 대. Keep the next-size distance and progress bar. Avoid duplicating the stage in the smaller flavor line.
- [x] Adjust existing runtime checks for the removed pause button. Verify the stage boundaries, capped overflow, removed elements, next-stage guidance and remaining pause/title flow using the development player.
- [x] Inspect actual captures, build the release and record validation.

HUD checks: 82 passed at each of 1600×900 and 1280×720. Full gameplay: 440 passed. Development and release builds succeeded. See `docs/hud-cleanup-verification.md`.

# Split race and shop review

2026-09-22. Reviewed continuous controller/session, automatic driving, split UI and their boundaries against the split-race-shop design. Camera implementation was inspected by the primary agent and exercised by runtime viewport checks.

Independent review found one actionable issue: the lap percentage used rewarded distance, which excludes shortcut projection jumps. The display now uses `GameController.RunProgress`, based on the current course sample, so the displayed lap fraction follows the finish line.

Scoped re-review confirmed the lap-progress fix and the manual drift/boost meter's charge, duration and tier colors. No regression was found in those changes.

Existing production, sales, inventory limits, pause, save and queued recipe behavior were reviewed. The real player additionally exercises those transitions. No additional actionable findings were reported in the independent review.

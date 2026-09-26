# Downhill HUD and proportional sugar input

User reports booster UI in the booster-free downhill mode and excessive sugar from tiny shakes.

- [x] Remove the entire booster card in every downhill HUD path. Preserve kart UI. Make pause help and auto-drive hover style-aware so they cannot advertise a nonexistent downhill booster. Verify automatic/manual state and switching both ways.
- [x] Replace the fixed 10g gesture with a stroke-amplitude amount: ignore 12px jitter, 24px gives1g,60px gives4g,132px gives10g, cap10g. Confirm a vertical direction reversal before pouring; measure full stroke amplitude rather than individual event deltas. Normalize with existing reference-canvas coordinates. Larger strokes pour more; more repeated strokes pour more per second. Leaving the race region or cancelling clears pending gesture state.
- [x] Pass the measured grams through UI → controller → ShopShift → machine pour. Retain default10g for worker/system calls, existing caps/flavor/grade locks, and charge premium sugar only on accepted quantity using existing per-pour rounding. Basic strawberry remains free. Reject zero/nonfinite quantities.
- [x] Add isolated gesture tests and production-quantity tests, adapt physical strong-shake fixtures without bypassing input handlers, verify tiny/medium/strong gestures at both UI sizes and the downhill/kart HUD transitions. Build, run regression scenarios, inspect screenshots and update the Release executable.

Prior user delegation covers tuning decisions and continuous implementation. This changes input sensitivity; candy still grows from driving distance using the amount of sugar actually inserted. No save schema change is required.


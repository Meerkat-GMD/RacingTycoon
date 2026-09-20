# Customer-order iteration
Base fd9688a; plan docs/superpowers/plans/2026-09-21-customer-orders.md. User approved order-first service and one-map flavor selection.
- Core orders: c2a9af9; 22 original core +17 driving +14 order checks pass.
- Blender furniture: 9aeb9e5; source/FBX round trip validated; parent inspected the preview.
- Runtime: manual stock selection/serve, 2-customer queue, 120sec patience, satisfaction/tips, shelf6/9/12, 30sec targeted runs connected.
- Collection: colored inner lane +neutral bypass, 5x previous production rate +15% per tank upgrade; 9 collection checks pass.
- Save: ef39af3; V1 migration +V2 validation; Unity editor 43 checks passed.
- Review: independent reviewer found no material issues (docs/briefs/orders-review.md).
- Verification: six recipes via real kart steering, manual button serving, pause/results freeze, expiry, shelf expansion, discard, and save reload all passed in Windows player (93 checks). Rendered screenshots inspected; no button clipping/text overflow.
- Delivery: Builds/OrderPreview/CottonCircuit.exe, Release receipt, COTTON_RELEASE_SUCCESS 97050845, exit 0. README and docs/verification.md updated; required work complete.
- Existing user-run Builds/Windows/CottonCircuit.exe remains open; new build is isolated in Builds/OrderPreview.

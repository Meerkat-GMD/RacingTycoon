# Manual boost and sustained acceleration

User correction: the kart needs a booster; Initial-D-inspired driving must continue gaining speed while the accelerator is held. This is an authorized iteration of the existing two-style comparison, in the existing Unity checkout and feature branch.

- Kart: add a Shift-triggered stored booster alongside the existing drift-release mini/super boost. Start with one, hold at most two, refill one after a clean charged drift. Stored boost lasts 2.5 seconds, adds at most 14 m/s to base speed, and is consumed only on a valid activation. Brake, wall, recovery and leaving a run cancel active effects. Pause/shop/results reject activation. A repeated or held input must not stack speed or spend extra stock. Existing drift boosts never shorten a stored boost.
- User confirmed the recharge choice during implementation: one starting booster, refill by drifting. Capacity two is a tuning choice.
- Downhill: replace the early 26 m/s plateau with a smooth longitudinal acceleration curve. Full throttle continues increasing speed over a 20-second clear-road interval, with diminishing acceleration toward a finite high-speed ceiling (base speed + 34 m/s). Coasting and braking slow the car; engine upgrades improve acceleration/top speed. No booster in this style. Faster corner approaches require driver braking; no production automatic steering/braking.
- Reuse the existing Blender kart/coupe. Update visible controls, stock feedback, sound and speed effects as needed. Keep recipes, course geometry, manual selling and save format unchanged.
- Verify pure dynamics, resource consumption/refill/cancellation, both-map manual-control simulation, actual player lifecycle/UI/save checks, then build a release executable in a new folder so an open previous build stays untouched.

Implementation defaults are reversible tuning decisions. No new approval gate is needed for this requested correction.

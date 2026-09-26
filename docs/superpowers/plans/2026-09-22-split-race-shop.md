# Split Race and Shop Implementation Plan

> **For agentic workers:** Implement the bounded tasks below in the current session, with independent file ownership and a final review.

**Goal:** Keep a car racing on the left while the player sells candy on the right.

**Architecture:** Add an opt-in legacy initialization argument while making continuous play the default. Separate continuous controller flow, world viewports, and split UI construction, sharing the existing economy and save system.

**Tech Stack:** Unity 6000.5.3f1, C#, uGUI, PowerShell verification scripts.

## Global Constraints

- Reuse the existing two maps, two driving styles, products, economy and save format.
- Default automatic driving; support a visible manual-driving toggle.
- Keep both panels visible, queue recipe changes until the next cycle, and pause both systems together.
- Preserve speed/position across same-course laps; no reward duplication or inventory overflow.
- Do not overwrite user saves in verification.

### Task 1: Continuous production and driving

- [x] Add behavior tests for an automatic driver on both courses/styles.
- [x] Implement `Initialize(string saveDirectory, bool continuous = true)`, `ContinuousMode`, `AutoDrive`, `RaceVisible`, and `ToggleAutoDrive()` in GameController.
- [x] Start/repeat recipes, allow sales during driving, and keep vehicle motion during full-shelf production waits.
- [x] Run core/driver regression suites.

### Task 2: World views

- [x] Implement `WorldView.SetContinuousMode(bool enabled)` and `ShopCamera`.
- [x] Render the car at design rect (0,92,960,808), shop at (980,82,596,126).
- [x] Keep customer queue/departure animations active during continuous racing.

### Task 3: Persistent UI

- [x] Add split chrome, race HUD and right-side shop with orders, inventory, production settings and upgrades.
- [x] Keep existing manual controls and explicit auto/manual toggle.
- [x] Display queued selections, inventory-full waiting and recent completion/sale notices.

### Task 4: Runtime verification and delivery

- [x] Preserve legacy tests with noncontinuous initialization and add continuous scenarios.
- [x] Build a development player and run the real runtime checks, including screenshots/responsive bounds.
- [x] Review changes, resolve findings and inspect captured screens.
- [x] Produce the Windows release build, update README and verification notes, and provide its path.

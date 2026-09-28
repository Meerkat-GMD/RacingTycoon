# Driving control and tutorial visibility fixes

> **Superseded driving policy (2026-09-28):** The user subsequently removed player automatic driving entirely. Only hired, assigned workers operate their own machines automatically, including while selected. The takeover/button requirements below describe the earlier implementation; the movement-based coach visibility requirement remains. Current evidence: [worker driving verification](../../worker-driving-verification.md).

> **For agentic workers:** Use subagent-driven-development for test and overlay work, with controller integration and builds in the parent task.

**Goal:** Manual driving takes control from automatic driving, and the tutorial coach stops obstructing the road after the player starts driving.

**Architecture:** Resolve manual takeover once in GameController.Tick before either automatic-input override. Reuse the existing manual-toggle behavior. In TutorialOverlayUI, latch actual movement during Drive and hide only the coach and guide until the next tutorial stage.

**Tech Stack:** Unity 6000.5.3f1, C#, existing uGUI, Windows standalone runtime smoke tests.

## Constraints

- User explicitly requested both fixes; keep the existing tutorial, skip, saves and one-time settlement message.
- Acceleration, steering, braking, drift and boost count as manual driving. Neutral frames do not cancel auto or re-enable it after takeover. Paused or unavailable driving must not change the mode.
- Show instructions before departure, hide the coach after actual motion, retain hiding through brief stops and pause, and restore instructions at Extract.
- Keep the top-right Skip and existing header auto/manual button usable.
- Preserve unrelated workspace changes. Use the existing batch build workflow and isolated smoke-test saves.

## Tasks

- [x] Trace automatic input overrides in StepShift and legacy Step; confirm the overlay ignores movement.
- [x] Add independent drive-control and drive-view runtime cases in TutorialRuntimeSmoke.cs / verify-tutorial.ps1.
- [x] Build baseline and observe the expected failures in both cases (Driving-red-control: check 55; Driving-red-view: check 72).
- [x] Add the guarded takeover before substeps in GameController.Tick.
- [x] Add movement latch and coach/guide visibility in TutorialOverlayUI.Refresh.
- [x] Run focused cases, tutorial flow and first-day hint, and existing driving/progression regression checks.
- [x] Inspect screenshots, review the code, update documentation and rebuild Tutorial-Release.




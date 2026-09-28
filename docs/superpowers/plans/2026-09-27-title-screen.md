# Cotton Circuit title screen implementation plan

**Goal:** Use the approved title background as the actual first screen, with Korean title, start/continue, confirmed new game and quit.

**Architecture:** Keep the existing scene and defer GameController.Initialize until the user enters the game. A separate uGUI title canvas owns presentation and menu interaction. Explicit Initialize remains available to existing smoke tests. The title checks save existence without loading or modifying saves.

**Tech Stack:** Unity 6000.5.3f1, existing uGUI and Korean dynamic font, existing SaveStore and GameAssets.

## Constraints

- Use Assets/CottonCircuit/Sprites/Title/TitleBackground.png without altering it.
- Match the pastel cream/pink/soda palette, retain the racing car and kiosk visibility, reserve the left area for menu.
- Fit artwork proportionally to cover the viewport; keep menu readable at 1600×900, 1280×720, 1280×960 and 1920×820.
- Do not create or advance a session while the title is open, or load/migrate/recover files before entry.
- Existing saves have Continue and a separate New Game confirmation. Archive only after confirmation; cancel changes nothing.
- Preserve current uncommitted project work. Work in the current checkout, which holds the approved art and latest game changes.

## Tasks

- [x] Add isolated development-player tests for title idle, first start, continuation, confirmation/cancel, archive, corrupt saves, pointer hits and resizing.
- [x] Add TitleScreenUI and minimal GameController startup integration; wire title sprite through GameAssets/SpriteCatalog.
- [x] Build with Tools/build.ps1, then run title cases at target dimensions and existing progression/gameplay smoke checks.
- [x] Inspect actual screenshots, review changes and fix material issues; record results and playable build path.

Results: title 318 checks, progression 434 checks, save regression 20 checks, editor 101 checks. Development and release builds succeeded. Review caught lost keyboard focus after empty-background clicks; fixed and covered in title/modal tests. Screenshots and reproduction commands are in docs/title-screen-verification.md. Release executable: Builds/TitleScreen-Release/CottonCircuit.exe.

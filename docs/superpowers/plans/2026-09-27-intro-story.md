# New-game opening story implementation plan

**Goal:** Show the approved five illustrated scenes when starting a new game, with dialogue navigation and a top-right Skip button, then enter normal preparation.

**Architecture:** A separate uGUI IntroStoryUI plays six dialogue pages over five images. TitleScreenUI decides between immediate continuation and the new-game story; new-game archiving and GameController.Initialize happen only after completion or skipping. The session stays null during the story, preserving existing saves if the player closes it. Keep explicit Initialize behavior for all existing game tests.

**Tech Stack:** Unity 6000.5.3f1, existing uGUI/Korean font/InputManager Submit, GameAssets/SpriteCatalog.

## Accepted content and behavior

- Use Intro_01_Dream, Intro_02_Stopped, Intro_03_Delivery, Intro_04_Machine_v2, Intro_05_RaceAgain in that order. Scene four has Mina's explanation then the owner's reaction.
- Display brief Korean text from the agreed racer-retirement / advanced cotton-candy machine story; no automatic timeout.
- Click artwork/dialogue or Next to advance; existing Submit mapping supports Enter and Space. Top-right 스킵 ends immediately. Last page offers 가게 시작.
- Display speaker and 01 / 05 scene count. Keep Skip anchored top right and the dialogue panel bottom with responsive sizing.
- One frame of input advances at most one page. Repeated completion/skip must not initialize or archive twice.
- Existing save Continue bypasses the story. Confirmed title New Game and fresh Game Start play it. Existing in-game reset behavior is unchanged.
- Keep source artwork and unrelated working-tree changes intact.

## Tasks

- [x] Add title-to-story expectations, build and observe failure before implementing the story (`Logs/IntroStory-red/result.txt`: missing intro canvas).
- [x] Implement IntroStoryUI and minimal title/asset wiring, with archive failure returning to title safely.
- [x] Run full/skip/continue/reset/corrupt cases and existing progression regression; inspect real screenshots at 1600×900, 1280×720, 1280×960, 1920×820. Fixed the hidden-player verification capture setup and added a rendered-pixel check.
- [x] Review changes, make a Windows release build, update documentation and screenshots. See `docs/intro-story-verification.md` for results and limitations.

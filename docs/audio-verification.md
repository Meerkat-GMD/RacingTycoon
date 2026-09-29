# Game audio verification

Date: 2026-09-28. Branch `claude/game-audio`. Plan: `docs/superpowers/plans/2026-09-28-game-audio.md`.

## What plays where

| Moment | Sound | File |
|---|---|---|
| Title | Music `Title` | `Audio/Music/Title_CasualTheme1.ogg` |
| Intro story | Music `Story` | `Audio/Music/Story_CasualTheme2Acoustic.ogg` |
| Tutorial in progress | Music `Tutorial` | `Audio/Music/Tutorial_EasyPeasy.ogg` |
| Preparation | Music `Preparation` | `Audio/Music/Preparation_CasualTheme3Acoustic.ogg` |
| Business, 기본 기계 | Music `Machine1` | `Audio/Music/Machine1_FloweySpeedway.ogg` (whole-clip loop) |
| Business, 소다 기계 | Music `Machine2` | `Audio/Music/Machine2_SecretStage.ogg` (loop 0–111.47 s) |
| Business, 특급 기계 | Music `Machine3` | `Audio/Music/Machine3_TimeTrial.ogg` (loop 0.60–117.93 s) |
| Last 30 s of an open day | Music pitch 1.06 | — |
| Pause | Music at 40 % | — |
| Closing | Bell, engine power-down, jingle after 0.8 s (the loss jingle only when revenue minus material cost is negative); music fades out | `ClosingBell`, `EngineStop`, `ProfitJingle` / `LossJingle` |
| Any authored button, on press or keyboard submit | Click (not `quiet-click` buttons) | `UiClick` |
| Tutorial bubble appears or changes | Pop-up | `TutorialPopup` |
| Sugar poured by shaking | Shaker, pitch ±8 %, at most every 0.35 s | `SugarShake` |
| Candy picked up, dropped on nothing, or dropped back on the machine | Drop | `CandyDrop` |
| Candy taken out (F or 꺼내기) | Pop | `CandyExtract` |
| Sale | Chime (long chime with a star bonus), coins 0.15 s later | `DeliverSuccess` / `StarBonus`, `Coins` |
| Wrong or refused delivery | Fail | `DeliverFail` |
| Trash | Soft thud | `Trash` |
| Upgrade, shelf or progression purchase | Confirm | `Purchase` |
| Business starts / legacy run starts | Engine rev | `EngineStart` |
| Player kart hits a wall | Thump, at most every 0.25 s | `WallHit` |
| Driving | Engine loop (pitch follows speed), machine hum; synthesized drift, wind and boost kept | `EngineLoop`, `MachineHum` |

Sources and licenses: `Assets/CottonCircuit/Audio/THIRD-PARTY.md`.

## Automated results

Final run after the review fixes:

- `Tools/test-core.ps1`: 30, 33 and 14 passed, 0 failed. Eight new core tests cover the cue per screen and machine, settlement silence, legacy modes, the last-30-second hurry, loop wrap points and that pause keeps the day running.
- `Tools/build.ps1 -BuildFolder Builds/UIToolkit`: build succeeded; 141 integration checks passed, including every `Sound` and `MusicCue` present in the bank, music compressed in memory for exact seeking, and loop windows inside each song.
- Conversion: all seven songs keep their exact sample count after Ogg Vorbis encoding (e.g. Flowey Speedway 3 304 107 frames both ways).
- `Tools/verify-uitk.ps1`: music PASSED (11), full PASSED (481, 16 of them audio checks), legacy PASSED (19), rack PASSED (849), hud PASSED (82).
- The full flow checks: title, story, tutorial, preparation and title-again music; first and second machine songs after the tutorial, on business start and on switching machines; a clicked title button; the tutorial bubble sound; the extract pop without a button click; one success chime per sale; one closing bell and silence during settlement; one purchase sound per bought node.
- The music case moves the soda machine song 0.8 s before its loop end: after 1.6 s the song plays 0.5–1.2 s past its loop start on another deck, the old deck has stopped, and a deliberately stopped deck restarts by itself.

## Review

An independent read-only review found five issues; all were confirmed and fixed (see the plan's "Review follow-up"). Buttons click on press, because a trickle-down pointer-up never reaches the document root once a button captures the pointer.

## Needs a human ear

- The runtime wrap of Secret Stage! (111.47 s → 0 s) and Time Trial (117.93 s → 0.60 s): whole bars from the beat grid; the restart is scheduled on the audio clock and the old deck fades over 0.15 s. The music smoke case proves the timing; whether the seam sounds musical needs listening.
- Relative loudness between songs (normalized toward -20 LUFS from measurements) and against the effects.
- Engine loop pitch range while driving and the hum level under the music.
- Coin sound on every sale (1.9 s): the user may prefer a shorter chime if sales overlap.

## Open license items

GameBurp-style effects (Coins, DeliverSuccess, StarBonus, SugarShake, ClosingBell, EngineStart, EngineStop, WallHit) and the `ui_*`, `engine_generator_loop_01` and `background_air_vent_vacumm_hum_motor_loop_02` files came from the user's folder without a recorded license. Casual Theme songs depend on the Fab license of the acquiring account.

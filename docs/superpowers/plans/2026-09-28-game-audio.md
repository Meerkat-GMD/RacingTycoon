# Game audio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the synthesized placeholder tones with the music and effects the user chose, one driving song per cotton-candy machine.

**Architecture:** A `SoundBank` ScriptableObject, filled by an editor `SoundCatalog` from `Assets/CottonCircuit/Audio`, holds every clip with its volume and loop window. `AudioFeedback` plays named `Sound` effects from a small source pool and owns a two-deck `MusicPlayer` that crossfades between `MusicCue`s. The pure-C# `MusicChoice` (Core) maps the current screen to a cue, so the rules are covered by core tests; `AudioFeedback` reads `GameController.MusicScene` every frame, which also works while the smoke runner disables `GameController`.

**Tech Stack:** Unity 6000.5.3f1, C#, UI Toolkit, AudioSource/AudioClip, ffmpeg (asset conversion), existing mono core tests and development-player smoke checks.

**Spec:** Design approved in chat on 2026-09-28 (recorded here; the repository keeps designs inside plans). User picks are in `.superpowers/audio-review/notes.json` (`confirmed`).

## Global Constraints

- Machine songs: 기본 기계 Flowey Speedway, 소다 기계 Secret Stage!, 특급 기계 Time Trial. Crossfade 0.6 s when switching machines; each machine song resumes where it stopped.
- Secret Stage! and Time Trial are CC BY-ND: never edit the audio. Only format conversion to Ogg Vorbis is allowed; looping happens at runtime inside a whole-bar window before the authored ending.
- Last 30 s of an open business day: music pitch 1.06. Pause: music at 40 %. Mute: everything silent.
- Closing: machine music stops, bell, engine power-down, then the pizzicato jingle 0.8 s later. Settlement stays quiet; preparation music starts when preparation opens.
- Every UI button clicks, except elements with the USS class `quiet-click` (the 꺼내기 button, which has its own pop).
- Keep the synthesized boost, drift and wind sounds; the user did not replace them.
- UI stays authored in UXML (AGENTS.md): only add a class attribute in `Business.uxml`, no generated UI.
- Record every source and license in `Assets/CottonCircuit/Audio/THIRD-PARTY.md`.

---

## File map

- Create `Assets/CottonCircuit/Audio/Music/*.ogg`, `Audio/Sfx/*`, `Audio/Licenses/*`, `Audio/THIRD-PARTY.md` — the chosen files.
- Create `Assets/CottonCircuit/Scripts/Core/MusicChoice.cs` — screen → cue rules, hurry rule, loop wrap rule.
- Modify `Assets/CottonCircuit/Scripts/Core/ShopShift.cs` — `DayRunning` (open ignoring pause).
- Modify `Tools/Tests/CoreTests.cs` — MusicChoice tests.
- Create `Assets/CottonCircuit/Scripts/SoundBank.cs` — `Sound`, `SoundEntry`, `MusicEntry`, `SoundBank`.
- Create `Assets/CottonCircuit/Scripts/MusicPlayer.cs` — two-deck crossfade player.
- Rewrite `Assets/CottonCircuit/Scripts/AudioFeedback.cs` — clip playback, loops, music, mute.
- Create `Assets/CottonCircuit/Editor/SoundCatalog.cs`, `Editor/AudioImportRules.cs`.
- Modify `Editor/ProjectBuilder.cs`, `Editor/IntegrationChecks.cs` — wire and check the bank.
- Modify `Scripts/ToolkitUI.cs`, `Assets/Resources/UI/Business.uxml` — button click event, quiet class.
- Modify `Scripts/GameController.cs`, `ShiftController.cs`, `ProgressionController.cs`, `ToolkitDragController.cs`, `TutorialOverlayUI.cs`, `TitleScreenUI.cs` — sound call sites and `MusicScene`.
- Modify `Tests/ToolkitRuntimeSmoke.cs` — audio assertions.
- Create `docs/audio-verification.md`.

---

### Task 1: Import the chosen audio files

**Files:**
- Create: `Assets/CottonCircuit/Audio/Music/*.ogg`, `Assets/CottonCircuit/Audio/Sfx/*`, `Assets/CottonCircuit/Audio/Licenses/*`, `Assets/CottonCircuit/Audio/THIRD-PARTY.md`

**Interfaces:**
- Produces: the file names below; Task 3's `SoundCatalog` loads exactly these paths.

- [x] **Step 1: Copy and convert**

Run in Git Bash from the repository root:

```bash
SRC="/c/Users/gmd13/Downloads/CCMusics-20260928T065045Z-1-001/CCMusics"
WEB=".superpowers/audio-review/web"; USR=".superpowers/audio-review/user"; DST="Assets/CottonCircuit/Audio"
mkdir -p "$DST/Music" "$DST/Sfx" "$DST/Licenses"
enc() { ffmpeg -v error -y -i "$1" -map_metadata -1 -c:a libvorbis -q:a 7 "$2"; }
enc "$SRC/BGM/캐쥬얼/Theme/Casual Theme #1 (Looped).wav" "$DST/Music/Title_CasualTheme1.ogg"
enc "$SRC/BGM/캐쥬얼/Theme/Casual Theme #2 - Acoustic Version (Looped).wav" "$DST/Music/Story_CasualTheme2Acoustic.ogg"
enc "$WEB/zakiro_free-casual-vol1_4226283/Track 4 (Easy Peasy).wav" "$DST/Music/Tutorial_EasyPeasy.ogg"
enc "$SRC/BGM/캐쥬얼/Theme/Casual Theme #3 - Acoustic Version (Looped).wav" "$DST/Music/Preparation_CasualTheme3Acoustic.ogg"
enc "$WEB/flowerhead_somewhat-good-karts/Flowey Speedway.wav" "$DST/Music/Machine1_FloweySpeedway.ogg"
enc "$WEB/joechrisman_lizard-scooter/08 - Secret Stage! (Nov-23-2024).wav" "$DST/Music/Machine2_SecretStage.ogg"
enc "$WEB/joechrisman_lizard-scooter/13 - Time Trial (Nov-23-2024).wav" "$DST/Music/Machine3_TimeTrial.ogg"
cp "$WEB/kenney_interface-sounds/Audio/click_001.ogg" "$DST/Sfx/UiClick.ogg"
cp "$SRC/SFX/UI/튜토리얼 팝업/ui_menu_popup_message_05.wav" "$DST/Sfx/TutorialPopup.wav"
cp "$SRC/SFX/기본 효과음/가루넣기/SHAKER Pieces Shuffle 02.ogg" "$DST/Sfx/SugarShake.ogg"
cp "$WEB/kenney_interface-sounds/Audio/drop_002.ogg" "$DST/Sfx/CandyDrop.ogg"
cp "$USR/trim_soundreality-pop.ogg" "$DST/Sfx/CandyExtract.ogg"
cp "$SRC/SFX/솜사탕 전달/만족/SUCCESS CHIME Bells Sparkle Tune Short 05.ogg" "$DST/Sfx/DeliverSuccess.ogg"
cp "$SRC/SFX/솜사탕 전달/만족/SUCCESS CHIME Bells Sparkle Tune 08.ogg" "$DST/Sfx/StarBonus.ogg"
cp "$SRC/SFX/가판대 관련 소리/돈 획득/COINS Collect Jackpot Win 01.ogg" "$DST/Sfx/Coins.ogg"
cp "$SRC/SFX/솜사탕 전달/실패/ui_fail_01.wav" "$DST/Sfx/DeliverFail.wav"
cp "$WEB/kenney_impact-sounds/Audio/impactSoft_medium_001.ogg" "$DST/Sfx/Trash.ogg"
cp "$SRC/SFX/솜사탕 전달/만족/ui_menu_button_confirm_09.wav" "$DST/Sfx/Purchase.wav"
cp "$SRC/SFX/가판대 관련 소리/영업종료/BONG Bell Timer Hit 02.ogg" "$DST/Sfx/ClosingBell.ogg"
cp "$WEB/kenney_music-jingles/Audio/Pizzicato jingles/jingles_PIZZI07.ogg" "$DST/Sfx/ClosingJingle.ogg"
cp "$SRC/SFX/자동차관련소리/출발음/MECH Engine Motor Rev 01.ogg" "$DST/Sfx/EngineStart.ogg"
cp "$SRC/SFX/자동차관련소리/자동차 엔진소리/OFF/MECH Servo Motor Power Down 04.ogg" "$DST/Sfx/EngineStop.ogg"
cp "$SRC/SFX/자동차관련소리/충돌소리/IMPACT Thump Thud 01.ogg" "$DST/Sfx/WallHit.ogg"
cp "$SRC/SFX/자동차관련소리/자동차 엔진소리/Normal/engine_generator_loop_01.wav" "$DST/Sfx/EngineLoop.wav"
cp "$SRC/BGM/솜사탕기계 백사운드/background_air_vent_vacumm_hum_motor_loop_02.wav" "$DST/Sfx/MachineHum.wav"
cp "$WEB/kenney_music-jingles/License.txt" "$DST/Licenses/Kenney-MusicJingles.txt"
cp "$WEB/kenney_interface-sounds/License.txt" "$DST/Licenses/Kenney-InterfaceSounds.txt"
cp "$WEB/kenney_impact-sounds/License.txt" "$DST/Licenses/Kenney-ImpactSounds.txt"
ls "$DST/Music" "$DST/Sfx" | wc -l
```

Expected: `27` lines (7 music, 18 effects and 2 headers).

- [x] **Step 2: Prove the conversion kept every sample**

Seamless loops must keep their exact length. Run with the analysis venv:

```bash
PY="/c/Users/gmd13/AppData/Local/Temp/claude/D--UnityProjects-RacingTycoon/310c970f-b470-4301-abde-dbdb4d75b547/scratchpad/audio-venv/Scripts/python"
"$PY" - <<'EOF'
import soundfile as sf
pairs = {
 "Title_CasualTheme1": r"C:/Users/gmd13/Downloads/CCMusics-20260928T065045Z-1-001/CCMusics/BGM/캐쥬얼/Theme/Casual Theme #1 (Looped).wav",
 "Machine1_FloweySpeedway": r".superpowers/audio-review/web/flowerhead_somewhat-good-karts/Flowey Speedway.wav",
 "Tutorial_EasyPeasy": r".superpowers/audio-review/web/zakiro_free-casual-vol1_4226283/Track 4 (Easy Peasy).wav",
}
for name, src in pairs.items():
    a = sf.info(src).frames; b = sf.info(f"Assets/CottonCircuit/Audio/Music/{name}.ogg").frames
    print(name, a, b, "OK" if a == b else "LENGTH CHANGED")
EOF
```

Expected: three `OK` lines. If a loop reports `LENGTH CHANGED`, copy that WAV unchanged under the same base name with `.wav` and use `.wav` in Task 3.

- [x] **Step 3: Write `Assets/CottonCircuit/Audio/THIRD-PARTY.md`**

```markdown
# Audio sources

Chosen by the user on 2026-09-28 from the listening review in `.superpowers/audio-review/`.
Music was converted to Ogg Vorbis (quality 7) only; no clip was otherwise edited except `Sfx/CandyExtract.ogg`.

| File | Source | License | Credit / notes |
|---|---|---|---|
| Music/Title_CasualTheme1.ogg | "Casual Theme #1 (Looped)", Casual Game Music, Muzstation Game Music (Fab) | Fab Standard License of the account that acquired it | Confirm the acquiring account before release |
| Music/Story_CasualTheme2Acoustic.ogg | "Casual Theme #2 - Acoustic Version (Looped)", same pack | same | same |
| Music/Preparation_CasualTheme3Acoustic.ogg | "Casual Theme #3 - Acoustic Version (Looped)", same pack | same | same |
| Music/Tutorial_EasyPeasy.ogg | "Easy Peasy", Free Casual Game Music Pack Vol. 1, Zakiro — https://zakiro101.itch.io/free-casual-game-music-pack-vol-1 | Free for commercial and non-commercial use; credit requested | Music by Zakiro |
| Music/Machine1_FloweySpeedway.ogg | "Flowey Speedway", SomeWhatGood: Karts, flowerhead — https://flowerheadmusic.itch.io/somewhat-good-karts | Royalty free (no further terms published) | Music by flowerhead |
| Music/Machine2_SecretStage.ogg | "Secret Stage!", Lizard Scooter!, JoeChrismanMusic — https://joechrismanmusic.itch.io/lizard-scooter-royalty-free-bgm | CC BY-ND 4.0 — do not edit; ask the artist before any modification | Music by Joe Chrisman |
| Music/Machine3_TimeTrial.ogg | "Time Trial", same album | CC BY-ND 4.0 | Music by Joe Chrisman |
| Sfx/UiClick.ogg, Sfx/CandyDrop.ogg | Kenney Interface Sounds `click_001`, `drop_002` — https://kenney.nl/assets/interface-sounds | CC0 (`Licenses/Kenney-InterfaceSounds.txt`) | |
| Sfx/Trash.ogg | Kenney Impact Sounds `impactSoft_medium_001` — https://kenney.nl/assets/impact-sounds | CC0 (`Licenses/Kenney-ImpactSounds.txt`) | |
| Sfx/ClosingJingle.ogg | Kenney Music Jingles `jingles_PIZZI07` — https://kenney.nl/assets/music-jingles | CC0 (`Licenses/Kenney-MusicJingles.txt`) | |
| Sfx/CandyExtract.ogg | Pixabay "pop" by soundreality (`soundreality-pop-423717.mp3`), leading silence removed | Pixabay Content License | Record the download page URL |
| Sfx/SugarShake.ogg, Coins.ogg, DeliverSuccess.ogg, StarBonus.ogg, ClosingBell.ogg, EngineStart.ogg, EngineStop.ogg, WallHit.ogg | User's CCMusics folder; names match GameBurp "2000 Game Sound FX" | Unconfirmed | Confirm purchase and license before release |
| Sfx/TutorialPopup.wav, DeliverFail.wav, Purchase.wav, EngineLoop.wav, MachineHum.wav | User's CCMusics folder (`ui_*`, `engine_generator_loop_01`, `background_air_vent_vacumm_hum_motor_loop_02`) | Unconfirmed | Confirm source before release |
```

- [x] **Step 4: Import in Unity to create `.meta` files**

The project must not be open in the Unity editor (`Temp/UnityLockfile` absent).

```bash
"D:/Unity/Hub/6000.5.3f1/Editor/Unity.exe" -batchmode -nographics -projectPath "D:/UnityProjects/RacingTycoon" -quit -logFile Logs/audio-import.log
ls Assets/CottonCircuit/Audio/Music/*.meta | wc -l
```

Expected: exit code 0 and `7`.

- [x] **Step 5: Commit**

```bash
git add Assets/CottonCircuit/Audio Assets/CottonCircuit/Audio.meta
git commit -m "Import the chosen music and effects with their sources"
```

---

### Task 2: Music rules in Core

**Files:**
- Create: `Assets/CottonCircuit/Scripts/Core/MusicChoice.cs`
- Modify: `Assets/CottonCircuit/Scripts/Core/ShopShift.cs:100-101`
- Test: `Tools/Tests/CoreTests.cs` (before the `RESULT` line)

**Interfaces:**
- Produces: `enum MusicCue { None, Title, Story, Tutorial, Preparation, Machine1, Machine2, Machine3 }`, `struct MusicScene`, `MusicChoice.Cue(MusicScene)`, `MusicChoice.Hurry(MusicScene)`, `MusicChoice.HurryPitch`, `MusicChoice.WrapDue(double time, double loopEnd)`, `MusicChoice.Remembers(MusicCue)`, `ShopShift.DayRunning`.

- [x] **Step 1: Write the failing tests**

Insert before `Console.WriteLine("RESULT: ...` in `Tools/Tests/CoreTests.cs`:

```csharp
        Test("title and story music come before any session", () => { Check(MusicChoice.Cue(new MusicScene { Title = true }) == MusicCue.Title, "title"); Check(MusicChoice.Cue(new MusicScene { Title = true, Story = true }) == MusicCue.Story, "story"); Check(MusicChoice.Cue(new MusicScene()) == MusicCue.None, "no session is silent"); });
        Test("tutorial music overrides the business song", () => { var s = new MusicScene { InGame = true, Tutorial = true, Progression = true, ShiftExists = true, ShiftOpen = true, Phase = BusinessPhase.Operating, Machine = 2 }; Check(MusicChoice.Cue(s) == MusicCue.Tutorial, "tutorial"); });
        Test("each machine has its own business song", () => { var s = new MusicScene { InGame = true, Progression = true, ShiftExists = true, ShiftOpen = true, Phase = BusinessPhase.Operating }; for (int m = 0; m < 3; m++) { s.Machine = m; Check(MusicChoice.Cue(s) == (MusicCue)((int)MusicCue.Machine1 + m), "machine " + m); } });
        Test("preparation plays its theme and settlement is quiet", () => { var s = new MusicScene { InGame = true, Progression = true, ShiftExists = true, Phase = BusinessPhase.Preparation }; Check(MusicChoice.Cue(s) == MusicCue.Preparation, "preparation"); s.Phase = BusinessPhase.Results; Check(MusicChoice.Cue(s) == MusicCue.None, "settlement"); s.Phase = BusinessPhase.Operating; s.ShiftOpen = false; Check(MusicChoice.Cue(s) == MusicCue.None, "closed day"); });
        Test("legacy modes map to preparation and the first machine song", () => { var s = new MusicScene { InGame = true, Mode = GameMode.Shop }; Check(MusicChoice.Cue(s) == MusicCue.Preparation, "shop"); s.Mode = GameMode.Racing; Check(MusicChoice.Cue(s) == MusicCue.Machine1, "racing"); s.Mode = GameMode.Results; Check(MusicChoice.Cue(s) == MusicCue.None, "results"); s = new MusicScene { InGame = true, ShiftExists = true, ShiftOpen = true }; Check(MusicChoice.Cue(s) == MusicCue.Machine1, "day without progression"); });
        Test("only the last thirty open seconds hurry", () => { var s = new MusicScene { InGame = true, ShiftOpen = true, RemainingSeconds = 31 }; Check(!MusicChoice.Hurry(s), "too early"); s.RemainingSeconds = 30; Check(MusicChoice.Hurry(s), "thirty seconds"); s.Tutorial = true; Check(!MusicChoice.Hurry(s), "tutorial never hurries"); s.Tutorial = false; s.ShiftOpen = false; Check(!MusicChoice.Hurry(s), "closed day"); });
        Test("authored endings wrap at their loop end and loops never wrap", () => { Check(!MusicChoice.WrapDue(111.4, 111.47) && MusicChoice.WrapDue(111.47, 111.47), "wrap point"); Check(!MusicChoice.WrapDue(500, 0), "whole-clip loop"); Check(MusicChoice.Remembers(MusicCue.Machine2) && !MusicChoice.Remembers(MusicCue.Preparation), "only machine songs resume"); });
        Test("pause does not end the business day", () => { var e = new Economy(); var shift = new ShopShift(e); Check(shift.IsOpen && shift.DayRunning, "open day"); shift.Paused = true; Check(!shift.IsOpen && shift.DayRunning, "paused day still running"); });
```

- [x] **Step 2: Run the core tests and see them fail to compile**

Run: `powershell -File Tools/test-core.ps1`
Expected: mcs error `The name 'MusicChoice' does not exist` (and `MusicScene`).

- [x] **Step 3: Implement**

Create `Assets/CottonCircuit/Scripts/Core/MusicChoice.cs`:

```csharp
namespace CottonCircuit
{
    public enum MusicCue { None, Title, Story, Tutorial, Preparation, Machine1, Machine2, Machine3 }

    // What is on screen, gathered by GameController for the audio layer.
    public struct MusicScene
    {
        public bool Title, Story, InGame, Tutorial, Progression, ShiftExists, ShiftOpen;
        public BusinessPhase Phase;
        public GameMode Mode;
        public int Machine;
        public double RemainingSeconds;
    }

    public static class MusicChoice
    {
        public const double HurrySeconds = 30;
        public const float HurryPitch = 1.06f;

        public static MusicCue Cue(MusicScene scene)
        {
            if (scene.Story) return MusicCue.Story;
            if (scene.Title) return MusicCue.Title;
            if (!scene.InGame) return MusicCue.None;
            if (scene.Tutorial) return MusicCue.Tutorial;
            if (scene.Progression)
            {
                if (scene.Phase == BusinessPhase.Preparation) return MusicCue.Preparation;
                return scene.Phase == BusinessPhase.Operating && scene.ShiftOpen ? Machine(scene.Machine) : MusicCue.None;
            }
            if (scene.ShiftExists) return scene.ShiftOpen ? MusicCue.Machine1 : MusicCue.None;
            return scene.Mode == GameMode.Shop ? MusicCue.Preparation : scene.Mode == GameMode.Racing ? MusicCue.Machine1 : MusicCue.None;
        }

        public static MusicCue Machine(int index) => index == 1 ? MusicCue.Machine2 : index == 2 ? MusicCue.Machine3 : MusicCue.Machine1;

        public static bool Hurry(MusicScene scene) =>
            !scene.Tutorial && scene.ShiftOpen && scene.RemainingSeconds > 0 && scene.RemainingSeconds <= HurrySeconds;

        // A song with an authored ending restarts from its loop start once playback reaches loopEnd.
        public static bool WrapDue(double time, double loopEnd) => loopEnd > 0 && time >= loopEnd;

        // Machine songs resume where they stopped, so switching machines does not restart them.
        public static bool Remembers(MusicCue cue) => cue >= MusicCue.Machine1;
    }
}
```

In `Assets/CottonCircuit/Scripts/Core/ShopShift.cs` replace lines 100-101:

```csharp
        public bool IsOpen { get { return !Paused && DayRunning; } }
        // Open ignoring pause: the day has time left and the business is operating.
        public bool DayRunning { get { return !State.Closed && State.RemainingSeconds > 0 &&
            (economy.Progression == null || economy.Progression.Phase == BusinessPhase.Operating); } }
```

- [x] **Step 4: Run the core tests**

Run: `powershell -File Tools/test-core.ps1`
Expected: all three result lines end with `0 failed`, including the eight new `PASS` lines.

- [x] **Step 5: Commit**

```bash
git add Assets/CottonCircuit/Scripts/Core/MusicChoice.cs Assets/CottonCircuit/Scripts/Core/ShopShift.cs Tools/Tests/CoreTests.cs
git commit -m "Choose music from the screen state in core rules"
```

(`MusicChoice.cs.meta` is created in Task 5's Unity run and committed there.)

---

### Task 3: Sound bank and editor wiring

**Files:**
- Create: `Assets/CottonCircuit/Scripts/SoundBank.cs`, `Assets/CottonCircuit/Editor/SoundCatalog.cs`, `Assets/CottonCircuit/Editor/AudioImportRules.cs`
- Modify: `Assets/CottonCircuit/Editor/ProjectBuilder.cs:52`, `Assets/CottonCircuit/Editor/IntegrationChecks.cs:256`

**Interfaces:**
- Consumes: `MusicCue` (Task 2), files from Task 1.
- Produces: `enum Sound { UiClick, TutorialPopup, SugarShake, CandyDrop, CandyExtract, DeliverSuccess, StarBonus, Coins, DeliverFail, Trash, Purchase, ClosingBell, ClosingJingle, EngineStart, EngineStop, WallHit }`, `SoundBank.TryGet(Sound, out SoundEntry)`, `SoundBank.TryGet(MusicCue, out MusicEntry)`, `SoundBank.EngineLoop/MachineHum/EngineVolume/MachineHumVolume`, `AudioFeedback.Sounds` field (declared in Task 4; this task adds a temporary declaration only if Task 4 is not yet done — do Task 3 and Task 4 back to back and compile once).

- [x] **Step 1: Create `Assets/CottonCircuit/Scripts/SoundBank.cs`**

```csharp
using System;
using UnityEngine;
namespace CottonCircuit
{
    public enum Sound
    {
        UiClick, TutorialPopup, SugarShake, CandyDrop, CandyExtract, DeliverSuccess, StarBonus, Coins,
        DeliverFail, Trash, Purchase, ClosingBell, ClosingJingle, EngineStart, EngineStop, WallHit
    }

    [Serializable] public struct SoundEntry
    {
        public Sound Id;
        public AudioClip Clip;
        [Range(0, 1)] public float Volume;
        public float MinInterval;   // a repeat sooner than this is skipped
        public float PitchJitter;   // random +/- pitch for sounds that repeat quickly
    }

    [Serializable] public struct MusicEntry
    {
        public MusicCue Cue;
        public AudioClip Clip;
        [Range(0, 1)] public float Volume;
        // LoopEnd 0 loops the whole clip. Otherwise playback returns to LoopStart at LoopEnd,
        // a whole number of bars before the authored ending; the file itself is never edited.
        public float LoopStart, LoopEnd;
    }

    // Filled by the editor SoundCatalog from Assets/CottonCircuit/Audio.
    public class SoundBank : ScriptableObject
    {
        public SoundEntry[] Sounds = new SoundEntry[0];
        public MusicEntry[] Music = new MusicEntry[0];
        public AudioClip EngineLoop, MachineHum;
        [Range(0, 1)] public float EngineVolume = .3f, MachineHumVolume = .18f;

        public bool TryGet(Sound id, out SoundEntry entry)
        {
            foreach (var candidate in Sounds) if (candidate.Id == id && candidate.Clip) { entry = candidate; return true; }
            entry = default; return false;
        }

        public bool TryGet(MusicCue cue, out MusicEntry entry)
        {
            foreach (var candidate in Music) if (candidate.Cue == cue && candidate.Clip) { entry = candidate; return true; }
            entry = default; return false;
        }
    }
}
```

- [x] **Step 2: Create `Assets/CottonCircuit/Editor/AudioImportRules.cs`**

```csharp
using UnityEditor;
using UnityEngine;
namespace CottonCircuit.Editor
{
    // Music streams from disk; short effects and loops decompress once on load.
    public sealed class AudioImportRules : AssetPostprocessor
    {
        public const string Root = "Assets/CottonCircuit/Audio/";
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            bool music = assetPath.StartsWith(Root + "Music/");
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? .7f : .8f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = music;
        }
    }
}
```

- [x] **Step 3: Create `Assets/CottonCircuit/Editor/SoundCatalog.cs`**

Volumes: music is normalized toward -20 LUFS from the measured loudness (Title -17.6, Story -22.9, Tutorial -13.7, Preparation -18.1, Flowey -16.4, Secret Stage -10.8, Time Trial -9.9); `AudioFeedback` then applies a 0.7 music level. Loop windows are whole bars measured from the beat grid (Secret Stage 103.35 BPM, 48 bars; Time Trial 90.0 BPM, 44 bars) and end before the authored endings at 114.5 s and 119.0 s.

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;
namespace CottonCircuit.Editor
{
    // Fills the SoundBank from Assets/CottonCircuit/Audio. Sources are listed in Audio/THIRD-PARTY.md.
    public static class SoundCatalog
    {
        const string Root = "Assets/CottonCircuit/Audio/";
        public const string BankPath = "Assets/CottonCircuit/Data/SoundBank.asset";

        static AudioClip Load(string file)
        {
            string path = Root + file;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip) throw new FileNotFoundException("Missing audio " + path, path);
            return clip;
        }

        static SoundEntry Effect(Sound id, string file, float volume, float minInterval = .04f, float jitter = 0) =>
            new SoundEntry { Id = id, Clip = Load("Sfx/" + file), Volume = volume, MinInterval = minInterval, PitchJitter = jitter };

        static MusicEntry Song(MusicCue cue, string file, float volume, float loopStart = 0, float loopEnd = 0) =>
            new MusicEntry { Cue = cue, Clip = Load("Music/" + file), Volume = volume, LoopStart = loopStart, LoopEnd = loopEnd };

        public static SoundBank Build()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (!bank) { bank = ScriptableObject.CreateInstance<SoundBank>(); AssetDatabase.CreateAsset(bank, BankPath); }
            bank.Sounds = new[] {
                Effect(Sound.UiClick, "UiClick.ogg", .5f, .05f),
                Effect(Sound.TutorialPopup, "TutorialPopup.wav", .6f, .3f),
                Effect(Sound.SugarShake, "SugarShake.ogg", .5f, .35f, .08f),
                Effect(Sound.CandyDrop, "CandyDrop.ogg", .7f, .08f),
                Effect(Sound.CandyExtract, "CandyExtract.ogg", .6f),
                Effect(Sound.DeliverSuccess, "DeliverSuccess.ogg", .7f),
                Effect(Sound.StarBonus, "StarBonus.ogg", .6f),
                Effect(Sound.Coins, "Coins.ogg", .5f),
                Effect(Sound.DeliverFail, "DeliverFail.wav", .6f, .2f),
                Effect(Sound.Trash, "Trash.ogg", .7f),
                Effect(Sound.Purchase, "Purchase.wav", .7f),
                Effect(Sound.ClosingBell, "ClosingBell.ogg", .7f),
                Effect(Sound.ClosingJingle, "ClosingJingle.ogg", .8f),
                Effect(Sound.EngineStart, "EngineStart.ogg", .35f, .5f),
                Effect(Sound.EngineStop, "EngineStop.ogg", .5f, .5f),
                Effect(Sound.WallHit, "WallHit.ogg", .6f, .25f, .06f),
            };
            bank.Music = new[] {
                Song(MusicCue.Title, "Title_CasualTheme1.ogg", .75f),
                Song(MusicCue.Story, "Story_CasualTheme2Acoustic.ogg", 1f),
                Song(MusicCue.Tutorial, "Tutorial_EasyPeasy.ogg", .49f),
                Song(MusicCue.Preparation, "Preparation_CasualTheme3Acoustic.ogg", .8f),
                Song(MusicCue.Machine1, "Machine1_FloweySpeedway.ogg", .66f),
                Song(MusicCue.Machine2, "Machine2_SecretStage.ogg", .35f, 0f, 111.47f),
                Song(MusicCue.Machine3, "Machine3_TimeTrial.ogg", .31f, .6f, 117.93f),
            };
            bank.EngineLoop = Load("Sfx/EngineLoop.wav");
            bank.MachineHum = Load("Sfx/MachineHum.wav");
            bank.EngineVolume = .3f; bank.MachineHumVolume = .18f;
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }
    }
}
```

- [x] **Step 4: Wire the bank in `ProjectBuilder.CreateScene`**

In `Assets/CottonCircuit/Editor/ProjectBuilder.cs` replace line 52:

```csharp
            controller.Audio = gameObject.AddComponent<AudioFeedback>(); controller.UI = gameObject.AddComponent<GameUI>();
```

with:

```csharp
            controller.Audio = gameObject.AddComponent<AudioFeedback>(); controller.Audio.Sounds = SoundCatalog.Build();
            controller.UI = gameObject.AddComponent<GameUI>();
```

- [x] **Step 5: Check the bank in `IntegrationChecks`**

In `Assets/CottonCircuit/Editor/IntegrationChecks.cs` after line 256 (`Check(game && game.World && game.UI && game.Audio, "wired playable scene");`) add:

```csharp
            var bank = game.Audio.Sounds;
            Check(bank, "audio feedback uses the authored sound bank");
            foreach (Sound id in Enum.GetValues(typeof(Sound))) Check(bank.TryGet(id, out _), "sound bank has " + id);
            foreach (MusicCue cue in Enum.GetValues(typeof(MusicCue)))
                if (cue != MusicCue.None) Check(bank.TryGet(cue, out _), "sound bank has music for " + cue);
            Check(bank.EngineLoop && bank.MachineHum, "sound bank has the engine and machine loops");
            foreach (var song in bank.Music)
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(song.Clip));
                Check(importer.defaultSampleSettings.loadType == AudioClipLoadType.Streaming, song.Cue + " music streams from disk");
                Check(song.LoopEnd == 0 || song.LoopEnd < song.Clip.length && song.LoopStart < song.LoopEnd, song.Cue + " loop window lies inside the song");
            }
```

- [x] **Step 6: Continue with Task 4 before compiling** (Task 3 references `AudioFeedback.Sounds`, declared in Task 4). Commit both together at the end of Task 4.

---

### Task 4: Clip playback, music player and screen state

**Files:**
- Create: `Assets/CottonCircuit/Scripts/MusicPlayer.cs`
- Rewrite: `Assets/CottonCircuit/Scripts/AudioFeedback.cs`
- Modify: `Assets/CottonCircuit/Scripts/GameController.cs` (add `MusicScene`), `Assets/CottonCircuit/Scripts/TitleScreenUI.cs` (add `StoryShowing`), `Assets/CottonCircuit/Scripts/ToolkitUI.cs` (add `ButtonPressed`)

**Interfaces:**
- Consumes: `MusicChoice`, `MusicScene`, `SoundBank`, `Sound`, `ShopShift.DayRunning`.
- Produces: `AudioFeedback.Play(Sound)`, `AudioFeedback.PlaySale(bool starBonus)`, `AudioFeedback.PlayClosing()`, `AudioFeedback.PlayBoost(int)`, `AudioFeedback.UpdateDriving(KartController, bool)`, `AudioFeedback.Toggle()`, `AudioFeedback.Muted`, `AudioFeedback.Played(Sound)` (`int`, times actually played), `AudioFeedback.Music` (`MusicCue`), `GameController.MusicScene`, `ToolkitUI.ButtonPressed` (`event Action`), `ToolkitUI.QuietClick` (`"quiet-click"`).

- [x] **Step 1: Create `Assets/CottonCircuit/Scripts/MusicPlayer.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
namespace CottonCircuit
{
    // Two decks crossfade between cues. Machine songs resume where they stopped; songs with an
    // authored ending wrap from LoopEnd to LoopStart by crossfading into the other deck.
    public sealed class MusicPlayer
    {
        const float SwitchFade = .6f, WrapFade = 1f, PitchRate = .1f;
        readonly AudioSource[] decks = new AudioSource[2];
        readonly MusicEntry[] entries = new MusicEntry[2];
        readonly bool[] loaded = new bool[2];
        readonly float[] levels = new float[2], fades = { SwitchFade, SwitchFade };
        readonly Dictionary<MusicCue, float> resume = new Dictionary<MusicCue, float>();
        int active;

        public MusicCue Current { get; private set; }

        public MusicPlayer(GameObject host)
        {
            for (int i = 0; i < decks.Length; i++)
            {
                decks[i] = host.AddComponent<AudioSource>();
                decks[i].playOnAwake = false; decks[i].volume = 0;
            }
        }

        public void Update(SoundBank bank, MusicCue cue, float level, float pitch, float deltaTime)
        {
            if (cue != Current) Switch(bank, cue);
            var deck = decks[active];
            if (loaded[active] && deck.isPlaying && MusicChoice.WrapDue(deck.time, entries[active].LoopEnd))
                Begin(entries[active], entries[active].LoopStart, WrapFade);
            for (int i = 0; i < decks.Length; i++)
            {
                bool audible = i == active && loaded[i];
                levels[i] = Mathf.MoveTowards(levels[i], audible ? 1 : 0, deltaTime / fades[i]);
                decks[i].volume = levels[i] * entries[i].Volume * level;
                decks[i].pitch = Mathf.MoveTowards(decks[i].pitch, pitch, deltaTime * PitchRate);
                if (!audible && levels[i] <= 0 && decks[i].isPlaying) decks[i].Stop();
            }
        }

        void Switch(SoundBank bank, MusicCue cue)
        {
            if (MusicChoice.Remembers(Current) && loaded[active]) resume[Current] = decks[active].time;
            Current = cue;
            if (!bank.TryGet(cue, out var next)) { loaded[active] = false; fades[active] = SwitchFade; return; }
            float at = MusicChoice.Remembers(cue) && resume.TryGetValue(cue, out float stopped) ? stopped : next.LoopStart;
            Begin(next, at, SwitchFade);
        }

        void Begin(MusicEntry next, float at, float crossfade)
        {
            fades[active] = crossfade;
            int incoming = 1 - active;
            var deck = decks[incoming];
            deck.Stop();
            deck.clip = next.Clip;
            deck.loop = next.LoopEnd <= 0;
            deck.pitch = decks[active].pitch;
            deck.Play();
            deck.time = Mathf.Clamp(at, 0, next.Clip.length - .05f);
            entries[incoming] = next; loaded[incoming] = true;
            levels[incoming] = 0; fades[incoming] = crossfade;
            active = incoming;
        }
    }
}
```

- [x] **Step 2: Rewrite `Assets/CottonCircuit/Scripts/AudioFeedback.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
namespace CottonCircuit
{
    public class AudioFeedback : MonoBehaviour
    {
        const float MusicLevel = .7f, PausedMusic = .4f, CoinDelay = .15f, JingleDelay = .8f, BoostVolume = .25f;
        public SoundBank Sounds;
        GameController game;
        readonly AudioSource[] pool = new AudioSource[8];
        int next;
        AudioSource engine, skid, wind, hum;
        AudioClip boost, superBoost, skidClip, windClip;
        MusicPlayer music;
        readonly Dictionary<Sound, float> lastTimes = new Dictionary<Sound, float>();
        readonly List<KeyValuePair<float, Sound>> queued = new List<KeyValuePair<float, Sound>>();
        readonly Dictionary<Sound, int> counts = new Dictionary<Sound, int>();
        public bool Muted { get; private set; }
        public MusicCue Music => music.Current;
        // How often a sound was actually heard; the development smoke check reads it.
        public int Played(Sound id) => counts.TryGetValue(id, out int count) ? count : 0;

        void Awake()
        {
            game = GetComponent<GameController>();
            for (int i = 0; i < pool.Length; i++) { pool[i] = gameObject.AddComponent<AudioSource>(); pool[i].playOnAwake = false; }
            boost = BoostSound(false); superBoost = BoostSound(true);
            skidClip = Noise("Tire slide"); windClip = Noise("Wind");
            engine = Loop(Sounds.EngineLoop); skid = Loop(skidClip); wind = Loop(windClip); hum = Loop(Sounds.MachineHum);
            music = new MusicPlayer(gameObject);
        }

        void OnEnable() { ToolkitUI.ButtonPressed += Click; }
        void OnDisable() { ToolkitUI.ButtonPressed -= Click; }
        void Click() { Play(Sound.UiClick); }

        void LateUpdate()
        {
            float now = Time.unscaledTime;
            for (int i = queued.Count - 1; i >= 0; i--)
                if (queued[i].Key <= now) { var sound = queued[i].Value; queued.RemoveAt(i); Play(sound); }
            var scene = game.MusicScene;
            bool paused = game.Session != null && game.Session.Paused;
            float level = Muted ? 0 : MusicLevel * (paused ? PausedMusic : 1);
            music.Update(Sounds, MusicChoice.Cue(scene), level, MusicChoice.Hurry(scene) ? MusicChoice.HurryPitch : 1, Time.unscaledDeltaTime);
        }

        public void Play(Sound id)
        {
            if (Muted || !Sounds.TryGet(id, out var entry)) return;
            float now = Time.unscaledTime;
            if (lastTimes.TryGetValue(id, out float last) && now - last < entry.MinInterval) return;
            lastTimes[id] = now;
            counts[id] = Played(id) + 1;
            Emit(entry.Clip, entry.Volume, 1 + Random.Range(-entry.PitchJitter, entry.PitchJitter));
        }

        void PlayAfter(Sound id, float delay) { queued.Add(new KeyValuePair<float, Sound>(Time.unscaledTime + delay, id)); }

        public void PlaySale(bool starBonus)
        {
            Play(starBonus ? Sound.StarBonus : Sound.DeliverSuccess);
            PlayAfter(Sound.Coins, CoinDelay);
        }

        public void PlayClosing()
        {
            Play(Sound.ClosingBell); Play(Sound.EngineStop);
            PlayAfter(Sound.ClosingJingle, JingleDelay);
        }

        public void PlayBoost(int tier) { if (!Muted) Emit(tier == 2 ? superBoost : boost, BoostVolume, 1); }

        void Emit(AudioClip clip, float volume, float pitch)
        {
            var source = pool[next]; next = (next + 1) % pool.Length;
            source.clip = clip; source.volume = volume; source.pitch = pitch; source.Play();
        }

        public void UpdateDriving(KartController kart, bool active)
        {
            if (!engine) return;
            bool audible = active && !Muted;
            bool downhill = kart.DriveModel.Style == DrivingStyle.Downhill;
            float speed = Mathf.Clamp01(kart.Speed / (downhill ? 45 : 32));
            engine.volume = audible ? Mathf.Lerp(.25f, 1f, speed) * Sounds.EngineVolume : 0;
            engine.pitch = .8f + speed * .8f;
            hum.volume = audible ? Sounds.MachineHumVolume : 0;
            skid.volume = audible && kart.Drifting && kart.Speed > 5 ? .13f : 0;
            skid.pitch = 1 + kart.Speed / 40;
            wind.volume = audible ? Mathf.Clamp01((kart.Speed - 12) / (downhill ? 43 : 28)) * (kart.Boosting ? .18f : downhill ? .15f : .09f) : 0;
            wind.pitch = .7f + kart.Speed / 60;
        }

        public void Toggle()
        {
            Muted = !Muted;
            if (!Muted) return;
            foreach (var source in pool) source.Stop();
            queued.Clear();
            engine.volume = skid.volume = wind.volume = hum.volume = 0;
        }

        AudioSource Loop(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.clip = clip; source.loop = true; source.volume = 0; source.Play();
            return source;
        }

        static AudioClip Noise(string name)
        {
            const int count = 22050; var data = new float[count]; var random = new System.Random(71); float low = 0;
            for (int i = 0; i < count; i++) { low = Mathf.Lerp(low, (float)random.NextDouble() * 2 - 1, .18f); data[i] = low * .65f; }
            var clip = AudioClip.Create(name, count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }

        static AudioClip BoostSound(bool strong)
        {
            int count = strong ? 15435 : 9922; var data = new float[count];
            var random = new System.Random(strong ? 103 : 91); float noise = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / 22050f, p = (float)i / count;
                noise = Mathf.Lerp(noise, (float)random.NextDouble() * 2 - 1, .4f);
                float envelope = Mathf.Min(1, p * 18) * Mathf.Pow(1 - p, 1.6f);
                data[i] = (noise * .85f + Mathf.Sin(2 * Mathf.PI * (100 * t + 220 * t * t)) * .22f) * envelope;
            }
            var clip = AudioClip.Create(strong ? "Super sugar rush" : "Sugar rush", count, 1, 22050, false);
            clip.SetData(data, 0); return clip;
        }

        void OnDestroy() { foreach (var clip in new[] { boost, superBoost, skidClip, windClip }) if (clip) Destroy(clip); }
    }
}
```

- [x] **Step 3: Expose the screen state**

In `Assets/CottonCircuit/Scripts/TitleScreenUI.cs` after the field `bool entering, confirming;` add:

```csharp

        public bool StoryShowing => intro != null;
```

In `Assets/CottonCircuit/Scripts/GameController.cs` after line 29 (`public Product SelectedProduct => ...`) add:

```csharp
        public MusicScene MusicScene => new MusicScene
        {
            Title = titleScreen, Story = titleScreen && titleScreen.StoryShowing, InGame = Session != null,
            Tutorial = TutorialActive, Progression = HasProgression, ShiftExists = Shift != null,
            ShiftOpen = Shift != null && Shift.DayRunning,
            Phase = HasProgression ? Session.Economy.Progression.Phase : BusinessPhase.Operating,
            Mode = Session != null ? Session.Mode : GameMode.Shop,
            Machine = HasProgression ? Shift.SelectedMachine : 0,
            RemainingSeconds = Shift != null ? Shift.State.RemainingSeconds : 0
        };
```

- [x] **Step 4: Raise a click event for authored buttons**

In `Assets/CottonCircuit/Scripts/ToolkitUI.cs` add inside the class, before `Open`:

```csharp
        // Buttons with this class play their own action sound instead of the click.
        public const string QuietClick = "quiet-click";
        public static event Action ButtonPressed;
```

In `Open`, after `document.rootVisualElement.pickingMode = PickingMode.Ignore;` add:

```csharp
            document.rootVisualElement.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            document.rootVisualElement.RegisterCallback<NavigationSubmitEvent>(OnSubmit, TrickleDown.TrickleDown);
```

and add these methods to the class:

```csharp
        static void OnClick(ClickEvent evt) => Pressed(evt.target as VisualElement);
        static void OnSubmit(NavigationSubmitEvent evt) => Pressed(evt.target as VisualElement);
        static void Pressed(VisualElement target)
        {
            var button = target as Button ?? target?.GetFirstAncestorOfType<Button>();
            if (button != null && button.enabledInHierarchy && !button.ClassListContains(QuietClick)) ButtonPressed?.Invoke();
        }
```

- [x] **Step 5: Compile through the core tests and a Unity import**

The old `Audio.Play(int)` calls still exist, so Unity will not compile yet. Do Task 5 Step 1-3 now, then run the build in Task 5.

---

### Task 5: Sound call sites

**Files:**
- Modify: `Assets/CottonCircuit/Scripts/GameController.cs`, `ShiftController.cs`, `ProgressionController.cs`, `ToolkitDragController.cs`, `TutorialOverlayUI.cs`, `Assets/Resources/UI/Business.uxml`

**Interfaces:**
- Consumes: `AudioFeedback.Play/PlaySale/PlayClosing`, `Sound`, `ToolkitUI.QuietClick`.

- [x] **Step 1: Replace the numbered calls**

`GameController.cs`:
- In `Step`, change `int boosts = World.Kart.DriveModel.BoostCount;` to `int boosts = World.Kart.DriveModel.BoostCount, hits = World.Kart.DriveModel.WallHits;` and after `if (World.Kart.DriveModel.BoostCount > boosts) Audio.PlayBoost(World.Kart.DriveModel.BoostTier);` add `if (World.Kart.DriveModel.WallHits > hits) Audio.Play(Sound.WallHit);`
- `World.ShowInventory(Session.Economy); Audio.Play(2); Save();` → `World.ShowInventory(Session.Economy); Audio.Play(Sound.CandyExtract); Save();`
- `World.CentralCandy.Show(null); Audio.Play(0); SyncMode();` → `World.CentralCandy.Show(null); Audio.Play(Sound.EngineStart); SyncMode();`
- In `Serve`: `Audio.Play(1); Notify("직접 전달했어요!` → `Audio.PlaySale(false); Notify("직접 전달했어요!`
- In `DiscardSelected`: after `Session.Economy.Discard(SelectedProductId);` add `Audio.Play(Sound.Trash);`
- In `SyncMode`: `Audio.UpdateDriving(World.Kart, false); Audio.Play(2);` → `Audio.UpdateDriving(World.Kart, false); Audio.Play(Sound.ClosingJingle);`
- In `BuyUpgrade` and `BuyShelf`: `Audio.Play(1)` → `Audio.Play(Sound.Purchase)`.

`ShiftController.cs`:
- In `StepShift` after `World.Kart.Drive(throttle, steering, brake, dt, drift, boost);` add `if (!worker && drive.WallHits > hits) Audio.Play(Sound.WallHit);`
- In the closing block `if (!active) { World.Kart.Stop(); ...` add `Audio.PlayClosing();` as its first statement.
- `ExtractCandy`: `Audio.Play(2);` → `Audio.Play(Sound.CandyExtract);`
- `ResumeCandy`: `Audio.Play(2);` → `Audio.Play(Sound.CandyDrop);`
- `DeliverCandy`: replace

```csharp
            if (delivery == DeliveryResult.Rejected)
            {
                if (product != null && ShopShift.SizeOf(product) < 0) Notify("아직 팔 수 없어요. 주행 화면에서 더 키우거나 쓰레기통에 버리세요.");
                return delivery;
            }
            SelectedProductId = null;
            if (delivery == DeliveryResult.Sold)
            {
                Audio.Play(1);
```

with

```csharp
            if (delivery != DeliveryResult.Sold) Audio.Play(Sound.DeliverFail);
            if (delivery == DeliveryResult.Rejected)
            {
                if (product != null && ShopShift.SizeOf(product) < 0) Notify("아직 팔 수 없어요. 주행 화면에서 더 키우거나 쓰레기통에 버리세요.");
                return delivery;
            }
            SelectedProductId = null;
            if (delivery == DeliveryResult.Sold)
            {
                Audio.PlaySale(starBonus > 0);
```

- `TrashCandy`: after the guard line add `Audio.Play(Sound.Trash);`

`ProgressionController.cs`:
- `PurchaseNode`: `Audio.Play(1); Save(); UI.Refresh();` → `Audio.Play(Sound.Purchase); Save(); UI.Refresh();`
- `BeginBusiness`: after `if (!PreparationActions || !Shift.BeginBusiness()) return;` add `Audio.Play(Sound.EngineStart);`

- [x] **Step 2: Drag, sugar and tutorial sounds**

`ToolkitDragController.cs` (`GameUI` partial):
- In `BeginBusinessDrag`, before `MoveBusinessDrag(pointerId, position); return true;` add `if (!sugar) game.Audio.Play(Sound.CandyDrop);`
- In `MoveBusinessDrag`, first line inside `if (shake && game.PourSugar(businessDragFlavor, businessShake.Amount)) {` add `game.Audio.Play(Sound.SugarShake);`
- In `DropBusinessDrag`, replace the final `return DeliveryResult.Rejected;` (after the customer loop) with `game.Audio.Play(Sound.CandyDrop); return DeliveryResult.Rejected;`

`TutorialOverlayUI.cs`:
- Add field `string shownBubble;` next to `bool driveCoachDismissed;`.
- In `Refresh`, right after `ToolkitUI.Show(view, visible);` add:

```csharp
            string bubble = !visible ? null : game.GrowthHintVisible ? "growth" : step == TutorialStep.Success ? "success" : step.ToString();
            if (bubble != null && bubble != shownBubble) game.Audio.Play(Sound.TutorialPopup);
            shownBubble = bubble;
```

`Assets/Resources/UI/Business.uxml` line 37: add `quiet-click` to the extract button's classes:

```xml
            <ui:Button focusable="false" name="businessExtract" text="꺼내기  F" class="biz-button primary quiet-click" />
```

- [x] **Step 3: Confirm no numbered call remains**

Run: `grep -rn "Audio.Play([0-9]" Assets/CottonCircuit/Scripts`
Expected: no output.

- [x] **Step 4: Build the development player (compiles, rebuilds the scene, runs IntegrationChecks)**

Run: `powershell -File Tools/build.ps1 -BuildFolder Builds/UIToolkit`
Expected: `Build ready: ...\Builds\UIToolkit\CottonCircuit.exe`; `Logs/build.log` contains the new `COTTON_CHECK_PASS sound bank has ...` lines and no `COTTON_CHECK_FAILED`.

- [x] **Step 5: Commit Tasks 3-5**

```bash
git add Assets/CottonCircuit/Scripts Assets/CottonCircuit/Editor Assets/CottonCircuit/Data/SoundBank.asset Assets/CottonCircuit/Data/SoundBank.asset.meta Assets/CottonCircuit/Scenes Assets/Resources/UI/Business.uxml
git commit -m "Play the chosen music per screen and machine with real effects"
```

---

### Task 6: Smoke checks, verification and docs

**Files:**
- Modify: `Assets/CottonCircuit/Tests/ToolkitRuntimeSmoke.cs`
- Create: `docs/audio-verification.md`

- [x] **Step 1: Add audio assertions to the existing flow**

Checks compare `game.Audio.Played(...)` before and after an action, because one action can trigger several sounds (a sale is followed by the success bubble).

In `Scenario()`:
- After `CheckScreenBounds("TitleScreen");` add `Check(game.Audio.Music == MusicCue.Title, "title plays the title theme");`
- Replace `yield return Click("TitlePrimaryButton");` (the first one, before the story) with:

```csharp
            int titleClicks = game.Audio.Played(Sound.UiClick);
            yield return Click("TitlePrimaryButton");
```

  and after `Check(game.Session == null && Visible(Find("IntroScreen")), ...)` add `Check(game.Audio.Music == MusicCue.Story && game.Audio.Played(Sound.UiClick) > titleClicks, "the story theme follows a clicked start button");`
- After `Check(game.TutorialStep == TutorialStep.PourSugar && game.TutorialActive, ...)` add `Check(game.Audio.Music == MusicCue.Tutorial && game.Audio.Played(Sound.TutorialPopup) > 0, "the tutorial plays its theme and the bubble sound");`

In `Tutorial()`:
- Replace `yield return Click("businessExtract");` with:

```csharp
            int pops = game.Audio.Played(Sound.CandyExtract), extractClicks = game.Audio.Played(Sound.UiClick);
            yield return Click("businessExtract");
            Check(game.Audio.Played(Sound.CandyExtract) == pops + 1 && game.Audio.Played(Sound.UiClick) == extractClicks,
                "extracting plays the pop without the button click");
```

- Before `Pick(item, item.worldBound.center);` add `int sales = game.Audio.Played(Sound.DeliverSuccess) + game.Audio.Played(Sound.StarBonus);` and after `Check(!game.UI.BusinessDragActive && game.TutorialStep == TutorialStep.Success, ...)` add `Check(game.Audio.Played(Sound.DeliverSuccess) + game.Audio.Played(Sound.StarBonus) == sales + 1, "a sale plays one success chime");`
- After `Check(!game.TutorialActive && game.TutorialStep == TutorialStep.Complete, ...)` add `Check(game.Audio.Music == MusicCue.Machine1, "the first machine song follows the tutorial");`
- Replace the first `yield return CloseDay();` in `Tutorial()` with:

```csharp
            int bells = game.Audio.Played(Sound.ClosingBell);
            yield return CloseDay();
            Check(game.Audio.Music == MusicCue.None && game.Audio.Played(Sound.ClosingBell) == bells + 1, "closing rings once and settlement is quiet");
```

- After `Check(!game.GrowthHintVisible && Visible(Find("PreparationScreen")), ...)` add `Check(game.Audio.Music == MusicCue.Preparation, "preparation plays its theme");`

In `PauseTitleResume()`:
- After `Check(game.Session == null && Element<Button>("TitlePrimaryButton").text == "이어하기", ...)` add `Check(game.Audio.Music == MusicCue.Title, "returning to the title restores the title theme");`

In `PreparationAndWorkers()`:
- Change `int before = economy.Coins;` to `int before = economy.Coins, purchases = game.Audio.Played(Sound.Purchase);` and after `Check(Progression.Level(economy, node) == 1 && economy.Coins < before, "authored upgrade button buys " + node);` add `Check(game.Audio.Played(Sound.Purchase) == purchases + 1, "buying " + node + " plays the purchase sound");`
- After `Check(game.InBusiness && economy.Day == 2 && !game.GrowthHintVisible, ...)` add `yield return Settle(); Check(game.Audio.Music == MusicCue.Machine1, "business opens on the first machine song");`
- After `yield return Click("businessMachine1");` add `Check(game.Audio.Music == MusicCue.Machine2, "switching to the soda machine switches its song");`

- [x] **Step 2: Rebuild and run every smoke case and core test**

```bash
powershell -File Tools/test-core.ps1
powershell -File Tools/build.ps1 -BuildFolder Builds/UIToolkit
powershell -File Tools/verify-uitk.ps1 -Case full
powershell -File Tools/verify-uitk.ps1 -Case legacy
powershell -File Tools/verify-uitk.ps1 -Case rack
powershell -File Tools/verify-uitk.ps1 -Case hud
```

Expected: core `0 failed`; each smoke `PASSED` with no runtime error logs.

- [x] **Step 3: Write `docs/audio-verification.md`**

Record: the command outputs above (pass counts), the chosen files per cue and event, the loop windows, and what still needs a human ear (loop wraps of Secret Stage!/Time Trial, relative loudness, engine pitch feel). Link `Assets/CottonCircuit/Audio/THIRD-PARTY.md` and list the unconfirmed licenses.

- [x] **Step 4: Commit**

```bash
git add Assets/CottonCircuit/Tests/ToolkitRuntimeSmoke.cs docs/audio-verification.md docs/superpowers/plans/2026-09-28-game-audio.md
git commit -m "Check music and effects in the runtime smoke flow"
```

## Review follow-up (2026-09-28)

An independent read-only review of the branch found no crash or missing wiring and five issues, all confirmed against the code and fixed:

- [x] Clicks were lost on buttons whose action closes or disables their document (이어하기, 건너뛰기, 새 게임), because `ClickEvent` arrives after the action. A trickle-down `PointerUpEvent` was tried first and never reached the document root: the release goes only to the button that captured the pointer. `ToolkitUI` now clicks on press (trickle-down `PointerDownEvent`, left button, enabled button); keyboard submit is unchanged.
- [x] The loop restart was frame-polled on a streamed clip and a long hitch could stop a song for good. `MusicPlayer` now schedules the restart on the audio clock (`PlayScheduled` at `LoopEnd`) with a 0.15 s tail on the old deck, restarts at once if the end already passed or the deck stopped, and music imports as compressed-in-memory (`AudioImportRules` version 2) so `timeSamples` seeks are exact.
- [x] A deck that was still fading could be cut. `MusicPlayer` uses three decks, takes the quietest free one, and fades a still-audible copy of the requested song back in instead of restarting it.
- [x] Resuming from pause replayed the tutorial bubble sound. `TutorialOverlayUI` no longer updates the shown bubble while paused.
- [x] One-off action sounds (purchase, extract, sale, coins, trash, closing) had a 0.04 s repeat limit that could hide a counted purchase at very high frame rates. Only burst sounds keep a limit now.
- [x] A new `music` smoke case (`Tools/verify-uitk.ps1 -Case music`) moves the soda machine song 0.8 s before its loop end and checks the scheduled restart position, the old deck stopping, and recovery after the playing deck is stopped.

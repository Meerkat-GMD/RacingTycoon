# Cotton Circuit Implementation Plan

> **For agentic workers:** Use subagent-driven-development for the independent Blender asset task and requesting-code-review for final review; execute the tightly coupled gameplay integration locally. Track evidence in `docs/progress.md`.

**Goal:** Deliver a playable Windows Unity game where driving inside a candy machine winds candy around a central stick, then sells it to fund upgrades.

**Architecture:** A pure C# domain owns winding samples, products, money and purchases. Unity components provide orbital kart input, sample-based candy meshes, an authored miniature world and Korean uGUI. An editor builder imports Blender FBX, assembles one scene, verifies it and builds Windows.

**Tech Stack:** Unity 6000.5.3f1 Built-in pipeline, C#, uGUI, Blender 5.2.2 LTS Python/FBX, local JSON saves.

## Global Constraints
- Work only in `D:\UnityProjects\RacingTycoon`; never operate on the MCP-connected Hell-o-World project.
- Actual Blender-authored models must be used; keep `.blend`, generation script and FBX.
- Use the user's selected central-stick winding mechanic.
- Korean UI; W accelerate, S/Space brake, A/D change lane, Enter finish, R reset, Escape pause.
- 60-second runs; three flavors/lanes; three upgrades with three levels each.
- Test meaningful economic/production behavior before implementing it. No source-string tests.
- Preserve other Unity processes. Keep generated builds and Library out of version control.

## Task 1: Domain and runnable Unity foundation
Files: `Assets/CottonCircuit/Scripts/Core/{Production,Economy}.cs`, `Tools/Tests/CoreTests.cs`, `Tools/test-core.ps1`, `Assets/CottonCircuit/Tests/Editor/CoreTests.cs`, project manifest/settings.
Interfaces: `Production.Advance(double radians, double radius, int flavor)` returns new sample count; `Product` stores sample list and grams; `Economy.CompleteRun(Product)`, `SellNext()`, `BuyUpgrade(int)` change owned state and return success/value. Capacity and maximum speed derive from upgrades.
- [ ] Initialize empty project and local feature branch; preserve initial docs.
- [ ] Create failing tests for a stationary run, one full revolution, flavor/radius retention, capacity, duplicate completion, inventory, insufficient money, sale and upgrade accounting, timestep-independent results.
- [ ] Run tests with Unity bundled C# compiler/Mono; inspect intentional failures.
- [ ] Implement domain until those tests pass; store output in `Logs/core-tests.txt`.
- [ ] Create lightweight Unity project, configure Windows name/input/display, commit this unit.

## Task 2: Blender asset kit (independent of domain)
Files: `Art/Blender/create_assets.py`, `Art/Blender/CottonCircuit.blend`, `Art/Blender/asset-manifest.json`, `Assets/CottonCircuit/Models/*.fbx`, `Art/Blender/preview.png`.
Contract: Unity units are meters, Y up after FBX, kart +Z forward. Named assets `Kart`, `Kiosk`, `Spinner`, `Puff`, `Customer`, `Crystal`, `Arch`, `Tree`, `Lamp`. Materials use semantic color names. Kart wheels have separate named objects. Rooted at ground origin. Agent receives concrete size/palette brief.
- [ ] Write asset validation for nonempty objects, sane dimensions, named materials, wheel pivots and FBX outputs.
- [ ] Generate attractive rounded low-poly kit using Blender Python and export each asset separately.
- [ ] Save one editable blend with collections and an asset overview render.
- [ ] Run validation, inspect render, correct issues, commit scoped art files.
- [ ] Review dimensions/material mappings against Unity integration.

## Task 3: Playable scene and presentation
Files: `Assets/CottonCircuit/Scripts/{GameController,KartController,CandyView,WorldView,GameUI,AudioFeedback,SaveStore}.cs`, `Assets/CottonCircuit/Editor/ProjectBuilder.cs`.
Consume Task 1 domain and Task 2 models. Controller owns Shop/Racing/Results state; a result is committed once. Saving uses serializable domain records. Lane radius 7.5/10/12.5; angle controls orbital position, each color's radius changes candy thickness.
- [ ] Add smoke specification that scene has kart/model references, camera, canvas and controller; play through a scripted real run → result → shop → sale → purchase with isolated save data.
- [ ] Assemble dish, lanes, spinner, kiosk, landscaping, kart and customer from prefabs and geometric set dressing.
- [ ] Implement keyboard orbital racing, pause, speed/position feedback and central thread. Prevent reset jumps from feeding production.
- [ ] Render candy from retained sample angle/flavor/radius with capped geometry; use same samples for result and display products.
- [ ] Implement Korean uGUI shop/upgrades, race HUD, result card, help/pause, save errors and new-game recovery.
- [ ] Generate small audio cues; support mute.
- [ ] Compile, run smoke scenario, capture shop/race/result screenshots and inspect at 1280x720 and 1920x1080.

## Task 4: Verification and delivery
Files: `README.md`, `docs/verification.md`, `Builds/Windows/CottonCircuit.exe`, `Tools/build.ps1`.
- [ ] Re-run core tests and Unity editor scene checks, confirm no compilation/runtime errors.
- [ ] Run isolated smoke scenario including empty run, capacity completion, repeat completion prevention, sale, upgrade, save/reload.
- [ ] Request code review, resolve material findings and run affected checks.
- [ ] Build Windows executable; launch verification mode and inspect captures/logs.
- [ ] Write real results and limitations, update progress ledger, commit source and open playable result.

## Concrete domain checks
```csharp
var p = new Production(120);
p.Advance(0, 10, 1);
Assert(p.Grams == 0, "stationary cart must not produce");
p.Advance(Math.PI * 2, 10, 1);
Assert(p.Samples.Count > 0 && p.Grams > 0, "revolution must wind cotton");
var e = new Economy();
var before = e.Coins;
Assert(!e.BuyUpgrade(0) || e.Coins < before, "upgrade must debit money");
```
Tests also cover exact observed products/currency, bounded input, and state transitions. Visual checks verify product winding around the stick rather than drawing a disconnected floor trail.

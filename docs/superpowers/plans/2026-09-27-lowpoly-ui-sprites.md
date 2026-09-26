# Lowpoly UI Sprite Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. The user authorised autonomous execution on 2026-09-27 ("이 두 캐릭터를 바탕으로 나머지 작업 진행해봐"); do not pause for approvals between tasks.

**Goal:** Replace every procedurally drawn 2D picture and the painted NPC portrait in Cotton Circuit with 66 Blender-rendered "pastel toy" lowpoly sprites that match the two approved customers, and wire them into the runtime uGUI without changing layout or behaviour.

**Architecture:** A bpy-free catalog module (`ui_sprite_spec.py`) is the single source of truth for the 66 sprite ids, canvases, kinds and validation rules. Per-category Blender modules build and render assets through one shared driver and one shared contact-shadow renderer, so parallel workers only add their own files. Unity loads the PNGs as Sprites through an editor catalog that fills `GameAssets` inside `ProjectBuilder.CreateScene`; runtime art components become `UnityEngine.UI.Image` subclasses that keep today's public API, hierarchy names and raycast behaviour.

**Tech Stack:** Blender 5.2.2 (Cycles, background mode `-b`), Python 3.13 with Pillow 12.3 and numpy 2.5 (plain `unittest`, no pytest), Unity 6000.5.3f1 URP with uGUI, C# console tests compiled by Unity's bundled Mono `mcs`, Windows development-player smokes.

**Spec:** `docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md` (read it fully before any task; it wins over this plan on conflicts).

## Global Constraints

- Style: flat-shaded lowpoly, no outlines, no image textures on 3D assets; hand-authored broad facets like the approved characters; icospheres only for round props.
- Palette (sRGB, material base colours): Strawberry `#F48DAB`, Soda `#7ACDCE`, Vanilla `#F9D27D`, Navy `#29324D`, Cream `#FFF1D4`, White `#FFF9ED`, Mint `#99C4AE`, Plum `#6C577F`, Gold `#DBAE61`, Wood `#D59C79`, Tire `#414059`, Base `#9DBBAF`, Skin1/2/3 `#F1C6A6`/`#B87A65`/`#E8AF88`, Hair1/2/3 `#574F59`/`#8D5B4A`/`#45475B`, Pants1/2/3 `#7C789D`/`#65688B`/`#5F968F`, Magenta `#C2577E` (Mina's hair only). No other material colours.
- Material recipe for every new asset: Principled BSDF, Roughness 0.85, Specular IOR Level 0.08.
- Lights: key (-2,-3,7) 900 W size 4 `#FFF1DD`; fill (4.5,-3.5,3) 250 W size 5 `#DDE6FF`; rim (1.5,3,5) 70 W size 3 `#FFFFFF`; environment `#F3ECF7` strength 0.75; all DISK area lights aimed at (0,0,1.25). Use `ui_sprite_common.studio()` unchanged.
- Camera: orthographic, yaw -20°, elevation 15° (`ui_sprite_common.camera`). Trait icons and emotes: |yaw| ≤ 10°.
- Render: Cycles 64 samples, denoise on, adaptive sampling off, seed 260927, view transform `Standard`, look `None`, exposure -1.8, gamma 1, film transparent, RGBA 8-bit PNG. Opaque kinds (locations, portrait) keep film transparent off by filling the frame with geometry, never by relying on the world colour.
- Canvas = 2 × primary display size, rounded up to a multiple of 4 (exact values in the catalog below). Shadowed kinds keep ≥ 2 px fully transparent margin on every edge and the contact shadow inside the canvas.
- Contact shadows only through `ui_sprite_render.render_sprite(..., shadow=...)`, which must stay pixel-identical to Codex's `create_ui_sprites.render_contact_sprite` with default parameters.
- Approved sources are frozen: `Art/Blender/GameCustomerFaceted/Customer_01_Faceted*.png` and `Art/Blender/GameCustomerFemaleExplorer/Customer_02_Explorer*.png` must stay byte-identical; `Customer_V0_Neutral` and `Customer_V2_Neutral` must be pixel-identical to them.
- Unity: all art `Image` components use `raycastTarget = false` except the trait `NodeCircle` disc (Button target). Hierarchy names stay exactly as today. Layout rects stay exactly as today.
- Import settings for everything under `Assets/CottonCircuit/Sprites/`: Sprite (2D and UI), Single, mipmaps off, alpha is transparency on, compression High Quality.

## Working agreements for every executor

- Branch `claude/lowpoly-ui-sprites` in the main checkout `D:/UnityProjects/RacingTycoon`. **Workers never run `git add`, `git commit`, `git stash`, `git checkout -- <file>` or `git reset`.** The orchestrator commits each reviewed task by explicit path.
- Do not touch these pre-existing uncommitted changes: `Assets/CottonCircuit/Scripts/Core/ArcadeDrive.cs`, `Assets/CottonCircuit/Scripts/Core/RaceCourse.cs`, `Tools/Tests/MapTests.cs`, `README.md` (until Task 11), `ProjectSettings/*`, `.claude/`, `reports/`, `research_notes/`, `yt.html`, `Art/Blender/CodexCharacters*/`, `Art/Blender/GameCustomerFemaleFaceted/`, `Art/Blender/ReferenceAdventurer/` (read-only reference: its `head.py:88` builds brow geometry).
- Never talk to, save through, or kill the running Blender MCP session (PID 16680, port 9876). Use background Blender only:
  `& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --factory-startup --python-exit-code 1 -P <script> -- <args>` (PowerShell) or the same with `"/c/Program Files/Blender Foundation/Blender 5.2/blender.exe"` (Git Bash).
- CPU is a 6-core / 12-thread Ryzen 5 5600. Set `UI_SPRITE_THREADS=4` in the environment for every Blender render except Task 2's customer renders (which must use the default 8 so they match the approved PNGs).
- Each art task owns only the files listed in its **Files** block. Shared modules from Task 1 are read-only for Tasks 2–8; report needed changes instead of editing them.
- Look at every PNG you produce (open it as an image) at full size and at display size before claiming completion.

## File structure

Blender side (`Art/Blender/`):

| Path | Responsibility | Task |
|---|---|---|
| `ui_sprite_spec.py` | bpy-free constants: palette, lighting, exposure, material recipe, kind rules, the 66-entry `CATALOG`, `TRAIT_ICONS`, path helpers | 1 |
| `ui_sprite_common.py` | existing bpy helpers; re-exports spec constants; window-safe `setup_scene`; `UI_SPRITE_THREADS` override | 1 (modify) |
| `ui_sprite_render.py` | `render_sprite()` plain or contact-shadow render, numpy compositor | 1 |
| `create_ui_sprites.py` | reduced to the compatibility wrapper `render_contact_sprite` + module global `ART` used by the approved generators | 1 (modify) |
| `toy_kit.py` | prefix-parameterised copy of `customer_lib.py` primitives with the canonical material recipe | 1 |
| `build_ui_sprites.py` | driver: `-- --category <name> [--only id,id] [--out-root dir] [--no-render]` | 1 |
| `ui_sprites/<category>.py` | one module per category: `ASSETS` list + `build(asset, col, kit)` | 4–8 |
| `ui_sprites/<category>.blend`, `ui_sprites/<category>.manifest.json` | per-category editable source and manifest written by the driver | 4–8 |
| `validate_ui_sprites.py` | generalised validator over the catalog, kind rules, manifests; `--complete` gate | 1 |
| `preview_ui_sprites.py` | per-category and full boards at display size on cream, ink and trait-disc colours | 1 |
| `tests/test_ui_sprite_spec.py`, `tests/test_render_identity.py`, `tests/test_customer_sprites.py` | unittest suites | 1, 2 |
| `render_customer_sprites.py` | renders the 6 customer sprites from the three customer `.blend` files | 2 |
| `GameCustomerChild/` | new child customer generator (V1) | 2 |
| `MinaPortrait/` | Mina bust generator and backdrop | 3 |
| `UI_SPRITES.md` | pipeline documentation, rewritten for the new layout | 1, 11 |

Unity side (`Assets/CottonCircuit/`):

| Path | Responsibility | Task |
|---|---|---|
| `Sprites/<Folder>/<id>.png` (+ `.meta`) | 66 sprites; folders Customers, Shop, Items, Icons, Locations, Machines, Characters | 2–8 |
| `Scripts/Core/TraitIcons.cs` | Unity-free explicit node-id → icon-sprite-id table | 9 |
| `Scripts/GameAssets.cs` | new sprite fields | 9 |
| `Editor/SpriteImport.cs` | `AssetPostprocessor` enforcing import settings under `Sprites/` | 9 |
| `Editor/SpriteCatalog.cs` | loads every sprite by id and assigns `GameAssets` fields | 9 |
| `Editor/ProjectBuilder.cs`, `Editor/IntegrationChecks.cs` | call `SpriteCatalog.Assign`; sprite reference checks | 9 |
| `Scripts/UiArt.cs` | runtime sprite lookup helpers and pivot constants | 10 |
| `Scripts/ShopArtGraphic.cs`, `ShopStreetGraphic.cs`, new `StreetSprite.cs`, `ProgressionArtGraphic.cs`, `ShiftUI.cs`, `ShopStreetUI.cs`, `CandyRackUI.cs`, `OutgameUI.cs`, `OutgameTraitsUI.cs`, `GameUI.cs` | runtime swap | 10 |
| `Tests/*RuntimeSmoke.cs` | follow the new component types; add Locations 1280×720 capture | 10 |
| `Resources/Progression/NpcPortrait.png(.meta)`, `Resources/` folder | deleted | 10 |

Tools and docs: `Tools/Tests/TraitIconTests.cs`, `Tools/test-trait-icons.ps1` (Task 9), `Tools/compare-captures.py` (Task 11), `docs/lowpoly-sprites-verification.md` (Task 11), `README.md` (Task 11).

## The sprite catalog (source of truth, copied into `ui_sprite_spec.py` in Task 1)

| Owner task | Ids | Folder | Kind | Canvas | Display sizes |
|---|---|---|---|---|---|
| 2 customers | `Customer_V{0,1,2}_{Neutral,Angry}` | Customers | character | 164×280 | 82×140 |
| 3 mina | `Mina_Portrait` | Characters | portrait | 808×784 | 404×391 |
| 4 items | `CottonCandy_{Strawberry,Soda,Vanilla}_{Small,Medium,Large}` | Items | prop | 156×176 | 77×88, 80×77 |
| 4 items | `BaggedCandy_{Strawberry,Soda,Vanilla}_{Small,Medium,Large}` | Items | prop | 148×184 | 74×92, 84×98, 62×86, 118×124 |
| 4 items | `SugarBag_{Strawberry,Soda,Vanilla}` | Items | prop | 164×200 | 82×100, 118×124 |
| 5 shop | `Storefront` | Shop | prop | 340×460 | 169×230 |
| 5 shop | `Trash` | Shop | prop | 136×164 | 68×82 |
| 5 shop | `Emote_Heart`, `Emote_Angry` | Shop | emote | 144×132 | 72×66 |
| 6 locations | `Location_{0,1,2,3}_Prep` | Locations | scene | 1176×708 | 588×354 |
| 6 locations | `Location_{0,1,2,3}_Street` | Locations | scene | 1192×628 | 596×314 |
| 7 machines | `Machine_{0,1,2}` | Machines | prop | 252×252 | 126×126 |
| 8 icons | 23 `Trait_*` ids (spec icon table) | Icons | icon | 84×84 | 42×42 |

Pivots (normalised, origin bottom-left, shared by art and runtime): `CottonCandy_*` stick base at (0.5, 0.06); `BaggedCandy_*` tie knot at (0.5, 0.14).

Variant mapping: V0 = approved male (Soda jacket), V1 = new child (Strawberry, Hair2, Skin2, Pants2), V2 = approved female explorer (cap). Location index: 0 동네 골목, 1 시장 앞, 2 강변 축제, 3 별빛 광장. Machine index: 0 basic, 1 soda, 2 premium.

---

### Task 1: Sprite pipeline foundation

**Files:**
- Create: `Art/Blender/ui_sprite_spec.py`, `Art/Blender/ui_sprite_render.py`, `Art/Blender/toy_kit.py`, `Art/Blender/build_ui_sprites.py`, `Art/Blender/ui_sprites/__init__.py`, `Art/Blender/tests/__init__.py`, `Art/Blender/tests/test_ui_sprite_spec.py`, `Art/Blender/tests/test_render_identity.py`, `Art/Blender/tests/fixtures/ui_sprites_fixture.py`
- Modify: `Art/Blender/ui_sprite_common.py`, `Art/Blender/create_ui_sprites.py`
- Rewrite: `Art/Blender/validate_ui_sprites.py`, `Art/Blender/preview_ui_sprites.py`, `Art/Blender/UI_SPRITES.md`
- Delete (obsolete three-sample pipeline): `Art/Blender/UiSprites.blend`, `Art/Blender/ui-sprites-manifest.json`, `Art/Blender/ui-sprites-scene-audit.json`, `Art/Blender/ui-sprites-reproducibility.json`, `Art/Blender/audit_ui_sprite_scene.py`, `Art/Blender/ui-sprites-native.png`, the three sample passes in `Art/Blender/ui-sprite-passes/` (`CottonCandy_Strawberry_Medium-*`, `Customer_01_Neutral-*`), and the untracked `Assets/CottonCircuit/Sprites/{Customers/Customer_01_Neutral.png,Items/CottonCandy_Strawberry_Medium.png,Icons/Trait_Hours.png}`. Their builder code stays reachable in commit `966cf73` (`git show 966cf73:Art/Blender/create_ui_sprites.py`) for Tasks 4 and 8.

**Interfaces:**
- Produces `ui_sprite_spec`: `PALETTE: dict[str,str]`, `LIGHTING`, `EXPOSURE = -1.8`, `SEED = 260927`, `MATERIAL = {'roughness': .85, 'specular_ior_level': .08}`, `KIND_RULES: dict[str, dict]`, `CATALOG: list[dict]` (keys `id, folder, kind, canvas, displays, owner, shadow, pivot`), `TRAIT_ICONS: list[str]`, `by_id() -> dict[str, dict]`, `sprite_path(entry) -> str` (`Assets/CottonCircuit/Sprites/<folder>/<id>.png`), `owned_by(owner) -> list[dict]`.
- Produces `ui_sprite_render.render_sprite(scene, sprite_id: str, output: Path, passes_dir: Path, shadow: dict|None, size: tuple[int,int]) -> None` and `DEFAULT_SHADOW = {'anchor': (.07, .10, 0), 'radii': (.30, .068), 'max_alpha': .32}`.
- Produces `toy_kit.Kit(prefix: str)` with methods `mat(name)`, `collection(scene, name)`, `mesh(col, name, verts, faces, color)`, `box`, `ico`, `segment`, `loft`, `panel`, `band`, `buckle` — same signatures as `GameCustomerFaceted/customer_lib.py`, material recipe from `MATERIAL`.
- Produces category-module contract used by Tasks 4–8:
  ```python
  CATEGORY = 'items'                       # matches catalog owner
  ASSETS = [{'id': 'CottonCandy_Strawberry_Small',
             'camera': {'target': (0, 0, 1.2), 'scale': 2.9, 'yaw': -20, 'elevation': 15},
             'shadow': {'anchor': (.07, .10, 0), 'radii': (.30, .068), 'max_alpha': .32},  # or None
             'params': {...}}]               # free-form, recorded in the manifest
  def build(asset: dict, col, kit) -> None:  # add all geometry for one asset into col
  ```
- Driver CLI: `build_ui_sprites.py -- --category items [--only a,b] [--out-root D:/tmp/x] [--module-path dir] [--no-render]` builds one scene per asset (`UIS_<id>`), camera from `asset['camera']`, a catcher only when `asset['shadow']`, renders to `sprite_path(entry)` under `--out-root` (default project root), saves `ui_sprites/<category>.blend` and `ui_sprites/<category>.manifest.json`, prints `UI_SPRITE_RENDERED <id> <path>` per asset.

- [ ] **Step 1: Write the failing spec test** — `Art/Blender/tests/test_ui_sprite_spec.py`:

```python
import sys, unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import ui_sprite_spec as s

class CatalogTests(unittest.TestCase):
    def test_count_and_unique_ids(self):
        ids = [e['id'] for e in s.CATALOG]
        self.assertEqual(len(ids), 66)
        self.assertEqual(len(set(ids)), 66)

    def test_owner_counts(self):
        counts = {o: len(s.owned_by(o)) for o in ('customers', 'mina', 'items', 'shop', 'locations', 'machines', 'icons')}
        self.assertEqual(counts, {'customers': 6, 'mina': 1, 'items': 21, 'shop': 4, 'locations': 8, 'machines': 3, 'icons': 23})

    def test_canvas_rule(self):
        for e in s.CATALOG:
            w, h = e['canvas']; dw, dh = e['displays'][0]
            self.assertEqual((w % 4, h % 4), (0, 0), e['id'])
            self.assertGreaterEqual(w, 2 * dw, e['id']); self.assertGreaterEqual(h, 2 * dh, e['id'])
            self.assertLess(w, 2 * dw + 4, e['id']); self.assertLess(h, 2 * dh + 4, e['id'])

    def test_folders_and_paths(self):
        folders = {'Customers', 'Shop', 'Items', 'Icons', 'Locations', 'Machines', 'Characters'}
        for e in s.CATALOG:
            self.assertIn(e['folder'], folders)
            self.assertEqual(s.sprite_path(e), 'Assets/CottonCircuit/Sprites/%s/%s.png' % (e['folder'], e['id']))

    def test_kinds_have_rules_and_shadow_policy(self):
        for e in s.CATALOG:
            self.assertIn(e['kind'], s.KIND_RULES)
            self.assertEqual(e['shadow'], s.KIND_RULES[e['kind']]['shadow'], e['id'])

    def test_trait_icons(self):
        self.assertEqual(len(s.TRAIT_ICONS), 23)
        self.assertEqual(sorted(s.TRAIT_ICONS), sorted(e['id'] for e in s.owned_by('icons')))

    def test_palette(self):
        self.assertEqual(s.PALETTE['Magenta'], '#C2577E')
        self.assertEqual(len(s.PALETTE), 22)
        self.assertEqual(s.EXPOSURE, -1.8)
        self.assertEqual(s.MATERIAL, {'roughness': .85, 'specular_ior_level': .08})

    def test_pivots(self):
        for e in s.CATALOG:
            if e['id'].startswith('CottonCandy_'): self.assertEqual(e['pivot'], (0.5, 0.06))
            elif e['id'].startswith('BaggedCandy_'): self.assertEqual(e['pivot'], (0.5, 0.14))
            else: self.assertIsNone(e['pivot'])

if __name__ == '__main__':
    unittest.main()
```

- [ ] **Step 2: Run it to see it fail** — `python -m unittest discover -s Art/Blender/tests -p "test_ui_sprite_spec.py" -v` → `ModuleNotFoundError: No module named 'ui_sprite_spec'`.

- [ ] **Step 3: Write `Art/Blender/ui_sprite_spec.py`**:

```python
"""bpy-free source of truth for the pastel-toy UI sprites.

Imported by ui_sprite_common (inside Blender) and by the validator, preview and
tests (plain Python). See docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md.
"""
PALETTE = {
    'Strawberry': '#F48DAB', 'Soda': '#7ACDCE', 'Vanilla': '#F9D27D',
    'Navy': '#29324D', 'Cream': '#FFF1D4', 'White': '#FFF9ED',
    'Mint': '#99C4AE', 'Plum': '#6C577F', 'Gold': '#DBAE61',
    'Wood': '#D59C79', 'Tire': '#414059', 'Base': '#9DBBAF',
    'Skin1': '#F1C6A6', 'Skin2': '#B87A65', 'Skin3': '#E8AF88',
    'Hair1': '#574F59', 'Hair2': '#8D5B4A', 'Hair3': '#45475B',
    'Pants1': '#7C789D', 'Pants2': '#65688B', 'Pants3': '#5F968F',
    'Magenta': '#C2577E',
}
LIGHTING = {
    'key': {'location': [-2, -3, 7], 'power_w': 900, 'size': 4, 'color': '#FFF1DD'},
    'fill': {'location': [4.5, -3.5, 3], 'power_w': 250, 'size': 5, 'color': '#DDE6FF'},
    'rim': {'location': [1.5, 3, 5], 'power_w': 70, 'size': 3, 'color': '#FFFFFF'},
    'environment': {'color': '#F3ECF7', 'strength': .75},
    'aim': [0, 0, 1.25],
}
EXPOSURE = -1.8
SEED = 260927
MATERIAL = {'roughness': .85, 'specular_ior_level': .08}
RENDER = {'engine': 'CYCLES', 'samples': 64, 'denoise': True, 'view_transform': 'Standard',
          'look': 'None', 'exposure': EXPOSURE, 'gamma': 1}
REVIEW_BACKGROUNDS = {'cream': '#FFF6E7', 'ink': '#29324D'}
# Trait disc colours from OutgameUI.CategoryColor, used by the preview board and icon review.
DISC_COLORS = {'business': '#EBC997', 'production': '#EBA5B3', 'equipment': '#B8D9CC',
               'staff': '#C9BDE0', 'location': '#ACD2D9', 'sales': '#E2C2AD'}

KIND_RULES = {
    'character': {'shadow': True, 'opaque': False, 'margin': 2, 'occupancy': (.08, .85)},
    'prop':      {'shadow': True, 'opaque': False, 'margin': 2, 'occupancy': (.06, .90)},
    'emote':     {'shadow': False, 'opaque': False, 'margin': 2, 'occupancy': (.15, .90), 'max_yaw': 10},
    'icon':      {'shadow': False, 'opaque': False, 'margin': 2, 'occupancy': (.15, .90), 'max_yaw': 10},
    'scene':     {'shadow': False, 'opaque': True},
    'portrait':  {'shadow': False, 'opaque': True},
}
FLAVORS = ('Strawberry', 'Soda', 'Vanilla')
SIZES = ('Small', 'Medium', 'Large')
TRAIT_ICONS = [
    'Trait_Hours', 'Trait_Patience', 'Trait_Ads', 'Trait_RepeatAds', 'Trait_Shelf', 'Trait_Sales',
    'Trait_PriceTag', 'Trait_Engine', 'Trait_Handling', 'Trait_Kart', 'Trait_StickSpeed', 'Trait_Spoon',
    'Trait_Ribbon', 'Trait_Sugar2', 'Trait_Sugar3', 'Trait_Machine', 'Trait_FlavorSoda', 'Trait_FlavorVanilla',
    'Trait_Worker', 'Trait_GradCap', 'Trait_Glove', 'Trait_MapPin', 'Trait_Group',
]


def _e(id, folder, kind, canvas, displays, owner, pivot=None):
    return {'id': id, 'folder': folder, 'kind': kind, 'canvas': canvas, 'displays': displays,
            'owner': owner, 'shadow': KIND_RULES[kind]['shadow'], 'pivot': pivot}


CATALOG = (
    [_e('Customer_V%d_%s' % (v, x), 'Customers', 'character', (164, 280), [(82, 140)], 'customers')
     for v in range(3) for x in ('Neutral', 'Angry')]
    + [_e('Mina_Portrait', 'Characters', 'portrait', (808, 784), [(404, 391)], 'mina')]
    + [_e('CottonCandy_%s_%s' % (f, z), 'Items', 'prop', (156, 176), [(77, 88), (80, 77)], 'items', (0.5, 0.06))
       for f in FLAVORS for z in SIZES]
    + [_e('BaggedCandy_%s_%s' % (f, z), 'Items', 'prop', (148, 184),
          [(74, 92), (84, 98), (62, 86), (118, 124)], 'items', (0.5, 0.14)) for f in FLAVORS for z in SIZES]
    + [_e('SugarBag_%s' % f, 'Items', 'prop', (164, 200), [(82, 100), (118, 124)], 'items') for f in FLAVORS]
    + [_e('Storefront', 'Shop', 'prop', (340, 460), [(169, 230)], 'shop'),
       _e('Trash', 'Shop', 'prop', (136, 164), [(68, 82)], 'shop'),
       _e('Emote_Heart', 'Shop', 'emote', (144, 132), [(72, 66)], 'shop'),
       _e('Emote_Angry', 'Shop', 'emote', (144, 132), [(72, 66)], 'shop')]
    + [_e('Location_%d_Prep' % i, 'Locations', 'scene', (1176, 708), [(588, 354)], 'locations') for i in range(4)]
    + [_e('Location_%d_Street' % i, 'Locations', 'scene', (1192, 628), [(596, 314)], 'locations') for i in range(4)]
    + [_e('Machine_%d' % i, 'Machines', 'prop', (252, 252), [(126, 126)], 'machines') for i in range(3)]
    + [_e(i, 'Icons', 'icon', (84, 84), [(42, 42)], 'icons') for i in TRAIT_ICONS]
)


def by_id():
    return {e['id']: e for e in CATALOG}


def owned_by(owner):
    return [e for e in CATALOG if e['owner'] == owner]


def sprite_path(entry):
    return 'Assets/CottonCircuit/Sprites/%s/%s.png' % (entry['folder'], entry['id'])
```

- [ ] **Step 4: Run the spec test** — same command → `OK` (8 tests).

- [ ] **Step 5: Make `ui_sprite_common.py` import the constants and run safely in background mode.** Replace the literal `PALETTE`, `LIGHTING`, `EXPOSURE` block with `from ui_sprite_spec import PALETTE, LIGHTING, EXPOSURE, MATERIAL  # noqa: re-exported for the approved generators`; add `import os`; in `setup_scene` replace `bpy.context.window.scene = scene` with

```python
    window = bpy.context.window or (bpy.context.window_manager.windows[0] if bpy.context.window_manager.windows else None)
    if window:
        window.scene = scene
```

and replace `scene.render.threads = 8` with `scene.render.threads = int(os.environ.get('UI_SPRITE_THREADS', '8'))`. Leave `material()`, `studio()`, `camera()`, `catcher()` byte-identical otherwise. Add `Magenta` only through the spec (the approved generators never use it).

- [ ] **Step 6: Write `Art/Blender/ui_sprite_render.py`** — move the body of `create_ui_sprites.render_contact_sprite` here as `render_sprite(scene, sprite_id, output, passes_dir, shadow, size)`: when `shadow is None` do `scene.render.filepath = str(output); bpy.ops.render.render(write_still=True, scene=scene.name)`; otherwise render the catcher and beauty passes to `passes_dir/<id>-catcher.png` and `-beauty.png` exactly as today, then composite with **numpy float64** arrays implementing the same formula in the same order (`distance`, smoothstep `t`, `falloff = 1-t*t*(3-2*t)`, `shadow = min(max_alpha, max(0,(ca-a)/max(1-a,1e-5)))*falloff`, `final = a+shadow*(1-a)`, colour `(c*a+ink*shadow*(1-a))/max(final,1e-5)`, ink = `linear(PALETTE['Navy'])`), anchor and radii taken from `shadow`. Pixel centres use `x+.5`, `y+.5` in Blender's bottom-up pixel order as today. Save through a new `bpy.data.images` image with `alpha_mode='STRAIGHT'`, `filepath_raw=output`, `file_format='PNG'`, then remove the three images. Export `DEFAULT_SHADOW`.

- [ ] **Step 7: Reduce `create_ui_sprites.py` to the compatibility wrapper** (both approved generators do `import create_ui_sprites as renderer; renderer.ART = HERE; renderer.render_contact_sprite(scene, entry, path)`):

```python
"""Compatibility wrapper for the approved customer generators.

GameCustomerFaceted/create_customer.py and GameCustomerFemaleExplorer/create_explorer.py
set ART and call render_contact_sprite(scene, entry, output). New assets use
build_ui_sprites.py and ui_sprite_render.render_sprite directly.
"""
import importlib
import sys
from pathlib import Path

ART = Path(__file__).resolve().parent
if str(ART) not in sys.path:
    sys.path.insert(0, str(ART))
import ui_sprite_render as r
importlib.reload(r)


def render_contact_sprite(scene, entry, output):
    r.render_sprite(scene, entry['id'], Path(output), ART / 'ui-sprite-passes', r.DEFAULT_SHADOW,
                    tuple(entry['render_size']))
```

- [ ] **Step 8: Write the renderer identity test** — `Art/Blender/tests/test_render_identity.py` is a Blender script (run with `-b`) that opens `Art/Blender/GameCustomerFaceted/GameCustomerFaceted.blend`, finds scene `Game_Customer_Faceted`, sets 164×280, calls `ui_sprite_render.render_sprite(scene, 'identity_check', <scratch>/identity.png, <scratch>/passes, DEFAULT_SHADOW, (164, 280))` with `UI_SPRITE_THREADS` unset, then compares decoded RGBA with `Customer_01_Faceted.png` using `bpy.data.images` pixel arrays and prints `RENDER_IDENTITY max_abs_diff=<n> differing_pixels=<n>`; exit code 1 unless both are 0. The scratch dir is `%TEMP%/ui_sprite_identity`. The script must not save the `.blend` or write inside `Art/Blender/GameCustomerFaceted/`.

- [ ] **Step 9: Run it** — `& blender -b --factory-startup --python-exit-code 1 -P Art/Blender/tests/test_render_identity.py` → `RENDER_IDENTITY max_abs_diff=0 differing_pixels=0`. This also proves background mode works for the approved scenes. If the numpy composite differs, fall back to the exact Python loop from commit 966cf73 for identity and record the timing; do not accept any non-zero diff. Also confirm `git status --short Art/Blender/GameCustomerFaceted` prints nothing.

- [ ] **Step 10: Write `toy_kit.py`** — copy `GameCustomerFaceted/customer_lib.py` into a `Kit` class whose constructor takes `prefix` (object/material names `prefix + name`), whose `mat()` uses `MATERIAL` (Roughness 0.85, Specular IOR Level 0.08) and tags `palette_srgb`/`palette_name`, and whose other methods keep the lib's signatures and bevel behaviour. Add a module docstring pointing at `customer_lib.py` as the origin.

- [ ] **Step 11: Write `build_ui_sprites.py`** following the driver contract above. Parse args after `--` with `argparse`. Import the module with `importlib.import_module('ui_sprites.' + category)`. For every asset: assert the id is in `ui_sprite_spec.by_id()` and owned by `CATEGORY`; `scene = c.setup_scene('UIS_' + id, rig, world, entry['canvas'], SEED)`; `col = kit.collection(scene, 'UIS_' + id)`; `module.build(asset, col, kit)`; recalc normals on every mesh like the approved generators; `c.camera(scene, **asset['camera'])`; `c.catcher(scene)` only when `asset['shadow']`; `scene['sprite_id'] = id`. Opaque kinds: set `scene.render.film_transparent = False` only if the module sets `asset['opaque_backdrop'] = True` and the frame is fully covered by geometry (validator checks alpha 255). After building all scenes, remove the factory `Scene`, save `ui_sprites/<category>.blend` with `bpy.ops.wm.save_as_mainfile(filepath=..., compress=True)`, then render each scene with `render_sprite` into `<out-root>/<sprite_path>` and passes into `Art/Blender/ui-sprite-passes/<category>/`. Write the manifest (`category`, `blend`, `render` settings from `RENDER`, `palette`, `lighting`, `assets[]` with id, file, canvas, camera, shadow, params, objects, materials).

- [ ] **Step 12: Driver fixture test** — `tests/fixtures/ui_sprites_fixture.py` defines `CATEGORY='fixture'` and one asset `Trait_Hours` built as a simple Gold box; run `& blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py -- --category fixture --out-root $env:TEMP/ui_sprite_fixture --module-path Art/Blender/tests/fixtures` (add `--module-path` to the driver for this) and assert that `$env:TEMP/ui_sprite_fixture/Assets/CottonCircuit/Sprites/Icons/Trait_Hours.png` is 84×84 RGBA. The fixture must not write into `Art/Blender/ui_sprites/` (driver writes `.blend`/manifest next to the module path when `--module-path` is given, i.e. inside the temp tree — make the driver honour that).

- [ ] **Step 13: Rewrite `validate_ui_sprites.py`** (plain Python, Pillow + numpy): for every catalog entry whose PNG exists check RGBA, size == canvas, and kind rules (transparent kinds: ≥ margin fully transparent px on each edge, occupancy of alpha ≥ 128 within range; opaque kinds: every alpha == 255); for shadowed entries whose `-beauty.png` pass exists, opaque pixels equal the beauty pass; reject any manifest palette hex not in `PALETTE`; check every `ui_sprites/*.manifest.json` render block against `RENDER` including exposure and gamma. `--complete` additionally fails when any of the 66 PNGs is missing. Write `Art/Blender/ui-sprites-validation.json` with per-sprite results and print `UI_SPRITES_VALID <present>/66` or the failures. Exit 1 on failure.

- [ ] **Step 14: Rewrite `preview_ui_sprites.py`** — `python Art/Blender/preview_ui_sprites.py [--category items]` writes `Art/Blender/previews/<category>.png` (and `all.png` without `--category`): each present sprite at 2× (canvas) and at every display size on cream `#FFF6E7` and ink `#29324D`; icons additionally on their six disc colours at the 64 px disc / 42 px icon geometry used by `OutgameTraitsUI`; missing sprites appear as labelled grey boxes.

- [ ] **Step 15: Delete the obsolete sample pipeline files** listed in **Files**. Rewrite `Art/Blender/UI_SPRITES.md` to document the catalog, driver, renderer, validator, preview and the background command lines (English or Korean, match the existing file language: Korean).

- [ ] **Step 16: Verify** — run: the spec test (OK), the identity test (0 diff), the fixture driver run (PNG exists), `python Art/Blender/validate_ui_sprites.py` → `UI_SPRITES_VALID 0/66` exit 0, `python Art/Blender/validate_ui_sprites.py --complete` → exit 1 listing 66 missing, `python Art/Blender/preview_ui_sprites.py` → writes `previews/all.png`. Also run `python Art/Blender/GameCustomerFaceted/validate_png.py` and `python Art/Blender/GameCustomerFemaleExplorer/validate_png.py` (unchanged approved PNGs still pass).

- [ ] **Step 17: Report** the files changed and the exact command outputs. (Orchestrator commits: `git add` of the listed paths, message "Generalise the UI sprite pipeline for the full sprite catalog".)

### Task 2: Customer sprites (6)

**Files:**
- Modify: `Art/Blender/GameCustomerFaceted/customer_head.py`, `Art/Blender/GameCustomerFaceted/create_customer.py`, `Art/Blender/GameCustomerFaceted/GameCustomerFaceted.blend`
- Modify: `Art/Blender/GameCustomerFemaleExplorer/explorer_head.py`, `Art/Blender/GameCustomerFemaleExplorer/create_explorer.py`, `Art/Blender/GameCustomerFemaleExplorer/GameCustomerFemaleExplorer.blend`
- Create: `Art/Blender/GameCustomerChild/{create_child.py, child_head.py, README.md, manifest.json, GameCustomerChild.blend, Customer_V1-large.png}`
- Create: `Art/Blender/render_customer_sprites.py`, `Art/Blender/tests/test_customer_sprites.py`
- Create: `Assets/CottonCircuit/Sprites/Customers/Customer_V{0,1,2}_{Neutral,Angry}.png`, review renders `Art/Blender/previews/customers-large.png`

**Interfaces:**
- Consumes: Task 1 `ui_sprite_render.render_sprite`, `DEFAULT_SHADOW`, `toy_kit.Kit`, `ui_sprite_common.studio/setup_scene/camera/catcher`.
- Produces: head-module functions `build_angry_face(col) -> list[Object]` in `customer_head.py`, `explorer_head.py`, `child_head.py`, creating objects whose names contain `Face_Angry_`; each head's neutral mouth object name contains `Face_` and `Smile` (existing names `GC_Face_TinySmile`, `EX_Face_SoftSmile`; child uses `CH_Face_Smile`). `render_customer_sprites.py -- [--only V0,V1,V2] [--large]`.

**Design requirements:**
- **Angry face (all three):** two Navy brow slabs (`_flat_mark`/`_slab`-style, depth ≈ .012 authored) above the eyes, inner ends lower than outer ends by ~25°, each ≈ .14 × .035 authored units so they read as ≥ 1 px at 82×140; replace the smile with a short down-turned Navy frown of the same width as the neutral mouth. Brows sit between the eye top and the hairline/cap brim; if hair or the cap covers that band, lower the brows onto the upper eye edge rather than moving hair. Reference geometry: `Art/Blender/ReferenceAdventurer/head.py:88`.
- **Neutral renders of V0 and V2** must equal the approved PNGs pixel-for-pixel. Render them from the saved `.blend` files with angry objects hidden; if a re-render differs by even one value, copy the approved PNG instead and record the reason in the manifest (`neutral_source`).
- **Angry renders** use the same `.blend`, camera, resolution and threads (8) with neutral mouth hidden and angry objects shown. Outside the head band the angry sprite may differ from the neutral one only by denoiser noise: max 8 per channel and mean absolute difference below 0.5.
- **Child (V1):** new generator using `toy_kit.Kit('CH_')`, same authored units, same `.5` whole-model scale, same camera call as the male (`c.camera(scene, (0, -.015, 1.22), 2.85)`) and same catcher so feet sit on the same baseline and the child reads shorter (top of hair ≈ 75 % of the male's height). Proportion ≈ 3 heads (bigger head relative to body than adults). Palette identity: Strawberry hoodie with two small round ears on the lowered hood and a Cream drawstring, Hair2 short bowl cut with a small cowlick, Skin2 face (Skin3 is not used; darker side planes use a darker palette colour such as Hair2 only for the neck shadow—keep front planes Skin2), Pants2 shorts, Cream/Strawberry striped socks, Navy sneakers with Cream soles, a small Mint coin purse on a Cream strap across the body. Same dot-eye/blush/mouth rules as the adults (eye box .075×.020×.106 authored, x = ±.205 at head scale 1; scale the whole head, not the eyes, if the child's head is larger).
- Large review renders (656×1120) for all six go to `Art/Blender/<customer folder>/review/` and a combined board `Art/Blender/previews/customers-large.png`.

- [ ] **Step 1: Write the failing test** — `Art/Blender/tests/test_customer_sprites.py` (plain Python):

```python
import sys, unittest
from pathlib import Path
import numpy as np
from PIL import Image
ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'Assets/CottonCircuit/Sprites/Customers'
APPROVED = {0: ROOT / 'Art/Blender/GameCustomerFaceted/Customer_01_Faceted.png',
            2: ROOT / 'Art/Blender/GameCustomerFemaleExplorer/Customer_02_Explorer.png'}

def rgba(path):
    return np.asarray(Image.open(path).convert('RGBA'), dtype=np.int16)

class CustomerSpriteTests(unittest.TestCase):
    def test_all_six_exist_with_canvas(self):
        for v in range(3):
            for x in ('Neutral', 'Angry'):
                img = Image.open(OUT / ('Customer_V%d_%s.png' % (v, x)))
                self.assertEqual((img.mode, img.size), ('RGBA', (164, 280)))

    def test_approved_neutrals_are_pixel_identical(self):
        for v, path in APPROVED.items():
            self.assertTrue(np.array_equal(rgba(OUT / ('Customer_V%d_Neutral.png' % v)), rgba(path)), v)

    def test_angry_differs_only_in_head(self):
        for v in range(3):
            n, a = rgba(OUT / ('Customer_V%d_Neutral.png' % v)), rgba(OUT / ('Customer_V%d_Angry.png' % v))
            ys, xs = np.nonzero(n[..., 3] > 8)
            head_bottom = int(ys.min() + 0.38 * (ys.max() - ys.min()))   # head band of the character
            head = np.abs(n[:head_bottom] - a[:head_bottom]).max(axis=2) > 24
            self.assertGreaterEqual(int(head.sum()), 6, 'V%d brows/frown too small to read' % v)
            body = np.abs(n[head_bottom:] - a[head_bottom:])
            self.assertLessEqual(int(body.max()), 8, 'V%d angry changed the body beyond denoiser noise' % v)
            self.assertLess(float(body.mean()), 0.5, 'V%d angry changed the body beyond denoiser noise' % v)

    def test_child_is_shorter(self):
        male, child = rgba(OUT / 'Customer_V0_Neutral.png'), rgba(OUT / 'Customer_V1_Neutral.png')
        h = lambda im: np.ptp(np.nonzero(im[..., 3] > 128)[0])
        self.assertLess(h(child), 0.85 * h(male))

if __name__ == '__main__':
    unittest.main()
```

- [ ] **Step 2: Run it** — `python -m unittest Art/Blender/tests/test_customer_sprites.py -v` → FAIL (files missing).
- [ ] **Step 3: Add `build_angry_face(col)` to `customer_head.py` and `explorer_head.py`** (authored coordinates; the caller applies the `.5`/`.48` model scale). Call it from `create_customer.build()` / `create_explorer.build()` after the head is built and set `hide_render = True` and `hide_viewport = True` on the returned objects, so a future rebuild keeps them hidden by default.
- [ ] **Step 4: Write `render_customer_sprites.py`** (Blender `-b`): for each customer `(variant, blend, scene_name, head_module, model_scale)` — `(0, GameCustomerFaceted.blend, 'Game_Customer_Faceted', customer_head, .5)`, `(2, GameCustomerFemaleExplorer.blend, <scene name from its create_explorer.py>, explorer_head, .48)`, `(1, GameCustomerChild.blend, 'Game_Customer_Child', child_head, .5)` — open the blend (`bpy.ops.wm.open_mainfile`), and if no object name contains `Face_Angry_` build them into the head collection, scale their location/scale by `model_scale`, recalc normals, hide them, and save the blend (angry objects are the only change; fingerprint every other object's matrix/vertex count/materials before and after like `mcp_tools/runtime/refine_explorer_eyes.py:15-46` and abort on any change). Then render neutral (angry hidden, smile shown) and angry (angry shown, smile hidden) at 164×280 with `render_sprite(..., DEFAULT_SHADOW, (164,280))` into `Assets/CottonCircuit/Sprites/Customers/Customer_V<n>_<Expr>.png`, passes into `Art/Blender/ui-sprite-passes/customers/`. With `--large` also render 656×1120 into `<folder>/review/Customer_V<n>_<Expr>-large.png`. Restore the scene's original resolution/filepath before exiting. Do not save the blend after rendering.
- [ ] **Step 5: Render V0 and V2** — `& blender -b --factory-startup --python-exit-code 1 -P Art/Blender/render_customer_sprites.py -- --only V0,V2 --large`. Then `git status --short Art/Blender/GameCustomerFaceted Art/Blender/GameCustomerFemaleExplorer` must show only the two `.blend` files and the two head/create modules as modified; the approved PNGs must be unchanged (`git diff --stat` shows no PNG).
- [ ] **Step 6: Look at the four PNGs** at 164×280 and the large reviews. The angry faces must read as angry at 82×140 on both review backgrounds (`python Art/Blender/preview_ui_sprites.py --category customers`, then open `Art/Blender/previews/customers.png`). Iterate on brow size/angle only.
- [ ] **Step 7: Build the child** — write `GameCustomerChild/create_child.py` + `child_head.py` modelled on the male generator's structure (collections `CH_Head_Hair`, `CH_Hoodie`, `CH_Arms_Hands`, `CH_Shorts_Shoes`, `CH_Purse`; scene `Game_Customer_Child`; `CH_ACTION` build/render/all; manifest with identity, proportion, palette, lighting, camera, `creation_transport: 'blender -b'`). Build: `& blender -b --factory-startup --python-exit-code 1 -P Art/Blender/GameCustomerChild/create_child.py` with `CH_ACTION` defaulting to build only (rendering goes through `render_customer_sprites.py`). Render: `... render_customer_sprites.py -- --only V1 --large`. Iterate until the child reads clearly at 82×140 next to V0 and V2 on the preview board.
- [ ] **Step 8: Run the tests** — `python -m unittest Art/Blender/tests/test_customer_sprites.py -v` → OK; `python Art/Blender/validate_ui_sprites.py` → all 6 customer entries pass.
- [ ] **Step 9: Report** outputs, the preview board path, and any approved-file diff check results.

### Task 3: Mina portrait (1)

**Files:** Create `Art/Blender/MinaPortrait/{create_mina.py, mina_head.py, README.md, manifest.json, MinaPortrait.blend}`, `Assets/CottonCircuit/Sprites/Characters/Mina_Portrait.png`.

**Interfaces:** Consumes Task 1 kit/common/render. Produces `Mina_Portrait.png` 808×784 opaque RGBA.

**Design requirements:** Same authored units and `.5` model scale as the male so the shared rig stays valid; camera `c.camera(scene, target, scale, yaw=-20, elevation=15)` framed as a bust (head and hands with candy fully inside, cropped below the apron bib). Mina: face/eyes/blush/mouth follow the adult rules (dot eyes; a slightly open happy smile is allowed as the mouth shape); short wavy **Magenta** bob with side-swept bangs built from `explorer_head._hair_clump`-style curved clumps (the female approved hair technique); Cream long-sleeve shirt with a turned-down collar; Mint apron bib with Mint ruffled edges (`_strip`-style) and straps; both hands hold a Strawberry cotton candy on a Cream stick at chest height (reuse the cotton-candy technique from the items module or from `git show 966cf73:Art/Blender/create_ui_sprites.py` `cotton()`), with a tiny Strawberry-and-Mint berry on top. Backdrop fills the whole frame with geometry: Cream back wall, Wood shelves with Soda/Strawberry/Vanilla candy jars, a Strawberry/White scalloped awning strip across the top, a Soda cotton-candy machine dome at the right edge, and a small window at the left showing a pink road with a Navy/White checkered flag (nod to the racing half). Keep the backdrop lower in contrast than Mina (Base/Cream/White dominant) so she reads first. Render plain (`shadow=None`), `film_transparent=False`; the validator requires alpha 255 everywhere.

- [ ] **Step 1:** Write `create_mina.py`/`mina_head.py` (`MN_` prefix via `toy_kit.Kit('MN_')`), build and render with `& blender -b --factory-startup --python-exit-code 1 -P Art/Blender/MinaPortrait/create_mina.py` into the catalog path.
- [ ] **Step 2:** `python Art/Blender/validate_ui_sprites.py` → `Mina_Portrait` passes (808×784, opaque).
- [ ] **Step 3:** Compare side by side with `Assets/CottonCircuit/Resources/Progression/NpcPortrait.png` (write `Art/Blender/previews/mina-vs-painting.png`) and iterate until hair shape/colour, apron, shirt and candy pose are recognisably the same character.
- [ ] **Step 4:** Report.

### Task 4: Items (21)

**Files:** Create `Art/Blender/ui_sprites/items.py`; outputs `Assets/CottonCircuit/Sprites/Items/{CottonCandy_*,BaggedCandy_*,SugarBag_*}.png`, `Art/Blender/ui_sprites/items.blend`, `Art/Blender/ui_sprites/items.manifest.json`.

**Design requirements:**
- **Loose cotton candy (9):** Cream stick with four flavour-coloured stripes; floss = one faceted core plus ~20 jittered lobes (seeded per id from `SEED`), flavour colour Strawberry/Soda/Vanilla with slightly lighter lobes allowed only by lighting (no extra colours). Small/Medium/Large differ only in floss size (Large ≈ 1.35 × Small in height); all nine share one camera so the stick base projects to the pivot (0.5, 0.06) of the 156×176 canvas and the stick length is identical. Large must still fit with its shadow. Must read at 77×88 and when contained in 80×77.
- **Bagged candy (9):** the same candy inside a lowpoly cellophane bag (White material with a Principled alpha ≈ .35 so the floss shows through; keep it palette White), gathered and tied with a flavour-coloured ribbon whose knot projects to the pivot (0.5, 0.14); the Cream stick continues below the knot. Floss size varies by tier as above, bag grows with it. Same camera for all nine.
- **Sugar bags (3):** flavour-coloured paper pouch with a folded top, a Cream label with a flavour emblem (strawberry for Strawberry, three bubbles for Soda, a small flower for Vanilla), readable at 82×100.
- Materials via `kit.mat`; shadows use `DEFAULT_SHADOW` unless a larger ellipse is needed (record in params).

- [ ] **Step 1:** Write `ui_sprites/items.py` per the category contract.
- [ ] **Step 2:** Render with `UI_SPRITE_THREADS=4`: `& blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py -- --category items`.
- [ ] **Step 3:** `python Art/Blender/validate_ui_sprites.py` → all 21 items pass. `python Art/Blender/preview_ui_sprites.py --category items` and inspect `previews/items.png` at display sizes (loose 77×88 and 80×77; bagged 84×98, 74×92, 62×86; sugar 82×100). Measure the pivots on the PNGs (stick base / knot row) and assert them within ±2 px of the catalog pivot in the report.
- [ ] **Step 4:** Report.

### Task 5: Shop (4)

**Files:** Create `Art/Blender/ui_sprites/shop.py`; outputs `Assets/CottonCircuit/Sprites/Shop/{Storefront,Trash,Emote_Heart,Emote_Angry}.png`, `ui_sprites/shop.blend`, `ui_sprites/shop.manifest.json`.

**Design requirements:**
- **Storefront (340×460):** a cotton-candy kiosk re-authored from the structure of `Art/Blender/create_assets.py:116-136` (Wood platform, Cream back wall and pillars, Strawberry/Cream striped awning, Strawberry counter, candy jars, a cotton candy on the roof). Two label bands must be plain light single-colour faces for overlaid text: the sign band at 71.5–92.5 % of the canvas height measured from the bottom, spanning x 15–85 %, and the counter front band at 18–32 % from the bottom. Contact shadow inside the canvas.
- **Trash (136×164):** a Mint/Base lidded bin with a Cream swing lid and one crumpled Strawberry wrapper peeking out; contact shadow.
- **Emote_Heart / Emote_Angry (144×132):** no shadow, |yaw| ≤ 10°: a chunky faceted Strawberry heart with a White highlight facet; an anger mark (four-armed "vein" cross) in Strawberry with Plum side facets plus a small Navy steam puff. Both must read at 72×66 inside the White speech bubble.

- [ ] Steps: write module → render (`--category shop`, threads 4) → validate → preview (`--category shop`) and check the label bands are flat and light (report the mean and std of RGB in both bands; std ≤ 12) → report.

### Task 6: Locations (8)

**Files:** Create `Art/Blender/ui_sprites/locations.py`; outputs `Assets/CottonCircuit/Sprites/Locations/Location_{0..3}_{Prep,Street}.png`, `ui_sprites/locations.blend`, `ui_sprites/locations.manifest.json`.

**Design requirements:** Four small lowpoly dioramas (one per location), each rendered twice with two cameras: Prep 1176×708 and Street 1192×628. Opaque: sky/back wall and ground geometry fill the frame (set `asset['opaque_backdrop'] = True`). Landmarks: 0 동네 골목 — low houses with pastel roofs, a lamp post, a narrow alley; 1 시장 앞 — a row of market stalls with Strawberry/Soda/Vanilla awnings and crates; 2 강변 축제 — a Soda river with a small bridge, bunting lines and festival booths; 3 별빛 광장 — a dusk plaza (Plum/Navy sky facets with small Vanilla stars) with a ferris wheel. Street composition: keep the left ~30 % (where the storefront overlays) and the middle band (where three customers and speech bubbles stand, x 30–98 %, y 15–90 % from the top) low-detail; put landmarks along the top/background; the bottom 11 % is a calm Base/Cream sidewalk band for Navy status text. Prep composition may show the landmark prominently. Colour and lighting follow the global rules; keep overall value lighter than characters so overlays read.

- [ ] Steps: write module → render (`--category locations`, threads 4) → validate (opaque) → preview: additionally composite `Shop/Storefront.png` (if present) at (6,48,169,230) and three customer sprites at the `CustomerDropTarget` positions onto each Street image at 596×314 (`previews/locations-street-composite.png`) to check overlay readability → report.

### Task 7: Machines (3)

**Files:** Create `Art/Blender/ui_sprites/machines.py`; outputs `Assets/CottonCircuit/Sprites/Machines/Machine_{0,1,2}.png`, `ui_sprites/machines.blend`, `ui_sprites/machines.manifest.json`.

**Design requirements:** Cotton-candy machines based on the `Spinner` structure in `Art/Blender/create_assets.py:138-145` (base, bowl, central spinner head). Machine 0 basic: small Strawberry bowl on a Cream base. Machine 1 soda: larger Soda bowl with a translucent-looking bubble dome (White facets) and small bubble spheres. Machine 2 premium: largest, Gold trims and a Gold star on top, Plum base. All three must be distinguishable at 126×126 and at 35 % alpha (locked state) on the White card.

- [ ] Steps: module → render (`--category machines`) → validate → preview → report.

### Task 8: Trait icons (23)

**Files:** Create `Art/Blender/ui_sprites/icons.py`; outputs `Assets/CottonCircuit/Sprites/Icons/Trait_*.png` (23), `ui_sprites/icons.blend`, `ui_sprites/icons.manifest.json`.

**Design requirements:** One object per icon in the spec table (wall clock, hourglass, megaphone, heart speech bubble, shelf, signboard, price tag, engine, steering wheel, classic kart, spinning stick with motion arcs, measuring spoon, ribbon cotton candy, sugar bag with 2 / 3 Gold stars, cotton-candy machine, Soda-coloured and Vanilla-coloured cotton candy, worker with apron, graduation cap, glove, map pin, customer group). Camera yaw between -10° and 0°, elevation 15°, no shadow, 84×84, subject filling 60–80 % of the canvas. Each icon must read on cream, ink and **its own category disc colour** (read the category of each node in `Assets/CottonCircuit/Scripts/Core/Progression.cs` and use `DISC_COLORS`): use Navy/Wood/Gold key shapes and avoid a dominant colour close to the disc (e.g. no Strawberry-dominant icon on the production disc `#EBA5B3`). Also 55 % alpha (unreachable state) must still show the silhouette.

- [ ] Steps: module → render (`--category icons`) → validate → preview (the board places every icon on its category disc) → report the node → icon → category table you verified.

### Task 9: Unity foundation (import, catalog, GameAssets, trait icon table, checks)

**Files:**
- Create: `Assets/CottonCircuit/Scripts/Core/TraitIcons.cs`, `Tools/Tests/TraitIconTests.cs`, `Tools/test-trait-icons.ps1`, `Assets/CottonCircuit/Editor/SpriteImport.cs`, `Assets/CottonCircuit/Editor/SpriteCatalog.cs`, folder `.meta` files under `Assets/CottonCircuit/Sprites/` (generated by Unity)
- Modify: `Assets/CottonCircuit/Scripts/GameAssets.cs`, `Assets/CottonCircuit/Editor/ProjectBuilder.cs` (CreateScene, before `ReplaceAsset`), `Assets/CottonCircuit/Editor/IntegrationChecks.cs` (next to the prefab checks at :192-197)

**Interfaces:**
- Produces `TraitIcons.All` (23 ids, same order as `ui_sprite_spec.TRAIT_ICONS`), `TraitIcons.For(string nodeId) -> string` (throws `KeyNotFoundException` for unknown ids).
- Produces `GameAssets` fields:

```csharp
[Serializable] public struct TraitIconSprite { public string Id; public Sprite Sprite; }
// inside GameAssets:
public Sprite[] CustomerNeutral = new Sprite[3], CustomerAngry = new Sprite[3];     // index = customer style 0..2
public Sprite Storefront, Trash, HeartEmote, AngryEmote, MinaPortrait;
public Sprite[] LocationPrep = new Sprite[4], LocationStreet = new Sprite[4];      // index = location 0..3
public Sprite[] CottonCandy = new Sprite[9], BaggedCandy = new Sprite[9];          // index = flavor * 3 + size
public Sprite[] SugarBags = new Sprite[3], Machines = new Sprite[3];
public TraitIconSprite[] TraitIcons = new TraitIconSprite[0];
public Sprite TraitIcon(string iconId) { foreach (var i in TraitIcons) if (i.Id == iconId) return i.Sprite; return null; }
```

- Produces `SpriteCatalog.Assign(GameAssets assets)` (Editor) that loads each sprite from `Assets/CottonCircuit/Sprites/<Folder>/<Id>.png`, throwing `System.IO.FileNotFoundException` naming the path when missing.

- [ ] **Step 1: Baseline before any Unity change.** Run `./Tools/build.ps1 -BuildFolder Builds/SpritesBaseline`, then in parallel `./Tools/verify-progression.ps1 -BuildFolder Builds/SpritesBaseline -OutputFolder Logs/SpritesBaseline-progression`, `./Tools/verify-player.ps1 -BuildFolder Builds/SpritesBaseline -OutputFolder Logs/SpritesBaseline-shift -ShopShift`, `./Tools/verify-player.ps1 -BuildFolder Builds/SpritesBaseline -OutputFolder Logs/SpritesBaseline-legacy`. Record pass/fail and check counts in the task report. This baseline includes the untouched seam-wall change, so later driving failures can be attributed correctly. After the build, compare `git status --short` with the list of pre-existing changes; report any tracked file `CreateScene` rewrote (do not restore anything yourself — the orchestrator decides).
- [ ] **Step 2: Failing console test** — `Tools/Tests/TraitIconTests.cs` in the style of `UpgradeTreeLayoutTests.cs`: every `Progression.Nodes` id maps through `TraitIcons.For` to an id in `TraitIcons.All`; `TraitIcons.All` has 23 unique entries; every entry of `All` is used by at least one node; the mapping equals the spec table (assert the 31 pairs explicitly); an unknown id throws. `Tools/test-trait-icons.ps1` copies `Tools/test-upgrade-tree-layout.ps1` with the new file names (`Logs/TraitIconTests.exe`, `Logs/trait-icon-tests.txt`). Run `./Tools/test-trait-icons.ps1` → compile error (`TraitIcons` missing).
- [ ] **Step 3: Write `Scripts/Core/TraitIcons.cs`** (namespace `CottonCircuit`, no UnityEngine):

```csharp
using System.Collections.Generic;

namespace CottonCircuit
{
    // Explicit trait -> icon sprite id table (spec "특성 아이콘"). Sprite ids match
    // Assets/CottonCircuit/Sprites/Icons/<id>.png and Art/Blender/ui_sprite_spec.TRAIT_ICONS.
    public static class TraitIcons
    {
        public static readonly string[] All =
        {
            "Trait_Hours", "Trait_Patience", "Trait_Ads", "Trait_RepeatAds", "Trait_Shelf", "Trait_Sales",
            "Trait_PriceTag", "Trait_Engine", "Trait_Handling", "Trait_Kart", "Trait_StickSpeed", "Trait_Spoon",
            "Trait_Ribbon", "Trait_Sugar2", "Trait_Sugar3", "Trait_Machine", "Trait_FlavorSoda", "Trait_FlavorVanilla",
            "Trait_Worker", "Trait_GradCap", "Trait_Glove", "Trait_MapPin", "Trait_Group",
        };

        static readonly Dictionary<string, string> byNode = new Dictionary<string, string>
        {
            { "hours", "Trait_Hours" }, { "patience", "Trait_Patience" }, { "ads", "Trait_Ads" },
            { "repeat_ads", "Trait_RepeatAds" }, { "shelf", "Trait_Shelf" }, { "sales", "Trait_Sales" },
            { "flavor_price", "Trait_PriceTag" }, { "location_price", "Trait_PriceTag" },
            { "engine", "Trait_Engine" }, { "handling", "Trait_Handling" }, { "coupe", "Trait_Kart" },
            { "stick_speed", "Trait_StickSpeed" }, { "stick_saving", "Trait_Spoon" }, { "sugar_saving", "Trait_Spoon" },
            { "stick_quality", "Trait_Ribbon" }, { "quality_focus", "Trait_Ribbon" },
            { "sugar_2", "Trait_Sugar2" }, { "sugar_3", "Trait_Sugar3" },
            { "machine_2", "Trait_Machine" }, { "machine_3", "Trait_Machine" },
            { "flavor_soda", "Trait_FlavorSoda" }, { "flavor_vanilla", "Trait_FlavorVanilla" },
            { "worker_1", "Trait_Worker" }, { "worker_2", "Trait_Worker" },
            { "worker_grade_2", "Trait_GradCap" }, { "worker_grade_3", "Trait_GradCap" },
            { "worker_speed", "Trait_Glove" },
            { "location_1", "Trait_MapPin" }, { "location_2", "Trait_MapPin" }, { "location_3", "Trait_MapPin" },
            { "group_visit", "Trait_Group" },
        };

        public static string For(string nodeId)
        {
            if (nodeId != null && byNode.TryGetValue(nodeId, out string id)) return id;
            throw new KeyNotFoundException("No trait icon for node '" + nodeId + "'");
        }
    }
}
```

- [ ] **Step 4:** `./Tools/test-trait-icons.ps1` → `RESULT` line with 0 failed. Also re-run `./Tools/test-progression.ps1` (Core still compiles).
- [ ] **Step 5: Import settings** — `Editor/SpriteImport.cs`: `public sealed class SpriteImport : AssetPostprocessor { void OnPreprocessTexture() { if (!assetPath.StartsWith("Assets/CottonCircuit/Sprites/")) return; var t = (TextureImporter)assetImporter; t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single; t.mipmapEnabled = false; t.alphaIsTransparency = true; t.textureCompression = TextureImporterCompression.CompressedHQ; t.sRGBTexture = true; t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; t.maxTextureSize = 2048; } }` (namespace `CottonCircuit.Editor`).
- [ ] **Step 6: `Editor/SpriteCatalog.cs`** — `Load(string folder, string id)` → `AssetDatabase.LoadAssetAtPath<Sprite>(path)`; if null and the file exists, `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)` then reload; throw `FileNotFoundException` if still null. `Assign(GameAssets a)` fills every field in the Interfaces block using the catalog ids (customers `Customer_V{i}_{Neutral,Angry}`; flavours in order Strawberry, Soda, Vanilla; sizes Small, Medium, Large; `TraitIcons` from `CottonCircuit.TraitIcons.All`).
- [ ] **Step 7: `ProjectBuilder.CreateScene`** — call `SpriteCatalog.Assign(assets);` after the existing field assignments and before `ReplaceAsset(assets, ...)` (ProjectBuilder.cs:45) so `CopySerialized` keeps them.
- [ ] **Step 8: `IntegrationChecks.Run`** — next to the prefab checks add: every sprite array has the documented length and no null entry; `Storefront`, `Trash`, `HeartEmote`, `AngryEmote`, `MinaPortrait` non-null; for every `Progression.Nodes` node `assets.TraitIcon(TraitIcons.For(node.Id)) != null`; every sprite's texture importer has `textureType == Sprite`, `mipmapEnabled == false`, `alphaIsTransparency == true`; `Assets/CottonCircuit/Resources/Progression/NpcPortrait.png` check is left to Task 10.
- [ ] **Step 9: Build** — `./Tools/build.ps1 -BuildFolder Builds/SpritesT9` must print `COTTON_EDITOR_CHECKS_PASSED` and `COTTON_BUILD_SUCCESS` (requires all 66 PNGs from Tasks 2–8). Inspect three generated `.meta` files to confirm the importer settings. `git status --short` must show only this task's files plus the new `.meta` files and `GameAssets.asset`.
- [ ] **Step 10: Report.**

### Task 10: Runtime swap (Image-based art, delete procedural drawing)

**Files:**
- Create: `Assets/CottonCircuit/Scripts/UiArt.cs`, `Assets/CottonCircuit/Scripts/StreetSprite.cs`
- Modify: `Scripts/GameUI.cs` (call `UiArt.Use(game.World.Assets)` at the start of `Initialize`), `Scripts/ShopArtGraphic.cs`, `Scripts/ShopStreetGraphic.cs`, `Scripts/ProgressionArtGraphic.cs`, `Scripts/ShiftUI.cs`, `Scripts/ShopStreetUI.cs`, `Scripts/CandyRackUI.cs`, `Scripts/OutgameUI.cs`, `Scripts/OutgameTraitsUI.cs`, `Tests/ShopShiftRuntimeSmoke.cs`, `Tests/CandyRackRuntimeSmoke.cs`, `Tests/ProgressionRuntimeSmoke.cs`, `Editor/IntegrationChecks.cs` (portrait path check)
- Delete: `Assets/CottonCircuit/Resources/Progression/NpcPortrait.png` (+ `.meta`), then the empty `Resources/Progression` and `Resources` folders (+ `.meta`)

**Interfaces:**
- Consumes Task 9 `GameAssets` fields and `TraitIcons.For`.
- Produces `UiArt` (static): `Use(GameAssets)`, `Sprite CottonCandy(int flavor, int size)`, `BaggedCandy(int flavor, int size)`, `SugarBag(int flavor)`, `Customer(int style, bool angry)`, `LocationPrep(int i)`, `LocationStreet(int i)`, `Machine(int i)`, `TraitIcon(string nodeId)`, `Storefront`, `Trash`, `HeartEmote`, `AngryEmote`, `MinaPortrait`; `Vector2 CandyPivot = (0.5, 0.06)`, `BagPivot = (0.5, 0.14)`; `float[] ReferenceGrowth = { .7597f, .845f, .917f }`. Missing sprites throw `InvalidOperationException` with the sprite name (IntegrationChecks guarantees presence).

**Behaviour requirements (from the code map):**
- `ShopArtGraphic` becomes `public sealed class ShopArtGraphic : UnityEngine.UI.Image`. Keep `Kind`, `FlavorIndex`, `SizeTier`, `GrowthScale` (default .69f), `Configure(ShopArtKind kind, int flavor = 0, int tier = 0)` and `SetDistance(double meters)` with today's growth formula (`.38f + .62f * Sqrt(Clamp01(meters / (ShopShift.MetersForSize(2) * 4/3)))`). Remove the unused `ShopArtKind.Customer` and the `angry` parameter. `Configure` picks the sprite (`tier < 0` → Small), sets `raycastTarget = false`, and for SugarBag/Trash resets the growth transform to 1. For CottonCandy/BaggedCandy the visual scale is `GrowthScale / ReferenceGrowth[max(tier,0)]` applied around the pivot (`CandyPivot`/`BagPivot`) without moving the rect: implement with a child-free approach by setting `rectTransform.pivot` while compensating `anchoredPosition` so the rect stays put, then `rectTransform.localScale = Vector3.one * scale`. `color` alpha keeps working (Growing candy .18 when empty). `preserveAspect` = true for CottonCandy (HUD 77×88 and order 80×77) and for the drag ghost; false for rack bagged candy so the knot stays on `CandyRackGraphic.ClipPoint` (the rect is resized to `BagRect` by `CandyRackUI`).
- `StreetSprite : Image` with `public ShopStreetArtKind Kind { get; private set; }` and `Configure(ShopStreetArtKind kind, int variant = 0, bool angry = false)`: Customer → `UiArt.Customer(((variant % 3) + 3) % 3, angry)`, Storefront, Backdrop → `UiArt.LocationStreet(0)`, HeartEmote, AngryEmote; `raycastTarget = false` in `Awake` and `Configure`; `preserveAspect = true` for Customer/emotes. `ShopStreetGraphic` keeps only the speech bubble (enum value SpeechBubble path, `P`, `Point`, `Polygon`, `Rect`, `RoundedRect`, `CircleCorner`, `Line`); bubble colours become palette-aligned: normal fill White `#FFF9ED` with Navy outline, angry fill White with Strawberry `#F48DAB` outline. `StreetArt` in `ShopStreetUI` adds `StreetSprite` for every kind except SpeechBubble.
- Street location scene (`ShopStreetUI.cs:26,64-67`): an `Image` named `LocationStreetBackdrop` showing `UiArt.LocationStreet(location)`, swapped when `streetLocation` changes; non-progression mode shows `LocationStreet(0)`.
- `ProgressionArtGraphic` keeps only the disc (`Kind == "disc"`, `Circle`, `Add`, `P`); all icon/scene/portrait branches and helpers are deleted. `OutgameUI` gets `Image PrepSprite(RectTransform parent, string name, float x, float y, float w, float h, Sprite sprite)` (raycastTarget false, preserveAspect true). Machine cards use `UiArt.Machine(i)` with `canvasRenderer.SetAlpha(unlocked ? 1 : .35f)` as today; the hero location uses `UiArt.LocationPrep(locationPreview)`; trait `NodeIcon` uses `UiArt.TraitIcon(node.Id)` keeping `SetAlpha(rank > 0 || reachable ? 1 : .55f)`; `NodeCircle`/`NavigationHighlight` still use `PrepArt(..., "disc", ...)`.
- Portrait: replace `OutgameUI.cs:55-63` with an `Image` named `CompanionPortrait` at (7,7,404,391) showing `UiArt.MinaPortrait`, `preserveAspect = false`, `raycastTarget = false`; delete the Resources load, `uvRect` crop and the `"portrait"` fallback. Update `ProgressionRuntimeSmoke.cs:47` to assert the `CompanionPortrait` Image has a sprite named `Mina_Portrait`. Add an IntegrationChecks line asserting `Assets/CottonCircuit/Resources` no longer exists.
- Tests: `ShopShiftRuntimeSmoke.cs:62-64` and `:352-353` keep using `ShopArtGraphic` (API preserved); `:107-108`, `:133-134`, `:565-566` switch from `ShopStreetGraphic` to `StreetSprite`; `CandyRackRuntimeSmoke.cs:170` keeps `ShopArtGraphic`. Raycast routing tests (`:356-360`) must still pass. In `ProgressionScenario` add `ClickProgression("OpenLocations")` + a 1280×720 capture named `26-locations-1280.png` next to the existing 1280 captures (around lines 229–240).

- [ ] **Step 1:** Update the three smoke files first to the new types/names (they will not compile yet — expected).
- [ ] **Step 2:** Implement `UiArt`, `StreetSprite`, the `ShopArtGraphic` rewrite and the `ShopStreetGraphic` trim; update `ShiftUI`, `ShopStreetUI`, `CandyRackUI` factories.
- [ ] **Step 3:** Implement the Outgame changes, trim `ProgressionArtGraphic`, delete the Resources portrait.
- [ ] **Step 4:** Grep proof that procedural art is gone: `grep -n "DrawCustomer\|DrawStorefront\|DrawBackdrop\|DrawHeartEmote\|DrawAngryEmote\|void Person\|void Bag\|void Bin\|void BaggedCandy\|void Scene(\|void Portrait(\|NpcPortrait" -r Assets/CottonCircuit/Scripts` → no matches.
- [ ] **Step 5:** `./Tools/build.ps1 -BuildFolder Builds/Sprites` → `COTTON_EDITOR_CHECKS_PASSED`, `COTTON_BUILD_SUCCESS`.
- [ ] **Step 6:** Run the three smokes against `Builds/Sprites` into `Logs/Sprites-progression`, `Logs/Sprites-shift`, `Logs/Sprites-legacy`; all must report `PASSED`. Any failure also present in the Task 9 baseline is pre-existing and must be reported, not fixed here.
- [ ] **Step 7:** Look at `Logs/Sprites-shift/06-shop-1600x900.png`, `09-shop-street.png`, `10-shop-street-angry.png`, `12-customer-heart.png`, `11-rack-nine-full.png`, `14-rack-sparse.png`, `02-growing.png`, and `Logs/Sprites-progression/01-preparation.png`, `06-equipment.png`, `03-locations.png`, `26-locations-1280.png`, `09-graph-1280.png`. Confirm sprites appear in every art slot, bagged knots sit on the rack clips, the growing candy scales from its stick base, labels over the storefront bands are readable.
- [ ] **Step 8: Report.**

### Task 11: Verification record, comparison tooling and documentation

**Files:** Create `Tools/compare-captures.py`, `docs/lowpoly-sprites-verification.md`; modify `README.md` (Blender section: keep Codex's existing art lines, correct them — remove the line for the unapproved `GameCustomerFemaleFaceted`, state that the PNGs are connected — and add the new pipeline files, the category modules, the three customer folders, `MinaPortrait/`, and `Assets/CottonCircuit/Sprites/`), `Art/Blender/UI_SPRITES.md` (final numbers).

- [ ] **Step 1:** `Tools/compare-captures.py <before_dir> <after_dir> <out_dir>`: for each same-named PNG pair write a side-by-side PNG and print mean absolute RGB difference and changed-pixel percentage; exit 0. Run it for the baseline vs new progression, shift and legacy logs.
- [ ] **Step 2:** Run `python Art/Blender/validate_ui_sprites.py --complete` (66/66), `python -m unittest discover -s Art/Blender/tests -p "test_*.py"` (identity test excluded because it needs Blender — run it separately with Blender), all `Tools/test-*.ps1`, and `python Art/Blender/preview_ui_sprites.py`.
- [ ] **Step 3:** Write `docs/lowpoly-sprites-verification.md` in Korean following `docs/urp-migration-verification.md`: commands, results, check counts before/after, the comparison table, readability notes judged on game captures (spec criterion 5), a 1600×900 split-screen capture proving race and shop halves look like one game (criterion 6), known limitations (loss of the spinning floss animation), and the seam-wall change attribution.
- [ ] **Step 4:** Report.

### Task 12: Final review (orchestrator)

- [ ] Run a broad independent review of the whole branch diff against the spec's 8 completion criteria and this plan's Global Constraints (spec compliance, C# correctness, preserved behaviours, raycast flags, deleted code, test updates, art style consistency across all 66 sprites on one board), fix confirmed findings through the task loop, re-run Task 11's verification commands, then report to the user with the full-board image and the verification document.

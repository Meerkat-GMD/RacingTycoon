# UI 스프라이트 파이프라인

2026-09-27. 기준 문서는 `docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md`(명세)이며, 명세와 이 문서가 다르면 명세를 따른다. 게임의 2D 그림 66장을 승인된 두 손님(`GameCustomerFaceted/`, `GameCustomerFemaleExplorer/`)과 같은 "파스텔 토이" 로우폴리 렌더로 만든다. 모든 명령은 프로젝트 루트에서 실행한다.

## 구성

| 파일 | 역할 |
|---|---|
| `ui_sprite_spec.py` | bpy 없이 읽는 기준값: 팔레트 22색, 조명, 노출 -1.8, seed 260927, 재질 레시피, 종류별 규칙(`KIND_RULES`), 66장 `CATALOG`, `TRAIT_ICONS`, 경로 함수 |
| `ui_sprite_common.py` | Blender 공용 함수: `studio()` 조명·환경, `setup_scene()`, `camera()`, `catcher()`. 기준값은 `ui_sprite_spec`에서 가져와 다시 내보낸다 |
| `ui_sprite_render.py` | `render_sprite()`: 그림자 없는 단일 렌더, 또는 접지 그림자 합성 렌더. `DEFAULT_SHADOW` |
| `toy_kit.py` | `Kit(prefix)`: 승인본 `customer_lib.py`와 같은 서명의 도형 함수(`mat`, `collection`, `place`, `mesh`, `box`, `ico`, `segment`, `loft`, `panel`, `band`, `buckle`). 재질은 Roughness 0.85, Specular IOR Level 0.08 |
| `build_ui_sprites.py` | 분류 하나를 만들고 렌더하는 드라이버 |
| `ui_sprites/<분류>.py` | 분류별 에셋 목록과 형태 코드: `items`(21장), `shop`(4), `locations`(8), `machines`(3), `icons`(23) |
| `ui_sprites/<분류>.blend`, `ui_sprites/<분류>.manifest.json` | 드라이버가 저장하는 편집용 원본과 목록 파일 |
| `ui-sprite-passes/<분류>/` | 그림자 합성 전 `-catcher.png`, `-beauty.png` 렌더 패스 |
| `validate_ui_sprites.py` | 카탈로그 기준 PNG·목록 파일 검사. 기본 보고서 `ui-sprites-validation.json` |
| `preview_ui_sprites.py` | `previews/<분류>.png`, `previews/all.png` 검토 보드 |
| `previews/` | 위 검토 보드와 분류별 추가 확인 이미지(`customers-large`, `icons-states`, `locations-street-composite`, `machines-locked`, `mina-vs-painting`, `shop-emotes-bubble`, `shop-storefront-labels`) |
| `render_customer_sprites.py` | 손님 세 원본(`GameCustomerFaceted/`, `GameCustomerChild/`, `GameCustomerFemaleExplorer/`)으로 `Customer_V{0,1,2}_{Neutral,Angry}` 6장을 렌더 |
| `GameCustomerChild/` | 어린이 손님(V1) 원본, 생성 코드, `measure_child.py` |
| `MinaPortrait/` | `Mina_Portrait` 원본, 생성 코드(`create_mina.py`, `mina_head.py`), 목록 파일, 기존 페인팅 비교 보드 `make_comparison.py` |
| `update_art_concept_boards.py` | 명세의 비교 보드 두 장(`docs/screenshots/art-concept-*.png`)과 `art-concept-boards-validation.json` |
| `create_ui_sprites.py` | 승인된 두 손님 생성기가 쓰는 호환용 `render_contact_sprite()`와 `ART` 전역값 |
| `tests/` | 카탈로그·검사기·손님 스프라이트 단위 시험, 드라이버 시험, 렌더 동일성 시험 |

초기 대표 샘플 3종(`UiSprites.blend`, `ui-sprites-manifest.json`, 감사·재현 기록, `audit_ui_sprite_scene.py`, `ui-sprites-native.png`, `ui-sprites-preview.png`, 샘플 패스와 PNG)은 삭제했다. 솜사탕과 벽시계 샘플의 형태 코드는 `git show 966cf73:Art/Blender/create_ui_sprites.py`의 `cotton()`, `clock()`에서 볼 수 있다. 전체 검토 보드는 `previews/all.png`다.

## 카탈로그

`ui_sprite_spec.CATALOG`가 유일한 기준이다. 캔버스는 대표 표시 크기의 2배를 4의 배수로 올린 값이다. 파일은 `Assets/CottonCircuit/Sprites/<폴더>/<id>.png`에 둔다.

| 분류(owner) | id | 폴더 | 종류 | 캔버스 | 표시 크기 |
|---|---|---|---|---|---|
| customers | `Customer_V{0,1,2}_{Neutral,Angry}` | Customers | character | 164×280 | 82×140 |
| mina | `Mina_Portrait` | Characters | portrait | 808×784 | 404×391 |
| items | `CottonCandy_{Strawberry,Soda,Vanilla}_{Small,Medium,Large}` | Items | prop | 156×176 | 77×88, 80×77 |
| items | `BaggedCandy_{맛}_{크기}` | Items | prop | 148×184 | 74×92, 84×98, 62×86, 118×124 |
| items | `SugarBag_{Strawberry,Soda,Vanilla}` | Items | prop | 164×200 | 82×100, 118×124 |
| shop | `Storefront` | Shop | prop | 340×460 | 169×230 |
| shop | `Trash` | Shop | prop | 136×164 | 68×82 |
| shop | `Emote_Heart`, `Emote_Angry` | Shop | emote | 144×132 | 72×66 |
| locations | `Location_{0..3}_Prep` | Locations | scene | 1176×708 | 588×354 |
| locations | `Location_{0..3}_Street` | Locations | scene | 1192×628 | 596×314 |
| machines | `Machine_{0,1,2}` | Machines | prop | 252×252 | 126×126 |
| icons | `Trait_*` 23장 (`TRAIT_ICONS`) | Icons | icon | 84×84 | 42×42 |

- 기준점(왼쪽 아래 원점 정규화): `CottonCandy_*` 막대 아래 끝 (0.5, 0.06), `BaggedCandy_*` 묶은 매듭 (0.5, 0.14). 나머지는 없음.
- 손님 변형: V0 승인 남자(소다 재킷), V1 새 어린이(딸기색, 머리 2, 피부 2, 바지 2), V2 승인 여자 탐험가(모자).
- 지역 번호: 0 동네 골목, 1 시장 앞, 2 강변 축제, 3 별빛 광장. 기계 번호: 0 기본, 1 소다, 2 특급.

## 종류별 규칙 (`KIND_RULES`)

| 종류 | 접지 그림자 | 배경 | 가장자리 투명 여백 | 알파 128 이상 면적 | 카메라 좌우 각 |
|---|---|---|---|---|---|
| character | 있음 | 투명 | 2px 이상 | 8–85% | -20° |
| prop | 있음 | 투명 | 2px 이상 | 6–90% | -20° |
| emote | 없음 | 투명 | 2px 이상 | 15–90% | 절댓값 10° 이하 |
| icon | 없음 | 투명 | 2px 이상 | 15–90% | 절댓값 10° 이하 |
| scene | 없음 | 불투명(알파 255) | – | – | -20° |
| portrait | 없음 | 불투명(알파 255) | – | – | -20° |

불투명 종류는 월드 색에 기대지 않고 형상으로 화면을 꽉 채운 뒤 모듈이 `asset['opaque_backdrop'] = True`를 설정한다. 그때만 드라이버가 `film_transparent`를 끈다.

## 공통 설정

| 항목 | 값 |
|---|---|
| 색 | `PALETTE` 22색만 사용. sRGB HEX를 선형값으로 바꿔 Principled Base Color에 넣는다. 자홍 `#C2577E`은 미나의 머리카락 전용 |
| 재질 | Principled BSDF, Roughness 0.85, Specular IOR Level 0.08 (`toy_kit.Kit.mat`) |
| 키 라이트 | (-2, -3, 7), 900W, 크기 4, `#FFF1DD` |
| 필 라이트 | (4.5, -3.5, 3), 250W, 크기 5, `#DDE6FF` |
| 림 라이트 | (1.5, 3, 5), 70W, 크기 3, `#FFFFFF` |
| 환경 | `#F3ECF7`, 세기 0.75. 세 조명 모두 DISK 면광원이며 (0, 0, 1.25)를 향한다 |
| 카메라 | 직교, 정면(-Y)에서 왼쪽 20°, 내려다보기 15° (`ui_sprite_common.camera`) |
| 렌더 | Cycles 64 샘플, 노이즈 제거, 적응 샘플링 끔, seed 260927, Standard / None, 노출 -1.8, 감마 1, RGBA 8비트 PNG |
| 스레드 | 기본 8. 환경 변수 `UI_SPRITE_THREADS`로 바꾼다(작업 2의 손님 렌더 외에는 4 사용) |
| 검토 배경 | 크림 `#FFF6E7`, 잉크 `#29324D`, 특성 원판 6색(`DISC_COLORS`) |

## 접지 그림자 합성

`render_sprite(scene, sprite_id, output, passes_dir, shadow, size)`는 `shadow`가 `None`이면 한 번 렌더해 그대로 저장한다. 그림자가 있으면 장면의 그림자 캐처를 켠 렌더(`-catcher.png`)와 끈 렌더(`-beauty.png`)를 `passes_dir`에 저장하고 다음을 합성한다.

- 캔버스 픽셀 중심 `(x+0.5, y+0.5)`(Blender의 아래에서 위 순서)과 `anchor`를 카메라에 투영한 점 사이의 타원 거리 `d`, 반지름은 `radii × 캔버스`.
- `t = clamp((d-0.25)/0.75)`, `falloff = 1 - t²(3-2t)`.
- `shadow = min(max_alpha, max(0, (캐처 알파 - a)/max(1-a, 1e-5))) × falloff`, `final = a + shadow(1-a)`.
- 색은 `(c·a + Navy선형값·shadow·(1-a)) / max(final, 1e-5)`. 불투명 오브젝트 픽셀은 beauty 패스와 같다.

`DEFAULT_SHADOW = {'anchor': (.07, .10, 0), 'radii': (.30, .068), 'max_alpha': .32}`이다. numpy float64로 원래 Codex 픽셀 반복문과 같은 연산 순서를 따르므로 승인 PNG와 픽셀이 같다(`tests/test_render_identity.py`). 두 승인 생성기는 계속 `import create_ui_sprites as renderer; renderer.ART = HERE; renderer.render_contact_sprite(scene, entry, output)`를 호출하고, 패스는 `ART/ui-sprite-passes/`에 저장된다.

## 분류 모듈 계약

```python
CATEGORY = 'items'                       # 카탈로그 owner와 같다
ASSETS = [{'id': 'CottonCandy_Strawberry_Small',
           'camera': {'target': (0, 0, 1.2), 'scale': 2.9, 'yaw': -20, 'elevation': 15},
           'shadow': {'anchor': (.07, .10, 0), 'radii': (.30, .068), 'max_alpha': .32},  # 또는 None
           'params': {...}}]               # 자유 형식, 목록 파일에 기록
def build(asset: dict, col, kit) -> None:  # 에셋 하나의 형상을 col에 추가
```

드라이버는 에셋마다 장면 `UIS_<id>`를 만들고, `Kit('<id>_')`와 컬렉션 `UIS_<id>`를 넘긴다. 형상을 만든 뒤 모든 메시의 면 방향을 다시 계산하고, `asset['camera']`로 카메라를, 그림자가 있을 때만 캐처를 둔다. 카탈로그에 없는 id, 다른 분류의 id, 종류 규칙과 다른 그림자 여부, 아이콘·감정 표시의 10° 초과 좌우 각은 오류로 멈춘다. 공용 렌더 설정을 바꾸는 모듈도 오류다.

## 명령

Blender는 항상 배경 모드로 실행한다. 실행 중인 Blender MCP 세션에는 연결하지 않는다.

```powershell
$env:UI_SPRITE_THREADS = '4'
$blender = 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe'

# 분류 하나를 만들고 렌더 (items, shop, locations, machines, icons)
& $blender -b --factory-startup --python-exit-code 1 -P Art/Blender/build_ui_sprites.py -- --category items
#   --only id,id       이 id만 렌더 (.blend와 목록 파일은 항상 분류 전체로 저장)
#   --no-render        .blend와 목록 파일만 저장
#   --out-root DIR     스프라이트, .blend, 목록 파일, 패스를 DIR 아래 같은 상대 경로에 쓴다
#   --module-path DIR  DIR/ui_sprites_<분류>.py 또는 DIR/<분류>.py를 모듈로 쓴다

# 검사: 있는 PNG만 검사, --complete는 66장이 모두 있어야 통과
python Art/Blender/validate_ui_sprites.py
python Art/Blender/validate_ui_sprites.py --complete
python Art/Blender/validate_ui_sprites.py --report <다른 경로.json>   # 기본 Art/Blender/ui-sprites-validation.json
python Art/Blender/validate_ui_sprites.py --owner items --complete   # --owner NAME(반복 또는 쉼표 구분)은 해당 owner(들)의 카탈로그 항목·목록 파일만 검사해, 7개 작업을 병렬로 돌릴 때 한 작업의 미완성 스프라이트가 다른 작업의 검사를 실패시키지 않게 한다; 알 수 없는 owner는 종료 코드 2

# 검토 보드: previews/all.png 또는 previews/<분류>.png
python Art/Blender/preview_ui_sprites.py
python Art/Blender/preview_ui_sprites.py --category items
```

손님(작업 2)과 미나(작업 3)는 각자의 생성기와 `render_sprite`를 쓴다. 승인본 재생성 방법은 명세의 "승인본과 비교 보드 재생성"과 각 폴더의 README를 따른다.

## 검사 항목

`validate_ui_sprites.py`는 카탈로그의 PNG가 있으면 다음을 확인한다.

- RGBA이고 크기가 캔버스와 같다.
- 투명 종류는 가장자리 네 방향 모두 알파 0인 픽셀이 2px 이상이고(접지 그림자 포함), 알파 128 이상 면적이 종류 범위 안이다. 불투명 종류는 모든 픽셀의 알파가 255다.
- 그림자 종류는 `ui-sprite-passes/<owner>/<id>-beauty.png`가 있으면 beauty 패스의 불투명 픽셀이 최종 PNG와 같다.
- `ui_sprites/*.manifest.json`의 렌더 설정(엔진, 샘플, 노이즈 제거, 색 변환, 룩, 노출, 감마, seed, 적응 샘플링)이 기준과 같고, 팔레트와 재질 색이 `PALETTE` 안에 있으며, 재질 레시피가 0.85 / 0.08이다.
- `Assets/CottonCircuit/Sprites/` 아래에 카탈로그에 없는 PNG가 없다.

통과하면 `UI_SPRITES_VALID <있는 수>/66`을 출력한다. 실패하면 항목을 나열하고 종료 코드 1을 돌려준다. 중립 흰색 픽셀이 1%를 넘으면 참고 메시지를 남긴다.

## 시험

```powershell
python -m unittest discover -s Art/Blender/tests -p "test_*.py"                      # 전체 29건(렌더 동일성 1건은 건너뜀)
python -m unittest discover -s Art/Blender/tests -p "test_ui_sprite_spec.py" -v      # 카탈로그 8건
python -m unittest discover -s Art/Blender/tests -p "test_validate_ui_sprites.py" -v # 검사기 15건
python -m unittest discover -s Art/Blender/tests -p "test_customer_sprites.py" -v    # 손님 스프라이트 4건
python -m unittest Art/Blender/tests/test_build_driver.py -v                          # 드라이버 1건, Blender 필요
& $blender -b --factory-startup --python-exit-code 1 -P Art/Blender/tests/test_render_identity.py
& $blender -b --factory-startup --python-exit-code 1 -P Art/Blender/tests/test_render_identity.py -- --customer explorer --wrapper
```

동일성 시험은 승인 `.blend`를 읽기만 하고 `%TEMP%/ui_sprite_identity`에 렌더해 `RENDER_IDENTITY max_abs_diff=0 differing_pixels=0`을 확인한다. 드라이버 시험은 `tests/fixtures/`의 두 시험 분류를 임시 폴더에 렌더하며 `ui_sprites/`에는 아무것도 쓰지 않는다.

## 최종 결과 (2026-09-27, 커밋 18eabd1 기준)

| 항목 | 값 |
|---|---|
| 스프라이트 | 66/66 (`customers` 6, `mina` 1, `items` 21, `shop` 4, `locations` 8, `machines` 3, `icons` 23), PNG 합계 9.3MB 중 `Locations` 7.0MB |
| 검사 | `validate_ui_sprites.py --complete` → `UI_SPRITES_VALID 66/66`, 오류·참고 0건, 목록 파일 5개 통과, 카탈로그 밖 PNG 없음 |
| 투명 종류 | 가장자리 투명 여백 최소 5px(`BaggedCandy_Soda_Large`), 알파 128 이상 면적 16.5%(`Trait_Kart`)–77.6%(`Storefront`) |
| 검토 보드 | `previews/all.png` 2400×16340 |
| 단위 시험 | 29건 중 28건 통과, 1건 건너뜀(렌더 동일성). 렌더 동일성은 Blender에서 따로 실행해 남자·여자 모두 `max_abs_diff=0 differing_pixels=0` |
| Unity | 에디터 검사 98개 통과(스프라이트 관련 14개 포함), 플레이 스모크 성장 434·영업 3319·레거시 625개 통과 |

전체 검증 기록과 교체 전후 캡처 비교는 `docs/lowpoly-sprites-verification.md`에 있다.

## 알려진 제한

- 미나 초상화는 드라이버 대신 `MinaPortrait/create_mina.py`로 만든다. 이 스크립트는 `build_ui_sprites`의 내부 함수(`recalc_normals`, `relative`, `render_settings`, `material_record`)를 가져다 쓰므로, 드라이버에서 이 이름을 바꾸면 함께 고쳐야 한다.
- `validate_ui_sprites.py`는 목록 파일로 `ui_sprites/*.manifest.json`만 읽고, `MinaPortrait/manifest.json`과 손님 폴더의 `manifest.json`은 검사하지 않는다. PNG는 66장 모두 검사한다.
- `previews/icons-states.png`, `previews/machines-locked.png`, `previews/shop-storefront-labels.png`는 작업 중 임시 스크립트로 만든 추가 확인 이미지라 저장소의 명령으로 다시 만들 수 없다. `all.png`와 분류별 보드는 `preview_ui_sprites.py`, `customers-large.png`는 `render_customer_sprites.py`, `locations-street-composite.png`와 `shop-emotes-bubble.png`는 각 분류 모듈, `mina-vs-painting.png`는 `MinaPortrait/make_comparison.py`가 만든다.

# UI 스프라이트 대표 샘플 3종

2026-09-27. 기준은 `docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md`이며, 참고 이미지 A는 형태와 분위기만 참고했다. 공식 Blender MCP의 `get_scene_info`와 `execute_blender_code`를 통해 장면 확인, 생성, 렌더, 원본 재열기 검증을 실행했다.

## 결과물

| 샘플 | 표시 슬롯 | RGBA PNG | 파일 |
|---|---|---|---|
| 딸기 맛 중간 솜사탕 | 77×88 | 154×176 | `Assets/CottonCircuit/Sprites/Items/CottonCandy_Strawberry_Medium.png` |
| 기본 손님 01 | 82×140 | 164×280 | `Assets/CottonCircuit/Sprites/Customers/Customer_01_Neutral.png` |
| 영업시간 벽시계 | 42×42 | 84×84 | `Assets/CottonCircuit/Sprites/Icons/Trait_Hours.png` |

- `UiSprites.blend`: 편집 가능한 일반 Blender 문서. `UI_CottonCandy`, `UI_Customer`, `UI_Clock` 세 장면. 장면마다 이름 있는 에셋 컬렉션이 있고 `UI_Common_Studio` 조명과 환경을 공유한다.
- `create_ui_sprites.py`, `ui_sprite_common.py`: 재생성 스크립트. 같은 스크립트가 `.blend`의 Text 데이터에도 들어 있다.
- `ui-sprites-manifest.json`: 공용 팔레트, 카메라, 조명, 출력 경로와 렌더 설정.
- `ui-sprites-preview.png`: 확대 보기와 두 배경의 실제 표시 크기 비교.
- `ui-sprites-native.png`: 816×304 비교 이미지. 100%로 보면 각 슬롯이 실제 표시 크기다.
- `ui-sprite-passes/`: 손님과 솜사탕의 원본 beauty / shadow catcher 렌더 패스.

## 유지한 특징과 크기

솜사탕은 중심 아이코스피어 1개와 작은 아이코스피어 20개, 크림 막대와 딸기색 띠 4개로 구성했다. 형태와 꼭짓점 변형 seed는 **260927**이다. 성장 슬롯 77×88은 `ShiftUI.cs:121`, 주문 슬롯 80×77은 `ShopStreetUI.cs:48`에서 확인했다. 중간 크기 시작은 1.5바퀴이며 기존 구름의 아래로 내려오는 실루엣과 노출된 막대를 유지했다. 118×124 드래그 그림은 실제 코드에서 포장 솜사탕이므로 이번 샘플에 포함하지 않았다.

손님은 `ShopStreetGraphic.DrawCustomer`의 variant 0을 따른다. 소다색 셔츠, 크림 배지, Hair1 머리, Skin1 피부, Pants1 바지, 분리된 다리와 짙은 신발/크림색 윗부분을 유지했다. 점 눈, 볼 터치, 작은 미소를 모두 메시로 만들었다. 실제 메시 높이 기준 머리 비율은 **2.495등신**이다.

벽시계는 위쪽과 오른쪽 아래를 가리키는 두 바늘을 유지했다. 42×42 슬롯 안에 시계 지름은 약 29px이며 기존 약 28px 그림에 가깝다. 분류 원판과 선택 원은 포함하지 않았다. 아이콘에는 바닥과 접지 그림자가 없다.

주문 슬롯 80×77에서 같은 솜사탕 PNG를 비율 유지로 맞추면 약 67×77이 된다. 기존 절차형 그림은 전체 슬롯 비율로 그려지므로, 이 수치는 추후 UI 적용 시 확인할 사항이다. 이번 결과는 3개 샘플 제작이며 기존 Unity UI 바인딩·배치·동작과 레이싱 3D를 수정하지 않았다.

## 공통 설정

| 항목 | 값 |
|---|---|
| 색 | 명세의 21개 HEX 공용 팔레트. sRGB를 scene-linear로 변환하여 Principled Base Color에 입력 |
| 셰이딩 | 모든 메시 flat. 둥근 부분은 ico subdivision 1–2, 상자는 1단계 작은 bevel |
| 키 | (-2, -3, 7), 900W, 크기4, #FFF1DD |
| 필 | (4.5, -3.5, 3), 250W, 크기5, #DDE6FF |
| 림 | **(1.5, 3, 5), 70W, 크기3, #FFFFFF** |
| 환경 | #F3ECF7, strength0.75 |
| 면광원 | DISK, 공통 조준점 (0, 0, 1.25) |
| 카메라 | 직교, 정면 -Y에서 좌측20°, 내려다보기15°. 시계 좌측6°, 내려다보기15° |
| 렌더 | Cycles, CPU8 threads, 64 samples, denoise, adaptive sampling 끔 |
| 컬러 관리 | Standard / None, **exposure -1.8**, gamma1 |
| 출력 | 투명 film, RGBA8 PNG, 표시 슬롯의 정확히2배 캔버스 |
| 비교 배경 | 실제 UI 크림 #FFF6E7 / 명세 잉크 #29324D |

노출은 딸기색 한 채널이 포화되는 문제를 제거하기 위해 공통으로 -1.8로 정했다. 최종 세 PNG의 불투명 픽셀에는 255로 잘린 색 채널이 없다. 시험 이미지보다 차분한 색이며, 재질 기준색은 시험 이미지에서 추출하지 않았다. 크림 재질은 명세 #FFF1D4를 사용하고, 비교용 밝은 배경만 실제 UI #FFF6E7을 썼다.

손님과 솜사탕은 실제 Cycles shadow catcher 패스와 별도 beauty 패스를 렌더한다. 캐처의 그림자 알파에서 오브젝트 알파를 분리하고, 접지점 주변 타원형 smoothstep 감쇠와 최대 알파0.32를 적용해 짧게 합성한다. 그림자 색은 공용 Navy다. 이 처리는 무한 캐처 평면에 남는 미세한 알파/긴 그림자만 정리하며 불투명 오브젝트 픽셀은 beauty 패스와 완전히 같다. 모든 합성은 Blender 스크립트에서 수행하며, Pillow는 검증과 비교 이미지 배치에만 쓴다.

## 재생성

프로젝트 루트에서 전체 재생성:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --factory-startup -t 8 --python-exit-code 1 -P 'Art/Blender/create_ui_sprites.py'
python Art/Blender/validate_ui_sprites.py
python Art/Blender/preview_ui_sprites.py
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b 'Art/Blender/UiSprites.blend' --python-exit-code 1 -P 'Art/Blender/audit_ui_sprite_scene.py'
```

수동으로 편집한 모델을 보존하고 다시 렌더하려면 `UiSprites.blend`를 연 뒤 Blender Python Console 또는 MCP의 `execute_blender_code`에서 실행한다:

```python
import runpy
runpy.run_path('D:/UnityProjects/RacingTycoon/Art/Blender/create_ui_sprites.py',
              init_globals={'UI_ACTION': 'render'}, run_name='__main__')
```

`UI_ACTION='build'`는 모델을 재생성하고, 기본값 `'all'`은 생성과 렌더를 모두 수행한다. 모델을 수동 편집한 후에는 `'render'`를 사용한다. 파일 경로/캔버스는 manifest와 일치시켜야 한다. MCP 연결 실행법과 공식 소스 버전은 `mcp_tools/README.md`에 있다. 태스크 전용 서버를 사용했으며 전역 Codex 설정이나 Blender 시작 파일은 변경하지 않았다.

Unity UI에 연결하는 작업은 이번 범위에 포함하지 않았다. 추후 임포트 시 Sprite (2D and UI), mipmap 끔, alpha transparency 켬, 고품질 압축을 적용한다.

## 검증 결과

- `validate_ui_sprites.py`: 3/3 PASS, RGBA/크기/알파 여백/개수 검사, 경고0.
- 최소 투명 여백: 솜사탕7px, 손님15px, 시계12px. 그림자까지 포함한 값이다.
- `.blend`를 다시 열어 `audit_ui_sprite_scene.py` 실행: **177개 검사 PASS**. 실제 카메라 각도, 공유 조명, HEX→linear 노드값, 플랫 면, ico 분할, 20개 돌기를 검사했다.
- beauty 패스 대비 불투명 픽셀: 솜사탕6,149개와 손님17,084개 모두 동일, 최대 차이0.
- 같은 seed로 전체 재생성: 세 이미지의 **디코딩된 RGBA 픽셀 SHA256 동일**. PNG에 기록되는 렌더 시간 메타데이터는 비교에서 제외했다.
- 밝은 배경/잉크 배경에서 실제 표시 크기를 직접 확인하고 독립 검토했다.
- 상세 증거: `ui-sprites-validation.json`, `ui-sprites-scene-audit.json`, `ui-sprites-reproducibility.json`.

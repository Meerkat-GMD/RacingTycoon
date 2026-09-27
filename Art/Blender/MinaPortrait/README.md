# 미나 초상화 — Mina_Portrait

준비 화면의 가게 친구 초상화(`Resources/Progression/NpcPortrait.png`의 페인팅)를 승인된 두 손님과 같은
"파스텔 토이" 로우폴리 상반신 렌더로 바꾼 원본이다. 페인팅 파일은 커밋 `af2e25f`에서 삭제했다.
`make_comparison.py`는 `git show e8e5987:Assets/CottonCircuit/Resources/Progression/NpcPortrait.png`로 읽는다. 기준 문서는
`docs/superpowers/specs/2026-09-26-lowpoly-art-concept-design.md`이며, 공용 설정은 `../UI_SPRITES.md`를 따른다.

- `create_mina.py`: 몸, 앞치마, 팔과 손, 솜사탕, 가게 배경을 만들고 검사·저장·렌더한다.
- `mina_head.py`: 얼굴, 점 눈·볼 터치·열린 웃는 입, 자홍색 단발 머리.
- `make_comparison.py`: 페인팅과 렌더를 나란히 놓은 `../previews/mina-vs-painting.png`를 만든다.
- `MinaPortrait.blend`: 컬렉션별로 편집 가능한 원본(`MN_Mina_Portrait` 장면, 두 스크립트를 Text로 포함).
- `manifest.json`: 렌더 설정, 팔레트, 조명, 재질 레시피, 카메라, 구도·덮임 검사 결과, 오브젝트와 재질 목록.
- 결과물: `Assets/CottonCircuit/Sprites/Characters/Mina_Portrait.png` (808×784 불투명 RGBA, 표시 404×391).

## 재생성

프로젝트 루트에서 실행한다. 실행 중인 Blender MCP 세션에는 연결하지 않는다.

```powershell
$env:UI_SPRITE_THREADS = '4'
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --factory-startup --python-exit-code 1 -P Art/Blender/MinaPortrait/create_mina.py
python Art/Blender/validate_ui_sprites.py --owner mina --report <보고서 경로.json>
python Art/Blender/preview_ui_sprites.py --category mina
python Art/Blender/MinaPortrait/make_comparison.py
```

`create_mina.py -- --preview <경로.png>`는 50% 크기, 16샘플 확인용 렌더만 쓰고 다른 파일은 건드리지 않는다.
`-- --no-render`는 검사 후 `.blend`와 `manifest.json`만 저장한다.

기본 실행은 다음을 확인한 뒤에만 저장·렌더한다.

- 구도: 머리·머리카락, 두 손, 솜사탕, 앞치마 가슴판의 모든 꼭짓점이 카메라 화면 안(0–1)에 있다.
  화면 아래 끝은 앞치마 허리띠 바로 아래다.
- 덮임: 필름 투명을 켠 202×196 저해상도 렌더에서 알파 255가 아닌 픽셀이 0개다. 배경색에 기대지 않고
  형상이 화면을 꽉 채운다는 뜻이다. 최종 렌더는 필름 투명을 끄고 `ui_sprite_render.render_sprite(..., shadow=None)`로 저장한다.

## 설정

- 남자 손님과 같은 작성 단위(얼굴 링 3.55–4.36)로 만들고 모든 오브젝트를 0.5배 해 공용 조명(목표점 (0,0,1.25))을 그대로 쓴다.
- 재질은 `toy_kit.Kit('MN_')`만 쓴다: 팔레트 색, Principled BSDF, Roughness 0.85, Specular IOR Level 0.08. 이미지 텍스처, 윤곽선, 부드러운 셰이딩 없음.
- 공용 `ui_sprite_common.studio()`, `setup_scene()`, `camera()`를 바꾸지 않고 쓰고, `thread_override()`로 `UI_SPRITE_THREADS`를 적용한다: Cycles 64샘플, 노이즈 제거, 적응 샘플링 끔, seed 260927, Standard / None, 노출 -1.8, 감마 1.
- 카메라: 직교, 좌우 -20°, 내려다보기 15°, 목표 (0.02, 0, 1.72), 직교 크기 1.62.

## 미나의 구성

| 부분 | 형태 |
|---|---|
| 얼굴 | 탐험가 얼굴 윤곽을 남자 얼굴 높이에 둔 각진 얼굴(Skin1/Skin3), 남자와 같은 크기·간격의 Navy 점 눈, Strawberry 볼 터치, 작은 쐐기 코, 살짝 벌린 Navy 웃는 입과 작은 Strawberry 혀 |
| 머리 | 자홍(Magenta)만 사용. 이어진 정수리 껍질, 관자놀이에서 뒤를 돌아 반대편까지 이어진 종 모양 단발 껍질(세로 물결 면, 오르내리는 끝선), 그 위의 곡선 다발 11개(끝이 바깥으로 뻗거나 안으로 말림), 미나의 오른쪽 가르마에서 왼쪽 관자놀이로 넘긴 앞머리 다발 5개. 다발은 `explorer_head._hair_clump`의 여섯 면 단면을 머리 표면 방향으로 돌려 쓴다 |
| 셔츠 | Cream 긴소매, 목 스탠드와 접힌 뾰족 칼라 두 장, Cream 소매 끝단 |
| 앞치마 | Mint 가슴판(가운데 접힘 두 면), 어깨끈, 둥근 물결 주름 장식(`_strip` 방식), 허리띠, 주머니, Vanilla 리본 |
| 솜사탕 | 커밋 966cf73 `cotton()`의 방식: 조금 큰 중심 구와 seed 260927의 구 20개, 꼭짓점 흔들기. 초상화에서는 품목 스프라이트보다 네 배쯤 크게 보이므로 구의 각도·크기 편차를 넓혀 고른 산딸기가 아니라 구름처럼 보이게 했다. Cream 막대에 Strawberry 줄무늬, 위에 Strawberry 딸기와 Mint 꼭지 |
| 자세 | 두 손이 가슴 앞에서 막대를 위아래로 쥔다. 팔꿈치는 옆구리 아래, 아래팔은 안쪽 위로 비스듬히 |

## 배경

캔버스 전체를 형상으로 채운다. 미나가 먼저 읽히도록 Cream/White 벽판과 Base 징두리가 대부분을 차지한다.

- 벽: Cream과 White가 번갈아 놓인 세로 판, 아래쪽 Base 징두리와 White 걸레받이 레일.
  벽은 작성 단위 y=2.45에 두어 림 라이트가 벽 위를 지나 미나에게 닿는다.
- 위: Strawberry/White 물결 차양 띠와 Wood 레일.
- 왼쪽: Base 틀의 작은 창. White 하늘, Mint 언덕, 분홍 도로와 흰 선, Cream 깃대의 Navy/White 체크 깃발(레이싱 쪽 연결).
- 오른쪽: Wood 선반 두 단의 병. 아래는 맛 색(Soda/Strawberry/Vanilla) 솜사탕, 위는 White 유리, Gold 뚜껑.
- 오른쪽 가장자리: Soda 솜사탕 기계. 받침과 그릇 위 분홍 솜사탕을 Soda 살 돔이 덮는다.
- 왼쪽 아래: Wood 선반 위 White 컵의 줄무늬 막대.

## 페인팅과 다른 점

- 자홍 `#C2577E`은 명세가 정한 재질 기준색이다. 같은 조명과 노출 -1.8에서 렌더된 머리의 평균색은 약 `#943E58`~`#9B425D`
  (측정 영역에 따라 다름)로, 페인팅 머리의 평균색 `#9A535A`보다 조금 어둡고 채도가 높은 분홍빛으로 보인다.
- 로우폴리 토이 비율(큰 머리) 때문에 어깨가 화면에서 차지하는 폭이 페인팅보다 좁다.
- 유리 돔과 유리병은 투명 재질을 쓰지 않는 규칙에 따라 살 돔과 두 색 병으로 옮겼다.

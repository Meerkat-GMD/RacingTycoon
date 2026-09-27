# Female explorer — reference redesign

새로 첨부한 여자 탐험가 이미지를 참고하여 제작한 Blender 원본과 UI 렌더다.
남자 손님과 같은 작은 남색 점 눈, 크림색 모자와 따뜻한 색의 챙, 분홍 목도리,
크림색 상의와 두 갈래의 긴 분홍 코트, 짧은 녹색 치마와 맨다리,
접힌 부츠, 큰 갈색 배낭과 끈을 잡은 비대칭 자세를 반영했다.
게임 공용 팔레트·스튜디오를 사용하고 숲 배경은 스프라이트에 포함하지 않았다.

- `GameCustomerFemaleExplorer.blend`: 컬렉션별 편집 가능한 원본; 참고 이미지도 내부에 포함.
- `Customer_02_Explorer.png`: 투명 RGBA PNG 164×280; 표시 크기 82×140.
- `Customer_02_Explorer-large.png`: 같은 카메라의 656×1120 확대 렌더.
- `Customer_02_Explorer-preview.png`: 밝은 배경의 확대 보기.
- `reference-comparison.png`: 첨부 참고 이미지와 Blender 결과의 형태 비교.
- `female-explorer-comparison.png`: 밝은 배경/#29324D, 이전 여자 손님과 새 캐릭터의 실제 크기 비교.
- `create_explorer.py`, `explorer_head.py`, `explorer_lib.py`: 모델 재생성 소스.
- `reference.png`: 이번에 사용자가 첨부한 참고 이미지.
- `manifest.json`: 팔레트·카메라·조명·출력 설정과 오브젝트 목록.
- `scene-validation.json`, `explorer-png-validation.json`: 원본 및 출력 검증.
- `mcp-build.json`, `mcp-verification.json`: 실제 Blender MCP 실행 기록.
- `hair-before-after.png`: 같은 구도에서 머리카락 수정 전후 확대 비교.
- `mcp-hair-revision.json`, `hair-revision-validation.json`: 머리카락 교체 기록과 다른 오브젝트가 유지됐는지 검사한 결과.
- `revisions/hair-v1/`: 수정 전 원본·머리 생성 코드·PNG 백업.
- `eyes-before-after.png`: 여자 캐릭터 눈 수정 전후, 남자 캐릭터의 눈, 두 배경의 실제 표시 크기 비교.
- `mcp-eye-revision.json`, `eye-revision-validation.json`: 눈 교체 기록과 다른 오브젝트의 동일성 검사.
- `revisions/eyes-v1/`: 눈 수정 직전 원본·머리 생성 코드·PNG·설정 백업.

## 눈 수정

승인된 남자 손님의 눈과 같은 크기·간격·1단 bevel을 적용했다.
흰자·홍채·반사광·속눈썹·눈썹을 제거하고 Navy #29324D의 작은 직사각형 두 개로 표현한다.
생성 좌표계에서 크기는 0.075×0.020×0.106, 중심 간격은 0.410, bevel은 0.006이다.
여자 얼굴의 턱 높이에 맞춰 z를 0.330 올리고 얼굴 표면 앞에 배치했다. 모델 전체 배율은 0.48이다.
머리카락·얼굴 바탕·볼·코·입·의상·카메라·조명이 그대로인지 오브젝트 데이터 해시로 검사한다.

## 머리카락 수정

넓고 곧은 판 형태의 머리 세 가닥을 제거하고, 이어진 두피 바탕과 12개의 곡선형 머리 다발로 다시 만들었다.
모자 아래에 뿌리를 묻고 각 다발의 폭·두께를 끝으로 갈수록 줄였다. 앞머리는 한쪽으로 흘러가며,
관자놀이 머리는 두상을 따라 휘어 귀와 눈을 가리지 않는다. 짧은 뒷머리는 목도리 위에서 끝난다.
머리 재질은 공용 팔레트의 Navy를 쓰고, 면 방향으로 음영을 만든다.
모자·얼굴·의상·몸·카메라·조명은 오브젝트별 데이터 해시로 수정 전후 동일함을 검사한다.

## 재생성

프로젝트 루트에서 실행한다.

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --factory-startup --python-exit-code 1 -P Art/Blender/GameCustomerFemaleExplorer/create_explorer.py
python Art/Blender/GameCustomerFemaleExplorer/make_preview.py
python Art/Blender/GameCustomerFemaleExplorer/validate_png.py
```

Blender MCP `execute_blender_code`:

```python
import runpy
runpy.run_path('D:/UnityProjects/RacingTycoon/Art/Blender/GameCustomerFemaleExplorer/create_explorer.py', run_name='__main__')
```

`EX_ACTION='build'`는 모델 생성, `EX_ACTION='render'`는 편집한 모델을 유지한 재렌더다.
`runpy.run_path(..., init_globals={'EX_ACTION':'render'}, run_name='__main__')`처럼 전달한다.
`../ui_sprite_common.py`의 게임 팔레트·조명과 `../create_ui_sprites.py`의 접지 그림자 처리를 재사용한다.
재생성 시에는 프로젝트 내 상대 위치를 유지한다. 모델 생성 코드와 공통 설정은 .blend의 Text 데이터에도 저장된다.

## 설정

- Flat shading, 윤곽선 없음. 넓은 다각형 면과 1단 bevel, 무작위 seed 260927.
- 직교 카메라: 좌측 20°, 내려다보기 15°.
- Key 900 W / Fill 250 W / Rim 70 W / 환경광 0.75.
- Cycles 64 samples, denoise, Standard / None, exposure -1.8.
- 공용 팔레트만 사용; 투명 배경에 짧은 실제 Cycles 접지 그림자.

이전 여자 손님과 남자 손님 파일은 보존했다. Unity UI·게임 동작 및 레이싱 장면에는 변경이 없다.

# Faceted game customer — Customer01

Blender MCP로 제작한 기본 손님 스타일 시안. 첨부한 모험가의 큰 다각형 면,
비대칭 머리 덩어리, 두께가 있는 겹옷을 게임의 일상복으로 옮겼다.
기존 손님의 소다색 옷, 어두운 머리, 크림색 배지, 보라색 바지와 밝은 신발을 유지한다.
참고 이미지처럼 몸을 길게 잡은 약 3.6등신 시안이며, 이전 2.5등신 시안과 비교할 수 있다.

- `GameCustomerFaceted.blend`: 컬렉션별로 편집 가능한 원본.
- `Customer_01_Faceted.png`: 투명 RGBA PNG, 164×280. UI 표시 82×140의 2배.
- `Customer_01_Faceted-large.png`: 같은 카메라의 656×1120 확인용 렌더.
- `customer-style-comparison.png`: 이전 시안과 새 시안, 밝은 배경과 #29324D 배경,
  실제 82×140 크기 비교.
- `Customer_01_Faceted-preview.png`: 새 캐릭터 확대 보기.
- `create_customer.py`, `customer_head.py`, `customer_lib.py`: 재생성 스크립트.
- `manifest.json`: 팔레트, 카메라, 공통 조명, 렌더 및 출력 설정.
- `scene-validation.json`, `customer-png-validation.json`: 검증 결과.
- `mcp-build.json`, `mcp-verification.json`: 실제 Blender MCP 실행 기록.

## 재생성

프로젝트 루트에서 실행:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --factory-startup --python-exit-code 1 -P Art/Blender/GameCustomerFaceted/create_customer.py
python Art/Blender/GameCustomerFaceted/make_preview.py
python Art/Blender/GameCustomerFaceted/validate_png.py
```

Blender MCP `execute_blender_code`에서는 다음을 실행한다:

```python
import runpy
runpy.run_path('D:/UnityProjects/RacingTycoon/Art/Blender/GameCustomerFaceted/create_customer.py', run_name='__main__')
```

`GC_ACTION='build'`는 모델 생성, `GC_ACTION='render'`는 현재 모델을 유지한 재렌더다.
`runpy.run_path(..., init_globals={'GC_ACTION':'render'}, run_name='__main__')`처럼 전달한다.

공용 `../ui_sprite_common.py`의 팔레트와 조명 및 `../create_ui_sprites.py`의
접지 그림자 처리를 재사용한다. 스크립트 파일은 원본 .blend의 Text 데이터에도 들어 있다.
CLI 재생성은 프로젝트 내 상대 위치를 유지해야 한다.

## 설정

- Flat shading, 윤곽선 없음. 머리와 옷은 직접 만든 다각형 면, 신발과 가방은 1단 bevel.
- 직교 카메라: 정면에서 좌측 20°, 내려다보기 15°.
- 공통 조명: Key 900 W, Fill 250 W, Rim 70 W, 환경광 0.75.
- Cycles 64 samples, denoise, seed 260927, Standard / None, exposure -1.8.
- 팔레트: 명세의 공용 및 캐릭터 보조 팔레트만 사용.
- 투명 배경, 실제 Cycles shadow catcher에서 얻은 짧은 접지 그림자.

시안은 이 폴더에 저장된다. 기존 레이싱 3D, Unity UI 배치·동작, 이전 3종 샘플은 수정하지 않았다.

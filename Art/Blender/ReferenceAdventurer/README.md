# 첨부 캐릭터 원본 재현

사용자의 “일단 해당 캐릭터를 똑같이 만들어줘” 요청에 따라, 이전 UI 샘플의 2.5등신과 파스텔 팔레트 대신 첨부 이미지의 비율·색·복장을 따라 새로 모델링했다. Blender MCP로 생성·렌더했다.

- `ReferenceAdventurer.blend`: 편집 가능한 원본. 머리/머리카락, 코트, 팔/장갑, 숄, 벨트/주머니, 다리/부츠, 검을 컬렉션으로 분리했다. 참고 이미지와 생성 스크립트를 내부에 포함했다.
- `ReferenceAdventurer.png`: 768×1024 RGBA. 배경 투명, 원본의 녹회색 접지 면 포함.
- `ReferenceAdventurer-preview.png`: 흰 배경 미리보기.
- `reference-comparison.png`: 원본과 생성 결과를 비슷한 높이로 배치한 비교.
- `create_reference.py`, `artlib.py`, `head.py`: 재생성 소스.
- `manifest.json`: 색, 카메라, 렌더 설정과 오브젝트 목록.

재현한 특징: 약4.4등신, 모래색/회색의 각진 머리, 사각 눈과 짧은 눈썹, 적갈색 겹친 숄과 술 장식, 긴 갈색 코트, 대각선 어깨끈과 골색 버클, 주머니, 손가락이 드러난 장갑, 바깥쪽을 향한 부츠, 왼손에 쥔 아래 방향 검. 주변에 별도로 놓인 횃불·보석·상자 등은 모델에 포함하지 않았다. 보이지 않는 뒷면은 앞면 디자인에 맞춰 구성했다.

모든 형상은 편집 가능한 메시이며 텍스처 그림을 입힌 평면이 아니다. 플랫 셰이딩, 직교 카메라(yaw -3°, elevation12°), Cycles64 samples/denoise, Standard/None, exposure -1.05를 사용했다. 기존 게임 코드와 세 UI 샘플 파일은 수정하지 않았다.

프로젝트 루트에서 재생성:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --factory-startup --python-exit-code 1 -P 'Art/Blender/ReferenceAdventurer/create_reference.py'
python Art/Blender/ReferenceAdventurer/make_preview.py
```

편집한 모델을 그대로 다시 렌더하려면 `.blend`를 열고 Blender Python Console 또는 MCP `execute_blender_code`에서:

```python
import runpy
runpy.run_path('D:/UnityProjects/RacingTycoon/Art/Blender/ReferenceAdventurer/create_reference.py',
              init_globals={'RA_ACTION':'render'}, run_name='__main__')
```

접지 면을 제외한 캐릭터만 필요하면 `RA_Contact_Ground` 컬렉션의 렌더 표시를 끄고 재렌더한다.

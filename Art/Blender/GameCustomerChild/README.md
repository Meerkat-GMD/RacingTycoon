# Child customer — Customer_V1

거리 손님 세 번째 변형(`Variant % 3 == 1`)인 어린이 손님의 Blender 원본이다.
승인된 남자·여자 손님과 같은 넓은 면의 플랫 셰이딩, 공용 팔레트, 조명, 카메라를 쓰고
키가 작은 약 3등신 비율로 실루엣부터 구분한다.

- 딸기색(Strawberry) 후드티: 목 뒤로 내린 후드, 후드 옆의 둥근 귀 두 개(Cream 안쪽), Cream 끈, 캥거루 주머니.
- Hair2 짧은 바가지 머리와 정수리의 작은 삐침 머리.
- Skin2 얼굴(앞면·옆면 모두 Skin2), 목 그늘만 Hair2. Skin3은 쓰지 않는다.
- 어른과 같은 얼굴 규칙: Navy 점 눈 .075×.020×.106(x = ±.205, 머리 배율 1), Strawberry 볼 터치, 작은 Navy 입(`CH_Face_Smile`).
- Pants2 반바지, Cream/Strawberry 줄무늬 양말, Cream 밑창의 Navy 운동화(Strawberry 뒤꿈치 탭).
- Cream 끈으로 몸에 둘러멘 작은 Mint 동전 지갑(Gold 잠금쇠).

## 파일

- `GameCustomerChild.blend`: 컬렉션 `CH_Head_Hair`, `CH_Hoodie`, `CH_Arms_Hands`, `CH_Shorts_Shoes`, `CH_Purse`로 나눈 원본. 장면 `Game_Customer_Child`. 생성 코드도 Text 데이터로 들어 있다.
- `create_child.py`: 몸·옷·소품과 장면 생성. `child_head.py`: 얼굴·머리와 화난 얼굴 조각(`build_angry_face`).
- `manifest.json`: 정체성, 비율, 사용 팔레트, 재질 규칙, 조명, 카메라, 렌더 설정, 출력 경로, 오브젝트 목록.
- `Customer_V1-large.png`: 기본 표정 656×1120 확대 렌더. `review/`에 기본·화남 확대 렌더가 있다.
- 게임용 PNG: `Assets/CottonCircuit/Sprites/Customers/Customer_V1_{Neutral,Angry}.png` (164×280, 표시 82×140).

## 좌표와 비율

Z 위, -Y 앞. 남자 손님과 같은 작성 단위와 전체 0.5배 모델 배율, 같은 카메라 호출
`c.camera(scene, (0, -.015, 1.22), 2.85)`과 같은 그림자 캐처를 쓴다. 그래서 발이 남자와
같은 기준선에 서고 머리 끝은 남자 키의 약 75%에 온다. 머리는 남자 얼굴 좌표로 작성한 뒤
`child_head._place`가 턱을 기준으로 `HEAD_SCALE`(1.0)만큼 통째로 배율을 주고 턱 높이 2.29로 내린다.
눈만 따로 키우지 않는다.

화난 표정은 Navy 눈썹 두 조각(안쪽이 25° 낮음, .15×.055 — Skin2 위에서 읽히도록 어른의 .14×.045보다 조금 크다)과
아래로 처진 입(`CH_Face_Angry_*`)이다. 원본에서는 숨겨 두고 렌더 때만 웃는 입과 바꾼다.

## 재생성

프로젝트 루트에서 실행한다. `UI_SPRITE_THREADS`는 설정하지 않는다(손님 스프라이트는 8 스레드 고정).

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --factory-startup --python-exit-code 1 -P Art/Blender/GameCustomerChild/create_child.py
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --factory-startup --python-exit-code 1 -P Art/Blender/render_customer_sprites.py -- --only V1 --large
python Art/Blender/validate_ui_sprites.py --owner customers --report <보고서 경로>
python Art/Blender/preview_ui_sprites.py --category customers
```

`CH_ACTION`의 기본값은 `build`다. `render`는 현재 장면(메모리의 수정 포함)을 `render_customer_sprites.render_customer`로
다시 렌더하고, `all`은 둘 다 한다. `runpy.run_path(..., init_globals={'CH_ACTION': 'render'}, run_name='__main__')`처럼 전달한다.

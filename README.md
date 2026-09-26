# 솜사탕 서킷 · Cotton Circuit

돈으로 특성을 구매하고, 레이싱으로 솜사탕을 만들어 판매하는 Windows용 Unity 게임입니다. **영업 준비 → 3분 영업 → 정산 → 성장**을 반복합니다.

## 실행

- 최신 완성 빌드: `Builds/Progression/CottonCircuit.exe`. 같은 폴더의 Data 폴더와 DLL도 함께 필요합니다.
- Unity **6000.5.3f1**에서 `Assets/CottonCircuit/Scenes/CottonCircuit.unity`를 열고 Play를 눌러도 됩니다.
- 처음에는 다운힐 쿠페와 무료 딸기 설탕으로 시작합니다. 이니셜 D 느낌의 가속·코너 감속 주행이 기본입니다. 자동 주행이 켜져 있으며 상단 버튼으로 수동 운전으로 바꿀 수 있습니다.

## 영업 준비

왼쪽은 현재 장소의 행동 화면, 오른쪽은 NPC와 페이지 이동 버튼입니다. 설명은 대부분 마우스를 올렸을 때 표시됩니다.

- **성장 지도:** 6개 탭에 30개 특성, 총 58단계. 같은 탭의 선행 특성은 아래쪽 화살표로 이어지고, 다른 탭의 조건은 특성 아래에 표시됩니다. 조건 이름을 누르면 해당 특성으로 이동합니다. 모든 선행 특성을 1단계 이상 구매해야 다음 특성을 구매할 수 있습니다. 돈만 사용하며 구매한 장비·알바는 즉시 지급됩니다. 가격·효과는 호버로 확인합니다. 드래그 이동, 휠/+·− 확대·축소, 전체 보기를 지원합니다.
- **기계와 레시피:** 기계 3대, 알바 2명, 설탕 3등급, 맛 3종, 주행 스타일 2종. 다운힐 쿠페는 처음부터 사용할 수 있고, 클래식 카트는 차량 특성에서 해금합니다. 알바에게 기계와 반복할 맛·크기를 지정합니다. 상위 기계를 맡기려면 알바 교육이 필요합니다.
- **오늘의 장사:** 4개 지역 중 해금한 곳을 선택하고 영업을 시작합니다. 지역마다 손님 구성, 방문 간격, 판매 단가가 달라집니다.

## 만들고 판매하기

1. 설탕 봉지를 왼쪽 주행 화면으로 끌어 위아래로 흔들면 흔든 폭에 비례해 투입합니다. 작은 흔들림은 소량, 큰 흔들림은 최대10g이며 미세한 떨림은 무시합니다. **1등급 딸기 설탕은0원**이며, 고급 설탕은 실제 투입량의 재료비를 코인 단위로 올림해 즉시 결제합니다.
2. 전진한 거리만큼 솜사탕이 커집니다. 처음에는 크기 구분이 없고, 설탕 등급을 해금하면 소·중·대가 표시됩니다. 설탕과 기계 중 낮은 등급이 최대 크기를 정합니다.
3. **F / 꺼내기**로 진열대에 보관하고 맞는 손님에게 드래그해 판매합니다. 잘못된 주문에 전달하면 제품을 잃습니다.
4. 영업 중 상단 기계 버튼으로 코스와 기계를 바꿉니다. 각 기계의 제작 상태가 유지됩니다. 알바 배치 기계는 자동 생산하며, 직접 운전 중인 기계는 플레이어가 담당합니다.

알바는 재료비가 부족하거나 진열대가 가득 차면 기다립니다. 일급은 없습니다. 직접 만들던 제품을 넘기면 원래 맛과 등급 한도로 먼저 마무리한 뒤 지정 메뉴로 돌아갑니다.

진열대 제품을 주행 화면으로 가져오면 이어 만들거나 현재 제품과 교환합니다. 제품에 사용한 설탕보다 낮은 등급 설정에서는 이어 만들 수 없습니다. 원래 제품의 크기 한도는 유지됩니다.

## 조작

| 입력 | 동작 |
|---|---|
| 설탕 봉지 드래그 후 위아래 흔들기 | 설탕 투입 |
| F / 꺼내기 | 솜사탕 추출 |
| 우클릭 / 설탕 비우기 | 남은 설탕만 버리기 |
| 제품을 손님 / 쓰레기통에 드래그 | 판매 / 폐기 |
| 제품을 주행 화면에 드래그 | 이어 만들기 / 교환 |
| 기계 버튼 | 해당 기계와 코스로 전환 |
| W / A D / S | 가속 / 조향 / 제동 |
| Space / Shift / R | 드리프트 감속 / 클래식 카트 전용 부스터 / 코스 복귀 |
| Esc | 전체 일시정지 |

기본 대기시간은90초, 방문 간격은26초입니다. 광고·지역·기다림 특성으로 변경됩니다. 성장·설탕 절약·품질·판매가·조향·속도 특성이 실제 제작과 판매에 적용됩니다.

## 정산과 저장

매출은 판매 즉시 입금됩니다. 마감 화면에서는 재료비와 순이익을 확인하며 보상을 중복 지급하지 않습니다. 준비 화면으로 돌아가면 진열대·설탕·미완성 제품을 비우고, 다음 영업 시작 시 날짜가 증가합니다. 돈과 모든 구매 내역·장비·알바 배치는 유지됩니다.

V8 자동 저장: `%USERPROFILE%\AppData\LocalLow\SugarRoad Studio\Cotton Circuit\cotton-circuit.json`. 준비/영업/정산 단계, 차량 선택, 모든 기계의 제작 상태와 레시피가 저장됩니다. 정상 백업이 있으면 손상된 저장을 복구하고, 복구 불가 시 원본 덮어쓰기를 차단합니다.

V1–V6 저장의 돈과 기존 영구 업그레이드를 성장 지도에 이전하고 준비 화면에서 시작합니다. 이전 영업의 재고와 미완성 재료는 초기화하며 이전 파일은 백업으로 남습니다. **도움말 → 새 가게 시작**은 기존 저장을 보관한 뒤 초기화합니다.

V7의 차량 해금 전 기본 카트 선택은 다운힐로 이전합니다. 돈·특성·영업 시간·제작 상태는 유지합니다. 이미 차량 특성을 해금한 저장의 선택은 유지되며, 기존 쿠페 해금 내역으로 클래식 카트를 선택할 수 있습니다.

Unity에서 `Assets/CottonCircuit/Scenes/CottonCircuit.unity`를 열고 Hierarchy의 **Cotton Circuit → Game Controller → 설탕 흔들기 → 10g 흔들기 폭 (px)**을 조절합니다. 범위는24~600, 기본값은132이며, 값이 클수록 같은 동작에서 적게 들어갑니다. 플레이 모드에서도 변경 즉시 적용됩니다. 값을 계속 유지하려면 플레이 모드를 종료한 뒤 Inspector에서 설정하고 씬을 저장하세요. 게임 내 조절 UI와 별도 설정 파일은 사용하지 않습니다.

설계 결정과 후속 판단 사항은 `docs/progression-design-notes.md`, 성장 시간 가정은 `docs/progression-balance.md`, 실제 검증 기록은 `docs/progression-verification.md`에 있습니다.

## Blender 원본

- `Art/Blender/CottonCircuit.blend`: 편집 가능한 원본, 에셋별 컬렉션과 프리뷰 장면.
- `Art/Blender/preview.png`: 에셋 전체 미리보기.
- `Art/Blender/create_assets.py`: 모델과 FBX를 재생성하는 Blender Python 스크립트.
- `Art/Blender/validate_assets.py`: 모델 크기, 재질, FBX 재임포트 검증.
- `Art/Blender/RacingProps.blend`: 방향 표지·가드레일·지름길 게이트 원본.
- `Art/Blender/create_racing_props.py`, `validate_racing_props.py`: 새 레이싱 소품 생성과 검증.
- `Art/Blender/ShopProps.blend`: 확장 진열대·주문판·대기 표지 원본.
- `Art/Blender/create_shop_props.py`, `validate_shop_props.py`: 가게 소품 생성과 검증.
- `Art/Blender/MapLandmarks.blend`: 캔디 터널·결승 표지 원본.
- `Art/Blender/create_map_landmarks.py`, `validate_map_landmarks.py`: 코스 소품 생성과 검증.
- `Art/Blender/DownhillCoupe.blend`: 다운힐 스포츠쿠페 원본. create_downhill_coupe.py / validate_downhill_coupe.py로 재생성·검증합니다.
- `Assets/CottonCircuit/Models/`: 기본 9종, 레이싱 3종, 가게 3종, 코스 소품 2종, 쿠페 1종의 FBX 총 18종.

프로젝트 루트에서 다음과 같이 재생성합니다.

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -b -t 4 -P Art/Blender/create_assets.py
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -b -t 4 -P Art/Blender/validate_assets.py
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -b -t 4 -P Art/Blender/create_racing_props.py
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -b -t 4 --python-exit-code 1 -P Art/Blender/validate_racing_props.py
```

Unity의 `Cotton Circuit > Rebuild game scene` 메뉴는 생성된 프리팹·재질과 메인 씬을 다시 만듭니다. 메인 씬에서 직접 편집한 배치는 재생성 시 대체되므로 별도 씬으로 저장해서 보관하세요.

## 개발 및 검증

```powershell
./Tools/test-progression.ps1
./Tools/test-progression-shift.ps1
./Tools/test-progression-save.ps1
./Tools/simulate-progression.ps1
./Tools/test-shop-shift.ps1
./Tools/test-core.ps1
./Tools/test-recipes.ps1
./Tools/test-continuous.ps1
./Tools/build.ps1 -BuildFolder Builds/Progression
./Tools/verify-progression.ps1 -BuildFolder Builds/Progression
./Tools/build.ps1 -BuildFolder Builds/Progression -Release
```

빌드 스크립트는 Unity의 에셋·씬 생성 코드와 저장 검증을 실행합니다. 실제 플레이어 검증은 개발 빌드에서만 실행하고, 별도의 저장 폴더를 사용합니다. 기존 주행/연속 생산 검증은 명시적인 레거시 진입점으로 유지합니다. 하루 초기화와 설탕 비우기 검증은 `docs/day-reset-verification.md`, 이어 만들기 검증은 `docs/candy-resume-verification.md`, 가게 앞 손님 구성은 `docs/shop-street-verification.md`, 드리프트 감속 기록은 `docs/drift-slowdown-verification.md`, 도로 확장 기록은 `docs/wide-road-downhill-verification.md`, 바퀴 기준 생산 기록은 `docs/lap-balance-verification.md`에 기록합니다.

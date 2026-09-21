# 솜사탕 서킷 · Cotton Circuit

거대한 솜사탕 기계 안을 카트로 달려 중앙 막대에 설탕 실을 감고, 완성한 솜사탕을 판매해 가게를 성장시키는 Windows용 Unity 게임입니다.

## 실행

- 바로 플레이: `Builds/BoosterAcceleration/CottonCircuit.exe` 실행. 같은 폴더의 `CottonCircuit_Data`, `MonoBleedingEdge`, DLL을 함께 유지하세요. 이전 게임이 열려 있다면 닫은 뒤 실행하세요.
- Unity 편집: Unity Hub에서 이 폴더를 추가하고 **Unity 6000.5.3f1**로 엽니다. `Assets/CottonCircuit/Scenes/CottonCircuit.unity`를 열고 Play를 누르세요.
- 주문 카드의 **만들러 가기**를 누르고 **W를 길게 눌러** 출발합니다. 재고가 있으면 **주문 선택 → 건네기**로 바로 판매할 수 있습니다.

## 두 가지 주행 스타일

가게 오른쪽의 **카트 스타일 / 다운힐 스타일**을 선택하고 출발하세요. 같은 두 맵과 솜사탕 레시피로 비교할 수 있습니다. 카트의 기본 최고 속도는 약94km/h이며 출발 후0.8초 안에90% 속도까지 가속합니다. 다운힐은 엑셀을 유지하면 이 속도를 넘어 계속 가속합니다. 스타일은 가게에 돌아와 다시 선택할 수 있으며 이번 실행 동안 유지합니다.

- **카트 스타일:** W를 누르며 **Shift로 보관 부스터**를 사용합니다. 시작 시1개, 최대2개 보관하며 깨끗한 드리프트 완료 시1개 충전합니다. 보관 부스터는2.5초 동안 기본 차량 기준 최대144km/h까지 가속합니다. 기존 드리프트 해제 부스트도 유지합니다. 게이지32%에서1.1초 미니 부스트,75%에서1.7초 슈퍼 부스트가 나오며, 이 가속이 끝난 뒤 Shift를 누르면 보관 부스터를 사용합니다. 가속 중 추가 입력으로 중복 소모되지 않습니다.
- **다운힐 스타일:** Blender로 만든 스포츠쿠페, 더 큰 관성과 미끄러짐, 낮은 추적 카메라. **W를 유지하면 속도가 계속 올라갑니다.** 충돌·코너가 없는 가속 계산에서는1/3/10/20초에 약93/132/170/190km/h이며, 기본 차량의216km/h 상한에 가까워질수록 가속이 완만해집니다. 실제 코스에서는 코너 전 S로 감속하고 Space와 조향으로 미끄러짐을 조절한 뒤 W로 탈출합니다. 완료한 드리프트는 품질을 높이며 부스터는 없습니다. 현재는 같은 평면 코스에서 조작감을 비교하는 모드입니다.

## 조작과 진행

| 입력 | 동작 |
|---|---|
| W / 위 방향키 | 가속 |
| A / 왼쪽 방향키 | 왼쪽 조향 |
| D / 오른쪽 방향키 | 오른쪽 조향 |
| Space | 코너에서 드리프트 · 카트는 놓으면 짧은 부스트와 보관 부스터 충전 |
| Shift | 카트 보관 부스터 사용 (W와 함께) |
| S / 아래 방향키 | 제동 |
| Enter | 출발 / 주행 포기 확인 / 가게 복귀 |
| R | 현재 위치 근처 코스 중앙으로 복귀 |
| Esc | 일시정지, 조작 안내, 소리 설정 |

맵이 제품 크기를 정하고, 출발 전 기계 세팅이 맛을 정합니다. 두 맵 모두 **딸기·소다·바닐라**를 선택할 수 있습니다. 주문의 **만들러 가기**를 누르면 맞는 맵과 맛이 자동 설정됩니다.

| 맵 | 완성품 | 코스 길이 | 기본 카트의 자동 시험 주행 |
|---|---|---|---|
| 1번 · 슈가웨이 | 작은 솜사탕 60g | 약 561m | 약 22초 |
| 2번 · 클라우드런 | 큰 솜사탕 120g | 약 669m | 약 27초 |

서로 다른 직선과 코너를 가진 코스를 **한 바퀴 완주**하면 솜사탕을 보관합니다. 중량이 먼저 가득 차도 계속 레이싱합니다. 모든 주행 라인과 지름길에서 선택한 맛으로 실을 감고, 주행 라인이 솜사탕의 두께를 바꿉니다. 멈추거나 역주행하거나 복귀하는 동작으로는 솜사탕을 추가로 얻지 못합니다. 표의 시간은 자동 시험 운전자의 기록이며 플레이 시간은 운전에 따라 달라집니다.

코너에서 W와 Space를 누르면서 조향하면 드리프트가 충전됩니다. 게이지가 노란색이 되면 Space를 놓아 짧은 부스트를 쓰세요. **부스트 중 생성량이 25% 증가**합니다. 벽에 부딪히면 속도와 충전이 줄어듭니다. 번개 모양 게이트의 좁은 **Sugar Cut 지름길**로 코너를 단축할 수 있습니다. 미니맵, 선택한 주행 스타일, 완주 진행도, 주행 시간, 솜사탕 미리보기가 주행 중 표시됩니다. 낮은 카메라와 가까운 도로 소품, 속도선, 바람 소리로 속도 변화를 표현합니다.

손님은 **맛 3종 × 크기 2종**을 주문하며, 최대 두 명이 300초 동안 기다립니다. 재고에서 알맞은 솜사탕을 고른 뒤 그 손님의 **건네기**를 눌러 판매합니다. 재고가 없으면 주문 카드의 **만들러 가기**로 출발하세요. 빨리 전달할수록 만족도와 팁이 높아집니다.

품질은 기본 50점에서 카트 부스트 또는 다운힐 드리프트 완료 1회당 +5, 충돌 1회당 −10, 설탕통 단계당 +5로 계산하며 0–100점입니다. 품질에 따라 판매가가 최대 30% 높아집니다. 완주 시 기록에 따라 1번 맵 최대 20코인, 2번 맵 최대 40코인을 한 번 지급합니다. 제한 시간은 180초/240초이며, 시간 초과 또는 **주행 포기** 두 번 확인으로 중단하면 제품과 완주 보상이 없습니다. 재고는 선택 후 **재고 정리**를 두 번 눌러 버릴 수 있습니다.

모터는 최고 속도, 설탕통은 감는 속도(단계당 +15%)와 품질, 가게 꾸미기는 판매가를 높입니다. 진열대는 **6 → 9 → 12칸**으로 확장됩니다. 손님이 없어도 오른쪽에서 맵과 맛을 골라 미리 만들어둘 수 있습니다. 손님의 대기 시간은 제작 중에도 흐르며, 결과 화면과 일시정지에서는 멈춥니다.

게임은 `%USERPROFILE%\AppData\LocalLow\SugarRoad Studio\Cotton Circuit\cotton-circuit.json`에 자동 저장됩니다. 도움말의 **새 가게 시작**을 두 번 누르면 기존 저장 파일을 보관하고 새로 시작합니다. 기존 V1/V2 저장은 재고·코인·업그레이드를 유지한 채 V3로 이전됩니다. V2 주문은 남은 대기 비율을 유지하고, 기존 제품 가격은 변하지 않습니다. 주문·팁·만족도·진열대·품질도 저장되며 주행 중간은 저장되지 않습니다. Windows 시스템 글꼴인 맑은 고딕을 사용하며 해당 글꼴 파일을 재배포하지 않습니다.

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
./Tools/test-core.ps1
./Tools/test-feel.ps1
./Tools/test-booster.ps1
./Tools/test-acceleration.ps1
./Tools/test-recipes.ps1
./Tools/test-maps.ps1
./Tools/build.ps1 -BuildFolder Builds/BoosterAcceleration
./Tools/verify-player.ps1 -BuildFolder Builds/BoosterAcceleration -OutputFolder Logs/Smoke-booster
./Tools/build.ps1 -BuildFolder Builds/BoosterAcceleration -Release
```

빌드 전 이 프로젝트의 Unity 에디터를 닫아주세요. 기본 빌드 스크립트는 씬을 구성하고 저장·연결 검증을 실행한 뒤 Windows 개발 빌드를 생성합니다. 실행 검증은 별도의 저장 경로를 사용해 정상 저장 데이터를 건드리지 않으며, 주행·맛 선택·수동 전달·확장·재저장과 화면 캡처를 수행합니다. 마지막의 `-Release` 빌드는 자동 검증 코드를 제외한 배포용 실행 파일로 교체합니다. 다시 자동 플레이를 검증할 때는 기본 개발 빌드를 먼저 만드세요.

현재 버전은 코스 2개와 싱글플레이 제작 레이스를 제공합니다. 경쟁 AI, 모바일 조작, 가게 자유 배치는 포함하지 않습니다. 검증 결과는 `docs/verification.md`에 기록합니다.

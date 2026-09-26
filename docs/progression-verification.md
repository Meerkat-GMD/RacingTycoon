# 성장·영업 루프 검증 기록

2026-09-24 · Unity 6000.5.3f1 · Windows x64

## 구현 범위

- 돈으로 구매하는 선행 조건 그래프30개 노드/58단계, 최초180초·기본 딸기 설탕0원.
- 영업시간·손님 대기·광고·진열대·설탕 맛/등급·기계·카트·젓가락 성장/절약/품질·알바/교육·지역.
- 독립적인 기계3대와 코스3개, 알바2명, 맛3종, 설탕3등급, 지역4개.
- 준비→영업→정산→준비 전환, 매 투입 결제, 알바 비용/보관 한도 대기, 일일 재료 초기화.
- 왼쪽 행동/오른쪽 NPC·이동 UI, 드래그 가능한 그래프, 호버 설명, 장비·레시피 관리와 장소 선택.
- V7 저장, 기존 영구 성장 이전, 정상 백업 복구, 복구 불가 파일 덮어쓰기 차단.

## 자동 검증

| 검증 | 결과 |
|---|---:|
| 코어/주행/주문 | 69 통과 |
| 기존 영업 규칙 | 38 통과 |
| 성장 그래프 | 14 통과 |
| 여러 기계·원가·알바·크기 한도 | 29 통과 |
| V7 저장/이전/손상 복구 | 13 통과 |
| 레시피/이전 | 17 통과 |
| 연속 생산 | 22 통과 |
| 코스/추가 코스 | 32 + 3 통과 |
| 부스터/가속 | 10 + 17 통과 |
| Unity 에디터 씬·에셋·저장 | 58 통과 |
| 기존 실제 플레이어 영업 회귀 | 3,033 통과 |

순수 코어 계열 합계264개. 개발 빌드의 실제 플레이어 검증은 독립 저장 폴더에서 수행하므로 사용자 저장을 건드리지 않는다. 성장 시나리오는 실제 버튼의 레이캐스트, 설탕 흔들기와 제품 드래그, 첫 매출, 특성 구매, 기계/알바/레시피, 지역 이동, 기계 전환, 중간 저장 재실행, 마감과 다음 영업을 검증한다.

중간 검토에서 발견한 알바 맛 전환 정지, 원래 등급 한도에 도달한 제품에 유료 설탕 투입, 고급 제품을 무료 설탕으로 재개하는 우회, 영업시간 구매 후 저장 불일치를 재현 테스트와 함께 수정했다. 재실행 시간 비교는 JSON double 반올림을 허용하는0.0000001초 오차 범위이며 실제 복원 차이는 약0.00000000000003초였다.

## 화면 확인

1600×900과1280×720에서 준비/성장 지도/호버/기계 관리/지역/영업/정산 화면을 렌더링하고 확인했다. 가림·클리핑·버튼 겹침이 없으며, 그래프 연결선이 이름과 가격을 가리지 않도록 캡션 배경을 보강했다. NPC는 오리지널 생성 이미지이며 참고 게임 이미지를 복사하지 않았다.

## 성장 시간

`Tools/simulate-progression.ps1`: 실제 코어 규칙과 UI에서 허용하는 준비·영업 중 조작 범위로36회 영업,173.3분(2.89시간)에 모든 특성 완료. 메뉴 선택·조작 시간과 주행 실수는 포함하지 않으므로 실측 플레이 시간이 아니다. 세부 가정은 [성장 시간 보고서](progression-balance.md)에 있다.

## 재현

```powershell
./Tools/test-progression.ps1
./Tools/test-progression-shift.ps1
./Tools/test-progression-save.ps1
./Tools/test-progression-maps.ps1
./Tools/simulate-progression.ps1
./Tools/build.ps1
./Tools/verify-progression.ps1
./Tools/verify-player.ps1 -ShopShift -OutputFolder Logs/ProgressionLegacySmoke
./Tools/build.ps1 -BuildFolder Builds/Progression -Release
```

최종 성장 플레이어 검증 및 배포 빌드 결과는 아래에 추가한다.

## 최종 실제 플레이어 검증

- `Tools/verify-progression.ps1`: **93개 통과**, 실제 영업 중 컨트롤러 재실행까지 포함.
- `Tools/verify-player.ps1 -ShopShift`: **3,033개 통과**.
- 화면: [성장 지도](screenshots/progression-graph.png), [호버](screenshots/progression-hover.png), [기계 관리](screenshots/progression-equipment.png), [지역](screenshots/progression-location.png), [영업](screenshots/progression-business.png).

## 배포 빌드

`Tools/build.ps1 -BuildFolder Builds/Progression -Release`가 종료 코드0으로 완료됐다. Windows x64 Release 실행 파일은 `Builds/Progression/CottonCircuit.exe`이며 함께 생성된 Data 폴더와 DLL을 유지해야 한다. 개발 빌드의 검증용 치트·시나리오 진입점은 배포 빌드에서 제외된다.

## 성장 지도 인접 배치 수정 — 2026-09-24

분류별 격자를 실제 선행 관계에 맞춰 재배치했다. 직접 연결된 노드 간 거리는 110~188 UI 단위이며, 연결선 교차는 53곳에서 4곳으로 감소했다. 가격·효과·선행 조건·저장 ID는 유지했다. 이름과 단계만 상시 표시하고 가격·효과는 호버로 옮겼다. 휠/버튼 확대·축소, 드래그 이동, 전체 보기와 연결 경로 강조를 추가했다.

- 순수 성장 규칙 14개, Unity Editor 검사 58개, 실제 플레이어 검사 **103개 통과**.
- 1600×900 / 1280×720 캡처에서 겹침·잘림 없음. 독립 UI 검토 완료.
- [새 성장 지도](screenshots/progression-adjacent-graph.png), [호버](screenshots/progression-adjacent-hover.png), [배치 수치와 검증](graph-layout-metrics.md).
- 변경 내용을 포함한 `Builds/Progression/CottonCircuit.exe` Release 빌드 갱신 완료(종료 코드 0).

## 연결선 교차 없는 탭 구조 — 2026-09-24

사용자의 추가 요청에 따라 위 단일 성장 지도를 6개 탭으로 교체했다. 내부 연결선 21개는 교차·겹침이 0개이며, 다른 탭 선행 조건 15개는 대상 특성 바로 아래의 이름 버튼으로 표시한다. 이름을 누르면 필요한 특성으로 이동하고 강조된다. 모든 36개 선행 조건과 기존 구매 규칙을 유지한다.

기하 배치 15개, 성장 규칙 14개, Editor 58개, 실제 플레이어 **171개 검사 통과**. 독립 코드·화면 검토 및 1600×900/1280×720 확인 완료. [상세 검증 및 화면](tree-layout-verification.md).

탭 UI를 포함한 `Builds/Progression/CottonCircuit.exe` Release 빌드 완료(종료 코드 0).

## 다운힐 기본 주행 — 2026-09-24

다운힐 쿠페를 기본 차량으로 변경했다. 이전 `coupe` 노드는 선택형 클래식 카트 해금으로 연결하며 저장 ID·가격·선행 조건을 유지한다. V8 저장은 유효한 V7의 기본 카트 선택을 다운힐로 이전하고, 차량 해금 후 선택했던 스타일 및 게임 진행 상태를 보존한다.

성장 16개, 저장 19개, 부스터 11개, 주행 감각 28개, 제작/이전 17개, Editor 58개, 실제 플레이어 **243개 통과**. [상세 검증과 화면](downhill-default-verification.md).

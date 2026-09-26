# 바퀴 기준 생산과 설탕 소모 조정

2026-09-23 · Unity 6000.5.3f1 · Windows

## 적용 기준

| 생산 진행도 | 크기 | 필요한 설탕 |
|---|---|---|
| 1바퀴 미만 | 미완성 · 판매 불가 | 50g 미만 |
| 1바퀴 이상, 1.5바퀴 미만 | A | 50g부터 |
| 1.5바퀴 이상, 2바퀴 미만 | B | 75g부터 |
| 2바퀴 이상 | C | 100g부터 |

한 바퀴는 현재 영업 모드의 1번 코스 길이인 약 560.815m를 기준으로 한다. 설탕이 있는 동안 얻은 유효 전진거리만 생산에 포함한다. 소비량은 한 바퀴 50g으로, 기존 0.02g/m 대비 약 4.46배다. 위아래 왕복 투입량 10g과 설탕통 용량 100g은 유지한다. 한 번 흔들면 0.2바퀴 분량이고, 가득 채우면 두 바퀴 분량이다.

제작 진행도, 다음 크기까지 남은 양, 손님 주문 범위, 진열대 제품, 도움말과 판매 불가 안내를 바퀴 단위로 통일했다. 다음 크기 진행 막대는 0→1, 1→1.5, 1.5→2 구간으로 계산한다. 주문 범위는 상한 미만을 명시한다.

기존 V4 저장의 실제 생산 거리는 그대로 보존하고 새 크기 기준으로 분류한다. 이전 중량 기반 제품은 A/B의 최소 생산 거리로 이전한다. 저장 형식과 10분 영업시간은 바뀌지 않는다.

## 모델과 회귀 검사

| 검사 | 결과 | 근거 |
|---|---|---|
| 바퀴 기준 생산·판매·설탕·저장 상태 | 25개 통과 | `Logs/shop-shift-tests.txt` |
| 기존 생산·주행·주문 | 22/17/14개 통과 | `Logs/core-tests.txt`, `Logs/driving-tests.txt`, `Logs/order-tests.txt` |
| 레시피·저장 호환 | 17개 통과 | `Logs/recipe-tests.txt` |
| 기존 연속 주행 | 22개 통과 | `Logs/continuous-tests.txt` |
| Unity 에디터 통합 | 55개 통과 | `Logs/editor-checks.txt`, `Logs/lap-balance-development-build.log` |
| 실제 개발 플레이어 | 275개 통과 | `Logs/Smoke-lap-balance/result.txt` |
| Windows 릴리스 | 종료 코드 0 · Release | `Logs/lap-balance-release-build.log`, `Builds/LapBalance/build-info.json` |

동작 변경 전 실패는 `Logs/shop-shift-tests-lap-balance-red.txt`에 기록했다. 여러 프레임에 걸쳐 설탕 100g을 소비할 때 누적 오차로 C에 극미량 못 미치는 현상을 별도로 재현했다(`Logs/shop-shift-tests-lap-boundary-red.txt`). 생산 중 크기 경계의 0.0000001m 이내 오차만 정확한 경계로 정규화한다. 크기 판정 자체는 엄격한 경계를 유지하며, 실제로 모자라는 제품은 한 단계 작은 크기로 남는다.

개발 플레이어에서는 추가 투입 없이 10g이 0.2바퀴에 소진되는지, 100g으로 1/1.5/2바퀴의 경계를 지나 C로 꺼낼 수 있는지를 실제 카트 전진거리로 확인했다. 이후 판매·오배송·폐기·마감·이월·저장 재실행도 통과했다. 1600×900과 1280×720 캡처에서 긴 주문 범위 문구가 잘리지 않는 것을 확인했으며, 1280×960과 1920×820에서도 화면 경계와 드래그 입력 검사가 통과했다.

화면: [바퀴 기준 제작·판매](screenshots/lap-balance.png).

수정본은 `Builds/LapBalance/CottonCircuit.exe`다. 배포 어셈블리에 개발 검증 타입이 제외된 것도 확인했다.

## 재현

```powershell
./Tools/test-shop-shift.ps1
./Tools/test-core.ps1
./Tools/test-recipes.ps1
./Tools/test-continuous.ps1
./Tools/build.ps1 -BuildFolder Builds/LapBalance
./Tools/verify-player.ps1 -BuildFolder Builds/LapBalance -OutputFolder Logs/Smoke-lap-balance -ShopShift
./Tools/build.ps1 -BuildFolder Builds/LapBalance -Release
```

플레이어 검증은 별도 저장 폴더에서 실제 주행 코드의 Tick을 가속하고 EventSystem 입력 핸들러를 호출한다. 물리 마우스·키보드 자동화와 10분 실시간 방치 측정은 포함하지 않는다.

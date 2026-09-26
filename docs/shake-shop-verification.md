# 10분 영업·설탕 흔들기·드래그 판매 검증

2026-09-23 · Unity 6000.5.3f1 · Windows

이 문서는 최초 구현의 검증 기록이다. 이후 크기 기준과 설탕 소모를 변경한 최신 내용은 [바퀴 기준 생산 검증](lap-balance-verification.md)을 참고한다.

하루 영업시간600초, 위아래 흔들기로 설탕 투입, 유효 전진거리 기반 A/B/C 생산, F 추출, 손님/쓰레기통 드래그, 오배송 반응 및 다음 날 이월을 구현했다. 왼쪽 주행과 오른쪽 가게를 함께 표시한다. 무료 설탕은 왕복10g, 설탕통100g, 소비량0.02g/m이며, 1km/2km/3km 경계로 크기를 나눈다.

## 확인 결과

| 검사 | 결과 | 근거 |
|---|---|---|
| 새 생산·영업·흔들기 모델 | 20개 통과 | `Logs/shop-shift-tests.txt` |
| 기존 생산·주행·주문 | 22/17/14개 통과 | `Logs/core-tests.txt`, `Logs/driving-tests.txt`, `Logs/order-tests.txt` |
| 레시피·이전 저장 호환 | 17개 통과 | `Logs/recipe-tests.txt` |
| 기존 연속 주행 | 22개 통과 | `Logs/continuous-tests.txt` |
| 맵·주행 감각·수집·부스터·가속 | 14/21/9/10/12개 통과 | 해당 `Logs/*-tests.txt` |
| 실제 Unity 에셋·씬·저장 검사 | 55개 통과 | `Logs/editor-checks.txt`, `Logs/shake-development-build.log` |
| 새 영업 개발 플레이어 | 250개 통과 | `Logs/Smoke-shake-final/result.txt` |
| 기존 모드 개발 플레이어 | 623개 통과 | `Logs/Smoke-shake-legacy/result.txt` |
| 독립 코드 리뷰 | 통합 경로의 중대한 문제 없음 | 생산 담당과 저장 담당의 컨트롤러·입력 교차 검토 |
| Windows 릴리스 빌드 | 종료 코드 0 · Release | `Logs/shake-release-build.log`, `Builds/ShakeShop/build-info.json` |

실제 개발 플레이어에서 다음을 확인했다.

- 자동 운전으로 실제 랩을 돌아도 설탕 없이는 제품·코인·날짜가 증가하지 않는다.
- 단순 드롭·가로 이동·가게 영역 흔들기는 투입하지 않으며, 주행 영역의 유효 왕복은10g을 투입한다.
- 1km 미만에서도 꺼낼 수 있고, 판매는 거절되며 쓰레기통으로 버릴 수 있다.
- 등급 경계를 넘기 전에도 화면의 솜사탕 크기가 연속적으로 커진다.
- 실제 전진거리로 주문 제품을 만든 뒤 정상 드래그는 정확히 한 번 판매된다. 재전달은 거절된다.
- 오배송은 제품을 소비하고 돈을 주지 않으며, 화난 손님을1.5초 뒤 내보낸다.
- 일시정지는 영업·성장·손님 반응을 함께 멈추고 진행 중 드래그와 잔상을 취소한다.
- 마감은 입력을 차단하고 정산을 표시한다. 마감 상태로 재실행한 뒤 다음 날을 시작해도 제품·설탕·미완성 제품을 유지한다.
- 저장 검사는 실제 JsonUtility 파일로 제작 중/분노/손님 대기/마감 및 V1~V3 재고 이전을 확인한다. 손상 파일은 덮어쓰지 않는다.
- EventSystem의 실제 레이캐스트가 설탕 봉지·진열대·손님·쓰레기통에 도달하는지 확인한 뒤 실제 드래그 이벤트 핸들러를 실행한다.
- 1600×900,1280×720,1280×960,1920×820에서 조작 대상의 화면 경계, 카메라 분리, 드래그 잔상과 일시정지 취소를 검사했다.

## 저장 및 빌드 수정

Unity의 inline 클래스 직렬화가 null인 Business/Customer를 기본 객체로 저장하는 현상을 실제 에디터 검사에서 재현했다. V4 envelope에 존재 여부를 기록해 null을 복원한다. 표시와 의미 있는 데이터가 충돌하는 파일은 거부한다. 수정 후55개 에디터 검사와250/623개 플레이어 검사가 통과했다.

PowerShell `Start-Process -Wait`가 에디터 종료 후 남는 Roslyn 컴파일러 서버까지 기다리는 현상도 확인했다. 빌드 스크립트는 에디터 프로세스의 `WaitForExit()`와 종료 코드를 사용한다.

## 화면과 재현

- `docs/screenshots/shake-shop.png`: 제작·진열 화면.
- `docs/screenshots/shake-shop-angry.png`: 오배송 대사·표정.
- `docs/screenshots/shake-shop-closed.png`: 영업 마감 정산.

```powershell
./Tools/test-shop-shift.ps1
./Tools/test-recipes.ps1
./Tools/build.ps1 -BuildFolder Builds/ShakeShop
./Tools/verify-player.ps1 -BuildFolder Builds/ShakeShop -OutputFolder Logs/Smoke-shake-final -ShopShift
./Tools/verify-player.ps1 -BuildFolder Builds/ShakeShop -OutputFolder Logs/Smoke-shake-legacy
./Tools/build.ps1 -BuildFolder Builds/ShakeShop -Release
```

검증은 별도의 저장 폴더에서 실행했다. 플레이어 시간은 실제 코드의 Tick을 통해 가속해 진행했으며, 물리 마우스·키보드 자동화나10분 실시간 방치 측정은 포함하지 않는다. 캡처는 숨긴 Windows 플레이어의 실제 카메라와 Canvas를 렌더링해 저장한다. 배포 빌드에는 개발 검증 코드가 포함되지 않는다.

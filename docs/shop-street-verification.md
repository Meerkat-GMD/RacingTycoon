# 가게 앞 손님 장면 검증

2026-09-23 · Unity 6000.5.3f1 · Windows

사용자 스케치처럼 오른쪽 위에 가게와 전신 손님 세 명을 가로로 배치했다. 기존 3D 가게 카메라와 단일 주문 카드를 대체하는 uGUI 일러스트이며, 왼쪽 주행/아래 진열대·쓰레기통·설탕 봉지는 기존 구성을 유지한다. 횡스크롤 게임처럼 옆에서 보는 장면으로 구현했으며 현재 세 명은 스크롤 없이 함께 보인다.

손님 머리 위 말풍선에 실제 주문 맛의 솜사탕 그림, A/B/C 크기 배지와 작은 맛 이름을 표시한다. 손님 몸통 또는 말풍선 어느 곳에 놓아도 해당 손님에게 전달한다. 잘못된 제품을 주면 그 손님만 1.5초간 화난 대사와 표정을 보인 뒤 떠난다. 남은 손님은 자리를 바꾸지 않는다. 드래그를 시작한 뒤 새로 도착한 손님에게는 기존 드래그로 전달할 수 없다.

V5 저장은 고객 ID·자리·주문·독립 반응 시간을 포함한다. V4의 한 명은 첫 자리에 이전하고 돈·재고·생산·설탕·영업시간은 유지한다. V1~V3의 기존 데이터도 계속 읽는다.

## 근거

- `Tools/test-shop-shift.ps1`: 다중 고객, 전달, 반응, 시간 경계 및 기존 생산/폐기/일시정지 모델 검사 28개 통과.
- `Tools/test-core.ps1`: 생산·경제 22개, 주행 33개, 주문 14개 통과.
- Unity 에디터 검증 55개 통과. 저장 버전 기대값을 V5로 갱신했다. 로그: `Logs/shop-street-development-build.log`.
- `Logs/Smoke-shop-street/result.txt`: 최종 개발 플레이어 402개 통과. 실제 EventSystem 드래그, 고객별 대상 지정, 교체 도중 드래그 거부, 두 번째·세 번째 판매, 이웃 자리 유지, 잘못된 주문 반응, 네 가지 해상도와 기존 영업/생산/드리프트 시나리오를 확인했다.
- `Logs/Smoke-street-legacy/result.txt`: 기존 모드 623개 통과.
- 독립 검토에서 반응 종료와 도착 예약이 겹칠 때 도착이 취소되는 경우를 발견했다. 회귀 검사 실패를 재현한 후 수정하고, 경계 시점과 긴 프레임을 검사했다. 검토 근거는 `Logs/shop-street-review-report.md`.

물리 키보드 자동화는 포함하지 않는다. 별도 테스트 저장 디렉터리에서 실제 Tick과 UI 이벤트 핸들러를 실행하고, 게임 카메라와 Canvas를 렌더링해 화면을 확인한다.

화면 확인: [가게와 세 손님](screenshots/shop-street.png), [개별 오주문 반응](screenshots/shop-street-angry.png). 최종 화면에서는 가게 표지의 글자가 선과 겹치지 않도록 배치를 조정했다.

최종 Windows 릴리스: `Builds/ShopStreet/CottonCircuit.exe`. 빌드 성공과 Release 영수증을 확인했고, 실제 어셈블리에 가게 장면/고객 조회 구현이 포함되며 검사 전용 타입이 제외됨을 확인했다. 로그: `Logs/shop-street-release-build.log`, `Builds/ShopStreet/build-info.json`.

## 재현

```powershell
./Tools/test-shop-shift.ps1
./Tools/test-core.ps1
./Tools/build.ps1 -BuildFolder Builds/ShopStreet
./Tools/verify-player.ps1 -BuildFolder Builds/ShopStreet -OutputFolder Logs/Smoke-shop-street -ShopShift
./Tools/verify-player.ps1 -BuildFolder Builds/ShopStreet -OutputFolder Logs/Smoke-street-legacy
./Tools/build.ps1 -BuildFolder Builds/ShopStreet -Release
```

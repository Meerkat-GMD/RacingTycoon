# 도로 확장 · 이니셜D 기본 모드 검증

2026-09-23 · Unity 6000.5.3f1 · Windows

두 코스의 기본 도로를 9.6m에서 14.4m로, 지름길을 4.4m에서 6.6m로 넓혔다. 도로 렌더링과 충돌 판정은 같은 폭을 사용한다. 중심선과 한 바퀴 길이는 유지하므로 A/B/C 1/1.5/2바퀴 및 설탕 50g/바퀴 규칙은 그대로 적용된다.

출발 아치, 지름길 게이트, 터널은 너비만 1.5배 확대했다. 출발 체크 무늬는 도로 전체 폭을 덮고, 가드레일·표지판·나무·크리스털·결승 표지는 확장된 도로 밖으로 옮겼다. Blender 원본의 개구부와 깊이를 별도로 측정해 양쪽 코스에서 구조물이 주행 영역을 침범하지 않는 배치를 확인했다.

일반 영업 모드에서는 이니셜D(Downhill) 주행과 스포츠쿠페가 기본이다. 시작과 저장 재실행 시 스타일을 지정한 뒤 주행 상태와 카메라를 초기화한다. 자동 주행은 켜진 상태로 시작하며, 수동 전환 시 W 지속 가속, S 감속, Space 드리프트 안내를 표시한다.

## 검증 결과

| 검사 | 결과 | 근거 |
|---|---|---|
| 기존 생산·경제 / 주행 / 주문 | 22 / 33 / 14개 통과 | `Logs/core-tests.txt`, `Logs/driving-tests.txt`, `Logs/order-tests.txt` |
| 두 맵과 지름길 | 22개 통과 | `Logs/map-tests.txt` |
| 가속·드리프트 감각 / 지속 가속 / 부스터 | 21 / 12 / 10개 통과 | `Logs/feel-tests.txt`, `Logs/acceleration-tests.txt`, `Logs/booster-tests.txt` |
| 자동 주행 / 생산·설탕·영업 | 22 / 25개 통과 | `Logs/continuous-tests.txt`, `Logs/shop-shift-tests.txt` |
| Unity 씬·에셋·저장 | 55개 통과 | `Logs/editor-checks.txt`, `Logs/wide-road-downhill-development-build.log` |
| 실제 개발 플레이어 | 310개 통과 | `Logs/Smoke-wide-road-downhill/result.txt` |
| Windows 릴리스 | 종료 코드 0 · Release | `Logs/wide-road-downhill-release-build.log`, `Builds/WideRoadDownhill/build-info.json` |

확장 전 실패를 `Logs/wider-roads-red-core-20260923.txt`와 `Logs/wider-roads-red-maps-20260923.txt`에 기록했다. 테스트는 새로 열린 차선에서 정상 조작으로 주행하고, 확장된 도로 밖에서는 여전히 충돌하는지를 확인한다. 두 맵·두 주행 방식·양쪽 방향을 검사했다.

개발 플레이어에서는 양쪽 코스의 실제 메시 폭이 14.4m/6.6m인지, 새 게임과 열린 날/마감 상태 재실행에서 스포츠쿠페와 Downhill 주행이 적용되는지, 수동 W 입력으로 기존 카트 한계를 넘어 가속하고 Shift로 카트 부스터를 만들지 않는지를 확인했다. 기존 생산·판매·쓰레기통·오배송·마감·저장·네 가지 해상도 드래그 검사도 통과했다.

자동 주행과 수동 주행 캡처를 확인했다. 도로·차량·UI가 표시되고 수동 운전 안내에 이니셜D 조작이 반영됐다.

실행 파일은 `Builds/WideRoadDownhill/CottonCircuit.exe`다. 배포 어셈블리에서 개발 검증 타입이 제외된 것도 확인했다.

- [자동 주행 화면](screenshots/wide-road-downhill.png)
- [수동 운전 화면](screenshots/wide-road-downhill-manual.png)

## 재현

```powershell
./Tools/test-core.ps1
./Tools/test-maps.ps1
./Tools/test-feel.ps1
./Tools/test-acceleration.ps1
./Tools/test-booster.ps1
./Tools/test-continuous.ps1
./Tools/test-shop-shift.ps1
./Tools/build.ps1 -BuildFolder Builds/WideRoadDownhill
./Tools/verify-player.ps1 -BuildFolder Builds/WideRoadDownhill -OutputFolder Logs/Smoke-wide-road-downhill -ShopShift
./Tools/build.ps1 -BuildFolder Builds/WideRoadDownhill -Release
```

플레이어 검증은 별도 저장 폴더를 사용하고 실제 Tick과 EventSystem 핸들러를 가속 실행했다. 물리 마우스·키보드 자동화는 포함하지 않는다. 넓어진 도로 메시와 재배치된 씬은 프로젝트에도 저장한다.

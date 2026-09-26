# Space 드리프트 감속 검증

2026-09-23 · Unity 6000.5.3f1 · Windows

이니셜D(Downhill) 모드에서 Space를 누르는 동안 가속 입력보다 드리프트 감속을 우선한다. W와 Space를 함께 눌러도 속도가 줄어들고, Space를 놓으면 기존 가속으로 돌아간다. 속도에 따라 초당 `4 + 0.18 × 속도(m/s)`만큼 감속하며, S 제동은 기존 강도로 우선 적용한다. 오래 누르면 정지할 수 있고 속도가 음수가 되지는 않는다.

기존에는 Space가 조향과 접지력만 바꾸고 가속 계산에는 전달되지 않았다. 이제 가속 모델에 드리프트 입력을 전달하고, 실제 속도 벡터에도 감속 비율을 적용한다. 미끄러지는 방향은 유지하면서 실제 이동거리와 생산 진행도도 함께 줄어든다. 카트 주행 방식의 부스터 동작은 유지한다.

## 모델 검사

| 검사 | 결과 | 근거 |
|---|---|---|
| 주행 감각·드리프트 | 28개 통과 | `Logs/feel-tests.txt` |
| 가속·감속 | 17개 통과 | `Logs/acceleration-tests.txt` |
| 생산·경제 / 주행 / 주문 | 22 / 33 / 14개 통과 | `Logs/core-tests.txt`, `Logs/driving-tests.txt`, `Logs/order-tests.txt` |
| 카트 부스터 | 10개 통과 | `Logs/booster-tests.txt` |
| Unity 에디터·씬·저장 | 55개 통과 | `Logs/editor-checks.txt`, `Logs/drift-slowdown-development-build.log` |
| 현재 영업 모드 개발 플레이어 | 315개 통과 | `Logs/Smoke-drift-slowdown/result.txt` |
| 기존 모드 개발 플레이어 | 623개 통과 | `Logs/Smoke-drift-legacy/result.txt` |
| Windows 릴리스 | 빌드 성공, 검사 전용 타입 제외, 최종 드리프트 안내 문구 포함 확인 | `Logs/drift-slowdown-release-build.log`, `Builds/DriftSlowdown/build-info.json` |

변경 전 7개 회귀 검사가 실패하는 것을 확인했다(`Logs/feel-tests-drift-red.txt`). 검사에는 W+Space의 실제 속도·이동거리·생산거리 감소, 저속 정지, Space 해제 후 재가속, S 제동 우선순위, 정지 상태에서 출발·충전 방지, 슬립 방향과 프레임 간격 일관성을 포함한다.

측정 예시: 25.813m/s(약 93km/h)에서 0.5초 동안 W+Space를 누르면 21.671m/s(약 78km/h)로 줄어든다. 같은 상황에서 W만 누르면 30.065m/s로 증가한다. 더 빠른 45m/s에서는 같은 드리프트 시간 후 39.204m/s가 된다.

실제 영업 모드 플레이어에서는 0.35초 동안 W+Space를 누르는 동안 110.93km/h에서 99.25km/h로 감소했다. 벽에 부딪히지 않은 상태에서 실제 속도 벡터도 함께 줄어들었고, Space 해제 후 W 입력으로 다시 가속했다. 드리프트 해제로 카트 부스터나 추가 생산거리 보상이 생기지 않는 것도 확인했다. 감속 안내와 속도계가 보이는 [플레이어 캡처](screenshots/drift-slowdown.png)를 확인했다.

감속으로 인해 기존 저속 충돌 검사 차량이 벽에 닿기 전에 정지하는 것을 확인했다. 해당 검사는 정상 가속으로 진입 속도를 확보하도록 준비만 변경했으며, 충돌 시 드리프트 취소와 잘못된 스킬 지급 방지 조건은 유지했다. 기존 플레이어의 긴 드리프트 충전 준비도 같은 이유로 이니셜D 모드 진입 가속을 0.4초에서 0.8초로 조정했다.

## 재현

```powershell
./Tools/test-feel.ps1
./Tools/test-acceleration.ps1
./Tools/test-core.ps1
./Tools/test-booster.ps1
./Tools/build.ps1 -BuildFolder Builds/DriftSlowdown
./Tools/verify-player.ps1 -BuildFolder Builds/DriftSlowdown -OutputFolder Logs/Smoke-drift-slowdown -ShopShift
./Tools/verify-player.ps1 -BuildFolder Builds/DriftSlowdown -OutputFolder Logs/Smoke-drift-legacy
./Tools/build.ps1 -BuildFolder Builds/DriftSlowdown -Release
```

플레이어 검증은 별도 저장 폴더에서 실제 Tick과 EventSystem 핸들러를 실행한다. 물리 키보드 자동화는 포함하지 않는다.

최종 실행 파일: `Builds/DriftSlowdown/CottonCircuit.exe` (Release, 2026-09-23 11:28:57 UTC).

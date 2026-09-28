# 솜사탕 줄 검증

2026-09-27 · Unity 6000.5.3f1 · Windows

카트와 가운데 솜사탕을 잇는 줄은 불투명한 단색 선이 거의 직선으로 그어져 레이저처럼 보였다. 이 줄을 솜사탕 기계에서 뽑혀 나오는 솜처럼 보이도록 바꿨다. 가운데의 솜 띠 하나와, 그 둘레를 감는 가는 실 두 가닥으로 이루어진다.

## 동작

- 솜 띠는 맛 색상(딸기 분홍, 소다 하늘색, 바닐라 노랑)의 부드러운 반투명 띠다. 양쪽 가장자리가 각자 뭉게뭉게 부풀고, 가운데와 가는 결이 조금 밝아서 둥근 솜처럼 보인다.
- 솜 띠의 무늬는 초당 6 m 속도로 카트에서 솜사탕 쪽으로 흘러간다. `CottonCircuit/SugarFloss` 셰이더가 시간에 따라 텍스처를 움직인다.
- 줄은 카트 노즐에서 가늘게 나와 처음 1.2 m 안에 부풀어 오른다. 줄 길이(약 75~125 m)와 관계없이 같은 거리에서 부푼다.
- 달리는 동안 줄은 노즐에서 약 10 m 떨어진 곳에서 가장 크게 휘며 카트 뒤로 늦게 따라온다. 코너를 돌면 휜 모양이 약 0.4초에 걸쳐 새 방향으로 옮겨 가서 줄이 부드럽게 흔들린다. 느린 물결도 솜사탕 쪽으로 흘러간다. 양 끝은 노즐과 솜사탕에 고정된다.
- 가는 실 두 가닥은 2.5 m마다 한 바퀴씩 솜 띠를 감으며 솜사탕 쪽으로 돈다. 노즐과 솜사탕에서는 솜 띠로 모인다.
- 줄이 켜지고 꺼지는 조건은 바꾸지 않았다. 일시정지, 설탕 비우기, 다음 날로 넘어갈 때는 솜 띠와 실이 함께 숨는다.

## 검증 근거

- 전용 시나리오(`--shop-shift-smoke --sugar-floss-smoke`) **87개 통과**: `Logs/Smoke-sugar-floss/result.txt`. 자동 주행 300프레임(게임 시간 60 fps 고정) 동안 매 프레임 다음을 확인했다.
  - 솜 띠와 두 실의 양 끝이 노즐과 솜사탕에 붙어 있다(291/291프레임).
  - 시속 72 km 이상에서 노즐 근처의 줄이 카트 뒤로 휘어 있다(263/263프레임).
  - 노즐 근처 줄의 프레임 간 최대 이동은 0.035 m다.
  - 일시정지하면 솜 띠와 두 실이 모두 숨는다.
- 연속 프레임 캡처 80장(`Logs/Smoke-sugar-floss/floss-*.png`)으로 흐름과 흔들림을 검토했다.
- 전체 상점 개발 플레이어 검사 **3,319개 통과**, 기존 모드 검사 **625개 통과**. 변경 전과 검사 수가 같다. `Logs/Smoke-sugar-floss-shift/result.txt`, `Logs/Smoke-sugar-floss-legacy/result.txt`.
- 씬 생성 후 통합 검사에서 솜 띠 재질과 숨겨진 실 두 가닥의 연결을 확인한다. URP 셰이더 검사는 서브셰이더에 `RenderPipeline = UniversalPipeline` 태그가 있는 프로젝트 셰이더를 허용하도록 넓혔다.

## 파일과 재현

- `Scripts/WorldView.cs`의 `UpdateThread`가 곡선, 흔들림, 실의 나선을 계산한다.
- `Rendering/SugarFloss.shader`는 URP 비조명 반투명 셰이더다. 텍스처 알파가 솜의 모양을, 빨강 채널이 흰 결의 세기를 정한다.
- `Editor/SugarFlossTexture.cs`가 `Materials/SugarFloss.png`를 만들고, `ProjectBuilder`가 재질과 두 실을 씬에 연결한다. 가는 실은 기존 `SugarThread.mat`를 쓴다.

```powershell
./Tools/build.ps1 -BuildFolder Builds/SugarFloss
./Tools/verify-player.ps1 -BuildFolder Builds/SugarFloss -OutputFolder Logs/Smoke-sugar-floss-shift -ShopShift
# 줄 전용 검사와 연속 캡처: 개발 플레이어에 다음 옵션을 전달
# --shop-shift-smoke --sugar-floss-smoke --smoke-dir=<검사 출력 폴더>
```

화면(왼쪽이 변경 전): [영업 중 딸기](screenshots/sugar-floss-growing.png), [클래식 카트 소다 부스트](screenshots/sugar-floss-boost.png), [다운힐 소다 드리프트](screenshots/sugar-floss-downhill.png).

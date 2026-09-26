# URP 전환 검증

2026-09-26 · Unity 6000.5.3f1 · URP 17.5.0 · Windows

Unity 6.5부터 Built-in Render Pipeline이 deprecated로 표시되어 프로젝트를 열 때 경고가 나왔다. 이 프로젝트는 템플릿 없이 `-createProject`로 만들어져 Built-in으로 시작했고, 이후 머티리얼과 빌더 코드가 모두 Built-in 셰이더를 기준으로 작성되어 있었다. 이번 작업에서 렌더링을 URP로 옮기고, 화면 결과는 기존 Built-in 빌드와 같게 유지했다.

## 변경 내용

- `Packages/manifest.json`에 `com.unity.render-pipelines.universal` 17.5.0을 추가했다.
- `ProjectBuilder`가 `Assets/CottonCircuit/Rendering/CottonCircuitURP.asset`과 Universal 렌더러를 만들고 유지한다. 기존 Standalone 기본 품질(Ultra)에 맞춰 4x MSAA, HDR 끔, 부드러운 태양 그림자, 4단 캐스케이드, 그림자 거리 100m로 설정한다. 이 에셋을 Graphics 설정과 6개 품질 단계 모두에 지정한다. URP에서는 무시되는 `QualitySettings.antiAliasing`, `QualitySettings.shadowDistance` 설정은 삭제했다.
- `Standard` 머티리얼 16개는 `Universal Render Pipeline/Lit`(Smoothness 0.18, Metallic 0)로, `Unlit/Color`였던 Ground는 `Universal Render Pipeline/Unlit`으로 바꿨다. 빌드할 때마다 머티리얼을 새로 작성하므로 Built-in 속성이 에셋에 남지 않는다. GUID는 유지되어 기존 프리팹 참조가 그대로 이어진다.
- 궤적·설탕 실의 `Sprites/Default`와 TextMesh의 `GUI/Text Shader`는 URP에서도 그대로 렌더링되어 유지했다. 클라우드 브랜치와 충돌하지 않도록 `KartController`는 수정하지 않았다.
- Unity가 URP를 활성화하면서 `Assets/UniversalRenderPipelineGlobalSettings.asset`, `Assets/DefaultVolumeProfile.asset`, `ProjectSettings/ShaderGraphSettings.asset`을 생성했다.
- 프로젝트에는 Post-processing Stack v2, 베이크된 라이트맵, 반사 프로브, 커스텀 셰이더가 없어 따로 옮길 항목이 없었다. 조명은 실시간 방향광 하나와 Trilight 환경광이다.
- `IntegrationChecks`에 URP 검사 11개를 추가했다. 기본 파이프라인, 6개 품질 단계의 파이프라인, 씬 렌더러가 쓰는 모든 셰이더가 URP에서 렌더링되는지 확인한다.

### 검사용 스크린샷 캡처 수정

URP의 Render Graph는 Base 카메라의 색 버퍼를 clear 설정과 관계없이 항상 지운다(`UniversalRendererRenderGraph.GetClearCameraParams`). 기존 검사 캡처는 월드를 그린 텍스처 위에 UI 카메라를 한 번 더 렌더링했기 때문에, URP에서는 월드가 카메라 기본 배경색으로 덮였다. 첫 URP 실행에서도 검사는 통과했지만 레이싱 화면이 파란색이었다. 그래서 `RuntimeCapture`는 이제 UI를 투명 텍스처에 따로 렌더링한 뒤, GPU처럼 선형 공간에서 premultiplied alpha로 월드 이미지 위에 합성한다. 실제 게임 화면 출력은 이 코드와 관계없다.

## 검사

| 검사 | Built-in 베이스라인 | URP | 근거 |
|---|---|---|---|
| Unity 에디터·씬·저장 | 73개 통과 | 84개 통과(URP 11개 추가) | `Logs/editor-checks.txt`, `Logs/build.log` |
| 기존 모드 개발 플레이어 | 623개 통과 | 623개 통과 | `Logs/Baseline-smoke`, `Logs/URP-smoke` |
| 영업 모드 개발 플레이어 | 623개 통과 | 623개 통과 | `Logs/Baseline-shopshift`, `Logs/URP-shopshift` |
| 성장 개발 플레이어 | 429개 통과 | 429개 통과 | `Logs/Baseline-progression`, `Logs/URP-progression` |
| Windows 릴리스 | — | 빌드 성공 | `Builds/URP-Release` |

같은 커밋(`4cd1ab6`)의 Built-in 빌드와 URP 빌드가 만든 스크린샷 45장을 픽셀 단위로 비교했다. 월드가 보이는 장면의 평균 차이는 0~255 기준 최대 3.1이고, 성장 트리처럼 UI만 있는 장면은 0.2 이하다. 남은 차이는 Lit 셰이더의 음영과 텍스트 가장자리 정도다. 레이싱, 다운힐, 결과, 가게 장면을 나란히 확인했을 때 머티리얼 색, 그림자, 궤적, 설탕 실, 솜사탕 메시가 같게 보였다([레이싱 비교](screenshots/urp-migration-racing.png)).

검사 캡처는 카메라를 하나씩 따로 렌더링하므로, 실제 창에서 여러 카메라가 합성되는 과정은 별도로 확인했다. 두 빌드를 창 모드로 실행하고 `PrintWindow`로 게임 창을 캡처했다. URP에서도 레터박스 배경, 왼쪽 게임 카메라, 오른쪽 위 가게 미리보기 카메라가 각자의 뷰포트에 정상적으로 합성되었다([분할 화면 창 캡처](screenshots/urp-migration-split-window.png)). 두 캡처는 자동 주행 시점만 몇 초 다르다.

그림자 편향은 조명의 Built-in 값(0.035) 대신 URP 파이프라인 기본값(depth 1, normal 1)을 쓴다. 비교한 장면에서 그림자 결함은 보이지 않았다.

## 재현

```powershell
./Tools/build.ps1 -BuildFolder Builds/URP
./Tools/verify-player.ps1 -BuildFolder Builds/URP -OutputFolder Logs/URP-smoke
./Tools/verify-player.ps1 -BuildFolder Builds/URP -OutputFolder Logs/URP-shopshift -ShopShift
./Tools/verify-progression.ps1 -BuildFolder Builds/URP -OutputFolder Logs/URP-progression
./Tools/build.ps1 -BuildFolder Builds/URP-Release -Release
```

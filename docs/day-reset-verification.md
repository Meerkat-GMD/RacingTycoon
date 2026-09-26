# 하루 초기화와 설탕 비우기 검증

2026-09-23 사용자 요청: 다음 날에는 진열대·손님·설탕 등 하루 상태를 초기화하고, 설탕 비우기 버튼 및 우클릭 단축키를 추가한다.

## 반영한 동작

- 다음 날 시작: 재고, 완료 제품 ID, 남은 설탕과 맛, 제작 중 솜사탕의 거리·맛·품질·제품 ID, 이전 손님과 반응, 당일 통계를 비운다. 10분 타이머와 새로운 첫 손님으로 시작한다.
- 차량은 출발점으로 돌아간다. 선택, 드래그, 제작 미리보기, 진열대 표시, 설탕 실 효과도 갱신한다.
- 코인, 업그레이드, 진열대 확장, 누적 판매 실적은 유지한다.
- `설탕 비우기 · 우클릭` 버튼과 마우스 오른쪽 버튼은 같은 `GameController.EmptySugar()`를 호출한다. 설탕만 비우고 제작 중 솜사탕과 재고·손님·시간은 유지한다.
- 비우기 시 잡고 있던 드래그를 취소한다. 비어 있거나 일시정지·마감 상태에서는 설탕 상태를 바꾸지 않는다. 변경은 즉시 저장한다.

## 검증 결과

- 핵심 모델: `Tools/test-shop-shift.ps1` — 38 통과, 0 실패. 변경 전 새 기대값은 35 통과, 3 실패였으며, 수정 후 통과했다. 마감 경계 프레임의 거리 잘라내기 검증도 유지했다.
- 개발 플레이어: `Tools/build.ps1 -BuildFolder Builds/DayReset` 성공.
- 최종 배포 빌드: `Tools/build.ps1 -BuildFolder Builds/DayReset -Release` 성공. 편집기 통합 검사 55개 통과, `build-info.json`의 Release 구성을 확인했다.
- 실제 플레이어: `Tools/verify-player.ps1 -BuildFolder Builds/DayReset -OutputFolder Logs/Smoke-day-reset -ShopShift` — 3,027 검사 통과. 별도 저장 폴더를 사용했다.
- 버튼의 실제 EventSystem 레이캐스트와 클릭 처리, 제작물 보존, 즉시 저장, 드래그 취소, 일시정지·마감 차단, 다음 날 초기화와 재로드를 검증했다.
- 우클릭은 컴파일된 `Update()`의 `Input.GetMouseButtonDown(1)` 연결을 코드 리뷰로 확인했다. 운영체제 마우스 이벤트를 주입하는 자동 검증은 수행하지 않았다.
- 1600×900, 1280×720, 1280×960, 1920×820에서 버튼 포함 화면 경계를 검사했다. 생성된 기본/720p/마감 화면을 눈으로 확인했고 새 버튼과 문구의 잘림이나 겹침은 없었다.
- 독립 코드 리뷰에서 핵심 로직과 화면·입력 연결에 수정이 필요한 문제는 없었다.
- 부가 회귀 검사: 기본 모델 22/22, 주행 33/33, 주문 14/14, 연속 생산 22/22 통과.

## 기존 실패와 한계

`Tools/test-recipes.ps1`의 오래된 저장 호환성 검사 3개는 14 통과, 3 실패였다. 작업 시작 시 보관한 `ShopShift.cs`와 동일한 나머지 소스로 다시 실행해 같은 실패를 재현했다. 이번 변경에서 추가된 실패는 아니다. 해당 검사는 V1/V2 이전과 미래 버전 허용 여부를 다루며, 이번 하루 초기화 변경의 범위 밖이므로 수정하지 않았다.

- 결과: `Logs/recipe-tests.txt`, `Logs/recipe-day-reset-baseline.txt`
- 실제 플레이어 결과: `Logs/Smoke-day-reset/result.txt`
- 개발 빌드 로그: `Logs/day-reset-development-build.log`
- 화면: `Logs/Smoke-day-reset/day-reset-fresh-day.png`, `day-reset-sugar-loaded.png`, `day-reset-sugar-empty.png`, `05-closed.png`, `06-shop-1280x720.png`

최종 실행 빌드 경로: `Builds/DayReset/CottonCircuit.exe`.

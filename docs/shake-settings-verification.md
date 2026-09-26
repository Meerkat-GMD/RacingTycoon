# 설탕 흔들기 폭 설정

> 이 문서는2026-09-24의 이전 구현 기록이다. 2026-09-26 사용자 정정에 따라 게임 내 설정 UI와 별도 설정 저장은 제거하고 Unity Inspector 조절로 대체했다. 현재 사용법은 README와 `docs/inspector-shake-verification.md`를 참고한다.

2026-09-24. 준비 화면 또는 영업 화면의 **설정** 버튼, 혹은 Esc에서 **설탕 흔들기 → 10g 흔들기 폭**을 조절한다.

- 슬라이더와 숫자 입력:24~600px, 기본값132px. 값이 클수록 같은 움직임의 투입량이 줄어든다.
- 기본값 버튼은132로 복원한다. 숫자 입력은 Enter·포커스 이동·Esc로 닫을 때 적용한다. 빈 입력은 현재 값으로 돌아가고, 범위를 벗어난 값은 상한/하한으로 맞춘다.
- 기존12px 미세 떨림 제외와 최대10g은 유지한다. 실제 계산은 `max(0, 흔든 폭 - 12) / (설정값 - 12) × 10g`, 최대10g이다. 입력 폭은 기준 Canvas 좌표를 사용한다.
- 설정 변경 중인 드래그는 취소한다. 같은 설정값을 다시 동기화하는 경우에는 진행 중인 흔들림을 끊지 않는다.
- `sugar-input-settings.txt`에 별도로 저장해 게임 재실행과 새 가게 시작 시에도 유지한다. 가게 저장 형식 V8은 바꾸지 않는다. 잘못된 설정 파일은 기본값으로 불러온다.
- 자세한 설명은 호버로 제공한다. 일시정지 중에도 호버가 유지되며, 저장 실패 알림은 마감 화면 위에서도 보이도록 처리했다.

## 검증 결과

- `Tools/test-sugar-shake.ps1`: **23개 통과**. 동일 동작의 설정별 투입량, 끝값, 범위/비정상 수치, 변경 시 초기화, 기존 흔들림 처리 확인.
- `Tools/test-sugar-input-settings.ps1`: **12개 통과**. 재로드, 손상 파일, 숫자 문화권, 독립 저장 경로, 쓰기 실패 시 기존 파일 보존 확인.
- `Tools/test-shop-shift.ps1`: **38개 통과**.
- Unity 개발 빌드 및 Editor 통합 검사 **58개 통과**.
- `Tools/verify-progression.ps1 -OutputFolder Logs/ShakeSettingsFinalSmoke`: **473개 통과**. 실제 UI 포인터와 InputField 키 처리, 숫자·슬라이더·기본값·Esc, 저장 재로드, 132px 동작이 설정252에서5g/기본값에서10g, 새 가게 시작 시 설정 유지,1600×900/1280×720 화면 표시 확인.
- `Tools/verify-player.ps1 -ShopShift -OutputFolder Logs/ShakeSettingsLegacySmoke`: **3,033개 통과**.
- 독립 검토에서 발견된 Esc 입력 취소 및 마감 후 저장 실패 알림 문제를 수정하고 재검토했다. 추가 결함 없음.

런타임 테스트는 별도 임시 저장 폴더에서 실행했다. 키 입력은 실제 InputField 처리 API를 사용하며, 실제 이벤트 루프처럼 `ForceLabelUpdate`로 표시를 갱신한 후 캡처했다. 숫자252의 렌더링과 두 해상도의 레이아웃을 직접 확인했다.

화면: [설정 화면](screenshots/sugar-shake-settings.png).

Release 빌드 성공(종료 코드0),2026-09-24T08:53:14Z. 실행 파일: `Builds/Progression/CottonCircuit.exe`.

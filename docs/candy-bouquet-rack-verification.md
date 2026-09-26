# 솜사탕 봉지 진열대 검증

2026-09-23 · Unity 6000.5.3f1 · Windows

승인된 시안의 투명봉지, 집게, 방사형 철제 가지를 기존 상점의 uGUI 진열대에 적용했다. 개별 카드와 빈 자리 문구를 제거하고, 중앙 기둥과 작은 금속 받침대에서 가지가 퍼지는 형태로 구성했다. 현재 게임의 크기 명칭인 소·중·대와 미완성 태그를 사용한다.

## 동작

- 기존 저장의 6/9/12개 용량을 그대로 사용하며 용량별로 봉지 크기와 간격을 조정한다.
- 솜사탕 색상은 맛, 내부 솜의 부피는 실제 제작 거리를 따른다. 투명봉지 가장자리와 반사선만 얇게 그린다.
- 상품 ID를 집게에 연결하여 이웃 상품을 판매·폐기해도 남은 상품은 제자리에 유지된다.
- 봉지 전체가 드래그 영역이다. 집으면 원래 상품을 숨기고 봉지 형태의 이동 이미지를 보여준다. 취소하면 같은 자리로 돌아온다.
- 마우스를 올리거나 클릭하면 하단 고정 정보줄에서 맛·크기·바퀴 수를 확인한다.
- 고객과 쓰레기통의 기존 전달 경로 및 병행 작업의 재제작 전달 경로를 사용한다.

## 검증 근거

- RED baseline: 현재 그리드 UI에 새 검증을 실행하여 `Cotton candy stand`가 없다는 예상 실패를 확인했다. `Logs/Smoke-candy-rack-red/result.txt`.
- 최신 전용 진열대 개발 플레이어 검사 **2,412개 통과**. `Logs/Smoke-candy-rack-final/result.txt`.
- 실제 EventSystem으로 각 봉지의 중앙과 여러 지점에서 선택을 확인했다. 6/9/12개를 최대 시각 크기까지 채운 경우, 미완성 태그, 집게 자리 유지, 선택 정보, 드래그 취소, 정확한 판매 대금, 폐기, 드래그 도중 이웃 재고 제거를 검사했다.
- 1600×900, 1280×720, 1280×960, 1920×820에서 확인했다. 모든 봉지 선택 영역은 서로 겹치지 않고 진열대 안에 들어간다. 기울어진 태그의 네 모서리도 선택 영역 안에 들어간다.
- 실제 렌더 캡처로 빈 진열대, 1개, 9개, 12개와 이동 중 화면을 검토했다. 숨김 플레이어에서 새로 활성화한 Canvas 요소는 화면을 렌더링한 뒤 포인터 검증을 실행한다.
- 기존 모드 개발 플레이어 검사 **623개 통과**. `Logs/Smoke-candy-rack-legacy/result.txt`.
- 독립 코드 리뷰에서 발견한 회전 태그의 하단 입력 영역 이탈을 수정했으며 재검토에서 남은 진열대 관련 지적은 없다.
- 최신 개발 빌드 성공: `Logs/candy-rack-development-build.log`, `Builds/CandyRack/build-info.json`.
- 병행 재제작/손님 반응 작업과 합친 최종 출시 전 개발 플레이어의 전체 상점 검사 **2,872개 통과**. `Logs/Smoke-candy-resume-release-check/result.txt`. 진열대 전용 시나리오와 임시 손님 ID의 저장 유효성 검사도 포함한다. 숨김 창에서 새로 활성화한 UI의 native Canvas depth가 -1이 되는 검증 문제는 실제 렌더 뒤 포인터를 검사하도록 해결했다. 프로덕션 우회 코드는 추가하지 않았다.
- 최종 통합 Release 빌드 완료: `Builds/CandyResume/build-info.json`의 `configuration`이 `Release`임을 확인했다.

## 파일과 재현

`CandyRackGraphic.cs`가 가지와 집게 배치를, `CandyRackUI.cs`가 상품 연결과 정보를, `ShopArtGraphic.cs`의 `BaggedCottonCandy`가 봉지 그림을 담당한다. `ShiftUI.cs`와 `ShopDragItem.cs`에서 기존 상점/드래그에 연결한다. 장면·프리팹을 재생성할 필요가 없다.

```powershell
./Tools/build-candy-rack.ps1
./Tools/verify-player.ps1 -BuildFolder Builds/CandyRack -OutputFolder Logs/Smoke-candy-rack-shop -ShopShift
# 전용 진열대 검사: 개발 플레이어에 다음 옵션을 전달
# --shop-shift-smoke --candy-rack-smoke --smoke-dir=<별도 검사 출력 폴더>
```

진열대 개발 빌드: `Builds/CandyRack/CottonCircuit.exe`. 병행 UI 작업을 포함한 최종 통합 Release 실행 파일: `Builds/CandyResume/CottonCircuit.exe`.

화면: [9개 진열](screenshots/candy-bouquet-rack.png), [판매 후 빈 집게](screenshots/candy-bouquet-rack-sparse.png), [12개 확장과 미완성 태그](screenshots/candy-bouquet-rack-expanded.png).

## 드래그 커서 위치 수정

사용자가 보고한 커서 왼쪽 아래로 솜사탕이 떨어지는 현상을 재현했다. `ShiftUI.MoveShiftDrag`의 상품 전용 고정 오프셋 때문에 1600×900에서 커서 `(1076,384)`에 대해 그림 중심은 `(1001.36,321.36)`으로 나타났다. 수정 전 실패는 `Logs/Smoke-candy-drag-red/result.txt`와 `player.log`에 기록했다.

축소 배율을 적용한 실제 그림 중심을 커서에 맞추도록 수정했다. 이름표는 중심 계산에서 제외하며 화면 가장자리에서도 강제 위치 제한으로 커서와 어긋나지 않는다. 설탕 봉지의 기존 위치와 흔들기 판정은 유지한다.

- 전체 상점 개발 플레이어 검사 **2,950개 통과**: `Logs/Smoke-candy-drag-fixed/result.txt`.
- 픽업 직후, 화면 중앙, 네 모서리에서 실제 그림 중심과 포인터의 오차가 1px 미만임을 검사했다. 1600×900, 1280×720, 1280×960, 1920×820과 6/9/12개 진열 용량을 포함한다.
- 이동 이미지의 포인터 통과, 드래그 취소 및 판매·폐기·재제작 전달 경로가 모두 통과했다. 실제 이동 중 렌더는 `Logs/Smoke-candy-drag-fixed/13-rack-carry.png`에 저장했다.
- 수정된 Release 실행 파일: `Builds/CandyDragFix/CottonCircuit.exe`. `build-info.json`의 Release 구성과 Unity 빌드 성공을 확인했다. 실행 중인 이전 `Builds/CandyResume` 플레이어는 변경하지 않았다.

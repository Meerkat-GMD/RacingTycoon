# 솜사탕 서킷 · Cotton Circuit

거대한 솜사탕 기계 안을 카트로 달려 중앙 막대에 설탕 실을 감고, 완성한 솜사탕을 판매해 가게를 성장시키는 Windows용 Unity 게임입니다.

## 실행

- 바로 플레이: `Builds/Windows/CottonCircuit.exe` 실행. 같은 폴더의 `CottonCircuit_Data`, `MonoBleedingEdge`, DLL을 함께 유지하세요.
- Unity 편집: Unity Hub에서 이 폴더를 추가하고 **Unity 6000.5.3f1**로 엽니다. `Assets/CottonCircuit/Scenes/CottonCircuit.unity`를 열고 Play를 누르세요.
- 가게 화면의 **만들러 가기**를 누르고 **W를 길게 눌러** 출발합니다.

## 조작과 진행

| 입력 | 동작 |
|---|---|
| W / 위 방향키 | 가속 |
| A / 왼쪽 방향키 | 안쪽 레인으로 이동 |
| D / 오른쪽 방향키 | 바깥쪽 레인으로 이동 |
| S / 아래 방향키 / Space | 제동 |
| Enter | 출발 / 조기 완성 / 가게 복귀 |
| R | 주행 중 안전한 출발 위치로 복귀 |
| Esc | 일시정지, 조작 안내, 소리 설정 |

원형 코스를 자동으로 따라가는 주행 보조가 있습니다. 안쪽은 **딸기**, 가운데는 **소다**, 바깥쪽은 **바닐라**입니다. 실제로 이동한 회전량이 솜사탕의 양과 층을 만들고, 레인 선택이 색과 두께를 바꿉니다. 멈추면 생산되지 않습니다.

주행은 최대 60초이며 설탕통이 가득 차면 자동 완성됩니다. 가게로 돌아오면 손님이 진열된 솜사탕을 구매합니다. 모은 코인으로 카트 모터, 설탕통, 가게를 각각 3단계까지 업그레이드합니다. 진열대에는 최대 6개를 보관합니다.

게임은 `%USERPROFILE%\AppData\LocalLow\SugarRoad Studio\Cotton Circuit\cotton-circuit.json`에 자동 저장됩니다. 도움말의 **새 가게 시작**을 두 번 누르면 기존 저장 파일을 보관하고 새로 시작합니다. 주행 중간은 저장되지 않습니다. Windows 시스템 글꼴인 맑은 고딕을 사용하며 해당 글꼴 파일을 재배포하지 않습니다.

## Blender 원본

- `Art/Blender/CottonCircuit.blend`: 편집 가능한 원본, 에셋별 컬렉션과 프리뷰 장면.
- `Art/Blender/preview.png`: 에셋 전체 미리보기.
- `Art/Blender/create_assets.py`: 모델과 FBX를 재생성하는 Blender Python 스크립트.
- `Art/Blender/validate_assets.py`: 모델 크기, 재질, FBX 재임포트 검증.
- `Assets/CottonCircuit/Models/`: 카트·판매대·제작기·솜·손님·설탕 결정·아치·나무·가로등의 FBX 9종.

프로젝트 루트에서 다음과 같이 재생성합니다.

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -b -t 4 -P Art/Blender/create_assets.py
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -b -t 4 -P Art/Blender/validate_assets.py
```

Unity의 `Cotton Circuit > Rebuild game scene` 메뉴는 생성된 프리팹·재질과 메인 씬을 다시 만듭니다. 메인 씬에서 직접 편집한 배치는 재생성 시 대체되므로 별도 씬으로 저장해서 보관하세요.

## 개발 및 검증

```powershell
./Tools/test-core.ps1
./Tools/build.ps1
./Tools/verify-player.ps1
./Tools/build.ps1 -Release
```

빌드 전 이 프로젝트의 Unity 에디터를 닫아주세요. 기본 빌드 스크립트는 씬을 구성하고 저장·연결 검증을 실행한 뒤 Windows 개발 빌드를 생성합니다. 실행 검증은 별도의 저장 경로를 사용해 정상 저장 데이터를 건드리지 않으며, 주행·색 변경·판매·구매·재저장과 화면 캡처를 수행합니다. 마지막의 `-Release` 빌드는 자동 검증 코드를 제외한 배포용 실행 파일로 교체합니다. 다시 자동 플레이를 검증할 때는 기본 개발 빌드를 먼저 만드세요.

첫 버전은 코스 1개와 싱글플레이 제작 레이스를 제공합니다. 경쟁 AI, 모바일 조작, 가게 자유 배치는 포함하지 않습니다. 검증 결과는 `docs/verification.md`에 기록합니다.

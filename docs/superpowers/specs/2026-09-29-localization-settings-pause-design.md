# 번역, 설정창, 일시정지 화면 리뉴얼

작성일: 2026-09-29. 브랜치: `claude/localization-settings` (기준 커밋 `b1a9048`).

## 사용자 결정

- 게임을 시작하면 OS 언어에 맞춰 언어를 자동으로 정한다. 지원하지 않는 언어이면 영어를 쓴다. 기본 지원 언어는 **영어와 한국어**다.
- 플레이어가 게임 안에서 언어를 바꿀 수 있도록 **설정창**을 추가한다. 설정창은 **타이틀 화면**과 **일시정지 화면** 양쪽에서 연다.
- 설정 항목은 **언어, 배경음 음량, 효과음 음량, 전체 음소거, 화면 모드, 해상도**다.
- **일시정지 화면을 리뉴얼**한다. 방향은 실제 게임 레퍼런스를 보고 정했다.
  - 일시정지: Dorfromantik의 측면 패널 구성(레퍼런스 8번)에 Mario Kart 8 Deluxe의 조작 안내(13번)를 더한다.
  - 설정창: Cozy Grove의 카드 한 장 구성(26번)을 따른다.
  - 오른쪽 진행 요약(8번·9번의 기록 표시)은 넣지 않는다. 상단 HUD에 같은 정보가 있기 때문이다.
- 번역 시스템은 **방식 A: 자체 문자열 표**로 만든다. Unity Localization 패키지(방식 B)와 C# 코드 사전(방식 C)은 채택하지 않았다.
- Kenney Input Prompts 1.5(CC0) 압축 파일을 다시 내려받는 것을 승인했다.

## 결정 과정

- 레퍼런스는 Game UI Database에서 일시정지 화면 22종과 설정 화면 11종을 골라 번호를 붙인 갤러리로 비교했다. 갤러리는 `.superpowers/pause-refs/index.html`(git 제외, `.claude/launch.json`의 `pause-refs` 항목으로 서버 실행)에 있다.
- 8번 구성을 고른 이유: 타이틀 화면이 이미 "왼쪽 크림색 카드 + 큰 제목 + 세로 버튼 + 오른쪽 배경 그림"이라서, 같은 카드와 버튼 스타일로 일시정지 화면을 만들면 타이틀과 일관된다. 1~7번 중앙 카드형은 현재 구조와 거의 같아서 리뉴얼 효과가 작다. 레이싱 게임 레퍼런스는 게임마다 고유한 원색·장식이 강해 파스텔 토이 톤과 맞지 않는다. 소품형은 전용 그림이 필요해 로우폴리 아트 방향과 맞추기 어렵다.
- 13번의 조작 안내를 더한 이유: 현재 일시정지 화면에서 가장 큰 부분이 조작법을 적은 긴 글이다. 키캡 이미지로 바꾸면 훨씬 빨리 읽힌다.
- 26번을 고른 이유: 항목이 여섯 개뿐이라 탭 없이 카드 한 장에 들어간다. 크림색 카드와 민트색 슬라이더가 게임 색과 같고, `◀ 값 ▶` 선택 방식이 방향키 조작과 맞는다.
- 방식 A를 고른 이유: `Core` 규칙 코드는 Unity 없이 `mcs`로 컴파일해 테스트하므로 Unity 패키지를 참조할 수 없다. 지원 언어가 두 개이고, 시작할 때 동기적으로 읽을 수 있고, 한 줄에 한국어와 영어가 나란히 있어 번역을 검토하기 쉽다. 방식 B는 Addressables와 비동기 초기화가 추가되고, 번역 표가 ScriptableObject라 변경 검토가 어렵다.

## 1. 번역 시스템

### 언어 결정

게임을 시작할 때 다음 순서로 한 번 결정한다.

1. 명령줄의 `--language=ko` 또는 `--language=en`. 자동 검사에서 언어를 고정할 때만 쓴다.
2. 설정 파일에 저장된 플레이어의 선택.
3. OS 언어. `Application.systemLanguage`가 `Korean`이면 한국어, 그 밖의 모든 값(`Unknown` 포함)은 영어.

플레이어가 설정창에서 언어를 고르기 전까지는 설정 파일에 언어를 저장하지 않는다. 그래서 OS 언어를 바꾸면 다음 실행에 반영된다.

### 문자열 표

- 파일: `Assets/Resources/Localization/strings.tsv` (UTF-8, BOM 없음). 첫 줄은 머리글 `key	ko	en`이다.
- 키는 소문자와 점으로 구분한다. 앞부분은 화면이나 기능 이름이다: `title.*`, `intro.*`, `tutorial.*`, `business.*`, `prep.*`, `trait.<id>.name`, `trait.<id>.desc`, `notice.*`, `save.*`, `pause.*`, `settings.*`, `legacy.*`, `common.*`.
- 값 안의 줄바꿈은 `\n`, 탭은 `\t`, 역슬래시는 `\\`로 쓴다.
- 값이 들어가는 문구는 `string.Format` 자리표시자 `{0}`, `{1}`을 쓴다. 한국어와 영어의 자리표시자 번호 집합은 같아야 한다.
- 문장 조각을 `+`로 이어 붙이던 코드는 완성된 문장 단위의 키로 바꾼다. 영어 어순과 복수형을 맞추기 위해서다. 복수형이 필요한 영어 문구는 `.one`과 `.other` 키로 나눈다.
- 숫자 형식(`N0`, `0.0`)은 지금처럼 코드에서 만든 뒤 자리표시자로 넘긴다.

### 코드 구조

- `Core/Strings.cs` (순수 C#, Unity 참조 없음)
  - `enum Language { Korean, English }`
  - `Strings.Load(string tsv)`: 표를 읽는다. 머리글이 틀리거나 키가 중복되면 예외를 던진다.
  - `Strings.Current`, `Strings.Version`(언어가 바뀔 때마다 1 증가), `Strings.Set(Language)`
  - `Strings.Get(key)`, `Strings.Format(key, params object[] args)`. 키가 없으면 키 문자열을 그대로 돌려주고 `Strings.Missing` 목록에 기록한다.
  - `Strings.Resolve(string commandLine, string saved, bool systemKorean)`: 위의 결정 규칙을 순수 함수로 구현한다.
  - `Strings.Code(Language)`와 `Strings.Parse(string code)`: `"ko"`, `"en"` 변환.
- Unity 쪽 `Localization.cs`: `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`에서 `Resources.Load<TextAsset>("Localization/strings")`를 읽고, 설정 파일과 명령줄을 확인해 언어를 정한다. 누락 키는 개발 빌드에서 `Debug.LogWarning`으로 알린다.
- `Core`의 표시 문구는 `Strings`를 통해 가져온다: 특성 이름·설명(`Progression.Nodes`), 특성 효과 문구(`Progression.Describe`), 기계·장소 이름과 장소 설명, 성장 지도 탭 이름(`UpgradeTreeLayout`), 코스 이름(`RaceRecipe.Name`), 크기 제한 문구(`ProgressionShift`). 이 값들은 속성이나 메서드로 바꿔 호출할 때마다 현재 언어로 읽는다.
- Core 테스트는 `Tools/Tests`에서 디스크의 `strings.tsv`를 직접 읽어 `Strings.Load`에 넘긴다.

### UXML 고정 문구

- `LocalizedText`: `[UxmlObject] public partial class LocalizedText : CustomBinding`, `[UxmlAttribute] string key`.
  - `Update`에서 대상이 `TextElement`이면 `text`를 `Strings.Get(key)`로 바꾼다. 마지막으로 적용한 `Strings.Version`을 기억해 버전이 같으면 아무것도 하지 않는다.
  - `updateTrigger`는 `EveryUpdate`로 둔다. 이벤트 구독이 없어서 요소가 사라질 때 정리할 것이 없다.
- UXML에서는 `<Bindings>` 안에 `<CottonCircuit.LocalizedText property="text" key="..." />`로 붙이고, UI Builder의 Bindings에서 편집할 수 있다. 기존 한국어 `text` 속성은 UI Builder 미리보기용으로 남긴다.
- **코드가 `text`를 바꾸는 요소에는 `LocalizedText`를 붙이지 않는다.** 바인딩이 코드가 넣은 값을 덮어쓰기 때문이다. 이런 요소는 코드에서 `Strings`로 문구를 채운다.

### 실행 중 언어 변경

- 설정창에서 언어를 바꾸면 `Strings.Set`이 버전을 올린다. 바인딩된 고정 문구는 다음 프레임에 스스로 갱신된다.
- 코드가 채우는 문구는 `Localization.Changed` 이벤트를 받은 화면이 `Refresh()`를 다시 호출해 갱신한다: 타이틀(`TitleScreenUI`), 게임 UI(`GameUI`), 튜토리얼(`TutorialOverlayUI`), 인트로(`IntroStoryUI`).
- 이미 떠 있는 알림(`GameController.Notice`)은 바꾸지 않는다. 다음 알림부터 새 언어로 나온다.

### 번역 범위

- 대상: 게임에서 보이는 모든 문구. 타이틀, 새 게임 확인, 인트로 대사와 화자 이름, 튜토리얼, 영업 HUD, 손님 말풍선, 진열대, 설탕 봉지, 결과 화면, 영업 준비, 성장 지도, 특성 설명, 장비·장소 탭, 알림, 저장 오류, 일시정지, 설정창, 이전 모드(Legacy) 화면.
- 제외: Inspector 전용 문구(`[Header]`, `[Tooltip]`, `[InspectorName]`), 코드 주석, 로그와 검사 메시지.
- 인트로 화자 비교(`Speakers[page] == "미나"`)처럼 문구로 상태를 판단하는 코드는 화자 id로 바꾼다.
- 영어 게임 이름은 **Cotton Circuit**이다. 타이틀의 큰 제목 두 줄은 영어에서 `Cotton` / `Circuit`이 된다. 등장인물 미나는 `Mina`, 주인공 화자 "사장"은 `Me`로 옮긴다.
- 영어 번역은 짧고 캐주얼한 어조로 쓴다. 버튼과 HUD 라벨은 한국어와 비슷한 폭이 되도록 가능한 한 짧은 표현을 고른다.
- 글꼴: Noto Sans KR 동적 글꼴(라틴 문자 포함)을 그대로 쓴다.

## 2. 설정창

### 여는 곳

- 타이틀 메뉴: `게임 시작(이어하기) → 새 게임 → 설정 → 게임 종료`. 방향키 이동 순서도 같다.
- 일시정지 화면의 메뉴 카드: `계속하기 → 설정 → 타이틀 화면으로`.
- 둘 다 같은 `Settings.uxml`과 `SettingsUI`를 쓴다. `SettingsUI`는 별도 `UIDocument`(정렬 순서 200)로 가장 위에 뜨고, 닫으면 열었던 화면의 `설정` 버튼에 초점을 돌려준다.
- 설정창이 열려 있는 동안 Esc는 설정창만 닫는다. 일시정지 해제나 타이틀의 `게임 종료` 초점 이동으로 넘어가지 않는다.

### 구성

반투명 어두운 배경 위 가운데 크림색 카드 한 장. 제목은 `설정`.

| 묶음 | 항목 | 조작 | 기본값 |
|---|---|---|---|
| 소리 | 배경음 | 슬라이더 0~100% | 100% |
| | 효과음 | 슬라이더 0~100% (엔진음·기계음·UI 클릭 포함) | 100% |
| | 전체 음소거 | 켜기/끄기 버튼 | 끄기 |
| 화면 | 화면 모드 | `◀ 창 모드 ▶` / `◀ 전체 화면 ▶` | 현재 상태 |
| | 해상도 | `◀ 1600 × 900 ▶` | 현재 상태 |
| 언어 | 언어 | `◀ 한국어 ▶` / `◀ English ▶` | 결정 규칙의 결과 |

- 맨 아래에 `닫기` 버튼.
- 언어 이름은 번역하지 않는다(`한국어`, `English`).
- 슬라이더 옆에 현재 퍼센트를 숫자로 보여 준다.

### 동작

- 모든 변경은 즉시 적용한다. `적용` 버튼은 없다.
- 음량 100%는 현재 게임의 소리 크기와 같다. 배경음 음량은 `MusicPlayer`에 넘기는 음악 레벨에 곱하고, 효과음 음량은 효과음 풀, 부스트음, 엔진·스키드·바람·기계 루프에 곱한다.
- 전체 음소거는 기존 `AudioFeedback.Muted`와 같은 동작이다. 설정창의 전환 결과를 `AudioFeedback`에 반영한다.
- 화면 모드: 창 모드는 `FullScreenMode.Windowed`, 전체 화면은 `FullScreenMode.FullScreenWindow`.
- 해상도 목록: `Screen.resolutions`에서 너비×높이가 겹치지 않게 추리고, 주 모니터의 해상도보다 큰 것을 뺀 뒤 작은 것부터 정렬한다. 현재 창 크기가 목록에 없으면 현재 크기를 목록에 넣는다. 선택하면 `Screen.SetResolution(너비, 높이, 현재 모드)`를 호출한다.
- 키보드: ↑↓로 행 이동, ←→로 값 변경(슬라이더는 10%씩), Enter로 음소거 전환과 `닫기`, Esc로 닫기. Tab·Shift+Tab도 ↓·↑처럼 행만 옮긴다. 마우스로 모두 조작할 수 있고, 마우스로 누른 뒤에도 키보드 조작이 이어지도록 행 안의 조작을 누르면 그 행에, 카드 빈 곳이나 바깥을 누르면 마지막 행에 초점을 둔다.
- 효과음 슬라이더를 움직이면 UI 클릭 소리로 새 음량을 들려준다.

### 저장

- 파일: 저장 폴더(`GameController`가 쓰는 `Application.persistentDataPath` 또는 검사용 폴더)의 `settings.json`. 게임 저장 파일 `cotton-circuit.json`과 분리한다.
- 내용: `Version`, `Language`(`""`, `"ko"`, `"en"`), `MusicVolume`, `EffectsVolume`(0~1), `Muted`.
- 슬라이더를 끄는 동안에는 값을 바로 적용만 하고, 손을 뗄 때 파일에 쓴다. 키보드로 값을 바꾸거나 다른 항목을 바꾸면 바로 쓰고, 창을 닫을 때 아직 쓰지 않은 변경을 쓴다.
- `새 게임`은 게임 저장 파일만 보관 처리하므로 설정은 유지된다.
- 화면 모드와 해상도는 Unity 플레이어가 스스로 기억하므로 파일에 넣지 않는다.
- 파일이 없거나 읽을 수 없거나 값이 범위를 벗어나면 기본값을 쓰고 오류 창은 띄우지 않는다(개발 빌드 로그에만 경고). 쓰기에 실패하면 이번 실행 동안만 적용하고 로그에 경고를 남긴다.
- 음소거는 이제 다음 실행에도 유지된다.
- 순수 C# 부분(값 제한, 직렬화 모델, 기본값)은 `Core/GameSettings.cs`에 두고 Core 테스트로 확인한다. 파일 입출력은 Unity 쪽 `SettingsStore`가 `JsonUtility`로 처리한다.
- 언어는 첫 화면보다 먼저 정해야 하므로, 시작할 때 `Application.persistentDataPath`의 `settings.json`을 읽는다. `GameController.ShowTitle(폴더)`가 다른 폴더를 받으면(검사 러너의 격리 폴더) 설정 저장 위치를 그 폴더로 옮기고 그 폴더의 설정을 다시 읽는다. 이때 명령줄 `--language`가 있으면 언어는 그대로 유지한다. 그래서 검사는 사용자의 설정 파일을 읽을 수는 있어도 절대 쓰지 않는다.

### 없어지는 기존 요소

- 일시정지 화면의 `소리 켜짐/꺼짐` 버튼과 `GameController.ToggleMute`의 UI 연결. 음소거는 설정창에서만 바꾼다.

## 3. 일시정지 화면 리뉴얼

### 위치

- 지금의 `PauseScreen`은 1600×900 고정 구도(`composition`) 안에 있어서 와이드 화면에서 위아래가 잘릴 수 있다. 새 일시정지 화면은 타이틀처럼 **화면 전체를 덮는 레이어**(`gameShell`의 직계 자식)에 두고 flex 배치로 어느 화면 비율에도 맞춘다.
- 배경은 지금보다 옅게 어둡게 한다(`rgba(26, 32, 49, 0.38)` 정도). 뒤의 주행 화면과 가게가 보인다.

### 왼쪽 메뉴 카드

- 타이틀의 `title-menu`와 같은 너비·여백·모서리·배경색, `title-button`과 같은 버튼 스타일을 쓴다. 공용 규칙은 새 `Menu.uss`로 옮겨 타이틀, 일시정지, 설정창이 함께 쓴다. 타이틀의 모양은 바뀌지 않아야 한다.
- 내용: `PAUSE` 작은 제목, `잠깐, 달콤한 휴식` 큰 제목, 버튼 `계속하기`(주 버튼, ▶ 아이콘), `설정`, `타이틀 화면으로`, 아래 `진행 상황은 자동으로 저장됩니다.`
- 저장 실패로 타이틀 복귀가 막히면 지금처럼 알림 토스트로 알린다.

### 오른쪽 조작 안내 카드

반투명 크림색 카드. 제목 `조작 안내`. 키캡 이미지와 짧은 설명을 행으로 나열한다.

| 묶음 | 행 |
|---|---|
| 주행 | `W` 가속 · `S` 제동 · `A` `D` 조향 · `Space` 드리프트 · `Shift` 부스터 · `R` 코스 복귀 |
| 가게 | 마우스 끌기: 설탕 봉지를 주행 화면으로 끌어 흔들기 · 마우스 끌기: 솜사탕을 손님에게 건네기 · `F` 꺼내기 · 마우스 오른쪽: 설탕 비우기 |
| 공통 | `Esc` 일시정지 / 돌아가기 |

- `Shift` 부스터 행은 `GameController.RunStyle == DrivingStyle.Kart`일 때만 보인다.
- 선택한 기계에 알바가 있으면(`SelectedMachineHasWorker`) 주행 묶음을 숨기고 `알바가 운전과 제작을 맡고 있어요.`와 `다른 기계는 상단 버튼으로 선택하고, 알바 배치는 영업 준비 화면에서 바꿀 수 있어요.`를 보여 준다. 가게 묶음에서도 알바가 맡는 설탕 봉지·꺼내기·설탕 비우기 행을 숨긴다.
- 이전 모드(`Shift == null`)에서는 가게 묶음 전체를 숨긴다. 이전 모드에는 설탕 봉지·꺼내기·설탕 비우기·손님에게 끌어 건네기 조작이 없다.
- `공통` 묶음(`Esc`)은 모든 모드에서 보인다. 가게 묶음 아래, 같은 라벨 열에 둔다.
- 방향키(↑↓←→)는 W/S/A/D와 같은 동작이지만 행을 늘리지 않고 설명 문구에 함께 적지 않는다. 화면을 단순하게 두기 위해서다.

### 키캡 이미지

- Kenney Input Prompts 1.5 (CC0), https://kenney.nl/assets/input-prompts, 압축 파일 `kenney_input-prompts_1.5.zip`(약 5.1 MB). 현재 HUD의 `KeyF.png`, `MouseRight.png`와 같은 팩·같은 변형을 쓴다.
- 원본 압축 파일은 `Art/UI/`에 보관하고, 쓰는 PNG만 `Assets/CottonCircuit/UI/Art/`로 복사한다. 가져올 이미지: W, A, S, D, Space, Shift, R, Esc, 마우스 왼쪽 끌기(또는 마우스 왼쪽).
- `Assets/CottonCircuit/UI/THIRD-PARTY.md`에 Input Prompts 항목(출처, 라이선스, 원본 경로, 압축 파일 URL, SHA-256, 내려받은 날짜)을 추가한다. 기존 `KeyF.png`, `MouseRight.png`도 이 항목에 적는다.
- 가져오기 설정은 HUD의 기존 `KeyF.png`와 같다: 기본(Default) 텍스처, 밉맵 켜짐, 알파 투명(`alphaIsTransparency`). 새 이미지 아홉 개의 `.meta`는 guid만 빼면 `KeyF.png.meta`와 똑같다.

### 원칙

- 모든 시각 요소는 UXML/USS로 구성한다. C#은 문구, 표시 여부, 초점만 바꾼다.
- 키보드: 화면이 열리면 `계속하기`에 초점. ↑↓로 버튼 이동, Enter로 실행, Esc로 게임에 돌아간다.

## 4. 검증

### Core 테스트 (`Tools/test-core.ps1` 방식, 새 `Tools/test-localization.ps1`)

- 언어 결정: `systemKorean=true` → 한국어, `false` → 영어, 저장된 `"en"`이 한국어 OS보다 우선, 명령줄이 저장값보다 우선, 알 수 없는 코드는 무시.
- 표 무결성: 머리글, 키 중복 없음, 모든 키에 `ko`와 `en`이 비어 있지 않음, 두 언어의 자리표시자 번호 집합이 같음, 모든 값이 `string.Format`에서 오류 없이 쓰임(짝이 맞지 않는 중괄호 없음), 이스케이프 해석.
- `Format`과 누락 키 처리.
- `GameSettings`: 기본값, 범위 제한, 알 수 없는 언어 코드 정리.
- 한국어 문구를 확인하던 기존 Core 테스트(`ProgressionShiftTests`, `TraitIconTests`)는 표를 읽고 한국어로 설정한 뒤 같은 결과를 확인하도록 고친다. 모든 Core 테스트 스크립트가 통과해야 한다.

### 정적 검사 (`python Tools/check-localization.py`, Unity 없이 실행)

Hangul/키 정적 검사는 `Editor/IntegrationChecks.cs`가 아니라 Unity를 띄우지 않는 `Tools/check-localization.py`로 한다. 빌드 없이 몇 초 안에 끝나 매 커밋마다 돌릴 수 있다.

- UXML의 모든 `LocalizedText` 키가 표에 있다.
- `Assets/CottonCircuit/Scripts`의 `Strings.Get`/`Strings.Format` 문자열 리터럴 키가 모두 표에 있다.
- 실행 코드(`Assets/CottonCircuit/Scripts`, Inspector 속성과 주석 제외)와 UXML `text` 속성 중 `LocalizedText`가 없는 요소에 한글이 남아 있지 않다.
- 표 자체의 형식(머리글, 한 줄에 세 칸, 중복 키)도 함께 본다. 빈 칸, 자리표시자 불일치, 잘못된 형식 문자열은 Core 테스트(`Strings.Validate`)가 본다.

새 키캡 이미지의 가져오기 설정은 자동으로 검사하지 않는다. `Editor/IntegrationChecks.cs`의 스프라이트 검사는 `Assets/CottonCircuit/Sprites/` 아래만 훑기 때문이다. 대신 새 이미지 아홉 개의 `.meta`가 guid 말고는 `KeyF.png.meta`와 같다는 것을 파일 비교로 확인했다.

### 실행 스모크 검사 (`Tools/verify-uitk.ps1`, 개발 빌드)

- 기존 모든 케이스는 `--language=ko`로 실행해 지금의 한국어 기대값을 그대로 확인한다.
- 새 `english` 케이스: `--language=en`으로 타이틀 → 인트로 → 튜토리얼 → 영업 → 일시정지 → 설정 → 영업 준비(성장 지도 모든 탭) → 이전 모드를 지나며 캡처한다. 각 화면에서 보이는 모든 `TextElement`에 한글이 없고 누락 키(`Strings.Missing`)가 없는지 확인한다.
- 새 `settings` 케이스:
  - 타이틀과 일시정지에서 설정창을 열고 닫기, Esc로 닫기, 닫은 뒤 초점 복귀
  - 실행 중 언어 변경이 타이틀·게임 UI·일시정지·설정창에 즉시 반영
  - 배경음·효과음 값이 실제 오디오 레벨에 반영
  - 음소거 전환
  - `settings.json`이 격리 폴더에 저장되고, 새 `SettingsStore`로 다시 읽으면 같은 값
  - 손상된 `settings.json`은 기본값으로 처리
- 문구 넘침과 한글 누출: `Capture(name)`이 스크린샷을 찍기 직전에 `CheckVisibleText(name)`을 불러, 모든 케이스·모든 언어의 모든 캡처에서 화면에 보이는 모든 `TextElement`를 검사한다. 영어일 때 한글이 남아 있으면 실패, `MeasureTextSize`로 잰 글자 크기가 요소 내용 폭(줄바꿈 라벨은 높이)을 넘으면 실패(레이아웃이 픽셀 단위로 반올림되므로 1.5px 여유를 둔다. 줄바꿈 라벨은 내용 폭 + 1.5px에서 잰 높이를 본다), `Strings.Missing`에 키가 남아 있으면 실패. 이 branch가 손대지 않은 요소의 기존 한국어 넘침은 `OverflowExempt`에 이름과 사유를 남기고 제외한다.
- 해상도: 일시정지와 설정창을 1280×720, 1600×900, 1280×960, 1920×820에서 캡처해 화면 안에 들어오는지(`CheckScreenBounds`) 확인한다.
- 모든 저장은 실행마다 새로 만든 테스트 폴더를 쓴다.

### 눈으로 확인하는 검사와 기록

- 한국어·영어로 모든 화면을 캡처해 직접 확인한다. 특히 영어 버튼과 HUD를 본다.
- Release 빌드를 만들고 실행해 타이틀과 설정창을 확인한다.
- 기록: `docs/localization-settings-verification.md`와 `docs/screenshots/`의 대표 캡처.

## 범위 밖

- 한국어·영어 이외 언어, 게임패드 지원, 키 재설정, 그래픽 품질 설정, 자막·접근성 설정.
- 이미 떠 있는 알림 문구의 즉시 번역.
- Inspector와 개발용 로그의 번역.

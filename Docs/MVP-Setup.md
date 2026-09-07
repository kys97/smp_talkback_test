# 남녀칠세 부동석 MVP 실행 안내

## 바로 실행

1. Unity Hub에서 이 프로젝트를 **Unity 6000.4.1f1**로 엽니다.
2. 스크립트와 폰트 Import가 끝날 때까지 기다립니다.
3. Project 창에서 `Assets/Scenes/SampleScene.unity`를 더블클릭합니다. 이미 씬이 열려 있었다면 디스크 변경 내용을 Reload하거나 씬을 다시 엽니다.
4. Hierarchy의 `NamnyeoChilseGame`을 선택합니다. `Game Bootstrap` 컴포넌트의 **Korean Font**가 `NanumGothic-Regular`로 연결되어 있습니다.
5. Game 뷰를 9:16 또는 1080×1920으로 설정하고 **Play**를 누릅니다. 메인화면의 **게임 시작**을 누르면 첫 이름과 점수 0, 남은 시간 60초가 표시됩니다. 메인화면 구조는 `Docs/Main-Menu.md`를 참고하세요.
6. 문제의 이름 음성을 모두 들으면 왼쪽 **남자**, 오른쪽 **여자** 버튼을 순서대로 누릅니다. 문제 전체 정답은 콤보 +1 후 기본 100점 × 해당 배율을 획득합니다. 중간 오답은 점수를 유지하고 콤보만 0으로 만듭니다. 콤보 5/10/15부터 다음 문제 인원은 2/3/4명입니다. 자세한 설정은 `Docs/Sequence-Playback.md`를 참고하세요.
7. 60초가 지나면 최종 점수, **다시 시작**, **메인으로**가 표시됩니다. 다시 시작하면 새 이름·0점·60초로 재개되고, 메인으로를 누르면 메인화면으로 복귀합니다.

추가 Inspector 연결이나 Button On Click 설정은 **필요 없습니다**. GameBootstrap이 Play 시작 시 GameManager, Canvas, GameUI, 버튼과 이벤트, EventSystem/InputSystemUIInputModule을 만들고 연결합니다. Play 전에는 Canvas가 보이지 않는 것이 정상입니다. 런타임에 생성된 UI 변경은 Play 종료 시 사라지므로 배치 수정은 `GameBootstrap.cs`에서 합니다.

UGS 익명 로그인은 메인 표시와 함께 시작됩니다. 닉네임 입력/서버 저장/재실행 복원 설정은 `Docs/UGS-Authentication-Nickname.md`를 참고하세요. 인증 오류가 나도 게임 시작은 가능합니다.

## 다른 씬에 배치할 때

1. 빈 GameObject를 하나 만들고 이름을 `NamnyeoChilseGame`으로 지정합니다.
2. **GameBootstrap**만 추가합니다. GameManager나 GameUI는 별도로 추가하지 않습니다.
3. Korean Font에 `Assets/Fonts/NanumGothic-Regular.ttf`를 드래그합니다.
4. 씬마다 GameBootstrap은 하나만 둡니다. 기존 EventSystem이 있다면 새 Input System용 **InputSystemUIInputModule**이 있는지 확인합니다.
5. 씬을 저장하고 File → Build Profiles → Scene List에 해당 씬을 넣습니다. 현재 SampleScene은 이미 활성화되어 있습니다.

## 규칙과 구조

- `NameData.cs`: 이름, Gender, 선택적인 AudioClip 참조.
- `ResourceNameLoader.cs`: Resources/Names에서 이름·성별·AudioClip을 자동 생성합니다. Inspector 등록은 없습니다.
- `NameManager.cs`: 자동 생성된 데이터의 랜덤 선택. 양 성별이 있으면 성별별 50:50입니다. 음성 추가 방법은 `Docs/Name-Audio-Setup.md`를 참고하세요.
- `GameRound.cs`: Unity와 독립적인 점수·시간·정오답·입력 차단·재시작 규칙. 고정 60초입니다.
- `GameManager.cs`: Unity 생명주기와 실제 경과 시간을 GameRound에 전달하고 상태 변경을 알립니다.
- `GameUI.cs`: 버튼 이벤트, 이름/시간/점수/결과 표시. 비활성화 시 이벤트를 해제합니다.
- `GameBootstrap.cs`: 기본 UI 생성, 컴포넌트 연결, 화면 크기 및 Safe Area 대응. 게임 규칙은 포함하지 않습니다.

시간은 `Time.realtimeSinceStartupAsDouble` 기준입니다. timeScale에 영향받지 않으며 앱이 백그라운드로 이동해도 라운드 시간은 계속 경과합니다. 복귀했을 때 60초가 지났다면 종료합니다. 별도 일시정지 기능은 없습니다.

## 직접 확인할 항목

1. 이름이 수로 끝날 때 남자, 희로 끝날 때 여자를 누르면 콤보와 점수가 증가하는지 확인합니다. 기본 배율은 5/10/15콤보부터 x2/x3/x4입니다. 설정 방법은 `Docs/Combo-Scoring.md`에 있습니다.
2. 반대 버튼에서는 점수가 유지되고 다음 이름이 표시되는지 확인합니다.
3. 60초 뒤 결과 표시, 남녀 입력 차단, 최종 점수를 확인합니다.
4. 다시 시작을 여러 번 실행해 점수·시간 초기화 및 한 번 누를 때 한 번만 판정되는지 확인합니다.
5. 모바일 세로/가로 Game 뷰와 실제 기기에서 한글, 좌우 배치, 버튼 터치와 Safe Area를 확인합니다.

자동 테스트는 **Window → General → Test Runner → PlayMode → Run All**에서 실행합니다. 실제 60초를 기다리는 테스트가 포함되어 있어 1분 이상 걸립니다. 테스트는 SampleScene을 로드하므로 먼저 작업 중인 씬을 저장하세요.

Resources 이름 음성 재생과 재생 중 입력 잠금이 구현되어 있습니다. `Docs/Name-Playback.md`를 참고하세요. Android TalkBack 버튼 연결과 실기기 확인 절차는 `Docs/Android-TalkBack.md`에 있습니다. 앱 TTS는 추가하지 않았습니다.

## 파일 목록

수정: `Assets/Scenes/SampleScene.unity` (기존 카메라와 조명을 유지하고 시작 오브젝트만 추가).

신규:
- `Assets/Scripts/{NameManager,GameRound,GameManager,GameUI,GameBootstrap}.cs`
- `Assets/Scripts/NamnyeoChilse.Runtime.asmdef`
- `Assets/Tests/PlayMode/GameFlowTests.cs`
- `Assets/Tests/PlayMode/NamnyeoChilse.PlayModeTests.asmdef`
- `Assets/Fonts/NanumGothic-Regular.ttf`, `Assets/Fonts/OFL.txt` (Google Fonts 저장소의 나눔고딕, SIL OFL)
- 위 Assets 파일 및 폴더의 `.meta`
- `Docs/AI/UnityProjectContext.md`, `Docs/MVP-Setup.md`, `Docs/MVP-Validation.md`
- `Validation/.gitignore` (검증용 복사본 및 로컬 결과 제외)

Packages 및 ProjectSettings는 변경하지 않았습니다. `Validation/MvpProject`는 열린 원본 에디터와 충돌을 피하기 위한 테스트용 복사본이며 개발용 프로젝트가 아닙니다.

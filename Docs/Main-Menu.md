# 메인화면과 화면 전환

## Unity 실행 설정

1. Unity 6000.4.1f1에서 `Assets/Scenes/SampleScene.unity`를 엽니다.
2. `NamnyeoChilseGame`의 Game Bootstrap에 Korean Font가 연결되어 있는지 확인합니다. 기존 연결 그대로이며 추가 Inspector 연결은 없습니다.
3. Game 뷰를 1080×1920 또는 9:16으로 맞추고 Play합니다.
4. 제목, `현재 닉네임: 미설정`, `게임 시작`, `랭킹`, `닉네임 설정`이 표시됩니다. 메인에서 타이머와 이름 음성은 시작하지 않습니다.
5. 게임 시작을 누르면 기존 60초 게임이 시작됩니다. 종료 후 다시 시작 또는 메인으로를 선택할 수 있습니다.
6. 랭킹은 안내 문구가 있는 임시 화면입니다. 닉네임 설정은 UGS 익명 인증과 Player Names 서버 저장에 연결되어 있습니다. 입력/확인/취소 및 설정은 `Docs/UGS-Authentication-Nickname.md`를 참고하세요.

## 오브젝트와 책임

UI는 기존처럼 GameBootstrap이 런타임에 생성합니다. Edit 모드에서 화면을 직접 연결하거나 별도 씬을 만들 필요가 없습니다.

```
NamnyeoChilseGame (GameBootstrap, GameManager, NameAudioPlayer, AudioSource)
  GameCanvas (Canvas, AccessibleButtonGroup, MainMenuNavigation)
    Background
      SafeArea
        MainScreen
        GameplayScreen (GameUI)
          ResultPanel (다시 시작, 메인으로)
        RankingScreen
        NicknameScreen
```

MainMenuNavigation은 같은 씬 안에서 한 화면만 SetActive(true)로 유지합니다. 기존 GameManager는 메인에서 비활성 상태이고, 게임 시작 시 활성화한 다음 StartGame을 호출합니다. 결과에서 메인으로 돌아오면 다시 비활성화합니다. GameManager, GameRound, 점수·콤보·제한시간·오디오 코드는 수정하지 않았습니다.

GameUI는 GameplayScreen으로 이동해 숨김 시 기존 OnDisable로 클릭/상태 구독을 해제하고, 재진입 시 OnEnable로 다시 연결합니다. 재진입할 때 이전 정오답 문구를 비웁니다. 화면 전체 UI와 버튼 이벤트는 한 번만 생성합니다.

## TalkBack

Canvas의 기존 AccessibleButtonGroup 하나를 재사용합니다. 모든 버튼의 자식 uGUI Text가 접근성 이름이 됩니다. 버튼별 읽기 이름 등록은 없습니다.

- 메인: 게임 시작 / 랭킹 / 닉네임 설정
- 게임: 남자 / 여자 (오디오 재생 중 비활성)
- 결과: 다시 시작 / 메인으로
- 랭킹 임시 화면: 메인으로
- 닉네임: 닉네임 입력 / 확인 / 취소 (Android에서는 기본 텍스트 편집 대화상자 사용)

이전 화면 GameObject를 끄고 접근성 범위를 새 화면으로 설정한 뒤 RefreshButtons를 호출합니다. 기존 EventSystem 선택도 비웁니다. 이전 화면 버튼은 접근성 노드가 비활성화되어 탐색·호출 대상에서 제외됩니다. 결과 모달 범위는 기존 GameUI가 관리합니다.

Android 8.0 이상 기기에서 TalkBack을 켜고 메인의 세 버튼을 탐색하여 문구가 읽히는지 확인하세요. 게임 시작 더블탭 → 오디오 종료 후 남녀 입력 → 60초 종료 → 메인으로 더블탭 순서로 확인합니다. 임시 화면 진입 시 메인 버튼이 탐색되지 않는지도 확인하세요. 실기기 TalkBack 검증은 연결된 기기가 없어 별도로 필요합니다.

## 파일

- 신규: `Assets/Scripts/MainMenuNavigation.cs`, `Assets/Tests/PlayMode/MainMenuTests.cs`와 각 `.meta`
- 수정: `Assets/Scripts/GameBootstrap.cs`, `Assets/Scripts/GameUI.cs`
- 기존 테스트 시작 동작 수정: `GameFlowTests.cs`, `ComboScoringTests.cs`, `AccessibilityButtonTests.cs` (SampleScene 로드 후 게임 시작 클릭)
- 문서: 이 파일, `MVP-Setup.md`, `MVP-Validation.md`, `Android-TalkBack.md`, `AI/UnityProjectContext.md`

씬, 패키지, 원본 ProjectSettings는 수정하지 않았습니다.

## 자동 검증

2026-09-06 Unity 6000.4.1f1의 격리 복사본에서 PlayMode **41/41 통과, 실패 0, 103.802초**. 결과: `Validation/main-menu-results.xml`, 로그: `Validation/main-menu-tests.log`.

신규 테스트는 메인에서 게임/오디오가 시작하지 않는지, 각 화면의 접근성 버튼 목록과 숨긴 버튼 호출 거부, 랭킹/닉네임 왕복, 게임 시작→판정→종료→메인→재시작 2회, 점수 초기화와 중복 구독 방지를 확인합니다. 기존 실제 60초 종료 테스트도 통과했습니다. 실제 Android TalkBack 음성·더블탭과 기기 화면의 시각 검사는 별도 확인이 필요합니다.

2026-09-07: 접근성 그룹을 생성하기 전에 비메인 화면을 숨기도록 초기화 순서를 보완한 뒤, MainMenuTests를 다시 실행해 **1/1 통과** (2.665초)를 확인했습니다. `Validation/main-menu-final-results.xml`.

최종 Android IL2CPP/ARM64 Development 빌드도 **Succeeded / errors=0 / 종료 코드 0**입니다. 로그: `Validation/main-menu-final-build.log`. APK: `Validation/MvpProject/Builds/TalkBack-MVP.apk` (47,980,191 bytes). 기존 이름의 APK를 메인화면 포함 버전으로 갱신했습니다. SHA-256: `AF60ECCB13CA63AE73EE6D832E536899ABED84DDCD4C07FC47B745A7C1520F1E`.

위 크기/해시는 메인화면 단계의 기록입니다. 같은 APK 경로는 이후 UGS 인증/닉네임 버전으로 갱신되었으며 최신 빌드 정보는 `Docs/UGS-Authentication-Nickname.md`에 있습니다.

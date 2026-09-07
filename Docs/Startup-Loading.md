# 앱 시작 인증 로딩 화면

## 활성 상태와 흐름

- GameBootstrap.Awake에서 MainScreen을 생성한 직후 SetActive(false)합니다. 자식 버튼과 접근성 그룹을 연결하기 전이므로 메인이 한 프레임 보이거나 초기에 TalkBack 대상으로 등록되는 일이 없습니다.
- 시작 시 활성 화면은 LoadingScreen 하나입니다. GameplayScreen, RankingScreen, NicknameScreen도 비활성입니다. 접근성 그룹의 modalRoot도 LoadingScreen으로 지정합니다.
- StartupLoadingUI.Start가 기존 PlayerAccountService.InitializeAsync를 한 번 호출합니다. PlayerAccountHost는 서비스를 생성하고, 초기 호출/화면 전환은 StartupLoadingUI가 담당합니다. UGS 초기화·익명 계정 캐시 복원·닉네임 조회 코드는 기존 것을 재사용합니다.
- 인증 완료 후 닉네임 조회 중에도 LoadingScreen을 유지합니다. 단계에 따라 로그인 중 / 로그인 확인 중을 표시하며, 같은 안내 문구가 유지되면 announcement를 반복하지 않습니다. 초기 로그인 중 문구는 최초 접근성 포커스로 읽히므로 중복 announcement를 보내지 않습니다.
- InitializeAsync 성공과 유효한 Player ID를 확인한 뒤에만 MainMenuNavigation.CompleteStartup을 호출합니다. 닉네임이 아직 없는 신규 사용자는 정상 로딩 성공이며, 서버 조회 오류는 성공으로 처리하지 않습니다.
- 성공 시 LoadingScreen 비활성 → MainScreen 활성 → Canvas/접근성 갱신 → EventSystem의 게임 시작 선택 및 Android SendScreenChanged(게임 시작 노드)로 포커스 이동 요청 순서입니다. 이전 로딩 노드는 inactive가 되어 탐색할 수 없습니다.
- 실패 시 MainScreen은 계속 비활성이고 LoadingScreen에 오류와 LoginRetryButton을 표시합니다. 재시도는 같은 계정 서비스를 사용하며 요청 중 버튼을 숨기고 중복 호출을 막습니다. 사용자 정보 조회만 실패했다면 이미 인증된 사용자를 재사용합니다.
- 인증 전에는 ShowMain/StartGame/ShowRanking/ShowNickname을 직접 호출해도 메인 UI로 진입하지 않습니다. 이후 게임 결과에서 메인으로 돌아오는 동작은 기존과 같습니다.

## Unity 설정

추가 Inspector 연결은 없습니다. Bootstrap이 LoadingScreen, LoadingStatus, LoginRetryButton과 StartupLoadingUI를 자동 생성합니다. 원래 SampleScene을 열고 Play하면 됩니다. 오프라인 실행은 메인 진입을 허용하지 않습니다. 인터넷 복구 후 재시도하세요.

## 변경 파일

- 신규: Assets/Scripts/StartupLoadingUI.cs 및 .meta
- 수정: GameBootstrap.cs, MainMenuNavigation.cs, PlayerAccountHost.cs, PlayerAccountService.cs
- 테스트: 신규 StartupLoadingTests.cs 및 .meta, 수정 PlayerAccountTests.cs의 테스트용 닉네임 대기 제어
- 문서: 이 문서 및 AI/UnityProjectContext.md

## 기기 확인

1. 최신 APK 설치 후 TalkBack을 켜고 실행합니다.
2. 로그인 동안 게임 시작/랭킹/닉네임 설정이 보이거나 탐색되지 않는지 확인합니다.
3. 사용자 정보 조회까지 끝난 후 게임 시작 버튼으로 포커스가 이동하고 메인 버튼만 탐색되는지 확인합니다.
4. 인터넷을 끄고 실행하여 오류/재시도만 표시되는지 확인합니다. 다시 연결하고 재시도하여 메인 진입을 확인합니다.
5. 기존 닉네임, 랭킹, 게임 종료/메인 복귀도 확인합니다. 랭킹은 기존 Dashboard 리더보드 설정이 필요합니다.

Editor 테스트는 접근성 노드 상태와 EventSystem 선택을 검증하며 실제 TalkBack 음성/포커스 결과는 Android 기기에서 확인해야 합니다.

## 테스트 APK

- 파일: Builds/NamnyeoChilse-Startup-Ranking-20260907.apk (78,110,235 bytes)
- Android IL2CPP ARM64 Development 빌드 성공, errors=0. 로그: Validation/startup-apk-build.log
- SHA256: 80DEDCBCE5C808DCC09468D5DFF3F0C5648DCD5D752D2DDAC20A5DE1AC743246
- 패키지: com.DefaultCompany.talkback_test, version 1.0 (code 1), min SDK 25 / target SDK 36. TalkBack 접근성은 Android 8 이상에서 확인합니다.
- 실제 Android 기기의 TalkBack 음성 및 포커스 동작은 아직 검증하지 않았습니다.

기존 AccessibilityButtonTests, ComboScoringTests, GameFlowTests도 인증 완료 후 메인 버튼에 접근하도록 수정했습니다. 로딩 화면은 신규 시작 단계이므로 게임 테스트에서 이 단계를 건너뛰지 않습니다.

검증: Unity 6000.4.1f1 전체 PlayMode 62/62 통과, 실패 0, 88.972초. 결과 Validation/startup-final-results.xml, 로그 startup-final-tests.log. 인증/이름조회 각각 대기 중 메인 비활성, 로딩만 접근성 노드 활성, 직접 메인 이동 차단, 성공 후 게임시작 선택, 이름 조회 실패/재시도 계정 재사용과 기존 게임/랭킹 회귀를 검증했습니다.

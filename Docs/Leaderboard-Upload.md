# UGS 최고점수 업로드

## 구현

Unity 6000.4.1f1, 기존 Authentication 3.7.4 / Core 1.18.0을 유지하고 공식 `com.unity.services.leaderboards` 2.3.3을 추가했습니다. 이 패키지의 최소 Unity 버전은 2021.3이며 기존 인증/Core 버전이 요구 의존성 이상입니다.

- `GameManager.Completed(gameNumber, score)`는 시작한 게임이 제한시간으로 종료될 때 한 번 발생합니다. UI 갱신/버튼 연타로 중복 발생하지 않으며, 시작 전·중간 비활성화·파괴·재시작으로 버린 판은 제출하지 않습니다.
- `LeaderboardScoreHost`는 GameBootstrap이 자동으로 생성하고 기존 PlayerAccountService를 연결합니다. Inspector 추가 연결은 없습니다.
- `LeaderboardScoreService`는 요청 번호를 await 전에 소비하여 중복을 막고, 인증 진행 중이면 완료를 기다립니다. 로그인되지 않았다면 기존 익명 인증으로 재시도하며 인증을 얻지 못하면 업로드하지 않습니다. 이름 요청/인증을 별도로 중복 구현하지 않았습니다.
- `UgsLeaderboardScoreBackend`가 공식 `AddPlayerScoreAsync`를 호출합니다. 현재 인증된 Player ID가 사용되며 Player Names와 같은 사용자입니다. 별도 nickname metadata 복사나 로컬 가짜 닉네임을 만들지 않습니다. 서버 항목의 PlayerName을 나중에 랭킹 UI에서 사용할 수 있습니다. 닉네임이 없는 사용자도 점수 업로드 자체는 막지 않습니다.
- 비동기 업로드는 게임 UI/오디오를 기다리게 하지 않습니다. 성공·실패는 Console에 남습니다. 네트워크 실패 후 같은 판 자동 재시도나 오프라인 영구 큐는 없습니다. 다음 정상 종료 게임은 새 요청을 할 수 있습니다. 앱을 업로드 완료 전에 종료하면 저장은 보장되지 않습니다.
- TOP 10, 내 순위, 기존 랭킹 임시 화면은 변경하지 않았습니다.

## Dashboard에서 한 번 해야 하는 작업

1. Unity Cloud Dashboard에서 현재 연결 프로젝트 `talkback_test`를 선택합니다. Project ID는 `3aebe42d-021c-47a4-b77a-7b3e2b1f3aac`, 조직은 `aorie`입니다. 표시 이름보다 ID를 기준으로 확인하세요.
2. 앱이 사용하는 기본 UGS 환경인 `production`을 선택합니다. 현재 코드에는 환경 이름을 따로 지정하는 설정이 없습니다.
3. Leaderboards 서비스를 열고 리더보드를 생성합니다.
4. **ID**: `namnyeo_chilse_high_score` / 표시 이름 예: `남녀칠세 부동석 최고점수`.
5. **Sort Order**: 높은 점수 → 낮은 점수(Descending / Highest to Lowest).
6. **Update Strategy**: **Keep Best**. Keep Latest 또는 Aggregate로 만들면 요구한 최고점수 보존 동작이 달라집니다.
7. **Reset Schedule**: 없음. **Buckets/Tiers**: 사용하지 않음. 전체 사용자 단일 누적 최고점수 보드로 저장합니다.
8. 프로젝트에 Access Control 정책이 있다면 인증된 플레이어의 자기 점수 제출을 허용합니다. 클라이언트에 관리자 키/비밀키를 넣을 필요는 없습니다.

이 ID는 `Assets/Scripts/LeaderboardSettings.cs` 한 곳에서 관리합니다. 다른 ID를 사용하면 이 값과 Dashboard ID를 일치시켜야 합니다. 이 작업에서 Dashboard 리소스를 자동 생성하지 않았습니다. 리소스 생성 전 요청은 실패하며 게임은 계속 실행됩니다.

## 최고점수만 유지하는 원리

매 정상 종료 때 이번 점수를 전송하되 **서버 Keep Best 정책이 같은 Player ID의 항목 하나를 갱신**합니다. 8000 → 6000 → 12000 제출 시 서버 점수는 8000 → 8000 → 12000입니다. 클라이언트가 먼저 읽어서 비교 후 덮어쓰는 방식이 아니므로 요청 완료 순서가 바뀌어도 낮은 점수로 내려가지 않습니다. PlayerPrefs를 최고점수의 원본으로 사용하지 않습니다.

## 실제 서버 확인

1. 위 보드를 만든 뒤 Editor Play → 닉네임 설정/저장 → 게임 시작을 실행합니다.
2. 60초를 끝까지 플레이합니다. 종료 시 Console의 `Leaderboard 점수 등록 완료: 이번 ..., 서버 최고 ...`를 확인합니다. 실패 로그는 업로드 성공이 아닙니다.
3. Dashboard에서 같은 환경/ID의 리더보드를 열고 Entries를 새로고침하여 Player ID, Player Name, Score를 확인합니다. 여러 번 해도 해당 사용자 항목은 하나여야 합니다.
4. 같은 사용자로 더 낮은 점수의 게임을 끝내 기존 최고점수가 유지되는지 확인합니다. 더 높은 점수를 기록하면 갱신되어야 합니다.
5. 게임 도중 Play를 중지하면 새 기록이 제출되지 않아야 합니다. 인터넷을 끊고 정상 종료해도 게임 결과 화면은 동작하고 실패 로그만 나와야 합니다.
6. 휴대폰 테스트는 **이번 패키지/코드가 포함되도록 새 APK를 빌드**해야 합니다. 이전 제공 APK에는 이 기능이 없습니다. Editor와 휴대폰의 익명 계정은 서로 다를 수 있습니다. 같은 사용자 비교 시 앱 데이터 삭제/재설치를 하지 마세요.

## 파일

- 신규: `Assets/Scripts/LeaderboardSettings.cs`, `LeaderboardScoreService.cs`, `UgsLeaderboardScoreBackend.cs`, `LeaderboardScoreHost.cs`와 각 `.meta`
- 수정: `Assets/Scripts/GameManager.cs`, `GameBootstrap.cs`, `NamnyeoChilse.Runtime.asmdef`
- 패키지: `Packages/manifest.json`, `Packages/packages-lock.json`
- 테스트: 신규 `Assets/Tests/PlayMode/LeaderboardUploadTests.cs`와 `.meta`, 수정 `PlayerAccountTests.cs`, `GameFlowTests.cs`
- 문서: 이 문서와 `Docs/AI/UnityProjectContext.md`

## 근거

- [공식 점수 제출 API](https://docs.unity.com/en-us/leaderboards/tutorials/unity-sdk/add-new-score)
- [Dashboard 설정](https://docs.unity.com/en-us/leaderboards/configuration/unity-dashboard)
- [점수 갱신 정책](https://docs.unity.com/en-us/leaderboards/concepts/update-strategies)
- [Authentication과 Leaderboards의 Player Names 연동](https://docs.unity.com/en-us/leaderboards)

## 검증 범위

자동 테스트의 최고점수 보존 검사는 서버 Keep Best를 모사한 백엔드로 수행합니다. 이것을 실제 서버 저장 검증으로 간주하지 않습니다. 실제 서버 등록/최고점수 유지의 최종 확인은 Dashboard 리소스를 만든 뒤 위 절차로 수행해야 합니다.

Unity 6000.4.1f1 전체 PlayMode 테스트 최종 58/58 통과, 실패 0, 88.242초. 결과: Validation/leaderboard-final-results.xml, 로그: Validation/leaderboard-final-tests.log. 인증 대기, 중복 요청, 네트워크 실패, 정상 종료/중간 종료/재시작, 기존 60초 게임과 오디오/UI/닉네임 회귀를 검사했습니다. 원본 런타임/패키지 파일과 검증 복사본의 해시 일치를 확인했습니다. Dashboard 생성과 실서버 쓰기/조회, 이 변경 이후 Android 빌드는 실행하지 않았습니다.

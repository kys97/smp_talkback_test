# 온라인 랭킹 화면

## 사용

메인화면 → 랭킹 → TOP 10 / 내 순위 확인 → 새로고침 또는 뒤로가기.

모바일 세로 화면의 SafeArea 안에 제목, 상태 안내, 10개 행, 내 순위, 큰 새로고침/뒤로가기 버튼을 배치합니다. 10개 행이 한 화면에 들어가므로 ScrollView가 필요하지 않습니다. 긴 닉네임은 화면에서 14자 이후 줄임표로 표시하며 TalkBack 문장에는 서버 이름 전체를 유지합니다. 서버 이름은 rich text로 해석하지 않습니다.

추가 Inspector 연결은 없습니다. GameBootstrap이 기존 RankingScreen을 만들면서 UI와 접근성 컴포넌트, 기존 인증 서비스를 자동 연결합니다. GameManager, 점수 업로드 로직, 이름 음성/콤보는 변경하지 않았습니다. 추가 패키지도 없습니다. 기존 Leaderboards 2.3.3, Authentication 3.7.4, Core 1.18.0을 사용합니다.

## 조회

- `RankingService`: 기존 PlayerAccountService의 인증이 진행 중이면 기다리고, 로그인되지 않았으면 기존 익명 로그인 경로로 초기화합니다. 인증 실패 시 Leaderboards 요청 없이 안내합니다.
- `UgsRankingBackend.GetTopAsync`: `GetScoresAsync(LeaderboardSettings.LeaderboardId, new GetScoresOptions { Offset = 0, Limit = 10 })`.
- `UgsRankingBackend.GetMineAsync`: `GetPlayerScoreAsync(LeaderboardSettings.LeaderboardId)`로 TOP 10 밖에 있는 내 기록도 조회합니다. 특정 Player ID를 임의 지정하지 않고 인증된 사용자의 기록을 가져옵니다.
- `LeaderboardEntry.Rank`는 0부터 시작하므로 화면/음성에는 +1하여 표시합니다. `PlayerName`을 닉네임으로 사용하며 비어 있으면 `이름 없음`입니다.
- 내 기록 요청의 `EntryNotFound`만 정상적인 기록 없음으로 처리합니다. 리더보드 자체가 없거나 권한/네트워크 오류가 발생하면 조회 실패입니다.
- TOP 10과 내 기록은 병렬 조회하며 한쪽만 실패해도 성공한 쪽은 표시합니다. 새 조회 시 이전 행을 숨겨 오래된 데이터를 최신 데이터로 오인하지 않게 합니다.
- 서비스는 진행 중인 요청 하나를 공유합니다. 화면 진입/새로고침 외에 Update/LateUpdate에서 서버 요청하지 않습니다. 요청 중 새로고침 버튼과 중복 메서드 호출을 차단합니다.
- 화면을 닫으면 요청 세대 번호를 바꾸고, 이전 응답이 돌아와도 비활성/파괴된 화면을 갱신하지 않습니다. SDK 요청 자체는 취소하지 않습니다. 닫았다 재진입할 때 진행 중인 요청은 공유하고, 완료된 후 재진입하면 새 조회를 합니다.

ID는 기존 `namnyeo_chilse_high_score`이며 `LeaderboardSettings.cs`를 그대로 참조합니다. Dashboard의 동일 환경/ID에 높은 점수 우선 + Keep Best 설정이 필요합니다. 설정 방법은 [Leaderboard-Upload.md](Leaderboard-Upload.md)를 참고하세요.

## TalkBack

`AccessibleReadOnlyText`는 기존 AccessibleButtonGroup의 자동 탐색/화면 범위/좌표 계산을 재사용하는 읽기 전용 요소입니다. Native 노드 역할은 StaticText이며 버튼 클릭 동작은 없습니다.

- 랭킹 행 한 개 = 노드 한 개: `1위, 영희#1234, 15200점`.
- 내 순위: `내 순위: 27위 / 6300점` 또는 기록 없음/조회 실패 안내.
- 상태 안내: 로딩, 빈 목록, 조회 실패도 탐색 가능하며 로딩과 결과 도착 시 Android TalkBack announcement로 알립니다.
- 새로고침/뒤로가기 버튼은 기존 버튼 텍스트 자동 읽기와 더블탭 연결을 그대로 사용합니다.
- 닫힌 랭킹 화면의 모든 노드는 기존 modalRoot/활성 상태 검사로 탐색 대상에서 제외됩니다. 행별 수동 접근성 문구 등록은 없습니다.

## 테스트 방법

1. Dashboard에 기존 Leaderboard를 생성/설정하고 인터넷에 연결합니다. 새 서버 설정이나 별도 ID는 필요하지 않습니다.
2. Editor에서 SampleScene → Play → 닉네임 설정 → 게임 시작 → 60초 종료 → 업로드 성공 로그를 확인합니다.
3. 메인으로 → 랭킹을 열고 서버 TOP 10과 내 순위/점수를 확인합니다. 업로드 진행 중 바로 입장하면 기록이 아직 반영되지 않을 수 있으므로 업로드 완료 후 새로고침합니다.
4. 새로고침 연타, 로딩 중 뒤로가기, 빠른 재진입을 확인합니다. 빈 보드와 아직 점수를 등록하지 않은 사용자도 오류 없이 안내되어야 합니다.
5. 인터넷을 끊고 새로고침하여 오류 안내와 뒤로가기 동작을 확인합니다. 인터넷 복원 후 새로고침합니다.
6. Android에는 이번 변경으로 새 APK를 빌드해 설치합니다. 이전 APK에는 랭킹 화면이 없습니다. Android 8.0 이상 ARM64 기기에서 TalkBack을 켭니다.
7. 랭킹 버튼 더블탭 → 상태/각 행/내 순위/새로고침/뒤로가기 탐색. 한 행은 순위·닉네임·점수 전체를 한 번에 읽어야 합니다. 뒤로가기 후 랭킹 행이 포커스에 남지 않아야 합니다.

Editor에서는 네이티브 TalkBack 음성을 재생하지 않습니다. 실제 서버 보드가 없는 환경의 자동 테스트는 인증/랭킹 백엔드를 대체합니다. 실서버 조회와 휴대폰 음성 탐색은 별도 확인 대상입니다.

## 파일

- 신규: `Assets/Scripts/RankingService.cs`, `UgsRankingBackend.cs`, `RankingUI.cs`, `AccessibleReadOnlyText.cs` 및 `.meta`
- 수정: `Assets/Scripts/GameBootstrap.cs`, `AccessibleButtonGroup.cs`, `AccessibleButtonBinding.cs`
- 테스트: 신규 `Assets/Tests/PlayMode/RankingTests.cs` 및 `.meta`, 수정 `PlayerAccountTests.cs`, `MainMenuTests.cs`
- 문서: 이 문서와 `Docs/AI/UnityProjectContext.md`

## 공식 API

- [TOP 점수 조회](https://docs.unity.com/en-us/leaderboards/tutorials/unity-sdk/get-score)
- [내 점수 조회](https://docs.unity.com/en-us/leaderboards/tutorials/unity-sdk/get-player-score)
- 설치된 2.3.3 SDK의 `LeaderboardsService.cs`, `LeaderboardsModels.cs`, `LeaderboardsException.cs`에서 API/Rank/PlayerName/EntryNotFound를 확인했습니다.

## 검증 결과

Unity 6000.4.1f1의 Validation/MvpProject에서 전체 PlayMode 60/60 통과, 실패 0, 88.232초. 결과: Validation/ranking-results.xml, 로그: Validation/ranking-tests.log. 실제 씬에 생성된 TOP10 행/내순위/접근성 노드와 로딩/부분오류/빈데이터/새로고침/닫기 후 응답 무시를 검사했습니다. 원본 변경 런타임과 검증 프로젝트의 파일 해시 일치를 확인했습니다. 랭킹 API 응답은 테스트 백엔드로 대체했으므로 실서버 데이터 및 Android TalkBack 청취를 검증한 것은 아닙니다. 이번 단계에서 APK는 새로 빌드하지 않았습니다.

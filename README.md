# 남녀칠세 부동석

Unity 기반 Android 이름 듣기 게임입니다. 이름 음성을 듣고 남자/여자 버튼을 순서대로 입력합니다.

## 실행

1. Unity Hub에서 이 폴더를 프로젝트로 추가합니다.
2. Unity **6000.4.1f1**로 열고 패키지 가져오기가 완료될 때까지 기다립니다.
3. `Assets/Scenes/SampleScene.unity`를 열고 실행합니다.
4. 앱은 UGS 인증 및 닉네임 조회 완료 후 메인 화면을 표시합니다.

게임 UI와 접근성 요소는 GameBootstrap에서 생성합니다. 이름 음원은 `Assets/Resources/Names`에서 자동으로 불러옵니다. TalkBack 검증에는 실제 Android 기기가 필요합니다.

## UGS 설정

- Cloud Project ID: `3aebe42d-021c-47a4-b77a-7b3e2b1f3aac`
- Environment: `production`
- Leaderboard ID: `namnyeo_chilse_high_score`
- ID 및 Environment 중앙 설정: `Assets/Scripts/LeaderboardSettings.cs`
- 리더보드 설정: 높은 점수순, Keep Best, 버킷/티어/자동 리셋 없음

리더보드는 반드시 위 Cloud Project의 해당 환경에 생성해야 합니다. 다른 프로젝트에 생성된 리더보드는 조회할 수 없습니다. 현재 서버 재설정 후 재검증을 기다리는 상태이며, 온라인 동작 검증 완료를 의미하지 않습니다.

## 문서와 검증

- [기본 설정](Docs/MVP-Setup.md)
- [Android TalkBack](Docs/Android-TalkBack.md)
- [인증 및 닉네임](Docs/UGS-Authentication-Nickname.md)
- [랭킹 오류 조사 및 기기 검증](Docs/Leaderboard-Diagnostics.md)
- [시작 로딩 화면](Docs/Startup-Loading.md)

최근 PlayMode 테스트: 65개 통과. Unity Test Runner의 PlayMode에서 실행할 수 있습니다. 실제 기기의 온라인 요청과 TalkBack 동작은 별도로 검증해야 합니다.

`Library`, `Logs`, `UserSettings`, `Validation`, APK 및 빌드 산출물은 Git에 포함하지 않습니다. `Assets`의 `.meta` 파일은 반드시 함께 관리합니다.

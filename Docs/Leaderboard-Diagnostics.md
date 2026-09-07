# Android 랭킹 조회 실패 조사 (2026-09-07)

## 확인된 원인

기존 검증 계정(codex-validation SDK profile)으로 실제 UGS 인증 및 닉네임 조회 후 TOP 10/내 순위를 요청했습니다. 두 요청 모두 LeaderboardsException, ErrorCode 27005, 메시지 `Leaderboard config could not be found`를 반환했습니다. 원본 증거: Validation/leaderboard-probe.log. 인증 실패나 빈 목록이 아니라 서버 리더보드 설정 부재입니다.

- Organization: aorie
- Cloud Project ID: 3aebe42d-021c-47a4-b77a-7b3e2b1f3aac (ProjectSettings 및 실제 실행 로그 일치)
- Environment: production (기존 Core 1.18.0 기본값. 이제 초기화 옵션으로 명시)
- Leaderboard ID: namnyeo_chilse_high_score (업로드/TOP 10/내 순위 공통)

다른 환경/프로젝트에 생성된 리더보드의 존재 여부는 Dashboard 권한으로 확인하지 않았습니다. 현재 조합에서는 서버가 설정을 찾지 못한다는 점을 확인했습니다. Android 기기는 ADB에 연결되지 않아 기기 자체의 로그와 업로드 성공은 검증하지 못했습니다. 운영 랭킹에 합성 점수를 넣지 않도록 검증 도구는 조회만 수행합니다.

## Dashboard에서 필요한 설정

1. Unity Dashboard에서 organization aorie, 위 Cloud Project ID와 일치하는 프로젝트를 선택합니다.
2. Development > Products > Leaderboards > Overview에서 Environment를 production으로 선택합니다.
3. Add Leaderboard: ID를 정확히 namnyeo_chilse_high_score로 지정합니다. 표시 이름은 남녀칠세 부동석 최고점수 등 자유롭게 지정합니다.
4. Sort Order: Descending / Highest to Lowest, Update Strategy: Keep Best.
5. Buckets, Tiers, Scheduled Resets는 사용하지 않습니다. Finish로 생성을 완료합니다.
6. Overview에서 production 환경의 해당 ID가 실제로 표시되는지 확인합니다.

공식 안내: https://docs.unity.com/ko-kr/leaderboards/configuration/unity-dashboard

클라이언트 SDK는 리더보드 설정을 생성하지 않습니다. 기존 설치 APK도 같은 ID와 production을 사용하므로 서버 설정을 추가한 뒤 새로 플레이하여 확인할 수 있습니다. 이번에 추가한 상세 로그/화면 문구는 수정 코드를 포함해 새 Development APK를 빌드해야 반영됩니다.

## 구현 변경

- LeaderboardSettings.cs: EnvironmentName을 한 곳에서 관리.
- UgsPlayerAccountBackend.cs / NamnyeoChilse.Runtime.asmdef: 공식 Environments 초기화 옵션 명시 및 어셈블리 참조.
- UgsDiagnostics.cs (+meta): Development/Editor 전용 작업/성공·실패/ID/환경/Cloud Project/Player ID/예외 타입/오류 코드/Reason/메시지 로그. 인증 토큰을 읽거나 예외 전체/HTTP 헤더를 출력하지 않으며 메시지 내 토큰 패턴도 마스킹.
- PlayerAccountService.cs: 인증/닉네임 성공·실패 진단.
- LeaderboardScoreService.cs: 실제 업로드 응답 이후 성공 로그, 실패 원인 로그 및 서버 설정 오류 구분. 실패는 false 반환하며 성공으로 처리하지 않음. 중복 방지 유지.
- RankingService.cs / RankingUI.cs: 설정 부재와 일반 요청 실패, 빈 목록, 내 기록 없음 구분. 기존 화면 수명 및 TalkBack 읽기 경로 유지.
- UgsAccountVerification.cs: 별도 검증 계정으로 실제 조회하는 ProbeLeaderboard 배치 진입점.
- UgsDiagnosticsTests.cs (+meta), PlayMode 테스트 asmdef: 마스킹과 설정 부재 분류 검증.

기존 게임 로직, 닉네임 저장 방식, 업로드 API, 이름 오디오, TalkBack 컴포넌트는 변경하지 않았습니다. 패키지 버전 변경 없음.

## 실제 기기 검증

1. 서버 설정을 먼저 완료합니다. 상세 로그 확인 시 수정 코드로 Development Build를 설치합니다.
2. USB 디버깅 연결 후 `adb devices`에 기기가 device 상태인지 확인합니다.
3. `adb logcat -s Unity`로 로그를 봅니다. 인증 토큰을 수집하거나 출력할 필요가 없습니다.
4. 게임을 60초 정상 종료합니다. `[UGS] operation=점수 업로드 result=SUCCESS`와 submitted / serverBest를 확인합니다. FAILED면 동일 줄의 code, reason, message를 확인합니다.
5. 랭킹 진입 후 `[UGS] operation=TOP 10 조회 result=SUCCESS count=...`, `operation=내 순위 조회 result=SUCCESS rank=... score=...`를 확인합니다.
6. Dashboard 해당 리더보드 Entries에서 로그의 Player ID와 점수를 대조합니다. 더 낮은 점수로 재플레이해 서버 최고점이 유지되는지 확인합니다.
7. TalkBack으로 각 행, 내 순위, 새로고침, 뒤로가기를 탐색합니다. 인터넷 차단 시 오류 안내, 복구 후 새로고침을 확인합니다.

설정이 만들어지기 전에는 조회와 업로드 성공 완료 기준을 충족할 수 없습니다. 코드의 오류 안내/로그 추가 자체를 온라인 기능 복구 완료로 간주하지 않습니다.

검증: 최종 PlayMode 64/64 통과, 실패 0, 89.271초. 결과 Validation/ugs-final-results.xml. 로그 마스킹/설정 부재 분류 및 기존 게임/접근성/인증/업로드/랭킹 테스트 포함. 첫 실행의 정상 로그 기대값 누락은 GameFlowTests에서 정상 인증/업로드 로그를 명시적으로 검증하도록 수정했습니다.

Development APK 빌드 성공(errors=0): Builds/NamnyeoChilse-UGS-Diagnostics-20260907.apk, 78,116,801 bytes. SHA256 B8C928870EFBBF70ED8004BD0272AA2FEC89B2DF3211803B628664D2886BAC35. 빌드 로그 Validation/ugs-diagnostics-apk.log. 이 APK는 상세 진단 변경을 포함하지만 서버 설정을 자동 생성하지 않습니다.

## 클라이언트 설정 재확인

ID/환경은 LeaderboardSettings.cs의 const이며 Inspector, ScriptableObject, Resources 또는 Bootstrap 인자에 의존하지 않습니다. Android 분기나 직렬화로 비어질 수 있는 필드는 없습니다. 업로드/두 조회 모두 같은 상수를 사용합니다.

기존 로그의 environment는 설정 상수를 출력했으므로 실제 적용 상태를 검증할 수 없었습니다. UgsDiagnostics.ActualEnvironment가 CoreRegistry의 IEnvironments.Current를 읽도록 수정했습니다. Core 1.18.0의 Authentication이 사용하는 환경 컴포넌트이며 토큰을 해석하지 않습니다. 이 공개 타입은 Core.Internal 어셈블리에 있어 런타임 asmdef 참조를 추가했습니다. SDK 변경 시 이 참조도 다시 검증해야 합니다.

UgsPlayerAccountBackend는 초기화 전/후와 익명 인증 완료 시 [UGS Startup]에 leaderboard, 실제 environment, configuredEnvironment, authenticated, cloudProject를 기록합니다. 기존에 다른 환경으로 초기화되어 있으면 이를 조용히 재사용하지 않고 명시적 오류로 처리합니다.

Validation/actual-environment-probe.log에서 실제 environment=production, authenticated=True를 확인했지만 TOP 10과 내 순위는 여전히 27005를 반환했습니다. 따라서 ID가 누락됐다고 결론 내릴 근거가 없습니다. 리더보드를 생성한 Dashboard 프로젝트의 Project ID와 앱 Cloud Project ID를 대조해야 합니다. Leaderboard ID와 Project ID는 서로 다른 값입니다.

추가/수정 파일: UgsDiagnostics.cs, UgsPlayerAccountBackend.cs, NamnyeoChilse.Runtime.asmdef, UgsDiagnosticsTests.cs. 기존 중앙 설정 기본값은 이미 요구값과 일치하여 변경하지 않았습니다.

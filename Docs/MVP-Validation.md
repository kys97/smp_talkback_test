# MVP 검증 결과 — 2026-09-06

## 후속: UGS 인증/닉네임 (2026-09-07)

- PlayMode 49/49 통과, 실패 0, 104.816초: `Validation/ugs-tests.xml`.
- 실제 UGS 검증용 프로필에서 익명 로그인/서버 저장 후 프로세스 재실행, 동일 ID/이름 복원 성공. `Validation/ugs-live-save.log`, `Validation/ugs-live-restore.log`.
- 검증 배치 Editor의 UnityEditor.Search 검색 인덱싱 예외는 별도 기록했습니다. 실기기 TalkBack 입력/키보드는 미검증입니다. 상세: `Docs/UGS-Authentication-Nickname.md`.
- 캐시 즉시 반영 보완 후 실제 복원 재확인: `Validation/ugs-live-final-restore.log`. 최종 Android 빌드 성공/오류0: `Validation/ugs-final-android-build.log`. APK 53,579,494 bytes, INTERNET/ARM64/네이티브 입력 클래스 포함 확인. 최초 APK의 이전 Java 호출 오류는 수정 후 최종 빌드에서 해소되었습니다.

## 후속: 메인화면

- Unity 6000.4.1f1 격리 복사본 PlayMode: 41/41 통과, 실패 0, 103.802초. `Validation/main-menu-results.xml`, `Validation/main-menu-tests.log`.
- 메인 대기 중 게임/오디오 정지, 임시 화면 왕복, 현재 화면의 접근성 요소만 활성화, 숨긴 버튼 호출 차단, 게임 종료 후 메인 복귀 및 재진입 초기화 확인. 기존 실제 60초 종료와 콤보/오디오 테스트도 통과.
- 기존 FindFirstObjectByType CS0618 경고 유지. 실기기 TalkBack과 시각 검사는 미검증.
- 구성/설정/파일 목록은 `Docs/Main-Menu.md`.
- 2026-09-07 최종 초기화 순서 보완 후 MainMenuTests 재실행: 1/1 통과 (2.665초), `Validation/main-menu-final-results.xml`.
- 최종 Android IL2CPP/ARM64 Development APK 빌드: Succeeded, errors=0, 종료 코드 0. `Validation/main-menu-final-build.log`, `Validation/MvpProject/Builds/TalkBack-MVP.apk` (47,980,191 bytes). 원본과 검증본의 변경 런타임 소스 해시 일치.

## 후속: Android TalkBack 버튼 연결

- 격리 프로젝트 PlayMode: 40/40 통과, 실패 0, 101.598초. `Validation/accessibility-results.xml` 및 `Validation/accessibility-tests.log`.
- 신규 접근성 테스트: 자동 label/role/frame, 입력 잠금 및 기존 판정 경로, 결과 모달 탐색 제한/재시작, 일반 버튼 동적 등록/삭제/텍스트 변경, 합성 탭 Raycast 차단/복구.
- 원본과 검증 복사본의 접근성 런타임 코드 및 Bootstrap/GameUI 해시 일치 확인.
- 실제 TalkBack 음성/더블탭은 연결된 Android 기기가 없어 미검증. 자동 테스트는 네이티브 TalkBack을 실행하지 않습니다.
- 적용 및 실기기 절차: `Docs/Android-TalkBack.md`.
- Android IL2CPP/ARM64 Development 빌드: Succeeded, errors=0, 프로세스 종료 코드 0. `Validation/android-build.log`, APK `Validation/MvpProject/Builds/TalkBack-MVP.apk` (47,559,599 bytes). aapt 확인: 최소 SDK 25, 대상 SDK 36. Unity가 Android 전환 중 검증 복사본의 최소 SDK를 23에서 25로 올렸으며 원본 설정은 수정하지 않았습니다. 기존 CS0618 경고 유지.

## 후속: 콤보별 이름 사이 재생 간격

Unity 6000.4.1f1 Windows Editor 검증 복사본에서 **38 passed / 0 failed**, 98.184초, 종료 코드 0. C# 컴파일 오류 없음.

- 콤보 0/4/5/9/10/14/15/30의 간격 기본값, 문제 시작 시 간격 저장, 오답/재시작 시 1초 복귀, 사용자 설정 직렬화 확인.
- 실제 AudioSource 첫 클립 종료 후 0.35초 대기를 측정하고, 그 사이 입력 차단과 pitch=1 유지 확인.
- 마지막 클립 뒤 추가 대기 없이 입력 허용 확인.
- 이름 사이 대기 중 재시작 시 기존 대기 취소, 대기 중 제한시간 만료 시 다음 클립 재생/입력 없음 확인.
- 기존 콤보/점수/1~4명 순서 재생/부분 입력/오디오 완료/60초 게임 테스트 모두 통과.

결과: `Validation/interval-results.xml`, 로그: `Validation/interval-tests.log`. 기존 FindFirstObjectByType 사용 중단 예정 경고는 유지됩니다. 실제 스피커 청취와 모바일 실기기 검증은 수행하지 않았습니다.

## 후속: 콤보별 연속 이름 문제

Unity 6000.4.1f1 Windows Editor에서 전체 PlayMode **26 passed / 0 failed** (81.288초), 이후 다중 음성 취소 테스트를 추가한 SequenceTests **4 passed / 0 failed** (9.861초). 중복 실행을 제외하면 총 27개 테스트 항목을 검증했습니다. 두 실행 모두 종료 코드 0이며 C# 컴파일 오류가 없습니다.

- 기본 콤보 0~15의 1/2/3/4명 진행, 이름 중복 없음, 성별 배열 일치, 부분 정답 무득점, 최종 정답만 기존 점수/콤보 적용.
- 두 번째 입력 오답 즉시 실패/콤보 0/점수 유지/다음 문제 1명.
- 중복 이름 카탈로그 및 int.MaxValue 요청에도 실제 보유 이름 수로 유한 선택, Inspector 구간 직렬화, 종료/재시작 입력과 배열 초기화.
- 실제 AudioSource의 모든 클립 재생 순서 관찰, 마지막 클립까지 입력 거부, 이후 순서 입력, 중간 입력 사이 재생 없음.
- 두 번째 음성 중 재시작, 부분 입력 중 재시작, 두 번째 음성 중 시간 만료 후 대기 중인 세 번째 음성이 나오지 않음.
- 기존 60초 종료, 입력 잠금, 오디오 완료, 점수/콤보, Resources 테스트 통과.

결과/로그: `Validation/sequence-results.xml`, `sequence-tests.log`, `sequence-cancellation-results.xml`, `sequence-cancellation-tests.log`. 전체 테스트는 SampleScene의 AudioListener를 사용했습니다. 별도 필터 실행은 빈 테스트 씬에서 실행되어 AudioListener 없음 경고가 발생했으나 AudioSource 상태·취소 검증은 통과했습니다. 실제 스피커 청취·모바일 기기·새 진행 UI의 육안 검사는 미실행입니다.

## 후속: 콤보와 점수 배율

Unity 6000.4.1f1 Windows Editor 검증 복사본에서 **23 passed / 0 failed**, 73.192초, 종료 코드 0. C# 컴파일 오류 없음.

- 콤보 4/5/9/10/14/15/16의 배율·이번 획득 점수·누적 점수 경계값 통과.
- 오답 시 콤보만 0, 총점 유지, 다음 정답 x1 재개, 종료 후 입력 무시, 재시작 전체 초기화 통과.
- Inspector용 직렬화 데이터의 사용자 지정 기본 점수/정렬되지 않은 구간, 빈 목록/잘못된 항목 처리 통과.
- SampleScene에서 실제 음성 종료를 기다린 뒤 5정답 → 콤보 5/x2/점수 600/정답 +200 표시, 오답 → 콤보 0/600점 유지, 재시작 → 콤보 0/점수 0 확인.
- 기존 음성 재생 완료·입력 잠금·재시작/종료 취소·실제 60초 게임 흐름 테스트도 새 100점 기준으로 통과.

결과: `Validation/combo-results.xml`, 로그: `Validation/combo-tests.log`. 기존 FindFirstObjectByType CS0618 경고는 유지됩니다(추가 씬 UI 테스트도 동일 API를 사용). UI 텍스트 값은 자동 검증했으며 실제 화면 배치의 육안 검사·모바일 실기기 빌드는 수행하지 않았습니다.

## 후속: 이름 음성 재생과 입력 잠금

Unity 6000.4.1f1 Windows Editor, 검증 복사본에서 **12 passed / 0 failed**, 67.580초, 종료 코드 0. `-batchmode`에서 실행했으며 이번에는 `-nographics`를 사용하지 않았습니다. 실제 AudioSource.isPlaying 상태를 검사했습니다.

- `ActualPlaybackBlocksInputAndUsesEngineCompletion`: AudioSource 재생 확인, 재생 중 두 버튼 메서드 거부, pitch 0.5에서 clip.length가 지난 뒤에도 잠금 유지, 재생 종료 후 허용, 정답 후 연속 입력 1회만 판정, 오답 후 다음 재생.
- `RestartAndDeadlineCancelPlaybackAndPreventLateUnlock`: 재생 도중 재시작으로 정지/초기화, 이전 재생 종료 시점에도 새 재생 잠금 유지, 제한시간 만료 시 정지/clip 해제, 종료 후 입력/재생 없음.
- `MissingClipAndDisableRemainRecoverable`: null 클립으로 입력 진행, 같은 프레임 중복 거부, GameManager 비활성화/재활성화 후 복구.
- 기존 Resources 데이터/직렬화/접미사 테스트와 실제 60초 SampleScene 흐름 통과. 씬 테스트는 재생 종료 후 버튼을 누르도록 갱신했으며, 재생 중 UI 이벤트를 강제로 호출해도 점수가 오르지 않음을 검사합니다.

결과: `Validation/playback-results.xml`. 로그: `Validation/playback-tests.log`. C# 오류 없음; 기존 FindFirstObjectByType CS0618 경고는 유지됩니다. 실제 스피커 청취와 모바일 기기/백그라운드 복귀 검증은 수행하지 않았습니다.

## 후속: Resources 자동 등록

Unity 6000.4.1f1 검증 복사본에서 컴파일 및 PlayMode **9 passed / 0 failed**, 60.560초. 종료 코드 0.

- 실제 Resources/Names의 변환 WAV 20개를 AudioClip으로 로드했고, 이름/성별/클립 참조와 양수 재생 길이를 확인했습니다.
- 잘못된 접미사 제외 및 경고, 동일 이름/Unicode 정규화 중복 제외 및 경고를 확인했습니다.
- 빈 입력에서 빈 카탈로그 생성 후 기존 기본 이름으로 게임 진행을 확인했습니다.
- SampleScene의 선택된 이름에 AudioClip이 연결됨을 확인하고 실제 60초 종료/입력 차단/재시작 테스트를 통과했습니다.
- 새로운 C# 오류 없음. 기존 FindFirstObjectByType 사용 중단 예정 경고는 유지됩니다.

결과/로그: Validation/playmode-results.xml, Validation/unity-tests.log. 직전 수동 등록 단계 결과는 Validation/manual-names-results.xml 및 manual-names-tests.log로 보존했습니다. 현재 실제 이름 파일 20개가 존재하는지 확인하는 통합 테스트이므로 이후 음성을 줄이거나 제거하면 해당 테스트의 기대 데이터 수도 조정해야 합니다. 재생/청취/실기기 빌드는 이번 단계에서 수행하지 않았습니다.

## 후속: 이름/AudioClip 데이터 확장

동일 Unity 6000.4.1f1 검증 복사본에서 재컴파일 및 PlayMode 테스트 **6 passed / 0 failed**, 60.576초. 기존 3개 테스트와 다음 3개 데이터 테스트가 통과했습니다.

- `ConfiguredEntryPreservesClipThroughSerializationAndGameManager`: NameData 직렬화 후 이름/enum/AudioClip 참조 보존, GameManager에서 선택된 데이터와 판정 후 다음 데이터의 참조 확인.
- `MissingClipIsValidAndBlankNamesAreSkipped`: 빈 이름 제외, null AudioClip으로 게임 시작 및 정답 처리.
- `EmptyCatalogFallsBackToPlayableDefaults`: 등록 목록이 비어 있을 때 기본 이름 반환.

결과: `Validation/playmode-results.xml`, 로그: `Validation/unity-tests.log`. 이전 MVP 결과와 로그는 `Validation/mvp-original-results.xml`, `Validation/mvp-original-tests.log`로 보존했습니다. C# 오류는 없고, 기존 `FindFirstObjectByType`의 CS0618 경고 3곳은 이전 로그에서도 확인되어 이번 데이터 작업에서 변경하지 않았습니다. 실제 사용자 음원 재생이나 실기기 확인은 이번 범위가 아닙니다.

아래는 최초 MVP 검증 기록입니다.

핵심 로직 및 씬 버튼 이벤트 자동 검증 통과. 시각 검증·실기기 입력은 미실행입니다.

## 실행 환경과 결과

- Unity 6000.4.1f1, Windows Editor, batchmode/nographics.
- 열린 원본 에디터를 유지하고 `Validation/MvpProject` 복사본에서 실행.
- 원본과 검증 복사본의 신규 C#/asmdef/폰트/라이선스 및 SampleScene SHA-256 일치 확인.
- C# 컴파일 오류 없음. PlayMode 테스트 **3 passed / 0 failed / 0 skipped**, 60.594초.
- 종료 로그: `Test run completed. Exiting with code 0 (Ok).`
- 결과 XML: `Validation/playmode-results.xml`.
- 전체 로컬 로그: `Validation/unity-tests.log`.

명령:

```text
Unity.exe -batchmode -nographics -projectPath C:/Users/User/Documents/UNITY/talkback_test/Validation/MvpProject -runTests -testPlatform PlayMode -testResults C:/Users/User/Documents/UNITY/talkback_test/Validation/playmode-results.xml -logFile C:/Users/User/Documents/UNITY/talkback_test/Validation/unity-tests.log
```

## 테스트

| 테스트 | 검증 내용 | 결과 |
| --- | --- | --- |
| NameDataFollowsSuffixRuleAndIncludesBothGenders | 고정 seed로 300회 선택, 전체 10개 이름 출현 및 성별/접미사 규칙 | 통과 |
| ScoringDeadlineAndRestartRespectRoundContract | 시작 전 입력 무시, 정답 +1, 오답 점수 유지, 종료 직전 허용, 정확히 60초 시 입력 거부, 재시작 초기화 | 통과 |
| SceneButtonsCompleteRealSixtySecondRoundAndRestart | SampleScene 로드, 이름 표시 일치, 한글 폰트 글리프, 남녀 Button.onClick 이벤트, 실제 60.1초 대기, 버튼 비활성화, 강제 종료 후 이벤트 무시, 최종 점수, 다시 시작, UI 재활성화 후 중복 구독 방지 | 통과 |

## 참조/기존 상태

- 원래 게임 코드와 테스트는 없었고 Git 커밋도 없었습니다. 기존 프로젝트 전체가 untracked 상태였습니다.
- 기존 SampleScene의 카메라와 Global Light 2D 유지. GameBootstrap 오브젝트와 폰트 참조만 추가.
- Runtime asmdef는 설치된 Unity.ugui와 Unity.InputSystem을 참조하며, 테스트 asmdef는 Runtime 및 TestAssemblies를 참조합니다.
- 신규 Assets 파일·폴더의 메타데이터 존재 확인. 패키지/프로젝트 설정 변경 없음.
- 원본 Editor 로그의 기존 disposed CancellationTokenSource 메시지는 게임 코드 추가 전부터 존재했습니다.
- 검증 복사본의 초기 패키지 임포트에서 URP/Terrain 등 shader dependency/fallback not found 메시지가 발생했습니다. 렌더링 없는 환경에서의 패키지 메시지이며 이 게임의 컴파일/테스트는 통과했지만, 일반 렌더링 환경에서 같은 문제가 발생하는지는 확인하지 않았습니다.

## 미검증 범위

- 실제 마우스/터치 장치로 누르는 입력 경로: 자동 테스트는 Button.onClick을 호출했습니다.
- 화면 렌더링, 폰트 가독성, 세로/가로 배치 및 노치 Safe Area 시각 검사.
- Android/iOS 플레이어 빌드와 실기기, 앱 백그라운드/복귀 동작.

다음 확인: 원본 SampleScene을 열고 Play → 버튼 클릭 → 60초 종료 → 다시 시작을 직접 실행합니다. 단계별 안내는 `Docs/MVP-Setup.md`를 따릅니다.

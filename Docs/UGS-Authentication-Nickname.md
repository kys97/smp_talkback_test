# UGS 익명 인증과 닉네임

## 패키지와 프로젝트 설정

공식 Unity 레지스트리에서 Authentication **3.7.4**, Services Core **1.18.0**을 추가했습니다. Authentication의 최소 Unity 버전은 2022.3이며 프로젝트는 6000.4.1f1입니다. Newtonsoft JSON 3.2.2는 의존성으로 해결됩니다. manifest와 packages-lock을 함께 갱신했고 Runtime asmdef에 Core/Authentication 참조를 추가했습니다.

기존 Cloud Project ID `3aebe42d-021c-47a4-b77a-7b3e2b1f3aac`와 조직 `aorie`를 그대로 사용합니다. 서비스 계정 비밀키나 별도 클라이언트 시크릿은 앱에 넣지 않습니다.

Unity Editor에서 **Edit → Project Settings → Services**의 연결 프로젝트를 확인하세요. Unity Dashboard에서도 같은 프로젝트를 열어 **Authentication** 서비스가 이용 가능한 상태인지, Player Names 요청을 막는 Access Control 정책이 없는지 확인합니다. 이번 기능은 익명 인증이므로 Google/Apple 로그인 공급자 설정은 필요 없습니다. 프로젝트 연결/약관/권한 확인은 해당 Unity 계정의 프로젝트 소유자가 해야 합니다. 온라인 랭킹 리소스는 만들지 않습니다.

## 동작과 저장 위치

GameBootstrap이 PlayerAccountHost를 생성하고 앱 시작 시 다음 순서로 실행합니다.

`UnityServices.InitializeAsync → SignInAnonymouslyAsync → GetPlayerNameAsync(false)`

- 이미 인증된 세션이면 중복 로그인하지 않습니다. 캐시된 사용자는 SDK의 익명 로그인 복원 경로를 사용합니다. 로그인 직후 PlayerPrefs.Save로 SDK 소유 캐시를 디스크에 반영합니다. 오류 시 SignOut/ClearSessionToken/DeleteAccount를 호출하지 않습니다.
- 최초 닉네임이 없으면 미설정으로 표시합니다. `autoGenerate=false`이므로 읽기만으로 임의 닉네임을 만들지 않습니다.
- 닉네임 확인은 인증 완료 후 `UpdatePlayerNameAsync`를 호출합니다. 반환된 서버 이름 전체를 메인에 표시합니다.
- 저장 실패 시 기존 닉네임을 유지합니다. 통신 중 중복 저장은 차단하며, 실패 후 닉네임 화면을 다시 열거나 확인을 눌러 재시도할 수 있습니다. 게임은 인증 성공과 관계없이 플레이할 수 있습니다.
- 닉네임의 원본은 **UGS Player Names 서버의 해당 Player ID 레코드**입니다. 별도 Cloud Save/랭킹 서버는 사용하지 않습니다. 인증 세션 캐시는 SDK가 PlayerPrefs에 관리하며 앱은 토큰을 읽어 복사하거나 로그에 남기지 않습니다.
- 앱 종료·재실행은 캐시를 유지합니다. 앱 데이터 삭제/삭제 후 재설치로 익명 세션 캐시를 잃으면 같은 계정 복원은 보장되지 않습니다. 외부 계정 연동은 이번 범위에 포함하지 않았습니다.

근거: [Unity 익명 로그인](https://docs.unity.com/en-us/authentication/use-anon-sign-in), [Player Names API](https://docs.unity.com/ko-kr/oas-player-names/1.0.0). 설치한 3.7.4 SDK의 `AuthenticationServiceInternal.PlayerNames.cs`도 확인했습니다.

## 입력 규칙과 suffix

SDK는 빈 이름/공백 포함 이름을 거부하고, 서버는 숫자 suffix를 추가합니다. suffix를 클라이언트에서 생성하거나 제거한 값을 저장 상태로 취급하지 않습니다. 서버 응답을 그대로 표시합니다.

앱의 입력 제한은 1~50 UTF-16 코드 단위입니다. 공백, 제어 문자, 보이지 않는 Format 문자, 잘못된 서로게이트, `<`/`>`는 전송 전에 거부합니다. 50자 제한은 앱의 보수적인 입력 정책이며 모든 서버 허용 규칙을 대체하지 않습니다. 서버가 거부하는 입력도 실패 메시지로 처리합니다. 한글 이름은 허용합니다.

변경 화면을 열면 새 기본 이름을 입력하도록 빈 입력창을 제공합니다. 기존 서버 suffix를 다시 전송하는 일을 피하며, 현재 이름은 메인에 보존됩니다. 취소는 입력 초안을 버립니다. 이미 확인으로 시작한 서버 요청을 취소하거나 되돌리지는 않습니다.

## TalkBack과 입력창

기존 AccessibleButtonGroup을 확장하여 Button과 AccessibleTextInput을 같은 화면 계층에서 관리합니다. 버튼은 자식 Text, 입력 항목은 Placeholder 문구 `닉네임 입력`을 자동 읽기 이름으로 사용하고 현재 입력 값도 노드 value로 전달합니다. 숨긴 화면과 입력 잠금 처리는 기존 경로를 유지합니다.

Android에서는 입력 항목을 더블탭하거나 일반 터치하면 기본 AlertDialog/EditText가 열립니다. TalkBack이 EditText와 키보드를 직접 조작합니다. **입력 완료**는 Unity 초안으로 복사하는 동작이고, 화면의 **확인**이 서버 저장입니다. 대화상자의 취소는 편집만 취소하고, Unity 화면의 취소는 메인으로 돌아갑니다. 입력 대화상자는 온라인 요청을 하지 않습니다.

Editor에서는 일반 uGUI InputField로 직접 편집합니다. Java 호출은 Android 플레이어에서만 컴파일되므로 Editor 실행에 Android 기능이 필요하지 않습니다. Android UI 스레드 콜백은 큐를 통해 Unity 메인 스레드에 전달합니다. 화면을 숨기면 대화상자를 닫고 늦은 콜백을 무시합니다. 상태/실패 안내는 TalkBack announcement로 전달합니다.

## Unity에서 확인

1. 패키지 Import가 끝난 뒤 SampleScene을 엽니다. 기존 GameBootstrap/Korean Font 연결 외에 수동 Inspector 연결은 없습니다.
2. Play → 닉네임 설정 → 닉네임 입력 → 확인을 실행합니다. 메인 이름에 서버 suffix가 포함되어야 합니다.
3. Play를 종료했다가 다시 시작해 같은 닉네임이 표시되는지 확인합니다.
4. 인터넷을 끈 뒤 실행하거나 저장해도 게임이 동작하고 오류 안내가 표시되는지 확인합니다. 다시 연결한 뒤 닉네임 화면에서 재시도합니다.

## Android 실기기 확인

Android 8.0 이상 ARM64 기기에 최신 APK를 설치합니다. 동일 사용자 유지 검증 중에는 앱 데이터 삭제/제거 없이 업데이트 설치합니다.

1. 인터넷 연결 상태에서 앱 실행 → 익명 인증 완료 → 닉네임 설정을 엽니다.
2. TalkBack으로 닉네임 입력을 찾아 더블탭합니다. 기본 EditText에서 한국어를 입력하고 입력 완료를 누릅니다.
3. 확인을 더블탭하고 메인의 서버 이름을 확인합니다. 이름을 바꾸는 절차도 반복합니다.
4. 앱을 강제 종료한 후 다시 실행합니다. 같은 닉네임과 Dashboard의 같은 Player ID가 유지되는지 확인합니다.
5. 빈 값·공백 포함 값·취소·빠른 확인 반복·오프라인 저장·재접속을 확인합니다.
6. 게임 시작과 기존 오디오 입력 잠금, 결과 및 메인 복귀도 확인합니다.

실기기가 연결되지 않은 환경에서는 TalkBack의 실제 음성·IME 편집·더블탭까지 검증했다고 간주할 수 없습니다.

## 파일 목록

- 신규 서비스: `PlayerAccountService.cs` (서비스/백엔드 계약/검증), `UgsPlayerAccountBackend.cs` (공식 SDK 호출), `PlayerAccountHost.cs` (앱 시작 연결)
- 신규 UI: `NicknameUI.cs`, `AccessibleTextInput.cs`
- 신규 Android 소스: `Assets/Plugins/Android/TextInputDialog.java`
- 수정: `GameBootstrap.cs`, `AccessibleButtonGroup.cs`, `AccessibleButtonBinding.cs`, `NamnyeoChilse.Runtime.asmdef`
- 패키지: `Packages/manifest.json`, `Packages/packages-lock.json`
- 검증: `PlayerAccountTests.cs` 신규, `MainMenuTests.cs` 수정, `Assets/Editor/UgsAccountVerification.cs` 신규
- 새 Assets 파일/폴더의 `.meta`, 이 문서와 기존 설정/검증 문서 갱신

GameManager/GameRound/점수·콤보·오디오 규칙은 수정하지 않았습니다. 별도 UGS 서비스 계정이나 랭킹 리소스를 추가하지 않았습니다.

## 검증 기록 (2026-09-07)

- PlayMode: **49/49 통과**, 실패 0, 104.816초. `Validation/ugs-tests.xml`, `Validation/ugs-tests.log`. 일반 테스트는 TestAccountBackend를 주입해 서버 계정을 생성하지 않습니다.
- 실제 UGS: `UgsAccountVerification.CreateAndSave`로 Play Mode에서 익명 로그인 및 `검증닉네임` 저장 성공. 서버 응답은 `검증닉네임#24667`이었습니다.
- Unity 프로세스를 종료한 뒤 `UgsAccountVerification.Restore`로 **SessionTokenExists=true, 동일 Player ID, 동일 서버 이름**을 확인했습니다. `Validation/ugs-live-save.log`, `Validation/ugs-live-restore.log`, `Validation/MvpProject/UgsVerification.json`.
- 검증용 SDK 프로필 `codex-validation`만 사용했으며 일반 앱은 SDK 기본 프로필을 사용합니다. 실제 서버에 검증 플레이어 하나가 만들어져 유지됩니다. 증거 파일에는 Player ID/이름/복원 여부만 기록하며 인증 토큰은 기록하지 않습니다.
- 따라서 현재 연결된 프로젝트에서는 별도 Dashboard 리소스 추가 없이 익명 인증과 Player Names 요청이 동작했습니다. 사용자가 다른 프로젝트로 연결을 바꾸는 경우 위 Services 확인 절차를 다시 따릅니다.
- 실제 서버 검증용 배치 Editor에서 `UnityEditor.Search.SearchDatabase`의 검색 인덱싱 `ArgumentOutOfRangeException`이 기록되었습니다. UGS 저장/복원은 성공했으며 이 스택은 게임 코드가 아닙니다. 일반 49개 PlayMode 테스트에서는 해당 실패가 없었습니다.
- Android 실기기 TalkBack/IME 편집은 미검증입니다. 위 테스트 절차로 확인해야 합니다.
- 로그인 직후 캐시 반영을 추가한 최종 백엔드로도 같은 ID/이름 복원을 재확인했습니다. `Validation/ugs-live-final-restore.log`.
- Java 대화상자 소스는 Android API 36의 android.jar를 참조하는 javac 컴파일을 통과했습니다. 최초 Unity APK 시도는 빌드 시작 시 캐시한 이전 Java 호출 때문에 실패하여 수정 소스를 재임포트했습니다.
- 최종 Android IL2CPP/ARM64 Development APK: **Succeeded, errors=0**, `Validation/ugs-final-android-build.log`. APK는 `Validation/MvpProject/Builds/TalkBack-MVP.apk` (53,579,494 bytes)이며 기존 APK를 이번 UGS 버전으로 갱신했습니다.
- SHA-256: `66F2317C32279533D18BD6FCC849DB85CC259DFFDE4EC89FFD58CDD01F74B614`.
- aapt 확인: `com.DefaultCompany.talkback_test`, minSdk25/targetSdk36, arm64-v8a, INTERNET 권한. APK DEX에서 TextInputDialog 및 Callback 클래스 포함 확인. Gradle에 복사된 Java 소스가 원본과 일치합니다.
- 최종 ADB 확인 결과 연결된 기기 없음. 실제 Android IME/TalkBack 및 해당 기기의 캐시 복원은 위 실기기 절차로 검증해야 합니다.

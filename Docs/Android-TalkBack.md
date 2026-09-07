# Android TalkBack 버튼 접근성

## 구성

Unity 6000.4.1f1에 포함된 `UnityEngine.Accessibility`를 사용합니다. 기존 프로젝트에 별도 Android Java/Kotlin, AAR/JAR 접근성 플러그인은 없었으며 새 플러그인이나 패키지를 추가하지 않았습니다. `com.unity.modules.accessibility`는 이미 설치되어 있습니다.

위 내용은 최초 버튼 연결 단계의 기록입니다. 후속 닉네임 기능에서 `AccessibleTextInput`과 Android `TextInputDialog.java`를 추가했습니다. 기본 버튼은 여전히 내장 접근성 API를 사용하고, 입력 항목을 활성화하면 Android 기본 EditText를 엽니다. 최신 설정은 `Docs/UGS-Authentication-Nickname.md`를 참고하세요.

- `AccessibleButtonGroup`: 하위 uGUI Button을 자동 탐색하고 접근성 계층 및 TalkBack 켜짐/꺼짐을 관리합니다.
- `AccessibleButtonBinding`: 버튼의 자식 `UnityEngine.UI.Text`를 읽기 이름으로 사용하고, 화면 영역·표시 여부·Disabled 상태를 동기화합니다. Text가 없거나 비어 있으면 오브젝트 이름을 사용합니다.
- `AccessibilityNode.invoked` → `TryActivate` → 기존 `Button.onClick.Invoke()`로 연결합니다. 남녀 버튼 전용 분기나 별도 판정 로직은 없습니다.
- `GameBootstrap`이 생성하는 GameCanvas에 그룹을 자동 추가합니다. `GameUI`는 결과창 표시 시 탐색 범위를 결과창 안으로 제한합니다. 따라서 남자/여자뿐 아니라 다시 시작도 적용됩니다.

Unity 모바일의 invoked 이벤트는 합성 탭도 발생시키므로 TalkBack 사용 중 그룹의 CanvasGroup.blocksRaycasts를 끄고 접근성 콜백으로 클릭을 전달합니다. TalkBack을 끄거나 컴포넌트를 비활성화하면 원래 값을 복원합니다. 호출 시 실제 버튼의 활성/표시/interactable 상태를 다시 검사합니다. 기존 게임 입력 잠금, 점수, 콤보, 오디오 Coroutine은 그대로 사용합니다.

근거: [Unity AccessibilityNode.invoked API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Accessibility.AccessibilityNode-invoked.html), [Unity 모바일 접근성 지원 범위](https://docs.unity3d.com/cn/6000.0/Manual/mobile-accessibility.html).

## Inspector와 새 버튼

현재 SampleScene에는 추가 수동 연결이 필요 없습니다. Play 모드에서 생성된 `GameCanvas`의 Accessible Button Group을 확인할 수 있습니다.

새 화면에는 다음 순서로 적용합니다.

1. uGUI Canvas 또는 해당 화면의 루트에 **Accessible Button Group**을 한 번 추가합니다. CanvasGroup은 자동 추가됩니다.
2. 그 아래에 일반 **Button**과 자식 **UI Text**를 만들고 `게임 시작`, `랭킹` 등 실제 표시 문구를 입력합니다. 버튼별 접근성 이름 필드는 없습니다. 현재 자동 이름 추출 대상은 uGUI Text이며 TextMeshPro는 별도 어댑터가 필요합니다.
3. 기존처럼 Button.onClick을 연결합니다. 런타임에 추가한 버튼이나 변경한 Text도 자동 반영됩니다.
4. 동시에 보이는 화면에는 그룹 하나만 사용하고 중첩 그룹을 만들지 않습니다. 하위 CanvasGroup의 **Ignore Parent Groups**는 꺼 두어 합성 탭 차단이 유지되게 합니다. 카메라 방식 Canvas는 World Camera를 지정합니다.
5. 모달을 띄우면 `group.SetModalRoot(modal.transform)`, 닫으면 `group.SetModalRoot(null)`을 호출합니다. GameUI의 결과창에는 이미 연결되어 있습니다.

이 단계는 버튼 접근성입니다. 점수/제한시간 자동 음성 안내, 메인화면, 랭킹 기능은 추가하지 않았습니다. 이름 음성은 기존 Resources AudioClip을 사용하며 앱 TTS는 추가하지 않았습니다. 버튼 문구를 읽는 음성은 Android TalkBack이 담당합니다.

## Android에서 실행

Unity 내장 화면 읽기 지원에 맞춰 **Android 8.0 / API 26 이상** 기기로 검증합니다. API 26 미만에서는 이 연결을 켜지 않고 일반 터치를 유지합니다. 원본 최소 SDK 23 설정은 수정하지 않았으나 Android 전환 시 Unity가 검증 복사본을 25로 올렸으며, 생성된 APK의 실제 최소 SDK는 25입니다. Editor는 Android 네이티브 접근성 API를 호출하지 않습니다.

1. Unity Hub에서 해당 Editor의 Android Build Support, SDK/NDK, OpenJDK가 설치되어 있는지 확인합니다.
2. 원본 프로젝트에서 씬을 저장하고 **File → Build Profiles → Android → Switch Platform**을 선택합니다.
3. Scene List에 `Assets/Scenes/SampleScene.unity`가 활성화되어 있는지 확인합니다. 기존 IL2CPP/ARM64 설정을 유지합니다.
4. USB 디버깅을 켠 기기를 연결하고 기기에서 디버깅을 허용한 뒤 **Build And Run**합니다.
5. 기기 **설정 → 접근성 → TalkBack**에서 TalkBack을 켭니다. 제조사에 따라 메뉴 이름이 다를 수 있습니다. 한국어 읽기 음성이 사용 가능한지도 확인합니다.

## 실기기 확인 순서

1. TalkBack을 켜고 앱을 시작합니다. 메인화면에서 게임 시작·랭킹·닉네임 설정을 탐색한 뒤 게임 시작을 더블탭합니다. 게임의 좌우 버튼을 손가락으로 탐색하거나 한 손가락 좌우 스와이프로 이동합니다. 화면 전환 구조는 `Docs/Main-Menu.md`를 참고하세요.
2. 각각 `남자`, `여자`라고 읽는지 확인합니다. TalkBack이 역할에 따라 `버튼`, 잠금 상태에 따라 `사용할 수 없음` 등을 덧붙일 수 있습니다.
3. 이름 오디오 재생 중에는 더블탭해도 판정되지 않아야 합니다. 모든 이름 재생 후 더블탭하면 선택한 성별로 기존 게임 입력이 한 번 실행되어야 합니다.
4. 정답 후 점수/콤보 증가, 오답 후 점수 유지/콤보 0을 확인합니다. 콤보가 올라 여러 명이 재생되면 모든 재생이 끝난 후 순서대로 입력합니다.
5. 60초가 지나면 뒤의 남녀 버튼은 탐색 대상에서 빠지고 `다시 시작`, `메인으로`를 탐색·더블탭할 수 있어야 합니다. 재시작 후 점수/콤보/오디오가 초기화되고, 메인으로 이동하면 메인 버튼만 탐색되어야 합니다.
6. 앱 실행 중 TalkBack을 껐다 켜기, 앱 백그라운드 후 복귀, 화면 방향 변경 후에도 탐색 영역과 입력이 맞는지 확인합니다. TalkBack을 끄면 일반 터치가 복원되어야 합니다.

## 검증

이번 작업의 파일 목록:

- 신규: `Assets/Scripts/AccessibleButtonGroup.cs`, `Assets/Scripts/AccessibleButtonBinding.cs`
- 신규: `Assets/Tests/PlayMode/AccessibilityButtonTests.cs`, `Assets/Editor/AndroidValidationBuild.cs` 및 새 Assets 파일/Editor 폴더의 `.meta`
- 수정: `Assets/Scripts/GameBootstrap.cs`, `Assets/Scripts/GameUI.cs`
- 문서: `Docs/Android-TalkBack.md` 신규, `Docs/MVP-Setup.md`, `Docs/MVP-Validation.md`, `Docs/AI/UnityProjectContext.md` 수정

AndroidValidationBuild는 배치 검증용 Editor 도구이며 게임에 포함되지 않습니다. `-executeMethod NamnyeoChilse.Editor.AndroidValidationBuild.Build`로 실행하면 해당 프로젝트의 `Builds/TalkBack-MVP.apk`에 Development APK를 만듭니다.

2026-09-06 격리 복사본 `Validation/MvpProject`에서 PlayMode **40개 통과, 실패 0개**. 결과: `Validation/accessibility-results.xml`, 로그: `Validation/accessibility-tests.log`.

신규 테스트는 자동 이름/역할/좌표, 재생 중 입력 거부, 기존 정답·오답 결과, 중복 호출 차단, 결과창 범위와 재시작, 일반 버튼 자동 등록/이름 변경/삭제, 합성 탭용 Raycast 차단과 복구를 확인합니다. 자동 테스트는 관리 코드 경로를 검사하며 TalkBack 자체의 실제 음성·더블탭을 대체하지 않습니다.

현재 `adb devices -l`에 연결된 기기가 없으므로 **실기기 TalkBack 최종 확인은 미완료**입니다.

Android Development APK 빌드도 **Succeeded / errors=0 / 종료 코드 0**으로 완료했습니다. 로그: `Validation/android-build.log`. 기존 FindFirstObjectByType 사용의 CS0618 경고는 남아 있습니다.

- APK: `Validation/MvpProject/Builds/TalkBack-MVP.apk` (2026-09-07 UGS/닉네임 입력 포함 빌드로 갱신, 53,579,494 bytes; 최신 검증은 `Docs/UGS-Authentication-Nickname.md`)
- 패키지: `com.DefaultCompany.talkback_test`, IL2CPP / arm64-v8a, 최소 SDK 25 / 대상 SDK 36 (`aapt dump badging` 확인)
- 현재 APK SHA-256: `66F2317C32279533D18BD6FCC849DB85CC259DFFDE4EC89FFD58CDD01F74B614`

이 APK를 연결된 기기에 `adb install -r`로 설치해도 됩니다. 위 실기기 확인 순서는 동일합니다. APK 생성은 TalkBack 실기기 동작 확인을 의미하지 않습니다.

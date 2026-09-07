# Resources 이름 음성 자동 등록

1. Play를 종료합니다.
2. `Assets/Resources/Names/`에 `영수.wav`, `민희.wav`처럼 이름만 있는 파일명으로 음성을 넣습니다.
3. Unity의 AudioClip 임포트가 완료되면 Play를 시작합니다.

Inspector 등록은 필요 없습니다. GameBootstrap의 수동 Names 필드와 SampleScene의 기존 목록을 제거했습니다. 매 Play 시작 시 목록을 만들며, 다시 시작 버튼은 로딩한 목록을 재사용합니다.

## 폴더 정리

- `Assets/Resources/Names/*.wav`: 이름 음성 20개. 기존 파일의 접두사를 제거했습니다.
- `Assets/AudioSource/Names/boys/*.m4a`, `girls/*.m4a`: 기존 원본과 메타 파일을 이동해 보존했습니다.
- `Assets/Resources/` 루트의 BGM/효과음: 변경하지 않았습니다.

기존 M4A는 이 Unity 환경에서 DefaultImporter로 인식되어 AudioClip 로딩 대상이 아니었습니다. 보유 음성을 PCM 16-bit WAV로 변환했습니다. 외부 음원은 추가하지 않았습니다. 앞으로도 Unity가 AudioClip으로 임포트하는 WAV/MP3/OGG 등을 사용하세요. 런타임 M4A 변환 기능은 없습니다.

## 로딩과 판정

`ResourceNameLoader.Load()`가 `Resources.LoadAll<AudioClip>("Names")`로 하위 폴더까지 읽고 `CreateNames()`가 데이터를 만듭니다. Resources 루트의 BGM/효과음은 대상이 아닙니다.

- 확장자를 제외한 AudioClip.name을 이름으로 사용합니다.
- 수로 끝나면 Gender.Male, 희로 끝나면 Gender.Female.
- 다른 끝 글자는 경고 로그를 출력하고 제외합니다. 파일명의 공백도 그대로 검사합니다.
- 같은 이름은 먼저 로드된 클립 하나만 등록하고 중복은 경고 후 제외합니다. 중복 파일은 하나만 유지하는 것이 좋습니다.
- 한글 Unicode NFC 정규화로 조합 방식이 다른 동일 이름도 중복 처리합니다.
- 유효한 음성이 0개면 경고를 출력하고 기존 NameManager의 음성 없는 기본 이름 10개로 진행합니다.

GameBootstrap이 자동 생성한 배열을 GameManager.ConfigureNames()로 전달합니다. 기존 NameManager는 이 데이터를 랜덤 선택합니다. 양 성별이 있으면 기존처럼 성별 50:50, 그룹 안에서 균등 선택합니다. 한 성별만 있으면 해당 그룹만 사용합니다. 점수·시간·판정·재시작 로직은 유지했습니다.

```csharp
NameData selected = gameManager.Round.CurrentName;
string name = selected.Name;
Gender gender = selected.Gender;
AudioClip clip = selected.AudioClip;
```

콤보에 따라 여러 이름을 연속 재생한 뒤 순서대로 입력하는 기능이 연결되어 있습니다. `Docs/Sequence-Playback.md`를 참고하세요. 음성 파일 추가 방법은 동일합니다.

## 변경 파일

- 신규: Assets/Scripts/ResourceNameLoader.cs 및 .meta
- 신규: Assets/Tests/PlayMode/ResourceNameLoaderTests.cs 및 .meta
- 신규: Assets/Resources/Names/*.wav 20개 및 메타
- 이동: Assets/Resources/{boys,girls} 및 메타 → Assets/AudioSource/Names/{boys,girls}
- 수정: GameBootstrap.cs, NameManager.cs(주석), SampleScene.unity, GameFlowTests.cs
- 갱신: 이 안내, UnityProjectContext.md, MVP-Setup.md, MVP-Validation.md
- 로컬 검증 전용: Validation/convert_name_audio.py, Validation/tools(FFmpeg), 결과/로그. 게임 런타임 의존성은 아닙니다.

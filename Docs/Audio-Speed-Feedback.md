# 이름 배속과 정답·오답 음성

## Unity 설정

1. Play를 종료하고 SampleScene의 `NamnyeoChilseGame`을 선택합니다.
2. `Game Bootstrap → Sequence Settings → Tiers`에서 Minimum Combo, Name Count, Playback Speed를 수정합니다.
3. Audio Interval은 모든 구간에서 **0**을 유지하면 인위적인 이름 사이 대기가 없습니다. 필요할 때만 양수로 설정할 수 있는 선택 항목입니다.
4. 씬을 저장하고 Play → 게임 시작을 누릅니다. 추가 AudioClip/AudioSource 연결은 없습니다.

| 콤보 시작 | 이름 개수 | 간격 | 배속 |
| --- | --- | --- | --- |
| 0 | 1 | 0초 | 1.0 |
| 5 | 2 | 0초 | 1.2 |
| 10 | 3 | 0초 | 1.4 |
| 15 | 4 | 0초 | 1.6 |

문제 시작 시 인원 수·배속·간격을 함께 확정합니다. 오답 또는 재시작 후 다음 문제는 콤보 0 구간입니다. 기존 씬의 간격도 0으로 변경했으며, 새 배속 값이 없는 데이터는 안전하게 1배로 처리합니다. pitch 방식이므로 빠르게 재생하면 음높이도 올라갑니다. 배속은 0.1~3 범위이고 비정상 값은 1배로 처리합니다.

## 음원과 자동 연결

Resources 전체에서 정답 `sfx_correct.m4a`, 오답 `sfx_line_wrong.m4a`를 확인했습니다. 현재 환경에서 M4A는 AudioClip으로 임포트되지 않으므로 원본은 그대로 두고 다음 WAV 사본을 만들었습니다. 외부 음원은 추가하지 않았습니다.

- `Assets/Resources/Feedback/sfx_correct.wav` (약 0.627초)
- `Assets/Resources/Feedback/sfx_line_wrong.wav` (약 0.557초)

`AnswerAudioPlayer.Awake()`가 `Resources.Load<AudioClip>("Feedback/sfx_correct")`와 `Resources.Load<AudioClip>("Feedback/sfx_line_wrong")`로 한 번 로드합니다. 교체할 때 위 WAV 파일을 같은 이름으로 바꾸면 됩니다. 파일이 없거나 로딩에 실패하면 경고하고 음성을 건너뜁니다.

이름 음원은 계속 `Resources/Names`만 읽으므로 피드백이 이름 데이터에 섞이지 않습니다. 다른 BGM, 점수, 카운트다운 음원은 이번 기능에서 사용하지 않습니다.

## 재생과 입력

- `NameAudioPlayer`의 기존 AudioSource가 현재 문제의 Playback Speed로 이름을 재생합니다.
- `AnswerAudioPlayer`는 자식 `AnswerAudio`에 별도 AudioSource와 재생 컴포넌트를 자동 생성합니다. 피드백은 방금 푼 문제의 배속을 사용합니다. 콤보가 증가하거나 오답으로 초기화되어도 해당 피드백은 이전 문제의 속도이며 다음 문제부터 새 배속이 적용됩니다.
- 이름은 앞 클립의 실제 `isPlaying` 종료를 확인한 프레임에 다음 클립으로 진행합니다. 고정 대기시간은 없지만 프레임/오디오 버퍼 수준의 전환 지연과 음원 자체의 무음은 남을 수 있습니다. 원본을 자르거나 DSP 샘플 단위 접합을 하지는 않습니다.
- 필요한 입력을 모두 받을 때까지 기록만 하며 판정 이벤트와 음성은 없습니다. 마지막 입력에서 전체 배열을 비교하여 정답/오답 한 번과 피드백 한 번을 처리합니다. 첫 입력이 틀려도 마지막까지 기다립니다.
- 입력 사이에는 동일 프레임 중복 이벤트를 막는 1프레임 보호만 있습니다. 마지막 입력은 즉시 판정하며, 다음 이름과 판정음을 별도 AudioSource에서 동시에 시작합니다. 판정음 종료를 기다리지 않으며 두 음성은 겹칠 수 있습니다. 별도 전환 딜레이 설정은 추가하지 않았습니다.
- 점수/콤보는 전체 정답에만 기존 규칙대로 증가합니다. 오답은 콤보만 0이 됩니다.
- 종료/재시작/비활성화는 하나의 진행 코루틴과 두 AudioSource를 중단합니다. 피드백 중에도 기존 60초 타이머는 계속 진행되며 종료 시 두 음성을 모두 중단합니다.

## 변경 파일

- 런타임: `SequenceSettings.cs`, `GameRound.cs`, `NameAudioPlayer.cs`, `GameManager.cs`, 신규 `AnswerAudioPlayer.cs`
- 씬: `Assets/Scenes/SampleScene.unity`의 콤보별 간격/배속만 변경
- 음원: 위 WAV 두 개 및 메타파일, Feedback 폴더 메타파일
- 테스트: `AudioIntervalTests.cs`, `NamePlaybackTests.cs`, 신규 `AnswerPlaybackTests.cs` 및 메타파일
- 문서: 이 문서, `Sequence-Playback.md`, `Name-Playback.md`, `AI/UnityProjectContext.md`

## 검증 결과

아래 51개 결과는 이전 입력별 피드백 구현의 기록입니다. 최신 전체 배열 판정 변경의 검증 결과는 문서 마지막에 기록합니다.

Unity 6000.4.1f1의 격리된 Validation/MvpProject에서 전체 PlayMode 테스트 51개 통과, 실패 0개, 120.938초. 결과: Validation/speed-feedback-results.xml, 로그: Validation/speed-feedback-tests.log. 실제 AudioSource를 사용해 1.6배 두 이름 재생 시간, 정답/부분정답/오답 피드백, 음성 겹침 방지, 입력 잠금, 재시작과 시간 만료 취소를 검증했습니다. 기존 60초 게임·메뉴·접근성·닉네임 테스트도 통과했습니다. 실제 스피커 청취 및 Android TalkBack 실기기 검증과 새 Android 빌드는 이번 변경에서 실행하지 않았습니다.

## 전체 배열 판정 및 템포 변경 검증

Unity 6000.4.1f1 격리 프로젝트 전체 PlayMode 55개 통과, 실패 0개, 108.144초. Validation/tempo-results.xml 및 tempo-tests.log. 2/3/4명 모든 오답 위치의 지연 판정, 중간 입력 무음, 최종 판정 이벤트 한 번, 피드백 중 중복 입력 차단, 1.6배 오답 피드백 후 1배 이름 재생(당시 순차 재생 방식), 추가 전환 지연 없음(테스트에서 0.1초 미만), 재시작/만료 취소와 기존 게임/UI 회귀를 검증했습니다. 실제 Android TalkBack 및 청취 검증은 하지 않았습니다.

이번 변경 파일: Assets/Scripts/{GameRound,GameManager,AnswerAudioPlayer}.cs, Assets/Tests/PlayMode/{SequenceTests,ComboScoringTests,AudioIntervalTests,AnswerPlaybackTests}.cs, 이 문서와 Sequence-Playback.md, Name-Playback.md, AI/UnityProjectContext.md. 새 Inspector 연결이나 패키지, 음원, 씬 변경은 없습니다.

최신 동시 재생 변경: GameManager는 전체 판정 후 즉시 BeginName(false)와 AnswerAudioPlayer.Play를 호출합니다. 판정음은 자체 코루틴에서 방금 문제의 배속으로 재생되며 다음 문제 입력을 지연시키지 않습니다. 종료/재시작/비활성화 시 독립 코루틴과 두 소스를 모두 취소합니다. 판정음이 끝나기 전에 새 문제가 판정되면 이전 판정음은 새 판정음으로 교체됩니다. 변경 파일은 GameManager.cs, AnswerAudioPlayer.cs, AnswerPlaybackTests.cs입니다.

동시 재생 검증: Unity PlayMode AnswerPlaybackTests 6개 모두 통과(4.050초), Validation/overlap-final-results.xml. 실제 AudioSource 동시 재생, 전체 입력 전 무판정, 문제별 pitch 유지, 재시작/만료 시 두 음성 중단을 확인했습니다. 이번에는 관련 테스트만 실행했으며 Android 실기기 청취는 하지 않았습니다.

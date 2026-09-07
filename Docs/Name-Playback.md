최신: 이름 사이 기본 간격은 0초, 콤보별 배속은 1/1.2/1.4/1.6배입니다. 모든 입력 완료 후 다음 이름과 판정음을 동시에 시작합니다. 중간 입력에는 판정음이 없습니다. [설정 및 구조](Audio-Speed-Feedback.md)

# 이름 음성 재생

## 실행

1. `Assets/Resources/Names`에 이름 WAV가 있는지 확인합니다.
2. SampleScene을 열고 Play → 게임 시작을 누릅니다. 추가 Inspector 연결은 없습니다.
3. 문제에 포함된 모든 이름 음성이 재생되는 동안 남자/여자 버튼은 비활성화됩니다.
4. 전체 재생이 끝나면 성별을 순서대로 입력합니다. 모든 입력이 모이면 전체 정답 여부를 한 번 판정하고 판정음과 다음 문제를 동시에 재생합니다. 인원 수는 콤보에 따라 증가하며 `Docs/Sequence-Playback.md`를 참고하세요.
5. 60초 종료 시 음성을 중지하고 결과를 표시합니다. 다시 시작하면 이전 대기 작업을 취소하고 새 이름으로 시작합니다.

소리가 들리지 않으면 Game 뷰의 Mute Audio, OS 출력 장치와 볼륨을 확인하세요. 기존 Main Camera의 AudioListener를 사용합니다.

## 책임과 수명

- `NameAudioPlayer.cs`: 이름 전용 AudioSource를 관리합니다. GameManager의 RequireComponent를 통해 같은 `NamnyeoChilseGame` 오브젝트에 자동 생성됩니다. 2D 오디오, Loop/Play On Awake 꺼짐입니다. 별도 Inspector 필드는 없습니다.
- `GameManager.cs`: 코루틴 하나와 CanAcceptInput을 관리합니다. 재생 시작 전에 잠그고, NameAudioPlayer의 실제 재생이 끝난 뒤 아직 게임 중이면 입력을 허용합니다.
- `GameUI.cs`: 두 버튼의 interactable을 CanAcceptInput과 동기화합니다. 메서드를 직접 호출해도 GameManager가 같은 조건으로 입력을 막습니다.

클립 종료는 AudioSource.isPlaying으로 확인합니다. 각 클립 종료 후 다음 이름이 있으면 Sequence Settings의 Audio Interval만큼 대기합니다. 오디오 로딩과 이름 사이 대기 중에도 입력 잠금을 유지하고, 로딩 실패/클립 없음은 해당 음성을 건너뛰어 진행합니다. 이름 오디오는 AudioListener.pause 영향을 받지 않도록 설정되어 있습니다.

입력을 받자마자 잠금을 걸어 해당 이름을 소비한 뒤 판정합니다. 다음 이름은 최소 한 프레임 이후 준비되므로 같은 프레임의 여러 버튼 이벤트로 중복 판정되지 않습니다. 입력이 다시 활성화된 뒤의 입력은 새 이름에 대한 답입니다.

종료·재시작·GameManager 비활성화 시 StopCoroutine과 AudioSource.Stop을 호출하고 clip 참조를 해제합니다. GameManager를 다시 활성화하면 시간이 남아 있을 때 현재 이름을 처음부터 재생합니다. 제한 시간은 음성 재생 중에도 계속 경과합니다. 앱 백그라운드/복귀 실기기 동작은 별도 검증 대상입니다.

## 변경 파일

- 신규: `Assets/Scripts/NameAudioPlayer.cs`, `.meta`
- 수정: `Assets/Scripts/GameManager.cs`, `Assets/Scripts/GameUI.cs`
- 신규: `Assets/Tests/PlayMode/NamePlaybackTests.cs`, `.meta`
- 수정: `Assets/Tests/PlayMode/GameFlowTests.cs`
- 문서: 이 안내 및 기존 실행/Resources/프로젝트/검증 문서 갱신

위 파일 목록은 최초 음성 재생 작업 기준입니다. 후속 연속 이름 문제 변경은 `Docs/Sequence-Playback.md`에 정리했습니다. TTS나 재생 속도 변경은 없습니다.

최신 배속 및 정답·오답 피드백 흐름은 [Audio-Speed-Feedback.md](Audio-Speed-Feedback.md)를 참고하세요.

# 콤보별 연속 이름 문제

60초 게임 안에서 한 묶음의 이름을 듣고 순서대로 답하는 것을 **문제(라운드)**로 봅니다. 문제 전체 성공 시에만 콤보 +1과 기존 배율 점수를 한 번 획득합니다.

## 설정

1. Play 종료 후 SampleScene을 엽니다.
2. `NamnyeoChilseGame → Game Bootstrap → Sequence Settings → Tiers`를 펼칩니다.
3. **Minimum Combo**, **Name Count**, **Audio Interval**(초), **Playback Speed**(배속)을 수정하고 씬을 저장합니다.
4. 다음 Play부터 적용됩니다. 점수 배율은 기존 Scoring에서 따로 설정합니다.

| Minimum Combo | Name Count | Audio Interval | Playback Speed |
| --- | --- | --- | --- |
| 0 | 1 | 0초 | 1.0 |
| 5 | 2 | 0초 | 1.2 |
| 10 | 3 | 0초 | 1.4 |
| 15 | 4 | 0초 | 1.6 |

구간 순서는 무관하며 현재 콤보 이하의 가장 높은 시작값을 적용합니다. 동일 시작값은 큰 인원 수를 가진 항목을 사용하며 인원 수도 같으면 먼저 등록한 항목을 사용합니다. 인원 수와 간격은 항상 같은 항목에서 가져옵니다. 음수 시작값/1 미만 인원 수는 무시하고 적용할 구간이 없으면 1명/0초/1배입니다. 음수 간격은 0초로 보정합니다.

## 플레이

- 문제 시작 시 현재 콤보로 인원 수를 결정하고 이름과 정답 성별 순서를 저장합니다. 진행 중에는 인원 수가 바뀌지 않습니다.
- 이름마다 실제 AudioSource 재생 종료 → 해당 문제의 Audio Interval만큼 대기 → 다음 이름을 재생합니다. 마지막 이름까지 끝나야 입력이 열리며 대기 중에도 잠금을 유지합니다. 기본 간격은 0초이며 Playback Speed를 pitch로 적용합니다.
- 간격은 문제 시작 시 현재 콤보로 고정합니다. 오답/재시작 후에는 콤보 0의 0초 간격/1배속로 돌아갑니다. 다만 첫 이름 전, 마지막 이름 후, 문제와 문제 사이에는 추가 대기가 없습니다. 기본 콤보 0에서는 1명뿐이므로 실제 이름 사이 대기는 발생하지 않습니다.
- 여러 명인 경우 화면에 재생 순서와 이름(`2/3 민희`), 입력 단계에서는 `입력 1/3`처럼 진행 위치를 표시합니다.
- 중간 입력은 정답 여부를 검사하지 않고 기록만 합니다. 입력 사이에 판정음이나 이름 음성을 재생하지 않습니다.
- 필요한 입력 개수가 모두 모이면 전체 배열을 비교합니다. 하나라도 다르면 그때 문제 실패로 처리하고 콤보 0, 점수 유지, 오답 음성 한 번을 재생합니다. 오답 음성은 방금 문제의 배속이며 다음 문제부터 기본 설정의 1명/1배로 돌아갑니다.
- 모두 정답이면 콤보 +1 후 기존 Scoring 배율로 점수를 한 번 더합니다. 5번째 문제 성공 다음부터 2명, 10번째 다음부터 3명, 15번째 다음부터 4명입니다.
- 각 입력 직후 다음 프레임까지 잠가 같은 프레임의 중복 버튼 이벤트를 막습니다.

## 이름 부족 및 초기화

`NameManager.NextSequence()`는 후보에서 선택한 이름을 제거하는 방식으로 중복 없이 추첨합니다. 유효한 서로 다른 이름이 설정 인원보다 적으면 **실제 보유 인원까지만 출제**합니다. 예: 이름 2개/요청 4명 → 2명 문제. 재추첨 반복은 없습니다. 빈 Resources에는 기존 무음 기본 이름 목록을 사용합니다.

게임 종료 시 현재 이름 목록·정답 배열·입력 기록을 비우고 재생 코루틴/AudioSource를 중지합니다. 재시작 시 점수·콤보·입력을 초기화하고 새 문제를 생성합니다. 비활성화 후 다시 활성화하면 현재 문제의 입력을 초기화하고 처음부터 다시 들려줍니다. 60초 시간은 계속 경과합니다.

## 코드와 변경 파일

- 신규: `Assets/Scripts/SequenceSettings.cs`, `.meta` — Inspector 인원 수 구간 데이터
- 수정: `NameManager.cs` — 중복 없는 유한 추첨, 중복 이름 데이터 제거
- 수정: `GameRound.cs` — Sequence/ExpectedAnswers/InputIndex, 부분 정답 Progress, 문제 단위 점수/콤보
- 수정: `GameManager.cs` — 전체 순서 재생 후 입력 허용, 부분 정답은 재생 없이 다음 입력 허용
- 수정: `GameBootstrap.cs`, `GameUI.cs`, `Assets/Scenes/SampleScene.unity` — 설정 전달과 진행 UI
- 신규: `Assets/Tests/PlayMode/SequenceTests.cs`, `.meta`
- 수정: `ComboScoringTests.cs` — 점수 경계 테스트에서 문제 전체를 입력하도록 갱신
- 문서: 이 안내와 기존 실행/콤보/음성/프로젝트/검증 문서 갱신

Resources 파일, ScoringSettings의 기본 점수/배율, NameAudioPlayer의 속도/완료 판정은 변경하지 않았습니다.

## 재생 간격 추가 변경

- `SequenceSettings.cs`: 기존 SequenceTier에 Audio Interval을 통합하고 기본값을 추가했습니다.
- `GameRound.cs`: 문제 시작 시 AudioInterval을 저장합니다.
- `GameManager.cs`: 이름 사이에서만 WaitForSecondsRealtime을 사용합니다. timeScale과 무관하며 기존 종료/재시작 취소 경로가 대기도 취소합니다. 다음 클립 재생 직전에도 게임 종료 상태를 확인합니다.
- `SampleScene.unity`: Inspector 구간별 간격 기본값 저장.
- 신규 테스트: `AudioIntervalTests.cs` 및 `.meta`.

실행 방법과 오브젝트 연결은 기존과 동일합니다. Play를 종료한 상태에서 값을 편집하고 저장한 뒤 다시 Play 하세요.

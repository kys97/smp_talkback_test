# 콤보와 점수 배율

## Inspector 설정

1. Play를 종료하고 SampleScene을 엽니다.
2. `NamnyeoChilseGame → Game Bootstrap → Scoring`을 펼칩니다.
3. **Base Score**에서 기본 점수를 설정합니다. 기본값은 100입니다.
4. **Tiers**에서 구간을 추가/삭제하고 **Minimum Combo**(시작 콤보), **Multiplier**(정수 배율)를 설정합니다.
5. 씬을 저장하고 Play를 시작합니다. 설정은 Play 시작 때 복사되며, 다시 시작 버튼은 동일 설정을 유지합니다.

기본 설정:

| Minimum Combo | Multiplier | 적용 구간 |
| --- | --- | --- |
| 0 | 1 | 0~4 |
| 5 | 2 | 5~9 |
| 10 | 3 | 10~14 |
| 15 | 4 | 15 이상 |

가장 높은 만족 임계값을 적용하므로 Tiers의 입력 순서는 상관없습니다. 다음 구간 직전까지 현재 배율을 유지합니다. 같은 시작값이 중복되면 더 큰 배율을 사용합니다. 음수 시작값/1 미만 배율은 무시하며, 적용할 구간이 없으면 x1입니다. 기본 점수는 최소 1로 보정됩니다. 정상 설정은 중복 없는 시작값을 오름차순으로 관리하는 것을 권장합니다.

## 점수 계산

문제 전체 정답: 콤보 +1 → 증가한 콤보의 배율 조회 → 기본 점수 × 배율을 누적 점수에 더합니다. 여러 이름 문제의 중간 정답은 점수/콤보를 올리지 않습니다.

- 1~4번째 연속 정답: 각각 +100
- 5번째: +200, 누적 600
- 10번째: +300, 누적 1700
- 15번째: +400, 누적 3300

오답은 콤보만 즉시 0으로 초기화하고, 누적 점수는 유지합니다. 다음 정답부터 새 콤보를 시작합니다. 종료된 상태/오디오 재생 중의 무시된 입력은 점수와 콤보를 변경하지 않습니다. 게임 재시작은 점수·콤보·직전 획득 점수를 0으로 초기화합니다.

## UI와 구조

- `ScoringSettings.cs`: Inspector 직렬화용 ScoringSettings/ComboTier 및 배율 조회. 기본값이 데이터에 들어 있으며 게임 판정 코드에는 특정 구간 조건이 없습니다.
- `GameRound.cs`: Score, Combo, Multiplier, LastAwardedScore를 관리합니다.
- `GameBootstrap.cs`: Inspector 설정을 GameManager에 전달하고 Combo 텍스트를 생성/연결합니다.
- `GameManager.cs`: 기존 ConfigureNames의 선택 매개변수로 점수 설정을 전달합니다. 재생/입력 잠금 코드는 유지했습니다.
- `GameUI.cs`: `콤보 5 · x2`, `점수 600`, `정답! +200`처럼 실제 게임 상태를 표시합니다.

콤보별 인원 수는 Sequence Settings에서 별도로 설정합니다. 연속 이름 문제는 `Docs/Sequence-Playback.md`를 참고하세요. 재생 속도와 60초 제한시간은 유지되며 추가 Inspector 오브젝트 연결은 필요 없습니다.

## 변경 파일

- 신규: Assets/Scripts/ScoringSettings.cs, Assets/Tests/PlayMode/ComboScoringTests.cs 및 .meta
- 수정: GameRound.cs, GameManager.cs, GameBootstrap.cs, GameUI.cs
- 수정: Assets/Scenes/SampleScene.unity (Scoring 기본값)
- 수정: GameFlowTests.cs, NamePlaybackTests.cs (100점 기준 기대값 반영)
- 문서: 이 안내와 프로젝트/실행/검증 문서 갱신

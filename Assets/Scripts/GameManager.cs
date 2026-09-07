using System;
using System.Collections;
using UnityEngine;

namespace NamnyeoChilse
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NameAudioPlayer))]
    [RequireComponent(typeof(AnswerAudioPlayer))]
    public sealed class GameManager : MonoBehaviour
    {
        private GameRound round;
        private int displayedSeconds = -1;
        private NameAudioPlayer audioPlayer;
        private AnswerAudioPlayer answerAudio;
        private Coroutine playback;
        private bool inputReady;
        private bool completionPending;
        public int GameNumber { get; private set; }
        public event Action<int, int> Completed;
        public int PlaybackIndex { get; private set; } = -1;
        public GameRound Round => round;
        public bool CanAcceptInput => isActiveAndEnabled && round != null && round.IsPlaying && inputReady;
        public event Action StateChanged;
        public event Action<AnswerResult> Answered;

        private void Awake()
        {
            round = new GameRound(new NameManager());
            audioPlayer = GetComponent<NameAudioPlayer>();
            answerAudio = GetComponent<AnswerAudioPlayer>();
        }

        private void Start()
        {
            if (!round.IsPlaying) StartGame();
        }

        // Bootstrap configures the catalog before Start begins the first round.
        public void ConfigureNames(System.Collections.Generic.IEnumerable<NameData> names, ScoringSettings scoring = null,
            SequenceSettings sequenceSettings = null)
        {
            if (round != null && round.IsPlaying)
                throw new InvalidOperationException("이름 목록은 게임 시작 전에 설정해야 합니다.");
            round = new GameRound(new NameManager(names, new System.Random()), scoring, sequenceSettings);
        }

        public void StartGame()
        {
            if (!isActiveAndEnabled) return;
            CancelPlayback();
            round.Start(Time.realtimeSinceStartupAsDouble);
            GameNumber++;
            completionPending = true;
            BeginName();
        }

        private void Update()
        {
            if (!round.IsPlaying) return;
            round.Tick(Time.realtimeSinceStartupAsDouble);
            if (!round.IsPlaying) CancelPlayback();
            if (!round.IsPlaying || displayedSeconds != (int)Math.Ceiling(round.RemainingSeconds))
                PublishState();
        }

        public void ChooseMale() => Submit(Gender.Male);
        public void ChooseFemale() => Submit(Gender.Female);

        private void Submit(Gender gender)
        {
            if (!isActiveAndEnabled) return;
            round.Tick(Time.realtimeSinceStartupAsDouble);
            if (!round.IsPlaying)
            {
                CancelPlayback();
                PublishState();
                return;
            }
            if (!CanAcceptInput) return;
            inputReady = false; // Consume this name before events or another button can run.
            float answeredSpeed = round.PlaybackSpeed; // Submit prepares the next tier, including a reset on failure.
            AnswerResult result = round.Submit(gender, Time.realtimeSinceStartupAsDouble);
            if (result == AnswerResult.Progress)
            {
                playback = StartCoroutine(AllowNextInput());
                PublishState();
                return;
            }
            if (result == AnswerResult.Correct || result == AnswerResult.Incorrect) Answered?.Invoke(result);
            BeginName(false);
            if (round.IsPlaying && (result == AnswerResult.Correct || result == AnswerResult.Incorrect))
                answerAudio.Play(result, answeredSpeed);
            PublishState();
        }

        private void BeginName(bool deferFirstFrame = true)
        {
            CancelPlayback();
            round.Tick(Time.realtimeSinceStartupAsDouble);
            round.ResetInputs();
            if (round.IsPlaying) playback = StartCoroutine(PlayCurrentName(deferFirstFrame));
            PublishState();
        }

        private IEnumerator PlayCurrentName(bool deferFirstFrame)
        {
            // Even silent entries yield once, so two queued clicks cannot answer two names.
            if (deferFirstFrame) yield return null;
            for (int i = 0; i < round.Sequence.Count; i++)
            {
                round.Tick(Time.realtimeSinceStartupAsDouble);
                if (!round.IsPlaying) break;
                PlaybackIndex = i;
                PublishState();
                yield return audioPlayer.PlayAndWait(round.Sequence[i].AudioClip, round.PlaybackSpeed);
                // Only between names. The first clip and final input have no extra delay.
                // This belongs to the same coroutine that expiry/restart cancels.
                if (i + 1 < round.Sequence.Count && round.AudioInterval > 0f)
                    yield return new WaitForSecondsRealtime(round.AudioInterval);
            }
            PlaybackIndex = -1;
            CompleteInputWait();
        }

        private IEnumerator AllowNextInput()
        {
            // One-frame duplicate-event guard, with no judgement or feedback.
            yield return null;
            CompleteInputWait();
        }

        private void CompleteInputWait()
        {
            round.Tick(Time.realtimeSinceStartupAsDouble);
            playback = null;
            inputReady = round.IsPlaying;
            PublishState();
        }

        private void CancelPlayback()
        {
            inputReady = false;
            PlaybackIndex = -1;
            if (playback != null) StopCoroutine(playback);
            playback = null;
            if (audioPlayer != null) audioPlayer.Stop();
            if (answerAudio != null) answerAudio.Stop();
        }

        private void OnDisable()
        {
            completionPending = false; // Abandoning a game is not a normal completion.
            CancelPlayback();
            if (round != null) PublishState();
        }

        private void OnEnable()
        {
            if (round != null && round.IsPlaying) BeginName();
        }

        private void PublishState()
        {
            if (completionPending && !round.IsPlaying && round.RemainingSeconds <= 0)
            {
                completionPending = false; // Consume before callbacks, including immediate restart.
                Completed?.Invoke(GameNumber, round.Score);
            }
            displayedSeconds = (int)Math.Ceiling(round.RemainingSeconds);
            StateChanged?.Invoke();
        }
    }
}

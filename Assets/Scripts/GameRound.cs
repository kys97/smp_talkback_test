using System;
using System.Collections.Generic;

namespace NamnyeoChilse
{
    public enum AnswerResult { Ignored, Correct, Incorrect, Progress }

    // The caller supplies a monotonic clock, allowing exact deadline tests.
    public sealed class GameRound
    {
        public const double DurationSeconds = 60;
        private readonly NameManager names;
        private readonly ScoringSettings scoring;
        private readonly SequenceSettings sequenceSettings;
        private IReadOnlyList<NameData> sequence = Array.Empty<NameData>();
        private IReadOnlyList<Gender> expectedAnswers = Array.Empty<Gender>();
        private readonly List<Gender> inputHistory = new List<Gender>();
        private double deadline;

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int Multiplier => scoring.GetMultiplier(Combo);
        public int LastAwardedScore { get; private set; }
        public double RemainingSeconds { get; private set; }
        public bool IsPlaying { get; private set; }
        public IReadOnlyList<NameData> Sequence => sequence;
        public IReadOnlyList<Gender> ExpectedAnswers => expectedAnswers;
        public int InputIndex => inputHistory.Count;
        public float AudioInterval { get; private set; }
        public float PlaybackSpeed { get; private set; }
        public NameData CurrentName => sequence.Count == 0 ? default : sequence[InputIndex];

        public GameRound(NameManager names, ScoringSettings scoring = null, SequenceSettings sequenceSettings = null)
        {
            this.names = names ?? throw new ArgumentNullException(nameof(names));
            this.scoring = (scoring ?? new ScoringSettings()).Snapshot();
            this.sequenceSettings = (sequenceSettings ?? new SequenceSettings()).Snapshot();
        }

        public void Start(double now)
        {
            Score = 0;
            Combo = 0;
            LastAwardedScore = 0;
            RemainingSeconds = DurationSeconds;
            deadline = now + DurationSeconds;
            CreateSequence();
            IsPlaying = true;
        }

        public void Tick(double now)
        {
            if (!IsPlaying) return;
            RemainingSeconds = Math.Max(0, Math.Min(DurationSeconds, deadline - now));
            if (RemainingSeconds <= 0)
            {
                IsPlaying = false;
                inputHistory.Clear();
                sequence = Array.Empty<NameData>();
                expectedAnswers = Array.Empty<Gender>();
            }
        }

        public AnswerResult Submit(Gender gender, double now)
        {
            // Check here too: the UI event can arrive before Update on the expiry frame.
            Tick(now);
            if (!IsPlaying || (gender != Gender.Male && gender != Gender.Female))
                return AnswerResult.Ignored;

            inputHistory.Add(gender);
            LastAwardedScore = 0;
            if (InputIndex < expectedAnswers.Count) return AnswerResult.Progress;
            bool correct = true;
            for (int i = 0; i < expectedAnswers.Count; i++)
                if (inputHistory[i] != expectedAnswers[i]) correct = false;
            if (correct)
            {
                Combo++;
                LastAwardedScore = scoring.BaseScore * Multiplier;
                Score += LastAwardedScore;
            }
            else Combo = 0;
            CreateSequence();
            return correct ? AnswerResult.Correct : AnswerResult.Incorrect;
        }

        public void ResetInputs() => inputHistory.Clear();

        private void CreateSequence()
        {
            AudioInterval = sequenceSettings.GetAudioInterval(Combo);
            PlaybackSpeed = sequenceSettings.GetPlaybackSpeed(Combo);
            NameData[] selected = names.NextSequence(sequenceSettings.GetNameCount(Combo));
            var answers = new Gender[selected.Length];
            for (int i = 0; i < selected.Length; i++) answers[i] = selected[i].Gender;
            sequence = Array.AsReadOnly(selected);
            expectedAnswers = Array.AsReadOnly(answers);
            inputHistory.Clear();
        }
    }
}

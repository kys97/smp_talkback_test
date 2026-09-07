using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NamnyeoChilse.Tests
{
    public sealed class AnswerPlaybackTests
    {
        [Test]
        public void FeedbackResourcesAreAudioClipsAndDefaultIntervalsAreZero()
        {
            Assert.That(Resources.Load<AudioClip>("Feedback/sfx_correct"), Is.Not.Null);
            Assert.That(Resources.Load<AudioClip>("Feedback/sfx_line_wrong"), Is.Not.Null);
            foreach (int combo in new[] { 0, 4, 5, 9, 10, 14, 15, 50 })
                Assert.That(new SequenceSettings().GetAudioInterval(combo), Is.Zero);
            Assert.That(new SequenceTier(0, 1, 0, float.NaN).PlaybackSpeed, Is.EqualTo(1));
            Assert.That(new SequenceTier(0, 1, 0, 0).PlaybackSpeed, Is.EqualTo(1));
            Assert.That(new SequenceTier(0, 1, 0, 5).PlaybackSpeed, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator SequenceStartsImmediatelyAlongsideFeedback()
        {
            var owner = new GameObject("Speed and feedback test");
            var clip = AudioClip.Create("테스트수", 4000, 1, 8000, false);
            try
            {
                var game = owner.AddComponent<GameManager>();
                game.ConfigureNames(new[] { new NameData("영수", Gender.Male, clip), new NameData("철수", Gender.Male, clip) },
                    null, new SequenceSettings(new SequenceTier(0, 2, 0, 1.6f)));
                var name = owner.GetComponent<AudioSource>();
                var feedback = owner.transform.Find("AnswerAudio").GetComponent<AudioSource>();
                game.StartGame();
                double started = Time.realtimeSinceStartupAsDouble;
                double timeout = started + 4;
                int lastIndex = -1;
                while (!game.CanAcceptInput && Time.realtimeSinceStartupAsDouble < timeout)
                {
                    if (name.isPlaying)
                    {
                        Assert.That(name.pitch, Is.EqualTo(1.6f).Within(0.001));
                        lastIndex = game.PlaybackIndex;
                    }
                    Assert.That(feedback.isPlaying, Is.False);
                    yield return null;
                }
                Assert.That(game.CanAcceptInput, Is.True);
                Assert.That(lastIndex, Is.EqualTo(1));
                Assert.That(Time.realtimeSinceStartupAsDouble - started, Is.LessThan(1.1), "Two 0.5-second clips at 1.6x, no inserted interval.");
                game.ChooseMale(); // Partial answer, no score yet.
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(feedback.isPlaying, Is.False);
                Assert.That(feedback.clip, Is.Null);
                Assert.That(game.Round.InputIndex, Is.EqualTo(1));
                Assert.That(game.Round.Score, Is.Zero);
                yield return GameFlowTests.WaitForInput(game);
                game.ChooseMale();
                yield return WaitForFeedback(feedback);
                Assert.That(feedback.clip.name, Is.EqualTo("sfx_correct"));
                Assert.That(feedback.pitch, Is.EqualTo(1.6f).Within(0.001));
                Assert.That(game.Round.Score, Is.EqualTo(100));
                Assert.That(name.isPlaying, Is.True, "Next name starts alongside feedback.");
                while (feedback.isPlaying)
                {
                    Assert.That(game.CanAcceptInput, Is.False);
                    game.ChooseMale();
                    Assert.That(game.Round.Score, Is.EqualTo(100));
                    yield return null;
                }
                yield return GameFlowTests.WaitForInput(game);
                game.ChooseFemale();
                yield return GameFlowTests.WaitForInput(game);
                Assert.That(game.Round.Combo, Is.EqualTo(1));
                Assert.That(feedback.isPlaying, Is.False);
                game.ChooseMale();
                yield return WaitForFeedback(feedback);
                Assert.That(feedback.clip.name, Is.EqualTo("sfx_line_wrong"));
                Assert.That(game.Round.Combo, Is.Zero);
                game.StartGame();
                Assert.That(feedback.isPlaying, Is.False);
                Assert.That(feedback.clip, Is.Null);
                Assert.That(game.Round.Score, Is.Zero);
                yield return GameFlowTests.WaitForInput(game);
                game.ChooseFemale();
                yield return GameFlowTests.WaitForInput(game);
                game.ChooseMale();
                yield return WaitForFeedback(feedback);
                game.Round.Start(Time.realtimeSinceStartupAsDouble - 60);
                yield return null;
                yield return new WaitForSecondsRealtime(0.8f);
                Assert.That(game.Round.IsPlaying, Is.False);
                Assert.That(game.CanAcceptInput, Is.False);
                Assert.That(name.isPlaying || feedback.isPlaying, Is.False);
                Assert.That(game.PlaybackIndex, Is.EqualTo(-1));
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(clip); }
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EveryWrongPositionWaitsUntilAllInputsAndJudgesOnce(int count)
        {
            for (int wrongIndex = 0; wrongIndex < count; wrongIndex++)
            {
                var round = new GameRound(new NameManager(new System.Random(1)), null,
                    new SequenceSettings(new SequenceTier(0, count)));
                round.Start(0);
                ComboScoringTests.CompleteSequence(round, 1);
                var question = round.ExpectedAnswers;
                for (int i = 0; i < count; i++)
                {
                    Gender answer = i == wrongIndex ? (question[i] == Gender.Male ? Gender.Female : Gender.Male) : question[i];
                    Assert.That(round.Submit(answer, 2), Is.EqualTo(i == count - 1 ? AnswerResult.Incorrect : AnswerResult.Progress));
                    Assert.That(round.Combo, Is.EqualTo(i == count - 1 ? 0 : 1));
                    Assert.That(round.Score, Is.EqualTo(100));
                }
            }
        }

        [UnityTest]
        public IEnumerator WrongFeedbackUsesOldSpeedThenNextQuestionResetsImmediately()
        {
            var owner = new GameObject("Feedback tier snapshot");
            var clip = AudioClip.Create("Tier", 2400, 1, 8000, false);
            try
            {
                var game = owner.AddComponent<GameManager>();
                game.ConfigureNames(new[] { new NameData("영수", Gender.Male, clip), new NameData("철수", Gender.Male, clip),
                    new NameData("민수", Gender.Male, clip), new NameData("경수", Gender.Male, clip) });
                game.StartGame();
                for (int i = 0; i < 15; i++) ComboScoringTests.CompleteSequence(game.Round, Time.realtimeSinceStartupAsDouble);
                int judgements = 0;
                game.Answered += _ => judgements++;
                var feedback = owner.transform.Find("AnswerAudio").GetComponent<AudioSource>();
                var name = owner.GetComponent<AudioSource>();
                for (int i = 0; i < 4; i++)
                {
                    yield return GameFlowTests.WaitForInput(game);
                    Assert.That(judgements, Is.Zero);
                    Assert.That(feedback.isPlaying, Is.False);
                    if (i == 0) game.ChooseFemale(); else game.ChooseMale();
                }
                Assert.That(judgements, Is.EqualTo(1));
                Assert.That(game.Round.Combo, Is.Zero);
                yield return WaitForFeedback(feedback);
                Assert.That(feedback.pitch, Is.EqualTo(1.6f).Within(0.001));
                Assert.That(feedback.isPlaying, Is.True);
                double ended = Time.realtimeSinceStartupAsDouble;
                while (!name.isPlaying && Time.realtimeSinceStartupAsDouble - ended < 1) yield return null;
                Assert.That(name.isPlaying, Is.True);
                Assert.That(Time.realtimeSinceStartupAsDouble - ended, Is.LessThan(0.1));
                Assert.That(name.pitch, Is.EqualTo(1));
                Assert.That(game.Round.Sequence.Count, Is.EqualTo(1));
                yield return GameFlowTests.WaitForInput(game);
                Assert.That(judgements, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(clip); }
        }

        private static IEnumerator WaitForFeedback(AudioSource source)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (!source.isPlaying && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(source.isPlaying, Is.True);
        }
    }
}

using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NamnyeoChilse.Tests
{
    public sealed class AudioIntervalTests
    {
        [TestCase(0, 1f)]
        [TestCase(4, 1f)]
        [TestCase(5, 1.2f)]
        [TestCase(9, 1.2f)]
        [TestCase(10, 1.4f)]
        [TestCase(14, 1.4f)]
        [TestCase(15, 1.6f)]
        [TestCase(30, 1.6f)]
        public void DefaultSpeedMatchesCombo(int combo, float expected)
        {
            Assert.That(new SequenceSettings().GetPlaybackSpeed(combo), Is.EqualTo(expected));
        }

        [Test]
        public void RoundSnapshotsIntervalAndResetsOnWrongAndRestart()
        {
            var round = new GameRound(new NameManager(new System.Random(1)));
            round.Start(0);
            for (int i = 0; i < 15; i++) ComboScoringTests.CompleteSequence(round, i);
            Assert.That(round.PlaybackSpeed, Is.EqualTo(1.6f));
            round.Submit(round.ExpectedAnswers[0], 20);
            Assert.That(round.PlaybackSpeed, Is.EqualTo(1.6f));
            var wrong = round.ExpectedAnswers[1] == Gender.Male ? Gender.Female : Gender.Male;
            round.Submit(wrong, 20);
            while (round.InputIndex > 0) round.Submit(round.CurrentName.Gender, 20);
            Assert.That(round.PlaybackSpeed, Is.EqualTo(1f));
            Assert.That(round.Sequence.Count, Is.EqualTo(1));
            for (int i = 0; i < 5; i++) ComboScoringTests.CompleteSequence(round, 21);
            Assert.That(round.PlaybackSpeed, Is.EqualTo(1.2f));
            round.Start(30);
            Assert.That(round.PlaybackSpeed, Is.EqualTo(1f));
            var settings = new SequenceSettings(new SequenceTier(5, 3, 0.23f), new SequenceTier(0, 2, 0.85f));
            settings = JsonUtility.FromJson<SequenceSettings>(JsonUtility.ToJson(settings));
            Assert.That(settings.GetAudioInterval(5), Is.EqualTo(0.23f));
            Assert.That(settings.GetNameCount(5), Is.EqualTo(3));
            Assert.That(new SequenceTier(0, 1, -1).AudioInterval, Is.Zero);
        }

        private static GameManager CreateGame(GameObject owner, AudioClip clip, int count, float interval)
        {
            if (UnityEngine.Object.FindAnyObjectByType<AudioListener>() == null) owner.AddComponent<AudioListener>();
            GameManager game = owner.AddComponent<GameManager>();
            game.ConfigureNames(new[] { new NameData("영수", Gender.Male, clip), new NameData("민희", Gender.Female, clip) },
                null, new SequenceSettings(new SequenceTier(0, count, interval)));
            game.StartGame();
            return game;
        }

        private static IEnumerator WaitForFirstClipToFinish(GameManager game, AudioSource source)
        {
            double timeout = Time.realtimeSinceStartupAsDouble + 5;
            while (!source.isPlaying && Time.realtimeSinceStartupAsDouble < timeout) yield return null;
            Assert.That(source.isPlaying, Is.True);
            while (source.isPlaying && Time.realtimeSinceStartupAsDouble < timeout) yield return null;
            Assert.That(source.isPlaying, Is.False);
            Assert.That(game.PlaybackIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator WaitsAfterActualCompletionWithInputLockedAndPitchUnchanged()
        {
            var owner = new GameObject("Interval timing");
            AudioClip clip = AudioClip.Create("Interval", 1600, 1, 8000, false);
            try
            {
                GameManager game = CreateGame(owner, clip, 2, 0.35f);
                AudioSource source = owner.GetComponent<AudioSource>();
                yield return WaitForFirstClipToFinish(game, source);
                double ended = Time.realtimeSinceStartupAsDouble;
                double timeout = ended + 3;
                while (game.PlaybackIndex == 0 && Time.realtimeSinceStartupAsDouble < timeout)
                {
                    Assert.That(game.CanAcceptInput, Is.False);
                    game.ChooseMale();
                    game.ChooseFemale();
                    Assert.That(game.Round.InputIndex, Is.Zero);
                    yield return null;
                }
                Assert.That(game.PlaybackIndex, Is.EqualTo(1));
                Assert.That(Time.realtimeSinceStartupAsDouble - ended, Is.InRange(0.32, 0.65));
                Assert.That(source.pitch, Is.EqualTo(1));
                Assert.That(source.isPlaying, Is.True);
                while (source.isPlaying && Time.realtimeSinceStartupAsDouble < timeout) yield return null;
                double finalEnded = Time.realtimeSinceStartupAsDouble;
                yield return GameFlowTests.WaitForInput(game);
                Assert.That(Time.realtimeSinceStartupAsDouble - finalEnded, Is.LessThan(0.2), "No interval after final clip.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [UnityTest]
        public IEnumerator RestartAndExpiryCancelIntervalBeforeNextClip()
        {
            var owner = new GameObject("Interval cancellation");
            AudioClip clip = AudioClip.Create("Interval", 800, 1, 8000, false);
            try
            {
                GameManager game = CreateGame(owner, clip, 2, 0.5f);
                AudioSource source = owner.GetComponent<AudioSource>();
                yield return WaitForFirstClipToFinish(game, source);
                yield return new WaitForSecondsRealtime(0.2f);
                game.StartGame();
                Assert.That(game.PlaybackIndex, Is.EqualTo(-1));
                Assert.That(source.isPlaying, Is.False);
                yield return WaitForFirstClipToFinish(game, source);
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.That(game.PlaybackIndex, Is.Zero, "Old interval must not start a queued clip.");
                Assert.That(game.CanAcceptInput, Is.False);
                game.Round.Start(Time.realtimeSinceStartupAsDouble - 59.95);
                yield return new WaitForSecondsRealtime(0.65f);
                Assert.That(game.Round.IsPlaying, Is.False);
                Assert.That(source.isPlaying, Is.False);
                Assert.That(source.clip, Is.Null);
                Assert.That(game.PlaybackIndex, Is.EqualTo(-1));
                Assert.That(game.CanAcceptInput, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
    }
}

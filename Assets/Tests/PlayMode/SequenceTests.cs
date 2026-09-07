using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NamnyeoChilse.Tests
{
    public sealed class SequenceTests
    {
        [Test]
        public void DefaultProgressionScoresOnlyCompletedSequencesAndFailureReturnsToOne()
        {
            var round = new GameRound(new NameManager(new System.Random(2)));
            round.Start(0);
            for (int combo = 0; combo <= 15; combo++)
            {
                int count = Math.Min(4, combo / 5 + 1);
                Assert.That(round.Sequence.Count, Is.EqualTo(count));
                var seen = new HashSet<string>();
                for (int i = 0; i < count; i++)
                {
                    Assert.That(seen.Add(round.Sequence[i].Name), Is.True);
                    Assert.That(round.ExpectedAnswers[i], Is.EqualTo(round.Sequence[i].Gender));
                }
                int previousScore = round.Score;
                for (int i = 0; i < count; i++)
                {
                    var result = round.Submit(round.ExpectedAnswers[i], combo);
                    if (i < count - 1)
                    {
                        Assert.That(result, Is.EqualTo(AnswerResult.Progress));
                        Assert.That(round.Combo, Is.EqualTo(combo));
                        Assert.That(round.Score, Is.EqualTo(previousScore));
                        Assert.That(round.InputIndex, Is.EqualTo(i + 1));
                    }
                    else Assert.That(result, Is.EqualTo(AnswerResult.Correct));
                }
            }
            Assert.That(round.Score, Is.EqualTo(3700));
            round.Submit(round.ExpectedAnswers[0], 20);
            Gender wrong = round.ExpectedAnswers[1] == Gender.Male ? Gender.Female : Gender.Male;
            Assert.That(round.Submit(wrong, 20), Is.EqualTo(AnswerResult.Progress));
            Assert.That(round.Combo, Is.EqualTo(16));
            round.Submit(round.CurrentName.Gender, 20);
            Assert.That(round.Submit(round.CurrentName.Gender, 20), Is.EqualTo(AnswerResult.Incorrect));
            Assert.That(round.Combo, Is.Zero);
            Assert.That(round.Score, Is.EqualTo(3700));
            Assert.That(round.InputIndex, Is.Zero);
            Assert.That(round.Sequence.Count, Is.EqualTo(1));
        }

        [Test]
        public void SmallCatalogAndSerializedSettingsStayBounded()
        {
            var names = new NameManager(new[] { new NameData("영수", Gender.Male),
                new NameData("영수", Gender.Male), new NameData("민희", Gender.Female) }, new System.Random(3));
            NameData[] selected = names.NextSequence(int.MaxValue);
            Assert.That(selected.Length, Is.EqualTo(2));
            Assert.That(selected[0].Name, Is.Not.EqualTo(selected[1].Name));
            var settings = new SequenceSettings(new SequenceTier(2, 7), new SequenceTier(0, 3));
            settings = JsonUtility.FromJson<SequenceSettings>(JsonUtility.ToJson(settings));
            Assert.That(settings.GetNameCount(0), Is.EqualTo(3));
            Assert.That(settings.GetNameCount(2), Is.EqualTo(7));
            Assert.That(new SequenceSettings(Array.Empty<SequenceTier>()).GetNameCount(20), Is.EqualTo(1));
            var round = new GameRound(names, null, settings);
            round.Start(0);
            Assert.That(round.Sequence.Count, Is.EqualTo(2));
            round.Submit(round.ExpectedAnswers[0], 1);
            round.Tick(60);
            Assert.That(round.InputIndex, Is.Zero);
            Assert.That(round.Sequence, Is.Empty);
            Assert.That(round.ExpectedAnswers, Is.Empty);
            round.Start(100);
            Assert.That(round.InputIndex, Is.Zero);
            Assert.That(round.Score, Is.Zero);
            Assert.That(round.Sequence.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RestartAndExpiryClearPartialInputAndPendingSequence()
        {
            var owner = new GameObject("Sequence cancellation");
            AudioClip clip = AudioClip.Create("Cancellation", 2400, 1, 8000, false);
            try
            {
                GameManager game = owner.AddComponent<GameManager>();
                game.ConfigureNames(new[] { new NameData("일수", Gender.Male, clip),
                    new NameData("이희", Gender.Female, clip), new NameData("삼수", Gender.Male, clip) },
                    null, new SequenceSettings(new SequenceTier(0, 3)));
                game.StartGame();
                AudioSource source = owner.GetComponent<AudioSource>();
                double timeout = Time.realtimeSinceStartupAsDouble + 5;
                while (game.PlaybackIndex != 1 && Time.realtimeSinceStartupAsDouble < timeout) yield return null;
                Assert.That(game.PlaybackIndex, Is.EqualTo(1));
                game.StartGame();
                Assert.That(source.isPlaying, Is.False);
                Assert.That(game.PlaybackIndex, Is.EqualTo(-1));
                Assert.That(game.Round.InputIndex, Is.Zero);
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.That(game.CanAcceptInput, Is.False, "Restart must wait for all three new clips.");
                yield return GameFlowTests.WaitForInput(game);
                if (game.Round.ExpectedAnswers[0] == Gender.Male) game.ChooseMale(); else game.ChooseFemale();
                Assert.That(game.Round.InputIndex, Is.EqualTo(1));
                game.StartGame();
                Assert.That(game.Round.InputIndex, Is.Zero);
                Assert.That(game.Round.Combo, Is.Zero);
                timeout = Time.realtimeSinceStartupAsDouble + 5;
                while (game.PlaybackIndex != 1 && Time.realtimeSinceStartupAsDouble < timeout) yield return null;
                Assert.That(game.PlaybackIndex, Is.EqualTo(1));
                game.Round.Start(Time.realtimeSinceStartupAsDouble - 59.9);
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(game.Round.IsPlaying, Is.False);
                Assert.That(game.Round.Sequence, Is.Empty);
                Assert.That(game.Round.ExpectedAnswers, Is.Empty);
                Assert.That(game.Round.InputIndex, Is.Zero);
                Assert.That(source.isPlaying, Is.False);
                Assert.That(source.clip, Is.Null);
                yield return new WaitForSecondsRealtime(0.7f);
                Assert.That(source.isPlaying, Is.False, "Pending third clip must not start after expiry.");
                Assert.That(game.CanAcceptInput, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [UnityTest]
        public IEnumerator AllAudioPlaysInOrderBeforeOrderedInputThroughFourNames()
        {
            var owner = new GameObject("Sequence integration");
            var clips = new AudioClip[4];
            try
            {
                var entries = new NameData[4];
                for (int i = 0; i < 4; i++)
                {
                    clips[i] = AudioClip.Create("Sequence " + i, 800 + i * 400, 1, 8000, false);
                    entries[i] = new NameData("이름" + i, i % 2 == 0 ? Gender.Male : Gender.Female, clips[i]);
                }
                GameManager game = owner.AddComponent<GameManager>();
                game.ConfigureNames(entries);
                game.StartGame();
                AudioSource source = owner.GetComponent<AudioSource>();
                for (int combo = 0; combo <= 15; combo++)
                {
                    int count = Math.Min(4, combo / 5 + 1);
                    Assert.That(game.Round.Sequence.Count, Is.EqualTo(count));
                    IReadOnlyList<NameData> question = game.Round.Sequence;
                    int observedIndex = -1;
                    double timeout = Time.realtimeSinceStartupAsDouble + 10;
                    while (!game.CanAcceptInput && Time.realtimeSinceStartupAsDouble < timeout)
                    {
                        if (source.isPlaying)
                        {
                            Assert.That(game.PlaybackIndex, Is.InRange(observedIndex < 0 ? 0 : observedIndex, observedIndex + 1));
                            observedIndex = game.PlaybackIndex;
                            Assert.That(source.clip, Is.SameAs(question[observedIndex].AudioClip));
                        }
                        game.ChooseMale();
                        game.ChooseFemale();
                        Assert.That(game.Round.InputIndex, Is.Zero);
                        yield return null;
                    }
                    Assert.That(game.CanAcceptInput, Is.True);
                    Assert.That(observedIndex, Is.EqualTo(count - 1));
                    for (int i = 0; i < count; i++)
                    {
                        yield return GameFlowTests.WaitForInput(game);
                        Assert.That(source.isPlaying, Is.False);
                        if (question[i].Gender == Gender.Male) game.ChooseMale();
                        else game.ChooseFemale();
                        game.ChooseMale();
                        Assert.That(game.Round.Combo, Is.EqualTo(i == count - 1 ? combo + 1 : combo));
                    }
                }
                yield return GameFlowTests.WaitForInput(game);
                Gender first = game.Round.ExpectedAnswers[0];
                if (first == Gender.Male) game.ChooseMale(); else game.ChooseFemale();
                yield return GameFlowTests.WaitForInput(game);
                if (game.Round.ExpectedAnswers[1] == Gender.Male) game.ChooseFemale(); else game.ChooseMale();
                Assert.That(game.Round.Combo, Is.EqualTo(16));
                while (game.Round.InputIndex > 0)
                {
                    yield return GameFlowTests.WaitForInput(game);
                    if (game.Round.CurrentName.Gender == Gender.Male) game.ChooseMale(); else game.ChooseFemale();
                }
                Assert.That(game.Round.Combo, Is.Zero);
                Assert.That(game.Round.Sequence.Count, Is.EqualTo(1));
                Assert.That(game.Round.Score, Is.EqualTo(3700));
                game.StartGame();
                Assert.That(game.Round.InputIndex, Is.Zero);
                Assert.That(source.isPlaying, Is.False);
                Assert.That(game.PlaybackIndex, Is.EqualTo(-1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                foreach (AudioClip clip in clips) if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
            }
        }
    }
}

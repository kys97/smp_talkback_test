using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NamnyeoChilse.Tests
{
    public sealed class GameFlowTests
    {
        [UnityTest]
        public IEnumerator ConfiguredEntryPreservesClipThroughSerializationAndGameManager()
        {
            AudioClip clip = AudioClip.Create("Test name reference", 32, 1, 8000, false);
            var owner = new GameObject("Configured game test");
            try
            {
                var entry = new NameData("테스트희", Gender.Female, clip);
                NameData restored = JsonUtility.FromJson<NameData>(JsonUtility.ToJson(entry));
                Assert.That(restored.Name, Is.EqualTo("테스트희"));
                Assert.That(restored.Gender, Is.EqualTo(Gender.Female));
                Assert.That(restored.AudioClip, Is.SameAs(clip));

                GameManager game = owner.AddComponent<GameManager>();
                game.ConfigureNames(new[] { restored });
                game.StartGame();
                Assert.That(game.Round.CurrentName.Name, Is.EqualTo("테스트희"));
                Assert.That(game.Round.CurrentName.Gender, Is.EqualTo(Gender.Female));
                Assert.That(game.Round.CurrentName.AudioClip, Is.SameAs(clip));
                yield return WaitForInput(game);
                game.ChooseFemale();
                Assert.That(game.Round.Score, Is.EqualTo(100));
                Assert.That(game.Round.CurrentName.AudioClip, Is.SameAs(clip));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void MissingClipIsValidAndBlankNamesAreSkipped()
        {
            var names = new NameManager(new[]
            {
                new NameData("  ", Gender.Female),
                new NameData("등록수", Gender.Male)
            }, new System.Random(5));
            var round = new GameRound(names);
            round.Start(0);
            Assert.That(round.CurrentName.Name, Is.EqualTo("등록수"));
            Assert.That(round.CurrentName.AudioClip, Is.Null);
            Assert.That(round.Submit(Gender.Male, 1), Is.EqualTo(AnswerResult.Correct));
            Assert.That(round.CurrentName.AudioClip, Is.Null);
        }

        [Test]
        public void EmptyCatalogFallsBackToPlayableDefaults()
        {
            var names = new NameManager(Array.Empty<NameData>(), new System.Random(5));
            NameData entry = names.Next();
            Assert.That(entry.Name, Is.Not.Null.And.Not.Empty);
            Assert.That(entry.AudioClip, Is.Null);
        }

        [Test]
        public void NameDataFollowsSuffixRuleAndIncludesBothGenders()
        {
            var names = new NameManager(new System.Random(123));
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 300; i++)
            {
                NameData name = names.Next();
                Assert.That(name.Name, Does.EndWith(name.Gender == Gender.Male ? "수" : "희"));
                seen.Add(name.Name);
            }
            Assert.That(seen.Count, Is.EqualTo(10));
        }

        [Test]
        public void ScoringDeadlineAndRestartRespectRoundContract()
        {
            var round = new GameRound(new NameManager(new System.Random(7)));
            Assert.That(round.Submit(Gender.Male, 0), Is.EqualTo(AnswerResult.Ignored));
            round.Start(100);
            Assert.That(round.RemainingSeconds, Is.EqualTo(60));
            Assert.That(round.Submit(round.CurrentName.Gender, 101), Is.EqualTo(AnswerResult.Correct));
            Assert.That(round.Score, Is.EqualTo(100));
            Gender wrong = round.CurrentName.Gender == Gender.Male ? Gender.Female : Gender.Male;
            Assert.That(round.Submit(wrong, 102), Is.EqualTo(AnswerResult.Incorrect));
            Assert.That(round.Score, Is.EqualTo(100));
            Assert.That(round.Submit(round.CurrentName.Gender, 159.999), Is.EqualTo(AnswerResult.Correct));
            Assert.That(round.Submit(round.CurrentName.Gender, 160), Is.EqualTo(AnswerResult.Ignored));
            Assert.That(round.Score, Is.EqualTo(200));
            Assert.That(round.IsPlaying, Is.False);
            Assert.That(round.RemainingSeconds, Is.Zero);
            round.Tick(300);
            Assert.That(round.RemainingSeconds, Is.Zero);
            round.Start(300);
            Assert.That(round.IsPlaying, Is.True);
            Assert.That(round.Score, Is.Zero);
            Assert.That(round.RemainingSeconds, Is.EqualTo(60));
        }

        [UnityTest]
        public IEnumerator SceneButtonsCompleteRealSixtySecondRoundAndRestart()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[UGS\] operation=인증/닉네임 조회 result=SUCCESS"));
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return GameFlowTests.WaitForMain();
            GameObject.Find("StartGameButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Round.CurrentName.AudioClip, Is.Not.Null, "Scene must use the resource catalog.");
            Button male = GameObject.Find("MaleButton").GetComponent<Button>();
            Button female = GameObject.Find("FemaleButton").GetComponent<Button>();
            Text name = GameObject.Find("Name").GetComponent<Text>();
            Assert.That(name.text, Is.EqualTo(game.Round.CurrentName.Name));
            Assert.That(name.font.HasCharacter('희'), Is.True, "Bundled Korean font must contain Hangul.");
            Assert.That(game.CanAcceptInput, Is.False);
            male.onClick.Invoke();
            female.onClick.Invoke();
            Assert.That(game.Round.Score, Is.Zero);
            yield return WaitForInput(game);
            Button correct = game.Round.CurrentName.Gender == Gender.Male ? male : female;
            correct.onClick.Invoke();
            Assert.That(game.Round.Score, Is.EqualTo(100));
            Assert.That(name.text, Is.EqualTo(game.Round.CurrentName.Name));
            yield return WaitForInput(game);
            Button wrong = game.Round.CurrentName.Gender == Gender.Male ? female : male;
            wrong.onClick.Invoke();
            Assert.That(game.Round.Score, Is.EqualTo(100));
            Assert.That(GameObject.Find("Feedback").GetComponent<Text>().text, Is.EqualTo("오답"));

            // Exercise actual MonoBehaviour time/lifecycle, not an accelerated duration.
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[UGS\] operation=점수 업로드 result=SUCCESS.*submitted=100 serverBest=100"));
            LogAssert.Expect(LogType.Log, "Leaderboard 점수 등록 완료: 이번 100, 서버 최고 100");
            yield return new WaitForSecondsRealtime(60.1f);
            Assert.That(game.Round.IsPlaying, Is.False);
            Assert.That(male.interactable || female.interactable, Is.False);
            male.onClick.Invoke();
            female.onClick.Invoke();
            Assert.That(game.Round.Score, Is.EqualTo(100));
            Assert.That(GameObject.Find("Result").GetComponent<Text>().text, Does.Contain("최종 점수 100점"));
            GameObject.Find("RestartButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Round.IsPlaying, Is.True);
            Assert.That(game.Round.Score, Is.Zero);
            Assert.That(game.Round.RemainingSeconds, Is.InRange(59.9, 60));
            Assert.That(male.interactable || female.interactable, Is.False);
            yield return WaitForInput(game);
            Assert.That(male.interactable && female.interactable, Is.True);

            GameUI ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
            ui.enabled = false;
            ui.enabled = true;
            correct = game.Round.CurrentName.Gender == Gender.Male ? male : female;
            correct.onClick.Invoke();
            Assert.That(game.Round.Score, Is.EqualTo(100), "Re-enable must not duplicate listeners.");
            LogAssert.NoUnexpectedReceived();
        }

        internal static IEnumerator WaitForInput(GameManager game)
        {
            double timeout = Time.realtimeSinceStartupAsDouble + 10;
            while (!game.CanAcceptInput && Time.realtimeSinceStartupAsDouble < timeout)
                yield return null;
            Assert.That(game.CanAcceptInput, Is.True, "Audio completion must unlock input within 10 seconds.");
        }

        internal static IEnumerator WaitForMain()
        {
            double timeout = Time.realtimeSinceStartupAsDouble + 5;
            while (GameObject.Find("StartGameButton") == null && Time.realtimeSinceStartupAsDouble < timeout)
                yield return null;
            Assert.That(GameObject.Find("StartGameButton"), Is.Not.Null, "Fake authentication must complete before menu interaction.");
        }
    }
}

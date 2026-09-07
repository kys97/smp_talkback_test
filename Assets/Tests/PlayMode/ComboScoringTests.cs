using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NamnyeoChilse.Tests
{
    public sealed class ComboScoringTests
    {
        internal static void CompleteSequence(GameRound round, double now)
        {
            int count = round.Sequence.Count;
            for (int i = 0; i < count; i++)
                Assert.That(round.Submit(round.CurrentName.Gender, now),
                    Is.EqualTo(i == count - 1 ? AnswerResult.Correct : AnswerResult.Progress));
        }

        private static GameRound CreateRound(ScoringSettings settings = null)
        {
            var round = new GameRound(new NameManager(new System.Random(1)), settings);
            round.Start(0);
            return round;
        }

        [TestCase(4, 1, 100, 400)]
        [TestCase(5, 2, 200, 600)]
        [TestCase(9, 2, 200, 1400)]
        [TestCase(10, 3, 300, 1700)]
        [TestCase(14, 3, 300, 2900)]
        [TestCase(15, 4, 400, 3300)]
        [TestCase(16, 4, 400, 3700)]
        public void AppliesMultiplierAfterIncreasingCombo(int combo, int multiplier, int award, int total)
        {
            GameRound round = CreateRound();
            for (int i = 0; i < combo; i++) CompleteSequence(round, i);
            Assert.That(round.Combo, Is.EqualTo(combo));
            Assert.That(round.Multiplier, Is.EqualTo(multiplier));
            Assert.That(round.LastAwardedScore, Is.EqualTo(award));
            Assert.That(round.Score, Is.EqualTo(total));
        }

        [Test]
        public void WrongAnswerResetsComboOnlyAndRestartResetsEverything()
        {
            GameRound round = CreateRound();
            for (int i = 0; i < 15; i++) CompleteSequence(round, i);
            Gender wrong = round.CurrentName.Gender == Gender.Male ? Gender.Female : Gender.Male;
            round.Submit(wrong, 16);
            Assert.That(round.Combo, Is.EqualTo(15));
            while (round.InputIndex > 0) round.Submit(round.CurrentName.Gender, 16);
            Assert.That(round.Combo, Is.Zero);
            Assert.That(round.Multiplier, Is.EqualTo(1));
            Assert.That(round.LastAwardedScore, Is.Zero);
            Assert.That(round.Score, Is.EqualTo(3300));
            round.Submit(round.CurrentName.Gender, 17);
            Assert.That(round.Combo, Is.EqualTo(1));
            Assert.That(round.Score, Is.EqualTo(3400));
            round.Submit(wrong, 60);
            Assert.That(round.Combo, Is.EqualTo(1), "Expired input must not change combo.");
            Assert.That(round.Score, Is.EqualTo(3400));
            round.Start(100);
            Assert.That(round.Combo, Is.Zero);
            Assert.That(round.Score, Is.Zero);
            Assert.That(round.LastAwardedScore, Is.Zero);
        }

        [Test]
        public void InspectorDataSurvivesSerializationAndSupportsCustomUnsortedTiers()
        {
            var settings = new ScoringSettings(25, new ComboTier(3, 7), new ComboTier(0, 2));
            settings = JsonUtility.FromJson<ScoringSettings>(JsonUtility.ToJson(settings));
            GameRound round = CreateRound(settings);
            for (int i = 0; i < 3; i++) CompleteSequence(round, i);
            Assert.That(round.Multiplier, Is.EqualTo(7));
            Assert.That(round.LastAwardedScore, Is.EqualTo(175));
            Assert.That(round.Score, Is.EqualTo(275));
        }

        [Test]
        public void EmptyAndInvalidTiersHaveSafeDefaults()
        {
            Assert.That(new ScoringSettings(100).GetMultiplier(20), Is.EqualTo(1));
            var settings = new ScoringSettings(-5, new ComboTier(-1, 3), new ComboTier(0, 0),
                new ComboTier(5, 2), new ComboTier(5, 4));
            Assert.That(settings.BaseScore, Is.EqualTo(1));
            Assert.That(settings.GetMultiplier(0), Is.EqualTo(1));
            Assert.That(settings.GetMultiplier(5), Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator SceneComboAndScoreFollowCorrectWrongAndRestart()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return GameFlowTests.WaitForMain();
            GameObject.Find("StartGameButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            GameManager game = Object.FindFirstObjectByType<GameManager>();
            Text combo = GameObject.Find("Combo").GetComponent<Text>();
            Assert.That(combo.text, Is.EqualTo("콤보 0 · x1"));
            for (int i = 0; i < 5; i++)
            {
                yield return GameFlowTests.WaitForInput(game);
                if (game.Round.CurrentName.Gender == Gender.Male) game.ChooseMale();
                else game.ChooseFemale();
                game.ChooseMale(); // Queued repeat during next audio must be ignored.
                Assert.That(game.Round.Combo, Is.EqualTo(i + 1));
            }
            Assert.That(combo.text, Is.EqualTo("콤보 5 · x2"));
            Assert.That(GameObject.Find("Score").GetComponent<Text>().text, Is.EqualTo("점수 600"));
            Assert.That(GameObject.Find("Feedback").GetComponent<Text>().text, Is.EqualTo("정답! +200"));
            yield return GameFlowTests.WaitForInput(game);
            if (game.Round.CurrentName.Gender == Gender.Male) game.ChooseFemale();
            else game.ChooseMale();
            Assert.That(combo.text, Is.EqualTo("콤보 5 · x2"));
            yield return GameFlowTests.WaitForInput(game);
            if (game.Round.CurrentName.Gender == Gender.Male) game.ChooseMale(); else game.ChooseFemale();
            Assert.That(combo.text, Is.EqualTo("콤보 0 · x1"));
            Assert.That(game.Round.Score, Is.EqualTo(600));
            game.StartGame();
            Assert.That(combo.text, Is.EqualTo("콤보 0 · x1"));
            Assert.That(GameObject.Find("Score").GetComponent<Text>().text, Is.EqualTo("점수 0"));
        }
    }
}

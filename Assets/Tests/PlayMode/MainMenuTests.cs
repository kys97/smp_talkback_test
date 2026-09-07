using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NamnyeoChilse.Tests
{
    public sealed class MainMenuTests
    {
        [UnityTest]
        public IEnumerator MenuScreensAndReturnKeepOnlyVisibleButtonsAccessible()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var game = Object.FindAnyObjectByType<GameManager>();
            var group = Object.FindAnyObjectByType<AccessibleButtonGroup>();
            var navigation = Object.FindAnyObjectByType<MainMenuNavigation>();
            var buttons = group.GetComponentsInChildren<Selectable>(true);
            Selectable Find(string name) => buttons.Single(b => b.name == name);
            void Visible(params string[] labels)
            {
                Canvas.ForceUpdateCanvases();
                group.RefreshButtons();
                Assert.That(buttons.Where(b => group.GetNode(b).isActive).Select(b => group.GetNode(b).label),
                    Is.EquivalentTo(labels));
            }
            Assert.That(game.enabled, Is.False);
            Assert.That(game.Round.IsPlaying, Is.False);
            Assert.That(game.GetComponent<AudioSource>().isPlaying, Is.False);
            Assert.That(GameObject.Find("NicknameDisplay").GetComponent<Text>().text, Does.Contain("미설정"));
            Visible("게임 시작", "랭킹", "닉네임 설정");
            Assert.That(group.TryActivate(Find("MaleButton")), Is.False);
            Assert.That(group.TryActivate(Find("RankingButton")), Is.True);
            Visible("아직 등록된 랭킹이 없습니다.", "내 순위: 등록된 기록 없음", "새로고침", "뒤로가기");
            Assert.That(navigation.CurrentScreen.name, Is.EqualTo("RankingScreen"));
            Assert.That(group.TryActivate(Find("StartGameButton")), Is.False);
            group.TryActivate(Find("RankingScreenHomeButton"));
            group.TryActivate(Find("NicknameButton"));
            Visible("닉네임 입력", "확인", "취소");
            Assert.That(navigation.CurrentScreen.name, Is.EqualTo("NicknameScreen"));
            group.TryActivate(Find("CancelNicknameButton"));

            for (int run = 0; run < 2; run++)
            {
                Visible("게임 시작", "랭킹", "닉네임 설정");
                Assert.That(group.TryActivate(Find("StartGameButton")), Is.True);
                Assert.That(group.TryActivate(Find("StartGameButton")), Is.False);
                Visible("남자", "여자");
                Assert.That(game.Round.IsPlaying, Is.True);
                Assert.That(game.Round.Score, Is.Zero);
                Assert.That(game.Round.Combo, Is.Zero);
                Assert.That(game.Round.RemainingSeconds, Is.InRange(59.9, 60));
                Assert.That(GameObject.Find("Feedback").GetComponent<Text>().text, Is.Empty);
                yield return GameFlowTests.WaitForInput(game);
                group.TryActivate(Find(game.Round.CurrentName.Gender == Gender.Male ? "MaleButton" : "FemaleButton"));
                Assert.That(game.Round.Score, Is.EqualTo(100), "Re-entry must not duplicate click listeners.");
                game.Round.Start(Time.realtimeSinceStartupAsDouble - 60);
                yield return null;
                yield return null;
                Visible("다시 시작", "메인으로");
                Assert.That(group.TryActivate(Find("ResultHomeButton")), Is.True);
                Visible("게임 시작", "랭킹", "닉네임 설정");
                Assert.That(game.enabled, Is.False);
                Assert.That(game.CanAcceptInput, Is.False);
                Assert.That(game.GetComponent<AudioSource>().isPlaying, Is.False);
                Assert.That(group.TryActivate(Find("RestartButton")), Is.False);
            }
        }
    }
}

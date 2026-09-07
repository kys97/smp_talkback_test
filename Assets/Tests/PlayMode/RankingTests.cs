using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NamnyeoChilse.Tests
{
    public sealed class TestRankingBackend : IRankingBackend
    {
        public RankingEntry[] Top = Array.Empty<RankingEntry>();
        public RankingEntry Mine;
        public bool FailTop, FailMine;
        public int TopCalls, MineCalls;
        public TaskCompletionSource<bool> Gate;
        public async Task<RankingEntry[]> GetTopAsync()
        {
            TopCalls++;
            if (Gate != null) await Gate.Task;
            if (FailTop) throw new Exception("Network error");
            return Top;
        }
        public async Task<RankingEntry> GetMineAsync()
        {
            MineCalls++;
            if (Gate != null) await Gate.Task;
            if (FailMine) throw new Exception("Network error");
            return Mine;
        }
    }

    public sealed class RankingTests
    {
        [Test]
        public async Task AuthenticationSingleFlightAndPartialErrors()
        {
            var auth = new TestAccountBackend { SignInGate = new TaskCompletionSource<bool>() };
            var account = new PlayerAccountService(auth);
            var login = account.InitializeAsync();
            var backend = new TestRankingBackend { Mine = new RankingEntry(26, null, 6300), FailTop = true };
            var service = new RankingService(account, backend);
            Task<RankingSnapshot> first = service.FetchAsync();
            Assert.That(service.FetchAsync(), Is.SameAs(first));
            Assert.That(backend.TopCalls + backend.MineCalls, Is.Zero);
            auth.SignInGate.SetResult(true);
            await login;
            var result = await first;
            Assert.That(result.TopFailed, Is.True);
            Assert.That(result.MineFailed, Is.False);
            Assert.That(result.Mine.SpokenText, Is.EqualTo("27위, 이름 없음, 6300점"));
            backend.FailTop = false;
            backend.FailMine = true;
            result = await service.FetchAsync();
            Assert.That(result.TopFailed, Is.False);
            Assert.That(result.Top, Is.Empty);
            Assert.That(result.MineFailed, Is.True);
            Assert.That(backend.TopCalls, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RowsReadWholeSentenceRefreshAndCloseDiscardLateResult()
        {
            var previous = RankingUI.BackendFactory;
            var backend = new TestRankingBackend
            {
                Top = Enumerable.Range(0, 10).Select(i => new RankingEntry(i, i == 0 ? null : "영희#1234", 15200 - i * 100)).ToArray(),
                Mine = new RankingEntry(26, "나#1234", 6300), Gate = new TaskCompletionSource<bool>()
            };
            RankingUI.BackendFactory = () => backend;
            try
            {
                yield return SceneManager.LoadSceneAsync("SampleScene");
                yield return null;
                var group = UnityEngine.Object.FindAnyObjectByType<AccessibleButtonGroup>();
                var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuNavigation>();
                group.TryActivate(GameObject.Find("RankingButton").GetComponent<Button>());
                var ui = UnityEngine.Object.FindAnyObjectByType<RankingUI>();
                var refresh = GameObject.Find("RankingRefreshButton").GetComponent<Button>();
                var status = GameObject.Find("RankingStatus").GetComponent<Text>();
                Assert.That(status.text, Does.Contain("불러오는 중"));
                ui.Refresh(); ui.Refresh();
                Assert.That(backend.TopCalls, Is.EqualTo(1));
                Assert.That(refresh.interactable, Is.False);
                group.TryActivate(GameObject.Find("RankingScreenHomeButton").GetComponent<Button>());
                backend.Gate.SetResult(true);
                yield return null;
                Assert.That(menu.CurrentScreen.name, Is.EqualTo("MainScreen"));
                Assert.That(status.text, Does.Contain("불러오는 중"), "Closed view ignores late results.");
                backend.Gate = null;
                group.TryActivate(GameObject.Find("RankingButton").GetComponent<Button>());
                yield return null;
                Canvas.ForceUpdateCanvases(); group.RefreshButtons();
                Assert.That(backend.TopCalls, Is.EqualTo(2));
                for (int i = 0; i < 10; i++)
                {
                    var row = GameObject.Find("RankingRow" + i).GetComponent<AccessibleReadOnlyText>();
                    var node = group.GetNode(row);
                    Assert.That(node.isActive, Is.True);
                    Assert.That(node.role, Is.EqualTo(AccessibilityRole.StaticText));
                    Assert.That(node.label, Is.EqualTo(backend.Top[i].SpokenText));
                    Assert.That(group.TryActivate(row), Is.False);
                }
                Assert.That(GameObject.Find("MyRanking").GetComponent<Text>().text, Is.EqualTo("내 순위: 27위 / 6300점"));
                backend.FailTop = backend.FailMine = true;
                group.TryActivate(refresh);
                yield return null;
                Assert.That(status.text, Does.Contain("조회 실패"));
                Assert.That(GameObject.Find("RankingRow0"), Is.Null);
                backend.FailTop = backend.FailMine = false;
                backend.Top = Array.Empty<RankingEntry>(); backend.Mine = null;
                group.TryActivate(refresh);
                yield return null;
                Assert.That(status.text, Does.Contain("아직 등록된"));
                Assert.That(GameObject.Find("MyRanking").GetComponent<Text>().text, Does.Contain("기록 없음"));
                group.TryActivate(GameObject.Find("RankingScreenHomeButton").GetComponent<Button>());
                group.RefreshButtons();
                Assert.That(group.GetNode(status.GetComponent<AccessibleReadOnlyText>()).isActive, Is.False);
                Assert.That(menu.CurrentScreen.name, Is.EqualTo("MainScreen"));
            }
            finally { RankingUI.BackendFactory = previous; }
        }
    }
}

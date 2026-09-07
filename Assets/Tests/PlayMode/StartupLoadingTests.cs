using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NamnyeoChilse.Tests
{
    public sealed class StartupLoadingTests
    {
        [UnityTest]
        public IEnumerator MainStaysInactiveUntilBothLoginAndNameLoadSucceed()
        {
            var previous = PlayerAccountHost.BackendFactory;
            var backend = new TestAccountBackend { SignInGate = new TaskCompletionSource<bool>(), NameGate = new TaskCompletionSource<bool>() };
            PlayerAccountHost.BackendFactory = () => backend;
            try
            {
                yield return SceneManager.LoadSceneAsync("SampleScene");
                yield return null;
                var group = Object.FindAnyObjectByType<AccessibleButtonGroup>();
                var menu = Object.FindAnyObjectByType<MainMenuNavigation>();
                var all = group.GetComponentsInChildren<Selectable>(true);
                var start = all.Single(x => x.name == "StartGameButton");
                var status = GameObject.Find("LoadingStatus").GetComponent<Text>();
                Assert.That(start.gameObject.activeInHierarchy, Is.False);
                Assert.That(menu.CurrentScreen.name, Is.EqualTo("LoadingScreen"));
                group.RefreshButtons();
                Assert.That(all.Where(x => group.GetNode(x).isActive).Select(x => group.GetNode(x).label), Is.EquivalentTo(new[] { "로그인 중" }));
                Assert.That(group.TryActivate(start), Is.False);
                menu.ShowMain(); menu.StartGame(); menu.ShowRanking(); menu.ShowNickname();
                Assert.That(menu.CurrentScreen.name, Is.EqualTo("LoadingScreen"));
                backend.SignInGate.SetResult(true);
                yield return null;
                Assert.That(status.text, Is.EqualTo("로그인 확인 중"));
                Assert.That(start.gameObject.activeInHierarchy, Is.False);
                backend.Name = "테스트#1234";
                backend.NameGate.SetResult(true);
                yield return null;
                group.RefreshButtons();
                Assert.That(menu.CurrentScreen.name, Is.EqualTo("MainScreen"));
                Assert.That(GameObject.Find("LoadingScreen"), Is.Null);
                Assert.That(group.GetNode(status.GetComponent<AccessibleReadOnlyText>()).isActive, Is.False);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(start.gameObject));
                Assert.That(GameObject.Find("NicknameDisplay").GetComponent<Text>().text, Does.Contain("테스트#1234"));
                Assert.That(backend.SignIns, Is.EqualTo(1));
                Assert.That(backend.Reads, Is.EqualTo(1));
            }
            finally { PlayerAccountHost.BackendFactory = previous; }
        }

        [UnityTest]
        public IEnumerator UserInfoFailureKeepsLoadingScreenAndRetryReusesIdentity()
        {
            var previous = PlayerAccountHost.BackendFactory;
            var backend = new TestAccountBackend { IsAuthorized = true, Fail = true };
            PlayerAccountHost.BackendFactory = () => backend;
            try
            {
                yield return SceneManager.LoadSceneAsync("SampleScene");
                yield return null;
                var menu = Object.FindAnyObjectByType<MainMenuNavigation>();
                var group = Object.FindAnyObjectByType<AccessibleButtonGroup>();
                Assert.That(menu.CurrentScreen.name, Is.EqualTo("LoadingScreen"));
                Assert.That(GameObject.Find("StartGameButton"), Is.Null);
                Assert.That(GameObject.Find("LoadingStatus").GetComponent<Text>().text, Does.Contain("실패"));
                backend.Fail = false;
                group.TryActivate(GameObject.Find("LoginRetryButton").GetComponent<Button>());
                yield return null;
                Assert.That(menu.CurrentScreen.name, Is.EqualTo("MainScreen"));
                Assert.That(backend.SignIns, Is.Zero);
                Assert.That(backend.Reads, Is.EqualTo(2));
            }
            finally { PlayerAccountHost.BackendFactory = previous; }
        }
    }
}

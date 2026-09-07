using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NamnyeoChilse.Tests
{
    public sealed class TestAccountBackend : IPlayerAccountBackend
    {
        public bool IsAuthorized { get; set; }
        public string PlayerId => "offline-test-player";
        public string Name;
        public bool Fail;
        public int Reads, Saves, SignIns;
        public TaskCompletionSource<bool> SignInGate;
        public TaskCompletionSource<bool> NameGate;
        public Task InitializeAsync() => Task.CompletedTask;
        public async Task SignInAsync()
        {
            SignIns++;
            if (SignInGate != null) await SignInGate.Task;
            if (Fail) throw new Exception("Simulated offline");
            IsAuthorized = true;
        }
        public async Task<string> ReadNameAsync()
        {
            Assert.That(IsAuthorized, Is.True);
            Reads++;
            if (NameGate != null) await NameGate.Task;
            if (Fail) throw new Exception("Simulated offline");
            return Name;
        }
        public Task<string> SaveNameAsync(string name)
        {
            Assert.That(IsAuthorized, Is.True);
            Saves++;
            if (Fail) throw new Exception("Simulated offline");
            Name = name + "#1234";
            return Task.FromResult(Name);
        }
    }

    [SetUpFixture]
    public sealed class AccountTestEnvironment
    {
        [OneTimeSetUp] public void Setup()
        {
            PlayerAccountHost.BackendFactory = () => new TestAccountBackend();
            LeaderboardScoreHost.BackendFactory = () => new TestLeaderboardBackend();
            RankingUI.BackendFactory = () => new TestRankingBackend();
        }
        [OneTimeTearDown] public void Cleanup()
        {
            PlayerAccountHost.BackendFactory = () => new UgsPlayerAccountBackend();
            LeaderboardScoreHost.BackendFactory = () => new UgsLeaderboardScoreBackend();
            RankingUI.BackendFactory = () => new UgsRankingBackend();
        }
    }

    public sealed class PlayerAccountTests
    {
        [Test]
        public async Task AuthenticationGatesRequestsAndFailuresPreserveServerName()
        {
            var backend = new TestAccountBackend { SignInGate = new TaskCompletionSource<bool>() };
            var service = new PlayerAccountService(backend);
            Task<bool> initialize = service.InitializeAsync();
            Assert.That(backend.Reads, Is.Zero);
            Assert.That(await service.SaveAsync("성급한입력"), Is.False);
            Assert.That(backend.Saves, Is.Zero);
            backend.SignInGate.SetResult(true);
            Assert.That(await initialize, Is.True);
            Assert.That(await service.SaveAsync("한글이름"), Is.True);
            Assert.That(service.PlayerName, Is.EqualTo("한글이름#1234"));
            Assert.That(backend.SignIns, Is.EqualTo(1));
            backend.Fail = true;
            Assert.That(await service.SaveAsync("변경이름"), Is.False);
            Assert.That(service.PlayerName, Is.EqualTo("한글이름#1234"));
            Assert.That(service.IsBusy, Is.False);
            backend.Fail = false;
            var reopened = new PlayerAccountService(backend);
            Assert.That(await reopened.InitializeAsync(), Is.True);
            Assert.That(reopened.PlayerName, Is.EqualTo("한글이름#1234"));
            Assert.That(backend.SignIns, Is.EqualTo(1));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("이름 공백")]
        [TestCase("이름\n")]
        [TestCase("<b>이름</b>")]
        [TestCase("이름\u200b")]
        public async Task InvalidNameDoesNotReachServer(string value)
        {
            var backend = new TestAccountBackend();
            var service = new PlayerAccountService(backend);
            Assert.That(await service.SaveAsync(value), Is.False);
            Assert.That(backend.Saves + backend.Reads + backend.SignIns, Is.Zero);
            Assert.That(PlayerAccountService.ValidateName(new string('가', 51), out _), Is.False);
        }

        [UnityTest]
        public IEnumerator NicknameScreenSavesServerValueAndCancelDiscardsDraft()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var group = UnityEngine.Object.FindAnyObjectByType<AccessibleButtonGroup>();
            group.TryActivate(GameObject.Find("NicknameButton").GetComponent<Button>());
            yield return null;
            var field = GameObject.Find("NicknameInput").GetComponent<AccessibleTextInput>();
            group.RefreshButtons();
            Assert.That(group.GetNode(field).isActive, Is.True);
            Assert.That(group.GetNode(field).label, Is.EqualTo("닉네임 입력"));
            Assert.That(group.TryActivate(field), Is.True);
            field.text = "새닉네임";
            group.TryActivate(GameObject.Find("ConfirmNicknameButton").GetComponent<Button>());
            yield return null;
            Assert.That(GameObject.Find("NicknameDisplay").GetComponent<Text>().text, Does.Contain("새닉네임#1234"));
            Assert.That(group.GetNode(field).isActive, Is.False);
            group.TryActivate(GameObject.Find("NicknameButton").GetComponent<Button>());
            field.text = "저장하지않음";
            group.TryActivate(GameObject.Find("CancelNicknameButton").GetComponent<Button>());
            Assert.That(GameObject.Find("NicknameDisplay").GetComponent<Text>().text, Does.Contain("새닉네임#1234"));
        }
    }
}

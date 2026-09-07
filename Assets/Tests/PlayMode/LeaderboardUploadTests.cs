using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NamnyeoChilse.Tests
{
    public sealed class TestLeaderboardBackend : ILeaderboardScoreBackend
    {
        public int Calls;
        public double Best;
        public bool Fail;
        public Task<double> SubmitAsync(string id, int score)
        {
            Calls++;
            Assert.That(id, Is.EqualTo(LeaderboardSettings.LeaderboardId));
            if (Fail) throw new Exception("Offline");
            Best = Math.Max(Best, score); // Models the required SERVER Keep Best policy.
            return Task.FromResult(Best);
        }
    }

    public sealed class LeaderboardUploadTests
    {
        [Test]
        public async Task WaitsForExistingAuthenticationAndDeduplicatesBeforeAwait()
        {
            var auth = new TestAccountBackend { SignInGate = new TaskCompletionSource<bool>(), Name = "닉네임#1234" };
            var account = new PlayerAccountService(auth);
            var login = account.InitializeAsync();
            var backend = new TestLeaderboardBackend();
            var scores = new LeaderboardScoreService(account, backend);
            var upload = scores.SubmitCompletedGameAsync(1, 8000);
            Assert.That(await scores.SubmitCompletedGameAsync(1, 8000), Is.False);
            Assert.That(backend.Calls, Is.Zero);
            auth.SignInGate.SetResult(true);
            Assert.That(await login, Is.True);
            Assert.That(await upload, Is.True);
            Assert.That(backend.Calls, Is.EqualTo(1));
            Assert.That(auth.SignIns, Is.EqualTo(1));
            Assert.That(account.PlayerName, Is.EqualTo("닉네임#1234"));
            Assert.That(await scores.SubmitCompletedGameAsync(2, 6000), Is.True);
            Assert.That(scores.ServerBestScore, Is.EqualTo(8000));
            Assert.That(await scores.SubmitCompletedGameAsync(3, 12000), Is.True);
            Assert.That(scores.ServerBestScore, Is.EqualTo(12000));
        }

        [Test]
        public async Task FailedAuthenticationAndNetworkDoNotThrowOrResubmit()
        {
            var auth = new TestAccountBackend { Fail = true };
            var backend = new TestLeaderboardBackend();
            var scores = new LeaderboardScoreService(new PlayerAccountService(auth), backend);
            Assert.That(await scores.SubmitCompletedGameAsync(0, 8000), Is.False);
            Assert.That(await scores.SubmitCompletedGameAsync(1, 8000), Is.False);
            Assert.That(backend.Calls, Is.Zero);
            auth.Fail = false;
            backend.Fail = true;
            Assert.That(await scores.SubmitCompletedGameAsync(2, 8000), Is.False);
            Assert.That(await scores.SubmitCompletedGameAsync(2, 8000), Is.False);
            Assert.That(backend.Calls, Is.EqualTo(1));
            backend.Fail = false;
            Assert.That(await scores.SubmitCompletedGameAsync(3, 9000), Is.True);
        }

        [UnityTest]
        public IEnumerator OnlyNaturalCompletionUploadsOnceAndRestartGetsNewGameId()
        {
            var owner = new GameObject("Upload lifecycle");
            var previous = LeaderboardScoreHost.BackendFactory;
            var backend = new TestLeaderboardBackend();
            LeaderboardScoreHost.BackendFactory = () => backend;
            try
            {
                var game = owner.AddComponent<GameManager>();
                game.enabled = false;
                var host = owner.AddComponent<LeaderboardScoreHost>();
                host.Initialize(game, new PlayerAccountService(new TestAccountBackend { IsAuthorized = true }));
                yield return null;
                Assert.That(backend.Calls, Is.Zero);
                game.enabled = true;
                game.StartGame();
                game.enabled = false; // Abandon before deadline.
                game.Round.Tick(Time.realtimeSinceStartupAsDouble + 70);
                game.enabled = true;
                yield return null;
                Assert.That(backend.Calls, Is.Zero);
                game.StartGame();
                yield return GameFlowTests.WaitForInput(game);
                if (game.Round.CurrentName.Gender == Gender.Male) game.ChooseMale(); else game.ChooseFemale();
                game.Round.Tick(Time.realtimeSinceStartupAsDouble + 70);
                game.ChooseMale(); // Expiry detected by input as well as Update.
                game.ChooseFemale();
                yield return null;
                Assert.That(backend.Calls, Is.EqualTo(1));
                Assert.That(backend.Best, Is.EqualTo(100));
                game.enabled = false;
                game.enabled = true;
                Assert.That(backend.Calls, Is.EqualTo(1));
                game.StartGame();
                game.Round.Start(Time.realtimeSinceStartupAsDouble - 60);
                yield return null;
                yield return null;
                Assert.That(backend.Calls, Is.EqualTo(2));
                Assert.That(backend.Best, Is.EqualTo(100));
            }
            finally
            {
                LeaderboardScoreHost.BackendFactory = previous;
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}

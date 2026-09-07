using System;
using UnityEngine;

namespace NamnyeoChilse
{
    public sealed class LeaderboardScoreHost : MonoBehaviour
    {
        public static Func<ILeaderboardScoreBackend> BackendFactory = () => new UgsLeaderboardScoreBackend();
        private GameManager game;
        public LeaderboardScoreService Service { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFactory() => BackendFactory = () => new UgsLeaderboardScoreBackend();

        public void Initialize(GameManager manager, PlayerAccountService account)
        {
            if (game != null) game.Completed -= OnCompleted;
            game = manager;
            Service = new LeaderboardScoreService(account, BackendFactory());
            game.Completed += OnCompleted;
        }

        private async void OnCompleted(int gameNumber, int score)
        {
            var service = Service;
            bool saved = await service.SubmitCompletedGameAsync(gameNumber, score);
            if (this == null) return;
            if (saved) Debug.Log($"Leaderboard 점수 등록 완료: 이번 {score}, 서버 최고 {service.ServerBestScore}", this);
            else Debug.LogWarning(service.Status, this);
        }

        private void OnDestroy() { if (game != null) game.Completed -= OnCompleted; }
    }
}

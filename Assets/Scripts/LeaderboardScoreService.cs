using System;
using System.Threading.Tasks;

namespace NamnyeoChilse
{
    public interface ILeaderboardScoreBackend
    {
        Task<double> SubmitAsync(string leaderboardId, int score);
    }

    public sealed class LeaderboardScoreService
    {
        private readonly PlayerAccountService account;
        private readonly ILeaderboardScoreBackend backend;
        private int lastRequestedGame;
        public string Status { get; private set; } = "점수 등록 대기";
        public double? ServerBestScore { get; private set; }

        public LeaderboardScoreService(PlayerAccountService account, ILeaderboardScoreBackend backend)
        {
            this.account = account;
            this.backend = backend;
        }

        public async Task<bool> SubmitCompletedGameAsync(int gameNumber, int score)
        {
            if (gameNumber <= lastRequestedGame || score < 0) return false;
            lastRequestedGame = gameNumber; // Before await: repeated callbacks cannot submit twice.
            try
            {
                Status = "점수 등록 중";
                // Reuse the existing sign-in/name operation if one is in progress.
                if (account.IsBusy || string.IsNullOrEmpty(account.PlayerId))
                    await account.InitializeAsync();
                if (string.IsNullOrEmpty(account.PlayerId))
                {
                    Status = "점수 미등록: 로그인되지 않았습니다.";
                    UgsDiagnostics.Report("점수 업로드", account.PlayerId, new InvalidOperationException("Authentication incomplete; request not sent"));
                    return false;
                }
                double best = await backend.SubmitAsync(LeaderboardSettings.LeaderboardId, score);
                ServerBestScore = Math.Max(ServerBestScore ?? best, best);
                Status = "점수 등록 완료";
                UgsDiagnostics.Report("점수 업로드", account.PlayerId, detail: $"submitted={score} serverBest={best}");
                return true;
            }
            catch (Exception error)
            {
                UgsDiagnostics.Report("점수 업로드", account.PlayerId, error);
                Status = UgsDiagnostics.UserError(error, "점수 등록 실패: 네트워크와 Leaderboard 설정을 확인하세요.");
                return false;
            }
        }
    }
}

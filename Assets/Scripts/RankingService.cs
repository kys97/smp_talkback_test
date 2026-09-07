using System;
using System.Globalization;
using System.Threading.Tasks;

namespace NamnyeoChilse
{
    public sealed class RankingEntry
    {
        public int Rank { get; }
        public string Nickname { get; }
        public double Score { get; }
        public string ScoreText => Score.ToString("0", CultureInfo.InvariantCulture);
        public string SpokenText => $"{Rank}위, {Nickname}, {ScoreText}점";
        public RankingEntry(int zeroBasedRank, string nickname, double score)
        {
            Rank = Math.Max(1, zeroBasedRank + 1);
            Nickname = string.IsNullOrWhiteSpace(nickname) ? "이름 없음" : nickname.Trim();
            Score = double.IsNaN(score) || double.IsInfinity(score) ? 0 : score;
        }
    }

    public sealed class RankingSnapshot
    {
        public RankingEntry[] Top = Array.Empty<RankingEntry>();
        public RankingEntry Mine;
        public bool TopFailed, MineFailed;
        public string AuthenticationError;
        public string TopError, MineError;
    }

    public interface IRankingBackend
    {
        Task<RankingEntry[]> GetTopAsync();
        Task<RankingEntry> GetMineAsync();
    }

    public sealed class RankingService
    {
        private readonly PlayerAccountService account;
        private readonly IRankingBackend backend;
        private Task<RankingSnapshot> pending;
        public RankingService(PlayerAccountService account, IRankingBackend backend)
        {
            this.account = account;
            this.backend = backend;
        }
        // Close/reopen while a request is in flight shares it instead of creating another pair.
        public Task<RankingSnapshot> FetchAsync()
        {
            if (pending != null && !pending.IsCompleted) return pending;
            return pending = FetchCoreAsync();
        }
        private async Task<RankingSnapshot> FetchCoreAsync()
        {
            var result = new RankingSnapshot();
            try
            {
                if (account.IsBusy || string.IsNullOrEmpty(account.PlayerId)) await account.InitializeAsync();
                if (string.IsNullOrEmpty(account.PlayerId))
                {
                    result.AuthenticationError = "로그인하지 못했습니다. 연결 상태를 확인하고 새로고침하세요.";
                    result.TopFailed = result.MineFailed = true;
                    var error = new InvalidOperationException("Authentication incomplete; requests not sent");
                    UgsDiagnostics.Report("TOP 10 조회", account.PlayerId, error);
                    UgsDiagnostics.Report("내 순위 조회", account.PlayerId, error);
                    return result;
                }
                await Task.WhenAll(ReadTop(result), ReadMine(result));
            }
            catch (Exception error)
            {
                result.TopFailed = result.MineFailed = true;
                UgsDiagnostics.Report("TOP 10 조회", account.PlayerId, error);
                UgsDiagnostics.Report("내 순위 조회", account.PlayerId, error);
            }
            return result;
        }
        private async Task ReadTop(RankingSnapshot result)
        {
            try
            {
                result.Top = await backend.GetTopAsync() ?? Array.Empty<RankingEntry>();
                UgsDiagnostics.Report("TOP 10 조회", account.PlayerId, detail: $"count={result.Top.Length}");
            }
            catch (Exception error)
            {
                result.TopFailed = true;
                result.TopError = UgsDiagnostics.UserError(error, "랭킹 조회 실패. 연결과 서버 설정을 확인한 뒤 새로고침하세요.");
                UgsDiagnostics.Report("TOP 10 조회", account.PlayerId, error);
            }
        }
        private async Task ReadMine(RankingSnapshot result)
        {
            try
            {
                result.Mine = await backend.GetMineAsync();
                UgsDiagnostics.Report("내 순위 조회", account.PlayerId, detail: result.Mine == null ? "entry=none" : $"rank={result.Mine.Rank} score={result.Mine.ScoreText}");
            }
            catch (Exception error)
            {
                result.MineFailed = true;
                result.MineError = UgsDiagnostics.UserError(error, "내 순위: 조회 실패. 새로고침하세요.");
                UgsDiagnostics.Report("내 순위 조회", account.PlayerId, error);
            }
        }
    }
}

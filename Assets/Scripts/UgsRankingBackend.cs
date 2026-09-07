using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;

namespace NamnyeoChilse
{
    public sealed class UgsRankingBackend : IRankingBackend
    {
        public async Task<RankingEntry[]> GetTopAsync()
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardSettings.LeaderboardId,
                new GetScoresOptions { Offset = 0, Limit = 10 });
            return page.Results.Where(entry => entry != null).Select(Convert).ToArray();
        }
        public async Task<RankingEntry> GetMineAsync()
        {
            try
            {
                return Convert(await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardSettings.LeaderboardId));
            }
            catch (LeaderboardsException ex) when (ex.Reason == LeaderboardsExceptionReason.EntryNotFound)
            {
                return null; // A player with no completed/uploaded score is not a network error.
            }
        }
        private static RankingEntry Convert(LeaderboardEntry entry) => entry == null ? null
            : new RankingEntry(entry.Rank, entry.PlayerName, entry.Score);
    }
}

using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;

namespace NamnyeoChilse
{
    public sealed class UgsLeaderboardScoreBackend : ILeaderboardScoreBackend
    {
        public async Task<double> SubmitAsync(string leaderboardId, int score)
        {
            if (!AuthenticationService.Instance.IsAuthorized)
                throw new InvalidOperationException("Authentication incomplete");
            // UGS associates this score with the authenticated Player ID and Player Name.
            // Keep Best must be configured on the server; never read-then-overwrite on the client.
            var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            return entry.Score;
        }
    }
}

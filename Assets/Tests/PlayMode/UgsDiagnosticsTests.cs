using NUnit.Framework;
using Unity.Services.Core;

namespace NamnyeoChilse.Tests
{
    public sealed class UgsDiagnosticsTests
    {
        [Test]
        public void CentralSettingsRequireNoInspectorOrResources()
        {
            Assert.That(LeaderboardSettings.LeaderboardId, Is.EqualTo("namnyeo_chilse_high_score"));
            Assert.That(LeaderboardSettings.EnvironmentName, Is.EqualTo("production"));
        }
        [Test]
        public void MessagesRedactTokensButPreserveServerCause()
        {
            string message = UgsDiagnostics.SafeMessage("Bearer secret123 access_token=secret456 sessionToken: secret789 eyJabc.def.ghi Leaderboard config could not be found");
            Assert.That(message, Does.Not.Contain("secret"));
            Assert.That(message, Does.Not.Contain("eyJabc"));
            Assert.That(message, Does.Contain("Leaderboard config could not be found"));
        }

        [Test]
        public void MissingConfigurationIsNotAnEmptyRankingOrGenericNetworkFailure()
        {
            Assert.That(UgsDiagnostics.UserError(new RequestFailedException(27005, "Leaderboard config could not be found"), "network"),
                Is.EqualTo("랭킹 서버 설정이 없습니다. 관리자에게 문의해 주세요."));
            Assert.That(UgsDiagnostics.UserError(new RequestFailedException(1, "Offline"), "network"), Is.EqualTo("network"));
        }
    }
}

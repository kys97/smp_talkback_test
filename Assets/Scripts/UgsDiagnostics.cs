using System;
using System.Text.RegularExpressions;
using Unity.Services.Core;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Environments.Internal;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;

namespace NamnyeoChilse
{
    public static class UgsDiagnostics
    {
        // Read the same SDK component used by Authentication's requests; never decode tokens.
        public static string ActualEnvironment => UnityServices.State == ServicesInitializationState.Initialized
            ? CoreRegistry.Instance.GetServiceComponent<IEnvironments>().Current : "not-initialized";

        public static void Startup(string phase)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool authorized = UnityServices.State == ServicesInitializationState.Initialized
                && AuthenticationService.Instance.IsAuthorized;
            Debug.Log($"[UGS Startup] phase={phase} leaderboard={LeaderboardSettings.LeaderboardId} "
                + $"environment={ActualEnvironment} configuredEnvironment={LeaderboardSettings.EnvironmentName} "
                + $"authenticated={authorized} cloudProject={Application.cloudProjectId}");
#endif
        }
        public static string UserError(Exception error, string fallback)
        {
            return error is RequestFailedException request && request.ErrorCode == 27005
                ? "랭킹 서버 설정이 없습니다. 관리자에게 문의해 주세요."
                : fallback;
        }
        // Never serialize exceptions, HTTP headers, credentials or complete response bodies.
        public static string SafeMessage(string message)
        {
            string value = Regex.Replace(message ?? "", @"(?i)(bearer\s+)[^\s,\""}]+", "$1[REDACTED]");
            value = Regex.Replace(value, @"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", "[REDACTED]");
            value = Regex.Replace(value, @"(?i)((?:access[_-]?token|session[_-]?token|refresh[_-]?token|authorization)[\""\s:=]+)[^\s,\""}]+", "$1[REDACTED]");
            return value.Replace('\n', ' ').Replace('\r', ' ');
        }

        public static void Report(string operation, string playerId, Exception error = null, string detail = "")
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string code = error is RequestFailedException request ? request.ErrorCode.ToString() : "n/a";
            string reason = error is LeaderboardsException leaderboardError ? leaderboardError.Reason.ToString() : "n/a";
            string message = $"[UGS] operation={operation} result={(error == null ? "SUCCESS" : "FAILED")} "
                + $"leaderboard={LeaderboardSettings.LeaderboardId} environment={ActualEnvironment} configuredEnvironment={LeaderboardSettings.EnvironmentName} "
                + $"cloudProject={Application.cloudProjectId} player={playerId ?? "unauthenticated"} "
                + $"exception={error?.GetType().FullName ?? "none"} code={code} reason={reason} message={SafeMessage(error?.Message)} {detail}";
            if (error == null) Debug.Log(message); else Debug.LogWarning(message);
#endif
        }
    }
}

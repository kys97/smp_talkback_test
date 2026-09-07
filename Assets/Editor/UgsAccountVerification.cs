using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace NamnyeoChilse.Editor
{
    // Explicit batch verification only. Uses a separate SDK profile, never the normal player.
    public static class UgsAccountVerification
    {
        [Serializable] private sealed class Evidence { public string playerId; public string playerName; public bool restored; }
        public static void CreateAndSave() => Begin(false);
        public static void Restore() => Begin(true);
        public static void ProbeLeaderboard()
        {
            SessionState.SetBool("UgsLeaderboardProbe", true);
            Begin(false); // A newly linked Cloud Project has its own anonymous session cache.
        }

        private static void Begin(bool restore)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool("UgsVerificationPending", true);
            SessionState.SetBool("UgsVerificationRestore", restore);
            Resume();
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool("UgsVerificationPending", false)) return;
            EditorApplication.playModeStateChanged -= OnPlay;
            EditorApplication.playModeStateChanged += OnPlay;
        }

        private static void OnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlay;
            SessionState.SetBool("UgsVerificationPending", false);
            Run(SessionState.GetBool("UgsVerificationRestore", false));
        }

        private static async void Run(bool restore)
        {
            try
            {
                string file = Path.GetFullPath(Path.Combine(Application.dataPath, "../UgsVerification.json"));
                await UnityServices.InitializeAsync(new InitializationOptions().SetProfile("codex-validation"));
                if (restore && !AuthenticationService.Instance.SessionTokenExists)
                    throw new InvalidOperationException("Cached session missing");
                var service = new PlayerAccountService(new UgsPlayerAccountBackend());
                if (!await service.InitializeAsync()) throw new InvalidOperationException("UGS sign-in/name read failed");
                if (SessionState.GetBool("UgsLeaderboardProbe", false))
                {
                    SessionState.SetBool("UgsLeaderboardProbe", false);
                    // Query only: do not add synthetic scores to the production board.
                    var ranking = await new RankingService(service, new UgsRankingBackend()).FetchAsync();
                    Debug.Log($"UGS leaderboard probe: topFailed={ranking.TopFailed} mineFailed={ranking.MineFailed}");
                    EditorApplication.Exit(ranking.TopFailed || ranking.MineFailed ? 1 : 0);
                    return;
                }
                if (restore)
                {
                    var previous = JsonUtility.FromJson<Evidence>(File.ReadAllText(file));
                    if (service.PlayerId != previous.playerId || service.PlayerName != previous.playerName)
                        throw new InvalidOperationException("Restored identity/name differs");
                }
                else if (!await service.SaveAsync("검증닉네임")) throw new InvalidOperationException("UGS name write failed");
                PlayerPrefs.Save(); // Flush the SDK-owned session cache before exiting this process.
                File.WriteAllText(file, JsonUtility.ToJson(new Evidence
                {
                    playerId = service.PlayerId, playerName = service.PlayerName, restored = restore
                }, true));
                Debug.Log(restore ? "UGS verification: cached identity and server name restored." : "UGS verification: anonymous sign-in and server name saved.");
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogError("UGS verification failed: " + error.GetType().Name + ". Check project Services configuration and network.");
                EditorApplication.Exit(1);
            }
        }
    }
}

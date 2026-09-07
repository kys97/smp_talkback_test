using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace NamnyeoChilse
{
    public sealed class UgsPlayerAccountBackend : IPlayerAccountBackend
    {
        public bool IsAuthorized => UnityServices.State == ServicesInitializationState.Initialized
            && AuthenticationService.Instance.IsAuthorized;
        public string PlayerId => AuthenticationService.Instance.PlayerId;
        public async Task InitializeAsync()
        {
            UgsDiagnostics.Startup("before-initialize");
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(LeaderboardSettings.EnvironmentName));
            UgsDiagnostics.Startup("initialized");
            if (UgsDiagnostics.ActualEnvironment != LeaderboardSettings.EnvironmentName)
                throw new System.InvalidOperationException("UGS was initialized with a different environment: " + UgsDiagnostics.ActualEnvironment);
        }
        public async Task SignInAsync()
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            PlayerPrefs.Save(); // Flush SDK-owned credentials without reading/copying the token.
            UgsDiagnostics.Startup("authenticated");
        }
        public Task<string> ReadNameAsync() => AuthenticationService.Instance.GetPlayerNameAsync(false);
        public Task<string> SaveNameAsync(string name) => AuthenticationService.Instance.UpdatePlayerNameAsync(name);
    }
}

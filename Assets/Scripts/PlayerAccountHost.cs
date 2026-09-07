using System;
using UnityEngine;

namespace NamnyeoChilse
{
    public sealed class PlayerAccountHost : MonoBehaviour
    {
        // Injection seam for offline tests. Production always uses the official SDK.
        public static Func<IPlayerAccountBackend> BackendFactory = () => new UgsPlayerAccountBackend();
        public PlayerAccountService Service { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFactory() => BackendFactory = () => new UgsPlayerAccountBackend();
        private void Awake() => Service = new PlayerAccountService(BackendFactory());
        // StartupLoadingUI owns the initial request and screen transition.
    }
}

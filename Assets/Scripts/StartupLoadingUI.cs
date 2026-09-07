using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NamnyeoChilse
{
    public sealed class StartupLoadingUI : MonoBehaviour
    {
        private PlayerAccountService account;
        private MainMenuNavigation navigation;
        private GameObject screen;
        private Text status;
        private Button retry, first;
        private AccessibleButtonGroup accessibility;
        private bool busy, completed;
        private string lastMessage = "로그인 중";

        public void Initialize(PlayerAccountService service, MainMenuNavigation menu, GameObject loading,
            Text message, Button retryButton, Button firstMainButton, AccessibleButtonGroup group)
        {
            account = service;
            navigation = menu;
            screen = loading;
            status = message;
            retry = retryButton;
            first = firstMainButton;
            accessibility = group;
            retry.onClick.AddListener(Retry);
            account.Changed += OnAccountChanged;
        }
        private void Start() => Retry();

        public async void Retry()
        {
            if (busy || completed || account == null) return;
            busy = true;
            retry.gameObject.SetActive(false);
            SetStatus("로그인 중");
            bool success = await account.InitializeAsync();
            if (this == null) return;
            busy = false;
            if (!success || string.IsNullOrEmpty(account.PlayerId))
            {
                SetStatus("로그인 또는 사용자 정보 확인에 실패했습니다. 연결 상태를 확인하고 재시도하세요.");
                retry.gameObject.SetActive(true);
                accessibility.RefreshButtons();
                return;
            }
            completed = true;
            account.Changed -= OnAccountChanged;
            navigation.CompleteStartup();
            Canvas.ForceUpdateCanvases();
            accessibility.RefreshButtons();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (AssistiveSupport.isScreenReaderEnabled)
                AssistiveSupport.notificationDispatcher.SendScreenChanged(accessibility.GetNode(first));
#endif
        }

        private void OnAccountChanged()
        {
            if (completed || !busy || !account.IsBusy || !screen.activeInHierarchy) return;
            SetStatus(account.Status == "사용자 정보 확인 중" ? "로그인 확인 중" : "로그인 중");
        }
        private void SetStatus(string message)
        {
            status.text = message;
            status.GetComponent<AccessibleReadOnlyText>().AccessibilityLabel = message;
            accessibility.RefreshButtons();
            if (message == lastMessage) return;
            lastMessage = message;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (screen.activeInHierarchy && AssistiveSupport.isScreenReaderEnabled)
                AssistiveSupport.notificationDispatcher.SendAnnouncement(message);
#endif
        }
        private void OnDestroy()
        {
            if (account != null) account.Changed -= OnAccountChanged;
            if (retry != null) retry.onClick.RemoveListener(Retry);
        }
    }
}

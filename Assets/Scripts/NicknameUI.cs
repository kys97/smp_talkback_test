using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Accessibility;

namespace NamnyeoChilse
{
    public sealed class NicknameUI : MonoBehaviour
    {
        private PlayerAccountService service;
        private MainMenuNavigation navigation;
        private GameObject screen;
        private AccessibleTextInput input;
        private Text currentName, status;
        private Button confirm;
        private string lastStatus;

        public void Initialize(PlayerAccountService account, MainMenuNavigation menu, GameObject panel,
            AccessibleTextInput field, Text mainName, Text message, Button save, Button cancel)
        {
            service = account;
            navigation = menu;
            screen = panel;
            input = field;
            currentName = mainName;
            status = message;
            confirm = save;
            confirm.onClick.AddListener(Save);
            cancel.onClick.AddListener(navigation.ShowMain);
            input.onValueChanged.AddListener(OnInputChanged);
            service.Changed += Refresh;
            Refresh();
        }

        public async void Open()
        {
            input.text = ""; // Enter a new base name; never resubmit the server suffix as a draft.
            navigation.ShowNickname();
            Refresh();
            await service.InitializeAsync();
        }

        private async void Save()
        {
            lastStatus = null;
            bool success = await service.SaveAsync(input.text);
            if (this != null && success && navigation.CurrentScreen == screen) navigation.ShowMain();
        }

        private void OnInputChanged(string value) => Refresh();

        private void Refresh()
        {
            if (service == null) return;
            currentName.text = "현재 닉네임: " + (string.IsNullOrEmpty(service.PlayerName) ? "미설정" : service.PlayerName);
            status.text = service.Status;
            input.interactable = !service.IsBusy;
            // Keep invalid drafts editable; service validates before sending any request.
            confirm.interactable = !service.IsBusy;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (lastStatus != service.Status && (screen.activeInHierarchy || currentName.isActiveAndEnabled)
                && AssistiveSupport.isScreenReaderEnabled)
                AssistiveSupport.notificationDispatcher.SendAnnouncement(service.Status +
                    (string.IsNullOrEmpty(service.PlayerName) ? "" : " " + currentName.text));
#endif
            lastStatus = service.Status;
        }

        private void OnDestroy()
        {
            if (service != null) service.Changed -= Refresh;
            if (input != null) input.onValueChanged.RemoveListener(OnInputChanged);
            if (confirm != null) confirm.onClick.RemoveListener(Save);
        }
    }
}

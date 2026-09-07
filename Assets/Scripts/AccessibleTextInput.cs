using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Concurrent;
using UnityEngine.Scripting;
#endif

namespace NamnyeoChilse
{
    // Unity provides the screen entry; Android EditText provides native text editing to TalkBack.
    public sealed class AccessibleTextInput : InputField
    {
        public string AccessibilityLabel => placeholder is Text label ? label.text : gameObject.name;
#if UNITY_ANDROID && !UNITY_EDITOR
        private ConcurrentQueue<string> completed = new ConcurrentQueue<string>();
        private AndroidJavaObject dialog;
        private Callback callback;
        private bool opening;

        [Preserve]
        private sealed class Callback : AndroidJavaProxy
        {
            private readonly ConcurrentQueue<string> queue;
            public Callback(ConcurrentQueue<string> queue) : base("com.namnyeochilse.accessibility.TextInputDialog$Callback") { this.queue = queue; }
            [Preserve] public void onResult(string value) => queue.Enqueue(value ?? "");
        }
#endif
        public void BeginAccessibleEdit()
        {
            if (!isActiveAndEnabled || !IsInteractable()) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (opening) return;
            opening = true;
            try
            {
                callback = new Callback(completed);
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var bridge = new AndroidJavaClass("com.namnyeochilse.accessibility.TextInputDialog"))
                    dialog = bridge.CallStatic<AndroidJavaObject>("open", player.GetStatic<AndroidJavaObject>("currentActivity"),
                        AccessibilityLabel, text, characterLimit, callback);
            }
            catch (System.Exception)
            {
                opening = false;
                Debug.LogWarning("닉네임 입력창을 열지 못했습니다. 다시 시도해 주세요.");
            }
#else
            Select();
            ActivateInputField();
#endif
        }

        public override void OnSelect(BaseEventData eventData)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            BeginAccessibleEdit();
#else
            base.OnSelect(eventData);
#endif
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (eventData.button == PointerEventData.InputButton.Left) BeginAccessibleEdit();
#else
            base.OnPointerClick(eventData);
#endif
        }

        protected override void LateUpdate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            while (completed.TryDequeue(out string result))
            {
                opening = false;
                // Prefix distinguishes cancel from a legitimately empty edited value.
                if (result.StartsWith("1:")) text = result.Substring(2);
                dialog?.Dispose();
                dialog = null;
                callback = null;
            }
#else
            base.LateUpdate();
#endif
        }

        protected override void OnDisable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (dialog != null) { dialog.Call("close"); dialog.Dispose(); dialog = null; }
            callback = null;
            opening = false;
            completed = new ConcurrentQueue<string>(); // Ignore any late callback from the closed dialog.
#endif
            base.OnDisable();
        }
    }
}

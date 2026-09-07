using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace NamnyeoChilse
{
    // One group per visible screen/Canvas. Child uGUI Buttons are discovered automatically.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class AccessibleButtonGroup : MonoBehaviour
    {
        private readonly Dictionary<Selectable, AccessibleButtonBinding> bindings = new Dictionary<Selectable, AccessibleButtonBinding>();
        private readonly List<Selectable> buttons = new List<Selectable>();
        private readonly List<Selectable> removed = new List<Selectable>();
        private CanvasGroup touchGate;
        private bool originalBlocksRaycasts;
        private bool screenReaderEnabled;
#if UNITY_ANDROID && !UNITY_EDITOR
        private bool platformSupported;
#endif
        private Transform modalRoot;
        public AccessibilityHierarchy Hierarchy { get; private set; }

        private void Awake()
        {
            touchGate = GetComponent<CanvasGroup>();
            Hierarchy = new AccessibilityHierarchy();
        }

        private void OnEnable()
        {
            RefreshButtons();
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                platformSupported = version.GetStatic<int>("SDK_INT") >= 26;
            if (platformSupported)
            {
                AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;
                OnScreenReaderStatusChanged(AssistiveSupport.isScreenReaderEnabled);
            }
#endif
        }

        private void LateUpdate() => RefreshButtons();

        public void SetModalRoot(Transform root)
        {
            if (modalRoot == root) return;
            modalRoot = root;
            RefreshButtons();
        }

        public AccessibilityNode GetNode(Selectable button)
        {
            return button != null && bindings.TryGetValue(button, out var binding) ? binding.Node : null;
        }

        public bool TryActivate(Selectable button)
        {
            if (!isActiveAndEnabled || button == null || !bindings.TryGetValue(button, out var binding)) return false;
            // Recheck live Unity state: native accessibility events can arrive before LateUpdate.
            if (!binding.IsVisible(modalRoot) || !button.IsInteractable()) return false;
            if (button is Button action) action.onClick.Invoke();
            else if (button is AccessibleTextInput field) field.BeginAccessibleEdit();
            else return false;
            return true;
        }

        public void RefreshButtons()
        {
            if (Hierarchy == null || !isActiveAndEnabled) return;
            bool changed = false;
            GetComponentsInChildren(true, buttons);
            buttons.RemoveAll(item => !(item is Button) && !(item is AccessibleTextInput) && !(item is AccessibleReadOnlyText));
            foreach (Selectable button in buttons)
            {
                if (bindings.ContainsKey(button)) continue;
                var binding = new AccessibleButtonBinding(button, Hierarchy.AddNode("", null));
                binding.Node.invoked += () => TryActivate(button);
                bindings.Add(button, binding);
                changed = true;
            }
            removed.Clear();
            foreach (var pair in bindings)
            {
                if (pair.Key == null || !buttons.Contains(pair.Key)) removed.Add(pair.Key);
                else changed |= pair.Value.Refresh(modalRoot);
            }
            foreach (Selectable button in removed)
            {
                var binding = bindings[button];
                binding.Node.isActive = false;
                Hierarchy.RemoveNode(binding.Node, true);
                bindings.Remove(button);
                changed = true;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (changed && screenReaderEnabled && AssistiveSupport.activeHierarchy == Hierarchy)
                Hierarchy.RefreshNodeFrames(); // Also sends the required layout-change notification.
#endif
        }

        private void OnScreenReaderStatusChanged(bool enabled)
        {
            SetScreenReaderMode(enabled);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (enabled)
            {
                Canvas.ForceUpdateCanvases();
                RefreshButtons();
                AssistiveSupport.activeHierarchy = Hierarchy;
            }
            else if (AssistiveSupport.activeHierarchy == Hierarchy) AssistiveSupport.activeHierarchy = null;
#endif
        }

        private void SetScreenReaderMode(bool enabled)
        {
            if (enabled == screenReaderEnabled) return;
            screenReaderEnabled = enabled;
            // Unity mobile also synthesizes a tap for invoked. Suppress the raw UI raycast
            // for this screen and dispatch only the native callback, avoiding double clicks.
            if (enabled)
            {
                originalBlocksRaycasts = touchGate.blocksRaycasts;
                touchGate.blocksRaycasts = false;
            }
            else touchGate.blocksRaycasts = originalBlocksRaycasts;
        }

#if UNITY_EDITOR
        // Exercises routing without activating a desktop screen reader/native backend.
        public void PreviewScreenReaderInput(bool enabled) => SetScreenReaderMode(enabled);
#endif

        private void OnApplicationFocus(bool focused)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (focused && platformSupported) OnScreenReaderStatusChanged(AssistiveSupport.isScreenReaderEnabled);
#endif
        }

        private void OnDisable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (platformSupported)
            {
                AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderStatusChanged;
                if (AssistiveSupport.activeHierarchy == Hierarchy) AssistiveSupport.activeHierarchy = null;
            }
#endif
            SetScreenReaderMode(false);
            if (Hierarchy == null) return;
            Hierarchy.Clear();
            bindings.Clear();
        }
    }
}

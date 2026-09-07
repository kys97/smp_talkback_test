using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace NamnyeoChilse
{
    internal sealed class AccessibleButtonBinding
    {
        private readonly Selectable button;
        private readonly RectTransform rect;
        private readonly Vector3[] corners = new Vector3[4];
        private Text text;
        private Rect previousFrame;
        public AccessibilityNode Node { get; }

        public AccessibleButtonBinding(Selectable button, AccessibilityNode node)
        {
            this.button = button;
            rect = button.transform as RectTransform;
            Node = node;
            Node.role = button is AccessibleReadOnlyText ? AccessibilityRole.StaticText : AccessibilityRole.Button;
            Node.frameGetter = GetFrame;
        }

        public bool IsVisible(Transform modalRoot)
        {
            if (button == null || !button.isActiveAndEnabled || (modalRoot != null && !button.transform.IsChildOf(modalRoot)))
                return false;
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled) return false;
            for (Transform parent = button.transform; parent != null; parent = parent.parent)
            {
                if (parent.TryGetComponent<CanvasGroup>(out var group) && group.alpha <= 0) return false;
            }
            Rect frame = GetFrame();
            return frame.width > 0 && frame.height > 0;
        }

        public bool Refresh(Transform modalRoot)
        {
            if (text == null) text = button.GetComponentInChildren<Text>(true);
            string label = button is AccessibleTextInput field ? field.AccessibilityLabel : text != null && !string.IsNullOrWhiteSpace(text.text) ? text.text : button.name;
            if (button is AccessibleReadOnlyText readOnly && !string.IsNullOrEmpty(readOnly.AccessibilityLabel))
                label = readOnly.AccessibilityLabel;
            string value = button is InputField input ? input.text : "";
            bool active = IsVisible(modalRoot);
            AccessibilityState state = button.IsInteractable() ? AccessibilityState.None : AccessibilityState.Disabled;
            Rect frame = GetFrame();
            bool changed = Node.label != label || Node.value != value || Node.isActive != active || Node.state != state || previousFrame != frame;
            Node.label = label;
            Node.value = value;
            Node.isActive = active;
            Node.state = state;
            previousFrame = frame;
            return changed;
        }

        private Rect GetFrame()
        {
            if (rect == null) return Rect.zero;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null) return Rect.zero;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && camera == null) return Rect.zero;
            rect.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            min = Vector2.Max(min, Vector2.zero);
            max = Vector2.Min(max, new Vector2(Screen.width, Screen.height));
            // Unity accessibility frames use top-left screen origin (not uGUI bottom-left).
            return new Rect(min.x, Screen.height - max.y, Mathf.Max(0, max.x - min.x), Mathf.Max(0, max.y - min.y));
        }
    }
}

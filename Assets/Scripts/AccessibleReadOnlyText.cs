using UnityEngine.UI;

namespace NamnyeoChilse
{
    // Shares the existing discovery, visibility and screen-scoping path without acting as a button.
    public sealed class AccessibleReadOnlyText : Selectable
    {
        public string AccessibilityLabel { get; set; }
        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;
            navigation = new Navigation { mode = Navigation.Mode.None };
        }
    }
}

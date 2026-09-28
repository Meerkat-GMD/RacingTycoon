using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    // Documents own their visual hierarchy. This helper only loads authored assets
    // and binds data; no visual elements or geometry are constructed here.
    public static class ToolkitUI
    {
        // Buttons with this class play their own action sound instead of the click.
        public const string QuietClick = "quiet-click";
        public static event Action ButtonPressed;

        public static UIDocument Open(GameObject owner, string screenName, int order)
        {
            var tree = Resources.Load<VisualTreeAsset>("UI/" + screenName);
            var settings = Resources.Load<PanelSettings>("UI/PanelSettings");
            if (!tree || !settings) throw new InvalidOperationException("Missing UI Toolkit assets: " + screenName);
            var document = owner.GetComponent<UIDocument>();
            if (!document) document = owner.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.sortingOrder = order;
            document.visualTreeAsset = tree;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            document.rootVisualElement.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            document.rootVisualElement.RegisterCallback<NavigationSubmitEvent>(OnSubmit, TrickleDown.TrickleDown);
            return document;
        }

        static void OnClick(ClickEvent evt) => Pressed(evt.target as VisualElement);
        static void OnSubmit(NavigationSubmitEvent evt) => Pressed(evt.target as VisualElement);
        static void Pressed(VisualElement target)
        {
            var button = target as Button ?? target?.GetFirstAncestorOfType<Button>();
            if (button != null && button.enabledInHierarchy && !button.ClassListContains(QuietClick)) ButtonPressed?.Invoke();
        }

        public static T Q<T>(VisualElement root, string name) where T : VisualElement
        {
            var result = root.Q<T>(name);
            if (result == null) throw new InvalidOperationException("Missing authored UI element: " + name);
            return result;
        }

        public static void Show(VisualElement element, bool visible)
        {
            if (element != null) element.EnableInClassList("hidden", !visible);
        }

        public static void SetArt(VisualElement element, Sprite sprite)
        {
            if (element != null) element.style.backgroundImage = sprite ? new StyleBackground(sprite) : StyleKeyword.None;
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    public partial class GameUI : MonoBehaviour
    {
        GameController game;
        UIDocument document;
        VisualElement uiRoot;
        bool pauseWasVisible;
        public VisualElement Root => uiRoot;

        public void Initialize(GameController controller)
        {
            game = controller;
            UiArt.Use(game.World.Assets);
            if (!document)
            {
                // The host is separate so title/tutorial documents never inherit
                // the hidden state of a gameplay document.
                var host = new GameObject("Game Interface");
                host.transform.SetParent(transform, false);
                document = ToolkitUI.Open(host, "Game", 10);
                uiRoot = document.rootVisualElement;
                BindBusiness();
                BindPreparation(uiRoot);
                BindLegacy();
                Q<Button>("PauseContinueButton").clicked += game.TogglePause;
                var pauseSettings = Q<Button>("PauseSettingsButton");
                pauseSettings.clicked += () => game.OpenSettings(() => pauseSettings.Focus());
                Q<VisualElement>("PauseMenu").RegisterCallback<NavigationMoveEvent>(NavigatePause, TrickleDown.TrickleDown);
                Q<Button>("PauseTitleButton").clicked += game.ReturnToTitle;
                Localization.Changed += Refresh;
            }
            Show(uiRoot, true);
            Refresh();
        }

        void OnDestroy() { Localization.Changed -= Refresh; }

        public void HideForTitle()
        {
            CancelShiftDrag();
            Show(uiRoot, false);
            pauseWasVisible = false;
        }

        public void Refresh()
        {
            if (uiRoot == null || game.Session == null) return;
            bool preparation = game.InPreparation;
            Show(Q<VisualElement>("preparationDocument"), preparation);
            Show(Q<VisualElement>("businessDocument"), !preparation && game.Shift != null);
            Show(Q<VisualElement>("legacyDocument"), game.Shift == null);
            RefreshPreparation();
            if (!preparation && game.Shift != null) RefreshBusiness();
            else if (game.Shift == null) RefreshLegacy();
            bool paused = game.Session.Paused;
            Show(Q<VisualElement>("PauseScreen"), paused);
            if (paused) RefreshPause();
            Show(Q<VisualElement>("NoticeToast"), !string.IsNullOrEmpty(game.Notice));
            SetText("NoticeText", game.Notice ?? "");
            if (paused && !pauseWasVisible && !game.SettingsOpen) Q<Button>("PauseContinueButton").Focus();
            if (!paused && pauseWasVisible) uiRoot.focusController?.focusedElement?.Blur();
            pauseWasVisible = paused;
        }

        void RefreshPause()
        {
            bool worker = game.SelectedMachineHasWorker, shift = game.Shift != null;
            Show(Q<VisualElement>("PauseDriveGroup"), !worker);
            Show(Q<VisualElement>("PauseWorkerGroup"), worker);
            Show(Q<VisualElement>("PauseBoostRow"), game.RunStyle == DrivingStyle.Kart);
            Show(Q<VisualElement>("PauseSugarRow"), shift && !worker);
            Show(Q<VisualElement>("PauseDeliverRow"), shift);
            Show(Q<VisualElement>("PauseExtractRow"), shift && !worker);
            Show(Q<VisualElement>("PauseEmptyRow"), shift && !worker);
        }

        void NavigatePause(NavigationMoveEvent evt)
        {
            var buttons = new[] { Q<Button>("PauseContinueButton"), Q<Button>("PauseSettingsButton"), Q<Button>("PauseTitleButton") };
            int delta = evt.direction == NavigationMoveEvent.Direction.Up || evt.direction == NavigationMoveEvent.Direction.Left ? -1 : 1;
            int index = System.Array.IndexOf(buttons, uiRoot.focusController.focusedElement as Button);
            buttons[(index + delta + buttons.Length) % buttons.Length].Focus();
            evt.PreventDefault();
            evt.StopPropagation();
        }

        T Q<T>(string name) where T : VisualElement => ToolkitUI.Q<T>(uiRoot, name);
        void SetText(string name, string text) => Q<Label>(name).text = text;
        static void Show(VisualElement element, bool visible) => ToolkitUI.Show(element, visible);
        static void SetArt(VisualElement element, Sprite sprite) => ToolkitUI.SetArt(element, sprite);
    }
}

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
                Q<Button>("PauseMuteButton").clicked += game.ToggleMute;
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
            SetText("PauseControls", game.SelectedMachineHasWorker
                ? "알바가 운전과 제작을 맡고 있어요.\n완성된 솜사탕을 손님에게 드래그해 주세요.\n\n다른 기계는 상단 버튼으로 선택해요.\n알바 배치는 영업 준비 화면에서 바꿀 수 있어요.\n\nEsc  돌아가기"
                : "설탕 봉지를 위아래로 흔들어 넣어요.\n솜사탕은 손님에게 드래그해 주세요.\n\nW 가속   A / D 조향   S 제동\nSpace 드리프트   F 꺼내기\nR 코스 복귀   Esc 돌아가기");
            Q<Button>("PauseMuteButton").text = game.Audio.Muted ? "소리 꺼짐" : "소리 켜짐";
            Show(Q<VisualElement>("NoticeToast"), !string.IsNullOrEmpty(game.Notice));
            SetText("NoticeText", game.Notice ?? "");
            if (paused && !pauseWasVisible) Q<Button>("PauseContinueButton").Focus();
            if (!paused && pauseWasVisible) uiRoot.focusController?.focusedElement?.Blur();
            pauseWasVisible = paused;
        }

        T Q<T>(string name) where T : VisualElement => ToolkitUI.Q<T>(uiRoot, name);
        void SetText(string name, string text) => Q<Label>(name).text = text;
        static void Show(VisualElement element, bool visible) => ToolkitUI.Show(element, visible);
        static void SetArt(VisualElement element, Sprite sprite) => ToolkitUI.SetArt(element, sprite);
    }
}

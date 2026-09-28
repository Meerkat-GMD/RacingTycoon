using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    // The authored overlay ignores pointer picking except for its explicit actions.
    public sealed class TutorialOverlayUI : MonoBehaviour
    {
        const float GuideBeatSeconds = .4f;

        GameController game;
        VisualElement view, coach, modal, sugarGuide;
        VisualElement sourceFocus, destinationFocus;
        Label dialogue, modalDialogue, heading, progressLabel, actionCue;
        ProgressBar progress;
        Button skip, complete, growthClose, focusedModal;
        bool driveCoachDismissed;

        public void Show(GameController controller)
        {
            game = controller;
            var document = ToolkitUI.Open(gameObject, "Tutorial", 40);
            var root = document.rootVisualElement;
            view = ToolkitUI.Q<VisualElement>(root, "TutorialContent");
            coach = ToolkitUI.Q<VisualElement>(root, "TutorialCoach");
            modal = ToolkitUI.Q<VisualElement>(root, "TutorialModal");
            sugarGuide = ToolkitUI.Q<VisualElement>(root, "TutorialSugarGuide");
            dialogue = ToolkitUI.Q<Label>(root, "TutorialDialogue");
            modalDialogue = ToolkitUI.Q<Label>(root, "TutorialModalDialogue");
            heading = ToolkitUI.Q<Label>(root, "TutorialStepTitle");
            progressLabel = ToolkitUI.Q<Label>(root, "TutorialProgress");
            progress = ToolkitUI.Q<ProgressBar>(root, "TutorialProgressBar");
            actionCue = ToolkitUI.Q<Label>(root, "TutorialActionCue");
            skip = ToolkitUI.Q<Button>(root, "TutorialSkipButton");
            complete = ToolkitUI.Q<Button>(root, "TutorialCompleteButton");
            growthClose = ToolkitUI.Q<Button>(root, "TutorialGrowthCloseButton");
            skip.clicked += game.SkipTutorial;
            complete.clicked += game.CompleteTutorial;
            growthClose.clicked += game.DismissGrowthHint;
            Refresh();
        }

        void LateUpdate() { Refresh(); }

        void Refresh()
        {
            if (!game || view == null) return;
            var step = game.TutorialStep;
            if (step != TutorialStep.Drive) driveCoachDismissed = false;
            else if (game.World && game.World.Kart && Mathf.Abs(game.World.Kart.Speed) > .1f)
                driveCoachDismissed = true;
            bool visible = game.Session != null && !game.Session.Paused && (game.TutorialActive || game.GrowthHintVisible);
            ToolkitUI.Show(view, visible);
            if (!visible)
            {
                SetFocusTargets(null, null);
                focusedModal = null;
                return;
            }

            bool growth = game.GrowthHintVisible;
            bool success = step == TutorialStep.Success;
            bool isModal = growth || success;
            bool showCoach = !isModal && !driveCoachDismissed;
            ToolkitUI.Show(modal, isModal);
            ToolkitUI.Show(coach, showCoach);
            ToolkitUI.Show(skip, !isModal);
            ToolkitUI.Show(complete, success && !growth);
            ToolkitUI.Show(growthClose, growth);
            RefreshSugarGuide(showCoach && step == TutorialStep.PourSugar);
            if (isModal)
            {
                SetFocusTargets(null, null);
                modalDialogue.text = growth ? "번 돈으로 가게를 성장시킬 수 있어요." : "첫 판매 성공이에요!\n이제 사장님 가게를 부탁해요.";
                var target = growth ? growthClose : complete;
                if (focusedModal != target)
                {
                    focusedModal = target;
                    target.Focus();
                }
                return;
            }
            focusedModal = null;
            ToolkitUI.Show(progressLabel, step != TutorialStep.Drive);
            ToolkitUI.Show(progress, step == TutorialStep.PourSugar);
            ToolkitUI.Show(actionCue, step != TutorialStep.Drive);
            VisualElement source = null, destination = null;
            switch (step)
            {
                case TutorialStep.PourSugar:
                    heading.text = "01 / 04  설탕 넣기";
                    bool dragging = game.UI && game.UI.TutorialIsSugarDragging;
                    dialogue.text = dragging ? "봉지를 놓지 말고\n위아래로 흔들어주세요!" : "첫 솜사탕은 함께 만들어봐요!\n딸기 설탕을 꾹 누른 채 끌어주세요.";
                    double required = Tutorial.RequiredSugar(game.Session.Economy);
                    double sugarProgress = Tutorial.SugarProgress(game.Session.Economy);
                    progressLabel.text = "한 개 만들 설탕  " + (required * sugarProgress).ToString("0") + " / " + required.ToString("0") + " g";
                    progress.value = (float)(sugarProgress * 100);
                    actionCue.text = dragging ? "잡은 채로 위아래로 흔들기" : "① 누르기 → ② 끌기 → ③ 흔들기";
                    if (!dragging && game.UI) source = game.UI.TutorialSugarTarget;
                    break;
                case TutorialStep.Drive:
                    heading.text = "02 / 04  달리며 감기";
                    dialogue.text = "W로 가속 · A / D로 조향해요.\n달리면 솜사탕이 감겨요!";
                    break;
                case TutorialStep.Extract:
                    heading.text = "03 / 04  솜사탕 꺼내기";
                    dialogue.text = "잘 만들어졌어요!\nF 또는 꺼내기 버튼을 눌러주세요.";
                    progressLabel.text = "완성된 솜사탕은 진열대에 놓여요.";
                    actionCue.text = "F 키 또는 솜사탕 꺼내기";
                    if (game.UI) destination = game.UI.TutorialExtractTarget;
                    break;
                case TutorialStep.Deliver:
                    heading.text = "04 / 04  첫 손님에게 판매";
                    dialogue.text = "진열대의 솜사탕을 끌어서\n딸기 주문 손님에게 건네주세요.";
                    progressLabel.text = "솜사탕 → 손님 또는 주문 말풍선";
                    actionCue.text = "강조된 솜사탕을 주문 손님에게";
                    if (game.UI)
                    {
                        source = game.UI.TutorialProductTarget();
                        destination = game.UI.TutorialCustomerTarget();
                    }
                    break;
            }
            SetFocusTargets(showCoach ? source : null, showCoach ? destination : null);
        }

        void RefreshSugarGuide(bool visible)
        {
            ToolkitUI.Show(sugarGuide, visible);
            if (!visible) return;
            bool dragging = game.UI && game.UI.TutorialIsSugarDragging;
            sugarGuide.EnableInClassList("dragging", dragging);
            sugarGuide.EnableInClassList("inside", dragging && game.UI.TutorialSugarOverRace);
            // The authored markers rest in two USS poses; this only switches between them.
            sugarGuide.EnableInClassList("beat", Mathf.Repeat(Time.unscaledTime, GuideBeatSeconds * 2) < GuideBeatSeconds);
        }

        void SetFocusTargets(VisualElement source, VisualElement destination)
        {
            if (sourceFocus == source && destinationFocus == destination) return;
            sourceFocus?.RemoveFromClassList("tutorial-focus");
            destinationFocus?.RemoveFromClassList("tutorial-focus");
            sourceFocus = source;
            destinationFocus = destination;
            sourceFocus?.AddToClassList("tutorial-focus");
            destinationFocus?.AddToClassList("tutorial-focus");
        }

        public void Close()
        {
            SetFocusTargets(null, null);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void OnDisable() { SetFocusTargets(null, null); }
        void OnDestroy() { SetFocusTargets(null, null); }
    }
}

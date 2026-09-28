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
        string shownBubble;

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
            Localization.Changed += Refresh;
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
            if (game.Session != null && !game.Session.Paused)
            {
                // Pausing hides the bubble; resuming shows the same one again without a new sound.
                string bubble = !visible ? null : game.GrowthHintVisible ? "growth" : step == TutorialStep.Success ? "success" : step.ToString();
                if (bubble != null && bubble != shownBubble) game.Audio.Play(Sound.TutorialPopup);
                shownBubble = bubble;
            }
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
                modalDialogue.text = Strings.Get(growth ? "tutorial.growth.hint" : "tutorial.first.sale");
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
                    heading.text = Strings.Get("tutorial.step1.heading");
                    bool dragging = game.UI && game.UI.TutorialIsSugarDragging;
                    dialogue.text = Strings.Get(dragging ? "tutorial.step1.dialogue.drag" : "tutorial.step1.dialogue.start");
                    double required = Tutorial.RequiredSugar(game.Session.Economy);
                    double sugarProgress = Tutorial.SugarProgress(game.Session.Economy);
                    progressLabel.text = Strings.Format("tutorial.step1.progress", (required * sugarProgress).ToString("0"), required.ToString("0"));
                    progress.value = (float)(sugarProgress * 100);
                    actionCue.text = Strings.Get(dragging ? "tutorial.step1.cue.drag" : "tutorial.step1.cue.start");
                    if (!dragging && game.UI) source = game.UI.TutorialSugarTarget;
                    break;
                case TutorialStep.Drive:
                    heading.text = Strings.Get("tutorial.step2.heading");
                    dialogue.text = Strings.Get("tutorial.step2.dialogue");
                    break;
                case TutorialStep.Extract:
                    heading.text = Strings.Get("tutorial.step3.heading");
                    dialogue.text = Strings.Get("tutorial.step3.dialogue");
                    progressLabel.text = Strings.Get("tutorial.step3.progress");
                    actionCue.text = Strings.Get("tutorial.step3.cue");
                    if (game.UI) destination = game.UI.TutorialExtractTarget;
                    break;
                case TutorialStep.Deliver:
                    heading.text = Strings.Get("tutorial.step4.heading");
                    dialogue.text = Strings.Get("tutorial.step4.dialogue");
                    progressLabel.text = Strings.Get("tutorial.step4.progress");
                    actionCue.text = Strings.Get("tutorial.step4.cue");
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
            Localization.Changed -= Refresh;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void OnDisable() { SetFocusTargets(null, null); }
        void OnDestroy() { SetFocusTargets(null, null); Localization.Changed -= Refresh; }
    }
}

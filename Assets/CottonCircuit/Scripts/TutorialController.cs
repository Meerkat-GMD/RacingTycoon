using UnityEngine;

namespace CottonCircuit
{
    public partial class GameController
    {
        TutorialOverlayUI tutorialOverlay;

        public TutorialStep TutorialStep => Session == null ? CottonCircuit.TutorialStep.Disabled : Session.Economy.TutorialStep;
        public bool TutorialActive => Session != null && Tutorial.Active(Session.Economy);
        public bool GrowthHintVisible { get; private set; }

        void CloseTutorialUI()
        {
            GrowthHintVisible = false;
            if (tutorialOverlay) { tutorialOverlay.Close(); tutorialOverlay = null; }
        }

        void PrepareTutorialUI()
        {
            // An old save defaults to Disabled. Only explicit new games and their resumes
            // own this overlay; direct Initialize calls keep their established behavior.
            if (!HasProgression || TutorialStep == CottonCircuit.TutorialStep.Disabled) return;
            GrowthHintVisible = Tutorial.TryConsumeGrowthHint(Session.Economy);
            var obj = new GameObject("Tutorial Overlay");
            obj.transform.SetParent(transform, false);
            tutorialOverlay = obj.AddComponent<TutorialOverlayUI>();
            tutorialOverlay.Show(this);
        }

        public void CompleteTutorial()
        {
            if (!TutorialActive || Session.Paused || !Store.CanSave || !Shift.CompleteTutorial()) return;
            UI.CancelShiftDrag();
            Save(); UI.Refresh();
        }

        public void SkipTutorial()
        {
            if (!TutorialActive || Session.Paused || !Store.CanSave || !Shift.SkipTutorial()) return;
            UI.CancelShiftDrag();
            World.Kart.ResetPosition();
            shiftPreviewCount = -1; shiftPreviewFlavor = -2;
            RefreshShiftPreview(); World.ShowInventory(Session.Economy);
            World.UpdateOrders(Session.Economy, 0);
            Save(); UI.Refresh();
        }

        public void DismissGrowthHint()
        {
            // The one-time flag is saved when the hint first appears, including if the
            // player quits without pressing this button.
            if (!GrowthHintVisible || Session == null || Session.Paused) return;
            GrowthHintVisible = false;
            if (UI) UI.Refresh();
        }
    }
}

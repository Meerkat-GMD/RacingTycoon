using System;

namespace CottonCircuit
{
    // Zero is deliberately outside onboarding: existing V8 saves have no tutorial fields.
    public enum TutorialStep { Disabled, PourSugar, Drive, Extract, Deliver, Success, Complete }

    public static class Tutorial
    {
        public static bool Active(Economy economy)
        {
            return economy != null && economy.TutorialStep >= TutorialStep.PourSugar &&
                economy.TutorialStep <= TutorialStep.Success;
        }

        public static double RequiredSugar(Economy economy)
        {
            return ShopShift.MetersForSize(0) * ShopShift.SugarPerMeter *
                (economy != null && economy.Progression != null ? Progression.SugarMultiplier(economy) : 1);
        }

        public static double SugarProgress(Economy economy)
        {
            if (economy == null || economy.Business == null) return 0;
            double rate = ShopShift.SugarPerMeter * (economy.Progression == null ? 1 : Progression.SugarMultiplier(economy));
            double sugar = economy.Business.SugarFlavor == 0 ? economy.Business.SugarGrams : 0;
            if (economy.Business.BatchFlavor == 0) sugar += economy.Business.BatchMeters * rate;
            return Math.Max(0, Math.Min(1, sugar / RequiredSugar(economy)));
        }

        public static bool Refresh(Economy economy)
        {
            if (!Active(economy) || economy.Business == null) return false;
            var before = economy.TutorialStep;
            if (before == TutorialStep.PourSugar && SugarProgress(economy) >= 1 - 1e-9)
                economy.TutorialStep = TutorialStep.Drive;
            if (economy.TutorialStep == TutorialStep.Drive && economy.Business.BatchFlavor == 0 &&
                economy.Business.BatchMeters + 1e-7 >= ShopShift.MetersForSize(0))
                economy.TutorialStep = TutorialStep.Extract;
            return before != economy.TutorialStep;
        }

        public static bool TryConsumeGrowthHint(Economy economy)
        {
            if (economy == null || economy.TutorialStep != TutorialStep.Complete || economy.GrowthHintShown ||
                economy.Day != 1 || economy.Progression == null || economy.Progression.Phase != BusinessPhase.Preparation ||
                economy.Business == null || !economy.Business.Closed) return false;
            economy.GrowthHintShown = true;
            return true;
        }

        public static bool Valid(Economy economy)
        {
            if (economy == null || economy.TutorialStep < TutorialStep.Disabled || economy.TutorialStep > TutorialStep.Complete ||
                economy.GrowthHintShown && economy.TutorialStep != TutorialStep.Complete) return false;
            return !Active(economy) || economy.Day == 1 && economy.Progression != null &&
                economy.Progression.Phase == BusinessPhase.Operating && economy.Business != null && !economy.Business.Closed;
        }
    }

    public partial class ShopShift
    {
        public bool BeginTutorial()
        {
            if (economy.TutorialStep != TutorialStep.Disabled || economy.Day != 1 || economy.TotalSold != 0 ||
                economy.Progression == null || economy.Progression.Phase != BusinessPhase.Preparation ||
                economy.Progression.Purchases.Count != 0 || economy.Inventory.Count != 0 || Paused) return false;
            economy.Progression.SelectedMachine = 0;
            if (!BeginBusiness()) return false;
            // The guided first order is stable even if ordinary arrivals later become groups.
            var customer = State.Customers[0];
            State.Customers.Clear();
            customer.Flavor = customer.Size = customer.Slot = 0;
            State.Customers.Add(customer);
            economy.TutorialStep = TutorialStep.PourSugar;
            economy.GrowthHintShown = false;
            return true;
        }

        public bool CompleteTutorial()
        {
            if (Paused || economy.TutorialStep != TutorialStep.Success) return false;
            economy.TutorialStep = TutorialStep.Complete;
            return true;
        }

        public bool SkipTutorial()
        {
            if (Paused || !Tutorial.Active(economy)) return false;
            if (economy.TutorialStep == TutorialStep.Success) return CompleteTutorial();
            economy.TutorialStep = TutorialStep.Complete;
            ClearDay();
            State.Closed = false;
            State.RemainingSeconds = Duration;
            State.NextCustomerIn = 0;
            Arrive(false);
            return true;
        }
    }
}

using System;
using UnityEngine;

namespace CottonCircuit
{
    public partial class GameController
    {
        public ShopShift Shift { get; private set; }
        float shiftSaveIn;
        int shiftPreviewCount = -1, shiftPreviewFlavor = -2;

        void InitializeShiftDriving()
        {
            Session.StartContinuousRecipe(0, 0);
            lastMode = Session.Mode;
            shiftSaveIn = 5;
            shiftPreviewCount = -1; shiftPreviewFlavor = -2;
            if (!World) return;
            World.Kart.SetStyle(RunStyle, World.Assets.DownhillCoupe);
            World.Kart.MaximumSpeed = Session.Economy.MaxSpeed;
            World.Kart.ResetPosition();
            World.SetMode(GameMode.Racing);
            // The street illustration replaces the old perspective shop preview.
            if (World.ShopCamera) World.ShopCamera.enabled = false;
            World.Kart.ConfiguredFlavor = Shift.State.SugarFlavor;
            World.AnimationPaused = !Shift.IsOpen;
            RefreshShiftPreview();
            World.UpdateOrders(Session.Economy, 0);
        }

        void StepShift(float throttle, float steering, bool brake, bool drift, float dt, bool boost)
        {
            Shift.Paused = Session.Paused;
            if (!Shift.IsOpen || !Store.CanSave)
            {
                World.AnimationPaused = true; World.Kart.SetEffects(false); World.UpdateThread(false);
                Audio.UpdateDriving(World.Kart, false); UI.Refresh(); return;
            }
            // Clamp the physical step as well as the business clock at the closing boundary.
            dt = (float)Math.Min(dt, Shift.State.RemainingSeconds);
            bool worker = SelectedMachineHasWorker;
            if (worker && WorkerDriving)
            {
                double autoThrottle, autoSteering; bool autoBrake;
                CottonCircuit.AutoDrive.Input(World.Kart.DriveModel, out autoThrottle, out autoSteering, out autoBrake);
                throttle = (float)autoThrottle; steering = (float)autoSteering; brake = autoBrake; drift = boost = false;
            }
            else if (worker)
            {
                World.Kart.Stop();
                throttle = steering = 0; brake = true; drift = boost = false;
            }
            if (TutorialActive && TutorialStep != CottonCircuit.TutorialStep.Drive)
            {
                World.Kart.Stop();
                throttle = steering = 0; brake = true; drift = boost = false;
            }
            World.AnimationPaused = false;
            var drive = World.Kart.DriveModel;
            int hits = drive.WallHits, boosts = drive.BoostCount;
            World.Kart.MaximumSpeed = Session.Economy.MaxSpeed;
            World.Kart.ConfiguredFlavor = Shift.State.SugarFlavor;
            drive.SteeringMultiplier = HasProgression ? Progression.SteeringMultiplier(Session.Economy) : 1;
            World.Kart.Drive(throttle, steering, brake, dt, drift, boost);
            if (!worker && drive.WallHits > hits) Audio.Play(Sound.WallHit);
            // LastRewardDistance excludes reverse/replayed/recovery progress. Growth per meter
            // follows speed, so slow driving, wall hits and stops wind little or nothing.
            double speedYield = ShopShift.SpeedYield(drive.Speed);
            double normalizedMeters = drive.LastRewardDistance * speedYield *
                (HasProgression ? ShopShift.LapMeters / drive.Course.Length : 1);
            var tutorialBefore = TutorialStep;
            int inventoryBefore = Session.Economy.Inventory.Count;
            // Worker output uses its established production rate, equally on and off screen.
            // Its animated car must not add a second source of production or wall penalties.
            Shift.Advance(dt, worker ? 0 : normalizedMeters, worker ? 0 : drive.WallHits - hits);
            World.Kart.ConfiguredFlavor = Shift.State.SugarFlavor;
            if (worker && !WorkerDriving) World.Kart.Stop();
            if (Session.Economy.Inventory.Count != inventoryBefore)
            {
                World.ShowInventory(Session.Economy);
                Save();
            }
            if (tutorialBefore != TutorialStep)
            {
                World.Kart.Stop();
                Save();
            }
            if (drive.BoostCount > boosts) Audio.PlayBoost(drive.BoostTier);
            RefreshShiftPreview();
            World.UpdateOrders(Session.Economy, dt);
            bool active = Shift.IsOpen;
            World.UpdateThread(active && Shift.State.SugarGrams > 0 && drive.LastRewardDistance > 0 && speedYield > 0 &&
                (Shift.State.BatchMeters <= 0 || Shift.State.SugarFlavor == Shift.State.BatchFlavor));
            Audio.UpdateDriving(World.Kart, active);
            if (!active)
            {
                Audio.PlayClosing(Shift.State.DayProfit < 0);
                World.Kart.Stop(); World.AnimationPaused = true; UI.CancelShiftDrag();
                World.ShowInventory(Session.Economy);
                Save(); Notify(Strings.Get("notice.shift.closing"));
            }
            else
            {
                shiftSaveIn -= dt;
                if (shiftSaveIn <= 0) { Save(); shiftSaveIn = 5; }
            }
            UI.Refresh();
        }

        void RefreshShiftPreview()
        {
            int count = ShopShift.PreviewSampleCount(Shift.State.BatchMeters);
            if (count == shiftPreviewCount && Shift.State.BatchFlavor == shiftPreviewFlavor) return;
            var preview = ShopShift.Preview(Shift.State.BatchMeters, Shift.State.BatchFlavor);
            var samples = preview == null ? null : preview.Samples;
            shiftPreviewCount = count; shiftPreviewFlavor = Shift.State.BatchFlavor;
            World.CentralCandy.Show(samples);
        }

        bool ShiftActionsAllowed => Shift != null && !Session.Paused && Shift.IsOpen && Store.CanSave;

        public bool EmptySugar()
        {
            if (!ShiftActionsAllowed || SelectedMachineHasWorker) return false;
            UI.CancelShiftDrag();
            if (!Shift.EmptySugar()) return false;
            World.Kart.ConfiguredFlavor = -1;
            World.UpdateThread(false);
            Save();
            Notify(Strings.Get(Shift.State.BatchMeters > 0 ? "notice.sugar.empty.batch" : "notice.sugar.empty.clear"));
            UI.Refresh(); return true;
        }

        public bool PourSugar(int flavorIndex, double grams = ShopShift.PourAmount)
        {
            if (!ShiftActionsAllowed || SelectedMachineHasWorker) return false;
            bool poured = Shift.Pour(flavorIndex, grams);
            if (poured)
            {
                World.Kart.ConfiguredFlavor = Shift.State.SugarFlavor;
                Save(); UI.Refresh();
            }
            else if (Shift.State.BatchMeters > 0 && Shift.State.BatchFlavor != flavorIndex)
                Notify(Strings.Get("notice.sugar.mismatch"));
            return poured;
        }

        public Product ExtractCandy()
        {
            if (!ShiftActionsAllowed || SelectedMachineHasWorker) return null;
            var product = Shift.Extract();
            if (product == null)
            {
                Notify(Strings.Get(TutorialActive && TutorialStep == CottonCircuit.TutorialStep.Drive
                    ? "notice.extract.tutorial"
                    : Session.Economy.Inventory.Count >= Session.Economy.StockCapacity
                    ? "notice.extract.shelf.full"
                    : "notice.extract.none"));
                return null;
            }
            SelectedProductId = product.Id;
            RefreshShiftPreview(); World.ShowInventory(Session.Economy); Audio.Play(Sound.CandyExtract);
            Notify(Strings.Get(ShopShift.SizeOf(product) < 0 ? "notice.extract.small" : "notice.extract.done"));
            Save(); UI.Refresh(); return product;
        }

        public bool ResumeCandy(string productId)
        {
            if (!ShiftActionsAllowed || SelectedMachineHasWorker) return false;
            bool exchanged = Shift.State.BatchMeters > 0;
            if (!Shift.ResumeProduct(productId)) return false;
            SelectedProductId = null;
            shiftPreviewCount = -1; shiftPreviewFlavor = -2;
            RefreshShiftPreview(); World.ShowInventory(Session.Economy); Audio.Play(Sound.CandyDrop);
            bool matchingSugar = Shift.State.SugarGrams > 0 && Shift.State.SugarFlavor == Shift.State.BatchFlavor;
            string message = Strings.Get(exchanged ? "notice.resume.exchange" : "notice.resume.simple");
            if (!matchingSugar) message = Strings.Format("notice.resume.mismatch", Palette.FlavorName(Shift.State.BatchFlavor));
            Save(); Notify(message); UI.Refresh(); return true;
        }

        public DeliveryResult DeliverCandy(string productId, string customerId)
        {
            if (!ShiftActionsAllowed) return DeliveryResult.Rejected;
            var product = Session.Economy.Inventory.Find(p => p.Id == productId);
            int before = Session.Economy.Coins, starBonus = Session.Economy.StarBonus(product);
            var delivery = Shift.Deliver(productId, customerId);
            if (delivery != DeliveryResult.Sold) Audio.Play(Sound.DeliverFail);
            if (delivery == DeliveryResult.Rejected)
            {
                if (product != null && ShopShift.SizeOf(product) < 0) Notify(Strings.Get("notice.deliver.small"));
                return delivery;
            }
            SelectedProductId = null;
            if (delivery == DeliveryResult.Sold)
            {
                Audio.PlaySale(starBonus > 0);
                Notify(Strings.Format("notice.sale.shift", Session.Economy.Coins - before, ShopShift.StarText(product.Quality),
                    starBonus > 0 ? Strings.Format("notice.sale.bonus", starBonus) : Strings.Get("notice.sale.nobonus")));
            }
            World.ShowInventory(Session.Economy); Save(); UI.Refresh(); return delivery;
        }

        public bool TrashCandy(string productId)
        {
            if (!ShiftActionsAllowed || !Shift.Discard(productId)) return false;
            Audio.Play(Sound.Trash);
            SelectedProductId = null; World.ShowInventory(Session.Economy);
            Save(); Notify(Strings.Get("notice.trash.done")); UI.Refresh(); return true;
        }

        public void StartNextDay()
        {
            if (HasProgression) { ReturnToPreparation(); return; }
            if (Shift == null || Session.Paused || !Store.CanSave || !Shift.NextDay()) return;
            UI.CancelShiftDrag();
            SelectedOrderId = SelectedProductId = null;
            InitializeShiftDriving();
            World.ShowInventory(Session.Economy); World.UpdateThread(false);
            Save();
            Notify(Strings.Get("notice.nextday.legacy"));
            UI.Refresh();
        }
    }
}

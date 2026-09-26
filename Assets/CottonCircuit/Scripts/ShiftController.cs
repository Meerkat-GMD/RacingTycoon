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
            if (AutoDrive)
            {
                double autoThrottle, autoSteering; bool autoBrake;
                CottonCircuit.AutoDrive.Input(World.Kart.DriveModel, out autoThrottle, out autoSteering, out autoBrake);
                throttle = (float)autoThrottle; steering = (float)autoSteering; brake = autoBrake; drift = boost = false;
            }
            World.AnimationPaused = false;
            var drive = World.Kart.DriveModel;
            int skills = drive.SkillCount, hits = drive.WallHits, boosts = drive.BoostCount;
            World.Kart.MaximumSpeed = Session.Economy.MaxSpeed;
            World.Kart.ConfiguredFlavor = Shift.State.SugarFlavor;
            drive.SteeringMultiplier = HasProgression ? Progression.SteeringMultiplier(Session.Economy) : 1;
            World.Kart.Drive(throttle, steering, brake, dt, drift, boost);
            // LastRewardDistance excludes reverse/replayed/recovery progress. Growth per meter
            // follows speed, so slow driving, wall hits and stops wind little or nothing.
            double speedYield = ShopShift.SpeedYield(drive.Speed);
            double normalizedMeters = drive.LastRewardDistance * speedYield *
                (HasProgression ? ShopShift.LapMeters / drive.Course.Length : 1);
            Shift.Advance(dt, normalizedMeters, drive.SkillCount - skills, drive.WallHits - hits);
            if (drive.BoostCount > boosts) Audio.PlayBoost(drive.BoostTier);
            RefreshShiftPreview();
            World.UpdateOrders(Session.Economy, dt);
            bool active = Shift.IsOpen;
            World.UpdateThread(active && Shift.State.SugarGrams > 0 && drive.LastRewardDistance > 0 && speedYield > 0 &&
                (Shift.State.BatchMeters <= 0 || Shift.State.SugarFlavor == Shift.State.BatchFlavor));
            Audio.UpdateDriving(World.Kart, active);
            if (!active)
            {
                World.Kart.Stop(); World.AnimationPaused = true; UI.CancelShiftDrag();
                World.ShowInventory(Session.Economy);
                Save(); Notify("영업 마감");
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
            if (!ShiftActionsAllowed) return false;
            UI.CancelShiftDrag();
            if (!Shift.EmptySugar()) return false;
            World.Kart.ConfiguredFlavor = -1;
            World.UpdateThread(false);
            Save();
            Notify(Shift.State.BatchMeters > 0
                ? "설탕을 비웠어요. 만들던 솜사탕은 남아 있어요."
                : "설탕을 비웠어요. 새 설탕을 넣어주세요.");
            UI.Refresh(); return true;
        }

        public bool PourSugar(int flavorIndex, double grams = ShopShift.PourAmount)
        {
            if (!ShiftActionsAllowed) return false;
            bool poured = Shift.Pour(flavorIndex, grams);
            if (poured)
            {
                World.Kart.ConfiguredFlavor = Shift.State.SugarFlavor;
                Save(); UI.Refresh();
            }
            else if (Shift.State.BatchMeters > 0 && Shift.State.BatchFlavor != flavorIndex)
                Notify("다른 맛을 넣으려면 지금 만든 솜사탕을 F로 먼저 꺼내세요.");
            return poured;
        }

        public Product ExtractCandy()
        {
            if (!ShiftActionsAllowed) return null;
            var product = Shift.Extract();
            if (product == null)
            {
                Notify(Session.Economy.Inventory.Count >= Session.Economy.StockCapacity
                    ? "진열대가 가득 찼어요. 판매하거나 쓰레기통에 버린 뒤 꺼내세요."
                    : "아직 만든 솜사탕이 없어요. 설탕을 넣고 달려보세요.");
                return null;
            }
            SelectedProductId = product.Id;
            RefreshShiftPreview(); World.ShowInventory(Session.Economy); Audio.Play(2);
            Notify(ShopShift.SizeOf(product) < 0 ? "아직 작아요. 주행 화면으로 가져와 키우거나 쓰레기통에 버릴 수 있어요."
                : "솜사탕을 꺼냈어요. 맞는 손님에게 드래그하세요!");
            Save(); UI.Refresh(); return product;
        }

        public bool ResumeCandy(string productId)
        {
            if (!ShiftActionsAllowed) return false;
            bool exchanged = Shift.State.BatchMeters > 0;
            if (!Shift.ResumeProduct(productId)) return false;
            SelectedProductId = null;
            shiftPreviewCount = -1; shiftPreviewFlavor = -2;
            RefreshShiftPreview(); World.ShowInventory(Session.Economy); Audio.Play(2);
            bool matchingSugar = Shift.State.SugarGrams > 0 && Shift.State.SugarFlavor == Shift.State.BatchFlavor;
            string message = exchanged ? "만들던 제품은 진열대로 옮기고, 가져온 솜사탕을 이어 만들어요." : "가져온 솜사탕을 이어 만들어요.";
            if (!matchingSugar) message = Palette.FlavorName(Shift.State.BatchFlavor) + " 솜사탕을 가져왔어요. 같은 맛 설탕을 넣어주세요.";
            Save(); Notify(message); UI.Refresh(); return true;
        }

        public DeliveryResult DeliverCandy(string productId, string customerId)
        {
            if (!ShiftActionsAllowed) return DeliveryResult.Rejected;
            var product = Session.Economy.Inventory.Find(p => p.Id == productId);
            int before = Session.Economy.Coins;
            var delivery = Shift.Deliver(productId, customerId);
            if (delivery == DeliveryResult.Rejected)
            {
                if (product != null && ShopShift.SizeOf(product) < 0) Notify("아직 팔 수 없어요. 주행 화면에서 더 키우거나 쓰레기통에 버리세요.");
                return delivery;
            }
            SelectedProductId = null;
            if (delivery == DeliveryResult.Sold)
            {
                Audio.Play(1);
                Notify("주문 전달 완료! +" + (Session.Economy.Coins - before) + " 코인");
            }
            World.ShowInventory(Session.Economy); Save(); UI.Refresh(); return delivery;
        }

        public bool TrashCandy(string productId)
        {
            if (!ShiftActionsAllowed || !Shift.Discard(productId)) return false;
            SelectedProductId = null; World.ShowInventory(Session.Economy);
            Save(); Notify("솜사탕을 쓰레기통에 버렸어요."); UI.Refresh(); return true;
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
            Notify("새로운 영업일! 진열대·설탕·제작 중 솜사탕을 비우고 새 손님을 맞이해요.");
            UI.Refresh();
        }
    }
}

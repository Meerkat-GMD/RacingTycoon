using System;
namespace CottonCircuit
{
    public partial class GameController
    {
        ArcadeDrive[] machineDrives = new ArcadeDrive[3];
        public bool HasProgression => Session != null && Session.Economy.Progression != null && Shift != null;
        public bool InPreparation => HasProgression && Session.Economy.Progression.Phase == BusinessPhase.Preparation;
        public bool InBusiness => HasProgression && Session.Economy.Progression.Phase == BusinessPhase.Operating;
        bool PreparationActions => InPreparation && !Session.Paused && Store.CanSave;
        public MachineProduction Machine(int index) => HasProgression ? Shift.Machine(index) : null;
        public void PurchaseNode(string id)
        {
            if (!PreparationActions || !Progression.Buy(Session.Economy, id)) return;
            Audio.Play(1); Save(); UI.Refresh();
        }
        public void BeginBusiness()
        {
            if (!PreparationActions || !Shift.BeginBusiness()) return;
            Notice = null; noticeTimer = 0;
            machineDrives = new ArcadeDrive[3]; SelectedProductId = SelectedOrderId = null;
            UI.CancelShiftDrag(); ApplyMachineCourse(false);
            shiftPreviewCount = -1; shiftPreviewFlavor = -2; RefreshShiftPreview();
            World.ShowInventory(Session.Economy); World.AnimationPaused = false;
            Save(); UI.Refresh();
        }
        public void ReturnToPreparation()
        {
            if (!HasProgression || Session.Paused || !Store.CanSave || !Shift.ReturnToPreparation()) return;
            Notice = null; noticeTimer = 0;
            UI.CancelShiftDrag(); World.Kart.Stop(); World.AnimationPaused = true; World.UpdateThread(false);
            Audio.UpdateDriving(World.Kart, false); SelectedProductId = SelectedOrderId = null;
            Save(); UI.Refresh();
        }
        public void ChooseMachine(int index)
        {
            if (!HasProgression || Session.Paused || !Store.CanSave || index == Shift.SelectedMachine) return;
            int old = Shift.SelectedMachine;
            if (!Shift.SelectMachine(index)) return;
            machineDrives[old] = World.Kart.DriveModel;
            UI.CancelShiftDrag(); ApplyMachineCourse(true);
            shiftPreviewCount = -1; shiftPreviewFlavor = -2; RefreshShiftPreview();
            Save(); UI.Refresh();
        }
        void ApplyMachineCourse(bool retain)
        {
            int selected = Shift.SelectedMachine;
            PreparedMap = RunMap = Progression.MachineMap(selected);
            World.SelectCourse(RunMap);
            if (retain && machineDrives[selected] != null) World.Kart.RestoreDrive(machineDrives[selected]);
            machineDrives[selected] = World.Kart.DriveModel;
            PreparedStyle = RunStyle = Session.Economy.Progression.CartStyle == 0 ? DrivingStyle.Kart : DrivingStyle.Downhill;
            World.Kart.SetStyle(RunStyle, World.Assets.DownhillCoupe);
            World.Kart.MaximumSpeed = Session.Economy.MaxSpeed;
            World.Kart.ConfiguredFlavor = Shift.State.SugarFlavor;
            World.SetMode(GameMode.Racing); World.AnimationPaused = !Shift.IsOpen;
            World.UpdateThread(false);
        }
        public void ChooseLocation(int index)
        {
            if (!PreparationActions || !Progression.HasLocation(Session.Economy, index)) return;
            Session.Economy.Progression.SelectedLocation = index; Save(); UI.Refresh();
        }
        public void ChooseCart(int style)
        {
            if (!PreparationActions || !Progression.HasCartStyle(Session.Economy, style)) return;
            Session.Economy.Progression.CartStyle = style; ApplyMachineCourse(true); Save(); UI.Refresh();
        }
        public void ToggleWorker(int index)
        {
            if (!PreparationActions || index < 0 || index >= Progression.OwnedMachines(Session.Economy)) return;
            var m = Machine(index);
            if (!m.WorkerAssigned)
            {
                int assigned = 0; foreach (var machine in Shift.State.Machines) if (machine.WorkerAssigned) assigned++;
                if (assigned >= Progression.WorkerCount(Session.Economy) || Progression.WorkerGrade(Session.Economy) < Progression.MachineTier(index)) return;
            }
            m.WorkerAssigned = !m.WorkerAssigned; Save(); UI.Refresh();
        }
        public void CycleRecipeFlavor(int index)
        {
            if (!PreparationActions || index < 0 || index >= Progression.OwnedMachines(Session.Economy)) return;
            var m = Machine(index);
            for (int step = 1; step <= 3; step++) if (Shift.CanMakeFlavor(index, (m.RecipeFlavor + step) % 3))
                { m.RecipeFlavor = (m.RecipeFlavor + step) % 3; break; }
            Save(); UI.Refresh();
        }
        public void CycleRecipeSize(int index)
        {
            if (!PreparationActions || index < 0 || index >= Progression.OwnedMachines(Session.Economy)) return;
            var m = Machine(index); m.RecipeSize = (m.RecipeSize + 1) % (Shift.MaxSize(index) + 1); Save(); UI.Refresh();
        }
        public void CycleSugarGrade(int index)
        {
            if (!HasProgression || Session.Paused || !Store.CanSave || index < 0 || index >= Progression.OwnedMachines(Session.Economy) ||
                (!InPreparation && !InBusiness)) return;
            var m = Machine(index);
            if (m.SugarGrams > 0 || m.BatchMeters > 0) { Notify("설탕과 솜사탕을 비운 뒤 변경"); return; }
            m.SugarGrade = m.SugarGrade % Progression.MaxSugarGrade(Session.Economy) + 1;
            m.RecipeSize = Math.Min(m.RecipeSize, Shift.MaxSize(index)); Save(); UI.Refresh();
        }
    }
}

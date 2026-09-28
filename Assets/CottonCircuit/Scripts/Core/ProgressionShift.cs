using System;
using System.Collections.Generic;
namespace CottonCircuit
{
    public partial class ShopShift
    {
        public int SelectedMachine { get { return economy.Progression == null ? 0 : economy.Progression.SelectedMachine; } }
        public double Duration { get { return economy.Progression == null ? DayDuration : Progression.DaySeconds(economy); } }
        public double PatienceLimit { get { return economy.Progression == null ? CustomerPatience : Progression.PatienceSeconds(economy); } }
        public double ArrivalInterval { get { return economy.Progression == null ? ArrivalDelay : Progression.ArrivalSeconds(economy); } }
        public bool HasWorker(int index)
        {
            if (economy.Progression == null || State.Machines == null || index < 0 ||
                index >= State.Machines.Count || index >= Progression.OwnedMachines(economy) ||
                State.Machines[index] == null || !State.Machines[index].WorkerAssigned ||
                Progression.WorkerGrade(economy) < Progression.MachineTier(index)) return false;
            int available = Progression.WorkerCount(economy), assigned = 0;
            if (available <= 0) return false;
            for (int i = 0; i <= index; i++)
                if (State.Machines[i] != null && State.Machines[i].WorkerAssigned &&
                    Progression.WorkerGrade(economy) >= Progression.MachineTier(i)) assigned++;
            return assigned <= available;
        }
        // Read-only: controller/UI calls cannot buy ingredients or change the selected machine copy.
        public bool WorkerCanOperate(int index)
        {
            if (!IsOpen || Tutorial.Active(economy) || !HasWorker(index) || economy.Inventory.Count >= economy.StockCapacity) return false;
            var machine = State.Machines[index];
            if (machine.RecipeSize < 0 || machine.RecipeSize > 2) return false;
            int flavor = machine.BatchMeters > 0 ? machine.BatchFlavor : machine.RecipeFlavor;
            if (!CanMakeFlavor(index, flavor)) return false;
            double target = MetersForSize(Math.Min(machine.RecipeSize, EffectiveMaxSize(index)));
            if (machine.BatchMeters + 1e-7 >= target) return true;
            if (machine.SugarGrams > 1e-9 && machine.SugarFlavor == flavor) return true;
            double existing = machine.SugarFlavor == flavor ? machine.SugarGrams : 0;
            double accepted = Math.Min(PourAmount, SugarCapacity - existing);
            return accepted > 1e-7 && economy.Coins >= PourCost(index, flavor, accepted);
        }
        void InitializeMachines(bool fresh)
        {
            if (State.Machines == null) State.Machines = new List<MachineProduction>();
            while (State.Machines.Count < 3) State.Machines.Add(new MachineProduction());
            if (fresh) State.RemainingSeconds = Duration;
            SyncSugarGrades();
            LoadActive();
        }
        /// <summary>Every machine uses the best unlocked sugar it can hold; the grade is never chosen by hand.</summary>
        public int SugarGrade(int index)
        {
            if (economy.Progression == null) return 1;
            return Math.Max(1, Math.Min(Progression.MachineTier(index), Progression.MaxSugarGrade(economy)));
        }
        void SyncSugarGrades()
        {
            if (economy.Progression == null || State.Machines == null) return;
            for (int i = 0; i < State.Machines.Count && i < 3; i++) State.Machines[i].SugarGrade = SugarGrade(i);
        }
        /// <summary>
        /// Explains the largest size: machine tiers never change, so a machine at or below the
        /// unlocked sugar grade is its own limit; otherwise better sugar raises it. Empty before sizes exist.
        /// </summary>
        public string SizeLimitNote(int index)
        {
            if (economy.Progression == null || index < 0 || index >= 3 || Progression.MaxSugarGrade(economy) <= 1) return "";
            int max = MaxSize(index);
            if (Progression.MachineTier(index) <= Progression.MaxSugarGrade(economy))
                return Strings.Format("shift.size.machine", Progression.MachineName(index), Progression.SizeName(max));
            return Strings.Format("shift.size.sugar", Progression.SizeName(max), Progression.SizeName(max + 1));
        }
        public MachineProduction Machine(int index)
        {
            if (index < 0 || index >= State.Machines.Count) return null;
            SyncSugarGrades();
            if (index == SelectedMachine) SyncActive();
            return State.Machines[index];
        }
        public void SyncActive()
        {
            if (economy.Progression == null || State.Machines == null || State.Machines.Count <= SelectedMachine) return;
            SyncSugarGrades();
            var m = State.Machines[SelectedMachine];
            m.SugarGrams = State.SugarGrams; m.SugarFlavor = State.SugarFlavor;
            m.BatchMeters = State.BatchMeters; m.BatchOverflowMeters = State.BatchOverflowMeters; m.BatchFlavor = State.BatchFlavor; m.BatchQuality = State.BatchQuality;
            m.BatchProductId = State.BatchProductId; m.BatchSugarGrade = State.BatchSugarGrade;
        }
        void LoadActive()
        {
            var m = State.Machines[SelectedMachine];
            State.SugarGrams = m.SugarGrams; State.SugarFlavor = m.SugarFlavor;
            State.BatchMeters = m.BatchMeters; State.BatchOverflowMeters = m.BatchOverflowMeters; State.BatchFlavor = m.BatchFlavor; State.BatchQuality = m.BatchQuality;
            State.BatchProductId = m.BatchProductId; State.BatchSugarGrade = m.BatchSugarGrade;
        }
        public bool SelectMachine(int index)
        {
            if (Tutorial.Active(economy) || economy.Progression == null || Paused || index < 0 || index >= Progression.OwnedMachines(economy) ||
                economy.Progression.Phase == BusinessPhase.Results) return false;
            SyncActive(); economy.Progression.SelectedMachine = index; LoadActive(); return true;
        }
        public bool BeginBusiness()
        {
            if (economy.Progression == null || Paused || economy.Progression.Phase != BusinessPhase.Preparation) return false;
            if (State.Closed) economy.Day++;
            ClearDay(); SyncSugarGrades(); State.Closed = false; State.RemainingSeconds = Duration;
            economy.Progression.Phase = BusinessPhase.Operating; Arrive(false); return true;
        }
        public bool ReturnToPreparation()
        {
            if (economy.Progression == null || Paused || economy.Progression.Phase != BusinessPhase.Results) return false;
            economy.Progression.Phase = BusinessPhase.Preparation;
            ClearDay(); State.Closed = true; State.RemainingSeconds = Duration; return true;
        }
        void ClearDay()
        {
            economy.Inventory.Clear(); economy.CompletedIds.Clear(); economy.Orders.Clear();
            foreach (var m in State.Machines) m.Clear();
            LoadActive(); State.Customers.Clear(); State.Customer = null; State.NextCustomerIn = 0;
            State.DayRevenue = State.DaySold = State.DayWrong = State.DayMissed = State.DayTrashed = State.DayMaterialCost = 0;
        }
        public int MaxSize(int index)
        {
            if (economy.Progression == null) return 2;
            return SugarGrade(index) - 1;
        }
        // The guided first sale is strawberry only, so other flavors open when the tutorial ends.
        public bool CanMakeFlavor(int index, int flavor)
        {
            return MachineMakesFlavor(index, flavor) && (flavor == 0 || !Tutorial.Active(economy));
        }
        // Whether the owned machine and unlocked flavors allow it, before the tutorial's strawberry-only lock.
        public bool MachineMakesFlavor(int index, int flavor)
        {
            return economy.Progression == null ? flavor >= 0 && flavor < 3 : index >= 0 && index < Progression.OwnedMachines(economy) &&
                Progression.HasFlavor(economy, flavor) && Progression.FlavorMachineTier(flavor) <= Progression.MachineTier(index);
        }
        public int PourCost(int index, int flavor, double grams = PourAmount)
        {
            if (economy.Progression == null || flavor == 0 && SugarGrade(index) == 1) return 0;
            return (int)Math.Ceiling(Math.Max(0, grams) / PourAmount * (SugarGrade(index) - 1 + flavor * 2));
        }
        bool PourMachine(int index, int flavor, double grams = PourAmount)
        {
            if (!Finite(grams) || grams <= 1e-7) return false;
            var m = State.Machines[index];
            if (!CanMakeFlavor(index, flavor) || m.BatchMeters > 0 && m.BatchFlavor != flavor) return false;
            double existing = m.SugarFlavor == flavor ? m.SugarGrams : 0;
            double accepted = Math.Min(Math.Min(PourAmount, grams), SugarCapacity - existing);
            if (accepted <= 1e-7) return false;
            int cost = PourCost(index, flavor, accepted);
            if (economy.Coins < cost) return false;
            economy.Coins -= cost; State.DayMaterialCost += cost;
            m.SugarFlavor = flavor; m.SugarGrams = existing + accepted; return true;
        }
        int EffectiveMaxSize(int index)
        {
            var m = State.Machines[index];
            return m.BatchMeters > 0 ? Math.Min(Progression.MachineTier(index), m.BatchSugarGrade) - 1 : MaxSize(index);
        }
        void Grow(int index, double meters)
        {
            var m = State.Machines[index];
            if (!Finite(meters) || meters <= 0 || m.SugarGrams <= 0 || m.BatchMeters > 0 && m.BatchFlavor != m.SugarFlavor) return;
            int grade = m.BatchMeters > 0 ? m.BatchSugarGrade : SugarGrade(index);
            int cap = Math.Min(Progression.MachineTier(index), grade) - 1;
            double needed = Math.Max(0, MetersForSize(cap) - m.BatchMeters);
            double sugarRate = SugarPerMeter * Progression.SugarMultiplier(economy);
            // Winding never stops at the size cap: sugar keeps draining and laps keep counting,
            // but only the part below the cap makes the candy bigger.
            double growth = Math.Min(meters * Progression.GrowthMultiplier(economy), m.SugarGrams / sugarRate);
            if (growth <= 0) return;
            if (m.BatchMeters == 0) { m.BatchFlavor = m.SugarFlavor; m.BatchQuality = QualityForStars(MaxStars); m.BatchSugarGrade = grade; }
            double sized = Math.Min(needed, growth);
            m.BatchMeters += sized;
            m.BatchOverflowMeters += growth - sized;
            for (int size = 0; size < 3; size++) if (Math.Abs(m.BatchMeters - MetersForSize(size)) < 1e-7) m.BatchMeters = MetersForSize(size);
            m.SugarGrams = Math.Max(0, m.SugarGrams - growth * sugarRate);
            if (m.SugarGrams < 1e-9) { m.SugarGrams = 0; m.SugarFlavor = -1; }
        }
        void AdvanceProgression(double seconds, double meters, int wallHits)
        {
            if (!IsOpen || !Finite(seconds) || seconds <= 0) return;
            if (Tutorial.Active(economy))
            {
                // Learning pauses the shop, while the real driving/production model still runs.
                SyncActive();
                if (economy.TutorialStep == TutorialStep.Drive)
                {
                    Grow(SelectedMachine, meters);
                    var practice = State.Machines[SelectedMachine];
                    practice.BatchQuality = AfterWallHits(practice.BatchMeters, practice.BatchQuality, wallHits);
                }
                LoadActive();
                Tutorial.Refresh(economy);
                return;
            }
            double elapsed = Math.Min(seconds, State.RemainingSeconds); SyncActive();
            if (!HasWorker(SelectedMachine))
            {
                Grow(SelectedMachine, meters * elapsed / seconds);
                var driven = State.Machines[SelectedMachine];
                driven.BatchQuality = AfterWallHits(driven.BatchMeters, driven.BatchQuality, wallHits);
            }
            double remaining = elapsed;
            while (remaining > 1e-9)
            {
                double dt = Math.Min(.2, remaining); remaining -= dt;
                for (int i = 0; i < Progression.OwnedMachines(economy); i++)
                {
                    var m = State.Machines[i];
                    if (!WorkerCanOperate(i)) continue;
                    double target = MetersForSize(Math.Min(m.RecipeSize, EffectiveMaxSize(i)));
                    if (m.BatchMeters + 1e-7 < target)
                    {
                        // Finish a handed-over batch with its original flavor and material limit.
                        int flavor = m.BatchMeters > 0 ? m.BatchFlavor : m.RecipeFlavor;
                        if ((m.SugarGrams <= 1e-9 || m.SugarFlavor != flavor) && !PourMachine(i, flavor)) continue;
                        double step = Math.Min(Progression.WorkerMetersPerSecond(economy) * dt,
                            (target - m.BatchMeters) / Progression.GrowthMultiplier(economy));
                        Grow(i, step);
                    }
                    if (m.BatchMeters + 1e-7 >= target)
                    {
                        var p = Preview(m.BatchMeters, m.BatchFlavor);
                        p.Id = string.IsNullOrEmpty(m.BatchProductId) ? Guid.NewGuid().ToString("N") : m.BatchProductId;
                        p.Quality = m.BatchQuality; p.SugarGrade = m.BatchSugarGrade;
                        economy.Inventory.Add(p); if (!economy.CompletedIds.Contains(p.Id)) economy.CompletedIds.Add(p.Id);
                        m.ClearBatch();
                    }
                }
            }
            LoadActive(); AdvanceCustomer(elapsed);
            State.RemainingSeconds = Math.Max(0, State.RemainingSeconds - elapsed);
            if (State.RemainingSeconds == 0)
            {
                State.Closed = true; economy.Progression.Phase = BusinessPhase.Results; DiscardLeftovers();
                State.Customers.Clear(); State.Customer = null; State.NextCustomerIn = 0;
            }
        }
        void ChooseOrder(out int flavor, out int size)
        {
            var choices = new List<int>();
            int location = economy.Progression.SelectedLocation;
            for (int f = 0; f < 3; f++)
                for (int m = 0; m < Progression.OwnedMachines(economy); m++)
                    if (CanMakeFlavor(m, f))
                    {
                        int cap = Math.Min(Progression.MachineTier(m), Progression.MaxSugarGrade(economy)) - 1;
                        for (int z = 0; z <= cap; z++) { int key = f * 3 + z; if (!choices.Contains(key)) choices.Add(key); }
                    }
            int choice = choices[(economy.OrderSerial * (location * 2 + 1) + economy.OrderSerial / Math.Max(1, choices.Count)) % choices.Count];
            flavor = choice / 3; size = choice % 3;
        }
    }
}

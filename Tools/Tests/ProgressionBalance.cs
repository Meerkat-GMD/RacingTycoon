using System;
using System.Collections.Generic;
using CottonCircuit;

// Deterministic strategy simulation, not a human-playtime measurement.
public static class ProgressionBalance
{
    const int MaxDays = 220;
    const double TickSeconds = 1;
    const double ActiveMetersPerSecond = 18;
    static readonly string[] Priority = {
        "hours", "sales", "ads", "stick_speed", "sugar_2", "machine_2",
        "worker_1", "worker_grade_2", "location_1", "shelf", "hours", "sales", "ads",
        "sugar_3", "machine_3", "worker_2", "worker_speed", "flavor_vanilla",
        "location_2", "location_3"
    };
    static readonly List<string> PurchaseOrder = new List<string>();
    static Economy economy;
    static ShopShift shift;
    static double playedSeconds;
    static int firstPurchaseDay, secondMachineDay, finalRegionDay, fullTreeDay;
    static double firstPurchaseSeconds, secondMachineSeconds, finalRegionSeconds, fullTreeSeconds;
    static int totalSales, totalMissed, totalSpent, totalMaterial;
    static int idleDays;

    static void AddOnce(string id)
    {
        var node = Progression.Find(id);
        if (node == null) throw new Exception("Unknown node " + id);
        foreach (string parent in node.Parents) AddOnce(parent);
        if (!PurchaseOrder.Contains(id)) PurchaseOrder.Add(id);
    }
    static void BuildPurchaseOrder()
    {
        foreach (string id in Priority) AddOnce(id);
        foreach (UpgradeNode node in Progression.Nodes) AddOnce(node.Id);
    }
    static bool AllPurchased()
    {
        foreach (var node in Progression.Nodes)
            if (Progression.Level(economy, node.Id) != node.MaxLevel) return false;
        return true;
    }
    static void RecordMilestones()
    {
        if (firstPurchaseDay == 0 && economy.Progression.Purchases.Count > 0)
        { firstPurchaseDay = economy.Day; firstPurchaseSeconds = playedSeconds; }
        if (secondMachineDay == 0 && Progression.OwnedMachines(economy) >= 2)
        { secondMachineDay = economy.Day; secondMachineSeconds = playedSeconds; }
        if (finalRegionDay == 0 && Progression.HasLocation(economy, 3))
        { finalRegionDay = economy.Day; finalRegionSeconds = playedSeconds; }
        if (fullTreeDay == 0 && AllPurchased())
        { fullTreeDay = economy.Day; fullTreeSeconds = playedSeconds; }
    }
    static int BuyAvailable()
    {
        int purchases = 0;
        bool changed;
        do
        {
            changed = false;
            foreach (string id in PurchaseOrder)
            {
                if (!Progression.CanBuy(economy, id) || economy.Coins - Progression.Cost(economy, id) < 60) continue;
                int cost = Progression.Cost(economy, id);
                if (!Progression.Buy(economy, id)) throw new Exception("Buy rejected " + id);
                totalSpent += cost; purchases++; changed = true;
                RecordMilestones();
                break;
            }
        } while (changed);
        return purchases;
    }
    static int BestMachine(int flavor, int size)
    {
        for (int i = Progression.OwnedMachines(economy) - 1; i >= 0; i--)
            if (shift.CanMakeFlavor(i, flavor) &&
                Math.Min(Progression.MachineTier(i), Progression.MaxSugarGrade(economy)) > size) return i;
        return -1;
    }
    static bool HasStock(int flavor, int size)
    {
        foreach (Product product in economy.Inventory)
            if (product.FlavorIndex == flavor && ShopShift.SizeOf(product) == size) return true;
        return false;
    }
    static void DeliverMatches()
    {
        var customers = new List<ShopCustomer>(shift.State.Customers);
        foreach (ShopCustomer customer in customers)
        {
            if (customer.Angry || customer.Happy) continue;
            Product found = economy.Inventory.Find(product =>
                product.FlavorIndex == customer.Flavor && ShopShift.SizeOf(product) == customer.Size);
            if (found == null) continue;
            if (shift.Deliver(found.Id, customer.Id) != DeliveryResult.Sold)
                throw new Exception("Matching sale rejected");
        }
    }
    static ShopCustomer ChooseOrder()
    {
        ShopCustomer best = null;
        foreach (ShopCustomer customer in shift.State.Customers)
        {
            if (customer.Angry || customer.Happy || HasStock(customer.Flavor, customer.Size)) continue;
            if (BestMachine(customer.Flavor, customer.Size) < 0) continue;
            if (best == null || customer.PatienceRemaining < best.PatienceRemaining) best = customer;
        }
        return best;
    }
    static void ConfigureWorkers(int active)
    {
        if (economy.Progression.Phase != BusinessPhase.Preparation)
            throw new Exception("Worker/menu configuration is only available in preparation");
        for (int i = 0; i < Progression.OwnedMachines(economy); i++)
        {
            MachineProduction machine = shift.Machine(i);
            bool assigned = i != active && i < Progression.WorkerCount(economy) &&
                Progression.WorkerGrade(economy) >= Progression.MachineTier(i);
            int flavor = i == 0 || !Progression.HasFlavor(economy, 1) ? 0 : 1;
            int size = i == 0 ? 0 : 1;
            machine.WorkerAssigned = assigned;
            machine.RecipeFlavor = flavor;
            machine.RecipeSize = Math.Min(size, shift.MaxSize(i));
        }
    }
    // Sugar grade is automatic, so the player stops each batch at the ordered size with F.
    static readonly int[] batchTargetSize = { 0, 0, 0 };
    static void PlayDay()
    {
        economy.Progression.SelectedLocation =
            Progression.HasLocation(economy, 3) ? 3 : Progression.HasLocation(economy, 2) ? 2 :
            Progression.HasLocation(economy, 1) ? 1 : 0;
        int active = Progression.OwnedMachines(economy) - 1;
        if (!shift.SelectMachine(active)) throw new Exception("Could not select prepared machine");
        ConfigureWorkers(active);
        if (!shift.BeginBusiness()) throw new Exception("Could not begin day " + economy.Day);
        while (shift.IsOpen)
        {
            DeliverMatches();
            ShopCustomer order = ChooseOrder();
            MachineProduction current = shift.Machine(shift.SelectedMachine);
            bool finishing = current.BatchMeters > 1e-7;
            int selected = finishing ? shift.SelectedMachine :
                order == null ? Progression.OwnedMachines(economy) - 1 : BestMachine(order.Flavor, order.Size);
            if (selected < 0) selected = 0;
            if (shift.SelectedMachine != selected && !shift.SelectMachine(selected))
                throw new Exception("Could not switch machine");
            double meters = 0;
            if (economy.Inventory.Count == economy.StockCapacity && order != null)
                shift.Discard(economy.Inventory[0].Id);
            if (economy.Inventory.Count < economy.StockCapacity)
            {
                MachineProduction machine = shift.Machine(selected);
                if (finishing)
                {
                    int size = Math.Min(batchTargetSize[selected],
                        Math.Min(Progression.MachineTier(selected), machine.BatchSugarGrade) - 1);
                    if (machine.BatchMeters >= ShopShift.MetersForSize(size) - 1e-7) shift.Extract();
                    else
                    {
                        if (shift.State.SugarGrams < 1e-7) shift.Pour(machine.BatchFlavor);
                        meters = ActiveMetersPerSecond * Progression.SpeedMultiplier(economy);
                    }
                }
                else if (order != null)
                {
                    batchTargetSize[selected] = order.Size;
                    if (shift.State.SugarGrams > 1e-7 && shift.State.SugarFlavor != order.Flavor)
                        shift.EmptySugar();
                    if (shift.State.SugarGrams < 1e-7) shift.Pour(order.Flavor);
                    meters = ActiveMetersPerSecond * Progression.SpeedMultiplier(economy);
                }
            }
            shift.Advance(TickSeconds, meters);
            playedSeconds += TickSeconds;
            DeliverMatches();
            if (playedSeconds > MaxDays * 300.0) throw new Exception("Simulation time guard exceeded");
        }
        totalSales += shift.State.DaySold;
        totalMissed += shift.State.DayMissed;
        totalMaterial += shift.State.DayMaterialCost;
        if (shift.State.DaySold == 0) idleDays++;
        Console.WriteLine("DAY " + economy.Day + " elapsed=" + (playedSeconds / 60).ToString("F1") +
            "m sales=" + shift.State.DaySold + " missed=" + shift.State.DayMissed +
            " revenue=" + shift.State.DayRevenue + " material=" + shift.State.DayMaterialCost +
            " coins=" + economy.Coins + " ranks=" + economy.Progression.Purchases.Count);
        if (!shift.ReturnToPreparation()) throw new Exception("Could not return to preparation");
    }
    public static int Main()
    {
        try
        {
            BuildPurchaseOrder();
            economy = new Economy(); Progression.Enable(economy); shift = new ShopShift(economy);
            for (int day = 0; day < MaxDays && !AllPurchased(); day++)
            {
                BuyAvailable();
                if (AllPurchased()) break;
                PlayDay();
            }
            RecordMilestones();
            Console.WriteLine("MILESTONE first_purchase day=" + firstPurchaseDay + " hours=" + (firstPurchaseSeconds / 3600).ToString("F2"));
            Console.WriteLine("MILESTONE second_machine day=" + secondMachineDay + " hours=" + (secondMachineSeconds / 3600).ToString("F2"));
            Console.WriteLine("MILESTONE final_region day=" + finalRegionDay + " hours=" + (finalRegionSeconds / 3600).ToString("F2"));
            Console.WriteLine("MILESTONE full_tree day=" + fullTreeDay + " hours=" + (fullTreeSeconds / 3600).ToString("F2"));
            Console.WriteLine("TOTAL sales=" + totalSales + " missed=" + totalMissed + " spent=" + totalSpent +
                " material=" + totalMaterial + " idle_days=" + idleDays + " coins=" + economy.Coins);
            if (!AllPurchased() || firstPurchaseDay == 0 || secondMachineDay == 0 ||
                finalRegionDay == 0 || idleDays > 0) throw new Exception("Progression stalled or milestone missing");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("SIMULATION FAIL " + ex); return 1; }
    }
}

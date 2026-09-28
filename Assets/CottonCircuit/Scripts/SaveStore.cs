using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace CottonCircuit
{
    public class SaveStore
    {
        [Serializable] public class Envelope
        {
            public int Version = 9;
            public Economy State;
            // Unity's inline class serialization replaces null with a default object.
            // Keep presence outside the inline data so legacy mode and customer gaps survive a reload.
            public bool HasBusiness;
            public bool HasCustomer;
            public bool HasProgression;
        }
        readonly string directory;
        public string DirectoryPath { get { return directory; } }
        string FilePath => Path.Combine(directory, "cotton-circuit.json");
        public bool HasSave => File.Exists(FilePath);
        public string Error { get; private set; }
        public bool CanSave { get; private set; } = true;
        public SaveStore(string directory) { this.directory = directory; }
        public Economy Load()
        {
            if (!File.Exists(FilePath)) return new Economy();
            try
            {
                return Read(FilePath);
            }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException || e is UnauthorizedAccessException)
            {
                try
                {
                    var backup = FilePath + ".bak";
                    if (File.Exists(backup))
                    {
                        Economy recovered = Read(backup);
                        // Keep the damaged payload for diagnosis and retain the good backup.
                        File.Move(FilePath, FilePath + ".invalid-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
                        File.Copy(backup, FilePath);
                        Error = Strings.Get("save.recovered");
                        return recovered;
                    }
                }
                catch (Exception backupError) when (backupError is IOException || backupError is InvalidDataException ||
                    backupError is ArgumentException || backupError is UnauthorizedAccessException) { }
                Error = Strings.Get("save.unreadable");
                CanSave = false;
                return new Economy();
            }
        }
        static Economy Read(string path)
        {
            if (new FileInfo(path).Length > 8 * 1024 * 1024) throw new InvalidDataException();
            var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
            if (!RestoreOptionalState(envelope) || !Upgrade(envelope)) throw new InvalidDataException();
            return envelope.State;
        }
        public bool Save(Economy state)
        {
            if (!CanSave) return false;
            if (!Valid(state))
            { Error = Strings.Get("save.invalid"); return false; }
            try
            {
                Directory.CreateDirectory(directory);
                var temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(new Envelope {
                    State = state,
                    HasBusiness = state.Business != null,
                    HasCustomer = false,
                    HasProgression = state.Progression != null
                }, true));
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
                else File.Move(temporary, FilePath);
                Error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Error = Strings.Get("save.write"); return false; }
        }
        public bool ArchiveAndReset()
        {
            try
            {
                if (File.Exists(FilePath)) File.Move(FilePath, FilePath + ".archived-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
                CanSave = true; Error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Error = Strings.Get("save.archive"); return false; }
        }
        public static bool Valid(Economy e)
        { return Valid(e, CustomerOrder.Patience, true); }
        static bool Valid(Economy e, double patience, bool validateQuality, bool legacyBusiness = false, bool legacyVehicle = false)
        {
            if (!Tutorial.Valid(e) || e.ShelfLevel < 0 || e.ShelfLevel > 2 ||
                e.Orders == null || e.Orders.Count > 2 || e.OrderSerial < 0 ||
                e.TotalTips < 0 || e.MissedOrders < 0 || e.OrdersServed < 0 ||
                e.OrdersServed > e.TotalSold || e.OrdersServed > e.OrderSerial ||
                (long)e.OrdersServed + e.MissedOrders + e.Orders.Count > e.OrderSerial ||
                e.TotalTips > e.LifetimeRevenue ||
                !Finite(e.NextCustomerIn) || e.NextCustomerIn <= 0 || e.NextCustomerIn > 15 ||
                !Finite(e.SatisfactionTotal) || e.SatisfactionTotal < 0 ||
                e.SatisfactionTotal > e.OrdersServed || !ValidBase(e, e.StockCapacity, validateQuality)) return false;
            var orderIds = new HashSet<string>();
            foreach (var order in e.Orders)
            {
                if (order == null || string.IsNullOrEmpty(order.Id) || !orderIds.Add(order.Id) ||
                    order.Flavor < 0 || order.Flavor > 2 || order.Size < 0 || order.Size > 1 ||
                    !Finite(order.Remaining) || order.Remaining <= 0 ||
                    order.Remaining > patience) return false;
                int serial;
                if (!order.Id.StartsWith("order-", StringComparison.Ordinal) ||
                    !int.TryParse(order.Id.Substring(6), out serial) || serial <= 0 ||
                    order.Id != "order-" + serial || serial > e.OrderSerial) return false;
            }
            return e.Progression == null ? ValidBusiness(e, legacyBusiness) :
                ValidProgression(e, legacyVehicle) && ValidBusiness(e, false);
        }
        static bool ValidBusiness(Economy e, bool legacy)
        {
            var business = e.Business;
            bool progression = e.Progression != null;
            if (business == null) return !progression || e.Progression.Phase == BusinessPhase.Preparation;
            double daySeconds = progression ? Progression.DaySeconds(e) : ShopShift.DayDuration;
            double arrivalSeconds = progression ? Progression.ArrivalSeconds(e) : ShopShift.ArrivalDelay;
            double customerPatience = progression ? Progression.PatienceSeconds(e) : ShopShift.CustomerPatience;
            if (e.Orders.Count != 0 ||
                !Finite(business.RemainingSeconds) || business.RemainingSeconds < 0 || business.RemainingSeconds > daySeconds ||
                (!progression && business.Closed != (business.RemainingSeconds == 0)) ||
                (progression && (e.Progression.Phase == BusinessPhase.Operating && business.Closed ||
                    e.Progression.Phase == BusinessPhase.Results && (!business.Closed || business.RemainingSeconds != 0) ||
                    e.Progression.Phase == BusinessPhase.Preparation && business.RemainingSeconds != daySeconds)) ||
                !Finite(business.SugarGrams) || business.SugarGrams < 0 || business.SugarGrams > ShopShift.SugarCapacity ||
                business.SugarFlavor < -1 || business.SugarFlavor > 2 ||
                (business.SugarGrams > 0 && business.SugarFlavor < 0) ||
                !Finite(business.BatchMeters) || business.BatchMeters < 0 ||
                !Finite(business.BatchOverflowMeters) || business.BatchOverflowMeters < 0 ||
                business.BatchOverflowMeters > 0 && business.BatchMeters == 0 ||
                business.BatchFlavor < -1 || business.BatchFlavor > 2 ||
                (business.BatchMeters == 0) != (business.BatchFlavor == -1) ||
                (legacy && business.BatchMeters > 0 && business.SugarGrams > 0 && business.BatchFlavor != business.SugarFlavor) ||
                business.BatchQuality < 0 || business.BatchQuality > 100 ||
                business.DayRevenue < 0 || business.DayRevenue > e.LifetimeRevenue ||
                business.DaySold < 0 || business.DaySold > e.TotalSold ||
                business.DayMissed < 0 || business.DayMissed > e.MissedOrders ||
                business.DayWrong < 0 || business.DayWrong > e.OrderSerial || business.DayTrashed < 0 ||
                !Finite(business.NextCustomerIn) || business.NextCustomerIn < 0 || business.NextCustomerIn > (legacy ? 2 : arrivalSeconds) ||
                (progression && (business.DayMaterialCost < 0 || business.DayMaterialCost > 1000000000 ||
                    business.BatchSugarGrade < 1 || business.BatchSugarGrade > Progression.MaxSugarGrade(e))))
                return false;
            if (!legacy && !string.IsNullOrEmpty(business.BatchProductId) &&
                (business.BatchMeters <= 0 || !e.CompletedIds.Contains(business.BatchProductId) ||
                 e.Inventory.Exists(product => product.Id == business.BatchProductId))) return false;
            if (business.Customer != null || business.Customers == null || business.Customers.Count > ShopShift.CustomerCapacity)
                return false;
            if (progression && e.Progression.Phase == BusinessPhase.Preparation &&
                (business.Customers.Count != 0 || business.NextCustomerIn != 0 || e.Inventory.Count != 0 ||
                 business.SugarGrams != 0 || business.BatchMeters != 0 || business.BatchOverflowMeters != 0 ||
                 !string.IsNullOrEmpty(business.BatchProductId))) return false;
            if (business.Closed) return business.Customers.Count == 0 && business.NextCustomerIn == 0 &&
                (!progression || ValidMachines(e));
            var ids = new HashSet<string>();
            var slots = new HashSet<int>();
            int unaccountedCustomers = 0, happyCustomers = 0, timedOutCustomers = 0;
            foreach (var customer in business.Customers)
            {
                if (customer == null || customer.Slot < 0 || customer.Slot >= ShopShift.CustomerCapacity || !slots.Add(customer.Slot) ||
                    customer.Flavor < 0 || customer.Flavor > 2 || customer.Size < 0 || customer.Size > 2 ||
                    string.IsNullOrEmpty(customer.Id) || !ids.Add(customer.Id) || !Finite(customer.ReactionRemaining) ||
                    ((customer.Angry || !legacy && customer.Happy) ? customer.ReactionRemaining <= 0 || customer.ReactionRemaining > ShopShift.ReactionDuration : customer.ReactionRemaining != 0)) return false;
                if (!legacy && (customer.Angry && customer.Happy || customer.TimedOut && !customer.Angry ||
                    !Finite(customer.PatienceRemaining) || customer.PatienceRemaining < 0 || customer.PatienceRemaining > customerPatience ||
                    (!customer.Angry && !customer.Happy && customer.PatienceRemaining <= 0) ||
                    (customer.TimedOut && customer.PatienceRemaining != 0))) return false;
                if (legacy || !customer.Happy && !customer.TimedOut) unaccountedCustomers++;
                if (customer.Happy) happyCustomers++;
                if (customer.TimedOut) timedOutCustomers++;
                int serial;
                if (!customer.Id.StartsWith("shop-", StringComparison.Ordinal) ||
                    !int.TryParse(customer.Id.Substring(5), out serial) || serial <= 0 || serial > e.OrderSerial ||
                    customer.Id != "shop-" + serial) return false;
            }
            if (!legacy && (happyCustomers > business.DaySold || timedOutCustomers > business.DayMissed)) return false;
            if ((long)e.OrdersServed + e.MissedOrders + unaccountedCustomers > e.OrderSerial) return false;
            return (!legacy || business.Customers.Count != ShopShift.CustomerCapacity || business.NextCustomerIn == 0) &&
                (!progression || ValidMachines(e));
        }
        static bool ValidProgression(Economy e, bool legacyVehicle)
        {
            var state = e.Progression;
            if (state == null || state.Purchases == null || state.Purchases.Count > Progression.Nodes.Length ||
                state.Phase < BusinessPhase.Preparation || state.Phase > BusinessPhase.Results ||
                state.SelectedMachine < 0 || state.SelectedMachine >= Progression.OwnedMachines(e) ||
                state.SelectedLocation < 0 || state.SelectedLocation > 3 || !Progression.HasLocation(e, state.SelectedLocation) ||
                (legacyVehicle ? state.CartStyle < 0 || state.CartStyle > 1 ||
                    state.CartStyle == 1 && Progression.Level(e, "coupe") == 0 :
                    !Progression.HasCartStyle(e, state.CartStyle)) ||
                state.Phase == BusinessPhase.Operating && (e.Business == null || e.Business.Closed) ||
                state.Phase == BusinessPhase.Results && (e.Business == null || !e.Business.Closed))
                return false;
            var ids = new HashSet<string>();
            foreach (var purchase in state.Purchases)
            {
                if (purchase == null || string.IsNullOrEmpty(purchase.Id) || !ids.Add(purchase.Id)) return false;
                var node = Progression.Find(purchase.Id);
                if (node == null || purchase.Level < 1 || purchase.Level > node.MaxLevel) return false;
            }
            foreach (var purchase in state.Purchases)
                foreach (var parent in Progression.Find(purchase.Id).Parents)
                    if (Progression.Level(e, parent) < 1) return false;
            return true;
        }
        static bool ValidMachines(Economy e)
        {
            var business = e.Business;
            if (business.Machines == null || business.Machines.Count != 3) return false;
            int workers = 0;
            for (int i = 0; i < business.Machines.Count; i++)
            {
                var machine = business.Machines[i];
                if (machine == null || !Finite(machine.SugarGrams) || machine.SugarGrams < 0 || machine.SugarGrams > ShopShift.SugarCapacity ||
                    machine.SugarFlavor < -1 || machine.SugarFlavor > 2 || machine.SugarGrams > 0 && machine.SugarFlavor < 0 ||
                    !Finite(machine.BatchMeters) || machine.BatchMeters < 0 ||
                    !Finite(machine.BatchOverflowMeters) || machine.BatchOverflowMeters < 0 ||
                    machine.BatchOverflowMeters > 0 && machine.BatchMeters == 0 ||
                    machine.BatchFlavor < -1 || machine.BatchFlavor > 2 || (machine.BatchMeters == 0) != (machine.BatchFlavor == -1) ||
                    machine.BatchQuality < 0 || machine.BatchQuality > 100 ||
                    machine.SugarGrade < 1 || machine.SugarGrade > Progression.MaxSugarGrade(e) ||
                    machine.BatchSugarGrade < 1 || machine.BatchSugarGrade > Progression.MaxSugarGrade(e) ||
                    machine.BatchMeters > 0 && machine.BatchSugarGrade > machine.SugarGrade ||
                    machine.RecipeFlavor < 0 || machine.RecipeFlavor > 2 || machine.RecipeSize < 0 || machine.RecipeSize > 2)
                    return false;
                bool owned = i < Progression.OwnedMachines(e);
                if (e.Progression.Phase == BusinessPhase.Preparation &&
                    (machine.SugarGrams != 0 || machine.BatchMeters != 0 || !string.IsNullOrEmpty(machine.BatchProductId))) return false;
                if ((!owned && (machine.WorkerAssigned || machine.SugarGrams > 0 || machine.BatchMeters > 0 ||
                    !string.IsNullOrEmpty(machine.BatchProductId))) ||
                    machine.WorkerAssigned && (i + 1 > Progression.WorkerGrade(e) || ++workers > Progression.WorkerCount(e)) ||
                    !Progression.HasFlavor(e, machine.RecipeFlavor) ||
                    Progression.FlavorMachineTier(machine.RecipeFlavor) > Progression.MachineTier(i) ||
                    machine.RecipeSize >= Math.Min(Progression.MachineTier(i), machine.SugarGrade) ||
                    machine.SugarFlavor >= 0 && (!Progression.HasFlavor(e, machine.SugarFlavor) ||
                        Progression.FlavorMachineTier(machine.SugarFlavor) > Progression.MachineTier(i)) ||
                    machine.BatchFlavor >= 0 && (!Progression.HasFlavor(e, machine.BatchFlavor) ||
                        Progression.FlavorMachineTier(machine.BatchFlavor) > Progression.MachineTier(i)) ||
                    machine.BatchMeters > ShopShift.MetersForSize(Math.Min(machine.BatchSugarGrade,
                        Progression.MachineTier(i)) - 1) + 1e-6 ||
                    !string.IsNullOrEmpty(machine.BatchProductId) &&
                        (machine.BatchMeters <= 0 || !e.CompletedIds.Contains(machine.BatchProductId) ||
                         e.Inventory.Exists(product => product.Id == machine.BatchProductId)))
                    return false;
            }
            var active = business.Machines[e.Progression.SelectedMachine];
            if (business.SugarGrams != active.SugarGrams || business.SugarFlavor != active.SugarFlavor ||
                business.BatchMeters != active.BatchMeters || business.BatchOverflowMeters != active.BatchOverflowMeters ||
                business.BatchFlavor != active.BatchFlavor ||
                business.BatchQuality != active.BatchQuality || business.BatchProductId != active.BatchProductId ||
                business.BatchSugarGrade != active.BatchSugarGrade) return false;
            return true;
        }
        static bool RestoreOptionalState(Envelope envelope)
        {
            if (envelope == null || envelope.State == null || envelope.Version < 1 || envelope.Version > 9) return false;
            if (envelope.Version < 7 || !envelope.HasProgression)
            {
                if (envelope.HasProgression || !AbsentProgressionPlaceholder(envelope.State.Progression)) return false;
                envelope.State.Progression = null;
            }
            else if (envelope.State.Progression == null) return false;
            var business = envelope.State.Business;
            if (envelope.Version < 4 || !envelope.HasBusiness)
            {
                // Old versions have no business state. Accept only absent data or the precise
                // placeholder Unity writes for null, never discard a meaningful or corrupt payload.
                if (envelope.HasBusiness || envelope.HasCustomer || !AbsentBusinessPlaceholder(business)) return false;
                envelope.State.Business = null;
                return true;
            }
            if (business == null) return false;
            if (envelope.Version == 4)
            {
                if (envelope.HasCustomer)
                {
                    if (business.Customer == null || business.Customers != null && business.Customers.Count != 0) return false;
                    business.Customer.Slot = 0;
                    business.Customers = new List<ShopCustomer> { business.Customer };
                    business.Customer = null;
                    if (!business.Closed && business.NextCustomerIn == 0) business.NextCustomerIn = 2;
                }
                else
                {
                    if (!AbsentCustomerPlaceholder(business.Customer)) return false;
                    business.Customer = null;
                    business.Customers = new List<ShopCustomer>();
                }
                return true;
            }
            if (envelope.HasCustomer || !AbsentCustomerPlaceholder(business.Customer)) return false;
            business.Customer = null;
            if (business.Customers == null) return false;
            return true;
        }
        static bool AbsentBusinessPlaceholder(BusinessState business)
        {
            return business == null ||
                (business.RemainingSeconds == ShopShift.DayDuration && !business.Closed &&
                business.SugarGrams == 0 && business.SugarFlavor == -1 &&
                business.BatchMeters == 0 && business.BatchOverflowMeters == 0 && business.BatchFlavor == -1 && business.BatchQuality == 50 && string.IsNullOrEmpty(business.BatchProductId) &&
                business.DayMissed == 0 && business.DayRevenue == 0 && business.DaySold == 0 && business.DayWrong == 0 && business.DayTrashed == 0 &&
                business.NextCustomerIn == 0 && AbsentCustomerPlaceholder(business.Customer) &&
                (business.Customers == null || business.Customers.Count == 0));
        }
        static bool AbsentProgressionPlaceholder(ProgressionState state)
        {
            return state == null || (state.Phase == BusinessPhase.Preparation &&
                (state.Purchases == null || state.Purchases.Count == 0) &&
                state.SelectedMachine == 0 && state.SelectedLocation == 0 &&
                (state.CartStyle == 0 || state.CartStyle == 1));
        }
        static bool AbsentCustomerPlaceholder(ShopCustomer customer)
        {
            return customer == null || (string.IsNullOrEmpty(customer.Id) && customer.Flavor == 0 &&
                customer.Size == 0 && !customer.Angry && !customer.Happy && !customer.TimedOut &&
                (customer.PatienceRemaining == 0 || customer.PatienceRemaining == ShopShift.CustomerPatience) && customer.ReactionRemaining == 0 && customer.Slot == 0);
        }
        static bool Upgrade(Envelope envelope)
        {
            if (envelope == null || envelope.State == null) return false;
            var state = envelope.State;
            if (envelope.Version == 1)
            {
                if (!ValidBase(state, Economy.InventoryLimit, false)) return false;
                MigrateLegacy(state);
            }
            else if (envelope.Version == 2)
            {
                // Validate against the old deadline before preserving each waiting ratio.
                if (!Valid(state, 120, false)) return false;
                foreach (var order in state.Orders) order.Remaining *= CustomerOrder.Patience / 120;
            }
            else if (envelope.Version != 3 && envelope.Version != 4 && envelope.Version != 5 && envelope.Version != 6 &&
                envelope.Version != 7 && envelope.Version != 8 && envelope.Version != 9) return false;
            if (envelope.Version < 3)
                foreach (var product in state.Inventory) product.Quality = 0;
            if (envelope.Version < 7)
                foreach (var product in state.Inventory) if (product.SugarGrade == 0) product.SugarGrade = 1;
            if (envelope.Version >= 4 && envelope.Version < 6 && state.Business != null)
            {
                // Validate old constraints before migration; never normalize corrupt old data.
                if (!Valid(state, CustomerOrder.Patience, true, true)) return false;
                var business = state.Business;
                business.BatchProductId = null;
                business.DayMissed = 0;
                if (!business.Closed)
                    business.NextCustomerIn = business.Customers.Count == ShopShift.CustomerCapacity ?
                        ShopShift.ArrivalDelay : business.NextCustomerIn / 2 * ShopShift.ArrivalDelay;
                foreach (var customer in business.Customers)
                {
                    customer.Happy = customer.TimedOut = false;
                    customer.PatienceRemaining = ShopShift.CustomerPatience;
                }
            }
            // Runs before the V7 check below, which validates against the current trait catalog.
            bool sodaRefund = false;
            if (envelope.Version < 9 && state.Progression != null && !RemoveSodaTrait(state, out sodaRefund)) return false;
            if (envelope.Version == 7 && state.Progression != null)
            {
                // In V7, kart was free and downhill required the coupe node. Validate those
                // original rules before moving an automatic default to the new free vehicle.
                // Owners of the alternate vehicle retain either explicitly available choice.
                if (!Valid(state, CustomerOrder.Patience, true, false, true)) return false;
                if (state.Progression.CartStyle == 0 && Progression.Level(state, "coupe") == 0)
                    state.Progression.CartStyle = 1;
            }
            if (!Valid(state)) return false;
            // The refund lands only after every check has seen the balance the old file stored.
            if (sodaRefund) state.Coins = Math.Min(1000000000, state.Coins + 310);
            envelope.Version = 9;
            return true;
        }
        // V9 made soda a starting flavor and removed its trait. Before V9, soda needed that trait (one rank,
        // bought after the second machine, required by two dependants) and a machine above the basic one.
        // Files that broke those rules are rejected; otherwise the purchase is dropped for a refund of its price.
        static bool RemoveSodaTrait(Economy e, out bool refund)
        {
            refund = false;
            var purchases = e.Progression.Purchases;
            if (purchases == null) return false;
            var records = purchases.FindAll(purchase => purchase != null && purchase.Id == "flavor_soda");
            bool owned = records.Count == 1 && records[0].Level == 1 && Progression.Level(e, "machine_2") > 0;
            if (records.Count > 0 && !owned ||
                !owned && (Progression.Level(e, "flavor_price") > 0 || Progression.Level(e, "quality_focus") > 0)) return false;
            if (e.Business != null && e.Business.Machines != null)
                for (int i = 0; i < e.Business.Machines.Count; i++)
                {
                    var machine = e.Business.Machines[i];
                    bool soda = machine != null && (machine.RecipeFlavor == 1 || machine.SugarFlavor == 1 || machine.BatchFlavor == 1);
                    if (soda && (!owned || i == 0)) return false;
                }
            if (!owned && e.Inventory != null && e.Inventory.Exists(product => product != null && product.DistanceBased && product.FlavorIndex == 1))
                return false;
            if (owned) purchases.Remove(records[0]);
            refund = owned;
            return true;
        }
        static void MigrateLegacy(Economy e)
        {
            e.ShelfLevel = 0;
            e.Orders = new List<CustomerOrder>();
            e.OrderSerial = 0;
            e.TotalTips = 0;
            e.MissedOrders = 0;
            e.NextCustomerIn = 15;
            e.SatisfactionTotal = 0;
            e.OrdersServed = 0;
        }
        static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        static bool ValidBase(Economy e, int stockCapacity, bool validateQuality)
        {
            if (e == null || e.Coins < 0 || e.Coins > 1000000000 || e.Day < 1 || e.Levels == null || e.Levels.Length != 3 ||
                e.Inventory == null || e.Inventory.Count > stockCapacity || e.CompletedIds == null || e.TotalSold < 0 || e.LifetimeRevenue < 0) return false;
            foreach (int level in e.Levels) if (level < 0 || level > 3) return false;
            var ids = new HashSet<string>();
            foreach (var product in e.Inventory)
            {
                if (product == null || string.IsNullOrEmpty(product.Id) || !ids.Add(product.Id) || product.Samples == null ||
                    product.Samples.Count == 0 || product.Samples.Count > (product.DistanceBased ? ShopShift.MaximumPreviewSamples : 230) ||
                    product.Grams != product.Samples.Count * 2 ||
                    (e.Progression != null && (product.SugarGrade < 1 || product.SugarGrade > Progression.MaxSugarGrade(e))) ||
                    (e.Progression != null && product.DistanceBased &&
                        (!Progression.HasFlavor(e, product.FlavorIndex) ||
                         Progression.FlavorMachineTier(product.FlavorIndex) > Progression.OwnedMachines(e) ||
                         product.DistanceMeters > ShopShift.MetersForSize(
                             Math.Min(Progression.OwnedMachines(e), product.SugarGrade) - 1) + 1e-6)) ||
                    (product.DistanceBased && (!Finite(product.DistanceMeters) || product.DistanceMeters <= 0 ||
                        product.FlavorIndex < 0 || product.FlavorIndex > 2)) ||
                    (validateQuality && (product.Quality < 0 || product.Quality > 100))) return false;
                double previous = 0;
                foreach (var sample in product.Samples)
                {
                    if (sample == null || sample.Flavor < 0 || sample.Flavor > 2 ||
                        (product.DistanceBased && sample.Flavor != product.FlavorIndex) ||
                        !Finite(sample.Radius) || sample.Radius < 7 || sample.Radius > 13 ||
                        !Finite(sample.Angle) || sample.Angle <= previous || sample.Angle > 100) return false;
                    previous = sample.Angle;
                }
                if (!e.CompletedIds.Contains(product.Id)) e.CompletedIds.Add(product.Id);
            }
            return true;
        }
    }
}

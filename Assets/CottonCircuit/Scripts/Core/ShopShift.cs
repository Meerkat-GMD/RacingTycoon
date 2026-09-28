using System;
using System.Collections.Generic;

namespace CottonCircuit
{
    [Serializable] public class BusinessState
    {
        public double RemainingSeconds = ShopShift.DayDuration;
        public bool Closed;
        public double SugarGrams;
        public int SugarFlavor = -1;
        public double BatchMeters;
        public double BatchOverflowMeters;
        public int BatchFlavor = -1;
        public int BatchQuality = 50;
        public string BatchProductId;
        public int BatchSugarGrade = 1;
        public int DayMaterialCost;
        public List<MachineProduction> Machines = new List<MachineProduction>();
        public int DayRevenue;
        public int DaySold;
        public int DayWrong;
        public int DayMissed;
        public int DayTrashed;
        public double NextCustomerIn;
        public List<ShopCustomer> Customers = new List<ShopCustomer>();
        // V4 save payload only. SaveStore migrates this field into Customers.
        public ShopCustomer Customer;
    }

    [Serializable] public class ShopCustomer
    {
        public string Id;
        public int Flavor;
        public int Size;
        public bool Angry;
        public bool Happy;
        public bool TimedOut;
        public double PatienceRemaining = ShopShift.CustomerPatience;
        public double ReactionRemaining;
        public int Slot;
    }

    public enum DeliveryResult { Rejected, Sold, Wrong }

    public partial class ShopShift
    {
        public const double DayDuration = 600;
        public const double SugarCapacity = 100;
        public const double PourAmount = 10;
        public static readonly double LapMeters = RaceCourse.Shared.Length;
        public static readonly double SugarPerMeter = 50 / LapMeters;
        public const double ArrivalDelay = 60;
        public const double CustomerPatience = 90;
        public const double ReactionDuration = 1.5;
        public const double AngryDuration = 1.5;
        public const int CustomerCapacity = 3;
        public const int TierPrice = 30;
        public const int MaximumPreviewSamples = 180;
        // Driven candy grows faster than speed: nothing sticks below the warm-up speed,
        // a clean lap at the base engine speed grows one lap of candy, and upgraded
        // engines, boosts or downhill runs grow more per meter. Sugar follows growth.
        public const double ReferenceSpeed = 26;
        public const double WarmupSpeedRatio = .2;
        public const double MaximumSpeedYield = 1.6;

        // Quality keeps its saved 0-100 scale; stars are the nearest third of it,
        // so older saved scores still read as a sensible star count.
        public const int MaxStars = 3;
        public static int Stars(int quality)
        {
            return Math.Max(0, Math.Min(MaxStars, (int)Math.Round(quality * MaxStars / 100.0, MidpointRounding.AwayFromZero)));
        }
        public static int QualityForStars(int stars)
        {
            return (int)Math.Round(Math.Max(0, Math.Min(MaxStars, stars)) * 100.0 / MaxStars, MidpointRounding.AwayFromZero);
        }
        public static string StarText(int quality)
        {
            int stars = Stars(quality);
            return new string('★', stars) + new string('☆', MaxStars - stars);
        }

        // Each new wall contact knocks one star off the candy on the stick,
        // even while it is not growing; with no candy there is nothing to lose.
        static int AfterWallHits(double batchMeters, int quality, int wallHits)
        {
            return batchMeters > 0 && wallHits > 0 ? QualityForStars(Stars(quality) - wallHits) : quality;
        }

        public static double SpeedYield(double speed)
        {
            if (!Finite(speed) || speed <= 0) return 0;
            double warmed = (speed / ReferenceSpeed - WarmupSpeedRatio) / (1 - WarmupSpeedRatio);
            return warmed <= 0 ? 0 : Math.Min(MaximumSpeedYield, Math.Pow(warmed, 1.3));
        }
        readonly Economy economy;
        public BusinessState State { get { return economy.Business; } }
        public bool Paused { get; set; }
        public bool IsOpen { get { return !Paused && DayRunning; } }
        // Open ignoring pause: the day has time left and the business is operating.
        public bool DayRunning { get { return !State.Closed && State.RemainingSeconds > 0 &&
            (economy.Progression == null || economy.Progression.Phase == BusinessPhase.Operating); } }

        public ShopCustomer CustomerAt(int slot)
        {
            if (slot < 0 || slot >= CustomerCapacity || State.Customers == null) return null;
            return State.Customers.Find(customer => customer != null && customer.Slot == slot);
        }

        public ShopShift(Economy economy)
        {
            if (economy == null) throw new ArgumentNullException("economy");
            this.economy = economy;
            bool firstShift = economy.Business == null;
            if (firstShift) economy.Business = new BusinessState();
            if (economy.Progression != null) InitializeMachines(firstShift);
            if (State.Customers == null) State.Customers = new List<ShopCustomer>();
            if (State.Customer != null)
            {
                if (State.Customers.Count == 0) { State.Customer.Slot = 0; State.Customers.Add(State.Customer); }
                State.Customer = null;
                if (IsOpen && State.Customers.Count < CustomerCapacity && State.NextCustomerIn <= 0)
                    State.NextCustomerIn = ArrivalDelay;
            }
            if (economy.Orders == null) economy.Orders = new List<CustomerOrder>();
            else economy.Orders.Clear();
            foreach (var product in economy.Inventory)
            {
                if (!product.DistanceBased)
                {
                    int flavor = CandyRecipe.FlavorOf(product);
                    var migrated = Preview(MetersForSize(product.Grams < 100 ? 0 : 1), Math.Max(0, flavor));
                    product.DistanceBased = true;
                    product.DistanceMeters = migrated.DistanceMeters;
                    product.FlavorIndex = migrated.FlavorIndex;
                    product.Samples = migrated.Samples;
                    product.Grams = migrated.Grams;
                }
                if (!economy.CompletedIds.Contains(product.Id)) economy.CompletedIds.Add(product.Id);
            }
            if (IsOpen && State.Customers.Count == 0 && State.NextCustomerIn <= 0) Arrive(firstShift);
        }

        public bool Pour(int flavor, double grams = PourAmount)
        {
            if (HasWorker(SelectedMachine) || !Finite(grams) || grams <= 1e-7) return false;
            if (Tutorial.Active(economy) && (flavor != 0 || economy.TutorialStep != TutorialStep.PourSugar &&
                economy.TutorialStep != TutorialStep.Drive)) return false;
            grams = Math.Min(PourAmount, grams);
            if (economy.Progression != null) { if (!IsOpen) return false; SyncActive(); bool poured = PourMachine(SelectedMachine, flavor, grams); LoadActive(); Tutorial.Refresh(economy); return poured; }
            if (!IsOpen || flavor < 0 || flavor > 2 || (State.BatchMeters > 0 && State.BatchFlavor != flavor)) return false;
            if (State.SugarFlavor != flavor)
            {
                State.SugarFlavor = flavor;
                State.SugarGrams = grams;
                return true;
            }
            if (State.SugarGrams >= SugarCapacity) return false;
            State.SugarGrams = Math.Min(SugarCapacity, State.SugarGrams + grams);
            return true;
        }

        public bool EmptySugar()
        {
            if (Tutorial.Active(economy) || HasWorker(SelectedMachine) || !IsOpen || State.SugarGrams <= 0) return false;
            State.SugarGrams = 0;
            State.SugarFlavor = -1;
            return true;
        }

        public void Advance(double seconds, double forwardMeters, int wallHits = 0)
        {
            if (economy.Progression != null) { AdvanceProgression(seconds, forwardMeters, wallHits); return; }
            if (!IsOpen || !Finite(seconds) || seconds <= 0) return;
            double elapsed = Math.Min(seconds, State.RemainingSeconds);
            if (Finite(forwardMeters) && forwardMeters > 0 && State.SugarGrams > 0 &&
                (State.BatchMeters == 0 || State.BatchFlavor == State.SugarFlavor))
            {
                double meters = Math.Min(forwardMeters * (elapsed / seconds), State.SugarGrams / SugarPerMeter);
                if (meters > 0)
                {
                    if (State.BatchMeters == 0)
                    {
                        State.BatchFlavor = State.SugarFlavor;
                        State.BatchQuality = QualityForStars(MaxStars);
                    }
                    State.BatchMeters += meters;
                    // Normalize tiny frame-summation drift at each visible size target.
                    for (int size = 0; size < 3; size++)
                    {
                        double target = MetersForSize(size);
                        if (Math.Abs(State.BatchMeters - target) <= 1e-7) State.BatchMeters = target;
                    }
                    State.SugarGrams = Math.Max(0, State.SugarGrams - meters * SugarPerMeter);
                    if (State.SugarGrams <= 1e-9) { State.SugarGrams = 0; State.SugarFlavor = -1; }
                }
            }
            State.BatchQuality = AfterWallHits(State.BatchMeters, State.BatchQuality, wallHits);
            AdvanceCustomer(elapsed);
            State.RemainingSeconds = Math.Max(0, State.RemainingSeconds - elapsed);
            if (State.RemainingSeconds == 0)
            {
                State.Closed = true;
                DiscardLeftovers();
                State.Customers.Clear();
                State.Customer = null;
                State.NextCustomerIn = 0;
            }
        }

        public Product Extract()
        {
            if (HasWorker(SelectedMachine) || !IsOpen || State.BatchMeters <= 0 || economy.Inventory.Count >= economy.StockCapacity) return null;
            if (Tutorial.Active(economy) && (economy.TutorialStep != TutorialStep.Extract ||
                State.BatchFlavor != 0 || State.BatchMeters + 1e-7 < MetersForSize(0))) return null;
            var product = Preview(State.BatchMeters, State.BatchFlavor);
            if (product == null) return null;
            product.Id = string.IsNullOrEmpty(State.BatchProductId) ? Guid.NewGuid().ToString("N") : State.BatchProductId;
            product.Quality = State.BatchQuality;
            product.SugarGrade = State.BatchSugarGrade;
            economy.Inventory.Add(product);
            if (!economy.CompletedIds.Contains(product.Id)) economy.CompletedIds.Add(product.Id);
            State.BatchProductId = null;
            State.BatchMeters = 0;
            State.BatchOverflowMeters = 0;
            State.BatchFlavor = -1;
            State.BatchQuality = 50;
            State.BatchSugarGrade = 1;
            if (Tutorial.Active(economy)) economy.TutorialStep = TutorialStep.Deliver;
            return product;
        }

        public bool ResumeProduct(string productId)
        {
            if (Tutorial.Active(economy) || HasWorker(SelectedMachine) || !IsOpen || string.IsNullOrEmpty(productId)) return false;
            int index = economy.Inventory.FindIndex(product => product != null && product.Id == productId);
            if (index < 0) return false;
            var selected = economy.Inventory[index];
            if (economy.Progression != null && (!CanMakeFlavor(SelectedMachine, selected.FlavorIndex) ||
                selected.SugarGrade > SugarGrade(SelectedMachine) ||
                selected.DistanceMeters > MetersForSize(MaxSize(SelectedMachine)) + 1e-7)) return false;
            if (!selected.DistanceBased || !Finite(selected.DistanceMeters) || selected.DistanceMeters <= 0 ||
                selected.FlavorIndex < 0 || selected.FlavorIndex > 2) return false;
            Product outgoing = null;
            if (State.BatchMeters > 0)
            {
                outgoing = Preview(State.BatchMeters, State.BatchFlavor);
                if (outgoing == null) return false;
                outgoing.Id = string.IsNullOrEmpty(State.BatchProductId) ? Guid.NewGuid().ToString("N") : State.BatchProductId;
                outgoing.Quality = State.BatchQuality;
                outgoing.SugarGrade = State.BatchSugarGrade;
            }
            if (outgoing == null) economy.Inventory.RemoveAt(index);
            else
            {
                economy.Inventory[index] = outgoing;
                if (!economy.CompletedIds.Contains(outgoing.Id)) economy.CompletedIds.Add(outgoing.Id);
            }
            State.BatchMeters = selected.DistanceMeters;
            State.BatchOverflowMeters = 0;
            State.BatchFlavor = selected.FlavorIndex;
            State.BatchQuality = selected.Quality;
            State.BatchProductId = selected.Id;
            State.BatchSugarGrade = Math.Max(1, selected.SugarGrade);
            if (!economy.CompletedIds.Contains(selected.Id)) economy.CompletedIds.Add(selected.Id);
            return true;
        }

        public DeliveryResult Deliver(string productId, string customerId)
        {
            var customer = State.Customers.Find(candidate => candidate != null && candidate.Id == customerId);
            if (!IsOpen || customer == null || customer.Angry || customer.Happy || string.IsNullOrEmpty(customerId) || customer.Id != customerId ||
                string.IsNullOrEmpty(productId)) return DeliveryResult.Rejected;
            int index = economy.Inventory.FindIndex(product => product != null && product.Id == productId);
            if (index < 0) return DeliveryResult.Rejected;
            var candy = economy.Inventory[index];
            int size = SizeOf(candy);
            if (size < 0) return DeliveryResult.Rejected;
            if (Tutorial.Active(economy) && (economy.TutorialStep != TutorialStep.Deliver || customer.Slot != 0 ||
                candy.FlavorIndex != customer.Flavor || size != customer.Size)) return DeliveryResult.Rejected;
            economy.Inventory.RemoveAt(index);
            if (candy.FlavorIndex != customer.Flavor || size != customer.Size)
            {
                State.DayWrong++;
                customer.Angry = true;
                customer.ReactionRemaining = AngryDuration;
                return DeliveryResult.Wrong;
            }
            int price = economy.Price(candy);
            economy.Coins += price;
            economy.LifetimeRevenue += price;
            economy.TotalSold++;
            economy.OrdersServed++;
            economy.SatisfactionTotal += 1;
            State.DayRevenue += price;
            State.DaySold++;
            customer.Happy = true;
            customer.ReactionRemaining = ReactionDuration;
            if (Tutorial.Active(economy)) economy.TutorialStep = TutorialStep.Success;
            return DeliveryResult.Sold;
        }

        public bool Discard(string productId)
        {
            if (!IsOpen || !economy.Discard(productId)) return false;
            State.DayTrashed++;
            return true;
        }

        // Closing throws away unsold stock and every unfinished batch, and counts each as a disposal.
        void DiscardLeftovers()
        {
            int count = economy.Inventory.Count;
            economy.Inventory.Clear();
            if (economy.Progression != null)
            {
                foreach (var machine in State.Machines)
                    if (machine.BatchMeters > 0) { count++; machine.ClearBatch(); }
                LoadActive();
            }
            else if (State.BatchMeters > 0)
            {
                count++;
                State.BatchMeters = 0;
                State.BatchFlavor = -1;
                State.BatchQuality = 50;
                State.BatchProductId = null;
                State.BatchSugarGrade = 1;
            }
            State.DayTrashed += count;
        }

        public bool NextDay()
        {
            if (economy.Progression != null) return ReturnToPreparation();
            if (Paused || !State.Closed) return false;
            economy.Day++;
            economy.Inventory.Clear();
            economy.CompletedIds.Clear();
            economy.Orders.Clear();
            State.Closed = false;
            State.RemainingSeconds = DayDuration;
            State.SugarGrams = 0;
            State.SugarFlavor = -1;
            State.BatchMeters = 0;
            State.BatchFlavor = -1;
            State.BatchQuality = 50;
            State.BatchProductId = null;
            State.DayRevenue = 0;
            State.DaySold = 0;
            State.DayWrong = 0;
            State.DayMissed = 0;
            State.DayTrashed = 0;
            State.Customers.Clear();
            State.Customer = null;
            State.NextCustomerIn = 0;
            Arrive(false);
            return true;
        }

        public static double MetersForSize(int size)
        {
            if (size < 0 || size > 2) throw new ArgumentOutOfRangeException("size");
            return LapMeters * (1 + size * .5);
        }

        public static int SizeForDistance(double meters)
        {
            if (!Finite(meters) || meters < MetersForSize(0)) return -1;
            return meters < MetersForSize(1) ? 0 : meters < MetersForSize(2) ? 1 : 2;
        }

        public static int SizeOf(Product product)
        {
            return product == null ? -1 : product.DistanceBased ? SizeForDistance(product.DistanceMeters) : CandyRecipe.SizeOf(product);
        }

        public static int PreviewSampleCount(double meters)
        {
            if (!Finite(meters) || meters <= 0) return 0;
            return (int)Math.Max(1, Math.Ceiling(Math.Min(MaximumPreviewSamples, meters / LapMeters * 30)));
        }

        public static Product Preview(double meters, int flavor)
        {
            int count = PreviewSampleCount(meters);
            if (count == 0 || flavor < 0 || flavor > 2) return null;
            var product = new Product { DistanceBased = true, DistanceMeters = meters, FlavorIndex = flavor, Quality = 50, Grams = count * 2 };
            for (int i = 0; i < count; i++)
                product.Samples.Add(new WindingSample { Angle = (i + 1) * Production.SampleStep, Radius = 10, Flavor = flavor });
            return product;
        }

        void AdvanceCustomer(double seconds)
        {
            // Each iteration reaches an arrival, patience expiry, reaction expiry, or the tick end.
            // A full street freezes the pending arrival; departures never reset a pending timer.
            while (seconds > 1e-9)
            {
                bool hasGap = State.Customers.Count < CustomerCapacity;
                double step = seconds;
                if (hasGap) step = Math.Min(step, State.NextCustomerIn);
                foreach (var customer in State.Customers)
                    step = Math.Min(step, customer.Angry || customer.Happy ? customer.ReactionRemaining : customer.PatienceRemaining);
                foreach (var customer in State.Customers)
                {
                    if (customer.Angry || customer.Happy) customer.ReactionRemaining = Math.Max(0, customer.ReactionRemaining - step);
                    else customer.PatienceRemaining = Math.Max(0, customer.PatienceRemaining - step);
                }
                if (hasGap) State.NextCustomerIn = Math.Max(0, State.NextCustomerIn - step);
                seconds -= step;
                State.Customers.RemoveAll(customer => (customer.Angry || customer.Happy) && customer.ReactionRemaining <= 1e-9);
                foreach (var customer in State.Customers)
                    if (!customer.Angry && !customer.Happy && customer.PatienceRemaining <= 1e-9)
                    {
                        customer.PatienceRemaining = 0;
                        customer.Angry = customer.TimedOut = true;
                        customer.ReactionRemaining = ReactionDuration;
                        State.DayMissed++;
                        economy.MissedOrders++;
                    }
                if (State.Customers.Count < CustomerCapacity && State.NextCustomerIn <= 1e-9) Arrive(false);
            }
        }

        void Arrive(bool matchStock)
        {
            // With progression, an arrival may be a group that fills several free slots at once.
            int visitors = economy.Progression == null ? 1 : Progression.GroupSize(economy, economy.OrderSerial);
            for (int visitor = 0; visitor < visitors; visitor++)
            {
                if (visitor > 0 && State.Customers.Count >= CustomerCapacity) break;
                int flavor = economy.OrderSerial % 3;
                int size = (economy.OrderSerial / 3) % 3;
                if (economy.Progression != null) ChooseOrder(out flavor, out size);
                if (matchStock && visitor == 0)
                    foreach (var product in economy.Inventory)
                        if (SizeOf(product) >= 0) { flavor = product.FlavorIndex; size = SizeOf(product); break; }
                economy.OrderSerial++;
                int slot = 0;
                while (slot < CustomerCapacity && CustomerAt(slot) != null) slot++;
                if (slot == CustomerCapacity) return;
                State.Customers.Add(new ShopCustomer { Id = "shop-" + economy.OrderSerial, Flavor = flavor, Size = size, Slot = slot,
                    PatienceRemaining = PatienceLimit });
                State.NextCustomerIn = ArrivalInterval;
            }
        }

        static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}

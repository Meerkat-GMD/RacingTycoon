using System;
using System.Collections.Generic;
using System.Globalization;

namespace CottonCircuit
{
    public enum BusinessPhase { Preparation, Operating, Results }

    [Serializable] public class NodePurchase { public string Id; public int Level; }

    [Serializable] public class ProgressionState
    {
        public BusinessPhase Phase;
        public List<NodePurchase> Purchases = new List<NodePurchase>();
        public int SelectedMachine, SelectedLocation;
        public int CartStyle = 1;
    }

    [Serializable] public class UpgradeNode
    {
        public string Id, Name, Description, Category, Icon;
        public string[] Parents;
        public int MaxLevel, BaseCost;
        public double CostGrowth;
        public float X, Y;
    }

    /// <summary>Permanent, coin-funded capabilities and their balancing rules.</summary>
    public static class Progression
    {
        static UpgradeNode N(string id, string name, string description, string category, string icon,
            int max, int cost, double growth, float x, float y, params string[] parents)
        {
            return new UpgradeNode { Id = id, Name = name, Description = description,
                Category = category, Icon = icon, MaxLevel = max, BaseCost = cost,
                CostGrowth = growth, X = x, Y = y, Parents = parents };
        }

        // Neighboring prerequisites share a compact topology layout; detached roots and the cart chain sit to the left.
        public static readonly UpgradeNode[] Nodes = {
            N("hours", "영업시간", "하루 영업시간이 단계마다 40초 늘어납니다.", "business", "시", 3, 110, 2.0, 70, 70),
            N("patience", "손님 기다림", "손님이 단계마다 12초 더 기다립니다.", "business", "참", 3, 90, 1.9, 215, 70),
            N("ads", "동네 광고", "손님 방문 간격이 단계마다 크게 줄어듭니다.", "business", "광", 3, 120, 2.1, 320, 296),
            N("shelf", "진열대 확장", "완제품 보관칸이 단계마다 3칸 늘어납니다.", "business", "칸", 2, 150, 2.0, 140, 205),
            N("sales", "가게 간판", "모든 판매 가격이 단계마다 10% 오릅니다.", "sales", "값", 3, 160, 2.0, 418, 60),
            N("engine", "차량 엔진", "차량 최고속도가 단계마다 8% 오릅니다.", "equipment", "속", 3, 130, 2.1, 70, 365),
            N("handling", "차량 조향", "차량 조향력이 단계마다 8% 오릅니다.", "equipment", "향", 3, 125, 2.0, 190, 440, "engine"),
            N("coupe", "클래식 카트", "드리프트와 부스터를 사용하는 카트 주행을 해금합니다.", "equipment", "차", 1, 480, 1, 120, 565, "handling"),
            N("stick_speed", "젓가락 회전", "달린 거리당 솜사탕 성장량이 단계마다 12% 오릅니다.", "production", "성", 3, 110, 2.0, 820, 343),
            N("stick_saving", "설탕 절약", "솜사탕 성장에 필요한 설탕이 단계마다 8% 줄어듭니다.", "production", "절", 3, 140, 2.0, 960, 315, "stick_speed"),
            N("stick_quality", "예쁜 말기", "별 하나당 판매 보너스가 단계마다 0.6%p 오릅니다.", "production", "품", 3, 135, 2.0, 890, 219, "stick_speed"),
            N("sugar_2", "고운 설탕", "2등급 설탕과 중간 크기를 해금합니다.", "production", "설", 1, 210, 1, 800, 451),
            N("sugar_3", "특급 설탕", "3등급 설탕과 큰 크기를 해금합니다.", "production", "특", 1, 690, 1, 891, 561, "sugar_2"),
            N("machine_2", "두 번째 기계", "2등급 기계를 즉시 설치합니다.", "equipment", "기", 1, 360, 1, 647, 342, "sugar_2"),
            N("machine_3", "세 번째 기계", "3등급 기계를 즉시 설치합니다.", "equipment", "기", 1, 1100, 1, 691, 444, "machine_2", "stick_speed"),
            N("flavor_soda", "소다 맛", "2등급 기계에서 소다 맛을 만듭니다.", "production", "소", 1, 310, 1, 670, 185, "machine_2"),
            N("flavor_vanilla", "바닐라 맛", "3등급 기계에서 바닐라 맛을 만듭니다.", "production", "바", 1, 980, 1, 749, 574, "machine_3", "sugar_3"),
            N("worker_1", "첫 번째 알바", "기계에 배치할 첫 알바를 고용합니다.", "staff", "알", 1, 420, 1, 577, 243, "machine_2"),
            N("worker_2", "두 번째 알바", "두 번째 알바를 고용합니다.", "staff", "알", 1, 930, 1, 734, 275, "worker_1", "machine_3"),
            N("worker_grade_2", "알바 2등급 교육", "알바가 2등급 기계를 다룰 수 있습니다.", "staff", "교", 1, 360, 1, 545, 384, "worker_1", "machine_2"),
            N("worker_grade_3", "알바 3등급 교육", "알바가 3등급 기계를 다룰 수 있습니다.", "staff", "교", 1, 880, 1, 577, 490, "worker_grade_2", "machine_3"),
            N("worker_speed", "알바 손놀림", "알바 제작 속도가 단계마다 15% 오릅니다.", "staff", "손", 3, 220, 2.0, 473, 155, "worker_1"),
            N("flavor_price", "맛 홍보", "특별한 맛의 판매 가격이 단계마다 오릅니다.", "sales", "맛", 3, 230, 2.0, 561, 89, "flavor_soda", "sales"),
            N("location_1", "시장 앞", "더 많은 손님이 찾는 시장 앞 장사를 엽니다.", "location", "장", 1, 620, 1, 462, 313, "ads", "machine_2"),
            N("location_2", "강변 축제", "단가가 높은 강변 축제 장사를 엽니다.", "location", "축", 1, 1300, 1, 467, 487, "location_1", "worker_grade_2"),
            N("location_3", "별빛 광장", "최종 지역인 별빛 광장 장사를 엽니다.", "location", "별", 1, 2700, 1, 609, 595, "location_2", "machine_3", "flavor_vanilla"),
            N("location_price", "지역 단골", "새 지역에서 받는 금액이 단계마다 오릅니다.", "sales", "단", 3, 280, 2.0, 372, 199, "location_1", "sales"),
            N("sugar_saving", "설탕 계량", "설탕 사용량이 추가로 줄어듭니다.", "production", "계", 2, 320, 2.1, 942, 456, "stick_saving", "sugar_2"),
            N("quality_focus", "장식 기술", "별 하나당 판매 보너스가 단계마다 0.5%p 더 오릅니다.", "production", "꽃", 2, 340, 2.1, 794, 115, "stick_quality", "flavor_soda"),
            N("repeat_ads", "입소문", "추가 홍보로 방문 간격을 더 줄입니다.", "business", "입", 2, 390, 2.1, 375, 426, "ads", "location_1"),
            N("group_visit", "단체 손님", "손님이 여럿 함께 찾아올 확률이 단계마다 오르고, 2단계부터는 세 명도 옵니다.", "business", "무", 3, 180, 2.0, 250, 380, "ads")
        };

        public static UpgradeNode Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var node in Nodes) if (node.Id == id) return node;
            return null;
        }

        public static int Level(Economy e, string id)
        {
            if (e == null || e.Progression == null || e.Progression.Purchases == null || Find(id) == null) return 0;
            int result = 0;
            foreach (var purchase in e.Progression.Purchases)
                if (purchase != null && purchase.Id == id) result = Math.Max(result, purchase.Level);
            return Math.Min(Find(id).MaxLevel, result);
        }

        public static int Cost(Economy e, string id)
        {
            var node = Find(id);
            if (e == null || e.Progression == null || node == null || Level(e, id) >= node.MaxLevel) return 0;
            return (int)Math.Ceiling(node.BaseCost * Math.Pow(node.CostGrowth, Level(e, id)));
        }

        public static bool CanBuy(Economy e, string id)
        {
            var node = Find(id);
            if (node == null || e == null || e.Progression == null ||
                e.Progression.Phase != BusinessPhase.Preparation || Cost(e, id) <= 0 || e.Coins < Cost(e, id)) return false;
            foreach (string parent in node.Parents) if (Level(e, parent) < 1) return false;
            return true;
        }

        public static bool Buy(Economy e, string id)
        {
            if (!CanBuy(e, id)) return false;
            int cost = Cost(e, id);
            if (e.Progression.Purchases == null) e.Progression.Purchases = new List<NodePurchase>();
            NodePurchase purchase = e.Progression.Purchases.Find(p => p != null && p.Id == id);
            if (purchase == null) { purchase = new NodePurchase { Id = id }; e.Progression.Purchases.Add(purchase); }
            e.Coins -= cost;
            purchase.Level++;
            if (id == "hours" && e.Business != null)
                e.Business.RemainingSeconds = DaySeconds(e);
            return true;
        }

        static void Grant(ProgressionState state, string id, int level)
        {
            var node = Find(id);
            if (node == null || level <= 0) return;
            state.Purchases.Add(new NodePurchase { Id = id, Level = Math.Min(node.MaxLevel, level) });
        }

        public static void Enable(Economy e)
        {
            if (e == null || e.Progression != null) return;
            var state = new ProgressionState { Phase = BusinessPhase.Preparation };
            e.Progression = state;
            if (e.Levels != null)
            {
                if (e.Levels.Length > 0) Grant(state, "engine", e.Levels[0]);
                if (e.Levels.Length > 1)
                {
                    if (e.Levels[1] >= 1) Grant(state, "sugar_2", 1);
                    if (e.Levels[1] >= 2) Grant(state, "sugar_3", 1);
                }
                if (e.Levels.Length > 2) Grant(state, "sales", e.Levels[2]);
            }
            Grant(state, "shelf", e.ShelfLevel);
        }

        public static double DaySeconds(Economy e) { return 180 + 40 * Level(e, "hours"); }
        public static double PatienceSeconds(Economy e) { return 90 + 12 * Level(e, "patience"); }
        public static double ArrivalSeconds(Economy e) { return ArrivalSeconds(e, Level(e, "ads"), Level(e, "repeat_ads")); }
        static double ArrivalSeconds(Economy e, int ads, int repeatAds)
        {
            int location = e != null && e.Progression != null ? Math.Max(0, Math.Min(3, e.Progression.SelectedLocation)) : 0;
            return 26.0 / (1 + .3 * ads + .2 * repeatAds) * new[] { 1.0, .92, .84, .76 }[location];
        }
        public static double GroupChance(Economy e) { return GroupChance(Level(e, "group_visit")); }
        static double GroupChance(int level) { return .12 + .16 * level; }
        /// <summary>Visitors in one arrival. Deterministic per order serial and day, so saves and tests replay.</summary>
        public static int GroupSize(Economy e, int serial)
        {
            int level = Level(e, "group_visit");
            if (Roll(serial, e.Day, 0) >= GroupChance(level)) return 1;
            return level >= 2 && Roll(serial, e.Day, 1) < .5 ? 3 : 2;
        }
        static double Roll(int serial, int day, int salt)
        {
            unchecked
            {
                uint hash = (uint)(serial * 73856093 ^ day * 19349663 ^ salt * 83492791);
                hash ^= hash >> 13; hash *= 0x5bd1e995; hash ^= hash >> 15;
                return hash % 10000 / 10000.0;
            }
        }
        public static int Capacity(Economy e) { return 6 + 3 * Level(e, "shelf"); }
        public static int MaxSugarGrade(Economy e) { return Level(e, "sugar_3") > 0 ? 3 : Level(e, "sugar_2") > 0 ? 2 : 1; }
        public static int OwnedMachines(Economy e) { return 1 + Level(e, "machine_2") + Level(e, "machine_3"); }
        public static int WorkerCount(Economy e) { return Math.Min(2, Level(e, "worker_1") + Level(e, "worker_2")); }
        public static int WorkerGrade(Economy e) { return 1 + Level(e, "worker_grade_2") + Level(e, "worker_grade_3"); }
        public static double GrowthMultiplier(Economy e) { return 1 + .12 * Level(e, "stick_speed"); }
        public static double SugarMultiplier(Economy e) { return Math.Max(.5, 1 - .08 * Level(e, "stick_saving") - .07 * Level(e, "sugar_saving")); }
        public static double StarBonus(Economy e) { return .05 + .006 * Level(e, "stick_quality") + .005 * Level(e, "quality_focus"); }
        public static double SalesMultiplier(Economy e)
        {
            int location = e != null && e.Progression != null ? Math.Max(0, Math.Min(3, e.Progression.SelectedLocation)) : 0;
            return (1 + .1 * Level(e, "sales")) * (1 + .12 * location + .06 * location * Level(e, "location_price"));
        }
        public static double FlavorPriceMultiplier(Economy e, int flavor)
        {
            return flavor <= 0 ? 1 : (1 + .07 * flavor + .06 * Level(e, "flavor_price"));
        }
        public static double SpeedMultiplier(Economy e) { return 1 + .08 * Level(e, "engine"); }
        public static double SteeringMultiplier(Economy e) { return 1 + .08 * Level(e, "handling"); }
        public static double WorkerMetersPerSecond(Economy e) { return ShopShift.LapMeters / 60.0 * (1 + .15 * Level(e, "worker_speed")); }
        public static bool HasFlavor(Economy e, int flavor)
        {
            return flavor == 0 || flavor == 1 && Level(e, "flavor_soda") > 0 ||
                flavor == 2 && Level(e, "flavor_vanilla") > 0;
        }
        public static bool HasLocation(Economy e, int location)
        {
            return location == 0 || location >= 1 && location <= 3 && Level(e, "location_" + location) > 0;
        }
        public static bool HasCartStyle(Economy e, int style)
        {
            return e != null && e.Progression != null &&
                (style == 1 || style == 0 && Level(e, "coupe") > 0);
        }
        public static int MachineTier(int index) { return index >= 0 && index < 3 ? index + 1 : 0; }
        public static int MachineMap(int index) { return index >= 0 && index < 3 ? index : -1; }
        public static string MachineName(int index) { return new[] { "기본 기계", "소다 기계", "특급 기계" }[Math.Max(0, Math.Min(2, index))]; }
        public static string LocationName(int index) { return new[] { "동네 골목", "시장 앞", "강변 축제", "별빛 광장" }[Math.Max(0, Math.Min(3, index))]; }
        public static string LocationDescription(int index)
        {
            return new[] { "처음 장사를 시작하는 조용한 거리", "손님이 꾸준히 모이는 시장", "빠른 손길이 필요한 축제장", "맛과 기술을 모두 시험하는 마지막 광장" }[Math.Max(0, Math.Min(3, index))];
        }
        public static int FlavorMachineTier(int flavor) { return flavor >= 0 && flavor < 3 ? flavor + 1 : 0; }

        public static string EffectSummary(Economy e, string id)
        {
            var node = Find(id);
            if (node == null) return "";
            int level = Level(e, id);
            if (level >= node.MaxLevel) return "최대 단계";
            if (node.MaxLevel == 1) return node.Description;
            if (id == "hours") return Values("영업시간", DaySeconds(e), DaySeconds(e) + 40, "0", "초");
            if (id == "patience") return Values("대기시간", PatienceSeconds(e), PatienceSeconds(e) + 12, "0", "초");
            if (id == "shelf") return Values("진열칸", Capacity(e), Capacity(e) + 3, "0", "칸");
            if (id == "ads" || id == "repeat_ads")
            {
                double next = ArrivalSeconds(e, Level(e, "ads") + (id == "ads" ? 1 : 0), Level(e, "repeat_ads") + (id == "repeat_ads" ? 1 : 0));
                return Values("손님 간격", ArrivalSeconds(e), next, "0.0", "초");
            }
            if (id == "group_visit") return Values("단체 방문", GroupChance(level) * 100, GroupChance(level + 1) * 100, "0", "%");
            if (id == "engine") return Values("최고속도", 26 * SpeedMultiplier(e), 26 * (SpeedMultiplier(e) + .08), "0.0", "m/s");
            if (id == "handling") return Values("조향력", SteeringMultiplier(e) * 100, (SteeringMultiplier(e) + .08) * 100, "0", "%");
            if (id == "stick_speed") return Values("성장 효율", GrowthMultiplier(e) * 100, (GrowthMultiplier(e) + .12) * 100, "0", "%");
            if (id == "stick_saving" || id == "sugar_saving")
                return Values("설탕 사용량", SugarMultiplier(e) * 100,
                    Math.Max(.5, SugarMultiplier(e) - (id == "stick_saving" ? .08 : .07)) * 100, "0", "%");
            if (id == "stick_quality" || id == "quality_focus")
                return Values("별 하나당 보너스", StarBonus(e) * 100, (StarBonus(e) + (id == "stick_quality" ? .006 : .005)) * 100, "0.0", "%");
            if (id == "sales") return Values("기본 가격", (1 + .1 * level) * 100, (1 + .1 * (level + 1)) * 100, "0", "%");
            if (id == "flavor_price") return Values("특별한 맛", (1.07 + .06 * level) * 100, (1.07 + .06 * (level + 1)) * 100, "0", "%");
            if (id == "location_price") return Values("지역 보너스", .06 * level * 100, .06 * (level + 1) * 100, "0", "%/지역");
            if (id == "worker_speed") return Values("알바 속도", WorkerMetersPerSecond(e),
                ShopShift.LapMeters / 60.0 * (1 + .15 * (level + 1)), "0.0", "m/s");
            return node.Description;
        }

        static string Values(string label, double current, double next, string format, string unit)
        {
            return label + " " + current.ToString(format, CultureInfo.InvariantCulture) + unit +
                " → " + next.ToString(format, CultureInfo.InvariantCulture) + unit;
        }
    }
}

using System.Collections.Generic;

namespace CottonCircuit
{
    // Explicit trait -> icon sprite id table (spec "특성 아이콘"). Sprite ids match
    // Assets/CottonCircuit/Sprites/Icons/<id>.png and Art/Blender/ui_sprite_spec.TRAIT_ICONS.
    public static class TraitIcons
    {
        public static readonly string[] All =
        {
            "Trait_Hours", "Trait_Patience", "Trait_Ads", "Trait_RepeatAds", "Trait_Shelf", "Trait_Sales",
            "Trait_PriceTag", "Trait_Engine", "Trait_Handling", "Trait_Kart", "Trait_StickSpeed", "Trait_Spoon",
            "Trait_Ribbon", "Trait_Sugar2", "Trait_Sugar3", "Trait_Machine", "Trait_FlavorSoda", "Trait_FlavorVanilla",
            "Trait_Worker", "Trait_GradCap", "Trait_Glove", "Trait_MapPin", "Trait_Group",
        };

        static readonly Dictionary<string, string> byNode = new Dictionary<string, string>
        {
            { "hours", "Trait_Hours" }, { "patience", "Trait_Patience" }, { "ads", "Trait_Ads" },
            { "repeat_ads", "Trait_RepeatAds" }, { "shelf", "Trait_Shelf" }, { "sales", "Trait_Sales" },
            { "flavor_price", "Trait_PriceTag" }, { "location_price", "Trait_PriceTag" },
            { "engine", "Trait_Engine" }, { "handling", "Trait_Handling" }, { "coupe", "Trait_Kart" },
            { "stick_speed", "Trait_StickSpeed" }, { "stick_saving", "Trait_Spoon" }, { "sugar_saving", "Trait_Spoon" },
            { "stick_quality", "Trait_Ribbon" }, { "quality_focus", "Trait_Ribbon" },
            { "sugar_2", "Trait_Sugar2" }, { "sugar_3", "Trait_Sugar3" },
            { "machine_2", "Trait_Machine" }, { "machine_3", "Trait_Machine" },
            { "flavor_soda", "Trait_FlavorSoda" }, { "flavor_vanilla", "Trait_FlavorVanilla" },
            { "worker_1", "Trait_Worker" }, { "worker_2", "Trait_Worker" },
            { "worker_grade_2", "Trait_GradCap" }, { "worker_grade_3", "Trait_GradCap" },
            { "worker_speed", "Trait_Glove" },
            { "location_1", "Trait_MapPin" }, { "location_2", "Trait_MapPin" }, { "location_3", "Trait_MapPin" },
            { "group_visit", "Trait_Group" },
        };

        public static string For(string nodeId)
        {
            if (nodeId != null && byNode.TryGetValue(nodeId, out string id)) return id;
            throw new KeyNotFoundException("No trait icon for node '" + nodeId + "'");
        }
    }
}

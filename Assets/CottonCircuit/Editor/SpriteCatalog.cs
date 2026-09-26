using System.IO;
using UnityEditor;
using UnityEngine;
namespace CottonCircuit.Editor
{
    // Fills the GameAssets sprite fields from Assets/CottonCircuit/Sprites/<Folder>/<Id>.png.
    // Ids follow the sprite catalog in Art/Blender/ui_sprite_spec.py.
    public static class SpriteCatalog
    {
        const string Root = "Assets/CottonCircuit/Sprites";
        static readonly string[] Flavors = { "Strawberry", "Soda", "Vanilla" };
        static readonly string[] Sizes = { "Small", "Medium", "Large" };

        public static Sprite Load(string folder, string id)
        {
            string path = Root + "/" + folder + "/" + id + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite && File.Exists(path))
            {
                // A PNG first imported before SpriteImport existed is still a plain texture.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            if (!sprite) throw new FileNotFoundException("Missing UI sprite " + path, path);
            return sprite;
        }

        public static void Assign(GameAssets a)
        {
            a.CustomerNeutral = new Sprite[3]; a.CustomerAngry = new Sprite[3];
            for (int style = 0; style < 3; style++)
            {
                a.CustomerNeutral[style] = Load("Customers", "Customer_V" + style + "_Neutral");
                a.CustomerAngry[style] = Load("Customers", "Customer_V" + style + "_Angry");
            }
            a.Storefront = Load("Shop", "Storefront"); a.Trash = Load("Shop", "Trash");
            a.HeartEmote = Load("Shop", "Emote_Heart"); a.AngryEmote = Load("Shop", "Emote_Angry");
            a.MinaPortrait = Load("Characters", "Mina_Portrait");
            a.LocationPrep = new Sprite[4]; a.LocationStreet = new Sprite[4];
            for (int location = 0; location < 4; location++)
            {
                a.LocationPrep[location] = Load("Locations", "Location_" + location + "_Prep");
                a.LocationStreet[location] = Load("Locations", "Location_" + location + "_Street");
            }
            a.CottonCandy = new Sprite[9]; a.BaggedCandy = new Sprite[9]; a.SugarBags = new Sprite[3];
            for (int flavor = 0; flavor < 3; flavor++)
            {
                for (int size = 0; size < 3; size++)
                {
                    a.CottonCandy[flavor * 3 + size] = Load("Items", "CottonCandy_" + Flavors[flavor] + "_" + Sizes[size]);
                    a.BaggedCandy[flavor * 3 + size] = Load("Items", "BaggedCandy_" + Flavors[flavor] + "_" + Sizes[size]);
                }
                a.SugarBags[flavor] = Load("Items", "SugarBag_" + Flavors[flavor]);
            }
            a.Machines = new Sprite[3];
            for (int machine = 0; machine < 3; machine++) a.Machines[machine] = Load("Machines", "Machine_" + machine);
            var icons = CottonCircuit.TraitIcons.All;
            a.TraitIcons = new TraitIconSprite[icons.Length];
            for (int i = 0; i < icons.Length; i++)
                a.TraitIcons[i] = new TraitIconSprite { Id = icons[i], Sprite = Load("Icons", icons[i]) };
        }
    }
}

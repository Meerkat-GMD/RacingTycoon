using System;
using UnityEngine;

namespace CottonCircuit
{
    // Runtime lookup for the pre-rendered lowpoly UI sprites that the editor
    // SpriteCatalog stores in GameAssets. GameUI.Initialize calls Use first.
    // IntegrationChecks guarantees every sprite, so a missing one is a build error.
    public static class UiArt
    {
        // Normalised pivots shared with the art (origin bottom-left): the loose
        // candy's stick base and the bagged candy's tie knot on the rack clip.
        public static readonly Vector2 CandyPivot = new Vector2(.5f, .06f), BagPivot = new Vector2(.5f, .14f);
        // ShopArtGraphic.GrowthScale at one, one and a half and two laps: the
        // distances at which the Small, Medium and Large sprites show at full size.
        public static readonly float[] ReferenceGrowth = { .7597f, .845f, .917f };

        static readonly string[] Flavors = { "Strawberry", "Soda", "Vanilla" }, Sizes = { "Small", "Medium", "Large" };
        static GameAssets assets;

        public static void Use(GameAssets gameAssets) { assets = gameAssets; }

        public static Sprite CottonCandy(int flavor, int size) =>
            Grid(Assets.CottonCandy, flavor, size) ?? Missing("CottonCandy_" + Part(Flavors, flavor) + "_" + Part(Sizes, size));
        public static Sprite BaggedCandy(int flavor, int size) =>
            Grid(Assets.BaggedCandy, flavor, size) ?? Missing("BaggedCandy_" + Part(Flavors, flavor) + "_" + Part(Sizes, size));
        public static Sprite SugarBag(int flavor) => At(Assets.SugarBags, flavor) ?? Missing("SugarBag_" + Part(Flavors, flavor));
        public static Sprite Customer(int style, bool angry) =>
            At(angry ? Assets.CustomerAngry : Assets.CustomerNeutral, style) ?? Missing("Customer_V" + style + (angry ? "_Angry" : "_Neutral"));
        public static Sprite LocationPrep(int location) => At(Assets.LocationPrep, location) ?? Missing("Location_" + location + "_Prep");
        public static Sprite LocationStreet(int location) => At(Assets.LocationStreet, location) ?? Missing("Location_" + location + "_Street");
        public static Sprite Machine(int machine) => At(Assets.Machines, machine) ?? Missing("Machine_" + machine);
        public static Sprite TraitIcon(string nodeId)
        {
            string id = TraitIcons.For(nodeId);
            return Present(Assets.TraitIcon(id)) ?? Missing(id);
        }
        public static Sprite Storefront => Present(Assets.Storefront) ?? Missing("Storefront");
        public static Sprite Trash => Present(Assets.Trash) ?? Missing("Trash");
        public static Sprite HeartEmote => Present(Assets.HeartEmote) ?? Missing("Emote_Heart");
        public static Sprite AngryEmote => Present(Assets.AngryEmote) ?? Missing("Emote_Angry");
        public static Sprite MinaPortrait => Present(Assets.MinaPortrait) ?? Missing("Mina_Portrait");

        static GameAssets Assets => assets ? assets : throw new InvalidOperationException("UiArt.Use(GameAssets) must run before UI art is requested");
        // Unity's destroyed-object equality does not survive ??, so absent sprites become a true null first.
        static Sprite Present(Sprite sprite) => sprite ? sprite : null;
        static Sprite At(Sprite[] sprites, int index) => sprites != null && index >= 0 && index < sprites.Length ? Present(sprites[index]) : null;
        static Sprite Grid(Sprite[] sprites, int flavor, int size) => size >= 0 && size < 3 && flavor >= 0 ? At(sprites, flavor * 3 + size) : null;
        static string Part(string[] parts, int index) => index >= 0 && index < parts.Length ? parts[index] : index.ToString();
        static Sprite Missing(string name) => throw new InvalidOperationException("GameAssets has no UI sprite " + name);
    }
}

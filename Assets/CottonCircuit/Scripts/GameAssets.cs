using System;
using UnityEngine;
namespace CottonCircuit
{
    [Serializable] public struct TraitIconSprite { public string Id; public Sprite Sprite; }
    public class GameAssets : ScriptableObject
    {
        public GameObject Kart, Kiosk, Spinner, Puff, Customer, Crystal, Arch, Tree, Lamp;
        public GameObject Chevron, Barrier, ShortcutGate;
        public GameObject DisplayRack, OrderBoard, QueuePost;
        public GameObject CandyTunnel, FinishMarker, DownhillCoupe;
        public Mesh PuffMesh;
        public Material[] Flavors;
        // Pre-rendered lowpoly UI sprites, filled by the editor SpriteCatalog.
        public Sprite[] CustomerNeutral = new Sprite[3], CustomerAngry = new Sprite[3];     // index = customer style 0..2
        public Sprite Storefront, Trash, HeartEmote, AngryEmote, MinaPortrait;
        public Sprite[] LocationPrep = new Sprite[4], LocationStreet = new Sprite[4];      // index = location 0..3
        public Sprite[] CottonCandy = new Sprite[9], BaggedCandy = new Sprite[9];          // index = flavor * 3 + size
        public Sprite[] SugarBags = new Sprite[3], Machines = new Sprite[3];
        public TraitIconSprite[] TraitIcons = new TraitIconSprite[0];
        public Sprite TraitIcon(string iconId) { foreach (var i in TraitIcons) if (i.Id == iconId) return i.Sprite; return null; }
    }
    public static class Palette
    {
        public static readonly Color Ink = Hex("29324D"), Cream = Hex("FFF6E7"), Pink = Hex("F48DAB"),
            Soda = Hex("7ACDCE"), Yellow = Hex("F9D27D"), Muted = Hex("807C8A");
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color; }
        public static Color Flavor(int index) { return index == 0 ? Pink : index == 1 ? Soda : Yellow; }
        public static string FlavorName(int index) { return index == 0 ? "딸기" : index == 1 ? "소다" : "바닐라"; }
    }
}

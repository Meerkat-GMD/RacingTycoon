using UnityEngine;
namespace CottonCircuit
{
    public class GameAssets : ScriptableObject
    {
        public GameObject Kart, Kiosk, Spinner, Puff, Customer, Crystal, Arch, Tree, Lamp;
        public GameObject Chevron, Barrier, ShortcutGate;
        public GameObject DisplayRack, OrderBoard, QueuePost;
        public GameObject CandyTunnel, FinishMarker, DownhillCoupe;
        public Mesh PuffMesh;
        public Material[] Flavors;
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

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace CottonCircuit.Editor
{
    public static class IntegrationChecks
    {
        static int count;
        static void Check(bool value, string message)
        {
            if (!value) throw new Exception("COTTON_CHECK_FAILED " + message);
            count++; Debug.Log("COTTON_CHECK_PASS " + message);
        }
        public static void Run()
        {
            count = 0;
            if (!UnityEngine.Object.FindAnyObjectByType<GameController>())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ProjectBuilder.ScenePath);
            var game = UnityEngine.Object.FindAnyObjectByType<GameController>();
            Check(game && game.World && game.UI && game.Audio, "wired playable scene");
            Check(game.World.Kart && game.World.CentralCandy && game.World.Customer && game.World.SugarThread, "world gameplay references");
            Debug.Log("COTTON_MESH_DIAGNOSTIC assets=" + (game.World.Assets ? game.World.Assets.name : "null") + " mesh=" + (game.World.Assets.PuffMesh ? game.World.Assets.PuffMesh.name + " vertices=" + game.World.Assets.PuffMesh.vertexCount : "null"));
            Check(game.World.Assets.PuffMesh && game.World.Assets.PuffMesh.vertexCount > 0, "Blender cotton mesh imported");
            var assets = game.World.Assets;
            Check(assets.Kart && assets.Kiosk && assets.Spinner && assets.Puff && assets.Customer && assets.Crystal && assets.Arch && assets.Tree && assets.Lamp, "all nine Blender assets used");
            Check(assets.Chevron && assets.Barrier && assets.ShortcutGate, "three new Blender racing props imported");
            Check(GameObject.Find("Sugarway circuit") && GameObject.Find("Racing surface") && GameObject.Find("Sugar cut shortcut"), "main course and shortcut rendered from physics geometry");
            Check(game.World.CandyCamera && game.World.CandyPreview, "live cotton preview camera wired");
            foreach (var part in assets.Kart.GetComponentsInChildren<Transform>())
                if (part.name == "Nose") Check(assets.Kart.transform.InverseTransformPoint(part.position).z > .2f, "kart nose faces gameplay +Z");
            var production = new Production(220); production.Advance(Math.PI, 7.5, 0); production.Advance(Math.PI, 12.5, 2);
            var e = new Economy(); e.CompleteRun(production.Finish()); e.Coins = 345;
            var dir = Path.Combine(Path.GetFullPath("Logs"), "save-check-" + Guid.NewGuid().ToString("N"));
            var store = new SaveStore(dir);
            Check(store.Save(e), "first atomic save");
            e.BuyUpgrade(0); Check(store.Save(e), "save replaces prior file");
            Check(File.Exists(Path.Combine(dir, "cotton-circuit.json.bak")), "previous save backed up");
            var loaded = new SaveStore(dir).Load();
            Check(loaded.Coins == 225 && loaded.Levels[0] == 1 && loaded.Inventory.Count == 1 && loaded.Inventory[0].Samples.Count == 20, "save restores economy and winding samples");
            Check(loaded.Inventory[0].Samples[19].Flavor == 2 && loaded.Inventory[0].Samples[0].Radius == 7.5, "save preserves lane history");
            File.WriteAllText(Path.Combine(dir, "cotton-circuit.json"), "{broken");
            var broken = new SaveStore(dir); broken.Load();
            Check(!broken.CanSave && !broken.Save(e) && File.ReadAllText(Path.Combine(dir, "cotton-circuit.json")) == "{broken", "corrupt save protected from overwrite");
            Check(broken.ArchiveAndReset() && broken.Save(new Economy()), "explicit reset archives corrupt save");
            e.Levels[1] = 99; Check(!SaveStore.Valid(e), "invalid upgrade level rejected");
            e.Levels[1] = 0; e.Inventory[0].Samples[0].Radius = double.NaN; Check(!SaveStore.Valid(e), "nonfinite product coordinates rejected");
            Debug.Log("COTTON_EDITOR_CHECKS_PASSED " + count);
            File.WriteAllText("Logs/editor-checks.txt", count + " editor checks passed\n" + dir);
        }
    }
}

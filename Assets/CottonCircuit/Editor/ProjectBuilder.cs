using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CottonCircuit.Editor
{
    public static class ProjectBuilder
    {
        const string Root = "Assets/CottonCircuit";
        public const string ScenePath = Root + "/Scenes/CottonCircuit.unity";
        public const string PipelinePath = Root + "/Rendering/CottonCircuitURP.asset";
        const string RendererPath = Root + "/Rendering/CottonCircuitURP_Renderer.asset";
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        [MenuItem("Cotton Circuit/Rebuild game scene")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            double shakeWidth = ReadSceneSugarShakeWidth(ScenePath);
            // Create the scene before loading generated assets: NewScene unloads unreferenced meshes.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var folder in new[] { "Materials", "Prefabs", "Meshes", "Scenes", "Data", "Rendering" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            ConfigureRenderPipeline();
            MakeMaterials();
            var assets = ScriptableObject.CreateInstance<GameAssets>();
            assets.Kart = Import("Kart"); assets.Kiosk = Import("Kiosk"); assets.Spinner = Import("Spinner");
            assets.Puff = Import("Puff"); assets.Customer = Import("Customer"); assets.Crystal = Import("Crystal");
            assets.Arch = Import("Arch"); assets.Tree = Import("Tree"); assets.Lamp = Import("Lamp");
            assets.Chevron = Import("Chevron"); assets.Barrier = Import("Barrier"); assets.ShortcutGate = Import("ShortcutGate");
            assets.DisplayRack = Import("DisplayRack"); assets.OrderBoard = Import("OrderBoard"); assets.QueuePost = Import("QueuePost");
            assets.CandyTunnel = Import("CandyTunnel"); assets.FinishMarker = Import("FinishMarker");
            assets.DownhillCoupe = Import("DownhillCoupe");
            assets.Flavors = new[] { materials["Strawberry"], materials["Soda"], materials["Vanilla"] };
            var puff = (GameObject)PrefabUtility.InstantiatePrefab(assets.Puff);
            var combine = new List<CombineInstance>();
            foreach (var filter in puff.GetComponentsInChildren<MeshFilter>()) combine.Add(new CombineInstance { mesh = filter.sharedMesh, transform = puff.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
            assets.PuffMesh = new Mesh { name = "Blender puff combined" }; assets.PuffMesh.CombineMeshes(combine.ToArray());
            ReplaceAsset(assets.PuffMesh, Root + "/Meshes/Puff.asset"); assets.PuffMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Meshes/Puff.asset"); UnityEngine.Object.DestroyImmediate(puff);
            SpriteCatalog.Assign(assets);
            ReplaceAsset(assets, Root + "/Data/GameAssets.asset");
            assets = AssetDatabase.LoadAssetAtPath<GameAssets>(Root + "/Data/GameAssets.asset");
            var gameObject = new GameObject("Cotton Circuit");
            var controller = gameObject.AddComponent<GameController>();
            controller.SetSugarShakeFullStrokePixels(shakeWidth);
            controller.Audio = gameObject.AddComponent<AudioFeedback>(); controller.UI = gameObject.AddComponent<GameUI>();
            var worldObject = new GameObject("Candy park"); var world = worldObject.AddComponent<WorldView>(); world.Assets = assets; controller.World = world;
            BuildWorld(world);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Hex("E7DFDB"); camera.orthographic = true; camera.orthographicSize = 22;
            camera.nearClipPlane = .1f; camera.farClipPlane = 750; camera.allowHDR = false; camera.allowMSAA = true;
            camera.rect = new Rect(0, 0, .755f, 1); camera.transform.position = new Vector3(22, 35, 42); camera.transform.LookAt(new Vector3(-6, 1, 2)); world.GameCamera = camera;
            var backdrop = new GameObject("Letterbox backdrop", typeof(Camera)).GetComponent<Camera>();
            backdrop.depth = -10; backdrop.cullingMask = 0; backdrop.clearFlags = CameraClearFlags.SolidColor; backdrop.backgroundColor = Palette.Hex("E7DFDB");
            var lightObject = new GameObject("Soft afternoon sun", typeof(Light)); var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = .95f; light.color = Palette.Hex("FFF0DA"); light.shadows = LightShadows.Soft;
            light.shadowStrength = .4f; light.shadowBias = .035f; lightObject.transform.rotation = Quaternion.Euler(48, -32, 0); world.Sun = light;
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex("D9E6EF") * .65f; RenderSettings.ambientEquatorColor = Palette.Hex("D9CBDD") * .65f; RenderSettings.ambientGroundColor = Palette.Hex("B6A3A5") * .65f;
            RenderSettings.ambientIntensity = 1; RenderSettings.fog = false;
            PlayerSettings.companyName = "SugarRoad Studio"; PlayerSettings.productName = "Cotton Circuit";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true; PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("COTTON_SCENE_READY " + ScenePath);
        }
        static void ConfigureRenderPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (!pipeline)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                AssetDatabase.CreateAsset(renderer, RendererPath);
                pipeline = UniversalRenderPipelineAsset.Create(renderer); AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            // Keep the former Built-in Ultra look: 4x MSAA, LDR cameras, soft sun shadows in four cascades to 100 m.
            pipeline.supportsHDR = false; pipeline.msaaSampleCount = 4; pipeline.shadowDistance = 100; pipeline.shadowCascadeCount = 4;
            // URP exposes soft shadow support read-only, so write the field its inspector writes.
            var serialized = new SerializedObject(pipeline);
            serialized.FindProperty("m_SoftShadowsSupported").boolValue = true; serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int activeLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.SetQualityLevel(activeLevel, false);
            AssetDatabase.SaveAssets();
        }
        static void MakeMaterials()
        {
            materials.Clear();
            string[] names = { "Strawberry", "Cream", "Soda", "Vanilla", "Navy", "Plum", "White", "Mint", "Gold", "Tire", "Wood", "Ground", "InnerLane", "MiddleLane", "OuterLane", "Base", "RoadNeutral" };
            string[] colors = { "F48DAB", "FFF1D4", "7ACDCE", "F9D27D", "29324D", "6C577F", "FFF9ED", "99C4AE", "DBAE61", "414059", "D59C79", "E7DFDB", "E7B4C3", "B4DADD", "F5DFAD", "9DBBAF", "C6CAD1" };
            var lit = Shader.Find("Universal Render Pipeline/Lit"); var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            for (int i = 0; i < names.Length; i++)
            {
                var path = Root + "/Materials/" + names[i] + ".mat";
                // Write each material fresh so no Built-in Standard properties linger in the asset.
                var material = new Material(names[i] == "Ground" ? unlit : lit) { name = names[i], color = Palette.Hex(colors[i]) };
                if (material.shader == lit) { material.SetFloat("_Smoothness", .18f); material.SetFloat("_Metallic", 0); }
                ReplaceAsset(material, path);
                materials[names[i]] = AssetDatabase.LoadAssetAtPath<Material>(path); EditorUtility.SetDirty(materials[names[i]]);
            }
        }
        static GameObject Import(string name)
        {
            var path = Root + "/Models/" + name + ".fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer) throw new Exception("Missing Blender FBX: " + path);
            importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true; importer.isReadable = true;
            importer.importCameras = false; importer.importLights = false; importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var wrapper = new GameObject(name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model); instance.transform.SetParent(wrapper.transform, false);
            // Unity's FBX handedness conversion leaves the Blender -Y nose toward -Z.
            // Correct the imported visual once so gameplay-facing +Z stays consistent.
            instance.transform.localRotation = Quaternion.Euler(0, 180, 0);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var mapped = renderer.sharedMaterials;
                for (int i = 0; i < mapped.Length; i++)
                {
                    string key = mapped[i] ? mapped[i].name.Replace(" (Instance)", "") : "White";
                    int suffix = key.IndexOf('.'); if (suffix >= 0) key = key.Substring(0, suffix);
                    mapped[i] = materials.ContainsKey(key) ? materials[key] : materials["White"];
                }
                renderer.sharedMaterials = mapped;
            }
            var bounds = BoundsOf(wrapper);
            if (name != "Puff") instance.transform.position -= Vector3.up * bounds.min.y;
            if (bounds.size.y < .1f || bounds.size.y > 20) throw new Exception("Invalid imported meter scale for " + name + ": " + bounds.size);
            Debug.Log("FBX_METERS " + name + " " + bounds.size);
            var prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, Root + "/Prefabs/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(wrapper); return prefab;
        }
        static Bounds BoundsOf(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(); if (renderers.Length == 0) throw new Exception("No renderers: " + obj.name);
            Bounds bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds); return bounds;
        }
        internal static void ReplaceAsset(UnityEngine.Object asset, string path)
        {
            var old = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (old) { EditorUtility.CopySerialized(asset, old); UnityEngine.Object.DestroyImmediate(asset); }
            else AssetDatabase.CreateAsset(asset, path);
        }
        static void BuildWorld(WorldView world)
        {
            Transform root = world.transform;
            Cylinder("Floating garden", new Vector3(-6, -.8f, 1), new Vector3(330, 1, 320), materials["Base"], root);
            Cylinder("Machine body", new Vector3(0, .05f, 0), new Vector3(280, .7f, 280), materials["Cream"], root);
            Cylinder("Inner plate", new Vector3(0, .8f, 0), new Vector3(278, .13f, 278), materials["White"], root);
            Ring("Rounded machine rim", 139, 140, .75f, 2.3f, materials["Cream"], root);
            Ring("Pink rim inlay", 139.35f, 139.65f, 2.3f, 2.33f, materials["Strawberry"], root);
            Cylinder("Spinner stage", new Vector3(0, 1, 0), new Vector3(60, .18f, 60), materials["Plum"], root);
            Cylinder("Spinner stage inset", new Vector3(0, 1.2f, 0), new Vector3(58, .06f, 58), materials["Cream"], root);
            RaceCourseBuilder.Build(world, materials);
            var spinner = Place(world.Assets.Spinner, new Vector3(0, 1.27f, 0), Quaternion.identity, root);
            spinner.transform.localScale = Vector3.one * WorldView.CandyScale; world.Stick = spinner.transform;
            var cotton = new GameObject("Growing cotton on central stick"); cotton.transform.SetParent(root); cotton.transform.position = new Vector3(0, 1.27f, 0);
            cotton.transform.localScale = Vector3.one * WorldView.CandyScale; world.CentralCandy = cotton.AddComponent<CandyView>(); world.CentralCandy.Assets = world.Assets;
            var kart = Place(world.Assets.Kart, new Vector3(0, 1.05f, -25), Quaternion.Euler(0, -90, 0), root);
            world.Kart = kart.AddComponent<KartController>();

            for (int i = 0; i < 14; i++)
            {
                float a = (i + .3f) * Mathf.PI * 2 / 14;
                var crystal = Place(world.Assets.Crystal, new Vector3(Mathf.Cos(a) * 33.4f, 1.03f, Mathf.Sin(a) * 33.4f), Quaternion.Euler(0, i * 47, 0), root);
                crystal.transform.localScale = Vector3.one * .7f;
            }
            var shopRoot = new GameObject("Shop area").transform; shopRoot.SetParent(root); world.ShopRoot = shopRoot;
            Cylinder("Shop terrace", new Vector3(-52, -.1f, 8), new Vector3(13, .35f, 13), materials["Cream"], shopRoot);
            Place(world.Assets.Kiosk, new Vector3(-52, .26f, 8), Quaternion.identity, shopRoot);
            var sign = Cube("Shop sign", new Vector3(-52, 3.8f, 9.57f), new Vector3(3.9f, .66f, .1f), materials["Navy"], shopRoot);
            AddSign("SUGAR CLOUD", new Vector3(-52, 3.8f, 9.64f), .12f, Color.white, shopRoot);
            var display = new GameObject("Shop product positions"); display.transform.SetParent(shopRoot); display.transform.position = new Vector3(-52, 1.52f, 9.44f); world.DisplayRoot = display.transform;
            world.Customer = Place(world.Assets.Customer, new Vector3(-55, .26f, 15), Quaternion.Euler(0, 180, 0), shopRoot).transform;
            world.DisplayRacks = new Transform[3];
            for (int i = 0; i < 3; i++)
                world.DisplayRacks[i] = Place(world.Assets.DisplayRack, new Vector3(-48.2f, .26f, 6.2f + i * 2.7f), Quaternion.Euler(0, 90, 0), shopRoot).transform;
            Place(world.Assets.OrderBoard, new Vector3(-48.9f, .26f, 10.5f), Quaternion.Euler(0, -12, 0), shopRoot);
            Place(world.Assets.QueuePost, new Vector3(-53.5f, .26f, 11.5f), Quaternion.identity, shopRoot);
            Place(world.Assets.QueuePost, new Vector3(-53.5f, .26f, 13.3f), Quaternion.identity, shopRoot);
            for (int i = 0; i < 18; i++)
            {
                float a = i * Mathf.PI * 2 / 18;
                var pos = new Vector3(Mathf.Cos(a) * 142, -.02f, Mathf.Sin(a) * 142);
                if (pos.x < -40 && pos.z > 0) continue;
                var tree = Place(i % 3 == 0 ? world.Assets.Lamp : world.Assets.Tree, pos, Quaternion.Euler(0, i * 51, 0), root);
                tree.transform.localScale *= i % 2 == 0 ? .85f : 1.1f;
            }
            Place(world.Assets.Tree, new Vector3(-56, -.02f, 5), Quaternion.identity, shopRoot);
            Place(world.Assets.Lamp, new Vector3(-55, .2f, 11.6f), Quaternion.identity, shopRoot);
            for (int i = 0; i < 9; i++)
                Cube("Shop walkway", new Vector3(-53 + i * .6f, -.01f, 14.3f + i * .16f), new Vector3(.46f, .04f, 1.4f), materials["White"], shopRoot);
            shopRoot.position = new Vector3(-220, 0, 0);
            var threadObject = new GameObject("Sugar thread from kart"); threadObject.transform.SetParent(root);
            var thread = threadObject.AddComponent<LineRenderer>(); thread.useWorldSpace = true; thread.startWidth = .065f; thread.endWidth = .04f; thread.numCapVertices = 4;
            var lineMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/SugarThread.mat");
            if (!lineMat) { lineMat = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(lineMat, Root + "/Materials/SugarThread.mat"); }
            thread.sharedMaterial = lineMat; thread.enabled = false; world.SugarThread = thread;
            var preview = new GameObject("Cotton preview camera", typeof(Camera)).GetComponent<Camera>();
            preview.transform.SetPositionAndRotation(new Vector3(48, 40, 56), Quaternion.LookRotation(new Vector3(-48, -12, -56)));
            preview.orthographic = true; preview.orthographicSize = 27.2f; preview.clearFlags = CameraClearFlags.SolidColor; preview.backgroundColor = Palette.Cream;
            preview.nearClipPlane = .1f; preview.farClipPlane = 160; preview.enabled = false; preview.allowHDR = false;
            var previewPath = Root + "/Data/CandyPreview.renderTexture";
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(previewPath);
            if (!texture) { texture = new RenderTexture(256, 256, 16) { name = "Candy preview" }; AssetDatabase.CreateAsset(texture, previewPath); }
            preview.targetTexture = texture; world.CandyCamera = preview; world.CandyPreview = texture;
            var ground = Cube("Studio ground", new Vector3(0, -2, 0), new Vector3(1000, .1f, 1000), materials["Ground"], root);
        }
        internal static GameObject Place(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab); instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation); return instance;
        }
        internal static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent) => Primitive(PrimitiveType.Cube, name, position, scale, material, parent);
        static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent) => Primitive(PrimitiveType.Cylinder, name, position, scale, material, parent);
        static GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static void Ring(string name, float inner, float outer, float bottom, float top, Material material, Transform parent)
        {
            const int count = 160; var vertices = new List<Vector3>(); var indices = new List<int>();
            for (int i = 0; i <= count; i++)
            {
                float a = i * Mathf.PI * 2 / count; float c = Mathf.Cos(a), s = Mathf.Sin(a);
                vertices.Add(new Vector3(c * inner, top, s * inner)); vertices.Add(new Vector3(c * outer, top, s * outer));
                vertices.Add(new Vector3(c * inner, bottom, s * inner)); vertices.Add(new Vector3(c * outer, bottom, s * outer));
                if (i == count) continue;
                int n = i * 4;
                indices.AddRange(new[] { n, n + 4, n + 1, n + 1, n + 4, n + 5, n + 1, n + 5, n + 3, n + 3, n + 5, n + 7, n, n + 2, n + 4, n + 2, n + 6, n + 4 });
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Root + "/Meshes/" + name.Replace(" ", "") + ".asset"; ReplaceAsset(mesh, path);
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent);
            obj.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); obj.GetComponent<Renderer>().sharedMaterial = material;
        }
        static void AddSign(string text, Vector3 position, float size, Color color, Transform parent)
        {
            var go = new GameObject(text, typeof(TextMesh)); go.transform.SetParent(parent); go.transform.position = position; go.transform.rotation = Quaternion.Euler(0, 180, 0);
            var mesh = go.GetComponent<TextMesh>(); mesh.text = text; mesh.fontSize = 64; mesh.characterSize = size; mesh.anchor = TextAnchor.MiddleCenter;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); mesh.color = color; go.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
        }
        internal static double ReadSceneSugarShakeWidth(string scenePath)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool preview = !scene.IsValid() || !scene.isLoaded;
            if (preview)
            {
                if (!File.Exists(scenePath)) return SugarShake.DefaultFullStrokePixels;
                scene = EditorSceneManager.OpenPreviewScene(scenePath);
            }
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var controller = root.GetComponentInChildren<GameController>(true);
                    if (controller) return controller.SugarShakeFullStrokePixels;
                }
                return SugarShake.DefaultFullStrokePixels;
            }
            finally { if (preview) EditorSceneManager.ClosePreviewScene(scene); }
        }

        public static void VerifyAndBuild()
        {
            CreateScene(); IntegrationChecks.Run();
            Directory.CreateDirectory(BuildDirectory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = Path.Combine(BuildDirectory, "CottonCircuit.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("COTTON_BUILD_SUCCESS " + report.summary.totalSize);
        }
        [MenuItem("Cotton Circuit/Build Windows release")]
        public static void BuildRelease()
        {
            CreateScene();
            IntegrationChecks.Run();
            Directory.CreateDirectory(BuildDirectory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = Path.Combine(BuildDirectory, "CottonCircuit.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Release build failed: " + report.summary.result);
            Debug.Log("COTTON_RELEASE_SUCCESS " + report.summary.totalSize);
        }
        static string BuildDirectory
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-cotton-build-output");
                return index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) : "Builds/Windows";
            }
        }
    }
}

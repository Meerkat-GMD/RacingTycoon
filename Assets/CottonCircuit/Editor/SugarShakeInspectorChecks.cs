using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CottonCircuit.Editor
{
    public static class SugarShakeInspectorChecks
    {
        const string WidthField = "sugarShakeFullStrokePixels";

        public static void Run(Action<bool, string> check)
        {
            GameObject sourceObject = null;
            GameObject restoredObject = null;
            try
            {
                sourceObject = CreateInactiveObject("Sugar shake Inspector check");
                restoredObject = CreateInactiveObject("Sugar shake Inspector round trip");
                var source = sourceObject.AddComponent<GameController>();
                var restored = restoredObject.AddComponent<GameController>();
                source.enabled = restored.enabled = false;

                var field = typeof(GameController).GetField(WidthField, BindingFlags.Instance | BindingFlags.NonPublic);
                check(field != null && field.IsPrivate && Attribute.IsDefined(field, typeof(SerializeField)),
                    "shake width is a private serialized Inspector setting");

                using (var serialized = new SerializedObject(source))
                {
                    var width = serialized.FindProperty(WidthField);
                    check(width != null && width.propertyType == SerializedPropertyType.Float,
                        "Inspector exposes shake width as a numeric serialized property");
                    check(width.floatValue == 132 && source.SugarShakeFullStrokePixels == 132,
                        "an untouched controller preserves the default shake width");
                    check(PourForStroke(source, 132) == 10,
                        "the default Inspector width pours ten grams from a 132 pixel stroke");

                    width.floatValue = 252;
                    serialized.ApplyModifiedProperties();
                    serialized.Update();
                    check(width.floatValue == 252 && source.SugarShakeFullStrokePixels == 252,
                        "an Inspector property edit reaches the gameplay shake width");
                    check(PourForStroke(source, 132) == 5,
                        "a 252 pixel Inspector width pours five grams from the same stroke");

                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), restored);
                    using (var restoredSerialized = new SerializedObject(restored))
                    {
                        check(restoredSerialized.FindProperty(WidthField).floatValue == 252 &&
                            restored.SugarShakeFullStrokePixels == 252 && PourForStroke(restored, 132) == 5,
                            "Unity serialization restores the authored width and its pouring response");
                    }

                    CheckValidatedEdit(serialized, width, float.NaN, 132, check,
                        "Inspector validation replaces a nonfinite serialized width with the default");
                    CheckValidatedEdit(serialized, width, -10, 24, check,
                        "Inspector validation clamps a negative serialized width to the minimum");
                    CheckValidatedEdit(serialized, width, 1000, 600, check,
                        "Inspector validation clamps an oversized serialized width to the maximum");
                }

                check(!source.isActiveAndEnabled && !restored.isActiveAndEnabled &&
                    source.Session == null && source.Store == null && restored.Session == null && restored.Store == null,
                    "Inspector checks keep temporary controllers inactive without initializing game saves");
            }
            finally
            {
                if (restoredObject) UnityEngine.Object.DestroyImmediate(restoredObject);
                if (sourceObject) UnityEngine.Object.DestroyImmediate(sourceObject);
            }
            CheckSceneRebuildTuning(check);
        }

        static void CheckSceneRebuildTuning(Action<bool, string> check)
        {
            var originalActiveScene = SceneManager.GetActiveScene();
            Scene temporaryScene = default(Scene);
            string scenePath = "Assets/__SugarShakeInspectorCheck_" + Guid.NewGuid().ToString("N") + ".unity";
            try
            {
                temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(temporaryScene);
                var controllerObject = new GameObject("Saved Inspector shake width");
                controllerObject.SetActive(false);
                var controller = controllerObject.AddComponent<GameController>();
                controller.enabled = false;
                controller.SetSugarShakeFullStrokePixels(252);
                check(EditorSceneManager.SaveScene(temporaryScene, scenePath),
                    "a temporary scene saves its authored Inspector shake width");
                check(ProjectBuilder.ReadSceneSugarShakeWidth(scenePath) == 252,
                    "scene rebuild reads the authored shake width from an already loaded scene");

                check(EditorSceneManager.CloseScene(temporaryScene, true),
                    "the temporary tuning scene closes before testing the preview reader");
                check(ProjectBuilder.ReadSceneSugarShakeWidth(scenePath) == 252,
                    "scene rebuild restores the authored shake width through a saved scene preview");
            }
            finally
            {
                if (temporaryScene.IsValid() && temporaryScene.isLoaded)
                    EditorSceneManager.CloseScene(temporaryScene, true);
                AssetDatabase.DeleteAsset(scenePath);
                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                    SceneManager.SetActiveScene(originalActiveScene);
            }
        }

        static GameObject CreateInactiveObject(string name)
        {
            var instance = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            instance.SetActive(false);
            return instance;
        }

        static void CheckValidatedEdit(SerializedObject serialized, SerializedProperty width,
            float input, float expected, Action<bool, string> check, string message)
        {
            width.floatValue = input;
            serialized.ApplyModifiedProperties();
            serialized.Update();
            // Read back the stored field so a normalized getter cannot hide a missing OnValidate.
            check(width.floatValue == expected &&
                ((GameController)serialized.targetObject).SugarShakeFullStrokePixels == expected, message);
        }

        static double PourForStroke(GameController controller, double amplitude)
        {
            var shake = new SugarShake { FullStrokePixels = controller.SugarShakeFullStrokePixels };
            shake.Move(0, 0, true);
            shake.Move(0, amplitude, true);
            return shake.Move(0, amplitude - 12, true) ? shake.Amount : 0;
        }
    }
}

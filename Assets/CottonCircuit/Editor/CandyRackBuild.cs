using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CottonCircuit.Editor
{
    // Build the current scene without regenerating the user's scene, prefabs or meshes.
    public static class CandyRackBuild
    {
        public static void Build()
        {
            string folder = "Builds/CandyRack";
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-cotton-build-output") folder = args[i + 1];
            bool release = Array.IndexOf(args, "-cotton-release") >= 0;
            EditorSceneManager.OpenScene(ProjectBuilder.ScenePath);
            IntegrationChecks.Run();
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectBuilder.ScenePath },
                locationPathName = Path.Combine(folder, "CottonCircuit.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = release ? BuildOptions.None : BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Candy rack build failed: " + report.summary.result);
            Debug.Log("CANDY_RACK_BUILD_SUCCESS " + report.summary.totalSize);
        }
    }
}

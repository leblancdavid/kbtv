using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.Linq;

namespace KBTV.Editor
{
    /// <summary>
    /// Build automation script for command-line and CI/CD builds.
    /// Usage: Unity.exe -batchmode -quit -executeMethod KBTV.Editor.BuildScript.BuildWindows
    /// </summary>
    public static class BuildScript
    {
        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        [MenuItem("Build/Build Windows")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Build/Windows/KBTV.exe");
        }

        [MenuItem("Build/Build WebGL")]
        public static void BuildWebGL()
        {
            Build(BuildTarget.WebGL, "Build/WebGL");
        }

        [MenuItem("Build/Build All")]
        public static void BuildAll()
        {
            BuildWindows();
            BuildWebGL();
        }

        private static void Build(BuildTarget target, string outputPath)
        {
            var scenes = GetEnabledScenes();
            
            Debug.Log($"[BuildScript] Starting {target} build...");
            Debug.Log($"[BuildScript] Scenes: {string.Join(", ", scenes)}");
            Debug.Log($"[BuildScript] Output: {outputPath}");

            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No scenes found in Build Settings!");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };

            // Use development build if requested via command line
            if (HasCommandLineArg("-developmentBuild"))
            {
                options.options |= BuildOptions.Development;
                Debug.Log("[BuildScript] Development build enabled");
            }

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] {target} build succeeded!");
                Debug.Log($"[BuildScript] Size: {summary.totalSize / (1024 * 1024):F2} MB");
                Debug.Log($"[BuildScript] Time: {summary.totalTime.TotalSeconds:F1}s");
            }
            else
            {
                Debug.LogError($"[BuildScript] {target} build failed!");
                Debug.LogError($"[BuildScript] Errors: {summary.totalErrors}");
                
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error)
                        {
                            Debug.LogError($"[BuildScript] {message.content}");
                        }
                    }
                }

                // Exit with error code for CI
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static bool HasCommandLineArg(string arg)
        {
            return Environment.GetCommandLineArgs().Contains(arg);
        }
    }
}

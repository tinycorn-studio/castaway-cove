using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CraftBuild
{
    [MenuItem("Castaway Cove/Build Player")]
    public static void Build()
    {
        try
        {
            PlayerSettings.companyName = "Cove Studio";
            PlayerSettings.productName = "Castaway Cove";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.usePlayerLog = true;

            AddAlwaysIncludedShaders();
            CC0Import.SetupAll();

            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Build");
            Directory.CreateDirectory("Logs");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var boot = new GameObject("GameBoot");
            boot.AddComponent<Cove.GameBoot>();
            string scenePath = "Assets/Scenes/Main.unity";
            bool saved = EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("Scene saved: " + saved + " at " + scenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };

            string exePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "Game.exe"));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log("BUILD_RESULT=" + summary.result + " size=" + summary.totalSize + " errors=" + summary.totalErrors + " warnings=" + summary.totalWarnings + " output=" + exePath);
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("BUILD FAILED with " + summary.totalErrors + " errors");
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("BUILD EXCEPTION: " + e);
            EditorApplication.Exit(1);
        }
    }

    private static void AddAlwaysIncludedShaders()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (assets == null || assets.Length == 0) return;
        var settings = new SerializedObject(assets[0]);
        var list = settings.FindProperty("m_AlwaysIncludedShaders");
        if (list == null) return;

        string[] names = { "Standard", "Sprites/Default", "UI/Default", "Legacy Shaders/Diffuse", "Particles/Standard Unlit", "Castaway/CC0Lit", "Castaway/CC0Unlit" };
        foreach (string name in names)
        {
            var shader = Shader.Find(name);
            if (shader == null) continue;
            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    present = true;
                    break;
                }
            }
            if (!present)
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                Debug.Log("Added always-included shader: " + name);
            }
        }
        settings.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }
}

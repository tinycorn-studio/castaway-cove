using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CC0Import
{
    private const string ResourceDir = "Assets/Resources/CC0";
    private const string MaterialDir = "Assets/CC0/Generated/Materials";
    private const string ShaderPath = "Assets/CC0/Generated/CC0Lit.shader";

    private static Material miniMat;
    private static Material pirateMat;
    private static Material survivalMat;
    private static Material palmTrunkMat;
    private static Material palmLeafMat;
    private static Material rockMat;
    private static Material grassMat;
    private static Material cliffMat;
    private static Material woodMat;
    private static Material fishTintMat;
    private static Material fishLitMat;

    [MenuItem("Castaway Cove/Import CC0 Assets")]
    public static void SetupAll()
    {
        AssetDatabase.Refresh();
        EnsureFolders();
        AssetDatabase.ImportAsset(ShaderPath);
        AssetDatabase.ImportAsset("Assets/CC0/Generated/CC0Unlit.shader");
        Shader shader = Shader.Find("Castaway/CC0Lit");
        if (shader == null)
        {
            Debug.LogError("CC0Import: Castaway/CC0Lit shader not found.");
            return;
        }


        CreatePackMaterials(shader);
        Debug.Log("CC0Import: materials ready.");

        string natureDir = "Assets/CC0/KenneyNature/Models/FBX format";
        string pirateDir = "Assets/CC0/KenneyPirate/Models/FBX format";
        string survivalDir = "Assets/CC0/KenneySurvival/Models/FBX format";
        string miniDir = "Assets/CC0/KenneyMiniCharacters";
        string fishDir = "Assets/CC0/QuaterniusFish";

        string[] natureModels = { "tree_palm", "tree_palmBend", "tree_palmDetailedTall", "tree_palmShort", "rock_largeA", "rock_smallA", "rock_smallFlatA", "grass", "grass_large", "plant_bush", "cliff_rock", "fence_planks" };
        string[] pirateModels = { "rocks-sand-a", "rocks-sand-b", "structure-platform-dock", "structure-platform-dock-small", "boat-row-small", "tool-paddle", "crate", "chest", "barrel", "patch-sand", "palm-straight", "palm-detailed-straight" };
        string[] survivalModels = { "workbench", "bucket", "campfire-fishing-stand", "resource-planks", "signpost-single", "fish", "fish-large" };
        string[] fishModels = { "Fish1", "Fish2", "Fish3", "Shark" };

        foreach (string name in natureModels) BuildPrefab(Path.Combine(natureDir, name + ".fbx"), name, "nature", false);
        foreach (string name in pirateModels) BuildPrefab(Path.Combine(pirateDir, name + ".fbx"), name, "pirate", false);
        foreach (string name in survivalModels) BuildPrefab(Path.Combine(survivalDir, name + ".fbx"), name, "survival", false);
        BuildPrefab(Path.Combine(miniDir, "character-male-a.fbx"), "character-male-a", "mini", false);
        BuildPrefab(Path.Combine(miniDir, "character-female-a.fbx"), "character-female-a", "mini", false);
        foreach (string name in fishModels) BuildPrefab(Path.Combine(fishDir, name + ".fbx"), name, "fish", true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CC0Import: done.");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(ResourceDir);
        Directory.CreateDirectory(MaterialDir);
        AssetDatabase.Refresh();
    }

    private static void CreatePackMaterials(Shader shader)
    {
        miniMat = GetOrCreate("MiniCharacters", shader, "Assets/CC0/KenneyMiniCharacters/Textures/colormap.png", Color.white);
        pirateMat = GetOrCreate("PirateKit", shader, "Assets/CC0/KenneyPirate/Models/FBX format/Textures/colormap.png", Color.white);
        survivalMat = GetOrCreate("SurvivalKit", shader, "Assets/CC0/KenneySurvival/Models/FBX format/Textures/colormap.png", Color.white);
        palmTrunkMat = GetOrCreate("PalmTrunk", shader, null, new Color32(0x9C, 0x7A, 0x4F, 0xFF));
        palmLeafMat = GetOrCreate("PalmLeaf", shader, null, new Color32(0x55, 0xA3, 0x5C, 0xFF));
        rockMat = GetOrCreate("Rock", shader, null, new Color32(0xA9, 0xA7, 0x9D, 0xFF));
        grassMat = GetOrCreate("Grass", shader, null, new Color32(0x6F, 0xB3, 0x5B, 0xFF));
        cliffMat = GetOrCreate("Cliff", shader, null, new Color32(0xC8, 0xBD, 0xA8, 0xFF));
        woodMat = GetOrCreate("Wood", shader, null, new Color32(0xB9, 0x8A, 0x5E, 0xFF));
        var unlit = Shader.Find("Castaway/CC0Unlit");
        fishTintMat = GetOrCreate("FishTint", unlit != null ? unlit : shader, null, Color.white);
        fishLitMat = GetOrCreate("FishLit", shader, null, Color.white);
    }

    private static Material GetOrCreate(string name, Shader shader, string texturePath, Color color)
    {
        string path = MaterialDir + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = name;
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.shader != shader) mat.shader = shader;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.12f);
        if (!string.IsNullOrEmpty(texturePath))
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex != null) mat.SetTexture("_MainTex", tex);
            else Debug.LogWarning("CC0Import: missing texture " + texturePath);
        }
        else
        {
            mat.SetTexture("_MainTex", null);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static void DumpModels()
    {
        foreach (string name in new[] { "tree_palm", "tree_palmBend", "tree_palmDetailedTall", "tree_palmShort", "palm-straight", "palm-detailed-straight", "rock_largeA", "grass", "cliff_rock", "fence_planks" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/CC0/" + name + ".prefab");
            if (prefab == null) { Debug.Log("DUMP " + name + " missing"); continue; }
            var inst = UnityEngine.Object.Instantiate(prefab);
            var mfs = inst.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in mfs)
            {
                string mats = "";
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null) mats = string.Join("|", mr.sharedMaterials.Select(m => m == null ? "null" : m.name + ":" + (m.mainTexture == null ? "noTex" : m.mainTexture.name)).ToArray());
                Debug.Log("DUMP " + name + " child=" + mf.name + " submeshes=" + mf.sharedMesh.subMeshCount + " verts=" + mf.sharedMesh.vertexCount + " colors=" + mf.sharedMesh.colors.Length + " mats=" + mats + " bounds=" + mf.sharedMesh.bounds.size.ToString("0.00"));
            }
            UnityEngine.Object.DestroyImmediate(inst);
        }
    }
    private static void ConfigureImporter(string path, bool isFish)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("CC0Import: no ModelImporter for " + path);
            return;
        }
        importer.globalScale = 1f;
        importer.useFileScale = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.importAnimation = isFish;
        if (isFish) importer.animationType = ModelImporterAnimationType.Legacy;
        importer.SaveAndReimport();
    }

    private static void BuildPrefab(string sourcePath, string name, string pack, bool isFish)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning("CC0Import: missing model " + sourcePath);
            return;
        }

        ConfigureImporter(sourcePath, isFish);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            Debug.LogWarning("CC0Import: could not load " + sourcePath);
            return;
        }

        var instance = UnityEngine.Object.Instantiate(source);
        instance.name = name;
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            UnityEngine.Object.DestroyImmediate(animator);

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            AssignMaterials(name, renderer, pack);
        }

        if (isFish) BuildFishDisplay(instance, name, sourcePath);

        if (isFish)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview", StringComparison.Ordinal))
                .ToArray();
            var anim = instance.GetComponent<Animation>();
            if (anim == null) anim = instance.AddComponent<Animation>();
            anim.playAutomatically = false;
            AnimationClip chosenClip = null;
            foreach (var clip in clips)
            {
                anim.AddClip(clip, clip.name);
                if (chosenClip == null || clip.name.ToLowerInvariant().Contains("swim"))
                    chosenClip = clip;
            }
            if (chosenClip != null)
            {
                anim.clip = chosenClip;
                anim.playAutomatically = true;
                chosenClip.wrapMode = WrapMode.Loop;
            }
            Debug.Log("CC0Import: fish " + name + " clips=" + string.Join(",", clips.Select(c => c.name).ToArray()));
        }

        var bounds = ComputeBounds(instance);
        var names = string.Join(",", instance.GetComponentsInChildren<Transform>(true).Select(t => t.name).Take(12).ToArray());
        Debug.Log("CC0Import: " + name + " size=" + bounds.size.ToString("0.000") + " center=" + bounds.center.ToString("0.000") + " children=" + names);

        string dest = ResourceDir + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, dest);
        UnityEngine.Object.DestroyImmediate(instance);
    }

    private static void BuildFishDisplay(GameObject instance, string name, string sourcePath)
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview", StringComparison.Ordinal))
            .ToArray();
        if (clips.Length > 0)
        {
            var clip = clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("swim")) ?? clips[0];
            try { clip.SampleAnimation(instance, 0.4f); }
            catch (Exception e) { Debug.LogWarning("CC0Import: fish pose sample failed for " + name + ": " + e.Message); }
        }

        var display = new GameObject(name + "_display");
        string meshDir = "Assets/CC0/Generated/Meshes";
        Directory.CreateDirectory(meshDir);
        AssetDatabase.Refresh();

        var smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int partIndex = 0;
        foreach (var smr in smrs)
        {
            var baked = new Mesh();
            smr.BakeMesh(baked);
            var verts = baked.vertices;
            var normals = baked.normals;
            Matrix4x4 m = instance.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
            Matrix4x4 nm = m.inverse.transpose;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = m.MultiplyPoint3x4(verts[i]);
                if (normals != null && i < normals.Length) normals[i] = nm.MultiplyVector(normals[i]).normalized;
            }
            Vector3 center = Vector3.zero;
            for (int i = 0; i < verts.Length; i++) center += verts[i];
            if (verts.Length > 0) center /= verts.Length;
            for (int i = 0; i < verts.Length; i++) verts[i] -= center;
            baked.vertices = verts;
            if (normals != null && normals.Length == verts.Length) baked.normals = normals;
            baked.RecalculateBounds();
            string meshPath = meshDir + "/" + name + "_pose" + partIndex + ".asset";
            AssetDatabase.CreateAsset(baked, meshPath);

            var part = new GameObject("Part" + partIndex);
            part.transform.SetParent(display.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = baked;
            var mr = part.AddComponent<MeshRenderer>();
            mr.sharedMaterial = fishTintMat;
            partIndex++;
        }

        if (partIndex == 0)
        {
            UnityEngine.Object.DestroyImmediate(display);
            Debug.LogWarning("CC0Import: no skinned meshes for fish " + name);
            return;
        }

        PrefabUtility.SaveAsPrefabAsset(display, ResourceDir + "/" + name + "_display.prefab");
        UnityEngine.Object.DestroyImmediate(display);
        Debug.Log("CC0Import: baked display for " + name + " parts=" + partIndex);
    }
    private static void AssignMaterials(string modelName, Renderer renderer, string pack)
    {
        int slots = Mathf.Max(1, renderer.sharedMaterials.Length);
        var mats = new Material[slots];

        if (pack == "pirate" || pack == "survival" || pack == "mini")
        {
            for (int i = 0; i < slots; i++) mats[i] = pack == "pirate" ? pirateMat : pack == "survival" ? survivalMat : miniMat;
            renderer.sharedMaterials = mats;
            return;
        }
        if (pack == "fish")
        {
            for (int i = 0; i < slots; i++) mats[i] = fishLitMat;
            renderer.sharedMaterials = mats;
            return;
        }

        string n = (modelName + " " + renderer.name).ToLowerInvariant();
        bool isPalm = n.Contains("palm");
        bool isLeafChild = n.Contains("leaf");

        for (int i = 0; i < slots; i++)
        {
            Material m;
            if (isPalm)
            {
                if (isLeafChild) m = palmLeafMat;
                else if (slots >= 2) m = i == 0 ? palmTrunkMat : palmLeafMat;
                else if (modelName == "tree_palmDetailedTall") m = palmTrunkMat;
                else m = palmLeafMat;
            }
            else if (n.Contains("cliff")) m = cliffMat;
            else if (n.Contains("rock")) m = rockMat;
            else if (n.Contains("grass") || n.Contains("plant") || n.Contains("bush")) m = grassMat;
            else if (n.Contains("fence")) m = woodMat;
            else m = rockMat;
            mats[i] = m;
        }
        renderer.sharedMaterials = mats;
    }

    public static Bounds ComputeBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        bool has = false;
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in renderers)
        {
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }
}

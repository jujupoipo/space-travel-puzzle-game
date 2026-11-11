using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// one-time automatic scene bake; no Tools menu is required
// original castle, npc, portals, player and other planets are never rebuilt
[InitializeOnLoad]
public static class DreamGardenWorldAutoBake
{
    private const string MainScene = "Assets/Scenes/two_planets.unity";
    private const string TestScene = "Assets/Scenes/DreamGarden.unity";
    private const string Generated = "Assets/PlanetDesign/DreamGarden/Generated";
    private static bool working;

    static DreamGardenWorldAutoBake()
    {
        EditorSceneManager.sceneOpened += SceneOpened;
        EditorApplication.delayCall += CheckCurrentScene;
    }

    private static void SceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += () => BakeIfNecessary(scene);
    }

    private static void CheckCurrentScene()
    {
        BakeIfNecessary(SceneManager.GetActiveScene());
    }

    private static void BakeIfNecessary(Scene scene)
    {
        if (working || !scene.IsValid() || !scene.isLoaded ||
            (scene.path != MainScene && scene.path != TestScene) ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += CheckCurrentScene;
            return;
        }

        DreamGardenGenerator garden = FindGarden(scene);
        if (garden == null) return;
        DreamGardenWorldEnhancer ecosystem = garden.GetComponent<DreamGardenWorldEnhancer>();
        if (ecosystem != null && !ecosystem.NeedsBake) return;

        working = true;
        try
        {
            Directory.CreateDirectory(Generated);
            if (ecosystem == null) ecosystem = garden.gameObject.AddComponent<DreamGardenWorldEnhancer>();
            garden.BindVisualAssets();
            DreamGardenTerrain terrain = garden.GetComponentInChildren<DreamGardenTerrain>(true);
            if (terrain == null) throw new InvalidOperationException("Saved Dream Garden terrain not found.");
            if (garden.fantasyGrassPrefab == null || garden.ChooseWorldTree(0) == null)
                throw new InvalidOperationException("Fantasy Forest or Japanese Garden models are missing.");

            // rebuild only the surface mesh, not the castle or gameplay scripts
            terrain.ApplyTerrain();
            PersistTerrainMesh(terrain, scene.path);

            ecosystem.BakeForest();
            PersistFoliageMaterials(ecosystem, scene.path);
            EditorUtility.SetDirty(ecosystem);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("Unable to save updated Dream Garden scene.");
            Debug.Log("Dream Garden world is permanently baked in " + scene.path +
                ". The existing castle, portals and puzzles were preserved.");
        }
        catch (Exception exception)
        {
            Debug.LogError("Dream Garden automatic world update failed in " + scene.path + ": " +
                exception.Message + "\n" + exception.StackTrace);
        }
        finally { working = false; }
    }

    private static void PersistTerrainMesh(DreamGardenTerrain terrain, string scenePath)
    {
        MeshFilter filter = terrain.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            throw new InvalidOperationException("Ground mesh was not generated.");

        string prefix = scenePath == MainScene ? "two_planets" : "DreamGarden";
        string path = Generated + "/" + prefix + "_mesh_0.asset";
        Mesh generated = filter.sharedMesh;
        Mesh persisted = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (persisted == null)
        {
            persisted = Object.Instantiate(generated);
            persisted.name = prefix + "_mesh_0";
            AssetDatabase.CreateAsset(persisted, path);
        }
        else
        {
            EditorUtility.CopySerialized(generated, persisted);
            EditorUtility.SetDirty(persisted);
        }
        filter.sharedMesh = persisted;
        MeshCollider collider = terrain.GetComponent<MeshCollider>();
        if (collider != null)
        {
            collider.sharedMesh = null;
            collider.sharedMesh = persisted;
        }
    }

    private static void PersistFoliageMaterials(DreamGardenWorldEnhancer ecosystem, string scenePath)
    {
        string prefix = scenePath == MainScene ? "two_planets" : "DreamGarden";
        var saved = new Dictionary<Material, Material>();
        int count = 0;
        foreach (Renderer renderer in ecosystem.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material original = materials[i];
                if (original == null || AssetDatabase.Contains(original)) continue;
                if (!saved.TryGetValue(original, out Material asset))
                {
                    string path = Generated + "/" + prefix + "_world_mat_" + count++ + ".mat";
                    asset = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (asset == null)
                    {
                        asset = Object.Instantiate(original);
                        AssetDatabase.CreateAsset(asset, path);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(original, asset);
                        EditorUtility.SetDirty(asset);
                    }
                    saved.Add(original, asset);
                }
                materials[i] = asset;
                changed = true;
            }
            if (changed)
            {
                renderer.sharedMaterials = materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
    }

    private static DreamGardenGenerator FindGarden(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            DreamGardenGenerator garden = root.GetComponentInChildren<DreamGardenGenerator>(true);
            if (garden != null) return garden;
        }
        return null;
    }
}

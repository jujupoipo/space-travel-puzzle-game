using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// rebuilds only the existing garden and retains generated asset guids
public static class DreamGardenVisualUpgrade
{
    public const string MainScene = "Assets/Scenes/two_planets.unity";
    public const string StandaloneScene = "Assets/Scenes/DreamGarden.unity";
    private const string Generated = "Assets/PlanetDesign/DreamGarden/Generated";

    [MenuItem("Tools/Space Travel/Dream Garden/Upgrade Both Scenes - Castle and Forest")]
    public static void UpgradeBoth()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try { BakeBoth(); }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    public static void BakeBoth()
    {
        Directory.CreateDirectory(Generated);
        Directory.CreateDirectory("Assets/PlanetDesign/DreamGarden/Prefabs");
        AssetDatabase.Refresh();
        DreamProgression.ResetProgress();
        foreach (string path in new[] { MainScene, StandaloneScene })
        {
            string backup = Path.Combine(".dreamgarden-work", "editor-backups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"), Path.GetFileName(path));
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            File.Copy(path, backup, false);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            DreamGardenGenerator garden = Find<DreamGardenGenerator>(scene);
            DreamPuzzleController puzzle = Find<DreamPuzzleController>(scene);
            PlayerController player = Find<PlayerController>(scene);
            if (garden == null || puzzle == null || player == null) throw new Exception("missing garden, puzzle or player in " + path);
            garden.puzzle = puzzle; puzzle.player = player.transform;
            puzzle.firstPlanetScene = "two_planets";
            if (puzzle.GetComponent<DreamDialogue>() == null) puzzle.gameObject.AddComponent<DreamDialogue>();
            garden.planetRadius = 70; garden.mazeCells = 13; garden.cellSize = 4;
            garden.perimeterTreeCount = 64; garden.groundDetailCount = 110;
            garden.reliefStrength = 1.05f; garden.reliefTransition = 11; garden.wallEmbedDepth = .28f;
            if (!garden.BindVisualAssets()) throw new Exception("required local packs are missing");
            garden.GenerateDreamGarden();
            Vector3 spawn = garden.transform.TransformPoint(garden.Entrance(player.halfHeight + .2f));
            Vector3 up = (spawn - garden.transform.position).normalized;
            Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(garden.transform.forward, up).normalized, up);
            if (path == StandaloneScene)
            {
                player.transform.SetPositionAndRotation(spawn, rotation);
                DreamGardenStandaloneOverlay overlay = Find<DreamGardenStandaloneOverlay>(scene);
                if (overlay != null) { overlay.puzzle = puzzle; overlay.player = player.transform; overlay.showMetrics = false; }
            }
            else
            {
                Transform arrival = Root(scene, "PortalToDreamGarden");
                Transform returnPortal = Root(scene, "Portal2");
                if (arrival != null) arrival.SetPositionAndRotation(spawn, rotation);
                if (returnPortal != null)
                {
                    Vector3 position = garden.transform.TransformPoint(garden.CellCenter(0, 1, player.halfHeight));
                    returnPortal.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.up, (position - garden.transform.position).normalized));
                }
            }
            foreach (PlanetCamera camera in Object.FindObjectsByType<PlanetCamera>())
            {
                if (camera.target != player.transform) continue;
                DreamCameraOcclusion occlusion = camera.GetComponent<DreamCameraOcclusion>();
                if (occlusion == null) occlusion = camera.gameObject.AddComponent<DreamCameraOcclusion>();
                occlusion.player = player.transform; occlusion.puzzle = puzzle;
                UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
                if (data != null) data.renderPostProcessing = true;
            }
            StoreAssets(garden, Path.GetFileNameWithoutExtension(path));
            if (path == StandaloneScene)
                foreach (DreamInteraction interaction in garden.GetComponentsInChildren<DreamInteraction>())
                    if (interaction.kind == DreamInteraction.Kind.Keeper)
                        PrefabUtility.SaveAsPrefabAsset(interaction.gameObject, "Assets/PlanetDesign/DreamGarden/Prefabs/ForgottenKeeper.prefab");
            ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("could not save " + path);
            Debug.Log("dream garden baked: " + path);
        }
        var scenes = new List<EditorBuildSettingsScene> {
            new EditorBuildSettingsScene(MainScene, true), new EditorBuildSettingsScene(StandaloneScene, true) };
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.path != MainScene && scene.path != StandaloneScene && File.Exists(scene.path)) scenes.Add(scene);
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
    }

    private static T Store<T>(T source, string path) where T : Object
    {
        T stored = AssetDatabase.LoadAssetAtPath<T>(path);
        if (stored == null) { stored = Object.Instantiate(source); AssetDatabase.CreateAsset(stored, path); }
        else { EditorUtility.CopySerialized(source, stored); EditorUtility.SetDirty(stored); }
        return stored;
    }

    private static void StoreAssets(DreamGardenGenerator garden, string suffix)
    {
        var materials = new Dictionary<Material, Material>();
        int index = 0;
        foreach (Renderer renderer in garden.GetComponentsInChildren<Renderer>(true))
        {
            Material[] array = renderer.sharedMaterials;
            for (int i = 0; i < array.Length; i++)
            {
                Material source = array[i];
                if (source == null || AssetDatabase.Contains(source)) continue;
                if (!materials.TryGetValue(source, out Material stored))
                {
                    stored = Store(source, Generated + "/" + suffix + "_material_" + index++ + ".mat");
                    materials.Add(source, stored);
                }
                array[i] = stored;
            }
            renderer.sharedMaterials = array;
        }
        index = 0;
        var meshes = new Dictionary<Mesh, Mesh>();
        foreach (MeshFilter filter in garden.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh source = filter.sharedMesh;
            if (source == null || AssetDatabase.Contains(source)) continue;
            if (!meshes.TryGetValue(source, out Mesh stored))
            {
                stored = Store(source, Generated + "/" + suffix + "_mesh_" + index++ + ".asset");
                meshes.Add(source, stored);
            }
            filter.sharedMesh = stored;
            MeshCollider collider = filter.GetComponent<MeshCollider>();
            if (collider != null) collider.sharedMesh = stored;
        }
        CubeSphereGenerator sphere = garden.GetComponentInChildren<CubeSphereGenerator>();
        if (sphere != null && materials.TryGetValue(sphere.planetMaterial, out Material ground)) sphere.planetMaterial = ground;
        foreach (Volume volume in garden.GetComponentsInChildren<Volume>())
        {
            string path = Generated + "/" + suffix + "_atmosphere.asset";
            VolumeProfile source = volume.sharedProfile;
            VolumeProfile stored = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (stored == null) { stored = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(stored, path); }
            foreach (VolumeComponent component in stored.components) Object.DestroyImmediate(component, true);
            stored.components.Clear();
            foreach (VolumeComponent component in source.components)
            {
                VolumeComponent copy = Object.Instantiate(component);
                stored.components.Add(copy); AssetDatabase.AddObjectToAsset(copy, stored);
            }
            EditorUtility.SetDirty(stored); volume.sharedProfile = stored;
            foreach (VolumeComponent component in source.components) Object.DestroyImmediate(component);
            Object.DestroyImmediate(source);
        }
    }

    public static void ValidateScene(Scene scene)
    {
        DreamGardenGenerator garden = Find<DreamGardenGenerator>(scene);
        if (garden.GetComponentsInChildren<GravitySource>(true).Length != 1) throw new Exception("duplicate garden gravity");
        if (garden.GetComponentsInChildren<DreamLantern>(true).Length != 3) throw new Exception("expected three memories");
        if (garden.GetComponentsInChildren<DreamInteraction>(true).Length != 10) throw new Exception("expected ten interactions");
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0) throw new Exception("missing script: " + child.name);
        foreach (Renderer renderer in garden.GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.sharedMaterials)
                if (material == null || material.shader == null) throw new Exception("missing material: " + renderer.name);
    }

    public static T Find<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        { T found = root.GetComponentInChildren<T>(true); if (found != null) return found; }
        return null;
    }

    private static Transform Root(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root.transform;
        return null;
    }
}


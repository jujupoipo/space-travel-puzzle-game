using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DreamGardenSceneBuilder
{
    private const string dreamScenePath = "Assets/Scenes/DreamGardenDemo.unity";
    private const string firstScenePath = "Assets/FirstPlanet.unity";

    [MenuItem("Tools/Space Travel/Dream Garden/1 - Create Planet 2 Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("planet 2 - procedural dream garden");
        DreamGardenGenerator generator = root.AddComponent<DreamGardenGenerator>();
        GameObject game = new GameObject("dream puzzle manager");
        DreamPuzzleController puzzle = game.AddComponent<DreamPuzzleController>();
        DreamGardenHUD hud = game.AddComponent<DreamGardenHUD>();
        hud.puzzle = puzzle;
        generator.puzzle = puzzle;

        Camera camera = CreateCamera();
        CreateSun();
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.37f, 0.55f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.48f, 0.38f, 0.59f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 32f;
        RenderSettings.fogEndDistance = 95f;

        GameObject player = CopyOriginalPlayer(scene);
        if (player == null)
        {
            Debug.LogWarning("no original player could be copied. add the player manually and assign it to the puzzle manager and camera.");
        }
        else
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            Rigidbody rb = player.GetComponent<Rigidbody>();
            CharacterController character = player.GetComponent<CharacterController>();
            if (character != null) character.enabled = false;
            if (rb != null)
            {
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
            SetLayerRecursively(player, 2);
            if (controller != null)
            {
                controller.groundMask = ~(1 << 2);
                player.transform.position = generator.Entrance(controller.halfHeight);
                player.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                controller.cameraTransform = camera.transform;
            }
            puzzle.player = player.transform;
            PlanetCamera follow = camera.gameObject.AddComponent<PlanetCamera>();
            follow.target = player.transform;
            camera.transform.position = player.transform.position + new Vector3(0f, 3f, -9f);
            camera.transform.LookAt(player.transform.position);
        }

        EditorSceneManager.SaveScene(scene, dreamScenePath);
        AddToBuildSettings(dreamScenePath);
        AddToBuildSettings(firstScenePath);
        Selection.activeGameObject = root;
        Debug.Log("dream garden scene created. press play. q switches worlds, e activates memories.");
    }

    [MenuItem("Tools/Space Travel/Dream Garden/2 - Add Test Portal to Open Planet 1")]
    public static void AddPortalToFirstPlanet()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != firstScenePath)
        {
            EditorUtility.DisplayDialog("open first planet", "open Assets/FirstPlanet.unity before placing the portal.", "ok");
            return;
        }
        PlayerController controller = FindInScene<PlayerController>(scene);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("no player", "first planet has no PlayerController in the scene.", "ok");
            return;
        }
        if (FindInScene<DreamTravelPortal>(scene) != null)
        {
            EditorUtility.DisplayDialog("portal exists", "the current scene already has a dream portal.", "ok");
            return;
        }
        GameObject portal = new GameObject("dream garden travel portal - test");
        Vector3 up = controller.transform.up;
        Vector3 forward = Vector3.ProjectOnPlane(controller.transform.forward, up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        portal.transform.position = controller.transform.position + forward * 3f;
        portal.transform.rotation = Quaternion.LookRotation(forward, up);
        DreamTravelPortal travel = portal.AddComponent<DreamTravelPortal>();
        travel.player = controller.transform;
        travel.destinationScene = "DreamGardenDemo";
        travel.unlockedForDemo = true;
        MakePortalPart(portal.transform, "left post", new Vector3(-1f, 1.2f, 0), new Vector3(.45f, 2.4f, .45f));
        MakePortalPart(portal.transform, "right post", new Vector3(1f, 1.2f, 0), new Vector3(.45f, 2.4f, .45f));
        MakePortalPart(portal.transform, "top", new Vector3(0f, 2.4f, 0), new Vector3(2.5f, .45f, .45f));
        EditorSceneManager.MarkSceneDirty(scene);
        AddToBuildSettings(dreamScenePath);
        AddToBuildSettings(firstScenePath);
        Selection.activeGameObject = portal;
        Debug.Log("travel portal placed. save FirstPlanet.unity and press play. press e near the arch.");
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 62f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.13f, 0.3f);
        cameraObject.AddComponent<AudioListener>();
        camera.transform.position = new Vector3(0f, 54f, -12f);
        camera.transform.LookAt(new Vector3(0f, 45f, 0f));
        return camera;
    }

    private static void CreateSun()
    {
        GameObject lightObject = new GameObject("lavender dusk light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        light.color = new Color(1f, .8f, .88f);
        lightObject.transform.rotation = Quaternion.Euler(32f, -44f, 0f);
    }

    private static GameObject CopyOriginalPlayer(Scene destination)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(firstScenePath) == null) return null;
        Scene source = EditorSceneManager.OpenScene(firstScenePath, OpenSceneMode.Additive);
        GameObject result = null;
        try
        {
            PlayerController original = FindInScene<PlayerController>(source);
            if (original != null)
            {
                result = Object.Instantiate(original.gameObject);
                result.name = "player - original alien";
                result.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(result, destination);
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(destination);
        }
        return result;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null) return result;
        }
        return null;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static void MakePortalPart(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(.7f, .5f, .85f));
        else material.color = new Color(.7f, .5f, .85f);
        renderer.sharedMaterial = material;
    }

    private static void AddToBuildSettings(string scenePath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) return;
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene item in scenes)
            if (item.path == scenePath) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

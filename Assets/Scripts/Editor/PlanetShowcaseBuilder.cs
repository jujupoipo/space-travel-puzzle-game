using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlanetShowcaseBuilder
{
    [MenuItem("Tools/Space Travel/Create Three Planet Demo")]
    public static void CreateDemo()
    {
        // keep the current scene safe before creating the demo
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject generator = new GameObject("three planet generator");
        generator.AddComponent<PlanetGenerator>();

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.012f, 0.019f, 0.055f);
        camera.fieldOfView = 52f;
        camera.transform.position = new Vector3(0f, 3f, -37f);
        camera.transform.LookAt(Vector3.zero);
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("sun light");
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.96f, 0.87f);
        sun.intensity = 1.5f;
        lightObject.transform.rotation = Quaternion.Euler(30f, -35f, 0f);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.32f);

        string path = "Assets/Scenes/ThreePlanetsDemo.unity";
        EditorSceneManager.SaveScene(scene, path);
        Selection.activeGameObject = generator;
        Debug.Log("three planet demo created. press play to see the planets.");
    }
}

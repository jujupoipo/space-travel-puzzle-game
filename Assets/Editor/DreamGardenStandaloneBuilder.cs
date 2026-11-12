using UnityEditor;
using UnityEditor.SceneManagement;

// keeps the existing standalone scene and its original asset guid
public static class DreamGardenStandaloneBuilder
{
    [MenuItem("Tools/Space Travel/Planet 2/Open Standalone Dream Garden")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(DreamGardenVisualUpgrade.StandaloneScene);
    }
}

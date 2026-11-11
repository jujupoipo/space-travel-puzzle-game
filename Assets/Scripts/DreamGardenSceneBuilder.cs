#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

// legacy menu now opens the maintained scene instead of creating a duplicate planet
public static class DreamGardenSceneBuilder
{
    [MenuItem("Tools/Space Travel/Dream Garden/Open Planet 2 Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/DreamGarden.unity");
    }
}
#endif

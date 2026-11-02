using UnityEngine;

public class DreamGardenHUD : MonoBehaviour
{
    public DreamPuzzleController puzzle;

    private GUIStyle titleStyle;
    private GUIStyle lineStyle;
    private GUIStyle centerStyle;
    private bool stylesInitialized;

    private void OnGUI()
    {
        if (puzzle == null) return;

        // Debug check to verify readiness
        // Debug.Log("HUD Ready state: " + puzzle.Ready); 

        if (!puzzle.Ready) return;

        InitStyles();

        // Draw background box for readability
        GUI.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
        GUI.Box(new Rect(20, 20, 380, 140), GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(new Rect(35, 30, 350, 30), "THE FORGOTTEN DREAM GARDEN", titleStyle);
        GUI.Label(new Rect(35, 70, 350, 25), "Phase: " + (puzzle.IsBloom ? "Bloom" : "Ruin") + " (Press Q to Shift)", lineStyle);
        GUI.Label(new Rect(35, 100, 350, 25), "Memories: " + puzzle.MemoriesCollected + " / " + puzzle.MemoryCount, lineStyle);
        GUI.Label(new Rect(35, 130, 350, 25), puzzle.MemoriesCollected == puzzle.MemoryCount
            ? "Objective: Return to portal!" : "Objective: Moon > Petal > Star", lineStyle);

        string message = puzzle.GetInteractionHint();
        if (string.IsNullOrEmpty(message)) message = puzzle.Notice;
        if (!string.IsNullOrEmpty(message))
        {
            float x = (Screen.width - 500f) * 0.5f;
            GUI.color = new Color(0.1f, 0.05f, 0.2f, 0.95f);
            GUI.Box(new Rect(x, Screen.height - 100, 500, 45), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, Screen.height - 95, 500, 35), message, centerStyle);
        }
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(1f, 0.8f, 0.9f);

        lineStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14
        };
        lineStyle.normal.textColor = Color.white;

        centerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        centerStyle.normal.textColor = Color.white;

        stylesInitialized = true;
    }
}
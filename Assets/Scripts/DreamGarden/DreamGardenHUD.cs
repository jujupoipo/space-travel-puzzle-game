using UnityEngine;

public class DreamGardenHUD : MonoBehaviour
{
    public DreamPuzzleController puzzle;
    private GUIStyle titleStyle;
    private GUIStyle lineStyle;
    private GUIStyle centerStyle;

    private void OnGUI()
    {
        if (puzzle == null || !puzzle.Ready) return;
        EnsureStyles();

        GUI.color = new Color(0.055f, 0.06f, 0.11f, 0.85f);
        GUI.Box(new Rect(14, 14, 400, 145), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(28, 22, 360, 30), "THE FORGOTTEN DREAM GARDEN", titleStyle);
        GUI.Label(new Rect(28, 58, 370, 24), "phase: " + (puzzle.IsBloom ? "bloom" : "ruin") + "   |   q: shift reality", lineStyle);
        GUI.Label(new Rect(28, 82, 370, 24), "memories: " + puzzle.MemoriesCollected + "/" + puzzle.MemoryCount, lineStyle);
        GUI.Label(new Rect(28, 106, 370, 44), puzzle.MemoriesCollected == puzzle.MemoryCount
            ? "objective: return to the castle portal" : "objective: moon > petal > star", lineStyle);

        string message = puzzle.GetInteractionHint();
        if (string.IsNullOrEmpty(message)) message = puzzle.Notice;
        if (!string.IsNullOrEmpty(message))
        {
            float x = (Screen.width - 580f) * 0.5f;
            GUI.color = new Color(0.08f, 0.06f, 0.15f, 0.88f);
            GUI.Box(new Rect(x, Screen.height - 104, 580, 50), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 10, Screen.height - 98, 560, 40), message, centerStyle);
        }

        if (puzzle.Finished)
        {
            GUI.color = new Color(0.055f, 0.035f, 0.1f, 0.96f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.38f, Screen.width, 90), DreamProgression.GameCompleted ? "SPACE TRAVEL COMPLETE" : "THE DREAM IS RESTORED", centerStyle);
            GUI.Label(new Rect(0, Screen.height * 0.5f, Screen.width, 80), "you recovered all three memories", centerStyle);
        }
        GUI.color = Color.white;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0.95f, 0.76f, 0.88f);
        lineStyle = new GUIStyle(GUI.skin.label);
        lineStyle.fontSize = 16;
        lineStyle.normal.textColor = Color.white;
        centerStyle = new GUIStyle(GUI.skin.label);
        centerStyle.fontSize = 22;
        centerStyle.alignment = TextAnchor.MiddleCenter;
        centerStyle.wordWrap = true;
        centerStyle.normal.textColor = Color.white;
    }
}

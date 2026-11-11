using UnityEngine;
using UnityEngine.SceneManagement;

public class DreamGardenHUD : MonoBehaviour
{
    public DreamPuzzleController puzzle;
    public Planet1Puzzle firstPlanetPuzzle;
    public DreamTravelPortal portal;
    private GUIStyle title, line, center, button;

    private void Start()
    {
        if (firstPlanetPuzzle == null)
            firstPlanetPuzzle = FindAnyObjectByType<Planet1Puzzle>();
    }

    private void OnGUI()
    {
        if (DreamSceneFlow.Busy || (puzzle != null && puzzle.DialogueOpen)) return;
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(1, .8f, .9f);
            line = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            line.normal.textColor = Color.white;
            center = new GUIStyle(line) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        }
        Matrix4x4 oldMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 960f, Screen.height / 600f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960 * scale) / 2, (Screen.height - 600 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
        // the shared scene has two planets, so display the correct objective
        if (puzzle != null && puzzle.Ready && puzzle.PlayerInGarden) DrawGarden();
        else if (firstPlanetPuzzle != null) DrawFirstPlanet();
        else DrawFirstPlanetPlaceholder();
        if (!string.IsNullOrEmpty(DreamSceneFlow.LastError)) PanelMessage(DreamSceneFlow.LastError, 470);
        GUI.matrix = oldMatrix;
        GUI.color = Color.white;
    }
    private void Box(Rect rect)
    {
        GUI.color = new Color(.08f, .055f, .15f, .94f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
    private void PanelMessage(string message, float y)
    {
        Box(new Rect(100, y, 760, 58));
        GUI.Label(new Rect(114, y + 5, 732, 48), message, center);
    }
    private void DrawFirstPlanetPlaceholder()
    {
        Box(new Rect(20, 20, 440, 130));
        GUI.Label(new Rect(35, 30, 410, 30), "THE BEACON BETWEEN WORLDS", title);
        GUI.Label(new Rect(35, 68, 405, 60),
            "Find the crystals on Planet 1 to unlock the journey to the Dream Garden.", line);
    }

    private void DrawFirstPlanet()
    {
        Box(new Rect(20, 20, 440, 155));
        GUI.Label(new Rect(35, 30, 410, 30), "THE BEACON BETWEEN WORLDS", title);
        string objective = DreamProgression.Planet1Solved
            ? "The first core is restored. Enter the portal to reach the Dream Garden."
            : (!firstPlanetPuzzle.enabled
                ? "Puzzle 1 is disabled in this scene. Enable it when ready."
                : "Restore the beacon's power. Crystals: " + firstPlanetPuzzle.Collected + " / " + firstPlanetPuzzle.Total);
        GUI.Label(new Rect(35, 68, 405, 90), objective, line);
        if (portal != null && portal.IsNearby)
            PanelMessage(DreamProgression.Planet1Solved ? "Enter the portal to travel to the Garden of Memories" : "The beacon still needs this planet's crystals.", 530);
        else if (DreamProgression.Planet1Solved && portal != null && portal.player != null)
            PanelMessage("Beacon: " + Mathf.RoundToInt(Vector3.Distance(portal.player.position, portal.transform.position)) + " m", 530);
    }
    private void DrawGarden()
    {
        Box(new Rect(20, 20, 440, 195));
        GUI.Label(new Rect(35, 30, 410, 30), "GARDEN OF MEMORIES", title);
        GUI.Label(new Rect(35, 68, 405, 30), "Dream: " + (puzzle.IsBloom ? "BLOOM" : "RUIN") + "   |   Memories: " + puzzle.MemoriesCollected + " / 3", line);
        GUI.Label(new Rect(35, 100, 405, 70), "Objective: " + puzzle.NextMemory + "\nQ at pedestals | E interact | R return", line);
        GUI.Label(new Rect(35, 172, 405, 30), puzzle.ChallengeHint, line);
        if (puzzle.Finished)
        {
            if (!DreamProgression.GameCompleted)
            {
                PanelMessage("Planet 2 complete! Return through the portal to finish your journey.", 470);
                return;
            }

            Box(new Rect(180, 200, 600, 270));
            GUI.Label(new Rect(200, 220, 560, 50), "THE STARS REMEMBER HOME", center);
            GUI.Label(new Rect(215, 275, 530, 95),
                "You restored both cores. The beacon has reunited the worlds.\nYour journey is complete.", center);

            string restartScene = SceneManager.GetActiveScene().name == "two_planets"
                ? "two_planets" : puzzle.firstPlanetScene;
            if (Application.CanStreamedLevelBeLoaded(restartScene))
            {
                if (GUI.Button(new Rect(315, 390, 330, 45), "Restart campaign", button))
                    DreamSceneFlow.TryLoad(restartScene, true);
            }
            else
                GUI.Label(new Rect(230, 395, 500, 40),
                    "In the Editor, stop and restart Play Mode to begin again.", center);
            return;
        }
        if (!string.IsNullOrEmpty(puzzle.Notice)) PanelMessage(puzzle.Notice, 460);
        string hint = puzzle.GetInteractionHint();
        if (!string.IsNullOrEmpty(hint)) PanelMessage(hint, 530);
    }
}

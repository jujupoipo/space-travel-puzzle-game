using UnityEngine;
using UnityEngine.SceneManagement;

public class DreamGardenHUD : MonoBehaviour
{
    public DreamPuzzleController puzzle;
    public Planet1Puzzle firstPlanetPuzzle;
    public DreamTravelPortal portal;
    private GUIStyle title, line, center, button;
    private bool introductionShown;
    private bool introductionOpen;
    private PlayerController suspendedController;
    private Rigidbody suspendedBody;
    private bool controllerWasEnabled;
    private bool bodyWasKinematic;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    private void Update()
    {
        if (introductionShown || introductionOpen || puzzle == null || !puzzle.Ready ||
            !puzzle.PlayerInGarden || puzzle.DialogueOpen || DreamSceneFlow.Busy) return;
        // show only once per play session, after actually arriving on planet 2
        introductionShown = true;
        introductionOpen = true;
        puzzle.IntroOpen = true;
        if (puzzle.player != null)
        {
            suspendedController = puzzle.player.GetComponent<PlayerController>();
            suspendedBody = puzzle.player.GetComponent<Rigidbody>();
            if (suspendedController != null)
            {
                controllerWasEnabled = suspendedController.enabled;
                suspendedController.enabled = false;
            }
            if (suspendedBody != null)
            {
                bodyWasKinematic = suspendedBody.isKinematic;
                suspendedBody.linearVelocity = Vector3.zero;
                suspendedBody.angularVelocity = Vector3.zero;
                suspendedBody.isKinematic = true;
            }
        }
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseIntroduction()
    {
        if (!introductionOpen) return;
        introductionOpen = false;
        if (puzzle != null) puzzle.IntroOpen = false;
        if (suspendedBody != null) suspendedBody.isKinematic = bodyWasKinematic;
        if (suspendedController != null) suspendedController.enabled = controllerWasEnabled;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private void OnDisable() => CloseIntroduction();

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
        if (introductionOpen)
        {
            DrawGardenIntroduction();
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
            return;
        }
        // the shared scene has two planets, so display the correct objective
        if (puzzle != null && puzzle.Ready && puzzle.PlayerInGarden) DrawGarden();
        else if (ReturnedToPlanet1AfterCompletion()) DrawJourneyEnding();
        else if (firstPlanetPuzzle != null) DrawFirstPlanet();
        else DrawFirstPlanetPlaceholder();
        if (!string.IsNullOrEmpty(DreamSceneFlow.LastError)) PanelMessage(DreamSceneFlow.LastError, 470);
        GUI.matrix = oldMatrix;
        GUI.color = Color.white;
    }
    private void DrawGardenIntroduction()
    {
        Box(new Rect(145, 73, 670, 458));
        GUI.Label(new Rect(185, 100, 595, 42), "THE FORGOTTEN DREAM GARDEN", title);
        GUI.Label(new Rect(185, 155, 595, 112),
            "Beyond the last beacon lies a garden abandoned by time.\n" +
            "Its castle still guards three memories, but the world has forgotten how to bloom.\n" +
            "A silent Keeper waits among the ruins. Listen closely: not every door yields to a key.", line);
        GUI.Label(new Rect(185, 278, 590, 110),
            "YOUR QUEST\n" +
            "1. MOON — Find the Keeper and answer the forgotten riddle.\n" +
            "2. PETAL — Shift between Bloom and Ruin to awaken three garden seals.\n" +
            "3. STAR — Read the inscription and restore the astral sequence.", line);
        GUI.Label(new Rect(185, 395, 590, 40),
            "E  Interact    |    Q  Shift at pedestals    |    R  Return to checkpoint", line);
        if (GUI.Button(new Rect(328, 457, 305, 49), "ENTER THE GARDEN", button))
            CloseIntroduction();
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
    private bool ReturnedToPlanet1AfterCompletion()
    {
        // the finale belongs on planet 1, after the player has walked
        // through the return portal. leaving the dream garden is not enough.
        if (puzzle == null || !puzzle.Ready || puzzle.player == null ||
            !DreamProgression.GameCompleted || puzzle.PlayerInGarden || firstPlanetPuzzle == null)
            return false;
        Vector3 center = firstPlanetPuzzle.transform.position;
        return Vector3.Distance(puzzle.player.position, center) < 60f;
    }

    private void DrawJourneyEnding()
    {
        Box(new Rect(180, 170, 600, 300));
        GUI.Label(new Rect(200, 190, 560, 60), "THE STARS REMEMBER HOME", center);
        GUI.Label(new Rect(215, 260, 530, 95),
            "You restored the two planetary cores and returned home.\nThe worlds are reunited. Your journey is complete.", center);
        if (Application.CanStreamedLevelBeLoaded("two_planets") &&
            GUI.Button(new Rect(315, 390, 330, 45), "Restart campaign", button))
            DreamSceneFlow.TryLoad("two_planets", true);
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
        string objective = puzzle.Finished ? "Return to the portal on Planet 2" : puzzle.NextMemory;
        GUI.Label(new Rect(35, 100, 405, 70), "Objective: " + objective + "\nQ at pedestals | E interact | R return", line);
        GUI.Label(new Rect(35, 172, 405, 30), puzzle.Finished ? "All memories restored. Find the return portal." : puzzle.ChallengeHint, line);
        if (puzzle.Finished)
        {
            PanelMessage("Planet 2 complete! Return through the portal to Planet 1.\nQ: Shift at pedestals  |  R: Return to checkpoint", 460);
            return;
        }
        if (!string.IsNullOrEmpty(puzzle.Notice)) PanelMessage(puzzle.Notice, 460);
        string hint = puzzle.GetInteractionHint();
        if (!string.IsNullOrEmpty(hint)) PanelMessage(hint, 530);
    }
}

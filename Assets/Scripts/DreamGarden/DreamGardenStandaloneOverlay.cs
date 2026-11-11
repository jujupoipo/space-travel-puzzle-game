
using UnityEngine;
using UnityEngine.SceneManagement;

public class DreamGardenStandaloneOverlay : MonoBehaviour
{
    public DreamPuzzleController puzzle;
    public Transform player;
    public DreamGardenHUD originalHud;
    public bool showMetrics;

    private float fps;
    private bool endingShown;

    private void Update()
    {
        float currentFps = 1f / Mathf.Max(
            0.001f, Time.unscaledDeltaTime);

        fps = Mathf.Lerp(
            fps, currentFps, Time.unscaledDeltaTime * 3f);

        if (puzzle == null || !puzzle.Finished || endingShown)
            return;

        endingShown = true;

        if (originalHud != null)
            originalHud.enabled = false;

        if (player != null)
        {
            PlayerController controller =
                player.GetComponent<PlayerController>();

            if (controller != null)
                controller.enabled = false;

            Rigidbody body = player.GetComponent<Rigidbody>();

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            PlanetCamera camera =
                FindFirstObjectByType<PlanetCamera>();

            if (camera != null && camera.target == player)
                camera.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        if (puzzle == null || !puzzle.Ready)
            return;

        if (endingShown)
        {
            DrawEnding();
            return;
        }

        if (!showMetrics) return;
        float gravity = 0f;

        if (puzzle.planetGravity != null)
        {
            gravity = puzzle.planetGravity.strength;

            if (!puzzle.IsBloom)
                gravity *= puzzle.planetGravity.lowGravityMultiplier;
        }

        Rect panel = new Rect(Screen.width - 260, 15, 245, 145);

        GUI.Box(panel, "dream garden - metrics");

        GUI.Label(new Rect(panel.x + 12, panel.y + 28, 220, 25),
            "phase: " + (puzzle.IsBloom ? "bloom" : "ruin"));

        GUI.Label(new Rect(panel.x + 12, panel.y + 51, 220, 25),
            "memories: " + puzzle.MemoriesCollected + " / 3");

        GUI.Label(new Rect(panel.x + 12, panel.y + 74, 220, 25),
            "gravity: " + gravity.ToString("F1"));

        GUI.Label(new Rect(panel.x + 12, panel.y + 97, 220, 25),
            "fps: " + fps.ToString("F0"));

        GUI.Label(new Rect(panel.x + 12, panel.y + 120, 220, 25),
            "q: phase  |  e: interact  |  r: reset");
    }

    private void DrawEnding()
    {
        float width = 460f;
        float height = 240f;

        Rect panel = new Rect(
            (Screen.width - width) / 2f,
            (Screen.height - height) / 2f,
            width,
            height);

        GUI.Box(panel, "the forgotten dream garden");

        GUIStyle centered = new GUIStyle(GUI.skin.label);
        centered.alignment = TextAnchor.MiddleCenter;
        centered.fontSize = 19;
        centered.wordWrap = true;

        GUI.Label(
            new Rect(panel.x + 25, panel.y + 50, width - 50, 85),
            "all three memories have been restored.\n" +
            "the dream garden is complete!",
            centered);

        if (GUI.Button(
            new Rect(panel.x + 100, panel.y + 160, width - 200, 42),
            "restart dream garden"))
        {
            Time.timeScale = 1f;
            DreamProgression.ResetProgress();

            SceneManager.LoadScene(
                SceneManager.GetActiveScene().name);
        }
    }
}

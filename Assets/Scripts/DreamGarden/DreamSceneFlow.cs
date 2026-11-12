using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The fade outlives the outgoing scene, so it also fades IN at the destination.
public class DreamSceneFlow : MonoBehaviour
{
    private static DreamSceneFlow instance;
    public static bool Busy => instance != null && instance.busy;
    public static string LastError { get; private set; }
    private bool busy;
    private float alpha;
    private float progress;
    private float previousTimeScale;
    private readonly List<Behaviour> paused = new List<Behaviour>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { instance = null; LastError = ""; }

    public static bool TryLoad(string scene, bool newGame = false)
    {
        if (Busy) return false;
        if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene))
        {
            LastError = "Scene unavailable. Check that the scene is included in Build Profiles.";
            Debug.LogWarning(LastError + " Destination: " + scene);
            return false;
        }
        if (instance == null)
        {
            instance = new GameObject("Dream scene transition").AddComponent<DreamSceneFlow>();
            DontDestroyOnLoad(instance.gameObject);
        }
        instance.StartCoroutine(instance.Travel(scene, newGame));
        return true;
    }
    private void PauseActors()
    {
        foreach (PlayerController actor in FindObjectsByType<PlayerController>()) Pause(actor);
        foreach (PlanetCamera camera in FindObjectsByType<PlanetCamera>()) Pause(camera);
    }
    private void Pause(Behaviour actor)
    {
        if (!actor.enabled) return;
        paused.Add(actor);
        actor.enabled = false;
    }
    private void SceneLoaded(Scene scene, LoadSceneMode mode) => PauseActors();

    private IEnumerator Travel(string scene, bool newGame)
    {
        busy = true;
        LastError = "";
        progress = 0;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0;
        PauseActors();
        SceneManager.sceneLoaded += SceneLoaded;
        yield return Fade(0, 1);
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single); }
        catch (System.Exception exception) { LastError = "Travel failed: " + exception.Message; }
        if (operation != null)
        {
            if (newGame) DreamProgression.ResetProgress();
            while (!operation.isDone)
            {
                progress = Mathf.Clamp01(operation.progress / 0.9f);
                yield return null;
            }
            progress = 1;
            yield return null; // Destination Start generates the garden before revealing it.
        }
        yield return Fade(1, 0);
        Restore();
    }
    private IEnumerator Fade(float from, float to)
    {
        const float duration = 0.65f;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        alpha = to;
    }
    private void Restore()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        foreach (Behaviour actor in paused) if (actor != null) actor.enabled = true;
        paused.Clear();
        Time.timeScale = previousTimeScale;
        busy = false;
        alpha = 0;
    }
    private void OnDestroy()
    {
        if (busy) Restore();
        if (instance == this) instance = null;
    }
    private void OnGUI()
    {
        if (!busy) return;
        GUI.depth = -1000;
        GUI.color = new Color(0.035f, 0.02f, 0.08f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = new Color(1, 1, 1, alpha);
        GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
        GUI.Label(new Rect(0, Screen.height * .65f, Screen.width, 50),
            "Among the stars... " + Mathf.RoundToInt(progress * 100) + "%", style);
        GUI.color = Color.white;
    }
}

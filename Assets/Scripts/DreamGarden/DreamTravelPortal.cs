using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DreamTravelPortal : MonoBehaviour
{
    public Transform player;
    public string destinationScene = "DreamGardenDemo";
    public float activationDistance = 3.5f;
    public float fadeDuration = 0.65f;
    public bool unlockedForDemo = true;

    private bool travelling;
    private float fadeAlpha;
    private GUIStyle style;

    public void Unlock()
    {
        DreamProgression.MarkPlanet1Solved();
    }

    private void Update()
    {
        if (travelling || player == null) return;
        if (Vector3.Distance(player.position, transform.position) > activationDistance) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (!unlockedForDemo && !DreamProgression.Planet1Solved)
        {
            Debug.Log("portal locked. solve planet 1 puzzle first.");
            return;
        }
        StartCoroutine(Travel());
    }

    private IEnumerator Travel()
    {
        travelling = true;
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            fadeAlpha = Mathf.Clamp01(time / fadeDuration);
            yield return null;
        }
        fadeAlpha = 1f;
        AsyncOperation loading = SceneManager.LoadSceneAsync(destinationScene);
        if (loading == null)
        {
            Debug.LogError("add the dream garden scene to build settings first.");
            travelling = false;
            fadeAlpha = 0f;
        }
    }

    private void OnGUI()
    {
        if (!travelling && player != null &&
            Vector3.Distance(player.position, transform.position) < activationDistance)
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = 22;
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Color.white;
            }
            GUI.color = new Color(0.14f, 0.10f, 0.25f, 0.9f);
            Rect box = new Rect(Screen.width * 0.5f - 240, Screen.height - 120, 480, 52);
            GUI.Box(box, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(box, (unlockedForDemo || DreamProgression.Planet1Solved) ? "press e to travel to the dream garden" : "the portal is sealed", style);
        }
        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(0f, 0f, 0.02f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }
}

using UnityEngine;

public class DreamTravelPortal : MonoBehaviour
{
    public Transform player;
    public string destinationScene = "JuliaPlanet2";
    public float activationDistance = 3.5f;
    public float fadeDuration = 0.65f; // Serialized legacy field; DreamSceneFlow owns the fade.
    public bool unlockedForDemo = false;
    public bool IsNearby => player != null && Vector3.Distance(player.position, transform.position) < activationDistance;
    public void Unlock() => DreamProgression.MarkPlanet1Solved();
    private void Update()
    {
        if (DreamSceneFlow.Busy || !IsNearby || !Input.GetKeyDown(KeyCode.E)) return;
        if (!unlockedForDemo && !DreamProgression.Planet1Solved) return;
        DreamSceneFlow.TryLoad(destinationScene);
    }
}

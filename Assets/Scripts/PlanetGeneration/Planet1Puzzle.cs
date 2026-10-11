using UnityEngine;
using UnityEngine.Events;

public class Planet1Puzzle : MonoBehaviour
{
    [Header("Result")]
    public GameObject beacon;            // hidden at the start, shown when solved

    [Header("Events (for UI / game flow)")]
    public UnityEvent<int, int> onProgress;   // (collected, total)
    public UnityEvent onCompleted;

    [Header("Temporary on-screen text (turn off when Julia's UI is ready)")]
    public bool showDebugText = true;

    public int Total { get; private set; }
    public int Collected { get; private set; }
    public bool IsComplete { get; private set; }

    string message = "";

    void Start()
    {
        if (beacon != null) beacon.SetActive(false);
        message = "Find the crystals!";
    }

    public void RegisterCrystal()
    {
        Total++;
        onProgress?.Invoke(Collected, Total);
    }

    public void CrystalCollected()
    {
        Collected++;
        message = $"Crystals: {Collected} / {Total}";
        onProgress?.Invoke(Collected, Total);

        if (Collected >= Total && Total > 0 && !IsComplete)
        {
            IsComplete = true;
            message = "Puzzle complete! Go to the beacon.";
            if (beacon != null) beacon.SetActive(true);
            onCompleted?.Invoke();
        }
    }

    void OnGUI()
    {
        if (!showDebugText) return;
        GUI.Label(new Rect(20, 20, 500, 30), message);
    }
}
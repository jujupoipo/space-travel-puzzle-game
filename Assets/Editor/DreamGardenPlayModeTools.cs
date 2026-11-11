using UnityEditor;
using UnityEngine;

// editor-only test helpers; nothing is changed in the saved scene
public static class DreamGardenPlayModeTools
{
    [MenuItem("Tools/Space Travel/Planet 2/Test - Enter Dream Garden")]
    private static void EnterGarden()
    {
        DreamGardenGenerator garden = Object.FindFirstObjectByType<DreamGardenGenerator>();
        GameObject actor = GameObject.FindGameObjectWithTag("Player");
        if (garden == null || actor == null)
        {
            Debug.LogWarning("planet 2 test: garden generator or tagged player not found.");
            return;
        }

        garden.EnsureInitialized();
        Vector3 location = garden.transform.TransformPoint(garden.Entrance(1.5f));
        Vector3 up = (location - garden.transform.position).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.ProjectOnPlane(Vector3.right, up).normalized;
        Quaternion rotation = Quaternion.LookRotation(forward, up);

        Rigidbody body = actor.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = location;
            body.rotation = rotation;
        }

        actor.transform.SetPositionAndRotation(location, rotation);
        Physics.SyncTransforms();
        Debug.Log("planet 2 test: player moved to the dream garden (play mode only).");
    }

    [MenuItem("Tools/Space Travel/Planet 2/Test - Unlock Planet 1 Progress")]
    private static void UnlockForTesting()
    {
        DreamProgression.MarkPlanet1Solved();
        Debug.Log("planet 2 test: planet 1 marked complete for this play session only.");
    }

    [MenuItem("Tools/Space Travel/Planet 2/Test - Enter Dream Garden", true)]
    [MenuItem("Tools/Space Travel/Planet 2/Test - Unlock Planet 1 Progress", true)]
    private static bool ValidatePlayMode() => Application.isPlaying;
}

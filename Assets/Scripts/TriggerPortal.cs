using UnityEngine;

public class TriggerPortal : MonoBehaviour
{
    [Header("Destination")]
    public Transform targetSpawnPoint;

    [Header("Settings")]
    public string playerTag = "Player";
    [Min(0f)] public float cooldownSeconds = 1f;

    private static float lastTeleportTime = -1000f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCooldown()
    {
        lastTeleportTime = -1000f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || targetSpawnPoint == null) return;
        if (Time.time - lastTeleportTime < cooldownSeconds) return;

        if (DestinationIsDreamGarden() && !DreamProgression.Planet1Solved)
        {
            Planet1Puzzle puzzle = FindAnyObjectByType<Planet1Puzzle>();
            if (puzzle == null || !puzzle.IsComplete)
            {
                Debug.Log("portal locked: finish planet 1 before travelling to the dream garden.");
                return;
            }
            DreamProgression.MarkPlanet1Solved();
        }

        Teleport(other.transform);
        lastTeleportTime = Time.time;
    }

    private bool DestinationIsDreamGarden()
    {
        DreamGardenGenerator garden = FindAnyObjectByType<DreamGardenGenerator>();
        if (garden != null)
        {
            // use the destination position, not an unreliable spawn name
            float limit = Mathf.Max(6f, garden.planetRadius * 1.7f);
            return Vector3.SqrMagnitude(targetSpawnPoint.position - garden.transform.position)
                < limit * limit;
        }

        string name = targetSpawnPoint.name.ToLowerInvariant();
        return name.Contains("dreamgarden") || name.Contains("planet2");
    }

    private void Teleport(Transform actor)
    {
        Rigidbody body = actor.GetComponent<Rigidbody>();
        Vector3 normal = targetSpawnPoint.up;
        Quaternion rotation = Quaternion.FromToRotation(actor.up, normal) * actor.rotation;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = targetSpawnPoint.position;
            body.rotation = rotation;
        }

        actor.SetPositionAndRotation(targetSpawnPoint.position, rotation);
        Physics.SyncTransforms();

        // initialize the saved garden without generating a second planet
        if (DestinationIsDreamGarden())
        {
            DreamGardenGenerator garden = FindAnyObjectByType<DreamGardenGenerator>();
            if (garden != null) garden.EnsureInitialized();
        }

        Debug.Log("portal travel completed.");
    }
}

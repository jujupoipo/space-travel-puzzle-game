using UnityEngine;

public class InterplanetaryPortal : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform destinationSpawnPoint; // Empty GameObject placed on the target planet
    public GravitySource destinationGravity; // The GravitySource component of the target planet

    [Header("Settings")]
    public float activationDistance = 3.5f;

    private void Update()
    {
        if (player == null || destinationSpawnPoint == null) return;

        // Check distance between player and portal
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= activationDistance && Input.GetKeyDown(KeyCode.E))
        {
            TeleportPlayer();
        }
    }

    private void TeleportPlayer()
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Temporarily disable physics velocity during teleport to avoid fling bugs
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Move player to the destination spawn point on the other planet
        player.position = destinationSpawnPoint.position;

        // Align player's rotation to the new planet's surface normal
        Vector3 up = destinationSpawnPoint.up;
        player.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(player.forward, up), up);

        Debug.Log("Teleported successfully between planets!");
    }
}
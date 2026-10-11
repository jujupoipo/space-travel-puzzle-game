using System.Collections;
using UnityEngine;

public class BeaconPlacer : MonoBehaviour
{
    public Transform beacon;
    public Transform player;                 // optional: puts the beacon near the player
    public float angleFromPlayer = 12f;      // how far around the planet from the player

    IEnumerator Start()
    {
        yield return null;                   // wait for the planet and collider to exist

        var col = GetComponent<MeshCollider>();
        if (col == null || beacon == null) yield break;

        Vector3 dir = player != null
            ? (player.position - transform.position).normalized
            : Vector3.up;

        Vector3 axis = Vector3.Cross(dir, Vector3.up);
        if (axis.sqrMagnitude < 0.01f) axis = Vector3.right;
        dir = Quaternion.AngleAxis(angleFromPlayer, axis) * dir;

        float far = col.bounds.extents.magnitude + 10f;
        Ray ray = new Ray(transform.position + dir * far, -dir);

        if (col.Raycast(ray, out RaycastHit hit, far * 2f))
        {
            // lift by half the beacon's height so it stands on the ground
            beacon.position = hit.point + hit.normal * beacon.localScale.y;
            beacon.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
    }
}

using System.Collections;
using UnityEngine;

public class PlanetEnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 3;
    public float surfaceOffset = 1f;
    public float maxSlope = 30f;
    public int maxAttempts = 300;

    IEnumerator Start()
    {
        yield return null;

        MeshCollider planetCollider = GetComponent<MeshCollider>();
        CubeSphereGenerator generator = GetComponent<CubeSphereGenerator>();

        if (enemyPrefab == null || planetCollider == null ||
            planetCollider.sharedMesh == null || generator == null)
        {
            Debug.LogWarning("Enemy spawner is missing a prefab or planet mesh.");
            yield break;
        }

        int placed = 0;

        for (int attempt = 0;
             attempt < maxAttempts && placed < enemyCount;
             attempt++)
        {
            Vector3 direction = Random.onUnitSphere;

            float rayDistance = generator.radius + 100f;

            Vector3 rayStart = transform.position +
                               direction * rayDistance;

            Ray ray = new Ray(rayStart, -direction);

            if (!planetCollider.Raycast(ray, out RaycastHit hit,
                                        rayDistance * 2f))
                continue;

            Vector3 outward =
                (hit.point - transform.position).normalized;

            if (Vector3.Angle(hit.normal, outward) > maxSlope)
                continue;

            Vector3 position =
                hit.point + hit.normal * surfaceOffset;

            Quaternion rotation =
                Quaternion.FromToRotation(Vector3.up, hit.normal);

            Instantiate(enemyPrefab, position, rotation, transform);

            placed++;
        }

        Debug.Log("Enemies spawned: " + placed);
    }
}

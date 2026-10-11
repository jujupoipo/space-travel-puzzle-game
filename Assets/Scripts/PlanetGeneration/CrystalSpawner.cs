using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalSpawner : MonoBehaviour
{
    public GameObject crystalPrefab;
    public int crystalCount = 3;
    public int samples = 3000;
    public float minSeparation = 8f;     // keeps crystals away from each other
    public float hoverHeight = 1f;       // lifts the crystal above the ground

    struct Spot { public Vector3 point, normal; public float altitude; }

    IEnumerator Start()
    {
        yield return null;   // wait for the planet and collider

        var gen = GetComponent<CubeSphereGenerator>();
        var col = GetComponent<MeshCollider>();
        var noise = GetComponent<PlanetNoise>();
        if (gen == null || col == null || crystalPrefab == null) yield break;

        Random.InitState(noise != null ? noise.seed : 0);
        float top = gen.radius + (noise ? noise.height + noise.mountainHeight : 0f) + 5f;

        // Sample many points on the surface and remember how high each one is
        var spots = new List<Spot>();
        for (int i = 0; i < samples; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            Ray ray = new Ray(transform.position + dir * top, -dir);
            if (!col.Raycast(ray, out RaycastHit hit, top * 2f)) continue;

            spots.Add(new Spot
            {
                point = hit.point,
                normal = hit.normal,
                altitude = (hit.point - transform.position).magnitude
            });
        }

        // Highest first
        spots.Sort((a, b) => b.altitude.CompareTo(a.altitude));

        var placed = new List<Vector3>();
        foreach (var s in spots)
        {
            if (placed.Count >= crystalCount) break;

            bool tooClose = false;
            foreach (var p in placed)
                if (Vector3.Distance(p, s.point) < minSeparation) { tooClose = true; break; }
            if (tooClose) continue;

            Vector3 pos = s.point + s.normal * hoverHeight;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, s.normal);
            Instantiate(crystalPrefab, pos, rot, transform);
            placed.Add(s.point);
        }
    }
}
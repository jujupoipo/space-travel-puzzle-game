using System.Collections.Generic;
using UnityEngine;

// baked once by the editor when a dream garden scene is opened
// all decorative objects remain serialized in the scene during gameplay
[DisallowMultipleComponent]
public class DreamGardenWorldEnhancer : MonoBehaviour
{
    public const int CurrentRevision = 1;
    [SerializeField] private int bakedRevision;
    public int BakedRevision => bakedRevision;

    [Header("planet-wide ecology")]
    [Range(60, 320)] public int treeCount = 210;
    [Range(100, 650)] public int grassCount = 330;
    [Range(15, 150)] public int rockCount = 85;
    public int distributionSeed = 38417;

    private const string ForestName = "planet-wide forest - baked";

    public bool NeedsBake => bakedRevision < CurrentRevision || transform.Find(ForestName) == null;

    // called by the editor once; safe if manually repeated on a backup scene
    public void BakeForest()
    {
        DreamGardenGenerator garden = GetComponent<DreamGardenGenerator>();
        if (garden == null) return;
        DreamGardenTerrain terrain = GetComponentInChildren<DreamGardenTerrain>(true);
        if (terrain == null || terrain.noise == null)
        {
            Debug.LogError("Dream Garden cannot distribute plants without its terrain and noise.", this);
            return;
        }

        Transform old = transform.Find(ForestName);
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }

        GameObject container = new GameObject(ForestName);
        container.transform.SetParent(transform, false);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;

        float radius = garden.planetRadius;
        var random = new System.Random(distributionSeed + garden.seed);
        List<Vector3> treeDirections = new List<Vector3>();
        int madeTrees = 0, madeGrass = 0, madeRocks = 0;
        const float goldenAngle = 2.39996323f;

        int maxTrees = Mathf.Max(treeCount * 3, 150);
        for (int i = 0; i < maxTrees; i++)
        {
            // take samples across the entire fibonacci sphere, not the first hemisphere
            if (i % 3 != 0) continue;
            // fibonacci sphere covers the back and underside, unlike a top-only ring
            float t = (i + .5f) / maxTrees;
            float y = 1f - 2f * t;
            float ring = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float angle = i * goldenAngle + garden.seed * .071f;
            Vector3 dir = new Vector3(Mathf.Cos(angle) * ring, y, Mathf.Sin(angle) * ring);
            if (CastleProtected(dir, radius, garden)) continue;
            if (TooCloseToTrees(dir, treeDirections, radius, 5.5f)) continue;
            GameObject prefab = garden.ChooseWorldTree(madeTrees + 14);
            if (prefab == null) continue;
            float height = 4.7f + (float)random.NextDouble() * 4.4f;
            Transform anchor = MakeAnchor(container.transform, "forest tree " + madeTrees, dir,
                radius, terrain, .04f, (float)random.NextDouble() * 360f);
            garden.SpawnWorldDecoration(prefab, anchor, new Vector3(height, height, height), "forest tree visual");
            treeDirections.Add(dir);
            madeTrees++;
        }

        int candidates = Mathf.Max(grassCount * 3, 700);
        for (int i = 0; i < candidates; i++)
        {
            if (i % 3 != 0) continue;
            float t = (i + .5f) / candidates;
            float y = 1f - 2f * t;
            float radial = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float angle = i * goldenAngle + 1.17f + garden.seed * .021f;
            Vector3 dir = new Vector3(Mathf.Cos(angle) * radial, y, Mathf.Sin(angle) * radial);
            if (CastleProtected(dir, radius, garden)) continue;
            if (garden.fantasyGrassPrefab == null) break;
            Transform anchor = MakeAnchor(container.transform, "forest ground cover " + madeGrass, dir,
                radius, terrain, .03f, (float)random.NextDouble() * 360f);
            float size = .5f + (float)random.NextDouble() * .55f;
            garden.SpawnWorldDecoration(garden.fantasyGrassPrefab, anchor,
                new Vector3(size * 2.3f, size, size * 2.3f), "forest grass visual");
            madeGrass++;
        }

        for (int i = 0; i < rockCount * 2; i++)
        {
            if (i % 2 != 0) continue;
            float y = 1f - 2f * ((i + .5f) / (rockCount * 2f));
            float radial = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float angle = i * goldenAngle + 2.5f;
            Vector3 dir = new Vector3(Mathf.Cos(angle) * radial, y, Mathf.Sin(angle) * radial);
            if (CastleProtected(dir, radius, garden)) continue;
            Transform anchor = MakeAnchor(container.transform, "mossy rock " + madeRocks, dir,
                radius, terrain, -.10f, (float)random.NextDouble() * 360f);
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.name = "low poly weathered stone";
            stone.transform.SetParent(anchor, false);
            stone.transform.localPosition = new Vector3(0f, .16f, 0f);
            float scale = .5f + (float)random.NextDouble() * 1.0f;
            stone.transform.localScale = new Vector3(scale * 1.2f, scale * .35f, scale * .9f);
            Collider collider = stone.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            madeRocks++;
        }

        bakedRevision = CurrentRevision;
        Debug.Log($"Dream Garden planet-wide forest baked: {madeTrees} trees, {madeGrass} grass patches, {madeRocks} rocks.", this);
    }

    private static Transform MakeAnchor(Transform parent, string label, Vector3 direction,
        float radius, DreamGardenTerrain terrain, float offset, float yaw)
    {
        GameObject anchor = new GameObject(label);
        anchor.transform.SetParent(parent, false);
        float h = terrain.SampleHeight(direction, direction.x * radius, direction.z * radius);
        anchor.transform.localPosition = direction * (radius + h + offset);
        anchor.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction) * Quaternion.Euler(0f, yaw, 0f);
        return anchor.transform;
    }

    private static bool TooCloseToTrees(Vector3 direction, List<Vector3> others, float radius, float spacing)
    {
        float limit = 1f - (spacing * spacing) / (2f * radius * radius);
        foreach (Vector3 other in others)
            if (Vector3.Dot(direction, other) > limit) return true;
        return false;
    }

    private static bool CastleProtected(Vector3 direction, float radius, DreamGardenGenerator garden)
    {
        if (direction.y <= .05f) return false;
        float x = direction.x * radius;
        float z = direction.z * radius;
        float half = garden.mazeCells * garden.cellSize * .5f + 12f;
        // use a square reserve, because the castle and portals are not circular
        return Mathf.Abs(x) < half && Mathf.Abs(z) < half;
    }
}

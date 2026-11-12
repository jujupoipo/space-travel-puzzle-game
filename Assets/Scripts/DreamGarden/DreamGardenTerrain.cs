using UnityEngine;

// terrain relief sampled from the same planet noise used on planet 1
// the playable castle plateau stays smooth while the outer forest receives hills
[DefaultExecutionOrder(900)]
public class DreamGardenTerrain : MonoBehaviour
{
    public CubeSphereGenerator generator;
    public PlanetNoise noise;
    public float plateauRadius = 29f;
    public float transitionWidth = 11f;
    public float reliefMultiplier = 1.05f;
    private Mesh ownedMesh;

    private void Awake()
    {
        // prevent the shared sphere generator from replacing the saved dream garden mesh
        if (generator == null) generator = GetComponent<CubeSphereGenerator>();
        if (generator != null) generator.enabled = false;
    }

    private void Start()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            Debug.LogError("Dream Garden terrain mesh is not saved in this scene. Restore the baked mesh asset before playing.", this);
    }

    public float SampleHeight(Vector3 direction, float localX, float localZ)
    {
        if (noise == null) return 0f;
        float radial = direction.y > 0 ? Mathf.Max(Mathf.Abs(localX), Mathf.Abs(localZ)) : plateauRadius + transitionWidth;
        float blend = Mathf.Clamp01((radial - plateauRadius) / Mathf.Max(0.01f, transitionWidth));
        blend = blend * blend * (3f - 2f * blend);
        return noise.GetHeight(direction) * reliefMultiplier * blend;
    }

    [ContextMenu("Apply Dream Terrain Relief")]
    public void ApplyTerrain()
    {
        if (generator == null) generator = GetComponent<CubeSphereGenerator>();
        if (noise == null) noise = GetComponent<PlanetNoise>();
        if (generator == null || noise == null) return;

        noise.ApplySeed();
        generator.enabled = false;
        generator.GeneratePlanet();
        MeshFilter filter = generator.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return;

        Mesh mesh = filter.sharedMesh;
        ownedMesh = mesh;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 direction = vertices[i].normalized;
            float height = SampleHeight(direction, vertices[i].x, vertices[i].z);
            vertices[i] += direction * height;
            uv[i] = new Vector2(
                Mathf.Atan2(direction.z, direction.x) / (2f * Mathf.PI) + 0.5f,
                Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) / Mathf.PI + 0.5f);
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshCollider collider = generator.GetComponent<MeshCollider>();
        if (collider == null) collider = generator.gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = null;
        collider.sharedMesh = mesh;
    }

    private void OnDestroy()
    {
        if (ownedMesh == null) return;
#if UNITY_EDITOR
        if (UnityEditor.AssetDatabase.Contains(ownedMesh)) return;
#endif
        if (Application.isPlaying) Destroy(ownedMesh); else DestroyImmediate(ownedMesh);
    }
}

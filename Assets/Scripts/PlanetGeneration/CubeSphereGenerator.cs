using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeSphereGenerator : MonoBehaviour
{
    public float radius = 10f;
    [Range(2, 256)] public int resolution = 100;   // use 100-128 for smooth terrain

    public Material planetMaterial;
    public PlanetNoise planetNoise;
    public PlanetBiomeColor biomeColor;

    [Header("Editor")]
    [Tooltip("Regenerate automatically when you change a value. Turn off if it feels slow.")]
    public bool autoUpdate = false;

    // Lowest/highest terrain height found; PlanetScatter uses them to match the biome colors
    [HideInInspector] public float minHeight;
    [HideInInspector] public float maxHeight;

    const string MeshName = "Planet 1 Cube Sphere";

    void Start()
    {
        GeneratePlanet();   // same seed = same planet, so it matches what you saw in the Editor
    }

    // Called by PlanetNoise / PlanetBiomeColor when a value changes in the Inspector
    public void EditorRefresh()
    {
#if UNITY_EDITOR
        if (!autoUpdate) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || Application.isPlaying) return;
            GeneratePlanet();
        };
#endif
    }

#if UNITY_EDITOR
    void OnValidate() => EditorRefresh();
#endif

    // Inspector ⋮ menu → "Generate Planet" builds it in Edit mode
    [ContextMenu("Generate Planet")]
    public void GeneratePlanet()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (resolution < 2)
        {
            Debug.LogError("Planet resolution must be at least 2.", this);
            return;
        }

        if (planetNoise != null) planetNoise.ApplySeed();

        // Delete the previous mesh so regenerating does not leak memory
        if (meshFilter.sharedMesh != null && meshFilter.sharedMesh.name == MeshName)
        {
            if (Application.isPlaying) Destroy(meshFilter.sharedMesh);
            else DestroyImmediate(meshFilter.sharedMesh);
        }

        int verticesPerFace = resolution * resolution;
        int totalVertices = verticesPerFace * 6;

        Vector3[] vertices = new Vector3[totalVertices];
        Vector3[] directions = new Vector3[totalVertices];
        float[] heights = new float[totalVertices];
        float[] rivers = new float[totalVertices];
        Color[] colors = new Color[totalVertices];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6 * 6];

        float minH = float.MaxValue;
        float maxH = float.MinValue;

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int face = 0; face < 6; face++)
        {
            int start = face * verticesPerFace;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float u = (float)x / (resolution - 1);
                    float v = (float)y / (resolution - 1);

                    // Cube point -> sphere direction (length 1)
                    Vector3 direction = GetCubePoint(face, u, v).normalized;

                    float terrainHeight = 0f;
                    if (planetNoise != null)
                        terrainHeight = planetNoise.GetHeight(direction);

                    directions[vertexIndex] = direction;
                    heights[vertexIndex] = terrainHeight;
                    rivers[vertexIndex] = planetNoise != null ? planetNoise.GetRiver(direction) : 0f;
                    vertices[vertexIndex] = direction * (radius + terrainHeight);

                    if (terrainHeight < minH) minH = terrainHeight;
                    if (terrainHeight > maxH) maxH = terrainHeight;

                    vertexIndex++;
                }
            }

            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int a = start + y * resolution + x;
                    int b = a + 1;
                    int c = a + resolution;
                    int d = c + 1;

                    // Pick the winding order so triangles face outward
                    Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                    Vector3 outward = vertices[a].normalized;

                    if (Vector3.Dot(normal, outward) > 0f)
                    {
                        triangles[triangleIndex++] = a; triangles[triangleIndex++] = b; triangles[triangleIndex++] = c;
                        triangles[triangleIndex++] = b; triangles[triangleIndex++] = d; triangles[triangleIndex++] = c;
                    }
                    else
                    {
                        triangles[triangleIndex++] = a; triangles[triangleIndex++] = c; triangles[triangleIndex++] = b;
                        triangles[triangleIndex++] = b; triangles[triangleIndex++] = c; triangles[triangleIndex++] = d;
                    }
                }
            }
        }

        minHeight = minH;
        maxHeight = maxH;

        Mesh mesh = new Mesh();
        mesh.name = MeshName;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();          // normals first: we need them to measure the slope
        Vector3[] normals = mesh.normals;

        // Vertex colors from height, latitude, rivers and slope
        for (int i = 0; i < totalVertices; i++)
        {
            if (biomeColor == null) { colors[i] = Color.white; continue; }

            float normalizedHeight = Mathf.InverseLerp(minH, maxH, heights[i]);
            float latitude = Mathf.Abs(directions[i].y);
            // slope: 0 = flat ground, bigger = steeper (normal tilts away from "straight out")
            float slope = 1f - Mathf.Clamp01(Vector3.Dot(normals[i], directions[i]));

            colors[i] = biomeColor.GetBiomeColor(normalizedHeight, latitude, rivers[i], slope, directions[i]);
        }

        mesh.colors = colors;
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;

        // Collider: refresh it so the player and crystal spawner use the new shape
        MeshCollider col = GetComponent<MeshCollider>();
        if (col == null) col = gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = null;
        col.sharedMesh = mesh;

        if (planetMaterial != null)
            meshRenderer.sharedMaterial = planetMaterial;
    }

    Vector3 GetCubePoint(int face, float u, float v)
    {
        float x = u * 2f - 1f;
        float y = v * 2f - 1f;

        switch (face)
        {
            case 0: return new Vector3(1, y, x);
            case 1: return new Vector3(-1, y, -x);
            case 2: return new Vector3(x, 1, y);
            case 3: return new Vector3(x, -1, -y);
            case 4: return new Vector3(x, y, 1);
            case 5: return new Vector3(-x, y, -1);
            default: return Vector3.zero;
        }
    }
}
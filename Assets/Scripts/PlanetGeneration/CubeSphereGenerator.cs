using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeSphereGenerator : MonoBehaviour
{
    public float radius = 10f;
    public int resolution = 100;

    public Material planetMaterial;
    public PlanetNoise planetNoise;
    public PlanetBiomeColor biomeColor;

    void Start()
    {
        GeneratePlanet();
    }

    void GeneratePlanet()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (resolution < 2)
        {
            Debug.LogError("Planet resolution must be at least 2.", this);
            return;
        }

        int verticesPerFace = resolution * resolution;
        int totalVertices = verticesPerFace * 6;

        Vector3[] vertices = new Vector3[totalVertices];
        Vector3[] directions = new Vector3[totalVertices];
        float[] heights = new float[totalVertices];
        float[] rivers = new float[totalVertices];
        Color[] colors = new Color[totalVertices];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6 * 6];

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

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

                    Vector3 direction = GetCubePoint(face, u, v).normalized;

                    float terrainHeight = 0f;
                    if (planetNoise != null)
                        terrainHeight = planetNoise.GetHeight(direction);

                    directions[vertexIndex] = direction;
                    heights[vertexIndex] = terrainHeight;
                    rivers[vertexIndex] = planetNoise != null ? planetNoise.GetRiver(direction) : 0f;
                    vertices[vertexIndex] = direction * (radius + terrainHeight);

                    if (terrainHeight < minHeight) minHeight = terrainHeight;
                    if (terrainHeight > maxHeight) maxHeight = terrainHeight;

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

                    Vector3 normal = Vector3.Cross(
                        vertices[b] - vertices[a],
                        vertices[c] - vertices[a]
                    );
                    Vector3 outward = vertices[a].normalized;

                    if (Vector3.Dot(normal, outward) > 0f)
                    {
                        triangles[triangleIndex++] = a;
                        triangles[triangleIndex++] = b;
                        triangles[triangleIndex++] = c;
                        triangles[triangleIndex++] = b;
                        triangles[triangleIndex++] = d;
                        triangles[triangleIndex++] = c;
                    }
                    else
                    {
                        triangles[triangleIndex++] = a;
                        triangles[triangleIndex++] = c;
                        triangles[triangleIndex++] = b;
                        triangles[triangleIndex++] = b;
                        triangles[triangleIndex++] = c;
                        triangles[triangleIndex++] = d;
                    }
                }
            }
        }

        for (int i = 0; i < totalVertices; i++)
        {
            if (biomeColor == null)
            {
                colors[i] = Color.white;
                continue;
            }

            float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, heights[i]);
            float latitude = Mathf.Abs(directions[i].y);
            colors[i] = biomeColor.GetBiomeColor(normalizedHeight, latitude, rivers[i]);
        }

        Mesh mesh = new Mesh();
        mesh.name = "Planet 1 Cube Sphere";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        MeshCollider col = GetComponent<MeshCollider>();
        if (col == null) col = gameObject.AddComponent<MeshCollider>();
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
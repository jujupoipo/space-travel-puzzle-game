using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SimplePlanet : MonoBehaviour
{
    [Header("Shape")]
    public float radius = 10f;
    [Range(2, 250)] public int resolution = 64;   // grid points per cube-face edge

    [Header("Noise")]
    public float noiseScale = 1.5f;      // bigger = more, smaller bumps
    public float noiseStrength = 1.5f;   // bigger = taller hills and deeper valleys
    public int seed = 1;                 // different seed = different planet

    // The 6 faces of the cube, each described by the direction it faces
    static readonly Vector3[] faceDirections =
    {
        Vector3.up, Vector3.down, Vector3.left,
        Vector3.right, Vector3.forward, Vector3.back
    };

    void Start()
    {
        Generate();
    }

    // Also callable from the Editor: Inspector ⋮ menu → Generate
    [ContextMenu("Generate")]
    public void Generate()
    {
        int vertsPerFace = resolution * resolution;
        Vector3[] vertices = new Vector3[vertsPerFace * 6];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6 * 6];

        int v = 0;   // next free vertex slot
        int t = 0;   // next free triangle-index slot

        foreach (Vector3 localUp in faceDirections)
        {
            // Two axes that lie ON this face (the "grid directions")
            Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            Vector3 axisB = Vector3.Cross(localUp, axisA);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = v;   // index of the vertex we are creating now

                    // 0..1 position inside this face
                    Vector2 percent = new Vector2(x, y) / (resolution - 1);

                    // Point on the cube: face centre + offsets along the two axes
                    Vector3 pointOnCube = localUp
                        + (percent.x - 0.5f) * 2f * axisA
                        + (percent.y - 0.5f) * 2f * axisB;

                    // Cube → sphere: length becomes exactly 1
                    Vector3 direction = pointOnCube.normalized;

                    // Noise → height → push the vertex outward
                    float height = GetHeight(direction);
                    vertices[v] = direction * (radius + height);
                    v++;

                    // Two triangles per grid square (skip the last row and column)
                    if (x != resolution - 1 && y != resolution - 1)
                    {
                        triangles[t++] = i;
                        triangles[t++] = i + resolution + 1;
                        triangles[t++] = i + resolution;

                        triangles[t++] = i;
                        triangles[t++] = i + 1;
                        triangles[t++] = i + resolution + 1;
                    }
                }
            }
        }

        // Build the mesh (reuse the same one if it already exists)
        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh mesh = filter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh { name = "Simple Planet" };
            filter.sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;  // we may exceed 65,535 vertices
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();   // needed for lighting
        mesh.RecalculateBounds();
    }

    // Height of the ground in a given direction (must be a unit vector)
    public float GetHeight(Vector3 direction)
    {
        float offset = seed * 10.37f;   // moves us to a different part of the noise

        float x = direction.x * noiseScale + offset;
        float y = direction.y * noiseScale + offset;
        float z = direction.z * noiseScale + offset;

        // Three 2D Perlin samples, averaged (0..1)
        float n = (Mathf.PerlinNoise(x, y)
                 + Mathf.PerlinNoise(y, z)
                 + Mathf.PerlinNoise(z, x)) / 3f;

        // Turn 0..1 into roughly -1..+1, then scale by the strength
        return (n - 0.5f) * 2f * noiseStrength;
    }
}
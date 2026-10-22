using UnityEngine;

public class PlanetBiomeColor : MonoBehaviour
{
    [Header("Colors")]
    public Color sand = new Color(0.78f, 0.70f, 0.45f);
    public Color grass = new Color(0.30f, 0.55f, 0.22f);
    public Color forest = new Color(0.14f, 0.36f, 0.16f);
    public Color rock = new Color(0.42f, 0.40f, 0.38f);
    public Color snow = Color.white;
    public Color water = new Color(0.16f, 0.40f, 0.70f);

    [Header("Height bands (0..1 = lowest..highest point of the planet)")]
    [Range(0f, 1f)] public float sandEnd = 0.33f;
    [Range(0f, 1f)] public float grassEnd = 0.50f;
    [Range(0f, 1f)] public float forestEnd = 0.60f;
    [Range(0f, 1f)] public float snowStart = 0.85f;
    [Range(0.01f, 0.2f)] public float blend = 0.05f;   // bigger = softer transitions

    [Header("Slope (steep ground becomes rock)")]
    [Range(0f, 1f)] public float rockSlopeStart = 0.08f;
    [Range(0f, 1f)] public float rockSlopeEnd = 0.25f;

    [Header("Poles")]
    [Range(0f, 1f)] public float snowLatitude = 0.8f;

    [Range(0f, 0.3f)] public float colorVariation = 0.12f;  // random light/dark patches

#if UNITY_EDITOR
    void OnValidate()
    {
        var gen = GetComponent<CubeSphereGenerator>();
        if (gen != null) gen.EditorRefresh();
    }
#endif

    // Smooth 0..1 transition around an edge
    float Edge(float edge, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - blend, edge + blend, value));
    }

    // h = height 0..1, latitude = |y| of the direction, river = 0..1,
    // slope = 0 (flat) to ~0.3+ (steep), dir = direction from planet centre
    public Color GetBiomeColor(float h, float latitude, float river, float slope, Vector3 dir)
    {
        // Height layers, each blended into the next
        Color c = sand;
        c = Color.Lerp(c, grass, Edge(sandEnd, h));
        c = Color.Lerp(c, forest, Edge(grassEnd, h));
        c = Color.Lerp(c, rock, Edge(forestEnd, h));
        c = Color.Lerp(c, snow, Edge(snowStart, h));

        // Snowy poles
        c = Color.Lerp(c, snow, Mathf.InverseLerp(snowLatitude, 1f, latitude));

        // Steep ground = bare rock (cliffs have no grass or snow)
        c = Color.Lerp(c, rock, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(rockSlopeStart, rockSlopeEnd, slope)));

        // Rivers on top
        c = Color.Lerp(c, water, Mathf.Clamp01((river - 0.4f) * 3f));

        // Light/dark patches so the surface is not flat-colored
        float v = Mathf.PerlinNoise(dir.x * 18f + dir.y * 7f + 3f, dir.z * 18f + 11f);
        float k = 1f + (v - 0.5f) * 2f * colorVariation;
        return new Color(c.r * k, c.g * k, c.b * k, 1f);
    }
}
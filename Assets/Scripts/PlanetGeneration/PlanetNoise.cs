using UnityEngine;

public class PlanetNoise : MonoBehaviour
{
    [Header("Base terrain (gentle hills)")]
    public float noiseScale = 1.5f;
    public float height = 1.5f;
    public int octaves = 4;
    public float persistence = 0.45f;
    public float lacunarity = 2f;

    [Header("Mountains")]
    public float mountainHeight = 3f;
    [Tooltip("LOWER = wider, smoother mountain ranges. HIGHER = many small spikes")]
    public float mountainScale = 1.2f;
    [Range(1, 4)] public int mountainOctaves = 3;
    [Tooltip("1 = rounded hills, 3 = sharp ridges")]
    [Range(1f, 3f)] public float mountainSharpness = 1.6f;
    [Tooltip("Cuts off the low part of the ridge noise so mountains only appear on real ridges")]
    [Range(0f, 0.9f)] public float ridgeFloor = 0.6f;

    [Header("Rivers")]
    public float riverScale = 1.5f;
    [Range(0.005f, 0.1f)] public float riverWidth = 0.04f;
    public float riverDepth = 0.6f;

    [Header("Seed")]
    public int seed = 12345;
    public float seedOffset = 0f;

    void Awake() => ApplySeed();

    // Public so the generator can call it in Edit mode (Awake does not run there)
    public void ApplySeed()
    {
        seedOffset = seed * 0.12345f;
    }

#if UNITY_EDITOR
    // Re-generate the planet when a value changes in the Inspector (if Auto Update is on)
    void OnValidate()
    {
        var gen = GetComponent<CubeSphereGenerator>();
        if (gen != null) gen.EditorRefresh();
    }
#endif

    // Fractal noise: several layers of Perlin noise added together (0..1)
    float Fbm(Vector3 d, float scale, int oct, float offset)
    {
        float total = 0f, amp = 1f, freq = scale, max = 0f;
        for (int i = 0; i < oct; i++)
        {
            float x = d.x * freq + seedOffset + offset;
            float y = d.y * freq + seedOffset + offset;
            float z = d.z * freq + seedOffset + offset;

            // 2D Perlin sampled on three planes = pseudo-3D noise on a sphere
            float n = (Mathf.PerlinNoise(x, y) + Mathf.PerlinNoise(y, z) + Mathf.PerlinNoise(z, x)) / 3f;

            total += n * amp;
            max += amp;
            amp *= persistence;
            freq *= lacunarity;
        }
        return total / max;
    }

    // Proper smoothstep: 0 when x <= edge0, 1 when x >= edge1, smooth curve in between.
    // (Mathf.SmoothStep does NOT work like this, so we write our own.)
    static float Smooth(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    // 0 = no mountains here, 1 = mountain zone
    float MountainMask(Vector3 d)
    {
        return Smooth(0.44f, 0.58f, Fbm(d, noiseScale * 0.5f, 2, 50f));
    }

    public float GetRiver(Vector3 d)
    {
        float n = Fbm(d, riverScale, 3, 200f);
        float dist = Mathf.Abs(n - 0.5f);                 // distance to the "river line" (noise = 0.5)
        float river = 1f - Smooth(0f, riverWidth, dist);  // 1 on the line, fading to 0 at riverWidth
        return river * (1f - MountainMask(d));            // no rivers on mountains
    }

    public float GetHeight(Vector3 d)
    {
        // 1) Gentle hills and valleys
        float h = (Fbm(d, noiseScale, octaves, 0f) - 0.5f) * 2f * height;

        // 2) Mountains: "ridged" noise, then shaped so they are rounder and wider
        float n = Fbm(d, mountainScale, mountainOctaves, 100f);
        float ridged = 1f - Mathf.Abs(n * 2f - 1f);              // peaks where noise crosses 0.5
        ridged = Mathf.InverseLerp(ridgeFloor, 1f, ridged);       // keep only the top of the ridges
        ridged = Mathf.Pow(ridged, mountainSharpness);            // sharpness control
        h += ridged * MountainMask(d) * mountainHeight;

        // 3) Carve the rivers
        h -= GetRiver(d) * riverDepth;
        return h;
    }
}
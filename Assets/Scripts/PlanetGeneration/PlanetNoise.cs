using UnityEngine;

public class PlanetNoise : MonoBehaviour
{
    [Header("Base terrain")]
    public float noiseScale = 2f;
    public float height = 1.5f;
    public int octaves = 3;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Mountains")]
    public float mountainHeight = 4f;

    [Header("Rivers")]
    public float riverScale = 1.5f;
    [Range(0.005f, 0.1f)] public float riverWidth = 0.03f;
    public float riverDepth = 0.6f;

    [Header("Seed")]
    public int seed = 12345;
    public float seedOffset = 0f;

    void Awake()
    {
        seedOffset = seed * 0.12345f;
    }

    float Fbm(Vector3 d, float scale, int oct, float offset)
    {
        float total = 0f, amp = 1f, freq = scale, max = 0f;
        for (int i = 0; i < oct; i++)
        {
            float x = d.x * freq + seedOffset + offset;
            float y = d.y * freq + seedOffset + offset;
            float z = d.z * freq + seedOffset + offset;

            float n = (Mathf.PerlinNoise(x, y) + Mathf.PerlinNoise(y, z) + Mathf.PerlinNoise(z, x)) / 3f;

            total += n * amp;
            max += amp;
            amp *= persistence;
            freq *= lacunarity;
        }
        return total / max;
    }

    float MountainMask(Vector3 d)
    {
        return Mathf.SmoothStep(0.45f, 0.6f, Fbm(d, noiseScale * 0.5f, 2, 50f));
    }

    public float GetRiver(Vector3 d)
    {
        float n = Fbm(d, riverScale, 3, 200f);
        float dist = Mathf.Abs(n - 0.5f);
        float river = 1f - Mathf.SmoothStep(0f, riverWidth, dist);
        return river * (1f - MountainMask(d));
    }

    public float GetHeight(Vector3 d)
    {
        float h = (Fbm(d, noiseScale, octaves, 0f) - 0.5f) * 2f * height;

        float ridged = 1f - Mathf.Abs(Fbm(d, noiseScale * 2f, 4, 100f) * 2f - 1f);
        h += ridged * ridged * MountainMask(d) * mountainHeight;

        h -= GetRiver(d) * riverDepth;

        return h;
    }
}
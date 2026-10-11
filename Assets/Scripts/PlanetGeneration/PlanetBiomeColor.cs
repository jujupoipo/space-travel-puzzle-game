using UnityEngine;

public class PlanetBiomeColor : MonoBehaviour
{
    public Color sand = new Color(0.8f, 0.65f, 0.35f);
    public Color grass = new Color(0.2f, 0.5f, 0.2f);
    public Color rock = new Color(0.35f, 0.35f, 0.35f);
    public Color snow = Color.white;
    public Color water = new Color(0.2f, 0.45f, 0.8f);

    public Color GetBiomeColor(float normalizedHeight, float latitude, float river)
    {
        Color baseColor;

        if (normalizedHeight < 0.35f) baseColor = grass;
        else if (normalizedHeight < 0.50f) baseColor = Color.Lerp(grass, sand, (normalizedHeight - 0.35f) / 0.15f);
        else if (normalizedHeight < 0.70f) baseColor = sand;
        else if (normalizedHeight < 0.88f) baseColor = rock;
        else baseColor = snow;

        // Poles are snowy
        if (latitude > 0.75f)
            baseColor = Color.Lerp(baseColor, snow, (latitude - 0.75f) / 0.25f);

        // Rivers on top
        baseColor = Color.Lerp(baseColor, water, Mathf.Clamp01((river - 0.4f) * 3f));

        return baseColor;
    }
}
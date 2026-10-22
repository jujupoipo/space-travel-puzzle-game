using System;
using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{
    private enum PlanetType { Earth, Mars, Moon }

    private struct Crater
    {
        public Vector3 direction;
        public float radius;
    }

    [Header("planet settings")]
    [Min(1f)] public float planetRadius = 4f;
    [Min(10f)] public float planetSpacing = 14f;
    [Range(128, 1024)] public int textureWidth = 512;
    public bool addGravity = true;
    public bool rotatePlanets = true;
    [Range(0f, 30f)] public float rotationSpeed = 4f;

    private void Start()
    {
        CreatePlanet("earth-like planet", PlanetType.Earth, new Vector3(-planetSpacing, 0f, 0f));
        CreatePlanet("mars-like planet", PlanetType.Mars, Vector3.zero);
        CreatePlanet("moon-like planet", PlanetType.Moon, new Vector3(planetSpacing, 0f, 0f));
    }

    private void CreatePlanet(string planetName, PlanetType type, Vector3 position)
    {
        // create a sphere for the planet
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = planetName;
        planet.transform.SetParent(transform, false);
        planet.transform.localPosition = position;
        planet.transform.localScale = Vector3.one * (planetRadius * 2f);

        // create a different texture for each planet
        Texture2D texture = CreateTexture(type);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        Material material = new Material(shader);
        material.name = planetName + " material";
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        else material.mainTexture = texture;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", type == PlanetType.Earth ? 0.3f : 0.1f);
        planet.GetComponent<Renderer>().material = material;

        // reuse the existing gravity system
        if (addGravity)
        {
            GravitySource gravity = planet.AddComponent<GravitySource>();
            gravity.influenceRadius = planetRadius * 2.5f;
            gravity.priority = 0;
            if (type == PlanetType.Earth)
            {
                gravity.rule = GravitySource.Rule.Normal;
                gravity.strength = 20f;
            }
            else if (type == PlanetType.Mars)
            {
                gravity.rule = GravitySource.Rule.Normal;
                gravity.strength = 12f;
            }
            else
            {
                gravity.rule = GravitySource.Rule.Low;
                gravity.strength = 12f;
                gravity.lowGravityMultiplier = 0.3f;
            }
        }

        // rotate the planets slowly
        if (rotatePlanets)
        {
            PlanetRotation rotation = planet.AddComponent<PlanetRotation>();
            rotation.degreesPerSecond = rotationSpeed;
        }
    }

    private Texture2D CreateTexture(PlanetType type)
    {
        int width = textureWidth;
        int height = width / 2;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Crater[] craters = type == PlanetType.Earth
            ? new Crater[0]
            : CreateCraters(type == PlanetType.Moon ? 48 : 24, type == PlanetType.Moon ? 20 : 30);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float latitude = ((y + 0.5f) / height - 0.5f) * Mathf.PI;
            for (int x = 0; x < width; x++)
            {
                // sample the sphere so the texture joins at the edges
                float longitude = (float)x / width * Mathf.PI * 2f;
                Vector3 point = new Vector3(
                    Mathf.Cos(latitude) * Mathf.Cos(longitude),
                    Mathf.Sin(latitude),
                    Mathf.Cos(latitude) * Mathf.Sin(longitude));

                float bigNoise = Noise(point * 2.7f, 11);
                float smallNoise = Noise(point * 8f, 47);
                float terrain = bigNoise * 0.75f + smallNoise * 0.25f;
                Color color;

                if (type == PlanetType.Earth) color = EarthColor(point, terrain, smallNoise);
                else if (type == PlanetType.Mars) color = MarsColor(point, terrain, smallNoise);
                else color = MoonColor(terrain, smallNoise);

                if (type != PlanetType.Earth)
                    color = AddCraters(color, point, craters, type == PlanetType.Moon);

                pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = type.ToString().ToLower() + " texture";
        return texture;
    }

    private float Noise(Vector3 point, int seed)
    {
        // mix three noise directions for a round planet
        float offset = seed * 10.73f;
        float xy = Mathf.PerlinNoise(point.x + offset, point.y + offset);
        float yz = Mathf.PerlinNoise(point.y + offset + 23f, point.z + offset + 23f);
        float zx = Mathf.PerlinNoise(point.z + offset + 51f, point.x + offset + 51f);
        return (xy + yz + zx) / 3f;
    }

    private Color EarthColor(Vector3 point, float terrain, float detail)
    {
        float pole = Mathf.Abs(point.y);
        if (pole > 0.9f) return Color.Lerp(new Color(0.65f, 0.82f, 0.88f), Color.white, pole);

        // blue water, green land and light mountains
        if (terrain < 0.52f)
        {
            float water = Mathf.InverseLerp(0.35f, 0.52f, terrain);
            return Color.Lerp(new Color(0.025f, 0.11f, 0.32f), new Color(0.05f, 0.37f, 0.63f), water);
        }
        if (terrain < 0.55f) return new Color(0.16f, 0.53f, 0.43f);
        float height = Mathf.InverseLerp(0.55f, 0.7f, terrain);
        return Color.Lerp(new Color(0.12f, 0.40f, 0.20f), new Color(0.54f, 0.48f, 0.33f), height * detail);
    }

    private Color MarsColor(Vector3 point, float terrain, float detail)
    {
        // red sand, darker rocks and pale polar areas
        if (Mathf.Abs(point.y) > 0.94f) return new Color(0.87f, 0.77f, 0.66f);
        Color sand = new Color(0.71f, 0.28f, 0.13f);
        Color rocks = new Color(0.35f, 0.13f, 0.09f);
        float dark = Mathf.Clamp01((terrain - 0.39f) * 2.2f + detail * 0.2f);
        return Color.Lerp(sand, rocks, dark);
    }

    private Color MoonColor(float terrain, float detail)
    {
        // gray rock with light and dark patches
        float shade = Mathf.Clamp01(terrain * 0.75f + detail * 0.25f);
        return Color.Lerp(new Color(0.28f, 0.29f, 0.31f), new Color(0.67f, 0.68f, 0.70f), shade);
    }

    private Crater[] CreateCraters(int count, int seed)
    {
        System.Random random = new System.Random(seed);
        Crater[] craters = new Crater[count];
        for (int i = 0; i < count; i++)
        {
            float y = (float)(random.NextDouble() * 2.0 - 1.0);
            float angle = (float)(random.NextDouble() * Math.PI * 2.0);
            float side = Mathf.Sqrt(1f - y * y);
            craters[i].direction = new Vector3(side * Mathf.Cos(angle), y, side * Mathf.Sin(angle));
            craters[i].radius = Mathf.Lerp(0.035f, 0.14f, (float)random.NextDouble());
        }
        return craters;
    }

    private Color AddCraters(Color color, Vector3 point, Crater[] craters, bool strong)
    {
        // draw a dark crater center with a brighter edge
        float effect = strong ? 0.42f : 0.19f;
        foreach (Crater crater in craters)
        {
            float dot = Mathf.Clamp(Vector3.Dot(point, crater.direction), -1f, 1f);
            float distance = Mathf.Sqrt(2f - 2f * dot);
            if (distance < crater.radius * 0.85f)
                color = Color.Lerp(color, Color.black, effect);
            else if (distance < crater.radius)
                color = Color.Lerp(color, Color.black, effect * 0.65f);
            else if (distance < crater.radius * 1.16f)
                color = Color.Lerp(color, Color.white, effect * 0.65f);
        }
        return color;
    }
}

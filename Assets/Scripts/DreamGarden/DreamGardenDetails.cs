using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// geometry and environmental storytelling share the generator's seed and route
public partial class DreamGardenGenerator
{
    private readonly List<Mesh> ownedMeshes = new List<Mesh>();

    private bool ReserveTree(float x, float z, float spacing)
    {
        if (x * x + z * z > (planetRadius - 4f) * (planetRadius - 4f)) return false;
        if (Mathf.Abs(x) < 17f && Mathf.Abs(z) > mazeCells * cellSize * .5f + 9f) return false;
        Vector2 point = new Vector2(x, z);
        foreach (Vector2 other in treePositions) if ((point - other).sqrMagnitude < spacing * spacing) return false;
        treePositions.Add(point);
        return true;
    }

    private void BuildCurvedCourtyard()
    {
        int divisions = mazeCells * 4;
        var vertices = new Vector3[(divisions + 1) * (divisions + 1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new List<int>();
        float edge = mazeCells * cellSize * .5f;
        for (int z = 0; z <= divisions; z++)
            for (int x = 0; x <= divisions; x++)
            {
                int i = z * (divisions + 1) + x;
                float px = Mathf.Lerp(-edge, edge, x / (float)divisions);
                float pz = Mathf.Lerp(-edge, edge, z / (float)divisions);
                vertices[i] = SurfacePoint(px, pz, .06f);
                uv[i] = new Vector2(px, pz) * .35f;
                if (x == divisions || z == divisions) continue;
                int next = i + divisions + 1;
                triangles.Add(i); triangles.Add(next); triangles.Add(i + 1);
                triangles.Add(i + 1); triangles.Add(next); triangles.Add(next + 1);
            }
        Mesh mesh = new Mesh { name = "dream curved courtyard" };
        mesh.vertices = vertices; mesh.uv = uv; mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        ownedMeshes.Add(mesh);
        GameObject floor = new GameObject("ornamented curved courtyard");
        floor.transform.SetParent(transform, false);
        floor.AddComponent<MeshFilter>().sharedMesh = mesh;
        floor.AddComponent<MeshRenderer>().sharedMaterial = pathMaterial;
        // the planet mesh is the only ground collider
    }

    private void AddCornerJoints()
    {
        float edge = mazeCells * cellSize * .5f;
        for (int row = 0; row <= mazeCells; row++)
            for (int col = 0; col <= mazeCells; col++)
            {
                Vector3 at = SurfacePoint(col * cellSize - edge, row * cellSize - edge, 2.7f);
                GameObject joint = Primitive(PrimitiveType.Cube, "mossed castle joint", at,
                    new Vector3(1.02f, 6.15f, 1.02f), stoneMaterial);
                joint.isStatic = true;
            }
    }

    private GameObject AtCell(Vector2Int cell, string label, float side = 0)
    {
        float half = (mazeCells - 1) * .5f;
        Vector2 offset = Vector2.one * side;
        // choose a closed side instead of assuming that every corridor runs north-south
        for (int direction = 0; direction < 4; direction++)
        {
            if (openings[cell.y, cell.x, direction]) continue;
            offset = direction == north ? Vector2.up * side : direction == east ? Vector2.right * side :
                direction == south ? Vector2.down * side : Vector2.left * side;
            break;
        }
        Vector3 at = SurfacePoint((cell.x - half) * cellSize + offset.x, (cell.y - half) * cellSize + offset.y, .12f);
        GameObject root = new GameObject(label);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = at;
        root.transform.localRotation = Quaternion.FromToRotation(Vector3.up, at.normalized);
        return root;
    }

    private DreamInteraction Mechanism(Vector2Int cell, DreamInteraction.Kind kind, int index, string title)
    {
        GameObject root = AtCell(cell, title, 1.05f);
        DreamInteraction interaction = root.AddComponent<DreamInteraction>();
        interaction.kind = kind; interaction.index = index; interaction.title = title;
        root.AddComponent<DreamAnchor>();
        GameObject plinth = CreatePart(root.transform, PrimitiveType.Cylinder, "weathered mechanism base",
            new Vector3(0, .3f, 0), new Vector3(.75f, .3f, .75f), stoneMaterial);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, .5f, 0); collider.size = new Vector3(.65f, 1f, .65f);
        DisableCollider(plinth);
        GameObject seal = CreatePart(root.transform, PrimitiveType.Sphere, "memory seal",
            new Vector3(0, 1.1f, 0), new Vector3(.35f, .35f, .35f), lightMaterial);
        DisableCollider(seal);
        interaction.indicator = seal.GetComponent<Renderer>();
        Label(root.transform, title, new Vector3(0, 1.7f, 0));
        return interaction;
    }

    private void Label(Transform parent, string value, Vector3 localPosition)
    {
        GameObject label = new GameObject("inscribed name");
        label.transform.SetParent(parent, false); label.transform.localPosition = localPosition;
        TextMesh text = label.AddComponent<TextMesh>();
        text.text = value; text.fontSize = 48; text.characterSize = .035f;
        text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
        text.color = new Color(.96f, .88f, .68f);
        label.AddComponent<DreamWorldLabel>();
    }

    private void CreateChallenges(List<Vector2Int> route)
    {
        int step = (route.Count - 1) / 4;
        DreamInteraction keeper = Mechanism(route[step - 1], DreamInteraction.Kind.Keeper, 0, "The Forgotten Keeper");
        Transform figure = keeper.transform;
        Vector3 approach = transform.TransformPoint(CellCenter(route[step - 2].y, route[step - 2].x)) - figure.position;
        figure.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(-approach, figure.up), figure.up);
        CreateKeeperCloak(figure);
        GameObject hood = CreatePart(figure, PrimitiveType.Sphere, "keeper stone hood",
            new Vector3(0, 1.72f, 0), new Vector3(.55f, .64f, .52f), stoneMaterial);
        GameObject face = CreatePart(figure, PrimitiveType.Sphere, "empty face beneath hood",
            new Vector3(0, 1.73f, -.24f), new Vector3(.33f, .37f, .08f), barkMaterial);
        DisableCollider(hood); DisableCollider(face);
        keeper.indicator.transform.localPosition = new Vector3(.32f, 1.15f, -.24f);
        GameObject staff = CreatePart(figure, PrimitiveType.Cylinder, "keeper lantern staff",
            new Vector3(.38f, 1f, 0), new Vector3(.055f, .85f, .055f), barkMaterial);
        DisableCollider(staff);
        Light lantern = keeper.indicator.gameObject.AddComponent<Light>();
        lantern.color = new Color(.7f, .77f, 1f); lantern.range = 3.5f; lantern.intensity = .7f; lantern.shadows = LightShadows.None;
        Transform name = figure.Find("inscribed name");
        if (name != null) name.localPosition = new Vector3(0, 2.35f, 0);
        string[] seals = { "Root | RUIN", "Dew | BLOOM", "Blossom | RUIN" };
        for (int i = 0; i < 3; i++)
        {
            DreamInteraction seal = Mechanism(route[step * 2 - 3 + i], DreamInteraction.Kind.Petal, i, seals[i]);
            // each shape remains readable without relying on color
            for (int petal = 0; petal <= i; petal++)
            {
                GameObject mark = CreatePart(seal.transform, PrimitiveType.Cube, "carved garden notch",
                    new Vector3((petal - i * .5f) * .15f, .68f, -.3f), new Vector3(.07f, .15f, .06f), pinkMaterial);
                DisableCollider(mark);
            }
        }
        string[] symbols = { "Lantern", "Crown", "Seed", "Wing" };
        DreamInteraction clue = Mechanism(route[step * 3 - 5], DreamInteraction.Kind.Inscription, 0, "The pilgrim's inscription");
        clue.inscription = "A seed slept beneath the abandoned gate. A lantern warmed its first leaf. A wing carried its blossom beyond the walls. Only then did a crown fall, and the sky remember its stars.\n\nIn Bloom, wake the four names in the order of this journey. A false note silences the whole constellation.";
        for (int i = 0; i < 4; i++)
        {
            DreamInteraction symbol = Mechanism(route[step * 3 - 4 + i], DreamInteraction.Kind.Astral, i, symbols[i]);
            CreateStarMesh(symbol.transform, new Vector3(0, 1.1f, .15f), .3f, pedestalStarMaterial);
        }
        DreamInteraction entry = Mechanism(route[1], DreamInteraction.Kind.Inscription, 1, "A gardener's last letter");
        entry.inscription = "If you find our houses empty, seek the Keeper inside the castle. The Moon must remember before the garden can wake.\n\nThe garden obeys three vows: roots endure in Ruin; dew gathers in Bloom; the last blossom opens in Ruin. Touch their seals in that order.\n\nE: read or interact. Q: shift at a memory stone. R: return to your last restored memory. The final arch requires Moon, Petal and Star.";
    }

    private void CreateAtmosphere()
    {
        DreamGardenAtmosphere atmosphere = GetComponent<DreamGardenAtmosphere>();
        if (atmosphere == null) atmosphere = gameObject.AddComponent<DreamGardenAtmosphere>();
        atmosphere.puzzle = puzzle;
        GameObject localVolume = new GameObject("garden atmosphere volume");
        localVolume.transform.SetParent(transform, false);
        SphereCollider region = localVolume.AddComponent<SphereCollider>();
        region.isTrigger = true; region.radius = planetRadius + 22f;
        Volume volume = localVolume.AddComponent<Volume>();
        volume.isGlobal = false; volume.blendDistance = 12f; volume.priority = 20f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Dream Garden Local Atmosphere";
        ColorAdjustments colors = profile.Add<ColorAdjustments>(true);
        colors.colorFilter.value = new Color(1f, .93f, .88f); colors.saturation.value = 8f;
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = .3f; bloom.threshold.value = 1f;
        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.value = .2f; vignette.smoothness.value = .7f;
        volume.sharedProfile = profile; atmosphere.volume = volume;
        atmosphere.bloomDetails = new GameObject("bloom - petals and flowers");
        atmosphere.ruinDetails = new GameObject("ruin - exposed roots and blue embers");
        atmosphere.bloomDetails.transform.SetParent(transform, false);
        atmosphere.ruinDetails.transform.SetParent(transform, false);
        for (int i = 0; i < 32; i++)
        {
            int col = random.Next(mazeCells); int row = random.Next(mazeCells);
            float half = (mazeCells - 1) * .5f;
            // details sit beside wall junctions, leaving the cell center clear
            Vector3 at = SurfacePoint((col - half) * cellSize + cellSize * .35f,
                (row - half) * cellSize + cellSize * .35f, .17f);
            GameObject flower = Primitive(PrimitiveType.Sphere, "fallen sakura petals", at,
                new Vector3(.35f, .08f, .35f), pinkMaterial);
            DisableCollider(flower); flower.transform.SetParent(atmosphere.bloomDetails.transform, true);
            GameObject root = Primitive(PrimitiveType.Cube, "exposed silver root", at,
                new Vector3(.1f, .12f, .7f), barkMaterial);
            DisableCollider(root); root.transform.SetParent(atmosphere.ruinDetails.transform, true);
            if (i % 8 == 0 && fantasyMushroomPrefab != null)
                SpawnPackModel(fantasyMushroomPrefab, flower.transform, new Vector3(.4f,.4f,.4f), "dream mushroom");
        }
        GameObject particles = new GameObject("drifting dream motes");
        particles.transform.SetParent(transform, false);
        particles.transform.localPosition = Vector3.up * (planetRadius + 3f);
        particles.transform.localRotation = Quaternion.Euler(90, 0, 0);
        ParticleSystem motes = particles.AddComponent<ParticleSystem>();
        motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = motes.main;
        main.maxParticles = 100; main.startLifetime = 9f; main.startSpeed = .15f;
        main.startSize = .06f; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(1f, .67f, .79f, .6f);
        var emission = motes.emission; emission.rateOverTime = 10f;
        var shape = motes.shape; shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(mazeCells * cellSize, mazeCells * cellSize, 8f);
        var particleRenderer = motes.GetComponent<ParticleSystemRenderer>();
        Material moteMaterial = moteShader != null ? new Material(moteShader) : new Material(lightMaterial);
        moteMaterial.name = "dream soft motes"; ownedMaterials.Add(moteMaterial);
        particleRenderer.sharedMaterial = moteMaterial;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        atmosphere.motes = motes;
        atmosphere.SetPhase(true);
        if (Application.isPlaying) motes.Play();
    }

    private void CreateKeeperCloak(Transform parent)
    {
        const int sides = 24;
        Vector3[] vertices = new Vector3[sides * 3];
        Vector2[] uv = new Vector2[vertices.Length];
        var triangles = new List<int>();
        for (int ring = 0; ring < 3; ring++)
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                float radius = Mathf.Lerp(.44f, .23f, ring * .5f) + (i % 2 == 0 ? .025f : 0);
                int index = ring * sides + i;
                vertices[index] = new Vector3(Mathf.Cos(angle) * radius, .5f + ring * .53f, Mathf.Sin(angle) * radius);
                uv[index] = new Vector2(i / (float)sides, ring * .5f);
                if (ring == 2) continue;
                int next = ring * sides + (i + 1) % sides;
                triangles.Add(index); triangles.Add(index + sides); triangles.Add(next);
                triangles.Add(next); triangles.Add(index + sides); triangles.Add(next + sides);
            }
        Mesh mesh = new Mesh { name = "keeper pleated mantle" };
        mesh.vertices = vertices; mesh.uv = uv; mesh.SetTriangles(triangles,0); mesh.RecalculateNormals();
        ownedMeshes.Add(mesh);
        GameObject cloak = new GameObject("keeper pleated mantle"); cloak.transform.SetParent(parent, false);
        cloak.AddComponent<MeshFilter>().sharedMesh = mesh;
        cloak.AddComponent<MeshRenderer>().sharedMaterial = purpleMaterial;
    }
}

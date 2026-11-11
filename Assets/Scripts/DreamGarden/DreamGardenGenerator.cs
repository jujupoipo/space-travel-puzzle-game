using System.Collections.Generic;
using UnityEngine;

public partial class DreamGardenGenerator : MonoBehaviour
{
    [Header("planet")]
    [Min(52f)] public float planetRadius = 70f;
    public int seed = 56;
    [Range(9, 13)] public int mazeCells = 13;
    [Range(3.7f, 4.8f)] public float cellSize = 4.0f;
    public DreamPuzzleController puzzle;

    [Header("visual revision")]
    [SerializeField] private int generatedVisualVersion;
    private const int CurrentVisualVersion = 5;
    public Shader terrainShader;
    public Shader moteShader;
    public GameObject fantasyHousePrefab;
    public GameObject fantasyMushroomPrefab;
    private readonly List<Vector2> treePositions = new List<Vector2>();

    [Header("medieval castle pack")]
    public GameObject castleWallPrefab;
    public GameObject castlePassagePrefab;
    public GameObject castleTowerPrefab;
    public GameObject castleDoorPrefab;
    public Material castleMaterial;

    [Header("fantasy forest pack")]
    public GameObject fantasyTreePrefab;
    public GameObject fantasyGrassPrefab;
    public Material forestGrassMaterial;
    public Material forestDirtMaterial;

    [Header("japanese garden pack")]
    public GameObject[] sakuraPrefabs = new GameObject[0];
    public GameObject[] maplePrefabs = new GameObject[0];
    public Material japaneseGrassMaterial;
    public Material japaneseRockMaterial;

    [Header("terrain relief")]
    [Range(0f, 2f)] public float reliefStrength = 1.05f;
    [Range(4f, 18f)] public float reliefTransition = 11f;

    [Header("foliage and structure")]
    [Range(0f, 1f)] public float foliageDensity = 0.82f;
    [Range(12, 64)] public int perimeterTreeCount = 42;
    [Range(20, 120)] public int groundDetailCount = 70;
    [Range(0.75f, 2f)] public float wallEmbedDepth = 0.28f;

    private DreamGardenTerrain currentTerrain;
    private readonly Dictionary<Material, Material> convertedMaterials = new Dictionary<Material, Material>();

    private const int north = 0;
    private const int east = 1;
    private const int south = 2;
    private const int west = 3;

    private bool[,,] openings;
    private readonly List<DreamLantern> lanterns = new List<DreamLantern>();
    private readonly List<DreamPhaseGate> phaseGates = new List<DreamPhaseGate>();
    private System.Random random;
    private Material groundMaterial;
    private Material pathMaterial;
    private Material hedgeMaterial;
    private Material pinkMaterial;
    private Material purpleMaterial;
    private Material stoneMaterial;
    private Material barkMaterial;
    private Material lightMaterial;
    private Material pedestalMoonMaterial;
    private Material pedestalPetalMaterial;
    private Material pedestalStarMaterial;
    private readonly List<Material> ownedMaterials = new List<Material>();
    public IReadOnlyList<Vector2Int> SolutionRoute { get; private set; }

    [ContextMenu("Generate Dream Garden")]
    public void GenerateDreamGarden()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                child.gameObject.SetActive(false);
                child.SetParent(null, true);
                Destroy(child.gameObject);
            }
            else DestroyImmediate(child.gameObject);
        }

        ReleaseMaterials();
        ReleaseMeshes();
#if UNITY_EDITOR
        BindVisualAssets();
#endif
        mazeCells = Mathf.Clamp(mazeCells, 9, 13);
        cellSize = Mathf.Clamp(cellSize, 3.7f, 4.8f);
        planetRadius = Mathf.Max(52f, planetRadius, mazeCells * cellSize * .72f + 22f);
        lanterns.Clear();
        phaseGates.Clear();
        treePositions.Clear();

        random = new System.Random(seed);
        CreateMaterials();
        CreatePlanet();
        List<Vector2Int> solution = GenerateMaze();
        SolutionRoute = solution.AsReadOnly();
        BuildMaze(solution);
        AddOuterForest();
        AddSakuras();
        AddRuinsAndHouses();
        AddFloatingStars();
        CreateMemories(solution);
        CreateChallenges(solution);
        CreateEntryAnchor();
        Vector3 exit = CreateExit(solution[solution.Count - 1]);
        CreateAtmosphere();
        if (puzzle != null) puzzle.Initialize(lanterns, phaseGates, exit);
        generatedVisualVersion = CurrentVisualVersion;
        if (Application.isPlaying) FixRuntimeSpawnAfterUpgrade();
    }

    private void FixRuntimeSpawnAfterUpgrade()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Vector3 spawn = transform.TransformPoint(Entrance(1.7f));
        Vector3 up = (spawn - transform.position).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.ProjectOnPlane(Vector3.right, up).normalized;
        Quaternion rotation = Quaternion.LookRotation(forward, up);
        if (scene == "DreamGarden" || scene == "DreamGardenStandalone")
        {
            if (puzzle == null || puzzle.player == null) return;
            Transform actor = puzzle.player;
            Rigidbody body = actor.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.position = spawn;
                body.rotation = rotation;
            }
            actor.SetPositionAndRotation(spawn, rotation);
            Physics.SyncTransforms();
        }
        else if (scene == "two_planets")
        {
            GameObject arrival = GameObject.Find("PortalToDreamGarden");
            if (arrival != null) arrival.transform.SetPositionAndRotation(spawn, rotation);
        }
    }

    private void Awake()
    {
        // the baked mesh is the scene source of truth; do not regenerate it at startup
        CubeSphereGenerator sphere = GetComponentInChildren<CubeSphereGenerator>(true);
        if (sphere != null && sphere.GetComponent<DreamGardenTerrain>() != null)
            sphere.enabled = false;
    }

    private void Start() => EnsureInitialized();

    public void EnsureInitialized()
    {
        // bind the objects already serialized in the scene; never overwrite manual edits
        EnsureEnglishSceneText();
        if (puzzle != null && puzzle.Ready) return;
        DreamLantern[] existing = GetComponentsInChildren<DreamLantern>(true);
        if (existing.Length == 0)
        {
            Debug.LogError("Dream Garden saved objects are missing. Reopen a baked scene or rebuild explicitly in the Editor.", this);
            return;
        }
        if (generatedVisualVersion != CurrentVisualVersion)
            Debug.LogWarning("Dream Garden scene uses visual version " + generatedVisualVersion +
                ". Saved objects are preserved; use the editor rebuild only if you intentionally want new geometry.", this);

        Transform exit = null;
        DreamExitMarker marker = GetComponentInChildren<DreamExitMarker>(true);
        if (marker != null) exit = marker.transform;
        else foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name.StartsWith("castle portal")) { exit = child; break; }

        if (puzzle == null || exit == null)
        {
            Debug.LogError("Dream garden needs its puzzle manager and castle exit.", this);
            return;
        }
        foreach (DreamLantern lantern in existing)
            if (lantern.GetComponent<DreamAnchor>() == null) lantern.gameObject.AddComponent<DreamAnchor>();
        puzzle.planetGravity = GetComponentInChildren<GravitySource>(true);
        puzzle.Initialize(new List<DreamLantern>(existing),
            new List<DreamPhaseGate>(GetComponentsInChildren<DreamPhaseGate>(true)), exit.position);
    }

    private void EnsureEnglishSceneText()
    {


        string[] memoryNames = { "Moon", "Petal", "Star" };
        foreach (DreamLantern lantern in GetComponentsInChildren<DreamLantern>(true))
        {
            if (lantern == null || lantern.orderIndex < 0 || lantern.orderIndex >= memoryNames.Length)
                continue;
            string englishName = memoryNames[lantern.orderIndex];
            lantern.memoryName = englishName;
            Transform label = lantern.transform.Find("memory label");
            if (label != null)
            {
                TextMesh mesh = label.GetComponent<TextMesh>();
                if (mesh != null)
                    mesh.text = (lantern.orderIndex + 1) + "  " + englishName.ToUpperInvariant();
            }
        }
        foreach (TextMesh label in GetComponentsInChildren<TextMesh>(true))
        {
            if (label.text == "FLOR") label.text = "BLOOM";
            else if (label.text == "RUÍNA") label.text = "RUIN";
        }
    }

    private void ReleaseMaterials()
    {
        foreach (Material material in ownedMaterials)
            if (material != null)
            {
#if UNITY_EDITOR
                if (UnityEditor.AssetDatabase.Contains(material)) continue;
#endif
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        ownedMaterials.Clear();
        convertedMaterials.Clear();
    }

    private void OnDestroy() { ReleaseMaterials(); ReleaseMeshes(); }

    private void ReleaseMeshes()
    {
        foreach (Mesh mesh in ownedMeshes)
        {
            if (mesh == null) continue;
#if UNITY_EDITOR
            if (UnityEditor.AssetDatabase.Contains(mesh)) continue;
#endif
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
        ownedMeshes.Clear();
    }

    public Vector3 SurfacePoint(float x, float z, float height = 0f)
    {
        float r = planetRadius;
        float y = Mathf.Sqrt(Mathf.Max(0.01f, r * r - x * x - z * z));
        Vector3 surface = new Vector3(x, y, z);
        Vector3 direction = surface.normalized;
        float relief = currentTerrain != null ? currentTerrain.SampleHeight(direction, x, z) : 0f;
        return surface + direction * (height + relief);
    }

    public Vector3 CellCenter(int row, int col, float height = 0f)
    {
        float half = (mazeCells - 1) * 0.5f;
        return SurfacePoint((col - half) * cellSize, (row - half) * cellSize, height);
    }

    public Vector3 Entrance(float playerHeight)
    {
        return CellCenter(0, 0, playerHeight + 0.3f);
    }

    private void CreateMaterials()
    {
        groundMaterial = MakeMaterial("dream garden three biome ground", new Color(.86f, .87f, .80f), false);
        if (terrainShader != null) groundMaterial.shader = terrainShader;
        if (groundMaterial.HasProperty("_BaseColor")) groundMaterial.SetColor("_BaseColor", new Color(.86f, .87f, .80f));
        Texture grass = FindMaterialTexture(forestGrassMaterial != null ? forestGrassMaterial : japaneseGrassMaterial);
        Texture dirt = FindMaterialTexture(forestDirtMaterial);
        Texture rock = FindMaterialTexture(japaneseRockMaterial);
        if (grass != null && groundMaterial.HasProperty("_GrassMap")) groundMaterial.SetTexture("_GrassMap", grass);
        if (dirt != null && groundMaterial.HasProperty("_DirtMap")) groundMaterial.SetTexture("_DirtMap", dirt);
        if (rock != null && groundMaterial.HasProperty("_RockMap")) groundMaterial.SetTexture("_RockMap", rock);
        if (grass != null && groundMaterial.HasProperty("_BaseMap")) groundMaterial.SetTexture("_BaseMap", grass);
        if (groundMaterial.HasProperty("_Tiling")) groundMaterial.SetFloat("_Tiling", .20f);
        if (groundMaterial.HasProperty("_RockSlope")) groundMaterial.SetFloat("_RockSlope", .17f);
        if (groundMaterial.HasProperty("_PlanetRadius")) groundMaterial.SetFloat("_PlanetRadius", planetRadius);
        pathMaterial = MakeMaterial("castle courtyard floor", new Color(0.47f, 0.47f, 0.52f), false);
        ApplySourceTexture(pathMaterial, forestDirtMaterial != null ? forestDirtMaterial : castleMaterial);
        hedgeMaterial = MakeMaterial("castle wall stone", new Color(0.59f, 0.60f, 0.69f), false);
        ApplySourceTexture(hedgeMaterial, castleMaterial);
        pinkMaterial = MakeMaterial("sakura pink", new Color(0.92f, 0.62f, 0.78f), false);
        purpleMaterial = MakeMaterial("ruin violet", new Color(0.51f, 0.42f, 0.68f), false);
        stoneMaterial = MakeMaterial("old stone", new Color(0.59f, 0.60f, 0.69f), false);
        barkMaterial = MakeMaterial("sakura wood", new Color(0.37f, 0.25f, 0.36f), false);
        lightMaterial = MakeMaterial("memory starlight", new Color(0.95f, 0.82f, 1f), false);
        pedestalMoonMaterial = MakeMaterial("pedestal moon", new Color(0.62f, 0.57f, 0.79f), false);
        pedestalPetalMaterial = MakeMaterial("pedestal petal", new Color(0.87f, 0.60f, 0.73f), false);
        pedestalStarMaterial = MakeMaterial("pedestal star", new Color(0.92f, 0.81f, 0.60f), false);

        if (lightMaterial.HasProperty("_EmissionColor"))
        {
            lightMaterial.EnableKeyword("_EMISSION");
            lightMaterial.SetColor("_EmissionColor", new Color(0.85f, 0.50f, 1f) * 2.1f);
        }
        if (pedestalStarMaterial.HasProperty("_EmissionColor"))
        {
            pedestalStarMaterial.EnableKeyword("_EMISSION");
            pedestalStarMaterial.SetColor("_EmissionColor", new Color(0.8f, 0.65f, 0.3f) * 0.8f);
        }
    }

    private Texture FindMaterialTexture(Material material)
    {
        if (material == null) return null;
        Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
        if (texture == null && material.HasProperty("_MainTex")) texture = material.GetTexture("_MainTex");
        return texture;
    }

    // reuse the already validated pack placement routine for the planet-wide forest
    public void SpawnWorldDecoration(GameObject prefab, Transform anchor, Vector3 dimensions, string label)
    {
        SpawnPackModel(prefab, anchor, dimensions, label);
    }

    public GameObject ChooseWorldTree(int index) => PickTree(index);

    private Material MakeMaterial(string label, Color color, bool doubleSided)
    {
        Shader shader = FindBestShader(doubleSided, true);
        Material result = new Material(shader);
        result.enableInstancing = true;
        result.EnableKeyword("_EMISSION");
        result.name = label;
        ownedMaterials.Add(result);
        if (result.HasProperty("_BaseColor")) result.SetColor("_BaseColor", color);
        else result.color = color;
        if (result.HasProperty("_Smoothness")) result.SetFloat("_Smoothness", doubleSided ? 0.12f : 0.25f);
        if (result.HasProperty("_Metallic")) result.SetFloat("_Metallic", 0f);
        if (doubleSided)
        {
            if (result.HasProperty("_Cull")) result.SetFloat("_Cull", 0f);
            if (result.HasProperty("_AlphaClip")) result.SetFloat("_AlphaClip", 1f);
            if (result.HasProperty("_Cutoff")) result.SetFloat("_Cutoff", 0.3f);
            result.EnableKeyword("_ALPHATEST_ON");
        }
        return result;
    }

    private Shader FindBestShader(bool foliage, bool allowSimple)
    {
        string[] names = foliage
            ? new[]
            {
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Lit",
                "Nature/SpeedTree",
                "Standard"
            }
            : new[]
            {
                "Universal Render Pipeline/Lit",
                allowSimple ? "Universal Render Pipeline/Simple Lit" : "",
                "Standard",
                "Sprites/Default"
            };
        foreach (string name in names)
        {
            if (string.IsNullOrEmpty(name)) continue;
            Shader shader = Shader.Find(name);
            if (shader != null) return shader;
        }
        return Shader.Find("Sprites/Default");
    }

    private void CreatePlanet()
    {
        GameObject planet = new GameObject();
        planet.name = "planet 2 - the forgotten dream garden";
        planet.transform.SetParent(transform, false);
        planet.transform.localPosition = Vector3.zero;

        CubeSphereGenerator mesh = planet.AddComponent<CubeSphereGenerator>();
        mesh.enabled = false;
        mesh.radius = planetRadius;
        mesh.resolution = 78;
        mesh.planetMaterial = groundMaterial;

        PlanetNoise noise = planet.AddComponent<PlanetNoise>();
        noise.seed = seed + 801;
        noise.noiseScale = 1.45f;
        noise.height = 2.6f;
        noise.mountainHeight = 6.6f;
        noise.mountainScale = 1.18f;
        noise.mountainSharpness = 1.7f;
        noise.riverDepth = 0.18f;
        noise.ApplySeed();

        currentTerrain = planet.AddComponent<DreamGardenTerrain>();
        currentTerrain.generator = mesh;
        currentTerrain.noise = noise;
        currentTerrain.plateauRadius = mazeCells * cellSize * 0.5f + 4.5f;
        currentTerrain.transitionWidth = reliefTransition;
        currentTerrain.reliefMultiplier = reliefStrength;
        currentTerrain.ApplyTerrain();

        GravitySource gravity = planet.AddComponent<GravitySource>();
        gravity.rule = GravitySource.Rule.Normal;
        gravity.strength = 18f;
        gravity.priority = 1;
        gravity.influenceRadius = planetRadius * 1.34f;
        if (puzzle != null) puzzle.planetGravity = gravity;
    }

    private List<Vector2Int> GenerateMaze()
    {
        DreamMazeLayout layout = new DreamMazeLayout(mazeCells, seed);
        openings = layout.Openings;
        List<Vector2Int> route = new List<Vector2Int>();
        foreach (DreamMazeLayout.Cell cell in layout.Route)
            route.Add(new Vector2Int(cell.Col, cell.Row));
        return route;
    }

    private void BuildMaze(List<Vector2Int> route)
    {
        BuildCurvedCourtyard();
        for (int row = 0; row < mazeCells; row++)
        {
            for (int col = 0; col < mazeCells; col++)
            {
                if (!openings[row, col, north]) PlaceWall(row, col, north, hedgeMaterial, "hedge north");
                if (!openings[row, col, east]) PlaceWall(row, col, east, hedgeMaterial, "hedge east");
                if (row == 0) PlaceWall(row, col, south, hedgeMaterial, "outer hedge");
                if (col == 0) PlaceWall(row, col, west, hedgeMaterial, "outer hedge");
            }
        }

        int step = Mathf.Max(3, (route.Count - 1) / 4);
        MakePhaseGate(route[step + 1], route[step + 2], false);
        MakePhaseGate(route[step * 2 + 1], route[step * 2 + 2], true);
        AddCastleTowers();
        AddCornerJoints();
        AddCourtyardFoliage(route);
        AddCourtyardRocks();
    }

    private void MakePhaseGate(Vector2Int a, Vector2Int b, bool opensInBloom)
    {
        int direction = GetDirection(a, b);
        GameObject root = new GameObject(opensInBloom ? "bloom phase gate" : "ruin phase gate");
        root.transform.SetParent(transform, false);
        DreamPhaseGate gate = root.AddComponent<DreamPhaseGate>();
        gate.openInBloom = opensInBloom;
        gate.player = puzzle != null ? puzzle.player : null;

        GameObject arch = PlaceWall(a.y, a.x, direction, stoneMaterial, "phase arch");
        arch.transform.SetParent(root.transform, true);
        BoxCollider archCollider = arch.GetComponent<BoxCollider>();
        if (archCollider != null) archCollider.enabled = false;

        GameObject barrier = CreatePart(arch.transform, PrimitiveType.Cube, "dream gate",
            new Vector3(0f, 2.55f, 0f), new Vector3(cellSize + .4f, 5.7f, 0.55f),
            opensInBloom ? pinkMaterial : purpleMaterial);
        gate.blockingWall = barrier;
        CreatePhaseBanner(root.transform, arch.transform.position + arch.transform.up * 5.0f,
            arch.transform.rotation, opensInBloom ? "BLOOM" : "RUIN", opensInBloom ? pinkMaterial : purpleMaterial);
        phaseGates.Add(gate);
    }

    private void CreatePhaseBanner(Transform parent, Vector3 position, Quaternion rotation, string textValue, Material mat)
    {
        GameObject sign = new GameObject("phase clue");
        sign.transform.SetParent(parent, false);
        sign.transform.position = position;
        sign.transform.rotation = rotation;

        GameObject board = CreatePart(sign.transform, PrimitiveType.Cube, "phase board",
            new Vector3(0f, 0f, 0f), new Vector3(1.6f, 0.45f, 0.1f), mat);
        Collider boardCollider = board.GetComponent<Collider>();
        if (boardCollider != null) boardCollider.enabled = false;

        TextMesh label = sign.AddComponent<TextMesh>();
        label.text = textValue;
        label.characterSize = .075f;
        label.fontSize = 40;
        label.anchor = TextAnchor.MiddleCenter;
        label.color = new Color(1, .95f, 1);
    }

    private int GetDirection(Vector2Int a, Vector2Int b)
    {
        if (b.y > a.y) return north;
        if (b.x > a.x) return east;
        if (b.y < a.y) return south;
        return west;
    }

    private GameObject PlaceWall(int row, int col, int direction, Material material, string label)
    {
        float half = (mazeCells - 1) * 0.5f;
        float x = (col - half) * cellSize;
        float z = (row - half) * cellSize;
        if (direction == north) z += cellSize * 0.5f;
        if (direction == south) z -= cellSize * 0.5f;
        if (direction == east) x += cellSize * 0.5f;
        if (direction == west) x -= cellSize * 0.5f;

        bool alongX = direction == north || direction == south;
        Vector3 point = SurfacePoint(x, z, -wallEmbedDepth);
        Vector3 normal = point.normalized;
        Vector3 a = SurfacePoint(x - (alongX ? cellSize * .5f : 0), z - (alongX ? 0 : cellSize * .5f), -wallEmbedDepth);
        Vector3 b = SurfacePoint(x + (alongX ? cellSize * .5f : 0), z + (alongX ? 0 : cellSize * .5f), -wallEmbedDepth);
        Vector3 tangent = (b - a).normalized;
        float wallLength = Vector3.Distance(a, b) + .55f;
        GameObject wall = new GameObject(label);
        wall.transform.SetParent(transform, false);
        wall.transform.localPosition = point;
        wall.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(tangent, normal), normal);

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 2.72f, 0f);
        collider.size = new Vector3(wallLength, 5.9f, 0.8f);

        GameObject model = castleWallPrefab;
        if (label == "phase arch" && castlePassagePrefab != null) model = castlePassagePrefab;
        if (model != null)
        {
            SpawnPackModel(model, wall.transform, new Vector3(wallLength, 5.9f, .85f), label + " - castle mesh");
            AddWallButtresses(wall.transform);
        }
        else
        {
            GameObject block = CreatePart(wall.transform, PrimitiveType.Cube, "fallback stone wall",
                new Vector3(0f, 2.75f, 0f), collider.size, material);
            Collider duplicate = block.GetComponent<Collider>();
            if (duplicate != null) duplicate.enabled = false;
        }
        return wall;
    }

    private void AddWallButtresses(Transform wall)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject brace = CreatePart(wall, PrimitiveType.Cube, "wall brace",
                new Vector3(side * (cellSize * 0.47f), 1.95f, 0f), new Vector3(0.34f, 3.9f, 0.75f), stoneMaterial);
            Collider c = brace.GetComponent<Collider>();
            if (c != null) c.enabled = false;
        }
    }

    private void AddCastleTowers()
    {
        float edge = (mazeCells * cellSize) * 0.5f + 0.7f;
        for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 pos = SurfacePoint(edge * x, edge * z, -0.02f);
                GameObject point = new GameObject("castle corner tower");
                point.transform.SetParent(transform, false);
                point.transform.localPosition = pos;
                point.transform.localRotation = Quaternion.FromToRotation(Vector3.up, pos.normalized);
                if (castleTowerPrefab != null)
                    SpawnPackModel(castleTowerPrefab, point.transform, new Vector3(5.4f, 10f, 5.4f), "castle tower model");
                else
                {
                    GameObject tower = CreatePart(point.transform, PrimitiveType.Cylinder, "fallback tower",
                        new Vector3(0f, 4f, 0f), new Vector3(2.2f, 4f, 2.2f), stoneMaterial);
                    Collider towerCol = tower.GetComponent<Collider>();
                    if (towerCol != null) towerCol.enabled = false;
                }
            }
    }

    private void AddCourtyardFoliage(List<Vector2Int> route)
    {
        HashSet<Vector2Int> routeCells = new HashSet<Vector2Int>(route);
        for (int row = 0; row < mazeCells; row++)
        {
            for (int col = 0; col < mazeCells; col++)
            {
                Vector2Int cell = new Vector2Int(col, row);
                if (routeCells.Contains(cell)) continue;
                if (random.NextDouble() > foliageDensity) continue;

                Vector3 at = CellCenter(row, col, 0.03f);
                if (fantasyGrassPrefab != null)
                {
                    GameObject tuft = new GameObject("castle courtyard grass");
                    tuft.transform.SetParent(transform, false);
                    tuft.transform.localPosition = at;
                    tuft.transform.localRotation = Quaternion.FromToRotation(Vector3.up, at.normalized)
                        * Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                    SpawnPackModel(fantasyGrassPrefab, tuft.transform, new Vector3(1.8f, 0.7f, 1.8f), "fantasy grass");
                }

                if (!routeCells.Contains(cell) && random.NextDouble() < 0.18)
                {
                    GameObject shrub = new GameObject("courtyard shrub");
                    shrub.transform.SetParent(transform, false);
                    shrub.transform.localPosition = at + at.normalized * 0.02f;
                    shrub.transform.localRotation = Quaternion.FromToRotation(Vector3.up, at.normalized);
                    GameObject prefab = PickTree(row + col + 7);
                    if (prefab != null)
                        SpawnPackModel(prefab, shrub.transform, new Vector3(2.4f, 2.8f, 2.4f), "courtyard small tree");
                }
            }
        }
    }

    private void AddCourtyardRocks()
    {
        for (int i = 0; i < Mathf.Max(8, mazeCells * 2); i++)
        {
            int row = random.Next(mazeCells);
            int col = random.Next(mazeCells);
            if (row == 0 && col == 0) continue;
            Vector3 at = CellCenter(row, col, 0.02f);
            GameObject rock = new GameObject("japanese rock cluster");
            rock.transform.SetParent(transform, false);
            rock.transform.localPosition = at;
            rock.transform.localRotation = Quaternion.FromToRotation(Vector3.up, at.normalized)
                * Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            GameObject a = CreatePart(rock.transform, PrimitiveType.Sphere, "rock a",
                new Vector3(-0.35f, 0.16f, -0.18f), new Vector3(0.42f, 0.3f, 0.36f), stoneMaterial);
            GameObject b = CreatePart(rock.transform, PrimitiveType.Sphere, "rock b",
                new Vector3(0.18f, 0.14f, 0.22f), new Vector3(0.35f, 0.26f, 0.31f),
                japaneseRockMaterial != null ? japaneseRockMaterial : stoneMaterial);
            DisableCollider(a);
            DisableCollider(b);
        }
    }

    private void CreateMemories(List<Vector2Int> route)
    {
        int step = Mathf.Max(1, (route.Count - 1) / 4);
        string[] names = { "Moon", "Petal", "Star" };
        Material[] orbColors = { purpleMaterial, pinkMaterial, lightMaterial };
        Material[] baseColors = { pedestalMoonMaterial, pedestalPetalMaterial, pedestalStarMaterial };

        for (int i = 0; i < names.Length; i++)
        {
            Vector2Int cell = route[step * (i + 1)];
            Vector3 at = CellCenter(cell.y, cell.x, 0.1f);
            GameObject baseObject = new GameObject("memory " + (i + 1) + " - " + names[i]);
            baseObject.transform.SetParent(transform, false);
            baseObject.transform.position = transform.TransformPoint(at);
            baseObject.transform.localRotation = Quaternion.FromToRotation(Vector3.up, at.normalized);

            DreamLantern memory = baseObject.AddComponent<DreamLantern>();
            baseObject.AddComponent<DreamAnchor>();
            memory.orderIndex = i;
            memory.memoryName = names[i];

            BuildPedestal(baseObject.transform, i, baseColors[i]);
            GameObject orb = CreatePart(baseObject.transform, PrimitiveType.Sphere, "memory orb",
                new Vector3(0, 2.18f, 0), Vector3.one * 1.1f, orbColors[i]);
            memory.orbRenderer = orb.GetComponent<Renderer>();
            memory.memoryLight = orb.AddComponent<Light>();
            memory.memoryLight.type = LightType.Point;
            memory.memoryLight.range = 5f;
            memory.memoryLight.shadows = LightShadows.None;
            DisableCollider(orb);

            GameObject sign = new GameObject("memory label");
            sign.transform.SetParent(baseObject.transform, false);
            sign.transform.localPosition = new Vector3(0f, 3.05f, 0f);
            TextMesh text = sign.AddComponent<TextMesh>();
            text.text = (i + 1) + "  " + names[i].ToUpperInvariant();
            text.fontSize = 45;
            text.characterSize = 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(0.97f, 0.93f, 1f);
            sign.AddComponent<DreamWorldLabel>();
            lanterns.Add(memory);
        }
    }

    private void BuildPedestal(Transform parent, int styleIndex, Material material)
    {
        GameObject baseRing = CreatePart(parent, PrimitiveType.Cylinder, "pedestal base",
            new Vector3(0f, 0.2f, 0f), new Vector3(1.2f, 0.2f, 1.2f), stoneMaterial);
        GameObject middle = CreatePart(parent, PrimitiveType.Cylinder, "pedestal middle",
            new Vector3(0f, 0.82f, 0f), new Vector3(0.82f, 0.38f, 0.82f), material);
        GameObject top = CreatePart(parent, PrimitiveType.Cylinder, "pedestal top",
            new Vector3(0f, 1.46f, 0f), new Vector3(0.58f, 0.24f, 0.58f), stoneMaterial);
        DisableCollider(baseRing);
        DisableCollider(middle);
        DisableCollider(top);

        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f + styleIndex * 0.18f;
            Vector3 p = new Vector3(Mathf.Cos(angle) * 0.78f, 1.18f, Mathf.Sin(angle) * 0.78f);
            GameObject column = CreatePart(parent, PrimitiveType.Cube, "pedestal column", p,
                new Vector3(0.16f, 0.88f, 0.16f), stoneMaterial);
            DisableCollider(column);
        }

        if (styleIndex == 0)
        {
            GameObject crescent = CreatePart(parent, PrimitiveType.Sphere, "moon accent",
                new Vector3(0.28f, 2.15f, 0.05f), new Vector3(0.52f, 0.52f, 0.22f), material);
            GameObject cut = CreatePart(parent, PrimitiveType.Sphere, "moon cut",
                new Vector3(0.40f, 2.15f, 0.11f), new Vector3(0.42f, 0.42f, 0.20f), stoneMaterial);
            DisableCollider(crescent);
            DisableCollider(cut);
        }
        else if (styleIndex == 1)
        {
            for (int p = 0; p < 5; p++)
            {
                float angle = p * Mathf.PI * 2f / 5f;
                Vector3 local = new Vector3(Mathf.Cos(angle) * 0.24f, 2.15f, Mathf.Sin(angle) * 0.24f);
                GameObject petal = CreatePart(parent, PrimitiveType.Sphere, "petal accent",
                    local, new Vector3(0.26f, 0.18f, 0.34f), material);
                DisableCollider(petal);
            }
        }
        else
        {
            CreateStarMesh(parent, new Vector3(0f, 2.18f, 0f), 0.42f, pedestalStarMaterial);
        }
    }

    private Vector3 CreateExit(Vector2Int cell)
    {
        Vector3 at = CellCenter(cell.y, cell.x, 0.2f);
        GameObject arch = new GameObject("castle portal - locked until 3 memories");
        arch.transform.SetParent(transform, false);
        arch.transform.position = transform.TransformPoint(at);
        arch.transform.localRotation = Quaternion.FromToRotation(Vector3.up, at.normalized);
        arch.AddComponent<DreamExitMarker>();

        CreatePart(arch.transform, PrimitiveType.Cube, "left ruin pillar",
            new Vector3(-1.05f, 1.45f, 0f), new Vector3(0.55f, 2.9f, 0.65f), stoneMaterial);
        CreatePart(arch.transform, PrimitiveType.Cube, "right ruin pillar",
            new Vector3(1.05f, 1.45f, 0f), new Vector3(0.55f, 2.9f, 0.65f), stoneMaterial);
        CreatePart(arch.transform, PrimitiveType.Cube, "archway",
            new Vector3(0f, 2.95f, 0f), new Vector3(2.8f, 0.55f, 0.72f), purpleMaterial);
        GameObject glow = CreatePart(arch.transform, PrimitiveType.Sphere, "portal glow",
            new Vector3(0f, 2f, 0.15f), new Vector3(0.45f, 1.1f, 0.25f), pinkMaterial);
        DisableCollider(glow);
        if (castleDoorPrefab != null)
            SpawnPackModel(castleDoorPrefab, arch.transform, new Vector3(2.45f, 3.55f, 0.5f), "castle doorway mesh");
        DreamExitMarker marker = arch.GetComponent<DreamExitMarker>();
        marker.puzzle = puzzle;
        marker.beaconRenderer = glow.GetComponent<Renderer>();
        return arch.transform.position;
    }

    private void AddFloatingStars()
    {
        for (int i = 0; i < 48; i++)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float distance = 4f + (float)random.NextDouble() * (mazeCells * cellSize * 0.58f);
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;
            float height = 9f + (float)random.NextDouble() * 14f;
            Vector3 point = SurfacePoint(x, z, height);
            GameObject sparkle = Primitive(PrimitiveType.Sphere, "floating dream star " + i,
                point, Vector3.one * (0.12f + (float)random.NextDouble() * 0.18f), lightMaterial);
            DisableCollider(sparkle);
        }
    }

    private void AddOuterForest()
    {
        float minDistance = mazeCells * cellSize * 0.5f + 6f;
        float maxDistance = minDistance + 13f;
        for (int i = 0; i < perimeterTreeCount; i++)
        {
            float angle = i * Mathf.PI * 2f / Mathf.Max(1, perimeterTreeCount) + (float)random.NextDouble() * 0.22f;
            float squareEdge = minDistance / Mathf.Max(Mathf.Abs(Mathf.Cos(angle)), Mathf.Abs(Mathf.Sin(angle)));
            float distance = squareEdge + (float)random.NextDouble() * 10f;
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;
            if (!ReserveTree(x, z, 4.5f)) continue;
            Vector3 point = SurfacePoint(x, z, 0.1f);
            GameObject tree = new GameObject("forest perimeter tree " + i);
            tree.transform.SetParent(transform, false);
            tree.transform.localPosition = point;
            tree.transform.localRotation = Quaternion.FromToRotation(Vector3.up, point.normalized);
            GameObject prefab = PickTree(i);
            if (prefab != null)
                SpawnPackModel(prefab, tree.transform, new Vector3(8f, 8.2f, 8f), "forest tree mesh");
        }

        for (int i = 0; i < groundDetailCount; i++)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float distance = minDistance / Mathf.Max(Mathf.Abs(Mathf.Cos(angle)), Mathf.Abs(Mathf.Sin(angle)))
                + (float)random.NextDouble() * 12f;
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;
            Vector3 point = SurfacePoint(x, z, 0.02f);
            GameObject patch = new GameObject("ground detail " + i);
            patch.transform.SetParent(transform, false);
            patch.transform.localPosition = point;
            patch.transform.localRotation = Quaternion.FromToRotation(Vector3.up, point.normalized)
                * Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            if (fantasyGrassPrefab != null)
                SpawnPackModel(fantasyGrassPrefab, patch.transform, new Vector3(2.4f, 0.9f, 2.4f), "forest detail grass");
            if (random.NextDouble() < 0.35)
            {
                GameObject rock = CreatePart(patch.transform, PrimitiveType.Sphere, "detail rock",
                    new Vector3(0.35f, 0.14f, 0.15f), new Vector3(0.52f, 0.28f, 0.48f),
                    japaneseRockMaterial != null ? japaneseRockMaterial : stoneMaterial);
                DisableCollider(rock);
            }
        }
    }

    private void AddSakuras()
    {
        float edge = mazeCells * cellSize * 0.5f + 2.6f;
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f + (float)random.NextDouble() * 0.1f;
            float distance = (edge + 2.5f) / Mathf.Max(Mathf.Abs(Mathf.Cos(angle)), Mathf.Abs(Mathf.Sin(angle)));
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;
            if (!ReserveTree(x, z, 4.5f)) continue;
            Vector3 point = SurfacePoint(x, z, 0.1f);
            GameObject tree = new GameObject("castle sakura " + i);
            tree.transform.SetParent(transform, false);
            tree.transform.localPosition = point;
            tree.transform.localRotation = Quaternion.FromToRotation(Vector3.up, point.normalized);
            GameObject prefab = sakuraPrefabs != null && sakuraPrefabs.Length > 0
                ? sakuraPrefabs[i % sakuraPrefabs.Length]
                : PickTree(i);
            if (prefab != null)
                SpawnPackModel(prefab, tree.transform, new Vector3(7.2f, 6.8f, 7.2f), "sakura tree mesh");
        }
    }

    private GameObject PickTree(int index)
    {
        if (index % 5 == 0 && fantasyTreePrefab != null) return fantasyTreePrefab;
        if (index % 4 == 0 && maplePrefabs != null && maplePrefabs.Length > 0)
            return maplePrefabs[index % maplePrefabs.Length];
        if (sakuraPrefabs != null && sakuraPrefabs.Length > 0)
            return sakuraPrefabs[index % sakuraPrefabs.Length];
        return fantasyTreePrefab;
    }

    private void AddRuinsAndHouses()
    {
        float outer = mazeCells * cellSize * 0.5f + 13f;
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 9f;
            Vector3 point = SurfacePoint(x, -outer, 0.15f);
            GameObject house = new GameObject("forgotten suburban house " + i);
            house.transform.SetParent(transform, false);
            house.transform.localPosition = point;
            house.transform.localRotation = Quaternion.FromToRotation(Vector3.up, point.normalized);
            CreatePart(house.transform, PrimitiveType.Cube, "old house body", new Vector3(0, 1.5f, 0),
                new Vector3(3.4f, 3f, 2.6f), stoneMaterial);
            CreatePart(house.transform, PrimitiveType.Cube, "dream roof", new Vector3(0, 3.35f, 0),
                new Vector3(3.9f, 0.55f, 3.2f), purpleMaterial);
            if (fantasyHousePrefab != null)
            {
                foreach (Renderer block in house.GetComponentsInChildren<Renderer>()) block.enabled = false;
                SpawnPackModel(fantasyHousePrefab, house.transform, new Vector3(5f, 5.5f, 4.5f), "forgotten house mesh");
            }
            if (i % 2 == 0)
            {
                GameObject tree = new GameObject("house garden tree");
                tree.transform.SetParent(house.transform, false);
                tree.transform.localPosition = new Vector3(2.2f, 0f, -1.6f);
                GameObject prefab = PickTree(i + 30);
                if (prefab != null) SpawnPackModel(prefab, tree.transform, new Vector3(4f, 4.2f, 4f), "yard tree");
            }
        }
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 12f;
            Vector3 point = SurfacePoint(x, outer + 1.5f, 0.2f);
            GameObject ruin = new GameObject("castle ruin " + i);
            ruin.transform.SetParent(transform, false);
            ruin.transform.localPosition = point;
            ruin.transform.localRotation = Quaternion.FromToRotation(Vector3.up, point.normalized);
            if (castleTowerPrefab != null)
                SpawnPackModel(castleTowerPrefab, ruin.transform, new Vector3(5.5f, 10f, 5.5f), "broken castle tower");
            else
                CreatePart(ruin.transform, PrimitiveType.Cylinder, "tower fragment",
                    new Vector3(0, 3f, 0), new Vector3(2f, 3f, 2f), stoneMaterial);
        }
    }

    private void SpawnPackModel(GameObject prefab, Transform parent, Vector3 desiredSize, string label)
    {
        if (prefab == null) return;
        GameObject art;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            art = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
        else
            art = Instantiate(prefab, parent);
#else
        art = Instantiate(prefab, parent);
#endif
        if (art == null) return;
        art.name = label;
        art.transform.localPosition = Vector3.zero;
        art.transform.localRotation = Quaternion.identity;
        art.transform.localScale = Vector3.one;
        foreach (Collider collider in art.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        // pack wind controllers target their original shaders; cloned garden materials are static
        foreach (MonoBehaviour behaviour in art.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (BillboardRenderer billboard in art.GetComponentsInChildren<BillboardRenderer>(true)) billboard.enabled = false;
        foreach (LODGroup group in art.GetComponentsInChildren<LODGroup>(true))
        {
            var levels = new List<LOD>();
            foreach (LOD lod in group.GetLODs())
            {
                var renderers = new List<Renderer>();
                foreach (Renderer renderer in lod.renderers)
                    if (renderer is MeshRenderer) renderers.Add(renderer);
                if (renderers.Count > 0) levels.Add(new LOD(lod.screenRelativeTransitionHeight, renderers.ToArray()));
            }
            if (levels.Count > 0)
            {
                LOD last = levels[levels.Count - 1]; last.screenRelativeTransitionHeight = .01f;
                levels[levels.Count - 1] = last; group.SetLODs(levels.ToArray());
            }
        }

        Bounds bounds = GetModelBounds(art, parent);
        if (bounds.size.y < 0.01f) return;
        Vector3 scale = new Vector3(
            desiredSize.x / Mathf.Max(0.05f, bounds.size.x),
            desiredSize.y / Mathf.Max(0.05f, bounds.size.y),
            desiredSize.z / Mathf.Max(0.05f, bounds.size.z));
        if (label.Contains("tree") || label.Contains("grass"))
            scale = Vector3.one * (desiredSize.y / Mathf.Max(0.05f, bounds.size.y));
        art.transform.localScale = scale;
        art.transform.localPosition = new Vector3(
            -bounds.center.x * scale.x,
            -bounds.min.y * scale.y,
            -bounds.center.z * scale.z);
        AdaptMaterialRenderers(art, label.Contains("tree") || label.Contains("grass"));
    }

    private Bounds GetModelBounds(GameObject model, Transform origin)
    {
        Bounds total = new Bounds(Vector3.zero, Vector3.zero);
        bool hasAny = false;
        foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
        {
            Bounds b = renderer.localBounds;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 world = renderer.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3(x, y, z)));
                        Vector3 local = origin.InverseTransformPoint(world);
                        if (!hasAny) { total = new Bounds(local, Vector3.zero); hasAny = true; }
                        else total.Encapsulate(local);
                    }
        }
        return hasAny ? total : new Bounds(Vector3.zero, Vector3.one);
    }

    private void AdaptMaterialRenderers(GameObject model, bool foliage)
    {
        Shader target = FindBestShader(foliage, true);
        if (target == null) return;
        Material fallback = null;
        foreach (MeshRenderer candidate in model.GetComponentsInChildren<MeshRenderer>(true))
            foreach (Material material in candidate.sharedMaterials)
                if (material != null) { fallback = material; break; }
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Material[] originals = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < originals.Length; i++)
            {
                Material original = originals[i];
                if (original == null) original = fallback;
                if (original == null || original.shader == null)
                {
                    originals[i] = foliage ? pinkMaterial : stoneMaterial;
                    changed = true;
                    continue;
                }

                if (!convertedMaterials.TryGetValue(original, out Material converted))
                {
                    converted = new Material(target);
                    converted.name = original.name + (foliage ? " (dream foliage urp)" : " (dream urp)");
                    converted.enableInstancing = true;
                    ownedMaterials.Add(converted);
                    convertedMaterials.Add(original, converted);
                    Color tint = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") :
                        original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white;
                    if (!foliage) tint *= new Color(.72f, .70f, .68f, 1f);
                    if (converted.HasProperty("_BaseColor")) converted.SetColor("_BaseColor", tint);
                    else converted.color = tint;
                    ApplySourceTexture(converted, original);
                    if (foliage) EnsureFoliageOptions(converted);
                    else if (converted.HasProperty("_Smoothness")) converted.SetFloat("_Smoothness", 0.2f);
                }
                originals[i] = converted;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = originals;
        }
    }

    private void EnsureFoliageOptions(Material material)
    {
        if (material == null) return;
        if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 1f);
        if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0.28f);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.08f);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.renderQueue = 2450;
    }

    private void ApplySourceTexture(Material destination, Material source)
    {
        if (destination == null || source == null) return;
        Texture texture = null;
        if (source.HasProperty("_BaseMap")) texture = source.GetTexture("_BaseMap");
        if (texture == null && source.HasProperty("_MainTex")) texture = source.GetTexture("_MainTex");
        if (texture != null)
        {
            string from = source.HasProperty("_BaseMap") && source.GetTexture("_BaseMap") != null ? "_BaseMap" : "_MainTex";
            string to = destination.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            destination.SetTexture(to, texture);
            destination.SetTextureScale(to, source.GetTextureScale(from));
            destination.SetTextureOffset(to, source.GetTextureOffset(from));
        }
    }

    private GameObject Primitive(PrimitiveType type, string label, Vector3 position, Vector3 size, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = label;
        obj.transform.SetParent(transform, false);
        obj.transform.position = transform.TransformPoint(position);
        obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, position.normalized);
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private GameObject CreatePart(Transform parent, PrimitiveType type, string label,
        Vector3 localPosition, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = label;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    private void DisableCollider(GameObject go)
    {
        if (go == null) return;
        Collider col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void CreateEntryAnchor()
    {
        Vector3 at = CellCenter(0, 0, .1f);
        GameObject anchor = new GameObject("Entrance pedestal - press Q to shift the dream");
        anchor.transform.SetParent(transform, false);
        anchor.transform.localPosition = at;
        anchor.transform.localRotation = Quaternion.FromToRotation(Vector3.up, at.normalized);
        anchor.AddComponent<DreamAnchor>();
        GameObject ring = CreatePart(anchor.transform, PrimitiveType.Cylinder, "Circle of petals",
            Vector3.zero, new Vector3(1.7f, .04f, 1.7f), pinkMaterial);
        Collider collider = ring.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }

    private void CreateStarMesh(Transform parent)
    {
        CreateStarMesh(parent, new Vector3(0f, 2.9f, 0f), 0.8f, lightMaterial);
    }

    private void CreateStarMesh(Transform parent, Vector3 localPosition, float scale, Material material)
    {
        GameObject star = new GameObject("star of the lost memory");
        star.transform.SetParent(parent, false);
        star.transform.localPosition = localPosition;
        Mesh mesh = new Mesh();
        mesh.name = "dream memory star";
        ownedMeshes.Add(mesh);
        Vector3[] vertices = new Vector3[22];
        vertices[0] = new Vector3(0, 0, -0.12f);
        vertices[11] = new Vector3(0, 0, 0.12f);
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = i % 2 == 0 ? 0.85f * scale : 0.38f * scale;
            Vector3 p = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
            vertices[1 + i] = p + Vector3.back * 0.12f;
            vertices[12 + i] = p + Vector3.forward * 0.12f;
        }
        List<int> tris = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            int n = (i + 1) % 10;
            tris.Add(0); tris.Add(1 + n); tris.Add(1 + i);
            tris.Add(11); tris.Add(12 + i); tris.Add(12 + n);
            tris.Add(1 + i); tris.Add(1 + n); tris.Add(12 + n);
            tris.Add(1 + i); tris.Add(12 + n); tris.Add(12 + i);
        }
        mesh.vertices = vertices;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        star.AddComponent<MeshFilter>().sharedMesh = mesh;
        star.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

#if UNITY_EDITOR
    public bool BindVisualAssets()
    {
        terrainShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/PlanetDesign/DreamGarden/Shaders/DreamGardenTriplanar.shader");
        moteShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/PlanetDesign/DreamGarden/Shaders/DreamGardenMote.shader");
        fantasyHousePrefab = FindPackAsset(fantasyHousePrefab, "", "house_001.prefab");
        fantasyMushroomPrefab = FindPackAsset(fantasyMushroomPrefab, "", "fabulous_mushroom_001.prefab");
        castleWallPrefab = FindPackAsset(castleWallPrefab, "ba554c772410f934284d8ded98ae9329", "straight-with-borders.obj");
        castlePassagePrefab = FindPackAsset(castlePassagePrefab, "5b8044e6f6d48d34d9b22b9c5eb6e799", "passage-with-borders.obj");
        castleTowerPrefab = FindPackAsset(castleTowerPrefab, "68cbf13e667e309489732c1a030b28a9", "square-tower.obj");
        castleDoorPrefab = FindPackAsset(castleDoorPrefab, "f967dbbbe60cc104abe6f03b295bfcd4", "rounded-door.obj");
        castleMaterial = FindPackAsset(castleMaterial, "b87c74479d767334aab3bfc099120105", "atlas-1.mat");
        fantasyTreePrefab = FindPackAsset(fantasyTreePrefab, "eded051879df4c147b9010199d240258", "tree_1.prefab");
        fantasyGrassPrefab = FindPackAsset(fantasyGrassPrefab, "b48086d9f175aa34b947a21c8056c3d2", "grass01.prefab");
        forestGrassMaterial = FindPackAsset(forestGrassMaterial, "40e69315dc8bd0b45be8bf7143248fba", "grass01.mat");
        forestDirtMaterial = FindPackAsset(forestDirtMaterial, "02f626fd2f11216458890862c5864d34", "dirt01.mat");
        japaneseGrassMaterial = FindPackAsset(japaneseGrassMaterial, "9f14e308898371238a84a2c559d3a217", "brocco_jg_grass_01.mat");
        japaneseRockMaterial = FindPackAsset(japaneseRockMaterial, "1f420555c987bebdb81915cd43a47494", "brocco_jg_rock.mat");
        string[] sakuraGuids = {
            "1d346702b2107531e939003fb256261b", "488c1b23b5470bb4986ad7d362420766",
            "67d3abe897c93a08e9593b0a183f2bb4", "261441bc3708ec7aa8a2c288c3fab3ef",
            "f6208658c4d45bf5c944dd7806489932", "9d642dd3a2ff904da90e9417ae5cddec" };
        string[] mapleGuids = {
            "6e92187b7f5500614b9ca0ab3b1812a0", "9aaafd7584909db6aae1b9e564dc5ef3",
            "eba8e66cbe98e874f97ecf77638129b2", "5c8f8dde9e3fb58dda8f56008939d4fd" };
        sakuraPrefabs = BindTreeArray(sakuraPrefabs, sakuraGuids, "SakuraTree_");
        maplePrefabs = BindTreeArray(maplePrefabs, mapleGuids, "JapaneseMapleTree_");
        return castleWallPrefab != null && sakuraPrefabs.Length > 0 && fantasyTreePrefab != null;
    }

    private GameObject[] BindTreeArray(GameObject[] existing, string[] guids, string prefix)
    {
        List<GameObject> found = new List<GameObject>();
        for (int i = 0; i < guids.Length; i++)
        {
            GameObject old = existing != null && i < existing.Length ? existing[i] : null;
            GameObject item = FindPackAsset(old, guids[i], prefix + (char)('A' + i) + ".prefab");
            if (item != null) found.Add(item);
        }
        return found.ToArray();
    }

    private T FindPackAsset<T>(T current, string guid, string filename) where T : UnityEngine.Object
    {
        if (current != null) return current;
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        T item = string.IsNullOrEmpty(path) ? null : UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        if (item != null) return item;
        foreach (string foundGuid in UnityEditor.AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(filename)))
        {
            string foundPath = UnityEditor.AssetDatabase.GUIDToAssetPath(foundGuid);
            if (foundPath.EndsWith("/" + filename, System.StringComparison.OrdinalIgnoreCase))
            {
                item = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(foundPath);
                if (item != null) return item;
            }
        }
        return null;
    }
#endif
}

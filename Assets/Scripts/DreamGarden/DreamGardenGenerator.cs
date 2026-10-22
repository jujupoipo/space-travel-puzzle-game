using System;
using System.Collections.Generic;
using UnityEngine;

public class DreamGardenGenerator : MonoBehaviour
{
    [Header("planet")]
    [Min(32f)] public float planetRadius = 45f;
    public int seed = 56;
    [Range(5, 9)] public int mazeCells = 7;
    [Range(2.8f, 5f)] public float cellSize = 3.45f;
    public DreamPuzzleController puzzle;

    private const int north = 0;
    private const int east = 1;
    private const int south = 2;
    private const int west = 3;
    private static readonly int[] dr = { 1, 0, -1, 0 };
    private static readonly int[] dc = { 0, 1, 0, -1 };

    private bool[,,] openings;
    private readonly List<DreamLantern> lanterns = new List<DreamLantern>();
    private readonly List<DreamPhaseGate> phaseGates = new List<DreamPhaseGate>();
    private System.Random random;
    private Material groundMaterial;
    private Material hedgeMaterial;
    private Material pinkMaterial;
    private Material purpleMaterial;
    private Material stoneMaterial;
    private Material barkMaterial;
    private Material lightMaterial;

    private void Start()
    {
        // the maze is made before the puzzle is initialized
        random = new System.Random(seed);
        CreateMaterials();
        CreatePlanet();
        List<Vector2Int> solution = GenerateMaze();
        BuildMaze(solution);
        AddSakuras();
        AddRuinsAndHouses();
        CreateMemories(solution);
        Vector3 exit = CreateExit(solution[solution.Count - 1]);
        if (puzzle != null) puzzle.Initialize(lanterns, phaseGates, exit);
    }

    public Vector3 SurfacePoint(float x, float z, float height = 0f)
    {
        // project a flat garden coordinate onto the spherical ground
        float r = planetRadius;
        float y = Mathf.Sqrt(Mathf.Max(0.01f, r * r - x * x - z * z));
        Vector3 surface = new Vector3(x, y, z);
        return surface + surface.normalized * height;
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
        groundMaterial = MakeMaterial("dream lavender ground", new Color(0.40f, 0.40f, 0.58f));
        hedgeMaterial = MakeMaterial("seafoam hedges", new Color(0.24f, 0.45f, 0.41f));
        pinkMaterial = MakeMaterial("sakura pink", new Color(0.92f, 0.62f, 0.78f));
        purpleMaterial = MakeMaterial("ruin violet", new Color(0.51f, 0.42f, 0.68f));
        stoneMaterial = MakeMaterial("old stone", new Color(0.59f, 0.60f, 0.69f));
        barkMaterial = MakeMaterial("sakura wood", new Color(0.37f, 0.25f, 0.36f));
        lightMaterial = MakeMaterial("memory light", new Color(0.8f, 0.7f, 1f));
    }

    private Material MakeMaterial(string label, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        Material result = new Material(shader);
        result.name = label;
        if (result.HasProperty("_BaseColor")) result.SetColor("_BaseColor", color);
        else result.color = color;
        return result;
    }

    private void CreatePlanet()
    {
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = "planet 2 - the forgotten dream garden";
        planet.transform.SetParent(transform);
        planet.transform.localPosition = Vector3.zero;
        planet.transform.localScale = Vector3.one * (planetRadius * 2f);
        planet.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        GravitySource gravity = planet.AddComponent<GravitySource>();
        gravity.rule = GravitySource.Rule.Normal;
        gravity.strength = 18f;
        gravity.priority = 1;
        gravity.influenceRadius = planetRadius * 2.5f;
        if (puzzle != null) puzzle.planetGravity = gravity;
    }

    private List<Vector2Int> GenerateMaze()
    {
        // randomized depth-first maze with a guaranteed route to the final cell
        List<Vector2Int> route = null;
        for (int attempt = 0; attempt < 80; attempt++)
        {
            openings = new bool[mazeCells, mazeCells, 4];
            bool[,] seen = new bool[mazeCells, mazeCells];
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(0, 0));
            seen[0, 0] = true;
            while (stack.Count > 0)
            {
                Vector2Int current = stack.Peek();
                List<int> available = new List<int>();
                for (int direction = 0; direction < 4; direction++)
                {
                    int r = current.y + dr[direction];
                    int c = current.x + dc[direction];
                    if (r >= 0 && r < mazeCells && c >= 0 && c < mazeCells && !seen[r, c])
                        available.Add(direction);
                }
                if (available.Count == 0) { stack.Pop(); continue; }
                int chosen = available[random.Next(available.Count)];
                int row = current.y + dr[chosen];
                int col = current.x + dc[chosen];
                openings[current.y, current.x, chosen] = true;
                openings[row, col, (chosen + 2) % 4] = true;
                seen[row, col] = true;
                stack.Push(new Vector2Int(col, row));
            }
            route = FindRoute();
            if (route.Count >= 13) break;
        }
        return route;
    }

    private List<Vector2Int> FindRoute()
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[mazeCells, mazeCells];
        Vector2Int[,] parent = new Vector2Int[mazeCells, mazeCells];
        queue.Enqueue(new Vector2Int(0, 0));
        visited[0, 0] = true;
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                if (!openings[cell.y, cell.x, d]) continue;
                int row = cell.y + dr[d];
                int col = cell.x + dc[d];
                if (visited[row, col]) continue;
                visited[row, col] = true;
                parent[row, col] = cell;
                queue.Enqueue(new Vector2Int(col, row));
            }
        }
        List<Vector2Int> route = new List<Vector2Int>();
        Vector2Int point = new Vector2Int(mazeCells - 1, mazeCells - 1);
        route.Add(point);
        while (point != Vector2Int.zero)
        {
            point = parent[point.y, point.x];
            route.Add(point);
        }
        route.Reverse();
        return route;
    }

    private void BuildMaze(List<Vector2Int> route)
    {
        // place each shared maze wall once
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

        // phase gates block only the route between consecutive memories
        int step = Mathf.Max(3, (route.Count - 1) / 4);
        MakePhaseGate(route[step + 1], route[step + 2], false);
        MakePhaseGate(route[step * 2 + 1], route[step * 2 + 2], true);
    }

    private void MakePhaseGate(Vector2Int a, Vector2Int b, bool opensInBloom)
    {
        int direction = GetDirection(a, b);
        GameObject root = new GameObject(opensInBloom ? "bloom phase gate" : "ruin phase gate");
        root.transform.SetParent(transform);
        DreamPhaseGate gate = root.AddComponent<DreamPhaseGate>();
        gate.openInBloom = opensInBloom;
        gate.player = puzzle != null ? puzzle.player : null;
        gate.blockingWall = PlaceWall(a.y, a.x, direction,
            opensInBloom ? pinkMaterial : purpleMaterial, "dream gate");
        phaseGates.Add(gate);
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
        GameObject wall = Primitive(PrimitiveType.Cube, label, SurfacePoint(x, z, 2.35f),
            new Vector3(alongX ? cellSize + 0.08f : 0.46f, 4.7f,
                alongX ? 0.46f : cellSize + 0.08f), material);
        return wall;
    }

    private void CreateMemories(List<Vector2Int> route)
    {
        int step = Mathf.Max(3, (route.Count - 1) / 4);
        string[] names = { "moon", "petal", "star" };
        Material[] colors = { purpleMaterial, pinkMaterial, lightMaterial };
        for (int i = 0; i < names.Length; i++)
        {
            Vector2Int cell = route[step * (i + 1)];
            Vector3 at = CellCenter(cell.y, cell.x, 0.1f);
            GameObject baseObject = new GameObject("memory " + (i + 1) + " - " + names[i]);
            baseObject.transform.SetParent(transform);
            baseObject.transform.position = at;
            baseObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, at.normalized);
            DreamLantern memory = baseObject.AddComponent<DreamLantern>();
            memory.orderIndex = i;
            memory.memoryName = names[i];
            CreatePart(baseObject.transform, PrimitiveType.Cylinder, "pedestal",
                new Vector3(0, 0.55f, 0), new Vector3(0.7f, 0.55f, 0.7f), stoneMaterial);
            GameObject orb = CreatePart(baseObject.transform, PrimitiveType.Sphere, "memory orb",
                new Vector3(0, 1.6f, 0), Vector3.one * 1.1f, colors[i]);
            memory.orbRenderer = orb.GetComponent<Renderer>();
            GameObject sign = new GameObject("memory label");
            sign.transform.SetParent(baseObject.transform);
            sign.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            TextMesh text = sign.AddComponent<TextMesh>();
            text.text = (i + 1) + "  " + names[i].ToUpperInvariant();
            text.fontSize = 45;
            text.characterSize = 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(0.97f, 0.93f, 1f);
            lanterns.Add(memory);
        }
    }

    private Vector3 CreateExit(Vector2Int cell)
    {
        Vector3 at = CellCenter(cell.y, cell.x, 0.2f);
        GameObject arch = new GameObject("castle portal - locked until 3 memories");
        arch.transform.SetParent(transform);
        arch.transform.position = at;
        arch.transform.rotation = Quaternion.FromToRotation(Vector3.up, at.normalized);
        CreatePart(arch.transform, PrimitiveType.Cube, "left ruin pillar",
            new Vector3(-1f, 1.45f, 0f), new Vector3(0.5f, 2.9f, 0.55f), stoneMaterial);
        CreatePart(arch.transform, PrimitiveType.Cube, "right ruin pillar",
            new Vector3(1f, 1.45f, 0f), new Vector3(0.5f, 2.9f, 0.55f), stoneMaterial);
        CreatePart(arch.transform, PrimitiveType.Cube, "archway",
            new Vector3(0f, 2.9f, 0f), new Vector3(2.5f, 0.5f, 0.65f), purpleMaterial);
        CreatePart(arch.transform, PrimitiveType.Sphere, "portal glow",
            new Vector3(0f, 2f, 0.15f), new Vector3(0.45f, 1.1f, 0.25f), pinkMaterial);
        return arch.transform.position;
    }

    private void AddSakuras()
    {
        // seeded cherry blossoms decorate the perimeter of the garden
        float outer = mazeCells * cellSize * 0.5f + 4f;
        for (int i = 0; i < 18; i++)
        {
            float angle = i * Mathf.PI * 2f / 18f;
            float distance = outer + (float)random.NextDouble() * 3f;
            Vector3 point = SurfacePoint(Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance, 0.1f);
            GameObject tree = new GameObject("procedural sakura " + i);
            tree.transform.SetParent(transform);
            tree.transform.position = point;
            tree.transform.rotation = Quaternion.FromToRotation(Vector3.up, point.normalized);
            float scale = 0.85f + (float)random.NextDouble() * 0.4f;
            tree.transform.localScale = Vector3.one * scale;
            CreatePart(tree.transform, PrimitiveType.Cylinder, "trunk",
                new Vector3(0, 1.5f, 0), new Vector3(0.28f, 1.5f, 0.28f), barkMaterial);
            for (int j = 0; j < 5; j++)
            {
                float a = j * Mathf.PI * 2f / 5f;
                GameObject blossom = CreatePart(tree.transform, PrimitiveType.Sphere, "sakura blossoms",
                    new Vector3(Mathf.Cos(a) * 1.1f, 3.3f + 0.35f * (j % 2), Mathf.Sin(a) * 1.1f),
                    new Vector3(2.3f, 1.6f, 2.3f), pinkMaterial);
                Collider c = blossom.GetComponent<Collider>();
                if (c != null) Destroy(c);
            }
        }
    }

    private void AddRuinsAndHouses()
    {
        float outer = mazeCells * cellSize * 0.5f + 8f;
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 7f;
            Vector3 place = SurfacePoint(x, -outer, 0.3f);
            GameObject house = new GameObject("forgotten suburban house " + i);
            house.transform.SetParent(transform);
            house.transform.position = place;
            house.transform.rotation = Quaternion.FromToRotation(Vector3.up, place.normalized);
            CreatePart(house.transform, PrimitiveType.Cube, "house body", new Vector3(0, 1.4f, 0),
                new Vector3(3.0f, 2.8f, 2.5f), stoneMaterial);
            CreatePart(house.transform, PrimitiveType.Cube, "slanted dream roof", new Vector3(0, 3.1f, 0),
                new Vector3(3.5f, 0.55f, 3f), purpleMaterial);
        }
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 8f;
            Vector3 place = SurfacePoint(x, outer + 1f, 0.3f);
            GameObject ruin = new GameObject("castle ruin " + i);
            ruin.transform.SetParent(transform);
            ruin.transform.position = place;
            ruin.transform.rotation = Quaternion.FromToRotation(Vector3.up, place.normalized);
            CreatePart(ruin.transform, PrimitiveType.Cylinder, "tower fragment", new Vector3(0, 2.5f, 0),
                new Vector3(1.5f, 2.5f, 1.5f), stoneMaterial);
            CreatePart(ruin.transform, PrimitiveType.Cube, "cracked roof", new Vector3(0, 5f, 0),
                new Vector3(2.6f, 0.5f, 2.6f), purpleMaterial);
        }
    }

    private GameObject Primitive(PrimitiveType type, string label, Vector3 position, Vector3 size, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = label;
        obj.transform.SetParent(transform);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.FromToRotation(Vector3.up, position.normalized);
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
}

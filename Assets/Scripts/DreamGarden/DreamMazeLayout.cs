using System;
using System.Collections.Generic;

// a deterministic maze with one long solution route and ordered checkpoints
// scene geometry is built by DreamGardenGenerator
public sealed class DreamMazeLayout
{
    public struct Cell
    {
        public int Row, Col;
        public Cell(int row, int col) { Row = row; Col = col; }
    }

    public int Size { get; private set; }
    public bool[,,] Openings { get; private set; }
    public List<Cell> Route { get; private set; }
    public int Step => (Route.Count - 1) / 4;

    private static readonly int[] Dr = { 1, 0, -1, 0 };
    private static readonly int[] Dc = { 0, 1, 0, -1 };

    public DreamMazeLayout(int size, int seed)
    {
        if (size < 7 || size > 13) throw new ArgumentOutOfRangeException(nameof(size));
        Size = size;
        var random = new Random(seed);
        bool[,,] bestOpenings = null;
        List<Cell> bestRoute = null;

        for (int attempt = 0; attempt < 128; attempt++)
        {
            Openings = new bool[size, size, 4];
            var visited = new bool[size, size];
            var stack = new Stack<Cell>();
            stack.Push(new Cell(0, 0));
            visited[0, 0] = true;

            while (stack.Count > 0)
            {
                Cell cell = stack.Peek();
                var choices = new List<int>();
                for (int d = 0; d < 4; d++)
                {
                    int r = cell.Row + Dr[d];
                    int c = cell.Col + Dc[d];
                    if (r >= 0 && r < size && c >= 0 && c < size && !visited[r, c])
                        choices.Add(d);
                }

                if (choices.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                // prefer moves that go away from the origin to stretch the main route
                choices.Sort((a, b) =>
                {
                    int ar = cell.Row + Dr[a];
                    int ac = cell.Col + Dc[a];
                    int br = cell.Row + Dr[b];
                    int bc = cell.Col + Dc[b];
                    int da = ar + ac;
                    int db = br + bc;
                    return db.CompareTo(da);
                });

                int chosen = choices[random.Next(Math.Min(choices.Count, 3))];
                Cell next = new Cell(cell.Row + Dr[chosen], cell.Col + Dc[chosen]);
                Openings[cell.Row, cell.Col, chosen] = true;
                Openings[next.Row, next.Col, (chosen + 2) % 4] = true;
                visited[next.Row, next.Col] = true;
                stack.Push(next);
            }

            Route = FindRoute();
            if (bestRoute == null || Route.Count > bestRoute.Count)
            {
                bestRoute = Route;
                bestOpenings = Openings;
            }

            if (Route.Count >= Math.Max(32, size * 4)) break;
        }

        Openings = bestOpenings;
        Route = bestRoute;
    }

    private List<Cell> FindRoute()
    {
        var queue = new Queue<Cell>();
        var seen = new bool[Size, Size];
        var parent = new Cell[Size, Size];
        queue.Enqueue(new Cell(0, 0));
        seen[0, 0] = true;

        while (queue.Count > 0)
        {
            Cell cell = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                if (!Openings[cell.Row, cell.Col, d]) continue;
                Cell next = new Cell(cell.Row + Dr[d], cell.Col + Dc[d]);
                if (seen[next.Row, next.Col]) continue;
                seen[next.Row, next.Col] = true;
                parent[next.Row, next.Col] = cell;
                queue.Enqueue(next);
            }
        }

        var result = new List<Cell>();
        Cell point = new Cell(Size - 1, Size - 1);
        while (point.Row != 0 || point.Col != 0)
        {
            result.Add(point);
            point = parent[point.Row, point.Col];
        }
        result.Add(point);
        result.Reverse();
        return result;
    }
}

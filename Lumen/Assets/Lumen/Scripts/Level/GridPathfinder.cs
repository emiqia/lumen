using System.Collections.Generic;
using UnityEngine;

namespace Lumen.Level
{
    /// <summary>
    /// 8-directional A* over the dungeon grid (no corner cutting).
    /// Shared by enemies and the autopilot bot. Buffers are reused between calls
    /// to avoid GC spikes when many agents repath on the same frame.
    /// </summary>
    public class GridPathfinder
    {
        readonly DungeonMap map;
        readonly float[,] gScore;
        readonly int[,] visitStamp;
        readonly Vector2Int[,] cameFrom;
        readonly bool[,] closed;
        int stamp;

        // Simple binary heap keyed on f-score.
        readonly List<(float f, Vector2Int c)> open = new List<(float, Vector2Int)>(256);

        static readonly Vector2Int[] Dirs8 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        public GridPathfinder(DungeonMap map)
        {
            this.map = map;
            gScore = new float[map.Width, map.Height];
            visitStamp = new int[map.Width, map.Height];
            cameFrom = new Vector2Int[map.Width, map.Height];
            closed = new bool[map.Width, map.Height];
        }

        /// <summary>Finds a path and writes cell waypoints (excluding start) into <paramref name="result"/>.</summary>
        public bool FindPath(Vector2Int start, Vector2Int goal, List<Vector2Int> result, int maxExpansions = 4000)
        {
            result.Clear();
            if (!map.IsFloor(start) || !map.IsFloor(goal)) return false;
            if (start == goal) return true;

            stamp++;
            open.Clear();
            Touch(start);
            gScore[start.x, start.y] = 0;
            Push(Heuristic(start, goal), start);

            int expansions = 0;
            while (open.Count > 0 && expansions++ < maxExpansions)
            {
                var current = Pop();
                if (current == goal)
                {
                    Reconstruct(start, goal, result);
                    return true;
                }
                if (closed[current.x, current.y]) continue;
                closed[current.x, current.y] = true;

                for (int i = 0; i < Dirs8.Length; i++)
                {
                    var d = Dirs8[i];
                    var n = current + d;
                    if (!map.IsFloor(n)) continue;
                    bool diagonal = d.x != 0 && d.y != 0;
                    if (diagonal && (!map.IsFloor(current.x + d.x, current.y) || !map.IsFloor(current.x, current.y + d.y)))
                        continue;

                    Touch(n);
                    if (closed[n.x, n.y]) continue;
                    float g = gScore[current.x, current.y] + (diagonal ? 1.4142f : 1f);
                    if (g < gScore[n.x, n.y])
                    {
                        gScore[n.x, n.y] = g;
                        cameFrom[n.x, n.y] = current;
                        Push(g + Heuristic(n, goal), n);
                    }
                }
            }
            return false;
        }

        void Touch(Vector2Int c)
        {
            if (visitStamp[c.x, c.y] == stamp) return;
            visitStamp[c.x, c.y] = stamp;
            gScore[c.x, c.y] = float.MaxValue;
            closed[c.x, c.y] = false;
        }

        static float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy); // octile distance
        }

        void Reconstruct(Vector2Int start, Vector2Int goal, List<Vector2Int> result)
        {
            var c = goal;
            while (c != start)
            {
                result.Add(c);
                c = cameFrom[c.x, c.y];
            }
            result.Reverse();
        }

        void Push(float f, Vector2Int c)
        {
            open.Add((f, c));
            int i = open.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (open[parent].f <= open[i].f) break;
                (open[parent], open[i]) = (open[i], open[parent]);
                i = parent;
            }
        }

        Vector2Int Pop()
        {
            var top = open[0].c;
            int last = open.Count - 1;
            open[0] = open[last];
            open.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, smallest = i;
                if (l < open.Count && open[l].f < open[smallest].f) smallest = l;
                if (r < open.Count && open[r].f < open[smallest].f) smallest = r;
                if (smallest == i) break;
                (open[smallest], open[i]) = (open[i], open[smallest]);
                i = smallest;
            }
            return top;
        }
    }
}

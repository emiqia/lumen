using System.Collections.Generic;
using UnityEngine;

namespace Lumen.Level
{
    public enum Tile : byte { Wall = 0, Floor = 1 }

    public enum EnemyKind { Stalker, Moth, Devourer }

    public struct EnemySpawn
    {
        public EnemyKind Kind;
        public Vector2Int Cell;
        public EnemySpawn(EnemyKind kind, Vector2Int cell) { Kind = kind; Cell = cell; }
    }

    /// <summary>
    /// Pure data result of dungeon generation. Contains no Unity scene objects,
    /// so it can be unit-tested and reused by the level builder, AI and the bot.
    /// </summary>
    public class DungeonMap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Tile[,] Tiles;
        public readonly List<RectInt> Rooms = new List<RectInt>();

        public Vector2Int PlayerSpawn;
        public Vector2Int Exit;
        public readonly List<Vector2Int> Braziers = new List<Vector2Int>();
        public readonly List<Vector2Int> Embers = new List<Vector2Int>();
        public readonly List<EnemySpawn> Enemies = new List<EnemySpawn>();
        public readonly List<Vector2Int> Decorations = new List<Vector2Int>();

        public DungeonMap(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new Tile[width, height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool InBounds(Vector2Int c) => InBounds(c.x, c.y);

        public bool IsFloor(int x, int y) => InBounds(x, y) && Tiles[x, y] == Tile.Floor;
        public bool IsFloor(Vector2Int c) => IsFloor(c.x, c.y);
        public bool IsWall(int x, int y) => !IsFloor(x, y);

        /// <summary>World-space center of a cell (1 world unit per tile).</summary>
        public static Vector2 CellToWorld(Vector2Int c) => new Vector2(c.x + 0.5f, c.y + 0.5f);
        public static Vector2Int WorldToCell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

        public int FloorCount()
        {
            int n = 0;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (Tiles[x, y] == Tile.Floor) n++;
            return n;
        }

        /// <summary>Breadth-first distance field (in steps) from a cell. Unreachable = -1.</summary>
        public int[,] DistanceField(Vector2Int from)
        {
            var dist = new int[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    dist[x, y] = -1;
            if (!IsFloor(from)) return dist;

            var queue = new Queue<Vector2Int>();
            dist[from.x, from.y] = 0;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                int d = dist[c.x, c.y] + 1;
                foreach (var dir in Dirs4)
                {
                    var n = c + dir;
                    if (IsFloor(n) && dist[n.x, n.y] < 0)
                    {
                        dist[n.x, n.y] = d;
                        queue.Enqueue(n);
                    }
                }
            }
            return dist;
        }

        public static readonly Vector2Int[] Dirs4 =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Whiskers.Run.Map
{
    [Serializable]
    public sealed class MapGenerationSettings
    {
        [Tooltip("Rows of normal nodes, the boss row comes on top")]
        [SerializeField, Min(2)] private int rows = 12;
        [SerializeField, Min(2)] private int columns = 7;
        [Tooltip("How many paths are walked from bottom to top. More paths = more nodes and branches")]
        [SerializeField, Min(1)] private int paths = 6;
        [Tooltip("Nodes in the bottom row. They only branch upward, two bottom nodes never lead into the same node")]
        [SerializeField, Range(1, 2)] private int startNodes = 2;

        [Header("Node types")]
        [SerializeField, Min(0f)] private float combatWeight = 0.5f;
        [SerializeField, Min(0f)] private float eliteWeight = 0.12f;
        [SerializeField, Min(0f)] private float eventWeight = 0.2f;
        [SerializeField, Min(0f)] private float shopWeight = 0.08f;
        [SerializeField, Min(0f)] private float restWeight = 0.1f;
        [Tooltip("Elites and rest sites appear from this row on (0 = bottom)")]
        [SerializeField, Min(0)] private int firstEliteOrRestRow = 4;
        [Tooltip("The row right below the boss is all rest sites")]
        [SerializeField] private bool restBeforeBoss = true;

        public int Rows => rows;
        public int Columns => columns;
        public int Paths => paths;
        public int StartNodes => startNodes;
        public int FirstEliteOrRestRow => firstEliteOrRestRow;
        public bool RestBeforeBoss => restBeforeBoss;

        public float GetWeight(MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Combat:
                    return combatWeight;
                case MapNodeType.Elite:
                    return eliteWeight;
                case MapNodeType.Event:
                    return eventWeight;
                case MapNodeType.Shop:
                    return shopWeight;
                case MapNodeType.Rest:
                    return restWeight;
                default:
                    return 0f;
            }
        }
    }

    // Slay the Spire style: paths walk up a grid one row at a time, moving at most one column sideways
    // without crossing other paths. The visited cells become nodes, every top node leads to the boss.
    public static class MapGenerator
    {
        private const int MaxSideStep = 1;
        private const int TypeRerolls = 8;
        private const int MaxStartNodes = 2;
        private const int MinStartDistance = 2;

        // Types that should not follow each other on a path
        private static readonly MapNodeType[] NoRepeatTypes = { MapNodeType.Elite, MapNodeType.Rest, MapNodeType.Shop };

        private static readonly MapNodeType[] RandomTypes =
        {
            MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Event, MapNodeType.Shop, MapNodeType.Rest
        };

        public static RunMap Generate(MapGenerationSettings settings, Random random)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            MapNode[,] grid = new MapNode[settings.Rows, settings.Columns];
            List<MapNode> nodes = new List<MapNode>();
            int nextId = 0;

            MapNode GetOrCreate(int row, int column)
            {
                if (grid[row, column] == null)
                {
                    grid[row, column] = new MapNode(nextId++, row, column);
                    nodes.Add(grid[row, column]);
                }

                return grid[row, column];
            }

            List<int> startColumns = PickStartColumns(settings, random);

            for (int path = 0; path < settings.Paths; path++)
            {
                // Paths take the start nodes in turn, so every start node is used
                MapNode current = GetOrCreate(0, startColumns[path % startColumns.Count]);

                for (int row = 1; row < settings.Rows; row++)
                {
                    int nextColumn = row == 1
                        ? PickFirstStepColumn(grid, current, settings.Columns, random)
                        : PickNextColumn(grid, row - 1, current.Column, settings.Columns, random);
                    MapNode next = GetOrCreate(row, nextColumn);
                    current.Connect(next);
                    current = next;
                }
            }

            int bossRow = settings.Rows;
            MapNode boss = new MapNode(nextId, bossRow, settings.Columns / 2) { Type = MapNodeType.Boss };

            foreach (MapNode node in nodes)
            {
                if (node.Row == settings.Rows - 1)
                {
                    node.Connect(boss);
                }
            }

            AssignTypes(nodes, settings, random);
            nodes.Add(boss);

            return new RunMap(nodes, bossRow + 1, settings.Columns, boss);
        }

        // Two start nodes at least MinStartDistance apart, so their first steps can always avoid each other
        private static List<int> PickStartColumns(MapGenerationSettings settings, Random random)
        {
            List<int> columns = new List<int> { random.Next(settings.Columns) };
            int wanted = Math.Min(settings.StartNodes, settings.Paths);

            if (wanted < MaxStartNodes)
            {
                return columns;
            }

            List<int> farEnough = new List<int>();

            for (int column = 0; column < settings.Columns; column++)
            {
                if (Math.Abs(column - columns[0]) >= MinStartDistance)
                {
                    farEnough.Add(column);
                }
            }

            if (farEnough.Count > 0)
            {
                columns.Add(farEnough[random.Next(farEnough.Count)]);
            }

            return columns;
        }

        // Row 0 -> row 1: one to many only. A row 1 node already reached from another start node is not taken.
        // Going straight is always possible: the other start is at least two columns away and can't reach this column.
        private static int PickFirstStepColumn(MapNode[,] grid, MapNode start, int columns, Random random)
        {
            const int FirstRow = 0;
            const int SecondRow = 1;

            List<int> candidates = new List<int>();

            for (int target = Math.Max(0, start.Column - MaxSideStep); target <= Math.Min(columns - 1, start.Column + MaxSideStep); target++)
            {
                MapNode above = grid[SecondRow, target];
                bool takenByOtherStart = above != null && !start.LeadsTo(above.Id) && HasParentInRow(grid, FirstRow, above, start);
                bool crosses = target != start.Column && Crosses(grid, FirstRow, start.Column, target);

                if (!takenByOtherStart && !crosses)
                {
                    candidates.Add(target);
                }
            }

            return candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : start.Column;
        }

        private static bool HasParentInRow(MapNode[,] grid, int row, MapNode child, MapNode except)
        {
            for (int column = 0; column < grid.GetLength(1); column++)
            {
                MapNode parent = grid[row, column];

                if (parent != null && parent != except && parent.LeadsTo(child.Id))
                {
                    return true;
                }
            }

            return false;
        }

        // One column left, straight or right; a step that would cross an existing edge goes straight instead
        private static int PickNextColumn(MapNode[,] grid, int row, int column, int columns, Random random)
        {
            int min = Math.Max(0, column - MaxSideStep);
            int max = Math.Min(columns - 1, column + MaxSideStep);
            int target = random.Next(min, max + 1);

            if (target != column && Crosses(grid, row, column, target))
            {
                target = column;
            }

            return target;
        }

        // Moving from (row, column) to (row + 1, target) crosses an edge (row, target) -> (row + 1, column)
        private static bool Crosses(MapNode[,] grid, int row, int column, int target)
        {
            MapNode neighbour = grid[row, target];
            MapNode above = grid[row + 1, column];
            return neighbour != null && above != null && neighbour.LeadsTo(above.Id);
        }

        // Bottom-up, so every node knows its parents' types
        private static void AssignTypes(List<MapNode> nodes, MapGenerationSettings settings, Random random)
        {
            nodes.Sort((a, b) => a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Column.CompareTo(b.Column));

            foreach (MapNode node in nodes)
            {
                if (node.Row == 0)
                {
                    node.Type = MapNodeType.Combat;
                }
                else if (settings.RestBeforeBoss && node.Row == settings.Rows - 1)
                {
                    node.Type = MapNodeType.Rest;
                }
                else
                {
                    node.Type = PickType(node, nodes, settings, random);
                }
            }
        }

        private static MapNodeType PickType(MapNode node, List<MapNode> nodes, MapGenerationSettings settings, Random random)
        {
            MapNodeType picked = MapNodeType.Combat;

            for (int attempt = 0; attempt < TypeRerolls; attempt++)
            {
                picked = RollWeighted(settings, random);

                bool tooEarly = (picked == MapNodeType.Elite || picked == MapNodeType.Rest) && node.Row < settings.FirstEliteOrRestRow;
                // Before the forced rest row a rest would come twice in a row
                bool restTwice = picked == MapNodeType.Rest && settings.RestBeforeBoss && node.Row == settings.Rows - 2;

                if (!tooEarly && !restTwice && !RepeatsParent(node, picked, nodes))
                {
                    return picked;
                }
            }

            return MapNodeType.Combat;
        }

        private static bool RepeatsParent(MapNode node, MapNodeType type, List<MapNode> nodes)
        {
            if (Array.IndexOf(NoRepeatTypes, type) < 0)
            {
                return false;
            }

            foreach (MapNode candidate in nodes)
            {
                if (candidate.Row == node.Row - 1 && candidate.LeadsTo(node.Id) && candidate.Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static MapNodeType RollWeighted(MapGenerationSettings settings, Random random)
        {
            float total = 0f;

            foreach (MapNodeType type in RandomTypes)
            {
                total += settings.GetWeight(type);
            }

            if (total <= 0f)
            {
                return MapNodeType.Combat;
            }

            double roll = random.NextDouble() * total;

            foreach (MapNodeType type in RandomTypes)
            {
                roll -= settings.GetWeight(type);

                if (roll < 0d)
                {
                    return type;
                }
            }

            return MapNodeType.Combat;
        }
    }
}

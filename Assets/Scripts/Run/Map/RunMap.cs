using System;
using System.Collections.Generic;

namespace Whiskers.Run.Map
{
    // Serialized in run config weights, add new values at the end
    public enum MapNodeType
    {
        Combat,
        Elite,
        Event,
        Shop,
        Rest,
        Boss
    }

    // One point on the map. Row 0 is the bottom, the boss is alone on the last row.
    public sealed class MapNode
    {
        private readonly List<int> nextIds = new List<int>();

        public int Id { get; }
        public int Row { get; }
        public int Column { get; }
        public MapNodeType Type { get; internal set; }

        // Nodes one row higher this node leads to
        public IReadOnlyList<int> NextIds => nextIds;

        internal MapNode(int id, int row, int column)
        {
            Id = id;
            Row = row;
            Column = column;
        }

        internal void Connect(MapNode next)
        {
            if (!nextIds.Contains(next.Id))
            {
                nextIds.Add(next.Id);
            }
        }

        public bool LeadsTo(int id)
        {
            return nextIds.Contains(id);
        }

        public override string ToString()
        {
            return $"{Type} #{Id} (row {Row}, column {Column})";
        }
    }

    // Branching map of one run (Slay the Spire style). Plain data, the progress is kept by RunManager.
    public sealed class RunMap
    {
        private readonly List<MapNode> nodes;
        private readonly Dictionary<int, MapNode> nodesById = new Dictionary<int, MapNode>();

        public IReadOnlyList<MapNode> Nodes => nodes;
        public int RowCount { get; }
        public int ColumnCount { get; }
        public MapNode Boss { get; }

        internal RunMap(List<MapNode> nodes, int rowCount, int columnCount, MapNode boss)
        {
            this.nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            RowCount = rowCount;
            ColumnCount = columnCount;
            Boss = boss;

            foreach (MapNode node in nodes)
            {
                nodesById.Add(node.Id, node);
            }
        }

        public MapNode Get(int id)
        {
            return nodesById[id];
        }

        public bool TryGet(int id, out MapNode node)
        {
            return nodesById.TryGetValue(id, out node);
        }

        public IEnumerable<MapNode> GetRow(int row)
        {
            foreach (MapNode node in nodes)
            {
                if (node.Row == row)
                {
                    yield return node;
                }
            }
        }
    }
}

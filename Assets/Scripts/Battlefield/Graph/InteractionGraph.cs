using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Battlefield
{
    // Nodes are the active battlefield objects, edges are every possible interaction between two of them.
    // Distances are measured once per edge and cached; only edges of a moved object are measured again.
    // Plain data: BattlefieldManager forwards the registry events to it.
    public sealed class InteractionGraph
    {
        private const float DistanceTolerance = 0.0001f;

        private readonly Dictionary<int, Node> nodesById = new Dictionary<int, Node>();
        private readonly Dictionary<InteractionEdgeKey, InteractionEdge> edgesByKey = new Dictionary<InteractionEdgeKey, InteractionEdge>();

        public int NodeCount => nodesById.Count;
        public int EdgeCount => edgesByKey.Count;

        // How many distances were measured since the last Clear. Lets you check that cached edges are reused.
        public int DistanceCalculationCount { get; private set; }

        public IReadOnlyCollection<InteractionEdge> Edges => edgesByKey.Values;

        public IEnumerable<BattlefieldObject> Nodes
        {
            get
            {
                foreach (Node node in nodesById.Values)
                {
                    yield return node.Object;
                }
            }
        }

        public bool ContainsNode(int id)
        {
            return nodesById.ContainsKey(id);
        }

        public bool TryGetEdge(int idA, int idB, out InteractionEdge edge)
        {
            return edgesByKey.TryGetValue(new InteractionEdgeKey(idA, idB), out edge);
        }

        // Edges connected to one object
        public IReadOnlyCollection<InteractionEdge> GetEdgesOf(int id)
        {
            return nodesById.TryGetValue(id, out Node node) ? (IReadOnlyCollection<InteractionEdge>)node.Edges.Values : Array.Empty<InteractionEdge>();
        }

        // Registers the object and creates an edge to every node already in the graph
        public void AddNode(BattlefieldObject battlefieldObject)
        {
            if (battlefieldObject == null)
            {
                throw new ArgumentNullException(nameof(battlefieldObject));
            }

            if (!battlefieldObject.CanInteract)
            {
                throw new InvalidOperationException($"{battlefieldObject} cannot interact and cannot become a graph node.");
            }

            if (nodesById.ContainsKey(battlefieldObject.Id))
            {
                throw new InvalidOperationException($"Battlefield object #{battlefieldObject.Id} is already in the interaction graph.");
            }

            Node added = new Node(battlefieldObject);

            foreach (Node other in nodesById.Values)
            {
                InteractionEdge edge = new InteractionEdge(battlefieldObject, other.Object, MeasureSqrDistance(battlefieldObject, other.Object));
                edgesByKey.Add(edge.Key, edge);
                added.Edges.Add(other.Object.Id, edge);
                other.Edges.Add(battlefieldObject.Id, edge);
            }

            nodesById.Add(battlefieldObject.Id, added);
        }

        // Removes the node and every edge connected to it. False if the object was not in the graph.
        public bool RemoveNode(int id)
        {
            if (!nodesById.TryGetValue(id, out Node removed))
            {
                return false;
            }

            // Only the other nodes' dictionaries change in this loop, not the one being enumerated
            foreach (KeyValuePair<int, InteractionEdge> connection in removed.Edges)
            {
                nodesById[connection.Key].Edges.Remove(id);
                edgesByKey.Remove(connection.Value.Key);
            }

            nodesById.Remove(id);
            return true;
        }

        // Merged objects leave the graph, the combined object takes their place with edges of its own
        public void ReplaceNodes(IReadOnlyList<BattlefieldObject> merged, BattlefieldObject combined)
        {
            if (merged == null)
            {
                throw new ArgumentNullException(nameof(merged));
            }

            foreach (BattlefieldObject part in merged)
            {
                RemoveNode(part.Id);
            }

            AddNode(combined);
        }

        // Measures the edges of this object again, every other edge keeps its cached distance
        public bool UpdatePosition(int id)
        {
            if (!nodesById.TryGetValue(id, out Node moved))
            {
                return false;
            }

            foreach (InteractionEdge edge in moved.Edges.Values)
            {
                edge.SqrDistance = MeasureSqrDistance(edge.First, edge.Second);
            }

            return true;
        }

        // The valid edge with the shortest distance. On equal distance an opposing pair wins, then the lower ids,
        // so the same board always gives the same answer.
        public bool TryGetMinimumEdge(out InteractionEdge minimum)
        {
            minimum = null;

            foreach (InteractionEdge candidate in edgesByKey.Values)
            {
                if (candidate.IsValid && IsBetter(candidate, minimum))
                {
                    minimum = candidate;
                }
            }

            return minimum != null;
        }

        // False if the pair has no edge (one of them already left the graph)
        public bool MarkResolved(int idA, int idB)
        {
            if (!TryGetEdge(idA, idB, out InteractionEdge edge))
            {
                return false;
            }

            edge.IsResolved = true;
            return true;
        }

        // Start of a new roll
        public void ResetResolved()
        {
            foreach (InteractionEdge edge in edgesByKey.Values)
            {
                edge.IsResolved = false;
            }
        }

        // Between combats
        public void Clear()
        {
            nodesById.Clear();
            edgesByKey.Clear();
            DistanceCalculationCount = 0;
        }

        // Adds a message to problems for everything that is wrong. expectedActiveObjects (optional)
        // is what the graph should contain, usually BattlefieldObjectRegistry.ActiveObjects.
        public bool Validate(List<string> problems, IReadOnlyList<BattlefieldObject> expectedActiveObjects = null)
        {
            if (problems == null)
            {
                throw new ArgumentNullException(nameof(problems));
            }

            int problemCountBefore = problems.Count;

            foreach (KeyValuePair<int, Node> entry in nodesById)
            {
                ValidateNode(entry.Key, entry.Value, problems);
            }

            foreach (KeyValuePair<InteractionEdgeKey, InteractionEdge> entry in edgesByKey)
            {
                ValidateEdge(entry.Key, entry.Value, problems);
            }

            int expectedEdgeCount = nodesById.Count * (nodesById.Count - 1) / 2;

            if (edgesByKey.Count != expectedEdgeCount)
            {
                problems.Add($"Graph has {edgesByKey.Count} edges but {nodesById.Count} nodes need {expectedEdgeCount}.");
            }

            if (expectedActiveObjects != null)
            {
                ValidateAgainstActiveObjects(expectedActiveObjects, problems);
            }

            return problems.Count == problemCountBefore;
        }

        private void ValidateNode(int id, Node node, List<string> problems)
        {
            if (node.Object.Id != id)
            {
                problems.Add($"Node stored under #{id} holds {node.Object}.");
            }

            if (!node.Object.CanInteract)
            {
                problems.Add($"Node {node.Object} is not active.");
            }

            foreach (KeyValuePair<int, InteractionEdge> connection in node.Edges)
            {
                if (!edgesByKey.TryGetValue(connection.Value.Key, out InteractionEdge registered) || registered != connection.Value)
                {
                    problems.Add($"Node #{id} lists edge {connection.Value.Key} that is not in the edge table.");
                }

                if (!connection.Value.Connects(id) || connection.Key == id || !connection.Value.Connects(connection.Key))
                {
                    problems.Add($"Node #{id} lists edge {connection.Value.Key} under the wrong neighbour #{connection.Key}.");
                }
            }
        }

        private void ValidateEdge(InteractionEdgeKey key, InteractionEdge edge, List<string> problems)
        {
            if (!key.Equals(edge.Key))
            {
                problems.Add($"Edge stored under {key} is {edge.Key}.");
            }

            bool hasFirst = nodesById.TryGetValue(edge.First.Id, out Node first);
            bool hasSecond = nodesById.TryGetValue(edge.Second.Id, out Node second);

            if (!hasFirst || !hasSecond)
            {
                problems.Add($"Edge {edge.Key} references an object that is not in the graph.");
                return;
            }

            if (first.Object != edge.First || second.Object != edge.Second)
            {
                problems.Add($"Edge {edge.Key} references a stale object.");
            }

            if (!first.Edges.TryGetValue(edge.Second.Id, out InteractionEdge fromFirst) || fromFirst != edge ||
                !second.Edges.TryGetValue(edge.First.Id, out InteractionEdge fromSecond) || fromSecond != edge)
            {
                problems.Add($"Edge {edge.Key} is missing from one of its nodes.");
            }

            float expected = Vector2.SqrMagnitude(edge.First.Position - edge.Second.Position);

            if (float.IsNaN(edge.SqrDistance) || float.IsInfinity(edge.SqrDistance) || Mathf.Abs(edge.SqrDistance - expected) > DistanceTolerance)
            {
                problems.Add($"Edge {edge.Key} stores squared distance {edge.SqrDistance} but the objects are {expected} apart.");
            }
        }

        private void ValidateAgainstActiveObjects(IReadOnlyList<BattlefieldObject> expected, List<string> problems)
        {
            HashSet<int> expectedIds = new HashSet<int>();

            foreach (BattlefieldObject active in expected)
            {
                expectedIds.Add(active.Id);

                if (!nodesById.ContainsKey(active.Id))
                {
                    problems.Add($"Active object {active} is missing from the graph.");
                }
            }

            foreach (int id in nodesById.Keys)
            {
                if (!expectedIds.Contains(id))
                {
                    problems.Add($"Graph node #{id} is not an active object.");
                }
            }
        }

        private float MeasureSqrDistance(BattlefieldObject first, BattlefieldObject second)
        {
            DistanceCalculationCount++;
            return Vector2.SqrMagnitude(first.Position - second.Position);
        }

        private static bool IsBetter(InteractionEdge candidate, InteractionEdge best)
        {
            if (best == null)
            {
                return true;
            }

            if (!Mathf.Approximately(candidate.SqrDistance, best.SqrDistance))
            {
                return candidate.SqrDistance < best.SqrDistance;
            }

            if (candidate.IsOpposing != best.IsOpposing)
            {
                return candidate.IsOpposing;
            }

            if (candidate.First.Id != best.First.Id)
            {
                return candidate.First.Id < best.First.Id;
            }

            return candidate.Second.Id < best.Second.Id;
        }

        private sealed class Node
        {
            public BattlefieldObject Object { get; }

            // Keyed by the id of the object on the other side
            public Dictionary<int, InteractionEdge> Edges { get; } = new Dictionary<int, InteractionEdge>();

            public Node(BattlefieldObject battlefieldObject)
            {
                Object = battlefieldObject;
            }
        }
    }
}

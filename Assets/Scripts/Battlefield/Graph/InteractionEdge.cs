using System;
using UnityEngine;

namespace Whiskers.Battlefield
{
    // Unordered pair of object ids, the same key for (a, b) and (b, a)
    public readonly struct InteractionEdgeKey : IEquatable<InteractionEdgeKey>
    {
        public int LowerId { get; }
        public int HigherId { get; }

        public InteractionEdgeKey(int idA, int idB)
        {
            LowerId = Math.Min(idA, idB);
            HigherId = Math.Max(idA, idB);
        }

        public bool Equals(InteractionEdgeKey other)
        {
            return LowerId == other.LowerId && HigherId == other.HigherId;
        }

        public override bool Equals(object obj)
        {
            return obj is InteractionEdgeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (LowerId * HashMultiplier) ^ HigherId;
            }
        }

        public override string ToString()
        {
            return $"{LowerId}-{HigherId}";
        }

        private const int HashMultiplier = 397;
    }

    // A possible interaction between two battlefield objects.
    // Changed only through InteractionGraph.
    public sealed class InteractionEdge
    {
        public InteractionEdgeKey Key { get; }

        // First has the lower id
        public BattlefieldObject First { get; }
        public BattlefieldObject Second { get; }

        // Cached when the edge is created and again when one of its objects moves.
        // Squared, so comparing edges never needs a square root.
        public float SqrDistance { get; internal set; }

        // Interaction already played this roll, the edge is skipped until the graph is reset
        public bool IsResolved { get; internal set; }

        public float Distance => Mathf.Sqrt(SqrDistance);
        public bool IsOpposing => First.IsOpposing(Second);
        public bool IsAllied => First.IsAlliedWith(Second);

        internal InteractionEdge(BattlefieldObject objectA, BattlefieldObject objectB, float sqrDistance)
        {
            bool aIsFirst = objectA.Id < objectB.Id;
            First = aIsFirst ? objectA : objectB;
            Second = aIsFirst ? objectB : objectA;
            Key = new InteractionEdgeKey(First.Id, Second.Id);
            SqrDistance = sqrDistance;
        }

        public bool Connects(int id)
        {
            return First.Id == id || Second.Id == id;
        }

        public BattlefieldObject GetOther(int id)
        {
            if (First.Id == id)
            {
                return Second;
            }

            if (Second.Id == id)
            {
                return First;
            }

            throw new ArgumentException($"Object #{id} is not part of edge {Key}.", nameof(id));
        }

        // Can still be picked: not resolved, and both objects can interact
        public bool IsValid => !IsResolved && First.CanInteract && Second.CanInteract;

        public override string ToString()
        {
            string suffix = IsResolved ? ", resolved" : string.Empty;
            return $"Edge {Key} ({(IsOpposing ? "opposing" : "allied")}, distance {Distance:0.###}{suffix})";
        }
    }
}

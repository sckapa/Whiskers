using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;
using Whiskers.Dice;

namespace Whiskers.Battlefield
{
    // Shared board: size and objects on it.
    // Board units, (0, 0) is bottom-left.
    public sealed class BattlefieldBoard
    {
        public Vector2 Size { get; }
        public BattlefieldObjectRegistry Objects { get; } = new BattlefieldObjectRegistry();

        // Possible interactions between the active objects. Kept up to date by BattlefieldManager.
        public InteractionGraph Graph { get; } = new InteractionGraph();

        public BattlefieldBoard(Vector2 size)
        {
            if (size.x <= 0f || size.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Board size must be positive.");
            }

            Size = size;
        }

        // 0..1 -> board units, clamped
        public Vector2 FromNormalized(Vector2 normalized)
        {
            Vector2 clamped = new Vector2(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
            return Vector2.Scale(clamped, Size);
        }

        public Vector2 ToNormalized(Vector2 boardPosition)
        {
            return new Vector2(boardPosition.x / Size.x, boardPosition.y / Size.y);
        }

        public bool Contains(Vector2 boardPosition)
        {
            return boardPosition.x >= 0f && boardPosition.y >= 0f && boardPosition.x <= Size.x && boardPosition.y <= Size.y;
        }

        public BattlefieldObject RegisterRolledDie(DiceInstance die, DiceFace rolledFace, CombatSide owner, Vector2 normalizedPosition)
        {
            return Objects.RegisterDie(die, rolledFace, owner, FromNormalized(normalizedPosition));
        }

        // Active object that contains the die
        public bool TryGetObjectForDie(int dieId, out BattlefieldObject found)
        {
            foreach (BattlefieldObject candidate in Objects.ActiveObjects)
            {
                if (Contains(candidate.SourceDiceIds, dieId))
                {
                    found = candidate;
                    return true;
                }
            }

            found = null;
            return false;
        }

        public List<BattlefieldObject> GetObjectsOwnedBy(CombatSide owner)
        {
            return FindActive(candidate => candidate.Owner == owner);
        }

        public List<BattlefieldObject> GetObjectsInArea(Rect boardArea)
        {
            return FindActive(candidate => boardArea.Contains(candidate.Position));
        }

        // Board units. Objects exactly at the radius are included.
        public List<BattlefieldObject> GetObjectsInRadius(Vector2 boardPosition, float radius)
        {
            float sqrRadius = radius * radius;
            return FindActive(candidate => Vector2.SqrMagnitude(candidate.Position - boardPosition) <= sqrRadius);
        }

        // Closest active object to a board position. exclude is skipped, so an object can look for its neighbour.
        public bool TryGetNearestObject(Vector2 boardPosition, out BattlefieldObject nearest, BattlefieldObject exclude = null)
        {
            nearest = null;
            float bestSqrDistance = float.PositiveInfinity;

            foreach (BattlefieldObject candidate in Objects.ActiveObjects)
            {
                if (candidate == exclude)
                {
                    continue;
                }

                float sqrDistance = Vector2.SqrMagnitude(candidate.Position - boardPosition);

                // Strict comparison keeps the lowest id on ties (ActiveObjects is sorted by id)
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    nearest = candidate;
                }
            }

            return nearest != null;
        }

        // Closest active object of the other side, e.g. for "stun the nearest rat die". Ties: lowest id.
        public bool TryGetNearestOpposing(BattlefieldObject source, out BattlefieldObject nearest)
        {
            nearest = null;

            if (source == null)
            {
                return false;
            }

            float bestSqrDistance = float.PositiveInfinity;

            foreach (BattlefieldObject candidate in Objects.ActiveObjects)
            {
                float sqrDistance = Vector2.SqrMagnitude(candidate.Position - source.Position);

                if (source.IsOpposing(candidate) && sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    nearest = candidate;
                }
            }

            return nearest != null;
        }

        // Between combats
        public void Clear()
        {
            Objects.Clear();
            Graph.Clear();
        }

        private List<BattlefieldObject> FindActive(Predicate<BattlefieldObject> match)
        {
            List<BattlefieldObject> result = new List<BattlefieldObject>();

            foreach (BattlefieldObject candidate in Objects.ActiveObjects)
            {
                if (match(candidate))
                {
                    result.Add(candidate);
                }
            }

            return result;
        }

        private static bool Contains(IReadOnlyList<int> ids, int id)
        {
            foreach (int candidate in ids)
            {
                if (candidate == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

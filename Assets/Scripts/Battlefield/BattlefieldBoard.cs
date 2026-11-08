using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;
using Whiskers.Dice;

namespace Whiskers.Battlefield
{
    // Shared board: size, territories and objects on it.
    // Board units, (0, 0) is bottom-left.
    public sealed class BattlefieldBoard
    {
        public Vector2 Size { get; }
        public BattlefieldTerritory Territory { get; }
        public BattlefieldObjectRegistry Objects { get; } = new BattlefieldObjectRegistry();

        public BattlefieldBoard(Vector2 size, BattlefieldTerritory territory)
        {
            if (size.x <= 0f || size.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Board size must be positive.");
            }

            Size = size;
            Territory = territory ?? throw new ArgumentNullException(nameof(territory));
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

        public CombatSide GetTerritoryOwner(Vector2 boardPosition)
        {
            return Territory.GetOwnerAt(ToNormalized(boardPosition));
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

        public List<BattlefieldObject> GetObjectsInTerritory(CombatSide territoryOwner)
        {
            return FindActive(candidate => GetTerritoryOwner(candidate.Position) == territoryOwner);
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

        // The two closest active objects. On equal distance an opposing pair wins, then the lowest ids.
        public bool TryGetClosestPair(out BattlefieldObject first, out BattlefieldObject second)
        {
            IReadOnlyList<BattlefieldObject> objects = Objects.ActiveObjects;
            first = null;
            second = null;
            float bestSqrDistance = float.PositiveInfinity;
            bool bestIsOpposing = false;

            for (int i = 0; i < objects.Count; i++)
            {
                for (int j = i + 1; j < objects.Count; j++)
                {
                    float sqrDistance = Vector2.SqrMagnitude(objects[i].Position - objects[j].Position);
                    bool isOpposing = objects[i].IsOpposing(objects[j]);
                    bool isCloser = sqrDistance < bestSqrDistance;
                    bool winsTie = Mathf.Approximately(sqrDistance, bestSqrDistance) && isOpposing && !bestIsOpposing;

                    if (isCloser || winsTie)
                    {
                        bestSqrDistance = sqrDistance;
                        bestIsOpposing = isOpposing;
                        first = objects[i];
                        second = objects[j];
                    }
                }
            }

            return first != null;
        }

        // Between combats
        public void Clear()
        {
            Objects.Clear();
            Territory.Reset();
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

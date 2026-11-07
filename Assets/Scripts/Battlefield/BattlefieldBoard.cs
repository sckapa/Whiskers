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

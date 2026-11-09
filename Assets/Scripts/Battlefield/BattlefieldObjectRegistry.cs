using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Dice;

namespace Whiskers.Battlefield
{
    // All battlefield objects of the current combat.
    // Every change is published on the EventBus.
    public sealed class BattlefieldObjectRegistry
    {
        private const int MinimumMergeCount = 2;
        private const int NonNumericalValue = 0;

        private readonly Dictionary<int, BattlefieldObject> objectsById = new Dictionary<int, BattlefieldObject>();

        // Sorted by id
        private readonly List<BattlefieldObject> activeObjects = new List<BattlefieldObject>();

        private readonly UniqueIdGenerator idGenerator = new UniqueIdGenerator();

        public IReadOnlyList<BattlefieldObject> ActiveObjects => activeObjects;

        public bool TryGet(int id, out BattlefieldObject battlefieldObject)
        {
            return objectsById.TryGetValue(id, out battlefieldObject);
        }

        public bool TryGetActive(int id, out BattlefieldObject battlefieldObject)
        {
            return TryGet(id, out battlefieldObject) && battlefieldObject.CanInteract;
        }

        // rolledFace can be CapFace for a D9
        public BattlefieldObject RegisterDie(DiceInstance die, DiceFace rolledFace, CombatSide owner, Vector2 position)
        {
            if (die == null)
            {
                throw new ArgumentNullException(nameof(die));
            }

            if (rolledFace == null)
            {
                throw new ArgumentNullException(nameof(rolledFace));
            }

            int value = rolledFace.IsNumber ? rolledFace.Value : NonNumericalValue;

            BattlefieldObject battlefieldObject = new BattlefieldObject(
                idGenerator.Next(),
                owner,
                BattlefieldObjectType.Die,
                position,
                value,
                new[] { rolledFace },
                new[] { die.Id });

            AddActive(battlefieldObject);
            EventBus.Publish(new BattlefieldObjectRegisteredEvent(battlefieldObject));
            return battlefieldObject;
        }

        public void Move(int id, Vector2 newPosition)
        {
            BattlefieldObject battlefieldObject = GetRequiredActive(id);
            Vector2 previousPosition = battlefieldObject.Position;

            if (previousPosition == newPosition)
            {
                return;
            }

            battlefieldObject.Position = newPosition;
            EventBus.Publish(new BattlefieldObjectMovedEvent(battlefieldObject, previousPosition));
        }

        public void SetValue(int id, int newValue)
        {
            BattlefieldObject battlefieldObject = GetRequiredActive(id);
            int previousValue = battlefieldObject.Value;

            if (previousValue == newValue)
            {
                return;
            }

            battlefieldObject.Value = newValue;
            EventBus.Publish(new BattlefieldObjectValueChangedEvent(battlefieldObject, previousValue));
        }

        public void MarkAwaitingInteraction(int id)
        {
            ChangeState(GetRequiredActive(id), BattlefieldObjectState.AwaitingInteraction);
        }

        public void MarkActive(int id)
        {
            ChangeState(GetRequiredActive(id), BattlefieldObjectState.Active);
        }

        // Removes a destroyed or consumed object from all further interactions
        public void Remove(int id)
        {
            BattlefieldObject battlefieldObject = GetRequiredActive(id);

            Deactivate(battlefieldObject, BattlefieldObjectState.Destroyed);
            EventBus.Publish(new BattlefieldObjectRemovedEvent(battlefieldObject));
        }

        public bool CanMerge(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count < MinimumMergeCount || new HashSet<int>(ids).Count != ids.Count)
            {
                return false;
            }

            if (!TryGetActive(ids[0], out BattlefieldObject first))
            {
                return false;
            }

            for (int i = 1; i < ids.Count; i++)
            {
                if (!TryGetActive(ids[i], out BattlefieldObject other) || !first.IsAlliedWith(other))
                {
                    return false;
                }
            }

            return true;
        }

        // New object at the center, values summed
        public BattlefieldObject Merge(IReadOnlyList<int> ids)
        {
            if (!CanMerge(ids))
            {
                throw new InvalidOperationException(
                    $"Cannot merge [{string.Join(", ", ids ?? Array.Empty<int>())}]: " +
                    $"needs at least {MinimumMergeCount} distinct active objects of the same owner.");
            }

            List<BattlefieldObject> parts = new List<BattlefieldObject>(ids.Count);
            List<DiceFace> combinedFaces = new List<DiceFace>();
            List<int> combinedSourceDiceIds = new List<int>();
            Vector2 positionSum = Vector2.zero;
            int combinedValue = 0;

            foreach (int id in ids)
            {
                BattlefieldObject part = objectsById[id];
                parts.Add(part);
                positionSum += part.Position;
                combinedValue += part.Value;
                combinedFaces.AddRange(part.Faces);
                combinedSourceDiceIds.AddRange(part.SourceDiceIds);
            }

            BattlefieldObject result = new BattlefieldObject(
                idGenerator.Next(),
                parts[0].Owner,
                BattlefieldObjectType.Combined,
                positionSum / parts.Count,
                combinedValue,
                combinedFaces,
                combinedSourceDiceIds);

            foreach (BattlefieldObject part in parts)
            {
                Deactivate(part, BattlefieldObjectState.Combined);
            }

            AddActive(result);
            EventBus.Publish(new BattlefieldObjectsMergedEvent(parts, result));
            return result;
        }

        // Called between combats
        public void Clear()
        {
            objectsById.Clear();
            activeObjects.Clear();
            idGenerator.Reset();
        }

        private BattlefieldObject GetRequiredActive(int id)
        {
            if (!TryGetActive(id, out BattlefieldObject battlefieldObject))
            {
                throw new InvalidOperationException($"Battlefield object #{id} does not exist or is no longer active.");
            }

            return battlefieldObject;
        }

        private void AddActive(BattlefieldObject battlefieldObject)
        {
            objectsById.Add(battlefieldObject.Id, battlefieldObject);
            activeObjects.Add(battlefieldObject);
        }

        // Callers publish their own dedicated event (removed or merged) instead of a generic state change
        private void Deactivate(BattlefieldObject battlefieldObject, BattlefieldObjectState finalState)
        {
            battlefieldObject.State = finalState;
            activeObjects.Remove(battlefieldObject);
        }

        private static void ChangeState(BattlefieldObject battlefieldObject, BattlefieldObjectState newState)
        {
            BattlefieldObjectState previousState = battlefieldObject.State;

            if (previousState == newState)
            {
                return;
            }

            battlefieldObject.State = newState;
            EventBus.Publish(new BattlefieldObjectStateChangedEvent(battlefieldObject, previousState));
        }
    }
}

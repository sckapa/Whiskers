using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Dice.Visuals;
using Random = UnityEngine.Random;

namespace Whiskers.Dice.Rolling
{
    // PrepareHand + Release for normal throws, Throw for extra rolls.
    public sealed class DiceThrower : MonoBehaviour
    {
        private const float TrayMiddle = 0.5f;

        [SerializeField] private DiceTray tray = null;
        [SerializeField] private List<DiePrefabEntry> diePrefabs = new List<DiePrefabEntry>();
        [SerializeField] private DiceRollSettings settings = null;

        private readonly List<RollingDie> diceOnTable = new List<RollingDie>();
        private readonly List<ThrowBatch> pendingThrows = new List<ThrowBatch>();
        private int nextThrowId;

        public IReadOnlyList<RollingDie> DiceOnTable => diceOnTable;
        public DiceRollSettings Settings => settings;

        private void OnEnable()
        {
            EventBus.Subscribe<DieSettledEvent>(OnDieSettled);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DieSettledEvent>(OnDieSettled);
        }

        public DiceHand PrepareHand(IReadOnlyList<DiceInstance> dice, CombatSide owner)
        {
            if (dice == null)
            {
                throw new ArgumentNullException(nameof(dice));
            }

            Vector2 corner = settings.GetHandCorner(owner);
            Vector3 cornerPosition = tray.FromNormalized(corner, tray.WallHeight - settings.HandDepthBelowLid);
            Vector3 alongWidth = tray.transform.right * Mathf.Sign(TrayMiddle - corner.x) * settings.HandSpacing;
            Vector3 alongDepth = tray.transform.forward * Mathf.Sign(TrayMiddle - corner.y) * settings.HandSpacing;

            List<RollingDie> held = new List<RollingDie>(dice.Count);

            for (int i = 0; i < dice.Count; i++)
            {
                int column = i % settings.HandColumns;
                int row = i / settings.HandColumns;
                Vector3 position = cornerPosition + alongWidth * column + alongDepth * row;

                RollingDie rollingDie = Spawn(dice[i], owner, position);
                rollingDie.Hold(position, Random.rotation);
                held.Add(rollingDie);
            }

            return new DiceHand(owner, held);
        }

        // direction in normalized tray space, strength 0..1
        public void Release(DiceHand hand, Vector2 direction, float strength)
        {
            if (hand.IsReleased)
            {
                return;
            }

            int throwId = nextThrowId++;
            hand.MarkReleased(throwId);

            foreach (RollingDie die in hand.Dice)
            {
                die.Release(CalculateScatteredImpulse(direction, strength), RandomTorque());
            }

            EventBus.Publish(new DiceHandReleasedEvent(hand.Owner));
            RegisterThrow(throwId, hand.Owner, new List<RollingDie>(hand.Dice));
        }

        // Extra rolls
        public IReadOnlyList<RollingDie> Throw(
            IReadOnlyList<DiceInstance> dice,
            CombatSide owner,
            Vector2 originNormalized,
            Vector2 direction,
            float strength)
        {
            if (dice == null)
            {
                throw new ArgumentNullException(nameof(dice));
            }

            Vector2 origin = ClampOrigin(originNormalized);
            List<RollingDie> thrown = new List<RollingDie>(dice.Count);

            for (int i = 0; i < dice.Count; i++)
            {
                float height = settings.SpawnHeight + settings.SpawnVerticalSpacing * i;
                Vector3 position = tray.FromNormalized(origin, height) + RandomScatter();

                RollingDie rollingDie = Spawn(dice[i], owner, position);
                rollingDie.Release(CalculateScatteredImpulse(direction, strength), RandomTorque());
                thrown.Add(rollingDie);
            }

            RegisterThrow(nextThrowId++, owner, thrown);
            return thrown;
        }

        public bool TryGetRollingDie(int dieId, out RollingDie found)
        {
            found = diceOnTable.Find(candidate => candidate.View.Die.Id == dieId);
            return found != null;
        }

        public void RemoveDie(DiceInstance die)
        {
            RollingDie rollingDie = diceOnTable.Find(candidate => candidate.View.Die == die);

            if (rollingDie == null)
            {
                return;
            }

            diceOnTable.Remove(rollingDie);
            Destroy(rollingDie.gameObject);
        }

        public void ClearTable()
        {
            foreach (RollingDie rollingDie in diceOnTable)
            {
                Destroy(rollingDie.gameObject);
            }

            diceOnTable.Clear();
            pendingThrows.Clear();
        }

        private RollingDie Spawn(DiceInstance die, CombatSide owner, Vector3 position)
        {
            DieView view = Instantiate(GetPrefab(die.Size), position, Random.rotation, transform);
            SetLayerRecursively(view.gameObject, gameObject.layer);
            view.Bind(die, owner);

            RollingDie rollingDie = view.gameObject.AddComponent<RollingDie>();
            rollingDie.Initialize(tray, settings);
            diceOnTable.Add(rollingDie);
            return rollingDie;
        }

        private DieView GetPrefab(DiceSize size)
        {
            DiePrefabEntry entry = diePrefabs.Find(candidate => candidate.Size == size);

            if (entry == null || entry.Prefab == null)
            {
                throw new InvalidOperationException($"No die prefab assigned for {size} on '{name}'.");
            }

            return entry.Prefab;
        }

        private void RegisterThrow(int throwId, CombatSide owner, List<RollingDie> dice)
        {
            if (dice.Count == 0)
            {
                EventBus.Publish(new DiceThrowSettledEvent(throwId, owner, new List<DieRollResult>()));
                return;
            }

            pendingThrows.Add(new ThrowBatch(throwId, owner, dice));
        }

        private Vector3 CalculateScatteredImpulse(Vector2 direction, float strength)
        {
            float angle = Random.Range(-settings.ThrowDirectionSpread, settings.ThrowDirectionSpread);
            float strengthOffset = Random.Range(-settings.ThrowStrengthSpread, settings.ThrowStrengthSpread);

            return CalculateImpulse(ThrowDirection.Rotate(direction, angle), strength + strengthOffset);
        }

        private Vector3 CalculateImpulse(Vector2 direction, float strength)
        {
            Vector3 local = new Vector3(direction.x * tray.FloorSize.x, 0f, direction.y * tray.FloorSize.y);
            Vector3 horizontal = tray.transform.TransformDirection(local).normalized;
            Vector3 blended = horizontal * (1f - settings.UpwardThrowShare) + tray.transform.up * settings.UpwardThrowShare;
            float magnitude = Mathf.Lerp(settings.MinThrowImpulse, settings.MaxThrowImpulse, Mathf.Clamp01(strength));
            return blended.normalized * magnitude;
        }

        private Vector3 RandomTorque()
        {
            return Random.insideUnitSphere * settings.ThrowTorque;
        }

        private Vector2 ClampOrigin(Vector2 originNormalized)
        {
            float min = settings.OriginEdgeMargin;
            float max = 1f - settings.OriginEdgeMargin;
            return new Vector2(Mathf.Clamp(originNormalized.x, min, max), Mathf.Clamp(originNormalized.y, min, max));
        }

        private Vector3 RandomScatter()
        {
            Vector2 offset = Random.insideUnitCircle * settings.SpawnScatter;
            return tray.transform.right * offset.x + tray.transform.forward * offset.y;
        }

        private void OnDieSettled(DieSettledEvent settled)
        {
            // Backwards, items get removed
            for (int i = pendingThrows.Count - 1; i >= 0; i--)
            {
                ThrowBatch batch = pendingThrows[i];

                if (!batch.AllSettled)
                {
                    continue;
                }

                pendingThrows.RemoveAt(i);
                EventBus.Publish(new DiceThrowSettledEvent(batch.Id, batch.Owner, batch.CollectResults()));
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;

            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        [Serializable]
        private sealed class DiePrefabEntry
        {
            [SerializeField] private DiceSize size = DiceSize.D6;
            [SerializeField] private DieView prefab = null;

            public DiceSize Size => size;
            public DieView Prefab => prefab;
        }

        private sealed class ThrowBatch
        {
            private readonly List<RollingDie> dice;

            public int Id { get; }
            public CombatSide Owner { get; }
            public bool AllSettled => dice.TrueForAll(die => die.IsSettled);

            public ThrowBatch(int id, CombatSide owner, List<RollingDie> dice)
            {
                Id = id;
                Owner = owner;
                this.dice = dice;
            }

            public List<DieRollResult> CollectResults()
            {
                return dice.ConvertAll(die => die.Result);
            }
        }
    }
}

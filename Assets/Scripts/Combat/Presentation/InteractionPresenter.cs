using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Combat.Interactions;
using Whiskers.Core.Events;
using Whiskers.Dice;
using Whiskers.Dice.Rolling;

namespace Whiskers.Combat.Presentation
{
    // Animates resolved interactions in the tray. Doesn't change the board.
    public sealed class InteractionPresenter : MonoBehaviour
    {
        private const float Half = 0.5f;

        [SerializeField] private DamagePopupSpawner damagePopups = null;

        [Header("Combination")]
        [SerializeField, Min(0f)] private float combineDuration = 0.4f;
        [SerializeField, Min(0f)] private float combineClusterRadius = 0.45f;
        [SerializeField] private float spinDegreesPerSecond = 90f;
        // TODO(art): merge effect, spawned in the tray where the dice meet
        [SerializeField] private GameObject combineEffectPrefab = null;
        [SerializeField, Min(0f)] private float combineEffectLifetime = 2f;

        [Header("Collision")]
        [SerializeField, Min(0f)] private float approachDuration = 0.25f;
        [SerializeField, Range(0f, 1f)] private float approachShare = 0.8f;
        [SerializeField, Min(0f)] private float returnDuration = 0.2f;

        [Header("Direct hit")]
        [SerializeField, Min(0f)] private float hopDuration = 0.35f;
        [SerializeField, Min(0f)] private float hopHeight = 0.8f;

        [Header("Removal")]
        [SerializeField, Min(0f)] private float shrinkDuration = 0.2f;
        [SerializeField, Min(0f)] private float pauseAfterInteraction = 0.15f;

        [Header("Sound")]
        // TODO(audio): merge, clash, break, hit
        [SerializeField] private AudioSource audioSource = null;
        [SerializeField] private AudioClip combineClip = null;
        // TODO(audio): quiet loop while merged dice spin
        [SerializeField] private AudioSource spinLoopSource = null;
        [SerializeField] private AudioClip collisionClip = null;
        [SerializeField] private AudioClip destroyClip = null;
        [SerializeField] private AudioClip damageClip = null;

        // Merged objects spin until their next interaction, key = object id
        private readonly Dictionary<int, SpinningGroup> spinning = new Dictionary<int, SpinningGroup>();

        private void Awake()
        {
            if (damagePopups == null)
            {
                damagePopups = GetComponent<DamagePopupSpawner>();
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void Update()
        {
            if (spinning.Count == 0)
            {
                return;
            }

            Quaternion step = Quaternion.AngleAxis(spinDegreesPerSecond * Time.deltaTime, Tray.transform.up);
            List<int> finished = null;

            foreach (KeyValuePair<int, SpinningGroup> pair in spinning)
            {
                if (!pair.Value.Rotate(step))
                {
                    finished ??= new List<int>();
                    finished.Add(pair.Key);
                }
            }

            finished?.ForEach(StopSpinning);
        }

        public IEnumerator Play(InteractionResult result)
        {
            if (DiceRollStage.Instance == null || BattlefieldManager.Instance == null || !BattlefieldManager.Instance.IsReady)
            {
                yield break;
            }

            StopSpinning(result.First);
            StopSpinning(result.Second);

            switch (result.Kind)
            {
                case InteractionKind.Combination:
                    yield return PlayCombination(result);
                    break;
                case InteractionKind.Collision:
                    yield return PlayCollision(result);
                    break;
                case InteractionKind.DirectHit:
                    yield return PlayDirectHit(result);
                    break;
            }

            // A merged object that survived keeps spinning
            KeepSpinningIfMerged(result.First);
            KeepSpinningIfMerged(result.Second);

            yield return new WaitForSeconds(pauseAfterInteraction);
        }

        // Straight to the midpoint, then spin until the next interaction
        private IEnumerator PlayCombination(InteractionResult result)
        {
            List<RollingDie> dice = GetDice(result.Combined);
            Vector3 center = BoardToTray(result.Combined.Position);
            Vector3 up = Tray.transform.up;

            List<Vector3> starts = new List<Vector3>(dice.Count);
            List<Vector3> targets = new List<Vector3>(dice.Count);

            foreach (RollingDie die in dice)
            {
                die.Pin();
                Vector3 position = die.transform.position;
                Vector3 offset = Vector3.ProjectOnPlane(position - center, up);
                Vector3 height = Vector3.Project(position - center, up);

                starts.Add(position);
                targets.Add(center + Vector3.ClampMagnitude(offset, combineClusterRadius) + height);
            }

            yield return Animate(combineDuration, progress =>
            {
                float eased = Mathf.SmoothStep(0f, 1f, progress);

                for (int i = 0; i < dice.Count; i++)
                {
                    dice[i].transform.position = Vector3.Lerp(starts[i], targets[i], eased);
                }
            });

            PlayCombineEffect(center);
            StartSpinning(result.Combined);
        }

        private void PlayCombineEffect(Vector3 center)
        {
            PlayClip(combineClip);

            if (combineEffectPrefab == null)
            {
                return;
            }

            // In the dice scene so the dice camera renders it
            GameObject effect = Instantiate(combineEffectPrefab, center, Quaternion.identity, Thrower.transform);
            SetLayerRecursively(effect, Thrower.gameObject.layer);
            Destroy(effect, combineEffectLifetime);
        }

        private void StartSpinning(BattlefieldObject merged)
        {
            List<RollingDie> dice = GetDice(merged);

            if (dice.Count == 0)
            {
                return;
            }

            spinning[merged.Id] = new SpinningGroup(BoardToTray(merged.Position), dice);
            UpdateSpinLoop();
        }

        private void KeepSpinningIfMerged(BattlefieldObject battlefieldObject)
        {
            if (battlefieldObject != null && battlefieldObject.Type == BattlefieldObjectType.Combined && battlefieldObject.CanInteract)
            {
                StartSpinning(battlefieldObject);
            }
        }

        private void StopSpinning(BattlefieldObject battlefieldObject)
        {
            if (battlefieldObject != null)
            {
                StopSpinning(battlefieldObject.Id);
            }
        }

        private void StopSpinning(int objectId)
        {
            if (spinning.Remove(objectId))
            {
                UpdateSpinLoop();
            }
        }

        private void UpdateSpinLoop()
        {
            if (spinLoopSource == null)
            {
                return;
            }

            if (spinning.Count > 0 && !spinLoopSource.isPlaying)
            {
                spinLoopSource.Play();
            }
            else if (spinning.Count == 0 && spinLoopSource.isPlaying)
            {
                spinLoopSource.Stop();
            }
        }

        // Object ids start over every combat
        private void OnCombatStarted(CombatStartedEvent started)
        {
            spinning.Clear();
            UpdateSpinLoop();
        }

        private void OnCombatEnded(CombatEndedEvent ended)
        {
            spinning.Clear();
            UpdateSpinLoop();
        }

        // TODO(art): hit effect
        private IEnumerator PlayCollision(InteractionResult result)
        {
            List<RollingDie> dice = GetDice(result.First);
            dice.AddRange(GetDice(result.Second));

            Vector3 impact = BoardToTray(result.Position);
            List<Vector3> starts = new List<Vector3>(dice.Count);

            foreach (RollingDie die in dice)
            {
                die.Pin();
                starts.Add(die.transform.position);
            }

            yield return Animate(approachDuration, progress =>
            {
                float eased = progress * progress * approachShare;

                for (int i = 0; i < dice.Count; i++)
                {
                    dice[i].transform.position = Vector3.Lerp(starts[i], OnSameHeight(impact, starts[i]), eased);
                }
            });

            PlayClip(collisionClip);
            ShowDamage(result.Damage);

            if (result.WinnerStays)
            {
                ShowRemainingValue(result.Decision.Winner);
            }

            List<Vector3> hitPositions = dice.ConvertAll(die => die.transform.position);
            HashSet<RollingDie> breaking = new HashSet<RollingDie>();

            foreach (BattlefieldObject removed in result.RemovedObjects)
            {
                breaking.UnionWith(GetDice(removed));
            }

            IEnumerator shrink = ShrinkAndRemove(result.RemovedObjects);
            IEnumerator moveBack = Animate(returnDuration, progress =>
            {
                for (int i = 0; i < dice.Count; i++)
                {
                    if (!breaking.Contains(dice[i]))
                    {
                        dice[i].transform.position = Vector3.Lerp(hitPositions[i], starts[i], Mathf.SmoothStep(0f, 1f, progress));
                    }
                }
            });

            yield return RunTogether(shrink, moveBack);
        }

        // TODO(art): attack effect
        private IEnumerator PlayDirectHit(InteractionResult result)
        {
            List<RollingDie> dice = GetDice(result.First);
            List<Vector3> starts = new List<Vector3>(dice.Count);
            Vector3 up = Tray.transform.up;

            foreach (RollingDie die in dice)
            {
                die.Pin();
                starts.Add(die.transform.position);
            }

            bool damageShown = false;

            yield return Animate(hopDuration, progress =>
            {
                float lift = Mathf.Sin(progress * Mathf.PI) * hopHeight;

                for (int i = 0; i < dice.Count; i++)
                {
                    dice[i].transform.position = starts[i] + up * lift;
                }

                if (!damageShown && progress >= Half)
                {
                    damageShown = true;
                    PlayClip(collisionClip);
                    ShowDamage(result.Damage);
                }
            });

            yield return ShrinkAndRemove(result.RemovedObjects);
        }

        private IEnumerator ShrinkAndRemove(IReadOnlyList<BattlefieldObject> removedObjects)
        {
            if (removedObjects.Count == 0)
            {
                yield break;
            }

            PlayClip(destroyClip);

            List<RollingDie> dice = new List<RollingDie>();

            foreach (BattlefieldObject removed in removedObjects)
            {
                dice.AddRange(GetDice(removed));
            }

            List<Vector3> scales = dice.ConvertAll(die => die.transform.localScale);

            yield return Animate(shrinkDuration, progress =>
            {
                for (int i = 0; i < dice.Count; i++)
                {
                    dice[i].transform.localScale = Vector3.Lerp(scales[i], Vector3.zero, progress);
                }
            });

            // Only the visual, CombatManager discards the dice
            foreach (RollingDie die in dice)
            {
                Thrower.RemoveDie(die.View.Die);
            }
        }

        // Merged dice: the number goes on the red die, others go blank
        private void ShowRemainingValue(BattlefieldObject winner)
        {
            List<RollingDie> dice = GetDice(winner);

            if (dice.Count == 0)
            {
                return;
            }

            RollingDie carrier = dice.Find(die => die.Result.Face.GlyphColor == OpposingCollision.StayOnBoardGlyph);

            if (carrier == null)
            {
                carrier = dice[0];
            }

            foreach (RollingDie die in dice)
            {
                DiceFace shown = die == carrier ? DiceFace.CreateNumber(winner.Value, die.Result.Face.GlyphColor) : null;
                die.ShowResultFace(shown);
            }
        }

        private void ShowDamage(DamageReport damage)
        {
            if (damage.IsEmpty)
            {
                return;
            }

            PlayClip(damageClip);

            if (damagePopups != null)
            {
                damagePopups.Show(damage);
            }
        }

        private static IEnumerator Animate(float duration, System.Action<float> step)
        {
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                step(time / duration);
                yield return null;
            }

            step(1f);
        }

        private IEnumerator RunTogether(IEnumerator first, IEnumerator second)
        {
            Coroutine firstRun = StartCoroutine(first);
            Coroutine secondRun = StartCoroutine(second);
            yield return firstRun;
            yield return secondRun;
        }

        private List<RollingDie> GetDice(BattlefieldObject battlefieldObject)
        {
            List<RollingDie> dice = new List<RollingDie>();

            if (battlefieldObject == null)
            {
                return dice;
            }

            foreach (int dieId in battlefieldObject.SourceDiceIds)
            {
                if (Thrower.TryGetRollingDie(dieId, out RollingDie die))
                {
                    dice.Add(die);
                }
            }

            return dice;
        }

        private static Vector3 BoardToTray(Vector2 boardPosition)
        {
            Vector2 normalized = BattlefieldManager.Instance.Board.ToNormalized(boardPosition);
            return Tray.FromNormalized(normalized, 0f);
        }

        private static Vector3 OnSameHeight(Vector3 floorPoint, Vector3 reference)
        {
            Vector3 up = Tray.transform.up;
            return floorPoint + Vector3.Project(reference - floorPoint, up);
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;

            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private static DiceTray Tray => DiceRollStage.Instance.Tray;
        private static DiceThrower Thrower => DiceRollStage.Instance.Thrower;

        // Dice of one merged object turning around its center
        private sealed class SpinningGroup
        {
            private readonly Vector3 center;
            private readonly List<RollingDie> dice;

            public SpinningGroup(Vector3 center, List<RollingDie> dice)
            {
                this.center = center;
                this.dice = dice;
            }

            // False once all dice are gone
            public bool Rotate(Quaternion step)
            {
                bool anyLeft = false;

                foreach (RollingDie die in dice)
                {
                    if (die == null)
                    {
                        continue;
                    }

                    anyLeft = true;
                    Transform dieTransform = die.transform;
                    dieTransform.SetPositionAndRotation(center + step * (dieTransform.position - center), step * dieTransform.rotation);
                }

                return anyLeft;
            }
        }
    }
}

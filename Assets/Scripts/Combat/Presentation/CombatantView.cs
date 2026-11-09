using UnityEngine;
using Whiskers.Combat.Interactions;
using Whiskers.Combatants;
using Whiskers.Combatants.Definitions;
using Whiskers.Core;
using Whiskers.Core.Events;

namespace Whiskers.Combat.Presentation
{
    // Serialized, add new values at the end
    public enum CombatantReaction
    {
        Hit,
        Heal,
        Block,
        Attack,
        Die,
        Victory
    }

    // The combatant's sprite in the combat scene. Takes sprite and animator from the definition (CombatantVisuals).
    // Reactions fire an Animator trigger with the reaction's name if the controller has it;
    // otherwise (or in addition, see playFallbackWithAnimator) a code animation plays: shake, flash, lunge, fall.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CombatantView : CombatantBoundView
    {
        private const float Half = 0.5f;
        private const float FullTurnRadians = Mathf.PI * 2f;

        [SerializeField] private Animator animator = null;
        [Tooltip("Plays the code reactions even when the animator handles the trigger")]
        [SerializeField] private bool playFallbackWithAnimator = false;

        [Header("Code reactions")]
        [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private Color healColor = new Color(0.45f, 1f, 0.5f);
        [SerializeField] private Color blockColor = new Color(0.5f, 0.75f, 1f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.25f;
        [SerializeField, Min(0f)] private float shakeStrength = 0.12f;
        [SerializeField, Min(0.01f)] private float shakeDuration = 0.25f;
        [SerializeField, Min(0f)] private float shakeFrequency = 30f;
        [Tooltip("World units toward the board")]
        [SerializeField] private Vector2 lungeOffset = new Vector2(0.4f, 0f);
        [SerializeField, Min(0.01f)] private float lungeDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float deathDuration = 0.6f;
        [SerializeField] private float deathRotation = 90f;
        [SerializeField, Min(0f)] private float victoryHopHeight = 0.3f;
        [SerializeField, Min(0.01f)] private float victoryHopDuration = 0.4f;

        [Header("Idle")]
        [SerializeField, Min(0f)] private float breatheAmount = 0.02f;
        [SerializeField, Min(0.01f)] private float breathePeriod = 2f;

        private SpriteRenderer spriteRenderer;
        private Vector3 basePosition;
        private Vector3 baseScale;
        // baseScale times the definition's scale
        private Vector3 visualScale;
        private Quaternion baseRotation;
        private Color baseColor;

        private Color flashColor;
        private float flashTime = float.PositiveInfinity;
        private float shakeTime = float.PositiveInfinity;
        private float lungeTime = float.PositiveInfinity;
        private float hopTime = float.PositiveInfinity;
        private float deathTime = float.PositiveInfinity;
        private bool isDead;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            basePosition = transform.localPosition;
            baseScale = transform.localScale;
            visualScale = baseScale;
            baseRotation = transform.localRotation;
            baseColor = spriteRenderer.color;
        }

        protected override void OnEnable()
        {
            EventBus.Subscribe<DisplayedHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<InteractionDamageEvent>(OnInteractionDamage);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Unsubscribe<DisplayedHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<InteractionDamageEvent>(OnInteractionDamage);
        }

        // Also callable from other systems, e.g. a summon or an effect with its own reaction
        public void Play(CombatantReaction reaction)
        {
            bool handledByAnimator = TrySetTrigger(reaction);

            if (handledByAnimator && !playFallbackWithAnimator)
            {
                return;
            }

            switch (reaction)
            {
                case CombatantReaction.Hit:
                    Flash(hitColor);
                    shakeTime = 0f;
                    break;
                case CombatantReaction.Heal:
                    Flash(healColor);
                    break;
                case CombatantReaction.Block:
                    Flash(blockColor);
                    break;
                case CombatantReaction.Attack:
                    lungeTime = 0f;
                    break;
                case CombatantReaction.Die:
                    deathTime = 0f;
                    break;
                case CombatantReaction.Victory:
                    hopTime = 0f;
                    break;
            }
        }

        protected override void OnBound()
        {
            ResetPose();
            ApplyVisuals(Combatant.Visuals);
        }

        private void Update()
        {
            float time = Time.time;
            Vector3 offset = Vector3.zero;
            float rotation = 0f;
            float alpha = 1f;
            Color tint = baseColor;

            float breathe = isDead ? 0f : Mathf.Sin(time / breathePeriod * FullTurnRadians) * breatheAmount;
            transform.localScale = new Vector3(visualScale.x, visualScale.y * (1f + breathe), visualScale.z);

            if (Advance(ref shakeTime, shakeDuration, out float shakeProgress))
            {
                float strength = shakeStrength * (1f - shakeProgress);
                offset.x += Mathf.Sin(shakeTime * shakeFrequency * FullTurnRadians) * strength;
            }

            if (Advance(ref lungeTime, lungeDuration, out float lungeProgress))
            {
                // Out and back
                offset += (Vector3)(GetLungeDirection() * Mathf.Sin(lungeProgress * Mathf.PI));
            }

            if (Advance(ref hopTime, victoryHopDuration, out float hopProgress))
            {
                offset.y += Mathf.Sin(hopProgress * Mathf.PI) * victoryHopHeight;
            }

            if (Advance(ref flashTime, flashDuration, out float flashProgress))
            {
                tint = Color.Lerp(flashColor, baseColor, flashProgress);
            }

            // Infinity = the animator plays the death instead of the code fall
            if (isDead && !float.IsPositiveInfinity(deathTime))
            {
                // Advance returns false once the fall is over, the progress then stays at 1 and the pose is kept
                Advance(ref deathTime, deathDuration, out float deathProgress);
                rotation = deathRotation * deathProgress * GetFacingSign();
                alpha = 1f - deathProgress * Half;
            }

            transform.localPosition = basePosition + offset;
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rotation);
            tint.a = baseColor.a * alpha;
            spriteRenderer.color = tint;
        }

        // Displayed HP, so the hit and the death play when the damage flight arrives
        private void OnHealthChanged(DisplayedHealthChangedEvent changed)
        {
            bool reachedZero = changed.CurrentHealth <= 0 && changed.Combatant.IsDead;

            if (isDead || Combatant == null)
            {
                return;
            }

            // The opponent visibly dropped to 0: we won
            if (!IsMine(changed.Combatant))
            {
                if (reachedZero)
                {
                    Play(CombatantReaction.Victory);
                }

                return;
            }

            if (reachedZero)
            {
                isDead = true;
                Play(CombatantReaction.Die);
                return;
            }

            Play(changed.Delta < 0 ? CombatantReaction.Hit : CombatantReaction.Heal);
        }

        private void OnInteractionDamage(InteractionDamageEvent damage)
        {
            DamageReport report = damage.Report;

            if (report.Source != null && report.Source.Owner == Side)
            {
                Play(CombatantReaction.Attack);
            }

            if (report.WasBlocked && IsMine(report.Target))
            {
                Play(CombatantReaction.Block);
            }
        }

        private void ApplyVisuals(CombatantVisuals visuals)
        {
            if (visuals == null)
            {
                return;
            }

            if (visuals.Sprite != null)
            {
                spriteRenderer.sprite = visuals.Sprite;
            }

            spriteRenderer.flipX = visuals.FlipX;
            visualScale = baseScale * visuals.Scale;

            if (visuals.Animator != null)
            {
                if (animator == null)
                {
                    animator = gameObject.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = visuals.Animator;
            }
        }

        private void ResetPose()
        {
            isDead = false;
            shakeTime = lungeTime = hopTime = flashTime = deathTime = float.PositiveInfinity;
            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
            spriteRenderer.color = baseColor;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Rebind();
            }
        }

        private bool TrySetTrigger(CombatantReaction reaction)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            string trigger = reaction.ToString();

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == trigger)
                {
                    animator.SetTrigger(trigger);
                    return true;
                }
            }

            return false;
        }

        private void Flash(Color color)
        {
            flashColor = color;
            flashTime = 0f;
        }

        // Player lunges toward +lungeOffset, the enemy mirrored
        private Vector2 GetLungeDirection()
        {
            return Side == CombatSide.Player ? lungeOffset : new Vector2(-lungeOffset.x, lungeOffset.y);
        }

        private float GetFacingSign()
        {
            return Side == CombatSide.Player ? 1f : -1f;
        }

        // Moves a reaction timer forward; false when the reaction is not running
        private static bool Advance(ref float time, float duration, out float progress)
        {
            if (time >= duration)
            {
                progress = 1f;
                return false;
            }

            time += Time.deltaTime;
            progress = Mathf.Clamp01(time / duration);
            return true;
        }
    }
}

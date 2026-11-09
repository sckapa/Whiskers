using UnityEngine;

namespace Whiskers.Effects.Types
{
    // Serialized, add new values at the end
    public enum AuraTargets
    {
        Allies,
        Enemies,
        All
    }

    // Serialized, add new values at the end
    public enum AuraDuration
    {
        // Ends after the leftovers have hit (phase 4)
        ThisRoll,
        // Ends when the source die leaves the board (or at the end of the combat)
        WhileSourceOnBoard,
        ThisCombat
    }

    // Domain expansion of a die: a circle around it (it follows the die and the merged object the die becomes).
    // Every other matching numeric object inside, also dice that land, move or merge inside later,
    // gets its value changed once. The die itself is never affected. Values are not restored when the aura ends.
    [CreateAssetMenu(fileName = "AuraEffect", menuName = "Whiskers/Effects/Aura")]
    public sealed class AuraEffect : EffectDefinition
    {
        [Tooltip("Board units (the tray floor is the board size)")]
        [SerializeField, Min(0f)] private float radius = 2f;
        [SerializeField] private AuraTargets targets = AuraTargets.Allies;
        [Tooltip("Added to the value of every target inside, negative = debuff. Values never drop below 0")]
        [SerializeField] private int valueChange = 1;
        [SerializeField] private AuraDuration duration = AuraDuration.ThisRoll;

        public float Radius => radius;
        public AuraTargets Targets => targets;
        public int ValueChange => valueChange;
        public AuraDuration Duration => duration;

        public override void Apply(EffectContext context)
        {
            context.Services.ActivateAura(this, context);
        }

        // Only a die on the board can open an aura (passives without a die can't)
        protected override bool IsApplicable(EffectContext context)
        {
            return context.SourceObject != null && context.SourceObject.CanInteract;
        }
    }
}

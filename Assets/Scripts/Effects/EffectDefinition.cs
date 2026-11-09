using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Effects
{
    // One effect asset: when it is checked (trigger), when it applies (conditions) and what it does (Apply).
    // The same asset can be used by a body color, a glyph color, a symbol or a combatant passive.
    public abstract class EffectDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private EffectTrigger trigger = EffectTrigger.Landing;

        // All of them must be met. Empty = always.
        [SerializeField] private List<EffectCondition> conditions = new List<EffectCondition>();

        // Applies at most once per combat for each side, e.g. an aura that should not stack every roll
        [SerializeField] private bool oncePerCombat = false;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public EffectTrigger Trigger => trigger;
        public IReadOnlyList<EffectCondition> Conditions => conditions;
        public bool OncePerCombat => oncePerCombat;

        public bool CanApply(EffectContext context)
        {
            if (context.Trigger != trigger)
            {
                return false;
            }

            foreach (EffectCondition condition in conditions)
            {
                if (condition != null && !condition.IsMet(context))
                {
                    return false;
                }
            }

            return IsApplicable(context);
        }

        public abstract void Apply(EffectContext context);

        // Checks that belong to the effect itself, e.g. a die effect without a die
        protected virtual bool IsApplicable(EffectContext context)
        {
            return true;
        }
    }

    // Reusable check, e.g. "rolled a 9" or "HP is 9 or less"
    public abstract class EffectCondition : ScriptableObject
    {
        public abstract bool IsMet(EffectContext context);
    }
}

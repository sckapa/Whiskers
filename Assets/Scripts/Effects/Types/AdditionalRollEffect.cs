using UnityEngine;

namespace Whiskers.Effects.Types
{
    // Draws more dice from the owner's bag and throws them; they go through the landing effects too
    [CreateAssetMenu(fileName = "AdditionalRollEffect", menuName = "Whiskers/Effects/Additional Roll")]
    public sealed class AdditionalRollEffect : EffectDefinition
    {
        private const int MinimumCount = 1;

        [SerializeField, Min(MinimumCount)] private int count = 1;

        public override void Apply(EffectContext context)
        {
            context.Services.RequestAdditionalRolls(context.Owner, count);
        }
    }
}

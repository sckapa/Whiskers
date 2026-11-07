using UnityEngine;

namespace Whiskers.Combatants.Definitions
{
    public enum EnemyTier
    {
        Regular,
        Elite,
        Boss
    }

    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Whiskers/Combatants/Enemy Definition")]
    public sealed class EnemyDefinition : CombatantDefinition
    {
        [SerializeField] private EnemyTier tier = EnemyTier.Regular;

        public EnemyTier Tier => tier;
    }
}

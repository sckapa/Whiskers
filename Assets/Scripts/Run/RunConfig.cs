using System.Collections.Generic;
using UnityEngine;
using Whiskers.Combatants.Definitions;
using Whiskers.Run.Map;

namespace Whiskers.Run
{
    // Everything a new run is built from: the player, the enemy pools and the map shape
    [CreateAssetMenu(fileName = "RunConfig", menuName = "Whiskers/Run Config")]
    public sealed class RunConfig : ScriptableObject
    {
        [SerializeField] private PlayerDefinition player = null;

        [Header("Enemies (picked at random for the node type)")]
        [SerializeField] private List<EnemyDefinition> regularEnemies = new List<EnemyDefinition>();
        [SerializeField] private List<EnemyDefinition> eliteEnemies = new List<EnemyDefinition>();
        [SerializeField] private List<EnemyDefinition> bosses = new List<EnemyDefinition>();

        [Header("Map")]
        [SerializeField] private MapGenerationSettings map = new MapGenerationSettings();

        [Header("Rest site")]
        [SerializeField, Range(0f, 1f)] private float restHealShare = 0.3f;

        [Tooltip("0 = new random seed every run")]
        [SerializeField] private int seed = 0;

        public PlayerDefinition Player => player;
        public MapGenerationSettings Map => map;
        public float RestHealShare => restHealShare;
        public int Seed => seed;

        // Falls back to the regular pool when the elite or boss pool is empty
        public IReadOnlyList<EnemyDefinition> GetEnemyPool(MapNodeType type)
        {
            List<EnemyDefinition> pool = type == MapNodeType.Elite ? eliteEnemies
                : type == MapNodeType.Boss ? bosses
                : regularEnemies;

            return HasAny(pool) ? pool : regularEnemies;
        }

        private static bool HasAny(List<EnemyDefinition> pool)
        {
            return pool.Exists(enemy => enemy != null);
        }
    }
}

using UnityEngine;
using Whiskers.Combatants;
using Whiskers.Core;
using Whiskers.Core.Events;

namespace Whiskers.Combat.Presentation
{
    // Base for anything that shows one side of the combat (name, HP, sprite).
    // Binds itself when a combat starts, or right away if one is already running.
    public abstract class CombatantBoundView : MonoBehaviour
    {
        [SerializeField] private CombatSide side = CombatSide.Player;

        public CombatSide Side => side;

        // Null until a combat starts
        protected Combatant Combatant { get; private set; }

        protected virtual void OnEnable()
        {
            EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);

            CombatManager combat = CombatManager.Instance;

            if (combat != null && combat.Player != null && combat.Enemy != null && combat.IsInCombat)
            {
                Bind(side == CombatSide.Player ? combat.Player : (Combatant)combat.Enemy);
            }
        }

        protected virtual void OnDisable()
        {
            EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
        }

        protected bool IsMine(Combatant combatant)
        {
            return combatant != null && combatant == Combatant;
        }

        // Called with the new combatant every time a combat starts
        protected abstract void OnBound();

        private void OnCombatStarted(CombatStartedEvent started)
        {
            Bind(side == CombatSide.Player ? started.Player : (Combatant)started.Enemy);
        }

        private void Bind(Combatant combatant)
        {
            Combatant = combatant;
            OnBound();
        }
    }
}

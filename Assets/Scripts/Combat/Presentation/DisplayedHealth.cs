using System.Collections.Generic;
using UnityEngine;
using Whiskers.Combat.Interactions;
using Whiskers.Combatants;
using Whiskers.Core.Events;

namespace Whiskers.Combat.Presentation
{
    // HP as the UI shows it. Equals the real HP, except while a damage flight is on its way:
    // the damage is already applied in the logic, but the bar, the text and the hit reaction wait until it arrives.
    // Holds are only made while something that releases them is registered (DamagePopupSpawner with flights on).
    // Safety net: everything still held is released when the next roll starts and when a combat starts.
    public static class DisplayedHealth
    {
        private static readonly Dictionary<Combatant, List<Hold>> holdsByCombatant = new Dictionary<Combatant, List<Hold>>();
        private static int delayerCount;

        public static int Get(Combatant combatant)
        {
            return Clamp(combatant, combatant.Health.Current + GetHeld(combatant));
        }

        // A damage flight presenter is active, damage from interactions will be held until it arrives
        public static void RegisterDelayer()
        {
            delayerCount++;
        }

        public static void UnregisterDelayer()
        {
            delayerCount = Mathf.Max(0, delayerCount - 1);

            if (delayerCount == 0)
            {
                ReleaseAll();
            }
        }

        // The oldest held interaction damage of this combatant reaches the HP display
        public static void ReleaseNext(Combatant combatant)
        {
            if (combatant == null || !holdsByCombatant.TryGetValue(combatant, out List<Hold> holds) || holds.Count == 0)
            {
                return;
            }

            int before = Get(combatant);
            holds.RemoveAt(0);
            PublishIfChanged(combatant, before);
        }

        public static void ReleaseAll()
        {
            // Copy, publishing can make views read the dictionary
            foreach (Combatant combatant in new List<Combatant>(holdsByCombatant.Keys))
            {
                int before = Get(combatant);
                holdsByCombatant[combatant].Clear();
                PublishIfChanged(combatant, before);
            }

            holdsByCombatant.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // EventBus is cleared on play mode enter (SubsystemRegistration), which runs before this
            holdsByCombatant.Clear();
            delayerCount = 0;
            EventBus.Subscribe<InteractionDamageIncomingEvent>(OnDamageIncoming);
            EventBus.Subscribe<InteractionDamageEvent>(OnDamageDealt);
            EventBus.Subscribe<CombatantHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Subscribe<CombatPhaseChangedEvent>(OnPhaseChanged);
        }

        // Before the damage is applied: the next HP drop of the target goes into this hold
        private static void OnDamageIncoming(InteractionDamageIncomingEvent incoming)
        {
            if (delayerCount == 0 || incoming.Target == null)
            {
                return;
            }

            if (!holdsByCombatant.TryGetValue(incoming.Target, out List<Hold> holds))
            {
                holds = new List<Hold>();
                holdsByCombatant.Add(incoming.Target, holds);
            }

            holds.Add(new Hold());
        }

        // After the damage: the hold stops collecting (a blocked attack leaves it at 0, it is still released on arrival)
        private static void OnDamageDealt(InteractionDamageEvent dealt)
        {
            Hold collecting = FindCollecting(dealt.Report.Target);

            if (collecting != null)
            {
                collecting.IsCollecting = false;
            }
        }

        private static void OnHealthChanged(CombatantHealthChangedEvent changed)
        {
            Combatant combatant = changed.Combatant;
            int before = Clamp(combatant, changed.PreviousHealth + GetHeld(combatant));
            Hold collecting = changed.Delta < 0 ? FindCollecting(combatant) : null;

            if (collecting != null)
            {
                collecting.Amount -= changed.Delta;
            }

            PublishIfChanged(combatant, before);
        }

        private static void OnCombatStarted(CombatStartedEvent started)
        {
            holdsByCombatant.Clear();
        }

        private static void OnPhaseChanged(CombatPhaseChangedEvent changed)
        {
            if (changed.Phase == CombatPhase.Rolling)
            {
                ReleaseAll();
            }
        }

        private static Hold FindCollecting(Combatant combatant)
        {
            if (combatant == null || !holdsByCombatant.TryGetValue(combatant, out List<Hold> holds))
            {
                return null;
            }

            return holds.Find(hold => hold.IsCollecting);
        }

        private static int GetHeld(Combatant combatant)
        {
            int held = 0;

            if (holdsByCombatant.TryGetValue(combatant, out List<Hold> holds))
            {
                foreach (Hold hold in holds)
                {
                    held += hold.Amount;
                }
            }

            return held;
        }

        private static int Clamp(Combatant combatant, int health)
        {
            return Mathf.Clamp(health, 0, combatant.Health.Max);
        }

        private static void PublishIfChanged(Combatant combatant, int before)
        {
            int after = Get(combatant);

            if (after != before)
            {
                EventBus.Publish(new DisplayedHealthChangedEvent(combatant, before, after));
            }
        }

        private sealed class Hold
        {
            public int Amount;
            public bool IsCollecting = true;
        }
    }

    public readonly struct DisplayedHealthChangedEvent : IGameEvent
    {
        public Combatant Combatant { get; }
        public int PreviousHealth { get; }
        public int CurrentHealth { get; }

        // Negative for damage, positive for healing
        public int Delta => CurrentHealth - PreviousHealth;

        public DisplayedHealthChangedEvent(Combatant combatant, int previousHealth, int currentHealth)
        {
            Combatant = combatant;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
        }
    }
}

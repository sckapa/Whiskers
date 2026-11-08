using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Combat.Interactions;
using Whiskers.Combat.Presentation;
using Whiskers.Core.Events;

namespace Whiskers.Combat
{
    // Temporary phase 3 until the interaction queue is done.
    public sealed class DebugInteractionRunner : MonoBehaviour
    {
        [SerializeField] private bool runAfterEachRoll = true;
        [SerializeField] private InteractionPresenter presenter = null;
        [SerializeField, Min(0f)] private float delayBeforeResolving = 0.5f;

        private void OnEnable()
        {
            EventBus.Subscribe<CombatRollsCompletedEvent>(OnRollsCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CombatRollsCompletedEvent>(OnRollsCompleted);
        }

        private void OnRollsCompleted(CombatRollsCompletedEvent completed)
        {
            if (runAfterEachRoll)
            {
                StartCoroutine(ResolveRoll());
            }
        }

        private IEnumerator ResolveRoll()
        {
            CombatManager combat = CombatManager.Instance;

            if (combat == null || combat.Phase != CombatPhase.Resolving)
            {
                yield break;
            }

            yield return new WaitForSeconds(delayBeforeResolving);

            BattlefieldBoard board = BattlefieldManager.Instance.Board;
            BattlefieldObjectRegistry registry = board.Objects;
            InteractionResolver resolver = combat.Interactions;

            while (combat.IsInCombat && board.Graph.TryGetMinimumEdge(out InteractionEdge nextEdge))
            {
                yield return Present(resolver.Resolve(nextEdge.First, nextEdge.Second));
            }

            foreach (BattlefieldObject leftover in new List<BattlefieldObject>(registry.ActiveObjects))
            {
                if (!combat.IsInCombat)
                {
                    yield break;
                }

                yield return Present(resolver.ResolveDirectHit(leftover));
            }

            if (combat.IsInCombat)
            {
                combat.ContinueAfterResolution();
            }
        }

        private IEnumerator Present(InteractionResult result)
        {
            if (presenter != null)
            {
                yield return presenter.Play(result);
            }
        }
    }
}

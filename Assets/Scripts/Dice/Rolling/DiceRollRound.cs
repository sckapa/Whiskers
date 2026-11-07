using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Combatants;
using Whiskers.Core;
using Whiskers.Core.Events;
using Random = UnityEngine.Random;

namespace Whiskers.Dice.Rolling
{
    // Both sides draw dice, player throws, enemy throws after a delay.
    public sealed class DiceRollRound : MonoBehaviour
    {
        private const float TrayMiddle = 0.5f;

        private enum RoundState
        {
            Idle,
            WaitingForPlayer,
            Rolling
        }

        [SerializeField] private DiceThrower thrower = null;

        private readonly List<DieRollResult> results = new List<DieRollResult>();
        private readonly HashSet<int> settledThrowIds = new HashSet<int>();
        private RoundState state = RoundState.Idle;
        private DiceHand playerHand;
        private DiceHand enemyHand;

        public bool IsWaitingForPlayer => state == RoundState.WaitingForPlayer;

        private DiceRollSettings Settings => thrower.Settings;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerThrowAimStartedEvent>(OnAimStarted);
            EventBus.Subscribe<PlayerThrowAimCanceledEvent>(OnAimCanceled);
            EventBus.Subscribe<PlayerThrowReleasedEvent>(OnPlayerReleased);
            EventBus.Subscribe<DiceThrowSettledEvent>(OnThrowSettled);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerThrowAimStartedEvent>(OnAimStarted);
            EventBus.Unsubscribe<PlayerThrowAimCanceledEvent>(OnAimCanceled);
            EventBus.Unsubscribe<PlayerThrowReleasedEvent>(OnPlayerReleased);
            EventBus.Unsubscribe<DiceThrowSettledEvent>(OnThrowSettled);
        }

        public void BeginRound(Combatant player, Combatant enemy)
        {
            BeginRound(player.DrawForRoll(), enemy.DrawForRoll());
        }

        public void BeginRound(IReadOnlyList<DiceInstance> playerDice, IReadOnlyList<DiceInstance> enemyDice)
        {
            if (state != RoundState.Idle)
            {
                throw new InvalidOperationException("A dice round is already running.");
            }

            results.Clear();
            settledThrowIds.Clear();
            playerHand = thrower.PrepareHand(playerDice, CombatSide.Player);
            enemyHand = thrower.PrepareHand(enemyDice, CombatSide.Enemy);
            state = RoundState.WaitingForPlayer;

            EventBus.Publish(new DiceRoundStartedEvent());
        }

        private void OnAimStarted(PlayerThrowAimStartedEvent aimStarted)
        {
            if (IsWaitingForPlayer)
            {
                playerHand.SetJiggling(true);
            }
        }

        private void OnAimCanceled(PlayerThrowAimCanceledEvent aimCanceled)
        {
            if (IsWaitingForPlayer)
            {
                playerHand.SetJiggling(false);
            }
        }

        private void OnPlayerReleased(PlayerThrowReleasedEvent released)
        {
            if (!IsWaitingForPlayer)
            {
                return;
            }

            // State first, an empty hand settles inside Release
            state = RoundState.Rolling;
            thrower.Release(playerHand, released.Direction, Settings.DragSpeedToStrength(released.Speed));
            StartCoroutine(ThrowEnemyHandAfterDelay());
        }

        private IEnumerator ThrowEnemyHandAfterDelay()
        {
            yield return new WaitForSeconds(Random.Range(Settings.EnemyMinDelay, Settings.EnemyMaxDelay));

            float strength = Random.Range(Settings.EnemyMinStrength, Settings.EnemyMaxStrength);
            thrower.Release(enemyHand, CalculateEnemyAim(), strength);
        }

        private Vector2 CalculateEnemyAim()
        {
            Vector2 towardCenter = (Vector2.one * TrayMiddle - Settings.GetHandCorner(CombatSide.Enemy)).normalized;
            float angle = Random.Range(-Settings.EnemyAimSpread, Settings.EnemyAimSpread);

            return ThrowDirection.Rotate(towardCenter, angle);
        }

        private void OnThrowSettled(DiceThrowSettledEvent settled)
        {
            if (state != RoundState.Rolling || !IsRoundThrow(settled.ThrowId))
            {
                return;
            }

            results.AddRange(settled.Results);
            settledThrowIds.Add(settled.ThrowId);
            TryFinishRound();
        }

        private bool IsRoundThrow(int throwId)
        {
            return (playerHand.IsReleased && playerHand.ThrowId == throwId)
                || (enemyHand.IsReleased && enemyHand.ThrowId == throwId);
        }

        private void TryFinishRound()
        {
            if (!HasSettled(playerHand) || !HasSettled(enemyHand))
            {
                return;
            }

            state = RoundState.Idle;
            EventBus.Publish(new DiceRoundSettledEvent(new List<DieRollResult>(results)));
        }

        private bool HasSettled(DiceHand hand)
        {
            return hand.IsReleased && settledThrowIds.Contains(hand.ThrowId);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Combat.Interactions;
using Whiskers.Combatants;
using Whiskers.Combatants.Definitions;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Core.GameStates;
using Whiskers.Dice;
using Whiskers.Dice.Rolling;

namespace Whiskers.Combat
{
    // setup -> roll -> resolve -> roll ... until someone dies
    public sealed class CombatManager : Singleton<CombatManager>
    {
        private const int FirstRollNumber = 1;

        [Header("Test combat")]
        [SerializeField] private bool startTestCombatOnPlay = false;
        [SerializeField] private PlayerDefinition testPlayer = null;
        [SerializeField] private EnemyDefinition testEnemy = null;

        [SerializeField] private int seed = 0;

        private System.Random random;
        private int rollNumber;

        public PlayerCombatant Player { get; private set; }
        public EnemyCombatant Enemy { get; private set; }
        public CombatPhase Phase { get; private set; } = CombatPhase.None;

        // Null until the board is ready
        public InteractionResolver Interactions { get; private set; }
        public bool IsInCombat => Phase != CombatPhase.None && Phase != CombatPhase.Finished;

        protected override void OnAwake()
        {
            random = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        private void Start()
        {
            if (startTestCombatOnPlay)
            {
                StartTestCombat();
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DiceRoundSettledEvent>(OnRoundSettled);
            EventBus.Subscribe<CombatantDiedEvent>(OnCombatantDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DiceRoundSettledEvent>(OnRoundSettled);
            EventBus.Unsubscribe<CombatantDiedEvent>(OnCombatantDied);
        }

        public void StartCombat(PlayerCombatant player, EnemyCombatant enemy)
        {
            ValidateCombatant(player);
            ValidateCombatant(enemy);

            if (IsInCombat)
            {
                throw new InvalidOperationException("A combat is already running.");
            }

            Player = player;
            Enemy = enemy;
            rollNumber = 0;
            SetPhase(CombatPhase.Setup);
            EnterCombatGameState();

            StartCoroutine(SetupWhenSceneReady());
        }

        // Call when the roll is resolved
        public void ContinueAfterResolution()
        {
            if (Phase != CombatPhase.Resolving)
            {
                return;
            }

            DiscardDiceOffTheBoard(Player);
            DiscardDiceOffTheBoard(Enemy);
            StartRoll();
        }

        [ContextMenu("Start Test Combat")]
        private void StartTestCombat()
        {
            if (testPlayer == null || testEnemy == null)
            {
                Debug.LogError("Assign Test Player and Test Enemy definitions first.", this);
                return;
            }

            if (DiceManager.Instance == null)
            {
                Debug.LogError("Test combat needs a DiceManager in the scene.", this);
                return;
            }

            Func<DiceDefinition, DiceInstance> createDice = DiceManager.Instance.CreateDice;
            StartCombat(CombatantFactory.CreatePlayer(testPlayer, createDice), CombatantFactory.CreateEnemy(testEnemy, createDice));
        }

        private IEnumerator SetupWhenSceneReady()
        {
            yield return new WaitUntil(() => DiceRollStage.Instance != null
                && BattlefieldManager.Instance != null
                && BattlefieldManager.Instance.IsReady);

            BattlefieldManager.Instance.ResetForCombat();
            DiceRollStage.Instance.Thrower.ClearTable();
            Player.BeginCombat(random);
            Enemy.BeginCombat(random);
            Interactions = new InteractionResolver(
                BattlefieldManager.Instance.Board.Objects,
                new DamageResolver(Player, Enemy),
                BattlefieldManager.Instance.Board.Graph);

            EventBus.Publish(new CombatStartedEvent(Player, Enemy));
            StartRoll();
        }

        private void StartRoll()
        {
            rollNumber = rollNumber == 0 ? FirstRollNumber : rollNumber + 1;
            SetPhase(CombatPhase.Rolling);
            Interactions.BeginRoll();
            DiceRollStage.Instance.Round.BeginRound(Player, Enemy);
        }

        // Dice still on the board are not discarded
        private static void DiscardDiceOffTheBoard(Combatant combatant)
        {
            BattlefieldBoard board = BattlefieldManager.Instance.Board;
            DiceThrower thrower = DiceRollStage.Instance.Thrower;
            // Copy, Discard changes the list
            List<DiceInstance> inPlay = new List<DiceInstance>(combatant.CombatBag.InPlay);

            foreach (DiceInstance die in inPlay)
            {
                if (!board.TryGetObjectForDie(die.Id, out _))
                {
                    combatant.CombatBag.Discard(die);
                    thrower.RemoveDie(die);
                }
            }
        }

        private void OnRoundSettled(DiceRoundSettledEvent roundSettled)
        {
            if (Phase != CombatPhase.Rolling)
            {
                return;
            }

            SetPhase(CombatPhase.Resolving);
            EventBus.Publish(new CombatRollsCompletedEvent(rollNumber, roundSettled.Results));

            // TODO: phases 1-3
        }

        private void OnCombatantDied(CombatantDiedEvent died)
        {
            if (!IsInCombat || (died.Combatant != Player && died.Combatant != Enemy))
            {
                return;
            }

            EndCombat(died.Combatant == Enemy ? CombatSide.Player : CombatSide.Enemy);
        }

        private void EndCombat(CombatSide winner)
        {
            StopAllCoroutines();
            Player.EndCombat();
            Enemy.EndCombat();
            SetPhase(CombatPhase.Finished);
            EventBus.Publish(new CombatEndedEvent(winner));
            LeaveCombatGameState(winner);
        }

        private void SetPhase(CombatPhase phase)
        {
            Phase = phase;
            EventBus.Publish(new CombatPhaseChangedEvent(phase));
        }

        private static void EnterCombatGameState()
        {
            GameManager gameManager = GameManager.Instance;

            if (gameManager != null && gameManager.CurrentState != GameState.Combat && gameManager.CanChangeState(GameState.Combat))
            {
                gameManager.ChangeState(GameState.Combat);
            }
        }

        private void LeaveCombatGameState(CombatSide winner)
        {
            GameManager gameManager = GameManager.Instance;

            if (gameManager == null)
            {
                return;
            }

            GameState next = winner == CombatSide.Enemy
                ? GameState.Defeat
                : Enemy.Tier == EnemyTier.Boss ? GameState.Victory : GameState.Run;

            if (gameManager.CanChangeState(next))
            {
                gameManager.ChangeState(next);
            }
        }

        private static void ValidateCombatant(Combatant combatant)
        {
            if (combatant == null)
            {
                throw new ArgumentNullException(nameof(combatant));
            }

            if (combatant.IsDead)
            {
                throw new ArgumentException($"{combatant.DisplayName} has no HP left.", nameof(combatant));
            }

            if (combatant.OwnedDice.Count == 0)
            {
                throw new ArgumentException($"{combatant.DisplayName} has no dice.", nameof(combatant));
            }
        }
    }
}

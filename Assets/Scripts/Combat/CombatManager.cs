using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Whiskers.Battlefield;
using Whiskers.Combat.Interactions;
using Whiskers.Combat.Presentation;
using Whiskers.Combatants;
using Whiskers.Combatants.Definitions;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Core.GameStates;
using Whiskers.Dice;
using Whiskers.Dice.Rolling;
using Whiskers.Effects;
using Whiskers.Run;

namespace Whiskers.Combat
{
    // setup -> roll -> resolve -> roll ... until someone dies
    public sealed class CombatManager : Singleton<CombatManager>
    {
        private const int FirstRollNumber = 1;

        [SerializeField] private int seed = 0;

        [Tooltip("3D dice scene, loaded additively next to Combat (must be in the build scene list)")]
        [SerializeField] private string diceRollSceneName = "DiceRoll";

        [Header("Effects")]
        // Body color / glyph color / symbol -> effects. Without it only combatant passives have effects.
        [SerializeField] private DiceEffectTable diceEffects = null;
        // Stops chains like "extra roll that rolls an extra roll"
        [SerializeField, Min(0)] private int maxAdditionalDicePerRoll = 10;

        [Header("Phase 4")]
        // Optional, plays the direct hits of the leftover objects
        [SerializeField] private InteractionPresenter presenter = null;
        [SerializeField, Min(0f)] private float pauseBeforeDamageResolution = 0.3f;

        private System.Random random;
        private int rollNumber;

        public PlayerCombatant Player { get; private set; }
        public EnemyCombatant Enemy { get; private set; }
        public CombatPhase Phase { get; private set; } = CombatPhase.None;

        // Null until the board is ready
        public InteractionResolver Interactions { get; private set; }

        // Phases 1 and 2, null outside of combat
        public CombatEffectRunner Effects { get; private set; }
        public bool IsInCombat => Phase != CombatPhase.None && Phase != CombatPhase.Finished;

        protected override void OnAwake()
        {
            random = seed == 0 ? new System.Random() : new System.Random(seed);
            LoadDiceRollScene();
        }

        // The run hands over its fight
        private void Start()
        {
            RunManager run = RunManager.Instance;

            if (run != null && run.TryTakePendingCombat(out PlayerCombatant player, out EnemyCombatant enemy))
            {
                StartCombat(player, enemy);
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

        // Additive, next to Combat. Skipped if it is already loaded, being loaded or open in the editor.
        private void LoadDiceRollScene()
        {
            if (!string.IsNullOrEmpty(diceRollSceneName) && !SceneManager.GetSceneByName(diceRollSceneName).IsValid())
            {
                SceneManager.LoadSceneAsync(diceRollSceneName, LoadSceneMode.Additive);
            }
        }

        // Call when phase 3 (all pair interactions) is done. Runs phase 4, then the next roll starts.
        public void ContinueAfterResolution()
        {
            if (Phase != CombatPhase.Resolving)
            {
                return;
            }

            StartCoroutine(RunDamageResolution());
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
            Effects = new CombatEffectRunner(Player, Enemy, BattlefieldManager.Instance.Board, diceEffects, random, maxAdditionalDicePerRoll);

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

        // Phase 4: every object still on the board that has not attacked this roll hits the opponent directly
        // (lowest id first), then the end-of-roll effects. TODO: summons and other persistent entities attack here too.
        private IEnumerator RunDamageResolution()
        {
            SetPhase(CombatPhase.DamageResolution);

            BattlefieldObjectRegistry registry = BattlefieldManager.Instance.Board.Objects;
            List<BattlefieldObject> leftovers = new List<BattlefieldObject>(registry.ActiveObjects);
            leftovers.RemoveAll(Interactions.HasAttackedThisRoll);

            if (leftovers.Count > 0)
            {
                yield return new WaitForSeconds(pauseBeforeDamageResolution);
            }

            foreach (BattlefieldObject leftover in leftovers)
            {
                // A die can leave the board while an earlier one is resolved, e.g. through an effect
                if (!IsInCombat || !Interactions.CanDealDirectHit(leftover))
                {
                    continue;
                }

                InteractionResult result = Interactions.ResolveDirectHit(leftover);

                // Runs on the presenter: a killing hit ends the combat, which stops this coroutine,
                // but its animation and damage flight must still play
                if (presenter != null)
                {
                    yield return presenter.StartCoroutine(presenter.Play(result));
                }
            }

            if (!IsInCombat)
            {
                yield break;
            }

            Effects.RunEndOfRollPhase();

            if (!IsInCombat)
            {
                yield break;
            }

            DiscardDiceOffTheBoard(Player);
            DiscardDiceOffTheBoard(Enemy);
            StartRoll();
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

            StartCoroutine(RunEffectPhases(roundSettled.Results));
        }

        // Phase 1 (landing, extra rolls included) -> phase 2 (start of combat) -> phase 3 starts on CombatRollsCompletedEvent
        private IEnumerator RunEffectPhases(IReadOnlyList<DieRollResult> roundResults)
        {
            SetPhase(CombatPhase.LandingEffects);
            yield return Effects.RunLandingPhase(rollNumber, roundResults);

            if (!IsInCombat)
            {
                yield break;
            }

            SetPhase(CombatPhase.StartEffects);
            Effects.RunStartPhase();

            if (!IsInCombat)
            {
                yield break;
            }

            SetPhase(CombatPhase.Resolving);
            EventBus.Publish(new CombatRollsCompletedEvent(rollNumber, new List<DieRollResult>(Effects.RollResults)));
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
            Effects?.EndCombat();
            Effects = null;
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

            // During a run RunManager picks the next state (map, victory, defeat)
            if (gameManager == null || (RunManager.Instance != null && RunManager.Instance.IsRunActive))
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

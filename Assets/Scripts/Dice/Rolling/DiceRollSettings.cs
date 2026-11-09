using UnityEngine;
using Whiskers.Core;

namespace Whiskers.Dice.Rolling
{
    // Normalized = 0..1 in the tray, (0, 0) is bottom-left.
    [CreateAssetMenu(fileName = "DiceRollSettings", menuName = "Whiskers/Dice/Dice Roll Settings")]
    public sealed class DiceRollSettings : ScriptableObject
    {
        [Header("Hand")]
        [SerializeField] private Vector2 playerHandCorner = new Vector2(0.1f, 0.1f);

        [SerializeField] private Vector2 enemyHandCorner = new Vector2(0.9f, 0.9f);

        [SerializeField, Min(0f)] private float handDepthBelowLid = 0.8f;

        [SerializeField, Min(1)] private int handColumns = 3;

        [SerializeField, Min(0f)] private float handSpacing = 1.15f;

        [Header("Jiggle")]
        [SerializeField, Min(0f)] private float jiggleOffset = 0.06f;
        [SerializeField, Min(0f)] private float jiggleAngle = 8f;
        [SerializeField, Min(0f)] private float jiggleFrequency = 9f;

        [Header("Player drag")]
        [SerializeField, Min(0f)] private float minDragDistance = 0.03f;

        [SerializeField, Min(0f)] private float autoThrowDragDistance = 0.15f;

        [SerializeField, Min(0f)] private float minDragSpeed = 0.3f;
        [SerializeField, Min(0f)] private float maxDragSpeed = 3f;

        [SerializeField, Min(0f)] private float dragVelocityWindow = 0.1f;

        [Header("Enemy throw")]
        [SerializeField, Min(0f)] private float enemyMinDelay = 0.3f;
        [SerializeField, Min(0f)] private float enemyMaxDelay = 1.5f;
        [SerializeField, Range(0f, 1f)] private float enemyMinStrength = 0.3f;
        [SerializeField, Range(0f, 1f)] private float enemyMaxStrength = 0.9f;

        [SerializeField, Range(0f, 180f)] private float enemyAimSpread = 45f;

        [Header("Throw")]
        [SerializeField, Min(0f)] private float minThrowImpulse = 4f;
        [SerializeField, Min(0f)] private float maxThrowImpulse = 14f;

        [SerializeField, Range(0f, 1f)] private float upwardThrowShare = 0.2f;

        [SerializeField, Range(0f, 90f)] private float throwDirectionSpread = 12f;

        [SerializeField, Range(0f, 1f)] private float throwStrengthSpread = 0.15f;

        [SerializeField, Min(0f)] private float throwTorque = 6f;

        [Header("Extra rolls")]
        [SerializeField, Min(0f)] private float spawnHeight = 3f;

        [SerializeField, Min(0f)] private float spawnVerticalSpacing = 1.1f;

        [SerializeField, Min(0f)] private float spawnScatter = 0.6f;

        [SerializeField, Range(0f, 0.5f)] private float originEdgeMargin = 0.1f;

        [Header("Settle")]
        [SerializeField, Min(0f)] private float maxSettledLinearSpeed = 0.05f;
        [SerializeField, Min(0f)] private float maxSettledAngularSpeed = 0.1f;

        [SerializeField, Min(0f)] private float requiredStillTime = 0.3f;

        [SerializeField, Min(0f)] private float maxRollTime = 8f;

        [SerializeField] private bool freezeWhenSettled = true;

        [Header("Cocked dice")]
        [SerializeField, Range(0f, 1f)] private float minFlatAlignment = 0.985f;

        [SerializeField, Min(0)] private int maxNudges = 3;
        [SerializeField, Min(0f)] private float nudgeImpulse = 1.5f;
        [SerializeField, Min(0f)] private float nudgeTorque = 2f;

        [Header("Impact sounds")]
        [SerializeField, Min(0f)] private float minImpactSpeed = 0.5f;
        [SerializeField, Min(0f)] private float maxImpactSpeed = 8f;

        [SerializeField, Min(0f)] private float impactSoundCooldown = 0.08f;

        public float HandDepthBelowLid => handDepthBelowLid;
        public int HandColumns => handColumns;
        public float HandSpacing => handSpacing;
        public float JiggleOffset => jiggleOffset;
        public float JiggleAngle => jiggleAngle;
        public float JiggleFrequency => jiggleFrequency;
        public float MinDragDistance => minDragDistance;
        public float AutoThrowDragDistance => autoThrowDragDistance;
        public float DragVelocityWindow => dragVelocityWindow;
        public float EnemyMinDelay => enemyMinDelay;
        public float EnemyMaxDelay => enemyMaxDelay;
        public float EnemyMinStrength => enemyMinStrength;
        public float EnemyMaxStrength => enemyMaxStrength;
        public float EnemyAimSpread => enemyAimSpread;
        public float MinThrowImpulse => minThrowImpulse;
        public float MaxThrowImpulse => maxThrowImpulse;
        public float UpwardThrowShare => upwardThrowShare;
        public float ThrowTorque => throwTorque;
        public float ThrowDirectionSpread => throwDirectionSpread;
        public float ThrowStrengthSpread => throwStrengthSpread;
        public float SpawnHeight => spawnHeight;
        public float SpawnVerticalSpacing => spawnVerticalSpacing;
        public float SpawnScatter => spawnScatter;
        public float OriginEdgeMargin => originEdgeMargin;
        public float MaxSettledLinearSpeed => maxSettledLinearSpeed;
        public float MaxSettledAngularSpeed => maxSettledAngularSpeed;
        public float RequiredStillTime => requiredStillTime;
        public float MaxRollTime => maxRollTime;
        public bool FreezeWhenSettled => freezeWhenSettled;
        public float MinFlatAlignment => minFlatAlignment;
        public int MaxNudges => maxNudges;
        public float NudgeImpulse => nudgeImpulse;
        public float NudgeTorque => nudgeTorque;
        public float MinImpactSpeed => minImpactSpeed;
        public float MaxImpactSpeed => maxImpactSpeed;
        public float ImpactSoundCooldown => impactSoundCooldown;

        public Vector2 GetHandCorner(CombatSide owner)
        {
            return owner == CombatSide.Player ? playerHandCorner : enemyHandCorner;
        }

        public float DragSpeedToStrength(float dragSpeed)
        {
            return Mathf.InverseLerp(minDragSpeed, maxDragSpeed, dragSpeed);
        }

        public float ImpactSpeedToVolume(float impactSpeed)
        {
            return Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, impactSpeed);
        }

        private void OnValidate()
        {
            maxThrowImpulse = Mathf.Max(maxThrowImpulse, minThrowImpulse);
            maxDragSpeed = Mathf.Max(maxDragSpeed, minDragSpeed);
            autoThrowDragDistance = Mathf.Max(autoThrowDragDistance, minDragDistance);
            enemyMaxDelay = Mathf.Max(enemyMaxDelay, enemyMinDelay);
            enemyMaxStrength = Mathf.Max(enemyMaxStrength, enemyMinStrength);
            maxImpactSpeed = Mathf.Max(maxImpactSpeed, minImpactSpeed);
        }
    }
}

using UnityEngine;
using Whiskers.Core.Events;
using Whiskers.Dice.Geometry;
using Whiskers.Dice.Visuals;

namespace Whiskers.Dice.Rolling
{
    public enum RollingDieState
    {
        Held,
        Rolling,
        Settled
    }

    // Holds the die before the throw, reports the result once it stops.
    [RequireComponent(typeof(Rigidbody), typeof(DieView))]
    public sealed class RollingDie : MonoBehaviour
    {
        // Perlin 0..1 -> -1..1
        private const float NoiseCenter = 0.5f;
        private const float NoiseToSigned = 2f;

        private const float JiggleSeedRange = 100f;
        private const float AxisSeedStep = 17f;

        private Rigidbody body;
        private DieView view;
        private DiceTray tray;
        private DiceRollSettings settings;
        private DieSettleTracker settleTracker;
        private float rollTime;
        private int nudgeCount;
        private Vector3 holdPosition;
        private Quaternion holdRotation;
        private bool isJiggling;
        private float jiggleSeed;
        private float lastImpactSoundTime = float.NegativeInfinity;

        public DieView View => view;
        public RollingDieState State { get; private set; }
        public bool IsSettled => State == RollingDieState.Settled;
        public DieRollResult Result { get; private set; }

        public void Initialize(DiceTray targetTray, DiceRollSettings rollSettings)
        {
            tray = targetTray;
            settings = rollSettings;
            body = GetComponent<Rigidbody>();
            view = GetComponent<DieView>();
            settleTracker = new DieSettleTracker(
                settings.MaxSettledLinearSpeed,
                settings.MaxSettledAngularSpeed,
                settings.RequiredStillTime);
        }

        public void Hold(Vector3 position, Quaternion rotation)
        {
            State = RollingDieState.Held;
            body.isKinematic = true;
            holdPosition = position;
            holdRotation = rotation;
            jiggleSeed = Random.value * JiggleSeedRange;
            transform.SetPositionAndRotation(position, rotation);
        }

        public void SetJiggling(bool jiggling)
        {
            isJiggling = jiggling;

            if (!jiggling && State == RollingDieState.Held)
            {
                transform.SetPositionAndRotation(holdPosition, holdRotation);
            }
        }

        public void Release(Vector3 impulse, Vector3 torque)
        {
            State = RollingDieState.Rolling;
            isJiggling = false;
            rollTime = 0f;
            body.isKinematic = false;
            body.AddForce(impulse, ForceMode.Impulse);
            body.AddTorque(torque, ForceMode.Impulse);
        }

        public void Pin()
        {
            body.isKinematic = true;
        }

        // Visual only
        public void ShowResultFace(DiceFace face)
        {
            if (IsSettled && !Result.Landing.IsCap)
            {
                view.ShowFaceOverride(Result.Landing.FaceIndex, face);
            }
        }

        private void Update()
        {
            if (State == RollingDieState.Held && isJiggling)
            {
                Jiggle();
            }
        }

        private void FixedUpdate()
        {
            if (State != RollingDieState.Rolling)
            {
                return;
            }

            rollTime += Time.fixedDeltaTime;
            bool timedOut = rollTime >= settings.MaxRollTime;
            bool isStill = settleTracker.Update(body.linearVelocity.magnitude, body.angularVelocity.magnitude, Time.fixedDeltaTime);

            if (!isStill && !timedOut)
            {
                return;
            }

            DieLanding landing = view.Model.EvaluateLanding();
            bool isCocked = landing.Alignment < settings.MinFlatAlignment;

            if (isCocked && !timedOut && nudgeCount < settings.MaxNudges)
            {
                Nudge();
                return;
            }

            Complete(landing);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (State != RollingDieState.Rolling || Time.time - lastImpactSoundTime < settings.ImpactSoundCooldown)
            {
                return;
            }

            float impactSpeed = collision.relativeVelocity.magnitude;

            if (impactSpeed < settings.MinImpactSpeed)
            {
                return;
            }

            RollingDie other = collision.rigidbody != null ? collision.rigidbody.GetComponent<RollingDie>() : null;

            // Only one of the two dice plays the sound
            if (other != null && other.State == RollingDieState.Rolling && other.GetInstanceID() < GetInstanceID())
            {
                return;
            }

            lastImpactSoundTime = Time.time;
            EventBus.Publish(new DieCollidedEvent(impactSpeed, other != null));
        }

        private void Jiggle()
        {
            float time = Time.time * settings.JiggleFrequency;
            Vector3 noise = new Vector3(
                SignedNoise(jiggleSeed, time),
                SignedNoise(jiggleSeed + AxisSeedStep, time),
                SignedNoise(jiggleSeed + AxisSeedStep * 2f, time));

            transform.SetPositionAndRotation(
                holdPosition + noise * settings.JiggleOffset,
                holdRotation * Quaternion.Euler(noise * settings.JiggleAngle));
        }

        private void Nudge()
        {
            nudgeCount++;
            settleTracker.Reset();
            body.AddForce(tray.transform.up * settings.NudgeImpulse, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * settings.NudgeTorque, ForceMode.Impulse);
        }

        private void Complete(DieLanding landing)
        {
            State = RollingDieState.Settled;

            if (settings.FreezeWhenSettled)
            {
                body.isKinematic = true;
            }

            Result = new DieRollResult(
                view.Die,
                view.Owner,
                landing,
                landing.Resolve(view.Die),
                tray.ToNormalized(transform.position));

            EventBus.Publish(new DieSettledEvent(Result));
        }

        private static float SignedNoise(float seed, float time)
        {
            return (Mathf.PerlinNoise(seed, time) - NoiseCenter) * NoiseToSigned;
        }
    }
}

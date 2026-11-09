using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;
using Whiskers.Core.Events;
using Random = UnityEngine.Random;

namespace Whiskers.Dice.Rolling
{
    // 2D dice sounds
    // TODO(audio): rattle, throw, hit clips
    public sealed class DiceAudio : MonoBehaviour
    {
        private const float FlatSpatialBlend = 0f;
        private const float BasePitch = 1f;

        [SerializeField] private DiceRollSettings settings = null;

        [Header("Clips")]
        [SerializeField] private AudioClip rattleLoop = null;
        [SerializeField] private AudioClip throwClip = null;
        [SerializeField] private AudioClip[] dieHitClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] trayHitClips = Array.Empty<AudioClip>();

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float rattleVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float throwVolume = 1f;

        [SerializeField, Range(0f, 1f)] private float impactPitchVariation = 0.1f;

        [SerializeField, Min(1)] private int impactVoices = 6;

        private AudioSource rattleSource;
        private AudioSource throwSource;
        private readonly List<AudioSource> impactSources = new List<AudioSource>();
        private int nextImpactSource;

        private void Awake()
        {
            rattleSource = CreateSource(true);
            throwSource = CreateSource(false);

            for (int i = 0; i < impactVoices; i++)
            {
                impactSources.Add(CreateSource(false));
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerThrowAimStartedEvent>(OnAimStarted);
            EventBus.Subscribe<PlayerThrowAimCanceledEvent>(OnAimCanceled);
            EventBus.Subscribe<DiceHandReleasedEvent>(OnHandReleased);
            EventBus.Subscribe<DieCollidedEvent>(OnDieCollided);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerThrowAimStartedEvent>(OnAimStarted);
            EventBus.Unsubscribe<PlayerThrowAimCanceledEvent>(OnAimCanceled);
            EventBus.Unsubscribe<DiceHandReleasedEvent>(OnHandReleased);
            EventBus.Unsubscribe<DieCollidedEvent>(OnDieCollided);
        }

        private void OnAimStarted(PlayerThrowAimStartedEvent aimStarted)
        {
            if (rattleLoop == null)
            {
                return;
            }

            rattleSource.clip = rattleLoop;
            rattleSource.volume = rattleVolume;
            rattleSource.Play();
        }

        private void OnAimCanceled(PlayerThrowAimCanceledEvent aimCanceled)
        {
            rattleSource.Stop();
        }

        private void OnHandReleased(DiceHandReleasedEvent released)
        {
            if (released.Owner == CombatSide.Player)
            {
                rattleSource.Stop();
            }

            if (throwClip != null)
            {
                throwSource.PlayOneShot(throwClip, throwVolume);
            }
        }

        private void OnDieCollided(DieCollidedEvent collided)
        {
            AudioClip[] clips = collided.HitAnotherDie ? dieHitClips : trayHitClips;
            float volume = settings.ImpactSpeedToVolume(collided.ImpactSpeed);

            if (clips.Length == 0 || volume <= 0f)
            {
                return;
            }

            AudioSource source = impactSources[nextImpactSource];
            nextImpactSource = (nextImpactSource + 1) % impactSources.Count;

            source.pitch = BasePitch + Random.Range(-impactPitchVariation, impactPitchVariation);
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = FlatSpatialBlend;
            return source;
        }
    }
}

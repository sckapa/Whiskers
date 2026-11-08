using System;
using UnityEngine;

namespace Whiskers.Combatants.Definitions
{
    // How a combatant looks in combat. Set per definition, so every rat can have its own sprite and animations.
    [Serializable]
    public sealed class CombatantVisuals
    {
        [Tooltip("Idle picture, also used when there is no animator")]
        [SerializeField] private Sprite sprite = null;
        [Tooltip("Optional. Triggers: Hit, Heal, Block, Attack, Die, Victory (all optional)")]
        [SerializeField] private RuntimeAnimatorController animator = null;
        [Tooltip("Rats face left by default; tick if this sprite is drawn facing the other way")]
        [SerializeField] private bool flipX = false;
        [SerializeField, Min(0.01f)] private float scale = 1f;

        public Sprite Sprite => sprite;
        public RuntimeAnimatorController Animator => animator;
        public bool FlipX => flipX;
        public float Scale => scale;
    }
}

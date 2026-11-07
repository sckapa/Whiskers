using System;
using UnityEngine;
using Whiskers.Core;

namespace Whiskers.Dice.Visuals
{
    [RequireComponent(typeof(DieModel))]
    public sealed class DieView : MonoBehaviour
    {
        private const int PreviewDiceId = 0;
        private const string PreviewDiceName = "Preview";

        // Base color property of URP Lit
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private DiceVisualSettings settings;
        [SerializeField] private MeshRenderer bodyRenderer;

        [Header("Preview")]
        [SerializeField] private CombatSide previewOwner = CombatSide.Player;

        private DieModel model;
        private MaterialPropertyBlock propertyBlock;

        public DieModel Model => model != null ? model : model = GetComponent<DieModel>();
        public DiceInstance Die { get; private set; }
        public CombatSide Owner { get; private set; }

        public void Bind(DiceInstance die, CombatSide owner)
        {
            if (die == null)
            {
                throw new ArgumentNullException(nameof(die));
            }

            if (die.Size != Model.Size)
            {
                throw new ArgumentException($"{die} does not fit the {Model.Size} model on '{name}'.", nameof(die));
            }

            Die = die;
            Owner = owner;
            Refresh();
        }

        public void Refresh()
        {
            if (settings == null)
            {
                Debug.LogError($"DieView on '{name}' has no DiceVisualSettings assigned.", this);
                return;
            }

            DiceStyle style = settings.GetStyle(Owner);
            ApplyBody(style);
            RebuildGlyphs(style);
        }

        // Visual only, the DiceInstance doesn't change. null = blank.
        public void ShowFaceOverride(int faceIndex, DiceFace face)
        {
            DiceStyle style = settings.GetStyle(Owner);

            foreach (DieGlyphSlot slot in Model.Faces[faceIndex].GlyphSlots)
            {
                ClearChildren(slot.Anchor);

                if (face != null)
                {
                    DieGlyphFactory.Create(face, slot, style, settings);
                }
            }
        }

        // Used by the prefab generator
        public void Configure(DiceVisualSettings visualSettings, MeshRenderer renderer)
        {
            settings = visualSettings;
            bodyRenderer = renderer;
        }

        [ContextMenu("Preview Standard Faces")]
        private void PreviewStandardFaces()
        {
            DiceInstance preview = new DiceInstance(PreviewDiceId, PreviewDiceName, Model.Size, DiceFaceSets.CreateStandardNumbers(Model.Size));
            Bind(preview, previewOwner);
        }

        [ContextMenu("Clear Glyphs")]
        private void ClearGlyphs()
        {
            foreach (DieFaceLayout face in Model.Faces)
            {
                foreach (DieGlyphSlot slot in face.GlyphSlots)
                {
                    ClearChildren(slot.Anchor);
                }
            }
        }

        private void ApplyBody(DiceStyle style)
        {
            if (style.BodyMaterial != null)
            {
                bodyRenderer.sharedMaterial = style.BodyMaterial;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, style.GetBodyColor(Die.BodyColor));
            bodyRenderer.SetPropertyBlock(propertyBlock);
        }

        private void RebuildGlyphs(DiceStyle style)
        {
            ClearGlyphs();

            for (int faceIndex = 0; faceIndex < Model.Faces.Count; faceIndex++)
            {
                DiceFace face = Die.GetFace(faceIndex);

                foreach (DieGlyphSlot slot in Model.Faces[faceIndex].GlyphSlots)
                {
                    DieGlyphFactory.Create(face, slot, style, settings);
                }
            }
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
    }
}

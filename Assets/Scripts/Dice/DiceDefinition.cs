using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice
{
    // Design-time template of a die, authored as an asset. Runtime copies are DiceInstance objects,
    // so upgrades during a run never modify the asset itself.
    [CreateAssetMenu(fileName = "NewDice", menuName = "Whiskers/Dice Definition")]
    public sealed class DiceDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private DiceSize size = DiceSize.D6;
        [Tooltip("Color of the whole die, defines its inherent effect. Material (regular or cheese) comes from the owner.")]
        [SerializeField] private DiceColor bodyColor = DiceColor.None;
        [SerializeField] private List<DiceFace> faces = new List<DiceFace>();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public DiceSize Size => size;
        public DiceColor BodyColor => bodyColor;
        public IReadOnlyList<DiceFace> Faces => faces;

        public bool HasValidFaceCount => faces.Count == size.GetFaceCount();

        [ContextMenu("Fill With Standard Numbers")]
        private void FillWithStandardNumbers()
        {
            faces = DiceFaceSets.CreateStandardNumbers(size);
        }

        private void OnValidate()
        {
            if (!HasValidFaceCount)
            {
                Debug.LogWarning($"Dice '{name}' has {faces.Count} faces, {size} needs {size.GetFaceCount()}.", this);
            }
        }
    }
}

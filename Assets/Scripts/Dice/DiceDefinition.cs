using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice
{
    // Template, the game uses DiceInstance copies
    [CreateAssetMenu(fileName = "NewDice", menuName = "Whiskers/Dice Definition")]
    public sealed class DiceDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private DiceSize size = DiceSize.D6;
        [SerializeField] private DiceColor bodyColor = DiceColor.None;
        [SerializeField] private List<DiceFace> faces = new List<DiceFace>();
        [SerializeField] private DiceFace capFace = DiceFaceSets.CreateDefaultCapFace();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public DiceSize Size => size;
        public DiceColor BodyColor => bodyColor;
        public IReadOnlyList<DiceFace> Faces => faces;
        public DiceFace CapFace => capFace;

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

using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Combat.Interactions;
using Whiskers.Dice.Rolling;

namespace Whiskers.Combat.Presentation
{
    public sealed class DamagePopupSpawner : MonoBehaviour
    {
        [SerializeField] private DiceRollDisplay display = null;
        [SerializeField] private DamagePopup popupPrefab = null;
        [SerializeField] private int sortingOrderAboveDisplay = 10;

        public void Show(DamageReport damage)
        {
            if (popupPrefab == null || display == null)
            {
                Debug.LogWarning("DamagePopupSpawner needs Display and Popup Prefab assigned.", this);
                return;
            }

            if (BattlefieldManager.Instance == null || !BattlefieldManager.Instance.IsReady)
            {
                return;
            }

            Vector2 normalized = BattlefieldManager.Instance.Board.ToNormalized(damage.Position);
            DamagePopup popup = Instantiate(popupPrefab, display.NormalizedToWorld(normalized), Quaternion.identity, transform);

            SpriteRenderer displayRenderer = display.GetComponent<SpriteRenderer>();
            popup.SetSorting(displayRenderer.sortingLayerID, displayRenderer.sortingOrder + sortingOrderAboveDisplay);
            popup.Play(damage.Dealt);
        }
    }
}

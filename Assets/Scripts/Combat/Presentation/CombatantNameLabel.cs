using TMPro;
using UnityEngine;

namespace Whiskers.Combat.Presentation
{
    // Display name of one side
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CombatantNameLabel : CombatantBoundView
    {
        protected override void OnBound()
        {
            GetComponent<TMP_Text>().text = Combatant.DisplayName;
        }
    }
}

using UnityEngine;

namespace Whiskers.Core
{
    // Counts visuals that are still playing (interaction animations, damage flights).
    // SceneFlow waits until it is free before leaving a scene, so the last hit and the death are seen.
    public static class PresentationGate
    {
        private static int busyCount;

        public static bool IsBusy => busyCount > 0;

        public static void Begin()
        {
            busyCount++;
        }

        public static void End()
        {
            busyCount = Mathf.Max(0, busyCount - 1);
        }

        // After a scene load: visuals of the old scene are gone, whatever they still held is dropped
        public static void Reset()
        {
            busyCount = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter()
        {
            Reset();
        }
    }
}

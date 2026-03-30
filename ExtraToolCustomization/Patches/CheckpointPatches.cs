using HarmonyLib;
using UnityEngine;

namespace ExtraToolCustomization.Patches
{
    [HarmonyPatch]
    internal static class CheckpointPatches
    {
        private static Vector3 _lastCheckpointPos = Vector3.zero;
        [HarmonyPatch(typeof(CheckpointManager), nameof(CheckpointManager.OnStateChange))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void OnCheckpointStateChange(pCheckpointState newState)
        {
            if (newState.lastInteraction == eCheckpointInteractionType.StoreCheckpoint && _lastCheckpointPos != newState.doorLockPosition)
            {
                _lastCheckpointPos = newState.doorLockPosition;
                EntryPoint.InvokeCheckpointReached();
            }
            else if (newState.lastInteraction == eCheckpointInteractionType.ReloadCheckpoint)
            {
                EntryPoint.InvokeCheckpointReloaded();
            }
        }

        public static void OnCleanup() => _lastCheckpointPos = Vector3.zero;
    }
}

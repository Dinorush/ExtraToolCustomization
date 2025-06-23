using ExtraToolCustomization.Utils;
using Gear;
using HarmonyLib;

namespace ExtraToolCustomization.Patches
{
    [HarmonyPatch(typeof(GearManager))]
    internal static class GearManagerPatches
    {
        [HarmonyPatch(nameof(GearManager.OnGearLoadingDone))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void CacheOfflineIDs(GearManager __instance)
        {
            foreach (var range in __instance.m_allGearPerInstanceKey.Values)
                GearIDRangeExtensions.CacheOfflineID(range);
        }
    }
}

using HarmonyLib;

namespace ExtraToolCustomization.Patches
{
    [HarmonyPatch]
    internal static class SentryGunPatches_BugFix
    {
        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.StartFiring))]
        [HarmonyPostfix]
        private static void Post_StartFiring(SentryGunInstance_Firing_Bullets __instance)
        {
            __instance.m_burstClipCurr = __instance.m_archetypeData.BurstShotCount;
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.StopFiring))]
        [HarmonyPostfix]
        private static void Post_StopFiring(SentryGunInstance_Firing_Bullets __instance)
        {
            __instance.m_burstTimer = Clock.Time + __instance.m_archetypeData.BurstDelay;
        }
    }
}

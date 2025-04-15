using HarmonyLib;
using UnityEngine;

namespace ExtraToolCustomization.Patches
{
    [HarmonyPatch]
    internal static class SentryGunPatches_BurstFix
    {
        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.StartFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_StartFiring(SentryGunInstance_Firing_Bullets __instance)
        {
            __instance.m_burstClipCurr = __instance.m_archetypeData.BurstShotCount;
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.StopFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_StopFiring(SentryGunInstance_Firing_Bullets __instance)
        {
            __instance.m_burstTimer = Clock.Time + __instance.m_archetypeData.BurstDelay;
        }
    }

    [HarmonyPatch]
    internal static class SentryGunPatches_DepletedFix
    {
        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.StopFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_StopFiring(SentryGunInstance __instance)
        {
            if (__instance.Ammo < __instance.CostOfBullet)
            {
                var detection = __instance.m_detection.Cast<SentryGunInstance_Detection>();
                detection.Target = null;
                detection.TargetAimTrans = null;
                detection.HasTarget = false;
                detection.TargetIsTagged = false;
            }
        }
    }

    [HarmonyPatch]
    internal static class SentryGunPatches_ShotgunFix
    {
        private static Vector3? _cachedDir = null;
        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.TriggerSingleFireAudio))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Pre_ShotgunFireBullet(SentryGunInstance_Firing_Bullets __instance)
        {
            if (__instance.m_fireMode != eWeaponFireMode.SentryGunShotgunSemi) return;
            if (!__instance.m_archetypeData.Sentry_FireTowardsTargetInsteadOfForward || !__instance.m_core.TryGetTargetAimPos(out var pos)) return;

            _cachedDir = __instance.MuzzleAlign.forward;
            __instance.MuzzleAlign.forward = (pos - __instance.MuzzleAlign.position).normalized;
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateAmmo))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_ShotgunFireBullet(SentryGunInstance_Firing_Bullets __instance)
        {
            if (_cachedDir != null)
            {
                __instance.MuzzleAlign.forward = (Vector3)_cachedDir;
                _cachedDir = null;
            }
        }
    }
}

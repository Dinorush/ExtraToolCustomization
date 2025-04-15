using CellMenu;
using ExtraToolCustomization.ToolData;
using ExtraToolCustomization.Utils;
using FireRateFPSFix.Dependencies;
using GameData;
using Gear;
using HarmonyLib;
using Player;
using System;

namespace ExtraToolCustomization.Patches
{
    [HarmonyPatch]
    internal static class SentryGunPatches
    {
        private static GearIDRange? _cachedRange = null;
        private static ArchetypeDataBlock? _cachedArchetype = null;

        [HarmonyPatch(typeof(CM_InventorySlotItem), nameof(CM_InventorySlotItem.LoadData))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_LoadData(GearIDRange idRange)
        {
            CacheArchetype(idRange);
        }

        [HarmonyPatch(typeof(CM_InventorySlotItem), nameof(CM_InventorySlotItem.LoadData))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_LoadData(CM_InventorySlotItem __instance)
        {
            if (_cachedArchetype != null)
            {
                __instance.GearDescription = _cachedArchetype.Description;
                __instance.GearArchetypeName = _cachedArchetype.PublicName;
                __instance.m_subTitleText.text = __instance.m_archetypePrefix + __instance.GearArchetypeName;
                TMPro.TMP_UpdateManager.RegisterTextElementForGraphicRebuild(__instance.m_subTitleText);
            }
        }

        [HarmonyPatch(typeof(CM_PageExpeditionSuccess), nameof(CM_PageExpeditionSuccess.TryGetArchetypeName))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_GetArchetypeName(PlayerBackpack backpack, InventorySlot slot)
        {
            if (backpack.TryGetBackpackItem(slot, out var backpackItem))
                CacheArchetype(backpackItem.GearIDRange);
        }

        [HarmonyPatch(typeof(SentryGunFirstPerson), nameof(SentryGunFirstPerson.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_FPGearSpawn(SentryGunFirstPerson __instance)
        {
            CacheArchetype(__instance.GearIDRange);
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_InstanceGearSpawn(SentryGunInstance __instance)
        {
            CacheArchetype(__instance.GearIDRange);
        }

        private static void CacheArchetype(GearIDRange idRange)
        {
            if (_cachedRange != null && _cachedRange.Pointer == idRange.Pointer) return;

            _cachedRange = idRange;
            _cachedArchetype = null;

            if (_cachedRange == null) return;

            // Ignore gear if fire mode is not sentry
            eWeaponFireMode fireMode = (eWeaponFireMode)_cachedRange.GetCompID(eGearComponent.FireMode);
            if (fireMode < eWeaponFireMode.SentryGunSemi) return;

            uint categoryID = _cachedRange.GetCompID(eGearComponent.Category);
            if (categoryID == 0) return;

            GearCategoryDataBlock? categoryBlock = GearCategoryDataBlock.GetBlock(categoryID);
            if (categoryBlock == null) return;

            uint archetypeID = (eWeaponFireMode) _cachedRange.GetCompID(eGearComponent.FireMode) switch
            {
                eWeaponFireMode.SentryGunSemi => categoryBlock.SemiArchetype,
                eWeaponFireMode.SentryGunAuto => categoryBlock.AutoArchetype,
                eWeaponFireMode.SentryGunBurst => categoryBlock.BurstArchetype,
                eWeaponFireMode.SentryGunShotgunSemi => categoryBlock.SemiArchetype,
                _ => 0
            };

            if (archetypeID == 0) return;

            ArchetypeDataBlock? archBlock = ArchetypeDataBlock.GetBlock(archetypeID);
            if (archBlock == null) return;

            _cachedArchetype = archBlock;
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.GetArchetypeDataForFireMode))]
        [HarmonyPostfix]
        private static void CorrectArchetype(ref ArchetypeDataBlock __result)
        {
            if (_cachedArchetype != null)
                __result = _cachedArchetype;
        }

        [HarmonyPatch(typeof(SentryGunFirstPerson), nameof(SentryGunFirstPerson.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_FPGearSpawn(SentryGunFirstPerson __instance)
        {
            SentryData? data = ToolDataManager.GetArchData<SentryData>(__instance.ArchetypeID);

            if (data != null)
            {
                __instance.m_deployPickupInteractionDuration = data.PlacementTime;
                __instance.m_interactPlaceItem.InteractDuration = data.PlacementTime;
            }
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_SentrySpawn(SentryGunInstance __instance)
        {
            var data = ToolDataManager.GetArchData<SentryData>(__instance.ArchetypeID);

            if (data != null)
            {
                __instance.m_rotationAnimator.speed = SentryData.DefDeployTime / Math.Max(0.01f, data.DeployTime);
                __instance.m_initialScanDelay = data.DeployTime;
                __instance.m_startScanTimer = Clock.Time + data.DeployTime;
                __instance.m_interactPickup.m_interactDuration = data.PickupTime;
                var visuals = __instance.m_visuals.Cast<SentryGunInstance_ScannerVisuals_Plane>();
                visuals.m_scanningColorOrg = visuals.m_scanningColor = data.ScanColor;
                visuals.m_hasTargetColor = data.TargetColor;
                visuals.SetVisualStatus(eSentryGunStatus.BootUp);
            }
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.StartScanning))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_SentryStartScanning(SentryGunInstance __instance)
        {
            var data = ToolDataManager.GetArchData<SentryData>(__instance.ArchetypeID);

            if (data != null)
            {
                __instance.m_detection.Cast<SentryGunInstance_Detection>().m_scanningTimer = Clock.Time + data.ScanDelay;
            }
        }

        private static IntPtr s_cachedHitDataPtr = IntPtr.Zero;
        private static SentryData? s_cachedData;
        private static bool s_hitFriendly;
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        [HarmonyPriority(Priority.Low)]
        [HarmonyBefore(EWCWrapper.PLUGIN_GUID)]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void SetBackDamage(ref Weapon.WeaponHitData weaponRayData, ref bool allowDirectionalBonus)
        {
            s_hitFriendly = false;
            if (weaponRayData.Pointer != s_cachedHitDataPtr)
            {
                s_cachedHitDataPtr = weaponRayData.Pointer;
                s_cachedData = null;
                if (allowDirectionalBonus && weaponRayData.vfxBulletHit == null) return;

                PlayerAgent source = weaponRayData.owner;
                if (!PlayerBackpackManager.TryGetBackpack(source.Owner, out var backpack)) return;
                if (!backpack.TryGetBackpackItem(InventorySlot.GearClass, out var item)) return;
                s_cachedData = ToolDataManager.GetArchData<SentryData>(item.Instance.Cast<ItemEquippable>().ArchetypeID);
            }

            if (s_cachedData == null) return;

            allowDirectionalBonus = s_cachedData.BackDamage;
            // Shotgun sentries with back damage get past sentry checks in other plugins because their vfx is null.
            // This gives them a vfx so they don't count as guns in other plugins.
            if (weaponRayData.vfxBulletHit == null)
                weaponRayData.vfxBulletHit = EmptyFeedbackPlayer.Instance;

            var damageable = DamageableUtil.GetDamageableFromRayHit(weaponRayData.rayHit);
            if (damageable == null) return;

            var agent = damageable.GetBaseAgent();
            if (agent != null && agent.Type == Agents.AgentType.Player)
            {
                s_hitFriendly = true;
                weaponRayData.damage *= s_cachedData.FriendlyDamageMulti;
            }
        }

        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        [HarmonyPostfix]
        private static void FixFFDamage(ref Weapon.WeaponHitData weaponRayData)
        {
            if (s_hitFriendly)
                weaponRayData.damage /= s_cachedData!.FriendlyDamageMulti;
        }
    }
}

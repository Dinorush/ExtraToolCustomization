using CellMenu;
using GameData;
using Gear;
using HarmonyLib;
using Player;

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
            if (_cachedRange == idRange) return;

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
    }
}

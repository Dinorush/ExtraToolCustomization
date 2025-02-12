using HarmonyLib;
using ExtraToolCustomization.ToolData;
using ExtraToolCustomization.Utils;
using ExtraToolCustomization.Networking.MineDeployer;
using SNetwork;
using Player;

namespace ExtraToolCustomization.Patches
{
    [HarmonyPatch]
    internal static class MineDeployerPatches
    {
        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Setup(MineDeployerFirstPerson __instance)
        {
            uint offlineID = __instance.GearIDRange?.GetOfflineID() ?? 0;
            var data = ToolDataManager.GetData<MineData>(offlineID, __instance.ItemDataBlock.persistentID, 0);
            if (data != null)
            {
                __instance.m_interactPlaceItem.InteractDuration = data.PlacementTime;
                __instance.m_timeBetweenPlacements = data.PlacementCooldown;
            }
        }

        [HarmonyPatch(typeof(PlayerBackpack), nameof(PlayerBackpack.SpawnAndEquipItem))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_ItemPickup(BackpackItem __result)
        {
            var data = ToolDataManager.GetItemData<MineData>(__result.ItemID);
            if (data != null)
            {
                var deployer = __result.Instance.Cast<MineDeployerFirstPerson>();
                deployer.m_interactPlaceItem.InteractDuration = data.PlacementTime;
                deployer.m_timeBetweenPlacements = data.PlacementCooldown;
            }
        }

        [HarmonyPatch(typeof(PlayerBotActionDeployTripMine), nameof(PlayerBotActionDeployTripMine.PlaceTripMine))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_PlaceMineBot(PlayerBotActionDeployTripMine __instance)
        {
            if (!SNet.Master) return;

            ItemEquippable item = __instance.m_desc.BackpackItem.Instance.TryCast<ItemEquippable>()!;
            uint offlineID = item.GearIDRange?.GetOfflineID() ?? 0;
            MineDeployerManager.SendMineDeployerID(__instance.m_agent.Owner, offlineID, item.ItemDataBlock.persistentID);
        }

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.PlaceMine))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_PlaceMine(MineDeployerFirstPerson __instance)
        {
            // Client can't modify mine damage, and host can't get the offline ID of the deployer nor the object for consumables.
            // Need to send the IDs separately and modify the mine using stored IDs.
            if (__instance.CheckCanPlace())
            {
                uint offlineID = __instance.GearIDRange?.GetOfflineID() ?? 0;
                MineDeployerManager.SendMineDeployerID(__instance.Owner.Owner, offlineID, __instance.ItemDataBlock.persistentID);
            }
        }

        [HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.OnSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_MineSpawned(MineDeployerInstance __instance, ref pItemSpawnData spawnData)
        {
            if (!spawnData.owner.GetPlayer(out SNet_Player source)) return;

            if (!MineDeployerManager.HasMineDeployerID(source))
            {
                // The packet that tells us the mine deployer IDs may be in transit. Store the mine for later modification.
                MineDeployerManager.StoreMineDeployer(source, __instance);
                return;
            }

            MineDeployerID deployerID = MineDeployerManager.PopMineDeployerID(source);
            MineData? data = MineDeployerManager.GetMineData(deployerID);
            if (data == null) return;

            MineDeployerManager.ApplyDataToMine(__instance, data);
        }

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.OnStickyMineSpawned))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_DeployedMine(MineDeployerFirstPerson __instance, ISyncedItem item)
        {
            if (__instance.m_isConsumable) return;

            uint offlineID = __instance.GearIDRange?.GetOfflineID() ?? 0;
            var data = ToolDataManager.GetData<MineData>(offlineID, __instance.ItemDataBlock.persistentID, 0);
            if (data == null) return;

            MineDeployerInstance? mineDeployerInstance = item.GetItem().TryCast<MineDeployerInstance>();
            if (mineDeployerInstance != null)
                mineDeployerInstance.PickupInteraction.Cast<Interact_Timed>().InteractDuration = data.PickupTime;
        }
    }
}

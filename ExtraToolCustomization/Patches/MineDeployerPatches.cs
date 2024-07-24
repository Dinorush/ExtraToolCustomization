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
        [HarmonyPatch(typeof(PlayerBotActionDeployTripMine), nameof(PlayerBotActionDeployTripMine.PlaceTripMine))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_PlaceMine(PlayerBotActionDeployTripMine __instance)
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
            if (!SNet.Master) return;

            if (!spawnData.owner.GetPlayer(out SNet_Player source)) return;

            MineDeployerInstance_Detonate_Explosive? explosive = __instance.m_detonation.TryCast<MineDeployerInstance_Detonate_Explosive>();
            if (explosive == null) return;

            if (!MineDeployerManager.HasMineDeployerID(source))
            {
                // The packet that tells us the mine deployer IDs may be in transit. Store the mine for later modification.
                MineDeployerManager.StoreMineDeployer(source, explosive);
                return;
            }

            MineDeployerID deployerID = MineDeployerManager.PopMineDeployerID(source);
            MineData? data = MineDeployerManager.GetMineData(deployerID);
            if (data == null) return;

            MineDeployerManager.ApplyDataToMine(explosive, data);
        }
    }
}

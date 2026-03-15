using AK;
using ExtraToolCustomization.Networking.MineDeployer;
using ExtraToolCustomization.ToolData;
using ExtraToolCustomization.Utils;
using HarmonyLib;
using Player;
using SNetwork;
using UnityEngine;

namespace ExtraToolCustomization.Patches
{
    [HarmonyPatch]
    internal static class MineDeployerPatches
    {
        private static MineData? _equippedMineData = null;
        private const float PlacementIndicatorBuffer = 1.5f;

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.OnGearSpawnComplete))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Setup(MineDeployerFirstPerson __instance)
        {
            uint offlineID = __instance.GearIDRange.GetOfflineID();
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
                var deployer = __result.Instance.TryCast<MineDeployerFirstPerson>();
                if (deployer == null) return;

                deployer.m_interactPlaceItem.InteractDuration = data.PlacementTime;
                deployer.m_timeBetweenPlacements = data.PlacementCooldown;
            }
        }

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.OnWield))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_MineEquipped(MineDeployerFirstPerson __instance)
        {
            uint offlineID = __instance.GearIDRange.GetOfflineID();
            _equippedMineData = ToolDataManager.GetData<MineData>(offlineID, __instance.ItemDataBlock.persistentID, 0);
        }

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.Update))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_Update(MineDeployerFirstPerson __instance)
        {
            if (_equippedMineData == null) return true;

            var placementIndicator = __instance.m_placementIndicator;
            var sound = __instance.Sound;
            sound.UpdatePosition(__instance.transform.position);
            if (!__instance.CanWield)
            {
                placementIndicator.SetVisible(vis: false);
                return false;
            }

            bool canPlace = CheckPlacement(__instance);
            var owner = __instance.Owner;
            __instance.m_interactPlaceItem.ManualUpdateWithCondition(canPlace, owner, canPlace);
            if (canPlace && owner != null)
            {
                owner.FPItemHolder.DontRelax();
                if (__instance.m_pointLight != null)
                {
                    __instance.m_pointLight.transform.rotation = Quaternion.LookRotation(__instance.m_lastRayHit.normal * -1f, owner.transform.forward);
                }
                if (!__instance.m_lastCanPlace)
                {
                    placementIndicator.SetVisible(vis: true);
                    placementIndicator.SetPlacementEnabled(enabled: true);
                    __instance.m_lastCanPlace = true;
                    sound.Post(EVENTS.DEPLOYER_BEEP_TARGET_ACQUIRED);
                }
                return false;
            }

            bool showIndicator = false;
            if (Clock.Time > __instance.m_indicatorTimer && __instance.m_hasRayHit)
                showIndicator = __instance.m_lastRayHit.distance < _equippedMineData.PlacementRange + PlacementIndicatorBuffer;
            if (showIndicator && !__instance.m_lastShowIndicator)
            {
                sound.Post(EVENTS.DEPLOYER_BEEP_SURFACE_DETECTED);
                __instance.m_lastShowIndicator = true;
            }
            else if (!showIndicator && __instance.m_lastShowIndicator)
            {
                sound.Post(EVENTS.DEPLOYER_BEEP_SURFACE_LOST);
                __instance.m_lastShowIndicator = false;
            }
            placementIndicator.SetVisible(showIndicator);
            placementIndicator.SetPlacementEnabled(enabled: false);
            if (__instance.m_lastCanPlace)
            {
                __instance.m_lastCanPlace = false;
                sound.Post(EVENTS.DEPLOYER_BEEP_TARGET_LOST);
            }
            return false;
        }

        private static bool CheckPlacement(MineDeployerFirstPerson __instance)
        {
            __instance.m_hasRayHit = false;
            var owner = __instance.Owner;
            var camera = owner.FPSCamera;
            if (!owner.FPItemHolder.ItemHiddenTrigger && !owner.Interaction.HasWorldInteraction && Physics.Raycast(camera.Position, camera.Forward, out var rayHit, _equippedMineData!.PlacementRange + PlacementIndicatorBuffer, LayerManager.MASK_TRIPMINE_CAMERARAY) && !Physics.Linecast(camera.Position, rayHit.point, LayerManager.MASK_TRIPMINE_PLACEMENT_BLOCKERS))
            {
                __instance.m_lastRayHit = rayHit;
                __instance.m_hasRayHit = true;
                bool validSurface = true;
                if (__instance.m_requiresGroundSurface)
                {
                    validSurface = Vector3.Angle(rayHit.normal, Vector3.up) < __instance.m_maxGroundSurfaceAngleDegrees;
                }
                return rayHit.distance < _equippedMineData.PlacementRange && validSurface;
            }

            return false;
        }

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.CheckCanPlace))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_CheckCanPlace(MineDeployerFirstPerson __instance, ref bool __result)
        {
            if (_equippedMineData == null) return true;

            __result = CheckPlacement(__instance);
            return false;
        }


        [HarmonyPatch(typeof(PlayerBotActionDeployTripMine), nameof(PlayerBotActionDeployTripMine.PlaceTripMine))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_PlaceMineBot(PlayerBotActionDeployTripMine __instance)
        {
            if (!SNet.Master || !PlayerBackpackManager.TryGetBackpack(__instance.m_agent.Owner, out var backpack)) return;

            if (!backpack.TryGetBackpackItem(InventorySlot.GearClass, out var bpItem)) return;

            ItemEquippable item = bpItem.Instance.Cast<ItemEquippable>();
            uint offlineID = item.GearIDRange.GetOfflineID();
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
                uint offlineID = __instance.GearIDRange.GetOfflineID();
                MineDeployerManager.SendMineDeployerID(__instance.Owner.Owner, offlineID, __instance.ItemDataBlock.persistentID);
            }
        }

        [HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.OnSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_MineSpawned(MineDeployerInstance __instance, ref pItemSpawnData spawnData)
        {
            if (!spawnData.owner.GetPlayer(out SNet_Player source)) return;

            MineDeployerManager.Internal_ReceiveMineDeployed(source.Lookup, __instance);
        }

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.OnStickyMineSpawned))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_DeployedMine(MineDeployerFirstPerson __instance, ISyncedItem item)
        {
            if (__instance.m_isConsumable) return;

            uint offlineID = __instance.GearIDRange.GetOfflineID();
            var data = ToolDataManager.GetData<MineData>(offlineID, __instance.ItemDataBlock.persistentID, 0);
            if (data == null) return;

            MineDeployerInstance? mineDeployerInstance = item.GetItem().TryCast<MineDeployerInstance>();
            if (mineDeployerInstance != null)
                mineDeployerInstance.PickupInteraction.Cast<Interact_Timed>().InteractDuration = data.PickupTime;
        }

        [HarmonyPatch(typeof(CheckpointManager), nameof(CheckpointManager.OnStateChange))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_CheckpointLoaded(pCheckpointState oldState, bool isRecall)
        {
            if (oldState.isReloadingCheckpoint && isRecall)
                MineDeployerManager.Reset();
        }
    }
}

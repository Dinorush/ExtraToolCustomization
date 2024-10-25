using ExtraToolCustomization.ToolData;
using SNetwork;
using System.Collections.Generic;
using static SNetwork.SNetStructs;

namespace ExtraToolCustomization.Networking.MineDeployer
{
    public static class MineDeployerManager
    {
        private static readonly MineDeployerSync _sync = new();

        internal static Dictionary<ulong, MineDeployerID> _storedPackets = new();
        internal static Dictionary<ulong, MineDeployerInstance_Detonate_Explosive> _storedMines = new();

        internal static void Init()
        {
            _sync.Setup();
        }

        public static void SendMineDeployerID(SNet_Player source, uint offlineID, uint itemID)
        {
            MineDeployerID packet = default;
            packet.source.SetPlayer(source);
            packet.itemID = (ushort) itemID;
            packet.offlineID = (ushort) offlineID;

            _sync.Send(packet);
        }

        public static bool HasMineDeployerID(SNet_Player source) => SNet.IsMaster && _storedPackets.ContainsKey(source.Lookup);

        public static void StoreMineDeployer(SNet_Player source, MineDeployerInstance_Detonate_Explosive instance) => _storedMines[source.Lookup] = instance;

        public static MineDeployerID PopMineDeployerID(SNet_Player source)
        {
            if (!HasMineDeployerID(source)) return default;
            
            MineDeployerID packet = _storedPackets[source.Lookup];
            _storedPackets.Remove(source.Lookup);
            return packet;
        }

        internal static void Internal_ReceiveMineDeployerPacket(ulong lookup, MineDeployerID packet)
        {
            if (!_storedMines.ContainsKey(lookup))
            {
                // The packet that tells us to spawn the mine may be in transit. Store the IDs for later modification.
                _storedPackets[lookup] = packet;
                return;
            }

            MineData? data = GetMineData(packet);
            MineDeployerInstance_Detonate_Explosive explosive = _storedMines[lookup];
            _storedMines.Remove(lookup);

            if (data == null) return;

            ApplyDataToMine(explosive, data);
        }

        public static MineData? GetMineData(MineDeployerID deployerID) => ToolDataManager.Current.GetData<MineData>(deployerID.offlineID, deployerID.itemID);

        public static void ApplyDataToMine(MineDeployerInstance_Detonate_Explosive explosive, MineData data)
        {
            explosive.m_explosionDelay = data.Delay;
            explosive.m_radius = data.Radius;
            explosive.m_distanceMin = data.DistanceMin;
            explosive.m_distanceMax = data.DistanceMax;
            explosive.m_damageMin = data.DamageMin;
            explosive.m_damageMax = data.DamageMax - data.DamageMin;
            explosive.m_explosionForce = data.Force;
        }
    }

    public struct MineDeployerID
    {
        public pPlayer source;
        public ushort offlineID;
        public ushort itemID;
    }
}

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
        internal static Dictionary<ulong, MineDeployerInstance> _storedMines = new();

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

        public static bool HasMineDeployerID(SNet_Player source) => _storedPackets.ContainsKey(source.Lookup);

        public static void StoreMineDeployer(SNet_Player source, MineDeployerInstance instance) => _storedMines[source.Lookup] = instance;

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
            MineDeployerInstance instance = _storedMines[lookup];
            _storedMines.Remove(lookup);

            if (data == null) return;

            ApplyDataToMine(instance, data);
        }

        public static MineData? GetMineData(MineDeployerID deployerID) => ToolDataManager.GetData<MineData>(deployerID.offlineID, deployerID.itemID, 0);

        public static void ApplyDataToMine(MineDeployerInstance instance, MineData data)
        {
            MineDeployerInstance_Detonate_Explosive? explosive = instance.m_detonation.Cast<MineDeployerInstance_Detonate_Explosive>();
            if (explosive != null)
            {
                explosive.m_explosionDelay = data.Delay;
                explosive.m_radius = data.Radius;
                explosive.m_distanceMin = data.DistanceMin;
                explosive.m_distanceMax = data.DistanceMax;
                explosive.m_damageMin = data.DamageMin;
                explosive.m_damageMax = data.DamageMax - data.DamageMin;
                explosive.m_explosionForce = data.Force;
            }

            if (data.BeamData != null)
            {
                var beam = instance.GetComponentInChildren<UnityEngine.LineRenderer>();
                if (beam != null)
                {
                    beam.startColor = data.BeamData.Color;
                    beam.startWidth = data.BeamData.Width;
                    beam.endWidth = data.BeamData.Width;
                    beam.widthMultiplier = 1f;
                    instance.m_detection.Cast<MineDeployerInstance_Detect_Laser>().m_maxLineDistance = data.BeamData.Length;
                    float lenMod = data.BeamData.Length / MineBeamData.DefLength;
                    var colorKeys = beam.colorGradient.colorKeys;
                    for (int i = 0; i < colorKeys.Count; i++)
                    {
                        UnityEngine.GradientColorKey key = colorKeys[i];
                        key.time *= lenMod;
                        colorKeys[i] = key;
                    }
                }
            }

            if (data.LightData != null)
            {
                var light = instance.GetComponentInChildren<UnityEngine.Light>();
                if (light != null)
                {
                    light.color = data.LightData.Color;
                    light.range = data.LightData.Range;
                    light.intensity = data.LightData.Intensity;
                }

                var point = instance.GetComponentInChildren<FX_EffectSystem.FX_SimplePointLight>();
                if (point != null)
                {
                    point.Color = data.LightData.Color;
                    point.Range = data.LightData.Range;
                    point.Intensity = data.LightData.Intensity;
                    var effectLight = point.Light;
                    if (effectLight != null)
                    {
                        effectLight.Color = data.LightData.Color;
                        effectLight.Range = data.LightData.Range;
                        effectLight.Intensity = data.LightData.Intensity;
                    }
                }
            }
        }
    }

    public struct MineDeployerID
    {
        public pPlayer source;
        public ushort offlineID;
        public ushort itemID;
    }
}

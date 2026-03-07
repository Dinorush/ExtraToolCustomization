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

        internal static void Reset()
        {
            _storedMines.Clear();
            _storedPackets.Clear();
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

        internal static void Internal_ReceiveMineDeployerID(ulong lookup, MineDeployerID packet)
        {
            if (_storedMines.Remove(lookup, out var instance))
                TryApplyMineData(packet, instance);
            else
                // The packet that tells us to spawn the mine may be in transit. Store the IDs for later modification.
                _storedPackets[lookup] = packet;
        }

        internal static void Internal_ReceiveMineDeployed(ulong lookup, MineDeployerInstance instance)
        {
            if (_storedPackets.Remove(lookup, out var packet))
                TryApplyMineData(packet, instance);
            else
                // The packet that tells us the mine deployer IDs may be in transit. Store the mine for later modification.
                _storedMines[lookup] = instance;
        }

        private static void TryApplyMineData(MineDeployerID deployerID, MineDeployerInstance instance)
        {
            var data = ToolDataManager.GetData<MineData>(deployerID.offlineID, deployerID.itemID, 0);
            if (data != null)
                ApplyDataToMine(instance, data);
        }

        private static void ApplyDataToMine(MineDeployerInstance instance, MineData data)
        {
            MineDeployerInstance_Detonate_Explosive? explosive = instance.m_detonation.TryCast<MineDeployerInstance_Detonate_Explosive>();
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
            else
            {
                MineDeployerInstance_Detonate_Glue? glue = instance.m_detonation.TryCast<MineDeployerInstance_Detonate_Glue>();
                if (glue != null)
                {
                    glue.m_explosionDelay = data.Delay;
                    glue.m_radius = data.Radius;
                    glue.m_distanceMin = data.DistanceMin;
                    glue.m_distanceMax = data.DistanceMax;
                    glue.m_damageMin = data.DamageMin;
                    glue.m_damageMax = data.DamageMax - data.DamageMin;
                    glue.m_initialExplosionDelay = data.BubbleDelay;
                    glue.m_projCount = data.BubbleCount;
                    glue.m_explosionDelay = data.BubbleBatchCooldown;
                }
            }

            var beamData = data.BeamData;
            if (beamData != null)
            {
                var beam = instance.GetComponentInChildren<UnityEngine.LineRenderer>();
                if (beam != null)
                {
                    beam.startColor = beamData.Color;
                    beam.startWidth = beamData.Width;
                    beam.endWidth = beamData.Width;
                    beam.widthMultiplier = 1f;
                    instance.m_detection.Cast<MineDeployerInstance_Detect_Laser>().m_maxLineDistance = beamData.Length;
                    float lenMod = beamData.Length / MineBeamData.DefLength;
                    var colorKeys = beam.colorGradient.colorKeys;
                    for (int i = 0; i < colorKeys.Count; i++)
                    {
                        UnityEngine.GradientColorKey key = colorKeys[i];
                        key.time *= lenMod;
                        colorKeys[i] = key;
                    }
                }
            }

            var lightData = data.LightData;
            if (lightData != null)
            {
                var light = instance.GetComponentInChildren<UnityEngine.Light>();
                if (light != null)
                {
                    light.color = lightData.Color;
                    light.range = lightData.Range;
                    light.intensity = lightData.Intensity;
                }

                var point = instance.GetComponentInChildren<FX_EffectSystem.FX_SimplePointLight>();
                if (point != null)
                {
                    point.Color = lightData.Color;
                    point.Range = lightData.Range;
                    point.Intensity = lightData.Intensity;
                    var effectLight = point.Light;
                    if (effectLight != null)
                    {
                        effectLight.Color = lightData.Color;
                        effectLight.Range = lightData.Range;
                        effectLight.Intensity = lightData.Intensity;
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

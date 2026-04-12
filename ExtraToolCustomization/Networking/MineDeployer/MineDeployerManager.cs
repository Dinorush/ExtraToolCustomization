using ExtraToolCustomization.ToolData;
using SNetwork;
using System;
using System.Collections.Generic;
using static SNetwork.SNetStructs;

namespace ExtraToolCustomization.Networking.MineDeployer
{
    public static class MineDeployerManager
    {
        private static readonly MineDeployerSync _sync = new();
        private static readonly Dictionary<ulong, Queue<MineDeployerID>> _storedPackets = new();
        private static readonly Dictionary<ulong, MineDeployerInstance> _storedMines = new();
        private static readonly Dictionary<ushort, MineData> _storedMineData = new();
        private static readonly Dictionary<ushort, MineData> _liveMineData = new();

        internal static void Init()
        {
            _sync.Setup();
            EntryPoint.OnCheckpointReached += OnCheckpointReached;
            EntryPoint.OnCheckpointReloaded += OnCheckpointReloaded;
        }

        internal static void Reset() => ResetStorage();

        private static void ResetStorage(bool checkpoint = false)
        {
            _storedMines.Clear();
            _storedPackets.Clear();
            _liveMineData.Clear();
            if (!checkpoint)
                _storedMineData.Clear();
            else
            {
                _liveMineData.EnsureCapacity(_storedMineData.Count);
                foreach (var kv in _storedMineData)
                    _liveMineData[kv.Key] = kv.Value;
            }
        }

        private static void OnCheckpointReached()
        {
            _storedMineData.Clear();
            _storedMineData.EnsureCapacity(_liveMineData.Count);
            foreach (var kv in _liveMineData)
                _storedMineData[kv.Key] = kv.Value;
        }

        private static void OnCheckpointReloaded() => ResetStorage(checkpoint: true);

        public static void SendMineDeployerID(SNet_Player source, uint offlineID, uint itemID)
        {
            MineDeployerID packet = default;
            packet.source.SetPlayer(source);
            packet.itemID = (ushort) itemID;
            packet.offlineID = (ushort) offlineID;

            _sync.Send(packet);
        }

        internal static void Internal_ReceiveMineDeployerID(ulong lookup, MineDeployerID packet)
        {
            if (_storedMines.Remove(lookup, out var instance))
                TryApplyMineData(packet, instance);
            else
            {
                // The packet that tells us to spawn the mine may be in transit. Store the IDs for later modification.
                // Store in queue since fast deployers may deploy 2+ before it spawns the first.
                // (Place packet is client -> players, mine packet is client -> host -> players)
                if (!_storedPackets.TryGetValue(lookup, out var queue))
                    _storedPackets.Add(lookup, queue = new());
                queue.Enqueue(packet);
            }
        }

        internal static void Internal_ReceiveMineDeployed(ulong lookup, MineDeployerInstance instance)
        {
            if (TryRestoreMineData(instance)) return;

            if (_storedPackets.TryGetValue(lookup, out var queue) && queue.TryDequeue(out var packet))
                TryApplyMineData(packet, instance);
            else
                // The packet that tells us the mine deployer IDs may be in transit. Store the mine for later modification.
                _storedMines[lookup] = instance;
        }

        private static void TryApplyMineData(MineDeployerID deployerID, MineDeployerInstance instance)
        {
            if (instance == null || instance.gameObject == null) return;

            var data = ToolDataManager.GetData<MineData>(deployerID.offlineID, deployerID.itemID, 0);
            if (data != null)
            {
                var key = instance.Replicator.Key;
                if (_liveMineData.TryAdd(key, data))
                    instance.m_detonation.add_OnDetonationDone(new Action(() => _liveMineData.Remove(key)));
                ApplyDataToMine(instance, data);
            }
        }

        private static bool TryRestoreMineData(MineDeployerInstance instance)
        {
            if (instance.gameObject == null) return true;

            if (!_storedMineData.TryGetValue(instance.Replicator.Key, out var data)) return false;

            ApplyDataToMine(instance, data);
            instance.PickupInteraction.Cast<Interact_Timed>().InteractDuration = data.PickupTime;
            return true;
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
                    
                    var foamData = data.FoamData;
                    if (foamData != null)
                    {
                        glue.m_initialExplosionDelay = foamData.BubbleDelay;
                        glue.m_projCount = foamData.BubbleCount;
                        glue.m_explosionDelay = foamData.BubbleBatchCooldown;
                        glue.m_projAngMinMax = new UnityEngine.Vector2(-foamData.BubbleAngle, foamData.BubbleAngle);
                        glue.m_projVelMinMax = new UnityEngine.Vector2(foamData.BubbleSpeedMin, foamData.BubbleSpeedMax);
                    }
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

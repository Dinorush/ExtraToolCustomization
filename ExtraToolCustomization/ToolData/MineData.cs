using System.Text.Json.Serialization;
using UnityEngine;

namespace ExtraToolCustomization.ToolData
{
    public sealed class MineData : IToolData
    {
        public uint OfflineID { get; set; } = 0;
        public uint ItemID { get; set; } = 0;
        [JsonIgnore]
        public uint ArchetypeID { get; set; } = 0;
        public string Name { get; set; } = string.Empty;

        public float Delay { get; set; } = 0;
        public float Radius { get; set; } = 0;
        public float DistanceMin { get; set; } = 0;
        public float DistanceMax { get; set; } = 0;
        public float DamageMin { get; set; } = 0;
        public float DamageMax { get; set; } = 0;
        public float Force { get; set; } = 0;
        public float PlacementTime { get; set; } = 0.5f;
        public float PlacementCooldown { get; set; } = 2f;
        public float PickupTime { get; set; } = 0.5f;

        public MineBeamData? BeamData { get; set; } = null;
        public MineLightData? LightData { get; set; } = null;
    }

    public sealed class MineBeamData
    {
        public const float DefLength = 20f;

        public Color Color { get; set; } = Color.black;
        public float Width { get; set; } = 0;
        public float Length { get; set; } = 0;
    }

    public sealed class MineLightData
    {
        public Color Color { get; set; } = Color.black;
        public float Intensity { get; set; } = 0;
        public float Range { get; set; } = 0;        
    }
}

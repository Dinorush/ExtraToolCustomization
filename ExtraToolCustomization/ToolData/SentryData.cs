using System.Text.Json.Serialization;
using UnityEngine;

namespace ExtraToolCustomization.ToolData
{
    public sealed class SentryData : IToolData
    {
        public const float DefDeployTime = 3f;

        [JsonIgnore]
        public uint OfflineID { get; set; } = 0;
        [JsonIgnore]
        public uint ItemID { get; set; } = 0;

        public uint ArchetypeID { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public bool BackDamage { get; set; } = false;
        public float DeployTime { get; set; } = 3f;
        public float ScanDelay { get; set; } = 1.5f;
        public float PlacementTime { get; set; } = 0.6f;
        public float PickupTime { get; set; } = 0.6f;
        public Color ScanColor { get; set; } = new Color(0, 0.5373f, 0.3608f);
        public Color TargetColor { get; set; } = new Color(0.9412f, 0.3373f, 0.1f, 0.957f);
    }
}

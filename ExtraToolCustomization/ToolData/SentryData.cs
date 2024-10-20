using System.Text.Json.Serialization;

namespace ExtraToolCustomization.ToolData
{
    public sealed class SentryData : IToolData
    {
        public uint OfflineID { get; set; } = 0;
        [JsonIgnore]
        public uint ItemID { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public bool BackDamage { get; set; } = false;
    }
}

namespace ExtraToolCustomization.ToolData
{
    public sealed class MineData : IToolData
    {
        public uint OfflineID { get; set; } = 0;
        public uint ItemID { get; set; } = 0;
        public string Name { get; set; } = string.Empty;

        public float Delay { get; set; } = 0;
        public float Radius { get; set; } = 0;
        public float DistanceMin { get; set; } = 0;
        public float DistanceMax { get; set; } = 0;
        public float DamageMin { get; set; } = 0;
        public float DamageMax { get; set; } = 0;
        public float Force { get; set; } = 0;
        public float PlacementTime { get; set; } = 0;
    }
}

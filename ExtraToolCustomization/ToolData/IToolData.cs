﻿namespace ExtraToolCustomization.ToolData
{
    public interface IToolData
    {
        public uint OfflineID { get; set; }
        public uint ItemID { get; set; }
        public uint ArchetypeID { get; set; }
        public string Name { get; set; }
    }
}

using System.Collections.Generic;

namespace ExtraToolCustomization.ToolData
{
    internal class ToolDataDict<T> where T : IToolData
    {
        public static string Name = string.Empty;
        public static readonly Dictionary<string, List<T>> FileData = new();
        public static readonly Dictionary<uint, T> OfflineData = new();
        public static readonly Dictionary<uint, T> ItemData = new();
    }
}

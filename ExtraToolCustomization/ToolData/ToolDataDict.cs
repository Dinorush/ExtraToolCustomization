using GTFO.API.Utilities;
using System.Collections.Generic;

namespace ExtraToolCustomization.ToolData
{
    internal static class ToolDataDict<T> where T : IToolData
    {
        public static string Name = string.Empty;
        public static readonly Dictionary<string, List<T>> FileData = new();
        public static readonly Dictionary<uint, T> OfflineData = new();
        public static readonly Dictionary<uint, T> ItemData = new();
        public static readonly Dictionary<uint, T> ArchData = new();
        private static LiveEditListener? _listener;
        
        public static LiveEditListener InitListener(string path)
        {
            _listener = LiveEdit.CreateListener(path, "*.json", true);
            return _listener;
        }
    }
}

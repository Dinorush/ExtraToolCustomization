using GTFO.API.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ExtraToolCustomization.JSON;
using ExtraToolCustomization.ToolData.Templates;
using System.Text;
using System.Collections.Immutable;
using ExtraToolCustomization.Dependencies;

namespace ExtraToolCustomization.ToolData
{
    public sealed class ToolDataManager
    {
        public static readonly ToolDataManager Current = new();

        private void FileChanged<T>(FileEventArgs e) where T : IToolData
        {
            DinoLogger.Warning($"File Changed: {e.FileName}");
            ReadFileContent<T>(e.FullPath, e.ReadContent());
            PrintCustomIDs<T>();
        }

        private void FileDeleted<T>(FileEventArgs e) where T : IToolData
        {
            DinoLogger.Warning($"File Removed: {e.FileName}");

            RemoveFile<T>(e.FullPath);
            PrintCustomIDs<T>();
        }

        private void FileCreated<T>(FileEventArgs e) where T : IToolData
        {
            DinoLogger.Warning($"File Created: {e.FileName}");
            ReadFileContent<T>(e.FullPath, e.ReadContent());
            PrintCustomIDs<T>();
        }

        private void FileRenamed<T>(FileRenamedEventArgs e) where T : IToolData
        {
            DinoLogger.Warning($"File Renamed: {e.OldFileName} -> {e.FileName}");
            if (ToolDataDict<T>.FileData.Remove(e.OldFullPath, out var data))
                ToolDataDict<T>.FileData[e.FullPath] = data;
        }

        private static void ReadFileContent<T>(string file, string content) where T : IToolData
        {
            RemoveFile<T>(file);

            List<T>? dataList = null;
            try
            {
                dataList = TCJson.Deserialize<List<T>>(content);
            }
            catch (JsonException ex)
            {
                DinoLogger.Error($"Error parsing {ToolDataDict<T>.Name} json " + file);
                DinoLogger.Error(ex.Message);
            }

            if (dataList == null) return;

            AddFile(file, dataList);
        }

        private static void RemoveFile<T>(string file) where T : IToolData
        {
            if (!ToolDataDict<T>.FileData.ContainsKey(file)) return;

            foreach (T data in ToolDataDict<T>.FileData[file])
            {
                ToolDataDict<T>.OfflineData.Remove(data.OfflineID);
                ToolDataDict<T>.ItemData.Remove(data.ItemID);
                ToolDataDict<T>.ArchData.Remove(data.ArchetypeID);
            }
            ToolDataDict<T>.FileData.Remove(file);
        }

        private static void AddFile<T>(string file, List<T> dataList) where T : IToolData
        {
            ToolDataDict<T>.FileData.Add(file, dataList);
            foreach (T data in dataList)
            {
                if (data.OfflineID != 0)
                {
                    if (ToolDataDict<T>.OfflineData.ContainsKey(data.OfflineID))
                        DinoLogger.Warning($"Duplicate {ToolDataDict<T>.Name} offline ID {data.OfflineID} detected. Previous name: {ToolDataDict<T>.OfflineData[data.OfflineID].Name}, new name: {data.Name}");
                    ToolDataDict<T>.OfflineData[data.OfflineID] = data;
                }
                if (data.ItemID != 0)
                {
                    if (ToolDataDict<T>.ItemData.ContainsKey(data.ItemID))
                        DinoLogger.Warning($"Duplicate {ToolDataDict<T>.Name} item ID {data.ItemID} detected. Previous name: {ToolDataDict<T>.ItemData[data.ItemID].Name}, new name: {data.Name}");
                    ToolDataDict<T>.ItemData[data.ItemID] = data;
                }
                if (data.ArchetypeID != 0)
                {
                    if (ToolDataDict<T>.ItemData.ContainsKey(data.ArchetypeID))
                        DinoLogger.Warning($"Duplicate {ToolDataDict<T>.Name} item ID {data.ArchetypeID} detected. Previous name: {ToolDataDict<T>.ArchData[data.ArchetypeID].Name}, new name: {data.Name}");
                    ToolDataDict<T>.ArchData[data.ArchetypeID] = data;
                }
            }
        }

        private static void PrintCustomIDs<T>() where T : IToolData
        {
            if (ToolDataDict<T>.OfflineData.Count > 0)
            {
                StringBuilder builder = new($"Found custom blocks for {ToolDataDict<T>.Name} offline IDs: ");
                builder.AppendJoin(", ", ToolDataDict<T>.OfflineData.Keys.ToImmutableSortedSet());
                DinoLogger.Log(builder.ToString());
            }

            if (ToolDataDict<T>.ItemData.Count > 0)
            {
                StringBuilder builder = new($"Found custom blocks for {ToolDataDict<T>.Name} item IDs: ");
                builder.AppendJoin(", ", ToolDataDict<T>.ItemData.Keys.ToImmutableSortedSet());
                DinoLogger.Log(builder.ToString());
            }

            if (ToolDataDict<T>.ArchData.Count > 0)
            {
                StringBuilder builder = new($"Found custom blocks for {ToolDataDict<T>.Name} archetype IDs: ");
                builder.AppendJoin(", ", ToolDataDict<T>.ArchData.Keys.ToImmutableSortedSet());
                DinoLogger.Log(builder.ToString());
            }
        }

        private ToolDataManager()
        {
            string BasePath = Path.Combine(MTFOWrapper.CustomPath, EntryPoint.MODNAME);
            if (!Directory.Exists(BasePath))
                Directory.CreateDirectory(BasePath);

            LoadDirectory("Mine", MineTemplate.Template);
            LoadDirectory("Sentry", SentryTemplate.Template);
        }

        internal void CreateTemplates()
        {
            CreateTemplate(MineTemplate.Template);
            CreateTemplate(SentryTemplate.Template);
        }

        private static void CreateTemplate<T>(params T[] defaultT) where T : IToolData
        {
            string name = ToolDataDict<T>.Name;
            string DirPath = Path.Combine(MTFOWrapper.CustomPath, EntryPoint.MODNAME, name);
            if (!Directory.Exists(DirPath))
            {
                DinoLogger.Log($"No {name} directory detected. Creating template.");
                Directory.CreateDirectory(DirPath);
            }

            string FilePath = Path.Combine(DirPath, "Template.json");
            var file = File.CreateText(FilePath);
            file.WriteLine(TCJson.Serialize(new List<T>(defaultT)));
            file.Flush();
            file.Close();
        }

        private void LoadDirectory<T>(string name, params T[] defaultT) where T : IToolData
        {
            ToolDataDict<T>.Name = name;
            string DirPath = Path.Combine(MTFOWrapper.CustomPath, EntryPoint.MODNAME, name);
            if (!Directory.Exists(DirPath))
                CreateTemplate(defaultT);
            else
                DinoLogger.Log($"{name} directory detected.");

            foreach (string confFile in Directory.EnumerateFiles(DirPath, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                ReadFileContent<T>(confFile, content);
            }
            PrintCustomIDs<T>();

            var sfsw = ToolDataDict<T>.CreateSFSW(DirPath);
            sfsw.OnCreated += FileCreated<T>;
            sfsw.OnChanged += FileChanged<T>;
            sfsw.OnDeleted += FileDeleted<T>;
            sfsw.OnRenamed += FileRenamed<T>;
        }

        internal void Init() { }

        public static T? GetItemData<T>(uint id) where T : IToolData => ToolDataDict<T>.ItemData.GetValueOrDefault(id);
        public static T? GetOfflineData<T>(uint id) where T : IToolData => ToolDataDict<T>.OfflineData.GetValueOrDefault(id);
        public static T? GetArchData<T>(uint id) where T : IToolData => ToolDataDict<T>.ArchData.GetValueOrDefault(id);
        public static T? GetData<T>(uint offlineID, uint itemID, uint archID) where T : IToolData
        {
            T? data = default;
            if (offlineID != 0)
                data = GetOfflineData<T>(offlineID);

            if (data == null && itemID != 0)
                data = GetItemData<T>(itemID);

            if (data == null && archID != 0)
                data = GetArchData<T>(archID);

            return data;
        }
    }
}

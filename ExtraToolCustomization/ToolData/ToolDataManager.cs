using GTFO.API.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ExtraToolCustomization.JSON;
using ExtraToolCustomization.ToolData.Templates;
using ExtraToolCustomization.Utils;
using System.Text;
using System.Collections.Immutable;
using ExtraToolCustomization.Dependencies;

namespace ExtraToolCustomization.ToolData
{
    public sealed class ToolDataManager
    {
        public static readonly ToolDataManager Current = new();

        private readonly LiveEditListener _liveEditListener;

        private void FileChanged<T>(LiveEditEventArgs e) where T : IToolData
        {
            DinoLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ReadFileContent<T>(e.FullPath, content);
                PrintCustomIDs<T>();
            });
        }

        private void FileDeleted<T>(LiveEditEventArgs e) where T : IToolData
        {
            DinoLogger.Warning($"LiveEdit File Removed: {e.FullPath}");

            RemoveFile<T>(e.FullPath);
            PrintCustomIDs<T>();
        }

        private void FileCreated<T>(LiveEditEventArgs e) where T : IToolData
        {
            DinoLogger.Warning($"LiveEdit File Created: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ReadFileContent<T>(e.FullPath, content);
                PrintCustomIDs<T>();
            });
        }

        private void ReadFileContent<T>(string file, string content) where T : IToolData
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
                        DinoLogger.Warning($"Duplicate {ToolDataDict<T>.Name} item ID {data.OfflineID} detected. Previous name: {ToolDataDict<T>.OfflineData[data.OfflineID].Name}, new name: {data.Name}");
                    ToolDataDict<T>.ItemData[data.ItemID] = data;
                }
            }
        }

        private void PrintCustomIDs<T>() where T : IToolData
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
        }

        private ToolDataManager()
        {
            string BasePath = Path.Combine(MTFOWrapper.CustomPath, EntryPoint.MODNAME);
            if (!Directory.Exists(BasePath))
                Directory.CreateDirectory(BasePath);

            _liveEditListener = LiveEdit.CreateListener(BasePath, "*.json", true);
            _liveEditListener.StopListen();

            LoadDirectory("Mine", MineTemplate.Template);
            //LoadDirectory<SentryData>("Sentry");

            _liveEditListener.StartListen();
        }

        private void LoadDirectory<T>(string name, params T[] defaultT) where T : IToolData
        {
            ToolDataDict<T>.Name = name;
            string FilePath = Path.Combine(MTFOWrapper.CustomPath, EntryPoint.MODNAME, name);
            if (!Directory.Exists(FilePath))
            {
                DinoLogger.Log($"No {name} directory detected. Creating {FilePath}/Template.json");
                Directory.CreateDirectory(FilePath);
                var file = File.CreateText(Path.Combine(FilePath, "Template.json"));
                file.WriteLine(TCJson.Serialize(new List<T>(defaultT)));
                file.Flush();
                file.Close();
            }
            else
                DinoLogger.Log($"{name} directory detected. {FilePath}");

            foreach (string confFile in Directory.EnumerateFiles(FilePath, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                ReadFileContent<T>(confFile, content);
            }
            PrintCustomIDs<T>();

            _liveEditListener.FileCreated += FileCreated<T>;
            _liveEditListener.FileChanged += FileChanged<T>;
            _liveEditListener.FileDeleted += FileDeleted<T>;
        }

        internal void Init() { }

        public T? GetItemData<T>(uint id) where T : IToolData => ToolDataDict<T>.ItemData.GetValueOrDefault(id);
        public T? GetOfflineData<T>(uint id) where T : IToolData => ToolDataDict<T>.OfflineData.GetValueOrDefault(id);
    }
}

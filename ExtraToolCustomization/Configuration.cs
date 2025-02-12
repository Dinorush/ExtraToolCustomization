using BepInEx.Configuration;
using BepInEx;
using System.IO;
using GTFO.API.Utilities;
using ExtraToolCustomization.ToolData;

namespace ExtraToolCustomization
{
    internal static class Configuration
    {
        private static ConfigEntry<bool> ForceCreateTemplate { get; set; }

        private readonly static ConfigFile configFile;

        static Configuration()
        {
            configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg"), saveOnInit: true);

            string section = "Tools";
            ForceCreateTemplate = configFile.Bind(section, "Force Create Template", false, "Creates the template files again.");
        }

        internal static void Init()
        {
            LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg", false);
            listener.FileChanged += OnFileChanged;
        }

        private static void OnFileChanged(LiveEditEventArgs _)
        {
            configFile.Reload();
            CheckAndRefreshTemplate();
        }

        private static void CheckAndRefreshTemplate()
        {
            if (ForceCreateTemplate.Value)
            {
                ForceCreateTemplate.Value = false;
                ToolDataManager.Current.CreateTemplates();
                configFile.Save();
            }
        }
    }
}

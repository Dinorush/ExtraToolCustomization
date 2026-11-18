using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ExtraToolCustomization.Dependencies;
using ExtraToolCustomization.ToolData;
using ExtraToolCustomization.Networking.MineDeployer;

namespace ExtraToolCustomization
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.7.2")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MTFOWrapper.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "ExtraToolCustomization";

        public override void Load()
        {
            if (MTFOWrapper.HasMTFO && MTFOWrapper.HasCustomContent)
            {
                new Harmony(MODNAME).PatchAll();
                ToolDataManager.Current.Init();
                MineDeployerManager.Init();
                Configuration.Init();
                DinoLogger.Log("Loaded " + MODNAME);
            }
            else
                DinoLogger.Warning($"MTFO not detected or has no Custom folder, skipping...");
        }
    }
}
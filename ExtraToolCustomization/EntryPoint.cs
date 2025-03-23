using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ExtraToolCustomization.Dependencies;
using ExtraToolCustomization.ToolData;
using ExtraToolCustomization.Patches;
using ExtraToolCustomization.Networking.MineDeployer;

namespace ExtraToolCustomization
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.4.1")]
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
            }
            else
            {
                var harmony = new Harmony(MODNAME);
                harmony.PatchAll(typeof(SentryGunPatches_BurstFix));
                harmony.PatchAll(typeof(ToolAmmoPatches_BugFix));
            }
            Log.LogMessage("Loaded " + MODNAME);

        }
    }
}
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ExtraToolCustomization.Dependencies;
using ExtraToolCustomization.ToolData;
using ExtraToolCustomization.Patches;
using ExtraToolCustomization.Networking.MineDeployer;
using GTFO.API;
using System;

namespace ExtraToolCustomization
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.7.0")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MTFOWrapper.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "ExtraToolCustomization";

        public static event Action? OnCheckpointReached;
        public static event Action? OnCheckpointReloaded;

        internal static void InvokeCheckpointReached() => OnCheckpointReached?.Invoke();
        internal static void InvokeCheckpointReloaded() => OnCheckpointReloaded?.Invoke();

        public override void Load()
        {
            if (MTFOWrapper.HasMTFO && MTFOWrapper.HasCustomContent)
            {
                new Harmony(MODNAME).PatchAll();
                ToolDataManager.Current.Init();
                MineDeployerManager.Init();
                Configuration.Init();
                LevelAPI.OnLevelCleanup += MineDeployerManager.Reset;
                LevelAPI.OnLevelCleanup += CheckpointPatches.OnCleanup;
            }
            else
            {
                var harmony = new Harmony(MODNAME);
                harmony.PatchAll(typeof(SentryGunPatches_AlwaysFix));
                harmony.PatchAll(typeof(ToolAmmoPatches_BugFix));
            }
            Log.LogMessage("Loaded " + MODNAME);

        }
    }
}
﻿using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ExtraToolCustomization.Dependencies;
using ExtraToolCustomization.ToolData;
using ExtraToolCustomization.Patches;

namespace ExtraToolCustomization
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.1.1")]
    [BepInDependency(MTFOWrapper.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "ExtraToolCustomization";

        public override void Load()
        {
            Log.LogMessage("Loading " + MODNAME);
            if (MTFOWrapper.HasMTFO)
            {
                new Harmony(MODNAME).PatchAll();
                ToolDataManager.Current.Init();
            }
            else
                new Harmony(MODNAME).PatchAll(typeof(SentryGunPatches_BurstFix));
            Log.LogMessage("Loaded " + MODNAME);

        }
    }
}
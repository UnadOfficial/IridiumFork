using System.Reflection;
using System.Security;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace Iridium
{
    public static class Main
    {
        public static UnityModManager.ModEntry? Mod { get; private set; }
        public static Harmony? Harmony { get; private set; }
        public static Settings Settings { get; private set; } = null!;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            Logger = Mod.Logger;
            Settings = Settings.Load(modEntry);
            Localization.Load();
            
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = Settings.OnGUI;
            modEntry.OnSaveGUI = Settings.OnSaveGUI;
            
            Harmony = new Harmony(modEntry.Info.Id);
            
            modEntry.Logger.Log(Localization.Get("ModLoaded", Settings.language));
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                modEntry.Logger.Log(Localization.Get("ModEnabled"));
                Harmony?.PatchAll(Assembly.GetExecutingAssembly());
                
                if (Settings.enableOptimizer)
                {
                    Iridium.Patches.OptimizerPatches.ResetDecorOptimization(true);
                }
            }
            else
            {
                modEntry.Logger.Log(Localization.Get("ModDisabled"));
                Harmony?.UnpatchAll(modEntry.Info.Id);
            }
            return true;
        }
    }
}

using System.Reflection;
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
        public static UnityModManager.ModEntry.ModLogger? Logger;
        private static int _mainThreadId;

        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Mod = modEntry;
            Logger = Mod.Logger;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Localization.Load();
            
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = Settings.OnGUI;
            modEntry.OnSaveGUI = Settings.Save;
            
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
                
                if (Settings.optimizer.enableOptimizer)
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

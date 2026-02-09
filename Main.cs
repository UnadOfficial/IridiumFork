using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using Iridium.UI;

namespace Iridium
{
    public static class Main
    {
        public static UnityModManager.ModEntry? Mod { get; private set; }
        public static Harmony? Harmony { get; private set; }
        public static Settings Settings { get; private set; } = null!;
        public static Logger? Logger;
        private static int _mainThreadId;

        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        private static bool _showFirstRunTips = false;
        private static Rect _windowRect = new(Screen.width / 2f - 200, Screen.height / 2f - 100, 400, 200);

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Mod = modEntry;
            Logger = new Logger(Mod.Logger);
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Localization.Load();
            
            if (Settings.firstRun)
            {
                _showFirstRunTips = true;
            }

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = Settings.OnGUI;
            modEntry.OnSaveGUI = Settings.Save;
            modEntry.OnUpdate = OnUpdate;
            
            Harmony = new Harmony(modEntry.Info.Id);
            
            Logger?.Log(Localization.Get("ModLoaded", Settings.language));
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            if (_showFirstRunTips)
            {
                // If UMM is not open, we might want to open it or show the window anyway.
                // But usually, we just show the window.
            }
            Iridium.Patches.AppearancePatches.OnUpdate(dt);
        }

        // We need a way to hook into Unity's OnGUI to show our window.
        // UMM's modEntry.OnGUI is only called inside the UMM menu.
        // To show a popup over the game, we can use a Harmony patch on some game OnGUI or create a GameObject.

        private static GameObject? _uiObject;

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                Logger?.Log(Localization.Get("ModEnabled"));
                Harmony?.PatchAll(Assembly.GetExecutingAssembly());
                
                if (Settings.optimizer.enableOptimizer)
                {
                    Iridium.Patches.OptimizerPatches.ResetDecorOptimization(true);
                }

                if (_showFirstRunTips)
                {
                    if (_uiObject == null)
                    {
                        _uiObject = new GameObject("IridiumUI");
                        _uiObject.AddComponent<IridiumGUI>();
                        Object.DontDestroyOnLoad(_uiObject);
                    }
                }
            }
            else
            {
                Logger?.Log(Localization.Get("ModDisabled"));
                Harmony?.UnpatchAll(modEntry.Info.Id);
                Iridium.Patches.AppearancePatches.Disable();
                
                if (_uiObject != null)
                {
                    Object.Destroy(_uiObject);
                    _uiObject = null;
                }
            }
            return true;
        }

        private class IridiumGUI : MonoBehaviour
        {
            private void OnGUI()
            {
                if (!_showFirstRunTips) return;

                UIUtils.InitializeStyles();
                _windowRect = GUI.Window(999, _windowRect, DrawWindow, Localization.Get("FirstRunTitle"), UIUtils.CardStyle);
            }

            private void DrawWindow(int windowID)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(10);
                GUILayout.Label(Localization.Get("FirstRunMessage"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Localization.Get("Understand"), UIUtils.ButtonStyle))
                {
                    _showFirstRunTips = false;
                    Settings.firstRun = false;
                    Settings.Save(Mod);
                    Destroy(gameObject);
                }
                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }
    }
}

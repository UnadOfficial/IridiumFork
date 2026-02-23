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

        // 当前版本号（用于版本升级检测）
        private static string CurrentVersion
        {
            get
            {
                return VersionManager.GetFullVersionString();
            }
        }

        private static bool _showFirstRunTips = false;
        private static bool _showUpgradeTips = false;
        private static Rect _windowRect = new(Screen.width / 2f - 200, Screen.height / 2f - 100, 400, 200);

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Mod = modEntry;
            Logger = new Logger(Mod.Logger);
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Localization.Load();

            // 检查是否需要显示首次启动提示
            if (Settings.firstRun)
            {
                _showFirstRunTips = true;
            }

            // 检查是否需要显示版本升级提示（从旧版本升级到此版本）
            if (!Settings.firstRun && Settings.lastVersion != CurrentVersion)
            {
                _showUpgradeTips = true;
            }

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = Settings.OnGUI;
            modEntry.OnSaveGUI = Settings.Save;
            modEntry.OnUpdate = OnUpdate;

            Harmony = new Harmony(modEntry.Info.Id);

            Logger?.Log(Localization.Get("ModLoaded", Settings.language));
            return true;
        }

        private static readonly System.Collections.Generic.Queue<System.Action> _actionQueue = new();
        private static readonly object _queueLock = new();

        public static void RunOnMainThread(System.Action action)
        {
            lock (_queueLock)
            {
                _actionQueue.Enqueue(action);
            }
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            if (_actionQueue.Count > 0)
            {
                System.Collections.Generic.List<System.Action> actions = new();
                lock (_queueLock)
                {
                    while (_actionQueue.Count > 0)
                    {
                        actions.Add(_actionQueue.Dequeue());
                    }
                }

                foreach (var action in actions)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (System.Exception e)
                    {
                        Logger?.Log($"[Main] Error in main thread action: {e}");
                    }
                }
            }
        }

        private static GameObject? _uiObject;

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                Logger?.Log(Localization.Get("ModEnabled"));

                // Strategy: Load all, then unload as needed
                Iridium.Patches.PatchManager.ApplyAllPatches();
                Iridium.Patches.PatchManager.UpdateAllPatches();

                if (Settings.optimizer.enableOptimizer)
                {
                    Iridium.Patches.OptimizerPatches.ResetDecorOptimization(true);
                }

                // 如果需要显示弹窗，创建UI对象
                if (_showFirstRunTips || _showUpgradeTips)
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
                Iridium.Patches.PatchManager.UnpatchAll();

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
                UIUtils.InitializeStyles();

                // 先显示首次启动提示
                if (_showFirstRunTips)
                {
                    _windowRect = GUI.Window(998, _windowRect, DrawFirstRunWindow, Localization.Get("FirstRunTitle"), UIUtils.CardStyle);
                    return; // 等待首次提示关闭后再显示升级提示
                }

                // 首次提示关闭后，显示升级提示
                if (_showUpgradeTips)
                {
                    _windowRect = GUI.Window(997, _windowRect, DrawUpgradeWindow, Localization.Get("UpgradeTitle"), UIUtils.CardStyle);
                }
            }

            private void DrawFirstRunWindow(int windowID)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(10);
                GUILayout.Label(Localization.Get("FirstRunMessage"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Localization.Get("Understand"), UIUtils.ButtonStyle))
                {
                    _showFirstRunTips = false;
                    Settings.firstRun = false;
                    Settings.lastVersion = CurrentVersion;
                    if (Mod != null) Settings.Save(Mod);
                    // 如果没有升级提示，销毁UI对象
                    if (!_showUpgradeTips)
                    {
                        Destroy(gameObject);
                    }
                }
                GUILayout.EndVertical();
                GUI.DragWindow();
            }

            private void DrawUpgradeWindow(int windowID)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(10);
                GUILayout.Label(Localization.Get("UpgradeMessage"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Localization.Get("Understand"), UIUtils.ButtonStyle))
                {
                    _showUpgradeTips = false;
                    Settings.lastVersion = CurrentVersion;
                    if (Mod != null) Settings.Save(Mod);
                    Destroy(gameObject);
                }
                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }
    }
}

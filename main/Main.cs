using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Iridium.UI;

namespace Iridium
{
    public static class Main
    {
        public static IHandler? Handler { get; private set; }
        public static Harmony? Harmony { get; private set; }
        public static Settings Settings { get; private set; } = null!;
        public static Logger? Logger;
        private static int _mainThreadId;

        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        // 当前版本号（用于版本升级检测）
        private static string CurrentVersion => VersionManager.GetFullVersionString();

        public static bool Load(dynamic modEntry)
        {
            return Initialize(new UmmHandler(modEntry));
        }

        public static bool Initialize(IHandler handler)
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Handler = handler;
            Logger = new Logger();
            Settings = handler.LoadSettings<Settings>();
            Localization.Load();

            handler.OnToggle += OnToggle;
            handler.OnGUI += () => Settings.OnGUI();
            handler.OnSaveGUI += () => handler.SaveSettings(Settings);
            handler.OnUpdate += OnUpdate;

            Harmony = new Harmony(handler.ModId);

            Logger?.Log(Localization.Get("ModLoaded", Settings.language));
            return true;
        }

        private static readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> _actionQueue = new();
        private static System.Collections.Concurrent.ConcurrentQueue<Object> _destroyImmObj = new();

        public static void RunOnMainThread(System.Action action)
        {
            _actionQueue.Enqueue(action);
        }

        public static void DestroyImmediate(Object obj)
        {
            _destroyImmObj.Enqueue(obj);
        }

        private static void OnUpdate(float dt)
        {
            while (_destroyImmObj.TryDequeue(out var obj))
            {
                Object.DestroyImmediate(obj);
            }
            while (_actionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (System.Exception e)
                {
                    Logger?.Log($"[Main] Error in main thread action: {e}");
                }
            }
            Logger.TaskRun();
        }

        private static void OnToggle(bool value)
        {
            if (value)
            {
                Logger?.Log(Localization.Get("ModEnabled"));

                // 启动异步 Patch 管理器
                Iridium.Patches.AsyncPatchManager.Start();

                // Strategy: Load only what's needed (lazy loading)
                Iridium.Patches.AsyncPatchManager.UpdateAllPatchesAsync();

                if (Main.Settings.optimizer.enableOptimizer)
                {
                    Iridium.Patches.OptimizerPatches.ResetDecorOptimization(true);
                    // 如果DOTween优化已启用，应用设置
                    if (Main.Settings.optimizer.optimizeDOTweenGlobal)
                    {
                        Iridium.Patches.DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }
                }

                // 如果需要显示弹窗，使用 MainWindow
                if (Main.Settings.firstRun)
                {
                    UI.MainWindow.ShowFirstRun();
                }
                else if (Main.Settings.lastVersion != CurrentVersion
                    && (string.IsNullOrEmpty(Main.Settings.lastUpgradeMessageSeen_106_beta5)
                        || Main.Settings.lastUpgradeMessageSeen_106_beta5 != "1.0.6_beta5"))
                {
                    UI.MainWindow.ShowUpgrade("UpgradeMessage_1_0_6_beta5");
                }
            }
            else
            {
                Logger?.Log(Localization.Get("ModDisabled"));

                // 停止异步 Patch 管理器
                Iridium.Patches.AsyncPatchManager.Stop();

                Iridium.Patches.PatchManager.UnpatchAll();
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using ADOFAI;
using DG.Tweening;
using Iridium.UI;

namespace Iridium.Patches
{
    /// <summary>
    /// 专门优化关卡加载和播放性能的 Patch
    /// 解决：1GB+ 内存占用、加载卡顿、MoveTrack/MoveDecoration 性能问题
    /// </summary>
    public static class LoadingOptimizationPatches
    {
        #region Shared State

        // Tween 对象池
        private static readonly Stack<Tween> _tweenPool = new(100);
        private static readonly object _tweenPoolLock = new();

        // 装饰物批处理状态
        private static bool _isBatchCreating = false;

        // 事件预处理缓存
        private static Dictionary<int, List<LevelEvent>>? _floorEventsCache = null;

        #endregion

        #region Event Processing Optimization

        /// <summary>
        /// 事件预处理 - 缓存每个地板的事件列表
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "ApplyEventsToFloors",
            typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
        public static class EventPreprocessingPatch
        {
            [HarmonyPrefix]
            public static void Prefix(List<scrFloor> floors, List<LevelEvent> events)
            {
                if (!Main.Settings.optimizer.cacheFloorEvents) return;

                try
                {
                    // 预处理：构建地板事件缓存
                    _floorEventsCache = new Dictionary<int, List<LevelEvent>>(floors.Count);

                    foreach (var evt in events)
                    {
                        int floorIndex = evt.floor;
                        if (floorIndex < 0 || floorIndex >= floors.Count) continue;

                        if (!_floorEventsCache.TryGetValue(floorIndex, out var list))
                        {
                            list = new List<LevelEvent>();
                            _floorEventsCache[floorIndex] = list;
                        }

                        list.Add(evt);
                    }

                    Main.Logger?.Log($"[LoadingOptimization] Cached events for {_floorEventsCache.Count} floors");
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[LoadingOptimization] Event preprocessing failed: {e}");
                    _floorEventsCache = null;
                }
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!Main.Settings.optimizer.cacheFloorEvents) return;

                // 清理缓存
                _floorEventsCache = null;
            }

            // 提供静态方法供其他代码使用
            public static List<LevelEvent>? GetCachedEvents(int floorIndex)
            {
                return _floorEventsCache?.TryGetValue(floorIndex, out var list) == true ? list : null;
            }
        }

        #endregion

        #region Tween Optimization

        /// <summary>
        /// Tween 对象池 - 减少 MoveTrack/MoveDecoration 的 GC 压力
        /// </summary>
        public static class TweenPoolManager
        {
            public static Tween? GetTween()
            {
                lock (_tweenPoolLock)
                {
                    if (_tweenPool.Count > 0)
                    {
                        return _tweenPool.Pop();
                    }
                }

                // 池中没有，返回 null，DOTween 会自动创建
                return null;
            }

            public static void ReturnTween(Tween tween)
            {
                if (tween == null) return;

                try
                {
                    tween.Kill(false); // 停止但不完成

                    lock (_tweenPoolLock)
                    {
                        if (_tweenPool.Count < 1000) // 限制池大小
                        {
                            _tweenPool.Push(tween);
                        }
                    }
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[LoadingOptimization] Failed to return tween: {e}");
                }
            }

            public static void ClearPool()
            {
                lock (_tweenPoolLock)
                {
                    while (_tweenPool.Count > 0)
                    {
                        var tween = _tweenPool.Pop();
                        tween?.Kill();
                    }
                }
            }
        }

        /// <summary>
        /// 优化 MoveTrack - 减少 Tween 创建
        /// </summary>
        [HarmonyPatch(typeof(ffxMoveFloorPlus), "StartEffect")]
        public static class MoveTrackTweenOptimizationPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ffxMoveFloorPlus __instance)
            {
                if (!Main.Settings.optimizer.optimizeMoveTrackTweens) return;

                // 在 TrackOptimizationPatches 中已有优化
                // 这里可以添加额外的 Tween 池化逻辑
            }
        }

        /// <summary>
        /// 优化 MoveDecoration - 批量处理装饰物动画
        /// </summary>
        [HarmonyPatch(typeof(ffxMoveDecorationsPlus), "StartEffect")]
        public static class MoveDecorationBatchOptimizationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ffxMoveDecorationsPlus __instance)
            {
                if (!Main.Settings.optimizer.batchMoveDecorations) return;

                // 已在 OptimizerPatches.MoveDecorationsOptimizationPatch 中优化
            }
        }

        #endregion

        #region Frame-Spread Decoration Loading

        /// <summary>
        /// 装饰物分帧加载 - 将 UpdateDecorationObjects 中的 CreateDecoration 分散到多帧执行
        /// 防止几千个装饰物在同一帧 Instantiate 导致卡死
        /// 
        /// v2.10.0 专项优化：
        /// - 去掉 reloadDecorations=false 死代码路径（此路径在游戏中从未被调用）
        /// - 时间预算分帧：双限制（最大数量 + 帧时间预算）
        /// - 简化 MoveDecoration 图片预加载，去掉冗余队列直接遍历 events
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "UpdateDecorationObjects")]
        public static class FrameSpreadDecorationLoadingPatch
        {
            private static readonly Queue<LevelEvent> _pendingDecorations = new();
            private static bool _isLoading = false;
            private static readonly List<GraphicRaycaster> _disabledRaycasters = new();
            private static bool _cancelled = false;
            private static scnGame? _pendingGame;
            private static bool _playWasBlocked;
            private static bool _uiCompleted;

            public static bool IsLoading => _isLoading;

            private const float TIME_BUDGET_PER_FRAME = 0.012f;

            [HarmonyPrefix]
            public static bool Prefix(scnGame __instance, bool reloadDecorations)
            {
                if (!Main.Settings.optimizer.enableOptimizer || !Main.Settings.optimizer.frameSpreadDecorationLoading)
                    return true;

                if (_isLoading) return false;

                if (ADOBase.isOfficialLevel) return true;

                if (!reloadDecorations) return true;

                try
                {
                    var decorations = __instance.decorations;
                    if (decorations == null || decorations.Count == 0)
                        return true;

                    int totalActive = 0;
                    foreach (var dec in decorations)
                    {
                        if (dec.active) totalActive++;
                    }

                    if (totalActive < 100)
                        return true;

                    Main.Logger?.Log($"[LoadingOptimization] Frame-spread loading {totalActive} decorations ({decorations.Count} total)");

                    _isLoading = true;
                    _pendingDecorations.Clear();
                    _disabledRaycasters.Clear();

                    foreach (var dec in decorations)
                    {
                        if (dec.active)
                            _pendingDecorations.Enqueue(dec);
                    }

                    BlockUIInput();

                    _pendingGame = __instance;
                    __instance.StartCoroutine(FrameSpreadLoadCoroutine(__instance));
                    return false;
                }
                catch (System.Exception ex)
                {
                    Main.Logger?.Error($"[LoadingOptimization] FrameSpreadDecorationLoading failed: {ex}");
                    CleanupState();
                    return true;
                }
            }

            private static void BlockUIInput()
            {
                try
                {
                    var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                    foreach (var canvas in canvases)
                    {
                        var raycaster = canvas.GetComponent<GraphicRaycaster>();
                        if (raycaster != null && raycaster.enabled)
                        {
                            raycaster.enabled = false;
                            _disabledRaycasters.Add(raycaster);
                        }
                    }
                    Main.Logger?.Log($"[LoadingOptimization] Blocked UI input: disabled {_disabledRaycasters.Count} raycaster(s)");
                }
                catch (Exception ex)
                {
                    Main.Logger?.Error($"[LoadingOptimization] Failed to block UI input: {ex}");
                }
            }

            private static void RestoreUIInput()
            {
                try
                {
                    foreach (var raycaster in _disabledRaycasters)
                    {
                        if (raycaster != null)
                            raycaster.enabled = true;
                    }
                    _disabledRaycasters.Clear();
                }
                catch (Exception ex)
                {
                    Main.Logger?.Error($"[LoadingOptimization] Failed to restore UI input: {ex}");
                }
            }

            public static void Cancel()
        {
            _cancelled = true;
            // User explicitly cancelled — let the UI fade out immediately rather
            // than wait for the min-display window.
            UI.VRAMNotificationUI.Complete(forceImmediate: true);
        }

            private static System.Collections.IEnumerator FrameSpreadLoadCoroutine(scnGame instance)
            {
                int maxPerFrame = Main.Settings.optimizer.decorationsPerFrame;
                if (maxPerFrame < 1) maxPerFrame = 50;

                if (instance == null || instance.decManager == null)
                {
                    CleanupState();
                    yield break;
                }

                instance.decManager.ClearDecorations();

                int processed = 0;
                int total = _pendingDecorations.Count;

                UI.VRAMNotificationUI.ShowPersistent(Localization.Get("LoadingDecorationsProgress", 0, total));
                Main.Logger?.Log($"[LoadingOptimization] Starting frame-spread loading: {total} decorations");

                while (_pendingDecorations.Count > 0 && !_cancelled)
                {
                    if (instance == null || instance.decManager == null)
                    {
                        Main.Logger?.Log($"[LoadingOptimization] scnGame destroyed during loading, aborting");
                        CleanupState();
                        yield break;
                    }

                    float frameStart = Time.realtimeSinceStartup;
                    int batchLimit = Mathf.Min(maxPerFrame, _pendingDecorations.Count);

                    // Add `!_cancelled` to the inner loop condition so a stop-button
                    // click is honored *immediately* — without this, the inner
                    // for-loop keeps running its full batch (up to `decorationsPerFrame`
                    // iterations or until the time budget is hit) before the
                    // outer while-loop's _cancelled check gets a chance to run,
                    // making the cancel feel laggy.
                    for (int i = 0; i < batchLimit && _pendingDecorations.Count > 0 && !_cancelled; i++)
                    {
                        var ev = _pendingDecorations.Dequeue();
                        try
                        {
                            bool spritesLoaded = false;
                            instance.decManager.CreateDecoration(ev, out spritesLoaded);
                        }
                        catch (System.Exception ex)
                        {
                            Main.Logger?.Error($"[LoadingOptimization] Failed to create decoration: {ex}");
                        }
                        processed++;

                        if (Time.realtimeSinceStartup - frameStart > TIME_BUDGET_PER_FRAME)
                            break;
                    }

                    if (_pendingDecorations.Count > 0 && !_cancelled)
                    {
                        UI.VRAMNotificationUI.UpdateProgress(Localization.Get("LoadingDecorationsProgress", processed, total));
                        yield return null;
                    }
                }

                if (_cancelled)
                {
                    Main.Logger?.Log($"[LoadingOptimization] Loading cancelled by user");
                    CleanupState();
                    // UI cleaned up in CleanupState()
                    yield break;
                }

                // MoveDecoration image preloading — collect paths first, then load with yields
                // to avoid piling all synchronous disk IO into a single frame
                var moveDecImages = new List<(string name, string path)>();
                foreach (var evt in instance.events)
                {
                    if (evt.eventType != LevelEventType.MoveDecorations) continue;
                    try
                    {
                        string? output2 = null;
                        if (evt.TryGetAndSet("decorationImage", ref output2, onlyIfEnabled: true) && !output2.IsNullOrEmpty())
                        {
                            string filePath2 = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(instance.levelPath), output2!);
                            moveDecImages.Add((output2!, filePath2));
                        }
                    }
                    catch { }
                }

                if (_cancelled)
                {
                    Main.Logger?.Log($"[LoadingOptimization] Loading cancelled by user");
                    CleanupState();
                    // UI cleaned up in CleanupState()
                    yield break;
                }

                if (moveDecImages.Count > 0)
                {
                    total += moveDecImages.Count;
                    for (int i = 0; i < moveDecImages.Count; i++)
                    {
                        if (_cancelled)
                        {
                            Main.Logger?.Log($"[LoadingOptimization] Loading cancelled by user");
                            // Switch to non-persistent Show() (no Stop button) so the
                            // button doesn't linger during the 0.5s fade-out — the
                            // user already clicked it, there's nothing more to stop.
                            UI.VRAMNotificationUI.Show(Localization.Get("LoadingDecorationsProgress", processed, total));
                            _uiCompleted = true;
                            CleanupState();
                            yield break;
                        }
                        var (name, path) = moveDecImages[i];
                        UI.VRAMNotificationUI.UpdateProgress(Localization.Get("LoadingDecorationsProgress", processed, total));
                        try
                        {
                            LoadResult status;
                            instance.imgHolder.AddSprite(name, path, out status);
                            if (ADOBase.editor != null)
                                ADOBase.editor.UpdateImageLoadResult(name, status);
                        }
                        catch (System.Exception ex)
                        {
                            Main.Logger?.Error($"[LoadingOptimization] Failed to load MoveDecoration image: {ex}");
                        }
                        processed++;
                        yield return null;
                    }
                }

                Main.Logger?.Log($"[LoadingOptimization] Finished loading {processed} decorations across multiple frames");

                if (!Main.Settings.optimizer.dontShowSavedMemory)
                {
                    if (OptimizerPatches.savedVRAM_MB > 0.1f)
                    {
                        // Switch to non-persistent Show() (no Stop button) instead of
                        // UpdateProgress+Complete. UpdateProgress only writes to the
                        // existing Text and never Rebuilds, so the persistent UI's
                        // Stop button would stay visible after the load is already
                        // done — both confusing (nothing to stop) and ugly. Show()
                        // Rebuilds with showStop:false, then Complete can fade it out
                        // on the standard 0.5s in / 2.5s hold / 0.5s out timeline.
                        UI.VRAMNotificationUI.Show(Localization.Get("SavedMemoryMsg", OptimizerPatches.savedVRAM_MB.ToString("F2")));
                        Main.Logger?.Log(Localization.Get("SavedMemoryLog", OptimizerPatches.savedVRAM_MB.ToString("F2")));
                    }
                    else
                    {
                        // Same fix for the "no saved memory" case — we still want
                        // a final non-persistent confirmation without a Stop button.
                        UI.VRAMNotificationUI.Show(Localization.Get("LoadingDecorationsProgress", processed, total));
                    }
                    _uiCompleted = true;
                    OptimizerPatches.VRAMNotificationPatch.isFinished = true;
                }
                else
                {
                    // dontShowSavedMemory = true — we just want the persistent UI
                    // to fade out without showing anything else, so we still
                    // need to switch to non-persistent (no Stop button) before
                    // Complete runs.
                    UI.VRAMNotificationUI.Show(Localization.Get("LoadingDecorationsProgress", processed, total));
                    _uiCompleted = true;
                }

                // ResetDecorations + Play() were about to run synchronously
                // after UpdateDecorationObjects returned, but we intercepted
                // synchronously so they ran on empty data. Replay them now.
                // Critical: ResetDecorations_Patch blocks any ResetDecorations
                // call while _isLoading is true, and the OLD IMGUI version
                // (commit 538c294^, no such patch) had no problem because
                // ResetDecorations was always allowed to run. So we must call
                // CleanupState() FIRST to flip _isLoading off, then call
                // ResetDecorations. The original sequence attempted to keep
                // _isLoading=true through Play() to guard against chain calls,
                // but in practice Play() doesn't re-trigger UpdateDecorationObjects,
                // and a missing ResetDecorations is exactly what was causing
                // "decorations missing" after the popup disappeared.
                var gameToPlay = (_pendingGame != null && _playWasBlocked) ? _pendingGame : null;
                CleanupState();
                if (instance != null && instance.decManager != null)
                    instance.decManager.ResetDecorations();
                gameToPlay?.Play();
            }

            /// <summary>
            /// During frame-spread loading, ResetDecorations is a no-op —
            /// the coroutine calls it after all decorations are created.
            /// </summary>
            [HarmonyPatch(typeof(scrDecorationManager), "ResetDecorations")]
            public static class ResetDecorations_Patch
            {
                [HarmonyPrefix]
                public static bool Prefix()
                {
                    if (_isLoading) return false;
                    return true;
                }
            }

            /// <summary>
            /// Blocks Play() while frame-spread is still running. The coroutine
            /// replays Play() after decorations are fully loaded.
            /// </summary>
            [HarmonyPatch(typeof(scnGame), "Play",
                new Type[] { typeof(int), typeof(bool) })]
            public static class Play_Patch
            {
                [HarmonyPrefix]
                public static bool Prefix()
                {
                    if (_isLoading)
                    {
                        _playWasBlocked = true;
                        return false;
                    }
                    return true;
                }
            }

            private static void CleanupState()
            {
                _isLoading = false;
                _cancelled = false;
                _pendingGame = null;
                _playWasBlocked = false;
                _pendingDecorations.Clear();
                RestoreUIInput();
                if (!_uiCompleted)
                    UI.VRAMNotificationUI.Complete();
                _uiCompleted = false;
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理缓存和对象池
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "OnDestroy")]
        public static class LoadingOptimizationCleanupPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // 只在启用优化时才清理
                if (!Main.Settings.optimizer.enableOptimizer) return;

                _floorEventsCache?.Clear();
                _floorEventsCache = null;
                _isBatchCreating = false;

                TweenPoolManager.ClearPool();

                Main.Logger?.Log("[LoadingOptimization] Cleaned up caches and pools");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 检查是否正在批处理创建装饰物
        /// </summary>
        public static bool IsBatchCreating => _isBatchCreating;

        /// <summary>
        /// 获取缓存的地板事件
        /// </summary>
        public static List<LevelEvent>? GetFloorEvents(int floorIndex)
        {
            return EventPreprocessingPatch.GetCachedEvents(floorIndex);
        }

        #endregion
    }
}

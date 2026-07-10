using Iris.Iml;
using Iridium.Patches;
using UnityEngine;

namespace Iridium.UI.SettingsPanel
{
    internal static class OptimizerSettingsHandlers
    {
        public static void Register(IrisGuiRenderer renderer, Iridium.Settings settings)
        {
            var optimizer = settings.optimizer;
            var memory = settings.memory;

            renderer.RegisterHandler("OnOptimizerToggled", obj =>
            {
                bool value = obj is bool b && b;
                optimizer.enableOptimizer = value;
                QualitySettings.shadows = value && optimizer.disableShadows ? ShadowQuality.Disable : ShadowQuality.All;
                AsyncPatchManager.UpdateOptimizerPatchesAsync();
                settings.Save();
            });

            renderer.RegisterHandler("OnCompressToggled", obj =>
            {
                optimizer.dontCompress = !(obj is bool b && b);
                OptimizerPatches.ResetTextureOptimizationState();
                settings.Save();
            });

            renderer.RegisterHandler("OnShowSavedMemoryToggled", obj =>
            {
                optimizer.dontShowSavedMemory = !(obj is bool b && b);
                settings.Save();
            });

            renderer.RegisterHandler("OnLossyCompressionToggled", obj =>
            {
                optimizer.useLossyCompression = obj is bool b && b;
                OptimizerPatches.ResetTextureOptimizationState();
                settings.Save();
            });

            renderer.RegisterHandler("OnLossyQualityChanged", obj =>
            {
                if (obj is float f)
                {
                    optimizer.lossyQuality = Mathf.Clamp((int)f, 10, 100);
                    OptimizerPatches.ResetTextureOptimizationState();
                    settings.Save();
                }
            });

            renderer.RegisterHandler("OnMultipleOf4Toggled", obj =>
            {
                optimizer.dontResizeMultipleOf4 = !(obj is bool b && b);
                OptimizerPatches.ResetTextureOptimizationState();
                settings.Save();
            });

            renderer.RegisterHandler("OnDivideByChanged", obj =>
            {
                if (obj is float f)
                {
                    optimizer.divideBy = f;
                    OptimizerPatches.ResetTextureOptimizationState();
                    settings.Save();
                }
            });

            renderer.RegisterHandler("OnDontResizeColliderToggled", obj =>
            {
                optimizer.dontResizeCollider = obj is bool b && b;
                settings.Save();
            });

            renderer.RegisterHandler("OnDisableShadowsToggled", obj =>
            {
                optimizer.disableShadows = obj is bool b && b;
                QualitySettings.shadows = optimizer.enableOptimizer && optimizer.disableShadows
                    ? ShadowQuality.Disable
                    : ShadowQuality.All;
                settings.Save();
            });

            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeDecorationUpdateToggled", value => optimizer.optimizeDecorationUpdate = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeTileUpdateToggled", value => optimizer.optimizeTileUpdate = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeMoveTrackToggled", value => optimizer.optimizeMoveTrack = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeRecolorTrackToggled", value => optimizer.optimizeRecolorTrack = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnSkipEventIfPausedToggled", value => optimizer.skipEventIfPaused = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeEventIconsToggled", value => optimizer.optimizeEventIcons = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeScnGameUpdateToggled", value => optimizer.optimizeScnGameUpdate = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeMoveDecorationsToggled", value => optimizer.optimizeMoveDecorations = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeFfxDecorationsToggled", value => optimizer.optimizeFfxDecorations = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeFloorMeshToggled", value => optimizer.optimizeFloorMesh = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeFiltersToggled", value => optimizer.optimizeFilters = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnFastLoadingToggled", value => optimizer.fastLoading = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeParticleToggled", value => optimizer.optimizeParticle = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeParticleInactiveToggled", value => optimizer.optimizeParticleInactive = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeParticleCullingToggled", value => optimizer.optimizeParticleCulling = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeParticleLodToggled", value => optimizer.optimizeParticleLod = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnCacheGameObjectReferencesToggled", value => optimizer.cacheGameObjectReferences = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeEventProcessingToggled", value => optimizer.optimizeEventProcessing = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeEditorMouseDetectionToggled", value => optimizer.optimizeEditorMouseDetection = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeEditorEventIndicatorsToggled", value => optimizer.optimizeEditorEventIndicators = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnCacheFloorEventsToggled", value => optimizer.cacheFloorEvents = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeMoveTrackTweensToggled", value => optimizer.optimizeMoveTrackTweens = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnBatchMoveDecorationsToggled", value => optimizer.batchMoveDecorations = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnIncrementalFloorInsertToggled", value => optimizer.incrementalFloorInsert = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnRangeBasedRedrawToggled", value => optimizer.rangeBasedRedraw = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnSkipRedundantRemakePathToggled", value => optimizer.skipRedundantRemakePath = value);
            RegisterSimpleOptimizerToggle(renderer, settings, "OnOptimizeOffsetFloorEventsToggled", value => optimizer.optimizeOffsetFloorEvents = value);

            renderer.RegisterHandler("OnCustomEasingEngineToggled", obj =>
            {
                optimizer.enableCustomEasingEngine = obj is bool b && b;
                Iridium.Settings.ApplyCustomEasingMutualExclusion(optimizer);
                AsyncPatchManager.UpdateOptimizerPatchesAsync();
                settings.Save();
            });

            renderer.RegisterHandler("OnCustomLevelReadOptimizationToggled", obj =>
            {
                optimizer.customLevelReadOptimization = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JsonPatches.PatchGetCustomLevelName));
                settings.Save();
            });

            renderer.RegisterHandler("OnFrameSpreadDecorationLoadingToggled", obj =>
            {
                optimizer.frameSpreadDecorationLoading = obj is bool b && b;
                AsyncPatchManager.UpdateOptimizerPatchesAsync();
                settings.Save();
            });

            renderer.RegisterHandler("OnDecorationsPerFrameChanged", obj =>
            {
                if (obj is float f)
                {
                    optimizer.decorationsPerFrame = Mathf.Clamp((int)f, 10, 500);
                    settings.Save();
                }
            });

            renderer.RegisterHandler("OnDOTweenGlobalToggled", obj =>
            {
                optimizer.optimizeDOTweenGlobal = obj is bool b && b;
                if (optimizer.optimizeDOTweenGlobal)
                    DOTweenOptimizationPatches.ApplyRuntimeSettings();
                else
                    DOTweenOptimizationPatches.ResetRuntimeSettings();
                settings.Save();
            });

            renderer.RegisterHandler("OnTweenerCapacityChanged", obj =>
            {
                if (obj is float f)
                {
                    optimizer.dotweenTweenerCapacity = Mathf.Clamp((int)f, 200, 2000);
                    DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    settings.Save();
                }
            });

            renderer.RegisterHandler("OnSequenceCapacityChanged", obj =>
            {
                if (obj is float f)
                {
                    optimizer.dotweenSequenceCapacity = Mathf.Clamp((int)f, 50, 500);
                    DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    settings.Save();
                }
            });

            renderer.RegisterHandler("OnDOTweenDefaultRecyclableToggled", obj =>
            {
                optimizer.dotweenDefaultRecyclable = obj is bool b && b;
                DOTweenOptimizationPatches.ApplyRuntimeSettings();
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(TweenSafetyPatches));
                settings.Save();
            });

            renderer.RegisterHandler("OnDOTweenDisableSafeModeToggled", obj =>
            {
                optimizer.dotweenDisableSafeMode = obj is bool b && b;
                DOTweenOptimizationPatches.ApplyRuntimeSettings();
                settings.Save();
            });

            renderer.RegisterHandler("OnExtremeOptimizationToggled", obj =>
            {
                optimizer.enableExtremeOptimization = obj is bool b && b;
                AsyncPatchManager.UpdateOptimizerPatchesAsync();
                settings.Save();
            });

            renderer.RegisterHandler("OnMaxTweensPerFrameChanged", obj =>
            {
                if (obj is float f)
                {
                    optimizer.maxTweensPerFrame = Mathf.Clamp((int)f, 50, 500);
                    settings.Save();
                }
            });

            renderer.RegisterHandler("OnMemoryOptimizationToggled", obj =>
            {
                memory.enableMemoryOptimization = obj is bool b && b;
                settings.Save();
            });

            renderer.RegisterHandler("OnEditorFloorOptimizationToggled", obj =>
            {
                optimizer.enableEditorFloorOptimization = obj is bool b && b;
                AsyncPatchManager.UpdateOptimizerPatchesAsync();
                settings.Save();
            });
        }

        private static void RegisterSimpleOptimizerToggle(
            IrisGuiRenderer renderer,
            Iridium.Settings settings,
            string handlerName,
            System.Action<bool> apply)
        {
            renderer.RegisterHandler(handlerName, obj =>
            {
                apply(obj is bool b && b);
                settings.Save();
            });
        }
    }
}

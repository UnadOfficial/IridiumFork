using System;
using UnityEngine;
using Iridium.UI;
using Iridium.Config;
using Iridium.Patches;
using System.Linq;
using static Iridium.UI.IridiumLayout;

namespace Iridium
{
    public class Settings
    {
        public string language = "en";
        public bool firstRun = true;
        public string? lastVersion = null;
        public string? lastUpgradeMessageSeen_106_beta5 = null;

        public OptimizerSettings optimizer = new();
        public UISettings ui = new();
        public LobbyMusicSettings lobbyMusic = new();
        public MemorySettings memory = new();
        public CompatibilitySettings compatibility = new();
        public HitSoundSettings hitSound = new();
        public JudgeTextSettings judgeText = new();
        public PatchModeSettings patchMode = new();
        public EditorShortcutSettings editorShortcuts = new();

        public string panelToggleHotkey = "Ctrl+F9";

        private string? _defaultLobbyMusicPathCache;
        private string? _fastLobbyMusicPathCache;

        private int _currentTabIndex;
        private Vector2 _contentScrollPosition = Vector2.zero;
        private SizesGroup.Holder _sizesHolder = new();

        private static readonly string[] TabNames = new string[]
        {
            "EnableOptimizer",
            "UISettings",
            "LevelSelectSettings",
            "CompatibilitySettings",
            "HitSoundSettings",
            "EditorShortcuts"
        };

        private int _compatFlashMode = -1;
        private int _compatCamRelMode = -1;
        private bool _isBindingPauseKey = false;
        private int _bindKeyStartFrame = -1;

        private string[] _cachedTabDisplayNames = System.Array.Empty<string>();
        private string _cachedLanguage = "";

        private string[] GetTabDisplayNames()
        {
            if (_cachedLanguage != language || _cachedTabDisplayNames.Length == 0)
            {
                _cachedTabDisplayNames = TabNames.Select(n => Localization.Get(n)).ToArray();
                _cachedLanguage = language;
            }
            return _cachedTabDisplayNames;
        }

		public void OnGUI()
        {
            // Record initial stack depth for exception safety
            int initialStackDepth = IridiumLayout.ContainerStack.Count;

            try
            {
                EnsureTexturesAlive();

                _defaultLobbyMusicPathCache ??= lobbyMusic.defaultMusicPath;
                _fastLobbyMusicPathCache ??= lobbyMusic.fastMusicPath;

                Begin(ContainerDirection.Vertical, ContainerStyle.Padding);
                {
                    Begin(ContainerDirection.Horizontal);
                    {
                        Space(4);
                        Selector(ref _currentTabIndex, GetTabDisplayNames(), options: WidthMin);
                        Fill();
                        Space(4);
                        Text($"Iridium {VersionManager.GetFullVersionString()}", TextStyle.Secondary);
                    }
                    End();

                    Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                    {
                        switch (_currentTabIndex)
                        {
                            case 0: DrawOptimizerTab(); break;
                            case 1: DrawUISettingsTab(); break;
                            case 2: DrawLevelSelectTab(); break;
                            case 3: DrawCompatibilityTab(); break;
                            case 4: DrawHitSoundAndJudgeTextTab(); break;
                            case 5: DrawEditorShortcutsTab(); break;
                        }
                    }
                    End();

                    Space(2);
                    string asyncStatus = AsyncPatchManager.IsProcessing ? "⏳ " + Localization.Get("AsyncPatchProcessing") : "";
                    Text(asyncStatus, TextStyle.Secondary, WidthMax);
                    Space(2);
                    Begin(ContainerDirection.Horizontal);
                    {
                        Fill();
                        foreach (var lang in Localization.AvailableLanguages)
                        {
                            var isCurrent = language == lang;
                            var displayName = Localization.GetDisplayName(lang);
                            if (Button(displayName.ToUpper(), isCurrent ? ButtonStyle.Primary : ButtonStyle.Element, Height(28)))
                                language = lang;
                            Space(2);
                        }
                    }
                    End();
                }
                End();

    			if (GUI.changed) Save();
            }
            catch (Exception ex)
            {
                Main.Logger?.Error($"Settings.OnGUI failed: {ex}");
                throw;
            }
            finally
            {
                // Ensure all containers are closed if an exception occurred mid-draw
                while (IridiumLayout.ContainerStack.Count > initialStackDepth)
                {
                    try
                    {
                        IridiumLayout.End();
                    }
                    catch
                    {
                        // If End() itself fails, break to avoid infinite loop
                        break;
                    }
                }
            }
        }

        #region Optimizer Tab
        private void DrawOptimizerTab()
        {
            var sizes = _sizesHolder.Begin();

            Text(Localization.Get("EnableOptimizer"), TextStyle.Title);
            Separator();

            var prevChanged = GUI.changed;
            GUI.changed = false;
            IridiumPreset.SwitchOption(sizes, ref optimizer.enableOptimizer, "EnableOptimizer");
            if (GUI.changed)
            {
                if (optimizer.enableOptimizer)
                {
                    if (optimizer.disableShadows) QualitySettings.shadows = ShadowQuality.Disable;
                    AsyncPatchManager.UpdateOptimizerPatchesAsync();
                }
                else
                {
                    QualitySettings.shadows = ShadowQuality.All;
                    AsyncPatchManager.UpdateOptimizerPatchesAsync();
                }
            }
            GUI.changed = prevChanged || GUI.changed;

            GUI.enabled = optimizer.enableOptimizer;

            Separator();

            if (OptimizerPatches.savedVRAM_MB > 0.1f)
            {
                IridiumPreset.IconTextFormatted(sizes, IconStyle.Success, "SavedMemoryMsg", OptimizerPatches.savedVRAM_MB.ToString("F2"));
                Separator();
            }

            Text(Localization.Get("ImageOptimizations"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                InvertedSwitchOption(sizes, ref optimizer.dontCompress, "CompressImage");

                bool compressEnabled = !optimizer.dontCompress;
                if (compressEnabled)
                {
                    Separator();
                    InvertedSwitchOption(sizes, ref optimizer.dontShowSavedMemory, "ShowSavedMemory");

                    Separator();
                    IridiumPreset.SwitchOption(sizes, ref optimizer.useLossyCompression, "UseLossyCompression");

                    if (optimizer.useLossyCompression)
                    {
                        Separator();
                        var quality = optimizer.lossyQuality;
                        IridiumPreset.IntOption(sizes, ref quality, "LossyQuality", IntFormat(10, 100));
                        optimizer.lossyQuality = Mathf.Clamp(quality, 10, 100);
                    }

                    Separator();
                    InvertedSwitchOption(sizes, ref optimizer.dontResizeMultipleOf4, "MultipleOf4");
                    if (optimizer.dontCompress) optimizer.dontResizeMultipleOf4 = true;

                    Separator();
                    IridiumPreset.DoubleOption(sizes, ref optimizer.divideBy, "DivideImageBy", DoubleFormat(precision: 1));
                    Separator();
                    InvertedSwitchOption(sizes, ref optimizer.dontResizeCollider, "DontResizeCollider");
                }
            }
            End();
            Separator();

            Text(Localization.Get("RenderingOptimizations"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref optimizer.disableShadows, "DisableShadows");
                if (GUI.changed)
                {
                    if (optimizer.enableOptimizer && optimizer.disableShadows) QualitySettings.shadows = ShadowQuality.Disable;
                    else QualitySettings.shadows = ShadowQuality.All;
                }
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeDecorationUpdate, "OptimizeDecorationUpdate");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeTileUpdate, "OptimizeTileUpdate");
                Separator();
                var prevEnabled = GUI.enabled;
                GUI.enabled = prevEnabled && !optimizer.enableCustomEasingEngine;
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeMoveTrack, "OptimizeMoveTrack");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeRecolorTrack, "OptimizeRecolorTrack");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.skipEventIfPaused, "SkipEventIfPaused");
                if (optimizer.skipEventIfPaused)
                {
                    Separator();
                    IridiumPreset.IconText(sizes, IconStyle.Warning, "SkipEventIfPausedWarning");
                    IridiumPreset.IconText(sizes, IconStyle.Information, "SkipEventIfPausedWarningDetail");
                }
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeEventIcons, "OptimizeEventIcons");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeScnGameUpdate, "OptimizeScnGameUpdate");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeMoveDecorations, "OptimizeMoveDecorations");
                GUI.enabled = prevEnabled;
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeFfxDecorations, "OptimizeFfxDecorations");
                IridiumPreset.IconText(sizes, IconStyle.Warning, "DOTweenOptimizationWarning");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeFloorMesh, "OptimizeFloorMesh");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeFilters, "OptimizeFilters");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.fastLoading, "FastLoading");
			}
			End();
			Separator();

            Text(Localization.Get("CustomEasingEngine"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref optimizer.enableCustomEasingEngine, "EnableCustomEasingEngine");
                if (GUI.changed)
                {
                    ApplyCustomEasingMutualExclusion(optimizer);
                    AsyncPatchManager.UpdateOptimizerPatchesAsync();
                }
                if (optimizer.enableCustomEasingEngine)
                {
                    Separator();
                    IridiumPreset.IconText(sizes, IconStyle.Information, "CustomEasingEngineHint");
                }
            }
            End();
            Separator();

			Text(Localization.Get("ParticleOptimizations"), TextStyle.Subtitle);
			Separator();
			Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
			{
				IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeParticle, "OptimizeParticle");
				if (optimizer.optimizeParticle)
				{
					Separator();
					IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeParticleInactive, "OptimizeParticleInactive");
					Separator();
					IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeParticleCulling, "OptimizeParticleCulling");
					Separator();
					IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeParticleLod, "OptimizeParticleLod");
				}
			}
			End();
			Separator();

			Text(Localization.Get("SceneOptimizations"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                IridiumPreset.SwitchOption(sizes, ref optimizer.cacheGameObjectReferences, "CacheGameObjectReferences");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeEventProcessing, "OptimizeEventProcessing");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeEditorMouseDetection, "OptimizeEditorMouseDetection");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeEditorEventIndicators, "OptimizeEditorEventIndicators");
            }
            End();
            Separator();

            Text(Localization.Get("LoadingOptimizations"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                IridiumPreset.SwitchOption(sizes, ref optimizer.cacheFloorEvents, "CacheFloorEvents");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeMoveTrackTweens, "OptimizeMoveTrackTweens");
                Separator();
                IridiumPreset.SwitchOption(sizes, ref optimizer.batchMoveDecorations, "BatchMoveDecorations");
                Separator();
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref optimizer.customLevelReadOptimization, "CustomLevelReadOptimization");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JsonPatches.PatchGetCustomLevelName));
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref optimizer.frameSpreadDecorationLoading, "FrameSpreadDecorationLoading");
                if (GUI.changed) AsyncPatchManager.UpdateOptimizerPatchesAsync();
                if (optimizer.frameSpreadDecorationLoading)
                {
                    Separator();
                    var decPerFrame = optimizer.decorationsPerFrame;
                    IridiumPreset.IntOption(sizes, ref decPerFrame, "DecorationsPerFrame", IntFormat(10, 500));
                    if (decPerFrame != optimizer.decorationsPerFrame)
                        optimizer.decorationsPerFrame = Mathf.Clamp(decPerFrame, 10, 500);
                    Separator();
                    IridiumPreset.IconText(sizes, IconStyle.Information, "FrameSpreadLoadingHint");
                }
            }
            End();
            Separator();

            Text(Localization.Get("DOTweenOptimizations"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeDOTweenGlobal, "EnableDOTweenOptimization");
                if (GUI.changed)
                {
                    if (optimizer.optimizeDOTweenGlobal)
                        DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    else
                        DOTweenOptimizationPatches.ResetRuntimeSettings();
                }

                if (optimizer.optimizeDOTweenGlobal)
                {
                    Separator();

                    var tweenerCap = optimizer.dotweenTweenerCapacity;
                    IridiumPreset.IntOption(sizes, ref tweenerCap, "TweenerCapacity", IntFormat(200, 2000));
                    if (tweenerCap != optimizer.dotweenTweenerCapacity)
                    {
                        optimizer.dotweenTweenerCapacity = Mathf.Clamp(tweenerCap, 200, 2000);
                        DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }
                    Separator();

                    var seqCap = optimizer.dotweenSequenceCapacity;
                    IridiumPreset.IntOption(sizes, ref seqCap, "SequenceCapacity", IntFormat(50, 500));
                    if (seqCap != optimizer.dotweenSequenceCapacity)
                    {
                        optimizer.dotweenSequenceCapacity = Mathf.Clamp(seqCap, 50, 500);
                        DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }
                    Separator();

                    GUI.changed = false;
                    IridiumPreset.SwitchOption(sizes, ref optimizer.dotweenDefaultRecyclable, "DOTweenDefaultRecyclable");
                    if (GUI.changed)
                    {
                        DOTweenOptimizationPatches.ApplyRuntimeSettings();
                        AsyncPatchManager.UpdatePatchByTypeAsync(typeof(TweenSafetyPatches));
                    }
                    Separator();

                    GUI.changed = false;
                    IridiumPreset.SwitchOption(sizes, ref optimizer.dotweenDisableSafeMode, "DOTweenDisableSafeMode");
                    if (GUI.changed) DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    Separator();

                    IridiumPreset.IconText(sizes, IconStyle.Warning, "DOTweenOptimizationRestartRequired");
                }
                else
                {
                    Separator();
                    IridiumPreset.IconText(sizes, IconStyle.Warning, "DOTweenOptimizationWarning");
                }
            }
            End();
            Separator();

            Text(Localization.Get("ExtremeOptimizations"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref optimizer.enableExtremeOptimization, "EnableExtremeOptimization");
                if (GUI.changed) AsyncPatchManager.UpdateOptimizerPatchesAsync();

                if (optimizer.enableExtremeOptimization)
                {
                    Separator();
                    var maxTweens = optimizer.maxTweensPerFrame;
                    IridiumPreset.IntOption(sizes, ref maxTweens, "MaxTweensPerFrame", IntFormat(50, 500));
                    optimizer.maxTweensPerFrame = Mathf.Clamp(maxTweens, 50, 500);
                    Separator();
                    IridiumPreset.IconText(sizes, IconStyle.Information, "ExtremeOptimizationHint");
                }
            }
            End();
            Separator();

            if (typeof(Notification).GetMethod("SetupNotification", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) == null)
            {
                IridiumPreset.IconText(sizes, IconStyle.Error, "MethodNotFound");
                optimizer.dontShowSavedMemory = true;
                Separator();
            }
            if (typeof(scrVisualDecoration).GetProperty("spriteUnscaledSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) == null)
            {
                IridiumPreset.IconText(sizes, IconStyle.Error, "PropertyNotFound");
                optimizer.dontResizeCollider = true;
                Separator();
            }

            Text(Localization.Get("MemorySettings"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref memory.enableMemoryOptimization, "MemorySettings");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.CleanOnSceneSwitchPatch));

                if (memory.enableMemoryOptimization)
                {
                    Separator();
                    GUI.changed = false;
                    IridiumPreset.SwitchOption(sizes, ref memory.cleanOnSceneSwitch, "CleanOnSceneSwitch");
                    if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.CleanOnSceneSwitchPatch));
                }
            }
            End();
            Separator();

            // --- Editor Floor Optimizations ---
            Text(Localization.Get("EditorFloorOptimizations"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref optimizer.enableEditorFloorOptimization, "EnableEditorFloorOptimization");
                if (GUI.changed) AsyncPatchManager.UpdateOptimizerPatchesAsync();

                if (optimizer.enableEditorFloorOptimization)
                {
                    Separator();
                    IridiumPreset.SwitchOption(sizes, ref optimizer.incrementalFloorInsert, "IncrementalFloorInsert");
                    if (optimizer.incrementalFloorInsert)
                    {
                        Separator();
                        IridiumPreset.SwitchOption(sizes, ref optimizer.rangeBasedRedraw, "RangeBasedRedraw");
                        Separator();
                        IridiumPreset.SwitchOption(sizes, ref optimizer.skipRedundantRemakePath, "SkipRedundantRemakePath");
                        Separator();
                        IridiumPreset.SwitchOption(sizes, ref optimizer.optimizeOffsetFloorEvents, "OptimizeOffsetFloorEvents");
                    }
                    Separator();
                    IridiumPreset.IconText(sizes, IconStyle.Warning, "EditorFloorOptimizationWarning");
                }
            }
            End();

            GUI.enabled = true;
        }
        #endregion

        #region UI Settings Tab
        private void DrawUISettingsTab()
        {
            var sizes = _sizesHolder.Begin();

            Text(Localization.Get("UISettings"), TextStyle.Title);
            Separator();

            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref ui.removeNews, "RemoveNews");
                if (GUI.changed)
                {
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.RemoveNewsPatch));
                    MiscPatches.RemoveNewsPatch.UpdateNews();
                }
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref ui.hideBetaWatermark, "HideBetaWatermark");
                if (GUI.changed)
                {
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.HideBetaWatermarkPatch));
                    MiscPatches.RefreshBetaWatermark();
                }
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref ui.forceDifficultyUI, "ForceDifficultyUI");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.ForceDifficultyUIPatch));
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref ui.alwaysCountdown, "AlwaysCountdown");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.AlwaysCountdownPatch));
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref ui.enablePausePlanetTrail, "EnablePausePlanetTrail");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(PausePlanetTrailPatch));
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref ui.moveAutoplayText, "MoveAutoplayText");
                if (GUI.changed)
                {
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.AutoplayTextPositionPatch));
                    MiscPatches.RefreshAutoplayTextPosition();
                }

                if (ui.moveAutoplayText)
                {
                    Separator();
                    Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
                    PushAlign(0.5);
                    {
                        Text("X:", TextStyle.Normal, WidthMin);
                        ui.autoplayTextX = GUILayout.HorizontalSlider(ui.autoplayTextX, -Screen.width / 2f, Screen.width / 2f);
                        Text(ui.autoplayTextX.ToString("F0"), TextStyle.Secondary, Width(40));
                    }
                    PopAlign();
                    End();

                    Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
                    PushAlign(0.5);
                    {
                        Text("Y:", TextStyle.Normal, WidthMin);
                        ui.autoplayTextY = GUILayout.HorizontalSlider(ui.autoplayTextY, -Screen.height / 2f, Screen.height / 2f);
                        Text(ui.autoplayTextY.ToString("F0"), TextStyle.Secondary, Width(40));
                    }
                    PopAlign();
                    End();

                    if (GUI.changed) MiscPatches.RefreshAutoplayTextPosition();
                }
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref ui.enableCircleArc, "EnableCircleArc");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.CircleArcPatch));
                if (ui.enableCircleArc)
                {
                    Separator();
                    IridiumPreset.IconText(sizes, IconStyle.Warning, "RestartRequired");
                }
            }
            End();
        }
        #endregion

        #region Level Select Tab
        private void DrawLevelSelectTab()
        {
            var sizes = _sizesHolder.Begin();

            Text(Localization.Get("LevelSelectSettings"), TextStyle.Title);
            Separator();

            GUI.changed = false;
            IridiumPreset.SwitchOption(sizes, ref lobbyMusic.enableLobbyMusicPatch, "LevelSelectSettings");
            if (GUI.changed)
            {
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.LobbyMusicPatch));
                if (lobbyMusic.enableLobbyMusicPatch) MiscPatches.LobbyMusicPatch.ReloadFromSettings();
            }

            GUI.enabled = lobbyMusic.enableLobbyMusicPatch;

            Separator();

            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                IridiumPreset.SwitchOption(sizes, ref lobbyMusic.enableCustomBpm, "EnableCustomBpm");

                if (lobbyMusic.enableCustomBpm)
                {
                    Separator();
                    var customBpmVal = (double)lobbyMusic.customBpm;
                    IridiumPreset.DoubleOption(sizes, ref customBpmVal, "CustomBpm", DoubleFormat(precision: 1));
                    lobbyMusic.customBpm = (float)customBpmVal;
                }

                Separator();
                IridiumPreset.SwitchOption(sizes, ref lobbyMusic.fastMusic, "LobbyFastMusic");
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref lobbyMusic.customMusic, "LobbyCustomMusic");
                if (GUI.changed) MiscPatches.LobbyMusicPatch.ReloadFromSettings();

                if (lobbyMusic.customMusic)
                {
                    Separator();

                    Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
                    PushAlign(0.5);
                    {
                        Text(Localization.Get("LobbyDefaultMusicPath"), options: WidthMin);
                        Fill();
                        TextField(ref _defaultLobbyMusicPathCache, options: Width(200));
                        Space(4);
                        if (Button(Localization.Get("Apply"), ButtonStyle.Element, Width(60)))
                        {
                            lobbyMusic.defaultMusicPath = (_defaultLobbyMusicPathCache ?? string.Empty).Trim();
                            MiscPatches.LobbyMusicPatch.StartLoad(true, lobbyMusic.defaultMusicPath);
                        }
                    }
                    PopAlign();
                    End();

                    Separator();

                    Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
                    PushAlign(0.5);
                    {
                        Text(Localization.Get("LobbyFastMusicPath"), options: WidthMin);
                        Fill();
                        TextField(ref _fastLobbyMusicPathCache, options: Width(200));
                        Space(4);
                        if (Button(Localization.Get("Apply"), ButtonStyle.Element, Width(60)))
                        {
                            lobbyMusic.fastMusicPath = (_fastLobbyMusicPathCache ?? string.Empty).Trim();
                            MiscPatches.LobbyMusicPatch.StartLoad(false, lobbyMusic.fastMusicPath);
                        }
                    }
                    PopAlign();
                    End();

                    Separator();

                    Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
                    {
                        Fill();
                        if (Button(Localization.Get("LobbyReloadMusic"), ButtonStyle.Element, Width(140)))
                            MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                    }
                    End();

                    Separator();
                    IridiumPreset.IconText(sizes, IconStyle.Information, "LobbyMusicHint");
                }
            }
            End();
            GUI.enabled = true;
        }
        #endregion

        #region Compatibility Tab
        private void DrawCompatibilityTab()
        {
            if (_compatFlashMode < 0) _compatFlashMode = (int)compatibility.legacyFlashMode;
            if (_compatCamRelMode < 0) _compatCamRelMode = (int)compatibility.legacyCamRelativeToMode;

            var sizes = _sizesHolder.Begin();

            Text(Localization.Get("CompatibilitySettings"), TextStyle.Title);
            Separator();

            Text(Localization.Get("CompatibleBehavior"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.enableLegacyPauseFix, "EnableLegacyPauseFix");
                if (GUI.changed)
                {
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(CompatibilityPatches.LegacyPauseFixPatch_Play));
                }
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.enableNoFailTooEarly, "EnableNoFailTooEarly");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(CompatibilityPatches.NoFailTooEarlyPatch));
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.scaleFilterSpeedWithPitch, "ScaleFilterSpeedWithPitch");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(CompatibilityPatches.ScaleFilterSpeedWithPitchPatch));

                IridiumPreset.SwitchOption(sizes, ref compatibility.editorPauseAllowed, "EditorPauseAllowed");
                if (compatibility.editorPauseAllowed)
                {
                    Separator();
                    IridiumPreset.SwitchOption(sizes, ref compatibility.editorPauseEnabled, "EditorPauseEnabled");
                    if (compatibility.editorPauseEnabled)
                    {
                        Separator();
                        DrawPauseKeyBinding(sizes);
                        Separator();
                        IridiumPreset.IconText(sizes, IconStyle.Information, "EditorPauseKeyHint");
                    }
                }

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.fixCameraRelativeDrag, "FixCameraRelativeDrag");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(CameraRelativeDragPatches));
            }
            End();
            Separator();

            Text(Localization.Get("BugFixes"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.portalTravelFix, "PortalTravelFix");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.PortalTravelFixPatch));
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.fixEditorPlayResetMistakes, "FixEditorPlayResetMistakes");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.EditorPlayResetMistakesPatch));
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.fixTurnaroundCondition, "FixTurnaroundCondition");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.TurnaroundConditionFix));
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.fixJudgeRotation, "FixJudgeRotation");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.HitTextMeshShowRotationFixPatch));
                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.fixCoopPauseLock, "FixCoopPauseLock");
                if (GUI.changed)
                {
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.CoopPauseLockFix));
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.CoopPauseHandleLockFix));
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.CoopPlayerHitFix));
                }
                Separator();

            }
            End();
            Separator();

            Text(Localization.Get("LegacyLevelBehavior"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref compatibility.forceAngleData, "ForceAngleData");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JsonPatches.ForceAngleDataPatch));
                Separator();

                IridiumPreset.SelectorOption(
                    sizes,
                    ref _compatFlashMode,
                    new string[] { Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn") },
                    "LegacyFlashMode");
                Separator();

                IridiumPreset.SelectorOption(
                    sizes,
                    ref _compatCamRelMode,
                    new string[] { Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn") },
                    "LegacyCamRelativeToMode");

                var newFlashMode = (LegacyBehaviorMode)_compatFlashMode;
                var newCamRelMode = (LegacyBehaviorMode)_compatCamRelMode;

                if (newFlashMode != compatibility.legacyFlashMode || newCamRelMode != compatibility.legacyCamRelativeToMode)
                {
                    compatibility.legacyFlashMode = newFlashMode;
                    compatibility.legacyCamRelativeToMode = newCamRelMode;
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JsonPatches.LegacyBehaviorPatch));
                }
            }
            End();
            Separator();

            Text(Localization.Get("PatchMode"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref patchMode.useILPatch, "UseILPatch");
                if (GUI.changed) Core.BasePatchMethod.SyncILModeFromSettings();
                Separator();

                if (patchMode.useILPatch)
                    IridiumPreset.IconText(sizes, IconStyle.Information, "UseILPatchHint");
                else
                    IridiumPreset.IconText(sizes, IconStyle.Information, "UsePrefixPostfixHint");
            }
            End();
        }
        #endregion

        #region HitSound & JudgeText Tab
        private void DrawHitSoundAndJudgeTextTab()
        {
            var sizes = _sizesHolder.Begin();

            Text(Localization.Get("HitSoundSettings"), TextStyle.Title);
            Separator();

            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref hitSound.enableHitSoundPitch, "EnableHitSoundPitch");
                if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(HitSoundPatch));
            }
            End();
            Separator();

            Text(Localization.Get("JudgeTextSettings"), TextStyle.Subtitle);
            Separator();
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref judgeText.enableJudgeTextCustomization, "JudgeTextSettings");
                if (GUI.changed)
                {
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JudgeTextPatches.HitTextMeshInitPatch));
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JudgeTextPatches.HitTextMeshShowPatch));
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JudgeTextPatches.ResetTimingOnRewindPatch));
                }

                GUI.enabled = judgeText.enableJudgeTextCustomization;

                Separator();

                GUI.changed = false;
                IridiumPreset.SwitchOption(sizes, ref judgeText.showAsOffset, "ShowAsOffset");
                if (GUI.changed)
                {
                    AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JudgeTextPatches.HitTextMeshShowPatch));
                }

                Separator();

                GUI.enabled = !judgeText.showAsOffset && judgeText.enableJudgeTextCustomization;

                Text(Localization.Get("CustomJudgeText"), TextStyle.Normal);
                Separator();

                DrawJudgeTextInput(sizes, "TooEarly", ref judgeText.tooEarly);
                Separator();
                DrawJudgeTextInput(sizes, "VeryEarly", ref judgeText.veryEarly);
                Separator();
                DrawJudgeTextInput(sizes, "EarlyPerfect", ref judgeText.earlyPerfect);
                Separator();
                DrawJudgeTextInput(sizes, "Perfect", ref judgeText.perfect);
                Separator();
                DrawJudgeTextInput(sizes, "LatePerfect", ref judgeText.latePerfect);
                Separator();
                DrawJudgeTextInput(sizes, "VeryLate", ref judgeText.veryLate);
                Separator();
                DrawJudgeTextInput(sizes, "TooLate", ref judgeText.tooLate);
                Separator();
                DrawJudgeTextInput(sizes, "Multipress", ref judgeText.multipress);
                Separator();
                DrawJudgeTextInput(sizes, "FailMiss", ref judgeText.failMiss);
                Separator();
                DrawJudgeTextInput(sizes, "FailOverload", ref judgeText.failOverload);

                GUI.enabled = true;

                Separator();

                Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
                {
                    Fill();
                    if (Button(Localization.Get("ResetJudgeText"), ButtonStyle.Element, Width(120)))
                        judgeText.ResetToDefault();
                }
                End();
            }
            End();
        }

        private void DrawJudgeTextInput(Sizes sizes, string key, ref string value)
        {
            Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
            PushAlign(0.5);
            {
                Text(Localization.Get($"JudgeText_{key}"), options: WidthMin);
                Fill();
                string? nullableValue = value;
                TextField(ref nullableValue, 128, Width(120));
                if (nullableValue != null) value = nullableValue;
            }
            PopAlign();
            End();
        }
        #endregion

        #region Helpers
        private void InvertedSwitchOption(Sizes sizes, ref bool invertedOption, string name)
        {
            var displayValue = !invertedOption;
            Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
            PushAlign(0.5);
            {
                Text(Localization.Get(name), options: WidthMin);
                Fill();
                var result = Switch(ref displayValue);
                if (result != null)
                {
                    invertedOption = !displayValue;
                }
            }
            PopAlign();
            End();
        }

        private void DrawPauseKeyBinding(Sizes sizes)
        {
            string display = _isBindingPauseKey
                ? $"< {Localization.Get("EditorPauseKeyPress")} >"
                : GetKeyDisplay(compatibility.editorPauseKey, compatibility.editorPauseModifiers);

            Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
            PushAlign(0.5);
            {
                Text(Localization.Get("EditorPauseKey"), options: WidthMin);
                Fill();
                if (GUILayout.Button(display, GUILayout.Width(160)))
                {
                    _isBindingPauseKey = true;
                    _bindKeyStartFrame = Time.frameCount;
                }
                if (GUILayout.Button("+", GUILayout.Width(28)))
                {
                    AddModifier();
                }
                if (GUILayout.Button("-", GUILayout.Width(28)))
                {
                    RemoveModifier();
                }
            }
            PopAlign();
            End();

            if (_isBindingPauseKey)
            {
                var e = Event.current;
                if (e.type == EventType.KeyDown)
                {
                    compatibility.editorPauseKey = (int)e.keyCode;
                    _isBindingPauseKey = false;
                    e.Use();
                }
                else if (e.type == EventType.MouseDown && Time.frameCount != _bindKeyStartFrame)
                {
                    _isBindingPauseKey = false;
                }
            }
        }

        private static readonly int[] _modifierBits = { 1, 4, 2, 8 }; // Ctrl, Shift, Alt, Win

        private void AddModifier()
        {
            int mods = compatibility.editorPauseModifiers;
            foreach (int bit in _modifierBits)
            {
                if ((mods & bit) == 0)
                {
                    compatibility.editorPauseModifiers = mods | bit;
                    return;
                }
            }
            compatibility.editorPauseModifiers = 0;
        }

        private void RemoveModifier()
        {
            int mods = compatibility.editorPauseModifiers;
            for (int i = _modifierBits.Length - 1; i >= 0; i--)
            {
                int bit = _modifierBits[i];
                if ((mods & bit) != 0)
                {
                    compatibility.editorPauseModifiers = mods & ~bit;
                    return;
                }
            }
        }

        private static string GetKeyDisplay(int key, int mods)
        {
            string result = "";
            if ((mods & 1) != 0) result += "Ctrl+";
            if ((mods & 4) != 0) result += "Shift+";
            if ((mods & 2) != 0) result += "Alt+";
            if ((mods & 8) != 0) result += "Win+";
            result += ((KeyCode)key).ToString();
            return result;
        }
        #endregion

        #region Editor Shortcuts Tab
        private bool _isBindingShortcutKey = false;
        private int _bindingShortcutIndex = -1;
        private int _shortcutBindKeyStartFrame = -1;

        private void DrawEditorShortcutsTab()
        {
            var sizes = _sizesHolder.Begin();
            var s = editorShortcuts;

            Text(Localization.Get("EditorShortcuts"), TextStyle.Title);
            Separator();

            GUI.changed = false;
            IridiumPreset.SwitchOption(sizes, ref s.enableEditorShortcuts, "EnableEditorShortcuts");
            if (GUI.changed) AsyncPatchManager.UpdatePatchByTypeAsync(typeof(EditorShortcutPatches.EditorShortcutUpdatePatch));

            if (s.enableEditorShortcuts)
            {
                int idx = 0;

                Separator();
                Text(Localization.Get("EditorShortcutsDecoration"), TextStyle.Subtitle);
                Separator();
                Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                {
                    DrawShortcutKeyBinding(sizes, idx++, "ShortcutSelectAll",
                        ref s.selectAllKey, ref s.selectAllModifiers);
                    Separator();
                    DrawShortcutKeyBinding(sizes, idx++, "ShortcutDeselectAll",
                        ref s.deselectAllKey, ref s.deselectAllModifiers);
                    Separator();
                    DrawShortcutKeyBinding(sizes, idx++, "ShortcutToggleVisibility",
                        ref s.toggleVisibilityKey, ref s.toggleVisibilityModifiers);
                    Separator();
                    DrawShortcutKeyBinding(sizes, idx++, "ShortcutFocusDecoration",
                        ref s.focusDecorationKey, ref s.focusDecorationModifiers);
                }
                End();

                Separator();
                Text(Localization.Get("EditorShortcutsNavigation"), TextStyle.Subtitle);
                Separator();
                Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                {
                    DrawShortcutKeyBinding(sizes, idx++, "ShortcutGoToFloor",
                        ref s.goToFloorKey, ref s.goToFloorModifiers);
                    Separator();
                    IridiumPreset.SwitchOption(sizes, ref s.cameraFollowOnFloorSelect, "CameraFollowOnFloorSelect");
                    Separator();
                    DrawShortcutKeyBinding(sizes, idx++, "ShortcutSelectAllFloors",
                        ref s.selectAllFloorsKey, ref s.selectAllFloorsModifiers);
                }
                End();

                Separator();
                Text(Localization.Get("EditorShortcutsPopup"), TextStyle.Subtitle);
                Separator();
                Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                {
                    DrawShortcutKeyBinding(sizes, idx++, "ShortcutPopupSave",
                        ref s.popupSaveKey, ref s.popupSaveModifiers);
                    Separator();
                    DrawShortcutKeyBinding(sizes, idx++, "ShortcutPopupDiscard",
                        ref s.popupDiscardKey, ref s.popupDiscardModifiers);
                }
                End();

                Separator();
                IridiumPreset.IconText(sizes, IconStyle.Information, "EditorShortcutsHint");
            }
        }

        private void DrawShortcutKeyBinding(Sizes sizes, int index, string name,
            ref int key, ref int modifiers)
        {
            string display = (_isBindingShortcutKey && _bindingShortcutIndex == index)
                ? Localization.Get("EditorPauseKeyPress")
                : EditorShortcutPatches.GetKeyDisplay(key, modifiers);

            Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
            PushAlign(0.5);
            {
                Text(Localization.Get(name), options: WidthMin);
                Fill();

                if (Button(display, ButtonStyle.Element, Width(160)))
                {
                    _isBindingShortcutKey = true;
                    _bindingShortcutIndex = index;
                    _shortcutBindKeyStartFrame = Time.frameCount;
                }

                string modLabel = GetShortcutModifierLabel(modifiers);
                if (Button(modLabel, ButtonStyle.Element, Width(60)))
                {
                    modifiers = EditorShortcutPatches.CycleModifier(modifiers);
                }
            }
            PopAlign();
            End();

            if (_isBindingShortcutKey && _bindingShortcutIndex == index)
            {
                var e = Event.current;
                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode != KeyCode.None && e.keyCode != KeyCode.Escape)
                    {
                        key = (int)e.keyCode;
                        _isBindingShortcutKey = false;
                        _bindingShortcutIndex = -1;
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Escape)
                    {
                        _isBindingShortcutKey = false;
                        _bindingShortcutIndex = -1;
                        e.Use();
                    }
                }
                else if (e.type == EventType.MouseDown && Time.frameCount != _shortcutBindKeyStartFrame)
                {
                    _isBindingShortcutKey = false;
                    _bindingShortcutIndex = -1;
                }
            }
        }

        private static string GetShortcutModifierLabel(int mods)
        {
            if (mods == 0) return "---";
            string result = "";
            if ((mods & EditorShortcutPatches.MOD_CTRL) != 0) result += "C";
            if ((mods & EditorShortcutPatches.MOD_ALT) != 0) result += "A";
            if ((mods & EditorShortcutPatches.MOD_SHIFT) != 0) result += "S";
            return result;
        }
        #endregion

		public void Save()
		{
			Main.Handler?.SaveSettings(this);
		}

        /// <summary>
        /// 启动时检测自定义缓速引擎与三个 Patch 的冲突。
        /// 如果引擎和任一 Patch 同时开启，强制关闭引擎并保存配置。
        /// </summary>
        public static void ValidateCustomEasingConflict(Settings settings)
        {
            if (!settings.optimizer.enableCustomEasingEngine) return;

            bool hasConflict = settings.optimizer.optimizeMoveTrack
                            || settings.optimizer.optimizeRecolorTrack
                            || settings.optimizer.optimizeMoveDecorations;

            if (hasConflict)
            {
                settings.optimizer.enableCustomEasingEngine = false;
                Main.Handler?.SaveSettings(settings);
                Main.Logger?.Warning(Localization.Get("CustomEasingEngineConflictDetected"));
            }
        }

        private static void ApplyCustomEasingMutualExclusion(OptimizerSettings opt)
        {
            // 启用缓速引擎 → 关闭三个冲突 Patch
            if (opt.enableCustomEasingEngine)
            {
                bool changed = false;
                if (opt.optimizeMoveTrack) { opt.optimizeMoveTrack = false; changed = true; }
                if (opt.optimizeRecolorTrack) { opt.optimizeRecolorTrack = false; changed = true; }
                if (opt.optimizeMoveDecorations) { opt.optimizeMoveDecorations = false; changed = true; }
                if (changed) AsyncPatchManager.UpdateOptimizerPatchesAsync();
            }
        }
    }
}
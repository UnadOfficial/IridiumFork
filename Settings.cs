using System;
using UnityModManagerNet;
using UnityEngine;
using Iridium.UI;
using Iridium.Config;
using Iridium.Patches;

namespace Iridium
{
    public class Settings : UnityModManager.ModSettings
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

        private string? _defaultLobbyMusicPathCache;
        private string? _fastLobbyMusicPathCache;

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UIUtils.InitializeStyles();

            _defaultLobbyMusicPathCache ??= lobbyMusic.defaultMusicPath;
            _fastLobbyMusicPathCache ??= lobbyMusic.fastMusicPath;

            // 顶部：语言选择
            DrawLanguageSection();

            GUILayout.Space(4);

            // 三列布局
            GUILayout.BeginHorizontal();

            // === 左列：核心优化 ===
            GUILayout.BeginVertical(GUILayout.Width(320));
            DrawOptimizerSection();
            DrawMemorySection();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // === 中列：UI 和外观 ===
            GUILayout.BeginVertical(GUILayout.Width(320));
            DrawUISection();
            DrawLobbyMusicSection();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // === 右列：高级设置 ===
            GUILayout.BeginVertical(GUILayout.Width(320));
            DrawCompatibilitySection();
            DrawHitSoundSection();
            DrawJudgeTextSection();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // 底部版本信息
            GUILayout.Space(8);
            DrawVersionInfo();

            if (GUI.changed)
            {
                Save(modEntry);
            }
        }

        private void DrawLanguageSection()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            
            var langs = Localization.AvailableLanguages;
            foreach (var lang in langs)
            {
                bool isCurrent = language == lang;
                GUIStyle btnStyle = isCurrent ? UIUtils.ButtonPrimaryStyle : UIUtils.ButtonStyle;
                string displayName = Localization.GetDisplayName(lang);
                if (GUILayout.Button(displayName.ToUpper(), btnStyle, GUILayout.Height(32), GUILayout.ExpandWidth(true)))
                {
                    language = lang;
                }
                if (lang != langs[langs.Length - 1]) GUILayout.Space(4);
            }
            
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawOptimizerSection()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("EnableOptimizer"), UIUtils.HeaderStyle);

            bool newEnable = UIUtils.M3Switch(optimizer.enableOptimizer, "");
            if (newEnable != optimizer.enableOptimizer)
            {
                optimizer.enableOptimizer = newEnable;
                if (optimizer.enableOptimizer && optimizer.disableShadows) 
                    QualitySettings.shadows = ShadowQuality.Disable;
                else 
                    QualitySettings.shadows = ShadowQuality.All;
                PatchManager.UpdateOptimizerPatches();
            }

            if (optimizer.enableOptimizer)
            {
                if (OptimizerPatches.savedVRAM_MB > 0.1f)
                {
                    UIUtils.DrawInfoBox("✨ " + Localization.Get("SavedMemoryMsg", OptimizerPatches.savedVRAM_MB.ToString("F2")));
                }

                // 图像压缩设置
                DrawCompactSwitch(ref optimizer.dontShowSavedMemory, Localization.Get("ShowSavedMemory"), true);
                DrawCompactSwitch(ref optimizer.dontCompress, Localization.Get("CompressImage"), true);
                DrawCompactSwitch(ref optimizer.dontResizeMultipleOf4, Localization.Get("MultipleOf4"), true);
                
                if (optimizer.dontCompress) optimizer.dontResizeMultipleOf4 = true;

                // 压缩比例
                GUILayout.BeginHorizontal(GUILayout.Height(36));
                GUILayout.Label(Localization.Get("DivideImageBy"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                string divideByStr = GUILayout.TextField(optimizer.divideBy.ToString("F1"), 5, UIUtils.TextFieldStyle, GUILayout.Width(60));
                if (double.TryParse(divideByStr, out double newDivideBy)) optimizer.divideBy = newDivideBy;
                GUILayout.EndHorizontal();

                DrawCompactSwitch(ref optimizer.dontResizeCollider, Localization.Get("DontResizeCollider"), true);

                bool newShadows = UIUtils.M3Switch(optimizer.disableShadows, Localization.Get("DisableShadows"));
                if (newShadows != optimizer.disableShadows)
                {
                    optimizer.disableShadows = newShadows;
                    if (optimizer.enableOptimizer && optimizer.disableShadows) 
                        QualitySettings.shadows = ShadowQuality.Disable;
                    else 
                        QualitySettings.shadows = ShadowQuality.All;
                }

                UIUtils.DrawDivider();

                // 游戏优化
                GUILayout.Label(Localization.Get("GameOptimizations"), UIUtils.SubHeaderStyle);
                DrawCompactSwitch(ref optimizer.optimizeDecorationUpdate, Localization.Get("OptimizeDecorationUpdate"));
                DrawCompactSwitch(ref optimizer.optimizeTileUpdate, Localization.Get("OptimizeTileUpdate"));
                DrawCompactSwitch(ref optimizer.optimizeMoveTrack, Localization.Get("OptimizeMoveTrack"));
                DrawCompactSwitch(ref optimizer.optimizeRecolorTrack, Localization.Get("OptimizeRecolorTrack"));
                DrawCompactSwitch(ref optimizer.skipEventIfPaused, Localization.Get("SkipEventIfPaused"));
                DrawCompactSwitch(ref optimizer.optimizeEventIcons, Localization.Get("OptimizeEventIcons"));
                DrawCompactSwitch(ref optimizer.optimizeScnGameUpdate, Localization.Get("OptimizeScnGameUpdate"));
                DrawCompactSwitch(ref optimizer.optimizeMoveDecorations, Localization.Get("OptimizeMoveDecorations"));
                DrawCompactSwitch(ref optimizer.optimizeFloorMesh, Localization.Get("OptimizeFloorMesh"));
                DrawCompactSwitch(ref optimizer.optimizeFilters, Localization.Get("OptimizeFilters"));
                DrawCompactSwitch(ref optimizer.fastLoading, Localization.Get("FastLoading"));

                UIUtils.DrawDivider();

                // 场景优化
                GUILayout.Label(Localization.Get("SceneOptimizations"), UIUtils.SubHeaderStyle);
                DrawCompactSwitch(ref optimizer.cacheGameObjectReferences, Localization.Get("CacheGameObjectReferences"));
                DrawCompactSwitch(ref optimizer.optimizeEventProcessing, Localization.Get("OptimizeEventProcessing"));
                DrawCompactSwitch(ref optimizer.optimizeEffectRemoval, Localization.Get("OptimizeEffectRemoval"));
                DrawCompactSwitch(ref optimizer.optimizeMissIndicators, Localization.Get("OptimizeMissIndicators"));
                DrawCompactSwitch(ref optimizer.optimizeEditorMouseDetection, Localization.Get("OptimizeEditorMouseDetection"));
                DrawCompactSwitch(ref optimizer.optimizeEditorEventIndicators, Localization.Get("OptimizeEditorEventIndicators"));

                UIUtils.DrawDivider();

                // 加载优化
                GUILayout.Label(Localization.Get("LoadingOptimizations"), UIUtils.SubHeaderStyle);
                DrawCompactSwitch(ref optimizer.batchCreateDecorations, Localization.Get("BatchCreateDecorations"));

                if (optimizer.batchCreateDecorations)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(36));
                    GUILayout.Space(16);
                    GUILayout.Label(Localization.Get("DecorationBatchSize"), UIUtils.LabelSecondaryStyle);
                    GUILayout.FlexibleSpace();
                    string batchSizeStr = GUILayout.TextField(optimizer.decorationBatchSize.ToString(), 3, UIUtils.TextFieldStyle, GUILayout.Width(50));
                    if (int.TryParse(batchSizeStr, out int newBatchSize))
                        optimizer.decorationBatchSize = Mathf.Clamp(newBatchSize, 1, 100);
                    GUILayout.EndHorizontal();
                }

                DrawCompactSwitch(ref optimizer.cacheFloorEvents, Localization.Get("CacheFloorEvents"));
                DrawCompactSwitch(ref optimizer.asyncDestroyEffects, Localization.Get("AsyncDestroyEffects"));
                DrawCompactSwitch(ref optimizer.optimizeMoveTrackTweens, Localization.Get("OptimizeMoveTrackTweens"));
                DrawCompactSwitch(ref optimizer.batchMoveDecorations, Localization.Get("BatchMoveDecorations"));

                // 错误提示
                DrawErrorWarnings();
            }

            GUILayout.EndVertical();
        }

        private void DrawMemorySection()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("MemorySettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            
            bool newEnable = UIUtils.M3Switch(memory.enableMemoryOptimization, "");
            if (newEnable != memory.enableMemoryOptimization)
            {
                memory.enableMemoryOptimization = newEnable;
                PatchManager.UpdatePatchByType(typeof(MiscPatches.SmartGCPatch));
            }
            GUILayout.EndHorizontal();

            if (memory.enableMemoryOptimization)
            {
                bool newSmartGC = UIUtils.M3Switch(memory.enableSmartGC, Localization.Get("EnableSmartGC"));
                if (newSmartGC != memory.enableSmartGC)
                {
                    memory.enableSmartGC = newSmartGC;
                    PatchManager.UpdatePatchByType(typeof(MiscPatches.SmartGCPatch));
                }

                if (memory.enableSmartGC)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(36));
                    GUILayout.Space(16);
                    GUILayout.Label(Localization.Get("GCInterval"), UIUtils.LabelSecondaryStyle);
                    GUILayout.FlexibleSpace();
                    string intervalStr = GUILayout.TextField(memory.gcInterval.ToString("F0"), 4, UIUtils.TextFieldStyle, GUILayout.Width(50));
                    if (float.TryParse(intervalStr, out float newInterval)) 
                        memory.gcInterval = Mathf.Clamp(newInterval, 10f, 3600f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    memory.gcInGame = UIUtils.M3Switch(memory.gcInGame, Localization.Get("GCInGame"));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawUISection()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("UISettings"), UIUtils.HeaderStyle);

            bool newRemoveNews = UIUtils.M3Switch(ui.removeNews, Localization.Get("RemoveNews"));
            if (newRemoveNews != ui.removeNews)
            {
                ui.removeNews = newRemoveNews;
                PatchManager.UpdatePatchByType(typeof(MiscPatches.RemoveNewsPatch));
                MiscPatches.RemoveNewsPatch.UpdateNews();
            }

            bool newHideBeta = UIUtils.M3Switch(ui.hideBetaWatermark, Localization.Get("HideBetaWatermark"));
            if (newHideBeta != ui.hideBetaWatermark)
            {
                ui.hideBetaWatermark = newHideBeta;
                PatchManager.UpdatePatchByType(typeof(MiscPatches.HideBetaWatermarkPatch));
                MiscPatches.RefreshBetaWatermark();
            }

            bool newForceDifficulty = UIUtils.M3Switch(ui.forceDifficultyUI, Localization.Get("ForceDifficultyUI"));
            if (newForceDifficulty != ui.forceDifficultyUI)
            {
                ui.forceDifficultyUI = newForceDifficulty;
                PatchManager.UpdatePatchByType(typeof(MiscPatches.ForceDifficultyUIPatch));
            }

            ui.alwaysCountdown = UIUtils.M3Switch(ui.alwaysCountdown, Localization.Get("AlwaysCountdown"));

            bool newMoveAutoplay = UIUtils.M3Switch(ui.moveAutoplayText, Localization.Get("MoveAutoplayText"));
            if (newMoveAutoplay != ui.moveAutoplayText)
            {
                ui.moveAutoplayText = newMoveAutoplay;
                PatchManager.UpdatePatchByType(typeof(MiscPatches.AutoplayTextPositionPatch));
                MiscPatches.RefreshAutoplayTextPosition();
            }

            if (ui.moveAutoplayText)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(28));
                GUILayout.Label("X:", UIUtils.LabelSecondaryStyle, GUILayout.Width(24));
                ui.autoplayTextX = GUILayout.HorizontalSlider(ui.autoplayTextX, -Screen.width / 2f, Screen.width / 2f);
                GUILayout.Label(ui.autoplayTextX.ToString("F0"), UIUtils.LabelSecondaryStyle, GUILayout.Width(36));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.Height(28));
                GUILayout.Label("Y:", UIUtils.LabelSecondaryStyle, GUILayout.Width(24));
                ui.autoplayTextY = GUILayout.HorizontalSlider(ui.autoplayTextY, -Screen.height / 2f, Screen.height / 2f);
                GUILayout.Label(ui.autoplayTextY.ToString("F0"), UIUtils.LabelSecondaryStyle, GUILayout.Width(36));
                GUILayout.EndHorizontal();

                if (GUI.changed) MiscPatches.RefreshAutoplayTextPosition();
            }

            bool newCircleArc = UIUtils.M3Switch(ui.enableCircleArc, Localization.Get("EnableCircleArc"));
            if (newCircleArc != ui.enableCircleArc)
            {
                ui.enableCircleArc = newCircleArc;
                PatchManager.UpdatePatchByType(typeof(MiscPatches.CircleArcPatch));
            }
            if (ui.enableCircleArc) 
                UIUtils.DrawInfoBox("⚠ " + Localization.Get("RestartRequired"));

            GUILayout.EndVertical();
        }

        private void DrawLobbyMusicSection()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("LevelSelectSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            
            bool newEnable = UIUtils.M3Switch(lobbyMusic.enableLobbyMusicPatch, "");
            if (newEnable != lobbyMusic.enableLobbyMusicPatch)
            {
                lobbyMusic.enableLobbyMusicPatch = newEnable;
                PatchManager.UpdatePatchByType(typeof(MiscPatches.LobbyMusicPatch));
                if (lobbyMusic.enableLobbyMusicPatch)
                    MiscPatches.LobbyMusicPatch.ReloadFromSettings();
            }
            GUILayout.EndHorizontal();

            if (lobbyMusic.enableLobbyMusicPatch)
            {
                lobbyMusic.enableCustomBpm = UIUtils.M3Switch(lobbyMusic.enableCustomBpm, Localization.Get("EnableCustomBpm"));
                
                if (lobbyMusic.enableCustomBpm)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(36));
                    GUILayout.Label(Localization.Get("CustomBpm"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string bpmStr = GUILayout.TextField(lobbyMusic.customBpm.ToString("F1"), 6, UIUtils.TextFieldStyle, GUILayout.Width(60));
                    if (float.TryParse(bpmStr, out float newBpm)) 
                        lobbyMusic.customBpm = Mathf.Max(1f, newBpm);
                    GUILayout.EndHorizontal();
                }

                lobbyMusic.fastMusic = UIUtils.M3Switch(lobbyMusic.fastMusic, Localization.Get("LobbyFastMusic"));

                bool newCustomMusic = UIUtils.M3Switch(lobbyMusic.customMusic, Localization.Get("LobbyCustomMusic"));
                if (newCustomMusic != lobbyMusic.customMusic)
                {
                    lobbyMusic.customMusic = newCustomMusic;
                    MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                }

                if (lobbyMusic.customMusic)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(36));
                    GUILayout.Label(Localization.Get("LobbyDefaultMusicPath"), UIUtils.LabelSecondaryStyle, GUILayout.Width(100));
                    _defaultLobbyMusicPathCache = GUILayout.TextField(_defaultLobbyMusicPathCache ?? "", UIUtils.TextFieldStyle);
                    if (GUILayout.Button(Localization.Get("Apply"), UIUtils.ButtonStyle, GUILayout.Width(60)))
                    {
                        lobbyMusic.defaultMusicPath = (_defaultLobbyMusicPathCache ?? "").Trim();
                        MiscPatches.LobbyMusicPatch.StartLoad(true, lobbyMusic.defaultMusicPath);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(36));
                    GUILayout.Label(Localization.Get("LobbyFastMusicPath"), UIUtils.LabelSecondaryStyle, GUILayout.Width(100));
                    _fastLobbyMusicPathCache = GUILayout.TextField(_fastLobbyMusicPathCache ?? "", UIUtils.TextFieldStyle);
                    if (GUILayout.Button(Localization.Get("Apply"), UIUtils.ButtonStyle, GUILayout.Width(60)))
                    {
                        lobbyMusic.fastMusicPath = (_fastLobbyMusicPathCache ?? "").Trim();
                        MiscPatches.LobbyMusicPatch.StartLoad(false, lobbyMusic.fastMusicPath);
                    }
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button(Localization.Get("LobbyReloadMusic"), UIUtils.ButtonStyle, GUILayout.Height(32)))
                        MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                        
                    UIUtils.DrawInfoBox(Localization.Get("LobbyMusicHint"));
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawCompatibilitySection()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("CompatibilitySettings"), UIUtils.HeaderStyle);

            bool newPauseFix = UIUtils.M3Switch(compatibility.enableLegacyPauseFix, Localization.Get("EnableLegacyPauseFix"));
            if (newPauseFix != compatibility.enableLegacyPauseFix)
            {
                compatibility.enableLegacyPauseFix = newPauseFix;
                PatchManager.UpdatePatchByType(typeof(CompatibilityPatches.LegacyPauseFixPatch_Play));
                PatchManager.UpdatePatchByType(typeof(CompatibilityPatches.LegacyPauseFixPatch_Apply));
            }

            bool newNoFail = UIUtils.M3Switch(compatibility.enableNoFailTooEarly, Localization.Get("EnableNoFailTooEarly"));
            if (newNoFail != compatibility.enableNoFailTooEarly)
            {
                compatibility.enableNoFailTooEarly = newNoFail;
                PatchManager.UpdatePatchByType(typeof(CompatibilityPatches.NoFailTooEarlyPatch));
            }

            bool newForceAngle = UIUtils.M3Switch(compatibility.forceAngleData, Localization.Get("ForceAngleData"));
            if (newForceAngle != compatibility.forceAngleData)
            {
                compatibility.forceAngleData = newForceAngle;
                PatchManager.UpdatePatchByType(typeof(JsonPatches.ForceAngleDataPatch));
            }

            UIUtils.DrawDivider();

            GUILayout.Label(Localization.Get("LegacyLevelBehavior"), UIUtils.SubHeaderStyle);

            GUILayout.Label(Localization.Get("LegacyFlashMode"), UIUtils.LabelSecondaryStyle);
            var newFlashMode = (LegacyBehaviorMode)UIUtils.M3SegmentedButton((int)compatibility.legacyFlashMode,
                [Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn")]);

            GUILayout.Space(4);
            GUILayout.Label(Localization.Get("LegacyCamRelativeToMode"), UIUtils.LabelSecondaryStyle);
            var newCamRelMode = (LegacyBehaviorMode)UIUtils.M3SegmentedButton((int)compatibility.legacyCamRelativeToMode,
                [Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn")]);

            if (newFlashMode != compatibility.legacyFlashMode || newCamRelMode != compatibility.legacyCamRelativeToMode)
            {
                compatibility.legacyFlashMode = newFlashMode;
                compatibility.legacyCamRelativeToMode = newCamRelMode;
                PatchManager.UpdatePatchByType(typeof(JsonPatches.LegacyBehaviorPatch));
            }

            GUILayout.EndVertical();
        }

        private void DrawHitSoundSection()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("HitSoundSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            
            bool newEnable = UIUtils.M3Switch(hitSound.enableHitSoundPitch, "");
            if (newEnable != hitSound.enableHitSoundPitch)
            {
                hitSound.enableHitSoundPitch = newEnable;
                PatchManager.UpdatePatchByType(typeof(HitSoundPatch));
            }
            GUILayout.EndHorizontal();

            if (hitSound.enableHitSoundPitch)
            {
                UIUtils.DrawInfoBox(Localization.Get("EnableHitSoundPitch"));
            }

            GUILayout.EndVertical();
        }

        private void DrawJudgeTextSection()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("JudgeTextSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            
            bool newEnable = UIUtils.M3Switch(judgeText.enableJudgeTextCustomization, "");
            if (newEnable != judgeText.enableJudgeTextCustomization)
            {
                judgeText.enableJudgeTextCustomization = newEnable;
                PatchManager.UpdatePatchByType(typeof(JudgeTextPatches.HitTextMeshInitPatch));
                PatchManager.UpdatePatchByType(typeof(JudgeTextPatches.HitTextMeshShowPatch));
                PatchManager.UpdatePatchByType(typeof(JudgeTextPatches.ResetTimingOnRewindPatch));
            }
            GUILayout.EndHorizontal();

            if (judgeText.enableJudgeTextCustomization)
            {
                bool newShowAsOffset = UIUtils.M3Switch(judgeText.showAsOffset, Localization.Get("ShowAsOffset"));
                if (newShowAsOffset != judgeText.showAsOffset)
                {
                    judgeText.showAsOffset = newShowAsOffset;
                    PatchManager.UpdatePatchByType(typeof(JudgeTextPatches.HitTextMeshShowPatch));
                }

                UIUtils.DrawDivider();

                GUI.enabled = !judgeText.showAsOffset;
                GUILayout.Label(Localization.Get("CustomJudgeText"), UIUtils.SubHeaderStyle);

                DrawJudgeTextInput("TooEarly", ref judgeText.tooEarly);
                DrawJudgeTextInput("VeryEarly", ref judgeText.veryEarly);
                DrawJudgeTextInput("EarlyPerfect", ref judgeText.earlyPerfect);
                DrawJudgeTextInput("Perfect", ref judgeText.perfect);
                DrawJudgeTextInput("LatePerfect", ref judgeText.latePerfect);
                DrawJudgeTextInput("VeryLate", ref judgeText.veryLate);
                DrawJudgeTextInput("TooLate", ref judgeText.tooLate);
                DrawJudgeTextInput("Multipress", ref judgeText.multipress);
                DrawJudgeTextInput("FailMiss", ref judgeText.failMiss);
                DrawJudgeTextInput("FailOverload", ref judgeText.failOverload);
                GUI.enabled = true;

                if (GUILayout.Button(Localization.Get("ResetJudgeText"), UIUtils.ButtonStyle, GUILayout.Height(32)))
                    judgeText.ResetToDefault();
            }

            GUILayout.EndVertical();
        }

        private void DrawVersionInfo()
        {
            GUIStyle versionStyle = new(UIUtils.LabelSecondaryStyle)
            {
                alignment = TextAnchor.MiddleRight
            };
            GUILayout.Label($"Iridium {VersionManager.GetFullVersionString()}", versionStyle);
        }

        private void DrawCompactSwitch(ref bool value, string label, bool invert = false)
        {
            bool actualValue = invert ? !value : value;
            bool newValue = UIUtils.M3Switch(actualValue, label);
            if (invert) value = !newValue;
            else value = newValue;
        }

        private void DrawErrorWarnings()
        {
            if (typeof(Notification).GetMethod("SetupNotification", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) == null)
            {
                UIUtils.DrawInfoBox("⚠ " + Localization.Get("MethodNotFound", "Notification.SetupNotification"), true);
                optimizer.dontShowSavedMemory = true;
            }
            if (typeof(scrVisualDecoration).GetProperty("spriteUnscaledSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) == null)
            {
                UIUtils.DrawInfoBox("⚠ " + Localization.Get("PropertyNotFound", "scrVisualDecoration.spriteUnscaledSize"), true);
                optimizer.dontResizeCollider = true;
            }
        }

        private void DrawJudgeTextInput(string key, ref string value)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(28));
            GUILayout.Label(Localization.Get($"JudgeText_{key}"), UIUtils.LabelSecondaryStyle, GUILayout.Width(90));
            GUILayout.FlexibleSpace();
            string newValue = GUILayout.TextField(value, 20, UIUtils.TextFieldStyle, GUILayout.Width(100));
            if (newValue != value) value = newValue;
            GUILayout.EndHorizontal();
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
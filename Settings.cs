using System;
using UnityModManagerNet;
using UnityEngine;
using Iridium.UI;
using Iridium.Config;
using Iridium.Patches;
using System.IO;
using System.Linq;

namespace Iridium
{
    public class Settings : UnityModManager.ModSettings
    {
        public string language = "en";
        public bool firstRun = true;
        public string? lastVersion = null; // 用于跟踪版本升级
        public string? lastUpgradeMessageSeen_106_beta5 = null; // 记录用户最后看过的特定升级提示 ID
        
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

            GUILayout.BeginHorizontal();

            // --- Left Column ---
            GUILayout.BeginVertical(GUILayout.Width(420));

            // Language Selection Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("Language"), UIUtils.HeaderStyle);
            
            GUILayout.BeginHorizontal();
            var langs = Localization.AvailableLanguages;
            foreach (var lang in langs)
            {
                bool isCurrent = language == lang;
                if (isCurrent) GUI.color = new(0.66f, 0.76f, 1.0f);
                string displayName = Localization.GetDisplayName(lang);
                if (GUILayout.Button(displayName.ToUpper(), UIUtils.ButtonStyle, GUILayout.Width(100)))
                {
                    language = lang;
                }
                GUI.color = Color.white;
                GUILayout.Space(6);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // CompressDecorations Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("EnableOptimizer"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            bool newEnableOptimizer = UIUtils.M3Switch(optimizer.enableOptimizer, "");
            if (newEnableOptimizer != optimizer.enableOptimizer)
            {
                optimizer.enableOptimizer = newEnableOptimizer;
                if (optimizer.enableOptimizer && optimizer.disableShadows) QualitySettings.shadows = ShadowQuality.Disable;
                else QualitySettings.shadows = ShadowQuality.All;
            }
            GUILayout.EndHorizontal();

            if (optimizer.enableOptimizer)
            {
                GUILayout.Space(8);
                
                if (Iridium.Patches.OptimizerPatches.savedVRAM_MB > 0.1f)
                {
                    UIUtils.DrawInfoBox("✨ " + Localization.Get("SavedMemoryMsg", Iridium.Patches.OptimizerPatches.savedVRAM_MB.ToString("F2")));
                    GUILayout.Space(8);
                }

                bool showSavedMemory = UIUtils.M3Switch(!optimizer.dontShowSavedMemory, Localization.Get("ShowSavedMemory"));
                if (showSavedMemory == optimizer.dontShowSavedMemory)
                {
                    optimizer.dontShowSavedMemory = !showSavedMemory;
                }

                bool compressImage = UIUtils.M3Switch(!optimizer.dontCompress, Localization.Get("CompressImage"));
                if (compressImage == optimizer.dontCompress)
                {
                    optimizer.dontCompress = !compressImage;
                }

                bool multipleOf4 = UIUtils.M3Switch(!optimizer.dontResizeMultipleOf4, Localization.Get("MultipleOf4"));
                if (multipleOf4 == optimizer.dontResizeMultipleOf4)
                {
                    optimizer.dontResizeMultipleOf4 = !multipleOf4;
                }
                
                if (optimizer.dontCompress) optimizer.dontResizeMultipleOf4 = true;

                GUILayout.Space(8);
                GUILayout.BeginHorizontal(GUILayout.Height(28));
                GUILayout.Label(Localization.Get("DivideImageBy"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                string divideByStr = GUILayout.TextField(optimizer.divideBy.ToString("F1"), 5, UIUtils.TextFieldStyle, GUILayout.Width(50));
                if (double.TryParse(divideByStr, out double newDivideBy)) optimizer.divideBy = newDivideBy;
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                optimizer.dontResizeCollider = UIUtils.M3Switch(optimizer.dontResizeCollider, Localization.Get("DontResizeCollider"));
                
                bool newDisableShadows = UIUtils.M3Switch(optimizer.disableShadows, Localization.Get("DisableShadows"));
                if (newDisableShadows != optimizer.disableShadows)
                {
                    optimizer.disableShadows = newDisableShadows;
                    if (optimizer.enableOptimizer && optimizer.disableShadows) QualitySettings.shadows = ShadowQuality.Disable;
                    else QualitySettings.shadows = ShadowQuality.All;
                }

                optimizer.optimizeDecorationUpdate = UIUtils.M3Switch(optimizer.optimizeDecorationUpdate, Localization.Get("OptimizeDecorationUpdate"));
                optimizer.optimizeTileUpdate = UIUtils.M3Switch(optimizer.optimizeTileUpdate, Localization.Get("OptimizeTileUpdate"));
                optimizer.optimizeMoveTrack = UIUtils.M3Switch(optimizer.optimizeMoveTrack, Localization.Get("OptimizeMoveTrack"));
                optimizer.optimizeRecolorTrack = UIUtils.M3Switch(optimizer.optimizeRecolorTrack, Localization.Get("OptimizeRecolorTrack"));
                optimizer.skipEventIfPaused = UIUtils.M3Switch(optimizer.skipEventIfPaused, Localization.Get("SkipEventIfPaused"));
                optimizer.optimizeEventIcons = UIUtils.M3Switch(optimizer.optimizeEventIcons, Localization.Get("OptimizeEventIcons"));
                optimizer.optimizeScnGameUpdate = UIUtils.M3Switch(optimizer.optimizeScnGameUpdate, Localization.Get("OptimizeScnGameUpdate"));
                optimizer.optimizeMoveDecorations = UIUtils.M3Switch(optimizer.optimizeMoveDecorations, Localization.Get("OptimizeMoveDecorations"));
                optimizer.optimizeFloorMesh = UIUtils.M3Switch(optimizer.optimizeFloorMesh, Localization.Get("OptimizeFloorMesh"));
                optimizer.optimizeFilters = UIUtils.M3Switch(optimizer.optimizeFilters, Localization.Get("OptimizeFilters"));
                optimizer.fastLoading = UIUtils.M3Switch(optimizer.fastLoading, Localization.Get("FastLoading"));

                GUILayout.Space(8);
                GUILayout.Label("🎯 " + Localization.Get("SceneOptimizations"), UIUtils.LabelStyle);
                GUILayout.Space(4);

                optimizer.cacheGameObjectReferences = UIUtils.M3Switch(optimizer.cacheGameObjectReferences, Localization.Get("CacheGameObjectReferences"));
                optimizer.optimizeEventProcessing = UIUtils.M3Switch(optimizer.optimizeEventProcessing, Localization.Get("OptimizeEventProcessing"));
                optimizer.optimizeEffectRemoval = UIUtils.M3Switch(optimizer.optimizeEffectRemoval, Localization.Get("OptimizeEffectRemoval"));
                optimizer.optimizeMissIndicators = UIUtils.M3Switch(optimizer.optimizeMissIndicators, Localization.Get("OptimizeMissIndicators"));
                optimizer.optimizeEditorMouseDetection = UIUtils.M3Switch(optimizer.optimizeEditorMouseDetection, Localization.Get("OptimizeEditorMouseDetection"));
                optimizer.optimizeEditorEventIndicators = UIUtils.M3Switch(optimizer.optimizeEditorEventIndicators, Localization.Get("OptimizeEditorEventIndicators"));

                GUILayout.Space(8);
                GUILayout.Label("⚡ " + Localization.Get("LoadingOptimizations"), UIUtils.LabelStyle);
                GUILayout.Space(4);

                optimizer.optimizeTextureLoading = UIUtils.M3Switch(optimizer.optimizeTextureLoading, Localization.Get("OptimizeTextureLoading"));
                optimizer.batchCreateDecorations = UIUtils.M3Switch(optimizer.batchCreateDecorations, Localization.Get("BatchCreateDecorations"));

                if (optimizer.batchCreateDecorations)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Space(24);
                    GUILayout.Label(Localization.Get("DecorationBatchSize"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string batchSizeStr = GUILayout.TextField(optimizer.decorationBatchSize.ToString(), 3, UIUtils.TextFieldStyle, GUILayout.Width(50));
                    if (int.TryParse(batchSizeStr, out int newBatchSize))
                        optimizer.decorationBatchSize = Mathf.Clamp(newBatchSize, 1, 100);
                    GUILayout.EndHorizontal();
                }

                optimizer.cacheFloorEvents = UIUtils.M3Switch(optimizer.cacheFloorEvents, Localization.Get("CacheFloorEvents"));
                optimizer.asyncDestroyEffects = UIUtils.M3Switch(optimizer.asyncDestroyEffects, Localization.Get("AsyncDestroyEffects"));
                optimizer.optimizeMoveTrackTweens = UIUtils.M3Switch(optimizer.optimizeMoveTrackTweens, Localization.Get("OptimizeMoveTrackTweens"));
                optimizer.batchMoveDecorations = UIUtils.M3Switch(optimizer.batchMoveDecorations, Localization.Get("BatchMoveDecorations"));

                GUILayout.Space(4);

                // Error states
                if (typeof(Notification).GetMethod("SetupNotification", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) == null)
                {
                    GUILayout.Space(4);
                    UIUtils.DrawInfoBox("⚠ " + Localization.Get("MethodNotFound", "Notification.SetupNotification"), true);
                    optimizer.dontShowSavedMemory = true;
                }
                if (typeof(scrVisualDecoration).GetProperty("spriteUnscaledSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) == null)
                {
                    GUILayout.Space(4);
                    UIUtils.DrawInfoBox("⚠ " + Localization.Get("PropertyNotFound", "scrVisualDecoration.spriteUnscaledSize"), true);
                    optimizer.dontResizeCollider = true;
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Memory Optimization Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("MemorySettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            memory.enableMemoryOptimization = UIUtils.M3Switch(memory.enableMemoryOptimization, "");
            GUILayout.EndHorizontal();

            if (memory.enableMemoryOptimization)
            {
                GUILayout.Space(8);
                
                // 定时清理
                memory.enableSmartGC = UIUtils.M3Switch(memory.enableSmartGC, Localization.Get("EnableSmartGC"));
                if (memory.enableSmartGC)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Space(24);
                    GUILayout.Label(Localization.Get("GCInterval"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string intervalStr = GUILayout.TextField(memory.gcInterval.ToString("F0"), 4, UIUtils.TextFieldStyle, GUILayout.Width(50));
                    if (float.TryParse(intervalStr, out float newInterval)) memory.gcInterval = Mathf.Clamp(newInterval, 10f, 3600f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(24);
                    memory.gcInGame = UIUtils.M3Switch(memory.gcInGame, Localization.Get("GCInGame"));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndVertical(); // End Left Column

            GUILayout.Space(16);

            // --- Right Column ---
            GUILayout.BeginVertical(GUILayout.Width(420));

            // UI Adjustments Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("UISettings"), UIUtils.HeaderStyle);
            GUILayout.Space(8);
            bool newRemoveNews = UIUtils.M3Switch(ui.removeNews, Localization.Get("RemoveNews"));
            if (newRemoveNews != ui.removeNews)
            {
                ui.removeNews = newRemoveNews;
                Iridium.Patches.MiscPatches.RemoveNewsPatch.UpdateNews();
            }
            bool newHideBeta = UIUtils.M3Switch(ui.hideBetaWatermark, Localization.Get("HideBetaWatermark"));
            if (newHideBeta != ui.hideBetaWatermark)
            {
                ui.hideBetaWatermark = newHideBeta;
                Iridium.Patches.MiscPatches.RefreshBetaWatermark();
            }
            ui.forceDifficultyUI = UIUtils.M3Switch(ui.forceDifficultyUI, Localization.Get("ForceDifficultyUI"));
            ui.alwaysCountdown = UIUtils.M3Switch(ui.alwaysCountdown, Localization.Get("AlwaysCountdown"));

            bool newMoveAutoplay = UIUtils.M3Switch(ui.moveAutoplayText, Localization.Get("MoveAutoplayText"));
            if (newMoveAutoplay != ui.moveAutoplayText)
            {
                ui.moveAutoplayText = newMoveAutoplay;
                Iridium.Patches.MiscPatches.RefreshAutoplayTextPosition();
            }

            if (ui.moveAutoplayText)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                ui.autoplayTextX = GUILayout.HorizontalSlider(ui.autoplayTextX, -Screen.width / 2f, Screen.width / 2f);
                GUILayout.Label(ui.autoplayTextX.ToString("F0"), UIUtils.LabelStyle, GUILayout.Width(40));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                ui.autoplayTextY = GUILayout.HorizontalSlider(ui.autoplayTextY, -Screen.height / 2f, Screen.height / 2f);
                GUILayout.Label(ui.autoplayTextY.ToString("F0"), UIUtils.LabelStyle, GUILayout.Width(40));
                GUILayout.EndHorizontal();

                if (GUI.changed)
                {
                    Iridium.Patches.MiscPatches.RefreshAutoplayTextPosition();
                }
            }
            
            GUILayout.Space(8);
            ui.enableCircleArc = UIUtils.M3Switch(ui.enableCircleArc, Localization.Get("EnableCircleArc"));
            if (ui.enableCircleArc) UIUtils.DrawInfoBox("⚠ " + Localization.Get("RestartRequired"));
                
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Level Select Settings Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("LevelSelectSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            bool newEnableLobbyMusic = UIUtils.M3Switch(lobbyMusic.enableLobbyMusicPatch, "");
            if (newEnableLobbyMusic != lobbyMusic.enableLobbyMusicPatch)
            {
                lobbyMusic.enableLobbyMusicPatch = newEnableLobbyMusic;
                if (lobbyMusic.enableLobbyMusicPatch)
                {
                    Iridium.Patches.MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                }
            }
            GUILayout.EndHorizontal();

            if (lobbyMusic.enableLobbyMusicPatch)
            {
                GUILayout.Space(8);
                lobbyMusic.enableCustomBpm = UIUtils.M3Switch(lobbyMusic.enableCustomBpm, Localization.Get("EnableCustomBpm"));
                if (lobbyMusic.enableCustomBpm)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("CustomBpm"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string bpmStr = GUILayout.TextField(lobbyMusic.customBpm.ToString("F1"), 6, UIUtils.TextFieldStyle, GUILayout.Width(60));
                    if (float.TryParse(bpmStr, out float newBpm)) lobbyMusic.customBpm = Mathf.Max(1f, newBpm);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(4);
                lobbyMusic.fastMusic = UIUtils.M3Switch(lobbyMusic.fastMusic, Localization.Get("LobbyFastMusic"));

                bool newCustomMusic = UIUtils.M3Switch(lobbyMusic.customMusic, Localization.Get("LobbyCustomMusic"));
                if (newCustomMusic != lobbyMusic.customMusic)
                {
                    lobbyMusic.customMusic = newCustomMusic;
                    Iridium.Patches.MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                }

                if (lobbyMusic.customMusic)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("LobbyDefaultMusicPath"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    _defaultLobbyMusicPathCache = GUILayout.TextField(_defaultLobbyMusicPathCache ?? string.Empty, UIUtils.TextFieldStyle, GUILayout.Width(260));
                    GUILayout.Space(6);
                    bool canApplyDefaultPath = (_defaultLobbyMusicPathCache ?? string.Empty) != lobbyMusic.defaultMusicPath;
                    GUI.enabled = canApplyDefaultPath;
                    if (GUILayout.Button(Localization.Get("Apply"), UIUtils.ButtonStyle, GUILayout.Width(70)))
                    {
                        lobbyMusic.defaultMusicPath = (_defaultLobbyMusicPathCache ?? string.Empty).Trim();
                        Iridium.Patches.MiscPatches.LobbyMusicPatch.StartLoad(true, lobbyMusic.defaultMusicPath);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("LobbyFastMusicPath"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    _fastLobbyMusicPathCache = GUILayout.TextField(_fastLobbyMusicPathCache ?? string.Empty, UIUtils.TextFieldStyle, GUILayout.Width(260));
                    GUILayout.Space(6);
                    bool canApplyFastPath = (_fastLobbyMusicPathCache ?? string.Empty) != lobbyMusic.fastMusicPath;
                    GUI.enabled = canApplyFastPath;
                    if (GUILayout.Button(Localization.Get("Apply"), UIUtils.ButtonStyle, GUILayout.Width(70)))
                    {
                        lobbyMusic.fastMusicPath = (_fastLobbyMusicPathCache ?? string.Empty).Trim();
                        Iridium.Patches.MiscPatches.LobbyMusicPatch.StartLoad(false, lobbyMusic.fastMusicPath);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6);
                    if (GUILayout.Button(Localization.Get("LobbyReloadMusic"), UIUtils.ButtonStyle, GUILayout.Width(140)))
                    {
                        Iridium.Patches.MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                    }
                    UIUtils.DrawInfoBox(Localization.Get("LobbyMusicHint"));
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Compatibility & Fixes Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("CompatibilitySettings"), UIUtils.HeaderStyle);
            GUILayout.Space(8);
            compatibility.enableLegacyPauseFix = UIUtils.M3Switch(compatibility.enableLegacyPauseFix, Localization.Get("EnableLegacyPauseFix"));
            compatibility.enableNoFailTooEarly = UIUtils.M3Switch(compatibility.enableNoFailTooEarly, Localization.Get("EnableNoFailTooEarly"));
            
            GUILayout.Space(12);
            GUILayout.Label(Localization.Get("LegacyLevelBehavior"), UIUtils.LabelStyle, GUILayout.Height(24));
            
            // 将 forceAngleData 移出子容器，使其与上方的开关对齐
            compatibility.forceAngleData = UIUtils.M3Switch(compatibility.forceAngleData, Localization.Get("ForceAngleData"));

            GUILayout.Space(4);
            
            GUIStyle subContainerStyle = new()
            {
                normal = { background = UIUtils.GetCachedRoundedTex(64, 64, 8, new Color(1, 1, 1, 0.04f)) }, 
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 4, 4)
            };
            GUILayout.BeginVertical(subContainerStyle); // Sub-container
            
            GUILayout.Label(Localization.Get("LegacyFlashMode"), UIUtils.LabelStyle);
            GUILayout.Space(2);
            compatibility.legacyFlashMode = (LegacyBehaviorMode)UIUtils.M3SegmentedButton((int)compatibility.legacyFlashMode, 
                [Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn")]);
            
            GUILayout.Space(10);
            GUILayout.Label(Localization.Get("LegacyCamRelativeToMode"), UIUtils.LabelStyle);
            GUILayout.Space(2);
            compatibility.legacyCamRelativeToMode = (LegacyBehaviorMode)UIUtils.M3SegmentedButton((int)compatibility.legacyCamRelativeToMode, 
                [Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn")]);
            
            GUILayout.EndVertical(); // End Sub-container
            GUILayout.EndVertical(); // End Compatibility & Fixes Card

            GUILayout.Space(8);

            // Hit Sound Settings Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("HitSoundSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            hitSound.enableHitSoundPitch = UIUtils.M3Switch(hitSound.enableHitSoundPitch, "");
            GUILayout.EndHorizontal();
            
            if (hitSound.enableHitSoundPitch)
            {
                GUILayout.Space(8);
                UIUtils.DrawInfoBox(Localization.Get("EnableHitSoundPitch"));
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Judge Text Settings Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("JudgeTextSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            judgeText.enableJudgeTextCustomization = UIUtils.M3Switch(judgeText.enableJudgeTextCustomization, "");
            GUILayout.EndHorizontal();

            if (judgeText.enableJudgeTextCustomization)
            {
                GUILayout.Space(8);
                
                bool newShowAsOffset = UIUtils.M3Switch(judgeText.showAsOffset, Localization.Get("ShowAsOffset"));
                if (newShowAsOffset != judgeText.showAsOffset)
                {
                    judgeText.showAsOffset = newShowAsOffset;
                }
                
                GUILayout.Space(8);
                
                // 自定义文本区域
                GUI.enabled = !judgeText.showAsOffset; // 如果显示为偏移，则禁用自定义文本输入
                
                GUILayout.Label(Localization.Get("CustomJudgeText"), UIUtils.LabelStyle);
                GUILayout.Space(4);
                
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
                
                GUILayout.Space(8);
                if (GUILayout.Button(Localization.Get("ResetJudgeText"), UIUtils.ButtonStyle, GUILayout.Width(120)))
                {
                    judgeText.ResetToDefault();
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndVertical(); // End Right Column

            GUILayout.EndHorizontal();

            GUILayout.Space(16);
            
            // Version Info
            GUIStyle versionStyle = new(UIUtils.LabelStyle)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) }
            };
            GUILayout.Label($"Iridium {VersionManager.GetFullVersionString()}", versionStyle);

            if (GUI.changed)
            {            
                Save(modEntry);
                Iridium.Patches.PatchManager.UpdateAllPatches();
            }
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        private void DrawJudgeTextInput(string key, ref string value)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(24));
            GUILayout.Label(Localization.Get($"JudgeText_{key}"), UIUtils.LabelStyle, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            string newValue = GUILayout.TextField(value, 20, UIUtils.TextFieldStyle, GUILayout.Width(120));
            if (newValue != value)
            {
                value = newValue;
            }
            GUILayout.EndHorizontal();
        }

    }
}

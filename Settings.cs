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
        
        public OptimizerSettings optimizer = new();
        public UISettings ui = new();
        public TailSettings tail = new();
        public MemorySettings memory = new();
        public CompatibilitySettings compatibility = new();
        public AppearanceSettings appearance = new();

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UIUtils.InitializeStyles();

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

                // 场景切换清理
                memory.gcInLoadScene = UIUtils.M3Switch(memory.gcInLoadScene, Localization.Get("GCInLoadScene"));
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

            // Tail Settings Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("TailSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            bool newEnableTail = UIUtils.M3Switch(tail.enableTailTweak, "");
            if (newEnableTail != tail.enableTailTweak)
            {
                tail.enableTailTweak = newEnableTail;
                if (!tail.enableTailTweak) Iridium.Patches.MiscPatches.TailTweakPatch.ResetTails();
            }
            GUILayout.EndHorizontal();

            if (tail.enableTailTweak)
            {
                GUILayout.Space(8);
                bool newFollowPitch = UIUtils.M3Switch(tail.tailFollowPitch, Localization.Get("TailFollowPitch"));
                if (newFollowPitch != tail.tailFollowPitch)
                {
                    tail.tailFollowPitch = newFollowPitch;
                    if (!tail.tailFollowPitch) Iridium.Patches.MiscPatches.TailTweakPatch.ResetTails();
                }
                
                if (!tail.tailFollowPitch)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                GUILayout.Label(Localization.Get("TailLength"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                string lengthStr = GUILayout.TextField(tail.tailLength.ToString("F1"), 5, UIUtils.TextFieldStyle, GUILayout.Width(50));
                if (float.TryParse(lengthStr, out float newLength)) tail.tailLength = newLength;
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal(GUILayout.Height(28));
            GUILayout.Label(Localization.Get("TailEmission"), UIUtils.LabelStyle);
            GUILayout.FlexibleSpace();
            string emissionStr = GUILayout.TextField(tail.tailEmission.ToString("F1"), 5, UIUtils.TextFieldStyle, GUILayout.Width(50));
            if (float.TryParse(emissionStr, out float newEmission)) tail.tailEmission = newEmission;
            GUILayout.EndHorizontal();
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
    }
}

using HarmonyLib;
using Iridium.Config;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Iridium.Patches
{
    internal static class PatchRegistry
    {
        private static readonly HashSet<Type> OptimizerParentTypes = new()
        {
            typeof(OptimizerPatches),
            typeof(TrackOptimizationPatches),
            typeof(SceneOptimizationPatches),
            typeof(LoadingOptimizationPatches),
            typeof(ExtremeOptimizationPatches),
            typeof(EditorFloorOptimizationPatches)
        };

        public static List<PatchDefinition> Build()
        {
            var definitions = new List<PatchDefinition>();

            var editorMaster = () => Main.Settings.optimizer.enableEditorFloorOptimization;
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.InsertCharFloorOptimizationPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert));
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.InsertFloatFloorOptimizationPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert));
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.InstantiateFloatFloorsOptimizationPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert));
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.DeleteFloorOptimizationPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert));
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.RemakePathRedundancyPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.skipRedundantRemakePath));
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.GameRemakePathOptimizationPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.skipRedundantRemakePath));
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.DrawFloorNumsOptimizationPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.rangeBasedRedraw));
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.DrawFloorOffsetLinesOptimizationPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.skipRedundantRemakePath));
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.OffsetFloorIDsOptimizationPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.optimizeOffsetFloorEvents));
            definitions.Add(new PatchDefinition(typeof(EditorFloorOptimizationPatches.SkipApplyEventsOnInsertPatch),
                () => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.skipApplyEventsOnInsert));

            var optCond = () => Main.Settings.optimizer.enableOptimizer;
            RegisterNestedPatches(definitions, typeof(OptimizerPatches), optCond);
            definitions.Add(new PatchDefinition(typeof(TrackOptimizationPatches), optCond));
            RegisterNestedPatches(definitions, typeof(FfxOptimizationPatches), optCond);
            RegisterNestedPatches(definitions, typeof(SceneOptimizationPatches), optCond);

            var eventTweenCond = () => Main.Settings.optimizer.optimizeEventProcessing;
            definitions.Add(new PatchDefinition(typeof(EventTweenOptimizationPatches.FfxMoveFloorPlusEventTweensPatch), eventTweenCond));
            definitions.Add(new PatchDefinition(typeof(EventTweenOptimizationPatches.FfxMoveDecorationsPlusEventTweensPatch), eventTweenCond));
            definitions.Add(new PatchDefinition(typeof(EventTweenOptimizationPatches.FfxRecolorFloorPlusEventTweensPatch), eventTweenCond));
            definitions.Add(new PatchDefinition(typeof(EventTweenOptimizationPatches.FfxPlusBaseKillCacheInvalidationPatch), eventTweenCond));

            RegisterNestedPatches(definitions, typeof(LoadingOptimizationPatches), optCond,
                new HashSet<Type> { typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch) });
            definitions.Add(new PatchDefinition(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch),
                () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));
            definitions.Add(new PatchDefinition(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.ResetDecorations_Patch),
                () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));
            definitions.Add(new PatchDefinition(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.Play_Patch),
                () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));

            RegisterNestedPatches(definitions, typeof(ExtremeOptimizationPatches),
                () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.enableExtremeOptimization);

            var tweenSafetyCond = () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.dotweenDefaultRecyclable;
            RegisterNestedPatches(definitions, typeof(TweenSafetyPatches), tweenSafetyCond);

            definitions.Add(new PatchDefinition(typeof(JsonPatches.PatchGetCustomLevelName),
                () => Main.Settings.optimizer.customLevelReadOptimization));

            definitions.Add(new PatchDefinition(typeof(BugfixPatches.PortalTravelFixPatch),
                () => Main.Settings.compatibility.portalTravelFix));
            definitions.Add(new PatchDefinition(typeof(BugfixPatches.AsyncInputPlaySnapPatch), () => true));
            definitions.Add(new PatchDefinition(typeof(BugfixPatches.EditorPlayResetMistakesPatch),
                () => Main.Settings.compatibility.fixEditorPlayResetMistakes));
            definitions.Add(new PatchDefinition(typeof(BugfixPatches.TurnaroundConditionFix),
                () => Main.Settings.compatibility.fixTurnaroundCondition));
            definitions.Add(new PatchDefinition(typeof(JudgeTextPatches.HitTextMeshShowRotationFixPatch),
                () => Main.Settings.compatibility.fixJudgeRotation));
            definitions.Add(new PatchDefinition(typeof(EditorPausePatches), () => true));
            definitions.Add(new PatchDefinition(typeof(BugfixPatches.CoopPauseHandleLockFix),
                () => Main.Settings.compatibility.fixCoopPauseLock));
            definitions.Add(new PatchDefinition(typeof(BugfixPatches.CoopPlayerHitFix),
                () => Main.Settings.compatibility.fixCoopPauseLock));
            definitions.Add(new PatchDefinition(typeof(BugfixPatches.CoopPauseLockFix),
                () => Main.Settings.compatibility.fixCoopPauseLock));

            definitions.Add(new PatchDefinition(typeof(MiscPatches.RemoveNewsPatch), () => Main.Settings.ui.removeNews));
            definitions.Add(new PatchDefinition(typeof(MiscPatches.HideBetaWatermarkPatch), () => Main.Settings.ui.hideBetaWatermark));
            definitions.Add(new PatchDefinition(typeof(MiscPatches.ForceDifficultyUIPatch), () => Main.Settings.ui.forceDifficultyUI));
            definitions.Add(new PatchDefinition(typeof(MiscPatches.CircleArcPatch), () => Main.Settings.ui.enableCircleArc));
            definitions.Add(new PatchDefinition(typeof(MiscPatches.AutoplayTextPositionPatch), () => Main.Settings.ui.moveAutoplayText));
            definitions.Add(new PatchDefinition(typeof(MiscPatches.AlwaysCountdownPatch), () => Main.Settings.ui.alwaysCountdown));
            definitions.Add(new PatchDefinition(typeof(PausePlanetTrailPatch), () => Main.Settings.ui.enablePausePlanetTrail));

            definitions.Add(new PatchDefinition(typeof(MiscPatches.LobbyMusicPatch),
                () => Main.Settings.lobbyMusic.enableLobbyMusicPatch));

            var pauseFixCond = () => Main.Settings.compatibility.enableLegacyPauseFix;
            definitions.Add(new PatchDefinition(typeof(CompatibilityPatches.LegacyPauseFixPatch_Play), pauseFixCond));
            definitions.Add(new PatchDefinition(typeof(CompatibilityPatches.NoFailTooEarlyPatch),
                () => Main.Settings.compatibility.enableNoFailTooEarly));
            definitions.Add(new PatchDefinition(typeof(CompatibilityPatches.ScaleFilterSpeedWithPitchPatch),
                () => Main.Settings.compatibility.scaleFilterSpeedWithPitch));
            definitions.Add(new PatchDefinition(typeof(CameraRelativeDragPatches),
                () => Main.Settings.compatibility.fixCameraRelativeDrag));
            definitions.Add(new PatchDefinition(typeof(JsonPatches.ForceAngleDataPatch),
                () => Main.Settings.compatibility.forceAngleData));
            definitions.Add(new PatchDefinition(typeof(JsonPatches.LegacyBehaviorPatch), () =>
                Main.Settings.compatibility.legacyFlashMode != LegacyBehaviorMode.Default ||
                Main.Settings.compatibility.legacyCamRelativeToMode != LegacyBehaviorMode.Default));

            definitions.Add(new PatchDefinition(typeof(HitSoundPatch),
                () => Main.Settings.hitSound.enableHitSoundPitch));
            definitions.Add(new PatchDefinition(typeof(JudgeTextPatches.HitTextMeshInitPatch),
                () => Main.Settings.judgeText.enableJudgeTextCustomization));
            definitions.Add(new PatchDefinition(typeof(JudgeTextPatches.HitTextManagerShowPatch), () => true));
            definitions.Add(new PatchDefinition(typeof(JudgeTextPatches.HitTextMeshShowPatch),
                () => Main.Settings.judgeText.enableJudgeTextCustomization));

            definitions.Add(new PatchDefinition(typeof(EditorShortcutPatches.EditorShortcutUpdatePatch),
                () => Main.Settings.editorShortcuts.enableEditorShortcuts));
            definitions.Add(new PatchDefinition(typeof(EditorShortcutPatches.FloorSelectCameraJumpPatch),
                () => Main.Settings.editorShortcuts.enableEditorShortcuts));

            RegisterNestedPatches(definitions, typeof(CustomEasingPatches),
                () => Main.Settings.optimizer.enableCustomEasingEngine);

            definitions.Add(new PatchDefinition(typeof(Modules.AsyncInputOptimize.Patch.UnityEngine__SceneManagement__SceneManager),
                () => Main.Settings.asyncInput.enableAIO));
            definitions.Add(new PatchDefinition(typeof(Modules.AsyncInputOptimize.Patch.__scnGame),
                () => Main.Settings.asyncInput.enableAIO));
            definitions.Add(new PatchDefinition(typeof(Modules.AsyncInputOptimize.Patch.__scrConductor),
                () => Main.Settings.asyncInput.enableAIO));
            definitions.Add(new PatchDefinition(typeof(Modules.AsyncInputOptimize.Patch.__scrCountdown),
                () => Main.Settings.asyncInput.enableAIO));

            return definitions;
        }

        public static bool IsOptimizerPatch(Type type)
        {
            return OptimizerParentTypes.Contains(type) ||
                (type.DeclaringType != null && OptimizerParentTypes.Contains(type.DeclaringType));
        }

        private static void RegisterNestedPatches(
            List<PatchDefinition> definitions,
            Type parentType,
            Func<bool> condition,
            HashSet<Type>? exclude = null)
        {
            foreach (var type in parentType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0)
                    continue;
                if (exclude != null && exclude.Contains(type))
                    continue;

                definitions.Add(new PatchDefinition(type, condition));
            }
        }
    }
}

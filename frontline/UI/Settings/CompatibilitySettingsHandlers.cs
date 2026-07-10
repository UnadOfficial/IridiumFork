using Iris.Iml;
using Iridium.Patches;

namespace Iridium.UI.SettingsPanel
{
    internal static class CompatibilitySettingsHandlers
    {
        public static void Register(IrisGuiRenderer renderer, Iridium.Settings settings)
        {
            var compatibility = settings.compatibility;

            renderer.RegisterHandler("OnEnableLegacyPauseFixToggled", obj =>
            {
                compatibility.enableLegacyPauseFix = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(CompatibilityPatches.LegacyPauseFixPatch_Play));
                settings.Save();
            });

            renderer.RegisterHandler("OnEnableNoFailTooEarlyToggled", obj =>
            {
                compatibility.enableNoFailTooEarly = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(CompatibilityPatches.NoFailTooEarlyPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnScaleFilterSpeedWithPitchToggled", obj =>
            {
                compatibility.scaleFilterSpeedWithPitch = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(CompatibilityPatches.ScaleFilterSpeedWithPitchPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnEditorPauseAllowedToggled", obj =>
            {
                compatibility.editorPauseAllowed = obj is bool b && b;
                settings.Save();
            });

            renderer.RegisterHandler("OnEditorPauseEnabledToggled", obj =>
            {
                compatibility.editorPauseEnabled = obj is bool b && b;
                settings.Save();
            });

            renderer.RegisterHandler("OnFixCameraRelativeDragToggled", obj =>
            {
                compatibility.fixCameraRelativeDrag = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(CameraRelativeDragPatches));
                settings.Save();
            });

            renderer.RegisterHandler("OnPortalTravelFixToggled", obj =>
            {
                compatibility.portalTravelFix = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.PortalTravelFixPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnFixEditorPlayResetMistakesToggled", obj =>
            {
                compatibility.fixEditorPlayResetMistakes = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.EditorPlayResetMistakesPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnFixTurnaroundConditionToggled", obj =>
            {
                compatibility.fixTurnaroundCondition = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.TurnaroundConditionFix));
                settings.Save();
            });

            renderer.RegisterHandler("OnFixJudgeRotationToggled", obj =>
            {
                compatibility.fixJudgeRotation = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JudgeTextPatches.HitTextMeshShowRotationFixPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnFixCoopPauseLockToggled", obj =>
            {
                compatibility.fixCoopPauseLock = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.CoopPauseLockFix));
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.CoopPauseHandleLockFix));
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(BugfixPatches.CoopPlayerHitFix));
                settings.Save();
            });

            renderer.RegisterHandler("OnForceAngleDataToggled", obj =>
            {
                compatibility.forceAngleData = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JsonPatches.ForceAngleDataPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnUseILPatchToggled", obj =>
            {
                settings.patchMode.useILPatch = obj is bool b && b;
                Core.BasePatchMethod.SyncILModeFromSettings();
                settings.Save();
            });
        }
    }
}

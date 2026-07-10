using Iris.Iml;
using Iridium.Patches;

namespace Iridium.UI.SettingsPanel
{
    internal static class UISettingsHandlers
    {
        public static void Register(IrisGuiRenderer renderer, Iridium.Settings settings)
        {
            var ui = settings.ui;

            renderer.RegisterHandler("OnRemoveNewsToggled", obj =>
            {
                ui.removeNews = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.RemoveNewsPatch));
                MiscPatches.RemoveNewsPatch.UpdateNews();
                settings.Save();
            });

            renderer.RegisterHandler("OnHideBetaWatermarkToggled", obj =>
            {
                ui.hideBetaWatermark = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.HideBetaWatermarkPatch));
                MiscPatches.RefreshBetaWatermark();
                settings.Save();
            });

            renderer.RegisterHandler("OnForceDifficultyUIToggled", obj =>
            {
                ui.forceDifficultyUI = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.ForceDifficultyUIPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnAlwaysCountdownToggled", obj =>
            {
                ui.alwaysCountdown = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.AlwaysCountdownPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnEnablePausePlanetTrailToggled", obj =>
            {
                ui.enablePausePlanetTrail = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(PausePlanetTrailPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnMoveAutoplayTextToggled", obj =>
            {
                ui.moveAutoplayText = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.AutoplayTextPositionPatch));
                MiscPatches.RefreshAutoplayTextPosition();
                settings.Save();
            });

            renderer.RegisterHandler("OnAutoplayTextXChanged", obj =>
            {
                if (obj is float f)
                {
                    ui.autoplayTextX = f;
                    MiscPatches.RefreshAutoplayTextPosition();
                    settings.Save();
                }
            });

            renderer.RegisterHandler("OnAutoplayTextYChanged", obj =>
            {
                if (obj is float f)
                {
                    ui.autoplayTextY = f;
                    MiscPatches.RefreshAutoplayTextPosition();
                    settings.Save();
                }
            });

            renderer.RegisterHandler("OnEnableCircleArcToggled", obj =>
            {
                ui.enableCircleArc = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.CircleArcPatch));
                settings.Save();
            });
        }
    }
}

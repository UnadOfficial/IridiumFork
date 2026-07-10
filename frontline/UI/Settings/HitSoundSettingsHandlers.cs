using Iris.Iml;
using Iridium.Patches;

namespace Iridium.UI.SettingsPanel
{
    internal static class HitSoundSettingsHandlers
    {
        public static void Register(IrisGuiRenderer renderer, Iridium.Settings settings)
        {
            renderer.RegisterHandler("OnEnableHitSoundPitchToggled", obj =>
            {
                settings.hitSound.enableHitSoundPitch = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(HitSoundPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnJudgeTextCustomizationToggled", obj =>
            {
                settings.judgeText.enableJudgeTextCustomization = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JudgeTextPatches.HitTextMeshInitPatch));
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(JudgeTextPatches.HitTextMeshShowPatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnJudgeTextChanged", obj =>
            {
                settings.Save();
            });

            renderer.RegisterHandler("OnResetJudgeText", () =>
            {
                settings.judgeText.ResetToDefault();
                settings.Save();
            });

            renderer.RegisterHandler("OnConvertJudgeTextToOffset", () =>
            {
                settings.judgeText.ConvertAllToOffset();
                settings.Save();
            });
        }
    }
}

using Iris.Iml;
using Iridium.Patches;

namespace Iridium.UI.SettingsPanel
{
    internal static class AsyncInputSettingsHandlers
    {
        public static void Register(IrisGuiRenderer renderer, Iridium.Settings settings)
        {
            renderer.RegisterHandler("OnAsyncInputToggled", obj =>
            {
                settings.asyncInput.enableAIO = obj is bool b && b;
                if (settings.asyncInput.enableAIO)
                    Modules.AsyncInputOptimize.Main.Enable();
                else
                    Modules.AsyncInputOptimize.Main.Disable();

                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Modules.AsyncInputOptimize.Patch.UnityEngine__SceneManagement__SceneManager));
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Modules.AsyncInputOptimize.Patch.__scnGame));
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Modules.AsyncInputOptimize.Patch.__scrConductor));
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Modules.AsyncInputOptimize.Patch.__scrCountdown));
                settings.Save();
            });
        }
    }
}

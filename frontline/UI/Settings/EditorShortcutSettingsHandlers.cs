using Iris.Iml;
using Iridium.Patches;

namespace Iridium.UI.SettingsPanel
{
    internal static class EditorShortcutSettingsHandlers
    {
        public static void Register(IrisGuiRenderer renderer, Iridium.Settings settings)
        {
            renderer.RegisterHandler("OnEnableEditorShortcutsToggled", obj =>
            {
                settings.editorShortcuts.enableEditorShortcuts = obj is bool b && b;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(EditorShortcutPatches.EditorShortcutUpdatePatch));
                settings.Save();
            });

            renderer.RegisterHandler("OnCameraFollowOnFloorSelectToggled", obj =>
            {
                settings.editorShortcuts.cameraFollowOnFloorSelect = obj is bool b && b;
                settings.Save();
            });
        }
    }
}

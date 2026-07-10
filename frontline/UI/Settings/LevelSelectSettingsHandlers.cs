using Iris.Iml;
using Iridium.Patches;

namespace Iridium.UI.SettingsPanel
{
    internal static class LevelSelectSettingsHandlers
    {
        public static void Register(IrisGuiRenderer renderer, Iridium.Settings settings, SettingsPanelContext context)
        {
            var lobbyMusic = settings.lobbyMusic;

            renderer.RegisterHandler("OnLobbyMusicPatchToggled", obj =>
            {
                bool value = obj is bool b && b;
                lobbyMusic.enableLobbyMusicPatch = value;
                AsyncPatchManager.UpdatePatchByTypeAsync(typeof(MiscPatches.LobbyMusicPatch));
                if (value) MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                settings.Save();
            });

            renderer.RegisterHandler("OnEnableCustomBpmToggled", obj =>
            {
                lobbyMusic.enableCustomBpm = obj is bool b && b;
                settings.Save();
            });

            renderer.RegisterHandler("OnCustomBpmChanged", obj =>
            {
                if (obj is float f)
                {
                    lobbyMusic.customBpm = f;
                    settings.Save();
                }
            });

            renderer.RegisterHandler("OnLobbyFastMusicToggled", obj =>
            {
                lobbyMusic.fastMusic = obj is bool b && b;
                settings.Save();
            });

            renderer.RegisterHandler("OnLobbyCustomMusicToggled", obj =>
            {
                lobbyMusic.customMusic = obj is bool b && b;
                MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                settings.Save();
            });

            renderer.RegisterHandler("OnDefaultMusicPathChanged", obj =>
            {
                if (obj is string s)
                    context.defaultMusicPath = s;
            });

            renderer.RegisterHandler("OnApplyDefaultMusic", () =>
            {
                lobbyMusic.defaultMusicPath = (context.defaultMusicPath ?? string.Empty).Trim();
                context.defaultMusicPath = lobbyMusic.defaultMusicPath;
                MiscPatches.LobbyMusicPatch.StartLoad(true, lobbyMusic.defaultMusicPath);
                settings.Save();
            });

            renderer.RegisterHandler("OnFastMusicPathChanged", obj =>
            {
                if (obj is string s)
                    context.fastMusicPath = s;
            });

            renderer.RegisterHandler("OnApplyFastMusic", () =>
            {
                lobbyMusic.fastMusicPath = (context.fastMusicPath ?? string.Empty).Trim();
                context.fastMusicPath = lobbyMusic.fastMusicPath;
                MiscPatches.LobbyMusicPatch.StartLoad(false, lobbyMusic.fastMusicPath);
                settings.Save();
            });

            renderer.RegisterHandler("OnLobbyReloadMusic", () =>
            {
                MiscPatches.LobbyMusicPatch.ReloadFromSettings();
            });
        }
    }
}

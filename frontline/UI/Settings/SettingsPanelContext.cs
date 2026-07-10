namespace Iridium.UI.SettingsPanel
{
    internal sealed class SettingsPanelContext
    {
        public SettingsPanelContext(Iridium.Settings settings)
        {
            this.settings = settings;
            defaultMusicPath = settings.lobbyMusic.defaultMusicPath;
            fastMusicPath = settings.lobbyMusic.fastMusicPath;
        }

        public Iridium.Settings settings { get; }
        public string defaultMusicPath { get; set; }
        public string fastMusicPath { get; set; }
    }
}

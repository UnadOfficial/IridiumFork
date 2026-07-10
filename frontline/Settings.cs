using Iridium.Config;
using Iridium.Patches;

namespace Iridium
{
    public class Settings
    {
        public string language = "en";
        public bool firstRun = true;
        public string? lastVersion = null;
        public string? lastUpgradeMessageSeen_106_beta5 = null;

        public OptimizerSettings optimizer = new();
        public UISettings ui = new();
        public LobbyMusicSettings lobbyMusic = new();
        public MemorySettings memory = new();
        public CompatibilitySettings compatibility = new();
        public HitSoundSettings hitSound = new();
        public JudgeTextSettings judgeText = new();
        public PatchModeSettings patchMode = new();
        public EditorShortcutSettings editorShortcuts = new();
        public AsyncInputSettings asyncInput = new();

        public string panelToggleHotkey = "Ctrl+F9";

        private string _currentTab = "optimizer";
        private UI.SettingsPanel.SettingsPanelController? _panelController;

        public string currentTab => _currentTab;

        internal void SetCurrentTab(string tab)
        {
            _currentTab = tab;
        }

        public void OnGUI()
        {
            _panelController ??= new UI.SettingsPanel.SettingsPanelController(this);
            _panelController.OnGUI();
        }

        public void Save()
        {
            Main.Handler?.SaveSettings(this);
        }

        public static void ValidateCustomEasingConflict(Settings settings)
        {
            if (!settings.optimizer.enableCustomEasingEngine) return;

            bool hasConflict = settings.optimizer.optimizeMoveTrack
                            || settings.optimizer.optimizeRecolorTrack
                            || settings.optimizer.optimizeMoveDecorations;

            if (hasConflict)
            {
                settings.optimizer.enableCustomEasingEngine = false;
                Main.Handler?.SaveSettings(settings);
                Main.Logger?.Warning(Localization.Get("CustomEasingEngineConflictDetected"));
            }
        }

        internal static void ApplyCustomEasingMutualExclusion(OptimizerSettings opt)
        {
            if (!opt.enableCustomEasingEngine) return;

            bool changed = false;
            if (opt.optimizeMoveTrack) { opt.optimizeMoveTrack = false; changed = true; }
            if (opt.optimizeRecolorTrack) { opt.optimizeRecolorTrack = false; changed = true; }
            if (opt.optimizeMoveDecorations) { opt.optimizeMoveDecorations = false; changed = true; }
            if (changed) AsyncPatchManager.UpdateOptimizerPatchesAsync();
        }
    }
}

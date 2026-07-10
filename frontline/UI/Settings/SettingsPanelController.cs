using System;
using System.Collections.Generic;
using System.IO;
using Iris.Iml;
using Iridium.Patches;
using UnityEngine;

namespace Iridium.UI.SettingsPanel
{
    internal sealed class SettingsPanelController
    {
        private readonly Iridium.Settings _settings;
        private readonly SettingsPanelContext _context;
        private readonly ShortcutBindingController _shortcuts;
        private IrisGuiRenderer? _renderer;

        public SettingsPanelController(Iridium.Settings settings)
        {
            _settings = settings;
            _context = new SettingsPanelContext(settings);
            _shortcuts = new ShortcutBindingController(settings);
        }

        public void OnGUI()
        {
            int initialStackDepth = IridiumLayout.ContainerStack.Count;

            try
            {
                IridiumLayout.EnsureTexturesAlive();
                InitializeRenderer();

                string imlPath = Path.Combine(
                    Main.Handler?.ModPath ?? string.Empty,
                    "Resources", "ui", "Settings.iml");

                if (File.Exists(imlPath))
                {
                    _renderer!.Render(imlPath);
                }
                else
                {
                    IridiumLayout.Begin(IridiumLayout.ContainerDirection.Vertical, IridiumLayout.ContainerStyle.Padding);
                    IridiumLayout.Text("IML file not found: Settings.iml", IridiumLayout.TextStyle.Secondary);
                    IridiumLayout.End();
                }

                _shortcuts.CaptureCurrentEvent();

                if (GUI.changed) _settings.Save();
            }
            catch (Exception ex)
            {
                Main.Logger?.Error($"[OnGUI] Settings.OnGUI failed: {ex}");
                throw;
            }
            finally
            {
                while (IridiumLayout.ContainerStack.Count > initialStackDepth)
                {
                    try { IridiumLayout.End(); }
                    catch { break; }
                }
            }
        }

        private void InitializeRenderer()
        {
            if (_renderer != null) return;

            var renderer = new IrisGuiRenderer();
            if (Main.Logger != null)
                renderer.LogDelegate = msg => Main.Logger.Log(msg);

            renderer.SetHotReload(false);
            renderer.SetDataContext(_context);

            renderer.RegisterFunction("localize", args =>
            {
                if (args.Length > 0 && args[0] is string key)
                    return Localization.Get(key);
                return string.Empty;
            });

            renderer.RegisterFunction("getVersion", args => VersionManager.GetFullVersionString());
            renderer.RegisterFunction("getAsyncStatus", args =>
                AsyncPatchManager.IsProcessing ? "\u9234?" + Localization.Get("AsyncPatchProcessing") : string.Empty);
            renderer.RegisterFunction("getLanguages", args =>
            {
                var result = new List<object>();
                foreach (var lang in Localization.AvailableLanguages)
                    result.Add(new { key = lang, displayName = Localization.GetDisplayName(lang) });
                return result;
            });
            renderer.RegisterFunction("getShortcutDisplay", args =>
            {
                if (args.Length >= 2 && args[0] is int key && args[1] is int mods)
                    return _shortcuts.GetDisplay(key, mods);
                return string.Empty;
            });

            renderer.RegisterHandler<string>("OnTabClick", key => _settings.SetCurrentTab(key));
            renderer.RegisterHandler<string>("OnLanguageClick", lang =>
            {
                _settings.language = lang;
                _settings.Save();
            });

            _shortcuts.RegisterHandlers(renderer);
            renderer.SetLayout(new SettingsLayoutAdapter());

            OptimizerSettingsHandlers.Register(renderer, _settings);
            UISettingsHandlers.Register(renderer, _settings);
            LevelSelectSettingsHandlers.Register(renderer, _settings, _context);
            CompatibilitySettingsHandlers.Register(renderer, _settings);
            HitSoundSettingsHandlers.Register(renderer, _settings);
            EditorShortcutSettingsHandlers.Register(renderer, _settings);
            AsyncInputSettingsHandlers.Register(renderer, _settings);

            _renderer = renderer;
        }
    }
}

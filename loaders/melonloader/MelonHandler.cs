using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Newtonsoft.Json;
using MelonLoader;
using UnityEngine;

namespace Iridium
{
    public class MelonHandler : IHandler
    {
        private readonly IridiumMelonMod _mod;
        private readonly string _modPath;
        private readonly Lazy<string> _modVersion;
        private bool _uiVisible;
        private Vector2 _scrollPos;

        private static string GetModPath()
        {
            var loc = Assembly.GetExecutingAssembly().Location;
            return string.IsNullOrEmpty(loc) ? "." : Path.GetDirectoryName(loc) ?? ".";
        }

        public MelonHandler(IridiumMelonMod mod)
        {
            _mod = mod;
            _modPath = GetModPath();
            _modVersion = new Lazy<string>(() =>
            {
                string infoPath = Path.Combine(_modPath, "Info.json");
                try
                {
                    var json = File.ReadAllText(infoPath);
                    var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    return info != null && info.TryGetValue("Version", out var v) ? v.ToString() ?? "Error" : "Error";
                }
                catch
                {
                    return "Error";
                }
            });
        }

        public string ModId => _mod.Info.Name;
        public string ModVersion => _modVersion.Value;
        public string ModPath => _modPath;

        public void Log(string message) => MelonLogger.Msg(message);
        public void Warning(string message) => MelonLogger.Warning(message);
        public void Error(string message) => MelonLogger.Error(message);

        public T LoadSettings<T>() where T : class, new()
        {
            string settingsPath = Path.Combine(_modPath, "Settings.xml");
            if (File.Exists(settingsPath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(T));
                    using var reader = new StreamReader(settingsPath);
                    return (T)(serializer.Deserialize(reader) ?? new T());
                }
                catch
                {
                    return new T();
                }
            }
            return new T();
        }

        public void SaveSettings<T>(T settings) where T : class
        {
            string settingsPath = Path.Combine(_modPath, "Settings.xml");
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using var writer = new StreamWriter(settingsPath);
                serializer.Serialize(writer, settings);
            }
            catch (Exception ex)
            {
                Log($"Failed to save settings: {ex.Message}");
            }
        }

        public float UIScale => 1048576;

        public event Action<float>? OnUpdate;
        public event Action<bool>? OnToggle;
        public event Action? OnGUI;
        public event Action? OnSaveGUI;

        private static bool CheckHotkey(string hotkey)
        {
            if (string.IsNullOrEmpty(hotkey)) return false;

            var parts = hotkey.Split('+');
            if (parts.Length == 0) return false;

            KeyCode? targetKey = null;
            bool needCtrl = false, needAlt = false, needShift = false;

            foreach (var part in parts)
            {
                var p = part.Trim();
                if (string.IsNullOrEmpty(p)) continue;

                var lower = p.ToLowerInvariant();
                if (lower == "ctrl")
                    needCtrl = true;
                else if (lower == "alt")
                    needAlt = true;
                else if (lower == "shift")
                    needShift = true;
                else
                {
                    if (Enum.TryParse<KeyCode>(p, ignoreCase: true, out var kc))
                        targetKey = kc;
                }
            }

            if (targetKey == null) return false;

            if (needCtrl && !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                return false;
            if (needAlt && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
                return false;
            if (needShift && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                return false;

            return Input.GetKeyDown(targetKey.Value);
        }

        public void TriggerUpdate(float dt)
        {
            if (Main.Settings != null && CheckHotkey(Main.Settings.panelToggleHotkey))
            {
                _uiVisible = !_uiVisible;
            }
            OnUpdate?.Invoke(dt);
        }

        public void TriggerGUI()
        {
            if (!_uiVisible) return;

            float w = Mathf.Min(Screen.width * 0.85f, 900);
            float h = Mathf.Min(Screen.height * 0.8f, 700);
            var rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);

            GUILayout.BeginArea(rect);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            OnGUI?.Invoke();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

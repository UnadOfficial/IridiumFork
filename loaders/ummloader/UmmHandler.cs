using System;
using System.IO;
using System.Xml.Serialization;
using UnityModManagerNet;

namespace Iridium
{
    public class UmmHandler : IHandler
    {
        private readonly UnityModManager.ModEntry _entry;

        public UmmHandler(object entry)
        {
            _entry = (UnityModManager.ModEntry)entry;
            _entry.OnUpdate = (_, dt) => OnUpdate?.Invoke(dt);
            _entry.OnToggle = (_, v) => { OnToggle?.Invoke(v); return true; };
            _entry.OnGUI = _ => OnGUI?.Invoke();
            _entry.OnSaveGUI = _ => OnSaveGUI?.Invoke();
        }

        public string ModId => _entry.Info.Id;
        public string ModVersion => _entry.Info.Version;
        public string ModPath => _entry.Path;

        public void Log(string message) => _entry.Logger.Log(message);
        public void Warning(string message) => _entry.Logger.Warning(message);
        public void Error(string message) => _entry.Logger.Error(message);

        public T LoadSettings<T>() where T : class, new()
        {
            string settingsPath = Path.Combine(_entry.Path, "Settings.xml");
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
            string settingsPath = Path.Combine(_entry.Path, "Settings.xml");
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

        public float UIScale => UnityModManager.UI.Scale(1048576);

        public event Action<float>? OnUpdate;
        public event Action<bool>? OnToggle;
        public event Action? OnGUI;
        public event Action? OnSaveGUI;
    }
}

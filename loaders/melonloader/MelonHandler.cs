using System;
using System.IO;
using System.Xml.Serialization;
using MelonLoader;

namespace Iridium
{
    public class MelonHandler : IHandler
    {
        private readonly IridiumMelonMod _mod;
        private readonly string _modPath;

        public MelonHandler(IridiumMelonMod mod)
        {
            _mod = mod;
            _modPath = Path.Combine(
                Path.GetDirectoryName(mod.MelonAssembly.Location) ?? ".",
                "Iridium"
            );
            Directory.CreateDirectory(_modPath);
        }

        public string ModId => _mod.Info.Name;
        public string ModVersion => _mod.Info.Version;
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

        public void TriggerUpdate(float dt) => OnUpdate?.Invoke(dt);
        public void TriggerGUI() => OnGUI?.Invoke();
    }
}

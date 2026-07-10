using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace Iridium
{
    public class BepInHandler : IHandler
    {
        private readonly BaseUnityPlugin _plugin;
        private readonly string _modPath;

        public BepInHandler(BaseUnityPlugin plugin)
        {
            _plugin = plugin;
            _modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        }

        public string ModId => BuildInfo.ModName;
        public string ModVersion => BuildInfo.ModVersion;
        public string ModPath => _modPath;

        public void Log(string message) => _plugin.Logger.LogInfo(message);
        public void Warning(string message) => _plugin.Logger.LogWarning(message);
        public void Error(string message) => _plugin.Logger.LogError(message);

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

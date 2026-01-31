using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Iridium
{
    public static class Localization
    {
        private static Dictionary<string, Dictionary<string, string>> languages = new Dictionary<string, Dictionary<string, string>>();
        private static bool loaded = false;
        private static List<string> _availableLanguages = new List<string>();
        public static List<string> AvailableLanguages
        {
            get
            {
                if (!loaded) Load();
                return _availableLanguages;
            }
        }

        public static void Load()
        {
            try
            {
                languages.Clear();
                _availableLanguages.Clear();
                string langDir = Path.Combine(ResourceLoader.ResourcesPath, "lang");
                
                if (Directory.Exists(langDir))
                {
                    string[] files = Directory.GetFiles(langDir, "*.json");
                    foreach (string file in files)
                    {
                        try
                        {
                            string langName = Path.GetFileNameWithoutExtension(file);
                            string json = File.ReadAllText(file);
                            var dict = ParseJson(json);
                            if (dict.Count > 0)
                            {
                                languages[langName] = dict;
                                _availableLanguages.Add(langName);
                                Main.Mod?.Logger.Log($"Loaded language: {langName} ({dict.Count} keys)");
                            }
                        }
                        catch (Exception ex)
                        {
                            Main.Mod?.Logger.Error($"Failed to load language file {file}: {ex.Message}");
                        }
                    }
                }

                if (languages.Count == 0)
                {
                    Main.Mod?.Logger.Warning("No language files found, using empty 'en' fallback.");
                    languages["en"] = new Dictionary<string, string>();
                    _availableLanguages.Add("en");
                }
            }
            catch (Exception ex)
            {
                Main.Mod?.Logger.Error($"Critical error in Localization.Load: {ex.Message}");
                if (languages.Count == 0) languages["en"] = new Dictionary<string, string>();
            }
            finally
            {
                loaded = true;
            }
        }

        private static Dictionary<string, string> ParseJson(string json)
        {
            var dict = new Dictionary<string, string>();
            var matches = Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\"");
            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    value = value.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
                    dict[key] = value;
                }
            }
            return dict;
        }

        public static string Get(string key)
        {
            if (!loaded) Load();
            
            string currentLang = Main.Settings?.language ?? "en";
            if (languages.TryGetValue(currentLang, out var dict) && dict.TryGetValue(key, out string value))
            {
                return value;
            }

            // Fallback to 'en' or first available
            if (languages.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out string enValue))
            {
                return enValue;
            }

            return key;
        }

        public static string Get(string key, params object[] args)
        {
            try
            {
                string format = Get(key);
                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                Main.Mod?.Logger.Error($"Localization.Get format error for key {key}: {ex.Message}");
                return key;
            }
        }
    }
}

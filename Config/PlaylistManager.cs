using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Iridium.Config
{
    public static class PlaylistManager
    {
        private static string PlaylistDir => Main.Mod != null ? Path.Combine(Main.Mod.Path, "Resources", "playlists") : "";
        public static List<Playlist> Playlists { get; private set; } = new();
        public static Playlist? ActivePlaylist { get; private set; }

        public static void Initialize()
        {
            if (string.IsNullOrEmpty(PlaylistDir)) return;
            
            if (!Directory.Exists(PlaylistDir))
            {
                Directory.CreateDirectory(PlaylistDir);
            }
            LoadAllPlaylists();
            SetActivePlaylist(Main.Settings.appearance.activePlaylistId);
            CheckForMigration();
        }

        public static void LoadAllPlaylists()
        {
            if (string.IsNullOrEmpty(PlaylistDir)) return;
            
            Playlists.Clear();
            var files = Directory.GetFiles(PlaylistDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    Playlist playlist = JsonUtility.FromJson<Playlist>(json);
                    if (playlist != null)
                    {
                        Playlists.Add(playlist);
                    }
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"Failed to load playlist {file}: {e.Message}");
                }
            }
        }

        public static void SetActivePlaylist(string? id)
        {
            ActivePlaylist = Playlists.FirstOrDefault(p => p.id == id) ?? Playlists.FirstOrDefault();
            if (ActivePlaylist != null)
            {
                Main.Settings.appearance.activePlaylistId = ActivePlaylist.id;
            }
        }

        public static void SavePlaylist(Playlist playlist)
        {
            if (string.IsNullOrEmpty(PlaylistDir)) return;
            
            string path = Path.Combine(PlaylistDir, $"{playlist.id}.json");
            string json = JsonUtility.ToJson(playlist, true);
            File.WriteAllText(path, json);
            if (!Playlists.Any(p => p.id == playlist.id))
            {
                Playlists.Add(playlist);
            }
        }

        public static void CreateNewPlaylist(string name)
        {
            var playlist = new Playlist { name = name };
            SavePlaylist(playlist);
            SetActivePlaylist(playlist.id);
        }

        public static void DeletePlaylist(string? id)
        {
            if (string.IsNullOrEmpty(PlaylistDir) || id == null) return;
            
            var playlist = Playlists.FirstOrDefault(p => p.id == id);
            if (playlist != null)
            {
                string path = Path.Combine(PlaylistDir, $"{id}.json");
                if (File.Exists(path)) File.Delete(path);
                Playlists.Remove(playlist);
                if (ActivePlaylist?.id == id) SetActivePlaylist(null);
            }
        }

        private static void CheckForMigration()
        {
            var app = Main.Settings.appearance;
            if (!string.IsNullOrEmpty(app.skinPath) && File.Exists(app.skinPath))
            {
                app.needsMigration = true;
            }
        }

        public static void MigrateLegacySettings()
        {
            var app = Main.Settings.appearance;
            if (!app.needsMigration) return;

            var playlist = new Playlist { name = "Migrated Playlist" };
            var item = new BackgroundItem
            {
                name = Path.GetFileNameWithoutExtension(app.skinPath),
                path = app.skinPath,
                type = IsVideo(app.skinPath) ? BackgroundType.Video : BackgroundType.Image
            };
            playlist.items.Add(item);

            var config = new SceneConfig
            {
                backgroundRefs = new List<string> { item.id },
                playbackMode = PlaybackMode.Loop,
                opacity = app.backgroundOpacity,
                brightness = app.backgroundBrightness,
                saturation = app.backgroundSaturation,
                contrast = app.backgroundContrast,
                hue = app.backgroundHue,
                loopVideo = app.backgroundLoop,
                playbackSpeed = app.backgroundPlaybackSpeed,
                audioEnabled = app.backgroundAudio,
                audioVolume = app.backgroundAudioVolume
            };

            // Apply to all scenes
            foreach (var entry in playlist.sceneConfigs)
            {
                entry.config = config;
            }

            SavePlaylist(playlist);
            SetActivePlaylist(playlist.id);

            // Clear legacy settings
            app.skinPath = "";
            app.needsMigration = false;
            if (Main.Mod != null) Main.Settings.Save(Main.Mod);
        }

        private static bool IsVideo(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".mp4" || ext == ".mov" || ext == ".webm";
        }
    }
}

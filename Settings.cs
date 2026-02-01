using System;
using UnityModManagerNet;
using UnityEngine;

namespace Iridium
{
    /// <summary>
    /// Mod settings class
    /// Mod 设置类
    /// </summary>
    public class Settings : UnityModManager.ModSettings
    {
        public string language = "en";
        public bool enableOptimizer = false;
        public double divideBy = 1.0;
        public bool dontShowSavedMemory = false;
        public bool dontCompress = false;
        public bool dontResizeMultipleOf4 = false;
        public bool dontResizeCollider = false;
        public bool disableShadows = false;
        public bool optimizeDecorationUpdate = false;

        public bool removeNews = false;

        public bool forceDifficultyUI = false;

        public bool enableCircleArc = false;

        public bool enableTailTweak = false;
        public float tailLength = 1f;
        public float tailEmission = 1f;
        public bool tailFollowPitch = false;

        public bool enableLegacyPauseFix = false;
        public bool enableNoFailTooEarly = false;

        private static GUIStyle? _cardStyle;
        private static GUIStyle? _headerStyle;
        private static GUIStyle? _buttonStyle;
        private static GUIStyle? _toggleStyle;
        private static GUIStyle? _labelStyle;
        private static GUIStyle? _textFieldStyle;
        private static GUIStyle? _infoBoxStyle;
        private static GUIStyle? _warningBoxStyle;
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _textureCache = [];

        private void InitializeStyles()
        {
            if (_cardStyle != null) return;

            // Android 14 / Material 3 Dark Palette
            Color surfaceContainer = new(0.13f, 0.13f, 0.15f); // Surface Container
            Color primary = new(0.66f, 0.76f, 1.0f);           // Primary (M3 Blue)
            Color onSurface = new(0.88f, 0.88f, 0.9f);         // On Surface
            Color surfaceContainerHigh = new(0.17f, 0.17f, 0.19f);
            Color errorContainer = new(0.35f, 0.1f, 0.1f);     // Error Container
            Color onErrorContainer = new(1.0f, 0.7f, 0.7f);    // On Error Container
            Color infoContainer = new(0.1f, 0.2f, 0.35f);      // Info/Secondary Container
            Color onInfoContainer = new(0.7f, 0.85f, 1.0f);    // On Info Container

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 6, 6),
                normal = { background = GetCachedRoundedTex(128, 128, 12, surfaceContainer) }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                normal = { textColor = primary },
                margin = new RectOffset(0, 0, 0, 8)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = onSurface },
                alignment = TextAnchor.MiddleLeft
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 28,
                padding = new RectOffset(12, 12, 0, 0),
                normal = { background = GetCachedRoundedTex(64, 64, 8, surfaceContainerHigh), textColor = primary },
                hover = { background = GetCachedRoundedTex(64, 64, 8, primary * 0.2f), textColor = Color.white },
                active = { background = GetCachedRoundedTex(64, 64, 8, primary), textColor = Color.black }
            };

            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                fixedHeight = 24,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 0, 0),
                normal = { background = GetCachedRoundedTex(64, 64, 4, surfaceContainerHigh), textColor = onSurface },
                focused = { background = GetCachedRoundedTex(64, 64, 4, surfaceContainerHigh), textColor = Color.white }
            };

            _infoBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { background = GetCachedRoundedTex(64, 64, 8, infoContainer), textColor = onInfoContainer }
            };

            _warningBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { background = GetCachedRoundedTex(64, 64, 8, errorContainer), textColor = onErrorContainer }
            };
        }

        private void DrawInfoBox(string text, bool isError = false)
        {
            GUILayout.Box(text, isError ? _warningBoxStyle : _infoBoxStyle, GUILayout.ExpandWidth(true));
        }

        private bool M3Switch(bool value, string label)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(32));
            if (!string.IsNullOrEmpty(label)) GUILayout.Label(label, _labelStyle, GUILayout.ExpandWidth(true));
            
            Color trackColor = value ? new(0.66f, 0.76f, 1.0f) : new(0.28f, 0.28f, 0.31f);
            Color thumbColor = value ? new(0.0f, 0.2f, 0.4f) : new(0.55f, 0.55f, 0.58f);

            Rect rect = GUILayoutUtility.GetRect(40, 24, GUILayout.Width(40), GUILayout.Height(24));
            
            // Draw Track (Rounded)
            GUI.color = trackColor;
            GUI.DrawTexture(rect, GetCachedRoundedTex(64, 32, 16, Color.white));
            
            // Draw Thumb (Circle)
            float thumbSize = 18;
            float thumbX = value ? rect.x + rect.width - thumbSize - 3 : rect.x + 3;
            Rect thumbRect = new(thumbX, rect.y + (rect.height - thumbSize) / 2, thumbSize, thumbSize);
            GUI.color = thumbColor;
            GUI.DrawTexture(thumbRect, GetCachedRoundedTex(32, 32, 16, Color.white));
            
            GUI.color = Color.white;
            if (GUI.Button(rect, "", GUIStyle.none)) value = !value;
            
            GUILayout.EndHorizontal();
            return value;
        }

        private Texture2D GetCachedRoundedTex(int width, int height, float radius, Color col)
        {
            string key = $"{width}_{height}_{radius}_{col.r}_{col.g}_{col.b}_{col.a}";
            if (_textureCache.TryGetValue(key, out Texture2D tex) && tex != null) return tex;

            tex = MakeRoundedTex(width, height, radius, col);
            tex.hideFlags = HideFlags.HideAndDontSave;
            _textureCache[key] = tex;
            return tex;
        }

        private Texture2D MakeRoundedTex(int width, int height, float radius, Color col)
        {
            Texture2D tex = new(width, height);
            Color[] pix = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Math.Min(x, width - 1 - x);
                    float dy = Math.Min(y, height - 1 - y);

                    if (dx < radius && dy < radius)
                    {
                        float d = (float)Math.Sqrt(Math.Pow(radius - dx, 2) + Math.Pow(radius - dy, 2));
                        if (d > radius)
                        {
                            pix[y * width + x] = Color.clear;
                        }
                        else
                        {
                            // Simple anti-aliasing
                            float alpha = Math.Min(1, radius + 0.5f - d);
                            pix[y * width + x] = new Color(col.r, col.g, col.b, col.a * alpha);
                        }
                    }
                    else
                    {
                        pix[y * width + x] = col;
                    }
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            InitializeStyles();

            // Language Selection Card
            GUILayout.BeginVertical(_cardStyle, GUILayout.Width(400));
            GUILayout.Label(Localization.Get("Language"), _headerStyle);
            
            GUILayout.BeginHorizontal();
            var langs = Localization.AvailableLanguages;
            foreach (var lang in langs)
            {
                bool isCurrent = language == lang;
                if (isCurrent) GUI.color = new(0.66f, 0.76f, 1.0f);
                string displayName = Localization.GetDisplayName(lang);
                if (GUILayout.Button(displayName.ToUpper(), _buttonStyle, GUILayout.Width(100)))
                {
                    language = lang;
                }
                GUI.color = Color.white;
                GUILayout.Space(6);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // CompressDecorations Card
            GUILayout.BeginVertical(_cardStyle, GUILayout.Width(400));
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("EnableOptimizer"), _headerStyle);
            GUILayout.FlexibleSpace();
            enableOptimizer = M3Switch(enableOptimizer, "");
            GUILayout.EndHorizontal();

            if (enableOptimizer)
            {
                GUILayout.Space(8);
                
                if (Iridium.Patches.OptimizerPatches.savedVRAM_MB > 0.1f)
                {
                    DrawInfoBox("✨ " + Localization.Get("SavedMemoryMsg", Iridium.Patches.OptimizerPatches.savedVRAM_MB.ToString("F2")));
                    GUILayout.Space(8);
                }

                dontShowSavedMemory = !M3Switch(!dontShowSavedMemory, Localization.Get("ShowSavedMemory"));
                dontCompress = !M3Switch(!dontCompress, Localization.Get("CompressImage"));
                dontResizeMultipleOf4 = !M3Switch(!dontResizeMultipleOf4, Localization.Get("MultipleOf4"));
                
                if (dontCompress) dontResizeMultipleOf4 = true;

                GUILayout.Space(8);
                GUILayout.BeginHorizontal(GUILayout.Height(28));
                GUILayout.Label(Localization.Get("DivideImageBy"), _labelStyle);
                GUILayout.FlexibleSpace();
                string divideByStr = GUILayout.TextField(divideBy.ToString("F1"), _textFieldStyle, GUILayout.Width(50));
                if (double.TryParse(divideByStr, out double newDivideBy)) divideBy = newDivideBy;
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                dontResizeCollider = M3Switch(dontResizeCollider, Localization.Get("DontResizeCollider"));
                disableShadows = M3Switch(disableShadows, Localization.Get("DisableShadows"));
                optimizeDecorationUpdate = M3Switch(optimizeDecorationUpdate, Localization.Get("OptimizeDecorationUpdate"));

                // Error states
                if (typeof(Notification).GetMethod("SetupNotification", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) == null)
                {
                    GUILayout.Space(4);
                    DrawInfoBox("⚠ " + Localization.Get("MethodNotFound", "Notification.SetupNotification"), true);
                    dontShowSavedMemory = true;
                }
                if (typeof(scrVisualDecoration).GetProperty("spriteUnscaledSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) == null)
                {
                    GUILayout.Space(4);
                    DrawInfoBox("⚠ " + Localization.Get("PropertyNotFound", "scrVisualDecoration.spriteUnscaledSize"), true);
                    dontResizeCollider = true;
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // UI Adjustments Card
            GUILayout.BeginVertical(_cardStyle, GUILayout.Width(400));
            GUILayout.Label(Localization.Get("UISettings"), _headerStyle);
            GUILayout.Space(8);
            removeNews = M3Switch(removeNews, Localization.Get("RemoveNews"));
            forceDifficultyUI = M3Switch(forceDifficultyUI, Localization.Get("ForceDifficultyUI"));
            
            GUILayout.Space(8);
            enableCircleArc = M3Switch(enableCircleArc, Localization.Get("EnableCircleArc"));
            DrawInfoBox("ℹ " + Localization.Get("RestartRequired"));
            
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Tail Settings Card
            GUILayout.BeginVertical(_cardStyle, GUILayout.Width(400));
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("TailSettings"), _headerStyle);
            GUILayout.FlexibleSpace();
            enableTailTweak = M3Switch(enableTailTweak, "");
            GUILayout.EndHorizontal();

            if (enableTailTweak)
            {
                GUILayout.Space(8);
                tailFollowPitch = M3Switch(tailFollowPitch, Localization.Get("TailFollowPitch"));
                
                if (!tailFollowPitch)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("TailLength"), _labelStyle);
                    GUILayout.FlexibleSpace();
                    string lengthStr = GUILayout.TextField(tailLength.ToString("F1"), _textFieldStyle, GUILayout.Width(50));
                    if (float.TryParse(lengthStr, out float newLength)) tailLength = newLength;
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal(GUILayout.Height(28));
                GUILayout.Label(Localization.Get("TailEmission"), _labelStyle);
                GUILayout.FlexibleSpace();
                string emissionStr = GUILayout.TextField(tailEmission.ToString("F1"), _textFieldStyle, GUILayout.Width(50));
                if (float.TryParse(emissionStr, out float newEmission)) tailEmission = newEmission;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Compatibility & Fixes Card
            GUILayout.BeginVertical(_cardStyle, GUILayout.Width(400));
            GUILayout.Label(Localization.Get("CompatibilitySettings"), _headerStyle);
            GUILayout.Space(8);
            enableLegacyPauseFix = M3Switch(enableLegacyPauseFix, Localization.Get("EnableLegacyPauseFix"));
            enableNoFailTooEarly = M3Switch(enableNoFailTooEarly, Localization.Get("EnableNoFailTooEarly"));
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Called when saving GUI / 保存设置时调用
        /// </summary>
        public void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Save(modEntry);
        }

        /// <summary>
        /// Save settings / 保存设置
        /// </summary>
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        /// <summary>
        /// Load settings / 加载设置
        /// </summary>
        public static Settings Load(UnityModManager.ModEntry modEntry)
        {
            Settings settings = Load<Settings>(modEntry);
            if (settings.divideBy <= 0.0) settings.divideBy = 1.0;
            return settings;
        }
    }
}

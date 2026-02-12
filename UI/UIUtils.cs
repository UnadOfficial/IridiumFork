using System;
using UnityEngine;

namespace Iridium.UI
{
    public static class UIUtils
    {
        private static GUIStyle? _cardStyle;
        private static GUIStyle? _headerStyle;
        private static GUIStyle? _buttonStyle;
        private static GUIStyle? _labelStyle;
        private static GUIStyle? _textFieldStyle;
        private static GUIStyle? _infoBoxStyle;
        private static GUIStyle? _warningBoxStyle;
        private static GUIStyle? _colorPickerLabelStyle;
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _textureCache = [];

        public static GUIStyle CardStyle => _cardStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle HeaderStyle => _headerStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle ButtonStyle => _buttonStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle LabelStyle => _labelStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle TextFieldStyle => _textFieldStyle ?? throw new InvalidOperationException("UI not initialized");

        public static void InitializeStyles()
        {
            if (_cardStyle != null) return;

            // Android 14 / Material 3 Dark Palette
            Color surfaceContainer = new(0.13f, 0.13f, 0.15f);
            Color primary = new(0.66f, 0.76f, 1.0f);
            Color onSurface = new(0.88f, 0.88f, 0.9f);
            Color surfaceContainerHigh = new(0.17f, 0.17f, 0.19f);
            Color errorContainer = new(0.35f, 0.1f, 0.1f);
            Color onErrorContainer = new(1.0f, 0.7f, 0.7f);
            Color infoContainer = new(0.1f, 0.2f, 0.35f);
            Color onInfoContainer = new(0.7f, 0.85f, 1.0f);

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
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 0, 0),
                // 提高非 Hover 状态下的亮度，并增加微弱的边框感（通过颜色对比）
                normal = { background = GetCachedRoundedTex(64, 64, 4, new Color(0.25f, 0.25f, 0.28f)), textColor = onSurface },
                hover = { background = GetCachedRoundedTex(64, 64, 4, new Color(0.35f, 0.35f, 0.4f)), textColor = Color.white },
                focused = { background = GetCachedRoundedTex(64, 64, 4, new Color(0.4f, 0.4f, 0.45f)), textColor = Color.white }
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

            _colorPickerLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = onSurface }
            };
        }

        public static Color ColorPicker(Color color)
        {
            GUILayout.BeginVertical();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("R", _colorPickerLabelStyle, GUILayout.Width(15));
            color.r = GUILayout.HorizontalSlider(color.r, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("G", _colorPickerLabelStyle, GUILayout.Width(15));
            color.g = GUILayout.HorizontalSlider(color.g, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("B", _colorPickerLabelStyle, GUILayout.Width(15));
            color.b = GUILayout.HorizontalSlider(color.b, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("A", _colorPickerLabelStyle, GUILayout.Width(15));
            color.a = GUILayout.HorizontalSlider(color.a, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            // Preview color
            Rect previewRect = GUILayoutUtility.GetRect(120, 12, GUILayout.ExpandWidth(true));
            GUI.color = color;
            GUI.DrawTexture(previewRect, GetCachedRoundedTex(64, 64, 4, Color.white));
            GUI.color = Color.white;
            
            GUILayout.EndVertical();
            
            return color;
        }

        public static void DrawInfoBox(string text, bool isError = false)
        {
            GUILayout.Box(text, isError ? _warningBoxStyle : _infoBoxStyle, GUILayout.ExpandWidth(true));
        }

        public static bool M3Switch(bool value, string label)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(32));
            if (!string.IsNullOrEmpty(label)) GUILayout.Label(label, _labelStyle, GUILayout.ExpandWidth(true));
            
            Color trackColor = value ? new(0.66f, 0.76f, 1.0f) : new(0.28f, 0.28f, 0.31f);
            Color thumbColor = value ? new(0.0f, 0.2f, 0.4f) : new(0.55f, 0.55f, 0.58f);

            Rect rect = GUILayoutUtility.GetRect(40, 24, GUILayout.Width(40), GUILayout.Height(24));
            
            GUI.color = trackColor;
            GUI.DrawTexture(rect, GetCachedRoundedTex(64, 32, 16, Color.white));
            
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
        
        public static int M3SegmentedButton(int selectedIndex, string[] options)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < options.Length; i++)
            {
                bool isSelected = selectedIndex == i;
                Color primary = new(0.66f, 0.76f, 1.0f);
                Color onSurfaceVariant = new(0.75f, 0.75f, 0.78f);
                Color surfaceVariant = new(0.24f, 0.24f, 0.26f);
                
                GUIStyle segmentStyle = new(ButtonStyle)
                {
                    fixedHeight = 30,
                    margin = new RectOffset(0, 0, 0, 0),
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { 
                        background = GetCachedRoundedTex(64, 64, 0, isSelected ? primary : surfaceVariant), 
                        textColor = isSelected ? Color.black : onSurfaceVariant 
                    },
                    hover = {
                        background = GetCachedRoundedTex(64, 64, 0, isSelected ? primary : new Color(0.3f, 0.3f, 0.33f)),
                        textColor = isSelected ? Color.black : Color.white
                    }
                };

                // Round corners for ends
                float r = 15;
                if (i == 0) 
                {
                    segmentStyle.normal.background = GetCachedRoundedTex(64, 64, r, isSelected ? primary : surfaceVariant, true, false, true, false);
                    segmentStyle.hover.background = GetCachedRoundedTex(64, 64, r, isSelected ? primary : new Color(0.3f, 0.3f, 0.33f), true, false, true, false);
                }
                else if (i == options.Length - 1) 
                {
                    segmentStyle.normal.background = GetCachedRoundedTex(64, 64, r, isSelected ? primary : surfaceVariant, false, true, false, true);
                    segmentStyle.hover.background = GetCachedRoundedTex(64, 64, r, isSelected ? primary : new Color(0.3f, 0.3f, 0.33f), false, true, false, true);
                }

                if (GUILayout.Button(options[i], segmentStyle, GUILayout.ExpandWidth(true)))
                {
                    selectedIndex = i;
                }
            }
            GUILayout.EndHorizontal();
            return selectedIndex;
        }

        public static Texture2D GetCachedRoundedTex(int width, int height, float radius, Color col, bool tl = true, bool tr = true, bool bl = true, bool br = true)
        {
            string key = $"{width}_{height}_{radius}_{col.r}_{col.g}_{col.b}_{col.a}_{tl}{tr}{bl}{br}";
            if (_textureCache.TryGetValue(key, out Texture2D tex) && tex != null) return tex;

            tex = MakeRoundedTex(width, height, radius, col, tl, tr, bl, br);
            tex.hideFlags = HideFlags.HideAndDontSave;
            _textureCache[key] = tex;
            return tex;
        }

        private static Texture2D MakeRoundedTex(int width, int height, float radius, Color col, bool tl = true, bool tr = true, bool bl = true, bool br = true)
        {
            Texture2D tex = new(width, height);
            Color[] pix = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = -1, dy = -1;
                    bool isCornerRegion = false;

                    // Top-Left
                    if (tl && x < radius && y >= height - radius) { dx = radius - x; dy = radius - (height - 1 - y); isCornerRegion = true; }
                    // Top-Right
                    else if (tr && x >= width - radius && y >= height - radius) { dx = radius - (width - 1 - x); dy = radius - (height - 1 - y); isCornerRegion = true; }
                    // Bottom-Left
                    else if (bl && x < radius && y < radius) { dx = radius - x; dy = radius - y; isCornerRegion = true; }
                    // Bottom-Right
                    else if (br && x >= width - radius && y < radius) { dx = radius - (width - 1 - x); dy = radius - y; isCornerRegion = true; }

                    if (isCornerRegion)
                    {
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d > radius)
                        {
                            pix[y * width + x] = Color.clear;
                        }
                        else
                        {
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

        public static Texture2D MakeSolidTex(int width, int height, Color col)
        {
            Texture2D tex = new(width, height);
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}

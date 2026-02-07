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
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _textureCache = [];

        public static GUIStyle CardStyle => _cardStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle HeaderStyle => _headerStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle ButtonStyle => _buttonStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle LabelStyle => _labelStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle TextFieldStyle => _textFieldStyle ?? throw new InvalidOperationException("UI not initialized");

        public static void InitializeStyles()
        {
            if (_cardStyle != null) return;

            // Material 3 Dark Palette (More accurate)
            Color surface = new(0.11f, 0.11f, 0.12f);
            Color surfaceContainer = new(0.13f, 0.13f, 0.14f);
            Color surfaceContainerHigh = new(0.16f, 0.16f, 0.17f);
            Color primary = new(0.81f, 0.88f, 1.0f); // M3 Primary Fixed
            Color onSurface = new(0.9f, 0.9f, 0.92f);
            Color onSurfaceVariant = new(0.75f, 0.75f, 0.78f);
            Color outlineVariant = new(0.27f, 0.27f, 0.29f);
            
            Color errorContainer = new(0.35f, 0.1f, 0.1f);
            Color onErrorContainer = new(1.0f, 0.7f, 0.7f);
            Color infoContainer = new(0.1f, 0.2f, 0.35f);
            Color onInfoContainer = new(0.7f, 0.85f, 1.0f);

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5),
                normal = { background = GetCachedRoundedTex(128, 128, 12, surfaceContainer) } // M3 Extra Large radius (scaled)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14, // Scaled from 16
                fontStyle = FontStyle.Normal,
                normal = { textColor = primary },
                margin = new RectOffset(2, 0, 0, 6)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, // Scaled from 13
                normal = { textColor = onSurface },
                alignment = TextAnchor.MiddleLeft
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10, // Scaled from 12
                fixedHeight = 24, // Scaled from 28
                padding = new RectOffset(10, 10, 0, 0),
                normal = { background = GetCachedRoundedTex(64, 64, 6, surfaceContainerHigh), textColor = primary },
                hover = { background = GetCachedRoundedTex(64, 64, 6, new Color(1,1,1,0.05f) + surfaceContainerHigh), textColor = Color.white },
                active = { background = GetCachedRoundedTex(64, 64, 6, primary), textColor = Color.black }
            };

            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 10,
                fixedHeight = 22,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 0, 0),
                normal = { background = GetCachedRoundedTex(64, 64, 4, surfaceContainerHigh), textColor = onSurface },
                focused = { background = GetCachedRoundedTex(64, 64, 4, surfaceContainerHigh), textColor = Color.white }
            };

            _infoBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 4, 4),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { background = GetCachedRoundedTex(64, 64, 6, infoContainer), textColor = onInfoContainer }
            };

            _warningBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 4, 4),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { background = GetCachedRoundedTex(64, 64, 6, errorContainer), textColor = onErrorContainer }
            };
        }

        public static void DrawInfoBox(string text, bool isError = false)
        {
            GUILayout.Box(text, isError ? _warningBoxStyle : _infoBoxStyle, GUILayout.ExpandWidth(true));
        }

        public static bool M3Switch(bool value, string label)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(28)); // Scaled from 32
            if (!string.IsNullOrEmpty(label)) GUILayout.Label(label, _labelStyle, GUILayout.ExpandWidth(true));
            
            // M3 Switch Colors
            Color trackColor = value ? new(0.81f, 0.88f, 1.0f) : new(0.27f, 0.27f, 0.29f);
            Color thumbColor = value ? new(0.0f, 0.2f, 0.4f) : new(0.55f, 0.55f, 0.58f);

            Rect rect = GUILayoutUtility.GetRect(34, 20, GUILayout.Width(34), GUILayout.Height(20)); // Scaled from 40x24
            
            GUI.color = trackColor;
            GUI.DrawTexture(rect, GetCachedRoundedTex(64, 32, 10, Color.white));
            
            float thumbSize = value ? 14 : 12; // M3 thumb grows when active
            float thumbX = value ? rect.x + rect.width - thumbSize - 3 : rect.x + 4;
            Rect thumbRect = new(thumbX, rect.y + (rect.height - thumbSize) / 2, thumbSize, thumbSize);
            GUI.color = thumbColor;
            GUI.DrawTexture(thumbRect, GetCachedRoundedTex(32, 32, thumbSize / 2f, Color.white));
            
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
                Color primary = new(0.81f, 0.88f, 1.0f);
                Color onSurfaceVariant = new(0.75f, 0.75f, 0.78f);
                Color surfaceVariant = new(0.16f, 0.16f, 0.17f);
                
                GUIStyle segmentStyle = new(ButtonStyle)
                {
                    fixedHeight = 24, // Scaled from 30
                    margin = new RectOffset(0, 0, 0, 0),
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { 
                        background = GetCachedRoundedTex(64, 64, 0, isSelected ? primary : surfaceVariant), 
                        textColor = isSelected ? Color.black : onSurfaceVariant 
                    },
                    hover = {
                        background = GetCachedRoundedTex(64, 64, 0, isSelected ? primary : new Color(1, 1, 1, 0.05f) + surfaceVariant),
                        textColor = isSelected ? Color.black : Color.white
                    }
                };

                // Round corners for ends (M3 pill shape)
                float r = 12;
                if (i == 0) 
                {
                    segmentStyle.normal.background = GetCachedRoundedTex(64, 64, r, isSelected ? primary : surfaceVariant, true, false, true, false);
                    segmentStyle.hover.background = GetCachedRoundedTex(64, 64, r, isSelected ? primary : new Color(1, 1, 1, 0.05f) + surfaceVariant, true, false, true, false);
                }
                else if (i == options.Length - 1) 
                {
                    segmentStyle.normal.background = GetCachedRoundedTex(64, 64, r, isSelected ? primary : surfaceVariant, false, true, false, true);
                    segmentStyle.hover.background = GetCachedRoundedTex(64, 64, r, isSelected ? primary : new Color(1, 1, 1, 0.05f) + surfaceVariant, false, true, false, true);
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
        
                            // 检测当前像素是否处于需要处理圆角的四个角区域内
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
                                    // 保持原有的抗锯齿逻辑
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
    }
}

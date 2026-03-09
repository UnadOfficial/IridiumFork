using System;
using UnityEngine;

namespace Iridium.UI
{
    /// <summary>
    /// Material Design 3 风格 UI 工具类
    /// 粉色主题 + 深色背景
    /// </summary>
    public static class UIUtils
    {
        #region 样式缓存
        private static GUIStyle? _cardStyle;
        private static GUIStyle? _cardHighStyle;
        private static GUIStyle? _headerStyle;
        private static GUIStyle? _subHeaderStyle;
        private static GUIStyle? _buttonStyle;
        private static GUIStyle? _buttonPrimaryStyle;
        private static GUIStyle? _labelStyle;
        private static GUIStyle? _labelSecondaryStyle;
        private static GUIStyle? _textFieldStyle;
        private static GUIStyle? _infoBoxStyle;
        private static GUIStyle? _warningBoxStyle;
        private static GUIStyle? _colorPickerLabelStyle;
        private static GUIStyle? _sectionDividerStyle;
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _textureCache = [];
        #endregion

        #region 颜色定义 - Material Design 3 Pink Theme
        // 主色调 - 粉色系
        private static readonly Color Primary = new(1.0f, 0.6f, 0.75f);           // 粉色主色
        private static readonly Color PrimaryContainer = new(0.4f, 0.15f, 0.25f); // 粉色容器
        private static readonly Color OnPrimary = new(0.2f, 0.05f, 0.15f);        // 粉色上的文字
        private static readonly Color OnPrimaryContainer = new(1.0f, 0.85f, 0.9f); // 粉色容器上的文字

        // 表面颜色 - 深色主题
        private static readonly Color Surface = new(0.08f, 0.06f, 0.08f);           // 最底层背景
        private static readonly Color SurfaceContainer = new(0.11f, 0.09f, 0.11f);  // 卡片背景
        private static readonly Color SurfaceContainerHigh = new(0.15f, 0.12f, 0.14f); // 高亮卡片
        private static readonly Color SurfaceVariant = new(0.20f, 0.16f, 0.18f);    // 变体表面

        // 文字颜色
        private static readonly Color OnSurface = new(1.0f, 1.0f, 1.0f);           // 主文字 - 纯白
        private static readonly Color OnSurfaceVariant = new(0.85f, 0.85f, 0.85f); // 次要文字 - 浅灰

        // 辅助色
        private static readonly Color Outline = new(0.35f, 0.30f, 0.33f);          // 边框
        private static readonly Color OutlineVariant = new(0.25f, 0.20f, 0.22f);   // 变体边框

        // 信息色
        private static readonly Color InfoContainer = new(0.15f, 0.20f, 0.30f);
        private static readonly Color OnInfoContainer = new(0.75f, 0.85f, 1.0f);
        private static readonly Color ErrorContainer = new(0.35f, 0.12f, 0.15f);
        private static readonly Color OnErrorContainer = new(1.0f, 0.75f, 0.78f);
        #endregion

        #region 公共样式属性
        public static GUIStyle CardStyle => _cardStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle CardHighStyle => _cardHighStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle HeaderStyle => _headerStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle SubHeaderStyle => _subHeaderStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle ButtonStyle => _buttonStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle ButtonPrimaryStyle => _buttonPrimaryStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle LabelStyle => _labelStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle LabelSecondaryStyle => _labelSecondaryStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle TextFieldStyle => _textFieldStyle ?? throw new InvalidOperationException("UI not initialized");
        #endregion

        /// <summary>
        /// 初始化所有样式
        /// </summary>
        public static void InitializeStyles()
        {
            if (_cardStyle != null) return;

            // 卡片样式 - 大圆角
            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(16, 16, 16, 16),
                margin = new RectOffset(0, 0, 8, 8),
                normal = { background = GetCachedRoundedTex(128, 128, 16, SurfaceContainer) }
            };

            // 高亮卡片样式
            _cardHighStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(16, 16, 16, 16),
                margin = new RectOffset(0, 0, 8, 8),
                normal = { background = GetCachedRoundedTex(128, 128, 16, SurfaceContainerHigh) }
            };

            // 标题样式
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Primary },
                margin = new RectOffset(0, 0, 0, 12)
            };

            // 子标题样式
            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                normal = { textColor = Primary },
                margin = new RectOffset(0, 0, 0, 8)
            };

            // 标签样式
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = OnSurface },
                alignment = TextAnchor.MiddleLeft
            };

            // 次要标签样式
            _labelSecondaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = OnSurfaceVariant },
                alignment = TextAnchor.MiddleLeft
            };

            // 按钮样式 - 普通按钮
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fixedHeight = 36,
                padding = new RectOffset(16, 16, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                normal = { background = GetCachedRoundedTex(64, 64, 18, SurfaceVariant), textColor = OnSurface },
                hover = { background = GetCachedRoundedTex(64, 64, 18, SurfaceContainerHigh), textColor = Color.white },
                active = { background = GetCachedRoundedTex(64, 64, 18, PrimaryContainer), textColor = OnPrimaryContainer }
            };

            // 主要按钮样式
            _buttonPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fixedHeight = 36,
                padding = new RectOffset(16, 16, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                normal = { background = GetCachedRoundedTex(64, 64, 18, Primary), textColor = OnPrimary },
                hover = { background = GetCachedRoundedTex(64, 64, 18, new Color(1.0f, 0.7f, 0.82f)), textColor = OnPrimary },
                active = { background = GetCachedRoundedTex(64, 64, 18, new Color(0.9f, 0.5f, 0.68f)), textColor = OnPrimary }
            };

            // 输入框样式
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                fixedHeight = 36,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 0, 0),
                normal = { background = GetCachedRoundedTex(64, 64, 12, SurfaceVariant), textColor = OnSurface },
                hover = { background = GetCachedRoundedTex(64, 64, 12, SurfaceContainerHigh), textColor = Color.white },
                focused = { background = GetCachedRoundedTex(64, 64, 12, SurfaceContainerHigh), textColor = Color.white }
            };

            // 信息框样式
            _infoBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 6, 6),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { background = GetCachedRoundedTex(64, 64, 12, InfoContainer), textColor = OnInfoContainer }
            };

            // 警告框样式
            _warningBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 6, 6),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { background = GetCachedRoundedTex(64, 64, 12, ErrorContainer), textColor = OnErrorContainer }
            };

            // 颜色选择器标签
            _colorPickerLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = OnSurface }
            };

            // 分隔线样式
            _sectionDividerStyle = new GUIStyle
            {
                fixedHeight = 1,
                margin = new RectOffset(0, 0, 12, 12),
                normal = { background = MakeSolidTex(1, 1, OutlineVariant) }
            };
        }

        /// <summary>
        /// 绘制分隔线
        /// </summary>
        public static void DrawDivider()
        {
            GUILayout.Box("", _sectionDividerStyle, GUILayout.ExpandWidth(true));
        }

        /// <summary>
        /// 绘制信息框
        /// </summary>
        public static void DrawInfoBox(string text, bool isError = false)
        {
            GUILayout.Box(text, isError ? _warningBoxStyle : _infoBoxStyle, GUILayout.ExpandWidth(true));
        }

        /// <summary>
        /// Material Design 3 Switch 开关
        /// </summary>
        public static bool M3Switch(bool value, string label)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(40));
            
            if (!string.IsNullOrEmpty(label))
            {
                GUILayout.Label(label, _labelStyle, GUILayout.ExpandWidth(true));
            }

            // Switch 尺寸
            float trackWidth = 52;
            float trackHeight = 32;
            float thumbSize = 24;

            Rect rect = GUILayoutUtility.GetRect(trackWidth, trackHeight, GUILayout.Width(trackWidth), GUILayout.Height(trackHeight));

            // 轨道颜色 - 选中时更亮的粉色
            Color trackColor = value 
                ? new Color(1.0f, 0.65f, 0.8f)  // 选中：粉色轨道
                : new Color(0.25f, 0.22f, 0.24f); // 未选中：深灰

            // 绘制轨道
            GUI.color = trackColor;
            GUI.DrawTexture(rect, GetCachedRoundedTex(64, 40, 20, Color.white));
            GUI.color = Color.white;

            // 滑块位置和颜色
            float thumbX = value ? rect.x + rect.width - thumbSize - 4 : rect.x + 4;
            Rect thumbRect = new(thumbX, rect.y + (trackHeight - thumbSize) / 2, thumbSize, thumbSize);
            
            Color thumbColor = value 
                ? new Color(0.95f, 0.45f, 0.65f)  // 选中：深粉拇指
                : new Color(0.65f, 0.60f, 0.62f); // 未选中：灰色拇指
            
            GUI.color = thumbColor;
            GUI.DrawTexture(thumbRect, GetCachedRoundedTex(32, 32, 16, Color.white));
            GUI.color = Color.white;

            // 点击检测
            if (GUI.Button(rect, "", GUIStyle.none)) value = !value;

            GUILayout.EndHorizontal();
            return value;
        }

        /// <summary>
        /// Material Design 3 分段按钮
        /// </summary>
        public static int M3SegmentedButton(int selectedIndex, string[] options)
        {
            GUILayout.BeginHorizontal();
            
            for (int i = 0; i < options.Length; i++)
            {
                bool isSelected = selectedIndex == i;

                GUIStyle segmentStyle = new(GUI.skin.button)
                {
                    fixedHeight = 40,
                    margin = new RectOffset(i == 0 ? 0 : 1, i == options.Length - 1 ? 0 : 1, 0, 0),
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    normal = {
                        background = GetCachedRoundedTex(64, 64, 0, isSelected ? Primary : SurfaceVariant),
                        textColor = isSelected ? OnPrimary : OnSurfaceVariant
                    },
                    hover = {
                        background = GetCachedRoundedTex(64, 64, 0, isSelected ? new Color(1.0f, 0.7f, 0.82f) : SurfaceContainerHigh),
                        textColor = isSelected ? OnPrimary : Color.white
                    }
                };

                // 两端圆角
                float r = 20;
                if (i == 0)
                {
                    segmentStyle.normal.background = GetCachedRoundedTex(64, 64, r, isSelected ? Primary : SurfaceVariant, true, false, true, false);
                    segmentStyle.hover.background = GetCachedRoundedTex(64, 64, r, isSelected ? new Color(1.0f, 0.7f, 0.82f) : SurfaceContainerHigh, true, false, true, false);
                }
                else if (i == options.Length - 1)
                {
                    segmentStyle.normal.background = GetCachedRoundedTex(64, 64, r, isSelected ? Primary : SurfaceVariant, false, true, false, true);
                    segmentStyle.hover.background = GetCachedRoundedTex(64, 64, r, isSelected ? new Color(1.0f, 0.7f, 0.82f) : SurfaceContainerHigh, false, true, false, true);
                }

                if (GUILayout.Button(options[i], segmentStyle, GUILayout.ExpandWidth(true)))
                {
                    selectedIndex = i;
                }
            }
            
            GUILayout.EndHorizontal();
            return selectedIndex;
        }

        /// <summary>
        /// 颜色选择器
        /// </summary>
        public static Color ColorPicker(Color color)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("R", _colorPickerLabelStyle, GUILayout.Width(20));
            color.r = GUILayout.HorizontalSlider(color.r, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("G", _colorPickerLabelStyle, GUILayout.Width(20));
            color.g = GUILayout.HorizontalSlider(color.g, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("B", _colorPickerLabelStyle, GUILayout.Width(20));
            color.b = GUILayout.HorizontalSlider(color.b, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("A", _colorPickerLabelStyle, GUILayout.Width(20));
            color.a = GUILayout.HorizontalSlider(color.a, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            // 预览
            Rect previewRect = GUILayoutUtility.GetRect(120, 16, GUILayout.ExpandWidth(true));
            GUI.color = color;
            GUI.DrawTexture(previewRect, GetCachedRoundedTex(64, 64, 8, Color.white));
            GUI.color = Color.white;

            GUILayout.EndVertical();

            return color;
        }

        #region 纹理生成
        /// <summary>
        /// 获取缓存的圆角纹理
        /// </summary>
        public static Texture2D GetCachedRoundedTex(int width, int height, float radius, Color col, bool tl = true, bool tr = true, bool bl = true, bool br = true)
        {
            string key = $"{width}_{height}_{radius}_{col.r:F3}_{col.g:F3}_{col.b:F3}_{col.a:F3}_{tl}{tr}{bl}{br}";
            if (_textureCache.TryGetValue(key, out Texture2D tex) && tex != null) return tex;

            tex = MakeRoundedTex(width, height, radius, col, tl, tr, bl, br);
            tex.hideFlags = HideFlags.HideAndDontSave;
            _textureCache[key] = tex;
            return tex;
        }

        /// <summary>
        /// 生成圆角纹理
        /// </summary>
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

                    if (tl && x < radius && y >= height - radius) 
                    { 
                        dx = radius - x; 
                        dy = radius - (height - 1 - y); 
                        isCornerRegion = true; 
                    }
                    else if (tr && x >= width - radius && y >= height - radius) 
                    { 
                        dx = radius - (width - 1 - x); 
                        dy = radius - (height - 1 - y); 
                        isCornerRegion = true; 
                    }
                    else if (bl && x < radius && y < radius) 
                    { 
                        dx = radius - x; 
                        dy = radius - y; 
                        isCornerRegion = true; 
                    }
                    else if (br && x >= width - radius && y < radius) 
                    { 
                        dx = radius - (width - 1 - x); 
                        dy = radius - y; 
                        isCornerRegion = true; 
                    }

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

        /// <summary>
        /// 生成纯色纹理
        /// </summary>
        public static Texture2D MakeSolidTex(int width, int height, Color col)
        {
            Texture2D tex = new(width, height);
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
        #endregion
    }
}
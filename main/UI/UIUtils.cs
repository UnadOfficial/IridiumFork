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
        private static GUIStyle? _languageButtonStyle;
        private static GUIStyle? _segmentedButtonSelectedStyle;
        private static GUIStyle? _segmentedButtonUnselectedStyle;
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
        public static readonly Color Primary = new(1.0f, 0.6f, 0.75f);           // 粉色主色
        public static readonly Color PrimaryContainer = new(0.4f, 0.15f, 0.25f); // 粉色容器
        public static readonly Color OnPrimary = new(0.2f, 0.05f, 0.15f);        // 粉色上的文字
        public static readonly Color OnPrimaryContainer = new(1.0f, 0.85f, 0.9f); // 粉色容器上的文字

        // 表面颜色 - 深色主题
        public static readonly Color Surface = new(0.08f, 0.06f, 0.08f);           // 最底层背景
        public static readonly Color SurfaceContainer = new(0.14f, 0.12f, 0.14f);  // 卡片背景
        public static readonly Color SurfaceContainerHigh = new(0.18f, 0.15f, 0.17f); // 高亮卡片
        public static readonly Color SurfaceVariant = new(0.22f, 0.18f, 0.20f);    // 变体表面
        public static readonly Color SidebarBackground = new(0.10f, 0.06f, 0.10f); // 侧边栏背景 - 更深的粉

        // 文字颜色
        public static readonly Color OnSurface = new(1.0f, 1.0f, 1.0f);           // 主文字 - 纯白
        public static readonly Color OnSurfaceVariant = new(0.85f, 0.85f, 0.85f); // 次要文字 - 浅灰

        // 辅助色
        public static readonly Color Outline = new(0.35f, 0.30f, 0.33f);          // 边框
        public static readonly Color OutlineVariant = new(0.25f, 0.20f, 0.22f);   // 变体边框

        // 信息色
        public static readonly Color InfoContainer = new(0.20f, 0.18f, 0.22f);
        public static readonly Color OnInfoContainer = new(1.0f, 0.9f, 0.95f);
        public static readonly Color ErrorContainer = new(0.35f, 0.12f, 0.15f);
        public static readonly Color OnErrorContainer = new(1.0f, 0.75f, 0.78f);
        #endregion

        #region 尺寸常量
        public const int CARD_WIDTH = 420;
        public const int SPACING = 8;
        public const int PADDING = 16;
        public const float CORNER_RADIUS = 16f;
        public const int SIDEBAR_WIDTH = 240;
        public const int SIDEBAR_ITEM_HEIGHT = 48;
        public const int CONTENT_PADDING = 24;
        public const int SETTING_ITEM_HEIGHT = 56;
        public const int SETTING_GROUP_SPACING = 24;
        #endregion

        #region 公共样式属性
        public static GUIStyle CardStyle => _cardStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle CardHighStyle => _cardHighStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle HeaderStyle => _headerStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle SubHeaderStyle => _subHeaderStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle ButtonStyle => _buttonStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle ButtonPrimaryStyle => _buttonPrimaryStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle LanguageButtonStyle => _languageButtonStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle SegmentedButtonSelectedStyle => _segmentedButtonSelectedStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle SegmentedButtonUnselectedStyle => _segmentedButtonUnselectedStyle ?? throw new InvalidOperationException("UI not initialized");
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
                normal = { background = GetCachedRoundedTex(32, 32, 18, new Color(0.28f, 0.25f, 0.28f)), textColor = new Color(0.9f, 0.9f, 0.9f) },
                hover = { background = GetCachedRoundedTex(32, 32, 18, new Color(0.35f, 0.32f, 0.36f)), textColor = Color.white },
                active = { background = GetCachedRoundedTex(32, 32, 18, new Color(0.22f, 0.20f, 0.24f)), textColor = Color.white }
            };

            // 主要按钮样式
            _buttonPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fixedHeight = 36,
                padding = new RectOffset(16, 16, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                normal = { background = GetCachedRoundedTex(32, 32, 18, Primary), textColor = OnPrimary },
                hover = { background = GetCachedRoundedTex(32, 32, 18, new Color(1.0f, 0.7f, 0.82f)), textColor = OnPrimary },
                active = { background = GetCachedRoundedTex(32, 32, 18, new Color(0.9f, 0.5f, 0.68f)), textColor = Color.white }
            };

            // 语言按钮样式 - 全宽，无固定高度
            _languageButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(0, 0, 2, 2),
                alignment = TextAnchor.MiddleCenter,
                normal = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.15f, 0.10f, 0.15f)), textColor = OnSurfaceVariant },
                hover = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.22f, 0.16f, 0.22f)), textColor = OnSurface },
                active = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.25f, 0.18f, 0.25f)), textColor = Primary }
            };

            // 分段按钮样式 - 选中状态
            _segmentedButtonSelectedStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 40,
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = GetCachedRoundedTex(32, 32, 12, Primary), textColor = OnPrimary },
                hover = { background = GetCachedRoundedTex(32, 32, 12, new Color(1.0f, 0.7f, 0.82f)), textColor = Color.white },
                active = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.9f, 0.5f, 0.68f)), textColor = Color.white }
            };

            // 分段按钮样式 - 未选中状态
            _segmentedButtonUnselectedStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 40,
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.20f, 0.18f, 0.22f)), textColor = OnSurfaceVariant },
                hover = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.25f, 0.22f, 0.26f)), textColor = Color.white },
                active = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.3f, 0.28f, 0.32f)), textColor = Color.white }
            };

            // 输入框样式
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                fixedHeight = 36,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 0, 0),
                normal = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.15f, 0.13f, 0.17f)), textColor = new Color(0.95f, 0.95f, 0.95f) },
                hover = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.20f, 0.18f, 0.22f)), textColor = Color.white },
                focused = { background = GetCachedRoundedTex(32, 32, 12, new Color(0.25f, 0.20f, 0.25f)), textColor = Color.white }
            };

            // 信息框样式
            _infoBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 6, 6),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { background = GetCachedRoundedTex(32, 32, 12, InfoContainer), textColor = OnInfoContainer }
            };

            // 警告框样式
            _warningBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 6, 6),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { background = GetCachedRoundedTex(32, 32, 12, ErrorContainer), textColor = OnErrorContainer }
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

        #region 性能优化：静态颜色缓存
        private static Color? _trackColorOn;
        private static Color? _thumbColorOn;
        private static Color? _trackColorOff;
        private static Color? _thumbColorOff;
        
        private static void EnsureColorsInitialized()
        {
            if (_trackColorOn == null)
            {
                _trackColorOn = new Color(1.0f, 0.65f, 0.8f);
                _thumbColorOn = new Color(0.95f, 0.45f, 0.65f);
                _trackColorOff = new Color(0.40f, 0.38f, 0.42f);
                _thumbColorOff = new Color(0.50f, 0.48f, 0.52f);
            }
        }
        #endregion

        /// <summary>
        /// Material Design 3 Switch 开关
        /// </summary>
        public static bool M3Switch(bool value, string label)
        {
            EnsureColorsInitialized();
            
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
            Color trackColor = value ? _trackColorOn!.Value : _trackColorOff!.Value;

            // 绘制轨道
            GUI.color = trackColor;
            GUI.DrawTexture(rect, GetCachedRoundedTex(64, 40, 20, Color.white));
            GUI.color = Color.white;

            // 滑块位置和颜色
            float thumbX = value ? rect.x + rect.width - thumbSize - 4 : rect.x + 4;
            Rect thumbRect = new(thumbX, rect.y + (trackHeight - thumbSize) / 2, thumbSize, thumbSize);
            
            Color thumbColor = value ? _thumbColorOn!.Value : _thumbColorOff!.Value;
            
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
            GUILayout.BeginHorizontal(GUILayout.Height(40));

            for (int i = 0; i < options.Length; i++)
            {
                bool isSelected = selectedIndex == i;
                GUIStyle style = isSelected ? SegmentedButtonSelectedStyle : SegmentedButtonUnselectedStyle;

                if (GUILayout.Button(options[i], style, GUILayout.ExpandWidth(true)))
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
            GUI.DrawTexture(previewRect, GetCachedRoundedTex(32, 32, 8, Color.white));
            GUI.color = Color.white;

            GUILayout.EndVertical();

            return color;
        }

        /// <summary>
        /// Material Design 3 复选框
        /// </summary>
        public static bool M3Checkbox(bool value, string label)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(32));

            // 复选框尺寸
            float checkboxSize = 24;
            Rect checkboxRect = GUILayoutUtility.GetRect(checkboxSize, checkboxSize, GUILayout.Width(checkboxSize), GUILayout.Height(checkboxSize));

            // 背景
            Color bgColor = value ? Primary : new Color(0.25f, 0.22f, 0.26f);
            GUI.color = bgColor;
            GUI.DrawTexture(checkboxRect, GetCachedRoundedTex(32, 32, 4, Color.white));
            GUI.color = Color.white;

            // 复选框边框
            if (!value)
            {
                GUI.color = Outline;
                GUI.DrawTexture(checkboxRect, GetCachedRoundedTex(32, 32, 4, Color.clear));
                GUI.color = Color.white;
            }

            // 点击检测
            if (GUI.Button(checkboxRect, "", GUIStyle.none)) value = !value;

            // 标签
            if (!string.IsNullOrEmpty(label))
            {
                GUILayout.Label(label, _labelStyle, GUILayout.ExpandWidth(true));
            }

            GUILayout.EndHorizontal();
            return value;
        }

        /// <summary>
        /// Material Design 3 单选按钮
        /// </summary>
        public static bool M3RadioButton(bool value, string label)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(32));

            // 单选按钮尺寸
            float radioSize = 24;
            Rect radioRect = GUILayoutUtility.GetRect(radioSize, radioSize, GUILayout.Width(radioSize), GUILayout.Height(radioSize));

            // 外圆
            Color outerColor = value ? Primary : new Color(0.25f, 0.22f, 0.26f);
            GUI.color = outerColor;
            GUI.DrawTexture(radioRect, GetCachedRoundedTex(32, 32, 16, Color.white));
            GUI.color = Color.white;

            // 内圆（选中时）
            if (value)
            {
                float innerSize = radioSize * 0.5f;
                Rect innerRect = new(radioRect.x + (radioSize - innerSize) / 2, radioRect.y + (radioSize - innerSize) / 2, innerSize, innerSize);
                GUI.color = OnPrimary;
                GUI.DrawTexture(innerRect, GetCachedRoundedTex(16, 16, 8, Color.white));
                GUI.color = Color.white;
            }

            // 点击检测
            if (GUI.Button(radioRect, "", GUIStyle.none)) value = !value;

            // 标签
            if (!string.IsNullOrEmpty(label))
            {
                GUILayout.Label(label, _labelStyle, GUILayout.ExpandWidth(true));
            }

            GUILayout.EndHorizontal();
            return value;
        }

        /// <summary>
        /// Material Design 3 滑块
        /// </summary>
        public static float M3Slider(float value, float minValue, float maxValue, string label = "")
        {
            GUILayout.BeginVertical();

            if (!string.IsNullOrEmpty(label))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, _labelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Label(value.ToString("F2"), _labelSecondaryStyle, GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }

            // 滑块轨道
            Rect trackRect = GUILayoutUtility.GetRect(100, 4, GUILayout.ExpandWidth(true), GUILayout.Height(4));

            // 绘制轨道背景
            GUI.color = new Color(0.25f, 0.22f, 0.26f);
            GUI.DrawTexture(trackRect, GetCachedRoundedTex(64, 8, 2, Color.white));
            GUI.color = Color.white;

            // 绘制已填充部分
            float fillWidth = (value - minValue) / (maxValue - minValue) * trackRect.width;
            Rect fillRect = new(trackRect.x, trackRect.y, fillWidth, trackRect.height);
            GUI.color = Primary;
            GUI.DrawTexture(fillRect, GetCachedRoundedTex(64, 8, 2, Color.white));
            GUI.color = Color.white;

            // 滑块位置
            float thumbSize = 20;
            float thumbX = trackRect.x + (value - minValue) / (maxValue - minValue) * trackRect.width - thumbSize / 2;
            Rect thumbRect = new(thumbX, trackRect.y - (thumbSize - trackRect.height) / 2, thumbSize, thumbSize);

            // 绘制滑块
            GUI.color = Primary;
            GUI.DrawTexture(thumbRect, GetCachedRoundedTex(32, 32, 16, Color.white));
            GUI.color = Color.white;

            // 交互处理
            if (GUI.Button(trackRect, "", GUIStyle.none))
            {
                Vector2 mousePos = Event.current.mousePosition;
                float newValue = minValue + (mousePos.x - trackRect.x) / trackRect.width * (maxValue - minValue);
                value = Mathf.Clamp(newValue, minValue, maxValue);
            }

            GUILayout.EndVertical();
            return value;
        }

        /// <summary>
        /// Material Design 3 芯片（标签）
        /// </summary>
        public static void M3Chip(string label, bool selected = false, System.Action? onDelete = null)
        {
            GUILayout.BeginHorizontal();

            // 芯片背景
            Color bgColor = selected ? Primary : new Color(0.20f, 0.18f, 0.22f);
            Color textColor = selected ? OnPrimary : OnSurfaceVariant;

            GUIStyle chipStyle = new(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 32,
                padding = new RectOffset(12, onDelete != null ? 8 : 12, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                normal = { background = GetCachedRoundedTex(64, 32, 16, bgColor), textColor = textColor },
                hover = { background = GetCachedRoundedTex(64, 32, 16, selected ? new Color(1.0f, 0.7f, 0.82f) : new Color(0.25f, 0.22f, 0.26f)), textColor = Color.white }
            };

            if (GUILayout.Button(label, chipStyle, GUILayout.ExpandWidth(false)))
            {
                // 芯片点击事件可在此处理
            }

            // 删除按钮
            if (onDelete != null)
            {
                if (GUILayout.Button("×", GUILayout.Width(24), GUILayout.Height(32)))
                {
                    onDelete();
                }
            }

            GUILayout.EndHorizontal();
        }

        #region 纹理生成
        /// <summary>
        /// 获取缓存的圆角纹理
        /// </summary>
        public static Texture2D GetCachedRoundedTex(int width, int height, float radius, Color col, bool tl = true, bool tr = true, bool bl = true, bool br = true)
        {
            // 性能优化：使用整数和简化的颜色值作为key
            int colorKey = (int)(col.r * 255) << 24 | (int)(col.g * 255) << 16 | (int)(col.b * 255) << 8 | (int)(col.a * 255);
            int cornerKey = (tl ? 1 : 0) | (tr ? 2 : 0) | (bl ? 4 : 0) | (br ? 8 : 0);
            string key = $"{width}x{height}_r{radius}_c{colorKey}_{cornerKey}";
            
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
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pix = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 使用像素中心点 (x+0.5, y+0.5) 进行采样
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float alpha = 1.0f;

                    // 检查四个角
                    if (tl && px < radius && py < radius)
                    {
                        // 左上角
                        float dx = radius - px;
                        float dy = radius - py;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        alpha = Math.Max(0, Math.Min(1, radius - d + 0.5f));
                    }
                    else if (tr && px >= width - radius && py < radius)
                    {
                        // 右上角
                        float dx = px - (width - radius);
                        float dy = radius - py;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        alpha = Math.Max(0, Math.Min(1, radius - d + 0.5f));
                    }
                    else if (bl && px < radius && py >= height - radius)
                    {
                        // 左下角
                        float dx = radius - px;
                        float dy = py - (height - radius);
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        alpha = Math.Max(0, Math.Min(1, radius - d + 0.5f));
                    }
                    else if (br && px >= width - radius && py >= height - radius)
                    {
                        // 右下角
                        float dx = px - (width - radius);
                        float dy = py - (height - radius);
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        alpha = Math.Max(0, Math.Min(1, radius - d + 0.5f));
                    }

                    pix[y * width + x] = new Color(col.r, col.g, col.b, col.a * alpha);
                }
            }

            tex.SetPixels(pix);
            tex.Apply(false, false);
            return tex;
        }

        /// <summary>
        /// 生成纯色纹理
        /// </summary>
        public static Texture2D MakeSolidTex(int width, int height, Color col)
        {
            Texture2D tex = new(width, height);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            tex.SetPixels(pix);
            tex.Apply(false, false);
            return tex;
        }
        #endregion
    }
}

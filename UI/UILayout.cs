using UnityEngine;
using System.Collections.Generic;

namespace Iridium.UI
{
    /// <summary>
    /// UI 布局常量和工具方法
    /// 用于管理设置界面的左侧导航 + 右侧内容布局
    /// </summary>
    public static class UILayout
    {
        #region 侧边栏常量
        public const int SIDEBAR_WIDTH = 240;
        public const int SIDEBAR_ITEM_HEIGHT = 48;
        public const int SIDEBAR_PADDING = 12;
        public const int SIDEBAR_HEADER_HEIGHT = 60;
        #endregion

        #region 内容区域常量
        public const int CONTENT_PADDING = 24;
        public const int CONTENT_HEADER_HEIGHT = 80;
        public const int CONTENT_MIN_WIDTH = 400;
        #endregion

        #region 设置项常量
        public const int SETTING_ITEM_HEIGHT = 56;
        public const int SETTING_GROUP_SPACING = 24;
        public const int SETTING_GROUP_TITLE_HEIGHT = 32;
        public const int SETTING_ITEM_SPACING = 8;
        #endregion

        #region 颜色常量
        public static readonly Color SidebarBgColor = UIUtils.SidebarBackground;
        public static readonly Color SidebarHoverColor = new(0.18f, 0.10f, 0.18f);
        public static readonly Color SidebarActiveColor = new(0.55f, 0.30f, 0.48f);  // 更浅的粉色，增强对比度
        public static readonly Color SidebarInactiveColor = new(0.12f, 0.07f, 0.12f);
        public static readonly Color ContentBgColor = UIUtils.Surface;
        #endregion

        // 样式缓存
        private static GUIStyle? _sidebarActiveStyle;
        private static GUIStyle? _sidebarInactiveStyle;

        private static GUIStyle SidebarActiveStyle
        {
            get
            {
                if (_sidebarActiveStyle == null)
                {
                    _sidebarActiveStyle = new(GUI.skin.button)
                    {
                        fixedHeight = SIDEBAR_ITEM_HEIGHT,
                        margin = new RectOffset(SIDEBAR_PADDING, SIDEBAR_PADDING, 4, 4),
                        padding = new RectOffset(12, 12, 0, 0),
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 13,
                        normal = { background = UIUtils.GetCachedRoundedTex(32, 32, 14, Color.white), textColor = UIUtils.OnSurface },
                        hover = { background = UIUtils.GetCachedRoundedTex(32, 32, 14, Color.white), textColor = Color.white },
                        active = { background = UIUtils.GetCachedRoundedTex(32, 32, 14, Color.white), textColor = Color.white }
                    };
                }
                return _sidebarActiveStyle;
            }
        }

        private static GUIStyle SidebarInactiveStyle
        {
            get
            {
                if (_sidebarInactiveStyle == null)
                {
                    _sidebarInactiveStyle = new(GUI.skin.button)
                    {
                        fixedHeight = SIDEBAR_ITEM_HEIGHT,
                        margin = new RectOffset(SIDEBAR_PADDING, SIDEBAR_PADDING, 4, 4),
                        padding = new RectOffset(12, 12, 0, 0),
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 13,
                        normal = { background = UIUtils.GetCachedRoundedTex(32, 32, 14, Color.white), textColor = UIUtils.OnSurface },
                        hover = { background = UIUtils.GetCachedRoundedTex(32, 32, 14, Color.white), textColor = Color.white },
                        active = { background = UIUtils.GetCachedRoundedTex(32, 32, 14, Color.white), textColor = Color.white }
                    };
                }
                return _sidebarInactiveStyle;
            }
        }

        /// <summary>
        /// 绘制侧边栏项目 - 使用缓存样式和 GUI.backgroundColor
        /// </summary>
        public static bool DrawSidebarItem(string label, bool isActive, Texture2D? icon = null)
        {
            Color bgColor = isActive ? SidebarActiveColor : SidebarInactiveColor;

            Color originalBgColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;

            GUIStyle style = isActive ? SidebarActiveStyle : SidebarInactiveStyle;
            bool clicked = GUILayout.Button(label, style, GUILayout.ExpandWidth(true));

            GUI.backgroundColor = originalBgColor;
            return clicked;
        }

        /// <summary>
        /// 绘制设置分组标题
        /// </summary>
        public static void DrawSettingGroupTitle(string title)
        {
            GUILayout.Space(SETTING_GROUP_SPACING / 2);
            GUILayout.Label(title, UIUtils.SubHeaderStyle, GUILayout.Height(SETTING_GROUP_TITLE_HEIGHT));
            UIUtils.DrawDivider();
        }

        /// <summary>
        /// 绘制设置项容器
        /// </summary>
        public static void BeginSettingItem()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle, GUILayout.Height(SETTING_ITEM_HEIGHT));
        }

        /// <summary>
        /// 结束设置项容器
        /// </summary>
        public static void EndSettingItem()
        {
            GUILayout.EndVertical();
            GUILayout.Space(SETTING_ITEM_SPACING);
        }

        /// <summary>
        /// 绘制内容区域标题
        /// </summary>
        public static void DrawContentHeader(string title, string description = "")
        {
            GUILayout.BeginVertical(GUILayout.Height(CONTENT_HEADER_HEIGHT));

            GUILayout.Label(title, UIUtils.HeaderStyle);

            if (!string.IsNullOrEmpty(description))
            {
                GUILayout.Label(description, UIUtils.LabelSecondaryStyle);
            }

            UIUtils.DrawDivider();
            GUILayout.EndVertical();
        }
    }
}

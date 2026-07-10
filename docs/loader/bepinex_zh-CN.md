# BepInEx 安装指南

Iridium 自 1.4.0_beta5 版本起支持 BepInEx。

## 前置要求

- 已安装 [BepInEx](https://docs.bepinex.dev/)（推荐最新稳定版）
- ADOFAI 游戏本体

## 安装步骤

1. 前往 [Releases](https://github.com/Xbodwf/Iridium/releases) 下载与你的 ADOFAI 游戏版本匹配的 Iridium 构建。

2. 将下载的文件解压到：`A Dance of Fire and Ice/BepInEx/plugins/Iridium/`

> [!IMPORTANT]
> 与 UnityModManager 不同，BepInEx 从 `BepInEx/plugins/` 目录加载模组——请将所有文件放置在 `A Dance of Fire and Ice/BepInEx/plugins/Iridium/` 子文件夹下。

3. 启动游戏，Iridium 将通过 BepInEx 自动加载。

## 使用说明

由于没有 UnityModManager 那样的内置管理器界面，按下已配置的热键来打开或关闭 Iridium 设置面板。默认热键为 **Ctrl+F9**。

> [!TIP]
> 如需修改热键，请编辑 Iridium 文件夹下的 `Settings.xml`（如 `BepInEx/plugins/Iridium/Settings.xml`）中的 `panelToggleHotkey` 字段。例如，将其改为 `"Alt+O"`。

# MelonLoader 安装指南

Iridium 自 1.3.0_beta1 版本起支持 MelonLoader。

## 前置要求

- 已安装 [MelonLoader](https://melonloader.net/)（推荐最新稳定版）
- ADOFAI 游戏本体

## 安装步骤

1. 前往 [Releases](https://github.com/Xbodwf/Iridium/releases) 下载与你的 ADOFAI 游戏版本匹配的 Iridium 构建。

2. 将下载的文件直接解压到：`A Dance of Fire and Ice/Mods/`

> [!IMPORTANT]
> 与 UnityModManager 不同，MelonLoader 直接从 `Mods/` 目录加载模组——**不要**创建 `Iridium` 子文件夹。将 `Iridium.Loader.Melon.dll` 及其他所有文件直接置于 `A Dance of Fire and Ice/Mods/` 下即可。

3. 启动游戏，Iridium 将通过 MelonLoader 自动加载。

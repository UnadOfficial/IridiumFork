# BepInEx Installation Guide

Iridium has supported BepInEx since version 1.4.0_beta5.

## Prerequisites

- [BepInEx](https://docs.bepinex.dev/) installed (latest stable recommended)
- ADOFAI game

## Installation

1. Go to [Releases](https://github.com/Xbodwf/Iridium/releases) and download the Iridium build that matches your ADOFAI version.

2. Extract the downloaded contents into: `A Dance of Fire and Ice/BepInEx/plugins/Iridium/`

> [!IMPORTANT]
> Unlike UnityModManager, BepInEx loads plugins from `BepInEx/plugins/` — place all files inside an `Iridium` subfolder under `A Dance of Fire and Ice/BepInEx/plugins/`.

3. Launch the game. Iridium will be loaded automatically via BepInEx.

## Usage

Since there is no built-in UI manager like UnityModManager, press the configured hotkey to open or close the Iridium settings panel. The default hotkey is **Ctrl+F9**.

> [!TIP]
> To change the hotkey, edit the `panelToggleHotkey` field in `Settings.xml` located in the Iridium mod folder (e.g. `BepInEx/plugins/Iridium/Settings.xml`). For example, change it to `"Alt+O"`.

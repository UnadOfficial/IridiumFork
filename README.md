![Scratch Compiler](https://socialify.git.ci/Xbodwf/Iridium/image?custom_description=An+optimized+mod+for+ADOFAI&custom_language=csharp&description=1&font=Lexend&forks=1&issues=1&language=1&name=1&pulls=1&stargazers=1&theme=Auto)


# Iridium

An optimized mod for A Dance of Fire and Ice, focusing on performance, visual customization, and compatibility.

[简体中文](#简体中文) | [English](#english-description)

---

## 简体中文

> [!IMPORTANT]
> Iridium 旨在通过更好的内存管理、极致的性能优化和现代化的视觉调整来提升您的《冰与火之舞》体验。

### 通用部署
1.首先去Releases下载最新稳定发行版。如果您想体验新功能，可以下载最新的beta或者prerelease.

2.确保您已安装UnityModManager

> [!WARNING]
> 为了您使用Iridium的稳定性,建议使用0.27.0.0以上版本的UMM

3.解压下载好的文件 到 游戏目录(与`A Dance of Fire And Ice_Data`同级)/Mods/Iridium (不存在就创建)

4.启动游戏。若游戏已运行，重启游戏。

### 构建部署。

1.首先确保您已安装.NET SDK.

2.克隆本项目
```bash
git clone https://github.com/Xbodwf/Iridium.git Iridium
cd Iridium
```
3.在[Iridium.csproj](Iridium.csproj)中设定游戏目录

4.使用dotnet构建并部署到游戏
```bash
dotnet build
```

## English Description

> [!IMPORTANT]
> Iridium is designed to elevate your ADOFAI experience through better memory management, extreme performance optimization, and modern visual enhancements.

### Universal Deployment
1.Download this project's release build.If you would like to experience new features,you can download latest beta build or prerelease build.

2.Make sure that you have installed UnityModManager to ADOFAI.

3.Unzip file you downloaded to the game folder(which has a children folder named `A Dance of Fire And Ice_Data`)/Mods/Iridium (mkdir if it does not exist)

4.Launch the game.Restart if it has started.

### Build Deployment

1.Make sure that you have installed .NET SDK.

2.Clone this project:
```bash
git clone https://github.com/Xbodwf/Iridium.git Iridium
cd Iridium
```

3.Set the game folder in [Iridium.csproj](Iridium.csproj)

4.Build Iridium with dotnet and deploy it to the game:
```bash
dotnet build
```


## Project Structure
- `Main.cs`: Entry point
- `Settings.cs`: Root configuration & UI rendering
- `Settings/`: Modular sub-settings classes
- `UI/`: Material 3 UI utilities and styles
- `Patches/`: Feature-specific Harmony patches
- `Resources/`: Assets & Localization

## Developing Environment
- IDE: Visual Studio 2022 / Rider 2025.3
- ADOFAI 2.9.7~2.9.8

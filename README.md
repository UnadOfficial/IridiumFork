![Iridium](https://socialify.git.ci/Xbodwf/Iridium/image?custom_description=An+optimized+mod+for+ADOFAI&custom_language=csharp&description=1&font=Lexend&forks=1&issues=1&language=1&name=1&pulls=1&stargazers=1&theme=Auto)


# Iridium

An optimized mod for A Dance of Fire and Ice, focusing on performance, visual customization, and compatibility.

[Chinese](README_zh-CN.md)

---

## English

> [!IMPORTANT]
> Iridium is designed to enhance your "A Dance of Fire and Ice" experience through better memory management, extreme performance optimization, and modern visual adjustments.

### General Deployment
1. First, go to Releases to download the latest stable release. If you want to experience new features, you can download the latest beta or prerelease.

2. Ensure that you have UnityModManager installed.

> [!WARNING]
> For the stability of Iridium, it is recommended to use UMM version 0.27.0.0 or higher.


3. Extract the downloaded files to: `Game Directory (same level as A Dance of Fire And Ice_Data)/Mods/Iridium` (Create the folder if it does not exist).

4. Launch the game. If the game is already running, restart it.

> [!Caution]
> Unless it is a specially tuned version of the mod released by maintainers for older versions of the game, do not attempt to run Iridium on older ADOFAI versions (<=2.9.7). We do not guarantee functional stability or compatibility in such cases.

### Build and Deployment

1. First, ensure you have the .NET SDK installed.

2. Clone this project:
```bash
git clone https://github.com/Xbodwf/Iridium.git Iridium
cd Iridium
```
3. Set your game directory in [Iridium.csproj](Iridium.csproj).

4. Use dotnet to build and deploy to the game:
```bash
dotnet build
```

## Special Thanks
Thanks to their contributions:

<a href="https://github.com/Xbodwf/Iridium/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Xbodwf/Iridium&max=200&columns=14" />
</a>

For other contributors, see [Contributors.md](Contributors.md)

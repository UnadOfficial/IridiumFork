![Iridium](https://socialify.git.ci/Xbodwf/Iridium/image?custom_description=An+optimized+mod+for+ADOFAI&custom_language=csharp&description=1&font=Lexend&forks=1&issues=1&language=1&name=1&pulls=1&stargazers=1&theme=Auto)


# Iridium

一个专注于性能优化、视觉调整和兼容性的A Dance of Fire and Ice优化Mod

[English](README.md)

---

## 简体中文

> [!IMPORTANT]
> Iridium 旨在通过更好的内存管理、极致的性能优化和现代化的视觉调整来提升您的《冰与火之舞》体验。

### 通用部署
1.首先去Releases下载最新稳定发行版。如果您想体验新功能，可以下载最新的beta或者prerelease.

2.确保您已安装UnityModManager

> [!WARNING]
> 为了您使用Iridium的稳定性,建议使用0.27.0.0以上版本的UMM.


3.解压下载好的文件 到 游戏目录(与`A Dance of Fire And Ice_Data`同级)/Mods/Iridium (不存在就创建)

4.启动游戏。若游戏已运行，重启游戏。

> [!Caution]
> 除非是经过维护者推出的适用于旧版游戏的特调版Mod，否则不要轻易在旧版ADOFAI(<=2.9.7)运行Iridium.若出现此行为我们不保障功能稳定性和兼容性。

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

## Special Thanks
感谢他们的贡献:

<a href="https://github.com/Xbodwf/Iridium/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Xbodwf/Iridium&max=200&columns=14" />
</a>

其余的贡献者见[Contributors.md](Contributors.md)

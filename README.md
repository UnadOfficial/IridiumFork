# Iridium

An optimized mod for A Dance of Fire and Ice, focusing on performance, visual customization, and compatibility.

[简体中文](#简体中文) | [English](#english-description)

---

## 简体中文

> [!IMPORTANT]
> Iridium 旨在通过更好的内存管理、极致的性能优化和现代化的视觉调整来提升您的《冰与火之舞》体验。

### 核心功能

#### 图形与性能优化
- **纹理动态优化**：在关卡加载时智能调整并压缩装饰物纹理，极大地缓解显存 (VRAM) 压力。
- **渲染器精简**：自动优化地砖 (Tile) 和装饰物的渲染器设置（如禁用冗余的阴影接收和光照探针），在不影响视觉效果的前提下提升帧率。
- **智能缩放适配**：自动同步调整碰撞箱与渲染缩放，确保在享受优化的同时，判定逻辑依然严丝合缝。
- **异步内存管理**：智能 GC 逻辑，在切换场景时自动清理内存，防止长时间游戏导致的卡顿。
- **显存节省统计**：每次加载完成后，系统将通过通知告知您节省的具体显存容量。

#### 兼容性与修复
- **旧版暂停逻辑 (2.9.3)**：还原了 2.9.4 版本之前的 U 型转弯暂停行为，让老关卡重现其原本的节奏设计。
- **强制 angleData 注入**：为旧版谱面强制注入角度数据，强制使用Mesh.这可以使CircleArc在旧谱面生效并且支持自定义角度。
- **旧版行为模拟**：支持模拟旧版的闪烁 (Flash) 和摄像机相对行为 (Camera Relative)，确保老谱面的视觉体验原汁原味。
- **不死模式智能判定**：在不死模式下，致死装饰物碰撞将自动转换为“太快了 (FailOverload)”判定，帮助您更有效地练习。
- **强制难度 UI**：在所有 CLS 关卡中启用完整的难度选择界面。

#### 视觉自定义
- **拖尾深度定制**：支持手动调节行星拖尾的长度、发射密度，并提供**音高跟随模式**。
- **动态圆弧转角**：还原并增强了极具动感的转角圆弧视觉效果，支持 90°~105° 的灵活范围。
- **界面净化**：支持隐藏选关界面的官方新闻容器，回归极简视觉。

#### 现代化 UI
- **Material 3 设计**：基于 M3 规范的设置面板。

---

## English Description

> [!IMPORTANT]
> Iridium is designed to elevate your ADOFAI experience through better memory management, extreme performance optimization, and modern visual enhancements.

### Key Features

#### Graphic & Performance Optimizer
- **Dynamic Texture Optimization**: Resizes and compresses decoration textures during level load to significantly reduce VRAM usage.
- **Renderer Streamlining**: Optimizes renderer settings for Tiles and Decorations (e.g., disabling redundant shadow receiving and light probes) to boost FPS without visual loss.
- **Smart Scaling**: Automatically synchronizes colliders and render scales, ensuring gameplay precision is never compromised by optimization.
- **Async Memory Management**: Smart GC logic that automatically cleans up memory during scene transitions to prevent stuttering.
- **VRAM Tracker**: Receive instant notifications showing the exact amount of memory saved after each load.

#### Compatibility & Fixes
- **Legacy Pause (2.9.3)**: Restores the pre-2.9.4 U-turn pause behavior, allowing classic levels to play as originally intended.
- **Forced angleData Injection**: Forces angle data injection for legacy charts and enables Mesh rendering. This allows "Circle Arc" visuals to work on old levels and supports custom angles.
- **Legacy Behavior Emulation**: Options to emulate legacy Flash and Camera Relative behaviors for an authentic retro experience.
- **No-Fail Judgment Conversion**: Lethal decoration hits are converted into "FailOverload" (Too Early) during No-Fail mode for better feedback.
- **Force Difficulty UI**: Forces the full difficulty selection UI to appear in all CLS levels.

#### Visual Customization
- **Advanced Tail Tweaks**: Manually adjust tail length and emission, or enable the dynamic **Pitch-Follow Mode**.
- **Dynamic Circle Arc Corners**: Restores and enhances smooth circular arc visuals for corners, supporting a flexible range of 90°~105°.
- **UI Decutter**: Option to hide the official news container in the level select screen for a cleaner look.

#### Modern UI
- **Material 3 Design**: A settings menu built on M3 principles, featuring a multi-column card layout for smooth and intuitive interaction.

---

## Project Structure
- `Main.cs`: Entry point
- `Settings.cs`: Root configuration & UI rendering
- `Settings/`: Modular sub-settings classes
- `UI/`: Material 3 UI utilities and styles
- `Patches/`: Feature-specific Harmony patches
- `Resources/`: Assets & Localization


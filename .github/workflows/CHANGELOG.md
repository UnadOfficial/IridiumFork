> [!IMPORTANT]
> 若您在使用Iridium时出现问题，请及时向维护者报告。

### 新增

1. 新增: 初步支持 MelonLoader（需要 MelonLoader v0.7.3+）
2. 新增: 构建时通过 `-p:Loader=ML` 或 `-p:Loader=UMM` 选择加载器部署

### 改进

1. 改进: 将 `IHandler` 接口从 `Iridium.Loader.UMM` 移至主 `Iridium.dll`，解耦加载器依赖
2. 改进: UMM 入口从 `Main.Load` 迁移至 `UmmEntry.Load`，主模块不再反向依赖加载器
3. 改进: 修复 MelonLoader 缺少 `Microsoft.CSharp` 依赖的问题

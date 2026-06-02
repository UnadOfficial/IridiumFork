> [!IMPORTANT]
> 若您在使用Iridium时出现问题，请及时向维护者报告。

## Iridium 1.2.2-nightly1

### 修复

1. 修复: 合作模式下每位玩家独立的暂停节拍锁定（Pause Beat Lock）
2. 修复: Twirl 后空敲（AirHit）误将计量器方向相反的问题
3. 修复: Release 工作流无先前 release tag 时回退到最新 tag

### 优化

1. 优化: Release tag 使用 `_final` 后缀标识稳定版，不再排除 beta/prerelease
2. 文档: 更新 README 详细功能描述
3. 更新: 适配最新 v2.10.0 Assembly-CSharp.dll

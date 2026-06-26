## r41_nightly

### 重构 & 修复 / Refactoring & Fixes / 리팩토링 및 수정

- **统一版本号读取**：将 MelonLoader、VersionManager 的版本号统一为 `BuildInfo.ModVersion` 单一数据源，移除 MelonHandler 中的硬编码版本号和 JSON 解析逻辑
- **Unified version source**: Consolidated version strings across MelonLoader, VersionManager, and Info.json into a single source of truth (`BuildInfo.ModVersion`); removed hardcoded version strings and JSON parsing from MelonHandler
- **통합 버전 읽기**: MelonLoader, VersionManager, Info.json의 버전 문자열을 `BuildInfo.ModVersion` 단일 데이터 소스로 통합, MelonHandler의 하드코딩된 버전 문자열 및 JSON 파싱 로직 제거

- **修复大厅音乐重叠问题**：修复了自定义大厅音乐在加速模式下普通音乐与快速音乐同时播放的 Bug，添加了正确的 Stop/Play 控制和音量切换逻辑
- **Fixed lobby music overlap**: Fixed custom lobby music bug where normal and fast-forward tracks played simultaneously; added proper Stop/Play control and volume toggling logic
- **로비 음악 중첩 수정**: 커스텀 로비 음악이 가속 모드에서 일반 음악과 고속 음악이 동시에 재생되는 버그 수정, 올바른 Stop/Play 제어 및 볼륨 전환 로직 추가

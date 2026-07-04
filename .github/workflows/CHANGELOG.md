## r41_nightly4

> [!Info]
> Iridium 从 1.3.0_beta1 开始就已经支持 MelonLoader，有关 Iridium 在 MelonLoader 上的安装，请查阅 README.md

### 改进 / Improvements / 개선

- **韩语支持**：新增 kr.json 韩语翻译，设置界面语言选择按钮下方增加提示区域
- **Korean language support**: Added kr.json with Korean translations; hint area below language selector in settings
- **한국어 지원**: kr.json 한국어 번역 추가, 설정 언어 선택 버튼 아래 힌트 영역 추가

- **安装文档重构**：按 Mod Loader 分流到 docs/loader/，新增 MelonLoader 安装指南
- **Installation docs restructured**: Redirected to loader-specific guides in docs/loader/; added MelonLoader guide
- **설치 문서 재구성**: Mod Loader별로 docs/loader/로 분기, MelonLoader 설치 가이드 추가

- **Patches 清理重构**：移除死代码(TweenBatchQueue/空 Patch)，修复冲突 Patch 互斥逻辑，统一 tab 缩进
- **Patches cleanup**: Removed dead code (TweenBatchQueue/empty patches), added mutual exclusion for conflicting patches, unified tab indentation
- **Patches 정리**: 죽은 코드 제거(TweenBatchQueue/빈 Patch), 충돌 Patch 상호 배제 로직 추가, 탭 들여쓰기 통일

### 修复 / Bug Fixes / 버그 수정

- **编辑器快捷键不响应**：修复 else-if 链导致修饰键组合被前置匹配项拦截的问题
- **Editor shortcuts not firing**: Fixed else-if chain blocking modifier key combos when earlier match intercepted
- **편집기 단축키 미응답**: else-if 체인으로 인해 수정자 키 조합이 선행 항목에 차단되던 문제 수정

- **切换场景时清理内存导致 scnEditor 崩溃**：移除此功能
- **Clean on scene switch crashing scnEditor**: Removed this feature
- **씬 전환 시 메모리 정리로 scnEditor 충돌**: 해당 기능 제거

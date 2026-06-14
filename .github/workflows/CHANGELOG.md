### 新增

1. MelonLoader 面板改为 Iridium UI 风格（圆角深色背景），添加 × 关闭按钮
2. 面板拖拽限制在标题栏，修复 Tab/开关/滚动条无法点击的问题
3. UI 纹理预加载至初始化阶段，消除首次打开面板卡顿
4. CoopPauseLockFix 改为运行时反射探测 LockInput 目标（scrPlayer ≥3.1.2 / scrController ≤3.1.1）

### Added

1. MelonHandler panel restyled with Iridium UI (rounded dark bg), added close button
2. Drag restricted to title bar — fixed Tab/Switch/scrollbar not responding to clicks
3. Pre-load UI textures at init to eliminate first-open lag
4. CoopPauseLockFix: runtime reflection for LockInput target (scrPlayer ≥3.1.2 / scrController ≤3.1.1)

### 추가

1. MelonHandler 패널 Iridium UI 스타일로 변경 (둥근 어두운 배경), 닫기 버튼 추가
2. 드래그를 타이틀바로 제한 — Tab/스위치/스크롤바 클릭 불가 문제 해결
3. UI 텍스처 초기화 단계에서 미리 로드하여 첫 열기 지연 제거
4. CoopPauseLockFix: 런타임 리플렉션으로 LockInput 대상 감지 (scrPlayer ≥3.1.2 / scrController ≤3.1.1)

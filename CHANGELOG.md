# Iridium Changelog

## r31_prerelease2

### Bug 修复 / Bug Fixes / 버그 수정

- **装饰压缩等比修复**：修复了 `AlignTo4()` 分别对宽高取整导致的不等比缩放问题，改用两轴平均值作为统一缩放比，确保装饰物视觉尺寸正确
- **Decoration compression aspect-ratio fix**: Fixed non-uniform scaling caused by independent `AlignTo4()` rounding on width/height; now uses averaged ratio for consistent visual size
- **장식 압축 비율 수정**: `AlignTo4()`가 너비/높이에 독립적으로 적용되어 발생하는 비등비 스케일링 문제 수정, 두 축 평균값을 통일 비율로 사용하여 시각적 크기 정상화

### 性能优化 / Performance Optimization / 성능 최적화

- **自定义缓速引擎兼容性优化**：修复了 `null.Kill()` 导致的 NRE（装饰物位置漂移/跳变），优化 Update 循环为双指针原地压缩算法，消除密集事件场景下的 O(n*k) 数组移位开销
- **Custom easing engine compatibility fix**: Fixed NRE from `null.Kill()` causing decoration position drift/jump; optimized Update loop with two-pointer in-place compaction, eliminating O(n*k) array shift overhead in dense event scenarios
- **사용자 정의 감속 엔진 호환성 개선**: `null.Kill()`로 인한 NRE(장식물 위치 드리프트/점프) 수정, 업데이트 루프를 이중 포인터 제자리 압축 알고리즘으로 최적화하여 밀집 이벤트 시나리오의 O(n*k) 배열 시프트 오버헤드 제거

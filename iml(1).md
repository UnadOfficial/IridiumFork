IML (Iris Markup Language) 规范文档 v2.0

版本：2.0
状态：草案
目标平台：Unity + IMGUI（兼容 UI Toolkit 后续扩展）
设计哲学：为 Unity 游戏 Mod 开发量身定制的 声明式 UI 语言，是 HTML + CSS + JSX 的平替，并在安全性、性能、Unity 集成方面实现超越。

---

1. 设计目标

1. UI 与逻辑彻底分离
   · 界面完全用 .iml 文件描述，C# 仅保留数据模型和业务逻辑。
   · 消除 52KB 量级的 Begin/End 样板代码，将 UI 代码量减少 80%～90%。
2. 安全沙箱
   · {} 表达式 禁止 调用 C# 方法、反射、new，仅允许属性访问、算术/逻辑/比较运算。
   · 事件绑定采用白名单映射（字符串处理程序名或 ICommand 属性），杜绝代码注入。
3. 立即生效 + 智能 Diff
   · 双向数据绑定保证 UI 修改即时写回 Settings。
   · 属性级、列表级 Diff 只在值 实际变化 时触发副作用（保存文件、重载补丁），避免每帧重复执行。
   · 副作用批处理（EffectScheduler）合并同一帧内的多次变更，最终统一提交。
4. Unity 原生集成
   · 直接加载 AssetBundle、Addressables、Resources 中的纹理、字体、预制体。
   · 支持自定义 Canvas 回调，利用 Unity GL/Graphics 绘制 2D 图形。
5. 开发体验优先
   · 热重载：修改 .iml 文件后按 Ctrl+R 即时刷新 UI（无需重启游戏）(这个功能默认关闭)。
   · 可视化调试器：运行中选中 UI 节点，定位到源文件行号，实时调整样式属性。

---

2. 文件结构与基本语法

2.1 文件扩展名与编码

· 文件扩展名：.iml
· 编码：UTF-8（无 BOM）

2.2 根元素

每个 .iml 文件必须且只有一个根元素 <Iris>。

```iml
<Iris>
    <!-- 内容 -->
</Iris>
```

2.3 注释

使用 <!-- 注释 -->，不支持嵌套。

---

3. 外部资源引用（@ 与 <Reference>）

3.1 引用其他 IML 文件（组件化）

使用 <Reference> 标签声明依赖，禁而非.NET csproj式<Include>。

```iml
<References>
    <Reference path="@subviews/AdvancedOptions.iml" />
    <Reference path="@styles/CommonStyles.iml" alias="Styles" />
</References>
```

· path 属性：以 @ 开头，基于当前文件路径解析（@../ 表示上级目录，@/ 表示 Mod 根目录）。
· alias（可选）：导入后可通过 alias 访问其导出的资源（<Style name="Styles.Card" />）。

3.2 外部资源路径约定

前缀 解析基准 示例
@/ Mod 根目录（Main.ModPath） @/assets/logo.png
@ 当前 .iml 所在目录 @images/icon.png
bundle:// Unity AssetBundle bundle://ui_pack#icon_gear
* 支持自定义ab包路径，UI加载事情动态加载，UI不使用时自动卸载，ab包加载异步，如果没有加载完成则使用占位资源，iml的逻辑判断里应该自行判定纹理是否加载完成，再将使用资源的节点显示出来。

addr:// Unity Addressables addr://Prefabs/3DViewer

---

4. 本地资源定义（<Resources>）

在 <Resources> 中定义可复用样式、模板、数据模板。

4.1 样式（<Style>）

类似 CSS 类，支持继承：

```iml
<Style name="Card" extends="BasePanel">
    <Setter property="background">#151617</Setter>
    <Setter property="radius">16</Setter>
    <Setter property="padding">12</Setter>
</Style>
```

· extends（可选）：继承父样式的所有属性，可覆盖。
· 属性名：background（颜色或纹理）、radius、border、borderColor、padding、margin、width、height、fontSize、color 等。

4.2 模板（<Template>）

用于循环渲染（<ForEach>）：

```iml
<Template name="OptionItem">
    <HBox>
        <Text text="{item.name}" />
        <Switch value="{item.enabled}" on-changed="OnItemToggled" />
    </HBox>
</Template>
```

模板内可使用 item 作为循环项的上下文变量。

---

5. 核心基础组件

所有组件均支持样式绑定（style 或 class 属性）。

组件 语义 关键属性
<View> 块级容器（类似 <div>） background, radius, border, padding, margin, width, height, visible
<HBox> 水平 Flex 容器 align, justify, gap, wrap
<VBox> 垂直 Flex 容器 同上
<Text> 文本渲染 text（支持模板字符串和Unity富文本）, font（@外部字体）, size, color, align
<Image> 图片/纹理渲染 source（@外部或 {} 表达式）, width, height, tint, scaleMode（Stretch/Fit/Crop）
<Button> 可点击按钮 text 或子内容, command（ICommand）, on-click（处理程序映射）
<Switch> 开关控件 value（{} 双向绑定）, on-changed
<Checkbox> 复选框 同 Switch
<Slider> 滑块 value, min, max, step, showValue, on-changed
<TextField> 单行文本输入 value: string, on-text-submit, accept: Regex(以string形式传入)
<TextArea> 多行文本输入 value: string, lines, on-text-submit
<FileInput> 允许用户上传一个或多个文件，并且触发回调 files: List<File>,on-file-upload,on-file-changed,value(展示的内容 可以理解为html input[file])
<Separator> 分割线 无
<If> 条件渲染 condition（{} 表达式）
<ForEach> 循环渲染 items（{} 集合）, key（用于 Diff 优化）, template（可选）

---

6. 属性值语法：JSX 风格

6.1 四种属性值类型

写法 类型 C# 解析结果
value={expr} 表达式（无引号） Expression 节点，直接求值
text="Hello" 纯字符串 LiteralString
text="Hello {name}" 模板字符串（带插值） 分段：["Hello ", Expression("name")]

需要处理转义的情况。

disabled 布尔简写 等价于 disabled={true}

6.2 转义规则

因为 { 和 } 是插值/表达式定界符，必须提供转义：

转义序列 输出
\\ \
\{ {
\} }
\" "（仅在字符串内）

示例：

```iml
<Text text="用户名为 \{name\}" />          <!-- 输出：用户名为 {name} -->
<Text text="Hello \{user.name\}" />        <!-- 输出：Hello {user.name} -->
<Text text="当前值：{value}" />            <!-- 动态插值 -->
```

---

7. 数据绑定与表达式（{}）

7.1 绑定路径

· 点分隔属性路径：optimizer.enableOptimizer
· 索引访问：list[0]、dict["key"]
· 当前循环项：item.name（在 <ForEach> 内）

7.2 允许的运算符

· 算术：+, -, *, /, %
· 比较：==, !=, >, <, >=, <=
· 逻辑：&&, ||, !
· 三元：condition ? true_expr : false_expr
· 字面量：数字, 字符串（单双引号）, true, false, null

7.3 严禁语法（沙箱拦截）

· 方法调用：obj.Method()
· new 关键字
· typeof / GetType
· Lambda 表达式
· 赋值（= 只允许在绑定属性中隐式写入）

解析器在词法阶段检测到括号后跟标识符（非运算符）即抛出 SecurityException。

---

8. 事件绑定

8.1 两种绑定模式

模式 IML 写法 C# 端注册方式 适用场景
命令绑定 command="{SaveCommand}" 数据上下文提供 ICommand 属性 复杂逻辑、带参数、可测
处理程序映射 on-changed="OnToggle" 渲染器 RegisterHandler("OnToggle", action) 轻量回调，快速实现

8.2 支持的事件属性

属性 触发时机
on-click 鼠标左键点击
on-changed Switch/Checkbox/Slider 值变化
on-key-down / on-key-up 键盘按下/释放
on-hover / on-hover-exit 鼠标悬停/离开
on-text-submit TextField 回车
on-loaded 节点首次渲染完成

8.3 事件冒泡

父容器可捕获子事件，通过 event.targetId 区分来源（需渲染器支持）。

---

9. 布局引擎

9.1 Flexbox 子集

· <HBox>：水平排列，align（交叉轴对齐）、justify（主轴对齐）、gap（间距）。
· <VBox>：垂直排列，属性同上。

9.2 约束布局（增强）

支持类似 Android ConstraintLayout 的约束语法，解决复杂居中问题：

```iml
<View constraints="parent.center">
    <Text text="绝对居中" />
</View>

<View width="{parent.width * 0.5 - 20}" height="100" />
```

· 约束表达式在布局阶段求值，精确计算像素。

---

10. 高级特性

10.1 状态驱动的样式选择（<StyleSelector>）

根据数据状态自动切换样式：

```iml
<View state="{connectionStatus}">
    <StyleSelector>
        <Case value="Connected">
            <Setter property="background">#00FF00</Setter>
        </Case>
        <Case value="Error">
            <Setter property="background">#FF0000</Setter>
        </Case>
    </StyleSelector>
    <Text text="{statusText}" />
</View>
```

10.2 插槽（<Slot>）

用于模板化组件：

```iml
<Template name="Dialog">
    <View>
        <Slot name="header" />
        <Slot name="body" />
        <Slot name="footer" />
    </View>
</Template>

<!-- 使用 -->
<Dialog>
    <Slot name="header"><Text text="警告" /></Slot>
    <Slot name="body"><Text text="确认删除？" /></Slot>
    <Slot name="footer"><Button text="确定" command="{DeleteCmd}" /></Slot>
</Dialog>
```

10.3 自定义 Unity 组件（<UnityComponent>）

嵌入预制体或自定义 C# 控件：

```iml
<UnityComponent prefab="addr://Prefabs/3DModelViewer" width="200" height="200" />
```

10.4 自定义画布（<CustomCanvas>）

绑定 C# 绘制回调：

```iml
<CustomCanvas on-draw="DrawRadarChart" width="100" height="100" />
```

C# 注册：renderer.RegisterDrawHandler("DrawRadarChart", (rect, context) => { /* Unity Graphics 绘制 */ })

---

11. Diff 更新机制（类似 React）

11.1 属性级 Diff

· 每次渲染前比较 {} 表达式的当前值与缓存值。
· 若变化，触发 on-changed 事件（但不立即执行，进入批处理队列）。

11.2 列表级 Diff（Keyed）

· <ForEach key="{item.id}"> 必须提供 key，用于增量更新。
· 新增/删除/移动项时，仅更新变化的子节点，保留其他节点状态（如焦点、输入内容）。

11.3 副作用批处理（EffectScheduler）

· 所有 on-changed 在帧末统一执行，合并为一次保存文件和补丁重载。

---

12. 调试与热重载

12.1 热重载

· 监听 .iml 文件变化（通过 FileSystemWatcher）。
· 按 Ctrl+R 手动触发重新解析和重绘（无需重启游戏）。

12.2 可视化 Inspector

· 按住 Ctrl 点击 UI 元素，高亮显示对应源文件行号。
· 在 Inspector 面板中实时调整属性（padding, color 等），自动同步到内存或写入文件。

12.3 时间旅行调试

· 记录每次 on-changed 触发的数据状态快照。
· 可回退到任意历史状态，精准定位问题。

---

13. C# 端集成 API

13.1 初始化

```csharp
var renderer = new IrisRenderer();
renderer.LoadFile("UI.iml");
renderer.SetDataContext(new MySettings());  // 提供 {} 绑定的数据源
renderer.RegisterHandler("OnToggle", () => { /* 处理逻辑 */ });
renderer.RegisterHandler("OnItemToggled", (string id) => { /* ... */ });

// 在 OnGUI 中调用
renderer.OnGUI();
```

13.2 数据上下文（BindingContext）

· 通过反射或表达式树访问属性。
· 支持嵌套对象，自动监听 INotifyPropertyChanged（可选）。

13.3 命令（ICommand）

· 内置 RelayCommand 辅助类。
· 支持 CanExecute 自动控制按钮启用状态。

---

14. 安全性保障总结

层级 措施
表达式 仅允许白名单运算符，拦截方法调用和 new
事件字符串 不执行任意字符串代码，仅通过预注册字典路由
外部资源 @ 路径限制在 Mod 目录内（防止目录遍历攻击）
热重载 仅重新解析 IML，不重新加载 C# 代码（避免任意代码执行）

---

15. 完整示例

以下是一个包含本规范所有特性的 IML 文件：

```iml
<Iris>
    <!-- ====== 外部引用 ====== -->
    <References>
        <Reference path="@styles/Theme.iml" alias="Theme" />
        <Reference path="@subviews/UserProfile.iml" />
    </References>

    <!-- ====== 本地资源 ====== -->
    <Resources>
        <Style name="Card" extends="Theme.Card">
            <Setter property="padding">20</Setter>
            <Setter property="radius">12</Setter>
        </Style>

        <Template name="PluginItem">
            <HBox gap="8" align="center">
                <Image source="{item.icon}" width="24" height="24" />
                <Text text="{item.displayName}" style="Theme.Label" width="120" />
                <Switch value="{item.enabled}" on-changed="OnPluginToggled" />
                <Button text="详情" command="{ViewDetailCmd}" command-parameter="{item.id}" />
            </HBox>
        </Template>
    </Resources>

    <!-- ====== 主 UI ====== -->
    <VBox style="Card" gap="12">
        
        <!-- 标题：模板字符串 + 表达式 -->
        <Text text="欢迎，{user.name}（等级 {user.level}）" style="Theme.Title" />

        <!-- 状态指示器（StyleSelector） -->
        <View state="{connectionStatus}">
            <StyleSelector>
                <Case value="Online">
                    <Setter property="background">#00CC66</Setter>
                    <Text text="● 在线" color="#FFFFFF" />
                </Case>
                <Case value="Offline">
                    <Setter property="background">#CC3333</Setter>
                    <Text text="● 离线" color="#FFFFFF" />
                </Case>
            </StyleSelector>
        </View>

        <!-- 主开关（立即生效） -->
        <Switch text="启用优化器" value="{optimizer.enableOptimizer}" on-changed="OnOptimizerToggled" />

        <!-- 条件渲染 -->
        <If condition="{optimizer.enableOptimizer}">
            <Separator />
            
            <!-- 滑块（双向绑定 + 实时预览） -->
            <HBox>
                <Text text="压缩质量" />
                <Slider value="{optimizer.lossyQuality}" min="10" max="100" showValue="true" on-changed="OnQualityChanged" />
            </HBox>

            <!-- 循环渲染插件列表（带 Key，支持 Diff） -->
            <ForEach items="{optimizer.plugins}" key="{item.guid}" template="PluginItem" />
        </If>

        <!-- 按钮：命令绑定 -->
        <Button command="{SaveAllCommand}">
            <HBox align="center" gap="4">
                <Image source="@icons/save.png" width="16" height="16" />
                <Text text="保存全部设置" />
            </HBox>
        </Button>

        <!-- 转义演示 -->
        <Text text="注意：修改后请重启游戏 \{restartRequired\}" style="Theme.Hint" />

        <!-- 外部引用子视图 -->
        <Reference path="@subviews/AdvancedOptions.iml" />

        <!-- 自定义 Canvas -->
        <CustomCanvas on-draw="DrawPerformanceChart" width="200" height="100" />
    </VBox>
</Iris>
```

---

附录 A：完整 C# 集成示例（简略）

```csharp
// 1. 定义数据上下文
public class MyDataContext
{
    public UserSettings user { get; set; } = new();
    public OptimizerSettings optimizer { get; set; } = new();
    public string connectionStatus { get; set; } = "Online";

    public ICommand SaveAllCommand { get; }
    public ICommand ViewDetailCmd { get; }

    public MyDataContext()
    {
        SaveAllCommand = new RelayCommand(_ => SaveAll());
        ViewDetailCmd = new RelayCommand(param => ShowDetail(param?.ToString()));
    }
}

// 2. 初始化渲染器
var renderer = new IrisRenderer();
renderer.LoadFile("UI.iml");
renderer.SetDataContext(new MyDataContext());

renderer.RegisterHandler("OnOptimizerToggled", () =>
{
    if (Main.Settings.optimizer.enableOptimizer)
        AsyncPatchManager.UpdateOptimizerPatchesAsync();
    Main.Handler?.SaveSettings(Main.Settings);
});

renderer.RegisterHandler("OnPluginToggled", (string pluginId, bool enabled) =>
{
    // 处理单个插件开关
});

renderer.RegisterDrawHandler("DrawPerformanceChart", (rect, ctx) =>
{
    // 使用 Unity Graphics 绘制曲线图
});

// 3. OnGUI 中渲染
renderer.OnGUI();
```

---
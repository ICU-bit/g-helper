# UI 重构开发进度

## 使用方式

本文件是 `docs/UI-REDESIGN-PLAN.md` 的执行记录。每完成一个可验证步骤立即更新：

- 写明代码改动和涉及文件。
- 记录实际运行的验证命令和结果。
- 明确未验证项与原因。
- 保留唯一的“下一动作”，中断后从该动作继续。

状态取值：`未开始`、`进行中`、`代码完成/待人工验收`、`完成`、`阻塞`。

## 交付目标

- [ ] Home 只显示 Performance 和 GPU。
- [ ] Overview 按 Display、Lighting、Devices、Application 分组列出具体功能。
- [ ] 四个功能组在主窗体内导航，支持返回、目标定位、滚动位置和工作区约束。
- [ ] 设备类别目录、功能可用性和页面可见性相互独立。
- [ ] 不可用条目可通过鼠标/键盘展开行内原因，不产生硬件副作用。
- [ ] Fans、Extra、Handheld、Matrix/Slash、Updates 和外接键鼠遵守统一 owned window 契约。
- [ ] 外接键鼠按设备标识单实例，不同设备可同时打开。
- [ ] 应用更新与 BIOS/驱动更新保持独立。
- [ ] Home 页关闭按钮隐藏到托盘；二级页面关闭按钮返回上一级，Application 中 Exit 才退出。
- [ ] 新导航完成资源、本地化、主题、键盘和无障碍接入。
- [x] Debug build、Release single-file publish 和 `git diff --check` 通过。
- [ ] 对无法在当前机器覆盖的设备/DPI/多显示器人工矩阵明确记录残余风险。

## 基线

记录时间：2026-08-23

- 分支：`main`
- 基线提交：`0ca4c1463e323b37c4d267345300907293d7436b`
- 远端状态：基线时与 `origin/main` 同步，领先 `upstream/main` 2 个自定义提交。
- 基线工作区：`docs/UI-REDESIGN-PLAN.md` 已按确认后的产品规格修改，尚未提交。
- 基线验证：`GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 成功，0 警告、0 错误；`git diff --check` 通过。
- 项目没有自动化测试项目或 lint 命令。
- 实现遵守 `ponytail full`：优先删除和复用现有代码，不添加 speculative abstraction、依赖或第二套硬件状态机。

## 阶段状态

| 阶段 | 状态 | 证据 | 未覆盖 |
|---|---|---|---|
| 0. 行为基线 | 代码入口已盘点/待人工设备验收 | `docs/UI-REDESIGN-PLAN.md` 验收矩阵；基线 Debug build | 当前环境不能模拟全部 ASUS 设备、DPI 和热插拔 |
| 1. 子窗体定位解耦 | 代码完成/待人工验收 | `SettingsForm.PositionOwnedForm`; 基线 Debug build | 多显示器负坐标和实际宽窗人工验证 |
| 2. Owned window 生命周期 | 代码完成/待人工验收 | Debug build 0 警告/0 错误；静态检查无旧哨兵；聚焦审查问题已修复 | 同型号多外设、断开重连、校准中 HideAll 需真机验证 |
| 3. 可用性与可见性分离 | 代码完成/待人工验收 | 七个能力状态和统一投影；Debug build 0 警告/0 错误；最终静态检查通过 | 全设备能力、异步刷新、热插拔和恢复真机验证 |
| 4. 导航壳和 Home | 代码完成/待人工验收 | 六种页面状态、固定导航、滚动内容、紧凑 Home；最终 Debug build 0 警告/0 错误 | 真机 UI、DPI、主题、多显示器和滚动人工验收 |
| 5. Overview | 代码完成/待人工验收 | 四组具体功能目录、独立应用/BIOS 更新、六态不可用交互；Debug build 0 警告/0 错误；静态路由和资源检查通过 | 真机能力、WinForms UI、DPI、主题、读屏和热插拔人工验收 |
| 6. 四个功能组 | 代码完成/待人工验收 | Application、Display、Devices、Lighting 页面投影、充电状态/AMD OLED/Extra 归属修正、设备类别目录和维护动作；最终 Debug build 0 警告/0 错误 | 实际窗体布局、DPI、主题、目标定位和复杂硬件条件 |
| 7. 本地化/主题/无障碍 | 代码完成/待人工验收 | 中性/简中/繁中导航、状态和维护文案；可聚焦导航/Overview 条目、Enter/Space 和 AccessibleDescription | 动态主题、高 DPI、长文本、读屏和真实键盘焦点矩阵 |
| 8. 后续 façade 解耦 | 延后 | 计划明确为 UI 稳定后的后续重构 | 不属于首个完整 UI 交付的阻塞项 |

## 开发日志

### 2026-08-23：规格锁定

- 将已确认的信息架构、导航、不可用状态、owned window、窗口尺寸、滚动、本地化和验收规则写入 `docs/UI-REDESIGN-PLAN.md`。
- 验证：`git diff --check` 通过。
- 未运行构建：此步骤只修改 Markdown，沿用同轮已通过的基线 Debug build。

### 2026-08-23：阶段 2 Owned window 生命周期

代码改动：

- 在 `SettingsForm` 中增加最小 `ShowOrActivate(Form)`，Fans、Extra、Handheld、Matrix/Slash、Updates 的重复入口改为激活并置前，不再关闭。
- `FansToggle(index)` 和 `ServicesToggle()` 在窗口已显示时仍执行目标导航或服务动作，保留命令行行为。
- 外接键鼠窗口改为 `Dictionary<IPeripheral, RForm>`，使用 .NET `ReferenceEqualityComparer.Instance` 按连接期间的设备对象身份区分；同设备单实例，不同设备可并行。
- `OwnedForms` 统一承担主题刷新、焦点检查和 `HideAll` 批量关闭，删除外接键鼠单窗口字段和重复清理代码。
- 删除 Fans 中剩余的 `Text == ""` 生命周期哨兵，并处理窗口销毁期间 `Invoke/BeginInvoke` 的关闭竞态。
- 主窗体定位改用完整 `WorkingArea.Right/Bottom/Top`，修复非零或负坐标显示器的工作区原点计算。

验证：

- 第一次 Debug build 失败：`Program.cs` 缺少 `RForm` 命名空间；改用现有类型的完整限定名后修复。
- 聚焦代码审查发现两个问题并已修复：默认设备相等比较会合并同型号键盘；Fans 校准回调存在关闭竞态。
- 最终 `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 成功，0 警告、0 错误，用时 3.52 秒。
- `git diff --check` 通过。
- `rg` 确认阶段涉及文件中无 `mouseSettings`、`keyboardSettings`、`Text == ""`、`Text != ""` 残留。

残余人工验证：

- 两个同型号外设并行设置、设备断开后立即重连。
- 键盘测试布局连续重开。
- Fans 校准期间执行 `HideAll`。
- 已可见 Fans 窗口通过 `cpu/gpu/uv` 入口切换页。
- 带左右或上方任务栏的正负坐标多显示器。

### 2026-08-25：阶段 3 可见性写入盘点

完成内容：

- 核对工作区与交接快照一致；阶段 2 的未提交代码和文档均保留，未重置或覆盖。
- 盘点 `InitVisual`、`VisualiseAlly`、`InitRearLight`、`InitMatrix`、`VisualiseScreen`、`HideGPUModes` 和 `VisualizePeripherals` 的调用链及顶层 Panel 写入。
- 确认运行时顶层功能区写入集中在 `panelGamma`、`panelAlly`、`panelRearLight`、`panelMatrix`、`panelScreen`、`panelGPU` 和 `panelPeripherals`；其他常驻区没有运行时顶层 `Visible` 写入。
- 确认 `_VisualiseAura` 读取 `panelRearLight.Visible` 作为硬件能力判断，页面导航隐藏该区域后会错误跳过后灯色块刷新，阶段 3 必须改为读取独立能力状态。
- 确认 `VisualiseScreen` 在 `maxFrequency <= 0`、`InitVisual` 在 `hide_visual`、以及 Matrix 等路径中存在保留上次状态的既有语义；实现统一投影时必须保持这些分支行为。
- 确认阶段 3 只集中顶层功能区投影；HDR、Mini-LED、色域、GPU 模式按钮和外设按钮等内部条件显示继续由原处理器维护，避免提前引入 Overview 或第二套硬件状态机。

验证：

- `git status --short --branch` 与交接记录一致，分支仍为 `main`，未发现额外改动。
- `git diff --check` 通过；仅报告 `docs/UI-REDESIGN-PLAN.md` 的既有 LF/CRLF 转换提示。
- 仓库级 `rg` 已确认上述七个顶层功能区的全部运行时 `Visible` 写入位置。

未验证：

- 此步骤只完成静态盘点，尚未修改运行时行为，因此未重复执行 Debug build。
- 全部设备能力、异步刷新和热插拔行为留待状态与投影实现后验证。

### 2026-08-25：阶段 3 最小能力状态与统一投影

代码改动：

- 在 `SettingsForm` 中增加七个最小布尔能力状态，分别对应 Visual、Ally、Rear Light、Matrix/Slash、Screen、GPU 和 Peripherals；初始值与现有 Designer 默认可见性一致。
- 增加唯一的 `ApplyFeatureVisibility()`，集中投影七个顶层功能区，并保留 Ally 对键盘标题、键盘上边距和 AMD 快捷区的既有布局行为。
- `InitVisual`、`VisualiseAlly`、`InitRearLight`、`InitMatrix`、`VisualiseScreen`、`HideGPUModes` 和 `VisualizePeripherals` 改为更新能力状态和原有控件值，不再直接写顶层 Panel 可见性。
- `_VisualiseAura` 改为读取 `rearLightAvailable`，后续页面隐藏 Lighting 内容时仍可正常刷新后灯色块。
- `VisualizePeripherals` 改为使用一次 `AllPeripherals()` 快照同时决定能力和渲染按钮，消除存在性检查与列表读取之间的热插拔时间窗。
- `HideGPUModes` 增加现有 WinForms `InvokeRequired` 封送，避免电源稳定定时器路径跨线程写 GPU 控件；硬件读取、配置写入和调用顺序未改变。
- HDR、Mini-LED、色域、GPU 模式按钮和外设按钮等内部条件显示仍由原处理器维护；未修改 Designer、硬件命令、`AppConfig` key 或提交时机。

验证：

- 仓库级 `rg` 确认七个顶层功能区的运行时 `Visible` 赋值只存在于 `ApplyFeatureVisibility()`；Designer 默认赋值保持不变。
- `rg` 确认 `panelRearLight.Visible` 不再被运行时代码读取。
- `git diff --check` 通过；仅有 `docs/UI-REDESIGN-PLAN.md` 的既有 LF/CRLF 转换提示。

未验证：

- 尚未执行 Debug build 和阶段 3 聚焦代码审查。
- 无法在当前环境覆盖 OLED、Ally、Matrix/Slash、无 dGPU、外设热插拔及睡眠恢复的真机行为。

### 2026-08-25：阶段 3 主 Agent 聚焦代码审查

审查结果：

- 主 Agent 复核 `SettingsForm` 构造顺序、Program 启动顺序、七个入口调用线程、Designer 默认值和所有能力字段写入；未发现默认可见性、`maxFrequency <= 0` 等保留上次状态分支或硬件调用顺序发生回归。
- 确认 `ApplyFeatureVisibility()` 只投影顶层功能区及 Ally 的既有布局，不覆盖 HDR、Mini-LED、色域、GPU 模式按钮等内部条件状态。
- 确认构造期间 `InitVisual()` 调用投影时窗体句柄尚未创建，但运行在创建线程，`InvokeRequired` 为 false；后续 `colors`、屏幕、Ally、外设和 GPU 异步路径均已有入口封送或调用方封送。
- 确认外设断开后空快照只更新能力并隐藏父区，保留子按钮缓存，行为与旧实现一致；重新连接时会按快照刷新按钮。
- 发现并删除 `VisualiseAlly()` 中投影后残留的 `tableAMD.Visible = true` 重复写入，统一由 `ApplyFeatureVisibility()` 管理。
- 用户明确要求后续全部开发、审查和验证由主 Agent 完成；不再使用子智能体。一次被取消的审查调用无返回结果、无文件改动，不计作验证证据。

验证：

- 聚焦 `rg` 再次确认七个顶层功能区和 `tableAMD` 的运行时可见性写入只存在于 `ApplyFeatureVisibility()`。
- `git diff --check` 通过；仅有既有 LF/CRLF 转换提示。
- `git status --short --branch` 与计划内工作区一致，没有新增非计划文件。

未验证：

- Debug build 尚未执行。
- 真机设备、DPI、多显示器和热插拔场景仍需人工验收。

### 2026-08-25：阶段 3 最终验证

验证结果：

- `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 成功，0 警告、0 错误，用时 4.43 秒；CI 环境变量避免触发本地 `KillRunningGHelper`。
- 最终 `git diff --check` 通过；仅有 `docs/UI-REDESIGN-PLAN.md` 的既有 LF/CRLF 转换提示。
- 最终 `rg` 确认七个顶层功能区的运行时 `Visible` 赋值全部集中在 `ApplyFeatureVisibility()`，Designer 默认赋值未改。
- 最终 `rg` 确认运行时代码不再读取 `panelRearLight.Visible`，`SettingsForm.VisualizePeripherals()` 不再执行分离的 `IsAnyPeripheralConnect()` 检查。
- 工作区只包含阶段 2、阶段 3 和已确认文档的计划内改动；未创建提交、未推送、未创建 PR。

阶段结论：

- 阶段 3 标记为“代码完成/待人工验收”。硬件可用性已从顶层 Panel 可见性中分离，现有长页面仍由统一投影呈现。
- 残余风险为 OLED、Ally、Matrix/Slash、无 dGPU、外设热插拔、睡眠恢复、DPI 和多显示器真机行为；这些场景无法由当前机器完整模拟。

### 2026-08-25：阶段 4 导航壳与布局盘点

完成内容：

- 主 Agent 确认 `SettingsForm` 当前把全部功能区直接作为 `DockStyle.Top` 子控件挂在 Form 上，Form 自身没有 `AutoScroll`；固定导航栏需要与可滚动内容区分离。
- 确认最小可靠布局为固定顶部 `panelNavigation` 加 `DockStyle.Fill`、`AutoScroll = true` 的 `panelContent`，现有十三个功能区原样移入内容容器，不复制控件或事件处理器。
- 确认 `RButton`、`RForm.InitTheme()` 和 `ControlHelper.Adjust()` 可直接覆盖导航按钮与容器主题，无需新 UserControl、导航库或自绘标题栏。
- 确认可复用 `icons8_settings_32` 作为设置按钮；现有资源没有明确返回箭头，首个导航骨架使用标准文本箭头符号作为按钮内容，阶段 7 再统一图标与高 DPI/主题验收。
- 确认导航所需 Home、Overview、Back、Display、Devices、Application 文案当前缺失；按计划应加入中性英文、简体中文和繁体中文资源，其他语言通过中性资源回退。
- 确认阶段 4 只建设六种页面状态、导航返回规则、紧凑 Home、内容滚动与重新打开回 Home；Overview 具体功能条目仍留在阶段 5。

未验证：

- 此步骤只完成布局和资产盘点，尚未修改 Designer 或运行时页面行为，因此未重复构建。
- 内容高度、工作区限制、负坐标显示器和高 DPI 行为需要实现后验证。

### 2026-08-25：阶段 4 导航容器与资源骨架

代码改动：

- `Settings.Designer.cs` 增加固定顶部 `panelNavigation`，包含可聚焦的 `buttonBack`、页面标题 `labelPageTitle` 和齿轮 `buttonSettings`。
- 增加 `DockStyle.Fill`、`AutoScroll = true` 的 `panelContent`，将原十三个功能区按原添加顺序迁入该容器；Form 顶层只保留导航栏和内容区。
- 关闭 `SettingsForm` 的旧 `AutoSize`/`GrowAndShrink`，为后续按页面计算受约束窗口高度和内容滚动建立稳定布局边界。
- 设置按钮复用 `icons8_settings_32`；返回按钮暂用标准 `Segoe UI Symbol` 箭头，并已设置 `AccessibleRole`、本地化 `AccessibleName` 和固定尺寸。
- 在 `Strings.resx`、`Strings.zh-CN.resx`、`Strings.zh-TW.resx` 和 `Strings.Designer.cs` 增加 Home、Overview、Display、Devices、Application、Back；其他语言使用中性英文回退。

验证：

- 初次 `git diff --check` 发现新 Designer 分隔注释存在 10 处尾随空格；已清理后复查通过，仅剩既有 LF/CRLF 提示。
- 聚焦 `rg` 确认 Form 顶层只添加 `panelContent` 与 `panelNavigation`，十三个旧功能区只添加到 `panelContent`，无重复父级。
- 聚焦 `rg` 确认三套资源文件均包含六个导航键，生成访问器包含对应属性。
- `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 成功，0 警告、0 错误，用时 4.00 秒。

未验证：

- 导航按钮尚未接入事件，页面状态与投影尚未实现。
- 窗口紧凑高度、滚动恢复、工作区 clamp、高 DPI 和主题动态切换仍待后续步骤。

### 2026-08-25：阶段 4 页面状态、投影与尺寸

代码改动：

- 在 `SettingsForm` 增加 `Home`、`Overview`、`Display`、`Lighting`、`Devices`、`Application` 六种独立 `SettingsPage` 状态；页面不再通过 Panel `Visible` 反推。
- `ApplyFeatureVisibility()` 改为统一组合“当前页面 + 阶段 3 能力状态”：Home 只显示 Performance/GPU；四组页面投影已确认归属的现有顶层区域；Overview 暂无条目，留待阶段 5。
- 增加 `NavigateTo()` 和 `ResetToHome()`，接入设置/返回按钮、页面标题、初始焦点和每页滚动位置；Overview 返回 Home，四组返回 Overview。
- `SettingsForm_VisibleChanged` 在主窗从隐藏状态重新显示时执行 `ResetToHome()`，Home 回顶部并聚焦 Performance；已显示窗体仅重新激活时不重置。
- 增加 `ResizeForCurrentPage()`，按当前可见内容计算紧凑高度，以当前显示器完整 `WorkingArea` 限制窗口高度和位置；超过工作区的内容由 `panelContent` 垂直滚动。
- 能力刷新仍只更新能力字段和值；统一投影重新布局当前页并恢复滚动位置，不改变 `currentPage`。

主 Agent 审查与修复：

- 将滚动位置捕获提前到可见性变化前，避免热插拔导致布局收缩时丢失当前位置。
- 返回按钮改为复制现有 `icons8_next_32` 后水平翻转，避免临时文本箭头和修改共享资源位图。
- 导航栏从混合 Left/Fill/Right 停靠改为固定三列 `TableLayoutPanel`，避免 z-order 和高 DPI 下标题挤压。
- 删除 `NavigateTo()` 中 `ApplyFeatureVisibility()` 后重复的 `PerformLayout()` 和 `ResizeForCurrentPage()`，每次页面切换只执行一轮布局。

验证：

- 页面模型首次构建成功，0 警告、0 错误，用时 4.40 秒。
- 审查修复后 `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 再次成功，0 警告、0 错误，用时 2.86 秒。
- `git diff --check` 通过，仅有既有 LF/CRLF 提示。
- 聚焦 `rg` 确认十三个顶层功能区的运行时可见性赋值只存在于页面统一投影，Designer 仅保留四个初始隐藏默认值。
- 聚焦 `rg` 确认导航栏为三列布局，设置与返回按钮固定 48px 轨道，页面切换没有重复布局调用。

未验证：

- 阶段 5 的 Overview 条目尚未实现，因此四个功能组暂时没有用户可点击入口。
- 真机运行、键盘 Tab 顺序、动态主题、高 DPI、窗口工作区和滚动恢复仍需人工或 UI 级验证。

### 2026-08-25：阶段 4 最终审查与验证

主 Agent 审查：

- 发现 `ResizeForCurrentPage()` 暂时抑制旧 `SettingsForm_Resize`，会让页面缩短后窗口停留在原顶部而不是保持右下角定位；已移除抑制，页面高度变化继续沿用现有完整 `WorkingArea.Right/Bottom` 定位。
- 复核 `ResetToHome()` 仅在构造完成和窗体从隐藏变为显示时执行；已显示窗体的激活/置前不会重置页面。
- 复核硬件能力刷新只调用统一投影，不写 `currentPage`；隐藏页面控件仍保留实例和值，重新进入页面显示最新状态。
- 复核 Home 只投影 Performance/GPU，其他功能区在 Home 均隐藏；Overview 暂时无条目，未违反阶段 5 的具体功能列表边界。
- 复核中性、简中、繁中导航资源和可聚焦按钮；阶段 7 仍需真机核对动态主题、Tab 顺序、读屏和长文本。

最终验证：

- `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 成功，0 警告、0 错误，用时 2.87 秒。
- 最终 `git diff --check` 通过，仅有 `docs/UI-REDESIGN-PLAN.md` 的既有 LF/CRLF 转换提示。
- 最终 `rg` 确认不存在 `applyingPageLayout` 残留；六种页面状态、每页滚动位置、`ResetToHome()`、`AutoScrollPosition` 和完整工作区边界均有单一实现入口。
- 最终 `rg` 确认十三个顶层功能区的运行时可见性赋值全部集中在 `ApplyFeatureVisibility()`。
- 工作区只新增计划内的 Settings、三套资源和文档改动；未提交、未推送、未创建 PR。

阶段结论：

- 阶段 4 标记为“代码完成/待人工验收”。导航壳、独立页面状态、紧凑 Home、重新显示回 Home、滚动状态和工作区高度约束已完成。
- 残余风险为实际 Windows 窗体布局、100%-200% DPI、浅色/深色/Flat/Windows 主题、负坐标多显示器、长文本和鼠标/键盘焦点行为。

### 2026-08-25：阶段 5 Overview 目录与动作盘点

完成内容：

- 主 Agent 将计划中的具体功能映射到现有入口：Screen/Visual/OLED 进入 Display；内置键盘、Rear Light、Matrix/Slash 进入 Lighting；Battery、Fans、Peripherals、Ally/Handheld 进入 Devices；Startup、应用更新、BIOS/驱动更新、Extra、维护动作和 Exit 进入 Application。
- 确认 Fans 复用 `FansToggle()`，Extra 复用 `GetExtraForm()`/`ShowOrActivate()`，Matrix/Slash 复用 Lighting 快捷区及 `ButtonMatrix_Click`，Handheld 复用 Devices 快捷区及 `ButtonHandheld_Click`，BIOS/驱动更新复用 `ButtonUpdates_Click`，应用更新复用 `updateControl.CheckForUpdates()`，退出复用 `ButtonQuit_Click`。
- 确认应用更新和 BIOS/驱动更新必须是两个独立条目和动作，不合并。
- 确认 Matrix/Slash、Ally 和已连接外设条目先进入对应快捷控制区域；高级窗口仍由组页内现有入口打开，不在 Overview 重复第二个高级条目。
- 设备类别目录以 `AppConfig.IsAlly()` 为首个明确分类边界：普通笔记本不显示 Handheld 专属条目；Ally 显示控制器快捷区。Matrix/Slash 与 Rear Light 只在设备类别适用时列入目录；外设类别保留目录并由连接状态决定可用性。
- Overview 将使用现有 `RButton`、`Label`、`TableLayoutPanel` 运行时构造轻量分组列表，不新增 UserControl、依赖或第二套硬件状态机。每条目显式记录动作类型和可用状态，后续不可用状态仍保持可聚焦以展开行内原因。

验证：

- 仓库级 `rg` 已覆盖 Settings 中全部现有点击处理器、owned-window 路由和能力来源。
- 资源检查确认大多数功能名称已有本地化键；仍缺应用更新、Rear Light、状态与原因等少量专用文案，将按中性/简中/繁中同步补齐。

未验证：

- 此步骤只完成目录和动作盘点，尚未创建 Overview 控件或改变运行时行为，因此未重复构建。
- 设备类别目录目前只使用代码中已有明确类别 helper，不引入推测性的型号分类。

### 2026-08-25：阶段 5 Overview 分组目录与状态交互

代码改动：

- 在 `SettingsForm` 内增加轻量 `OverviewItem` 条目记录和 `OverviewAction` 路由，复用现有 `NavigateTo()`、Fans、Extra、Updates、应用更新和退出处理器。
- 增加运行时构造的四组分组列表：Display、Lighting、Devices、Application；应用更新与 BIOS/驱动更新保持两个独立条目。
- Overview 内嵌条目按目标 Panel 定位，独立窗口条目直接执行现有 owned-window 路由，不复制硬件控制逻辑。
- 增加 `OverviewStatus` 六态投影：Available、Unsupported、Disconnected、Conditional、Loading、Error；不可用条目不设置 `Enabled = false`，保留鼠标、Enter 和 Space 操作。
- 不可用条目点击或键盘操作只展开一条本地化行内原因，同时更新 `AccessibleDescription`，不执行硬件调用、不弹对话框、不改变页面。
- 在中性英文、简体中文、繁体中文资源中增加五个 Overview 状态原因及生成访问器；其他语言继续回退到中性资源。
- `ApplyFeatureVisibility()` 在能力刷新后同步刷新 Overview 状态，保持页面状态独立。

验证：

- 第一次 Debug build 因插入方法边界错误失败，已修复；第二次只剩静态事件绑定错误，已改为实例方法。
- 最终 `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 成功，0 警告、0 错误，用时约 2.54 秒。
- 最终 `git diff --check` 通过，仅有 `docs/UI-REDESIGN-PLAN.md` 的既有 LF/CRLF 转换提示。
- 聚焦 `rg` 确认四组条目、独立应用更新/BIOS 更新动作、不可用状态资源和键盘入口均存在。
- 聚焦 `rg` 确认十三个既有顶层功能区的运行时可见性仍只由 `ApplyFeatureVisibility()` 写入；Overview 仅增加独立内容容器投影。

未验证：

- 当前环境无法执行真实 WinForms UI、读屏、鼠标/键盘焦点、DPI、主题、多显示器和设备热插拔人工矩阵。
- 当前状态函数使用阶段 3 的最小能力字段；Loading/Error 状态已进入模型和交互映射，尚未接入新的异步探测源。

### 2026-08-25：阶段 6 Application/Devices 归属迁移

代码改动：

- 将充电状态 `labelCharge` 从 Startup 区迁入 Battery 区，保留电池悬停、健康度、充电报告和异步传感器更新处理器不变。
- 将 AMD OLED 快捷动作从 Application 版本区迁入 Display 的 Gamma 标题区，保留 `VisualiseAmdOled()`、焦点回调和现有 Adrenalin 动作不变。
- 底部独立窗口按钮明确显示 `BIOS and Driver Updates`，与 Overview 中的应用更新条目保持语义独立。
- 只调整 Designer 父容器、尺寸和显示文案，没有修改硬件命令、配置键或设置提交时机。

验证：

- `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 成功，0 警告、0 错误，用时约 3.93 秒。
- `git diff --check` 通过，仅有计划文档既有 LF/CRLF 转换提示。
- 静态检查确认 `labelCharge` 只加入 `panelBattery`，`buttonAmdOled` 只加入 `panelGammaTitle`，旧父级已删除；页面可见性仍集中在 `ApplyFeatureVisibility()`。

未验证：

- 当前环境无法运行真实 WinForms 窗体，Battery 区新增高度、Gamma 标题内 AMD 按钮、高 DPI 和长文本仍需人工验收。


### 2026-08-25：最终交付门禁与发布产物

最终验证：

- `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 成功，0 警告、0 错误。
- `git diff --check` 通过；仅有 `docs/UI-REDESIGN-PLAN.md` 的既有 LF/CRLF 转换提示。
- 最终静态审计确认：Overview 四组具体条目、设备类别过滤、应用更新与 BIOS/驱动更新独立、Extra 位于 Application 动作区、充电状态位于 Battery、AMD OLED 位于 Display/Gamma、顶层功能可见性集中于 `ApplyFeatureVisibility()`，无旧窗口哨兵或 `panelRearLight.Visible` 能力读取。
- 已按用户授权关闭运行中的 G-Helper，默认 Release publish 成功覆盖到 `app/bin/x64/Release/net10.0-windows/win-x64/publish/GHelper.exe`；未保留隔离发布目录。
- 默认发布产物与最终源码同步，大小约 5.96 MB。
- 已在真实 Windows ASUS 环境读取默认发布版：Home、Overview 和四组 Overview 条目可见；目标 1366x768 尚未覆盖，当前显示器为 2560x1440。

交付结论：

- 阶段 2-7 的代码路径已完成并通过构建、静态检查和默认发布；阶段 8 façade 解耦按计划延后。
- 残余风险为当前机器无法覆盖的真实 ASUS 设备能力、1366x768/DPI、多显示器、动态主题、读屏、键盘焦点、热插拔、睡眠恢复和 owned-window 人工矩阵。
- 本轮工作区只包含计划内代码和文档改动；按用户明确要求提交并推送到 `origin/main`，发布 GitHub `v0.279`，不推送 `upstream`。

### 2026-08-25：二级页面系统关闭按钮返回上一级

代码改动：

- `SettingsForm_FormClosing` 继续拦截用户点击系统关闭按钮的行为。
- 当前页面为 Home 时复用 `HideAll()` 隐藏主窗体并关闭 owned windows；当前页面为 Overview、Display、Lighting、Devices 或 Application 时复用 `ButtonBack_Click` 返回上一级。
- `ButtonQuit_Click` 的程序化退出路径未修改，Application 页的 Exit 仍执行完整退出流程。

验证：

- `GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug` 成功，0 警告、0 错误。
- `git diff --check` 通过；关闭路径静态检查确认 UserClosing 分支按 Home/二级页面分流，程序化 `ButtonQuit_Click` 仍调用完整退出流程。
- 默认发布版真实窗口已成功显示 Home 和 Overview；Overview 进入 Display 的动作已发送但 UI 自动化后续读取遇到一次瞬时权限代理错误，未重放非幂等动作。

未覆盖：

- 真实 Windows ASUS 环境下的系统关闭按钮返回、1366x768/100% DPI、owned window 同时显示和多页面完整人工验收仍需继续验证。

## 下一动作

交付给用户进行真实设备/UI 人工验收；若发现具体设备或布局回归，再按单一问题执行“最小改动、立即构建和静态验证”的修复循环。

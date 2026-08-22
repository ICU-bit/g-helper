# UI 重构开发计划

## 目标

将主界面从当前的长页面调整为更聚焦的入口：一级页面默认只显示性能模式和显卡模式；屏幕、灯效、电池、外设、矩阵、风扇、更新和其他高级功能进入设置入口或现有独立窗口。

## 最小改动原则

- 优先复用现有控件、事件处理器、硬件控制器和独立窗体；只有现有逻辑无法安全复用时才增加抽象。
- 不复制 GPU、性能模式、电池、屏幕、灯效或外设状态机，不改变硬件写入参数和调用顺序。
- 先解决已确认的生命周期和尺寸耦合，再增加导航；不同时进行无关清理、命名重构或格式化。
- 第一阶段不大规模重排 `Settings.cs` 或 `Settings.Designer.cs`；Designer 只在新增或移动实际控件时修改。
- 每一步保持单一目的、小范围差异，完成构建和人工验证后再进入下一步。

## 开发流程

### 0. 建立行为基线

- 记录普通笔记本、无 dGPU、ROG Ally、OLED、Matrix/Slash、外接键鼠等设备的当前面板可见性。
- 验证托盘、热键、电源切换、睡眠恢复、XG Mobile、热插拔和 `cpu/gpu/uv/services/colors/autoupdate` 命令行入口。
- 后续每个提交都以“硬件行为不变、旧入口可用”为验收红线。

### 1. 解除二级窗体定位耦合（当前阶段）

- 在 `SettingsForm` 中增加一个最小的子窗体定位方法，以工作区和主窗体位置计算子窗体位置。
- 将 `Fans`、`Extra`、`Handheld`、`Matrix`、`Slash`、`Updates`、外接键盘和鼠标窗体改为调用该方法。
- 删除子窗体对 `Program.settingsForm.Height` 的读取、强制高度同步和基于主窗体高度的 `MaximumSize` 设置。
- 不改变子窗体自己的内容、滚动、单实例或硬件逻辑。
- 验收：主窗体高度变化后所有子窗体仍能显示、定位和关闭；主窗体压缩前后硬件功能无差异。

### 2. 统一子窗体生命周期

- 在 `Settings.cs` 中集中复用现有 `AddOwnedForm` 和窗口字段，补充 Fans、Extra、Updates、Matrix、Slash、Handheld 的打开入口。
- 使用 `IsDisposed`/`Disposing` 判断窗口有效性，不再使用 `Text == ""` 作为生命周期哨兵。
- 统一 `FormClosed` 清理字段、重新打开时激活已有窗口、`HideAll` 关闭 owned forms、`HasAnyFocus` 检查所有 owned forms。
- 主题变化复用现有 `RForm.InitTheme`，补齐当前遗漏的 Slash、键盘和鼠标设置窗口。
- 保留托盘、热键和命令行入口，只改变它们调用的内部路由。

### 3. 分离硬件支持状态和页面可见性

- 增加轻量的页面/功能组状态，不引入新的硬件控制层。
- 将最终显示规则统一为“设备支持且当前页面包含该功能组”。
- 修改 `InitVisual`、`VisualiseAlly`、`InitRearLight`、`InitMatrix`、`VisualiseScreen`、`HideGPUModes`、`VisualizePeripherals`，使硬件刷新只更新支持状态和控件状态，不绕过导航强行显示面板。
- 硬件后台刷新继续运行，页面隐藏只影响 UI 投影。

### 4. 增加紧凑首页

- 在现有 `SettingsForm` 上增加最小导航壳和“首页/更多设置”入口，不立即创建新的主窗体或页面框架。
- 首页默认只显示 `panelPerformance` 和 `panelGPU`，保留电池状态、版本、更新、设置和退出入口。
- `SettingsToggle` 每次从托盘显示时进入首页；`HideAll/ShowAll` 继续只负责应用窗口的隐藏和恢复，不与页面切换混用。
- 页面切换后统一执行布局刷新和主窗体定位，避免窗口反复跳动。

### 5. 建设设置总览

- 复用现有独立窗体作为 Fans、Extra、Handheld、Matrix/Slash、Updates、外接键鼠的目标入口。
- 为显示、灯效、设备/电池、应用等剩余嵌入面板增加功能组入口。
- 不支持的硬件入口不显示；Matrix 和 Slash 根据现有 `matrixControl` 状态显示正确名称。
- Fans 继续支持 CPU、GPU、Advanced 导航和原有命令行参数。

### 6. 按功能组组织剩余嵌入面板

- `Display`：`panelScreen`、`panelGamma`。
- `Lighting`：`panelKeyboard`、`panelRearLight`、`panelMatrix`。
- `Devices`：`panelBattery`、`panelPeripherals`、`panelAlly`。
- `Application`：`panelStartup`、`panelVersion`、`panelFooter`。
- 每次只迁移一个功能组；稳定后再考虑抽成独立窗体，继续通过 `SettingsForm` façade 维持回调和生命周期。

### 7. 接入主题、本地化和无障碍

- 新增导航文案时使用 `app/Properties/Strings*.resx`，同步中文资源并保留英文回退。
- 为导航按钮设置 `AccessibleName`、Tab 顺序和合理的初始焦点。
- 验证浅色、深色、Flat 主题，Windows 主题切换，以及高 DPI 和长文本布局。

### 8. 后续硬件 façade 解耦

- UI 稳定后，再按 Battery、Display、GPU、Peripherals 逐组消除 `Program.settingsForm.VisualiseXxx()` 的直接依赖。
- 每组改动单独提交，不与 Designer 大改或其他功能迁移混合。
- Matrix/Aura 生命周期依赖最深，最后处理。

## 每阶段验证

每个提交执行：

```bash
GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug
git diff --check
```

人工验证至少覆盖 1366x768、1920x1080、4K，100%/125%/150%/200% DPI，浅色/深色/Flat 主题，中英文和长文本语言；同时覆盖 GPU 模式、电源切换、睡眠恢复、托盘反复开关、热插拔外设、Matrix/Slash、更新窗口和全部命令行入口。

## 风险与回滚

- 保留现有控件和独立窗体作为兼容实现，功能组出现设备特定回归时按提交粒度回退。
- 不删除旧事件处理器，直到新导航稳定一个发布周期。
## 建议提交顺序

1. `refactor(ui): centralize owned form placement`
2. `refactor(ui): centralize settings window lifecycle`
3. `refactor(ui): separate section availability from visibility`
4. `feat(ui): add compact settings navigation shell`
5. `feat(ui): add settings hub destinations`
6. `feat(ui): group display and lighting views`
7. `feat(ui): group device and application views`
8. `refactor(ui): unify theme focus and close handling`
9. `refactor(ui): decouple hardware status views`
10. `docs(ui): update redesign plan and validation matrix`

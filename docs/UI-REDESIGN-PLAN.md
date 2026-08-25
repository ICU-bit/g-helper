# UI 重构开发计划

## 当前状态

截至 2026-08-23：

- 阶段 1 的子窗体定位解耦已完成代码实现，尚缺完整设备和显示环境的人工验收。
- 阶段 2 的 ownership、关闭清理、`HideAll` 和焦点检查已大部分完成，仍需统一“打开或激活”行为、外接设备多窗口和运行时主题刷新。
- 阶段 3 及之后尚未开始。
- 当前 Debug 构建通过，0 个警告、0 个错误；`git diff --check` 通过。

计划中所有产品和交互决策均已确认。实现时不再按旧 Designer 中的物理嵌套机械迁移控件，应以本文的功能归属和行为契约为准。

## 目标

将当前长页面重构为紧凑、可导航的设置界面：

- 首页只显示性能模式和 GPU 模式。
- 设置总览按 Display、Lighting、Devices、Application 分组，并直接列出具体功能。
- Display、Lighting、Devices、Application 在主窗体内切换。
- 已有成熟编辑器继续使用独立 owned window，不复制其硬件逻辑或编辑能力。
- 硬件支持状态、当前页面和控件值互相独立，异步刷新不得改变用户所在页面。

## 硬约束与非目标

- 继续使用 `SettingsForm` 作为现有硬件状态和 UI 更新的 façade，不在本轮创建第二套硬件控制层。
- 不改变硬件命令、参数、调用顺序、`AppConfig` key 或设置提交时机。
- 不复制 GPU、性能、电池、屏幕、Aura、Matrix、Ally 或外设状态机。
- 保留托盘、热键、电源切换、睡眠恢复、XG Mobile、热插拔以及 `cpu/gpu/uv/services/colors/autoupdate` 命令行入口。
- 不因页面隐藏而停止硬件后台刷新；页面只控制 UI 投影。
- Designer 只在实际增加导航、移动控件或调整布局时修改，不混入无关清理和格式化。
- Matrix/Slash、Handheld 和外接键鼠的完整编辑能力只存在于独立窗口；主窗体最多保留已确认的快捷控制和状态入口。

## 页面与导航契约

### 页面集合

主窗体包含六种页面状态：

1. `Home`
2. `Overview`
3. `Display`
4. `Lighting`
5. `Devices`
6. `Application`

当前页面必须由独立页面状态表示，不能通过某个 panel 的 `Visible` 值反推。

### 标题和导航区域

- 保留 Windows 原生标题栏、系统关闭按钮和现有窗口拖动行为，不改为自绘无边框窗口。
- 在客户端内容顶部增加紧凑导航栏。
- Home 导航栏显示页面标题和设置齿轮；齿轮进入 Overview。
- Overview 显示返回按钮和页面标题；返回进入 Home。
- 四个功能组页面显示返回按钮和组标题；返回进入 Overview。
- 返回和设置使用现有图标资源及 tooltip，并提供本地化的 `AccessibleName`。
- 不增加最小化按钮；保持当前不进入任务栏的工具窗口行为。

### 重新打开主窗体

- 主窗体从隐藏状态通过托盘、热键或其他应用入口重新显示时，总是执行 `ResetToHome()`。
- Home 重新显示时滚动位置回到顶部，初始焦点落在性能模式区域。
- `HideAll` 和 `ShowAll` 只处理窗口生命周期，不把页面状态隐式编码在窗口可见性中。

### 设置总览

- Overview 使用分组列表，不使用图标网格或仅含四个分类的中间页。
- 列表按 Display、Lighting、Devices、Application 分区，但每行是具体功能，例如 Screen、Battery、Fans、Extra settings。
- 每个条目必须声明一种动作：
  - `Navigate`：进入内嵌功能组并定位到对应功能区域。
  - `OpenOwned`：创建或激活独立窗口。
  - `DirectAction`：执行已有明确命令，例如退出。
  - `ExplainUnavailable`：展开不可用原因，不执行硬件操作。
- 内嵌功能条目直接进入对应组页并定位到该功能，不要求用户再次查找。
- 纯独立窗口条目直接打开或激活窗口，不先进入空的功能组页。
- 有“快捷控制 + 高级窗口”的功能在 Overview 中只显示一个条目：先进入对应快捷控制区域，再由区域内的“高级设置”入口打开 owned window。

## 功能归属

### Home

- `panelPerformance`：保留所有现有性能模式、温度和风扇状态，以及 Fans + Power 快捷入口。
- `panelGPU`：保留 Eco、Standard、Ultimate、Optimized、GPU 应用、XG Mobile，以及现有 Ally GPU 快捷项。
- Home 内容区不显示电池、版本、更新、开机启动或退出。

### Display

- `panelScreen`：刷新率、自动刷新率、Mini-LED、分辨率和 HDR 控制。
- `panelGamma`：OLED/Flicker-free dimming、Visual mode、色温、色域和颜色配置。
- AMD OLED/Adrenalin 显示专项动作。
- Display 条目进入本页后定位到 Screen 或 Visual/OLED 对应区域。

### Lighting

- `panelKeyboard`：仅代表笔记本内置键盘和机身 Aura 灯效，不代表外接键盘设置。
- `panelRearLight`：后部或边缘灯效快捷控制。
- `panelMatrix`：保留 Matrix/Slash 亮度和运行模式等快捷控制。
- Matrix/Slash 区域提供“高级设置”入口，打开现有 `Matrix` 或 `Slash` owned window。
- Matrix 图片、GIF、文字、时钟、音频可视化和 Slash 电源场景等完整编辑能力不复制到主窗体。

### Devices

- `panelBattery`：充电上限、满充选项、电池状态和充电报告入口。
- 将当前位于 `panelStartup` 的充电状态按语义迁入 Battery 区域。
- `panelAlly`：保留控制器模式、背光和控制器快捷操作。
- Ally 区域提供“高级设置”入口，打开现有 `Handheld` owned window；死区、震动和按键绑定不复制到主窗体。
- `panelPeripherals`：保留已探测外设的状态和启动器，不嵌入外接键盘或鼠标的完整设置。
- 每个已连接外设打开自己的设置窗口；同一设备只允许一个实例，不同设备允许同时打开。
- Fans 条目直接打开或激活现有 `Fans` 窗口；Home 中的 Fans + Power 入口调用同一路由。

### Application

- 开机启动设置。
- G-Helper 版本和“应用更新”入口，继续使用现有应用版本检查流程。
- “BIOS 与驱动更新”作为独立条目，打开或激活现有 `Updates` 窗口。
- 应用更新与 BIOS/驱动更新不得合并成同一个动作。
- `Extra settings` 作为 Application 中的独立条目，打开或激活现有 `Extra` 窗口。
- Energy Saver、Armoury Crate 维护等现有应用或系统动作。
- Exit 作为明确的退出命令；不依赖关闭按钮退出应用。

## 设备目录与不可用状态

### 目录范围

- Overview 不显示整个产品支持过的所有功能，而是先按设备类别选择合理目录。
- 普通笔记本不列出 Handheld 专属功能；掌机、普通笔记本及其他已识别类别只显示对该类别有意义的功能。
- 类别目录中的功能即使当前型号不支持、设备未连接或条件不满足，也保留条目并呈现不可用状态。
- 外设条目可区分“此设备类别支持但未连接”和“当前型号不支持”。

### 状态模型

功能至少区分以下逻辑状态，具体实现保持轻量：

- `Available`：可进入页面、打开窗口或执行动作。
- `Unsupported`：当前型号不支持。
- `Disconnected`：功能类别适用，但设备当前未连接。
- `Conditional`：硬件存在，但当前 GPU、HDR、电源或其他条件不允许使用。
- `Loading`：能力或状态仍在探测。
- `Error`：探测失败或状态暂不可用。

设备类别目录、功能可用性、当前页面和控件值必须是不同状态。硬件刷新只能更新能力和值，不能直接改变当前页面。

### 不可用条目的交互

- 不可用条目视觉上使用禁用样式，但不能简单设置原生 `Enabled = false`，因为仍需接收鼠标和键盘操作。
- 点击或按 Enter/Space 时，在条目内部展开本地化的原因和可执行的简短处理建议。
- 同一页面同时只展开一个不可用说明；再次点击可收起。
- 展开说明不弹对话框、不创建窗口、不执行设备调用，也不改变当前页面。
- 条目必须通过 `AccessibleDescription` 或等效方式向屏幕阅读器暴露同一原因和状态。

## Owned Window 契约

受统一生命周期管理的窗口包括：

- Fans
- Extra
- Handheld
- Matrix 或 Slash
- Updates
- 每个外接键盘设置窗口
- 每个外接鼠标设置窗口

统一打开规则：

1. 实例不存在或已释放：创建、`AddOwnedForm`、注册关闭清理、定位、显示并激活。
2. 实例存在但隐藏：显示、重新定位并激活。
3. 实例已显示：`Activate()` 和 `BringToFront()`，不得关闭或创建重复实例。
4. Matrix 与 Slash 根据当前硬件类型选择正确窗口和本地化名称。
5. 外接键鼠按稳定设备标识管理实例；同一设备单实例，不同设备可并排打开。
6. 设备断开或窗口关闭时只清理对应实例，不影响其他设备窗口。

生命周期验收：

- 不再使用 `Text == ""` 作为窗体有效性哨兵。
- `FormClosed` 清理对应字段或设备实例表。
- `HideAll` 关闭全部 owned windows，然后隐藏主窗体。
- `HasAnyFocus` 覆盖全部 owned windows，包括所有外接设备实例。
- Windows 主题切换覆盖 Matrix、Slash、键盘、鼠标及其他 owned windows。
- 命令行和旧快捷入口统一调用同一打开路由。

## 关闭、退出和窗口恢复

- 二级页面点击系统关闭按钮返回上一级；Home 页系统关闭按钮继续隐藏到托盘。
- 不新增任务栏最小化按钮。
- Application 页中的 Exit 才调用现有完整退出流程。
- 隐藏主窗体时关闭 owned windows；再次打开时仅显示 Home，不自动恢复之前的独立窗口。
- 退出流程不新增确认对话框，保持现有语义。

## 页面状态、刷新和滚动

- 页面切换只改变 UI 投影并执行布局，不销毁内嵌控件，不重新初始化硬件，也不重复写入设置。
- 异步硬件刷新更新隐藏页面中的缓存和值；用户再次进入页面时显示最新状态。
- 热插拔、睡眠恢复、GPU 模式切换和 HDR/OLED 状态变化不得切换页面或强制显示其他 panel。
- 每个功能组保存自己的垂直滚动位置。
- 从 Overview 点击具体功能时，该次导航优先定位到目标区域；离开后记录新的滚动位置。
- 主窗体从隐藏状态重新打开时 Home 回到顶部，但其他功能组的滚动位置可在当前进程内保留。

## 尺寸和定位

- Home 保持紧凑；Overview 和功能组按当前内容自适应高度。
- 内容高度不超过工作区时不显示无意义空白或滚动条。
- 内容超过工作区时限制主窗体最大高度并启用垂直滚动。
- 窗口位置和 clamp 必须使用完整的 `WorkingArea.Left/Top/Right/Bottom`，不能只使用 Width/Height；需要覆盖负坐标和非主显示器。
- 页面切换后的顺序为：应用页面投影、执行布局、计算受约束尺寸、恢复或定位滚动位置、限制窗口位置。
- owned window 的尺寸不再依赖主窗体高度；宽窗口在小工作区中也必须保留可访问区域。

## 主题、本地化和无障碍

- 新增 Home、Overview、Display、Devices、Application、Back、Unavailable、Unsupported、Disconnected、Loading、Error 及处理建议等文案必须进入 `Properties.Strings` 资源。
- 中性英文资源提供回退，同步简体中文和繁体中文；其他语言在翻译前使用中性资源。
- 复用现有图标资源和现有按钮样式，不引入新的导航组件库。
- 导航和动作使用真正可聚焦的控件，不把可点击 Label 作为新导航入口。
- Home、Overview 和每个功能组有明确初始焦点；Tab 顺序连续且不会进入隐藏控件。
- 可用条目和不可用说明均支持 Enter/Space；返回和设置按钮有 tooltip、`AccessibleName` 和合理的 `AccessibleRole`。
- 验证浅色、深色、Flat 和 Windows 主题动态切换，高 DPI 下不得重叠或裁切。

## 开发阶段

### 0. 建立行为基线（未完成）

- 记录普通笔记本、无 dGPU、ROG Ally、OLED、Matrix/Slash、外接键鼠等设备的当前支持和可见性。
- 验证托盘、热键、电源切换、睡眠恢复、XG Mobile、热插拔和全部命令行入口。
- 建立人工验证矩阵，后续提交以“硬件行为不变、旧入口可用”为红线。

### 1. 解除二级窗体定位耦合（代码完成，待人工验收）

- 已增加集中 owned form 定位方法，并移除子窗体对主窗体高度的依赖。
- 补充多显示器工作区原点修正和负坐标验证。
- 验收所有 owned windows 在主窗高度变化、小屏幕和多显示器上可显示、定位和关闭。

### 2. 统一子窗体生命周期（当前阶段）

- 删除剩余 `Text == ""` 生命周期哨兵。
- 将重复点击由关闭改为激活已有实例。
- 外接键鼠改为按设备标识管理单实例窗口。
- 补齐主题传播、关闭清理、`HideAll`、`HasAnyFocus` 和命令行路由。
- 完成后再进入页面导航开发。

### 3. 分离设备目录、功能可用性和页面可见性

- 增加轻量的设备类别目录、功能可用性和不可用原因状态。
- 修改 `InitVisual`、`VisualiseAlly`、`InitRearLight`、`InitMatrix`、`VisualiseScreen`、`HideGPUModes`、`VisualizePeripherals` 等路径，使其只更新支持状态和控件值。
- 增加统一页面投影，最终呈现由“当前页面 + 设备目录 + 功能状态”决定。
- 验收异步刷新、热插拔和恢复不会改变当前页面。

### 4. 增加导航壳和紧凑 Home

- 增加客户端导航栏、Home/Overview/四组页面状态、设置和返回操作。
- Home 只投影 `panelPerformance` 和 `panelGPU`。
- 主窗体从隐藏状态重新显示时回 Home。
- 接入按页面自适应尺寸、工作区限制和滚动状态。

### 5. 建设 Overview

- 按四组创建具体功能条目并标注 `Navigate`、`OpenOwned`、`DirectAction` 或 `ExplainUnavailable`。
- 实现按设备类别展示目录。
- 实现不可用样式、行内解释和键盘/屏幕阅读器行为。
- Matrix/Slash、Handheld 和外设使用单条目分层进入，不创建重复高级入口。

### 6. 按功能组迁移内嵌内容

按风险由低到高逐组迁移，每次只处理一组：

1. Application：Startup、Version/App update、BIOS/driver Updates、Extra、维护动作、Exit。
2. Display：Screen、Gamma/Visual/OLED、HDR 和 AMD OLED 动作。
3. Devices：Battery、charge status、Peripherals、Ally quick controls、Fans/Handheld destinations。
4. Lighting：built-in keyboard、rear light、Matrix/Slash quick controls 和高级窗口入口。

迁移只改变归属和投影，不改变现有事件处理器和硬件调用。

### 7. 完成本地化、主题和无障碍

- 增加并核对资源字符串和图标。
- 修正导航、功能条目和动态不可用状态的焦点与读屏信息。
- 验证浅色、深色、Flat、Windows 主题切换、高 DPI 和长文本语言。

### 8. 后续硬件 façade 解耦

- UI 稳定后，再按 Battery、Display、GPU、Peripherals 逐组消除硬件模块对 `Program.settingsForm.VisualiseXxx()` 的直接依赖。
- 每组改动单独提交，不与 Designer 大改或其他功能迁移混合。
- Matrix/Aura 生命周期依赖最深，最后处理。

## 验收矩阵

每个实现提交执行：

```bash
GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug
git diff --check
```

人工验证至少覆盖：

- 1366x768、1920x1080、4K。
- 100%、125%、150%、200% DPI。
- 主显示器、负坐标副显示器和不同任务栏位置。
- 浅色、深色、Flat 和 Windows 主题动态切换。
- 英文、简体中文、繁体中文和长文本语言。
- 普通笔记本、无 dGPU、ROG Ally、OLED、Matrix、Slash、后灯、无外设和多外设。
- GPU 模式、电源切换、睡眠恢复、托盘反复开关、XG Mobile 和外设热插拔。
- Fans、Extra、Handheld、Matrix/Slash、Updates 及多键盘/鼠标窗口的单实例和激活行为。
- `cpu/gpu/uv/services/colors/autoupdate` 命令行入口。
- 应用更新与 BIOS/驱动更新的独立行为。
- 不可用条目的鼠标、键盘、行内解释和无硬件副作用。

## 建议提交顺序

1. `refactor(ui): finish owned window lifecycle`
2. `refactor(ui): model settings feature availability`
3. `refactor(ui): separate page visibility from hardware state`
4. `feat(ui): add compact settings navigation shell`
5. `feat(ui): add grouped settings overview`
6. `feat(ui): add application settings page`
7. `feat(ui): add display settings page`
8. `feat(ui): add device settings page`
9. `feat(ui): add lighting settings page`
10. `refactor(ui): complete theme and accessibility routing`
11. `docs(ui): record redesign validation matrix`

## 回滚原则

- 每个提交保持单一目的，设备特定回归按提交粒度回滚。
- 保留现有控件和独立窗口作为兼容实现，直到新导航稳定一个发布周期。
- 不删除旧事件处理器，直到对应新入口完成全部验收。
- 任一阶段发现硬件行为、命令行入口或托盘生命周期变化时，先停止页面迁移并恢复行为基线。

# UI 重构任务交接

## 任务

沿 `docs/UI-REDESIGN-PLAN.md` 持续完成 G-Helper UI 重构，执行“小步开发、验证、立即更新进度”的循环，直到交付完整可构建和可发布产物。

当前任务因用户切换 Agent/工作台而暂停。接手后从阶段 5 继续，不得重做或回退阶段 2-4。

## 必读文件

1. `AGENTS.md`：仓库规则、构建命令、Windows/x64/.NET 10 约束。
2. `docs/UI-REDESIGN-PLAN.md`：已确认的完整产品和技术规格。
3. `docs/UI-REDESIGN-PROGRESS.md`：唯一执行事实来源、逐步验证结果、残余风险和下一动作。
4. 本文件：当前交接摘要和恢复顺序。

## 当前仓库状态

- 记录时间：2026-08-25，当前开发轮已完成阶段 2-7 的代码路径。
- 分支：`main`，当前提交将在本轮发布前创建。
- 本轮将提交并推送到 `origin/main`，不推送 `upstream`。
- 当前开发改动包含 `app/Fans.cs`、`app/Program.cs`、`app/Settings.cs`、`app/Settings.Designer.cs`、三套字符串资源、`app/GHelper.csproj` 以及 UI 重构计划/进度文档；本轮将统一提交到 `origin/main`。
- 最终 Debug build 成功，0 警告、0 错误；`git diff --check` 通过；默认 Release single-file publish 已成功生成。
- 默认发布产物位于 `E:\ST\projects\Ghelper\app\bin\x64\Release\net10.0-windows\win-x64\publish\GHelper.exe`。

## 已完成工作

### 阶段 2：Owned window 生命周期

代码完成/待人工验收：

- Fans、Extra、Handheld、Matrix/Slash、Updates 使用统一 open-or-activate 行为。
- 外接键鼠以 `Dictionary<IPeripheral, RForm>` 和引用相等比较管理：同设备单实例，不同设备可同时打开。
- `OwnedForms` 统一承担主题刷新、焦点检查和 `HideAll` 关闭。
- 删除 Fans 的 `Text == ""` 生命周期哨兵并处理校准关闭竞态。
- owned window 定位使用完整工作区边界。

### 阶段 3：功能可用性与页面可见性分离

代码完成/待人工验收：

- `SettingsForm` 增加 Visual、Ally、Rear Light、Matrix/Slash、Screen、GPU、Peripherals 七个最小能力状态。
- `ApplyFeatureVisibility()` 成为顶层功能区唯一运行时投影入口。
- 七个硬件/刷新入口只更新能力和值，不再直接决定当前页面。
- `_VisualiseAura` 不再读取 `panelRearLight.Visible` 作为硬件能力。
- 外设使用一次快照决定能力和按钮渲染。
- `HideGPUModes` 已补 UI 线程封送。

### 阶段 4：导航壳与紧凑 Home

代码完成/待人工验收：

- 主窗体已有 `Home`、`Overview`、`Display`、`Lighting`、`Devices`、`Application` 六种独立页面状态。
- 新增固定三列导航栏：返回按钮、页面标题、设置按钮。
- 新增 `AutoScroll` 内容容器，原十三个功能区只改变父容器，不复制控件或硬件事件。
- Home 只投影 Performance 和可用 GPU。
- 四个功能组按已确认归属投影现有顶层区域；Overview 暂时为空。
- `NavigateTo()`、`ResetToHome()`、每页滚动位置和页面标题已接入。
- 主窗从隐藏状态重新显示时回 Home 顶部并聚焦 Performance。
- 页面高度按可见内容计算，限制在当前显示器完整工作区内，较长内容滚动。
- 返回按钮复用 `icons8_next_32` 的副本并翻转；设置按钮复用 `icons8_settings_32`。
- 中性英文、简体中文、繁体中文已增加 Home、Overview、Display、Devices、Application、Back 资源键；其他语言使用中性回退。

## 当前验证证据

最近一次最终命令：

```bash
GITHUB_ACTIONS=true dotnet build app/GHelper.sln --configuration Debug
```

结果：成功，0 警告、0 错误，用时 2.87 秒。

最近一次 `git diff --check`：通过，仅有 `docs/UI-REDESIGN-PLAN.md` 的既有 LF/CRLF 转换提示。

聚焦静态检查已确认：

- 十三个顶层功能区的运行时 `Visible` 写入只存在于 `ApplyFeatureVisibility()`。
- Form 顶层只挂载 `panelNavigation` 和 `panelContent`。
- 旧十三个功能区只挂载到 `panelContent`。
- 六种页面状态、滚动位置、`ResetToHome()` 和工作区约束各有单一实现入口。

## 当前状态：阶段 2-7 代码完成/待人工验收

已完成：

- Overview 已实现四组具体功能目录、动作模型、设备类别过滤、六态不可用状态、行内解释和无障碍描述。
- Application、Display、Devices、Lighting 页面投影和归属已完成；Extra 位于 Application，充电状态位于 Battery，AMD OLED 位于 Display/Gamma。
- 中性英文、简体中文、繁体中文资源已同步；导航、键盘和 `AccessibleDescription` 代码路径已接入。
- 主窗体二级页面点击系统关闭按钮返回上一级；Home 页仍隐藏到托盘，Application 页 Exit 仍执行完整退出流程。
- 最终 Debug build、`git diff --check`、静态审计和默认 Release single-file publish 已通过。

人工验收剩余项：

- ASUS 设备能力组合、DPI/多显示器、动态主题、读屏、键盘焦点、热插拔、睡眠恢复和 owned-window 行为矩阵。
- 默认发布目录已重新生成并用于本轮人工验收：`E:\ST\projects\Ghelper\app\bin\x64\Release\net10.0-windows\win-x64\publish\GHelper.exe`。

阶段 8 façade 解耦按计划延后，不阻塞首个 UI 交付。


## 强制约束

- 用户明确要求全部分析、审查、实现和验证由主 Agent 完成。禁止调用子智能体或 `Agent` 工具。
- 普通独立本地工具调用可以并行。
- `ponytail full` 持续生效：复用现有代码和 .NET/WinForms 能力，不新增 speculative abstraction、依赖或第二套硬件状态机。
- 每完成一个可验证部分，必须立即更新 `docs/UI-REDESIGN-PROGRESS.md`，记录代码、命令、失败与修复、未覆盖风险和唯一下一动作。
- 不修改硬件命令、参数、调用顺序、`AppConfig` key 或设置提交时机。
- 不终止用户运行中的 G-Helper；构建必须使用 `GITHUB_ACTIONS=true`。
- 不提交或推送，除非用户明确要求。

## 交付后动作

交付给用户进行真实设备/UI 人工验收；若发现具体设备或布局回归，再按单一问题执行最小改动、立即构建和静态验证的修复循环。

## 完成标准

以 `docs/UI-REDESIGN-PROGRESS.md` 的“交付目标”和 `docs/UI-REDESIGN-PLAN.md` 的“验收矩阵”为准。构建通过只是证据之一，不能替代页面行为、owned window、能力状态、资源、无障碍和发布产物的逐项核验。

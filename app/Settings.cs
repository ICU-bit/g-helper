using GHelper.Ally;
using GHelper.AnimeMatrix;
using GHelper.AutoUpdate;
using GHelper.Battery;
using GHelper.Display;
using GHelper.Fan;
using GHelper.Gpu;
using GHelper.Helpers;
using GHelper.Input;
using GHelper.Mode;
using GHelper.Peripherals;
using GHelper.Peripherals.Keyboard;
using GHelper.Peripherals.Mouse;
using GHelper.Properties;
using GHelper.UI;
using GHelper.USB;
using System.Diagnostics;
using System.Timers;

namespace GHelper
{
    public partial class SettingsForm : RForm
    {
        private enum SettingsPage
        {
            Home,
            Overview,
            Display,
            Lighting,
            Devices,
            Application
        }

        private enum OverviewAction
        {
            Navigate,
            OpenOwned,
            DirectAction,
            ExplainUnavailable
        }

        private enum OverviewStatus
        {
            Available,
            Unsupported,
            Disconnected,
            Conditional,
            Loading,
            Error
        }

        private sealed record OverviewItem(
            string Label,
            SettingsPage Page,
            Control? Target,
            OverviewAction Action,
            Action? Execute,
            Func<OverviewStatus> GetStatus)
        {
            public RButton? Button { get; set; }
            public Label? Explanation { get; set; }
        }

        ContextMenuStrip contextMenuStrip = new CustomContextMenu();
        ToolStripMenuItem menuEco, menuStandard, menuUltimate, menuOptimized;
        public GPUModeControl gpuControl;
        public AllyControl allyControl;
        AutoUpdateControl updateControl;

        private readonly Dictionary<IPeripheral, RForm> peripheralSettings = new(ReferenceEqualityComparer.Instance);

        public AniMatrixControl matrixControl;

        public static System.Timers.Timer sensorTimer = default!;
        private static readonly bool sensorsAlways = AppConfig.Is("sensors_always");
        private readonly System.Windows.Forms.Timer batteryTimer = new() { Interval = 200 };

        public Matrix? matrixForm;
        public Slash? slashForm;
        public Fans? fansForm;
        public Extra? extraForm;
        public Updates? updatesForm;
        public Handheld? handheldForm;

        static long lastRefresh;
        static long lastBatteryRefresh;
        static long lastLostFocus;

        bool isGpuSection = true;
        bool isMuxGpu = true;

        bool visualAvailable;
        bool allyAvailable;
        bool rearLightAvailable;
        bool matrixAvailable = true;
        bool screenAvailable = true;
        bool gpuAvailable = true;
        bool peripheralsAvailable;

        SettingsPage currentPage = SettingsPage.Home;
        readonly Dictionary<SettingsPage, int> pageScrollPositions = new();
        private readonly Panel overviewPanel = new() { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        private readonly List<OverviewItem> overviewItems = new();

        private OverviewItem? expandedOverviewItem;

        bool batteryMouseOver = false;
        bool batteryFullMouseOver = false;

        bool sliderGammaIgnore = false;
        bool activateCheck = false;

        public SettingsForm()
        {

            InitializeComponent();
            panelContent.Controls.Add(overviewPanel);
            overviewPanel.AccessibleRole = AccessibleRole.Grouping;
            overviewPanel.AccessibleName = Properties.Strings.Overview;
            buttonBack.Image = new Bitmap(Properties.Resources.icons8_next_32);
            buttonBack.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            InitTheme(true);

            gpuControl = new GPUModeControl(this);
            updateControl = new AutoUpdateControl(this);
            matrixControl = new AniMatrixControl(this);
            allyControl = new AllyControl(this);

            buttonSilent.Text = Properties.Strings.Silent;
            buttonBalanced.Text = Properties.Strings.Balanced;
            buttonTurbo.Text = Properties.Strings.Turbo;
            buttonFans.Text = Properties.Strings.FansPower;

            buttonEco.Text = Properties.Strings.EcoMode;
            buttonUltimate.Text = Properties.Strings.UltimateMode;
            buttonStandard.Text = Properties.Strings.StandardMode;
            buttonOptimized.Text = Properties.Strings.Optimized;
            buttonStopGPU.Text = Properties.Strings.StopGPUApps;

            buttonScreenAuto.Text = Properties.Strings.AutoMode;
            buttonMiniled.Text = Properties.Strings.Multizone;

            buttonKeyboardColor.Text = Properties.Strings.Color;
            buttonKeyboard.Text = Properties.Strings.ExtraSettings;

            labelPerf.Text = Properties.Strings.PerformanceMode;
            labelGPU.Text = Properties.Strings.GPUMode;
            labelSreen.Text = Properties.Strings.LaptopScreen;
            UpdateKeyboardLabel();
            labelMatrix.Text = Properties.Strings.AnimeMatrix;
            labelBatteryTitle.Text = Properties.Strings.BatteryChargeLimit;

            checkStartup.Text = Properties.Strings.RunOnStartup;

            buttonMatrix.Text = "Matrix";
            buttonQuit.Text = Properties.Strings.Quit;
            buttonUpdates.Text = Properties.Strings.BiosAndDriverUpdates;
            buttonEnergySaver.Text = Properties.Strings.EnergySettings;
            buttonAmdOled.Text = Properties.Strings.AmdOledSaver;
            buttonArmoury.Text = Properties.Strings.ArmouryCrate;

            buttonController.Text = Properties.Strings.Controller;
            labelAlly.Text = Properties.Strings.AllyController;

            // Accessible Labels

            panelMatrix.AccessibleName = Properties.Strings.AnimeMatrix;
            sliderBattery.AccessibleName = Properties.Strings.BatteryChargeLimit;
            buttonQuit.AccessibleName = Properties.Strings.Quit;
            buttonUpdates.AccessibleName = Properties.Strings.BiosAndDriverUpdates;
            panelPerformance.AccessibleName = Properties.Strings.PerformanceMode;
            buttonSilent.AccessibleName = Properties.Strings.Silent;
            buttonBalanced.AccessibleName = Properties.Strings.Balanced;
            buttonTurbo.AccessibleName = Properties.Strings.Turbo;
            buttonFans.AccessibleName = Properties.Strings.FansAndPower;
            panelGPU.AccessibleName = Properties.Strings.GPUMode;
            buttonEco.AccessibleName = Properties.Strings.EcoMode;
            buttonStandard.AccessibleName = Properties.Strings.StandardMode;
            buttonOptimized.AccessibleName = Properties.Strings.Optimized;
            buttonUltimate.AccessibleName = Properties.Strings.UltimateMode;
            panelScreen.AccessibleName = Properties.Strings.LaptopScreen;

            buttonScreenAuto.AccessibleName = Properties.Strings.AutoMode;
            //button60Hz.AccessibleName = "60Hz Refresh Rate";
            //button120Hz.AccessibleName = "Maximum Refresh Rate";

            panelKeyboard.AccessibleName = Properties.Strings.LaptopKeyboard;
            buttonKeyboard.AccessibleName = Properties.Strings.ExtraSettings;
            buttonKeyboardColor.AccessibleName = Properties.Strings.LaptopKeyboard + " " + Properties.Strings.Color;
            comboKeyboard.AccessibleName = Properties.Strings.LaptopBacklight;

            FormClosing += SettingsForm_FormClosing;
            Deactivate += SettingsForm_LostFocus;
            Activated += SettingsForm_Focused;

            buttonSilent.BorderColor = colorEco;
            buttonBalanced.BorderColor = colorStandard;
            buttonTurbo.BorderColor = colorTurbo;
            buttonFans.BorderColor = colorCustom;

            buttonEco.BorderColor = colorEco;
            buttonStandard.BorderColor = colorStandard;
            buttonUltimate.BorderColor = colorTurbo;
            buttonOptimized.BorderColor = colorEco;
            buttonXGM.BorderColor = colorTurbo;

            button60Hz.BorderColor = colorGray;
            button120Hz.BorderColor = colorGray;
            buttonScreenAuto.BorderColor = colorGray;
            buttonMiniled.BorderColor = colorTurbo;

            buttonEnergySaver.BackColor = colorEco;
            buttonEnergySaver.ForeColor = SystemColors.ControlLightLight;
            buttonEnergySaver.Click += ButtonEnergySaver_Click;

            buttonAmdOled.BackColor = colorTurbo;
            buttonAmdOled.ForeColor = SystemColors.ControlLightLight;
            buttonAmdOled.Click += ButtonAmdOled_Click;

            buttonArmoury.BackColor = colorTurbo;
            buttonArmoury.ForeColor = SystemColors.ControlLightLight;
            buttonArmoury.Click += ButtonArmoury_Click;

            buttonSilent.Click += ButtonSilent_Click;
            buttonBalanced.Click += ButtonBalanced_Click;
            buttonTurbo.Click += ButtonTurbo_Click;

            buttonBack.Click += ButtonBack_Click;
            buttonSettings.Click += ButtonSettings_Click;
            toolTip.SetToolTip(buttonBack, Properties.Strings.Back);
            toolTip.SetToolTip(buttonSettings, Properties.Strings.Overview);

            buttonEco.Click += ButtonEco_Click;
            buttonStandard.Click += ButtonStandard_Click;
            buttonUltimate.Click += ButtonUltimate_Click;
            buttonOptimized.Click += ButtonOptimized_Click;
            buttonStopGPU.Click += ButtonStopGPU_Click;
            pictureGPU.Click += PictureGPU_Click;

            VisibleChanged += SettingsForm_VisibleChanged;

            button60Hz.Click += Button60Hz_Click;
            button120Hz.Click += Button120Hz_Click;
            buttonScreenAuto.Click += ButtonScreenAuto_Click;
            buttonMiniled.Click += ButtonMiniled_Click;
            buttonFHD.Click += ButtonFHD_Click;
            buttonHDRControl.Click += ButtonHDRControl_Click;

            buttonQuit.Click += ButtonQuit_Click;

            buttonKeyboardColor.Click += ButtonKeyboardColor_Click;
            buttonKeyboardColor.Swatch2Click += ButtonKeyboardColor2_Click;

            buttonFans.Click += ButtonFans_Click;
            buttonKeyboard.Click += ButtonKeyboard_Click;
            buttonController.Click += ButtonHandheld_Click;

            labelCPUFan.Click += LabelCPUFan_Click;
            labelGPUFan.Click += LabelCPUFan_Click;

            comboMatrix.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMatrixRunning.DropDownStyle = ComboBoxStyle.DropDownList;

            comboMatrix.DropDownClosed += ComboMatrix_SelectedValueChanged;
            comboMatrixRunning.DropDownClosed += ComboMatrixRunning_SelectedValueChanged;

            buttonMatrix.Click += ButtonMatrix_Click;

            checkStartup.Checked = Startup.IsScheduled();
            checkStartup.CheckedChanged += CheckStartup_CheckedChanged;

            labelVersion.Click += LabelVersion_Click;
            labelVersion.ForeColor = Color.FromArgb(128, Color.Gray);

            buttonOptimized.MouseMove += ButtonOptimized_MouseHover;
            buttonOptimized.MouseLeave += ButtonGPU_MouseLeave;

            buttonEco.MouseMove += ButtonEco_MouseHover;
            buttonEco.MouseLeave += ButtonGPU_MouseLeave;

            buttonStandard.MouseMove += ButtonStandard_MouseHover;
            buttonStandard.MouseLeave += ButtonGPU_MouseLeave;

            buttonUltimate.MouseMove += ButtonUltimate_MouseHover;
            buttonUltimate.MouseLeave += ButtonGPU_MouseLeave;

            tableGPU.MouseMove += ButtonXGM_MouseMove;
            tableGPU.MouseLeave += ButtonGPU_MouseLeave;

            buttonXGM.Click += ButtonXGM_Click;

            buttonScreenAuto.MouseMove += ButtonScreenAuto_MouseHover;
            buttonScreenAuto.MouseLeave += ButtonScreen_MouseLeave;

            button60Hz.MouseMove += Button60Hz_MouseHover;
            button60Hz.MouseLeave += ButtonScreen_MouseLeave;

            button120Hz.MouseMove += Button120Hz_MouseHover;
            button120Hz.MouseLeave += ButtonScreen_MouseLeave;

            buttonFHD.MouseMove += ButtonFHD_MouseHover;
            buttonFHD.MouseLeave += ButtonScreen_MouseLeave;

            buttonUpdates.Click += ButtonUpdates_Click;

            BuildOverview();

            sliderBattery.MouseUp += SliderBattery_MouseUp;
            sliderBattery.KeyUp += SliderBattery_KeyUp;
            sliderBattery.ValueChanged += SliderBattery_ValueChanged;
            batteryTimer.Tick += (_, _) => { batteryTimer.Stop(); BatteryControl.SetBatteryChargeLimit(sliderBattery.Value); };
            if (AppConfig.IsChargeLimit6080()) sliderBattery.supportedValues = new() { 60, 65, 70, 75, 80, 100 };

            sensorTimer = new System.Timers.Timer(AppConfig.Get("sensor_timer", 1000));
            sensorTimer.Elapsed += OnTimedEvent;
            sensorTimer.Enabled = sensorsAlways;

            labelCharge.MouseEnter += PanelBattery_MouseEnter;
            labelCharge.MouseLeave += PanelBattery_MouseLeave;
            labelBattery.Click += LabelBattery_Click;

            buttonPeripheral1.Click += ButtonPeripheral_Click;
            buttonPeripheral2.Click += ButtonPeripheral_Click;
            buttonPeripheral3.Click += ButtonPeripheral_Click;

            buttonPeripheral1.MouseEnter += ButtonPeripheral_MouseEnter;
            buttonPeripheral2.MouseEnter += ButtonPeripheral_MouseEnter;
            buttonPeripheral3.MouseEnter += ButtonPeripheral_MouseEnter;

            buttonBatteryFull.MouseEnter += ButtonBatteryFull_MouseEnter;
            buttonBatteryFull.MouseLeave += ButtonBatteryFull_MouseLeave;
            buttonBatteryFull.Click += ButtonBatteryFull_Click;

            buttonControllerMode.Click += ButtonControllerMode_Click;
            buttonBacklight.Click += ButtonBacklight_Click;

            buttonFPS.Click += ButtonFPS_Click;
            buttonOverlay.Click += ButtonOverlay_Click;
            buttonOverlay.BorderColor = colorStandard;

            buttonAutoTDP.Click += ButtonAutoTDP_Click;
            buttonAutoTDP.BorderColor = colorTurbo;

            Text = "G-Helper " + (ProcessHelper.IsUserAdministrator() ? "—" : "-") + " " + AppConfig.GetModelShort();
            TopMost = AppConfig.Is("topmost");

            //This will auto position the window again when it resizes. Might mess with position if people drag the window somewhere else.
            this.Resize += SettingsForm_Resize;

            VisualiseFnLock();
            buttonFnLock.Click += ButtonFnLock_Click;

            labelVisual.Click += LabelVisual_Click;
            labelCharge.Click += LabelCharge_Click;

            labelBacklight.ForeColor = colorStandard;
            labelBacklight.Click += LabelBacklight_Click;

            panelPerformance.Focus();
            InitVisual();
            ResetToHome();
        }

        private void ButtonArmoury_Click(object? sender, EventArgs e)
        {
            var dialogResult = MessageBox.Show(this, "Armoury Crate is active, download official uninstaller app?", "Armoury Crate", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes) AsusService.RunArmouryUninstaller();
        }


        private void ButtonAmdOled_Click(object? sender, EventArgs e)
        {
            AmdDisplay.RunAdrenaline();
            activateCheck = true;
        }

        private void LabelBattery_Click(object? sender, EventArgs e)
        {
            HardwareControl.chargeWatt = !HardwareControl.chargeWatt;
            RefreshSensors(true);
        }

        private void ButtonEnergySaver_Click(object? sender, EventArgs e)
        {
            KeyboardHook.KeyKeyPress(Keys.LWin, Keys.A);
        }

        private void LabelBacklight_Click(object? sender, EventArgs e)
        {
            if (AppConfig.IsDynamicLighting() && DynamicLightingHelper.IsEnabled()) DynamicLightingHelper.OpenSettings();
        }

        private void ButtonFHD_Click(object? sender, EventArgs e)
        {
            ScreenControl.ToogleFHD();
        }

        private void ButtonHDRControl_Click(object? sender, EventArgs e)
        {
            ScreenControl.ToogleHDRControl();
        }

        private void SliderBattery_ValueChanged(object? sender, EventArgs e)
        {
            VisualiseBatteryTitle(sliderBattery.Value);
        }

        private void SliderBattery_KeyUp(object? sender, KeyEventArgs e)
        {
            batteryTimer.Stop();
            batteryTimer.Start();
        }

        private void SliderBattery_MouseUp(object? sender, MouseEventArgs e)
        {
            batteryTimer.Stop();
            batteryTimer.Start();
        }

        private void ButtonAutoTDP_Click(object? sender, EventArgs e)
        {
            allyControl.ToggleAutoTDP();
        }

        private void LabelCharge_Click(object? sender, EventArgs e)
        {
            BatteryControl.BatteryReport();
        }

        private void BuildOverview()
        {
            overviewItems.Clear();
            overviewItems.AddRange(new[]
            {
                new OverviewItem(Properties.Strings.LaptopScreen, SettingsPage.Display, panelScreen, OverviewAction.Navigate, null, () => screenAvailable ? OverviewStatus.Available : OverviewStatus.Conditional),
                new OverviewItem(Properties.Strings.VisualMode + " / OLED", SettingsPage.Display, panelGamma, OverviewAction.Navigate, null, () => visualAvailable ? OverviewStatus.Available : OverviewStatus.Unsupported),
                new OverviewItem(Properties.Strings.AmdOledSaver, SettingsPage.Display, null, OverviewAction.DirectAction, () => ButtonAmdOled_Click(this, EventArgs.Empty), () => buttonAmdOled.Visible ? OverviewStatus.Available : OverviewStatus.Unsupported),
                new OverviewItem(Properties.Strings.LaptopKeyboard, SettingsPage.Lighting, panelKeyboard, OverviewAction.Navigate, null, () => OverviewStatus.Available),
                new OverviewItem(Properties.Strings.Battery, SettingsPage.Devices, panelBattery, OverviewAction.Navigate, null, () => OverviewStatus.Available),
                new OverviewItem(Properties.Strings.FansAndPower, SettingsPage.Devices, null, OverviewAction.OpenOwned, () => FansToggle(), () => OverviewStatus.Available),
                new OverviewItem(Properties.Strings.Peripherals, SettingsPage.Devices, panelPeripherals, OverviewAction.Navigate, null, () => peripheralsAvailable ? OverviewStatus.Available : OverviewStatus.Disconnected),
                new OverviewItem(Properties.Strings.RunOnStartup, SettingsPage.Application, panelStartup, OverviewAction.Navigate, null, () => OverviewStatus.Available),
                new OverviewItem(Properties.Strings.Updates, SettingsPage.Application, panelVersion, OverviewAction.DirectAction, () => updateControl.CheckForUpdates(), () => OverviewStatus.Available),
                new OverviewItem(Properties.Strings.BiosAndDriverUpdates, SettingsPage.Application, null, OverviewAction.OpenOwned, () => ButtonUpdates_Click(this, EventArgs.Empty), () => OverviewStatus.Available),
                new OverviewItem(Properties.Strings.EnergySettings, SettingsPage.Application, null, OverviewAction.DirectAction, () => ButtonEnergySaver_Click(this, EventArgs.Empty), () => buttonEnergySaver.Visible ? OverviewStatus.Available : OverviewStatus.Conditional),
                new OverviewItem(Properties.Strings.ArmouryCrate, SettingsPage.Application, null, OverviewAction.DirectAction, () => ButtonArmoury_Click(this, EventArgs.Empty), () => buttonArmoury.Visible ? OverviewStatus.Available : OverviewStatus.Unsupported),
                new OverviewItem(Properties.Strings.ExtraSettings, SettingsPage.Application, null, OverviewAction.OpenOwned, () => ButtonKeyboard_Click(this, EventArgs.Empty), () => OverviewStatus.Available),
                new OverviewItem(Properties.Strings.Quit, SettingsPage.Application, null, OverviewAction.DirectAction, () => ButtonQuit_Click(this, EventArgs.Empty), () => OverviewStatus.Available),
            });

            if (AppConfig.HasRearLight())
                overviewItems.Insert(3, new OverviewItem(Properties.Strings.Lightbar, SettingsPage.Lighting, panelRearLight, OverviewAction.Navigate, null, () => rearLightAvailable ? OverviewStatus.Available : OverviewStatus.Unsupported));

            if (AppConfig.IsAnimeMatrix() || AppConfig.IsSlash())
                overviewItems.Insert(4, new OverviewItem(Properties.Strings.AnimeMatrix, SettingsPage.Lighting, panelMatrix, OverviewAction.Navigate, null, () => matrixAvailable ? OverviewStatus.Available : OverviewStatus.Unsupported));

            if (AppConfig.IsAlly())
                overviewItems.Insert(8, new OverviewItem(Properties.Strings.AllyController, SettingsPage.Devices, panelAlly, OverviewAction.Navigate, null, () => allyAvailable ? OverviewStatus.Available : OverviewStatus.Unsupported));

            overviewPanel.Controls.Clear();
            overviewPanel.Padding = new Padding(20, 10, 20, 20);
            TableLayoutPanel groups = new()
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            AddOverviewGroup(groups, Properties.Strings.Display, overviewItems.Where(item => item.Page == SettingsPage.Display));
            AddOverviewGroup(groups, Properties.Strings.Lighting, overviewItems.Where(item => item.Page == SettingsPage.Lighting));
            AddOverviewGroup(groups, Properties.Strings.Devices, overviewItems.Where(item => item.Page == SettingsPage.Devices));
            AddOverviewGroup(groups, Properties.Strings.Application, overviewItems.Where(item => item.Page == SettingsPage.Application));

            overviewPanel.Controls.Add(groups);
            ControlHelper.Adjust(this);
        }

        private void AddOverviewGroup(TableLayoutPanel groups, string title, IEnumerable<OverviewItem> items)
        {
            Label heading = new()
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(4, 10, 4, 4),
                Text = title
            };
            groups.Controls.Add(heading);

            foreach (OverviewItem item in items)
            {
                RButton button = new()
                {
                    AccessibleName = item.Label,
                    Dock = DockStyle.Top,
                    Height = 48,
                    Margin = new Padding(0, 2, 0, 2),
                    Name = "overview" + item.Label.Replace(" ", string.Empty),
                    Secondary = true,
                    TabStop = true,
                    Text = item.Label,
                    Tag = item
                };
                Label explanation = new()
                {
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    ForeColor = SystemColors.GrayText,
                    Margin = new Padding(8, 0, 8, 6),
                    MaximumSize = new Size(760, 0),
                    Visible = false
                };
                item.Button = button;
                item.Explanation = explanation;
                button.Click += OverviewItem_Click;
                button.KeyDown += OverviewItem_KeyDown;
                groups.Controls.Add(button);
                groups.Controls.Add(explanation);
            }
        }

        private void OverviewItem_Click(object? sender, EventArgs e)
        {
            if (sender is not RButton { Tag: OverviewItem item }) return;

            OverviewStatus status = item.GetStatus();
            if (status != OverviewStatus.Available)
            {
                ToggleOverviewExplanation(item, status);
                return;
            }

            if (item.Action == OverviewAction.Navigate)
            {
                NavigateTo(item.Page, true);
                if (item.Target is not null)
                {
                    panelContent.ScrollControlIntoView(item.Target);
                    item.Target.Focus();
                }
                return;
            }

            item.Execute?.Invoke();
        }

        private void OverviewItem_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                OverviewItem_Click(sender, EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void ToggleOverviewExplanation(OverviewItem item, OverviewStatus status)
        {
            if (expandedOverviewItem is not null && !ReferenceEquals(expandedOverviewItem, item))
            {
                expandedOverviewItem.Explanation!.Visible = false;
                expandedOverviewItem.Button!.AccessibleDescription = null;
            }

            if (ReferenceEquals(expandedOverviewItem, item) && item.Explanation!.Visible)
            {
                item.Explanation.Visible = false;
                item.Button!.AccessibleDescription = null;
                expandedOverviewItem = null;
                return;
            }

            string reason = status switch
            {
                OverviewStatus.Unsupported => Properties.Strings.OverviewUnsupported,
                OverviewStatus.Disconnected => Properties.Strings.OverviewDisconnected,
                OverviewStatus.Conditional => Properties.Strings.OverviewConditional,
                OverviewStatus.Loading => Properties.Strings.OverviewLoading,
                OverviewStatus.Error => Properties.Strings.OverviewError,
                _ => string.Empty
            };
            item.Explanation!.Text = reason;
            item.Explanation.Visible = true;
            item.Button!.AccessibleDescription = reason;
            expandedOverviewItem = item;
            overviewPanel.PerformLayout();
            ResizeForCurrentPage();
        }

        private void RefreshOverviewStatuses()
        {
            foreach (OverviewItem item in overviewItems)
            {
                if (item.Button is null) continue;
                OverviewStatus status = item.GetStatus();
                bool available = status == OverviewStatus.Available;
                item.Button.ForeColor = available ? foreMain : SystemColors.GrayText;
                item.Button.AccessibleDescription = available ? null : GetOverviewReason(status);
                item.Button.Invalidate();
            }
        }

        private static string GetOverviewReason(OverviewStatus status)
        {
            return status switch
            {
                OverviewStatus.Unsupported => Properties.Strings.OverviewUnsupported,
                OverviewStatus.Disconnected => Properties.Strings.OverviewDisconnected,
                OverviewStatus.Conditional => Properties.Strings.OverviewConditional,
                OverviewStatus.Loading => Properties.Strings.OverviewLoading,
                OverviewStatus.Error => Properties.Strings.OverviewError,
                _ => string.Empty
            };
        }

        private void ApplyFeatureVisibility()
        {
            if (InvokeRequired) { Invoke(ApplyFeatureVisibility); return; }

            int scroll = -panelContent.AutoScrollPosition.Y;
            bool home = currentPage == SettingsPage.Home;
            bool overview = currentPage == SettingsPage.Overview;
            bool display = currentPage == SettingsPage.Display;
            bool lighting = currentPage == SettingsPage.Lighting;
            bool devices = currentPage == SettingsPage.Devices;
            bool application = currentPage == SettingsPage.Application;

            overviewPanel.Visible = overview;
            panelPerformance.Visible = home;
            panelGPU.Visible = home && gpuAvailable;

            panelScreen.Visible = display && screenAvailable;
            panelGamma.Visible = display && visualAvailable;

            panelKeyboard.Visible = lighting;
            panelRearLight.Visible = lighting && rearLightAvailable;
            panelMatrix.Visible = lighting && matrixAvailable;

            panelBattery.Visible = devices;
            panelPeripherals.Visible = devices && peripheralsAvailable;
            panelAlly.Visible = devices && allyAvailable;

            panelStartup.Visible = application;
            panelVersion.Visible = application;
            panelFooter.Visible = application;

            panelKeyboardTitle.Visible = !allyAvailable;
            panelKeyboard.Padding = new Padding(panelKeyboard.Padding.Left, allyAvailable ? 0 : 20, panelKeyboard.Padding.Right, panelKeyboard.Padding.Bottom);
            tableAMD.Visible = allyAvailable;

            panelContent.PerformLayout();
            ResizeForCurrentPage();
            RefreshOverviewStatuses();
            panelContent.AutoScrollPosition = new Point(0, scroll);
        }

        private void NavigateTo(SettingsPage page, bool resetScroll = false)
        {
            if (currentPage != page)
                pageScrollPositions[currentPage] = -panelContent.AutoScrollPosition.Y;

            currentPage = page;
            labelPageTitle.Text = page switch
            {
                SettingsPage.Home => Properties.Strings.Home,
                SettingsPage.Overview => Properties.Strings.Overview,
                SettingsPage.Display => Properties.Strings.Display,
                SettingsPage.Lighting => Properties.Strings.Lighting,
                SettingsPage.Devices => Properties.Strings.Devices,
                SettingsPage.Application => Properties.Strings.Application,
                _ => Properties.Strings.Home,
            };

            buttonSettings.Visible = page == SettingsPage.Home;
            buttonBack.Visible = page != SettingsPage.Home;

            ApplyFeatureVisibility();

            int scroll = resetScroll ? 0 : pageScrollPositions.GetValueOrDefault(page);
            panelContent.AutoScrollPosition = new Point(0, scroll);

            if (page == SettingsPage.Home)
                panelPerformance.Focus();
            else
                buttonBack.Focus();
        }

        public void ResetToHome()
        {
            pageScrollPositions[SettingsPage.Home] = 0;
            NavigateTo(SettingsPage.Home, true);
        }

        private void ResizeForCurrentPage()
        {
            int contentHeight = panelContent.Controls.Cast<Control>().Where(control => control.Visible).Sum(control => control.Height);
            int nonClientHeight = Height - ClientSize.Height;
            int desiredHeight = Padding.Vertical + panelNavigation.Height + contentHeight + nonClientHeight;
            Rectangle workArea = Screen.FromControl(this).WorkingArea;

            Height = Math.Clamp(desiredHeight, MinimumSize.Height, Math.Max(MinimumSize.Height, workArea.Height - 20));

            Left = Math.Clamp(Left, workArea.Left, Math.Max(workArea.Left, workArea.Right - Width));
            Top = Math.Clamp(Top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - Height));
        }

        private void ButtonSettings_Click(object? sender, EventArgs e)
        {
            NavigateTo(SettingsPage.Overview);
        }

        private void ButtonBack_Click(object? sender, EventArgs e)
        {
            NavigateTo(currentPage == SettingsPage.Overview ? SettingsPage.Home : SettingsPage.Overview);
        }

        private void LabelVisual_Click(object? sender, EventArgs e)
        {
            labelVisual.Visible = false;
            VisualControl.forceVisual = true;
        }

        public void InitVisual()
        {

            if (AppConfig.Is("hide_visual")) return;

            if (AppConfig.IsOLED())
            {
                visualAvailable = true;
                ApplyFeatureVisibility();
                sliderGamma.Visible = true;
                labelGammaTitle.Text = Properties.Strings.FlickerFreeDimming + " / " + Properties.Strings.VisualMode;

                VisualiseBrightness();

                sliderGamma.ValueChanged += SliderGamma_ValueChanged;
                sliderGamma.MouseUp += SliderGamma_ValueChanged;

            }
            else
            {
                labelGammaTitle.Text = Properties.Strings.VisualMode;
            }

            var gamuts = VisualControl.GetGamutModes();

            // Color profiles exist
            if (gamuts.Count > 0)
            {
                tableVisual.ColumnCount = 3;
                buttonInstallColor.Visible = false;
            }
            else
            {
                // If it's possible to retrieve color profiles
                if (ColorProfileHelper.ProfileExists())
                {
                    tableVisual.ColumnCount = 2;

                    buttonInstallColor.Text = Properties.Strings.DownloadColorProfiles;
                    buttonInstallColor.Visible = true;
                    buttonInstallColor.Click += ButtonInstallColorProfile_Click;

                    visualAvailable = true;
                    ApplyFeatureVisibility();
                    tableVisual.Visible = true;
                }

                return;
            }

            visualAvailable = true;
            ApplyFeatureVisibility();
            tableVisual.Visible = true;

            var visualValue = (SplendidCommand)AppConfig.Get("visual", (int)VisualControl.GetDefaultVisualMode());
            var colorTempValue = AppConfig.Get("color_temp", VisualControl.DefaultColorTemp);

            comboVisual.DropDownStyle = ComboBoxStyle.DropDownList;
            comboVisual.DataSource = new BindingSource(VisualControl.GetVisualModes(), null);
            comboVisual.DisplayMember = "Value";
            comboVisual.ValueMember = "Key";
            comboVisual.SelectedValue = visualValue;

            comboColorTemp.DropDownStyle = ComboBoxStyle.DropDownList;
            comboColorTemp.DataSource = new BindingSource(VisualControl.GetTemperatures(), null);
            comboColorTemp.DisplayMember = "Value";
            comboColorTemp.ValueMember = "Key";
            comboColorTemp.SelectedValue = colorTempValue;

            VisualControl.SetVisual(visualValue, colorTempValue, true);

            comboVisual.SelectedValueChanged += ComboVisual_SelectedValueChanged;
            comboVisual.Visible = true;
            VisualiseDisabled();

            comboColorTemp.SelectedValueChanged += ComboVisual_SelectedValueChanged;
            comboColorTemp.Visible = true;

            if (gamuts.Count <= 1) return;

            comboGamut.DropDownStyle = ComboBoxStyle.DropDownList;
            comboGamut.DataSource = new BindingSource(gamuts, null);
            comboGamut.DisplayMember = "Value";
            comboGamut.ValueMember = "Key";
            comboGamut.SelectedValue = (SplendidGamut)AppConfig.Get("gamut", (int)VisualControl.GetDefaultGamut());

            comboGamut.SelectedValueChanged += ComboGamut_SelectedValueChanged;
            comboGamut.Visible = true;

        }

        public void CycleVisualMode(int delta)
        {

            if (comboVisual.Items.Count < 1) return;

            if (delta > 0)
            {
                if (comboVisual.SelectedIndex < comboVisual.Items.Count - 1)
                    comboVisual.SelectedIndex += 1;
                else
                    comboVisual.SelectedIndex = 0;
            }
            else
            {
                if (comboVisual.SelectedIndex > 0)
                    comboVisual.SelectedIndex -= 1;
                else
                    comboVisual.SelectedIndex = comboVisual.Items.Count - 1;
            }

            Program.toast.RunToast(comboVisual.GetItemText(comboVisual.SelectedItem), ToastIcon.BrightnessUp);
        }

        private async void ButtonInstallColorProfile_Click(object? sender, EventArgs e)
        {
            await ColorProfileHelper.InstallProfile();
            InitVisual();
        }

        private void ComboGamut_SelectedValueChanged(object? sender, EventArgs e)
        {
            VisualControl.SetGamut((int)comboGamut.SelectedValue);
        }

        private void ComboVisual_SelectedValueChanged(object? sender, EventArgs e)
        {
            VisualControl.SetVisual((SplendidCommand)comboVisual.SelectedValue, (int)comboColorTemp.SelectedValue);
            VisualiseDisabled();
        }

        public void VisualiseBrightness()
        {
            if (InvokeRequired) { Invoke(VisualiseBrightness); return; }
            sliderGammaIgnore = true;
            sliderGamma.Value = VisualControl.GetBrightness();
            labelGamma.Text = sliderGamma.Value + "%";
            sliderGammaIgnore = false;
        }

        public void VisualiseAmdOled(bool status = false)
        {
            if (InvokeRequired) { Invoke(() => VisualiseAmdOled(status)); return; }
            buttonAmdOled.Visible = status;
            RefreshOverviewStatuses();
        }

        public void VisualiseArmoury(bool status = false)
        {
            if (InvokeRequired) { Invoke(() => VisualiseArmoury(status)); return; }
            buttonArmoury.Visible = status;
            RefreshOverviewStatuses();
        }

        public void VisualiseDisabled()
        {
            comboGamut.Enabled = comboColorTemp.Enabled = (SplendidCommand)AppConfig.Get("visual") != SplendidCommand.Disabled;
        }

        public void VisualiseGamut()
        {
            if (InvokeRequired) { Invoke(VisualiseGamut); return; }
            if (comboGamut.Items.Count > 0) comboGamut.SelectedIndex = 0;
        }

        private void SliderGamma_ValueChanged(object? sender, EventArgs e)
        {
            if (sliderGammaIgnore) return;
            VisualControl.SetBrightness(sliderGamma.Value);
        }

        private void ButtonOverlay_Click(object? sender, EventArgs e)
        {
            ToggleOverlay();
        }

        private void ButtonHandheld_Click(object? sender, EventArgs e)
        {
            if (!IsFormAlive(handheldForm))
            {
                handheldForm = new Handheld();
                RegisterOwnedForm(handheldForm);
            }

            ShowOrActivate(handheldForm);
        }

        private void ButtonFPS_Click(object? sender, EventArgs e)
        {
            allyControl.ToggleFPSLimit();
        }

        private void ButtonBacklight_Click(object? sender, EventArgs e)
        {
            allyControl.ToggleBacklight();
        }

        private void ButtonControllerMode_Click(object? sender, EventArgs e)
        {
            allyControl.ToggleMode();
        }

        public void VisualiseAlly(bool visible = false)
        {
            if (!visible) return;
            if (InvokeRequired) { Invoke(() => VisualiseAlly(visible)); return; }

            allyAvailable = true;
            ApplyFeatureVisibility();

            buttonOverlay.Text = Properties.Strings.Overlay;
            buttonOverlay.Activated = AppConfig.IsOverlay();
        }

        public void VisualiseController(ControllerMode mode)
        {
            switch (mode)
            {
                case ControllerMode.Gamepad:
                    buttonControllerMode.Text = "Gamepad";
                    break;
                case ControllerMode.Mouse:
                    buttonControllerMode.Text = "Mouse";
                    break;
                case ControllerMode.Skip:
                    buttonControllerMode.Text = "Skip";
                    break;
                default:
                    buttonControllerMode.Text = "Auto";
                    break;
            }
        }

        public void VisualiseBacklight(int backlight)
        {
            if (InvokeRequired) { Invoke(() => VisualiseBacklight(backlight)); return; }
            buttonBacklight.Text = Math.Round((double)backlight * 33.33).ToString() + "%";
        }

        public void VisualiseFPSLimit(int limit)
        {
            if (InvokeRequired) { Invoke(() => VisualiseFPSLimit(limit)); return; }
            buttonFPS.Text = "FPS Limit " + ((limit > 0 && limit <= 120) ? limit : "OFF");
        }

        public void VisualiseAutoTDP(bool status)
        {
            Logger.WriteLine($"Auto TDP: {status}");
            buttonAutoTDP.Activated = status;
        }

        private void SettingsForm_Focused(object? sender, EventArgs e)
        {
            if (activateCheck)
            {
                buttonAmdOled.Visible = AmdDisplay.IsOledPowerOptimization();
                RefreshOverviewStatuses();
                activateCheck = false;
            }
        }
        private void SettingsForm_LostFocus(object? sender, EventArgs e)
        {
            lastLostFocus = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private void ButtonBatteryFull_Click(object? sender, EventArgs e)
        {
            BatteryControl.ToggleBatteryLimitFull();
        }

        private void ButtonBatteryFull_MouseLeave(object? sender, EventArgs e)
        {
            batteryFullMouseOver = false;
            RefreshSensors(true);
        }

        private void ButtonBatteryFull_MouseEnter(object? sender, EventArgs e)
        {
            batteryFullMouseOver = true;
            labelCharge.Text = Properties.Strings.BatteryLimitFull;
        }

        private void SettingsForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Normal)
            {
                WindowState = FormWindowState.Normal;
                return;
            }

            Rectangle workArea = Screen.FromControl(this).WorkingArea;
            Left = workArea.Right - 10 - Width;
            Top = workArea.Bottom - 10 - Height;
        }

        public void PositionOwnedForm(Form form)
        {
            if (form is null || form.IsDisposed) return;

            Rectangle workArea = Screen.FromControl(this).WorkingArea;
            int left = Left - form.Width - 5;
            if (left < workArea.Left)
                left = Left + Width + 5;

            int top = form.Height > Height ? Top + Height - form.Height : Top;
            form.Left = Math.Clamp(left, workArea.Left, Math.Max(workArea.Left, workArea.Right - form.Width));
            form.Top = Math.Clamp(top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - form.Height));
        }

        public bool IsFormAlive(Form? form)
        {
            return form is not null && !form.IsDisposed && !form.Disposing;
        }

        private void ShowOrActivate(Form form)
        {
            if (!form.Visible)
            {
                PositionOwnedForm(form);
                form.Show();
            }

            form.Activate();
            form.BringToFront();
        }

        private void RegisterOwnedForm(Form form)
        {
            AddOwnedForm(form);
            form.FormClosed += OwnedForm_FormClosed;
        }

        private void OwnedForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (ReferenceEquals(sender, fansForm)) fansForm = null;
            if (ReferenceEquals(sender, extraForm)) extraForm = null;
            if (ReferenceEquals(sender, updatesForm)) updatesForm = null;
            if (ReferenceEquals(sender, matrixForm)) matrixForm = null;
            if (ReferenceEquals(sender, slashForm)) slashForm = null;
            if (ReferenceEquals(sender, handheldForm)) handheldForm = null;

            if (sender is RForm form)
            {
                IPeripheral? device = peripheralSettings.FirstOrDefault(pair => ReferenceEquals(pair.Value, form)).Key;
                if (device is not null) peripheralSettings.Remove(device);
            }
        }

        private void PanelBattery_MouseEnter(object? sender, EventArgs e)
        {
            batteryMouseOver = true;
            ShowBatteryWear();
        }

        private void PanelBattery_MouseLeave(object? sender, EventArgs e)
        {
            batteryMouseOver = false;
            RefreshSensors(true);
        }

        private void ShowBatteryWear()
        {
            //Refresh again only after 15 Minutes since the last refresh
            if (lastBatteryRefresh == 0 || Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastBatteryRefresh) > 15 * 60_000)
            {
                lastBatteryRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                HardwareControl.RefreshBatteryHealth();
            }

            if (HardwareControl.batteryHealth != -1)
            {
                labelCharge.Text = Properties.Strings.BatteryHealth + ": " + Math.Round(HardwareControl.batteryHealth, 1) + "%";
            }
        }

        private void SettingsForm_VisibleChanged(object? sender, EventArgs e)
        {
            sensorTimer.Enabled = Visible || sensorsAlways;
            if (Visible)
            {
                ResetToHome();
                Task.Run((Action)RefreshPeripheralsBattery);
                if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "autoupdate")
                    updateControl.CheckForUpdates();
            }
        }

        private void RefreshPeripheralsBattery()
        {
            PeripheralsProvider.RefreshBatteryForAllDevices(true);
        }

        private void ButtonUpdates_Click(object? sender, EventArgs e)
        {
            if (!IsFormAlive(updatesForm))
            {
                updatesForm = new Updates();
                RegisterOwnedForm(updatesForm);
            }

            ShowOrActivate(updatesForm);
        }

        public void VisualiseMatrixPicture(string image)
        {
            if (!IsFormAlive(matrixForm)) return;
            matrixForm.VisualiseMatrix(image);
        }

        protected override void WndProc(ref Message m)
        {

            if (m.Msg == NativeMethods.WM_POWERBROADCAST && m.WParam == (IntPtr)NativeMethods.PBT_APMSUSPEND)
            {
                Logger.WriteLine("System Suspend");
                GPUModeControl.suspended = true;
                Program.modeControl.SleepReset();
                m.Result = (IntPtr)1;
            }

            if (m.Msg == NativeMethods.WM_POWERBROADCAST && m.WParam == (IntPtr)NativeMethods.PBT_APMRESUMEAUTOMATIC)
            {
                Logger.WriteLine("System Resume");
                GPUModeControl.suspended = false;
                BatteryControl.AutoBattery();
                m.Result = (IntPtr)1;
            }

            if (m.Msg == NativeMethods.WM_POWERBROADCAST && m.WParam == (IntPtr)NativeMethods.PBT_POWERSETTINGCHANGE)
            {
                var settings = (NativeMethods.POWERBROADCAST_SETTING)m.GetLParam(typeof(NativeMethods.POWERBROADCAST_SETTING));
                if (settings.PowerSetting == NativeMethods.PowerSettingGuid.LIDSWITCH_STATE_CHANGE)
                {
                    switch (settings.Data)
                    {
                        case 0:
                            Logger.WriteLine("Lid Closed");
                            BatteryControl.AutoBattery();
                            InputDispatcher.lidClose = AniMatrixControl.lidClose = true;
                            Aura.ApplyBrightness(0, "Lid");
                            matrixControl.SetLidMode();
                            break;
                        case 1:
                            Logger.WriteLine("Lid Open");
                            InputDispatcher.InitFNLock();
                            InputDispatcher.lidClose = AniMatrixControl.lidClose = false;
                            Aura.ApplyBrightness(InputDispatcher.GetBacklight(), "Lid");
                            matrixControl.SetLidMode();
                            break;
                    }

                }
                else if (settings.PowerSetting == NativeMethods.PowerSettingGuid.EnergySaverStatus)
                {
                    Logger.WriteLine("Battery Saver: " + settings.Data);
                    buttonEnergySaver.Visible = settings.Data != 0;
                    RefreshOverviewStatuses();
                }
                else
                {
                    switch (settings.Data)
                    {
                        case 0:
                            Logger.WriteLine("Monitor Power Off");
                            Aura.SleepBrightness();
                            XGM.NotifyShutdown();
                            Program.hardwareOverlay?.SuspendForDisplayOff();
                            break;
                        case 1:
                            Logger.WriteLine("Monitor Power On");
                            GPUModeControl.suspended = false;
                            if (!Program.SetAutoModes(wakeup: true)) BatteryControl.AutoBattery();
                            Program.hardwareOverlay?.ResumeForDisplayOn();
                            break;
                        case 2:
                            Logger.WriteLine("Monitor Dimmed");
                            break;
                    }
                }
                m.Result = (IntPtr)1;
            }

            if (m.Msg == Program.WM_TASKBARCREATED)
            {
                Logger.WriteLine("Taskbar created, re-creating tray icon");
                if (Program.trayIcon is not null) Program.trayIcon.Visible = true;
            }

            try
            {
                base.WndProc(ref m);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void SetContextMenu()
        {
            var currentMode = Modes.GetCurrent();

            foreach (ToolStripItem item in contextMenuStrip.Items.Cast<ToolStripItem>().ToList())
            {
                if (item is ToolStripMenuItem menuItem) menuItem.Dispose();
            }
            contextMenuStrip.Items.Clear();
            contextMenuStrip.ShowCheckMargin = true;
            contextMenuStrip.ImageScalingSize = new Size(16, 16);
            contextMenuStrip.ShowImageMargin = false;
            Padding padding = new Padding(5, 5, 5, 5);

            var title = new ToolStripMenuItem(Properties.Strings.PerformanceMode);
            title.Margin = padding;
            title.Enabled = false;
            contextMenuStrip.Items.Add(title);

            foreach (var mode in Modes.GetDictonary())
            {
                var menuMode = new ToolStripMenuItem(mode.Value);
                menuMode.Tag = mode.Key;
                menuMode.Click += (sender, args) => { Program.modeControl.SetPerformanceMode(mode.Key); };
                menuMode.Margin = padding;
                menuMode.Checked = (mode.Key == currentMode);
                contextMenuStrip.Items.Add(menuMode);
            }

            contextMenuStrip.Items.Add("-");

            if (isGpuSection)
            {
                var titleGPU = new ToolStripMenuItem(Properties.Strings.GPUMode);
                titleGPU.Margin = padding;
                titleGPU.Enabled = false;
                contextMenuStrip.Items.Add(titleGPU);

                menuEco = new ToolStripMenuItem(Properties.Strings.EcoMode);
                menuEco.Click += ButtonEco_Click;
                menuEco.Margin = padding;
                menuEco.Checked = buttonEco.Activated;
                contextMenuStrip.Items.Add(menuEco);

                menuStandard = new ToolStripMenuItem(Properties.Strings.StandardMode);
                menuStandard.Click += ButtonStandard_Click;
                menuStandard.Margin = padding;
                menuStandard.Checked = buttonStandard.Activated;
                contextMenuStrip.Items.Add(menuStandard);

                menuUltimate = new ToolStripMenuItem(Properties.Strings.UltimateMode);
                menuUltimate.Click += ButtonUltimate_Click;
                menuUltimate.Margin = padding;
                menuUltimate.Checked = buttonUltimate.Activated;
                menuUltimate.Visible = isMuxGpu;
                contextMenuStrip.Items.Add(menuUltimate);

                menuOptimized = new ToolStripMenuItem(Properties.Strings.Optimized);
                menuOptimized.Click += ButtonOptimized_Click;
                menuOptimized.Margin = padding;
                menuOptimized.Checked = buttonOptimized.Activated;
                contextMenuStrip.Items.Add(menuOptimized);

                contextMenuStrip.Items.Add("-");
            }

            var bwIcon = new ToolStripMenuItem(Properties.Strings.BWTrayIcon);
            bwIcon.Margin = padding;
            bwIcon.Checked = AppConfig.IsBWIcon();
            bwIcon.Click += (sender, args) =>
            {
                bwIcon.Checked = !bwIcon.Checked;
                AppConfig.Set("bw_icon", bwIcon.Checked ? 1 : 0);
                VisualiseIcon();
            };
            contextMenuStrip.Items.Add(bwIcon);

            contextMenuStrip.Items.Add("-");

            var menuOverlay = new ToolStripMenuItem(Properties.Strings.Overlay);
            menuOverlay.Click += (sender, args) => ToggleOverlay();
            menuOverlay.Margin = padding;
            menuOverlay.Checked = AppConfig.IsOverlay();
            contextMenuStrip.Items.Add(menuOverlay);

            var menuOverlayGameOnly = new ToolStripMenuItem(Properties.Strings.OverlayOnlyInGames);
            menuOverlayGameOnly.Click += (sender, args) => ToggleOverlayGameOnly();
            menuOverlayGameOnly.Margin = padding;
            menuOverlayGameOnly.Checked = AppConfig.IsOverlayGameOnly();
            menuOverlayGameOnly.Enabled = AppConfig.IsOverlay();
            contextMenuStrip.Items.Add(menuOverlayGameOnly);

            var quit = new ToolStripMenuItem(Properties.Strings.Quit);
            quit.Click += ButtonQuit_Click;
            quit.Margin = padding;
            contextMenuStrip.Items.Add(quit);

            //contextMenuStrip.ShowCheckMargin = true;
            contextMenuStrip.Renderer = new CustomMenuRenderer();

            InitContextMenuTheme();

            if (Program.trayIcon is not null) Program.trayIcon.ContextMenuStrip = contextMenuStrip;


        }

        public void InitContextMenuTheme()
        {
            if (contextMenuStrip is not null)
            {
                contextMenuStrip.BackColor = this.BackColor;
                contextMenuStrip.ForeColor = this.ForeColor;
            }
        }

        private void ButtonXGM_Click(object? sender, EventArgs e)
        {
            gpuControl.ToggleXGM();
        }


        public void SetVersionLabel(string label, bool update = false)
        {
            if (InvokeRequired)
                Invoke(delegate
                {
                    labelVersion.Text = label;
                    if (update) labelVersion.ForeColor = colorTurbo;
                });
            else
            {
                labelVersion.Text = label;
                if (update) labelVersion.ForeColor = colorTurbo;
            }
        }


        private void LabelVersion_Click(object? sender, EventArgs e)
        {
            updateControl.CheckForUpdates();
        }


        private static void OnTimedEvent(Object? source, ElapsedEventArgs? e)
        {
            Program.settingsForm.RefreshSensors();
        }

        private void ButtonFHD_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = "Switch to " + ((buttonFHD.Text == "FHD") ? "UHD" : "FHD") + " Mode";
        }

        private void Button120Hz_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.MaxRefreshTooltip;
        }

        private void Button60Hz_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.MinRefreshTooltip.Replace("60", ScreenControl.MIN_RATE.ToString());
        }

        private void ButtonScreen_MouseLeave(object? sender, EventArgs e)
        {
            labelTipScreen.Text = "";
        }

        private void ButtonScreenAuto_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.AutoRefreshTooltip.Replace("60", ScreenControl.MIN_RATE.ToString());
        }

        private void ButtonUltimate_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.UltimateGPUTooltip;
        }

        private void ButtonStandard_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.StandardGPUTooltip;
        }

        private void ButtonEco_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.EcoGPUTooltip;
        }

        private void ButtonOptimized_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.OptimizedGPUTooltip;
        }

        private void ButtonGPU_MouseLeave(object? sender, EventArgs e)
        {
            labelTipGPU.Text = "";
        }

        private void ButtonXGM_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is null) return;
            TableLayoutPanel table = (TableLayoutPanel)sender;

            if (!buttonXGM.Visible) return;

            labelTipGPU.Text = buttonXGM.Bounds.Contains(table.PointToClient(Cursor.Position)) ?
                "XGMobile toggle works only in Standard mode" : "";

        }


        private void ButtonScreenAuto_Click(object? sender, EventArgs e)
        {
            ScreenControl.SetAutoRefresh(1);
            ScreenControl.AutoScreen();
        }


        private void CheckStartup_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is null) return;
            CheckBox chk = (CheckBox)sender;

            if (chk.Checked)
                Startup.Schedule();
            else
                Startup.UnSchedule();
        }

        private void ButtonMatrix_Click(object? sender, EventArgs e)
        {
            if (matrixControl.IsSlash)
            {
                if (!IsFormAlive(slashForm))
                {
                    slashForm = new Slash();
                    RegisterOwnedForm(slashForm);
                }

                ShowOrActivate(slashForm);
                return;
            }

            if (!IsFormAlive(matrixForm))
            {
                matrixForm = new Matrix();
                RegisterOwnedForm(matrixForm);
            }

            ShowOrActivate(matrixForm);
        }

        public void VisualiseMatrixRunning(int mode)
        {
            if (InvokeRequired) { Invoke(() => VisualiseMatrixRunning(mode)); return; }
            comboMatrixRunning.SelectedIndex = mode;
            if (comboMatrix.SelectedIndex == 0) comboMatrix.SelectedIndex = 3;
        }

        public void SetMatrixRunning(int mode)
        {
            VisualiseMatrixRunning(mode);
            AppConfig.Set("matrix_running", mode);
            matrixControl.SetDevice();
            if (!matrixControl.IsSlash && IsFormAlive(matrixForm)) matrixForm.VisualiseMode();
        }

        private void ComboMatrixRunning_SelectedValueChanged(object? sender, EventArgs e)
        {
            SetMatrixRunning(comboMatrixRunning.SelectedIndex);
            if (!matrixControl.IsSlash && comboMatrixRunning.SelectedIndex == (int)MatrixMode.Text && (!IsFormAlive(matrixForm) || !matrixForm.Visible)) ButtonMatrix_Click(sender, e);
        }


        private void ComboMatrix_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set("matrix_brightness", comboMatrix.SelectedIndex);
            matrixControl.SetDevice();
        }


        private void LabelCPUFan_Click(object? sender, EventArgs e)
        {
            FanSensorControl.fanRpm = !FanSensorControl.fanRpm;
            RefreshSensors(true);
        }

        private void ButtonKeyboardColor2_Click(object? sender, EventArgs e)
        {
            SetColorPicker("aura_color2", Aura.Color2);
        }

        private Extra GetExtraForm()
        {
            if (!IsFormAlive(extraForm))
            {
                extraForm = new Extra();
                RegisterOwnedForm(extraForm);
            }

            return extraForm;
        }

        public void ServicesToggle()
        {
            Extra form = GetExtraForm();
            ShowOrActivate(form);
            form.ServiesToggle();
        }

        private void ButtonKeyboard_Click(object? sender, EventArgs e)
        {
            ShowOrActivate(GetExtraForm());
        }

        public void FansInit()
        {
            if (!IsFormAlive(fansForm)) return;
            Invoke(fansForm.InitAll);
        }

        public void GPUInit()
        {
            if (!IsFormAlive(fansForm)) return;
            Invoke(fansForm.InitGPU);
        }

        public void FansToggle(int index = 0)
        {
            if (!IsFormAlive(fansForm))
            {
                fansForm = new Fans();
                RegisterOwnedForm(fansForm);
            }

            ShowOrActivate(fansForm);
            fansForm.ToggleNavigation(index);
        }

        private void ButtonFans_Click(object? sender, EventArgs e)
        {
            FansToggle();
        }

        private void SetColorPicker(string colorField, Color initial)
        {
            RColorPicker colorDlg = new RColorPicker(initial, colorField == "aura_color" && Aura.HasRandomColor());
            colorDlg.ColorChanged += c =>
            {
                AppConfig.Set(colorField, c.ToArgb());
                SetAura();
            };
            colorDlg.ShowDialog(this);
        }

        private void ButtonKeyboardColor_Click(object? sender, EventArgs e)
        {
            SetColorPicker("aura_color", Aura.Color1);
        }

        private void ButtonRearColor_Click(object? sender, EventArgs e)
        {
            SetColorPicker("rear_color", Aura.RearColor);
        }

        private void ComboRearLight_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set("rear_mode", (int)comboRearLight.SelectedValue);
            SetAura();
        }

        public void InitRearLight()
        {
            if (!AppConfig.HasRearLight())
                return;

            Aura.RearMode = (AuraMode)AppConfig.Get("rear_mode");
            Aura.SetRearColor(AppConfig.Get("rear_color"));

            comboRearLight.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRearLight.DataSource = new BindingSource(Aura.GetRearModes(), null);
            comboRearLight.DisplayMember = "Value";
            comboRearLight.ValueMember = "Key";
            comboRearLight.SelectedValue = Aura.RearMode;
            comboRearLight.SelectedValueChanged += ComboRearLight_SelectedValueChanged;

            buttonRearColor.Click += ButtonRearColor_Click;

            buttonRearColor.SwatchColor = Aura.RearColor;
            rearLightAvailable = true;
            ApplyFeatureVisibility();
        }

        public void InitAura()
        {
            comboKeyboard.DropDownStyle = ComboBoxStyle.DropDownList;
            if (!Aura.IsBacklightDetected)
                Aura.Init();

            Aura.Mode = (AuraMode)AppConfig.Get("aura_mode");
            Aura.Speed = (AuraSpeed)AppConfig.Get("aura_speed");
            Aura.SetColor(AppConfig.Get("aura_color"));
            Aura.SetColor2(AppConfig.Get("aura_color2"));

            comboKeyboard.DataSource = new BindingSource(Aura.GetModes(), null);
            comboKeyboard.DisplayMember = "Value";
            comboKeyboard.ValueMember = "Key";
            comboKeyboard.SelectedValue = Aura.Mode;
            comboKeyboard.SelectedValueChanged += ComboKeyboard_SelectedValueChanged;


            if (Aura.isWhite)
            {
                buttonKeyboardColor.Visible = false;
            }

            if (AppConfig.NoAura())
            {
                comboKeyboard.Visible = false;
            }

            VisualiseAura();

            InitRearLight();
        }

        public void SetAura()
        {
            Task.Run(() =>
            {
                Aura.ApplyAura();
                VisualiseAura();
            });
        }

        private void _VisualiseAura()
        {
            buttonKeyboardColor.SwatchColor = Aura.Color1;
            buttonKeyboardColor.SwatchColor2 = Aura.HasSecondColor() ? Aura.Color2 : (Color?)null;

            if (rearLightAvailable) buttonRearColor.SwatchColor = Aura.RearColor;

            bool dynamic = AppConfig.IsDynamicLighting() && DynamicLightingHelper.IsEnabled() && !AppConfig.IsDynamicLightingOnly();

            if (dynamic)
            {
                labelBacklight.Cursor = Cursors.Hand;
                labelBacklight.Text = Strings.DisableDynamicLighting;
            } else if (Aura.Mode == AuraMode.AMBIENT)
            {
                labelBacklight.Cursor = Cursors.Default;
                labelBacklight.Text = Strings.AmbientModeResources;
            } else
            {
                labelBacklight.Cursor = Cursors.Default;
                labelBacklight.Text = "";
            }
        }

        public void VisualiseAura()
        {
            if (InvokeRequired)
                Invoke(_VisualiseAura);
            else
                _VisualiseAura();
        }

        public void InitMatrix()
        {

            if (!matrixControl.IsValid)
            {
                matrixAvailable = false;
                ApplyFeatureVisibility();
                return;
            }

            if (matrixControl.IsSlash)
            {
                labelMatrix.Text = "Slash Lighting";
                pictureMatrix.BackgroundImage = ControlHelper.TintImage(Properties.Resources.slash_32, foreMain);
                comboMatrixRunning.Items.Clear();

                foreach (var item in SlashDevice.Modes)
                {
                    comboMatrixRunning.Items.Add(item.Value);
                }

                buttonMatrix.Text = "Slash";
            }

            comboMatrix.SelectedIndex = Math.Max(0, Math.Min(AppConfig.Get("matrix_brightness", 0), comboMatrix.Items.Count - 1));
            comboMatrixRunning.SelectedIndex = Math.Min(AppConfig.Get("matrix_running", 0), comboMatrixRunning.Items.Count - 1);
        }


        public void CycleMatrix(int delta)
        {
            comboMatrix.SelectedIndex = Math.Min(Math.Max(0, comboMatrix.SelectedIndex + delta), comboMatrix.Items.Count - 1);
            AppConfig.Set("matrix_brightness", comboMatrix.SelectedIndex);
            matrixControl.SetDevice();
            Program.toast.RunToast(comboMatrix.GetItemText(comboMatrix.SelectedItem), delta > 0 ? ToastIcon.BacklightUp : ToastIcon.BacklightDown);
        }


        public void CycleAuraMode(int delta)
        {
            if (delta > 0)
            {
                if (comboKeyboard.SelectedIndex < comboKeyboard.Items.Count - 1)
                    comboKeyboard.SelectedIndex += 1;
                else
                    comboKeyboard.SelectedIndex = 0;
            }
            else
            {
                if (comboKeyboard.SelectedIndex > 0)
                    comboKeyboard.SelectedIndex -= 1;
                else
                    comboKeyboard.SelectedIndex = comboKeyboard.Items.Count - 1;
            }

            Program.toast.RunToast(comboKeyboard.GetItemText(comboKeyboard.SelectedItem), ToastIcon.BacklightUp);
        }

        private void ComboKeyboard_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set("aura_mode", (int)comboKeyboard.SelectedValue);
            SetAura();
        }


        private void Button120Hz_Click(object? sender, EventArgs e)
        {
            ScreenControl.SetAutoRefresh(0);
            ScreenControl.SetScreen(ScreenControl.MAX_REFRESH, 1);
        }

        private void Button60Hz_Click(object? sender, EventArgs e)
        {
            ScreenControl.SetAutoRefresh(0);
            ScreenControl.SetScreen(ScreenControl.MIN_RATE, 0);
        }


        private void ButtonMiniled_Click(object? sender, EventArgs e)
        {
            ScreenControl.ToogleMiniled();
        }



        public void VisualiseScreen(bool screenEnabled, bool screenAuto, int frequency, int maxFrequency, int overdrive, bool overdriveSetting, int miniled1, int miniled2, bool hdr, bool acm, int fhd, int hdrControl)
        {
            bool advancedColor = hdr || acm;

            ButtonEnabled(button60Hz, screenEnabled);
            ButtonEnabled(button120Hz, screenEnabled);
            ButtonEnabled(buttonScreenAuto, screenEnabled);
            ButtonEnabled(buttonMiniled, screenEnabled);

            labelSreen.Text = screenEnabled
                ? Properties.Strings.LaptopScreen + ": " + frequency + "Hz" + ((overdrive == 1) ? " + " + Properties.Strings.Overdrive : "")
                : Properties.Strings.LaptopScreen + ": " + Properties.Strings.TurnedOff;

            panelScreen.AccessibleName = labelSreen.Text;

            button60Hz.Activated = false;
            button120Hz.Activated = false;
            buttonScreenAuto.Activated = false;

            if (screenAuto)
            {
                buttonScreenAuto.Activated = true;
            }
            else if (frequency == ScreenControl.MIN_RATE)
            {
                button60Hz.Activated = true;
            }
            else if (frequency > ScreenControl.MIN_RATE)
            {
                button120Hz.Activated = true;
            }

            button60Hz.Text = ScreenControl.MIN_RATE + "Hz";

            if (maxFrequency > ScreenControl.MIN_RATE)
            {
                button120Hz.Text = maxFrequency.ToString() + "Hz" + (overdriveSetting ? " + OD" : "");
                screenAvailable = true;
                ApplyFeatureVisibility();
                tableScreen.Visible = true;
            }
            else if (maxFrequency > 0)
            {
                tableScreen.Visible = false;
                screenAvailable = AppConfig.NoGpu();
                ApplyFeatureVisibility();
            }

            if (fhd >= 0)
            {
                buttonFHD.Visible = true;
                buttonFHD.Text = fhd > 0 ? "FHD" : "UHD";
            }

            bool hdrControlVisible = (hdr && hdrControl >= 0);

            if (miniled1 >= 0)
            {
                buttonMiniled.Visible = !hdrControlVisible;
                buttonMiniled.Enabled = !hdr;
                buttonMiniled.Activated = miniled1 == 1 || hdr;
            }
            else if (miniled2 >= 0)
            {
                buttonMiniled.Visible = !hdrControlVisible;
                buttonMiniled.Enabled = !hdr;
                if (hdr) miniled2 = 1; // Show HDR as Multizone Strong

                switch (miniled2)
                {
                    // Multizone On
                    case 0:
                        buttonMiniled.Text = Properties.Strings.Multizone;
                        buttonMiniled.BorderColor = colorStandard;
                        buttonMiniled.Activated = true;
                        break;
                    // Multizone Strong
                    case 1:
                        buttonMiniled.Text = Properties.Strings.MultizoneStrong;
                        buttonMiniled.BorderColor = colorTurbo;
                        buttonMiniled.Activated = true;
                        break;
                    // Multizone Off
                    case 2:
                        buttonMiniled.Text = Properties.Strings.OneZone;
                        buttonMiniled.BorderColor = colorStandard;
                        buttonMiniled.Activated = false;
                        break;
                }
            }
            else
            {
                buttonMiniled.Visible = false;
            }

            if (hdrControlVisible)
            {
                buttonHDRControl.Visible = true;
                buttonHDRControl.Activated = hdrControl > 0;
                buttonHDRControl.BorderColor = colorTurbo;
            } else
            {
                buttonHDRControl.Visible = false;
            }

            if (advancedColor) labelVisual.Text = Properties.Strings.VisualModesHDR;
            if (!screenEnabled) labelVisual.Text = Properties.Strings.VisualModesScreen;

            if (!screenEnabled || advancedColor)
            {
                labelVisual.Location = tableVisual.Location;
                labelVisual.Width = tableVisual.Width;
                labelVisual.Height = tableVisual.Height;
                labelVisual.Visible = true;
            }
            else
            {
                labelVisual.Visible = false;
            }


        }

        private void ButtonQuit_Click(object? sender, EventArgs e)
        {
            AsusLampArray.Release();
            matrixControl.Dispose();
            Close();
            Program.trayIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// Closes all forms except the settings. Hides the settings
        /// </summary>
        public void HideAll()
        {
            foreach (Form form in OwnedForms.ToArray())
                if (IsFormAlive(form)) form.Close();

            Hide();
            MemoryHelper.TrimAfter();
        }

        /// <summary>
        /// Brings all visible windows to the top, with settings being the focus
        /// </summary>
        public void ShowAll()
        {
            this.Activate();
            this.TopMost = true;
            this.TopMost = AppConfig.Is("topmost");
        }

        /// <summary>
        /// Check if any of fans, keyboard, update, or itself has focus
        /// </summary>
        /// <returns>Focus state</returns>
        public bool HasAnyFocus(bool lostFocusCheck = false)
        {
            return OwnedForms.Any(form => IsFormAlive(form) && form.ContainsFocus) ||
                   ContainsFocus ||
                   (lostFocusCheck && Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastLostFocus) < 300);
        }

        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                if (currentPage == SettingsPage.Home)
                    HideAll();
                else
                    ButtonBack_Click(this, EventArgs.Empty);
            }
        }

        private void ButtonUltimate_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(AsusACPI.GPUModeUltimate);
        }

        private void ButtonStandard_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(AsusACPI.GPUModeStandard);
        }

        private void ButtonEco_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(AsusACPI.GPUModeEco);
        }


        private void ButtonOptimized_Click(object? sender, EventArgs e)
        {
            AppConfig.Set("gpu_auto", (AppConfig.Get("gpu_auto") == 1) ? 0 : 1);
            VisualiseGPUMode();
            gpuControl.AutoGPUMode(true);
        }

        private void ButtonStopGPU_Click(object? sender, EventArgs e)
        {
            gpuControl.KillGPUApps();
        }

        public async void RefreshSensors(bool force = false)
        {
            int throttle = (!Visible && sensorsAlways) ? 6000 : 2000;
            if (!force && Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastRefresh) < throttle) return;
            lastRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            string cpuTemp = "";
            string gpuTemp = "";

            string cpuFan = "";
            string gpuFan = "";
            string midFan = "";

            string battery = "";
            string charge = "";

            await Task.Run(() => HardwareControl.ReadSensors());
            if (Visible) _ = Task.Run((Action)PeripheralsProvider.RefreshBatteryForAllDevices);

            if (HardwareControl.cpuTemp > 0)
                cpuTemp = ": " + TempHelper.FormatTemp((double)HardwareControl.cpuTemp);

            if (HardwareControl.batteryCapacity > 0)
            {
                charge = Properties.Strings.BatteryCharge + ": " + HardwareControl.batteryCharge;
            }

            if (HardwareControl.batteryRate < 0)
                battery = Properties.Strings.Discharging + ": " + Math.Round(-(decimal)HardwareControl.batteryRate, 1).ToString() + "W";
            else if (HardwareControl.batteryRate > 0)
                battery = Properties.Strings.Charging + ": " + Math.Round((decimal)HardwareControl.batteryRate, 1).ToString() + "W";


            if (HardwareControl.gpuTemp > 0)
            {
                gpuTemp = ": " + TempHelper.FormatTemp((double)HardwareControl.gpuTemp);
            }

            if (HardwareControl.cpuFan is not null) cpuFan = Strings.FanSpeed + ": " + HardwareControl.cpuFan;
            if (HardwareControl.gpuFan is not null) gpuFan = Strings.FanSpeed + ": " + HardwareControl.gpuFan;
            if (HardwareControl.midFan is not null) midFan = Strings.FanSpeed + ": " + HardwareControl.midFan;

            string trayTip = "CPU" + cpuTemp + " " + cpuFan;
            if (gpuTemp.Length > 0) trayTip += "\nGPU" + gpuTemp + " " + gpuFan;
            if (battery.Length > 0) trayTip += "\n" + battery;
            
            if (Program.settingsForm.IsHandleCreated)
                Program.settingsForm.BeginInvoke(delegate
                {
                    labelCPUFan.Text = "CPU" + cpuTemp + "  " + cpuFan;
                    labelGPUFan.Text = "GPU" + gpuTemp + "  " + gpuFan;

                    if (HardwareControl.gpuFan is not null && AppConfig.NoGpu())
                        labelMidFan.Text = "GPU" + gpuTemp + " " + gpuFan;

                    if (HardwareControl.midFan is not null) 
                        labelMidFan.Text = "Mid " + midFan;
                    
                    labelBattery.Text = battery;
                    if (!batteryMouseOver && !batteryFullMouseOver) labelCharge.Text = charge;
                });

            if (Program.trayIcon is not null) Program.trayIcon.Text = trayTip;
        }

        public void LabelFansResult(string text)
        {
            if (IsFormAlive(fansForm))
                fansForm.LabelFansResult(text);
        }

        public void ToggleOverlay(bool fromHotkey = false)
        {
            bool enable = !AppConfig.IsOverlay();
            AppConfig.Set("overlay", enable ? 1 : 0);
            Logger.WriteLine("Overlay " + (enable ? "On" : "Off") + (AppConfig.IsOverlayGameOnly() ? " (game only)" : ""));
            if (enable)
                Program.hardwareOverlay?.StartOverlay();
            else
                Program.hardwareOverlay?.StopOverlay();

            buttonOverlay.Activated = enable;

            if (fromHotkey && AppConfig.IsOverlayGameOnly())
                Program.toast.RunToast(Properties.Strings.Overlay + " " + (enable ? Properties.Strings.On : Properties.Strings.Off));

            SetContextMenu();
        }

        public void ToggleOverlayGameOnly()
        {
            AppConfig.Set("overlay_game_only", AppConfig.IsOverlayGameOnly() ? 0 : 1);
            if (AppConfig.IsOverlay())
            {
                Program.hardwareOverlay?.StopOverlay();
                Program.hardwareOverlay?.StartOverlay();
            }
            SetContextMenu();
        }

        public void ShowMode(int mode)
        {
            if (InvokeRequired)
                Invoke(delegate
                {
                    VisualiseMode(mode);
                });
            else
                VisualiseMode(mode);
        }

        protected void VisualiseMode(int mode)
        {
            buttonSilent.Activated = false;
            buttonBalanced.Activated = false;
            buttonTurbo.Activated = false;
            buttonFans.Activated = false;

            switch (mode)
            {
                case AsusACPI.PerformanceSilent:
                    buttonSilent.Activated = true;
                    break;
                case AsusACPI.PerformanceTurbo:
                    buttonTurbo.Activated = true;
                    break;
                case AsusACPI.PerformanceBalanced:
                    buttonBalanced.Activated = true;
                    break;
                default:
                    buttonFans.Activated = true;
                    buttonFans.BorderColor = Modes.GetBase(mode) switch
                    {
                        AsusACPI.PerformanceSilent => colorEco,
                        AsusACPI.PerformanceTurbo => colorTurbo,
                        AsusACPI.PerformanceFullSpeed => Color.Orange,
                        _ => colorStandard,
                    };
                    break;
            }

            foreach (var item in contextMenuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Tag is not null)
                {
                    menuItem.Checked = ((int)menuItem.Tag == mode);
                }
            }
        }


        public void SetModeLabel(string modeText)
        {
            if (InvokeRequired)
            {
                Invoke(delegate
                {
                    labelPerf.Text = modeText;
                    panelPerformance.AccessibleName = labelPerf.Text;
                });
            }
            else
            {
                labelPerf.Text = modeText;
                panelPerformance.AccessibleName = labelPerf.Text;
            }

        }



        public void VisualizeXGM(int GPUMode = -1)
        {

            bool connected = Program.acpi.IsXGConnected();
            int activated = connected ? Program.acpi.DeviceGet(AsusACPI.GPUXG) : -1;
            Invoke(() => VisualizeXGM(connected, activated, GPUMode));
        }

        void VisualizeXGM(bool connected, int activated, int GPUMode)
        {
            buttonXGM.Enabled = buttonXGM.Visible = connected;

            if (!connected) return;

            if (GPUMode != -1)
                ButtonEnabled(buttonXGM, AppConfig.IsAMDiGPU() || GPUMode != AsusACPI.GPUModeEco);


            Logger.WriteLine("XGM Activated flag: " + activated);

            buttonXGM.Activated = activated == 1;

            if (activated == 1)
            {
                ButtonEnabled(buttonOptimized, false);
                ButtonEnabled(buttonEco, false);
                ButtonEnabled(buttonStandard, false);
                ButtonEnabled(buttonUltimate, false);
            }
            else
            {
                ButtonEnabled(buttonOptimized, true);
                ButtonEnabled(buttonEco, true);
                ButtonEnabled(buttonStandard, true);
                ButtonEnabled(buttonUltimate, true);
            }

        }

        public void VisualiseGPUButtons(bool eco = true, bool ultimate = true)
        {
            if (InvokeRequired) { Invoke(() => VisualiseGPUButtons(eco, ultimate)); return; }
            isMuxGpu = ultimate;

            if (!eco)
            {
                menuEco.Visible = buttonEco.Visible = false;
                menuOptimized.Visible = buttonOptimized.Visible = false;
                buttonStopGPU.Visible = true;
                tableGPU.ColumnCount = 3;
                tableScreen.ColumnCount = 3;
            }
            else
            {
                buttonStopGPU.Visible = false;
            }

            if (!ultimate)
            {
                menuUltimate.Visible = buttonUltimate.Visible = false;
                tableGPU.ColumnCount = 3;
                tableScreen.ColumnCount = 3;
            }
        }

        public void HideGPUModes(bool gpuExists)
        {
            if (InvokeRequired) { Invoke(() => HideGPUModes(gpuExists)); return; }

            isGpuSection = false;

            buttonEco.Visible = false;
            buttonStandard.Visible = false;
            buttonUltimate.Visible = false;
            buttonOptimized.Visible = false;
            buttonStopGPU.Visible = true;

            tableGPU.ColumnCount = 0;

            SetContextMenu();

            gpuAvailable = gpuExists;
            ApplyFeatureVisibility();

        }


        public void LockGPUModes(string text = null)
        {
            if (InvokeRequired) { Invoke(() => LockGPUModes(text)); return; }
            if (text is null) text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUChanging + " ...";

            ButtonEnabled(buttonOptimized, false);
            ButtonEnabled(buttonEco, false);
            ButtonEnabled(buttonStandard, false);
            ButtonEnabled(buttonUltimate, false);
            ButtonEnabled(buttonXGM, false);

            labelGPU.Text = text;
        }

        public void VisualiseGPUMode(int GPUMode = -1)
        {
            if (InvokeRequired) { Invoke(() => VisualiseGPUMode(GPUMode)); return; }

            if (toolTip.GetToolTip(pictureGPU) != (GPUModeControl.gpuError ?? ""))
            {
                pictureGPU.BackgroundImage = GPUModeControl.gpuError is null ? Properties.Resources.icons8_video_card_32 : SystemIcons.Warning.ToBitmap();
                pictureGPU.Cursor = GPUModeControl.gpuError is null ? Cursors.Default : Cursors.Hand;
                toolTip.SetToolTip(pictureGPU, GPUModeControl.gpuError);
            }

            if (AppConfig.IsAlly())
            {
                tableGPU.Visible = false;
                labelGPU.Text = "GPU";
                if (Program.acpi.IsXGConnected())
                {
                    tableAMD.Controls.Add(buttonXGM, 1, 0);
                    VisualizeXGM();
                }
                VisualiseIcon();
                return;
            }

            ButtonEnabled(buttonOptimized, true);
            ButtonEnabled(buttonEco, true);
            ButtonEnabled(buttonStandard, true);
            ButtonEnabled(buttonUltimate, true);

            if (GPUMode == -1)
                GPUMode = AppConfig.Get("gpu_mode");

            bool GPUAuto = AppConfig.Is("gpu_auto");

            buttonEco.Activated = false;
            buttonStandard.Activated = false;
            buttonUltimate.Activated = false;
            buttonOptimized.Activated = false;

            switch (GPUMode)
            {
                case AsusACPI.GPUModeEco:
                    buttonOptimized.BorderColor = colorEco;
                    buttonEco.Activated = !GPUAuto;
                    buttonOptimized.Activated = GPUAuto;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUModeEco;
                    panelGPU.AccessibleName = Properties.Strings.GPUMode + " - " + (GPUAuto ? Properties.Strings.Optimized : Properties.Strings.EcoMode);
                    break;
                case AsusACPI.GPUModeUltimate:
                    buttonUltimate.Activated = true;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUModeUltimate;
                    panelGPU.AccessibleName = Properties.Strings.GPUMode + " - " + Properties.Strings.UltimateMode;
                    break;
                default:
                    buttonOptimized.BorderColor = colorStandard;
                    buttonStandard.Activated = !GPUAuto;
                    buttonOptimized.Activated = GPUAuto;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + (AppConfig.IsAlwaysUltimate() ? Properties.Strings.GPUModeUltimate : Properties.Strings.GPUModeStandard);
                    panelGPU.AccessibleName = Properties.Strings.GPUMode + " - " + (GPUAuto ? Properties.Strings.Optimized : Properties.Strings.StandardMode);
                    break;
            }

            VisualiseIcon();
            VisualizeXGM(GPUMode);

            if (isGpuSection)
            {
                menuEco.Checked = buttonEco.Activated;
                menuStandard.Checked = buttonStandard.Activated;
                menuUltimate.Checked = buttonUltimate.Activated;
                menuOptimized.Checked = buttonOptimized.Activated;
            }

            // UI Fix for small screeens
            if (Top < 0)
            {
                labelTipGPU.Visible = false;
                labelTipScreen.Visible = false;
                Top = 5;
            }

        }


        private (int, bool, bool)? lastIcon;
        private bool isDark = CheckSystemDarkModeStatus();

        public void VisualiseIcon(bool themeChange = false)
        {
            if (Program.trayIcon is null) return;
            if (themeChange) isDark = CheckSystemDarkModeStatus();

            int GPUMode = AppConfig.Get("gpu_mode");
            bool bw = AppConfig.IsBWIcon();

            if (lastIcon == (GPUMode, isDark, bw)) return;
            lastIcon = (GPUMode, isDark, bw);

            Icon newIcon = GPUMode switch
            {
                AsusACPI.GPUModeEco => bw ? (isDark ? Properties.Resources.light_eco : Properties.Resources.dark_eco) : Properties.Resources.eco,
                AsusACPI.GPUModeUltimate => bw ? (isDark ? Properties.Resources.light_standard : Properties.Resources.dark_standard) : Properties.Resources.ultimate,
                _ => bw ? (isDark ? Properties.Resources.light_standard : Properties.Resources.dark_standard) : Properties.Resources.standard,
            };

            Icon? oldIcon = Program.trayIcon.Icon;
            Program.trayIcon.Icon = newIcon;
            oldIcon?.Dispose();
        }

        private void PictureGPU_Click(object? sender, EventArgs e)
        {
            if (GPUModeControl.gpuError is not null)
                Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });
        }

        private void ButtonSilent_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(AsusACPI.PerformanceSilent);
        }

        private void ButtonBalanced_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(AsusACPI.PerformanceBalanced);
        }

        private void ButtonTurbo_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(AsusACPI.PerformanceTurbo);
        }


        public void ButtonEnabled(RButton but, bool enabled)
        {
            but.Enabled = enabled;
            but.BackColor = but.Enabled ? Color.FromArgb(255, but.BackColor) : Color.FromArgb(100, but.BackColor);
        }

        public void VisualiseBatteryTitle(int limit)
        {
            labelBatteryTitle.Text = Properties.Strings.BatteryChargeLimit + ": " + limit.ToString() + "%";
        }

        public void VisualiseBattery(int limit)
        {
            if (InvokeRequired) { Invoke(() => VisualiseBattery(limit)); return; }
            VisualiseBatteryTitle(limit);
            sliderBattery.Value = limit;

            sliderBattery.AccessibleName = Properties.Strings.BatteryChargeLimit + ": " + limit.ToString() + "%";
            //sliderBattery.AccessibilityObject.Select(AccessibleSelection.TakeFocus);

            VisualiseBatteryFull();
        }

        public void VisualiseBatteryFull()
        {
            if (InvokeRequired) { Invoke(VisualiseBatteryFull); return; }
            if (BatteryControl.chargeFull)
            {
                buttonBatteryFull.BackColor = colorStandard;
                buttonBatteryFull.ForeColor = SystemColors.ControlLightLight;
                buttonBatteryFull.AccessibleName = Properties.Strings.BatteryChargeLimit + "100% on";
            }
            else
            {
                buttonBatteryFull.BackColor = buttonSecond;
                buttonBatteryFull.ForeColor = SystemColors.ControlDark;
                buttonBatteryFull.AccessibleName = Properties.Strings.BatteryChargeLimit + "100% off";
            }

        }


        public void UpdateKeyboardLabel()
        {
            labelKeyboard.Text = Properties.Strings.LaptopKeyboard + (PeripheralsProvider.IsAuraSync ? " +" : "");
        }

        public void VisualizePeripherals()
        {
            List<IPeripheral> lp = PeripheralsProvider.AllPeripherals();
            peripheralsAvailable = lp.Count > 0;

            if (!peripheralsAvailable)
            {
                ApplyFeatureVisibility();
                return;
            }

            Button[] buttons = new Button[] { buttonPeripheral1, buttonPeripheral2, buttonPeripheral3 };

            //we only support 4 devces for now. Who has more than 4 mice connected to the same PC anyways....

            for (int i = 0; i < lp.Count && i < buttons.Length; ++i)
            {
                IPeripheral m = lp.ElementAt(i);
                Button b = buttons[i];

                string id = m.GetDisplayName();
                bool ready = m.IsDeviceReady;
                bool hasBat = m.HasBattery();
                bool charging = ready && hasBat && m.Charging;
                int level = (ready && hasBat) ? Math.Min(5, (m.Battery + 10) / 20) : -1;
                bool showPercent = AppConfig.Is("mouse_battery") && ready && hasBat;
                int cacheBattery = showPercent ? m.Battery : -1;
                var state = (id, ready, charging, level, cacheBattery, b.ForeColor.ToArgb());

                if (b.Tag is ValueTuple<string, bool, bool, int, int, int> prev && prev.Equals(state) && b.Visible)
                    continue;

                b.Text = showPercent ? id + "\n" + m.Battery + "%" : id;

                Image? baseIcon = m.DeviceType() switch
                {
                    PeripheralType.Mouse => Properties.Resources.icons8_maus_48,
                    PeripheralType.Keyboard => Properties.Resources.icons8_keyboard_48,
                    _ => null,
                };

                if (baseIcon is not null)
                {
                    int ih = baseIcon.Height;
                    // icon PNG may be wider than tall (baked-in right text padding); badge/bars anchor to the glyph square
                    int iw = Math.Min(baseIcon.Width, ih);
                    Image composed = ControlHelper.TintImage(baseIcon, b.ForeColor);
                    if (!ready)
                    {
                        composed = ControlHelper.OverlayBadge(composed, Properties.Resources.icons8_cancel_48, RForm.colorTurbo, iconWidth: iw, iconHeight: ih);
                    }
                    else if (hasBat)
                    {
                        if (charging)
                            composed = ControlHelper.OverlayBadge(composed, Properties.Resources.icons8_flash_48, RForm.colorEco, iconWidth: iw, iconHeight: ih);

                        Color barColor = level <= 1 ? colorTurbo
                                       : level <= 3 ? colorStandard
                                       : colorEco;
                        composed = ControlHelper.OverlayChargeBars(composed, level, 5, barColor, iconWidth: iw, iconHeight: ih);
                    }

                    b.Image = ControlHelper.ResizeImage(composed, ControlHelper.Scale);
                }

                b.Tag = state;
                b.Visible = true;
            }

            for (int i = lp.Count; i < buttons.Length; ++i)
            {
                buttons[i].Visible = false;
            }

            ApplyFeatureVisibility();
        }

        private void ButtonPeripheral_MouseEnter(object? sender, EventArgs e)
        {
            int index = 0;
            if (sender == buttonPeripheral2) index = 1;
            if (sender == buttonPeripheral3) index = 2;
            IPeripheral iph = PeripheralsProvider.AllPeripherals().ElementAt(index);


            if (iph is null)
            {
                return;
            }

            if (!iph.IsDeviceReady)
            {
                //Refresh battery on hover if the device is marked as "Not Ready"
                iph.ReadBattery();
            }
        }

        private void ButtonPeripheral_Click(object? sender, EventArgs e)
        {
            int index = 0;
            if (sender == buttonPeripheral2) index = 1;
            if (sender == buttonPeripheral3) index = 2;

            IPeripheral? peripheral = PeripheralsProvider.AllPeripherals().ElementAtOrDefault(index);
            if (peripheral is null || !peripheral.IsDeviceReady) return;

            if (peripheralSettings.TryGetValue(peripheral, out RForm? existing) && IsFormAlive(existing))
            {
                ShowOrActivate(existing);
                return;
            }

            RForm? form = peripheral switch
            {
                AsusMouse mouse => new AsusMouseSettings(mouse),
                AsusKeyboard keyboard => CreateKeyboardSettings(keyboard),
                _ => null,
            };
            if (form is null) return;

            peripheralSettings[peripheral] = form;
            RegisterOwnedForm(form);
            form.TopMost = AppConfig.Is("topmost");
            ShowOrActivate(form);
        }

        private AsusKeyboardSettings CreateKeyboardSettings(AsusKeyboard keyboard)
        {
            AsusKeyboardSettings.RequestReopen = ShowKeyboardSettings;
            return new AsusKeyboardSettings(keyboard);
        }

        private void ShowKeyboardSettings(AsusKeyboard keyboard)
        {
            if (peripheralSettings.TryGetValue(keyboard, out RForm? existing) && IsFormAlive(existing))
            {
                ShowOrActivate(existing);
                return;
            }

            AsusKeyboardSettings form = CreateKeyboardSettings(keyboard);
            peripheralSettings[keyboard] = form;
            RegisterOwnedForm(form);
            form.TopMost = AppConfig.Is("topmost");
            ShowOrActivate(form);
        }

        public void VisualiseAudio(double level)
        {
            if (InvokeRequired) { Invoke(() => VisualiseAudio(level)); return; }
            int filledSquares = (int)Math.Round(level/2);
            string squares = new string('|', filledSquares);
            labelMatrix.Text = $"Slash Lighting: {squares}";
        }

        public void VisualiseFnLock()
        {

            if (AppConfig.Is("fn_lock"))
            {
                buttonFnLock.BackColor = colorStandard;
                buttonFnLock.ForeColor = SystemColors.ControlLightLight;
                buttonFnLock.AccessibleName = "Fn-Lock on";
            }
            else
            {
                buttonFnLock.BackColor = buttonSecond;
                buttonFnLock.ForeColor = SystemColors.ControlDark;
                buttonFnLock.AccessibleName = "Fn-Lock off";
            }
        }


        private void ButtonFnLock_Click(object? sender, EventArgs e)
        {
            InputDispatcher.ToggleFnLock();
        }

    }


}

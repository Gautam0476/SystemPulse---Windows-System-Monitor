using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using CustomTaskManager.Controls;
using CustomTaskManager.Models;
using CustomTaskManager.Services;
using System.Linq;

namespace CustomTaskManager;

public sealed class MainForm : Form
{
    private static Color WindowBack = Color.FromArgb(14, 17, 21);
    private static Color PanelBack = Color.FromArgb(23, 27, 33);
    private static Color SurfaceBack = Color.FromArgb(30, 36, 44);
    private static Color HeaderBack = Color.FromArgb(19, 23, 29);
    private static Color Border = Color.FromArgb(55, 66, 80);
    private static Color TextColor = Color.FromArgb(242, 245, 248);
    private static Color MutedText = Color.FromArgb(154, 164, 177);
    private static Color Accent = Color.FromArgb(0, 179, 167);
    private static Color AccentDark = Color.FromArgb(20, 126, 119);
    private static Color Danger = Color.FromArgb(224, 82, 82);
    private static Color GridAlternateBack = Color.FromArgb(27, 32, 39);
    private const int ThemeToggleWidth = 148;
    private const int ThemeToggleHeight = 48;
    private const double SmartAlertCpuThreshold = 85d;
    private const double SmartAlertMemoryThreshold = 85d;
    private const int SmartAlertRequiredSamples = 4;
    private static readonly TimeSpan SmartAlertCooldown = TimeSpan.FromMinutes(2);

    private readonly ProcessMonitor _processMonitor = new();
    private readonly StartupManager _startupManager = new();
    private readonly RuleStore _ruleStore = new();
    private readonly RuleEngine _ruleEngine = new();
    private readonly HistoryStore _historyStore = new();
    private readonly SystemPerformanceMonitor _performanceMonitor = new();
    private readonly ProcessAdvisor _processAdvisor = new();
    private readonly DashboardState _dashboardState = new();
    private readonly DashboardServer _dashboardServer;
    private readonly NotifyIcon _notifyIcon = new();

    private readonly BindingSource _processSource = new();
    private readonly BindingSource _startupSource = new();
    private readonly BindingSource _rulesSource = new();
    private readonly BindingSource _historySource = new();
    private readonly BindingList<AutomationRule> _rules;
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 3000 };
    private readonly System.Windows.Forms.Timer _themeAnimationTimer = new() { Interval = 15 };

    private List<ProcessSnapshot> _snapshots = [];
    private DateTime _lastHistoryWriteUtc = DateTime.MinValue;
    private bool _refreshingProcesses;
    private bool _isLightTheme;
    private float _themeToggleProgress;
    private float _themeToggleTargetProgress;
    private string _processTreeSignature = string.Empty;
    private int _highCpuSamples;
    private int _highMemorySamples;
    private DateTime _lastSmartAlertUtc = DateTime.MinValue;
    private Guid? _editingRuleId;

    private Label _summaryLabel = null!;
    private Label _advisorTitleLabel = null!;
    private Label _advisorDetailsLabel = null!;
    private TextBox _searchBox = null!;
    private Button _killButton = null!;
    private Button _openLocationButton = null!;
    private Button _themeButton = null!;
    private Button _selectAdvisorButton = null!;
    private TreeView _processTree = null!;
    private DataGridView _processGrid = null!;
    private Label _dashboardUrlLabel = null!;
    private Button _openDashboardButton = null!;
    private PerformanceGraph _cpuGraph = null!;
    private PerformanceGraph _memoryGraph = null!;
    private PerformanceGraph _gpuGraph = null!;
    private Label _performanceCpuLabel = null!;
    private Label _performanceMemoryLabel = null!;
    private Label _performanceGpuLabel = null!;
    private Label _performanceBatteryLabel = null!;
    private Label _performancePowerLabel = null!;
    private Label _performanceProcessesLabel = null!;
    private Label _performanceThreadsLabel = null!;
    private Label _performanceHandlesLabel = null!;
    private Label _performanceUptimeLabel = null!;
    private Label _performanceProcessorLabel = null!;
    private Label _performanceOsLabel = null!;
    private DataGridView _startupGrid = null!;
    private Button _startupToggleButton = null!;
    private DataGridView _rulesGrid = null!;
    private CheckBox _rulesActiveCheckBox = null!;
    private CheckBox _ruleEnabledCheckBox = null!;
    private TextBox _ruleProcessBox = null!;
    private ComboBox _ruleMetricCombo = null!;
    private NumericUpDown _ruleThresholdInput = null!;
    private ComboBox _ruleActionCombo = null!;
    private DataGridView _historyGrid = null!;
    private ComboBox _historyWindowCombo = null!;
    private SystemPieChart _visualizationPieChart = null!;
    private Label _visualizationScoreLabel = null!;
    private Label _visualizationStatusLabel = null!;
    private Label _visualizationCpuLabel = null!;
    private Label _visualizationGpuLabel = null!;
    private Label _visualizationMemoryLabel = null!;
    private Label _visualizationBatteryLabel = null!;
    private Label _visualizationProcessLabel = null!;
    private Label _visualizationRecommendationLabel = null!;
    private Label _visualizationUpdatedLabel = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public MainForm()
    {
        _dashboardServer = new DashboardServer(_dashboardState);
        _rules = _ruleStore.Load();

        BuildUi();
        ConfigureNotifications();
        WireEvents();

        _rulesSource.DataSource = _rules;
        _rulesGrid.DataSource = _rulesSource;

        RefreshStartupEntries();
        RefreshHistory();
        _refreshTimer.Start();
    }

    private void BuildUi()
    {
        SuspendLayout();

        Text = "Custom Task Manager";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1160, 720);
        Size = new Size(1360, 820);
        BackColor = WindowBack;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 9.25F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = WindowBack
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        root.Controls.Add(BuildHeader(), 0, 0);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.FlatButtons,
            BackColor = WindowBack,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(148, 38),
            Padding = new Point(14, 6),
            SizeMode = TabSizeMode.Fixed
        };
        tabs.DrawItem += DrawMainTab;
        tabs.SelectedIndexChanged += (_, _) => tabs.Invalidate();
        tabs.TabPages.Add(BuildProcessesTab());
        tabs.TabPages.Add(BuildPerformanceTab());
        tabs.TabPages.Add(BuildStartupTab());
        tabs.TabPages.Add(BuildRulesTab());
        tabs.TabPages.Add(BuildHistoryTab());
        tabs.TabPages.Add(BuildVisualizationTab());
        root.Controls.Add(tabs, 0, 1);

        var statusStrip = new StatusStrip
        {
            BackColor = HeaderBack,
            ForeColor = MutedText,
            SizingGrip = false
        };
        _statusLabel = new ToolStripStatusLabel("Ready") { ForeColor = MutedText };
        statusStrip.Items.Add(_statusLabel);
        root.Controls.Add(statusStrip, 0, 2);

        Controls.Add(root);
        ResumeLayout();
    }

    private void DrawMainTab(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tabs)
        {
            return;
        }

        var selected = e.Index == tabs.SelectedIndex;
        var bounds = tabs.GetTabRect(e.Index);
        bounds.Inflate(-2, -2);

        using var background = new SolidBrush(selected ? SurfaceBack : HeaderBack);
        e.Graphics.FillRectangle(background, bounds);

        if (selected)
        {
            using var accentBrush = new SolidBrush(Accent);
            e.Graphics.FillRectangle(accentBrush, bounds.Left, bounds.Bottom - 3, bounds.Width, 3);
        }

        using var borderPen = new Pen(selected ? Border : Color.FromArgb(38, Border));
        e.Graphics.DrawRectangle(borderPen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);

        using var tabFont = new Font(Font.FontFamily, 9F, selected ? FontStyle.Bold : FontStyle.Regular);
        TextRenderer.DrawText(
            e.Graphics,
            tabs.TabPages[e.Index].Text,
            tabFont,
            bounds,
            selected ? TextColor : MutedText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(16, 10, 16, 10),
            BackColor = HeaderBack
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "Custom Task Manager",
            Dock = DockStyle.Fill,
            ForeColor = TextColor,
            Font = new Font(Font.FontFamily, 17F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(title, 0, 0);

        _summaryLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font(Font.FontFamily, 9F)
        };
        header.Controls.Add(_summaryLabel, 1, 0);
        header.SetColumnSpan(_summaryLabel, 2);

        _searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Search process name, PID, or path",
            BackColor = SurfaceBack,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 8, 10, 0),
            Font = new Font(Font.FontFamily, 9.5F)
        };
        header.Controls.Add(_searchBox, 0, 1);
        header.SetColumnSpan(_searchBox, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = HeaderBack,
            Padding = new Padding(0, 10, 0, 8),
            Margin = new Padding(0)
        };

        _killButton = CreateButton("Kill", Danger);
        _openLocationButton = CreateButton("Open file", Accent);
        _themeButton = CreateThemeButton();
        var refreshButton = CreateButton("Refresh", Accent);
        refreshButton.Click += async (_, _) => await RefreshProcessesAsync(manual: true);

        actions.Controls.Add(_killButton);
        actions.Controls.Add(_openLocationButton);
        actions.Controls.Add(refreshButton);
        actions.Controls.Add(_themeButton);
        header.Controls.Add(actions, 2, 1);

        return header;
    }

    private TabPage BuildProcessesTab()
    {
        var page = CreateTabPage("Processes");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = WindowBack,
            Padding = new Padding(2)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BackColor = Border,
            SplitterWidth = 6
        };
        split.HandleCreated += (_, _) => BeginInvoke(() => ConfigureProcessSplitter(split));
        split.SizeChanged += (_, _) => ConfigureProcessSplitter(split);

        _processTree = new TreeView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SurfaceBack,
            ForeColor = TextColor,
            HideSelection = false,
            FullRowSelect = true,
            HotTracking = true,
            Indent = 18,
            ItemHeight = 27,
            LineColor = Border,
            Font = new Font("Segoe UI", 9.25F)
        };
        split.Panel1.Controls.Add(_processTree);

        _processGrid = CreateGrid();
        AddTextColumns(_processGrid,
            new(nameof(ProcessSnapshot.Name), "Process", 190),
            new(nameof(ProcessSnapshot.Id), "PID", 70),
            new(nameof(ProcessSnapshot.ParentText), "Parent", 70),
            new(nameof(ProcessSnapshot.CpuPercent), "CPU %", 80, "N1"),
            new(nameof(ProcessSnapshot.MemoryMb), "RAM MB", 90, "N1"),
            new(nameof(ProcessSnapshot.ThreadCount), "Threads", 75),
            new(nameof(ProcessSnapshot.HandleCount), "Handles", 80),
            new(nameof(ProcessSnapshot.Status), "Status", 120),
            new(nameof(ProcessSnapshot.StartTimeText), "Started", 130),
            new(nameof(ProcessSnapshot.Path), "Path", 360, AutoFill: true));
        _processGrid.DataSource = _processSource;
        split.Panel2.Controls.Add(_processGrid);

        layout.Controls.Add(BuildProcessAdvisorPanel(), 0, 0);
        layout.Controls.Add(split, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private Control BuildProcessAdvisorPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 2,
            BackColor = SurfaceBack,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(6, 2, 6, 8)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 164));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Paint += (_, args) =>
        {
            using var borderPen = new Pen(Border);
            args.Graphics.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);

            using var accentBrush = new SolidBrush(Color.FromArgb(150, Accent));
            args.Graphics.FillRectangle(accentBrush, 0, 0, 4, panel.Height);
        };

        _advisorTitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Smart RAM Advisor",
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        _advisorDetailsLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Collecting process data...",
            ForeColor = MutedText,
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        _selectAdvisorButton = CreateButton("Select suggestion", Accent);
        _selectAdvisorButton.Dock = DockStyle.Fill;
        _selectAdvisorButton.Enabled = false;

        panel.Controls.Add(_advisorTitleLabel, 0, 0);
        panel.Controls.Add(_advisorDetailsLabel, 0, 1);
        panel.Controls.Add(_selectAdvisorButton, 1, 0);
        panel.SetRowSpan(_selectAdvisorButton, 2);
        return panel;
    }

    private static void ConfigureProcessSplitter(SplitContainer split)
    {
        if (split.Width < 520)
        {
            return;
        }

        var distance = Math.Clamp(300, 180, split.Width - 320);

        if (split.SplitterDistance != distance)
        {
            split.SplitterDistance = distance;
        }
    }

    private TabPage BuildPerformanceTab()
    {
        var page = CreateTabPage("Performance");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = WindowBack,
            Padding = new Padding(2)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38));

        var dashboardBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = SurfaceBack,
            Padding = new Padding(14, 9, 14, 9),
            Margin = new Padding(6, 2, 6, 8)
        };
        dashboardBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        dashboardBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

        _dashboardUrlLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Secure mobile dashboard starting...",
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Font = new Font(Font.FontFamily, 9.25F)
        };
        _openDashboardButton = CreateButton("Open dashboard", Accent);
        _openDashboardButton.Dock = DockStyle.Fill;
        _openDashboardButton.Enabled = false;

        dashboardBar.Controls.Add(_dashboardUrlLabel, 0, 0);
        dashboardBar.Controls.Add(_openDashboardButton, 1, 0);
        layout.Controls.Add(dashboardBar, 0, 0);

        var graphLayout = CreateEvenGrid(3, 1, new Padding(1));
        _cpuGraph = CreatePerformanceGraph("CPU", Accent);
        _memoryGraph = CreatePerformanceGraph("Memory", Color.FromArgb(233, 196, 106));
        _gpuGraph = CreatePerformanceGraph("GPU", Color.FromArgb(138, 111, 214));
        graphLayout.Controls.Add(_cpuGraph, 0, 0);
        graphLayout.Controls.Add(_memoryGraph, 1, 0);
        graphLayout.Controls.Add(_gpuGraph, 2, 0);
        layout.Controls.Add(graphLayout, 0, 1);

        var metricsLayout = CreateEvenGrid(6, 2, new Padding(1));
        _performanceCpuLabel = AddMetricCard(metricsLayout, "CPU", 0, 0);
        _performanceMemoryLabel = AddMetricCard(metricsLayout, "Memory", 1, 0);
        _performanceGpuLabel = AddMetricCard(metricsLayout, "GPU", 2, 0);
        _performanceBatteryLabel = AddMetricCard(metricsLayout, "Battery", 3, 0);
        _performancePowerLabel = AddMetricCard(metricsLayout, "Power", 4, 0);
        _performanceProcessesLabel = AddMetricCard(metricsLayout, "Processes", 5, 0);
        _performanceThreadsLabel = AddMetricCard(metricsLayout, "Threads", 0, 1);
        _performanceHandlesLabel = AddMetricCard(metricsLayout, "Handles", 1, 1);
        _performanceUptimeLabel = AddMetricCard(metricsLayout, "Uptime", 2, 1);
        _performanceProcessorLabel = AddMetricCard(metricsLayout, "Logical CPUs", 3, 1);
        _performanceOsLabel = AddMetricCard(metricsLayout, "OS", 4, 1);

        layout.Controls.Add(metricsLayout, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildStartupTab()
    {
        var page = CreateTabPage("Startup");
        var layout = CreateTwoRowLayout(62);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(12, 7, 12, 7),
            BackColor = SurfaceBack,
            Margin = new Padding(6, 2, 6, 8)
        };

        var refreshButton = CreateButton("Refresh", Accent);
        refreshButton.Click += (_, _) => RefreshStartupEntries();
        _startupToggleButton = CreateButton("Disable", Accent);
        toolbar.Controls.Add(refreshButton);
        toolbar.Controls.Add(_startupToggleButton);
        layout.Controls.Add(toolbar, 0, 0);

        _startupGrid = CreateGrid();
        AddTextColumns(_startupGrid,
            new(nameof(StartupEntry.Name), "Name", 190),
            new(nameof(StartupEntry.State), "State", 90),
            new(nameof(StartupEntry.Scope), "Scope", 140),
            new(nameof(StartupEntry.Command), "Command", 360),
            new(nameof(StartupEntry.RegistryPath), "Source", 320, AutoFill: true));
        _startupGrid.DataSource = _startupSource;
        layout.Controls.Add(_startupGrid, 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildRulesTab()
    {
        var page = CreateTabPage("Rules");
        var layout = CreateTwoRowLayout(108);

        var editor = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10, 9, 10, 6),
            BackColor = SurfaceBack,
            Margin = new Padding(6, 2, 6, 8),
            AutoScroll = true
        };

        _rulesActiveCheckBox = CreateCheckBox("Rules active");
        _ruleEnabledCheckBox = CreateCheckBox("Rule on");
        _ruleProcessBox = CreateTextBox("Process name", 180);
        _ruleMetricCombo = CreateComboBox(120);
        _ruleMetricCombo.DataSource = Enum.GetValues<RuleMetric>();
        _ruleThresholdInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100000,
            DecimalPlaces = 1,
            Increment = 50,
            Width = 110,
            BackColor = SurfaceBack,
            ForeColor = TextColor,
            Margin = new Padding(6, 4, 8, 4)
        };
        _ruleActionCombo = CreateComboBox(130);
        _ruleActionCombo.DataSource = Enum.GetValues<RuleAction>();

        var newButton = CreateButton("New", Accent);
        var saveButton = CreateButton("Save", Accent);
        var deleteButton = CreateButton("Delete", Danger);
        newButton.Click += (_, _) => ClearRuleEditor();
        saveButton.Click += (_, _) => SaveRuleFromEditor();
        deleteButton.Click += (_, _) => DeleteSelectedRule();

        editor.Controls.Add(_rulesActiveCheckBox);
        editor.Controls.Add(_ruleEnabledCheckBox);
        editor.Controls.Add(_ruleProcessBox);
        editor.Controls.Add(CreateCaption("Metric"));
        editor.Controls.Add(_ruleMetricCombo);
        editor.Controls.Add(CreateCaption("Threshold"));
        editor.Controls.Add(_ruleThresholdInput);
        editor.Controls.Add(CreateCaption("Action"));
        editor.Controls.Add(_ruleActionCombo);
        editor.Controls.Add(newButton);
        editor.Controls.Add(saveButton);
        editor.Controls.Add(deleteButton);
        layout.Controls.Add(editor, 0, 0);

        _rulesGrid = CreateGrid();
        _rulesGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(AutomationRule.Enabled),
            HeaderText = "On",
            Width = 52,
            ReadOnly = true
        });
        AddTextColumns(_rulesGrid,
            new(nameof(AutomationRule.ProcessNameContains), "Process contains", 190),
            new(nameof(AutomationRule.Metric), "Metric", 110),
            new(nameof(AutomationRule.Threshold), "Threshold", 100, "N1"),
            new(nameof(AutomationRule.Action), "Action", 120),
            new(nameof(AutomationRule.LastTriggeredText), "Last triggered", 150, AutoFill: true));
        layout.Controls.Add(_rulesGrid, 0, 1);

        page.Controls.Add(layout);
        ClearRuleEditor();
        return page;
    }

    private TabPage BuildHistoryTab()
    {
        var page = CreateTabPage("History");
        var layout = CreateTwoRowLayout(62);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(12, 7, 12, 7),
            BackColor = SurfaceBack,
            Margin = new Padding(6, 2, 6, 8)
        };

        _historyWindowCombo = CreateComboBox(130);
        _historyWindowCombo.Items.AddRange(["Last 1 hour", "Last 24 hours", "Last 7 days"]);
        _historyWindowCombo.SelectedIndex = 1;
        var refreshButton = CreateButton("Refresh", Accent);
        refreshButton.Click += (_, _) => RefreshHistory();

        toolbar.Controls.Add(_historyWindowCombo);
        toolbar.Controls.Add(refreshButton);
        layout.Controls.Add(toolbar, 0, 0);

        _historyGrid = CreateGrid();
        AddTextColumns(_historyGrid,
            new(nameof(HistorySummary.Name), "Process", 220),
            new(nameof(HistorySummary.Samples), "Samples", 90),
            new(nameof(HistorySummary.AverageCpu), "Avg CPU %", 100, "N1"),
            new(nameof(HistorySummary.AverageMemoryMb), "Avg RAM MB", 110, "N1"),
            new(nameof(HistorySummary.MaxMemoryMb), "Max RAM MB", 120, "N1", AutoFill: true));
        _historyGrid.DataSource = _historySource;
        layout.Controls.Add(_historyGrid, 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildVisualizationTab()
    {
        var page = CreateTabPage("Visualization");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = WindowBack
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));

        var chartPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = WindowBack,
            Padding = new Padding(10)
        };
        chartPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        chartPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        chartPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "System Visualization",
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        chartPanel.Controls.Add(title, 0, 0);

        _visualizationPieChart = new SystemPieChart
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBack,
            ForeColor = TextColor,
            MutedColor = MutedText,
            BorderColor = Border,
            Margin = new Padding(0, 0, 10, 0)
        };
        chartPanel.Controls.Add(_visualizationPieChart, 0, 1);

        _visualizationUpdatedLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Waiting for system data...",
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        chartPanel.Controls.Add(_visualizationUpdatedLabel, 0, 2);
        layout.Controls.Add(chartPanel, 0, 0);

        var analysisPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = WindowBack,
            Padding = new Padding(10)
        };
        analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        analysisPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var scorePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = SurfaceBack,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
        scorePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        scorePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

        _visualizationScoreLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "0%",
            ForeColor = Accent,
            Font = new Font("Segoe UI", 25F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _visualizationStatusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Waiting",
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        scorePanel.Controls.Add(_visualizationScoreLabel, 0, 0);
        scorePanel.Controls.Add(_visualizationStatusLabel, 1, 0);
        analysisPanel.Controls.Add(scorePanel, 0, 0);

        var metricGrid = CreateEvenGrid(2, 3);
        _visualizationCpuLabel = AddMetricCard(metricGrid, "CPU Load", 0, 0);
        _visualizationGpuLabel = AddMetricCard(metricGrid, "GPU Load", 1, 0);
        _visualizationMemoryLabel = AddMetricCard(metricGrid, "Memory Load", 0, 1);
        _visualizationBatteryLabel = AddMetricCard(metricGrid, "Battery Risk", 1, 1);
        _visualizationProcessLabel = AddMetricCard(metricGrid, "Process Load", 0, 2);
        analysisPanel.Controls.Add(metricGrid, 0, 1);

        _visualizationRecommendationLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBack,
            ForeColor = TextColor,
            Padding = new Padding(14, 10, 14, 10),
            Text = "System recommendation will appear here.",
            TextAlign = ContentAlignment.MiddleLeft,
            BorderStyle = BorderStyle.FixedSingle
        };
        analysisPanel.Controls.Add(_visualizationRecommendationLabel, 0, 2);

        var note = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            Text = "Pie chart shows pressure share from CPU, GPU, memory, battery risk, and process load.",
            TextAlign = ContentAlignment.MiddleLeft
        };
        analysisPanel.Controls.Add(note, 0, 3);

        layout.Controls.Add(analysisPanel, 1, 0);
        page.Controls.Add(layout);
        return page;
    }

    private void WireEvents()
    {
        _refreshTimer.Tick += async (_, _) => await RefreshProcessesAsync();
        _themeAnimationTimer.Tick += (_, _) => AdvanceThemeToggleAnimation();
        _searchBox.TextChanged += (_, _) => ApplyProcessFilter();
        _killButton.Click += async (_, _) => await KillSelectedProcessAsync();
        _openLocationButton.Click += (_, _) => OpenSelectedProcessLocation();
        _themeButton.Click += (_, _) => ToggleTheme();
        _selectAdvisorButton.Click += (_, _) => SelectAdvisorRecommendation();
        _processGrid.SelectionChanged += (_, _) => UpdateProcessActionState();
        _processGrid.CellDoubleClick += (_, _) => OpenSelectedProcessLocation();
        _processTree.AfterSelect += (_, args) =>
        {
            if (args.Node?.Tag is int pid)
            {
                SelectProcessInGrid(pid);
            }
        };
        _startupToggleButton.Click += (_, _) => ToggleSelectedStartupEntry();
        _startupGrid.SelectionChanged += (_, _) => UpdateStartupActionState();
        _rulesGrid.SelectionChanged += (_, _) => LoadSelectedRuleIntoEditor();
        _historyWindowCombo.SelectedIndexChanged += (_, _) => RefreshHistory();
        _openDashboardButton.Click += (_, _) => OpenDashboard();
        Shown += async (_, _) =>
        {
            await RefreshProcessesAsync();
            await StartDashboardServerAsync();
        };
        FormClosing += async (_, _) =>
        {
            _themeAnimationTimer.Stop();
            _themeAnimationTimer.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            await _dashboardServer.DisposeAsync();
        };
    }

    private void ConfigureNotifications()
    {
        _notifyIcon.Icon = SystemIcons.Application;
        _notifyIcon.Text = "Custom Task Manager";
        _notifyIcon.Visible = true;
        _notifyIcon.BalloonTipClicked += (_, _) =>
        {
            if (IsDisposed)
            {
                return;
            }

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Show();
            Activate();
        };
    }

    private async Task RefreshProcessesAsync(bool manual = false)
    {
        if (_refreshingProcesses)
        {
            return;
        }

        _refreshingProcesses = true;
        var selectedPid = SelectedProcess?.Id;

        try
        {
            var snapshots = await Task.Run(_processMonitor.Capture);
            if (IsDisposed)
            {
                return;
            }

            _snapshots = snapshots;
            ApplyProcessFilter(selectedPid);
            RefreshProcessTree(selectedPid);
            UpdateSummary();
            RefreshProcessAdvisor();
            RefreshPerformance();
            RunAutomationRules();
            AppendHistorySample();

            if (manual)
            {
                SetStatus("Process list refreshed.");
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or IOException)
        {
            SetStatus($"Process refresh failed: {ex.Message}");
        }
        finally
        {
            _refreshingProcesses = false;
        }
    }

    private void ApplyProcessFilter(int? selectedPid = null)
    {
        var filter = _searchBox.Text.Trim();
        IEnumerable<ProcessSnapshot> query = _snapshots;

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(snapshot =>
                snapshot.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                snapshot.Path.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                snapshot.Id.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        _processSource.DataSource = query
            .OrderByDescending(snapshot => snapshot.CpuPercent)
            .ThenByDescending(snapshot => snapshot.MemoryMb)
            .ToList();

        if (selectedPid is not null)
        {
            SelectProcessInGrid(selectedPid.Value);
        }

        UpdateProcessActionState();
    }

    private void RefreshProcessTree(int? selectedPid)
    {
        var treeSignature = string.Join(
            '|',
            _snapshots
                .OrderBy(snapshot => snapshot.Id)
                .Select(snapshot => $"{snapshot.Id}:{snapshot.ParentId}:{snapshot.Name}"));

        if (treeSignature == _processTreeSignature)
        {
            if (selectedPid is not null)
            {
                SelectTreeNode(selectedPid.Value);
            }

            return;
        }

        _processTreeSignature = treeSignature;
        _processTree.BeginUpdate();
        _processTree.Nodes.Clear();

        var nodes = _snapshots.ToDictionary(
            snapshot => snapshot.Id,
            snapshot => new TreeNode($"{snapshot.Name} ({snapshot.Id})") { Tag = snapshot.Id });

        var roots = new List<TreeNode>();
        foreach (var snapshot in _snapshots.OrderBy(snapshot => snapshot.Name))
        {
            var node = nodes[snapshot.Id];
            if (snapshot.ParentId is not null && nodes.TryGetValue(snapshot.ParentId.Value, out var parent))
            {
                parent.Nodes.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        _processTree.Nodes.AddRange(roots.OrderBy(node => node.Text).ToArray());

        foreach (TreeNode root in _processTree.Nodes)
        {
            root.Expand();
        }

        if (selectedPid is not null)
        {
            SelectTreeNode(selectedPid.Value);
        }

        _processTree.EndUpdate();
    }

    private void RefreshStartupEntries()
    {
        try
        {
            _startupSource.DataSource = _startupManager.GetEntries();
            UpdateStartupActionState();
            SetStatus("Startup entries refreshed.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or IOException)
        {
            SetStatus($"Startup refresh failed: {ex.Message}");
        }
    }

    private void RefreshHistory()
    {
        _historySource.DataSource = _historyStore.GetSummary(GetSelectedHistoryWindow());
    }

    private void RunAutomationRules()
    {
        if (!_rulesActiveCheckBox.Checked || _rules.Count == 0)
        {
            return;
        }

        var messages = _ruleEngine.Evaluate(_rules, _snapshots);
        if (messages.Count == 0)
        {
            return;
        }

        _ruleStore.Save(_rules);
        _rulesSource.ResetBindings(false);
        SetStatus(messages[0]);
        ShowDesktopNotification("Automation rule triggered", messages[0], ToolTipIcon.Warning);
    }

    private void AppendHistorySample()
    {
        if (DateTime.UtcNow - _lastHistoryWriteUtc < TimeSpan.FromSeconds(30))
        {
            return;
        }

        var topProcesses = _snapshots
            .OrderByDescending(snapshot => snapshot.CpuPercent)
            .ThenByDescending(snapshot => snapshot.MemoryMb)
            .Take(25);

        _historyStore.Append(DateTime.Now, topProcesses);
        _lastHistoryWriteUtc = DateTime.UtcNow;
    }

    private void UpdateSummary()
    {
        var cpu = Math.Min(100, _snapshots.Sum(snapshot => snapshot.CpuPercent));
        var memory = _snapshots.Sum(snapshot => snapshot.MemoryMb);
        _summaryLabel.Text = $"Processes {_snapshots.Count:N0}    CPU {cpu:N1}%    RAM {memory:N0} MB    Updated {DateTime.Now:T}";
    }

    private void RefreshProcessAdvisor()
    {
        var recommendations = _processAdvisor.RecommendMemoryClosures(_snapshots);
        var topRecommendation = recommendations.FirstOrDefault();

        if (topRecommendation is null)
        {
            _advisorTitleLabel.Text = "Smart RAM Advisor";
            _advisorDetailsLabel.Text = "No safe high-memory app process found right now. Keep monitoring before closing anything.";
            _selectAdvisorButton.Enabled = false;
            return;
        }

        var shortList = string.Join(
            "  |  ",
            recommendations.Select(recommendation =>
                $"{recommendation.ProcessName} ~{recommendation.EstimatedMemoryMb:N0} MB"));

        _advisorTitleLabel.Text = $"Smart RAM Advisor: review {topRecommendation.ProcessName}";
        _advisorDetailsLabel.Text =
            $"{shortList}. Why: {topRecommendation.Reason}. {topRecommendation.SafetyNote}";
        _selectAdvisorButton.Enabled = true;
    }

    private void RefreshPerformance()
    {
        var snapshot = _performanceMonitor.Capture(_snapshots);
        _dashboardState.Update(snapshot, _snapshots);

        _cpuGraph.AddValue(snapshot.CpuPercent);
        _memoryGraph.AddValue(snapshot.MemoryPercent);
        _gpuGraph.ValueTextOverride = snapshot.Gpu.Summary;
        _gpuGraph.AddValue(snapshot.Gpu.IsAvailable ? snapshot.Gpu.UsagePercent : 0);

        _performanceCpuLabel.Text = $"{snapshot.CpuPercent:N1}%";
        _performanceMemoryLabel.Text = $"{snapshot.MemoryUsedGb:N1} / {snapshot.MemoryTotalGb:N1} GB ({snapshot.MemoryPercent:N1}%)";
        _performanceGpuLabel.Text = snapshot.Gpu.Summary;
        _performanceBatteryLabel.Text = snapshot.Battery.Summary;
        _performancePowerLabel.Text = FormatPowerStatus(snapshot.Battery);
        _performanceProcessesLabel.Text = snapshot.ProcessCount.ToString("N0");
        _performanceThreadsLabel.Text = snapshot.ThreadCount.ToString("N0");
        _performanceHandlesLabel.Text = snapshot.HandleCount.ToString("N0");
        _performanceUptimeLabel.Text = FormatUptime(snapshot.Uptime);
        _performanceProcessorLabel.Text = snapshot.ProcessorCount.ToString("N0");
        _performanceOsLabel.Text = snapshot.OsVersion.Replace("Microsoft Windows ", "Windows ");

        RefreshVisualization(snapshot);
        EvaluateSmartAlerts(snapshot);
    }

    private void EvaluateSmartAlerts(PerformanceSnapshot snapshot)
    {
        _highCpuSamples = snapshot.CpuPercent >= SmartAlertCpuThreshold
            ? Math.Min(_highCpuSamples + 1, SmartAlertRequiredSamples)
            : 0;
        _highMemorySamples = snapshot.MemoryPercent >= SmartAlertMemoryThreshold
            ? Math.Min(_highMemorySamples + 1, SmartAlertRequiredSamples)
            : 0;

        if (DateTime.UtcNow - _lastSmartAlertUtc < SmartAlertCooldown)
        {
            return;
        }

        if (_highCpuSamples >= SmartAlertRequiredSamples)
        {
            var topCpu = _snapshots
                .OrderByDescending(process => process.CpuPercent)
                .FirstOrDefault();
            var topText = topCpu is null
                ? string.Empty
                : $" Top CPU: {topCpu.Name} ({topCpu.CpuPercent:N1}%).";

            ShowDesktopNotification(
                "High CPU alert",
                $"CPU stayed above {SmartAlertCpuThreshold:N0}% ({snapshot.CpuPercent:N1}%).{topText}",
                ToolTipIcon.Warning);
            SetStatus("Smart alert: sustained high CPU detected.");
            _lastSmartAlertUtc = DateTime.UtcNow;
            return;
        }

        if (_highMemorySamples >= SmartAlertRequiredSamples)
        {
            var topMemory = _snapshots
                .OrderByDescending(process => process.MemoryMb)
                .FirstOrDefault();
            var topText = topMemory is null
                ? string.Empty
                : $" Top RAM: {topMemory.Name} ({topMemory.MemoryMb:N1} MB).";

            ShowDesktopNotification(
                "High memory alert",
                $"Memory stayed above {SmartAlertMemoryThreshold:N0}% ({snapshot.MemoryPercent:N1}%).{topText}",
                ToolTipIcon.Warning);
            SetStatus("Smart alert: sustained high memory detected.");
            _lastSmartAlertUtc = DateTime.UtcNow;
        }
    }

    private void RefreshVisualization(PerformanceSnapshot snapshot)
    {
        var cpuPressure = Math.Clamp(snapshot.CpuPercent, 0, 100);
        var memoryPressure = Math.Clamp(snapshot.MemoryPercent, 0, 100);
        var gpuPressure = snapshot.Gpu.IsAvailable
            ? Math.Clamp(snapshot.Gpu.UsagePercent, 0, 100)
            : (double?)null;
        var batteryPressure = GetBatteryPressure(snapshot.Battery);
        var processPressure = GetProcessPressure(snapshot);

        var weightedScore = CalculateSystemPressure(cpuPressure, memoryPressure, gpuPressure, batteryPressure, processPressure);
        var status = GetSystemPressureStatus(weightedScore);

        var slices = new List<PieChartSlice>
        {
            new("CPU", cpuPressure, Color.FromArgb(42, 157, 143), $"{cpuPressure:N1}% current CPU"),
            new("Memory", memoryPressure, Color.FromArgb(233, 196, 106), $"{memoryPressure:N1}% RAM used"),
            new("Processes", processPressure, Color.FromArgb(69, 123, 157), $"{snapshot.ProcessCount:N0} processes, {snapshot.ThreadCount:N0} threads")
        };

        if (gpuPressure is not null)
        {
            slices.Add(new PieChartSlice("GPU", gpuPressure.Value, Color.FromArgb(138, 111, 214), $"{gpuPressure.Value:N1}% GPU engine load"));
        }

        if (batteryPressure is not null)
        {
            slices.Add(new PieChartSlice("Battery", batteryPressure.Value, Color.FromArgb(198, 73, 70), $"{snapshot.Battery.Summary}"));
        }

        if (slices.All(slice => slice.Value < 1))
        {
            slices = [new PieChartSlice("Idle", 1, Color.FromArgb(80, 86, 96), "No meaningful pressure detected")];
        }

        _visualizationPieChart.CenterText = $"{weightedScore:N0}%";
        _visualizationPieChart.SubText = "Pressure";
        _visualizationPieChart.SetSlices(slices);

        _visualizationScoreLabel.Text = $"{weightedScore:N0}%";
        _visualizationScoreLabel.ForeColor = GetPressureColor(weightedScore);
        _visualizationStatusLabel.Text = status;
        _visualizationCpuLabel.Text = $"{cpuPressure:N1}%";
        _visualizationGpuLabel.Text = snapshot.Gpu.IsAvailable
            ? $"{snapshot.Gpu.UsagePercent:N1}%"
            : "Unavailable";
        _visualizationMemoryLabel.Text = $"{memoryPressure:N1}%";
        _visualizationBatteryLabel.Text = batteryPressure is null
            ? "No battery"
            : $"{batteryPressure.Value:N1}% risk";
        _visualizationProcessLabel.Text = $"{processPressure:N1}%";
        _visualizationRecommendationLabel.Text = BuildVisualizationRecommendation(
            snapshot,
            cpuPressure,
            memoryPressure,
            gpuPressure,
            batteryPressure,
            processPressure,
            weightedScore);
        _visualizationUpdatedLabel.Text = $"Updated {DateTime.Now:T}    GPU: {snapshot.Gpu.Summary}";
    }

    private static double? GetBatteryPressure(BatterySnapshot battery)
    {
        if (!battery.IsBatteryPresent || battery.ChargePercent is null)
        {
            return null;
        }

        var lowChargePressure = 100 - battery.ChargePercent.Value;
        if (battery.PowerLineStatus.Equals("Online", StringComparison.OrdinalIgnoreCase))
        {
            lowChargePressure *= 0.45;
        }

        return Math.Round(Math.Clamp(lowChargePressure, 0, 100), 1);
    }

    private static double GetProcessPressure(PerformanceSnapshot snapshot)
    {
        var processPressure = snapshot.ProcessCount / 320d * 100d;
        var threadPressure = snapshot.ThreadCount / 5000d * 100d;
        var handlePressure = snapshot.HandleCount / 160000d * 100d;
        return Math.Round(Math.Clamp(Math.Max(processPressure, Math.Max(threadPressure, handlePressure)), 0, 100), 1);
    }

    private static double CalculateSystemPressure(
        double cpuPressure,
        double memoryPressure,
        double? gpuPressure,
        double? batteryPressure,
        double processPressure)
    {
        var weightedTotal = cpuPressure * 0.30 + memoryPressure * 0.30 + processPressure * 0.16;
        var totalWeight = 0.76;

        if (gpuPressure is not null)
        {
            weightedTotal += gpuPressure.Value * 0.14;
            totalWeight += 0.14;
        }

        if (batteryPressure is not null)
        {
            weightedTotal += batteryPressure.Value * 0.10;
            totalWeight += 0.10;
        }

        return Math.Round(Math.Clamp(weightedTotal / totalWeight, 0, 100), 1);
    }

    private static string GetSystemPressureStatus(double score)
    {
        return score switch
        {
            >= 85 => "Critical pressure",
            >= 70 => "High pressure",
            >= 45 => "Moderate pressure",
            >= 25 => "Light pressure",
            _ => "Healthy"
        };
    }

    private static Color GetPressureColor(double score)
    {
        return score switch
        {
            >= 85 => Danger,
            >= 70 => Color.FromArgb(231, 111, 81),
            >= 45 => Color.FromArgb(233, 196, 106),
            _ => Accent
        };
    }

    private static string BuildVisualizationRecommendation(
        PerformanceSnapshot snapshot,
        double cpuPressure,
        double memoryPressure,
        double? gpuPressure,
        double? batteryPressure,
        double processPressure,
        double score)
    {
        if (cpuPressure >= 85)
        {
            return "CPU pressure high hai. Processes tab me Top CPU process check karo aur unnecessary task close karo.";
        }

        if (memoryPressure >= 85)
        {
            return "RAM pressure high hai. Browser tabs/heavy apps close karo, warna system slow feel ho sakta hai.";
        }

        if (gpuPressure >= 80)
        {
            return "GPU load high hai. Game, video render, browser acceleration ya graphics app background me chal sakti hai.";
        }

        if (batteryPressure >= 75 && !snapshot.Battery.PowerLineStatus.Equals("Online", StringComparison.OrdinalIgnoreCase))
        {
            return "Battery risk high hai. Charger connect karo ya heavy apps close karo.";
        }

        if (processPressure >= 80)
        {
            return "Process/thread/handle load high hai. Startup apps aur background services review karna useful hoga.";
        }

        if (score >= 70)
        {
            return "System pressure high side par hai. CPU, RAM aur process count ko saath me review karo.";
        }

        if (!snapshot.Gpu.IsAvailable)
        {
            return "System mostly stable hai. GPU counter unavailable hai, so GPU analysis skip ho raha hai.";
        }

        return "System healthy range me hai. No immediate action needed.";
    }

    private async Task StartDashboardServerAsync()
    {
        try
        {
            await _dashboardServer.StartAsync();
            var urls = _dashboardServer.GetAccessUrls()
                .Select(_dashboardServer.BuildAccessUrl)
                .ToList();
            _dashboardUrlLabel.Text = $"Secure dashboard: {string.Join("  |  ", urls)}    Key: {_dashboardServer.AccessKey}";
            _openDashboardButton.Enabled = true;
            SetStatus($"Secure dashboard running at {urls.First()}.");
        }
        catch (Exception ex)
        {
            _dashboardUrlLabel.Text = $"Mobile dashboard unavailable: {ex.Message}";
            _openDashboardButton.Enabled = false;
            SetStatus($"Mobile dashboard failed: {ex.Message}");
        }
    }

    private void OpenDashboard()
    {
        Process.Start(new ProcessStartInfo(_dashboardServer.LocalAccessUrl)
        {
            UseShellExecute = true
        });
    }

    private async Task KillSelectedProcessAsync()
    {
        var selected = SelectedProcess;
        if (selected is null)
        {
            return;
        }

        if (selected.Id == Environment.ProcessId)
        {
            SetStatus("This app cannot kill itself.");
            return;
        }

        var answer = MessageBox.Show(
            this,
            $"Terminate {selected.Name} ({selected.Id}) and its child processes?",
            "Confirm kill",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(selected.Id);
            process.Kill(entireProcessTree: true);
            SetStatus($"Killed {selected.Name} ({selected.Id}).");
            await RefreshProcessesAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            SetStatus($"Kill failed: {ex.Message}");
        }
    }

    private void OpenSelectedProcessLocation()
    {
        var selected = SelectedProcess;
        if (selected is null || string.IsNullOrWhiteSpace(selected.Path) || !File.Exists(selected.Path))
        {
            SetStatus("Process location is not available.");
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{selected.Path}\"")
        {
            UseShellExecute = true
        });
    }

    private void ToggleSelectedStartupEntry()
    {
        if (_startupSource.Current is not StartupEntry entry)
        {
            return;
        }

        try
        {
            if (entry.Enabled)
            {
                _startupManager.Disable(entry);
                SetStatus($"Disabled startup entry {entry.Name}.");
            }
            else
            {
                _startupManager.Enable(entry);
                SetStatus($"Enabled startup entry {entry.Name}.");
            }

            RefreshStartupEntries();
        }
        catch (UnauthorizedAccessException)
        {
            SetStatus("Administrator permission is required for this startup entry.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or IOException)
        {
            SetStatus($"Startup change failed: {ex.Message}");
        }
    }

    private void SaveRuleFromEditor()
    {
        var processName = _ruleProcessBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(processName))
        {
            SetStatus("Rule needs a process name.");
            return;
        }

        var rule = _editingRuleId is null
            ? null
            : _rules.FirstOrDefault(candidate => candidate.Id == _editingRuleId.Value);

        if (rule is null)
        {
            rule = new AutomationRule();
            _rules.Add(rule);
        }

        rule.Enabled = _ruleEnabledCheckBox.Checked;
        rule.ProcessNameContains = processName;
        rule.Metric = (RuleMetric)_ruleMetricCombo.SelectedItem!;
        rule.Threshold = (double)_ruleThresholdInput.Value;
        rule.Action = (RuleAction)_ruleActionCombo.SelectedItem!;

        _editingRuleId = rule.Id;
        _ruleStore.Save(_rules);
        _rulesSource.ResetBindings(false);
        SelectRule(rule.Id);
        SetStatus("Rule saved.");
    }

    private void DeleteSelectedRule()
    {
        var rule = _editingRuleId is null
            ? _rulesSource.Current as AutomationRule
            : _rules.FirstOrDefault(candidate => candidate.Id == _editingRuleId.Value);

        if (rule is null)
        {
            return;
        }

        _rules.Remove(rule);
        _ruleStore.Save(_rules);
        _rulesSource.ResetBindings(false);
        ClearRuleEditor();
        SetStatus("Rule deleted.");
    }

    private void LoadSelectedRuleIntoEditor()
    {
        if (_rulesSource.Current is not AutomationRule rule)
        {
            return;
        }

        _editingRuleId = rule.Id;
        _ruleEnabledCheckBox.Checked = rule.Enabled;
        _ruleProcessBox.Text = rule.ProcessNameContains;
        _ruleMetricCombo.SelectedItem = rule.Metric;
        _ruleThresholdInput.Value = (decimal)Math.Clamp(rule.Threshold, (double)_ruleThresholdInput.Minimum, (double)_ruleThresholdInput.Maximum);
        _ruleActionCombo.SelectedItem = rule.Action;
    }

    private void ClearRuleEditor()
    {
        _editingRuleId = null;
        _ruleEnabledCheckBox.Checked = true;
        _ruleProcessBox.Clear();
        _ruleMetricCombo.SelectedItem = RuleMetric.MemoryMb;
        _ruleThresholdInput.Value = 1024;
        _ruleActionCombo.SelectedItem = RuleAction.Alert;
    }

    private void UpdateProcessActionState()
    {
        var hasSelection = SelectedProcess is not null;
        _killButton.Enabled = hasSelection;
        _openLocationButton.Enabled = hasSelection;
    }

    private void UpdateStartupActionState()
    {
        if (_startupSource.Current is StartupEntry entry)
        {
            _startupToggleButton.Enabled = true;
            _startupToggleButton.Text = entry.Enabled ? "Disable" : "Enable";
            _startupToggleButton.Tag = entry.Enabled ? "danger" : "accent";
            StyleButton(_startupToggleButton, entry.Enabled ? Danger : Accent);
            return;
        }

        _startupToggleButton.Enabled = false;
        _startupToggleButton.Text = "Disable";
        _startupToggleButton.Tag = "accent";
        StyleButton(_startupToggleButton, Accent);
    }

    private void SelectAdvisorRecommendation()
    {
        var recommendation = _processAdvisor.RecommendMemoryClosures(_snapshots).FirstOrDefault();
        if (recommendation is null)
        {
            SetStatus("Smart RAM Advisor has no safe suggestion right now.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_searchBox.Text))
        {
            _searchBox.Clear();
        }

        SelectProcessInGrid(recommendation.ProcessId);
        SelectTreeNode(recommendation.ProcessId);
        SetStatus(
            $"Advisor selected {recommendation.ProcessName} ({recommendation.ProcessId}); review it, then use Kill only if safe.");
    }

    private ProcessSnapshot? SelectedProcess => _processSource.Current as ProcessSnapshot;

    private void SelectProcessInGrid(int pid)
    {
        foreach (DataGridViewRow row in _processGrid.Rows)
        {
            if (row.DataBoundItem is not ProcessSnapshot snapshot || snapshot.Id != pid)
            {
                continue;
            }

            row.Selected = true;
            _processGrid.CurrentCell = row.Cells[0];
            return;
        }
    }

    private void SelectTreeNode(int pid)
    {
        foreach (TreeNode root in _processTree.Nodes)
        {
            var match = FindNode(root, pid);
            if (match is null)
            {
                continue;
            }

            _processTree.SelectedNode = match;
            match.EnsureVisible();
            return;
        }
    }

    private void SelectRule(Guid ruleId)
    {
        foreach (DataGridViewRow row in _rulesGrid.Rows)
        {
            if (row.DataBoundItem is not AutomationRule rule || rule.Id != ruleId)
            {
                continue;
            }

            row.Selected = true;
            _rulesGrid.CurrentCell = row.Cells[0];
            return;
        }
    }

    private static TreeNode? FindNode(TreeNode node, int pid)
    {
        if (node.Tag is int nodePid && nodePid == pid)
        {
            return node;
        }

        foreach (TreeNode child in node.Nodes)
        {
            var match = FindNode(child, pid);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private TimeSpan GetSelectedHistoryWindow()
    {
        return _historyWindowCombo.SelectedIndex switch
        {
            0 => TimeSpan.FromHours(1),
            2 => TimeSpan.FromDays(7),
            _ => TimeSpan.FromDays(1)
        };
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = $"{DateTime.Now:T}  {message}";
    }

    private void ShowDesktopNotification(string title, string message, ToolTipIcon icon)
    {
        if (IsDisposed || !_notifyIcon.Visible)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = TrimNotificationText(message);
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(5000);
    }

    private static string TrimNotificationText(string message)
    {
        const int maximumBalloonTextLength = 255;
        return message.Length <= maximumBalloonTextLength
            ? message
            : message[..(maximumBalloonTextLength - 3)] + "...";
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
    }

    private static string FormatPowerStatus(BatterySnapshot battery)
    {
        if (!battery.IsBatteryPresent)
        {
            return battery.PowerLineStatus;
        }

        if (battery.LifeRemaining is null)
        {
            return battery.PowerLineStatus;
        }

        var remaining = battery.LifeRemaining.Value;
        return $"{battery.PowerLineStatus}, {remaining.Hours}h {remaining.Minutes}m left";
    }

    private void ToggleTheme()
    {
        var previousPalette = CapturePalette();
        _isLightTheme = !_isLightTheme;
        ApplyPalette(CreatePalette(_isLightTheme));

        ApplyThemeToControl(this, previousPalette);
        StyleThemeButton(_themeButton);
        StartThemeToggleAnimation();
        UpdateStartupActionState();

        SetStatus($"{(_isLightTheme ? "Light" : "Dark")} theme applied.");
        Invalidate(true);
    }

    private void StartThemeToggleAnimation()
    {
        _themeToggleTargetProgress = _isLightTheme ? 1F : 0F;
        _themeAnimationTimer.Start();
    }

    private void AdvanceThemeToggleAnimation()
    {
        const float step = 0.08F;
        var delta = _themeToggleTargetProgress - _themeToggleProgress;
        if (Math.Abs(delta) <= step)
        {
            _themeToggleProgress = _themeToggleTargetProgress;
            _themeAnimationTimer.Stop();
        }
        else
        {
            _themeToggleProgress += Math.Sign(delta) * step;
        }

        _themeButton.Invalidate();
    }

    private void ApplyThemeToControl(Control control, UiPalette previousPalette)
    {
        control.SuspendLayout();
        control.BackColor = MapThemeColor(control.BackColor, previousPalette);
        control.ForeColor = MapThemeColor(control.ForeColor, previousPalette);

        switch (control)
        {
            case Button button:
                if (button.Tag as string == "theme")
                {
                    StyleThemeButton(button);
                }
                else
                {
                    var buttonColor = button.Tag as string == "danger" ? Danger : Accent;
                    StyleButton(button, buttonColor);
                }
                break;
            case DataGridView grid:
                ApplyGridTheme(grid);
                break;
            case PerformanceGraph graph:
                graph.BackColor = SurfaceBack;
                graph.ForeColor = TextColor;
                graph.GridColor = Border;
                graph.LineColor = MapThemeColor(graph.LineColor, previousPalette);
                graph.Invalidate();
                break;
            case SystemPieChart pieChart:
                pieChart.BackColor = SurfaceBack;
                pieChart.ForeColor = TextColor;
                pieChart.MutedColor = MutedText;
                pieChart.BorderColor = Border;
                pieChart.Invalidate();
                break;
            case TabControl tabControl:
                tabControl.BackColor = WindowBack;
                tabControl.Invalidate();
                break;
            case StatusStrip statusStrip:
                statusStrip.BackColor = HeaderBack;
                statusStrip.ForeColor = MutedText;
                foreach (ToolStripItem item in statusStrip.Items)
                {
                    item.ForeColor = MutedText;
                }
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child, previousPalette);
        }

        control.ResumeLayout();
    }

    private static UiPalette CapturePalette()
    {
        return new UiPalette(
            WindowBack,
            PanelBack,
            SurfaceBack,
            HeaderBack,
            Border,
            TextColor,
            MutedText,
            Accent,
            AccentDark,
            Danger,
            GridAlternateBack);
    }

    private static UiPalette CreatePalette(bool lightTheme)
    {
        return lightTheme
            ? new UiPalette(
                Color.FromArgb(244, 247, 250),
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(236, 241, 247),
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(207, 216, 226),
                Color.FromArgb(28, 38, 50),
                Color.FromArgb(91, 103, 118),
                Color.FromArgb(0, 145, 135),
                Color.FromArgb(0, 118, 110),
                Color.FromArgb(211, 68, 68),
                Color.FromArgb(247, 249, 252))
            : new UiPalette(
                Color.FromArgb(14, 17, 21),
                Color.FromArgb(23, 27, 33),
                Color.FromArgb(30, 36, 44),
                Color.FromArgb(19, 23, 29),
                Color.FromArgb(55, 66, 80),
                Color.FromArgb(242, 245, 248),
                Color.FromArgb(154, 164, 177),
                Color.FromArgb(0, 179, 167),
                Color.FromArgb(20, 126, 119),
                Color.FromArgb(224, 82, 82),
                Color.FromArgb(27, 32, 39));
    }

    private static void ApplyPalette(UiPalette palette)
    {
        WindowBack = palette.WindowBack;
        PanelBack = palette.PanelBack;
        SurfaceBack = palette.SurfaceBack;
        HeaderBack = palette.HeaderBack;
        Border = palette.Border;
        TextColor = palette.TextColor;
        MutedText = palette.MutedText;
        Accent = palette.Accent;
        AccentDark = palette.AccentDark;
        Danger = palette.Danger;
        GridAlternateBack = palette.GridAlternateBack;
    }

    private static Color MapThemeColor(Color color, UiPalette previousPalette)
    {
        var argb = color.ToArgb();
        if (argb == previousPalette.WindowBack.ToArgb()) return WindowBack;
        if (argb == previousPalette.PanelBack.ToArgb()) return PanelBack;
        if (argb == previousPalette.SurfaceBack.ToArgb()) return SurfaceBack;
        if (argb == previousPalette.HeaderBack.ToArgb()) return HeaderBack;
        if (argb == previousPalette.Border.ToArgb()) return Border;
        if (argb == previousPalette.TextColor.ToArgb()) return TextColor;
        if (argb == previousPalette.MutedText.ToArgb()) return MutedText;
        if (argb == previousPalette.Accent.ToArgb()) return Accent;
        if (argb == previousPalette.AccentDark.ToArgb()) return AccentDark;
        if (argb == previousPalette.Danger.ToArgb()) return Danger;
        if (argb == previousPalette.GridAlternateBack.ToArgb()) return GridAlternateBack;
        return color;
    }

    private readonly record struct UiPalette(
        Color WindowBack,
        Color PanelBack,
        Color SurfaceBack,
        Color HeaderBack,
        Color Border,
        Color TextColor,
        Color MutedText,
        Color Accent,
        Color AccentDark,
        Color Danger,
        Color GridAlternateBack);

    private static PerformanceGraph CreatePerformanceGraph(string caption, Color lineColor)
    {
        return new PerformanceGraph
        {
            Dock = DockStyle.Fill,
            Caption = caption,
            LineColor = lineColor,
            BackColor = SurfaceBack,
            GridColor = Border,
            Margin = new Padding(7)
        };
    }

    private static TableLayoutPanel CreateEvenGrid(int columns, int rows, Padding? padding = null)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = rows,
            ColumnCount = columns,
            BackColor = WindowBack,
            Padding = padding ?? Padding.Empty
        };

        for (var column = 0; column < columns; column++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
        }

        for (var row = 0; row < rows; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
        }

        return layout;
    }

    private static Label AddMetricCard(TableLayoutPanel layout, string title, int column, int row)
    {
        layout.Controls.Add(CreateMetricCard(title, out var label), column, row);
        return label;
    }

    private static Control CreateMetricCard(string title, out Label valueLabel)
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = SurfaceBack,
            Padding = new Padding(14, 9, 14, 9),
            Margin = new Padding(7)
        };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Paint += (_, args) =>
        {
            using var borderPen = new Pen(Border);
            args.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);

            using var accentBrush = new SolidBrush(Color.FromArgb(140, Accent));
            args.Graphics.FillRectangle(accentBrush, 0, 0, 3, card.Height);
        };

        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.75F, FontStyle.Bold)
        };

        valueLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "-",
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        card.Controls.Add(titleLabel, 0, 0);
        card.Controls.Add(valueLabel, 0, 1);
        return card;
    }

    private static TabPage CreateTabPage(string title)
    {
        return new TabPage(title)
        {
            BackColor = WindowBack,
            ForeColor = TextColor,
            Padding = new Padding(10)
        };
    }

    private static TableLayoutPanel CreateTwoRowLayout(int topHeight)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = WindowBack,
            Padding = new Padding(2)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, topHeight));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        return layout;
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = PanelBack,
            GridColor = Border,
            EnableHeadersVisualStyles = false,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            RowTemplate = { Height = 32 }
        };

        ApplyGridTheme(grid);
        return grid;
    }

    private static void ApplyGridTheme(DataGridView grid)
    {
        grid.BackgroundColor = PanelBack;
        grid.GridColor = Border;
        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBack;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderBack;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextColor;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.DefaultCellStyle.BackColor = PanelBack;
        grid.DefaultCellStyle.ForeColor = TextColor;
        grid.DefaultCellStyle.SelectionBackColor = AccentDark;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        grid.AlternatingRowsDefaultCellStyle.BackColor = GridAlternateBack;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = TextColor;
    }

    private readonly record struct GridColumn(
        string PropertyName,
        string HeaderText,
        int Width,
        string? Format = null,
        bool AutoFill = false);

    private static void AddTextColumns(DataGridView grid, params GridColumn[] columns)
    {
        foreach (var column in columns)
        {
            AddTextColumn(
                grid,
                column.PropertyName,
                column.HeaderText,
                column.Width,
                column.Format,
                column.AutoFill);
        }
    }

    private static void AddTextColumn(
        DataGridView grid,
        string propertyName,
        string headerText,
        int width,
        string? format = null,
        bool autoFill = false)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            Width = width,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        if (!string.IsNullOrWhiteSpace(format))
        {
            column.DefaultCellStyle.Format = format;
        }

        if (autoFill)
        {
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        grid.Columns.Add(column);
    }

    private static Button CreateButton(string text, Color backColor)
    {
        var font = new Font("Segoe UI", 9F, FontStyle.Bold);
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Height = 36,
            Width = GetButtonWidth(text),
            MinimumSize = new Size(82, 36),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = font,
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(6, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            Tag = backColor.ToArgb() == Danger.ToArgb() ? "danger" : "accent"
        };
        StyleButton(button, backColor);
        return button;
    }

    private Button CreateThemeButton()
    {
        var button = new Button
        {
            AutoSize = false,
            Width = ThemeToggleWidth,
            Height = ThemeToggleHeight,
            MinimumSize = new Size(ThemeToggleWidth, ThemeToggleHeight),
            MaximumSize = new Size(ThemeToggleWidth, ThemeToggleHeight),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0),
            Padding = Padding.Empty,
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            Tag = "theme",
            AccessibleName = "Theme toggle"
        };

        button.Resize += (_, _) => SetPillButtonRegion(button);
        button.Paint += (_, args) =>
        {
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawThemeToggle(args.Graphics, button.ClientRectangle);
        };

        StyleThemeButton(button);
        return button;
    }

    private static int GetButtonWidth(string text)
    {
        using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
        return Math.Max(82, TextRenderer.MeasureText(text, font).Width + 26);
    }

    private static void StyleButton(Button button, Color backColor)
    {
        button.BackColor = backColor;
        button.ForeColor = Color.White;
        button.FlatAppearance.BorderColor = backColor;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.12F);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.08F);
    }

    private void StyleThemeButton(Button button)
    {
        button.Text = string.Empty;
        button.BackColor = _isLightTheme ? Color.FromArgb(232, 232, 229) : Color.FromArgb(36, 39, 46);
        button.ForeColor = _isLightTheme ? Color.White : Color.FromArgb(230, 231, 235);
        button.AccessibleDescription = _isLightTheme ? "Switch to dark theme" : "Switch to light theme";
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.BorderColor = button.BackColor;
        button.FlatAppearance.MouseOverBackColor = _isLightTheme
            ? Color.FromArgb(244, 244, 241)
            : Color.FromArgb(48, 52, 60);
        button.FlatAppearance.MouseDownBackColor = _isLightTheme
            ? Color.FromArgb(218, 218, 214)
            : Color.FromArgb(28, 31, 37);
        SetPillButtonRegion(button);
        button.Invalidate();
    }

    private void DrawThemeToggle(Graphics graphics, Rectangle bounds)
    {
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var progress = EaseThemeToggle(_themeToggleProgress);
        var trackBounds = new RectangleF(1F, 1F, bounds.Width - 2F, bounds.Height - 2F);
        var trackRadius = trackBounds.Height / 2F;
        var trackTop = BlendColor(Color.FromArgb(28, 31, 37), Color.FromArgb(242, 242, 239), progress);
        var trackBottom = BlendColor(Color.FromArgb(42, 45, 52), Color.FromArgb(220, 220, 216), progress);
        var borderColor = BlendColor(Color.FromArgb(14, 16, 20), Color.FromArgb(205, 205, 201), progress);
        var highlightColor = BlendColor(Color.FromArgb(70, 74, 82), Color.FromArgb(252, 252, 250), progress);

        var shadowBounds = trackBounds;
        shadowBounds.Offset(0F, 1.5F);
        using (var shadowPath = CreateRoundedRectanglePath(shadowBounds, trackRadius))
        using (var trackShadow = new SolidBrush(Color.FromArgb(50, Color.Black)))
        {
            graphics.FillPath(trackShadow, shadowPath);
        }

        trackBounds.Inflate(-1F, -1F);
        trackRadius = trackBounds.Height / 2F;
        using var buttonPath = CreateRoundedRectanglePath(trackBounds, trackRadius);
        using var buttonBrush = new LinearGradientBrush(trackBounds, trackTop, trackBottom, LinearGradientMode.Vertical);
        using var borderPen = new Pen(borderColor, 1.3F);
        graphics.FillPath(buttonBrush, buttonPath);
        graphics.DrawPath(borderPen, buttonPath);

        var innerBounds = trackBounds;
        innerBounds.Inflate(-3.5F, -3.5F);
        using (var innerPath = CreateRoundedRectanglePath(innerBounds, innerBounds.Height / 2F))
        using (var innerPen = new Pen(Color.FromArgb(155, highlightColor), 1F))
        {
            graphics.DrawPath(innerPen, innerPath);
        }

        var lightLabelAlpha = (int)Math.Round(235 * progress);
        var darkLabelAlpha = (int)Math.Round(235 * (1F - progress));
        DrawThemeLabel(
            graphics,
            new RectangleF(trackBounds.Left + 15F, trackBounds.Top + 5F, 74F, trackBounds.Height - 10F),
            "LIGHT\nMODE",
            Color.FromArgb(lightLabelAlpha, Color.White));
        DrawThemeLabel(
            graphics,
            new RectangleF(trackBounds.Right - 82F, trackBounds.Top + 5F, 72F, trackBounds.Height - 10F),
            "DARK\nMODE",
            Color.FromArgb(darkLabelAlpha, Color.FromArgb(151, 153, 160)));

        var knobSize = trackBounds.Height - 7F;
        var knobTravel = trackBounds.Width - knobSize - 10F;
        var knobX = trackBounds.Left + 5F + knobTravel * progress;
        var knobBounds = new RectangleF(knobX, trackBounds.Top + 3.5F, knobSize, knobSize);
        var shadowAlpha = (int)Math.Round(90 - 26 * progress);
        using var shadowBrush = new SolidBrush(Color.FromArgb(shadowAlpha, Color.Black));
        graphics.FillEllipse(shadowBrush, knobBounds.Left + 2.4F, knobBounds.Top + 2.7F, knobBounds.Width, knobBounds.Height);

        var knobTop = BlendColor(Color.FromArgb(255, 255, 255), Color.FromArgb(248, 248, 246), progress);
        var knobBottom = BlendColor(Color.FromArgb(235, 237, 241), Color.FromArgb(224, 224, 220), progress);
        var knobFill = BlendColor(knobTop, knobBottom, 0.42F);
        var knobBorder = BlendColor(Color.FromArgb(216, 219, 224), Color.FromArgb(199, 199, 194), progress);
        using var knobBrush = new LinearGradientBrush(knobBounds, knobTop, knobBottom, LinearGradientMode.Vertical);
        using var knobBorderPen = new Pen(knobBorder, 1.3F);
        graphics.FillEllipse(knobBrush, knobBounds);
        graphics.DrawEllipse(knobBorderPen, knobBounds);

        var knobHighlight = knobBounds;
        knobHighlight.Inflate(-4F, -4F);
        using (var highlightPen = new Pen(Color.FromArgb(130, Color.White), 1F))
        {
            graphics.DrawArc(highlightPen, knobHighlight, 205F, 125F);
        }

        var iconBounds = knobBounds;
        iconBounds.Inflate(-6.1F, -6.1F);
        var moonAlpha = (int)Math.Round(255 * (1F - progress));
        var sunAlpha = (int)Math.Round(255 * progress);

        var graphicsState = graphics.Save();
        graphics.SetClip(knobBounds, CombineMode.Intersect);
        if (moonAlpha > 0)
        {
            DrawMoonIcon(
                graphics,
                iconBounds,
                Color.FromArgb(moonAlpha, Color.FromArgb(32, 36, 43)),
                knobFill);
        }

        if (sunAlpha > 0)
        {
            DrawSunIcon(
                graphics,
                iconBounds,
                Color.FromArgb(sunAlpha, Color.FromArgb(172, 172, 174)),
                0.9F + 0.1F * progress,
                -40F * (1F - progress));
        }

        graphics.Restore(graphicsState);
    }

    private static void DrawThemeLabel(Graphics graphics, RectangleF bounds, string text, Color color)
    {
        if (color.A <= 0)
        {
            return;
        }

        using var labelFont = new Font("Segoe UI", 10.8F, FontStyle.Bold);
        using var labelBrush = new SolidBrush(color);
        using var labelFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        graphics.DrawString(text, labelFont, labelBrush, bounds, labelFormat);
    }

    private static float EaseThemeToggle(float progress)
    {
        progress = Math.Clamp(progress, 0F, 1F);
        return progress * progress * (3F - 2F * progress);
    }

    private static Color BlendColor(Color from, Color to, float progress)
    {
        progress = Math.Clamp(progress, 0F, 1F);
        return Color.FromArgb(
            (int)Math.Round(from.A + (to.A - from.A) * progress),
            (int)Math.Round(from.R + (to.R - from.R) * progress),
            (int)Math.Round(from.G + (to.G - from.G) * progress),
            (int)Math.Round(from.B + (to.B - from.B) * progress));
    }

    private static void DrawSunIcon(Graphics graphics, RectangleF bounds, Color iconColor, float scale, float rotationDegrees)
    {
        var center = new PointF(bounds.Left + bounds.Width / 2F, bounds.Top + bounds.Height / 2F);
        var graphicsState = graphics.Save();
        graphics.TranslateTransform(center.X, center.Y);
        graphics.RotateTransform(rotationDegrees);
        graphics.ScaleTransform(scale, scale);

        using var rayPen = new Pen(iconColor, 2.1F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var coreBrush = new SolidBrush(iconColor);

        for (var index = 0; index < 8; index++)
        {
            var angle = Math.PI * 2 * index / 8;
            var inner = new PointF(
                (float)Math.Cos(angle) * 8.5F,
                (float)Math.Sin(angle) * 8.5F);
            var outer = new PointF(
                (float)Math.Cos(angle) * 13F,
                (float)Math.Sin(angle) * 13F);
            graphics.DrawLine(rayPen, inner, outer);
        }

        graphics.FillEllipse(coreBrush, -6.2F, -6.2F, 12.4F, 12.4F);
        graphics.Restore(graphicsState);
    }

    private static void DrawMoonIcon(Graphics graphics, RectangleF bounds, Color iconColor, Color backgroundColor)
    {
        var center = new PointF(bounds.Left + bounds.Width / 2F, bounds.Top + bounds.Height / 2F);
        using var moonBrush = new SolidBrush(iconColor);
        using var cutBrush = new SolidBrush(backgroundColor);
        using var starBrush = new SolidBrush(iconColor);

        graphics.FillEllipse(moonBrush, center.X - 9.5F, center.Y - 9.5F, 19F, 19F);
        graphics.FillEllipse(cutBrush, center.X - 12.6F, center.Y - 9.5F, 19F, 19F);
        graphics.FillPolygon(starBrush, CreateFourPointStar(center.X + 8.5F, center.Y - 7.2F, 3.8F));
        graphics.FillPolygon(starBrush, CreateFourPointStar(center.X + 12.7F, center.Y - 0.5F, 2.8F));
    }

    private static PointF[] CreateFourPointStar(float centerX, float centerY, float radius)
    {
        return
        [
            new PointF(centerX, centerY - radius),
            new PointF(centerX + radius * 0.32F, centerY - radius * 0.32F),
            new PointF(centerX + radius, centerY),
            new PointF(centerX + radius * 0.32F, centerY + radius * 0.32F),
            new PointF(centerX, centerY + radius),
            new PointF(centerX - radius * 0.32F, centerY + radius * 0.32F),
            new PointF(centerX - radius, centerY),
            new PointF(centerX - radius * 0.32F, centerY - radius * 0.32F)
        ];
    }

    private static void SetPillButtonRegion(Button button)
    {
        button.Region?.Dispose();
        using var path = CreateRoundedRectanglePath(
            new RectangleF(0, 0, button.Width - 1, button.Height - 1),
            (button.Height - 1) / 2F);
        button.Region = new Region(path);
    }

    private static GraphicsPath CreateRoundedRectanglePath(RectangleF bounds, float radius)
    {
        var diameter = radius * 2F;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static TextBox CreateTextBox(string placeholder, int width)
    {
        return new TextBox
        {
            Width = width,
            PlaceholderText = placeholder,
            BackColor = SurfaceBack,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.25F),
            Margin = new Padding(6, 5, 8, 4)
        };
    }

    private static ComboBox CreateComboBox(int width)
    {
        return new ComboBox
        {
            Width = width,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = SurfaceBack,
            ForeColor = TextColor,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.25F),
            Margin = new Padding(6, 4, 8, 4)
        };
    }

    private static CheckBox CreateCheckBox(string text)
    {
        return new CheckBox
        {
            Text = text,
            AutoSize = true,
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 9.25F),
            Margin = new Padding(6, 7, 10, 4)
        };
    }

    private static Label CreateCaption(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(8, 8, 0, 4)
        };
    }
}

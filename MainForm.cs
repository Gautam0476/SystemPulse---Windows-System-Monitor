using System.ComponentModel;
using System.Diagnostics;
using CustomTaskManager.Controls;
using CustomTaskManager.Models;
using CustomTaskManager.Services;
using System.Linq;

namespace CustomTaskManager;

public sealed class MainForm : Form
{
    private static readonly Color WindowBack = Color.FromArgb(18, 18, 20);
    private static readonly Color PanelBack = Color.FromArgb(28, 29, 32);
    private static readonly Color HeaderBack = Color.FromArgb(35, 36, 40);
    private static readonly Color Border = Color.FromArgb(55, 57, 62);
    private static readonly Color TextColor = Color.FromArgb(238, 239, 241);
    private static readonly Color MutedText = Color.FromArgb(169, 174, 181);
    private static readonly Color Accent = Color.FromArgb(42, 157, 143);
    private static readonly Color Danger = Color.FromArgb(198, 73, 70);

    private readonly ProcessMonitor _processMonitor = new();
    private readonly StartupManager _startupManager = new();
    private readonly RuleStore _ruleStore = new();
    private readonly RuleEngine _ruleEngine = new();
    private readonly HistoryStore _historyStore = new();
    private readonly SystemPerformanceMonitor _performanceMonitor = new();
    private readonly DashboardState _dashboardState = new();
    private readonly DashboardServer _dashboardServer;

    private readonly BindingSource _processSource = new();
    private readonly BindingSource _startupSource = new();
    private readonly BindingSource _rulesSource = new();
    private readonly BindingSource _historySource = new();
    private readonly BindingList<AutomationRule> _rules;
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 2000 };

    private List<ProcessSnapshot> _snapshots = [];
    private DateTime _lastHistoryWriteUtc = DateTime.MinValue;
    private bool _refreshingProcesses;
    private Guid? _editingRuleId;

    private Label _summaryLabel = null!;
    private TextBox _searchBox = null!;
    private Button _killButton = null!;
    private Button _openLocationButton = null!;
    private TreeView _processTree = null!;
    private DataGridView _processGrid = null!;
    private Label _dashboardUrlLabel = null!;
    private Button _openDashboardButton = null!;
    private PerformanceGraph _cpuGraph = null!;
    private PerformanceGraph _memoryGraph = null!;
    private Label _performanceCpuLabel = null!;
    private Label _performanceMemoryLabel = null!;
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
        WireEvents();

        _rulesSource.DataSource = _rules;
        _rulesGrid.DataSource = _rulesSource;

        RefreshStartupEntries();
        RefreshHistory();
        RefreshProcesses();
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
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = WindowBack
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        root.Controls.Add(BuildHeader(), 0, 0);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill
        };
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

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(14, 10, 14, 10),
            BackColor = HeaderBack
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 370));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "Custom Task Manager",
            Dock = DockStyle.Fill,
            ForeColor = TextColor,
            Font = new Font(Font.FontFamily, 15F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(title, 0, 0);

        _summaryLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(_summaryLabel, 1, 0);
        header.SetColumnSpan(_summaryLabel, 2);

        _searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Search process name, PID, or path",
            BackColor = PanelBack,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 8, 10, 0)
        };
        header.Controls.Add(_searchBox, 0, 1);
        header.SetColumnSpan(_searchBox, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = HeaderBack,
            Margin = new Padding(0, 4, 0, 0)
        };

        _killButton = CreateButton("Kill", Danger);
        _openLocationButton = CreateButton("Open location", Accent);
        var refreshButton = CreateButton("Refresh", Accent);
        refreshButton.Click += (_, _) => RefreshProcesses(manual: true);

        actions.Controls.Add(_killButton);
        actions.Controls.Add(_openLocationButton);
        actions.Controls.Add(refreshButton);
        header.Controls.Add(actions, 2, 1);

        return header;
    }

    private TabPage BuildProcessesTab()
    {
        var page = CreateTabPage("Processes");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BackColor = Border
        };
        split.HandleCreated += (_, _) => BeginInvoke(() => ConfigureProcessSplitter(split));
        split.SizeChanged += (_, _) => ConfigureProcessSplitter(split);

        _processTree = new TreeView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = PanelBack,
            ForeColor = TextColor,
            HideSelection = false
        };
        split.Panel1.Controls.Add(_processTree);

        _processGrid = CreateGrid();
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.Name), "Process", 190);
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.Id), "PID", 70);
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.ParentText), "Parent", 70);
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.CpuPercent), "CPU %", 80, "N1");
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.MemoryMb), "RAM MB", 90, "N1");
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.ThreadCount), "Threads", 75);
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.HandleCount), "Handles", 80);
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.Status), "Status", 120);
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.StartTimeText), "Started", 130);
        AddTextColumn(_processGrid, nameof(ProcessSnapshot.Path), "Path", 360, autoFill: true);
        _processGrid.DataSource = _processSource;
        split.Panel2.Controls.Add(_processGrid);

        page.Controls.Add(split);
        return page;
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
            BackColor = WindowBack
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38));

        var dashboardBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = WindowBack,
            Padding = new Padding(8, 7, 8, 7)
        };
        dashboardBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        dashboardBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

        _dashboardUrlLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Secure mobile dashboard starting...",
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        _openDashboardButton = CreateButton("Open dashboard", Accent);
        _openDashboardButton.Dock = DockStyle.Fill;
        _openDashboardButton.Enabled = false;

        dashboardBar.Controls.Add(_dashboardUrlLabel, 0, 0);
        dashboardBar.Controls.Add(_openDashboardButton, 1, 0);
        layout.Controls.Add(dashboardBar, 0, 0);

        var graphLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = WindowBack,
            Padding = new Padding(2)
        };
        graphLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        graphLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _cpuGraph = new PerformanceGraph
        {
            Dock = DockStyle.Fill,
            Caption = "CPU",
            LineColor = Accent,
            Margin = new Padding(6)
        };
        _memoryGraph = new PerformanceGraph
        {
            Dock = DockStyle.Fill,
            Caption = "Memory",
            LineColor = Color.FromArgb(233, 196, 106),
            Margin = new Padding(6)
        };
        graphLayout.Controls.Add(_cpuGraph, 0, 0);
        graphLayout.Controls.Add(_memoryGraph, 1, 0);
        layout.Controls.Add(graphLayout, 0, 1);

        var metricsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 5,
            BackColor = WindowBack,
            Padding = new Padding(2)
        };

        for (var column = 0; column < 5; column++)
        {
            metricsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        }

        metricsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        metricsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        metricsLayout.Controls.Add(CreateMetricCard("CPU", out _performanceCpuLabel), 0, 0);
        metricsLayout.Controls.Add(CreateMetricCard("Memory", out _performanceMemoryLabel), 1, 0);
        metricsLayout.Controls.Add(CreateMetricCard("Battery", out _performanceBatteryLabel), 2, 0);
        metricsLayout.Controls.Add(CreateMetricCard("Power", out _performancePowerLabel), 3, 0);
        metricsLayout.Controls.Add(CreateMetricCard("Processes", out _performanceProcessesLabel), 4, 0);
        metricsLayout.Controls.Add(CreateMetricCard("Threads", out _performanceThreadsLabel), 0, 1);
        metricsLayout.Controls.Add(CreateMetricCard("Handles", out _performanceHandlesLabel), 1, 1);
        metricsLayout.Controls.Add(CreateMetricCard("Uptime", out _performanceUptimeLabel), 2, 1);
        metricsLayout.Controls.Add(CreateMetricCard("Logical CPUs", out _performanceProcessorLabel), 3, 1);
        metricsLayout.Controls.Add(CreateMetricCard("OS", out _performanceOsLabel), 4, 1);

        layout.Controls.Add(metricsLayout, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildStartupTab()
    {
        var page = CreateTabPage("Startup");
        var layout = CreateTwoRowLayout(48);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(10, 7, 10, 7),
            BackColor = WindowBack
        };

        var refreshButton = CreateButton("Refresh", Accent);
        refreshButton.Click += (_, _) => RefreshStartupEntries();
        _startupToggleButton = CreateButton("Disable", Accent);
        toolbar.Controls.Add(refreshButton);
        toolbar.Controls.Add(_startupToggleButton);
        layout.Controls.Add(toolbar, 0, 0);

        _startupGrid = CreateGrid();
        AddTextColumn(_startupGrid, nameof(StartupEntry.Name), "Name", 190);
        AddTextColumn(_startupGrid, nameof(StartupEntry.State), "State", 90);
        AddTextColumn(_startupGrid, nameof(StartupEntry.Scope), "Scope", 140);
        AddTextColumn(_startupGrid, nameof(StartupEntry.Command), "Command", 360);
        AddTextColumn(_startupGrid, nameof(StartupEntry.RegistryPath), "Source", 320, autoFill: true);
        _startupGrid.DataSource = _startupSource;
        layout.Controls.Add(_startupGrid, 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildRulesTab()
    {
        var page = CreateTabPage("Rules");
        var layout = CreateTwoRowLayout(86);

        var editor = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10, 9, 10, 6),
            BackColor = WindowBack,
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
            BackColor = PanelBack,
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
        AddTextColumn(_rulesGrid, nameof(AutomationRule.ProcessNameContains), "Process contains", 190);
        AddTextColumn(_rulesGrid, nameof(AutomationRule.Metric), "Metric", 110);
        AddTextColumn(_rulesGrid, nameof(AutomationRule.Threshold), "Threshold", 100, "N1");
        AddTextColumn(_rulesGrid, nameof(AutomationRule.Action), "Action", 120);
        AddTextColumn(_rulesGrid, nameof(AutomationRule.LastTriggeredText), "Last triggered", 150, autoFill: true);
        layout.Controls.Add(_rulesGrid, 0, 1);

        page.Controls.Add(layout);
        ClearRuleEditor();
        return page;
    }

    private TabPage BuildHistoryTab()
    {
        var page = CreateTabPage("History");
        var layout = CreateTwoRowLayout(48);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(10, 7, 10, 7),
            BackColor = WindowBack
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
        AddTextColumn(_historyGrid, nameof(HistorySummary.Name), "Process", 220);
        AddTextColumn(_historyGrid, nameof(HistorySummary.Samples), "Samples", 90);
        AddTextColumn(_historyGrid, nameof(HistorySummary.AverageCpu), "Avg CPU %", 100, "N1");
        AddTextColumn(_historyGrid, nameof(HistorySummary.AverageMemoryMb), "Avg RAM MB", 110, "N1");
        AddTextColumn(_historyGrid, nameof(HistorySummary.MaxMemoryMb), "Max RAM MB", 120, "N1", autoFill: true);
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
            Padding = new Padding(8)
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
            BackColor = PanelBack,
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
            Padding = new Padding(8)
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
            BackColor = PanelBack,
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

        var metricGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 2,
            BackColor = WindowBack
        };
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

        metricGrid.Controls.Add(CreateMetricCard("CPU Load", out _visualizationCpuLabel), 0, 0);
        metricGrid.Controls.Add(CreateMetricCard("GPU Load", out _visualizationGpuLabel), 1, 0);
        metricGrid.Controls.Add(CreateMetricCard("Memory Load", out _visualizationMemoryLabel), 0, 1);
        metricGrid.Controls.Add(CreateMetricCard("Battery Risk", out _visualizationBatteryLabel), 1, 1);
        metricGrid.Controls.Add(CreateMetricCard("Process Load", out _visualizationProcessLabel), 0, 2);
        analysisPanel.Controls.Add(metricGrid, 0, 1);

        _visualizationRecommendationLabel = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBack,
            ForeColor = TextColor,
            Padding = new Padding(14, 10, 14, 10),
            Text = "System recommendation will appear here.",
            TextAlign = ContentAlignment.MiddleLeft
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
        _refreshTimer.Tick += (_, _) => RefreshProcesses();
        _searchBox.TextChanged += (_, _) => ApplyProcessFilter();
        _killButton.Click += (_, _) => KillSelectedProcess();
        _openLocationButton.Click += (_, _) => OpenSelectedProcessLocation();
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
        Shown += async (_, _) => await StartDashboardServerAsync();
        FormClosing += async (_, _) => await _dashboardServer.DisposeAsync();
    }

    private void RefreshProcesses(bool manual = false)
    {
        if (_refreshingProcesses)
        {
            return;
        }

        _refreshingProcesses = true;
        var selectedPid = SelectedProcess?.Id;

        try
        {
            _snapshots = _processMonitor.Capture();
            ApplyProcessFilter(selectedPid);
            RefreshProcessTree(selectedPid);
            UpdateSummary();
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

    private void RefreshPerformance()
    {
        var snapshot = _performanceMonitor.Capture(_snapshots);
        _dashboardState.Update(snapshot, _snapshots);

        _cpuGraph.AddValue(snapshot.CpuPercent);
        _memoryGraph.AddValue(snapshot.MemoryPercent);

        _performanceCpuLabel.Text = $"{snapshot.CpuPercent:N1}%";
        _performanceMemoryLabel.Text = $"{snapshot.MemoryUsedGb:N1} / {snapshot.MemoryTotalGb:N1} GB ({snapshot.MemoryPercent:N1}%)";
        _performanceBatteryLabel.Text = snapshot.Battery.Summary;
        _performancePowerLabel.Text = FormatPowerStatus(snapshot.Battery);
        _performanceProcessesLabel.Text = snapshot.ProcessCount.ToString("N0");
        _performanceThreadsLabel.Text = snapshot.ThreadCount.ToString("N0");
        _performanceHandlesLabel.Text = snapshot.HandleCount.ToString("N0");
        _performanceUptimeLabel.Text = FormatUptime(snapshot.Uptime);
        _performanceProcessorLabel.Text = snapshot.ProcessorCount.ToString("N0");
        _performanceOsLabel.Text = snapshot.OsVersion.Replace("Microsoft Windows ", "Windows ");

        RefreshVisualization(snapshot);
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

    private void KillSelectedProcess()
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
            RefreshProcesses();
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
            _startupToggleButton.BackColor = entry.Enabled ? Danger : Accent;
            return;
        }

        _startupToggleButton.Enabled = false;
        _startupToggleButton.Text = "Disable";
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

    private static Control CreateMetricCard(string title, out Label valueLabel)
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = PanelBack,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(6)
        };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        };

        valueLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "-",
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
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
            Padding = new Padding(8)
        };
    }

    private static TableLayoutPanel CreateTwoRowLayout(int topHeight)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = WindowBack
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
            BorderStyle = BorderStyle.FixedSingle,
            BackgroundColor = WindowBack,
            GridColor = Border,
            EnableHeadersVisualStyles = false
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBack;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderBack;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextColor;
        grid.DefaultCellStyle.BackColor = PanelBack;
        grid.DefaultCellStyle.ForeColor = TextColor;
        grid.DefaultCellStyle.SelectionBackColor = Accent;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(33, 34, 38);
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.RowTemplate.Height = 28;
        return grid;
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
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 31,
            MinimumSize = new Size(78, 31),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(6, 2, 0, 2)
        };
        button.FlatAppearance.BorderColor = backColor;
        return button;
    }

    private static TextBox CreateTextBox(string placeholder, int width)
    {
        return new TextBox
        {
            Width = width,
            PlaceholderText = placeholder,
            BackColor = PanelBack,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(6, 5, 8, 4)
        };
    }

    private static ComboBox CreateComboBox(int width)
    {
        return new ComboBox
        {
            Width = width,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = PanelBack,
            ForeColor = TextColor,
            FlatStyle = FlatStyle.Flat,
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

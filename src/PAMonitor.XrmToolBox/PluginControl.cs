using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using PAMonitor.XrmToolBox.Controls;
using PAMonitor.XrmToolBox.Models;
using PAMonitor.XrmToolBox.Services;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace PAMonitor.XrmToolBox
{
    public partial class PluginControl : PluginControlBase, ISettingsPlugin
    {
        private Settings _settings;
        private FlowRunQueryService _queryService;
        private FlowManagementApiClient _flowApi;
        private FlowTokenProvider _flowTokenProvider;
        private string _environmentId;
        private Guid _tenantId;
        private FlowRunInfo _selectedRun;
        private int _detailLoadVersion;
        private readonly List<FlowDefinition> _flows = new List<FlowDefinition>();
        private readonly List<FlowRunInfo> _currentRuns = new List<FlowRunInfo>();
        private readonly List<SolutionDefinition> _allSolutions = new List<SolutionDefinition>();
        private IReadOnlyList<FlowRunInfo> _treeRelatedRuns = Array.Empty<FlowRunInfo>();
        private int _treeLoadVersion;

        private ToolStrip _toolStrip;
        private ToolStripButton _btnRefreshSolutions;
        private ToolStripButton _btnLoadFlows;
        private ToolStripButton _btnSearch;
        private ToolStripButton _btnExpandFailed;
        private ToolStripButton _btnOpenRun;
        private ToolStripButton _btnFlowApiSettings;
        private Timer _debounceSolutions;
        private Timer _debounceFlows;

        private SearchableCheckedListPanel _pnlSolutions;
        private SearchableCheckedListPanel _pnlFlows;
        private ComboBox _cboStatus;
        private DateTimePicker _dtFrom;
        private DateTimePicker _dtTo;
        private NumericUpDown _numTop;
        private CheckBox _chkFrom;
        private CheckBox _chkTo;
        private TextBox _txtRunId;

        private ListView _lvRuns;
        private TreeView _tvTree;
        private RichTextBox _txtDetails;
        private System.Windows.Forms.Label _lblStatus;

        public PluginControl()
        {
            InitializeComponent();
            LoadSettings();
        }

        public void ShowSettings()
        {
            OpenFlowApiSettings();
        }

        private void LoadSettings()
        {
            if (!SettingsManager.Instance.TryLoad(GetType(), out _settings) || _settings == null)
            {
                _settings = new Settings();
            }
        }

        private void SaveSettings()
        {
            SettingsManager.Instance.Save(GetType(), _settings ?? new Settings());
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Name = "PluginControl";
            Size = new Size(1200, 700);

            _toolStrip = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Dock = DockStyle.Top
            };
            _btnRefreshSolutions = new ToolStripButton("Refresh solutions")
            {
                Image = ToolbarIcons.RefreshSolutions,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Reload the solutions list from Dataverse."
            };
            _btnLoadFlows = new ToolStripButton("Refresh flows")
            {
                Image = ToolbarIcons.RefreshFlows,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Reload flows (optional; selecting a solution also loads flows)."
            };
            _btnSearch = new ToolStripButton("Refresh runs")
            {
                Image = ToolbarIcons.RefreshRuns,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Reload runs with current filters (optional; selecting a flow also loads runs)."
            };
            _btnExpandFailed = new ToolStripButton("Expand failures")
            {
                Image = ToolbarIcons.ExpandFailures,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "In the run tree, expand nodes marked Failed so you can quickly see which child flow failed."
            };
            _btnOpenRun = new ToolStripButton("Open run")
            {
                Image = ToolbarIcons.OpenRun,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Open the selected run in Power Automate (browser)."
            };
            _btnFlowApiSettings = new ToolStripButton("Flow API settings")
            {
                Image = ToolbarIcons.FlowApiSettings,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Configure Entra app Client Id for action-level error details."
            };
            _btnRefreshSolutions.Click += (_, __) => ExecuteMethod(LoadSolutions);
            _btnLoadFlows.Click += (_, __) => ExecuteMethod(() => LoadFlows(requireSelectedSolutions: false));
            _btnSearch.Click += (_, __) => ExecuteMethod(() => SearchRuns(requireSelectedFlows: false));
            _btnExpandFailed.Click += (_, __) => ExpandFailedNodes();
            _btnOpenRun.Click += (_, __) => OpenSelectedRunInBrowser();
            _btnFlowApiSettings.Click += (_, __) => OpenFlowApiSettings();
            _toolStrip.Items.AddRange(new ToolStripItem[]
            {
                _btnRefreshSolutions,
                _btnLoadFlows,
                new ToolStripSeparator(),
                _btnSearch,
                _btnExpandFailed,
                _btnOpenRun,
                new ToolStripSeparator(),
                _btnFlowApiSettings
            });

            _debounceSolutions = new Timer { Interval = 350 };
            _debounceSolutions.Tick += DebounceSolutions_Tick;
            _debounceFlows = new Timer { Interval = 350 };
            _debounceFlows.Tick += DebounceFlows_Tick;

            var filters = BuildFiltersPanel();
            var mainSplit = BuildMainSplit();

            _lblStatus = new System.Windows.Forms.Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Text = "Connect to an environment to load solutions."
            };

            Controls.Add(mainSplit);
            Controls.Add(filters);
            Controls.Add(_lblStatus);
            Controls.Add(_toolStrip);

            ResumeLayout(false);
            PerformLayout();
        }

        private Panel BuildFiltersPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 300,
                Padding = new Padding(8, 6, 8, 4)
            };

            var listsSplit = new SplitContainer
            {
                Dock = DockStyle.Top,
                Height = 210,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };

            _pnlSolutions = new SearchableCheckedListPanel
            {
                Dock = DockStyle.Fill,
                Title = "Solutions — select to load flows"
            };
            _pnlFlows = new SearchableCheckedListPanel
            {
                Dock = DockStyle.Fill,
                Title = "Flows — select to load runs"
            };
            _pnlSolutions.SelectionChanged += (_, __) => ScheduleSolutionDrivenLoad();
            _pnlFlows.SelectionChanged += (_, __) => ScheduleFlowDrivenSearch();

            listsSplit.Panel1.Controls.Add(_pnlSolutions);
            listsSplit.Panel2.Controls.Add(_pnlFlows);
            listsSplit.SizeChanged += (_, __) => SafeSetSplitterDistance(listsSplit, 0.5);

            var criteria = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 0)
            };

            var lblStatus = new System.Windows.Forms.Label { Text = "Status", Left = 0, Top = 6, AutoSize = true };
            _cboStatus = new ComboBox
            {
                Left = 50,
                Top = 2,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboStatus.Items.AddRange(Enum.GetNames(typeof(FlowRunStatus)));
            _cboStatus.SelectedIndex = 0;

            _chkFrom = new CheckBox { Text = "From", Left = 210, Top = 4, AutoSize = true, Checked = true };
            _dtFrom = new DateTimePicker
            {
                Left = 270,
                Top = 2,
                Width = 160,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                Value = DateTime.Now.Date.AddDays(-1)
            };

            _chkTo = new CheckBox { Text = "To", Left = 450, Top = 4, AutoSize = true, Checked = true };
            _dtTo = new DateTimePicker
            {
                Left = 495,
                Top = 2,
                Width = 160,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                Value = DateTime.Now.Date.AddDays(1).AddSeconds(-1)
            };

            var lblTop = new System.Windows.Forms.Label { Text = "Max results", Left = 680, Top = 6, AutoSize = true };
            _numTop = new NumericUpDown
            {
                Left = 760,
                Top = 2,
                Width = 80,
                Minimum = 10,
                Maximum = 5000,
                Value = 100,
                Increment = 50
            };

            var lblRunId = new System.Windows.Forms.Label
            {
                Text = "Run Id / Workflow Id",
                Left = 0,
                Top = 36,
                AutoSize = true
            };
            _txtRunId = new TextBox
            {
                Left = 130,
                Top = 32,
                Width = 340,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _txtRunId.TextChanged += (_, __) => ApplyClientRunIdFilter();

            var hint = new System.Windows.Forms.Label
            {
                Left = 490,
                Top = 36,
                AutoSize = true,
                Text = "Filters by Run Id (GUID), Logic Apps run name, or Workflow Id. Status rows are color-coded."
            };

            criteria.Controls.AddRange(new Control[]
            {
                lblStatus, _cboStatus,
                _chkFrom, _dtFrom,
                _chkTo, _dtTo,
                lblTop, _numTop,
                lblRunId, _txtRunId,
                hint
            });

            panel.Controls.Add(criteria);
            panel.Controls.Add(listsSplit);

            return panel;
        }

        private SplitContainer BuildMainSplit()
        {
            var horizontal = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel1MinSize = 80,
                Panel2MinSize = 80
            };

            _lvRuns = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false
            };
            _lvRuns.Columns.Add("Start", 140);
            _lvRuns.Columns.Add("Flow", 200);
            _lvRuns.Columns.Add("Status", 80);
            _lvRuns.Columns.Add("Duration", 70);
            _lvRuns.Columns.Add("Error", 220);
            _lvRuns.Columns.Add("Open", 70);
            _lvRuns.Columns.Add("Run Id", 220);
            _lvRuns.SelectedIndexChanged += LvRuns_SelectedIndexChanged;
            _lvRuns.DoubleClick += (_, __) => OpenSelectedRunInBrowser();
            _lvRuns.MouseClick += LvRuns_MouseClick;

            var openMenu = new ContextMenuStrip();
            openMenu.Items.Add("Open run in Power Automate", ToolbarIcons.OpenRun, (_, __) => OpenSelectedRunInBrowser());
            openMenu.Items.Add("Copy run URL", ToolbarIcons.Copy, (_, __) => CopySelectedRunUrl());
            _lvRuns.ContextMenuStrip = openMenu;

            var rightSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };

            _tvTree = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                ShowNodeToolTips = true
            };
            _tvTree.AfterSelect += TvTree_AfterSelect;
            _tvTree.BeforeExpand += TvTree_BeforeExpand;
            _tvTree.NodeMouseDoubleClick += (_, e) =>
            {
                if (e.Node?.Tag is FlowRunInfo run)
                {
                    OpenRunInBrowser(run);
                }
            };

            _txtDetails = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = true,
                Font = new Font("Consolas", 9.5f),
                WordWrap = false,
                BackColor = SystemColors.Window
            };
            _txtDetails.LinkClicked += (_, e) => OpenUrl(e.LinkText);

            rightSplit.Panel1.Controls.Add(_tvTree);
            rightSplit.Panel2.Controls.Add(_txtDetails);
            rightSplit.SizeChanged += (_, __) => SafeSetSplitterDistance(rightSplit, 0.55);

            horizontal.Panel1.Controls.Add(_lvRuns);
            horizontal.Panel2.Controls.Add(rightSplit);
            horizontal.SizeChanged += (_, __) => SafeSetSplitterDistance(horizontal, 0.55);

            return horizontal;
        }

        private static void SafeSetSplitterDistance(SplitContainer split, double ratio)
        {
            if (split == null || split.IsDisposed || !split.IsHandleCreated)
            {
                return;
            }

            var total = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
            var min1 = split.Panel1MinSize;
            var min2 = split.Panel2MinSize;
            var splitter = split.SplitterWidth;
            var max = total - min2 - splitter;
            if (max <= min1)
            {
                return;
            }

            var desired = (int)(total * ratio);
            if (desired < min1)
            {
                desired = min1;
            }
            else if (desired > max)
            {
                desired = max;
            }

            try
            {
                if (Math.Abs(split.SplitterDistance - desired) > 2)
                {
                    split.SplitterDistance = desired;
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore layout races while the host is still sizing the tool.
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            _queryService = newService != null ? new FlowRunQueryService(newService) : null;
            _flowApi = null;
            _flowTokenProvider = null;
            _environmentId = null;
            _tenantId = Guid.Empty;
            _allSolutions.Clear();
            _pnlSolutions.Clear();
            _pnlFlows.Clear();
            _flows.Clear();

            if (detail == null || _queryService == null)
            {
                _lblStatus.Text = "Not connected";
                return;
            }

            _environmentId = detail.EnvironmentId;
            _tenantId = detail.TenantId;
            TryInitializeFlowApi(newService, detail);

            _lblStatus.Text = $"Connected to: {detail.ConnectionName}. Loading solutions...";
            ExecuteMethod(LoadSolutions);
        }

        private bool TryInitializeFlowApi(IOrganizationService service, ConnectionDetail detail)
        {
            try
            {
                if (_settings == null)
                {
                    LoadSettings();
                }

                if (string.IsNullOrWhiteSpace(_settings.FlowAppClientId))
                {
                    _flowApi = null;
                    _flowTokenProvider = null;
                    return false;
                }

                var ctx = EnvironmentContextResolver.Resolve(
                    service,
                    detail?.EnvironmentId,
                    detail?.TenantId ?? Guid.Empty);

                if (ctx == null)
                {
                    _flowApi = null;
                    _flowTokenProvider = null;
                    return false;
                }

                _environmentId = ctx.EnvironmentId;
                _tenantId = ctx.TenantId;
                _flowTokenProvider = new FlowTokenProvider(
                    _tenantId,
                    _settings.FlowAppClientId,
                    _settings.FlowRedirectUri);
                _flowApi = new FlowManagementApiClient(_flowTokenProvider, _environmentId);
                return true;
            }
            catch
            {
                _flowApi = null;
                _flowTokenProvider = null;
                return false;
            }
        }

        private void OpenFlowApiSettings()
        {
            if (_settings == null)
            {
                LoadSettings();
            }

            using (var dlg = new FlowApiSettingsForm(_settings))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _settings.FlowAppClientId = dlg.ClientId;
                _settings.FlowRedirectUri = string.IsNullOrWhiteSpace(dlg.RedirectUri)
                    ? "http://localhost"
                    : dlg.RedirectUri;
                SaveSettings();

                _flowApi = null;
                _flowTokenProvider = null;
                if (Service != null && ConnectionDetail != null)
                {
                    TryInitializeFlowApi(Service, ConnectionDetail);
                }

                _lblStatus.Text = string.IsNullOrWhiteSpace(_settings.FlowAppClientId)
                    ? "Flow API Client Id cleared."
                    : "Flow API Client Id saved. Select a run to load action details.";
            }
        }

        private void LoadSolutions()
        {
            if (_queryService == null)
            {
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solutions...",
                Work = (worker, args) =>
                {
                    args.Result = _queryService.GetSolutions();
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(this, args.Error.ToString(), "Error loading solutions",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _lblStatus.Text = "Failed to load solutions.";
                        return;
                    }

                    var solutions = (IReadOnlyList<SolutionDefinition>)args.Result;
                    _allSolutions.Clear();
                    _allSolutions.AddRange(solutions);
                    _pnlSolutions.SetItems(
                        solutions,
                        s => s.SolutionId,
                        s => s.ToString());
                    _lblStatus.Text = $"{solutions.Count} solutions ready. Select a solution to load its flows.";
                }
            });
        }

        private void ScheduleSolutionDrivenLoad()
        {
            _debounceSolutions.Stop();
            _debounceSolutions.Start();
        }

        private void ScheduleFlowDrivenSearch()
        {
            _debounceFlows.Stop();
            _debounceFlows.Start();
        }

        private void DebounceSolutions_Tick(object sender, EventArgs e)
        {
            _debounceSolutions.Stop();
            if (_queryService == null)
            {
                return;
            }

            if (_pnlSolutions.SelectedCount == 0)
            {
                _pnlFlows.Clear();
                _flows.Clear();
                ClearRunsUi("Select one or more solutions to load flows.");
                return;
            }

            ExecuteMethod(() => LoadFlows(requireSelectedSolutions: true));
        }

        private void DebounceFlows_Tick(object sender, EventArgs e)
        {
            _debounceFlows.Stop();
            if (_queryService == null)
            {
                return;
            }

            if (_pnlFlows.SelectedCount == 0)
            {
                ClearRunsUi("Select one or more flows to load runs.");
                return;
            }

            ExecuteMethod(() => SearchRuns(requireSelectedFlows: true));
        }

        private void ClearRunsUi(string statusMessage = null)
        {
            _currentRuns.Clear();
            _lvRuns.Items.Clear();
            _tvTree.Nodes.Clear();
            _txtDetails.Clear();
            _selectedRun = null;
            if (!string.IsNullOrEmpty(statusMessage))
            {
                _lblStatus.Text = statusMessage;
            }
        }

        private void LoadFlows(bool requireSelectedSolutions)
        {
            if (_queryService == null)
            {
                MessageBox.Show(this, "Connect to an environment first.", "PA Run Monitor",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var solutionIds = _pnlSolutions.GetSelectedIds().ToArray();
            if (requireSelectedSolutions && solutionIds.Length == 0)
            {
                _pnlFlows.Clear();
                _flows.Clear();
                ClearRunsUi("Select one or more solutions to load flows.");
                return;
            }

            var message = solutionIds.Length > 0
                ? "Loading flows from selected solutions..."
                : "Loading all cloud flows...";

            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (worker, args) =>
                {
                    args.Result = _queryService.GetCloudFlows(solutionIds.Length > 0 ? solutionIds : null);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(this, args.Error.ToString(), "Error loading flows",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _flows.Clear();
                    _flows.AddRange((IReadOnlyList<FlowDefinition>)args.Result);
                    _pnlFlows.SetItems(_flows, f => f.WorkflowId, f => f.Name);
                    ClearRunsUi();

                    var scope = solutionIds.Length > 0 ? $" (from {solutionIds.Length} solution(s))" : "";
                    _lblStatus.Text = $"{_flows.Count} flows loaded{scope}. Select a flow to load runs.";
                }
            });
        }

        private void SearchRuns(bool requireSelectedFlows)
        {
            if (_queryService == null)
            {
                MessageBox.Show(this, "Connect to an environment first.", "PA Run Monitor",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedFlows = _pnlFlows.GetSelectedIds().ToArray();
            if (requireSelectedFlows && selectedFlows.Length == 0)
            {
                ClearRunsUi("Select one or more flows to load runs.");
                return;
            }

            var filter = BuildFilter();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Searching runs...",
                Work = (worker, args) =>
                {
                    args.Result = _queryService.GetRuns(filter);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(this,
                            "Error querying flowrun. The environment schema may differ.\n\n" +
                            args.Error.Message,
                            "Error searching runs",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _currentRuns.Clear();
                    _currentRuns.AddRange((IReadOnlyList<FlowRunInfo>)args.Result);
                    BindRuns(GetVisibleRuns());
                    _tvTree.Nodes.Clear();
                    _txtDetails.Clear();
                    _selectedRun = null;
                    _lblStatus.Text = $"{GetVisibleRuns().Count()} of {_currentRuns.Count} runs shown.";
                }
            });
        }

        private FlowRunFilter BuildFilter()
        {
            Enum.TryParse(_cboStatus.SelectedItem?.ToString(), out FlowRunStatus status);

            return new FlowRunFilter
            {
                WorkflowIds = _pnlFlows.GetSelectedIds().ToArray(),
                Status = status,
                FromUtc = _chkFrom.Checked ? _dtFrom.Value.ToUniversalTime() : (DateTime?)null,
                ToUtc = _chkTo.Checked ? _dtTo.Value.ToUniversalTime() : (DateTime?)null,
                Top = (int)_numTop.Value,
                RunIdText = (_txtRunId.Text ?? string.Empty).Trim()
            };
        }

        private IEnumerable<FlowRunInfo> GetVisibleRuns()
        {
            var text = (_txtRunId.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text))
            {
                return _currentRuns;
            }

            return _currentRuns.Where(r => MatchesRunIdFilter(r, text));
        }

        private static bool MatchesRunIdFilter(FlowRunInfo run, string text)
        {
            if (run == null || string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (Guid.TryParse(text, out var guid))
            {
                if (run.RunId == guid || run.WorkflowId == guid)
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(run.RunName)
                && run.RunName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (run.WorkflowId != Guid.Empty
                && (run.WorkflowId.ToString("D").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                    || run.WorkflowId.ToString("N").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }

            return run.RunId.ToString("D").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                   || run.RunId.ToString("N").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ApplyClientRunIdFilter()
        {
            BindRuns(GetVisibleRuns());
            _lblStatus.Text = $"{GetVisibleRuns().Count()} of {_currentRuns.Count} runs shown.";
        }

        private void BindRuns(IEnumerable<FlowRunInfo> runs)
        {
            _lvRuns.BeginUpdate();
            _lvRuns.Items.Clear();

            foreach (var run in runs)
            {
                var item = new ListViewItem(run.StartTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "")
                {
                    Tag = run,
                    UseItemStyleForSubItems = true
                };
                item.SubItems.Add(run.FlowName ?? run.RunName ?? "");
                item.SubItems.Add(run.Status ?? "");
                item.SubItems.Add(run.Duration?.ToString(@"hh\:mm\:ss") ?? "");
                item.SubItems.Add(Truncate(run.ErrorMessage, 120));
                item.SubItems.Add("Open");
                item.SubItems.Add(run.RunId.ToString());

                ApplyStatusColors(item, run.Status);
                _lvRuns.Items.Add(item);
            }

            _lvRuns.EndUpdate();
        }

        private static void ApplyStatusColors(ListViewItem item, string status)
        {
            if (item == null)
            {
                return;
            }

            Color fore;
            Color back;
            switch ((status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "succeeded":
                case "success":
                    fore = Color.FromArgb(0, 100, 0);
                    back = Color.FromArgb(220, 245, 220);
                    break;
                case "failed":
                case "faulted":
                    fore = Color.FromArgb(139, 0, 0);
                    back = Color.FromArgb(255, 228, 225);
                    break;
                case "cancelled":
                case "canceled":
                    fore = Color.FromArgb(90, 90, 90);
                    back = Color.FromArgb(235, 235, 235);
                    break;
                case "running":
                case "inprogress":
                case "in progress":
                    fore = Color.FromArgb(0, 70, 140);
                    back = Color.FromArgb(220, 235, 255);
                    break;
                case "waiting":
                case "paused":
                    fore = Color.FromArgb(150, 90, 0);
                    back = Color.FromArgb(255, 243, 205);
                    break;
                case "skipped":
                    fore = Color.FromArgb(110, 110, 110);
                    back = Color.FromArgb(245, 245, 245);
                    break;
                default:
                    fore = SystemColors.WindowText;
                    back = SystemColors.Window;
                    break;
            }

            item.ForeColor = fore;
            item.BackColor = back;
        }

        private void LvRuns_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var hit = _lvRuns.HitTest(e.Location);
            if (hit.Item == null || hit.SubItem == null)
            {
                return;
            }

            // Column index 5 = "Open"
            if (hit.Item.SubItems.IndexOf(hit.SubItem) == 5 && hit.Item.Tag is FlowRunInfo run)
            {
                OpenRunInBrowser(run);
            }
        }

        private void LvRuns_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lvRuns.SelectedItems.Count == 0)
            {
                return;
            }

            var run = _lvRuns.SelectedItems[0].Tag as FlowRunInfo;
            if (run == null)
            {
                return;
            }

            _selectedRun = run;
            ShowDetails(run);
            LoadRunTree(run);
            LoadActionDetails(run);
        }

        private void LoadActionDetails(FlowRunInfo run)
        {
            if (run == null)
            {
                return;
            }

            if (_flowApi == null)
            {
                if (_settings == null)
                {
                    LoadSettings();
                }

                if (string.IsNullOrWhiteSpace(_settings.FlowAppClientId))
                {
                    AppendDetailsNote(
                        "Action-level error details are not available because Flow API settings are not configured.\r\n" +
                        "You can configure them anytime with «Flow API settings» on the toolbar.");

                    if (!_settings.FlowApiSettingsPromptShown)
                    {
                        _settings.FlowApiSettingsPromptShown = true;
                        SaveSettings();

                        var answer = MessageBox.Show(this,
                            "Flow API is not configured.\n\n" +
                            "Without this setup, the tool cannot load action-level error details " +
                            "(you will only see the generic Dataverse ActionFailed summary).\n\n" +
                            "Do you want to configure Flow API settings now?",
                            "PA Run Monitor",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (answer == DialogResult.Yes)
                        {
                            OpenFlowApiSettings();
                            if (_flowApi == null && !TryInitializeFlowApi(Service, ConnectionDetail))
                            {
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show(this,
                                "Understood. You can configure Flow API settings whenever you want by clicking " +
                                "«Flow API settings» on the top toolbar.",
                                "PA Run Monitor",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else if (!TryInitializeFlowApi(Service, ConnectionDetail))
                {
                    AppendDetailsNote(
                        "Action-level details unavailable: could not resolve EnvironmentId/TenantId from the connection or Dataverse.");
                    return;
                }
            }

            if (_flowApi == null && !TryInitializeFlowApi(Service, ConnectionDetail))
            {
                AppendDetailsNote("Action-level details unavailable: Flow API client was not initialized.");
                return;
            }

            if (string.IsNullOrWhiteSpace(run.RunName) || run.WorkflowId == Guid.Empty)
            {
                AppendDetailsNote("Action-level details unavailable: missing run name or workflow id.");
                return;
            }

            var loadVersion = ++_detailLoadVersion;
            var flowId = !string.IsNullOrWhiteSpace(run.ResourceId)
                ? run.ResourceId
                : run.WorkflowId.ToString("D");
            var runName = run.RunName;

            AppendDetailsNote("Loading action-level details from Flow API...");

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading action details...",
                Work = (worker, args) =>
                {
                    args.Result = _flowApi.GetRunActionsAsync(flowId, runName).GetAwaiter().GetResult();
                },
                PostWorkCallBack = args =>
                {
                    if (loadVersion != _detailLoadVersion || !ReferenceEquals(_selectedRun, run))
                    {
                        return;
                    }

                    if (args.Error != null)
                    {
                        var message = args.Error.Message ?? string.Empty;
                        AppendDetailsNote("Failed to load action details:\r\n" + message);
                        return;
                    }

                    var actions = (IReadOnlyList<FlowActionInfo>)args.Result;
                    run.Actions = actions;
                    run.DetailedErrorMessage = FlowManagementApiClient.BuildDetailedErrorSummary(actions);
                    ShowDetails(run);
                }
            });
        }

        private void AppendDetailsNote(string note)
        {
            if (string.IsNullOrEmpty(_txtDetails.Text))
            {
                _txtDetails.Text = note;
                return;
            }

            _txtDetails.Text = _txtDetails.Text.TrimEnd() + Environment.NewLine + Environment.NewLine + note;
        }

        private void LoadRunTree(FlowRunInfo root)
        {
            if (root == null || _queryService == null)
            {
                return;
            }

            var loadVersion = ++_treeLoadVersion;
            _treeRelatedRuns = Array.Empty<FlowRunInfo>();

            _tvTree.BeginUpdate();
            _tvTree.Nodes.Clear();
            var loadingNode = new TreeNode("Loading run tree…");
            _tvTree.Nodes.Add(loadingNode);
            _tvTree.EndUpdate();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading nested flow runs...",
                Work = (worker, args) =>
                {
                    IReadOnlyList<FlowRunInfo> related;
                    if (!string.IsNullOrWhiteSpace(root.ClientTrackingId))
                    {
                        related = _queryService.GetRunsByClientTrackingId(root.ClientTrackingId);
                    }
                    else
                    {
                        related = Array.Empty<FlowRunInfo>();
                    }

                    args.Result = related;
                },
                PostWorkCallBack = args =>
                {
                    if (loadVersion != _treeLoadVersion || !ReferenceEquals(_selectedRun, root))
                    {
                        return;
                    }

                    _tvTree.BeginUpdate();
                    _tvTree.Nodes.Clear();

                    if (args.Error != null)
                    {
                        var errorNode = CreateRunNode(root);
                        errorNode.Nodes.Add(new TreeNode("Error loading tree: " + args.Error.Message));
                        _tvTree.Nodes.Add(errorNode);
                        _tvTree.EndUpdate();
                        return;
                    }

                    var related = (IReadOnlyList<FlowRunInfo>)args.Result ?? Array.Empty<FlowRunInfo>();
                    if (related.Count > 0)
                    {
                        _treeRelatedRuns = related;
                        var rootNode = CreateRunNode(root);
                        PopulateChildNodes(rootNode, root, related);
                        _tvTree.Nodes.Add(rootNode);
                        rootNode.Expand();
                        _tvTree.EndUpdate();
                        return;
                    }

                    _treeRelatedRuns = Array.Empty<FlowRunInfo>();
                    var lazyRootNode = CreateRunNode(root);
                    lazyRootNode.Nodes.Add(new TreeNode("…") { Tag = "lazy" });
                    _tvTree.Nodes.Add(lazyRootNode);
                    lazyRootNode.Expand();
                    _tvTree.EndUpdate();
                }
            });
        }

        private void PopulateChildNodes(TreeNode parentNode, FlowRunInfo parentRun, IReadOnlyList<FlowRunInfo> relatedRuns)
        {
            var children = FlowRunTreeBuilder.GetChildren(parentRun, relatedRuns);
            foreach (var child in children)
            {
                var childNode = CreateRunNode(child);
                PopulateChildNodes(childNode, child, relatedRuns);
                parentNode.Nodes.Add(childNode);
            }
        }

        private TreeNode CreateRunNode(FlowRunInfo run)
        {
            var label = $"{run.FlowName ?? run.RunName ?? "Run"} [{run.Status}]";
            var node = new TreeNode(label)
            {
                Tag = run,
                ToolTipText = run.ErrorMessage ?? run.RunId.ToString(),
                ForeColor = string.Equals(run.Status, "Failed", StringComparison.OrdinalIgnoreCase)
                    ? Color.Firebrick
                    : SystemColors.WindowText
            };
            return node;
        }

        private void TvTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count != 1 || !(e.Node.Nodes[0].Tag is string tag) || tag != "lazy")
            {
                return;
            }

            var parentRun = e.Node.Tag as FlowRunInfo;
            if (parentRun == null || _queryService == null)
            {
                e.Node.Nodes.Clear();
                return;
            }

            e.Node.Nodes.Clear();
            e.Node.Nodes.Add(new TreeNode("Loading…"));

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading child flows...",
                Work = (worker, args) =>
                {
                    if (_treeRelatedRuns != null && _treeRelatedRuns.Count > 0)
                    {
                        args.Result = FlowRunTreeBuilder.GetChildren(parentRun, _treeRelatedRuns);
                    }
                    else
                    {
                        args.Result = _queryService.GetChildRuns(parentRun);
                    }
                },
                PostWorkCallBack = args =>
                {
                    e.Node.Nodes.Clear();

                    if (args.Error != null)
                    {
                        e.Node.Nodes.Add(new TreeNode("Error: " + args.Error.Message));
                        return;
                    }

                    var children = (IReadOnlyList<FlowRunInfo>)args.Result;
                    if (children.Count == 0)
                    {
                        e.Node.Nodes.Add(new TreeNode("(no child runs in Dataverse)"));
                        return;
                    }

                    foreach (var child in children)
                    {
                        var childNode = CreateRunNode(child);
                        if (_treeRelatedRuns != null && _treeRelatedRuns.Count > 0)
                        {
                            PopulateChildNodes(childNode, child, _treeRelatedRuns);
                        }
                        else
                        {
                            childNode.Nodes.Add(new TreeNode("…") { Tag = "lazy" });
                        }

                        e.Node.Nodes.Add(childNode);
                    }

                    if (_expandFailedPending)
                    {
                        ExpandFailedRecursive(e.Node);
                    }
                }
            });
        }

        private void TvTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is FlowRunInfo run)
            {
                _selectedRun = run;
                ShowDetails(run);
                LoadActionDetails(run);
            }
        }

        private bool _expandFailedPending;

        private void ExpandFailedNodes()
        {
            if (_tvTree.Nodes.Count == 0)
            {
                MessageBox.Show(this,
                    "Expand failures works on the run tree (bottom-left).\n\n" +
                    "Select a run first so the parent/child tree is loaded. Then this button expands every node with status Failed, so you can quickly spot which nested child flow broke.",
                    "Expand failures",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _expandFailedPending = true;
            try
            {
                foreach (TreeNode node in _tvTree.Nodes)
                {
                    ExpandFailedRecursive(node);
                }
            }
            finally
            {
                _expandFailedPending = false;
            }
        }

        private void ExpandFailedRecursive(TreeNode node)
        {
            if (node.Tag is FlowRunInfo run
                && string.Equals(run.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                if (node.Nodes.Count == 1 && node.Nodes[0].Tag is string lazyTag && lazyTag == "lazy")
                {
                    node.Expand();
                    return;
                }

                node.Expand();
                node.EnsureVisible();
            }

            foreach (TreeNode child in node.Nodes)
            {
                ExpandFailedRecursive(child);
            }
        }

        private void ShowDetails(FlowRunInfo run)
        {
            var detailed = run.DetailedErrorMessage;
            var dataverseError = run.ErrorMessage;
            var runUrl = EnsureEnvironmentAndBuildUrl(run);

            _txtDetails.Text =
                $"Run Id:      {run.RunId}{Environment.NewLine}" +
                $"Flow:        {run.FlowName}{Environment.NewLine}" +
                $"Workflow Id: {run.WorkflowId}{Environment.NewLine}" +
                $"Status:      {run.Status}{Environment.NewLine}" +
                $"Start:       {run.StartTime?.ToLocalTime()}{Environment.NewLine}" +
                $"End:         {run.EndTime?.ToLocalTime()}{Environment.NewLine}" +
                $"Duration:    {run.Duration}{Environment.NewLine}" +
                $"Trigger:     {run.TriggerType}{Environment.NewLine}" +
                $"Parent Run:  {run.ParentRunName}{Environment.NewLine}" +
                $"Caller Run:  {run.CallingProductRunId}{Environment.NewLine}" +
                $"Tracking Id: {run.ClientTrackingId}{Environment.NewLine}" +
                $"IsPrimary:   {run.IsPrimary}{Environment.NewLine}" +
                $"Error code:  {run.ErrorCode}{Environment.NewLine}" +
                $"Open run:    {(runUrl ?? "(unavailable — missing environment/flow/run id)")}{Environment.NewLine}" +
                $"{Environment.NewLine}=== Action-level error (Flow API) ==={Environment.NewLine}" +
                $"{(string.IsNullOrWhiteSpace(detailed) ? "(not loaded yet or no failed actions)" : detailed)}" +
                $"{Environment.NewLine}{Environment.NewLine}=== Dataverse errormessage (summary) ==={Environment.NewLine}" +
                $"{dataverseError}" +
                FormatActionsSection(run.Actions);
        }

        private string EnsureEnvironmentAndBuildUrl(FlowRunInfo run)
        {
            if (string.IsNullOrWhiteSpace(_environmentId) && Service != null)
            {
                TryInitializeFlowApi(Service, ConnectionDetail);
                if (string.IsNullOrWhiteSpace(_environmentId))
                {
                    var ctx = EnvironmentContextResolver.Resolve(
                        Service,
                        ConnectionDetail?.EnvironmentId,
                        ConnectionDetail?.TenantId ?? Guid.Empty);
                    if (ctx != null)
                    {
                        _environmentId = ctx.EnvironmentId;
                        _tenantId = ctx.TenantId;
                    }
                }
            }

            return FlowRunUrlBuilder.Build(_environmentId, run);
        }

        private void OpenSelectedRunInBrowser()
        {
            var run = _selectedRun;
            if (run == null && _lvRuns.SelectedItems.Count > 0)
            {
                run = _lvRuns.SelectedItems[0].Tag as FlowRunInfo;
            }

            OpenRunInBrowser(run);
        }

        private void OpenRunInBrowser(FlowRunInfo run)
        {
            var url = EnsureEnvironmentAndBuildUrl(run);
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(this,
                    "Could not build the Power Automate run URL. Environment id, flow id or run name is missing.",
                    "PA Run Monitor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            OpenUrl(url);
        }

        private void CopySelectedRunUrl()
        {
            var run = _selectedRun;
            if (run == null && _lvRuns.SelectedItems.Count > 0)
            {
                run = _lvRuns.SelectedItems[0].Tag as FlowRunInfo;
            }

            var url = EnsureEnvironmentAndBuildUrl(run);
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(this, "Run URL is not available.", "PA Run Monitor",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Clipboard.SetText(url);
                _lblStatus.Text = "Run URL copied to clipboard.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not copy URL:\r\n" + ex.Message, "PA Run Monitor",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not open browser:\r\n" + ex.Message + "\r\n\r\nURL:\r\n" + url,
                    "PA Run Monitor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string FormatActionsSection(IReadOnlyList<FlowActionInfo> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                return string.Empty;
            }

            var lines = new System.Text.StringBuilder();
            lines.AppendLine();
            lines.AppendLine();
            lines.AppendLine("=== Actions ===");
            foreach (var action in actions)
            {
                lines.AppendLine($"- [{action.Status}] {action.Name}" +
                                 (string.IsNullOrWhiteSpace(action.Code) ? "" : $" ({action.Code})"));
                if (!string.IsNullOrWhiteSpace(action.ErrorMessage)
                    && !string.Equals(action.Status, "Succeeded", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(action.Status, "Skipped", StringComparison.OrdinalIgnoreCase))
                {
                    lines.AppendLine($"    {action.ErrorMessage}");
                }
            }

            return lines.ToString();
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, max - 1) + "…";
        }
    }
}

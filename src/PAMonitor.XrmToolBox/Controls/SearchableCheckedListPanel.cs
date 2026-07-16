using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PAMonitor.XrmToolBox.Controls
{
    /// <summary>
    /// Bordered list with Select All, search box, and multi-check selection.
    /// </summary>
    public sealed class SearchableCheckedListPanel : UserControl
    {
        private readonly GroupBox _group;
        private readonly CheckBox _chkSelectAll;
        private readonly TextBox _txtSearch;
        private readonly CheckedListBox _lstItems;

        private readonly List<object> _allItems = new List<object>();
        private readonly HashSet<Guid> _selectedIds = new HashSet<Guid>();
        private Func<object, Guid> _idSelector = _ => Guid.Empty;
        private Func<object, string> _textSelector = o => o?.ToString() ?? string.Empty;
        private bool _syncingChecks;
        private bool _suppressSelectionEvents;

        public event EventHandler SelectionChanged;

        public SearchableCheckedListPanel()
        {
            _group = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "Items",
                Padding = new Padding(8, 6, 8, 8)
            };

            _chkSelectAll = new CheckBox
            {
                Text = "Select All",
                Dock = DockStyle.Top,
                Height = 22,
                Padding = new Padding(2, 0, 0, 0)
            };
            _chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 23
            };
            _txtSearch.TextChanged += (_, __) => RebindList();

            var searchHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(0, 2, 0, 4)
            };
            _txtSearch.Dock = DockStyle.Fill;
            searchHost.Controls.Add(_txtSearch);

            _lstItems = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            _lstItems.ItemCheck += LstItems_ItemCheck;

            _group.Controls.Add(_lstItems);
            _group.Controls.Add(searchHost);
            _group.Controls.Add(_chkSelectAll);

            Controls.Add(_group);
        }

        public string Title
        {
            get => _group.Text;
            set => _group.Text = value;
        }

        public int SelectedCount => _selectedIds.Count;

        public IReadOnlyList<Guid> GetSelectedIds() => _selectedIds.ToList();

        public void SetItems<T>(IEnumerable<T> items, Func<T, Guid> idSelector, Func<T, string> textSelector = null)
        {
            if (idSelector == null)
            {
                throw new ArgumentNullException(nameof(idSelector));
            }

            _suppressSelectionEvents = true;
            try
            {
                _idSelector = o => idSelector((T)o);
                _textSelector = textSelector != null
                    ? o => textSelector((T)o) ?? string.Empty
                    : o => o?.ToString() ?? string.Empty;

                _allItems.Clear();
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        _allItems.Add(item);
                    }
                }

                _selectedIds.Clear();
                _txtSearch.Clear();
                RebindList();
            }
            finally
            {
                _suppressSelectionEvents = false;
            }
        }

        public void Clear()
        {
            _suppressSelectionEvents = true;
            try
            {
                _allItems.Clear();
                _selectedIds.Clear();
                _txtSearch.Clear();
                RebindList();
            }
            finally
            {
                _suppressSelectionEvents = false;
            }
        }

        private void RaiseSelectionChanged()
        {
            if (_suppressSelectionEvents)
            {
                return;
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private IList<object> GetVisibleItems()
        {
            var filter = (_txtSearch.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(filter))
            {
                return _allItems;
            }

            return _allItems
                .Where(o => _textSelector(o).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void RebindList()
        {
            var visible = GetVisibleItems();

            _syncingChecks = true;
            try
            {
                _lstItems.BeginUpdate();
                _lstItems.Items.Clear();
                foreach (var item in visible)
                {
                    var index = _lstItems.Items.Add(item);
                    _lstItems.SetItemChecked(index, _selectedIds.Contains(_idSelector(item)));
                }
                _lstItems.EndUpdate();
                SyncSelectAllCheckbox(visible);
            }
            finally
            {
                _syncingChecks = false;
            }
        }

        private void SyncSelectAllCheckbox(IList<object> visible)
        {
            if (visible.Count == 0)
            {
                _chkSelectAll.Checked = false;
                return;
            }

            _chkSelectAll.Checked = visible.All(o => _selectedIds.Contains(_idSelector(o)));
        }

        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_syncingChecks)
            {
                return;
            }

            var visible = GetVisibleItems();
            if (_chkSelectAll.Checked)
            {
                foreach (var item in visible)
                {
                    _selectedIds.Add(_idSelector(item));
                }
            }
            else
            {
                foreach (var item in visible)
                {
                    _selectedIds.Remove(_idSelector(item));
                }
            }

            RebindList();
            RaiseSelectionChanged();
        }

        private void LstItems_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_syncingChecks)
            {
                return;
            }

            var item = _lstItems.Items[e.Index];
            var id = _idSelector(item);
            if (e.NewValue == CheckState.Checked)
            {
                _selectedIds.Add(id);
            }
            else
            {
                _selectedIds.Remove(id);
            }

            BeginInvoke(new Action(() =>
            {
                _syncingChecks = true;
                try
                {
                    SyncSelectAllCheckbox(GetVisibleItems());
                }
                finally
                {
                    _syncingChecks = false;
                }

                RaiseSelectionChanged();
            }));
        }
    }
}

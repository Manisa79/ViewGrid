using System.Runtime.InteropServices;
using ViewGrid.Columns;
using ViewGrid.Localization;
using ViewGrid.Theming;

namespace ViewGrid.Layout;

public sealed class ColumnChooserForm : Form
{
    private readonly TextBox _search = new() { Dock = DockStyle.Fill, PlaceholderText = ViewGridText.ColumnSearchPlaceholder, BorderStyle = BorderStyle.FixedSingle };
    private readonly CheckedListBox _list = new()
    {
        Dock = DockStyle.Fill,
        CheckOnClick = true,
        BorderStyle = BorderStyle.FixedSingle,
        IntegralHeight = false,
        DrawMode = DrawMode.Normal
    };
    private readonly Button _all = new() { Text = ViewGridText.ShowAll, Width = 112, Height = 30 };
    private readonly Button _none = new() { Text = ViewGridText.HideAll, Width = 112, Height = 30 };
    private readonly Button _ok = new() { Text = ViewGridText.OK, DialogResult = DialogResult.OK, Width = 92, Height = 30 };
    private readonly Button _cancel = new() { Text = ViewGridText.Cancel, DialogResult = DialogResult.Cancel, Width = 92, Height = 30 };
    private readonly Panel _buttons = new() { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8) };
    private readonly Panel _searchPanel = new() { Dock = DockStyle.Top, Height = 42, Padding = new Padding(8, 8, 8, 4) };
    private readonly Panel _listPanel = new() { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 8, 0) };
    private readonly IList<ViewGridColumn> _columns;
    private readonly ViewGridTheme _theme;
    private readonly Dictionary<ViewGridColumn, bool> _pendingStates = new();
    private bool _isRefilling;


    private const int SC_MINIMIZE = 0xF020;
    private const int SC_MAXIMIZE = 0xF030;
    private const int MF_BYCOMMAND = 0x00000000;

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern bool DeleteMenu(IntPtr hMenu, int uPosition, int uFlags);

    public ColumnChooserForm(IList<ViewGridColumn> columns) : this(columns, WindowsThemeService.CurrentTheme())
    {
    }

    public ColumnChooserForm(IList<ViewGridColumn> columns, ViewGridTheme? theme)
    {
        _columns = columns;
        _theme = theme ?? WindowsThemeService.CurrentTheme();

        Text = ViewGridText.ColumnChooserTitle;
        Width = 450;
        Height = 460;
        MinimumSize = new Size(380, 360);
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterParent;
        ViewGridDialogChrome.ConfigureStandardDialog(this, _theme, new Size(380, 360), sizeable: true, iconKind: ViewGridDialogIconKind.Column);
        AcceptButton = _ok;
        CancelButton = _cancel;

        _searchPanel.Controls.Add(_search);
        _listPanel.Controls.Add(_list);

        _buttons.Controls.Add(_cancel);
        _buttons.Controls.Add(_ok);
        _buttons.Controls.Add(_none);
        _buttons.Controls.Add(_all);

        _cancel.Dock = DockStyle.Right;
        _ok.Dock = DockStyle.Right;
        _none.Dock = DockStyle.Left;
        _all.Dock = DockStyle.Left;

        Controls.Add(_listPanel);
        Controls.Add(_status);
        Controls.Add(_searchPanel);
        Controls.Add(_buttons);

        ApplyTheme();

        _search.TextChanged += (_, __) => { CaptureVisibleListStates(); RefillList(_search.Text); };
        _all.Click += (_, __) => SetVisibleItems(true);
        _none.Click += (_, __) => SetVisibleItems(false);
        _list.ItemCheck += (_, e) =>
        {
            if (_isRefilling)
                return;

            var c = e.Index >= 0 && e.Index < _list.Items.Count
                ? GetColumnFromItem(_list.Items[e.Index])
                : null;

            if (c != null)
                _pendingStates[c] = e.NewValue == CheckState.Checked;

            UpdateStatus(e.Index, e.NewValue);
        };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        RefillList(string.Empty);
    }

    private void ApplyTheme()
    {
        ViewGridDialogChrome.ConfigureOpenViewGridDialogs(this, _theme);
        _status.ForeColor = _theme.MutedForeColor;
    }

    private void RefillList(string filter)
    {
        CaptureVisibleListStates();

        _isRefilling = true;
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var c in _columns)
            {
                if (c.PrivateColumn || !c.AllowColumnChooser) continue;
                var text = GetColumnText(c);
                if (!string.IsNullOrWhiteSpace(filter) && text.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) < 0)
                    continue;

                var visible = _pendingStates.TryGetValue(c, out var saved) ? saved : c.Visible;
                _list.Items.Add(new ColumnChooserItem(c, text), visible);
            }
        }
        finally
        {
            _list.EndUpdate();
            _isRefilling = false;
        }

        UpdateStatus();
    }

    private void CaptureVisibleListStates()
    {
        for (int i = 0; i < _list.Items.Count; i++)
        {
            var c = GetColumnFromItem(_list.Items[i]);
            if (c != null)
                _pendingStates[c] = _list.GetItemChecked(i);
        }
    }

    private static string GetColumnText(ViewGridColumn c)
    {
        var caption = !string.IsNullOrWhiteSpace(c.Header) ? c.Header.Trim()
            : !string.IsNullOrWhiteSpace(c.AspectName) ? c.AspectName.Trim()
            : !string.IsNullOrWhiteSpace(c.Name) ? c.Name.Trim()
            : "Column";

        if (!string.IsNullOrWhiteSpace(c.AspectName) &&
            !string.Equals(caption, c.AspectName, StringComparison.OrdinalIgnoreCase))
            return $"{caption} ({c.AspectName})";

        if (!string.IsNullOrWhiteSpace(c.Name) &&
            !string.Equals(caption, c.Name, StringComparison.OrdinalIgnoreCase))
            return $"{caption} [{c.Name}]";

        return caption;
    }

    private static ViewGridColumn? GetColumnFromItem(object? item)
    {
        if (item is ColumnChooserItem chooserItem) return chooserItem.Column;
        return item as ViewGridColumn;
    }

    private sealed class ColumnChooserItem
    {
        public ColumnChooserItem(ViewGridColumn column, string text)
        {
            Column = column;
            Text = text;
        }

        public ViewGridColumn Column { get; }
        public string Text { get; }
        public override string ToString() => Text;
    }

    private void SetVisibleItems(bool visible)
    {
        _isRefilling = true;
        _list.BeginUpdate();
        try
        {
            for (int i = 0; i < _list.Items.Count; i++)
            {
                _list.SetItemChecked(i, visible);
                var c = GetColumnFromItem(_list.Items[i]);
                if (c != null)
                    _pendingStates[c] = visible;
            }
        }
        finally
        {
            _list.EndUpdate();
            _isRefilling = false;
        }
        UpdateStatus();
    }

    private void UpdateStatus(int changedIndex = -1, CheckState? changedValue = null)
    {
        int visible = 0;
        for (int i = 0; i < _list.Items.Count; i++)
        {
            bool isChecked = _list.GetItemChecked(i);
            if (i == changedIndex && changedValue.HasValue)
                isChecked = changedValue.Value == CheckState.Checked;
            if (isChecked) visible++;
        }
        _status.Text = $"{visible}/{_list.Items.Count} kolon görünür";
    }



    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        HideUnavailableSystemMenuItems();
    }

    private void HideUnavailableSystemMenuItems()
    {
        if (DesignMode)
            return;

        IntPtr systemMenu = GetSystemMenu(Handle, false);
        if (systemMenu == IntPtr.Zero)
            return;

        if (!MinimizeBox)
            DeleteMenu(systemMenu, SC_MINIMIZE, MF_BYCOMMAND);

        if (!MaximizeBox)
            DeleteMenu(systemMenu, SC_MAXIMIZE, MF_BYCOMMAND);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        _search.Focus();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            CaptureVisibleListStates();
            foreach (var pair in _pendingStates)
                pair.Key.ApplyRuntimeVisible(pair.Value);
        }
        base.OnFormClosing(e);
    }
}

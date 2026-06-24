using System.ComponentModel;
using System.Data;
using System.Text;
using Taylan.Pano.Columns;
using Taylan.Pano.Filtering;

namespace Taylan.Pano.Core;

public enum PanoColumnPinMode
{
    None = 0,
    Left = 1,
    Right = 2
}

public enum PanoCopyFormat
{
    PlainText = 0,
    Json = 1,
    Markdown = 2,
    Html = 3
}

public enum PanoRowChangeFlashMode
{
    None = 0,
    Cell = 1,
    Row = 2,
    Accent = 3
}

public sealed class PanoQuickFilterOptions
{
    public bool Visible { get; set; }
    public int Height { get; set; } = 34;
    public bool PerColumnTextBoxes { get; set; } = true;
    public bool ApplyWhileTyping { get; set; } = true;
    public int DebounceMs { get; set; } = 180;
    public string PlaceholderText { get; set; } = "Hızlı filtre...";
}

public sealed class PanoFilterPresetInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string SerializedState { get; set; } = string.Empty;
}

public sealed class PanoRowDetailsTemplate
{
    public string Name { get; set; } = string.Empty;
    public int PreferredHeight { get; set; } = 120;
    public Func<object, Control>? CreateControl { get; set; }
    public Func<object, string>? CreateText { get; set; }
}

public sealed class PanoCommandPaletteCommand
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Category { get; set; } = "Pano";
    public string? ShortcutText { get; set; }
    public Action<PanoControl>? Execute { get; set; }
}

public sealed class PanoContextMenuRequest
{
    public PanoContextMenuRequest(PanoControl grid, PanoColumn? column, object? rowObject, Point clientLocation)
    {
        Grid = grid;
        Column = column;
        RowObject = rowObject;
        ClientLocation = clientLocation;
    }

    public PanoControl Grid { get; }
    public PanoColumn? Column { get; }
    public object? RowObject { get; }
    public Point ClientLocation { get; }
    public ContextMenuStrip Menu { get; } = new ContextMenuStrip();
}

public sealed class PanoPlatformFeatureOptions
{
    public bool EnableComputedColumns { get; set; } = true;
    public bool EnableGroupPanel { get; set; } = true;
    public bool EnableQuickFilterBar { get; set; } = true;
    public bool EnableFilterPresets { get; set; } = true;
    public bool EnableCommandPalette { get; set; } = true;
    public bool EnableCopyPro { get; set; } = true;
    public bool EnableRowDetails { get; set; } = true;
    public bool EnableContextMenuProvider { get; set; } = true;
    public bool EnableKeyboardPowerMode { get; set; } = true;
    public bool EnableChangeFlash { get; set; } = true;
}

public enum PanoCardFilterColumnSelectionMode
{
    AskUser = 0,
    FirstVisible = 1,
    LastUsed = 2,
    AutoBestMatch = 3
}

public sealed class PanoDashboardFilterOptions
{
    public PanoCardFilterColumnSelectionMode ColumnSelectionMode { get; set; } = PanoCardFilterColumnSelectionMode.AskUser;
    public bool ShowColumnChooserBeforeFilter { get; set; } = true;
    public bool RememberLastColumn { get; set; } = true;
    public string LastAspectName { get; set; } = string.Empty;
}

public partial class PanoControl
{
    private readonly Dictionary<string, PanoFilterPresetInfo> _v2813FilterPresets = new Dictionary<string, PanoFilterPresetInfo>(StringComparer.OrdinalIgnoreCase);
    private readonly List<PanoCommandPaletteCommand> _v2813Commands = new List<PanoCommandPaletteCommand>();
    private PanoQuickFilterOptions? _v2813QuickFilterOptions;
    private Panel? _v2813QuickFilterPanel;
    private readonly Dictionary<string, TextBox> _v2813QuickFilterBoxes = new Dictionary<string, TextBox>(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _v2813QuickFilterTimer = new System.Windows.Forms.Timer { Interval = 180 };
    private PanoRowDetailsTemplate? _v2813RowDetailsTemplate;
    private readonly Dictionary<object, DateTime> _v2813PinnedRows = new Dictionary<object, DateTime>();

    [Category("Pano - Platform")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public PanoPlatformFeatureOptions PlatformFeatures { get; } = new PanoPlatformFeatureOptions();

    [Category("Pano - Grouping")]
    [DefaultValue(false)]
    [Description("Header üzerinde sürükle-bırak grup paneli gösterilmesini sağlar. Draw pipeline desteği olan formlarda grup kolonları görünür hale gelir.")]
    public bool ShowGroupPanel { get; set; }

    [Category("Pano - Grouping")]
    [DefaultValue("Gruplamak için kolon başlığını buraya sürükleyin")]
    public string GroupPanelEmptyText { get; set; } = "Gruplamak için kolon başlığını buraya sürükleyin";


    [Category("Pano - Copy")]
    [DefaultValue(true)]
    public bool EnableCopyPro { get; set; } = true;

    [Category("Pano - Change Flash")]
    [DefaultValue(PanoRowChangeFlashMode.Row)]
    public PanoRowChangeFlashMode ChangeFlashMode { get; set; } = PanoRowChangeFlashMode.Row;

    [Browsable(false)]
    public IReadOnlyDictionary<string, PanoFilterPresetInfo> FilterPresets => _v2813FilterPresets;

    [Browsable(false)]
    public IList<PanoCommandPaletteCommand> CommandPaletteCommands => _v2813Commands;

    [Browsable(false)]
    public Func<PanoContextMenuRequest, ContextMenuStrip?>? ContextMenuProvider { get; set; }

    [Category("Pano - Card/Dashboard Filter")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public PanoDashboardFilterOptions DashboardFilterOptions { get; } = new PanoDashboardFilterOptions();

    public void ShowDashboardColumnFilter(Control anchorControl)
    {
        Point screen = anchorControl == null ? Control.MousePosition : anchorControl.PointToScreen(new Point(0, anchorControl.Height));
        ShowDashboardColumnFilterAtScreen(screen);
    }

    public void ShowDashboardColumnFilterAtScreen(Point screenAnchor)
    {
        PanoColumn col = ResolveDashboardFilterColumn(null);
        if (col == null || DashboardFilterOptions.ShowColumnChooserBeforeFilter || DashboardFilterOptions.ColumnSelectionMode == PanoCardFilterColumnSelectionMode.AskUser)
        {
            col = ShowDashboardFilterColumnChooser(screenAnchor, col);
        }

        if (col == null)
            return;

        DashboardFilterOptions.LastAspectName = col.AspectName ?? string.Empty;
        ShowFilterMenuForAspectAtScreen(col.AspectName, screenAnchor);
    }

    public void ShowDashboardColumnFilterForTypedText(string typedText, Control anchorControl)
    {
        Point screen = anchorControl == null ? Control.MousePosition : anchorControl.PointToScreen(new Point(0, anchorControl.Height));
        PanoColumn col = ResolveDashboardFilterColumn(typedText);
        if (col == null)
            col = ShowDashboardFilterColumnChooser(screen, null);
        if (col == null)
            return;
        DashboardFilterOptions.LastAspectName = col.AspectName ?? string.Empty;
        ShowFilterMenuForAspectAtScreen(col.AspectName, screen);
    }

    private PanoColumn ResolveDashboardFilterColumn(string typedText)
    {
        List<PanoColumn> visible = Columns.Where(c => c.Visible && c.AllowFilter).ToList();
        if (visible.Count == 0)
            return null;

        if (DashboardFilterOptions.ColumnSelectionMode == PanoCardFilterColumnSelectionMode.LastUsed && !string.IsNullOrWhiteSpace(DashboardFilterOptions.LastAspectName))
        {
            PanoColumn last = visible.FirstOrDefault(c => string.Equals(c.AspectName, DashboardFilterOptions.LastAspectName, StringComparison.OrdinalIgnoreCase));
            if (last != null) return last;
        }

        if (DashboardFilterOptions.ColumnSelectionMode == PanoCardFilterColumnSelectionMode.AutoBestMatch && !string.IsNullOrWhiteSpace(typedText))
        {
            string q = typedText.Trim();
            PanoColumn best = null;
            int bestScore = -1;
            foreach (PanoColumn c in visible)
            {
                int score = 0;
                string name = ((c.Header ?? string.Empty) + " " + (c.AspectName ?? string.Empty)).ToLowerInvariant();
                string query = q.ToLowerInvariant();
                if (name.Contains(query)) score += 100;
                IReadOnlyList<string> values = GetDistinctColumnValues(c, Math.Min(200, Math.Max(20, _provider.Count)), q);
                if (values.Count > 0) score += 20 + Math.Min(50, values.Count);
                if (score > bestScore) { bestScore = score; best = c; }
            }
            if (best != null && bestScore > 0) return best;
        }

        if (DashboardFilterOptions.ColumnSelectionMode == PanoCardFilterColumnSelectionMode.FirstVisible)
            return visible[0];

        if (!string.IsNullOrWhiteSpace(DashboardFilterOptions.LastAspectName))
        {
            PanoColumn last = visible.FirstOrDefault(c => string.Equals(c.AspectName, DashboardFilterOptions.LastAspectName, StringComparison.OrdinalIgnoreCase));
            if (last != null) return last;
        }

        return visible[0];
    }

    private PanoColumn ShowDashboardFilterColumnChooser(Point screenAnchor, PanoColumn suggested)
    {
        List<PanoColumn> visible = Columns.Where(c => c.Visible && c.AllowFilter).ToList();
        if (visible.Count == 0)
            return null;

        using (Form form = new Form())
        {
            form.Text = "Filtre kolonu seç";
            form.StartPosition = FormStartPosition.Manual;
            form.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            form.ShowInTaskbar = false;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.Width = 320;
            form.Height = 420;
            form.BackColor = _theme.PanelBackColor;
            form.ForeColor = _theme.ForeColor;

            Rectangle wa = Screen.FromPoint(screenAnchor).WorkingArea;
            int left = Math.Min(Math.Max(wa.Left + 8, screenAnchor.X), wa.Right - form.Width - 8);
            int top = Math.Min(Math.Max(wa.Top + 8, screenAnchor.Y), wa.Bottom - form.Height - 8);
            form.Location = new Point(left, top);

            Label title = new Label();
            title.Text = "Card/Dashboard filtresi için kolon seçin";
            title.Dock = DockStyle.Top;
            title.Height = 34;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Padding = new Padding(10, 0, 0, 0);
            title.BackColor = _theme.HeaderBack;
            title.ForeColor = _theme.ForeColor;

            ListBox list = new ListBox();
            list.Dock = DockStyle.Fill;
            list.IntegralHeight = false;
            list.BackColor = _theme.BackColor;
            list.ForeColor = _theme.ForeColor;
            list.BorderStyle = BorderStyle.FixedSingle;

            foreach (PanoColumn c in visible)
                list.Items.Add(new DashboardFilterColumnItem(c));

            Button ok = new Button();
            ok.Text = "Filtrele";
            ok.Width = 96;
            ok.Height = 30;
            ok.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            ok.Left = form.ClientSize.Width - 108;
            ok.Top = form.ClientSize.Height - 42;
            ok.BackColor = _theme.AccentColor;
            ok.ForeColor = Color.White;
            ok.FlatStyle = FlatStyle.Flat;
            ok.DialogResult = DialogResult.OK;

            Button cancel = new Button();
            cancel.Text = "Vazgeç";
            cancel.Width = 86;
            cancel.Height = 30;
            cancel.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            cancel.Left = ok.Left - cancel.Width - 8;
            cancel.Top = ok.Top;
            cancel.DialogResult = DialogResult.Cancel;

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 50;
            bottom.BackColor = _theme.PanelBackColor;
            bottom.Controls.Add(ok);
            bottom.Controls.Add(cancel);
            bottom.Resize += delegate
            {
                ok.Left = bottom.ClientSize.Width - ok.Width - 10;
                ok.Top = 10;
                cancel.Left = ok.Left - cancel.Width - 8;
                cancel.Top = 10;
            };

            form.Controls.Add(list);
            form.Controls.Add(bottom);
            form.Controls.Add(title);
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            list.DoubleClick += delegate { form.DialogResult = DialogResult.OK; form.Close(); };

            if (suggested != null)
            {
                for (int i = 0; i < list.Items.Count; i++)
                {
                    DashboardFilterColumnItem item = list.Items[i] as DashboardFilterColumnItem;
                    if (item != null && item.Column == suggested) { list.SelectedIndex = i; break; }
                }
            }
            if (list.SelectedIndex < 0 && list.Items.Count > 0) list.SelectedIndex = 0;

            if (form.ShowDialog(FindForm()) == DialogResult.OK && list.SelectedItem is DashboardFilterColumnItem selected)
                return selected.Column;
        }

        return null;
    }

    private sealed class DashboardFilterColumnItem
    {
        public DashboardFilterColumnItem(PanoColumn column) { Column = column; }
        public PanoColumn Column { get; private set; }
        public override string ToString()
        {
            string header = Column.Header ?? string.Empty;
            return string.IsNullOrWhiteSpace(header) ? Column.AspectName : header;
        }
    }

    public void EnableQuickFilterBar(PanoQuickFilterOptions? options = null)
    {
        _v2813QuickFilterOptions = options ?? new PanoQuickFilterOptions { Visible = true };
        _v2813QuickFilterOptions.Visible = true;
        EnsureQuickFilterPanel();
        LayoutQuickFilterPanel();
    }

    public void DisableQuickFilterBar()
    {
        _v2813QuickFilterOptions = null;
        if (_v2813QuickFilterPanel != null)
            _v2813QuickFilterPanel.Visible = false;
    }

    public void SetRowDetailsTemplate(PanoRowDetailsTemplate template)
    {
        _v2813RowDetailsTemplate = template ?? throw new ArgumentNullException(nameof(template));
    }

    public Control? CreateRowDetailsControl(object rowObject)
    {
        if (_v2813RowDetailsTemplate == null || rowObject == null) return null;
        if (_v2813RowDetailsTemplate.CreateControl != null) return _v2813RowDetailsTemplate.CreateControl(rowObject);
        if (_v2813RowDetailsTemplate.CreateText != null)
        {
            return new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical,
                Text = _v2813RowDetailsTemplate.CreateText(rowObject) ?? string.Empty,
                BackColor = BackColor,
                ForeColor = ForeColor,
                Font = Font
            };
        }
        return null;
    }

    public void PinRow(object rowObject)
    {
        if (rowObject == null) return;
        _v2813PinnedRows[rowObject] = DateTime.Now;
        HighlightObject(rowObject, Color.FromArgb(80, 90, 150, 255), -1, "Pinned");
        Invalidate();
    }

    public void UnpinRow(object rowObject)
    {
        if (rowObject == null) return;
        _v2813PinnedRows.Remove(rowObject);
        Invalidate();
    }

    public bool IsRowPinned(object rowObject) => rowObject != null && _v2813PinnedRows.ContainsKey(rowObject);

    public void SaveFilterPreset(string name, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Preset adı boş olamaz.", nameof(name));
        _v2813FilterPresets[name] = new PanoFilterPresetInfo
        {
            Name = name,
            DisplayName = displayName ?? name,
            CreatedAt = DateTime.Now,
            SerializedState = ExportFilterStateForPreset()
        };
    }

    private bool TryLoadPlatformFilterPreset(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (!_v2813FilterPresets.TryGetValue(name, out PanoFilterPresetInfo? preset)) return false;
        ImportFilterStateFromPreset(preset.SerializedState);
        BuildViewIndex();
        Invalidate();
        return true;
    }

    public bool DeleteFilterPreset(string name) => _v2813FilterPresets.Remove(name);

    public void AddCommand(string id, string text, Action<PanoControl> execute, string category = "Pano", string? shortcutText = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Komut id boş olamaz.", nameof(id));
        _v2813Commands.RemoveAll(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
        _v2813Commands.Add(new PanoCommandPaletteCommand
        {
            Id = id,
            Text = text,
            Category = category,
            ShortcutText = shortcutText,
            Execute = execute
        });
    }

    public void ShowCommandPalette()
    {
        if (!EnableCommandPalette) return;
        using Form form = new Form
        {
            Text = "Pano Komut Paleti",
            StartPosition = FormStartPosition.CenterParent,
            Width = 520,
            Height = 460,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowIcon = false
        };
        TextBox search = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Komut ara...", Height = 30 };
        ListBox list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        form.Controls.Add(list);
        form.Controls.Add(search);
        void Reload()
        {
            string q = search.Text.Trim();
            list.Items.Clear();
            foreach (PanoCommandPaletteCommand cmd in GetBuiltInAndCustomCommands())
            {
                string line = string.IsNullOrWhiteSpace(cmd.ShortcutText)
                    ? $"{cmd.Category}  •  {cmd.Text}"
                    : $"{cmd.Category}  •  {cmd.Text}    {cmd.ShortcutText}";
                if (q.Length == 0 || line.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    list.Items.Add(new CommandListItem(cmd, line));
            }
        }
        search.TextChanged += (_, __) => Reload();
        list.DoubleClick += (_, __) => ExecuteSelected();
        form.KeyPreview = true;
        form.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) ExecuteSelected(); if (e.KeyCode == Keys.Escape) form.Close(); };
        void ExecuteSelected()
        {
            if (list.SelectedItem is CommandListItem item)
            {
                form.Close();
                item.Command.Execute?.Invoke(this);
            }
        }
        Reload();
        if (list.Items.Count > 0) list.SelectedIndex = 0;
        form.ShowDialog(FindForm());
    }

    public void CopySelection(PanoCopyFormat format)
    {
        if (!EnableCopyPro) return;
        string text = format switch
        {
            PanoCopyFormat.Json => BuildCopyJson(),
            PanoCopyFormat.Markdown => BuildCopyMarkdown(),
            PanoCopyFormat.Html => BuildCopyHtml(),
            _ => BuildCopyPlainText()
        };
        if (!string.IsNullOrEmpty(text)) Clipboard.SetText(text);
    }

    public void FlashObject(object rowObject, Color? color = null, string? reason = null)
    {
        if (rowObject == null) return;
        HighlightObject(rowObject, color ?? Color.FromArgb(90, 255, 190, 80), -1, reason ?? "Changed");
    }

    public void EnablePowerKeyboardShortcuts()
    {
        KeyDown -= PanoV2813_KeyDown;
        KeyDown += PanoV2813_KeyDown;
    }

    private void PanoV2813_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.K)
        {
            ShowCommandPalette();
            e.Handled = true;
        }
        else if (e.Control && e.Shift && e.KeyCode == Keys.C)
        {
            CopySelection(PanoCopyFormat.Json);
            e.Handled = true;
        }
        else if (e.Control && e.Alt && e.KeyCode == Keys.C)
        {
            CopySelection(PanoCopyFormat.Markdown);
            e.Handled = true;
        }
    }

    private void EnsureQuickFilterPanel()
    {
        if (_v2813QuickFilterPanel == null)
        {
            _v2813QuickFilterPanel = new Panel { Height = _v2813QuickFilterOptions?.Height ?? 34, Dock = DockStyle.Top, Visible = true };
            Controls.Add(_v2813QuickFilterPanel);
            _v2813QuickFilterPanel.BringToFront();
            _v2813QuickFilterTimer.Tick += (_, __) =>
            {
                _v2813QuickFilterTimer.Stop();
                ApplyQuickFilterText();
            };
        }
        BuildQuickFilterBoxes();
    }

    private void BuildQuickFilterBoxes()
    {
        if (_v2813QuickFilterPanel == null) return;
        _v2813QuickFilterPanel.Controls.Clear();
        _v2813QuickFilterBoxes.Clear();
        int x = 6;
        foreach (PanoColumn col in Columns.Where(c => c.Visible))
        {
            TextBox box = new TextBox
            {
                Left = x,
                Top = 5,
                Width = Math.Max(80, Math.Min(180, col.Width)),
                Height = 24,
                PlaceholderText = col.Header,
                Tag = col
            };
            box.TextChanged += (_, __) =>
            {
                _v2813QuickFilterTimer.Interval = _v2813QuickFilterOptions?.DebounceMs ?? 180;
                _v2813QuickFilterTimer.Stop();
                _v2813QuickFilterTimer.Start();
            };
            _v2813QuickFilterPanel.Controls.Add(box);
            _v2813QuickFilterBoxes[col.Key] = box;
            x += box.Width + 6;
        }
    }

    private void LayoutQuickFilterPanel()
    {
        if (_v2813QuickFilterPanel == null) return;
        _v2813QuickFilterPanel.Height = _v2813QuickFilterOptions?.Height ?? 34;
        _v2813QuickFilterPanel.Visible = _v2813QuickFilterOptions?.Visible == true;
        _v2813QuickFilterPanel.BackColor = _theme.HeaderBack;
        foreach (Control c in _v2813QuickFilterPanel.Controls)
        {
            c.BackColor = _theme.BackColor;
            c.ForeColor = _theme.ForeColor;
        }
    }

    private void ApplyQuickFilterText()
    {
        // v28.14: Quick Filter artık gerçek kolon filtresi uygular.
        // Önceki taslak GlobalFilter içine "Kolon:değer" yazıyordu; bu global arama
        // motorunda düz metin gibi çalıştığı için özellikle Card/Dashboard tarafında
        // beklenen kolon bazlı sonucu vermiyordu.
        foreach (KeyValuePair<string, TextBox> pair in _v2813QuickFilterBoxes)
        {
            TextBox box = pair.Value;
            PanoColumn col = box.Tag as PanoColumn;
            if (col == null || string.IsNullOrWhiteSpace(col.AspectName))
                continue;

            string text = (box.Text ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                _filters.Clear(col.AspectName);
                continue;
            }

            _filters.Set(new PanoColumnFilter
            {
                AspectName = col.AspectName,
                Mode = PanoFilterMode.Contains,
                Text = text,
                Enabled = true
            });
        }

        BuildViewIndex();
        Invalidate();
    }

    private sealed class CommandListItem
    {
        public CommandListItem(PanoCommandPaletteCommand command, string text)
        {
            Command = command;
            Text = text;
        }
        public PanoCommandPaletteCommand Command { get; }
        private string Text { get; }
        public override string ToString() => Text;
    }

    private IEnumerable<PanoCommandPaletteCommand> GetBuiltInAndCustomCommands()
    {
        yield return new PanoCommandPaletteCommand { Id = "copy", Text = "Seçimi kopyala", Category = "Clipboard", ShortcutText = "Ctrl+C", Execute = g => g.CopySelection(PanoCopyFormat.PlainText) };
        yield return new PanoCommandPaletteCommand { Id = "copy-json", Text = "Seçimi JSON kopyala", Category = "Clipboard", ShortcutText = "Ctrl+Shift+C", Execute = g => g.CopySelection(PanoCopyFormat.Json) };
        yield return new PanoCommandPaletteCommand { Id = "copy-md", Text = "Seçimi Markdown kopyala", Category = "Clipboard", ShortcutText = "Ctrl+Alt+C", Execute = g => g.CopySelection(PanoCopyFormat.Markdown) };
        yield return new PanoCommandPaletteCommand { Id = "clear-filters", Text = "Tüm filtreleri temizle", Category = "Filter", Execute = g => g.ClearFilters() };
        foreach (PanoCommandPaletteCommand cmd in _v2813Commands) yield return cmd;
    }

    private string BuildCopyPlainText()
    {
        List<PanoColumn> visible = Columns.Where(c => c.Visible).ToList();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Join("\t", visible.Select(c => c.Header)));
        foreach (object row in EnumerateSelectedOrVisibleRows())
            sb.AppendLine(string.Join("\t", visible.Select(c => Convert.ToString(c.GetValue(row)) ?? string.Empty)));
        return sb.ToString();
    }

    private string BuildCopyMarkdown()
    {
        List<PanoColumn> visible = Columns.Where(c => c.Visible).ToList();
        StringBuilder sb = new StringBuilder();
        sb.Append("| ").Append(string.Join(" | ", visible.Select(c => EscapeMarkdown(c.Header)))).AppendLine(" |");
        sb.Append("| ").Append(string.Join(" | ", visible.Select(_ => "---"))).AppendLine(" |");
        foreach (object row in EnumerateSelectedOrVisibleRows())
            sb.Append("| ").Append(string.Join(" | ", visible.Select(c => EscapeMarkdown(Convert.ToString(c.GetValue(row)) ?? string.Empty)))).AppendLine(" |");
        return sb.ToString();
    }

    private string BuildCopyHtml()
    {
        List<PanoColumn> visible = Columns.Where(c => c.Visible).ToList();
        StringBuilder sb = new StringBuilder("<table>\n<thead><tr>");
        foreach (PanoColumn c in visible) sb.Append("<th>").Append(System.Net.WebUtility.HtmlEncode(c.Header)).Append("</th>");
        sb.AppendLine("</tr></thead>\n<tbody>");
        foreach (object row in EnumerateSelectedOrVisibleRows())
        {
            sb.Append("<tr>");
            foreach (PanoColumn c in visible) sb.Append("<td>").Append(System.Net.WebUtility.HtmlEncode(Convert.ToString(c.GetValue(row)) ?? string.Empty)).Append("</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    private string BuildCopyJson()
    {
        List<PanoColumn> visible = Columns.Where(c => c.Visible).ToList();
        StringBuilder sb = new("[\n");
        bool firstRow = true;
        foreach (object row in EnumerateSelectedOrVisibleRows())
        {
            if (!firstRow) sb.AppendLine(",");
            firstRow = false;
            sb.Append("  {");
            for (int i = 0; i < visible.Count; i++)
            {
                PanoColumn c = visible[i];
                if (i > 0) sb.Append(",");
                sb.Append('\n').Append("    \"").Append(JsonEscape(c.Key)).Append("\": ").Append(ToJsonValue(c.GetValue(row)));
            }
            sb.Append("\n  }");
        }
        sb.AppendLine("\n]");
        return sb.ToString();
    }

    private IReadOnlyList<object> GetRowsSnapshot(int startIndex, int count)
    {
        List<object> rows = new List<object>();
        int start = Math.Max(0, startIndex);
        int end = Math.Min(_provider.Count, start + Math.Max(0, count));
        for (int i = start; i < end; i++)
        {
            object? row = _provider.GetRow(i);
            if (row != null) rows.Add(row);
        }
        return rows;
    }

    private IEnumerable<object> EnumerateSelectedOrVisibleRows()
    {
        IReadOnlyList<object> all = GetRowsSnapshot(0, _provider.Count);
        if (_selectedRows.Count > 0)
        {
            foreach (int viewIndex in _selectedRows)
            {
                if (viewIndex >= 0 && viewIndex < _viewIndexes.Count)
                {
                    int dataIndex = _viewIndexes[viewIndex];
                    if (dataIndex >= 0 && dataIndex < all.Count) yield return all[dataIndex];
                }
            }
            yield break;
        }
        foreach (int dataIndex in _viewIndexes.Take(5000))
        {
            if (dataIndex >= 0 && dataIndex < all.Count) yield return all[dataIndex];
        }
    }

    private string ExportFilterStateForPreset()
    {
        // Stable, human-readable snapshot. Internal PanoFilterSet remains source of truth.
        return $"Global={_filters.GlobalText ?? string.Empty}";
    }

    private void ImportFilterStateFromPreset(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return;
        const string prefix = "Global=";
        if (state.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            SetGlobalFilter(state.Substring(prefix.Length));
    }

    private static string EscapeMarkdown(string text) => (text ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    private static string JsonEscape(string text) => (text ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
    private static string ToJsonValue(object? value)
    {
        if (value == null || value == DBNull.Value) return "null";
        if (value is bool b) return b ? "true" : "false";
        if (value is byte || value is sbyte || value is short || value is ushort ||
            value is int || value is uint || value is long || value is ulong ||
            value is float || value is double || value is decimal)
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "0";
        return "\"" + JsonEscape(Convert.ToString(value) ?? string.Empty) + "\"";
    }
}

using System.ComponentModel;
using Taylan.Pano.Columns;
using Taylan.Pano.Localization;

namespace Taylan.Pano.Core;

public enum PanoColumnChooserMenuMode
{
    WindowOnly,
    PopupOnly,
    Both
}

public enum PanoColumnChooserPopupPlacement
{
    SubMenu,
    Inline
}

public partial class PanoControl
{
    private readonly System.Windows.Forms.Timer _sortBusyInvalidateTimer = new() { Interval = 80 };

    [Category("Pano - Column Chooser")]
    [DefaultValue(PanoColumnChooserMenuMode.Both)]
    [Description("Kolon görünürlük seçimi header menüsünde popup olarak mı, ayrı pencerede mi, yoksa ikisi birlikte mi gösterilsin.")]
    public PanoColumnChooserMenuMode ColumnChooserMenuMode { get; set; } = PanoColumnChooserMenuMode.Both;

    [Category("Pano - Column Chooser")]
    [DefaultValue(true)]
    [Description("Header/context menü içinde hızlı kolon işaretleme menüsünü gösterir.")]
    public bool ShowColumnChooserInHeaderMenu { get; set; } = true;

    [Category("Pano - Column Chooser")]
    [DefaultValue(true)]
    [Description("Header/context menü içinde ayrı kolon seçici penceresini açan komutu gösterir.")]
    public bool ShowColumnChooserWindowInHeaderMenu { get; set; } = true;

    [Category("Pano - Column Chooser")]
    [DefaultValue(true)]
    [Description("Kolon görünürlüğü menüden/pencereden değişince kullanıcı layout profilini otomatik kaydeder.")]
    public bool AutoSaveLayoutOnColumnVisibilityChange { get; set; } = true;

    [Category("Pano - Column Chooser")]
    [DefaultValue("Kolonlar")]
    public string ColumnChooserPopupText { get; set; } = "Kolonlar";

    [Category("Pano - Column Chooser")]
    [DefaultValue("Kolon seçici penceresi...")]
    public string ColumnChooserWindowText { get; set; } = "Kolon seçici penceresi...";

    [Category("Pano - Column Chooser")]
    [DefaultValue(PanoColumnChooserPopupPlacement.SubMenu)]
    [Description("Hızlı kolon görünürlük seçimleri ana menüde direkt mi yoksa Kolonlar alt menüsü altında mı gösterilsin.")]
    public PanoColumnChooserPopupPlacement ColumnChooserPopupPlacement { get; set; } = PanoColumnChooserPopupPlacement.SubMenu;

    [Category("Pano - Column Chooser")]
    [DefaultValue(true)]
    [Description("Kolon görünürlük işaretleri hızlı menüden değiştirildiğinde menünün açık kalmasını sağlar. ObjectListView SelectColumnsMenuStaysOpen davranışına denk gelir.")]
    public bool ColumnChooserMenuStaysOpen { get; set; } = true;

    internal void SetSortBusyState(bool busy)
    {
        IsSorting = busy;
        if (busy)
        {
            if (!_sortBusyInvalidateTimer.Enabled)
            {
                _sortBusyInvalidateTimer.Tick += SortBusyInvalidateTimer_Tick;
                _sortBusyInvalidateTimer.Start();
            }
        }
        else
        {
            _sortBusyInvalidateTimer.Stop();
            _sortBusyInvalidateTimer.Tick -= SortBusyInvalidateTimer_Tick;
        }
        Invalidate();
    }

    private void SortBusyInvalidateTimer_Tick(object? sender, EventArgs e)
    {
        if (IsSorting && ShowSortBusyIndicator) Invalidate();
        else _sortBusyInvalidateTimer.Stop();
    }

    private void AddColumnChooserMenu(ToolStripItemCollection items)
    {
        if (!ShowColumnChooserInHeaderMenu && !ShowColumnChooserWindowInHeaderMenu) return;

        bool showPopup = ShowColumnChooserInHeaderMenu && ColumnChooserMenuMode != PanoColumnChooserMenuMode.WindowOnly;
        bool showWindow = ShowColumnChooserWindowInHeaderMenu && ColumnChooserMenuMode != PanoColumnChooserMenuMode.PopupOnly;

        if (ColumnChooserPopupPlacement == PanoColumnChooserPopupPlacement.Inline)
        {
            if (showPopup)
            {
                AddColumnChooserPopupItems(items);
            }

            if (showWindow)
            {
                if (items.Count > 0) items.Add(new ToolStripSeparator());
                items.Add(string.IsNullOrWhiteSpace(ColumnChooserWindowText) ? PanoText.ColumnChooserWindow : ColumnChooserWindowText.Trim(), null, (_, __) => ShowColumnChooserDeferred());
            }

            RemoveRedundantMenuSeparators(items);
            ApplyMenuIcons(items);
            return;
        }

        var root = new ToolStripMenuItem(string.IsNullOrWhiteSpace(ColumnChooserPopupText) ? PanoText.Columns : ColumnChooserPopupText.Trim());

        if (showPopup)
        {
            AddColumnChooserPopupItems(root.DropDownItems);
        }

        if (showWindow)
        {
            if (root.DropDownItems.Count > 0) root.DropDownItems.Add(new ToolStripSeparator());
            root.DropDownItems.Add(string.IsNullOrWhiteSpace(ColumnChooserWindowText) ? PanoText.ColumnChooserWindow : ColumnChooserWindowText.Trim(), null, (_, __) => ShowColumnChooserDeferred());
        }

        root.DropDown.Closing += ColumnChooserDropDown_Closing;
        RemoveRedundantMenuSeparators(root.DropDownItems);
        ApplyMenuIcons(root.DropDownItems);
        global::Taylan.Pano.Theming.SmartMenuRenderer.ApplyTo(root.DropDown, _theme);
        if (root.DropDownItems.Count > 0) items.Add(root);
    }

    private void AddColumnChooserPopupItems(ToolStripItemCollection items)
    {
        items.Add(PanoText.ShowAll, null, (_, __) => SetAllColumnsVisible(true));
        items.Add(PanoText.HideAll, null, (_, __) => SetAllColumnsVisible(false));
        items.Add(PanoText.DefaultColumnLayout, null, (_, __) => ResetColumnLayout());
        items.Add(new ToolStripSeparator());

        foreach (var col in Columns)
        {
            if (col.PrivateColumn || !col.AllowColumnChooser) continue;
            var item = new ToolStripMenuItem(string.IsNullOrWhiteSpace(col.Header) ? col.AspectName : col.Header)
            {
                Checked = col.Visible,
                CheckOnClick = true,
                Tag = col,
                Enabled = col.Visible ? col.CanBeHidden : true
            };
            item.CheckedChanged += (_, __) =>
            {
                if (item.Tag is not PanoColumn c) return;
                if (ColumnChooserMenuStaysOpen) _columnChooserMenuKeepOpenRequested = true;
                if (c.Visible == item.Checked) return;
                c.ApplyRuntimeVisible(item.Checked);
                AutoSizeFillColumns();
                RefreshView();
                if (AutoSaveLayoutOnColumnVisibilityChange) QueueAutoSaveUserLayout();
            };
            items.Add(item);
        }

        items.Add(new ToolStripSeparator());
        items.Add(PanoText.FitVisibleColumns, null, (_, __) => AutoResizeAllColumnsToContent());
    }

    private void ColumnChooserDropDown_Closing(object? sender, ToolStripDropDownClosingEventArgs e)
    {
        if (!ColumnChooserMenuStaysOpen) return;
        if (e.CloseReason != ToolStripDropDownCloseReason.ItemClicked) return;
        if (!_columnChooserMenuKeepOpenRequested) return;
        _columnChooserMenuKeepOpenRequested = false;
        e.Cancel = true;
    }

    private bool IsColumnChooserVisibilityMenuItem(ToolStripItem? item)
    {
        return ColumnChooserMenuStaysOpen
            && item is ToolStripMenuItem menuItem
            && menuItem.CheckOnClick
            && menuItem.Tag is PanoColumn;
    }


    private void SetAllColumnsVisible(bool visible)
    {
        bool anyChanged = false;
        foreach (var col in Columns)
        {
            if (col.PrivateColumn || !col.AllowColumnChooser) continue;
            if (!visible && !col.CanBeHidden) continue;
            if (col.Visible == visible) continue;
            col.ApplyRuntimeVisible(visible);
            anyChanged = true;
        }
        if (!anyChanged) return;
        AutoSizeFillColumns();
        RefreshView();
        if (AutoSaveLayoutOnColumnVisibilityChange) QueueAutoSaveUserLayout();
    }
}

using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Filtering;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

/// <summary>
/// v25.59 sample: Menu Visibility Manager + checkbox + empty-list visibility.
/// Tek dosyalı tutuldu; .Designer.cs gerektirmez ve csproj SDK default include ile otomatik derlenir.
/// </summary>
public sealed class MenuVisibilityManagerSampleForm : Form
{
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        EmptyListMessage = "Kayıt bulunamadı / liste boş",
        ShowEmptyListMessage = true,
        EnableModernEmptyState = true,
        HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full,
        FilterMenuMode = PanoFilterMenuMode.Both,
        AllowEditAllCells = true,
        CellEditActivationKey = Keys.F2
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Bottom,
        Height = 58,
        Padding = new Padding(10),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly CheckedListBox _headerGroups = new()
    {
        Dock = DockStyle.Left,
        Width = 210,
        CheckOnClick = true,
        IntegralHeight = false
    };

    private readonly CheckedListBox _bodyGroups = new()
    {
        Dock = DockStyle.Left,
        Width = 210,
        CheckOnClick = true,
        IntegralHeight = false
    };

    private bool _syncing;
    private List<DemoRow> _rows = new();

    public MenuVisibilityManagerSampleForm()
    {
        Text = "Pano Menu Visibility Manager Örneği";
        Width = 1180;
        Height = 720;
        MinimumSize = new Size(900, 520);

        PanoWindowChrome.ApplyOnHandleCreated(this, () => Program.AppTheme, true);
        ApplyTheme(Program.AppTheme);

        ConfigureColumns();
        ConfigureToolbar();
        ConfigureGroupPanels();
        ConfigureUserContextMenu();

        Controls.Add(_grid);
        Controls.Add(CreateGroupHost());
        Controls.Add(_info);
        Controls.Add(_tool);

        LoadRows();
        SyncUiFromGrid();
        UpdateInfo();
    }

    private Control CreateGroupHost()
    {
        var host = new Panel { Dock = DockStyle.Right, Width = 430, Padding = new Padding(6) };
        var headerPanel = CreateGroupPanel("Header Menü Grupları", _headerGroups);
        var bodyPanel = CreateGroupPanel("Body/List Menü Grupları", _bodyGroups);
        bodyPanel.Dock = DockStyle.Fill;
        headerPanel.Dock = DockStyle.Left;
        headerPanel.Width = 210;
        host.Controls.Add(bodyPanel);
        host.Controls.Add(headerPanel);
        return host;
    }

    private static Panel CreateGroupPanel(string title, CheckedListBox list)
    {
        var panel = new Panel { Padding = new Padding(4) };
        var label = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(list);
        panel.Controls.Add(label);
        return panel;
    }

    private void ConfigureToolbar()
    {
        var profile = new ToolStripDropDownButton("Profil");
        AddProfileItem(profile, PanoMenuProfile.Full);
        AddProfileItem(profile, PanoMenuProfile.Standard);
        AddProfileItem(profile, PanoMenuProfile.Minimal);
        AddProfileItem(profile, PanoMenuProfile.ReadOnly);
        AddProfileItem(profile, PanoMenuProfile.None);
        AddProfileItem(profile, PanoMenuProfile.Custom);
        _tool.Items.Add(profile);

        _tool.Items.Add(new ToolStripButton("Header Menü Aç", null, (_, __) => _grid.ShowHeaderContextMenuForAspect(nameof(DemoRow.Name))));
        _tool.Items.Add(new ToolStripButton("Ad Filtre Popup", null, (_, __) => _grid.ShowFilterMenuForAspect(nameof(DemoRow.Name))));
        _tool.Items.Add(new ToolStripButton("Kolon Seç", null, (_, __) => _grid.ShowColumnChooser()));
        _tool.Items.Add(new ToolStripSeparator());

        _tool.Items.Add(new ToolStripButton("Built-in Header Aç/Kapat", null, (_, __) =>
        {
            _grid.UseBuiltInHeaderMenu = !_grid.UseBuiltInHeaderMenu;
            if (_grid.MenuProfile == PanoMenuProfile.None && _grid.UseBuiltInHeaderMenu) _grid.MenuProfile = PanoMenuProfile.Custom;
            UpdateInfo();
        }));

        _tool.Items.Add(new ToolStripButton("Built-in Body Aç/Kapat", null, (_, __) =>
        {
            _grid.UseBuiltInBodyMenu = !_grid.UseBuiltInBodyMenu;
            if (_grid.MenuProfile == PanoMenuProfile.None && _grid.UseBuiltInBodyMenu) _grid.MenuProfile = PanoMenuProfile.Custom;
            UpdateInfo();
        }));

        _tool.Items.Add(new ToolStripButton("Body Override Menü", null, (_, __) =>
        {
            if (_grid.BodyMenuOverride == null)
            {
                var custom = new ContextMenuStrip();
                custom.Items.Add("Özel menü: seçili satırı işle", null, (_, __) => MessageBox.Show(this, "Bu menü uygulama tarafından verildi."));
                custom.Items.Add("Özel menü: barkodu kopyala");
                _grid.BodyMenuOverride = custom;
            }
            else
            {
                _grid.BodyMenuOverride.Dispose();
                _grid.BodyMenuOverride = null;
            }
            UpdateInfo();
        }));

        _tool.Items.Add(new ToolStripSeparator());

        var mergePlacement = new ToolStripDropDownButton("Merge Konumu");
        AddMergePlacementItem(mergePlacement, PanoMenuMergePlacement.Top);
        AddMergePlacementItem(mergePlacement, PanoMenuMergePlacement.Bottom);
        AddMergePlacementItem(mergePlacement, PanoMenuMergePlacement.BeforeFirstSeparator);
        AddMergePlacementItem(mergePlacement, PanoMenuMergePlacement.AfterFirstSeparator);
        _tool.Items.Add(mergePlacement);

        var mergePresentation = new ToolStripDropDownButton("Merge Sunumu");
        AddMergePresentationItem(mergePresentation, PanoMenuMergePresentation.SubMenu);
        AddMergePresentationItem(mergePresentation, PanoMenuMergePresentation.Inline);
        AddMergePresentationItem(mergePresentation, PanoMenuMergePresentation.GroupedSubMenus);
        _tool.Items.Add(mergePresentation);

        _tool.Items.Add(new ToolStripButton("Öncelik: Filtre > Kolon > Pano", null, (_, __) =>
        {
            _grid.MergedMenuGroupOrder = "Filter,ColumnChooser,Clipboard,Sort,AutoSize,Layout,Theme";
            _grid.MergedMenuGroups = PanoMenuGroups.Filter | PanoMenuGroups.ColumnChooser | PanoMenuGroups.Clipboard | PanoMenuGroups.Sort | PanoMenuGroups.AutoSize | PanoMenuGroups.Layout | PanoMenuGroups.Theme;
            UpdateInfo();
        }));

        _tool.Items.Add(new ToolStripButton("Öncelik: Tüm Menüler", null, (_, __) =>
        {
            _grid.MergedMenuGroupOrder = "Mode,ViewMode,Filter,Sort,ColumnChooser,Clipboard,AutoSize,Freeze,Layout,Grouping,Editing,RowDetails,Analytics,FilterStyle,Theme";
            _grid.MergedMenuGroups = PanoMenuGroups.All;
            UpdateInfo();
        }));

        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Boş Liste Bilgisi Aç/Kapat", null, (_, __) =>
        {
            _grid.ShowEmptyListMessage = !_grid.ShowEmptyListMessage;
            _grid.Invalidate();
            UpdateInfo();
        }));
        _tool.Items.Add(new ToolStripButton("Listeyi Boşalt", null, (_, __) => { _grid.SetObjects(Array.Empty<DemoRow>()); UpdateInfo(); }));
        _tool.Items.Add(new ToolStripButton("Veri Yükle", null, (_, __) => LoadRows()));
        _tool.Items.Add(new ToolStripButton("Header Checkbox Aç/Kapat", null, (_, __) =>
        {
            var checkCol = _grid.Columns.FirstOrDefault(c => c.AspectName == nameof(DemoRow.Checked));
            if (checkCol != null)
            {
                checkCol.HeaderCheckBox = !checkCol.HeaderCheckBox;
                _grid.Invalidate();
            }
        }));
    }

    private void AddProfileItem(ToolStripDropDownButton menu, PanoMenuProfile p)
    {
        menu.DropDownItems.Add(p.ToString(), null, (_, __) =>
        {
            _grid.ApplyMenuProfile(p);
            SyncUiFromGrid();
            UpdateInfo();
        });
    }

    private void AddMergePlacementItem(ToolStripDropDownButton menu, PanoMenuMergePlacement placement)
    {
        menu.DropDownItems.Add(placement.ToString(), null, (_, __) =>
        {
            _grid.BuiltInMenuMergePlacement = placement;
            UpdateInfo();
        });
    }

    private void AddMergePresentationItem(ToolStripDropDownButton menu, PanoMenuMergePresentation presentation)
    {
        menu.DropDownItems.Add(presentation.ToString(), null, (_, __) =>
        {
            _grid.BuiltInMenuMergePresentation = presentation;
            UpdateInfo();
        });
    }

    private void ConfigureUserContextMenu()
    {
        var custom = new ContextMenuStrip();
        custom.Items.Add("Uygulama menüsü: seçili kaydı aç", null, (_, __) => MessageBox.Show(this, "Bu komut uygulamanın kendi ContextMenuStrip öğesi."));
        custom.Items.Add("Uygulama menüsü: özel işlem", null, (_, __) => MessageBox.Show(this, "Pano menüsü bu kullanıcı menüsüne seçilen konum ve sunuma göre merge edilir."));
        custom.Items.Add(new ToolStripSeparator());
        custom.Items.Add("Uygulama menüsü: en alttaki sabit komut");
        _grid.ContextMenuStrip = custom;
        _grid.MergeBuiltInMenuWithUserContextMenu = true;
        _grid.BuiltInMenuMergeText = "Pano menüleri";
        _grid.BuiltInMenuMergePlacement = PanoMenuMergePlacement.Top;
        _grid.BuiltInMenuMergePresentation = PanoMenuMergePresentation.SubMenu;
        _grid.MergedMenuGroupOrder = "Filter,ColumnChooser,Clipboard,Sort,AutoSize,Layout,Theme";
        _grid.MergedMenuGroups = PanoMenuGroups.Filter | PanoMenuGroups.ColumnChooser | PanoMenuGroups.Clipboard | PanoMenuGroups.Sort | PanoMenuGroups.AutoSize | PanoMenuGroups.Layout | PanoMenuGroups.Theme;
    }

    private void ConfigureGroupPanels()
    {
        foreach (PanoMenuGroups group in Enum.GetValues(typeof(PanoMenuGroups)))
        {
            if (group == PanoMenuGroups.None || group == PanoMenuGroups.All) continue;
            _headerGroups.Items.Add(group, true);
            _bodyGroups.Items.Add(group, true);
        }

        _headerGroups.ItemCheck += (_, e) => BeginInvoke(new Action(() => ApplyGroupsFromUi()));
        _bodyGroups.ItemCheck += (_, e) => BeginInvoke(new Action(() => ApplyGroupsFromUi()));
    }

    private void ApplyGroupsFromUi()
    {
        if (_syncing) return;
        _grid.HeaderMenuGroups = ReadGroups(_headerGroups);
        _grid.BodyMenuGroups = ReadGroups(_bodyGroups);
        if (_grid.MenuProfile != PanoMenuProfile.None)
            _grid.MenuProfile = PanoMenuProfile.Custom;
        UpdateInfo();
    }

    private static PanoMenuGroups ReadGroups(CheckedListBox list)
    {
        PanoMenuGroups result = PanoMenuGroups.None;
        foreach (var item in list.CheckedItems)
            if (item is PanoMenuGroups group) result |= group;
        return result;
    }

    private void SyncUiFromGrid()
    {
        _syncing = true;
        try
        {
            SyncList(_headerGroups, _grid.HeaderMenuGroups);
            SyncList(_bodyGroups, _grid.BodyMenuGroups);
        }
        finally
        {
            _syncing = false;
        }
    }

    private static void SyncList(CheckedListBox list, PanoMenuGroups groups)
    {
        for (int i = 0; i < list.Items.Count; i++)
        {
            if (list.Items[i] is PanoMenuGroups group)
                list.SetItemChecked(i, (groups & group) == group);
        }
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Add(new PanoColumn("✓", nameof(DemoRow.Checked), 46)
        {
            Kind = PanoColumnKind.CheckBox,
            HeaderCheckBox = true,
            HeaderCheckBoxThreeState = true,
            AspectPutter = (row, value) => ((DemoRow)row).SetChecked(Convert.ToBoolean(value))
        });

        _grid.Columns.Add(new PanoColumn("Id", nameof(DemoRow.Id), 70));
        _grid.Columns.Add(new PanoColumn("Ad", nameof(DemoRow.Name), 220) { Editable = true, FillFreeSpace = true });
        _grid.Columns.Add(new PanoColumn("Meslek", nameof(DemoRow.Occupation), 150));
        _grid.Columns.Add(new PanoColumn("Durum", nameof(DemoRow.State), 110) { Kind = PanoColumnKind.Badge });
        _grid.Columns.Add(new PanoColumn("İlerleme", nameof(DemoRow.Progress), 130) { Kind = PanoColumnKind.ProgressBar });
    }

    private void LoadRows()
    {
        _rows = CreateRows(200);
        _grid.SetObjects(_rows);
        UpdateInfo();
    }

    private static List<DemoRow> CreateRows(int count)
    {
        string[] jobs = { "AOI", "Operator", "Quality", "Repair", "Engineer", "Planning" };
        return Enumerable.Range(1, count).Select(i => new DemoRow
        {
            Id = i,
            Name = "Menu test kayıt " + i,
            Occupation = jobs[i % jobs.Length],
            State = i % 7 == 0 ? "Fail" : i % 5 == 0 ? "Review" : "OK",
            Progress = (i * 9) % 101,
            Checked = i % 6 == 0,
            Rating = i % 6
        }).ToList();
    }

    private void UpdateInfo()
    {
        _info.Text =
            $"Profil={_grid.MenuProfile} | Header={_grid.UseBuiltInHeaderMenu} | Body={_grid.UseBuiltInBodyMenu} | " +
            $"HeaderGroups={_grid.HeaderMenuGroups} | BodyGroups={_grid.BodyMenuGroups} | " +
            $"Merge={_grid.BuiltInMenuMergePlacement}/{_grid.BuiltInMenuMergePresentation} | " +
            $"MergedGroups={_grid.MergedMenuGroups} | EmptyInfo={_grid.ShowEmptyListMessage} | BodyOverride={_grid.BodyMenuOverride != null}";
    }

    private void ApplyTheme(PanoTheme theme)
    {
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _tool.BackColor = theme.HeaderBackColor;
        _tool.ForeColor = theme.HeaderForeColor;
        _tool.Renderer = new SmartMenuRenderer(theme);
        _info.BackColor = theme.PanelBackColor;
        _info.ForeColor = theme.ForeColor;
        _headerGroups.BackColor = theme.PanelBackColor;
        _headerGroups.ForeColor = theme.ForeColor;
        _bodyGroups.BackColor = theme.PanelBackColor;
        _bodyGroups.ForeColor = theme.ForeColor;
        _grid.ApplyTheme(theme);
        PanoWindowChrome.Apply(this, theme, true);
    }
}


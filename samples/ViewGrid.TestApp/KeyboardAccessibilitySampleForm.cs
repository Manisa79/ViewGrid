using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Filtering;
using ViewGrid.Theming;

namespace ViewGrid.TestApp;

public sealed class KeyboardAccessibilitySampleForm : Form
{
    private readonly Label _help = new()
    {
        Dock = DockStyle.Top,
        Height = 104,
        Padding = new Padding(12),
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "ViewGrid klavye erişimi: Ok/Page/Home/End satır gezinme, Space seçili satır checkbox durumunu değiştirir, çoklu seçimde Space seçili satırları birlikte işaretler/kaldırır, Enter checkbox toggle yapmaz; button/default aksiyon içindir. Ctrl+Shift+Space header checkbox ile tümünü seçer. Ctrl+Left/Right aktif kolon, Alt+Down kolon menüsü, Ctrl+Shift+F filtre, Ctrl+Shift+L kolon seçici, Ctrl+Shift+R aktif kolon sığdır, Ctrl+Shift+Plus tüm kolonları sığdır, Ctrl+G grupla, Ctrl+Shift+G gruplamayı temizle, F3/Shift+F3 arama. Bu örnekte hücre edit açıkça F2 ile etkinleştirilmiştir."
    };

    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        CheckBoxes = true,
        FullRowSelect = true,
        MultiSelect = true,
        EnableCellEditing = true,
        CellEditActivationKey = Keys.F2,
        ShowEditCellMenuItem = true,
        EditCellMenuText = "Hücreyi Düzenle",
        EditCellMenuShortcutText = "F2",
        AllowEditAllCells = true,
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        FilterMenuMode = ViewGridFilterMenuMode.Both,
        EnableKeyboardAccessibilityShortcuts = true,
        KeyboardColumnNavigationEnabled = true,
        KeyboardColumnFilterShortcutEnabled = true,
        KeyboardColumnChooserShortcutEnabled = true,
        KeyboardAutoSizeShortcutEnabled = true,
        KeyboardHeaderCheckBoxShortcutEnabled = true,
        KeyboardSpaceTogglesCheckBoxes = true,
        KeyboardSpaceTogglesSelectedRows = true,
        KeyboardGroupingShortcutsEnabled = true,
        KeyboardFindNextShortcutEnabled = true
    };

    public KeyboardAccessibilitySampleForm()
    {
        Text = "ViewGrid Klavye Erişimi Örneği";
        Width = 1100;
        Height = 680;
        MinimumSize = new Size(840, 520);

        var name = new ViewGridColumn("Person", nameof(KeyboardRow.Person), 180)
        {
            HeaderCheckBox = true,
            HeaderCheckBoxUpdatesRowCheckBoxes = true
        };

        _grid.Columns.Add(name);
        _grid.Columns.Add(new ViewGridColumn("Occupation", nameof(KeyboardRow.Occupation), 160));
        _grid.Columns.Add(new ViewGridColumn("Status", nameof(KeyboardRow.Status), 120));
        _grid.Columns.Add(new ViewGridColumn("Note", nameof(KeyboardRow.Note), 360) { FillsFreeSpace = true });
        _grid.Columns.Add(new ViewGridColumn("Action", nameof(KeyboardRow.Action), 100) { Kind = ViewGridColumnKind.Button, ButtonText = "Aç" });

        _grid.SetObjects(new[]
        {
            new KeyboardRow("Wilhelm", "Technician", "Active", "Space ile satır checkbox değişir; Enter checkbox toggle yapmaz.", "Open"),
            new KeyboardRow("Alana", "Operator", "Waiting", "Ctrl+Shift+F aktif kolon filtresini açar; Ctrl+Shift+Space tümünü işaretler.", "Open"),
            new KeyboardRow("Mehmet", "Quality", "Done", "Ctrl+Left/Right aktif kolonu değiştirir.", "Open"),
            new KeyboardRow("Ayşe", "Engineer", "Active", "F2 hücre düzenleme; Enter button hücresini çalıştırır.", "Open"),
            new KeyboardRow("Güner", "Developer", "Testing", "Alt+Down / Shift+F10 menü erişimini gösterir.", "Open")
        });

        _grid.ButtonClick += (_, e) => MessageBox.Show(this, $"Klavye/mouse action: {((KeyboardRow)e.RowObject).Person}", "ViewGrid");

        Controls.Add(_grid);
        Controls.Add(_help);
        ApplyTheme(WindowsThemeService.CurrentTheme());
    }

    private void ApplyTheme(ViewGridTheme theme)
    {
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _help.BackColor = theme.HeaderBackColor;
        _help.ForeColor = theme.ForeColor;
        _grid.ApplyTheme(theme);
    }

    private sealed record KeyboardRow(string Person, string Occupation, string Status, string Note, string Action);
}

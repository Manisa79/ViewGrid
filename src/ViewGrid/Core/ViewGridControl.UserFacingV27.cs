using System.ComponentModel;
using ViewGrid.Layout;
using ViewGrid.State;

namespace ViewGrid.Core;

/// <summary>
/// v27.2 user-facing layer: state/scenario/preset features exposed through properties,
/// public methods and built-in context menus so host apps can use the new engine without
/// writing custom toolbar code.
/// </summary>
public partial class ViewGridControl
{
    [Category("ViewGrid - v27 User Features")]
    [DefaultValue(ViewGridScenario.DataTable)]
    [Description("Hazır ViewGrid kullanım senaryosu. Runtime menüden veya designer property penceresinden değiştirilebilir.")]
    public ViewGridScenario ActiveScenario { get; set; } = ViewGridScenario.DataTable;

    [Category("ViewGrid - v27 User Features")]
    [DefaultValue(false)]
    [Description("Kontrol oluşturulduğunda AutoStateFilePath doluysa state otomatik yüklenir.")]
    public bool AutoLoadStateOnCreate { get; set; }

    [Category("ViewGrid - v27 User Features")]
    [DefaultValue(false)]
    [Description("Kontrol dispose edilirken AutoStateFilePath doluysa state otomatik kaydedilir.")]
    public bool AutoSaveStateOnDispose { get; set; }

    [Category("ViewGrid - v27 User Features")]
    [DefaultValue("")]
    [Description("Otomatik veya menüden kaydet/yükle için varsayılan state json dosya yolu.")]
    public string AutoStateFilePath { get; set; } = string.Empty;

    [Category("ViewGrid - v27 User Features")]
    [DefaultValue(true)]
    [Description("Başlık/gövde menülerinde state kaydet/yükle/preset komutlarını gösterir.")]
    public bool ShowStateMenuItems { get; set; } = true;

    [Category("ViewGrid - v27 User Features")]
    [DefaultValue(true)]
    [Description("Başlık/gövde menülerinde hazır görünüm senaryosu komutlarını gösterir.")]
    public bool ShowScenarioMenuItems { get; set; } = true;

    [Category("ViewGrid - v27 User Features")]
    [DefaultValue(true)]
    [Description("State menüsünde hızlı layout sıfırlama komutunu gösterir.")]
    public bool ShowResetStateMenuItem { get; set; } = true;

    [Category("ViewGrid - v27 User Features")]
    [DefaultValue(true)]
    [Description("Senaryo değiştirildiğinde ilgili ViewMode, RowHeight ve kart ölçüleri otomatik uygulanır.")]
    public bool ScenarioAppliesViewDefaults { get; set; } = true;

    [Category("ViewGrid - v27 User Features")]
    [DefaultValue("")]
    [Description("Kullanıcı presetlerinin saklanacağı klasör. Boşsa LocalAppData/ViewGrid/States kullanılır.")]
    public string UserStatePresetFolder { get; set; } = string.Empty;

    private bool _autoStateLoaded;


    protected override void Dispose(bool disposing)
    {
        if (disposing)
            TryAutoSaveV27State();
        base.Dispose(disposing);
    }

    /// <summary>
    /// Hazır senaryo varsayılanlarını uygular. Eski örnekler ve dış projeler için tek parametreli çağrı korunur.
    /// </summary>
    public void ApplyScenario(ViewGridScenario scenario)
        => ApplyScenario(scenario, updateActiveScenario: true);

    /// <summary>
    /// Hazır senaryo varsayılanlarını uygular. updateActiveScenario false olduğunda aktif senaryo property değeri korunur.
    /// </summary>
    public void ApplyScenario(ViewGridScenario scenario, bool updateActiveScenario)
    {
        if (updateActiveScenario)
            ActiveScenario = scenario;

        if (ScenarioAppliesViewDefaults)
        {
            using var _ = SuspendViewModeMemory();
            ViewGridScenarioExtensions.ApplyScenario(this, scenario);
        }

        TryRestoreRememberedViewMode(scenario);

        Invalidate();
    }

    public void ApplyActiveScenario()
    {
        ApplyScenario(ActiveScenario, updateActiveScenario: true);
    }

    public string ResolveStateFilePath(string fallbackName = "viewgrid-state")
    {
        if (!string.IsNullOrWhiteSpace(AutoStateFilePath))
            return AutoStateFilePath;

        string safeName = MakeSafeFileName(string.IsNullOrWhiteSpace(Name) ? fallbackName : Name);
        return Path.Combine(GetUserStatePresetFolder(), safeName + ".json");
    }

    public void SaveStateToDefaultPath(string name = "")
    {
        SaveState(ResolveStateFilePath(), string.IsNullOrWhiteSpace(name) ? Name : name);
    }

    public bool LoadStateFromDefaultPath()
    {
        return LoadState(ResolveStateFilePath());
    }

    public void SaveStatePreset(string presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName))
            throw new ArgumentException("Preset adı boş olamaz.", nameof(presetName));

        string path = GetStatePresetPath(presetName);
        SaveState(path, presetName);
    }

    public bool LoadStatePreset(string presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName)) return false;
        return LoadState(GetStatePresetPath(presetName));
    }

    public IReadOnlyList<string> GetStatePresetNames()
    {
        string folder = GetUserStatePresetFolder();
        if (!Directory.Exists(folder)) return Array.Empty<string>();
        return Directory.EnumerateFiles(folder, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
            .ToList()!;
    }

    public void ResetRuntimeState(bool clearFilters = true, bool clearGrouping = true, bool resetColumns = false)
    {
        if (clearFilters)
            ClearFilters();
        if (clearGrouping)
            ClearGrouping();
        if (resetColumns)
        {
            foreach (var column in Columns)
            {
                column.ApplyRuntimeVisible(true);
                if (column.DefaultWidth > 0)
                    column.Width = column.DefaultWidth;
            }
        }
        RefreshView();
    }

    private void TryAutoLoadV27State()
    {
        if (_autoStateLoaded || !AutoLoadStateOnCreate) return;
        _autoStateLoaded = true;
        try { LoadStateFromDefaultPath(); }
        catch { /* host uygulama açılışını state dosyası yüzünden bozma */ }
    }

    private void TryAutoSaveV27State()
    {
        if (!AutoSaveStateOnDispose) return;
        try { SaveStateToDefaultPath(); }
        catch { /* dispose sırasında exception fırlatma */ }
    }

    private string GetUserStatePresetFolder()
    {
        if (!string.IsNullOrWhiteSpace(UserStatePresetFolder))
            return UserStatePresetFolder;

        string appName = Application.ProductName;
        if (string.IsNullOrWhiteSpace(appName)) appName = "ViewGrid";
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName, "ViewGrid", "States");
    }

    private string GetStatePresetPath(string presetName)
    {
        string folder = GetUserStatePresetFolder();
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, MakeSafeFileName(presetName) + ".json");
    }

    private void AddV27UserFeatureMenus(ToolStripItemCollection items)
    {
        if (ShowScenarioMenuItems)
            AddScenarioMenu(items);
        if (ShowStateMenuItems)
            AddStateMenu(items);
    }

    private void AddScenarioMenu(ToolStripItemCollection items)
    {
        var menu = new ToolStripMenuItem("Görünüm Senaryosu");
        bool mediaSeparatorAdded = false;
        foreach (ViewGridScenario scenario in Enum.GetValues(typeof(ViewGridScenario)))
        {
            if (scenario.IsMediaScenario() && !mediaSeparatorAdded)
            {
                menu.DropDownItems.Add(new ToolStripSeparator());
                var mediaHeader = new ToolStripMenuItem("Medya / Görsel Senaryolar") { Enabled = false };
                menu.DropDownItems.Add(mediaHeader);
                mediaSeparatorAdded = true;
            }

            var item = new ToolStripMenuItem(scenario.GetDisplayName())
            {
                Checked = scenario == ActiveScenario,
                Tag = scenario
            };
            item.Click += (_, _) => ApplyScenario((ViewGridScenario)item.Tag!, updateActiveScenario: true);
            menu.DropDownItems.Add(item);
        }
        items.Add(menu);
    }

    private void AddStateMenu(ToolStripItemCollection items)
    {
        var menu = new ToolStripMenuItem("State / Preset");
        menu.DropDownItems.Add("Varsayılan State Kaydet", null, (_, _) => SaveStateToDefaultPath());
        menu.DropDownItems.Add("Varsayılan State Yükle", null, (_, _) => LoadStateFromDefaultPath()).Enabled = File.Exists(ResolveStateFilePath());
        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add("Dosyaya Kaydet...", null, (_, _) => SaveStateWithDialog());
        menu.DropDownItems.Add("Dosyadan Yükle...", null, (_, _) => LoadStateWithDialog());
        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add("Preset Olarak Kaydet...", null, (_, _) => SaveStatePresetWithDialog());

        var presetsMenu = new ToolStripMenuItem("Preset Yükle");
        foreach (string presetName in GetStatePresetNames())
        {
            string captured = presetName;
            presetsMenu.DropDownItems.Add(captured, null, (_, _) => LoadStatePreset(captured));
        }
        presetsMenu.Enabled = presetsMenu.DropDownItems.Count > 0;
        menu.DropDownItems.Add(presetsMenu);

        if (ShowResetStateMenuItem)
        {
            menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add("Filtre / Gruplamayı Temizle", null, (_, _) => ResetRuntimeState(clearFilters: true, clearGrouping: true, resetColumns: false));
            menu.DropDownItems.Add("Görünümü Sıfırla", null, (_, _) => ResetRuntimeState(clearFilters: true, clearGrouping: true, resetColumns: true));
        }

        items.Add(menu);
    }

    private void SaveStateWithDialog()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "ViewGrid state (*.json)|*.json|Tüm dosyalar (*.*)|*.*",
            FileName = MakeSafeFileName(string.IsNullOrWhiteSpace(Name) ? "viewgrid-state" : Name) + ".json",
            AddExtension = true,
            DefaultExt = "json"
        };
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            SaveState(dialog.FileName, Name);
    }

    private void LoadStateWithDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "ViewGrid state (*.json)|*.json|Tüm dosyalar (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            LoadState(dialog.FileName);
    }

    private void SaveStatePresetWithDialog()
    {
        string? presetName = ViewGridPresetNameDialog.ShowDialog(FindForm(), "Preset adı", string.IsNullOrWhiteSpace(Name) ? "ViewGridPreset" : Name);
        if (!string.IsNullOrWhiteSpace(presetName))
            SaveStatePreset(presetName);
    }
}

internal sealed class ViewGridPresetNameDialog : Form
{
    private readonly TextBox _textBox = new() { Left = 12, Top = 38, Width = 330, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
    private readonly Button _ok = new() { Text = "Tamam", DialogResult = DialogResult.OK, Width = 92, Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
    private readonly Button _cancel = new() { Text = "İptal", DialogResult = DialogResult.Cancel, Width = 92, Anchor = AnchorStyles.Right | AnchorStyles.Bottom };

    private ViewGridPresetNameDialog(string caption, string defaultValue)
    {
        Text = caption;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(360, 118);

        var label = new Label { Left = 12, Top = 12, Width = 330, Text = "Preset adı:" };
        _textBox.Text = defaultValue;
        _ok.Left = ClientSize.Width - 200;
        _ok.Top = ClientSize.Height - 38;
        _cancel.Left = ClientSize.Width - 102;
        _cancel.Top = ClientSize.Height - 38;
        AcceptButton = _ok;
        CancelButton = _cancel;

        Controls.Add(label);
        Controls.Add(_textBox);
        Controls.Add(_ok);
        Controls.Add(_cancel);
    }

    public static string? ShowDialog(IWin32Window? owner, string caption, string defaultValue)
    {
        using var form = new ViewGridPresetNameDialog(caption, defaultValue);
        return form.ShowDialog(owner) == DialogResult.OK ? form._textBox.Text.Trim() : null;
    }
}

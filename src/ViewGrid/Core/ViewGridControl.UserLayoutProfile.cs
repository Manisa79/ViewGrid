using System.ComponentModel;
using System.Text.RegularExpressions;
using ViewGrid.Layout;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    private System.Windows.Forms.Timer? _userLayoutSaveTimer;
    private bool _userLayoutLoadedOnce;

    [Category("ViewGrid - User Layout"), DefaultValue(true)]
    [Description("Kullanıcı kolon/sort/filtre/görünüm ayarlarını otomatik kaydeder.")]
    public bool AutoSaveUserLayout { get; set; } = true;

    [Category("ViewGrid - User Layout"), DefaultValue(true)]
    [Description("Kontrol handle oluşturulduğunda kullanıcı düzenini otomatik yükler.")]
    public bool AutoLoadUserLayout { get; set; } = true;

    [Category("ViewGrid - User Layout"), DefaultValue(650)]
    [Description("Otomatik kaydetme gecikmesi. Sık değişikliklerde dosya yazma baskısını azaltır.")]
    public int AutoSaveUserLayoutDebounceMs { get; set; } = 650;

    [Category("ViewGrid - User Layout"), DefaultValue(null)]
    [Description("Kullanıcı düzeni için benzersiz anahtar. Boşsa FormAdı.KontrolAdı kullanılır.")]
    public string? UserLayoutKey { get; set; }

    [Category("ViewGrid - User Layout"), DefaultValue(null)]
    [Description("Kullanıcı düzenlerinin saklanacağı klasör. Boşsa %AppData%\\ViewGrid\\UserLayouts kullanılır.")]
    public string? UserLayoutFolder { get; set; }

    private bool _persistColumnFilters;

    [Category("ViewGrid - User Layout")]
    [DefaultValue(false)]
    [Description("Layout kaydedilirken/yüklenirken aktif global arama ve kolon filtrelerinin de saklanıp saklanmayacağını belirler. False ise kolon düzeni korunur ancak filtreler her açılışta temiz gelir.")]
    public bool PersistColumnFilters
    {
        get => _persistColumnFilters;
        set => SetPersistColumnFilters(value, clearExistingFiltersWhenDisabled: true, autoSaveWhenRuntimeChanged: true);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool PersistFiltersInLayout
    {
        get => PersistColumnFilters;
        set => PersistColumnFilters = value;
    }

    private void SetPersistColumnFilters(bool value, bool clearExistingFiltersWhenDisabled, bool autoSaveWhenRuntimeChanged)
    {
        if (_persistColumnFilters == value) return;

        _persistColumnFilters = value;

        if (!_persistColumnFilters && clearExistingFiltersWhenDisabled && !DesignMode && !IsDisposed)
        {
            ClearFilters();
        }

        if (autoSaveWhenRuntimeChanged && !DesignMode && !IsDisposed)
        {
            QueueAutoSaveUserLayout();
        }
    }

    [Category("ViewGrid - User Layout"), DefaultValue(true)]
    [Description("Filtre stili, filtre/sort ikon stili, satır yüksekliği ve view mode gibi görsel tercihleri saklar.")]
    public bool PersistVisualPreferences { get; set; } = true;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        TryAutoLoadUserLayoutOnce();
    }

    internal void QueueAutoSaveUserLayout()
    {
        if (!AutoSaveUserLayout || DesignMode || IsDisposed) return;
        if (_userLayoutSaveTimer == null)
        {
            _userLayoutSaveTimer = new System.Windows.Forms.Timer();
            _userLayoutSaveTimer.Tick += (_, __) =>
            {
                _userLayoutSaveTimer.Stop();
                try { SaveUserLayout(); } catch { }
            };
        }
        _userLayoutSaveTimer.Interval = Math.Max(100, AutoSaveUserLayoutDebounceMs);
        _userLayoutSaveTimer.Stop();
        _userLayoutSaveTimer.Start();
    }

    private void TryAutoLoadUserLayoutOnce()
    {
        if (_userLayoutLoadedOnce || !AutoLoadUserLayout || DesignMode) return;
        _userLayoutLoadedOnce = true;
        try { LoadUserLayout(); } catch { }
    }

    public string GetUserLayoutFilePath(string? profileName = null)
    {
        var folder = string.IsNullOrWhiteSpace(UserLayoutFolder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ViewGrid", "UserLayouts")
            : UserLayoutFolder!;
        Directory.CreateDirectory(folder);
        var key = string.IsNullOrWhiteSpace(UserLayoutKey) ? GetDefaultUserLayoutKey() : UserLayoutKey!;
        if (!string.IsNullOrWhiteSpace(profileName)) key += "." + profileName;
        return Path.Combine(folder, SanitizeFileName(key) + ".viewgrid-layout.json");
    }

    public void SaveUserLayout(string? profileName = null)
    {
        SaveLayout(GetUserLayoutFilePath(profileName));
    }

    public bool LoadUserLayout(string? profileName = null)
    {
        var path = GetUserLayoutFilePath(profileName);
        if (!File.Exists(path)) return false;
        LoadLayout(path);
        return true;
    }

    public bool ResetUserLayout(string? profileName = null)
    {
        var path = GetUserLayoutFilePath(profileName);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    public string ExportUserLayout(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Dosya yolu boş olamaz.", nameof(filePath));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        SaveLayout(filePath);
        return filePath;
    }

    public bool ImportUserLayout(string filePath, bool applyImmediately = true)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;
        File.Copy(filePath, GetUserLayoutFilePath(), overwrite: true);
        if (applyImmediately) LoadUserLayout();
        return true;
    }

    public void ExportUserLayoutWithDialog()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "ViewGrid kullanıcı düzenini dışa aktar",
            Filter = "ViewGrid Layout (*.viewgrid-layout.json)|*.viewgrid-layout.json|JSON (*.json)|*.json|Tüm dosyalar (*.*)|*.*",
            FileName = SanitizeFileName((string.IsNullOrWhiteSpace(UserLayoutKey) ? GetDefaultUserLayoutKey() : UserLayoutKey!) + ".viewgrid-layout.json")
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) ExportUserLayout(dlg.FileName);
    }

    public void ImportUserLayoutWithDialog()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "ViewGrid kullanıcı düzenini içe aktar",
            Filter = "ViewGrid Layout (*.viewgrid-layout.json;*.json)|*.viewgrid-layout.json;*.json|Tüm dosyalar (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) ImportUserLayout(dlg.FileName, applyImmediately: true);
    }

    private string GetDefaultUserLayoutKey()
    {
        var form = FindForm();
        var formName = form == null ? "Form" : form.GetType().FullName ?? form.Name;
        var controlName = string.IsNullOrWhiteSpace(Name) ? GetType().Name : Name;
        return formName + "." + controlName;
    }

    private static string SanitizeFileName(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "ViewGridLayout" : value.Trim();
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return Regex.Replace(value, "\\s+", "_");
    }
}

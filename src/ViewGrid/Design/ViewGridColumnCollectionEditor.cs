using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using ViewGrid.Columns;
using ViewGrid.Theming;

namespace ViewGrid.Design;

/// <summary>
/// ViewGrid kolon editorü.
///
/// Not: Önceki sürümlerde System.ComponentModel.Design.CollectionEditor kullanılıyordu.
/// Bazı VS/.NET 10 designer kombinasyonlarında CollectionEditor sessizce exception yutup
/// pencereyi hiç açmayabiliyordu. Bu editor kendi modal formunu açar ve
/// SmartTag/DesignerVerb üzerinden tek kolon düzenleme yolu olarak kullanılır.
/// </summary>
public sealed class ViewGridColumnCollectionEditor : UITypeEditor
{
    // WinForms designer / ViewGridControlDesigner tarafı bazen editor
    // örneğini CollectionEditor gibi Type parametresiyle oluşturur.
    // UITypeEditor için parametresiz ctor yeterlidir; bu overload ise
    // eski çağrılarla binary/source uyumluluk sağlar.
    public ViewGridColumnCollectionEditor()
    {
    }

    public ViewGridColumnCollectionEditor(Type collectionType)
    {
        CollectionType = collectionType;
    }

    public Type? CollectionType { get; }

    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        => UITypeEditorEditStyle.Modal;

    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    {
        if (value is not ViewGridColumnCollection columns)
            return value;

        ViewGridColumnCollectionEditor.EditColumns(
            columns,
            context?.Instance,
            context?.PropertyDescriptor,
            provider,
            (context?.Instance as System.Windows.Forms.Control)?.FindForm());

        return value;
    }

    internal static bool EditColumns(
        ViewGridColumnCollection columns,
        object? ownerComponent,
        PropertyDescriptor? columnsProperty,
        IServiceProvider? provider,
        System.Windows.Forms.IWin32Window? owner = null,
        bool centerScreen = false)
    {
        var changeService = provider?.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
        var host = provider?.GetService(typeof(IDesignerHost)) as IDesignerHost;

        using var tx = host?.CreateTransaction("ViewGridControl kolonları düzenlendi");
        try
        {
            if (ownerComponent != null)
                changeService?.OnComponentChanging(ownerComponent, columnsProperty);

            EnsureDesignerSitedColumns(columns, host);

            using var form = new ViewGridColumnEditorForm(columns, host);
            if (centerScreen)
            {
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                form.ShowInTaskbar = true;
                form.TopMost = true;
                form.Shown += (_, _) =>
                {
                    form.Activate();
                    form.BringToFront();
                    form.TopMost = false;
                };
            }

            var result = owner != null ? form.ShowDialog(owner) : form.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                columns.NormalizeNames();
                if (ownerComponent != null)
                    changeService?.OnComponentChanged(ownerComponent, columnsProperty, null, null);

                tx?.Commit();
                return true;
            }

            tx?.Cancel();
            return false;
        }
        catch (Exception ex)
        {
            tx?.Cancel();
            System.Windows.Forms.MessageBox.Show(
                ex.ToString(),
                "ViewGrid Kolon Editor Hatası",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
            return false;
        }
    }

    private static void EnsureDesignerSitedColumns(ViewGridColumnCollection columns, IDesignerHost? host)
    {
        if (host == null || columns.Count == 0) return;

        for (int i = 0; i < columns.Count; i++)
        {
            var current = columns[i];
            if (current == null) continue;
            if (current is IComponent component && component.Site != null)
            {
                if (string.IsNullOrWhiteSpace(current.Name))
                    current.Name = component.Site.Name ?? string.Empty;
                continue;
            }

            var desiredName = !string.IsNullOrWhiteSpace(current.Name)
                ? current.Name
                : CreateUniqueName(columns, host, i + 1);
            desiredName = ViewGridColumnNameHelper.EnsureUnique(desiredName, columns.Where((_, idx) => idx != i));

            current.Name = desiredName;
        }
    }

    internal static void CopyColumn(ViewGridColumn source, ViewGridColumn target)
    {
        var sourceProperties = TypeDescriptor.GetProperties(source, true);
        var targetProperties = TypeDescriptor.GetProperties(target, true);

        foreach (PropertyDescriptor sourceProperty in sourceProperties)
        {
            if (sourceProperty.IsReadOnly)
                continue;

            if (string.Equals(sourceProperty.Name, nameof(IComponent.Site), StringComparison.Ordinal) ||
                string.Equals(sourceProperty.Name, nameof(Component.Container), StringComparison.Ordinal))
                continue;

            var targetProperty = targetProperties[sourceProperty.Name];
            if (targetProperty == null || targetProperty.IsReadOnly)
                continue;

            if (!targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                continue;

            try
            {
                targetProperty.SetValue(target, sourceProperty.GetValue(source));
            }
            catch
            {
                // Design-time conversion/special properties should not prevent
                // preserving the rest of the column definition.
            }
        }

        target.Name = source.Name;
    }

    internal static GLVColumn CreateDesignerColumn(ViewGridColumnCollection columns, IDesignerHost? host)
    {
        var name = CreateUniqueName(columns, host, ViewGridColumnNameHelper.GetNextDefaultOrdinal(columns));

        var column = new GLVColumn();

        column.Name = name;
        column.Header = "Column";
        column.AspectName = string.Empty;
        column.Width = 120;
        column.DefaultWidth = 120;
        return column;
    }

    private static string CreateUniqueName(ViewGridColumnCollection columns, IDesignerHost? host, int startIndex)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns)
            if (!string.IsNullOrWhiteSpace(column.Name)) used.Add(column.Name);

        if (host?.Container != null)
        {
            foreach (IComponent component in host.Container.Components)
                if (!string.IsNullOrWhiteSpace(component.Site?.Name)) used.Add(component.Site.Name);
        }

        var i = Math.Max(1, startIndex);
        while (true)
        {
            var name = ViewGridColumnNameHelper.CreateDefaultName(i);
            if (!used.Contains(name)) return name;
            i++;
        }
    }
}

internal sealed class ViewGridColumnEditorForm : System.Windows.Forms.Form
{
    private readonly ViewGridColumnCollection _columns;
    private readonly IDesignerHost? _host;
    private readonly System.Windows.Forms.ListBox _list = new();
    private readonly System.Windows.Forms.PropertyGrid _propertyGrid = new();
    private readonly System.Windows.Forms.Button _btnAdd = new();
    private readonly System.Windows.Forms.Button _btnRemove = new();
    private readonly System.Windows.Forms.Button _btnUp = new();
    private readonly System.Windows.Forms.Button _btnDown = new();
    private readonly System.Windows.Forms.Button _btnOk = new();
    private readonly System.Windows.Forms.Button _btnCancel = new();
    private readonly List<OriginalColumnEntry> _originalColumns = new();
    private readonly HashSet<ViewGridColumn> _createdColumns = new();
    private bool _committed;
    private bool _restored;

    public ViewGridColumnEditorForm(ViewGridColumnCollection columns, IDesignerHost? host)
    {
        _columns = columns;
        _host = host;

        Text = "ViewGrid Kolonları";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Width = 760;
        Height = 520;
        ViewGridDialogChrome.ConfigureStandardDialog(this, WindowsThemeService.CurrentTheme(), new System.Drawing.Size(640, 420), sizeable: true, iconKind: ViewGridDialogIconKind.Column);

        var root = new System.Windows.Forms.TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new System.Windows.Forms.Padding(10)
        };
        root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 270));
        root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100));
        root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46));
        Controls.Add(root);

        var leftPanel = new System.Windows.Forms.TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100));
        leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40));
        root.Controls.Add(leftPanel, 0, 0);

        _list.Dock = System.Windows.Forms.DockStyle.Fill;
        _list.IntegralHeight = false;
        // DisplayMember kullanmıyoruz; ListBox item text'i doğrudan ViewGridColumn.ToString()
        // üzerinden alınır. Böylece PrivateColumn/AspectName değişince ReloadList sonrası
        // designer listesindeki [Private] etiketi kaybolmadan yenilenir.
        _list.SelectedIndexChanged += (_, _) => UpdateSelectedColumn();
        leftPanel.Controls.Add(_list, 0, 0);

        var listButtons = new System.Windows.Forms.TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = System.Windows.Forms.Padding.Empty,
            Padding = System.Windows.Forms.Padding.Empty
        };
        listButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25));
        listButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25));
        listButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25));
        listButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25));
        listButtons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100));
        leftPanel.Controls.Add(listButtons, 0, 1);

        InitButton(_btnAdd, "Ekle", (_, _) => AddColumn());
        InitButton(_btnRemove, "Sil", (_, _) => RemoveColumn());
        InitButton(_btnUp, "Yukarı", (_, _) => MoveColumn(-1));
        InitButton(_btnDown, "Aşağı", (_, _) => MoveColumn(1));
        listButtons.Controls.Add(_btnAdd, 0, 0);
        listButtons.Controls.Add(_btnRemove, 1, 0);
        listButtons.Controls.Add(_btnUp, 2, 0);
        listButtons.Controls.Add(_btnDown, 3, 0);

        _propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        _propertyGrid.PropertyValueChanged += (_, _) =>
        {
            // PrivateColumn=true olduğunda kolon grid/design yüzeyinde gizlenir, fakat
            // bu editor listesinde görünür kalmalıdır. Listeyi tüm _columns üzerinden
            // yeniden doldurup seçimi referansla koruyoruz.
            RefreshListPreserveSelection();
        };
        root.Controls.Add(_propertyGrid, 1, 0);

        var bottom = new System.Windows.Forms.FlowLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft,
            WrapContents = false
        };
        root.SetColumnSpan(bottom, 2);
        root.Controls.Add(bottom, 0, 1);

        InitButton(_btnOk, "Tamam", (_, _) => { CommitColumns(); DialogResult = System.Windows.Forms.DialogResult.OK; Close(); });
        InitButton(_btnCancel, "İptal", (_, _) => { RestoreOriginalColumns(); DialogResult = System.Windows.Forms.DialogResult.Cancel; Close(); });
        _btnOk.Width = 90;
        _btnCancel.Width = 90;
        bottom.Controls.AddRange(new System.Windows.Forms.Control[] { _btnOk, _btnCancel });

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
        CaptureOriginalColumns();

        ReloadList();
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
    }

    protected override void OnFormClosing(System.Windows.Forms.FormClosingEventArgs e)
    {
        if (!_committed && DialogResult != System.Windows.Forms.DialogResult.OK)
            RestoreOriginalColumns();

        base.OnFormClosing(e);
    }

    private static void InitButton(System.Windows.Forms.Button button, string text, EventHandler click)
    {
        button.Text = text;
        button.AutoSize = false;
        button.Dock = System.Windows.Forms.DockStyle.Fill;
        button.MinimumSize = new System.Drawing.Size(0, 27);
        button.Margin = new System.Windows.Forms.Padding(3);
        button.Click += click;
    }

    private void ReloadList()
    {
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var column in _columns)
            {
                // PrivateColumn kolonları runtime/design yüzeyinde ve kolon seçicide gizlenir;
                // fakat bu designer editor içinde mutlaka görünür kalır ki kullanıcı geri alabilsin.
                _list.Items.Add(column);
            }
        }
        finally
        {
            _list.EndUpdate();
        }
        UpdateButtons();
    }

    private void RefreshListPreserveSelection()
    {
        var selected = _list.SelectedItem;
        ReloadList();
        if (selected != null)
            _list.SelectedItem = selected;
    }

    private void CaptureOriginalColumns()
    {
        _originalColumns.Clear();
        foreach (var column in _columns)
            _originalColumns.Add(new OriginalColumnEntry(column, ColumnSnapshot.From(column)));
    }

    private void RestoreOriginalColumns()
    {
        if (_restored)
            return;

        _restored = true;
        _propertyGrid.SelectedObject = null;
        _columns.Clear();
        foreach (var entry in _originalColumns)
        {
            entry.Snapshot.ApplyTo(entry.Column);
            _columns.Add(entry.Column);
        }

        DestroyCreatedColumnsNotInCollection();
    }

    private void CommitColumns()
    {
        _committed = true;

        foreach (var entry in _originalColumns)
        {
            if (!_columns.Contains(entry.Column))
                DestroyDesignerComponent(entry.Column);
        }

        DestroyCreatedColumnsNotInCollection();
    }

    private bool IsOriginalColumn(ViewGridColumn column)
        => _originalColumns.Any(entry => ReferenceEquals(entry.Column, column));

    private void DestroyCreatedColumnsNotInCollection()
    {
        foreach (var column in _createdColumns.ToArray())
        {
            if (!_columns.Contains(column))
            {
                DestroyDesignerComponent(column);
                _createdColumns.Remove(column);
            }
        }
    }

    private void DestroyDesignerComponent(ViewGridColumn column)
    {
        if (_host == null || column is not IComponent component || component.Site == null)
            return;

        try
        {
            _host.DestroyComponent(component);
        }
        catch
        {
            try { component.Dispose(); }
            catch { }
        }
    }

    private void UpdateSelectedColumn()
    {
        // v26.64: ViewGridColumn artık Component tabanlı olduğu için Visual Studio
        // zaten gerçek Design > (Name) alanını kendisi sağlar. PropertyGrid'i
        // doğrudan kolona bağlamak çift (Name) satırını engeller; ViewGridColumn.Name
        // property’si Browsable(false) olduğu için sadece VS'nin Component adı görünür.
        _propertyGrid.SelectedObject = _list.SelectedItem is ViewGridColumn column
            ? column
            : null;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var hasSelection = _list.SelectedIndex >= 0;
        _btnRemove.Enabled = hasSelection;
        _btnUp.Enabled = hasSelection && _list.SelectedIndex > 0;
        _btnDown.Enabled = hasSelection && _list.SelectedIndex >= 0 && _list.SelectedIndex < _list.Items.Count - 1;
    }

    private void AddColumn()
    {
        var column = ViewGridColumnCollectionEditor.CreateDesignerColumn(_columns, _host);
        if (!IsOriginalColumn(column))
            _createdColumns.Add(column);

        _columns.Add(column);
        ReloadList();
        _list.SelectedItem = column;
    }

    private void RemoveColumn()
    {
        if (_list.SelectedItem is not ViewGridColumn column) return;
        var index = _columns.IndexOf(column);
        if (index < 0) return;

        _columns.RemoveAt(index);
        if (!IsOriginalColumn(column))
        {
            DestroyDesignerComponent(column);
            _createdColumns.Remove(column);
        }

        ReloadList();
        if (_list.Items.Count > 0)
            _list.SelectedIndex = Math.Min(index, _list.Items.Count - 1);
    }

    private void MoveColumn(int delta)
    {
        if (_list.SelectedItem is not ViewGridColumn column) return;
        var oldIndex = _columns.IndexOf(column);
        var newIndex = oldIndex + delta;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= _columns.Count) return;

        _columns.RemoveAt(oldIndex);
        _columns.Insert(newIndex, column);
        ReloadList();
        _list.SelectedItem = column;
    }
}

internal sealed class ViewGridColumnPropertyGridObject : CustomTypeDescriptor
{
    private readonly ViewGridColumn _column;

    public ViewGridColumnPropertyGridObject(ViewGridColumn column)
        : base(TypeDescriptor.GetProvider(column).GetTypeDescriptor(column))
    {
        _column = column;
    }

    public override PropertyDescriptorCollection GetProperties()
        => GetProperties(Array.Empty<Attribute>());

    public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        var source = attributes == null
            ? TypeDescriptor.GetProperties(_column, true)
            : TypeDescriptor.GetProperties(_column, attributes, true);

        var result = new List<PropertyDescriptor>();
        var nameAdded = false;

        foreach (PropertyDescriptor property in source)
        {
            if (string.Equals(property.Name, nameof(ViewGridColumn.Name), StringComparison.Ordinal))
            {
                if (!nameAdded)
                {
                    result.Add(new ViewGridColumnNamePropertyDescriptor(property));
                    nameAdded = true;
                }
                continue;
            }

            if (string.Equals(property.DisplayName, "(Name)", StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(property);
        }

        if (!nameAdded)
            result.Insert(0, new ViewGridColumnNamePropertyDescriptor(null));

        return new PropertyDescriptorCollection(result.ToArray(), true);
    }

    public override object? GetPropertyOwner(PropertyDescriptor? pd)
        => _column;
}

internal sealed class ViewGridColumnNamePropertyDescriptor : PropertyDescriptor
{
    private readonly PropertyDescriptor? _inner;

    public ViewGridColumnNamePropertyDescriptor(PropertyDescriptor? inner)
        : base(nameof(ViewGridColumn.Name), null)
    {
        _inner = inner;
    }

    public override Type ComponentType => typeof(ViewGridColumn);
    public override bool IsReadOnly => false;
    public override Type PropertyType => typeof(string);
    public override string Category => "Design";
    public override string DisplayName => "(Name)";
    public override string Description => "Kolonun tasarım/kod tarafındaki benzersiz kimliği. Varsayılan ad glvColumn1 şeklinde başlar.";

    public override bool CanResetValue(object component) => false;

    public override object? GetValue(object? component)
    {
        var column = component as ViewGridColumn;
        return column?.Name ?? string.Empty;
    }

    public override void ResetValue(object component)
    {
    }

    public override void SetValue(object? component, object? value)
    {
        if (component is ViewGridColumn column)
            column.Name = value?.ToString() ?? string.Empty;
        else
            _inner?.SetValue(component, value);
    }

    public override bool ShouldSerializeValue(object component) => true;
}

internal sealed class OriginalColumnEntry
{
    public OriginalColumnEntry(ViewGridColumn column, ColumnSnapshot snapshot)
    {
        Column = column;
        Snapshot = snapshot;
    }

    public ViewGridColumn Column { get; }
    public ColumnSnapshot Snapshot { get; }
}

internal sealed class ColumnSnapshot
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

        public static ColumnSnapshot From(ViewGridColumn column)
        {
            var snapshot = new ColumnSnapshot();
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(column, true))
            {
                if (property.IsReadOnly)
                    continue;

                if (string.Equals(property.Name, nameof(IComponent.Site), StringComparison.Ordinal) ||
                    string.Equals(property.Name, nameof(Component.Container), StringComparison.Ordinal))
                    continue;

                try
                {
                    snapshot._values[property.Name] = property.GetValue(column);
                }
                catch
                {
                }
            }

            return snapshot;
        }

        public void ApplyTo(ViewGridColumn column)
        {
            var properties = TypeDescriptor.GetProperties(column, true);
            foreach (var pair in _values)
            {
                var property = properties[pair.Key];
                if (property == null || property.IsReadOnly)
                    continue;

                try
                {
                    property.SetValue(column, pair.Value);
                }
                catch
                {
                }
            }
        }
    }

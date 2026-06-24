using System.Collections.ObjectModel;

namespace Taylan.Pano.Columns;

public class PanoColumnCollection : Collection<PanoColumn>
{
    public event EventHandler? CollectionChanged;
    public PanoColumn? this[string nameOrAspectName] => this.FirstOrDefault(x =>
        string.Equals(x.Name, nameOrAspectName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(x.AspectName, nameOrAspectName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(x.Header, nameOrAspectName, StringComparison.OrdinalIgnoreCase));

    public PanoColumn? ByName(string name) => this.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    public PanoColumn? ByAspectName(string aspectName) => this.FirstOrDefault(x => string.Equals(x.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
    public IEnumerable<PanoColumn> VisibleColumns => this.Where(x => !x.PrivateColumn && x.Visible).OrderBy(x => x.DisplayIndex < 0 ? int.MaxValue : x.DisplayIndex).ThenBy(x => IndexOf(x));

    protected override void InsertItem(int index, PanoColumn item)
    {
        PrepareColumnName(item, index);
        base.InsertItem(index, item);
        OnCollectionChanged();
    }

    protected override void SetItem(int index, PanoColumn item)
    {
        PrepareColumnName(item, index);
        base.SetItem(index, item);
        OnCollectionChanged();
    }

    private void PrepareColumnName(PanoColumn item, int index)
    {
        if (item == null) return;

        // Pano designer varsayılan kolon kimliği glvColumn1, glvColumn2... şeklinde üretilir.
        // Kullanıcı PropertyGrid > Design > (Name) alanına ProgramName gibi özel bir ad yazarsa
        // bu ad aynen korunur; başına glv eklenmez ve Text/AspectName değişince otomatik değiştirilmez.
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            var ordinal = index >= 0 ? index + 1 : Count + 1;
            item.Name = PanoColumnNameHelper.CreateDefaultName(ordinal);
        }
        // Önemli: Kullanıcı kolonları CollectionEditor içinde yukarı/aşağı taşıdığında
        // glvColumn2 başa gelse bile adını glvColumn1 diye yeniden numaralandırma.
        // ObjectListView/VS CollectionEditor davranışında kolonun (Name) değeri kolonla
        // birlikte taşınır; sıra değişimi kimlik değişimi değildir.

        item.Name = PanoColumnNameHelper.EnsureUnique(item.Name, this.Where((_, i) => i != index));
    }

    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        OnCollectionChanged();
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        OnCollectionChanged();
    }

    private void OnCollectionChanged()
    {
        CollectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public PanoColumn? FindByName(string name) => ByName(name);
    public PanoColumn? FindByAspectName(string aspectName) => ByAspectName(aspectName);

    public PanoColumn? FindByText(string text)
        => this.FirstOrDefault(x => string.Equals(x.Header, text, StringComparison.OrdinalIgnoreCase));

    public bool ContainsName(string name) => ByName(name) != null;
    public bool ContainsAspectName(string aspectName) => ByAspectName(aspectName) != null;

    public void NormalizeNames()
    {
        for (int i = 0; i < Count; i++)
        {
            PrepareColumnName(this[i], i);
        }
        OnCollectionChanged();
    }

    /// <summary>
    /// Adds multiple columns in one call. This keeps sample/designer code simple and
    /// avoids relying on List&lt;T&gt;-only extension methods.
    /// </summary>
    public void AddRange(IEnumerable<PanoColumn> columns)
    {
        if (columns == null) return;
        foreach (var column in columns)
        {
            if (column != null) Add(column);
        }
    }

    /// <summary>
    /// Params overload for concise usage: Columns.AddRange(col1, col2, col3).
    /// </summary>
    public void AddRange(params PanoColumn[] columns)
    {
        AddRange((IEnumerable<PanoColumn>)columns);
    }

    /// <summary>
    /// Clears existing columns and adds the new set. Useful for designer/demo rebuilds.
    /// </summary>
    public void Reset(IEnumerable<PanoColumn> columns)
    {
        Clear();
        AddRange(columns);
    }
}

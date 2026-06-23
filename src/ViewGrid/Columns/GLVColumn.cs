using System.ComponentModel;

namespace ViewGrid.Columns;

/// <summary>
/// ViewGrid List View kolonu. Yeni ViewGrid/GLV geçişinde ana kolon tipidir.
/// </summary>
[ToolboxItem(false)]
[DesignTimeVisible(false)]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class GLVColumn : ViewGridColumn
{
    public GLVColumn() : base() { }
    public GLVColumn(string title, string aspectName, int width = 120) : base(title, aspectName, width) { }
}

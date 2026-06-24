using System.ComponentModel;

namespace Taylan.Pano.Columns;

/// <summary>
/// Pano List View kolonu. Yeni Pano/GLV geçişinde ana kolon tipidir.
/// </summary>
[ToolboxItem(false)]
[DesignTimeVisible(false)]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class GLVColumn : PanoColumn
{
    public GLVColumn() : base() { }
    public GLVColumn(string title, string aspectName, int width = 120) : base(title, aspectName, width) { }
}

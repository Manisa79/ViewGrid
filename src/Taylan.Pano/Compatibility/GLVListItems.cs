namespace Taylan.Pano.Compatibility;

public class GLVListItem
{
    public object? RowObject { get; set; }
    public object? ModelObject { get => RowObject; set => RowObject = value; }
    public int Index { get; set; } = -1;
    public string Text { get; set; } = string.Empty;
}

public class GLVListSubItem
{
    public object? ModelValue { get; set; }
    public string Text { get; set; } = string.Empty;
}

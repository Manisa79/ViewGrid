namespace ViewGrid.Sorting;
public sealed class ViewGridComparer : IComparer<object?>
{
    public int Compare(object? x, object? y)
    {
        if (x is IComparable cx) return cx.CompareTo(y);
        return string.Compare(Convert.ToString(x), Convert.ToString(y), StringComparison.CurrentCultureIgnoreCase);
    }
}

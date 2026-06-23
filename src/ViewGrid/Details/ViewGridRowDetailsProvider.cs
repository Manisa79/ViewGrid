namespace ViewGrid.Details;

public sealed class ViewGridRowDetailsProvider
{
    public Func<object, Control>? CreateDetailsControl { get; set; }
    public int PreferredHeight { get; set; } = 90;
}

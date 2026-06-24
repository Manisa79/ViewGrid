namespace Taylan.Pano.Details;

public sealed class PanoRowDetailsProvider
{
    public Func<object, Control>? CreateDetailsControl { get; set; }
    public int PreferredHeight { get; set; } = 90;
}

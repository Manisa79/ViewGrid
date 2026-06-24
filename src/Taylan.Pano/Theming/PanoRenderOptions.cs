namespace Taylan.Pano.Theming;

public sealed class PanoRenderOptions
{
    public bool EnableFluentBackdrop { get; set; } = true;
    public bool EnableAcrylicSimulation { get; set; } = true;
    public bool EnableAnimatedSelection { get; set; } = true;
    public bool EnableSoftShadows { get; set; } = true;
    public bool EnableRoundedCells { get; set; } = true;
    public bool PreferGpuRenderer { get; set; }
    public int AnimationIntervalMs { get; set; } = 16;
    public int SelectionAnimationStep { get; set; } = 22;
}

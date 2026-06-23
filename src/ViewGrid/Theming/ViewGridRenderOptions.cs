namespace ViewGrid.Theming;

public sealed class ViewGridRenderOptions
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

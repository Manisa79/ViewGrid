using ViewGrid.Virtualization;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    public void SetRangeVirtualProvider(IViewGridRangeRowProvider provider)
    {
        SetVirtualProvider(provider ?? throw new ArgumentNullException(nameof(provider)));
    }
}

using Taylan.Pano.Virtualization;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    public void SetRangeVirtualProvider(IPanoRangeRowProvider provider)
    {
        SetVirtualProvider(provider ?? throw new ArgumentNullException(nameof(provider)));
    }
}

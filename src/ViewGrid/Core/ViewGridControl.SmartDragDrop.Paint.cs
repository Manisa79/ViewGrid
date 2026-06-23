using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    [Category("ViewGrid - Smart Drag Drop")]
    [DefaultValue(typeof(Color), "Empty")]
    public Color DropIndicatorColor { get; set; } = Color.Empty;

    private void DrawSmartDropIndicator(Graphics g)
    {
        if (!ShowDropIndicator || _smartDropTargetViewIndex < 0 || _smartDropPosition == ViewGridDropPosition.None) return;
        int header = ShowHeader ? HeaderHeight : 0;
        int y = header + (_smartDropTargetViewIndex - _scrollY) * RowHeight;
        if (_smartDropPosition == ViewGridDropPosition.After) y += RowHeight;
        if (y < header || y > Height) return;

        Color color = DropIndicatorColor == Color.Empty ? _theme.AccentColor : DropIndicatorColor;
        using var pen = new Pen(color, 2f) { DashStyle = DashStyle.Solid };
        int left = 0;
        int right = Math.Max(0, Width - VBarWidth - 1);
        g.DrawLine(pen, left + 6, y, right - 6, y);
        using var b = new SolidBrush(color);
        g.FillEllipse(b, left + 3, y - 4, 8, 8);
        g.FillEllipse(b, right - 11, y - 4, 8, 8);
    }
}

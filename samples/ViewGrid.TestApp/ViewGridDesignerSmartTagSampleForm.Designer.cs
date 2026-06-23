namespace ViewGrid.TestApp;

public sealed partial class ViewGridDesignerSmartTagSampleForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Label lblInfo;
    private ViewGrid.Core.ViewGridControl viewgrid;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.lblInfo = new System.Windows.Forms.Label();
        this.viewgrid = new ViewGrid.Core.ViewGridControl();
        this.SuspendLayout();
        // 
        // lblInfo
        // 
        this.lblInfo.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblInfo.Height = 92;
        this.lblInfo.Padding = new System.Windows.Forms.Padding(12);
        this.lblInfo.Text = "Designer SmartTag örneği: Bu formu Visual Studio Designer'da aç, ViewGrid kontrolünü seç ve sağ üst hızlı görev okunu kullan.";
        this.lblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // viewgrid
        // 
        this.viewgrid.AutoThemeFromParent = true;
        this.viewgrid.DesignTimeSampleData = true;
        this.viewgrid.DesignTimeThemePreview = ViewGrid.Theming.ViewGridDesignTimeThemePreview.Auto;
        this.viewgrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this.viewgrid.EmptyListMessage = "Designer SmartTag örneği";
        this.viewgrid.HeaderContextMenuBehavior = ViewGrid.Core.ViewGridHeaderContextMenuBehavior.Full;
        this.viewgrid.Location = new System.Drawing.Point(0, 92);
        this.viewgrid.Name = "viewgrid";
        this.viewgrid.Size = new System.Drawing.Size(980, 528);
        this.viewgrid.TabIndex = 1;
        // 
        // ViewGridDesignerSmartTagSampleForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(980, 620);
        this.Controls.Add(this.viewgrid);
        this.Controls.Add(this.lblInfo);
        this.MinimumSize = new System.Drawing.Size(760, 460);
        this.Name = "ViewGridDesignerSmartTagSampleForm";
        this.Text = "ViewGrid Designer SmartTag Örneği";
        this.ResumeLayout(false);
    }
}

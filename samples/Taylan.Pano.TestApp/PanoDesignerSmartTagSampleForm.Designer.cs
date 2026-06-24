namespace Taylan.Pano.TestApp;

public sealed partial class PanoDesignerSmartTagSampleForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Label lblInfo;
    private Taylan.Pano.Core.PanoControl pano;

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
        this.pano = new Taylan.Pano.Core.PanoControl();
        this.SuspendLayout();
        // 
        // lblInfo
        // 
        this.lblInfo.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblInfo.Height = 92;
        this.lblInfo.Padding = new System.Windows.Forms.Padding(12);
        this.lblInfo.Text = "Designer SmartTag örneği: Bu formu Visual Studio Designer'da aç, Pano kontrolünü seç ve sağ üst hızlı görev okunu kullan.";
        this.lblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // pano
        // 
        this.pano.AutoThemeFromParent = true;
        this.pano.DesignTimeSampleData = true;
        this.pano.DesignTimeThemePreview = Taylan.Pano.Theming.PanoDesignTimeThemePreview.Auto;
        this.pano.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pano.EmptyListMessage = "Designer SmartTag örneği";
        this.pano.HeaderContextMenuBehavior = Taylan.Pano.Core.PanoHeaderContextMenuBehavior.Full;
        this.pano.Location = new System.Drawing.Point(0, 92);
        this.pano.Name = "pano";
        this.pano.Size = new System.Drawing.Size(980, 528);
        this.pano.TabIndex = 1;
        // 
        // PanoDesignerSmartTagSampleForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(980, 620);
        this.Controls.Add(this.pano);
        this.Controls.Add(this.lblInfo);
        this.MinimumSize = new System.Drawing.Size(760, 460);
        this.Name = "PanoDesignerSmartTagSampleForm";
        this.Text = "Pano Designer SmartTag Örneği";
        this.ResumeLayout(false);
    }
}

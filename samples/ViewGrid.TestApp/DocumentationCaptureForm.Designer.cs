namespace ViewGrid.TestApp;

partial class DocumentationCaptureForm
{
    private System.ComponentModel.IContainer? components = null;
    private System.Windows.Forms.TableLayoutPanel layoutRoot = null!;
    private System.Windows.Forms.Panel headerPanel = null!;
    private System.Windows.Forms.Label titleLabel = null!;
    private System.Windows.Forms.Label descriptionLabel = null!;
    private System.Windows.Forms.Panel commandPanel = null!;
    private System.Windows.Forms.Button btnSelectAll = null!;
    private System.Windows.Forms.Button btnSelectMissing = null!;
    private System.Windows.Forms.Button btnClear = null!;
    private System.Windows.Forms.Button btnChooseFolder = null!;
    private System.Windows.Forms.Button btnCapture = null!;
    private System.Windows.Forms.Button btnOpenFolder = null!;
    private System.Windows.Forms.TextBox txtOutputFolder = null!;
    private System.Windows.Forms.Label outputLabel = null!;
    private System.Windows.Forms.SplitContainer splitMain = null!;
    private System.Windows.Forms.CheckedListBox checkedSamples = null!;
    private System.Windows.Forms.TextBox txtLog = null!;
    private System.Windows.Forms.ProgressBar progressBar = null!;
    private System.Windows.Forms.Label statusLabel = null!;
    private System.Windows.Forms.Label outputHintLabel = null!;
    private System.Windows.Forms.Label selectionHintLabel = null!;
    private System.Windows.Forms.Panel samplePanel = null!;
    private System.Windows.Forms.Panel logPanel = null!;
    private System.Windows.Forms.Label sampleTitleLabel = null!;
    private System.Windows.Forms.Label logTitleLabel = null!;
    private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        layoutRoot = new System.Windows.Forms.TableLayoutPanel();
        headerPanel = new System.Windows.Forms.Panel();
        titleLabel = new System.Windows.Forms.Label();
        descriptionLabel = new System.Windows.Forms.Label();
        commandPanel = new System.Windows.Forms.Panel();
        btnSelectAll = new System.Windows.Forms.Button();
        btnSelectMissing = new System.Windows.Forms.Button();
        btnClear = new System.Windows.Forms.Button();
        btnChooseFolder = new System.Windows.Forms.Button();
        btnCapture = new System.Windows.Forms.Button();
        btnOpenFolder = new System.Windows.Forms.Button();
        txtOutputFolder = new System.Windows.Forms.TextBox();
        outputLabel = new System.Windows.Forms.Label();
        splitMain = new System.Windows.Forms.SplitContainer();
        checkedSamples = new System.Windows.Forms.CheckedListBox();
        txtLog = new System.Windows.Forms.TextBox();
        progressBar = new System.Windows.Forms.ProgressBar();
        statusLabel = new System.Windows.Forms.Label();
        outputHintLabel = new System.Windows.Forms.Label();
        selectionHintLabel = new System.Windows.Forms.Label();
        samplePanel = new System.Windows.Forms.Panel();
        logPanel = new System.Windows.Forms.Panel();
        sampleTitleLabel = new System.Windows.Forms.Label();
        logTitleLabel = new System.Windows.Forms.Label();
        folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
        layoutRoot.SuspendLayout();
        headerPanel.SuspendLayout();
        commandPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        samplePanel.SuspendLayout();
        logPanel.SuspendLayout();
        splitMain.SuspendLayout();
        SuspendLayout();
        // 
        // layoutRoot
        // 
        layoutRoot.ColumnCount = 1;
        layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        layoutRoot.Controls.Add(headerPanel, 0, 0);
        layoutRoot.Controls.Add(commandPanel, 0, 1);
        layoutRoot.Controls.Add(splitMain, 0, 2);
        layoutRoot.Controls.Add(progressBar, 0, 3);
        layoutRoot.Controls.Add(statusLabel, 0, 4);
        layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
        layoutRoot.Location = new System.Drawing.Point(0, 0);
        layoutRoot.Name = "layoutRoot";
        layoutRoot.RowCount = 5;
        layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 82F));
        layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 112F));
        layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
        layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        layoutRoot.Size = new System.Drawing.Size(1184, 721);
        layoutRoot.TabIndex = 0;
        // 
        // headerPanel
        // 
        headerPanel.Controls.Add(descriptionLabel);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        headerPanel.Location = new System.Drawing.Point(0, 0);
        headerPanel.Margin = new System.Windows.Forms.Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Padding = new System.Windows.Forms.Padding(18, 12, 18, 8);
        headerPanel.Size = new System.Drawing.Size(1184, 82);
        headerPanel.TabIndex = 0;
        // 
        // titleLabel
        // 
        titleLabel.Dock = System.Windows.Forms.DockStyle.Top;
        titleLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
        titleLabel.Location = new System.Drawing.Point(18, 12);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new System.Drawing.Size(1148, 34);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Documentation Capture Mode";
        titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // descriptionLabel
        // 
        descriptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        descriptionLabel.Location = new System.Drawing.Point(18, 46);
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Size = new System.Drawing.Size(1148, 28);
        descriptionLabel.TabIndex = 1;
        descriptionLabel.Text = "Seçilen Example Center ekranlarını PNG olarak üretir; manifest ve Word/PDF dokümanı için ekleme haritası oluşturur.";
        descriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // commandPanel
        // 
        commandPanel.Controls.Add(selectionHintLabel);
        commandPanel.Controls.Add(outputHintLabel);
        commandPanel.Controls.Add(btnOpenFolder);
        commandPanel.Controls.Add(btnCapture);
        commandPanel.Controls.Add(btnChooseFolder);
        commandPanel.Controls.Add(btnClear);
        commandPanel.Controls.Add(btnSelectMissing);
        commandPanel.Controls.Add(btnSelectAll);
        commandPanel.Controls.Add(txtOutputFolder);
        commandPanel.Controls.Add(outputLabel);
        commandPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        commandPanel.Location = new System.Drawing.Point(0, 82);
        commandPanel.Margin = new System.Windows.Forms.Padding(0);
        commandPanel.Name = "commandPanel";
        commandPanel.Padding = new System.Windows.Forms.Padding(18, 10, 18, 8);
        commandPanel.Size = new System.Drawing.Size(1184, 112);
        commandPanel.TabIndex = 1;
        // 
        // btnSelectAll
        // 
        btnSelectAll.Location = new System.Drawing.Point(18, 72);
        btnSelectAll.Name = "btnSelectAll";
        btnSelectAll.Size = new System.Drawing.Size(132, 32);
        btnSelectAll.TabIndex = 2;
        btnSelectAll.Text = "✓ Tümünü Seç";
        btnSelectAll.UseVisualStyleBackColor = false;
        // 
        // btnSelectMissing
        // 
        btnSelectMissing.Location = new System.Drawing.Point(158, 72);
        btnSelectMissing.Name = "btnSelectMissing";
        btnSelectMissing.Size = new System.Drawing.Size(178, 32);
        btnSelectMissing.TabIndex = 3;
        btnSelectMissing.Text = "Eksik DOCX Görselleri";
        btnSelectMissing.UseVisualStyleBackColor = false;
        // 
        // btnClear
        // 
        btnClear.Location = new System.Drawing.Point(344, 72);
        btnClear.Name = "btnClear";
        btnClear.Size = new System.Drawing.Size(132, 32);
        btnClear.TabIndex = 4;
        btnClear.Text = "✕ Tümünü Temizle";
        btnClear.UseVisualStyleBackColor = false;
        // 
        // btnChooseFolder
        // 
        btnChooseFolder.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnChooseFolder.Location = new System.Drawing.Point(742, 10);
        btnChooseFolder.Name = "btnChooseFolder";
        btnChooseFolder.Size = new System.Drawing.Size(126, 32);
        btnChooseFolder.TabIndex = 4;
        btnChooseFolder.Text = "📁 Klasör Seç";
        btnChooseFolder.UseVisualStyleBackColor = false;
        // 
        // btnCapture
        // 
        btnCapture.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnCapture.Location = new System.Drawing.Point(874, 10);
        btnCapture.Name = "btnCapture";
        btnCapture.Size = new System.Drawing.Size(146, 32);
        btnCapture.TabIndex = 5;
        btnCapture.Text = "▶ Ekranları Üret";
        btnCapture.UseVisualStyleBackColor = false;
        // 
        // btnOpenFolder
        // 
        btnOpenFolder.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnOpenFolder.Location = new System.Drawing.Point(1026, 10);
        btnOpenFolder.Name = "btnOpenFolder";
        btnOpenFolder.Size = new System.Drawing.Size(140, 32);
        btnOpenFolder.TabIndex = 6;
        btnOpenFolder.Text = "↗ Klasörü Aç";
        btnOpenFolder.UseVisualStyleBackColor = false;
        // 
        // txtOutputFolder
        // 
        txtOutputFolder.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        txtOutputFolder.Location = new System.Drawing.Point(124, 12);
        txtOutputFolder.Name = "txtOutputFolder";
        txtOutputFolder.Size = new System.Drawing.Size(612, 27);
        txtOutputFolder.TabIndex = 1;
        // 
        // outputLabel
        // 
        outputLabel.Location = new System.Drawing.Point(18, 12);
        outputLabel.Name = "outputLabel";
        outputLabel.Size = new System.Drawing.Size(100, 27);
        outputLabel.TabIndex = 0;
        outputLabel.Text = "Çıktı klasörü";
        outputLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // outputHintLabel
        // 
        outputHintLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        outputHintLabel.Location = new System.Drawing.Point(124, 42);
        outputHintLabel.Name = "outputHintLabel";
        outputHintLabel.Size = new System.Drawing.Size(612, 22);
        outputHintLabel.TabIndex = 7;
        outputHintLabel.Text = "PNG, manifest.json, screenshots.md ve docx-insert-map.json bu klasöre yazılır.";
        outputHintLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // selectionHintLabel
        // 
        selectionHintLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        selectionHintLabel.Location = new System.Drawing.Point(486, 76);
        selectionHintLabel.Name = "selectionHintLabel";
        selectionHintLabel.Size = new System.Drawing.Size(680, 24);
        selectionHintLabel.TabIndex = 8;
        selectionHintLabel.Text = "Sol listeden dokümana eklenecek ekranları seç; sağ panel işlem adımlarını ve üretim çıktısını gösterir.";
        selectionHintLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // splitMain
        // 
        splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
        splitMain.Location = new System.Drawing.Point(12, 202);
        splitMain.Margin = new System.Windows.Forms.Padding(12, 8, 12, 8);
        splitMain.Name = "splitMain";
        // 
        // splitMain.Panel1
        // 
        splitMain.Panel1.Controls.Add(samplePanel);
        // 
        // splitMain.Panel2
        // 
        splitMain.Panel2.Controls.Add(logPanel);
        splitMain.Size = new System.Drawing.Size(1160, 461);
        splitMain.SplitterDistance = 560;
        splitMain.SplitterWidth = 6;
        splitMain.TabIndex = 2;
        // 
        // samplePanel
        // 
        samplePanel.Controls.Add(checkedSamples);
        samplePanel.Controls.Add(sampleTitleLabel);
        samplePanel.Dock = System.Windows.Forms.DockStyle.Fill;
        samplePanel.Location = new System.Drawing.Point(0, 0);
        samplePanel.Name = "samplePanel";
        samplePanel.Padding = new System.Windows.Forms.Padding(0);
        samplePanel.Size = new System.Drawing.Size(560, 461);
        samplePanel.TabIndex = 0;
        // 
        // sampleTitleLabel
        // 
        sampleTitleLabel.Dock = System.Windows.Forms.DockStyle.Top;
        sampleTitleLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        sampleTitleLabel.Location = new System.Drawing.Point(0, 0);
        sampleTitleLabel.Name = "sampleTitleLabel";
        sampleTitleLabel.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
        sampleTitleLabel.Size = new System.Drawing.Size(560, 30);
        sampleTitleLabel.TabIndex = 1;
        sampleTitleLabel.Text = "Üretilecek Ekranlar";
        sampleTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // checkedSamples
        // 
        checkedSamples.CheckOnClick = true;
        checkedSamples.Dock = System.Windows.Forms.DockStyle.Fill;
        checkedSamples.FormattingEnabled = true;
        checkedSamples.IntegralHeight = false;
        checkedSamples.Location = new System.Drawing.Point(0, 30);
        checkedSamples.Name = "checkedSamples";
        checkedSamples.Size = new System.Drawing.Size(560, 431);
        checkedSamples.TabIndex = 0;
        // 
        // logPanel
        // 
        logPanel.Controls.Add(txtLog);
        logPanel.Controls.Add(logTitleLabel);
        logPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        logPanel.Location = new System.Drawing.Point(0, 0);
        logPanel.Name = "logPanel";
        logPanel.Size = new System.Drawing.Size(594, 461);
        logPanel.TabIndex = 0;
        // 
        // logTitleLabel
        // 
        logTitleLabel.Dock = System.Windows.Forms.DockStyle.Top;
        logTitleLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        logTitleLabel.Location = new System.Drawing.Point(0, 0);
        logTitleLabel.Name = "logTitleLabel";
        logTitleLabel.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
        logTitleLabel.Size = new System.Drawing.Size(594, 30);
        logTitleLabel.TabIndex = 1;
        logTitleLabel.Text = "Bilgi ve İşlem Günlüğü";
        logTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // txtLog
        // 
        txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
        txtLog.Font = new System.Drawing.Font("Consolas", 9.5F);
        txtLog.Location = new System.Drawing.Point(0, 30);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        txtLog.Size = new System.Drawing.Size(594, 431);
        txtLog.TabIndex = 0;
        txtLog.WordWrap = false;
        // 
        // progressBar
        // 
        progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
        progressBar.Location = new System.Drawing.Point(12, 671);
        progressBar.Margin = new System.Windows.Forms.Padding(12, 0, 12, 0);
        progressBar.Name = "progressBar";
        progressBar.Size = new System.Drawing.Size(1160, 22);
        progressBar.TabIndex = 3;
        // 
        // statusLabel
        // 
        statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        statusLabel.Location = new System.Drawing.Point(12, 693);
        statusLabel.Margin = new System.Windows.Forms.Padding(12, 0, 12, 0);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new System.Drawing.Size(1160, 28);
        statusLabel.TabIndex = 4;
        statusLabel.Text = "Hazır";
        statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // DocumentationCaptureForm
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1184, 721);
        Controls.Add(layoutRoot);
        MinimumSize = new System.Drawing.Size(1120, 680);
        Name = "DocumentationCaptureForm";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "Documentation Capture Mode";
        layoutRoot.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        commandPanel.ResumeLayout(false);
        commandPanel.PerformLayout();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        splitMain.Panel2.PerformLayout();
        samplePanel.ResumeLayout(false);
        logPanel.ResumeLayout(false);
        logPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        ResumeLayout(false);
    }
}

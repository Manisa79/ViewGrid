namespace ViewGrid.TestApp;

partial class StartupLanguageForm
{
    private System.ComponentModel.IContainer? components = null;
    private Label lblTitle = null!;
    private Label lblInfo = null!;
    private Label lblLanguage = null!;
    private ComboBox cmbLanguage = null!;
    private CheckBox chkRemember = null!;
    private Button btnContinue = null!;
    private Button btnCancel = null!;
    private Panel panelButtons = null!;
    private Panel panelContent = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        lblTitle = new Label();
        lblInfo = new Label();
        lblLanguage = new Label();
        cmbLanguage = new ComboBox();
        chkRemember = new CheckBox();
        btnContinue = new Button();
        btnCancel = new Button();
        panelButtons = new Panel();
        panelContent = new Panel();
        panelButtons.SuspendLayout();
        panelContent.SuspendLayout();
        SuspendLayout();
        // 
        // lblTitle
        // 
        lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
        lblTitle.Location = new Point(24, 18);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(470, 40);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "ViewGrid TestApp Language";
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblInfo
        // 
        lblInfo.Location = new Point(24, 74);
        lblInfo.Name = "lblInfo";
        lblInfo.Size = new Size(470, 46);
        lblInfo.TabIndex = 1;
        lblInfo.Text = "Select the language used by ViewGrid built-in menus, dialogs and sample screens.";
        lblInfo.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblLanguage
        // 
        lblLanguage.Location = new Point(24, 138);
        lblLanguage.Name = "lblLanguage";
        lblLanguage.Size = new Size(105, 24);
        lblLanguage.TabIndex = 2;
        lblLanguage.Text = "Language";
        lblLanguage.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbLanguage
        // 
        cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLanguage.FormattingEnabled = true;
        cmbLanguage.Location = new Point(134, 136);
        cmbLanguage.Name = "cmbLanguage";
        cmbLanguage.Size = new Size(260, 25);
        cmbLanguage.TabIndex = 3;
        // 
        // chkRemember
        // 
        chkRemember.AutoSize = true;
        chkRemember.Checked = true;
        chkRemember.CheckState = CheckState.Checked;
        chkRemember.Location = new Point(134, 178);
        chkRemember.Name = "chkRemember";
        chkRemember.Size = new Size(151, 21);
        chkRemember.TabIndex = 4;
        chkRemember.Text = "Remember my selection";
        chkRemember.UseVisualStyleBackColor = true;
        // 
        // btnContinue
        // 
        btnContinue.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnContinue.DialogResult = DialogResult.OK;
        btnContinue.Location = new Point(380, 14);
        btnContinue.Name = "btnContinue";
        btnContinue.Size = new Size(118, 34);
        btnContinue.TabIndex = 0;
        btnContinue.Text = "Continue";
        btnContinue.UseVisualStyleBackColor = true;
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(270, 14);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(100, 34);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // panelButtons
        // 
        panelButtons.BackColor = Color.FromArgb(241, 245, 249);
        panelButtons.Controls.Add(btnContinue);
        panelButtons.Controls.Add(btnCancel);
        panelButtons.Dock = DockStyle.Bottom;
        panelButtons.Location = new Point(0, 226);
        panelButtons.Name = "panelButtons";
        panelButtons.Size = new Size(522, 62);
        panelButtons.TabIndex = 1;
        // 
        // panelContent
        // 
        panelContent.BackColor = Color.FromArgb(248, 250, 252);
        panelContent.Controls.Add(lblTitle);
        panelContent.Controls.Add(lblInfo);
        panelContent.Controls.Add(lblLanguage);
        panelContent.Controls.Add(cmbLanguage);
        panelContent.Controls.Add(chkRemember);
        panelContent.Dock = DockStyle.Fill;
        panelContent.Location = new Point(0, 0);
        panelContent.Name = "panelContent";
        panelContent.Size = new Size(522, 226);
        panelContent.TabIndex = 0;
        // 
        // StartupLanguageForm
        // 
        AcceptButton = btnContinue;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(248, 250, 252);
        CancelButton = btnCancel;
        ClientSize = new Size(522, 288);
        Controls.Add(panelContent);
        Controls.Add(panelButtons);
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "StartupLanguageForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ViewGrid TestApp - Language";
        panelButtons.ResumeLayout(false);
        panelContent.ResumeLayout(false);
        panelContent.PerformLayout();
        ResumeLayout(false);
    }
}

namespace Plus4Commander
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            pbLogo = new PictureBox();
            lblTitle = new Label();
            lblVersion = new Label();
            lblDescription = new Label();
            btnClose = new Button();
            openFileDialog1 = new OpenFileDialog();
            panelYape = new Panel();
            btnYapeExe = new Button();
            label1 = new Label();
            textBox1 = new TextBox();
            linkLabel1 = new LinkLabel();
            ((System.ComponentModel.ISupportInitialize)pbLogo).BeginInit();
            panelYape.SuspendLayout();
            SuspendLayout();
            // 
            // pbLogo
            // 
            pbLogo.Image = Properties.Resources.ImageAbout;
            pbLogo.InitialImage = Properties.Resources.ImageAbout;
            pbLogo.Location = new Point(22, 13);
            pbLogo.Name = "pbLogo";
            pbLogo.Size = new Size(64, 64);
            pbLogo.SizeMode = PictureBoxSizeMode.StretchImage;
            pbLogo.TabIndex = 0;
            pbLogo.TabStop = false;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(106, 13);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(234, 32);
            lblTitle.TabIndex = 1;
            lblTitle.Text = "Plus/4 Commander";
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Font = new Font("Segoe UI", 9.5F);
            lblVersion.ForeColor = Color.DimGray;
            lblVersion.Location = new Point(111, 48);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(66, 21);
            lblVersion.TabIndex = 2;
            lblVersion.Text = "Version ";
            // 
            // lblDescription
            // 
            lblDescription.Font = new Font("Segoe UI", 9.5F);
            lblDescription.Location = new Point(22, 80);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(434, 131);
            lblDescription.TabIndex = 3;
            lblDescription.Text = resources.GetString("lblDescription.Text");
            lblDescription.Click += lblDescription_Click;
            // 
            // btnClose
            // 
            btnClose.DialogResult = DialogResult.OK;
            btnClose.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnClose.Location = new Point(184, 335);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(100, 34);
            btnClose.TabIndex = 4;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // panelYape
            // 
            panelYape.Controls.Add(btnYapeExe);
            panelYape.Controls.Add(label1);
            panelYape.Controls.Add(textBox1);
            panelYape.Location = new Point(22, 248);
            panelYape.Name = "panelYape";
            panelYape.Size = new Size(424, 69);
            panelYape.TabIndex = 5;
            // 
            // btnYapeExe
            // 
            btnYapeExe.Location = new Point(327, 3);
            btnYapeExe.Name = "btnYapeExe";
            btnYapeExe.Size = new Size(94, 29);
            btnYapeExe.TabIndex = 3;
            btnYapeExe.Text = "Open";
            btnYapeExe.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label1.Location = new Point(3, 6);
            label1.Name = "label1";
            label1.Size = new Size(153, 20);
            label1.TabIndex = 1;
            label1.Text = "YAPE emulator path:";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(3, 36);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(418, 27);
            textBox1.TabIndex = 0;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(22, 211);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(316, 20);
            linkLabel1.TabIndex = 6;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "https://github.com/bunnyhu/Plus4Commander";
            // 
            // AboutForm
            // 
            AcceptButton = btnClose;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(468, 381);
            Controls.Add(linkLabel1);
            Controls.Add(panelYape);
            Controls.Add(btnClose);
            Controls.Add(lblDescription);
            Controls.Add(lblVersion);
            Controls.Add(lblTitle);
            Controls.Add(pbLogo);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "About Plus/4 Commander";
            ((System.ComponentModel.ISupportInitialize)pbLogo).EndInit();
            panelYape.ResumeLayout(false);
            panelYape.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private PictureBox pbLogo;
        private Label lblTitle;
        private Label lblVersion;
        private Label lblDescription;
        private Button btnClose;
        private OpenFileDialog openFileDialog1;
        private Panel panelYape;
        private Button btnYapeExe;
        private Label label1;
        private TextBox textBox1;
        private LinkLabel linkLabel1;
    }
}
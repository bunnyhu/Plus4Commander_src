namespace Plus4Commander
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            splitContainer1 = new SplitContainer();
            listBoxFiles = new ListBox();
            lblDiskHeader = new Label();
            lblDiskFooter = new Label();
            splitRight = new SplitContainer();
            textBoxFileInfo = new TextBox();
            panelContentWrapper = new Panel();
            pbView = new PictureBox();
            textBoxInfo = new TextBox();
            panelLoadMoreBar = new Panel();
            lblTruncatedHint = new Label();
            btnShowAllContent = new Button();
            panelViewButtons = new Panel();
            btnShiftPlus = new Button();
            btnShiftMinus = new Button();
            btnViewSeq = new Button();
            btnViewDisasm = new Button();
            btnViewHex = new Button();
            btnToggleMulti = new Button();
            btnViewImage = new Button();
            btnViewFont = new Button();
            btnViewBasic = new Button();
            btnViewAuto = new Button();
            panelTop = new Panel();
            toolStrip1 = new ToolStrip();
            toolComboDrives = new ToolStripComboBox();
            toolButtonRoot = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            toolCurrentLocation = new ToolStripTextBox();
            toolStripSeparator2 = new ToolStripSeparator();
            toolBtnExit = new ToolStripButton();
            toolStripSeparator6 = new ToolStripSeparator();
            toolBtnAbout = new ToolStripButton();
            toolBtnExportFile = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            toolBtnYapeMountRun = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            toolBtnYapeQRun = new ToolStripButton();
            toolStripSeparator5 = new ToolStripSeparator();
            panelBottomHelp = new Panel();
            lblStatusHelp = new Label();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitRight).BeginInit();
            splitRight.Panel1.SuspendLayout();
            splitRight.Panel2.SuspendLayout();
            splitRight.SuspendLayout();
            panelContentWrapper.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbView).BeginInit();
            panelLoadMoreBar.SuspendLayout();
            panelViewButtons.SuspendLayout();
            panelTop.SuspendLayout();
            toolStrip1.SuspendLayout();
            panelBottomHelp.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new Point(0, 32);
            splitContainer1.Margin = new Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(listBoxFiles);
            splitContainer1.Panel1.Controls.Add(lblDiskHeader);
            splitContainer1.Panel1.Controls.Add(lblDiskFooter);
            splitContainer1.Panel1MinSize = 430;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitRight);
            splitContainer1.Size = new Size(1282, 799);
            splitContainer1.SplitterDistance = 439;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 1;
            // 
            // listBoxFiles
            // 
            listBoxFiles.BackColor = Color.White;
            listBoxFiles.BorderStyle = BorderStyle.FixedSingle;
            listBoxFiles.Dock = DockStyle.Fill;
            listBoxFiles.DrawMode = DrawMode.OwnerDrawFixed;
            listBoxFiles.Font = new Font("Consolas", 10.5F);
            listBoxFiles.ForeColor = Color.Black;
            listBoxFiles.FormattingEnabled = true;
            listBoxFiles.ItemHeight = 22;
            listBoxFiles.Location = new Point(0, 30);
            listBoxFiles.Margin = new Padding(3, 4, 3, 4);
            listBoxFiles.Name = "listBoxFiles";
            listBoxFiles.Size = new Size(439, 732);
            listBoxFiles.TabIndex = 0;
            listBoxFiles.SelectedIndexChanged += ListBoxFiles_SelectedIndexChanged;
            // 
            // lblDiskHeader
            // 
            lblDiskHeader.BackColor = Color.FromArgb(40, 40, 52);
            lblDiskHeader.BorderStyle = BorderStyle.FixedSingle;
            lblDiskHeader.Dock = DockStyle.Top;
            lblDiskHeader.Font = new Font("Consolas", 9.5F, FontStyle.Bold);
            lblDiskHeader.ForeColor = Color.White;
            lblDiskHeader.Location = new Point(0, 0);
            lblDiskHeader.Name = "lblDiskHeader";
            lblDiskHeader.Padding = new Padding(4, 0, 4, 0);
            lblDiskHeader.Size = new Size(439, 30);
            lblDiskHeader.TabIndex = 1;
            lblDiskHeader.Text = " Drive C:\\";
            lblDiskHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDiskFooter
            // 
            lblDiskFooter.BackColor = Color.FromArgb(40, 40, 52);
            lblDiskFooter.BorderStyle = BorderStyle.FixedSingle;
            lblDiskFooter.Dock = DockStyle.Bottom;
            lblDiskFooter.Font = new Font("Consolas", 9F, FontStyle.Bold);
            lblDiskFooter.ForeColor = Color.LightGray;
            lblDiskFooter.Location = new Point(0, 762);
            lblDiskFooter.Name = "lblDiskFooter";
            lblDiskFooter.Padding = new Padding(4, 0, 4, 0);
            lblDiskFooter.Size = new Size(439, 37);
            lblDiskFooter.TabIndex = 2;
            lblDiskFooter.Text = " 0 dirs, 0 files";
            lblDiskFooter.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // splitRight
            // 
            splitRight.Dock = DockStyle.Fill;
            splitRight.Location = new Point(0, 0);
            splitRight.Margin = new Padding(3, 4, 3, 4);
            splitRight.Name = "splitRight";
            splitRight.Orientation = Orientation.Horizontal;
            // 
            // splitRight.Panel1
            // 
            splitRight.Panel1.Controls.Add(textBoxFileInfo);
            // 
            // splitRight.Panel2
            // 
            splitRight.Panel2.Controls.Add(panelContentWrapper);
            splitRight.Panel2.Controls.Add(panelViewButtons);
            splitRight.Size = new Size(838, 799);
            splitRight.SplitterDistance = 126;
            splitRight.SplitterWidth = 5;
            splitRight.TabIndex = 0;
            // 
            // textBoxFileInfo
            // 
            textBoxFileInfo.BackColor = Color.FromArgb(30, 30, 40);
            textBoxFileInfo.Dock = DockStyle.Fill;
            textBoxFileInfo.Font = new Font("Consolas", 10F, FontStyle.Bold);
            textBoxFileInfo.ForeColor = Color.White;
            textBoxFileInfo.Location = new Point(0, 0);
            textBoxFileInfo.Margin = new Padding(3, 4, 3, 4);
            textBoxFileInfo.Multiline = true;
            textBoxFileInfo.Name = "textBoxFileInfo";
            textBoxFileInfo.ReadOnly = true;
            textBoxFileInfo.Size = new Size(838, 126);
            textBoxFileInfo.TabIndex = 0;
            // 
            // panelContentWrapper
            // 
            panelContentWrapper.Controls.Add(pbView);
            panelContentWrapper.Controls.Add(textBoxInfo);
            panelContentWrapper.Controls.Add(panelLoadMoreBar);
            panelContentWrapper.Dock = DockStyle.Fill;
            panelContentWrapper.Location = new Point(0, 40);
            panelContentWrapper.Margin = new Padding(3, 4, 3, 4);
            panelContentWrapper.Name = "panelContentWrapper";
            panelContentWrapper.Size = new Size(838, 628);
            panelContentWrapper.TabIndex = 1;
            // 
            // pbView
            // 
            pbView.BackColor = Color.FromArgb(20, 20, 30);
            pbView.Dock = DockStyle.Fill;
            pbView.Location = new Point(0, 0);
            pbView.Margin = new Padding(3, 4, 3, 4);
            pbView.Name = "pbView";
            pbView.Size = new Size(838, 596);
            pbView.SizeMode = PictureBoxSizeMode.Zoom;
            pbView.TabIndex = 1;
            pbView.TabStop = false;
            pbView.Visible = false;
            // 
            // textBoxInfo
            // 
            textBoxInfo.BackColor = Color.FromArgb(20, 20, 30);
            textBoxInfo.Dock = DockStyle.Fill;
            textBoxInfo.Font = new Font("Consolas", 10F);
            textBoxInfo.ForeColor = Color.FromArgb(0, 255, 128);
            textBoxInfo.Location = new Point(0, 0);
            textBoxInfo.Margin = new Padding(3, 4, 3, 4);
            textBoxInfo.Multiline = true;
            textBoxInfo.Name = "textBoxInfo";
            textBoxInfo.ReadOnly = true;
            textBoxInfo.ScrollBars = ScrollBars.Vertical;
            textBoxInfo.Size = new Size(838, 596);
            textBoxInfo.TabIndex = 0;
            // 
            // panelLoadMoreBar
            // 
            panelLoadMoreBar.BackColor = Color.FromArgb(35, 38, 54);
            panelLoadMoreBar.Controls.Add(lblTruncatedHint);
            panelLoadMoreBar.Controls.Add(btnShowAllContent);
            panelLoadMoreBar.Dock = DockStyle.Bottom;
            panelLoadMoreBar.Location = new Point(0, 596);
            panelLoadMoreBar.Name = "panelLoadMoreBar";
            panelLoadMoreBar.Padding = new Padding(8, 3, 8, 3);
            panelLoadMoreBar.Size = new Size(838, 32);
            panelLoadMoreBar.TabIndex = 2;
            panelLoadMoreBar.Visible = false;
            // 
            // lblTruncatedHint
            // 
            lblTruncatedHint.Dock = DockStyle.Fill;
            lblTruncatedHint.Font = new Font("Consolas", 9.5F, FontStyle.Bold);
            lblTruncatedHint.ForeColor = Color.Gold;
            lblTruncatedHint.Location = new Point(8, 3);
            lblTruncatedHint.Name = "lblTruncatedHint";
            lblTruncatedHint.Size = new Size(662, 26);
            lblTruncatedHint.TabIndex = 0;
            lblTruncatedHint.Text = "Output truncated (more bytes available)";
            lblTruncatedHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnShowAllContent
            // 
            btnShowAllContent.BackColor = Color.FromArgb(62, 49, 162);
            btnShowAllContent.Dock = DockStyle.Right;
            btnShowAllContent.FlatAppearance.BorderColor = Color.CornflowerBlue;
            btnShowAllContent.FlatStyle = FlatStyle.Flat;
            btnShowAllContent.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnShowAllContent.ForeColor = Color.White;
            btnShowAllContent.Location = new Point(670, 3);
            btnShowAllContent.Name = "btnShowAllContent";
            btnShowAllContent.Size = new Size(160, 26);
            btnShowAllContent.TabIndex = 1;
            btnShowAllContent.Text = "Show Full File";
            btnShowAllContent.UseVisualStyleBackColor = false;
            // 
            // panelViewButtons
            // 
            panelViewButtons.Controls.Add(btnShiftPlus);
            panelViewButtons.Controls.Add(btnShiftMinus);
            panelViewButtons.Controls.Add(btnViewSeq);
            panelViewButtons.Controls.Add(btnViewDisasm);
            panelViewButtons.Controls.Add(btnViewHex);
            panelViewButtons.Controls.Add(btnToggleMulti);
            panelViewButtons.Controls.Add(btnViewImage);
            panelViewButtons.Controls.Add(btnViewFont);
            panelViewButtons.Controls.Add(btnViewBasic);
            panelViewButtons.Controls.Add(btnViewAuto);
            panelViewButtons.Dock = DockStyle.Top;
            panelViewButtons.Location = new Point(0, 0);
            panelViewButtons.Margin = new Padding(0);
            panelViewButtons.Name = "panelViewButtons";
            panelViewButtons.Size = new Size(838, 40);
            panelViewButtons.TabIndex = 0;
            // 
            // btnShiftPlus
            // 
            btnShiftPlus.Location = new Point(763, 3);
            btnShiftPlus.Margin = new Padding(3, 4, 3, 4);
            btnShiftPlus.Name = "btnShiftPlus";
            btnShiftPlus.Size = new Size(63, 30);
            btnShiftPlus.TabIndex = 9;
            btnShiftPlus.Text = "Font ►";
            btnShiftPlus.UseVisualStyleBackColor = true;
            // 
            // btnShiftMinus
            // 
            btnShiftMinus.Location = new Point(694, 3);
            btnShiftMinus.Margin = new Padding(3, 4, 3, 4);
            btnShiftMinus.Name = "btnShiftMinus";
            btnShiftMinus.Size = new Size(63, 30);
            btnShiftMinus.TabIndex = 8;
            btnShiftMinus.Text = "◄ Font";
            btnShiftMinus.UseVisualStyleBackColor = true;
            // 
            // btnViewSeq
            // 
            btnViewSeq.BackColor = SystemColors.Control;
            btnViewSeq.FlatAppearance.BorderColor = Color.Gray;
            btnViewSeq.FlatStyle = FlatStyle.Flat;
            btnViewSeq.Font = new Font("Segoe UI", 9F);
            btnViewSeq.ForeColor = Color.Black;
            btnViewSeq.Location = new Point(280, 3);
            btnViewSeq.Margin = new Padding(3, 4, 3, 4);
            btnViewSeq.Name = "btnViewSeq";
            btnViewSeq.Size = new Size(60, 30);
            btnViewSeq.TabIndex = 7;
            btnViewSeq.Text = "Text";
            btnViewSeq.UseVisualStyleBackColor = false;
            // 
            // btnViewDisasm
            // 
            btnViewDisasm.BackColor = SystemColors.Control;
            btnViewDisasm.FlatAppearance.BorderColor = Color.Gray;
            btnViewDisasm.FlatStyle = FlatStyle.Flat;
            btnViewDisasm.Font = new Font("Segoe UI", 9F);
            btnViewDisasm.ForeColor = Color.Black;
            btnViewDisasm.Location = new Point(214, 3);
            btnViewDisasm.Margin = new Padding(3, 4, 3, 4);
            btnViewDisasm.Name = "btnViewDisasm";
            btnViewDisasm.Size = new Size(60, 30);
            btnViewDisasm.TabIndex = 6;
            btnViewDisasm.Text = "Asm";
            btnViewDisasm.UseVisualStyleBackColor = false;
            // 
            // btnViewHex
            // 
            btnViewHex.BackColor = SystemColors.Control;
            btnViewHex.FlatAppearance.BorderColor = Color.Gray;
            btnViewHex.FlatStyle = FlatStyle.Flat;
            btnViewHex.Font = new Font("Segoe UI", 9F);
            btnViewHex.ForeColor = Color.Black;
            btnViewHex.Location = new Point(82, 3);
            btnViewHex.Margin = new Padding(3, 4, 3, 4);
            btnViewHex.Name = "btnViewHex";
            btnViewHex.Size = new Size(60, 30);
            btnViewHex.TabIndex = 5;
            btnViewHex.Text = "HEX";
            btnViewHex.UseVisualStyleBackColor = false;
            // 
            // btnToggleMulti
            // 
            btnToggleMulti.FlatStyle = FlatStyle.Flat;
            btnToggleMulti.Image = (Image)resources.GetObject("btnToggleMulti.Image");
            btnToggleMulti.ImageAlign = ContentAlignment.MiddleLeft;
            btnToggleMulti.Location = new Point(596, 3);
            btnToggleMulti.Margin = new Padding(3, 4, 3, 4);
            btnToggleMulti.Name = "btnToggleMulti";
            btnToggleMulti.Padding = new Padding(5, 0, 5, 0);
            btnToggleMulti.Size = new Size(83, 30);
            btnToggleMulti.TabIndex = 4;
            btnToggleMulti.Text = "Color";
            btnToggleMulti.TextAlign = ContentAlignment.MiddleRight;
            btnToggleMulti.UseVisualStyleBackColor = true;
            // 
            // btnViewImage
            // 
            btnViewImage.BackColor = SystemColors.Control;
            btnViewImage.FlatAppearance.BorderColor = Color.Gray;
            btnViewImage.FlatStyle = FlatStyle.Flat;
            btnViewImage.Font = new Font("Segoe UI", 9F);
            btnViewImage.ForeColor = Color.Black;
            btnViewImage.Location = new Point(346, 3);
            btnViewImage.Margin = new Padding(3, 4, 3, 4);
            btnViewImage.Name = "btnViewImage";
            btnViewImage.Size = new Size(70, 30);
            btnViewImage.TabIndex = 3;
            btnViewImage.Text = "Image";
            btnViewImage.UseVisualStyleBackColor = false;
            // 
            // btnViewFont
            // 
            btnViewFont.BackColor = SystemColors.Control;
            btnViewFont.FlatAppearance.BorderColor = Color.Gray;
            btnViewFont.FlatStyle = FlatStyle.Flat;
            btnViewFont.Font = new Font("Segoe UI", 9F);
            btnViewFont.ForeColor = Color.Black;
            btnViewFont.Location = new Point(422, 3);
            btnViewFont.Margin = new Padding(3, 4, 3, 4);
            btnViewFont.Name = "btnViewFont";
            btnViewFont.Size = new Size(60, 30);
            btnViewFont.TabIndex = 2;
            btnViewFont.Text = "Font";
            btnViewFont.UseVisualStyleBackColor = false;
            // 
            // btnViewBasic
            // 
            btnViewBasic.BackColor = SystemColors.Control;
            btnViewBasic.FlatAppearance.BorderColor = Color.Gray;
            btnViewBasic.FlatStyle = FlatStyle.Flat;
            btnViewBasic.Font = new Font("Segoe UI", 9F);
            btnViewBasic.ForeColor = Color.Black;
            btnViewBasic.Location = new Point(148, 3);
            btnViewBasic.Margin = new Padding(3, 4, 3, 4);
            btnViewBasic.Name = "btnViewBasic";
            btnViewBasic.Size = new Size(60, 30);
            btnViewBasic.TabIndex = 1;
            btnViewBasic.Text = "Basic";
            btnViewBasic.UseVisualStyleBackColor = false;
            // 
            // btnViewAuto
            // 
            btnViewAuto.BackColor = Color.FromArgb(62, 49, 162);
            btnViewAuto.FlatAppearance.BorderColor = Color.Gray;
            btnViewAuto.FlatStyle = FlatStyle.Flat;
            btnViewAuto.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnViewAuto.ForeColor = Color.White;
            btnViewAuto.Location = new Point(7, 3);
            btnViewAuto.Margin = new Padding(3, 4, 3, 4);
            btnViewAuto.Name = "btnViewAuto";
            btnViewAuto.Size = new Size(69, 30);
            btnViewAuto.TabIndex = 0;
            btnViewAuto.Text = "Auto";
            btnViewAuto.UseVisualStyleBackColor = false;
            // 
            // panelTop
            // 
            panelTop.BackColor = Color.FromArgb(242, 244, 248);
            panelTop.Controls.Add(toolStrip1);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Margin = new Padding(3, 4, 3, 4);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1282, 32);
            panelTop.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.BackColor = SystemColors.Control;
            toolStrip1.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 238);
            toolStrip1.ImageScalingSize = new Size(20, 20);
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolComboDrives, toolButtonRoot, toolStripSeparator1, toolCurrentLocation, toolStripSeparator2, toolBtnExit, toolStripSeparator6, toolBtnAbout, toolBtnExportFile, toolStripSeparator3, toolBtnYapeMountRun, toolStripSeparator4, toolBtnYapeQRun, toolStripSeparator5 });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(1282, 31);
            toolStrip1.TabIndex = 6;
            toolStrip1.Text = "toolStripMain";
            // 
            // toolComboDrives
            // 
            toolComboDrives.DropDownStyle = ComboBoxStyle.DropDownList;
            toolComboDrives.Name = "toolComboDrives";
            toolComboDrives.Size = new Size(75, 31);
            toolComboDrives.ToolTipText = "Select PC drive";
            // 
            // toolButtonRoot
            // 
            toolButtonRoot.BackColor = SystemColors.ButtonFace;
            toolButtonRoot.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolButtonRoot.Image = Properties.Resources.icoRoot;
            toolButtonRoot.ImageTransparentColor = Color.Magenta;
            toolButtonRoot.Name = "toolButtonRoot";
            toolButtonRoot.Padding = new Padding(10, 0, 10, 0);
            toolButtonRoot.Size = new Size(44, 28);
            toolButtonRoot.Text = "\\";
            toolButtonRoot.TextImageRelation = TextImageRelation.TextBeforeImage;
            toolButtonRoot.ToolTipText = "Select root folder";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 31);
            // 
            // toolCurrentLocation
            // 
            toolCurrentLocation.Font = new Font("Segoe UI", 7.8F, FontStyle.Bold, GraphicsUnit.Point, 238);
            toolCurrentLocation.Name = "toolCurrentLocation";
            toolCurrentLocation.Size = new Size(500, 31);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.AutoSize = false;
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(20, 31);
            // 
            // toolBtnExit
            // 
            toolBtnExit.Alignment = ToolStripItemAlignment.Right;
            toolBtnExit.Image = Properties.Resources.icoExit;
            toolBtnExit.ImageTransparentColor = Color.Magenta;
            toolBtnExit.Name = "toolBtnExit";
            toolBtnExit.Size = new Size(61, 28);
            toolBtnExit.Text = "Exit";
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Alignment = ToolStripItemAlignment.Right;
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(6, 31);
            // 
            // toolBtnAbout
            // 
            toolBtnAbout.Alignment = ToolStripItemAlignment.Right;
            toolBtnAbout.Image = (Image)resources.GetObject("toolBtnAbout.Image");
            toolBtnAbout.ImageTransparentColor = Color.Magenta;
            toolBtnAbout.Name = "toolBtnAbout";
            toolBtnAbout.Size = new Size(81, 28);
            toolBtnAbout.Text = "About";
            toolBtnAbout.ToolTipText = "About this app";
            // 
            // toolBtnExportFile
            // 
            toolBtnExportFile.BackColor = SystemColors.ButtonFace;
            toolBtnExportFile.Enabled = false;
            toolBtnExportFile.Image = Properties.Resources.icoExport;
            toolBtnExportFile.ImageTransparentColor = Color.Magenta;
            toolBtnExportFile.Name = "toolBtnExportFile";
            toolBtnExportFile.Size = new Size(83, 28);
            toolBtnExportFile.Text = "Export";
            toolBtnExportFile.ToolTipText = "Selected Commodore file export from D64/T64";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 31);
            // 
            // toolBtnYapeMountRun
            // 
            toolBtnYapeMountRun.BackColor = SystemColors.ButtonFace;
            toolBtnYapeMountRun.Image = (Image)resources.GetObject("toolBtnYapeMountRun.Image");
            toolBtnYapeMountRun.ImageTransparentColor = Color.Magenta;
            toolBtnYapeMountRun.Name = "toolBtnYapeMountRun";
            toolBtnYapeMountRun.Size = new Size(134, 28);
            toolBtnYapeMountRun.Text = "Mount+Load";
            toolBtnYapeMountRun.ToolTipText = "Mount D64/T64 and load PRG.";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 31);
            // 
            // toolBtnYapeQRun
            // 
            toolBtnYapeQRun.BackColor = SystemColors.ButtonFace;
            toolBtnYapeQRun.Image = Properties.Resources.icoRun;
            toolBtnYapeQRun.ImageTransparentColor = Color.Magenta;
            toolBtnYapeQRun.Name = "toolBtnYapeQRun";
            toolBtnYapeQRun.Size = new Size(112, 28);
            toolBtnYapeQRun.Text = "Quick Run";
            toolBtnYapeQRun.ToolTipText = "Direct run BASIC file in YAPE. Its very fast loading!";
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(6, 31);
            // 
            // panelBottomHelp
            // 
            panelBottomHelp.BackColor = Color.FromArgb(25, 25, 35);
            panelBottomHelp.Controls.Add(lblStatusHelp);
            panelBottomHelp.Dock = DockStyle.Bottom;
            panelBottomHelp.Location = new Point(0, 831);
            panelBottomHelp.Name = "panelBottomHelp";
            panelBottomHelp.Size = new Size(1282, 22);
            panelBottomHelp.TabIndex = 2;
            // 
            // lblStatusHelp
            // 
            lblStatusHelp.BackColor = SystemColors.Control;
            lblStatusHelp.Dock = DockStyle.Fill;
            lblStatusHelp.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblStatusHelp.ForeColor = Color.Teal;
            lblStatusHelp.Location = new Point(0, 0);
            lblStatusHelp.Name = "lblStatusHelp";
            lblStatusHelp.Size = new Size(1282, 22);
            lblStatusHelp.TabIndex = 0;
            lblStatusHelp.Text = "  [↑↓] Files    [Enter] Open D64/T64    [Backspace] Back    [←→] View    [Space] Auto    [F5] Export    [F6] Mount+Load    [F7] Quick Run    [F1] About";
            lblStatusHelp.TextAlign = ContentAlignment.MiddleRight;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1282, 853);
            Controls.Add(splitContainer1);
            Controls.Add(panelTop);
            Controls.Add(panelBottomHelp);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimumSize = new Size(1300, 900);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Plus/4 Commander v1.0";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitRight.Panel1.ResumeLayout(false);
            splitRight.Panel1.PerformLayout();
            splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitRight).EndInit();
            splitRight.ResumeLayout(false);
            panelContentWrapper.ResumeLayout(false);
            panelContentWrapper.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbView).EndInit();
            panelLoadMoreBar.ResumeLayout(false);
            panelViewButtons.ResumeLayout(false);
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            panelBottomHelp.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListBox listBoxFiles;
        private System.Windows.Forms.Label lblDiskHeader;
        private System.Windows.Forms.Label lblDiskFooter;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.SplitContainer splitRight;
        private System.Windows.Forms.TextBox textBoxFileInfo;
        private System.Windows.Forms.Panel panelContentWrapper;
        private System.Windows.Forms.PictureBox pbView;
        private System.Windows.Forms.TextBox textBoxInfo;
        private System.Windows.Forms.Panel panelLoadMoreBar;
        private System.Windows.Forms.Label lblTruncatedHint;
        private System.Windows.Forms.Button btnShowAllContent;
        private System.Windows.Forms.Panel panelViewButtons;
        private System.Windows.Forms.Button btnViewAuto;
        private System.Windows.Forms.Button btnViewBasic;
        private System.Windows.Forms.Button btnViewFont;
        private System.Windows.Forms.Button btnViewImage;
        private System.Windows.Forms.Button btnToggleMulti;
        private System.Windows.Forms.Button btnViewHex;
        private System.Windows.Forms.Button btnViewDisasm;
        private System.Windows.Forms.Button btnViewSeq;
        private System.Windows.Forms.Button btnShiftMinus;
        private System.Windows.Forms.Button btnShiftPlus;
        private System.Windows.Forms.Panel panelBottomHelp;
        private System.Windows.Forms.Label lblStatusHelp;
        private ToolStrip toolStrip1;
        private ToolStripComboBox toolComboDrives;
        private ToolStripButton toolButtonRoot;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripTextBox toolCurrentLocation;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton toolBtnExportFile;
        private ToolStripButton toolBtnAbout;
        private ToolStripButton toolBtnExit;
        private ToolStripButton toolBtnYapeMountRun;
        public ToolStripButton toolBtnYapeQRun;
        private ToolStripSeparator toolStripSeparator6;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripSeparator toolStripSeparator5;
    }
}
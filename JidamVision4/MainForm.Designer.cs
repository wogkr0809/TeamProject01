namespace JidamVision4
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modelNewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modelOpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modelSaveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modelSaveAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.imageOpenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageSaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setupToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.inspectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cycleModeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.measureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.miMeasureDistance = new System.Windows.Forms.ToolStripMenuItem();
            this.miMeasureBoardSize = new System.Windows.Forms.ToolStripMenuItem();
            this.modelCloseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.mainMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.setupToolStripMenuItem,
            this.inspectToolStripMenuItem,
            this.measureToolStripMenuItem});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Size = new System.Drawing.Size(1143, 33);
            this.mainMenu.TabIndex = 0;
            this.mainMenu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.modelNewMenuItem,
            this.modelOpenMenuItem,
            this.modelSaveMenuItem,
            this.modelSaveAsMenuItem,
            this.modelCloseToolStripMenuItem,
            this.toolStripSeparator1,
            this.imageOpenToolStripMenuItem,
            this.imageSaveToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(55, 29);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // modelNewMenuItem
            // 
            this.modelNewMenuItem.Name = "modelNewMenuItem";
            this.modelNewMenuItem.Size = new System.Drawing.Size(270, 34);
            this.modelNewMenuItem.Text = "Model New";
            this.modelNewMenuItem.Click += new System.EventHandler(this.modelNewMenuItem_Click);
            // 
            // modelOpenMenuItem
            // 
            this.modelOpenMenuItem.Name = "modelOpenMenuItem";
            this.modelOpenMenuItem.Size = new System.Drawing.Size(270, 34);
            this.modelOpenMenuItem.Text = "Model Open";
            this.modelOpenMenuItem.Click += new System.EventHandler(this.modelOpenMenuItem_Click);
            // 
            // modelSaveMenuItem
            // 
            this.modelSaveMenuItem.Name = "modelSaveMenuItem";
            this.modelSaveMenuItem.Size = new System.Drawing.Size(270, 34);
            this.modelSaveMenuItem.Text = "Model Save";
            this.modelSaveMenuItem.Click += new System.EventHandler(this.modelSaveMenuItem_Click);
            // 
            // modelSaveAsMenuItem
            // 
            this.modelSaveAsMenuItem.Name = "modelSaveAsMenuItem";
            this.modelSaveAsMenuItem.Size = new System.Drawing.Size(270, 34);
            this.modelSaveAsMenuItem.Text = "Model Save As";
            this.modelSaveAsMenuItem.Click += new System.EventHandler(this.modelSaveAsMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(267, 6);
            // 
            // imageOpenToolStripMenuItem
            // 
            this.imageOpenToolStripMenuItem.Name = "imageOpenToolStripMenuItem";
            this.imageOpenToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.imageOpenToolStripMenuItem.Text = "Image Open";
            this.imageOpenToolStripMenuItem.Click += new System.EventHandler(this.imageOpenToolStripMenuItem_Click);
            // 
            // imageSaveToolStripMenuItem
            // 
            this.imageSaveToolStripMenuItem.Name = "imageSaveToolStripMenuItem";
            this.imageSaveToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.imageSaveToolStripMenuItem.Text = "Image Save";
            // 
            // setupToolStripMenuItem
            // 
            this.setupToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setupToolStripMenuItem1});
            this.setupToolStripMenuItem.Name = "setupToolStripMenuItem";
            this.setupToolStripMenuItem.Size = new System.Drawing.Size(75, 29);
            this.setupToolStripMenuItem.Text = "Setup";
            // 
            // setupToolStripMenuItem1
            // 
            this.setupToolStripMenuItem1.Name = "setupToolStripMenuItem1";
            this.setupToolStripMenuItem1.Size = new System.Drawing.Size(161, 34);
            this.setupToolStripMenuItem1.Text = "Setup";
            this.setupToolStripMenuItem1.Click += new System.EventHandler(this.SetupMenuItem_Click);
            // 
            // inspectToolStripMenuItem
            // 
            this.inspectToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cycleModeMenuItem});
            this.inspectToolStripMenuItem.Name = "inspectToolStripMenuItem";
            this.inspectToolStripMenuItem.Size = new System.Drawing.Size(87, 29);
            this.inspectToolStripMenuItem.Text = "Inspect";
            // 
            // cycleModeMenuItem
            // 
            this.cycleModeMenuItem.CheckOnClick = true;
            this.cycleModeMenuItem.Name = "cycleModeMenuItem";
            this.cycleModeMenuItem.Size = new System.Drawing.Size(212, 34);
            this.cycleModeMenuItem.Text = "Cycle Mode";
            this.cycleModeMenuItem.Click += new System.EventHandler(this.cycleModeMenuItem_Click);
            // 
            // measureToolStripMenuItem
            // 
            this.measureToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miMeasureDistance,
            this.miMeasureBoardSize});
            this.measureToolStripMenuItem.Name = "measureToolStripMenuItem";
            this.measureToolStripMenuItem.Size = new System.Drawing.Size(98, 29);
            this.measureToolStripMenuItem.Text = "Measure";
            // 
            // miMeasureDistance
            // 
            this.miMeasureDistance.CheckOnClick = true;
            this.miMeasureDistance.Name = "miMeasureDistance";
            this.miMeasureDistance.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.miMeasureDistance.Size = new System.Drawing.Size(353, 34);
            this.miMeasureDistance.Text = "Distance (Two Points)";
            this.miMeasureDistance.Click += new System.EventHandler(this.miMeasureDistance_Click);
            // 
            // miMeasureBoardSize
            // 
            this.miMeasureBoardSize.Name = "miMeasureBoardSize";
            this.miMeasureBoardSize.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
            this.miMeasureBoardSize.Size = new System.Drawing.Size(353, 34);
            this.miMeasureBoardSize.Text = "Board Size (Auto)";
            this.miMeasureBoardSize.Click += new System.EventHandler(this.miMeasureBoardSize_Click);
            // 
            // modelCloseToolStripMenuItem
            // 
            this.modelCloseToolStripMenuItem.Name = "modelCloseToolStripMenuItem";
            this.modelCloseToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.modelCloseToolStripMenuItem.Text = "Model Close";
            this.modelCloseToolStripMenuItem.Click += new System.EventHandler(this.modelCloseToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1143, 675);
            this.Controls.Add(this.mainMenu);
            this.MainMenuStrip = this.mainMenu;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageOpenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageSaveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setupToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setupToolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem modelNewMenuItem;
        private System.Windows.Forms.ToolStripMenuItem modelOpenMenuItem;
        private System.Windows.Forms.ToolStripMenuItem modelSaveMenuItem;
        private System.Windows.Forms.ToolStripMenuItem modelSaveAsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem inspectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cycleModeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem measureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem miMeasureDistance;
        private System.Windows.Forms.ToolStripMenuItem miMeasureBoardSize;
        private System.Windows.Forms.ToolStripMenuItem modelCloseToolStripMenuItem;
    }
}
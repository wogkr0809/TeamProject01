namespace JidamVision4
{
    partial class CameraForm
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
            this.mainViewToolbar = new JidamVision4.UIControl.MainViewToolbar();
            this.imageViewer = new JidamVision4.UIControl.ImageViewCtrl();
            this.SuspendLayout();
            // 
            // mainViewToolbar
            // 
            this.mainViewToolbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.mainViewToolbar.Location = new System.Drawing.Point(999, 0);
            this.mainViewToolbar.Margin = new System.Windows.Forms.Padding(6);
            this.mainViewToolbar.Name = "mainViewToolbar";
            this.mainViewToolbar.Size = new System.Drawing.Size(93, 825);
            this.mainViewToolbar.TabIndex = 2;
            // 
            // imageViewer
            // 
            this.imageViewer.Dock = System.Windows.Forms.DockStyle.Left;
            this.imageViewer.Location = new System.Drawing.Point(0, 0);
            this.imageViewer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.imageViewer.Name = "imageViewer";
            this.imageViewer.Size = new System.Drawing.Size(673, 825);
            this.imageViewer.TabIndex = 1;
            this.imageViewer.WorkingState = "";
            // 
            // CameraForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1092, 825);
            this.Controls.Add(this.mainViewToolbar);
            this.Controls.Add(this.imageViewer);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "CameraForm";
            this.Text = "CameraForm";
            this.Resize += new System.EventHandler(this.CameraForm_Resize);
            this.ResumeLayout(false);

        }

        #endregion
        private UIControl.ImageViewCtrl imageViewer;
        private UIControl.MainViewToolbar mainViewToolbar;
    }
}
namespace JidamVision4.Property
{
    partial class SurfaceDefectProp
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.numMaskGrow = new System.Windows.Forms.NumericUpDown();
            this.chkScratch = new System.Windows.Forms.CheckBox();
            this.numTopHat = new System.Windows.Forms.NumericUpDown();
            this.chkSolder = new System.Windows.Forms.CheckBox();
            this.MaskGrow = new System.Windows.Forms.Label();
            this.TopHat = new System.Windows.Forms.Label();
            this.BinThr = new System.Windows.Forms.Label();
            this.MinLen = new System.Windows.Forms.Label();
            this.MaxWidth = new System.Windows.Forms.Label();
            this.numBinThr = new System.Windows.Forms.NumericUpDown();
            this.numMinLen = new System.Windows.Forms.NumericUpDown();
            this.numMaxWidth = new System.Windows.Forms.NumericUpDown();
            this.Thr = new System.Windows.Forms.Label();
            this.MinArea = new System.Windows.Forms.Label();
            this.MaxArea = new System.Windows.Forms.Label();
            this.numThr = new System.Windows.Forms.NumericUpDown();
            this.numMinArea = new System.Windows.Forms.NumericUpDown();
            this.numMaxArea = new System.Windows.Forms.NumericUpDown();
            this.btnApply = new System.Windows.Forms.Button();
            this.Dilate = new System.Windows.Forms.Label();
            this.numDilate = new System.Windows.Forms.NumericUpDown();
            this.SolderOpen = new System.Windows.Forms.Label();
            this.numSolderOpen = new System.Windows.Forms.NumericUpDown();
            this.MinRatio = new System.Windows.Forms.Label();
            this.numMinRatio = new System.Windows.Forms.NumericUpDown();
            this.ScratchOpen = new System.Windows.Forms.Label();
            this.numScratchOpen = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numMaskGrow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTopHat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBinThr)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinLen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThr)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinArea)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxArea)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDilate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSolderOpen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinRatio)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numScratchOpen)).BeginInit();
            this.SuspendLayout();
            // 
            // numMaskGrow
            // 
            this.numMaskGrow.Location = new System.Drawing.Point(350, 308);
            this.numMaskGrow.Name = "numMaskGrow";
            this.numMaskGrow.Size = new System.Drawing.Size(120, 25);
            this.numMaskGrow.TabIndex = 0;
            // 
            // chkScratch
            // 
            this.chkScratch.AutoSize = true;
            this.chkScratch.Location = new System.Drawing.Point(26, 45);
            this.chkScratch.Name = "chkScratch";
            this.chkScratch.Size = new System.Drawing.Size(102, 19);
            this.chkScratch.TabIndex = 1;
            this.chkScratch.Text = "chkScratch";
            this.chkScratch.UseVisualStyleBackColor = true;
            // 
            // numTopHat
            // 
            this.numTopHat.Location = new System.Drawing.Point(105, 70);
            this.numTopHat.Name = "numTopHat";
            this.numTopHat.Size = new System.Drawing.Size(120, 25);
            this.numTopHat.TabIndex = 2;
            // 
            // chkSolder
            // 
            this.chkSolder.AutoSize = true;
            this.chkSolder.Location = new System.Drawing.Point(351, 33);
            this.chkSolder.Name = "chkSolder";
            this.chkSolder.Size = new System.Drawing.Size(94, 19);
            this.chkSolder.TabIndex = 3;
            this.chkSolder.Text = "chkSolder";
            this.chkSolder.UseVisualStyleBackColor = true;
            // 
            // MaskGrow
            // 
            this.MaskGrow.AutoSize = true;
            this.MaskGrow.Location = new System.Drawing.Point(268, 310);
            this.MaskGrow.Name = "MaskGrow";
            this.MaskGrow.Size = new System.Drawing.Size(76, 15);
            this.MaskGrow.TabIndex = 4;
            this.MaskGrow.Text = "MaskGrow";
            // 
            // TopHat
            // 
            this.TopHat.AutoSize = true;
            this.TopHat.Location = new System.Drawing.Point(17, 77);
            this.TopHat.Name = "TopHat";
            this.TopHat.Size = new System.Drawing.Size(54, 15);
            this.TopHat.TabIndex = 5;
            this.TopHat.Text = "TopHat";
            // 
            // BinThr
            // 
            this.BinThr.AutoSize = true;
            this.BinThr.Location = new System.Drawing.Point(18, 108);
            this.BinThr.Name = "BinThr";
            this.BinThr.Size = new System.Drawing.Size(48, 15);
            this.BinThr.TabIndex = 6;
            this.BinThr.Text = "BinThr";
            // 
            // MinLen
            // 
            this.MinLen.AutoSize = true;
            this.MinLen.Location = new System.Drawing.Point(18, 176);
            this.MinLen.Name = "MinLen";
            this.MinLen.Size = new System.Drawing.Size(54, 15);
            this.MinLen.TabIndex = 8;
            this.MinLen.Text = "MinLen";
            // 
            // MaxWidth
            // 
            this.MaxWidth.AutoSize = true;
            this.MaxWidth.Location = new System.Drawing.Point(17, 207);
            this.MaxWidth.Name = "MaxWidth";
            this.MaxWidth.Size = new System.Drawing.Size(73, 15);
            this.MaxWidth.TabIndex = 9;
            this.MaxWidth.Text = "MaxWidth";
            // 
            // numBinThr
            // 
            this.numBinThr.Location = new System.Drawing.Point(105, 106);
            this.numBinThr.Name = "numBinThr";
            this.numBinThr.Size = new System.Drawing.Size(120, 25);
            this.numBinThr.TabIndex = 11;
            // 
            // numMinLen
            // 
            this.numMinLen.Location = new System.Drawing.Point(105, 176);
            this.numMinLen.Name = "numMinLen";
            this.numMinLen.Size = new System.Drawing.Size(120, 25);
            this.numMinLen.TabIndex = 13;
            // 
            // numMaxWidth
            // 
            this.numMaxWidth.Location = new System.Drawing.Point(105, 207);
            this.numMaxWidth.Name = "numMaxWidth";
            this.numMaxWidth.Size = new System.Drawing.Size(120, 25);
            this.numMaxWidth.TabIndex = 14;
            // 
            // Thr
            // 
            this.Thr.AutoSize = true;
            this.Thr.Location = new System.Drawing.Point(249, 77);
            this.Thr.Name = "Thr";
            this.Thr.Size = new System.Drawing.Size(27, 15);
            this.Thr.TabIndex = 16;
            this.Thr.Text = "Thr";
            // 
            // MinArea
            // 
            this.MinArea.AutoSize = true;
            this.MinArea.Location = new System.Drawing.Point(237, 145);
            this.MinArea.Name = "MinArea";
            this.MinArea.Size = new System.Drawing.Size(59, 15);
            this.MinArea.TabIndex = 17;
            this.MinArea.Text = "MinArea";
            // 
            // MaxArea
            // 
            this.MaxArea.AutoSize = true;
            this.MaxArea.Location = new System.Drawing.Point(231, 178);
            this.MaxArea.Name = "MaxArea";
            this.MaxArea.Size = new System.Drawing.Size(65, 15);
            this.MaxArea.TabIndex = 18;
            this.MaxArea.Text = "MaxArea";
            // 
            // numThr
            // 
            this.numThr.Location = new System.Drawing.Point(334, 75);
            this.numThr.Name = "numThr";
            this.numThr.Size = new System.Drawing.Size(120, 25);
            this.numThr.TabIndex = 22;
            // 
            // numMinArea
            // 
            this.numMinArea.Location = new System.Drawing.Point(334, 145);
            this.numMinArea.Name = "numMinArea";
            this.numMinArea.Size = new System.Drawing.Size(120, 25);
            this.numMinArea.TabIndex = 23;
            // 
            // numMaxArea
            // 
            this.numMaxArea.Location = new System.Drawing.Point(334, 176);
            this.numMaxArea.Name = "numMaxArea";
            this.numMaxArea.Size = new System.Drawing.Size(120, 25);
            this.numMaxArea.TabIndex = 24;
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(369, 217);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(101, 44);
            this.btnApply.TabIndex = 25;
            this.btnApply.Text = "적용";
            this.btnApply.UseVisualStyleBackColor = true;
            // 
            // Dilate
            // 
            this.Dilate.AutoSize = true;
            this.Dilate.Location = new System.Drawing.Point(18, 143);
            this.Dilate.Name = "Dilate";
            this.Dilate.Size = new System.Drawing.Size(43, 15);
            this.Dilate.TabIndex = 26;
            this.Dilate.Text = "Dilate";
            // 
            // numDilate
            // 
            this.numDilate.Location = new System.Drawing.Point(105, 141);
            this.numDilate.Name = "numDilate";
            this.numDilate.Size = new System.Drawing.Size(120, 25);
            this.numDilate.TabIndex = 27;
            // 
            // SolderOpen
            // 
            this.SolderOpen.AutoSize = true;
            this.SolderOpen.Location = new System.Drawing.Point(243, 108);
            this.SolderOpen.Name = "SolderOpen";
            this.SolderOpen.Size = new System.Drawing.Size(85, 15);
            this.SolderOpen.TabIndex = 28;
            this.SolderOpen.Text = "SolderOpen";
            // 
            // numSolderOpen
            // 
            this.numSolderOpen.Location = new System.Drawing.Point(334, 106);
            this.numSolderOpen.Name = "numSolderOpen";
            this.numSolderOpen.Size = new System.Drawing.Size(120, 25);
            this.numSolderOpen.TabIndex = 29;
            // 
            // MinRatio
            // 
            this.MinRatio.AutoSize = true;
            this.MinRatio.Location = new System.Drawing.Point(17, 236);
            this.MinRatio.Name = "MinRatio";
            this.MinRatio.Size = new System.Drawing.Size(64, 15);
            this.MinRatio.TabIndex = 30;
            this.MinRatio.Text = "MinRatio";
            // 
            // numMinRatio
            // 
            this.numMinRatio.Location = new System.Drawing.Point(105, 238);
            this.numMinRatio.Name = "numMinRatio";
            this.numMinRatio.Size = new System.Drawing.Size(120, 25);
            this.numMinRatio.TabIndex = 31;
            // 
            // ScratchOpen
            // 
            this.ScratchOpen.AutoSize = true;
            this.ScratchOpen.Location = new System.Drawing.Point(3, 269);
            this.ScratchOpen.Name = "ScratchOpen";
            this.ScratchOpen.Size = new System.Drawing.Size(93, 15);
            this.ScratchOpen.TabIndex = 32;
            this.ScratchOpen.Text = "ScratchOpen";
            // 
            // numScratchOpen
            // 
            this.numScratchOpen.Location = new System.Drawing.Point(105, 269);
            this.numScratchOpen.Name = "numScratchOpen";
            this.numScratchOpen.Size = new System.Drawing.Size(120, 25);
            this.numScratchOpen.TabIndex = 33;
            // 
            // SurfaceDefectProp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.numScratchOpen);
            this.Controls.Add(this.ScratchOpen);
            this.Controls.Add(this.numMinRatio);
            this.Controls.Add(this.MinRatio);
            this.Controls.Add(this.numSolderOpen);
            this.Controls.Add(this.SolderOpen);
            this.Controls.Add(this.numDilate);
            this.Controls.Add(this.Dilate);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.numMaxArea);
            this.Controls.Add(this.numMinArea);
            this.Controls.Add(this.numThr);
            this.Controls.Add(this.MaxArea);
            this.Controls.Add(this.MinArea);
            this.Controls.Add(this.Thr);
            this.Controls.Add(this.numMaxWidth);
            this.Controls.Add(this.numMinLen);
            this.Controls.Add(this.numBinThr);
            this.Controls.Add(this.MaxWidth);
            this.Controls.Add(this.MinLen);
            this.Controls.Add(this.BinThr);
            this.Controls.Add(this.TopHat);
            this.Controls.Add(this.MaskGrow);
            this.Controls.Add(this.chkSolder);
            this.Controls.Add(this.numTopHat);
            this.Controls.Add(this.chkScratch);
            this.Controls.Add(this.numMaskGrow);
            this.Name = "SurfaceDefectProp";
            this.Size = new System.Drawing.Size(499, 373);
            ((System.ComponentModel.ISupportInitialize)(this.numMaskGrow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTopHat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBinThr)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinLen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThr)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinArea)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxArea)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDilate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSolderOpen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinRatio)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numScratchOpen)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown numMaskGrow;
        private System.Windows.Forms.CheckBox chkScratch;
        private System.Windows.Forms.NumericUpDown numTopHat;
        private System.Windows.Forms.CheckBox chkSolder;
        private System.Windows.Forms.Label MaskGrow;
        private System.Windows.Forms.Label TopHat;
        private System.Windows.Forms.Label BinThr;
        private System.Windows.Forms.Label MinLen;
        private System.Windows.Forms.Label MaxWidth;
        private System.Windows.Forms.NumericUpDown numBinThr;
        private System.Windows.Forms.NumericUpDown numMinLen;
        private System.Windows.Forms.NumericUpDown numMaxWidth;
        private System.Windows.Forms.Label Thr;
        private System.Windows.Forms.Label MinArea;
        private System.Windows.Forms.Label MaxArea;
        private System.Windows.Forms.NumericUpDown numThr;
        private System.Windows.Forms.NumericUpDown numMinArea;
        private System.Windows.Forms.NumericUpDown numMaxArea;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Label Dilate;
        private System.Windows.Forms.NumericUpDown numDilate;
        private System.Windows.Forms.Label SolderOpen;
        private System.Windows.Forms.NumericUpDown numSolderOpen;
        private System.Windows.Forms.Label MinRatio;
        private System.Windows.Forms.NumericUpDown numMinRatio;
        private System.Windows.Forms.Label ScratchOpen;
        private System.Windows.Forms.NumericUpDown numScratchOpen;
    }
}

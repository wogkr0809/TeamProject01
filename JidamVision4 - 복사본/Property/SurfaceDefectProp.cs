using JidamVision4.Algorithm;
using JidamVision4.Core;
using JidamVision4.Teach;
using OpenCvSharp;
using OpenCvSharp.Extensions; // BitmapConverter
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace JidamVision4.Property
{
    public partial class SurfaceDefectProp : UserControl
    {

        private InspWindow _window;
        private SurfaceDefectAlgorithm _algo;

        // VisualStyles와의 모호성 회피를 위해 완전수식 사용
        private System.Windows.Forms.Button _btnEditMask;
        private System.Windows.Forms.CheckBox _chkUseMask;

        public SurfaceDefectProp()
        {
            InitializeComponent();

            // ---- UI 구성(동적 추가) ----
            _btnEditMask = new System.Windows.Forms.Button { Text = "Edit Mask...", Width = 110 };
            _chkUseMask = new System.Windows.Forms.CheckBox { Text = "Use Manual Mask", AutoSize = true, Checked = true };

            Controls.Add(_btnEditMask);
            Controls.Add(_chkUseMask);
            _btnEditMask.Top = 4; _btnEditMask.Left = 4;
            _chkUseMask.Top = 8; _chkUseMask.Left = 130;

            _btnEditMask.Click += btnEditMask_Click;
            _chkUseMask.CheckedChanged += (s, e) =>
            {
                if (_algo != null) _algo.UseManualMask = _chkUseMask.Checked;
            };
            btnApply.Click += (s, e) => ApplyToAlgo();

            chkScratch.CheckedChanged += (s, e) =>
            {
                EnableScratchInputs(chkScratch.Checked);
                if (_algo != null) _algo.EnableScratch = chkScratch.Checked;
            };
            chkSolder.CheckedChanged += (s, e) =>
            {
                EnableSolderInputs(chkSolder.Checked);
                if (_algo != null) _algo.EnableSolder = chkSolder.Checked;
            };
        }
        private void ApplyToAlgo()
        {
            if (_algo == null) return;

            // 체크박스
            _algo.EnableScratch = chkScratch.Checked;
            _algo.EnableSolder = chkSolder.Checked;

            // Scratch
            _algo.ScratchTopHat = (int)numTopHat.Value;
            _algo.ScratchBinThr = (int)numBinThr.Value;
            _algo.ScratchDilate = (int)numDilate.Value;  
            _algo.ScratchMinLen = (int)numMinLen.Value;
            _algo.ScratchMaxWidth = (int)numMaxWidth.Value;
           
            // Solder
            _algo.SolderThr = (int)numThr.Value;
            _algo.SolderOpen = (int)numDilate.Value;
            _algo.SolderMinArea = (int)numMinArea.Value;
            _algo.SolderMaxArea = (int)numMaxArea.Value;

            // 공통
            _algo.MaskGrow = (int)numMaskGrow.Value;

            
        }
        static OpenCvSharp.Rect ClampRectToMat(OpenCvSharp.Rect r, Mat m)
        {
            int x = Math.Max(0, Math.Min(r.X, m.Cols - 1));
            int y = Math.Max(0, Math.Min(r.Y, m.Rows - 1));
            int w = Math.Min(r.Width, m.Cols - x);
            int h = Math.Min(r.Height, m.Rows - y);
            if (w < 0) w = 0;
            if (h < 0) h = 0;
            return new OpenCvSharp.Rect(x, y, w, h);
        }

        static Bitmap SafeRoiToBitmap(Mat whole, OpenCvSharp.Rect r)
        {
            if (whole == null || whole.Empty()) throw new ArgumentException("empty image");
            r = ClampRectToMat(r, whole);
            if (r.Width == 0 || r.Height == 0) throw new ArgumentException("ROI out of range / zero size");

            // SubMat → Clone으로 연속 메모리 확보
            using (var sub = new Mat(whole, r))
            using (var roi = sub.Clone())
            {
                Mat vis;
                int ch = roi.Channels();
                if (ch == 1)
                {
                    vis = new Mat();
                    Cv2.CvtColor(roi, vis, ColorConversionCodes.GRAY2BGR);
                }
                else if (ch == 3)
                {
                    vis = roi.Clone();
                }
                else if (ch == 4)
                {
                    vis = new Mat();
                    Cv2.CvtColor(roi, vis, ColorConversionCodes.BGRA2BGR);
                }
                else
                {
                    // 8UC3으로 강제
                    vis = new Mat();
                    Cv2.ConvertScaleAbs(roi, vis);
                    Cv2.CvtColor(vis, vis, ColorConversionCodes.GRAY2BGR);
                }

                try
                {
                    return BitmapConverter.ToBitmap(vis); // 8UC3 → Bitmap
                }
                finally
                {
                    vis.Dispose();
                }
            }
        }

        static Bitmap EnsureEditable(Bitmap src)
        {
            if ((src.PixelFormat & PixelFormat.Indexed) != 0)
            {
                var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(dst))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    g.DrawImageUnscaled(src, 0, 0);
                }
                src.Dispose();
                return dst;
            }
            if (src.PixelFormat != PixelFormat.Format32bppArgb)
            {
                var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(dst)) g.DrawImageUnscaled(src, 0, 0);
                src.Dispose();
                return dst;
            }
            return src;
        }
        private void LoadFromAlgo()
        {
            if (_algo == null) return;

            // 체크박스
            chkScratch.Checked = _algo.EnableScratch;
            chkSolder.Checked = _algo.EnableSolder;

            // Scratch 쪽
            numTopHat.Value = _algo.ScratchTopHat;
            numBinThr.Value = _algo.ScratchBinThr;
            numDilate.Value = _algo.ScratchDilate;
            numMinLen.Value = _algo.ScratchMinLen;
            numMaxWidth.Value = _algo.ScratchMaxWidth;
            

            // Solder 쪽
            numThr.Value = _algo.SolderThr;
            numOpen.Value = _algo.SolderOpen;
            numMinArea.Value = _algo.SolderMinArea;
            numMaxArea.Value = _algo.SolderMaxArea;

            // 공통
            numMaskGrow.Value = _algo.MaskGrow;

            // 필요하면 잠금/활성화
            EnableScratchInputs(chkScratch.Checked);
            EnableSolderInputs(chkSolder.Checked);
        }

        /// <summary>PropertiesForm에서 ROI/알고리즘을 바인딩</summary>
        public void Bind(InspWindow win, SurfaceDefectAlgorithm algo)
        {
            _window = win;
            _algo = algo;

            bool ok = (_window != null && _algo != null);
            this.Enabled = ok;
            _chkUseMask.Checked = ok && _algo.UseManualMask;

            if (ok) LoadFromAlgo();
        }
        private void EnableScratchInputs(bool on)
        {
            numTopHat.Enabled = numBinThr.Enabled = numDilate.Enabled = numMinLen.Enabled = numMaxWidth.Enabled = on;
        }
        private void EnableSolderInputs(bool on)
        {
            numThr.Enabled = numOpen.Enabled = numMinArea.Enabled = numMaxArea.Enabled = on;
        }

        private void btnEditMask_Click(object sender, EventArgs e)
        {
            if (_algo == null || _window == null) return;

            // 현재 채널 영상 얻기
            var whole = Global.Inst.InspStage.GetMat(0, _algo.ImageChannel);
            if (whole == null || whole.Empty()) { MessageBox.Show("이미지가 없습니다."); return; }

            // ROI로 자르기
            var r = new OpenCvSharp.Rect(_algo.InspRect.X, _algo.InspRect.Y, _algo.InspRect.Width, _algo.InspRect.Height);
            Bitmap bg = null;
            try
            {
                bg = SafeRoiToBitmap(whole, r);   // ← 안전 변환

                Bitmap mask = _algo.CustomMask;
                if (mask == null || mask.Width != bg.Width || mask.Height != bg.Height)
                {
                    mask = new Bitmap(bg.Width, bg.Height, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(mask)) g.Clear(Color.White);  // 기본: 전부 검사
                }
                else
                {
                    mask = EnsureEditable(mask);  // 인덱스/24bpp → 32bpp
                }

                using (var dlg = new MaskEditorForm(bg, mask))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        _algo.CustomMask = (Bitmap)dlg.ResultMask.Clone();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ROI 비트맵 변환 실패: " + ex.Message);
            }
            finally
            {
                bg?.Dispose();
            }
        }
    }
}
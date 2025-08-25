using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JidamVision4
{
    /// <summary>
    /// 검정(제외) / 흰색(검사)로 마스크를 직접 칠하는 간단한 에디터
    /// - 좌클릭: 현재 색으로 칠하기
    /// - 우클릭: 색 빠르게 토글(검정↔흰색)
    /// - 휠/UpDown: 브러시 크기
    /// - X 키: 색 토글
    /// </summary>
    public partial class MaskEditorForm : Form
    {
        readonly Bitmap _bg;          // ROI 원본 배경
        Bitmap _mask;                 // 편집 대상(흰=검사, 검=제외)
        PictureBox _view;
        TrackBar _brush;
        NumericUpDown _brushUd;
        System.Windows.Forms.Button _btnBlack, _btnWhite, _btnOK, _btnCancel;

        bool _paintToBlack = true;    // true=검정 칠하기, false=흰색 칠하기
        bool _isDown = false;
        Point _last;

        public Bitmap ResultMask { get; private set; }

        public MaskEditorForm(Bitmap background, Bitmap mask)
        {
            Text = "Edit Mask (Black=Exclude, White=Inspect)";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            Width = background.Width + 120;
            Height = background.Height + 120;

            _bg = (Bitmap)background.Clone();
            _mask = EnsureEditable(mask);

            BuildUI();
            WireEvents();
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
                return dst;
            }
            // 32bpp로 통일 권장
            if (src.PixelFormat != PixelFormat.Format32bppArgb)
            {
                var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(dst)) g.DrawImageUnscaled(src, 0, 0);
                return dst;
            }
            return (Bitmap)src.Clone();
        }

        void BuildUI()
        {
            var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40 };
            _brush = new TrackBar { Minimum = 3, Maximum = 120, TickFrequency = 10, Value = 30, Width = 150 };
            _brushUd = new NumericUpDown { Minimum = 3, Maximum = 120, Value = 30, Width = 60 };
            _btnBlack = new System.Windows.Forms.Button { Text = "Paint Black(제외)", Width = 140 };
            _btnWhite = new System.Windows.Forms.Button { Text = "Paint White(검사)", Width = 140 };
            _btnOK = new System.Windows.Forms.Button { Text = "OK", Width = 100 };
            _btnCancel = new System.Windows.Forms.Button { Text = "Cancel", Width = 100 };

            pnlTop.Controls.Add(new Label { Text = "Brush", AutoSize = true, Padding = new Padding(10, 10, 0, 0) });
            pnlTop.Controls.Add(_brush);
            pnlTop.Controls.Add(_brushUd);
            pnlTop.Controls.Add(_btnBlack);
            pnlTop.Controls.Add(_btnWhite);
            pnlTop.Controls.Add(_btnOK);
            pnlTop.Controls.Add(_btnCancel);

            _view = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Normal,   // 1:1 픽셀 편집
                BackColor = Color.Black
            };

            Controls.Add(_view);
            Controls.Add(pnlTop);
        }

        void WireEvents()
        {
            _brush.ValueChanged += (s, e) => _brushUd.Value = _brush.Value;
            _brushUd.ValueChanged += (s, e) => _brush.Value = (int)_brushUd.Value;
            _btnBlack.Click += (s, e) => _paintToBlack = true;
            _btnWhite.Click += (s, e) => _paintToBlack = false;
            _btnOK.Click += (s, e) => { ResultMask = (Bitmap)_mask.Clone(); DialogResult = DialogResult.OK; Close(); };
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _view.Paint += View_Paint;
            _view.MouseDown += View_MouseDown;
            _view.MouseMove += View_MouseMove;
            _view.MouseUp += (s, e) => _isDown = false;
            Resize += (s, e) => _view.Invalidate();
        }

        void View_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            // 배경
            g.DrawImageUnscaled(_bg, 0, 0);

            // 마스크(흰=투명, 검=반투명 표시)
            using (var ia = new ImageAttributes())
            {
                ia.SetColorKey(Color.White, Color.White); // 흰색 투명
                                                          // 약간 어둡게 보이도록 알파 적용
                var cm = new ColorMatrix(new float[][]
                {
                new float[]{1,0,0,0,0},
                new float[]{0,1,0,0,0},
                new float[]{0,0,1,0,0},
                new float[]{0,0,0,0.35f,0}, // 알파 0.35
                new float[]{0,0,0,0,0}
                });
                ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                var r = new Rectangle(0, 0, _mask.Width, _mask.Height);
                g.DrawImage(_mask, r, 0, 0, _mask.Width, _mask.Height, GraphicsUnit.Pixel, ia);
            }
        }

        void View_MouseDown(object sender, MouseEventArgs e)
        {
            _isDown = true;
            _last = e.Location;
            DrawDot(e.Location);
        }

        void View_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDown) return;
            DrawLine(_last, e.Location);
            _last = e.Location;
        }

        void DrawDot(Point p)
        {
            using (var g = Graphics.FromImage(_mask))
            using (var b = new SolidBrush(_paintToBlack ? Color.Black : Color.White))
            {
                int r = _brush.Value;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillEllipse(b, p.X - r / 2, p.Y - r / 2, r, r);
            }
            _view.Invalidate(new Rectangle(p.X - _brush.Value, p.Y - _brush.Value, _brush.Value * 2, _brush.Value * 2));
        }

        void DrawLine(Point a, Point b)
        {
            using (var g = Graphics.FromImage(_mask))
            using (var p = new Pen(_paintToBlack ? Color.Black : Color.White, _brush.Value)
            { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawLine(p, a, b);
            }
            _view.Invalidate();
        }
    }
}



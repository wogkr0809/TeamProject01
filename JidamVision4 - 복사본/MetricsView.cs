using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JidamVision4
{
    public class MetricsView : Control
    {
        long _total, _ok, _ng;  

        public MetricsView()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
        }

        public void UpdateValues(long total, long ok, long ng)
        {
            _total = total; _ok = ok; _ng = ng; 
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var area = ClientRectangle;
            if (area.Width < 40 || area.Height < 40) return;

            // 여백
            area.Inflate(-10, -10);

            bool horizontal = area.Width >= 360; // 충분히 넓으면 가로 3개
            if (horizontal)
            {
                int tileW = (area.Width - 40) / 3;
                int tileH = Math.Min(80, area.Height);
                int y = area.Top + (area.Height - tileH) / 2;

                DrawTile(g, new Rectangle(area.Left + 10, y, tileW, tileH), "Total", _total);
                DrawTile(g, new Rectangle(area.Left + 20 + tileW, y, tileW, tileH), "OK", _ok);
                DrawTile(g, new Rectangle(area.Left + 30 + tileW * 2, y, tileW, tileH), "NG", _ng);
            }
            else
            {
                int gap = 8;
                int tileW = area.Width;
                int tileH = Math.Min(64, (area.Height - gap * 2) / 3);

                DrawTile(g, new Rectangle(area.Left, area.Top, tileW, tileH), "Total", _total);
                DrawTile(g, new Rectangle(area.Left, area.Top + tileH + gap, tileW, tileH), "OK", _ok);
                DrawTile(g, new Rectangle(area.Left, area.Top + (tileH + gap) * 2, tileW, tileH), "NG", _ng);
            }
        }

        void DrawTile(Graphics g, Rectangle r, string title, long value)
        {
            using (var bg = new SolidBrush(Color.FromArgb(37, 68, 115)))
                GfxExt.FillRounded(g, r, Math.Min(14, r.Height / 5), bg);

            int titleSize = Math.Max(10, r.Height / 6);
            int valueSize = Math.Max(18, r.Height / 2);

            using (var f1 = new Font("Segoe UI", titleSize, FontStyle.Bold))
            using (var white = new SolidBrush(Color.White))
                g.DrawString(title, f1, white, new PointF(r.Left + 12, r.Top + 8));

            using (var f2 = new Font("Segoe UI", valueSize, FontStyle.Bold))
            using (var white2 = new SolidBrush(Color.White))
            {
                string txt = value.ToString();
                var sz = g.MeasureString(txt, f2);
                g.DrawString(txt, f2, white2,
                    new PointF(r.Left + (r.Width - sz.Width) / 2f,
                               r.Top + (r.Height - sz.Height) / 2f + 4));
            }
        }
    }
}

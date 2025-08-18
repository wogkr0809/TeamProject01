using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JidamVision4
{
    public static class GfxExt
    {
        public static void FillRounded(this Graphics g, Rectangle r, int radius, Brush brush)
        {
            using (var gp = new GraphicsPath())
            {
                int d = radius * 2;
                gp.AddArc(r.Left, r.Top, d, d, 180, 90);
                gp.AddArc(r.Right - d, r.Top, d, d, 270, 90);
                gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                gp.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
                gp.CloseFigure();
                g.FillPath(brush, gp);
            }
        }
    }
}

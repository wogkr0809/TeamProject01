using JidamVision4.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace JidamVision4
{
    public partial class CountForm : DockContent
    {
        MetricsView _view;
        Button _btnReset, _btnExport;
       
        public CountForm()
        {
            Text = "Counter";
            Padding = new Padding(10);

            _view = new MetricsView { Dock = DockStyle.Fill };

            _btnExport = new Button { Dock = DockStyle.Bottom, Height = 36, Text = "Export PDF" };
            _btnReset = new Button { Dock = DockStyle.Bottom, Height = 36, Text = "Reset" };

            Controls.Add(_view);
            Controls.Add(_btnExport);   // 아래쪽
            Controls.Add(_btnReset);    // 그 위(Reset → Export 순서로 보이게)

            _btnReset.Click += (s, e) => Global.Inst.InspStage.ResetAccum();
            _btnExport.Click += (s, e) => ExportPng();  // PDF 라이브러리 없으면 PNG로 내보내기

            Global.Inst.InspStage.AccumChanged += OnAccumChanged;
        }
           
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Global.Inst.InspStage.AccumChanged -= OnAccumChanged;
      
            base.OnFormClosed(e);
        }

        void OnAccumChanged(AccumCounter c)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnAccumChanged(c))); return; }
            _view.UpdateValues(c.Total, c.OK, c.NG);
        }
       

        void ExportPng()
        {
            using (var sfd = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = "Dashboard.png" })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;
                using (var bmp = new Bitmap(Width, Height))
                {
                    DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    bmp.Save(sfd.FileName);
                }
            }
        }
    }
}

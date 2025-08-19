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
using JidamVision4;

namespace JidamVision4
{
    public partial class CountForm : DockContent
    {
        MetricsView _view;
        DataGridView _catGrid;
        Button _btnReset, _btnExport;


        readonly string[] _cats = NgCategoryCounter.Keys;
        public CountForm()
        {
            Text = "Counter";
            Padding = new Padding(10);

            _view = new MetricsView { Dock = DockStyle.Top, Height = 260 };

            // ▼ 카테고리 표
            _catGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _catGrid.Columns.Add("cat", "Category");
            _catGrid.Columns.Add("cnt", "NG");
            foreach (var c in _cats) _catGrid.Rows.Add(c, 0);

            _btnExport = new Button { Dock = DockStyle.Bottom, Height = 36, Text = "Export PDF" };
            _btnReset = new Button { Dock = DockStyle.Bottom, Height = 36, Text = "Reset" };

            Controls.Add(_catGrid);   // 가운데(Fill)
            Controls.Add(_view);
            Controls.Add(_btnExport);   // 아래쪽
            Controls.Add(_btnReset);    // 그 위(Reset → Export 순서로 보이게)

            _btnReset.Click += (s, e) =>
            {
                Global.Inst.InspStage.ResetAccum();
                Global.Inst.InspStage.ResetCategory();
            };
            _btnExport.Click += (s, e) => ExportPng();

            Global.Inst.InspStage.AccumChanged += OnAccumChanged;

            Global.Inst.InspStage.CategoryChanged += OnCategoryChanged;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Global.Inst.InspStage.AccumChanged -= OnAccumChanged;
            Global.Inst.InspStage.CategoryChanged -= OnCategoryChanged;
            base.OnFormClosed(e);
        }

        void OnAccumChanged(AccumCounter c)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnAccumChanged(c))); return; }
            _view.UpdateValues(c.Total, c.OK, c.NG);
        }

        void OnCategoryChanged(IReadOnlyDictionary<string, long> map)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnCategoryChanged(map))); return; }
            for (int i = 0; i < _cats.Length; i++)
            {
                long v = 0;
                map.TryGetValue(_cats[i], out v);
                _catGrid.Rows[i].Cells[1].Value = v;
            }
        }

        void ExportPng()
        {
            using (var sfd = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = "Counter.png" })
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

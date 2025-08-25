using JidamVision4.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

// iTextSharp (PDF 생성에 필요한 것만)
using PdfDocument = iTextSharp.text.Document;
using PdfPageSize = iTextSharp.text.PageSize;
using PdfWriter = iTextSharp.text.pdf.PdfWriter;

using JidamVision4.Reports;

namespace JidamVision4
{
    public partial class CountForm : DockContent
    {
        // ====== 필드 ======
        MetricsView _view;
        DataGridView _catGrid;
        Button _btnReset, _btnExport;

        // KPI 누적값 (페이지2에 사용)
        int _accTotal, _accOk, _accNg;

        readonly string[] _cats = NgCategoryCounter.Keys;

        // 카테고리 표(DataGridView) → Dictionary<string,int>
        private Dictionary<string, int> ReadCategoryCountsFromGrid(DataGridView grid)
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (grid == null) return d;

            foreach (DataGridViewRow r in grid.Rows)
            {
                if (r.IsNewRow) continue;
                var nameObj = (r.Cells.Count > 0) ? r.Cells[0].Value : null;
                var valObj = (r.Cells.Count > 1) ? r.Cells[1].Value : null;

                var name = nameObj?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                int n = 0;
                if (valObj != null) int.TryParse(valObj.ToString(), out n);
                d[name] = n;
            }
            return d;
        }

        // 현재 이미지 폴더 해석(갤러리 파일 경로 기준)
        private string ResolveCurrentImagePath()
        {
            var p = Global.Inst?.InspStage?.CurrentImagePath;
            if (!string.IsNullOrWhiteSpace(p)) return p;

            p = Global.Inst?.InspStage?.CurModel?.InspectImagePath;
            return string.IsNullOrWhiteSpace(p) ? null : p;
        }

        public CountForm()
        {
            Text = "Counter";
            Padding = new Padding(10);

            _view = new MetricsView { Dock = DockStyle.Top, Height = 260 };

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

            Controls.Add(_catGrid);
            Controls.Add(_view);
            Controls.Add(_btnExport);
            Controls.Add(_btnReset);

            _btnReset.Click += (s, e) =>
            {
                Global.Inst.InspStage.ResetAccum();
                Global.Inst.InspStage.ResetCategory();
            };
            _btnExport.Click += (s, e) => ExportPdf();

            Global.Inst.InspStage.AccumChanged += OnAccumChanged;
            Global.Inst.InspStage.CategoryChanged += OnCategoryChanged;

            // 스크롤바로 인한 여백 방지 (필수는 아니지만 권장)
            _catGrid.ScrollBars = ScrollBars.None;

            // 행 높이 자동 맞춤 이벤트 연결
            _catGrid.Resize += (s, e) => FitRowsToFill(_catGrid);
            _catGrid.RowsAdded += (s, e) => FitRowsToFill(_catGrid);
            _catGrid.RowsRemoved += (s, e) => FitRowsToFill(_catGrid);

            // 초기 1회 맞춤
            FitRowsToFill(_catGrid);
        }

        // 남는 세로 공간 없이 행 높이를 균등 분배
        private void FitRowsToFill(DataGridView g)
        {
            if (g == null || g.RowCount == 0) return;

            // 헤더와 테두리를 제외한 실제 셀 영역 높이
            int headerH = g.ColumnHeadersVisible ? g.ColumnHeadersHeight : 0;
            int clientH = g.ClientSize.Height;

            // 수평 스크롤바가 없도록 AutoSizeColumnsMode=Fill을 이미 사용 중이라면 아래는 대부분 0
            int hScroll = (g.HorizontalScrollingOffset > 0) ? SystemInformation.HorizontalScrollBarHeight : 0;

            int available = clientH - headerH - hScroll;
            if (available <= 0) return;

            // 최소/최대 행 높이(가독성 보장)
            const int minRowH = 22;
            const int maxRowH = 80;

            // 균등 분배
            int perRow = Math.Max(minRowH, Math.Min(maxRowH, available / g.RowCount));
            foreach (DataGridViewRow row in g.Rows)
                row.Height = perRow;

            // 혹시 미세한 픽셀 오차가 있으면 마지막 행에 보정치 더해주기
            int used = perRow * g.RowCount;
            int remain = available - used;
            if (remain > 0 && g.RowCount > 0)
                g.Rows[g.RowCount - 1].Height = Math.Min(maxRowH, g.Rows[g.RowCount - 1].Height + remain);
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

            // Page2 KPI에 사용
            _accTotal = (int)c.Total;
            _accOk = (int)c.OK;
            _accNg = (int)c.NG;
        }

        void OnCategoryChanged(IReadOnlyDictionary<string, long> map)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnCategoryChanged(map))); return; }
            for (int i = 0; i < _cats.Length; i++)
            {
                map.TryGetValue(_cats[i], out long v);
                _catGrid.Rows[i].Cells[1].Value = v;
            }
        }

        // =========================================================
        // PDF Export (A4) — Page2(OverallReportPage2)만 생성
        // =========================================================
        private void ExportPdf()
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "PDF File|*.pdf",
                FileName = "InspectionReport.pdf"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var page = PdfPageSize.A4; // 595x842pt
                float lm = 36f, rm = 36f, tm = 36f, bm = 36f; // 0.5”

                using (var fs = new FileStream(sfd.FileName, FileMode.Create))
                {
                    var doc = new PdfDocument(page, lm, rm, tm, bm);
                    PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    // === Page2: 전체 요약 보고서 1장 생성 ===
                    string baseDir = Path.GetDirectoryName(ResolveCurrentImagePath());
                    if (string.IsNullOrWhiteSpace(baseDir)) baseDir = Application.StartupPath;

                    OverallReportPage2.BuildFromFiles(
                        doc,
                        DateTime.Now,
                        _accTotal, _accOk, _accNg,                   // KPI
                        ReadCategoryCountsFromGrid(_catGrid),        // 카테고리 카운트
                        baseDir,
                        "0000.JPG",   // original
                        "020.JPG",    // Chip
                        "008.JPG",    // Lead
                        "033.JPG",    // Resistance
                        "093.JPG",    // Scratch
                        "019.JPG"     // Soldering
                    );

                    doc.Close();
                }
            }
        }
    }
}
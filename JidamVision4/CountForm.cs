using JidamVision4.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

// GDI alias
using GdiRect = System.Drawing.Rectangle;

// iTextSharp alias
using PdfBaseColor = iTextSharp.text.BaseColor;
using PdfBaseFont = iTextSharp.text.pdf.BaseFont;
using PdfChunk = iTextSharp.text.Chunk;
using PdfDocument = iTextSharp.text.Document;
using PdfElement = iTextSharp.text.Element;
using PdfFont = iTextSharp.text.Font;
using PdfImage = iTextSharp.text.Image;
using PdfLineSep = iTextSharp.text.pdf.draw.LineSeparator;
using PdfPageSize = iTextSharp.text.PageSize;
using PdfParagraph = iTextSharp.text.Paragraph;
using PdfPhrase = iTextSharp.text.Phrase;
using PdfPCell = iTextSharp.text.pdf.PdfPCell;
using PdfPTable = iTextSharp.text.pdf.PdfPTable;
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
        int _accTotal, _accOk, _accNg;
        readonly string[] _cats = NgCategoryCounter.Keys;

        // CountForm 클래스 내부에 1개만 유지하세요 (맨 위쪽에 두는 걸 권장)
        private static string SafeFileNameFrom(string path, string fallback = "미지정")
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    var name = System.IO.Path.GetFileName(path.Trim());
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                }
            }
            catch { }
            return fallback;
        }


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

            // === [PAGE2 KPI 저장] ===
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
        // PDF Export (A4, 시안 레이아웃)
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

                    // Fonts
                    var fTitle = GetKFont(22f, PdfFont.BOLD);
                    var fLab = GetKFont(11f, PdfFont.BOLD);
                    var fText = GetKFont(11f, PdfFont.NORMAL);

                    // ===== 헤더 =====
                    doc.Add(new PdfParagraph("반도체 칩 불량 검사 보고서", fTitle) { SpacingAfter = 10f });

                    var info = new PdfPTable(4) { WidthPercentage = 100 };
                    info.SetWidths(new float[] { 12, 38, 12, 38 });

                    AddInfoRow(info, "검사일시:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), fLab, fText);

                    string imgPath = ResolveCurrentImagePath();
                    string fileName = SafeFileNameFrom(imgPath, "미지정");
                    AddInfoRow(info, "파일명:", fileName, fLab, fText);

                    AddInfoRow(info, "불량 유형:", GetDefectSummaryFromGrid(), fLab, fText);

                    doc.Add(info);
                    doc.Add(new PdfChunk(new PdfLineSep(0.7f, 100, PdfBaseColor.BLACK, PdfElement.ALIGN_LEFT, -4)));
                    doc.Add(new PdfParagraph(" ", fText));

                    float usableW = page.Width - lm - rm;

                    // ===== 상단 2열 =====
                    var top = new PdfPTable(2) { WidthPercentage = 100 };
                    top.SetWidths(new float[] { 62, 38 });

                    // 좌측(컬러 + 라벨 + ROI)
                    {
                        float boxW = usableW * 0.62f;
                        float boxH = 240f; // 시안 느낌
                        using (var color = LoadMainBitmapPlain())
                        {
                            Bitmap painted = null;
                            if (color != null)
                            {
                                // ROI 수집
                                var rects = CollectResultRectsFromModel();
                                painted = DrawOverlayAndLabel(color, rects, GetDefectSummaryForImageLabel());
                            }
                            else
                            {
                                painted = MakeCheckerboard((int)boxW, (int)boxH, 12);
                            }

                            var img = ToPdfImageFit(painted, boxW, boxH);
                            img.SpacingAfter = 4f;

                            var lt = new PdfPTable(1) { WidthPercentage = 100 };
                            lt.AddCell(Wrap(img));
                            top.AddCell(Wrap(lt));
                        }
                    }

                    // 우측(Gray up BIG + ROI zoom small)
                    {
                        float colW = usableW * 0.38f;
                        var rt = new PdfPTable(1) { WidthPercentage = 100 };

                        using (var color = LoadMainBitmapPlain())
                        {
                            // Gray (조금 더 크게)
                            {
                                float h = 150f;
                                Bitmap gray;
                                if (color != null)
                                {
                                    using (var gimg = ToGray(color))
                                        gray = DrawOverlayAndLabel(gimg, CollectResultRectsFromModel(), "Gray");
                                }
                                else gray = MakeCheckerboard((int)colW, (int)h, 12);

                                var img = ToPdfImageFit(gray, colW, h);
                                rt.AddCell(Wrap(img));
                            }

                            // ROI 확대 (조금 작게)
                            {
                                float h = 95f;
                                Bitmap zoom;
                                if (color != null)
                                {
                                    var primary = GetPrimaryResultRect();
                                    if (primary.HasValue)
                                        zoom = CropRect(color, Inflate(primary.Value, 8), (int)colW, (int)h);
                                    else
                                        zoom = CropCenter(color, (int)colW, (int)h);
                                }
                                else zoom = MakeCheckerboard((int)colW, (int)h, 12);

                                var img = ToPdfImageFit(zoom, colW, h);
                                img.SpacingBefore = 8f;
                                rt.AddCell(Wrap(img));
                            }
                        }

                        top.AddCell(Wrap(rt));
                    }

                    doc.Add(top);

                    // ===== 하단 2열 =====
                    var bottom = new PdfPTable(2) { WidthPercentage = 100, SpacingBefore = 14f };
                    bottom.SetWidths(new float[] { 62, 38 });

                    // 좌측: Result 분석
                    {
                        var left = new PdfPTable(1) { WidthPercentage = 100 };
                        left.AddCell(Wrap(new PdfParagraph("Result 분석", fLab) { SpacingAfter = 6f }));

                        var rf = MainForm.GetDockForm<ResultForm>();
                        var table = BuildResultPdfTableFromResultForm(rf, 6);
                        left.AddCell(Wrap(table));

                        var footer = ReadFooterFromResultForm(rf);
                        if (!string.IsNullOrWhiteSpace(footer))
                            left.AddCell(Wrap(new PdfParagraph(footer, fText) { SpacingBefore = 4f }));

                        bottom.AddCell(Wrap(left));
                    }

                    // 우측: 전체 Anomaly + 카테고리
                    {
                        var right = new PdfPTable(1) { WidthPercentage = 100 };

                        using (var mvBmp = RenderControl(_view, new Size((int)(usableW * 0.38f), 160)))
                        {
                            var mvImg = ToPdfImageFit(mvBmp, usableW * 0.38f, 160f);
                            right.AddCell(Wrap(new PdfParagraph("전체 Anomaly 분석", fLab) { SpacingAfter = 4f }));
                            right.AddCell(Wrap(mvImg));
                        }

                        var catTbl = BuildCategoryTableFromGrid(_catGrid, fLab, fText);
                        catTbl.SpacingBefore = 6f;
                        right.AddCell(Wrap(catTbl));

                        bottom.AddCell(Wrap(right));
                    }

                    // === [PAGE2: 전체 요약 페이지 추가] ===
                    doc.NewPage();

                    string baseDir = Path.GetDirectoryName(ResolveCurrentImagePath());
                    if (string.IsNullOrWhiteSpace(baseDir)) baseDir = Application.StartupPath;

                    OverallReportPage2.BuildFromFiles(
     doc,
     DateTime.Now,
     (int)(Global.Inst?.InspStage?.Accum?.Total ?? 0L), // ← long → int
     (int)(Global.Inst?.InspStage?.Accum?.OK ?? 0L), // ← long → int
     (int)(Global.Inst?.InspStage?.Accum?.NG ?? 0L), // ← long → int
     ReadCategoryCountsFromGrid(_catGrid),
     baseDir,
     "0000.JPG",   // original
     "020.JPG",    // Chip
     "008.JPG",    // Lead
     "033.JPG",    // Resistance  (null → 파일명 교체)
     "093.JPG",    // Scratch
     "019.JPG"     // Soldering
 );

                    // 바로 이어서
                    doc.Close();
                }
            }
        }



        // =========================================================
        // ===== 이미지/ROI/라벨 유틸 =====
        // =========================================================

        private Bitmap LoadMainBitmapPlain()
        {
            try
            {
                var path = ResolveCurrentImagePath();
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return (Bitmap)Bitmap.FromFile(path);
            }
            catch { }
            return null;
        }

        // 모델에서 ResultRectList 수집 (Rectangle 목록)
        private List<GdiRect> CollectResultRectsFromModel()
        {
            var rects = new List<GdiRect>();
            try
            {
                var model = Global.Inst?.InspStage?.CurModel;
                if (model?.InspWindowList == null) return rects;

                foreach (var w in model.InspWindowList)
                {
                    var listProp = w?.GetType().GetProperty("InspResultList");
                    var rlist = listProp?.GetValue(w) as System.Collections.IEnumerable;
                    if (rlist == null) continue;

                    foreach (var r in rlist)
                    {
                        var rrProp = r?.GetType().GetProperty("ResultRectList");
                        var arr = rrProp?.GetValue(r) as System.Collections.IEnumerable;
                        if (arr == null) continue;

                        foreach (var di in arr)
                            if (TryGetRectFromDrawInspectInfo(di, out var rc) && rc.Width > 1 && rc.Height > 1)
                                rects.Add(rc);
                    }
                }
            }
            catch { }
            return rects;
        }

        // 가장 큰 결과 사각형
        private GdiRect? GetPrimaryResultRect()
        {
            var rects = CollectResultRectsFromModel();
            if (rects.Count == 0) return null;
            GdiRect best = rects[0];
            int bestArea = best.Width * best.Height;
            for (int i = 1; i < rects.Count; i++)
            {
                int a = rects[i].Width * rects[i].Height;
                if (a > bestArea) { best = rects[i]; bestArea = a; }
            }
            return best;
        }

        // Draw ROI + label (큰 글씨)
        private Bitmap DrawOverlayAndLabel(Bitmap src, List<GdiRect> rects, string label)
        {
            var bmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                g.DrawImage(src, 0, 0, src.Width, src.Height);

                // ROI
                using (var pen = new Pen(Color.Yellow, Math.Max(2f, src.Width * 0.003f)))
                {
                    foreach (var r in rects)
                        g.DrawRectangle(pen, r);
                }

                // Label
                if (!string.IsNullOrWhiteSpace(label))
                {
                    float fontPx = Math.Max(18f, src.Width * 0.04f); // 4% 폭
                    using (var f = new Font("Malgun Gothic", fontPx, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        var size = g.MeasureString(label, f);
                        int pad = (int)Math.Max(6, fontPx * 0.35f);
                        var rect = new Rectangle(pad, pad, (int)(size.Width + pad * 2), (int)(size.Height + pad));
                        using (var bg = new SolidBrush(Color.FromArgb(28, 28, 28)))
                            g.FillRectangle(bg, rect);
                        using (var br = new SolidBrush(Color.White))
                            g.DrawString(label, f, br, new PointF(pad + 2, pad + 1));
                    }
                }
            }
            return bmp;
        }

        // DrawInspectInfo → Rectangle 추출(리플렉션 보호)
        private static bool TryGetRectFromDrawInspectInfo(object di, out GdiRect rc)
        {
            rc = GdiRect.Empty;
            if (di == null) return false;
            var t = di.GetType();

            foreach (var name in new[] { "Rect", "Rectangle", "Bounds", "WindowArea", "Roi", "Area" })
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null) continue;
                var val = p.GetValue(di);
                if (val is Rectangle r1) { rc = r1; return true; }
                if (val is System.Drawing.RectangleF rf) { rc = new GdiRect((int)rf.X, (int)rf.Y, (int)rf.Width, (int)rf.Height); return true; }
            }
            try
            {
                int x = Convert.ToInt32(t.GetProperty("X")?.GetValue(di) ?? 0);
                int y = Convert.ToInt32(t.GetProperty("Y")?.GetValue(di) ?? 0);
                int w = Convert.ToInt32(t.GetProperty("Width")?.GetValue(di) ?? 0);
                int h = Convert.ToInt32(t.GetProperty("Height")?.GetValue(di) ?? 0);
                if (w > 0 && h > 0) { rc = new GdiRect(x, y, w, h); return true; }
            }
            catch { }
            return false;
        }

        private static PdfImage ToPdfImageFit(Bitmap bmp, float boxWpt, float boxHpt)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var img = PdfImage.GetInstance(ms.ToArray());
                img.ScaleToFit(boxWpt, boxHpt);      // 비율 유지
                img.Alignment = PdfElement.ALIGN_CENTER;
                return img;
            }
        }

        private static Bitmap ToGray(Bitmap src)
        {
            var dst = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(dst))
            {
                var cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[]{0.299f,0.299f,0.299f,0,0},
                    new float[]{0.587f,0.587f,0.587f,0,0},
                    new float[]{0.114f,0.114f,0.114f,0,0},
                    new float[]{0,0,0,1,0},
                    new float[]{0,0,0,0,1}
                });
                var ia = new System.Drawing.Imaging.ImageAttributes();
                ia.SetColorMatrix(cm);
                g.DrawImage(src, new GdiRect(0, 0, dst.Width, dst.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
            }
            return dst;
        }

        private static Bitmap MakeCheckerboard(int w, int h, int cell = 12)
        {
            var bmp = new Bitmap(Math.Max(10, w), Math.Max(10, h));
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                for (int y = 0; y < h; y += cell)
                    for (int x = 0; x < w; x += cell)
                    {
                        bool on = ((x / cell) + (y / cell)) % 2 == 0;
                        using (var br = new SolidBrush(on ? Color.Gainsboro : Color.WhiteSmoke))
                            g.FillRectangle(br, x, y, cell, cell);
                    }
                g.DrawRectangle(Pens.Silver, 0, 0, w - 1, h - 1);
            }
            return bmp;
        }

        private static Bitmap CropCenter(Bitmap src, int outW, int outH)
        {
            var r = new GdiRect(src.Width / 4, src.Height / 4, src.Width / 2, src.Height / 2);
            return CropRect(src, r, outW, outH);
        }

        private static Bitmap CropRect(Bitmap src, GdiRect r, int outW, int outH)
        {
            var bmp = new Bitmap(outW, outH, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.DrawImage(src, new GdiRect(0, 0, outW, outH), r, GraphicsUnit.Pixel);
                g.DrawRectangle(new Pen(Color.DimGray, 1f), 0, 0, outW - 1, outH - 1);
            }
            return bmp;
        }

        private static GdiRect Inflate(GdiRect r, int p)
        {
            return new GdiRect(Math.Max(0, r.X - p), Math.Max(0, r.Y - p), r.Width + p * 2, r.Height + p * 2);
        }

        private static Bitmap RenderControl(Control ctrl, Size targetPx)
        {
            int w = Math.Max(10, targetPx.Width);
            int h = Math.Max(10, targetPx.Height);

            var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                using (var tmp = new Bitmap(Math.Max(1, ctrl.Width), Math.Max(1, ctrl.Height)))
                {
                    ctrl.DrawToBitmap(tmp, new GdiRect(0, 0, tmp.Width, tmp.Height));
                    g.DrawImage(tmp, new GdiRect(0, 0, w, h));
                }
            }
            return bmp;
        }

        // =========================================================
        // Result 표 생성
        // =========================================================
        private PdfPTable BuildResultPdfTableFromResultForm(ResultForm rf, int maxRows)
        {
            var table = new PdfPTable(4) { WidthPercentage = 100f, SpacingAfter = 6f };
            table.SetWidths(new float[] { 24f, 28f, 16f, 32f });

            PdfFont fHead = GetKFont(11f, PdfFont.BOLD);
            PdfFont fCell = GetKFont(10f, PdfFont.NORMAL);

            AddCell(table, "UID", fHead, true);
            AddCell(table, "Algorithm", fHead, true);
            AddCell(table, "Status", fHead, true);
            AddCell(table, "Result", fHead, true);

            var rows = ReadResultRowsFromResultForm(rf, maxRows);

            foreach (var r in rows)
            {
                AddCell(table, r.Length > 0 ? r[0] : "", fCell, false);
                AddCell(table, r.Length > 1 ? r[1] : "", fCell, false);
                AddCell(table, r.Length > 2 ? r[2] : "", fCell, false);
                AddCell(table, r.Length > 3 ? r[3] : "", fCell, false);
            }

            int needEmpty = Math.Max(0, maxRows - rows.Count);
            for (int i = 0; i < needEmpty; i++)
            {
                AddCell(table, "", fCell, false);
                AddCell(table, "", fCell, false);
                AddCell(table, "", fCell, false);
                AddCell(table, "", fCell, false);
            }

            return table;
        }

        private List<string[]> ReadResultRowsFromResultForm(ResultForm rf, int maxRows)
        {
            var rows = new List<string[]>();
            if (rf == null) return rows;

            var mi = rf.GetType().GetMethod("ExportRowsForPdf", BindingFlags.Instance | BindingFlags.Public);
            if (mi != null)
            {
                try
                {
                    var ret = mi.Invoke(rf, new object[] { maxRows }) as List<string[]>;
                    if (ret != null) return ret;
                }
                catch { }
            }

            // 폴백: TreeListView 내부 유추
            try
            {
                var fi = rf.GetType().GetField("_treeListView", BindingFlags.Instance | BindingFlags.NonPublic);
                var tlv = fi?.GetValue(rf) as Control;
                if (tlv == null) return rows;

                var propObjects = tlv.GetType().GetProperty("Objects");
                var objEnum = propObjects?.GetValue(tlv) as System.Collections.IEnumerable;
                if (objEnum == null) return rows;

                foreach (var win in objEnum)
                {
                    if (win == null) continue;
                    var pUid = win.GetType().GetProperty("UID");
                    string uid = pUid != null ? (pUid.GetValue(win) ?? "").ToString() : "";

                    var pList = win.GetType().GetProperty("InspResultList");
                    var list = pList?.GetValue(win) as System.Collections.IEnumerable;
                    if (list == null) continue;

                    foreach (var r in list)
                    {
                        string alg = "";
                        var pInspType = r.GetType().GetProperty("InspType");
                        if (pInspType != null) alg = pInspType.GetValue(r)?.ToString() ?? "";

                        bool isDefect = false;
                        var pIsDefect = r.GetType().GetProperty("IsDefect");
                        if (pIsDefect != null)
                        {
                            var v = pIsDefect.GetValue(r);
                            if (v is bool b) isDefect = b;
                        }
                        string stat = isDefect ? "NG" : "OK";

                        string res = "";
                        var pRes = r.GetType().GetProperty("ResultValue");
                        if (pRes != null) res = pRes.GetValue(r)?.ToString() ?? "";

                        rows.Add(new[] { uid, alg, stat, res });
                        if (rows.Count >= maxRows) return rows;
                    }
                }
            }
            catch { }

            return rows;
        }

        private string ReadFooterFromResultForm(ResultForm rf)
        {
            if (rf == null) return "";
            var mi = rf.GetType().GetMethod("ExportFooterLineForPdf", BindingFlags.Instance | BindingFlags.Public);
            if (mi != null)
            {
                try { return mi.Invoke(rf, null)?.ToString() ?? ""; }
                catch { }
            }

            try
            {
                var fi = rf.GetType().GetField("_txtDetails", BindingFlags.Instance | BindingFlags.NonPublic);
                var tb = fi?.GetValue(rf) as TextBox;
                var s = tb?.Text ?? "";
                int nl = s.IndexOf('\n');
                if (nl >= 0) s = s.Substring(0, nl);
                return s.Trim();
            }
            catch { return ""; }
        }

        // =========================================================
        // 카테고리 표
        // =========================================================
        private PdfPTable BuildCategoryTableFromGrid(DataGridView grid, PdfFont fHead, PdfFont fText)
        {
            var tbl = new PdfPTable(2) { WidthPercentage = 100f };
            tbl.SetWidths(new float[] { 60f, 40f });

            AddCell(tbl, "Category", fHead, true);
            AddCell(tbl, "NG", fHead, true);

            if (grid != null)
            {
                foreach (DataGridViewRow r in grid.Rows)
                {
                    if (r.IsNewRow) continue;
                    string name = (r.Cells.Count > 0 && r.Cells[0].Value != null) ? r.Cells[0].Value.ToString() : "";
                    string val = (r.Cells.Count > 1 && r.Cells[1].Value != null) ? r.Cells[1].Value.ToString() : "0";
                    AddCell(tbl, name, fText, false);
                    AddCell(tbl, val, fText, false);
                }
            }
            return tbl;
        }

        // =========================================================
        // 공통 유틸
        // =========================================================
        private PdfFont GetKFont(float size, int style)
        {
            try
            {
                string malgun = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "malgun.ttf");
                if (File.Exists(malgun))
                {
                    var bf = PdfBaseFont.CreateFont(malgun, PdfBaseFont.IDENTITY_H, PdfBaseFont.EMBEDDED);
                    return new PdfFont(bf, size, style, PdfBaseColor.BLACK);
                }
            }
            catch { }
            return new PdfFont(PdfFont.FontFamily.HELVETICA, size, style, PdfBaseColor.BLACK);
        }

        private void AddInfoRow(PdfPTable tbl, string label, string value, PdfFont fLabel, PdfFont fText)
        {
            var c1 = new PdfPCell(new PdfPhrase(label, fLabel)) { Border = PdfPCell.NO_BORDER, PaddingBottom = 2f };
            var c2 = new PdfPCell(new PdfPhrase(string.IsNullOrEmpty(value) ? "-" : value, fText)) { Border = PdfPCell.NO_BORDER, PaddingBottom = 2f };
            var c3 = new PdfPCell(new PdfPhrase(" ", fLabel)) { Border = PdfPCell.NO_BORDER, PaddingBottom = 2f };
            var c4 = new PdfPCell(new PdfPhrase(" ", fText)) { Border = PdfPCell.NO_BORDER, PaddingBottom = 2f };
            tbl.AddCell(c1); tbl.AddCell(c2); tbl.AddCell(c3); tbl.AddCell(c4);
        }

        private static void AddCell(PdfPTable t, string text, PdfFont f, bool header)
        {
            var c = new PdfPCell(new PdfPhrase(text ?? "", f));
            c.Padding = header ? 4f : 3f;
            c.HorizontalAlignment = PdfElement.ALIGN_LEFT;
            c.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
            if (header) c.BackgroundColor = new PdfBaseColor(245, 245, 245);
            t.AddCell(c);
        }

        private static PdfPCell Wrap(PdfImage img) => new PdfPCell(img) { Border = PdfPCell.NO_BORDER, Padding = 0 };
        private static PdfPCell Wrap(PdfPTable inner) => new PdfPCell(inner) { Border = PdfPCell.NO_BORDER, Padding = 0 };
        private static PdfPCell Wrap(PdfParagraph p) { var cell = new PdfPCell(); cell.AddElement(p); cell.Border = PdfPCell.NO_BORDER; cell.Padding = 0; return cell; }

        // 이미지 라벨용 텍스트 (정상이면 Normal)
        private string GetDefectSummaryFromGridLabel()
        {
            var s = GetDefectSummaryFromGrid();
            return string.IsNullOrWhiteSpace(s) || s == "-" ? "Normal" : s;
        }

        private string GetDefectSummaryFromGrid()
        {
            try
            {
                var names = new List<string>();
                foreach (DataGridViewRow row in _catGrid.Rows)
                {
                    if (row.Cells[0].Value == null || row.Cells[1].Value == null) continue;
                    if (long.TryParse(row.Cells[1].Value.ToString(), out long v) && v > 0)
                        names.Add(row.Cells[0].Value.ToString());
                }
                return names.Any() ? string.Join(", ", names) : "-";
            }
            catch { return "-"; }
        }

        // 좌측 컬러 이미지 라벨(불량유형) - 헤더 “불량 유형:”과 동일 로직
        private string GetDefectSummaryForImageLabel() => GetDefectSummaryFromGridLabel();

        // === [PAGE2 컨텍스트 만들기] ===
        private JidamVision4.Reports.OverallReportContext BuildOverallContextForPage2()
        {
            var ctx = new JidamVision4.Reports.OverallReportContext();
            ctx.InspectDate = DateTime.Now;

            // KPI
            ctx.Total = _accTotal;
            ctx.Ok = _accOk;
            ctx.Ng = _accNg;

            // 카테고리 NG 카운트 (그리드에서 읽기)
            ctx.DefectCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow r in _catGrid.Rows)
            {
                if (r.IsNewRow) continue;
                var name = (r.Cells.Count > 0 && r.Cells[0].Value != null) ? r.Cells[0].Value.ToString() : "";
                var valS = (r.Cells.Count > 1 && r.Cells[1].Value != null) ? r.Cells[1].Value.ToString() : "0";
                int v; if (!int.TryParse(valS, out v)) v = 0;
                if (!string.IsNullOrWhiteSpace(name)) ctx.DefectCounts[name] = v;
            }

            // 막대그래프/갤러리는 비워두면 내부에서 자동 처리(차트 생성, 슬롯 Placeholder)
            // 필요 시:
            // ctx.ModelName = Global.Inst?.InspStage?.CurModel?.Name;
            // ctx.LineId = "LINE-2"; ctx.EquipmentId = "CAM-01"; ctx.SoftwareVersion = "v4.8";

            return ctx;
        }

    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

// iTextSharp alias
using PdfBaseColor = iTextSharp.text.BaseColor;
using PdfBaseFont = iTextSharp.text.pdf.BaseFont;
using PdfDocument = iTextSharp.text.Document;
using PdfElement = iTextSharp.text.Element;
using PdfFont = iTextSharp.text.Font;
using PdfImage = iTextSharp.text.Image;
using PdfParagraph = iTextSharp.text.Paragraph;
using PdfPhrase = iTextSharp.text.Phrase;
using PdfLineSep = iTextSharp.text.pdf.draw.LineSeparator;
using PdfPCell = iTextSharp.text.pdf.PdfPCell;
using PdfPTable = iTextSharp.text.pdf.PdfPTable;

namespace JidamVision4.Reports
{
    public sealed class OverallReportContext
    {
        public DateTime InspectDate = DateTime.Now;
        public string ModelName;
        public string LineId;
        public string EquipmentId;
        public string SoftwareVersion;

        public int Total;
        public int Ok;
        public int Ng;

        public Dictionary<string, int> DefectCounts = new Dictionary<string, int>();
        public Bitmap BarChartBitmap; // null이면 내부 생성
        public Dictionary<string, Bitmap> GalleryImages = new Dictionary<string, Bitmap>();
    }

    public static class OverallReportPage2
    {
        // src를 "cover" 방식으로 타일 박스에 맞춰 크롭+스케일
        static Bitmap RenderCoverBitmap(Bitmap src, int outWpx, int outHpx)
        {
            if (src == null) return null;
            outWpx = Math.Max(10, outWpx);
            outHpx = Math.Max(10, outHpx);

            double sx = outWpx / (double)src.Width;
            double sy = outHpx / (double)src.Height;
            double s = Math.Max(sx, sy);

            int cropW = (int)Math.Round(outWpx / s);
            int cropH = (int)Math.Round(outHpx / s);
            int cx = src.Width / 2, cy = src.Height / 2;

            var crop = new Rectangle(
                Math.Max(0, cx - cropW / 2),
                Math.Max(0, cy - cropH / 2),
                Math.Min(cropW, src.Width),
                Math.Min(cropH, src.Height));

            var dst = new Bitmap(outWpx, outHpx, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(dst))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, new Rectangle(0, 0, outWpx, outHpx), crop, GraphicsUnit.Pixel);
            }
            return dst;
        }

        public static void Build(PdfDocument doc, OverallReportContext ctx)
        {
            AddHeader(doc, ctx);

            // ── 좌/우 타이틀을 같은 라인에 고정 ──
            PdfFont h = F(12, PdfFont.BOLD);
            var titleRow = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 6f };
            titleRow.SetWidths(new float[] { 46f, 54f });
            titleRow.AddCell(new PdfPCell(new PdfPhrase("검사 통계", h)) { Border = PdfPCell.NO_BORDER, Padding = 0f });
            titleRow.AddCell(new PdfPCell(new PdfPhrase("불량 유형별 분포 (NG 기준)", h))
            {
                Border = PdfPCell.NO_BORDER,
                // 차트 블록과 동일한 좌측 여백으로 정렬
                PaddingTop = 0f,
                PaddingBottom = 0f,
                PaddingRight = 0f,
                PaddingLeft = 10f
            });

            doc.Add(titleRow);

            // ── 상단 2열 본문 ──
            var twoCols = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 6f };
            twoCols.SetWidths(new float[] { 46f, 54f });

            // (좌) KPI + 표
            var left = new PdfPTable(1) { WidthPercentage = 100 };
            left.AddCell(KpiRow("Total", ctx.Total));
            left.AddCell(KpiRow("OK", ctx.Ok));
            left.AddCell(KpiRow("NG", ctx.Ng));
            left.AddCell(Spacer(10f)); // KPI와 표 섹션 간격

            left.AddCell(SectionTitle("불량 유형별 비율", bottom: 4f, top: 0f));
            left.AddCell(DefectTable(ctx.DefectCounts));

            twoCols.AddCell(new PdfPCell(left)
            {
                Border = PdfPCell.NO_BORDER,
                PaddingRight = 10f
            });

            // (우) 차트 (제목/밑줄 없음)
            var right = new PdfPTable(1) { WidthPercentage = 100 };

            float usableWpt = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin;
            float rightColWpt = usableWpt * 0.54f - 12f;
            float chartHeightPt = 240f;

            int chartWpx = (int)Math.Round(rightColWpt / 72f * 300f);
            int chartHpx = (int)Math.Round(chartHeightPt / 72f * 300f);

            Bitmap chartBmp = ctx.BarChartBitmap ?? RenderBarChart(ctx.DefectCounts, chartWpx, chartHpx);
            PdfImage chartImg = ToPdfImageExact(chartBmp, rightColWpt, chartHeightPt, 300);

            var chartBlock = new PdfPTable(1) { WidthPercentage = 100 };
            chartBlock.AddCell(new PdfPCell(chartImg)
            {
                Border = PdfPCell.NO_BORDER,
                Padding = 6f,
                HorizontalAlignment = PdfElement.ALIGN_LEFT
            });
            right.AddCell(new PdfPCell(chartBlock) { Border = PdfPCell.NO_BORDER, PaddingLeft = 10f, PaddingRight = 6f, PaddingBottom = 0f });

            var note = new PdfParagraph(
                "• 내림차순 가로 막대 그래프입니다.\n• 값 옆 괄호는 각 유형의 비율입니다.\n• 총 NG 기준 산출.",
                F(9));
            var noteCell = new PdfPCell { Border = PdfPCell.NO_BORDER, PaddingTop = 4f, PaddingLeft = 2f, PaddingBottom = 0f };
            noteCell.AddElement(note);
            right.AddCell(noteCell);

            twoCols.AddCell(new PdfPCell(right) { Border = PdfPCell.NO_BORDER, PaddingLeft = 8f });
            doc.Add(twoCols);

            // ── 하단 3×2 슬롯(cover 크롭, 한 페이지 유지) ──
            float pageUsableW = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin;
            float tileWpt = (pageUsableW / 3f) - 8f;
            float tileHpt = 130f;

            var grid = new PdfPTable(3) { WidthPercentage = 100, SpacingBefore = 12f, KeepTogether = true };
            grid.SetWidths(new float[] { 1f, 1f, 1f });

            string[] labels = { "original", "Chip", "Lead", "Resistance", "Scratch", "Soldering" };
            for (int i = 0; i < labels.Length; i++)
            {
                ctx.GalleryImages.TryGetValue(labels[i], out Bitmap bmp);
                grid.AddCell(ImageTile(labels[i], bmp, tileWpt, tileHpt));
            }
            doc.Add(grid);
        }

        // ========== 파일명만으로 Page2 만들기 ==========
        private static Bitmap TryLoadBmpLocal(string path)
        {
            try { return (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) ? (Bitmap)Bitmap.FromFile(path) : null; }
            catch { return null; }
        }

        private static string ResolvePath(string baseDir, string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return null;
            return Path.IsPathRooted(p) ? p : Path.Combine(baseDir ?? "", p);
        }

        public static void SetGalleryFromFiles(
            OverallReportContext ctx, string baseDir,
            string original = null, string chip = null, string lead = null,
            string resistance = null, string scratch = null, string soldering = null)
        {
            if (ctx.GalleryImages == null)
                ctx.GalleryImages = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = ResolvePath(baseDir, original),
                ["Chip"] = ResolvePath(baseDir, chip),
                ["Lead"] = ResolvePath(baseDir, lead),
                ["Resistance"] = ResolvePath(baseDir, resistance),
                ["Scratch"] = ResolvePath(baseDir, scratch),
                ["Soldering"] = ResolvePath(baseDir, soldering)
            };

            foreach (var kv in map)
                ctx.GalleryImages[kv.Key] = TryLoadBmpLocal(kv.Value);
        }

        public static void BuildFromFiles(
            PdfDocument doc,
            DateTime inspectDate, int total, int ok, int ng,
            Dictionary<string, int> counts,
            string baseDir,
            string original, string chip, string lead, string resistance, string scratch, string soldering,
            Bitmap chartBmp = null)
        {
            var ctx = new OverallReportContext
            {
                InspectDate = inspectDate,
                Total = total,
                Ok = ok,
                Ng = ng,
                DefectCounts = counts ?? new Dictionary<string, int>(),
                BarChartBitmap = chartBmp
            };

            SetGalleryFromFiles(ctx, baseDir, original, chip, lead, resistance, scratch, soldering);
            Build(doc, ctx);
        }

        // ---------- Header ----------
        static void AddHeader(PdfDocument doc, OverallReportContext ctx)
        {
            doc.Add(new PdfParagraph("반도체 칩 불량 검사 보고서", F(22, PdfFont.BOLD)) { SpacingAfter = 10f });

            string meta = "검사일시: " + ctx.InspectDate.ToString("yyyy-MM-dd");
            var extras = new List<string>();
            if (!string.IsNullOrWhiteSpace(ctx.ModelName)) extras.Add("모델: " + ctx.ModelName);
            if (!string.IsNullOrWhiteSpace(ctx.LineId)) extras.Add("라인: " + ctx.LineId);
            if (!string.IsNullOrWhiteSpace(ctx.EquipmentId)) extras.Add("장비: " + ctx.EquipmentId);
            if (!string.IsNullOrWhiteSpace(ctx.SoftwareVersion)) extras.Add("SW: " + ctx.SoftwareVersion);
            if (extras.Count > 0) meta += "   " + string.Join("   ", extras.ToArray());

            doc.Add(new PdfParagraph(meta, F(11)));
            doc.Add(new iTextSharp.text.Chunk(new PdfLineSep(0.7f, 100f, PdfBaseColor.BLACK, PdfElement.ALIGN_LEFT, -4f)));
            doc.Add(new PdfParagraph(" ", F(9)));
        }

        static PdfPCell SectionTitle(string text, float bottom = 4f, float top = 0f)
        {
            return new PdfPCell(new PdfPhrase(text, F(12, PdfFont.BOLD)))
            {
                Border = PdfPCell.NO_BORDER,
                PaddingTop = top,
                PaddingBottom = bottom
            };
        }

        static PdfPCell Spacer(float h) => new PdfPCell() { Border = PdfPCell.NO_BORDER, FixedHeight = h, Padding = 0f };

        static PdfPCell KpiRow(string title, int value)
        {
            var inner = new PdfPTable(2) { WidthPercentage = 100 };
            inner.SetWidths(new float[] { 1f, 0.6f });

            inner.AddCell(new PdfPCell(new PdfPhrase(title, F(12, PdfFont.BOLD))) { Border = PdfPCell.NO_BORDER, Padding = 6f });
            inner.AddCell(new PdfPCell(new PdfPhrase(value.ToString(), F(18, PdfFont.BOLD))) { Border = PdfPCell.NO_BORDER, Padding = 6f, HorizontalAlignment = PdfElement.ALIGN_RIGHT });

            return new PdfPCell(inner)
            {
                BackgroundColor = new PdfBaseColor(245, 248, 255),
                BorderColor = new PdfBaseColor(40, 80, 200),
                BorderWidth = 1.2f,
                PaddingTop = 3f,
                PaddingLeft = 2f,
                PaddingRight = 2f,
                PaddingBottom = 8f,
                MinimumHeight = 30f
            };
        }

        static PdfPCell DefectTable(Dictionary<string, int> counts)
        {
            var tbl = new PdfPTable(2) { WidthPercentage = 100 };
            tbl.SetWidths(new float[] { 60f, 40f });

            tbl.AddCell(HeaderCell("Category"));
            tbl.AddCell(HeaderCell("NG"));

            string[] order = new string[] { "Chip", "Lead", "Resistance", "Scratch", "Soldering" };
            for (int i = 0; i < order.Length; i++)
            {
                int v = 0;
                if (counts != null && counts.ContainsKey(order[i])) v = counts[order[i]];
                tbl.AddCell(BodyCell(order[i]));
                tbl.AddCell(BodyCell(v.ToString(), PdfElement.ALIGN_RIGHT));
            }
            return new PdfPCell(tbl) { Border = PdfPCell.NO_BORDER, PaddingTop = 6f, PaddingBottom = 0f };
        }

        static PdfPCell HeaderCell(string text)
            => new PdfPCell(new PdfPhrase(text, F(11, PdfFont.BOLD))) { BackgroundColor = new PdfBaseColor(245, 245, 245), Padding = 6f };

        static PdfPCell BodyCell(string text, int align = PdfElement.ALIGN_LEFT)
        {
            var c = new PdfPCell(new PdfPhrase(text, F(10)));
            c.Padding = 6f;
            c.HorizontalAlignment = align;
            return c;
        }

        static PdfImage ToPdfImageExact(Bitmap bmp, float widthPt, float heightPt, int dpi)
        {
            using (var ms = new MemoryStream())
            {
                bmp.SetResolution(dpi, dpi);
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var img = PdfImage.GetInstance(ms.ToArray());
                img.SetDpi(dpi, dpi);
                img.ScaleAbsolute(widthPt, heightPt);
                img.Alignment = PdfElement.ALIGN_LEFT;
                return img;
            }
        }

        static PdfPCell ImageTile(string label, Bitmap bmp, float boxWpt, float boxHpt)
        {
            var panel = new PdfPTable(1) { WidthPercentage = 100 };
            panel.AddCell(new PdfPCell(new PdfPhrase(label, F(11, PdfFont.BOLD))) { Border = PdfPCell.NO_BORDER, PaddingLeft = 6f, PaddingTop = 2f, PaddingBottom = 2f });

            PdfPCell imgCell;
            if (bmp != null)
            {
                int wpx = (int)Math.Round(boxWpt / 72f * 300f);
                int hpx = (int)Math.Round(boxHpt / 72f * 300f);
                using (var cov = RenderCoverBitmap(bmp, wpx, hpx))
                {
                    var it = ToPdfImageExact(cov, boxWpt, boxHpt, 300);
                    imgCell = new PdfPCell(it) { Border = PdfPCell.NO_BORDER, Padding = 0f, HorizontalAlignment = PdfElement.ALIGN_CENTER };
                }
            }
            else
            {
                imgCell = new PdfPCell(new PdfPhrase("이미지 없음", F(10, PdfFont.ITALIC, new PdfBaseColor(120, 120, 120))))
                {
                    FixedHeight = boxHpt,
                    Border = PdfPCell.NO_BORDER,
                    HorizontalAlignment = PdfElement.ALIGN_CENTER,
                    VerticalAlignment = PdfElement.ALIGN_MIDDLE
                };
            }

            panel.AddCell(imgCell);
            return new PdfPCell(panel) { Border = PdfPCell.NO_BORDER, Padding = 4f };
            //
        }

        static PdfPCell ImageTile(string label, Bitmap bmp) => ImageTile(label, bmp, 160f, 130f);

        // ---------- Font helpers ----------
        static PdfFont F(float size, int style = PdfFont.NORMAL)
        {
            try
            {
                string malgun = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "malgun.ttf");
                var bf = PdfBaseFont.CreateFont(malgun, PdfBaseFont.IDENTITY_H, PdfBaseFont.EMBEDDED);
                return new PdfFont(bf, size, style, PdfBaseColor.BLACK);
            }
            catch
            {
                return new PdfFont(PdfFont.FontFamily.HELVETICA, size, style, PdfBaseColor.BLACK);
            }
        }

        static PdfFont F(float size, int style, PdfBaseColor color)
        {
            try
            {
                string malgun = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "malgun.ttf");
                var bf = PdfBaseFont.CreateFont(malgun, PdfBaseFont.IDENTITY_H, PdfBaseFont.EMBEDDED);
                return new PdfFont(bf, size, style, color ?? PdfBaseColor.BLACK);
            }
            catch
            {
                return new PdfFont(PdfFont.FontFamily.HELVETICA, size, style, color ?? PdfBaseColor.BLACK);
            }
        }

        // ---------- Bar chart (GDI+) ----------
        static Bitmap RenderBarChart(Dictionary<string, int> counts, int width, int height)
        {
            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            bmp.SetResolution(300f, 300f); // 300DPI에서 그리기(폰트 실제 pt 크기 유지)

            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.White);

                var items = (counts ?? new Dictionary<string, int>()).OrderByDescending(kv => kv.Value).ToList();
                int n = Math.Max(1, items.Count);
                int ngSum = items.Sum(it => it.Value);
                int vmax = Math.Max(1, items.Count > 0 ? items.Max(it => it.Value) : 1);

                // ✔ 글씨 크기: 살짝 줄임 (잘림 방지)
                const float LabelPt = 11f;   // 카테고리 라벨(그대로)
                const float ValuePt = 7f;   // ▶ 막대 안 숫자/퍼센트 살짝 축소

                using (var fLab = new Font("Malgun Gothic", LabelPt, FontStyle.Regular, GraphicsUnit.Point))
                using (var fVal = new Font("Malgun Gothic", ValuePt, FontStyle.Bold, GraphicsUnit.Point))
                using (var bar = new SolidBrush(Color.FromArgb(54, 97, 214)))
                using (var grid = new Pen(Color.Gainsboro, 1f))
                {
                    // ✔ 라벨 최대 폭을 먼저 측정해서 좌측 여백을 동적으로 확보(잘림 방지 포인트)
                    float maxLabW = 0f;
                    foreach (var it in items)
                    {
                        var lab = it.Key ?? "";
                        var sz = g.MeasureString(lab, fLab);
                        if (sz.Width > maxLabW) maxLabW = sz.Width;
                    }

                    int padL = Math.Max(130, (int)Math.Ceiling(maxLabW) + 26); // 라벨 폭 + 여유 26px (최소 130)
                    int padR = 30, padT = 18, padB = 28;

                    int plotW = width - padL - padR;
                    int plotH = height - padT - padB;
                    int rowH = Math.Max(1, plotH / n);

                    for (int i = 0; i < n; i++)
                    {
                        var it = items[i];
                        int y = padT + i * rowH + rowH / 6;
                        int hBar = (int)(rowH * 0.72);
                        int wBar = (int)(plotW * (it.Value / (double)vmax));

                        // 가이드 라인
                        g.DrawLine(grid, padL, y + hBar + 6, padL + plotW, y + hBar + 6);

                        // 막대
                        var rectBar = new Rectangle(padL, y, Math.Max(1, wBar), hBar);
                        g.FillRectangle(bar, rectBar);

                        // ◀ 라벨 (플롯 왼쪽, 화면 안쪽에서 시작)
                        string lab = it.Key ?? "";
                        var szL = g.MeasureString(lab, fLab);
                        float xL = padL - 12 - szL.Width;                // 플롯 왼쪽에서 12px 여유
                        float yL = y + hBar / 2f - szL.Height / 2f;
                        g.DrawString(lab, fLab, Brushes.Black, xL, yL);

                        // ▶ 값/퍼센트(막대 내부 흰색)
                        string txt = ngSum > 0 ? $"{it.Value} ({(it.Value * 100.0 / ngSum):0.0}%)" : it.Value.ToString();
                        var szV = g.MeasureString(txt, fVal);
                        float xV = padL + wBar - 6 - szV.Width;          // 기본: 막대 오른쪽 안쪽
                        float yV = y + hBar / 2f - szV.Height / 2f;

                        if (wBar < szV.Width + 12) xV = padL + 6;          // 막대가 짧으면 왼쪽 내부

                        using (var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                            g.DrawString(txt, fVal, shadow, xV + 1, yV + 1);
                        g.DrawString(txt, fVal, Brushes.White, xV, yV);
                    }
                }
            }
            return bmp;
        }

        // 작은 래퍼
        static PdfPCell Wrap(PdfPTable inner) => new PdfPCell(inner) { Border = PdfPCell.NO_BORDER, Padding = 0f };
        static PdfPCell Wrap(PdfParagraph p) { var c = new PdfPCell(); c.AddElement(p); c.Border = PdfPCell.NO_BORDER; c.Padding = 0f; return c; }
    }
}
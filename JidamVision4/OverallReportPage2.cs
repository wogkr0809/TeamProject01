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

        // src의 중심을 기준으로 box 크기를 "가득 채우도록" 크롭+스케일 (배경 잘라내기)
        static Bitmap RenderCoverBitmap(Bitmap src, int outWpx, int outHpx)
        {
            if (src == null) return null;
            outWpx = Math.Max(10, outWpx);
            outHpx = Math.Max(10, outHpx);

            double sx = outWpx / (double)src.Width;
            double sy = outHpx / (double)src.Height;
            double s = Math.Max(sx, sy);                // cover

            int cropW = (int)Math.Round(outWpx / s);
            int cropH = (int)Math.Round(outHpx / s);
            int cx = src.Width / 2;
            int cy = src.Height / 2;

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

                // ⛔ 테두리 그리던 라인 삭제:
                // g.DrawRectangle(Pens.Gainsboro, 0, 0, outWpx - 1, outHpx - 1);
            }
            return dst;
        }

        public static void Build(PdfDocument doc, OverallReportContext ctx)
        {
            AddHeader(doc, ctx);

            // 상단 2열 (왼쪽: 통계/표, 오른쪽: 그래프)
            var twoCols = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 8f };
            twoCols.SetWidths(new float[] { 46f, 54f }); // 시안 비율

            // (좌) KPI + 섹션 타이틀 + 표
            var left = new PdfPTable(1) { WidthPercentage = 100 };
            left.AddCell(SectionTitle("검사 통계"));
            left.AddCell(KpiRow("Total", ctx.Total));
            left.AddCell(KpiRow("OK", ctx.Ok));
            left.AddCell(KpiRow("NG", ctx.Ng));

            // KPI와 다음 섹션 타이틀 사이 간격 확보(시안처럼 더 띄움)
            left.AddCell(Spacer(4f));
            left.AddCell(SectionTitle("불량 유형별 비율"));

            left.AddCell(DefectTable(ctx.DefectCounts));

            // 왼쪽 컬럼 전체를 살짝 아래로 내려 표 아래 여백 최소화 & 오른쪽 노트와 끝단 맞춤
            twoCols.AddCell(new PdfPCell(left)
            {
                Border = PdfPCell.NO_BORDER,
                PaddingTop = 10f,   // ← 필요시 8~12f 사이로 미세조정
                PaddingRight = 10f,
                PaddingBottom = 0f
            });

            // (우) 막대그래프 + 설명
            var right = new PdfPTable(1) { WidthPercentage = 100 };

            // 우측 열의 실제 폭(포인트) 계산 후, 300DPI 비트맵을 정확 크기로 배치
            float usableWpt = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin;
            float rightColWpt = usableWpt * 0.54f - 12f; // 열 비율 - 내부 여유
            float chartHeightPt = 240f;

            int chartWpx = (int)Math.Round(rightColWpt / 72f * 300f);
            int chartHpx = (int)Math.Round(chartHeightPt / 72f * 300f);

            Bitmap chartBmp = ctx.BarChartBitmap ?? RenderBarChart(ctx.DefectCounts, chartWpx, chartHpx);
            PdfImage chartImg = ToPdfImageExact(chartBmp, rightColWpt, chartHeightPt, 300);

            var chartBlock = new PdfPTable(1) { WidthPercentage = 100 };
            chartBlock.AddCell(new PdfPCell(new PdfPhrase("불량 유형별 분포 (NG 기준)", F(12, PdfFont.BOLD)))
            { Border = PdfPCell.NO_BORDER, PaddingLeft = 4f, PaddingRight = 4f, PaddingTop = 2f, PaddingBottom = 2f });

            // 얇은 구분선(시안 느낌)
            chartBlock.AddCell(new PdfPCell() { Border = PdfPCell.BOTTOM_BORDER, BorderColorBottom = new PdfBaseColor(210, 210, 210), Padding = 0f, FixedHeight = 2f });

            chartBlock.AddCell(new PdfPCell(chartImg)
            { Border = PdfPCell.NO_BORDER, Padding = 6f, HorizontalAlignment = PdfElement.ALIGN_LEFT });

            right.AddCell(new PdfPCell(chartBlock) { Border = PdfPCell.NO_BORDER, PaddingLeft = 0f, PaddingRight = 0f, PaddingBottom = 2f });

            var note = new PdfParagraph(
                "• 내림차순 가로 막대 그래프입니다.\n• 값 옆 괄호는 각 유형의 비율입니다.\n• 총 NG 기준 산출.",
                F(9));
            var noteCell = new PdfPCell(); noteCell.AddElement(note);
            noteCell.Border = PdfPCell.NO_BORDER;
            noteCell.PaddingTop = 4f;
            noteCell.PaddingLeft = 2f;
            noteCell.PaddingBottom = 1f; // 하단 과도 여백 방지
            right.AddCell(noteCell);

            twoCols.AddCell(new PdfPCell(right) { Border = PdfPCell.NO_BORDER, PaddingLeft = 10f, PaddingRight = 6f });
            doc.Add(twoCols);

            // ===== 하단 3×2 슬롯 (한 페이지 유지 + 타일 내부에 cover 크롭) =====
            float pageUsableW = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin;
            float tileWpt = (pageUsableW / 3f) - 8f;   // 3열 균등 + 소폭 여유
            float tileHpt = 130f;                      // 높이를 고정해야 다음 장으로 안 넘어감

            var grid = new PdfPTable(3) { WidthPercentage = 100, SpacingBefore = 12f, KeepTogether = true };
            grid.SetWidths(new float[] { 1f, 1f, 1f });

            string[] labels = new string[] { "original", "Chip", "Lead", "Resistance", "Scratch", "Soldering" };
            for (int i = 0; i < labels.Length; i++)
            {
                ctx.GalleryImages.TryGetValue(labels[i], out Bitmap bmp);
                grid.AddCell(ImageTile(labels[i], bmp, tileWpt, tileHpt));  // ← 크기 지정 버전
            }
            doc.Add(grid);
        }

        // ========== Convenience: 파일명만으로 Page2 만들기 ==========
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

        /// <summary>
        /// ctx.GalleryImages를 파일 경로들로 채워준다(없으면 null 유지).
        /// </summary>
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

        /// <summary>
        /// 파일명만 넘겨서 2번째 페이지를 생성하는 오버로드.
        /// </summary>
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

        // ---------- Components ----------
        static PdfPCell SectionTitle(string text)
        {
            return new PdfPCell(new PdfPhrase(text, F(11, PdfFont.BOLD, new PdfBaseColor(60, 60, 60))))
            { Border = PdfPCell.NO_BORDER, PaddingTop = 2f, PaddingBottom = 6f };
        }

        static PdfPCell Spacer(float h)
        {
            return new PdfPCell() { Border = PdfPCell.NO_BORDER, FixedHeight = h, Padding = 0f };
        }

        static PdfPCell KpiRow(string title, int value)
        {
            var inner = new PdfPTable(2) { WidthPercentage = 100 };
            inner.SetWidths(new float[] { 1f, 0.6f });

            inner.AddCell(new PdfPCell(new PdfPhrase(title, F(12, PdfFont.BOLD)))
            { Border = PdfPCell.NO_BORDER, Padding = 6f });

            inner.AddCell(new PdfPCell(new PdfPhrase(value.ToString(), F(18, PdfFont.BOLD)))
            { Border = PdfPCell.NO_BORDER, Padding = 6f, HorizontalAlignment = PdfElement.ALIGN_RIGHT });

            return new PdfPCell(inner)
            {
                BackgroundColor = new PdfBaseColor(245, 248, 255),
                BorderColor = new PdfBaseColor(40, 80, 200),
                BorderWidth = 1.2f,
                PaddingTop = 3f,   // 상단 살짝 띄움
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
            return new PdfPCell(tbl)
            {
                Border = PdfPCell.NO_BORDER,
                PaddingTop = 6f,
                PaddingBottom = 0f // 표 아래 여백 제거
            };
        }

        static PdfPCell HeaderCell(string text)
        {
            return new PdfPCell(new PdfPhrase(text, F(11, PdfFont.BOLD)))
            { BackgroundColor = new PdfBaseColor(245, 245, 245), Padding = 6f };
        }

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
                img.ScaleAbsolute(widthPt, heightPt); // 포인트 절대 크기
                img.Alignment = PdfElement.ALIGN_LEFT;
                return img;
            }
        }

        static PdfPCell ImageTile(string label, Bitmap bmp, float boxWpt, float boxHpt)
        {
            var panel = new PdfPTable(1) { WidthPercentage = 100 };

            // 라벨(유지)
            panel.AddCell(new PdfPCell(new PdfPhrase(label, F(11, PdfFont.BOLD)))
            {
                Border = PdfPCell.NO_BORDER,
                PaddingLeft = 6f,
                PaddingTop = 2f,
                PaddingBottom = 2f
            });

            PdfPCell imgCell;
            if (bmp != null)
            {
                int wpx = (int)Math.Round(boxWpt / 72f * 300f);
                int hpx = (int)Math.Round(boxHpt / 72f * 300f);
                using (var cov = RenderCoverBitmap(bmp, wpx, hpx))
                {
                    var it = ToPdfImageExact(cov, boxWpt, boxHpt, 300);
                    imgCell = new PdfPCell(it)
                    {
                        // ⛔ 테두리 삭제
                        Border = PdfPCell.NO_BORDER,
                        Padding = 0f,
                        HorizontalAlignment = PdfElement.ALIGN_CENTER
                    };
                }
            }
            else
            {
                // 빈 슬롯은 시각적 안내만 유지(보더 없음)
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
        }

        // (호환용 오버로드 유지 시)
        static PdfPCell ImageTile(string label, Bitmap bmp)
        {
            return ImageTile(label, bmp, 160f, 130f);
        }

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

                // 라벨을 더 크게 쓰므로 왼쪽 여백을 넉넉히
                int padL = 140, padR = 30, padT = 18, padB = 28;
                int plotW = width - padL - padR;
                int plotH = height - padT - padB;
                int rowH = Math.Max(1, plotH / n);

                using (var fLab = new System.Drawing.Font("Malgun Gothic", 14f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point))
                using (var fVal = new System.Drawing.Font("Malgun Gothic", 14f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point))
                using (var bar = new SolidBrush(Color.FromArgb(54, 97, 214)))
                using (var grid = new Pen(Color.Gainsboro, 1f))
                {
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

                        // ◀ 카테고리 라벨 (오른쪽 정렬 느낌으로 플롯 왼쪽에 배치)
                        string lab = it.Key ?? "";
                        SizeF szLab = g.MeasureString(lab, fLab);
                        float xLab = padL - 12 - szLab.Width;               // 플롯 왼쪽에서 12px 여유
                        float yLab = y + hBar / 2f - szLab.Height / 2f;
                        g.DrawString(lab, fLab, Brushes.Black, xLab, yLab);

                        // ▶ 값/퍼센트 텍스트 (막대 "안쪽"에 흰색, 짧으면 안쪽 왼쪽)
                        string txt = ngSum > 0 ? $"{it.Value} ({(it.Value * 100.0 / ngSum):0.0}%)" : it.Value.ToString();
                        SizeF szVal = g.MeasureString(txt, fVal);

                        float xVal = padL + wBar - 6 - szVal.Width;         // 기본: 막대 오른쪽 내부 6px
                        float yVal = y + hBar / 2f - szVal.Height / 2f;

                        if (wBar < szVal.Width + 12)                        // 막대가 짧으면 왼쪽 내부로
                            xVal = padL + 6;

                        // 약한 그림자 + 흰색 본문(가독성)
                        using (var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                            g.DrawString(txt, fVal, shadow, xVal + 1, yVal + 1);
                        g.DrawString(txt, fVal, Brushes.White, xVal, yVal);
                    }
                }
            }
            return bmp;
        }

        // 작은 래퍼
        static PdfPCell Wrap(PdfPTable inner) { return new PdfPCell(inner) { Border = PdfPCell.NO_BORDER, Padding = 0f }; }
        static PdfPCell Wrap(PdfParagraph p) { var c = new PdfPCell(); c.AddElement(p); c.Border = PdfPCell.NO_BORDER; c.Padding = 0f; return c; }
    }
}
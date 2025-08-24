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
        public static void Build(PdfDocument doc, OverallReportContext ctx)
        {
            AddHeader(doc, ctx);

            var twoCols = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 6f };
            twoCols.SetWidths(new float[] { 48f, 52f });

            // (좌) KPI + 표
            var left = new PdfPTable(1) { WidthPercentage = 100 };
            left.AddCell(KpiRow("Total", ctx.Total));
            left.AddCell(KpiRow("OK", ctx.Ok));
            left.AddCell(KpiRow("NG", ctx.Ng));
            left.AddCell(DefectTable(ctx.DefectCounts));
            twoCols.AddCell(new PdfPCell(left) { Border = PdfPCell.NO_BORDER, PaddingRight = 6f });

            // (우) 막대그래프 + 설명
            var right = new PdfPTable(1) { WidthPercentage = 100 };
            Bitmap chartBmp = ctx.BarChartBitmap ?? RenderBarChart(ctx.DefectCounts, 1400, 420);
            PdfImage chartImg = ToPdfImageFit(chartBmp, 9999f, 230f);

            var chartBlock = new PdfPTable(1) { WidthPercentage = 100 };
            chartBlock.AddCell(new PdfPCell(new PdfPhrase("불량 유형별 분포 (NG 기준)", F(12, PdfFont.BOLD)))
            { Border = PdfPCell.NO_BORDER, Padding = 4f, PaddingBottom = 2f });
            chartBlock.AddCell(new PdfPCell(chartImg)
            { BorderColor = new PdfBaseColor(210, 210, 210), Padding = 6f, HorizontalAlignment = PdfElement.ALIGN_LEFT });
            right.AddCell(new PdfPCell(chartBlock) { Border = PdfPCell.NO_BORDER, PaddingLeft = 0f, PaddingRight = 0f, PaddingBottom = 2f });

            var note = new PdfParagraph(  
                "• 내림차순 가로 막대 그래프입니다.\n• 값 옆 괄호는 각 유형의 비율입니다.\n• 총 NG 기준 산출.",
                F(9));
            right.AddCell(Wrap(note));

           twoCols.AddCell(new PdfPCell(right) { Border = PdfPCell.NO_BORDER, PaddingLeft = 6f, PaddingRight = 6f });

            doc.Add(twoCols);

            // 하단 3×2 슬롯
            var grid = new PdfPTable(3) { WidthPercentage = 100, SpacingBefore = 6f, KeepTogether = true };
            grid.SetWidths(new float[] { 1f, 1f, 1f });

            string[] labels = new string[] { "original", "Chip", "Lead", "Resistance", "Scratch", "Soldering" };
            for (int i = 0; i < labels.Length; i++)
            {
                Bitmap bmp;
                ctx.GalleryImages.TryGetValue(labels[i], out bmp);
                grid.AddCell(ImageTile(labels[i], bmp));
            }

            doc.Add(grid);
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
        static PdfPCell KpiRow(string title, int value)
        {
            var inner = new PdfPTable(2) { WidthPercentage = 100 };
            inner.SetWidths(new float[] { 1f, 0.6f });

            inner.AddCell(new PdfPCell(new PdfPhrase(title, F(12, PdfFont.BOLD)))
            { Border = PdfPCell.NO_BORDER, Padding = 6f });

            inner.AddCell(new PdfPCell(new PdfPhrase(value.ToString(), F(18, PdfFont.BOLD)))
            { Border = PdfPCell.NO_BORDER, Padding = 6f, HorizontalAlignment = PdfElement.ALIGN_RIGHT });

            // ⚠ PdfPCell에는 SpacingAfter가 없습니다 → PaddingBottom으로 간격 확보
            return new PdfPCell(inner)
            {
                BackgroundColor = new PdfBaseColor(245, 248, 255),
                BorderColor = new PdfBaseColor(40, 80, 200),
                BorderWidth = 1.2f,
                Padding = 2f,
                PaddingBottom = 6f,
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

            return new PdfPCell(tbl) { Border = PdfPCell.NO_BORDER, PaddingTop = 6f };
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

        static PdfImage ToPdfImageFit(Bitmap bmp, float boxWpt, float boxHpt)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var img = PdfImage.GetInstance(ms.ToArray());
                img.ScaleToFit(boxWpt, boxHpt);
                img.Alignment = PdfElement.ALIGN_CENTER;
                return img;
            }
        }

        static PdfPCell ImageTile(string label, Bitmap bmp)
        {
            var panel = new PdfPTable(1) { WidthPercentage = 100 };
            panel.AddCell(new PdfPCell(new PdfPhrase(label, F(11, PdfFont.BOLD)))
            { Border = PdfPCell.NO_BORDER, PaddingLeft = 6f, PaddingTop = 2f, PaddingBottom = 2f });

            PdfPCell imgCell;
            if (bmp != null)
            {
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var it = PdfImage.GetInstance(ms.ToArray());
                    it.ScaleToFit(9999f, 150f);
                    imgCell = new PdfPCell(it)
                    {
                        BorderColor = new PdfBaseColor(200, 200, 200),
                        Padding = 6f,
                        HorizontalAlignment = PdfElement.ALIGN_CENTER
                    };
                }
            }
            else
            {
                imgCell = new PdfPCell(new PdfPhrase("이미지 없음", F(10, PdfFont.ITALIC, new PdfBaseColor(120, 120, 120))))
                {
                    FixedHeight = 130f,
                    Border = PdfPCell.BOX,
                    BorderColor = new PdfBaseColor(200, 200, 200),
                    HorizontalAlignment = PdfElement.ALIGN_CENTER,
                    VerticalAlignment = PdfElement.ALIGN_MIDDLE
                };
            }
            panel.AddCell(imgCell);

            return new PdfPCell(panel) { Border = PdfPCell.NO_BORDER, Padding = 4f };
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

        // ➕ 색상까지 지정하는 오버로드 (컴파일 에러 원인 해결)
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

                int padL = 90, padR = 60, padT = 20, padB = 30;
                int plotW = width - padL - padR;
                int plotH = height - padT - padB;
                int rowH = plotH / n;

                using (var fLab = new System.Drawing.Font("Malgun Gothic", 10f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point))
                using (var fVal = new System.Drawing.Font("Malgun Gothic", 10f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point))
                using (var bar = new SolidBrush(Color.FromArgb(54, 97, 214)))
                using (var grid = new Pen(Color.Gainsboro, 1f))
                {
                    for (int i = 0; i < n; i++)
                    {
                        var it = items[i];
                        int y = padT + i * rowH + rowH / 6;
                        int hBar = (int)(rowH * 0.72);
                        int wBar = (int)(plotW * (it.Value / (double)vmax));

                        g.DrawLine(grid, padL, y + hBar + 6, padL + plotW, y + hBar + 6);
                        g.FillRectangle(bar, padL, y, Math.Max(1, wBar), hBar);

                        g.DrawString(it.Key, fLab, Brushes.Black, 10, y + hBar / 2 - 9);

                        string txt = ngSum > 0 ? $"{it.Value} ({(it.Value * 100.0 / ngSum):0.0}%)" : it.Value.ToString();
                        SizeF sz = g.MeasureString(txt, fVal);
                        g.DrawString(txt, fVal, Brushes.Black, padL + Math.Max(wBar + 6, 6), y + hBar / 2 - sz.Height / 2);
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
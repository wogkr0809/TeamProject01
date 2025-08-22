using JidamVision4.Core;
using JidamVision4.Property;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace JidamVision4.Algorithm
{
    /// <summary>
    /// - 다른 ROI(부품)들을 마스크로 제외
    /// - 제외되지 않은 영역에서만 이진화 기반으로
    ///   1) Scratch(가늘고 긴 선형)  2) Soldering(밝은 비드/스패터) 검출
    /// </summary>
    public class SurfaceDefectAlgorithm : InspAlgorithm
    {

        public Bitmap CustomMask { get; set; }
        // ───── 수동 마스크(검정=제외, 흰색=검사) ─────
        public bool UseManualMask { get; set; } = false;
        public string ManualMaskPath { get; set; } = "";

        public int MaskGrow { get; set; }                             // 제외영역 팽창(px)

        // ===== [스크래치] =====
        public bool EnableScratch { get; set; } = true;
        public int ScratchTopHat { get; set; }     // 선 강조(홀수)
        public int ScratchBinThr { get; set; }     // 0=Otsu
        public int ScratchDilate { get; set; }
        public int ScratchMinLen { get; set; }    // 길이(픽셀)
        public int ScratchMaxWidth { get; set; }     // 최대폭(픽셀)
        public double ScratchMinRatio { get; set; }  // 길이/폭 비

        public bool ScratchUseListContours { get; set; } = true; // true=RETR_LIST, false=RETR_EXTERNAL
        public ContourApproximationModes ScratchApprox { get; set; } = ContourApproximationModes.ApproxSimple;
        // ===== [납땜 이물] =====
        public bool EnableSolder { get; set; } = true;
        public int SolderThr { get; set; }  // 밝은 금속 임계값 (그레이)
        public int SolderOpen { get; set; }      // 잡티 제거(open 반복)
        public int SolderMinArea { get; set; }
        public int SolderMaxArea { get; set; }
        public double SolderMinCirc { get; set; } = 0.45;  // 4πA/P²

        // (선택) 핀 갭 채움 검출이 필요하면 세팅
        public List<Rect> GapRects { get; set; } = new List<Rect>();
        public int BridgeDilate { get; set; } = 1;
        public int BridgeMinFilled { get; set; } = 60;

        private readonly List<DrawInspectInfo> _rects = new List<DrawInspectInfo>();

        public SurfaceDefectAlgorithm()
        {
            //#ABSTRACT ALGORITHM#2 각 함수마다 자신의 알고리즘 타입 설정
            InspectType = InspectType.InspSurfaceDefect;
        }

        // --- 베이스 요구사항 구현 ---
        public override InspAlgorithm Clone()
        {
            var x = new SurfaceDefectAlgorithm();
            x.CopyFrom(this);
            return x;
        }

        public override bool CopyFrom(InspAlgorithm src)
        {
            var s = src as SurfaceDefectAlgorithm;
            if (s == null) return false;

            UseManualMask = s.UseManualMask;
            ManualMaskPath = s.ManualMaskPath;

            MaskGrow = s.MaskGrow;

            EnableScratch = s.EnableScratch;
            ScratchTopHat = s.ScratchTopHat;
            ScratchBinThr = s.ScratchBinThr;
            ScratchDilate = s.ScratchDilate;
            ScratchMinLen = s.ScratchMinLen;
            ScratchMaxWidth = s.ScratchMaxWidth;
            ScratchMinRatio = s.ScratchMinRatio;

            EnableSolder = s.EnableSolder;
            SolderThr = s.SolderThr;
            SolderOpen = s.SolderOpen;
            SolderMinArea = s.SolderMinArea;
            SolderMaxArea = s.SolderMaxArea;
            SolderMinCirc = s.SolderMinCirc;

            BridgeDilate = s.BridgeDilate;
            BridgeMinFilled = s.BridgeMinFilled;

            GapRects = new List<Rect>(s.GapRects);
            return true;
        }

        public override bool DoInspect()
        {
            _rects.Clear();

            // ROI(그레이) 확보 실패 시 바로 false
            Mat gray;
            if (!TryGetGrayRoi(out gray)) return false;

            // 검사 마스크(흰=검사, 검정=제외)
            Mat inspectMask = BuildInspectMask(gray.Size());
            if (inspectMask == null || inspectMask.Empty())
                inspectMask = new Mat(gray.Size(), MatType.CV_8UC1, Scalar.All(255));

            // ── Scratch ──
            if (EnableScratch)
            {
                var ksize = new OpenCvSharp.Size((ScratchTopHat | 1), (ScratchTopHat | 1));
                using (var k = Cv2.GetStructuringElement(MorphShapes.Rect, ksize))
                using (var enhB = new Mat())
                using (var enhD = new Mat())
                using (var enh = new Mat())
                using (var bin = new Mat())
                using (var valid = new Mat())
                {
                    Cv2.MorphologyEx(gray, enhB, MorphTypes.TopHat, k);
                    Cv2.MorphologyEx(gray, enhD, MorphTypes.BlackHat, k);
                    Cv2.Max(enhB, enhD, enh);

                    if (ScratchBinThr <= 0)
                        Cv2.Threshold(enh, bin, 0, 255, ThresholdTypes.Otsu);
                    else
                        Cv2.Threshold(enh, bin, ScratchBinThr, 255, ThresholdTypes.Binary);

                    if (ScratchDilate > 0)
                    {
                        using (var kd = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                            Cv2.Dilate(bin, bin, kd, iterations: ScratchDilate);
                    }

                    Cv2.BitwiseAnd(bin, inspectMask, valid);

                    OpenCvSharp.Point[][] cont; OpenCvSharp.HierarchyIndex[] hier;
                    Cv2.FindContours(valid, out cont, out hier, RetrievalModes.External, ContourApproximationModes.ApproxNone);

                    foreach (var c in cont)
                    {
                        if (c.Length < 5) continue;
                        var rr = Cv2.MinAreaRect(c);
                        double w = rr.Size.Width, h = rr.Size.Height;
                        double width = Math.Min(w, h), len = Math.Max(w, h);
                        if (len < ScratchMinLen || width > ScratchMaxWidth) continue;
                        double ratio = (width < 1e-3) ? 999 : (len / width);
                        if (ratio < ScratchMinRatio) continue;

                        var b = rr.BoundingRect();
                        _rects.Add(new DrawInspectInfo
                        {
                            rect = new Rect(b.X + InspRect.X, b.Y + InspRect.Y, b.Width, b.Height),
                            inspectType = InspectType.InspBinary,
                            decision = DecisionType.Defect,
                            info = "Scratch",
                            color = System.Drawing.Color.Red,
                        });
                    }
                }
            }

            // ── Soldering ──
            if (EnableSolder)
            {
                using (var bin = new Mat())
                using (var valid = new Mat())
                {
                    Cv2.Threshold(gray, bin, SolderThr, 255, ThresholdTypes.Binary);
                    if (SolderOpen > 0)
                    {
                        using (var k = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                            Cv2.MorphologyEx(bin, bin, MorphTypes.Open, k, iterations: SolderOpen);
                    }

                    Cv2.BitwiseAnd(bin, inspectMask, valid);

                    OpenCvSharp.Point[][] cont; OpenCvSharp.HierarchyIndex[] hier;

                    var retr = ScratchUseListContours ? RetrievalModes.List : RetrievalModes.External;
                    Cv2.FindContours(valid, out cont, out hier, retr, ScratchApprox);

                    for (int i = 0; i < cont.Length; i++)
                    {
                        double area = Cv2.ContourArea(cont[i]);
                        if (area < SolderMinArea || area > SolderMaxArea) continue;
                        double per = Math.Max(Cv2.ArcLength(cont[i], true), 1e-3);
                        double circ = 4.0 * Math.PI * area / (per * per);
                        if (circ < SolderMinCirc) continue;

                        var b = Cv2.BoundingRect(cont[i]);
                        _rects.Add(new DrawInspectInfo
                        {
                            rect = new Rect(b.X + InspRect.X, b.Y + InspRect.Y, b.Width, b.Height),
                            inspectType = InspectType.InspBinary,
                            decision = DecisionType.Defect,
                            info = "Soldering",
                            color = System.Drawing.Color.OrangeRed,
                        });
                    }
                }
            }

            // ── (옵션) 브리지 ──
            if (GapRects.Count > 0)
            {
                using (var metal = new Mat())
                {
                    Cv2.Threshold(gray, metal, SolderThr, 255, ThresholdTypes.Binary);
                    if (BridgeDilate > 0)
                    {
                        using (var k = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                            Cv2.Dilate(metal, metal, k, iterations: BridgeDilate);
                    }

                    foreach (var g in GapRects)
                    {
                        var loc = new OpenCvSharp.Rect(g.X - InspRect.X, g.Y - InspRect.Y, g.Width, g.Height);
                        var inter = loc & new OpenCvSharp.Rect(0, 0, metal.Width, metal.Height);
                        if (inter.Width <= 0 || inter.Height <= 0) continue;

                        using (var sub = new Mat(metal, inter))
                        {
                            if (Cv2.CountNonZero(sub) >= BridgeMinFilled)
                            {
                                _rects.Add(new DrawInspectInfo
                                {
                                    rect = new Rect(g.X, g.Y, g.Width, g.Height),
                                    inspectType = InspectType.InspBinary,
                                    decision = DecisionType.Defect,
                                    info = "SolderBridge",
                                    color = System.Drawing.Color.Red,
                                });
                            }
                        }
                    }
                }
            }

            // 정리 및 반환
            gray.Dispose();
            if (inspectMask != null) inspectMask.Dispose();
            this.IsDefect = (_rects.Count > 0);
            return true;
        }

        // ── 수동 마스크 읽어서 ROI크기로 반환(흰=검사, 검정=제외) ──
        private Mat BuildInspectMask(OpenCvSharp.Size roiSize)
        {
            // 수동 마스크 사용 체크 + (1) 메모리에 있는 CustomMask가 있으면 그것부터 사용
            if (UseManualMask)
            {
                if (CustomMask != null)
                {
                    using (var bm = new Bitmap(CustomMask)) // clone 안전
                    {
                        var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bm);
                        Mat gray = (src.Channels() == 1) ? src.Clone() : new Mat();
                        if (src.Channels() != 1) Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

                        Mat mask = new Mat();
                        if (gray.Size() != roiSize)
                            Cv2.Resize(gray, mask, roiSize, 0, 0, InterpolationFlags.Nearest);
                        else
                            mask = gray;

                        Cv2.Threshold(mask, mask, 127, 255, ThresholdTypes.Binary); // 흰=검사, 검정=제외
                        return mask;
                    }
                }

                // (2) 파일 경로가 있으면 파일 사용
                if (!string.IsNullOrEmpty(ManualMaskPath) && File.Exists(ManualMaskPath))
                {
                    var raw = Cv2.ImRead(ManualMaskPath, ImreadModes.Grayscale);
                    if (raw.Empty()) return new Mat(roiSize, MatType.CV_8UC1, Scalar.All(255));

                    Mat mask = new Mat();
                    if (raw.Size() != roiSize)
                        Cv2.Resize(raw, mask, roiSize, 0, 0, InterpolationFlags.Nearest);
                    else
                        mask = raw;

                    Cv2.Threshold(mask, mask, 127, 255, ThresholdTypes.Binary);
                    return mask;
                }
            }

            // 수동 마스크 미사용 or 없음 → 전체 허용(흰)
            return new Mat(roiSize, MatType.CV_8UC1, Scalar.All(255));
        }

        /// <summary>현재 채널 원본에서 ROI를 잘라 GRAY로 반환</summary>
        private bool TryGetGrayRoi(out Mat gray)
        {
            gray = null;

            // 현재 채널의 원본 Mat
            var whole = Global.Inst.InspStage.GetMat(0, this.ImageChannel);
            if (whole == null || whole.Empty()) return false;

            // ROI(검사 Rect)로 자르기
            var r = new OpenCvSharp.Rect(InspRect.X, InspRect.Y, InspRect.Width, InspRect.Height);
            var roi = new Mat(whole, r); // whole은 외부 소유, roi는 우리가 Dispose

            if (roi.Channels() == 1)
            {
                gray = roi; // 그대로 반환 (호출부에서 using(gray)로 정리)
                return true;
            }

            // 컬러라면 GRAY로 변환
            gray = new Mat();
            Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);
            roi.Dispose();
            return true;
        }
        public override int GetResultRect(out List<DrawInspectInfo> resultArea)
        {
            resultArea = _rects;
            return _rects.Count;
        }
    }
}

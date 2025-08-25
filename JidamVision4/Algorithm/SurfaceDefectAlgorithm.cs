using JidamVision4.Core;
using JidamVision4.Property;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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
        public int MaskGrow { get; set; } = 0;                         // 제외영역 팽창(px)

        // ===== [스크래치] =====
        public bool EnableScratch { get; set; } = true;
        public int ScratchTopHat { get; set; }     // 선 강조(홀수)
        public int ScratchBinThr { get; set; }     // 0=Otsu
        public int ScratchDilate { get; set; }
        public int ScratchMinLen { get; set; }    // 길이(픽셀)
        public int ScratchMaxWidth { get; set; }     // 최대폭(픽셀)
        public double ScratchMinRatio { get; set; }  // 길이/폭 비
        public int ScratchOpen { get; set; } = 0;      // 작은 점 제거(팽창+침식)

        // ===== [납땜 이물] =====
        public bool EnableSolder { get; set; } = true;
        public int SolderThr { get; set; }  // 밝은 금속 임계값 (그레이)
        public int SolderOpen { get; set; }     
        public int SolderMinArea { get; set; }
        public int SolderMaxArea { get; set; }
        public double SolderMinCirc { get; set; } = 0.45;  // 4πA/P²

        public bool ScratchUseListContours { get; set; } = true;

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


            using (Mat inspectMask = BuildInspectMask(gray.Size()))
            {
                // ───────────── Scratch ─────────────
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
                        // 선/홈 강조
                        Cv2.MorphologyEx(gray, enhB, MorphTypes.TopHat, k);
                        Cv2.MorphologyEx(gray, enhD, MorphTypes.BlackHat, k);
                        Cv2.Max(enhB, enhD, enh);

                        // 이진화
                        if (ScratchBinThr <= 0) Cv2.Threshold(enh, bin, 0, 255, ThresholdTypes.Otsu);
                        else Cv2.Threshold(enh, bin, ScratchBinThr, 255, ThresholdTypes.Binary);

                        // 팽창(선 연결)
                        if (ScratchDilate > 0)
                        {
                            using (var kd = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                                Cv2.Dilate(bin, bin, kd, iterations: ScratchDilate);
                        }

                        // **옵션: 작은 점 제거(Open)**
                        if (ScratchOpen > 0)
                        {
                            using (var ko = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                                Cv2.MorphologyEx(bin, bin, MorphTypes.Open, ko, iterations: ScratchOpen);
                        }

                        // **마스크 적용: 흰(검사) 영역만 남기기**
                        Cv2.BitwiseAnd(bin, inspectMask, valid);

                        // **컨투어 모드: List(내부 홀 포함) / External(외곽만)**
                        var retr = ScratchUseListContours ? RetrievalModes.List : RetrievalModes.External;

                        OpenCvSharp.Point[][] cont; OpenCvSharp.HierarchyIndex[] hier;
                        Cv2.FindContours(valid, out cont, out hier, retr, ContourApproximationModes.ApproxSimple);

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
                                inspectType = InspectType.InspSurfaceDefect,   // ← SurfaceDefect로 명확히
                                decision = DecisionType.Defect,
                                info = "Scratch",
                                color = System.Drawing.Color.Red,
                            });
                        }
                    }
                }

                // ───────────── Soldering ─────────────
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

                        // **마스크 적용**
                        Cv2.BitwiseAnd(bin, inspectMask, valid);


                        // **납땜도 List로**(원/내부가 중요한 경우) – 필요 없으면 External로 바꿔도 됨
                        var retr = ScratchUseListContours ? RetrievalModes.List : RetrievalModes.External;

                        OpenCvSharp.Point[][] cont; OpenCvSharp.HierarchyIndex[] hier;
                        Cv2.FindContours(valid, out cont, out hier, retr, ContourApproximationModes.ApproxSimple);

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
                                inspectType = InspectType.InspSurfaceDefect,
                                decision = DecisionType.Defect,
                                info = "Soldering",
                                color = System.Drawing.Color.OrangeRed,
                            });
                        }
                    }
                }
            } // using (inspectMask)

            // 정리 & 결과
            gray.Dispose();
            this.IsDefect = (_rects.Count > 0);
            return true;
        }

        private Mat BuildInspectMask(OpenCvSharp.Size roiSize)
        {// 0) 마스크 미사용 → ROI 전체 흰색(검사)
            if (!UseManualMask || CustomMask == null || CustomMask.Width <= 0 || CustomMask.Height <= 0)
                return new Mat(roiSize, MatType.CV_8UC1, Scalar.All(255));

            // 1) "원본 프레임"의 실제 크기 확보
            //    가능하면 현재 채널의 풀 프레임 크기를 사용(가장 정확)
            //    없으면 CustomMask의 크기를 사용
            OpenCvSharp.Size frameSize;
            try
            {
                var frame = Global.Inst?.InspStage?.GetMat(0, this.ImageChannel);
                if (frame != null && !frame.Empty())
                    frameSize = new OpenCvSharp.Size(frame.Width, frame.Height);
                else
                    frameSize = new OpenCvSharp.Size(CustomMask.Width, CustomMask.Height);
            }
            catch
            {
                frameSize = new OpenCvSharp.Size(CustomMask.Width, CustomMask.Height);
            }

            // 2) Bitmap → Mat (Index 색상표 형식이면 편집 가능한 포맷으로 복사)
            Bitmap src = CustomMask;
            if ((src.PixelFormat & PixelFormat.Indexed) != 0)
            {
                var editable = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(editable))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    g.DrawImageUnscaled(src, 0, 0);
                }
                src = editable;
            }

            Mat fullMask = BitmapConverter.ToMat(src);
            if (!ReferenceEquals(src, CustomMask)) src.Dispose();
            if (fullMask.Empty())
                return new Mat(roiSize, MatType.CV_8UC1, Scalar.All(255));

            // 3) 마스크 해상도가 프레임과 다르면 프레임 크기로 리사이즈(Nearest: 경계 보존)
            if (fullMask.Width != frameSize.Width || fullMask.Height != frameSize.Height)
            {
                using (var tmp = new Mat())
                {
                    Cv2.Resize(fullMask, tmp, frameSize, 0, 0, InterpolationFlags.Nearest);
                    fullMask.Dispose();
                    fullMask = tmp.Clone();
                }
            }

            // 4) ROI와 full 프레임의 교집합(안전 영역) 계산
            var fullRect = new OpenCvSharp.Rect(0, 0, fullMask.Width, fullMask.Height);
            var roiRect = new OpenCvSharp.Rect(InspRect.X, InspRect.Y, roiSize.Width, roiSize.Height);
            var inter = fullRect & roiRect;

            if (inter.Width <= 0 || inter.Height <= 0)
            {
                fullMask.Dispose();
                return new Mat(roiSize, MatType.CV_8UC1, Scalar.All(255));
            }

            // 5) 교집합 영역을 잘라서 Gray → Binary(흰=검사, 검정=제외)
            using (var sub = new Mat(fullMask, inter))
            using (var gray = new Mat())
            {
                if (sub.Channels() == 4)
                    Cv2.CvtColor(sub, gray, ColorConversionCodes.BGRA2GRAY);
                else if (sub.Channels() == 3)
                    Cv2.CvtColor(sub, gray, ColorConversionCodes.BGR2GRAY);
                else
                    sub.CopyTo(gray); // 이미 GRAY

                var interBin = new Mat();
                Cv2.Threshold(gray, interBin, 127, 255, ThresholdTypes.Binary); // 검정=0 제외, 흰=255 검사

                // 6) 여유치(팽창) – 선택사항
                if (MaskGrow > 0)
                {
                    using (var k = Cv2.GetStructuringElement(
                        MorphShapes.Rect,
                        new OpenCvSharp.Size((MaskGrow | 1), (MaskGrow | 1))))
                    {
                        Cv2.Dilate(interBin, interBin, k);
                    }
                }

                // 7) ROI 크기의 마스크에 교집합 위치로 복사(ROI 기준 오프셋 보정)
                var roiMask = new Mat(roiSize, MatType.CV_8UC1, Scalar.All(0));
                var dstRect = new OpenCvSharp.Rect(inter.X - roiRect.X, inter.Y - roiRect.Y, inter.Width, inter.Height);
                using (var dstRoi = new Mat(roiMask, dstRect))
                    interBin.CopyTo(dstRoi);

                fullMask.Dispose();
                return roiMask;
            }
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

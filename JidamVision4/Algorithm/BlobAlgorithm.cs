using OpenCvSharp;
using SaigeVision.Net.V2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JidamVision4.Core;


namespace JidamVision4.Algorithm
{
    public struct BinaryThreshold
    {
        public int lower { get; set; }
        public int upper { get; set; }
        public bool invert { get; set; }

        public BinaryThreshold(int _lower, int _upper, bool _invert)
        {
            lower = _lower;
            upper = _upper;
            invert = _invert;
        }
    }

    public enum BinaryMethod : int
    {
        [Description("필터")]
        Feature,
        [Description("픽셀갯수")]
        PixelCount
    }

    public class BlobFilter
    {
        public string name { get; set; }
        public bool isUse { get; set; }
        public int min { get; set; }
        public int max { get; set; }

        public BlobFilter() { }
    }

    public class BlobAlgorithm : InspAlgorithm
    {
        // --- Scratch-friendly options ---

        // --- Thick object suppression (붙은 저항/패드 제거용) ---
        public bool ExcludeThickBlobs { get; set; } = true; // 사용 여부
        public int ThickKernel { get; set; } = 7;           // 두꺼운 물체 판별 커널(홀수, 5~11 권장)

        // --- Scratch-friendly options ---
        public bool ScratchBoost { get; set; } = true;
        public int ScratchCloseLen { get; set; } = 7;
        public int ScratchDilate { get; set; } = 0;
        public double MinAspectRatio { get; set; } = 3.0;
        public int MinLength { get; set; } = 30;

        public BinaryThreshold BinThreshold { get; set; } = new BinaryThreshold();

        public readonly int FILTER_AREA = 0;
        public readonly int FILTER_WIDTH = 1;
        public readonly int FILTER_HEIGHT = 2;
        public readonly int FILTER_COUNT = 3;

        private List<DrawInspectInfo> _findArea = new List<DrawInspectInfo>();
        public List<string> ResultString { get; private set; } = new List<string>();

        public BinaryMethod BinMethod { get; set; } = BinaryMethod.Feature;
        public bool UseRotatedRect { get; set; } = true;

        private List<BlobFilter> _filterBlobs = new List<BlobFilter>();
        public List<BlobFilter> BlobFilters
        {
            get { return _filterBlobs; }
            set { _filterBlobs = value; }
        }

        public int OutBlobCount { get; set; } = 0;

        public BlobAlgorithm()
        {
            InspectType = InspectType.InspBinary;
            BinThreshold = new BinaryThreshold(100, 200, false);
            EnsureDefaultFilters();
        }

        public override InspAlgorithm Clone()
        {
            var cloneAlgo = new BlobAlgorithm();
            this.CopyBaseTo(cloneAlgo);
            cloneAlgo.CopyFrom(this);
            return cloneAlgo;
        }

        public override bool CopyFrom(InspAlgorithm sourceAlgo)
        {
            BlobAlgorithm blobAlgo = (BlobAlgorithm)sourceAlgo;

            this.BinThreshold = blobAlgo.BinThreshold;
            this.BinMethod = blobAlgo.BinMethod;
            this.UseRotatedRect = blobAlgo.UseRotatedRect;

            this.ScratchBoost = blobAlgo.ScratchBoost;
            this.ScratchCloseLen = blobAlgo.ScratchCloseLen;
            this.ScratchDilate = blobAlgo.ScratchDilate;
            this.MinAspectRatio = blobAlgo.MinAspectRatio;
            this.MinLength = blobAlgo.MinLength;

            this.BlobFilters = (blobAlgo.BlobFilters != null)
                ? blobAlgo.BlobFilters.Select(b => new BlobFilter
                {
                    name = b.name,
                    isUse = b.isUse,
                    min = b.min,
                    max = b.max
                }).ToList()
                : new List<BlobFilter>();

            EnsureDefaultFilters();
            return true;
        }

        private void EnsureDefaultFilters()
        {
            if (_filterBlobs == null) _filterBlobs = new List<BlobFilter>();
            string[] names = { "Area", "Width", "Height", "Count" };
            while (_filterBlobs.Count < 4)
            {
                _filterBlobs.Add(new BlobFilter { name = names[_filterBlobs.Count], isUse = false, min = 0, max = 0 });
            }
        }

        public void SetDefault()
        {
            _filterBlobs = new List<BlobFilter>
            {
                new BlobFilter(){ name="Area",  isUse=false, min=200, max=500 },
                new BlobFilter(){ name="Width", isUse=false, min=0,   max=0   },
                new BlobFilter(){ name="Height",isUse=false, min=0,   max=0   },
                new BlobFilter(){ name="Count", isUse=false, min=0,   max=0   }
            };
        }

        public override bool DoInspect()
        {
            ResetResult();
            OutBlobCount = 0;

            if (_srcImage == null)
                return false;

            if (InspRect.Right > _srcImage.Width ||
                InspRect.Bottom > _srcImage.Height)
                return false;

            Mat targetImage = _srcImage[InspRect];

            Mat grayImage = new Mat();
            if (targetImage.Type() == MatType.CV_8UC3)
                Cv2.CvtColor(targetImage, grayImage, ColorConversionCodes.BGR2GRAY);
            else
                grayImage = targetImage;

            Mat binaryImage = new Mat();
            Cv2.InRange(grayImage, BinThreshold.lower, BinThreshold.upper, binaryImage);
            if (BinThreshold.invert)
                binaryImage = ~binaryImage;

            if (BinaryMethod.PixelCount == BinMethod)
            {
                if (!InspPixelCount(binaryImage))
                    return false;
            }
            else
            {
                if (!InspBlobFilter(binaryImage))
                    return false;
            }

            IsInspected = true;
            return true;
        }

        public override void ResetResult()
        {
            base.ResetResult();
            if (_findArea != null) _findArea.Clear();
            if (ResultString != null) ResultString.Clear();
        }

        private bool InspPixelCount(Mat binImage)
        {
            if (binImage == null || binImage.Empty())
                return false;
            if (binImage.Type() != MatType.CV_8UC1)
                return false;

            if (_findArea == null) _findArea = new List<DrawInspectInfo>();
            _findArea.Clear();
            if (ResultString == null) ResultString = new List<string>();
            ResultString.Clear();

            int pixelCount = Cv2.CountNonZero(binImage);

            IsDefect = false;
            string featureInfo = $"A:{pixelCount}";

            BlobFilter areaFilter = null;
            if (BlobFilters != null && BlobFilters.Count > FILTER_AREA)
                areaFilter = BlobFilters[FILTER_AREA];

            if (areaFilter != null && areaFilter.isUse)
            {
                if ((areaFilter.min > 0 && pixelCount < areaFilter.min) ||
                    (areaFilter.max > 0 && pixelCount > areaFilter.max))
                {
                    IsDefect = true;
                }
            }

            var blobRect = new Rect(InspRect.Left, InspRect.Top, binImage.Width, binImage.Height);
            ResultString.Add($"Blob X:{blobRect.X}, Y:{blobRect.Y}, Size({blobRect.Width},{blobRect.Height})");

            _findArea.Add(new DrawInspectInfo(blobRect, featureInfo, InspectType.InspBinary, DecisionType.Info));
            OutBlobCount = 1;

            if (areaFilter != null)
            {
                string ngok = IsDefect ? "NG" : "OK";
                ResultString.Add($"[{ngok}] Blob count [in:{areaFilter.min},{areaFilter.max}, out:{pixelCount}]");
            }

            return true;
        }

        private bool InspBlobFilter(Mat binImage)
        {
            if (binImage == null || binImage.Empty())
                return false;

            EnsureDefaultFilters();

            if (ExcludeThickBlobs)
            {
                int k = Math.Max(3, (ThickKernel | 1)); // 홀수 보장
                using (var kSq = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(k, k)))
                using (var opened = new Mat())
                using (var notThick = new Mat())
                {
                    // Open: 작은/얇은 것(스크래치)은 사라지고, 두꺼운 밝은 물체만 남음
                    Cv2.MorphologyEx(binImage, opened, MorphTypes.Open, kSq);

                    // 여유폭을 조금 더 주고 싶으면 1회 팽창(선택)
                    // Cv2.Dilate(opened, opened, kSq, iterations: 1);

                    // 두꺼운 것만 있는 마스크를 반전 → "두껍지 않은 영역"(= 얇은 선 + 배경)
                    Cv2.BitwiseNot(opened, notThick);

                    // 원본과 AND → 얇은 선(스크래치)만 남김
                    Cv2.BitwiseAnd(binImage, notThick, binImage);
                }
            }
            // --- [2] (기존) 스크래치 연결 단계 (가로/세로 Close로 잔틈 메움)
            if (ScratchBoost)
            {
                int k = Math.Max(3, (ScratchCloseLen | 1));
                using (var kH = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(k, 1)))
                using (var kV = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, k)))
                {
                    Cv2.MorphologyEx(binImage, binImage, MorphTypes.Close, kH);
                    Cv2.MorphologyEx(binImage, binImage, MorphTypes.Close, kV);
                }
                if (ScratchDilate > 0)
                {
                    using (var kD = Cv2.GetStructuringElement(MorphShapes.Rect,
                                 new Size(2 * ScratchDilate + 1, 2 * ScratchDilate + 1)))
                    {
                        Cv2.Dilate(binImage, binImage, kD, iterations: 1);
                    }
                }
            }
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(binImage, out contours, out hierarchy,
                             RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            if (_findArea == null) _findArea = new List<DrawInspectInfo>();
            _findArea.Clear();

            int findBlobCount = 0;

            for (int i = 0; i < contours.Length; i++)
            {
                var contour = contours[i];
                double area = Cv2.ContourArea(contour);
                if (area <= 0)
                    continue;

                int showArea = 0;
                int showWidth = 0;
                int showHeight = 0;

                BlobFilter areaFilter = (BlobFilters.Count > FILTER_AREA) ? BlobFilters[FILTER_AREA] : null;
                if (areaFilter != null && areaFilter.isUse)
                {
                    if (areaFilter.min > 0 && area < areaFilter.min) continue;
                    if (areaFilter.max > 0 && area > areaFilter.max) continue;
                    showArea = (int)(area + 0.5);
                }

                Rect boundingRect = Cv2.BoundingRect(contour);
                RotatedRect rotatedRect = Cv2.MinAreaRect(contour);

                Size2d blobSize = new Size2d(boundingRect.Width, boundingRect.Height);

                if (UseRotatedRect)
                {
                    float w = rotatedRect.Size.Width;
                    float h = rotatedRect.Size.Height;
                    blobSize.Width = Math.Max(w, h);
                    blobSize.Height = Math.Min(w, h);
                }

                double longSide = blobSize.Width;
                double shortSide = Math.Max(1.0, blobSize.Height);

                if (MinLength > 0 && longSide < MinLength)
                    continue;

                double ratio = longSide / shortSide;
                if (MinAspectRatio > 0 && ratio < MinAspectRatio)
                    continue;

                BlobFilter widthFilter = (BlobFilters.Count > FILTER_WIDTH) ? BlobFilters[FILTER_WIDTH] : null;
                if (widthFilter != null && widthFilter.isUse)
                {
                    if (widthFilter.min > 0 && blobSize.Width < widthFilter.min) continue;
                    if (widthFilter.max > 0 && blobSize.Width > widthFilter.max) continue;
                    showWidth = (int)(blobSize.Width + 0.5);
                }

                BlobFilter heightFilter = (BlobFilters.Count > FILTER_HEIGHT) ? BlobFilters[FILTER_HEIGHT] : null;
                if (heightFilter != null && heightFilter.isUse)
                {
                    if (heightFilter.min > 0 && blobSize.Height < heightFilter.min) continue;
                    if (heightFilter.max > 0 && blobSize.Height > heightFilter.max) continue;
                    showHeight = (int)(blobSize.Height + 0.5);
                }

                findBlobCount++;

                Rect blobRect = new Rect(
                    boundingRect.X + InspRect.X,
                    boundingRect.Y + InspRect.Y,
                    boundingRect.Width,
                    boundingRect.Height);

                string featureInfo = "";
                if (showArea > 0) featureInfo += $"A:{showArea}";
                if (showWidth > 0) featureInfo += (featureInfo == "" ? "" : "\r\n") + $"W:{showWidth}";
                if (showHeight > 0) featureInfo += (featureInfo == "" ? "" : "\r\n") + $"H:{showHeight}";

                ResultString.Add($"Blob X:{blobRect.X}, Y:{blobRect.Y}, Size({blobRect.Width},{blobRect.Height})");

                var rectInfo = new DrawInspectInfo(blobRect, featureInfo, InspectType.InspBinary, DecisionType.Info);

                if (UseRotatedRect)
                {
                    Point2f[] pts = rotatedRect.Points().Select(p => p + (Point2f)InspRect.TopLeft).ToArray();
                    rectInfo.SetRotatedRectPoints(pts);
                }

                _findArea.Add(rectInfo);
            }

            OutBlobCount = findBlobCount;

            IsDefect = false;
            string result = "OK";

            BlobFilter countFilter = (BlobFilters.Count > FILTER_COUNT) ? BlobFilters[FILTER_COUNT] : null;

            if (countFilter != null && countFilter.isUse)
            {
                if (countFilter.min > 0 && findBlobCount < countFilter.min) IsDefect = true;
                if (!IsDefect && countFilter.max > 0 && findBlobCount > countFilter.max) IsDefect = true;
            }
            else
            {
                if (_findArea.Count > 0) IsDefect = true;
            }

            // ✅ Count 텍스트는 항상 추가 (OK면 Info, NG면 Defect 스타일)
            DecisionType dec = IsDefect ? DecisionType.Defect : DecisionType.Info;
            _findArea.Add(new DrawInspectInfo(InspRect, $"Count:{findBlobCount}",
                           InspectType.InspBinary, dec));


            if (IsDefect)
            {
                _findArea.Add(new DrawInspectInfo(InspRect, $"Count:{findBlobCount}", InspectType.InspBinary, DecisionType.Defect));
                result = "NG";

                if (countFilter != null)
                {
                    ResultString.Add($"[{result}] Blob count [in : {countFilter.min},{countFilter.max}, out : {findBlobCount}]");
                }
            }

            return true;
        }

        public override int GetResultRect(out List<DrawInspectInfo> resultArea)
        {
            resultArea = null;

            if (!IsInspected)
                return -1;

            if (_findArea == null || _findArea.Count <= 0)
                return -1;

            resultArea = _findArea;
            return resultArea.Count;
        }
    }
}
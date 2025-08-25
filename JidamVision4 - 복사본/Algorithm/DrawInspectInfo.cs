using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JidamVision4.Core;
using CvPoint = OpenCvSharp.Point;
using CvPoint2f = OpenCvSharp.Point2f;
using CvRect = OpenCvSharp.Rect;
using SDColor = System.Drawing.Color;

namespace JidamVision4.Algorithm
{
    public class DrawInspectInfo
    {
        public Rect rect;
        public Point[] rotatedPoints;
        public string info;
        public InspectType inspectType;
        public DecisionType decision;

        public bool UseRotatedRect = false; // 회전된 Rect를 사용할지 여부

        public System.Drawing.Color color = System.Drawing.Color.LimeGreen; 

        public string Label { get => info; set => info = value; }
        public string NgReason { get => info; set => info = value; }

        public DrawInspectInfo()
        {
            rect = new Rect();
            rotatedPoints = null;
            info = string.Empty;
            inspectType = InspectType.InspNone;
            decision = DecisionType.None;
        }

        public DrawInspectInfo(Rect _rect, string _info, InspectType _inspectType, DecisionType _decision)
        {
            rect = _rect;
            info = _info;
            inspectType = _inspectType;
            decision = _decision;
        }

        public void SetRotatedRectPoints(CvPoint2f[] pts)
        {
            if (pts == null || pts.Length == 0)
            {
                rotatedPoints = null;
                UseRotatedRect = false;
                return;
            }
            rotatedPoints = Array.ConvertAll(pts,
                p => new CvPoint((int)Math.Round(p.X), (int)Math.Round(p.Y)));
            UseRotatedRect = true;
        }
    }
}
